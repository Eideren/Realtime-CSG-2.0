using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using InternalRealtimeCSG;
using RealtimeCSG.Legacy;
using RealtimeCSG.Components;
using RealtimeCSG.Foundation;

namespace RealtimeCSG
{
    internal sealed class GeneratorFreeDraw : GeneratorExtrudedBase, IGenerator
    {
        [NonSerialized] private Vector3 worldPosition;
        [NonSerialized] private Vector3 prevWorldPosition;
        [NonSerialized] private bool onLastPoint = false;
        [NonSerialized] private CSGPlane geometryPlane = new CSGPlane(0, 1, 0, 0);
        [NonSerialized] private CSGPlane? hoverDefaultPlane;

        [NonSerialized] private bool haveDragged = false;
        [NonSerialized] private bool generateSmoothing = true;

        [NonSerialized] private Vector3 prevDragDifference;
        [NonSerialized] private Vector2 clickMousePosition;
        [NonSerialized] private int clickCount = 0;

        // free-draw specific
        private static readonly int ShapeBuilderEdgeHash = "CSGShapeBuilderEdge".GetHashCode();
        private const float handle_on_distance = 4.0f;

        public bool HaveSelectedEdges
        {
            get => CanCommit && settings.HaveSelectedEdges;
        }

        public bool HaveSelectedEdgesOrVertices
        {
            get => CanCommit && (settings.HaveSelectedEdges || settings.HaveSelectedVertices);
        }

        [SerializeField] private Outline2D settings = new Outline2D();

        public uint CurveSides
        {
            get => CSGSettings.CurveSides;
            set
            {
                if(CSGSettings.CurveSides == value)
                {
                    return;
                }

                Undo.RecordObject(this, "Modified Shape Curve Sides");
                CSGSettings.CurveSides = value;
                UpdateBaseShape();
            }
        }

        //set from 0-1
        private float alpha = 1.0f;

        public float Alpha
        {
            get => alpha;
            set
            {
                if(alpha == value)
                {
                    return;
                }

                Undo.RecordObject(this, "Modified Shape Subdivision");
                alpha = value;
                UpdateBaseShape();
            }
        }

        protected override IShapeSettings ShapeSettings
        {
            get => settings;
        }

        public override void Init()
        {
            base.Init();
            forceDragSource = null;
            haveDragged     = false;
        }

        public void GenerateFromPolygon(
            Camera camera, CSGBrush brush, CSGPlane plane, Vector3 direction, Vector3[] meshVertices, int[] indices
          , uint[] smoothingGroups, bool drag, CSGOperationType forceDragSource, bool commitExtrusionAfterRelease
        )
        {
            generateSmoothing = false;
            Init();

            this.commitExtrusionAfterRelease = commitExtrusionAfterRelease;
            forceDragHandle                  = drag;
            this.forceDragSource             = forceDragSource;
            ignoreOrbit                      = true;
            planeOnGeometry                  = true;
            smearTextures                    = false;

            Vector3 brushScale    = brush.transform.lossyScale;
            bool    brushInverted = Math.Sign(brushScale.x) * Math.Sign(brushScale.y) * Math.Sign(brushScale.z) < 0;

            if(brushInverted)
            {
                plane = plane.Negated();
            }

            settings.Init(meshVertices, indices);

            haveForcedDirection = true;
            forcedDirection     = direction;

            for(int i = 0; i < indices.Length; i++)
            {
                settings.onGeometryVertices[i] = true;
                settings.onBrushVertices[i]    = brush;
            }

            Vector3[] realVertices = settings.GetVertices();
            geometryPlane = plane;
            buildPlane    = plane;
            CSGPlane newPlane = GeometryUtility.CalcPolygonPlane(realVertices);

            if(newPlane.normal.sqrMagnitude != 0)
            {
                buildPlane = newPlane;
            }

            CSGGrid.SetForcedGrid(camera, newPlane);

            if(brush.ChildData != null && brush.ChildData.Model)
            {
                geometryModel = brush.ChildData.Model;
            }

            if(StartEditMode(camera))
            {
                UpdateBaseShape();
            }

            brushPosition = plane.Project(brushPosition);

            extrusionPoints[1].Position =
                extrusionPoints[0].Position = brushPosition;

            for(int i = 0; i < indices.Length; i++)
            {
                settings.onGeometryVertices[i]                     = true;
                settings.onBrushVertices[i]                        = brush;
                settings.curveEdgeHandles[i].Texgen.SmoothingGroup = smoothingGroups[i];
            }
        }

        public override void Reset()
        {
            settings.Reset();
            base.Reset();
            onLastPoint       = false;
            hoverDefaultPlane = null;
        }

        public bool HotKeyReleased()
        {
            Camera camera = Camera.current;

            ResetVisuals();

            if(editMode == EditMode.CreatePlane)
            {
                if(settings.VertexLength < 3)
                {
                    Cancel();

                    return false;
                }
                else
                {
                    editMode = EditMode.CreateShape;
                }
            }

            if(editMode == EditMode.CreateShape)
            {
                if(StartEditMode(camera))
                {
                    UpdateBaseShape();
                }
            }

            return true;
        }

        internal void ToggleSubdivisionOnVertex(int index)
        {
            int length = settings.curveEdgeHandles.Length;

            if(length < 3)
            {
                return;
            }

            Undo.RecordObject(this, "Modified Shape Curvature");

            switch(settings.curveTangentHandles[index * 2 + 0].Constraint)
            {
                case HandleConstraints.Straight:
                    settings.SetPointConstraint(buildPlane, index, HandleConstraints.Mirrored); break;
                case HandleConstraints.Mirrored:
                    settings.SetPointConstraint(buildPlane, index, HandleConstraints.Broken); break;
                case HandleConstraints.Broken:
                    settings.SetPointConstraint(buildPlane, index, HandleConstraints.Straight); break;
            }

            UpdateBaseShape();
        }

        internal HandleConstraints? SelectedTangentState
        {
            get
            {
                HandleConstraints? state = null;

                for(int b = 0, a = settings.curveEdgeHandles.Length - 1; a >= 0; b = a, a--)
                {
                    if((settings.curveEdgeHandles[a].State & SelectState.Selected) == 0)
                    {
                        continue;
                    }

                    if(state == null)
                    {
                        state = settings.curveTangentHandles[a * 2 + 1].Constraint;
                    }

                    if(state.Value != settings.curveTangentHandles[a * 2 + 1].Constraint)
                    {
                        return null;
                    }

                    if(state.Value != settings.curveTangentHandles[b * 2 + 0].Constraint)
                    {
                        return null;
                    }
                }

                for(int b = settings.curveEdgeHandles.Length - 1, a = 0
                    ; a < settings.curveEdgeHandles.Length
                    ; b = a, a++)
                {
                    if((settings.curvePointHandles[a].State & SelectState.Selected) == 0)
                    {
                        continue;
                    }

                    if((settings.curveEdgeHandles[a].State & SelectState.Selected) == 0 !=
                       ((settings.curveEdgeHandles[b].State & SelectState.Selected) == 0))
                    {
                        continue;
                    }

                    if(state == null)
                    {
                        state = settings.curveTangentHandles[a * 2 + 0].Constraint;
                    }

                    if(state.Value != settings.curveTangentHandles[a * 2 + 0].Constraint)
                    {
                        return null;
                    }

                    if(state.Value != settings.curveTangentHandles[a * 2 + 1].Constraint)
                    {
                        return null;
                    }
                }

                return state;
            }
            set
            {
                if(!value.HasValue)
                {
                    return;
                }

                Undo.RecordObject(this, "Modified Shape Curvature");

                for(int b = settings.curveEdgeHandles.Length - 1, a = 0
                    ; a < settings.curveEdgeHandles.Length
                    ; b = a, a++)
                {
                    if((settings.curvePointHandles[a].State & SelectState.Selected) == 0)
                    {
                        continue;
                    }

                    if(((settings.curveEdgeHandles[a].State & SelectState.Selected) == 0 &&
                           (settings.curveEdgeHandles[b].State & SelectState.Selected) == 0) ||
                       value.Value == HandleConstraints.Mirrored)
                    {
                        settings.SetPointConstraint(buildPlane, a, value.Value);
                    }
                }

                for(int b = 0, a = settings.curveEdgeHandles.Length - 1; a >= 0; b = a, a--)
                {
                    if((settings.curveEdgeHandles[a].State & SelectState.Selected) == 0)
                    {
                        continue;
                    }

                    settings.SetPointConstaintSide(buildPlane, a, 1, value.Value);
                    settings.SetPointConstaintSide(buildPlane, b, 0, value.Value);
                }

                UpdateBaseShape();
            }
        }

        internal void ToggleSubdivisionOnSelectedEdges()
        {
            int length = settings.curveEdgeHandles.Length;

            if(length < 3)
            {
                return;
            }

            Undo.RecordObject(this, "Modified Shape Curvature");
            HandleConstraints state = HandleConstraints.Mirrored;

            for(int b = 0, a = settings.curveEdgeHandles.Length - 1; a >= 0; b = a, a--)
            {
                if((settings.curveEdgeHandles[a].State & SelectState.Selected) == 0)
                {
                    continue;
                }

                if(settings.curveTangentHandles[a * 2 + 1].Constraint != HandleConstraints.Straight ||
                   settings.curveTangentHandles[b * 2 + 0].Constraint != HandleConstraints.Straight)
                {
                    state = HandleConstraints.Straight;

                    break;
                }
            }

            if(state == HandleConstraints.Straight)
            {
                state = HandleConstraints.Broken;

                for(int b = 0, a = settings.curveEdgeHandles.Length - 1; a >= 0; b = a, a--)
                {
                    if((settings.curveEdgeHandles[a].State & SelectState.Selected) == 0)
                    {
                        continue;
                    }

                    if(settings.curveTangentHandles[a * 2 + 1].Constraint != HandleConstraints.Mirrored ||
                       settings.curveTangentHandles[b * 2 + 0].Constraint != HandleConstraints.Mirrored)
                    {
                        state = HandleConstraints.Straight;

                        break;
                    }
                }
            }

            for(int b = 0, a = settings.curveEdgeHandles.Length - 1; a >= 0; b = a, a--)
            {
                if((settings.curveEdgeHandles[a].State & SelectState.Selected) == 0)
                {
                    continue;
                }

                settings.SetPointConstaintSide(buildPlane, a, 1, state);
                settings.SetPointConstaintSide(buildPlane, b, 0, state);
            }

            UpdateBaseShape();
        }

        public void SplitSelectedEdge()
        {
            Undo.RecordObject(this, "Split edges");
            int[][] curvedEdges = null;
            //var realVertices	= settings.GetVertices();
            Vector3[] curvedVertices = settings.GetCurvedVertices(buildPlane, CurveSides, out curvedEdges);

            if(curvedEdges == null)
            {
                return;
            }

            for(int i = settings.curveEdgeHandles.Length - 1; i >= 0; i--)
            {
                if((settings.curveEdgeHandles[i].State & SelectState.Selected) == 0)
                {
                    continue;
                }

                if(i >= curvedEdges.Length ||
                   curvedEdges[i].Length < 2)
                {
                    continue;
                }

                int[]   indices = curvedEdges[i];
                Vector3 origin;

                if(indices.Length == 2)
                {
                    origin = (curvedVertices[indices[0]] + curvedVertices[indices[1]]) * 0.5f;
                }
                else
                {
                    if(indices.Length > 2 &&
                       (indices.Length & 1) == 0)
                    {
                        origin = (curvedVertices[indices[indices.Length / 2 - 1]] +
                            curvedVertices[indices[indices.Length / 2]]) * 0.5f;
                    }
                    else
                    {
                        origin = curvedVertices[indices[indices.Length / 2]];
                    }
                }

                settings.InsertVertexAfter(i, origin);
            }
        }

        protected override void MoveShape(Vector3 offset)
        {
            settings.MoveShape(offset);
        }

        void IGenerator.OnDefaultMaterialModified()
        {
            if(generatedBrushes == null)
            {
                return;
            }

            Material defaultMaterial = CSGSettings.DefaultMaterial;

            for(int i = 0; i < generatedBrushes.Length; i++)
            {
                CSGBrush brush = generatedBrushes[i];

                if(!brush)
                {
                    continue;
                }

                Shape shape = brush.Shape;

                for(int m = 0; m < shape.TexGens.Length; m++)
                {
                    shape.TexGens[m].RenderMaterial = defaultMaterial;
                }

                if(brush.ControlMesh != null)
                {
                    brush.ControlMesh.SetDirty();
                }
            }
        }

        internal override bool UpdateBaseShape(bool registerUndo = true)
        {
            int[][]   curvedEdges     = null;
            Vector3[] outlineVertices = settings.GetCurvedVertices(buildPlane, CurveSides, out curvedEdges);

            if(curvedEdges == null)
            {
                return false;
            }

            ShapePolygonUtility.RemoveDuplicatePoints(ref outlineVertices);

            if(generateSmoothing)
            {
                HashSet<uint> usedSmoothingGroupIndices = SurfaceUtility.GetUsedSmoothingGroupIndices();

                for(int i = 0; i < settings.curveEdgeHandles.Length; i++)
                {
                    settings.curveEdgeHandles[i].Texgen.SmoothingGroup = 0;
                }

                uint smoothingGroup = SurfaceUtility.FindUnusedSmoothingGroupIndex(usedSmoothingGroupIndices);
                usedSmoothingGroupIndices.Add(smoothingGroup);

                for(int i = 0; i < settings.curveEdgeHandles.Length; i++)
                {
                    if(i >= curvedEdges.Length)
                    {
                        continue;
                    }

                    if(curvedEdges[i].Length <= 2)
                    {
                        settings.curveEdgeHandles[i].Texgen.SmoothingGroup = 0;
                    }

                    if(settings.curveEdgeHandles[i].Texgen.SmoothingGroup != 0)
                    {
                        continue;
                    }

                    settings.curveEdgeHandles[i].Texgen.SmoothingGroup = smoothingGroup;

                    if((i == 0 &&
                           settings.curveTangentHandles[i * 2 + 0].Constraint == HandleConstraints.Mirrored) ||
                       settings.curveTangentHandles[i * 2 + 1].Constraint == HandleConstraints.Mirrored)
                    {
                        int last = settings.curveEdgeHandles.Length - 1;
                        settings.curveEdgeHandles[last].Texgen.SmoothingGroup = smoothingGroup;
                    }

                    if((i + 1) * 2 + 1 < settings.curveTangentHandles.Length &&
                       (settings.curveTangentHandles[(i + 1) * 2 + 0].Constraint == HandleConstraints.Mirrored ||
                           settings.curveTangentHandles[(i + 1) * 2 + 1].Constraint == HandleConstraints.Mirrored))
                    {
                        settings.curveEdgeHandles[i + 1].Texgen.SmoothingGroup = smoothingGroup;
                    }
                }
            }

            int vertexCount = 0;

            for(int i = 0; i < settings.curveEdgeHandles.Length; i++)
            {
                if(i >= curvedEdges.Length)
                {
                    continue;
                }

                vertexCount += curvedEdges[i].Length;
            }

            TexGen[]    curvedEdgeTexgens = new TexGen[vertexCount];
            ShapeEdge[] curvedShapeEdges  = new ShapeEdge[vertexCount];

            for(int i = 0, n = 0; i < settings.curveEdgeHandles.Length; i++)
            {
                TexGen texGen = settings.curveEdgeHandles[i].Texgen;

                if(i >= curvedEdges.Length)
                {
                    continue;
                }

                if(generateSmoothing &&
                   curvedEdges[i].Length <= 2)
                {
                    texGen.SmoothingGroup = 0;
                }

                for(int j = 0; j < curvedEdges[i].Length - 1; j++, n++)
                {
                    curvedEdgeTexgens[n] = texGen;
                }
            }

            Vector3[] projectedVertices;

            List<ShapePolygon> newPolygons = ShapePolygonUtility.CreateCleanPolygonsFromVertices(
                outlineVertices,
                brushPosition,
                buildPlane,
                out projectedVertices
            );

            if(newPolygons == null)
            {
                if(registerUndo)
                {
                    EditModeManager.ShowMessage("Could not create brush from given 2D shape");
                }

                return true;
            }

            if(newPolygons != null)
            {
                UpdatePolygons(outlineVertices, newPolygons.ToArray());
            }

            if(editMode != EditMode.EditShape &&
               editMode != EditMode.ExtrudeShape)
            {
                return false;
            }

            if(!HaveExtrusion)
            {
                return true;
            }

            ShapePolygonUtility.FixMaterials(
                projectedVertices,
                newPolygons,
                Quaternion.identity,
                brushPosition,
                buildPlane,
                curvedEdgeTexgens,
                curvedShapeEdges
            );

            if(registerUndo)
            {
                EditModeManager.ResetMessage();
            }

            GenerateBrushesFromPolygons(curvedShapeEdges);
            UpdateExtrudedShape(registerUndo);

            return true;
        }

        private bool BuildPlaneIsReversed
        {
            get
            {                                       /*
                if (planeOnGeometry)
                {
                    if (settings.onPlaneVertices.Length > 0)
                    {
                        var plane0 = settings.onPlaneVertices[0];
                        var plane0_neg = plane0.Negated();
                        for (int i = 1; i < settings.onPlaneVertices.Length; i++)
                        {
                            if (settings.onPlaneVertices[i] != plane0 &&
                                settings.onPlaneVertices[i] != plane0_neg)
                            {
                                planeOnGeometry = false;
                                break;
                            }
                        }
                    } else
                        planeOnGeometry = false;
                }
                */
                CSGPlane realPlane = geometryPlane; /*
                if (!planeOnGeometry)
                    realPlane = RealtimeCSG.Grid.CurrentGridPlane;*/

                if(Vector3.Dot(realPlane.normal, buildPlane.normal) < 0)
                {
                    return true;
                }

                return false;
            }
        }

        internal override void BeginExtrusion()
        {
            settings.CopyBackupVertices();
        }

        internal override void EndExtrusion()
        {
        }

        internal override bool StartExtrudeMode(Camera camera, bool showErrorMessage = true)
        {
            if(settings.VertexLength < 3)
            {
                return false;
            }

            settings.TryFindPlaneMaterial(buildPlane);

            int[][]   curvedEdges     = null;
            Vector3[] outlineVertices = settings.GetCurvedVertices(buildPlane, CurveSides, out curvedEdges);
            ShapePolygonUtility.RemoveDuplicatePoints(ref outlineVertices);

            settings.UpdateEdgeMaterials(buildPlane.normal);

            HashSet<uint> usedSmoothingGroupIndices = SurfaceUtility.GetUsedSmoothingGroupIndices();
            List<TexGen>  curvedEdgeTexgensList     = new List<TexGen>();

            if(curvedEdges != null)
            {
                if(generateSmoothing)
                {
                    settings.UpdateSmoothingGroups(usedSmoothingGroupIndices);
                }

                for(int i = 0; i < settings.curveEdgeHandles.Length; i++)
                {
                    if(i >= curvedEdges.Length)
                    {
                        continue;
                    }

                    TexGen texGen = settings.curveEdgeHandles[i].Texgen;

                    if(generateSmoothing && curvedEdges[i].Length <= 2)
                    {
                        texGen.SmoothingGroup = 0;
                    }

                    for(int j = 0; j < curvedEdges[i].Length - 1; j++)
                    {
                        curvedEdgeTexgensList.Add(texGen);
                    }
                }
            }

            TexGen[]    curvedEdgeTexgens = curvedEdgeTexgensList.ToArray();
            ShapeEdge[] curvedShapeEdges  = new ShapeEdge[curvedEdgeTexgens.Length];

            // reverse buildPlane if it's different
            if(BuildPlaneIsReversed)
            {
                buildPlane = buildPlane.Negated();
                settings.Negated();
            }

            Vector3[] projectedVertices;

            List<ShapePolygon> newPolygons = ShapePolygonUtility.CreateCleanPolygonsFromVertices(
                outlineVertices,
                brushPosition,
                buildPlane,
                out projectedVertices
            );

            if(newPolygons == null)
            {
                ClearPolygons();

                if(showErrorMessage)
                {
                    EditModeManager.ShowMessage("Could not create brush from given 2D shape");
                }

                HideGenerateBrushes();

                return false;
            }

            ShapePolygonUtility.FixMaterials(
                projectedVertices,
                newPolygons,
                Quaternion.identity,
                brushPosition,
                buildPlane,
                curvedEdgeTexgens,
                curvedShapeEdges
            );

            EditModeManager.ResetMessage();

            UpdatePolygons(outlineVertices, newPolygons.ToArray());
            GenerateBrushesFromPolygons(curvedShapeEdges);

            return true;
        }

        internal override bool CreateControlMeshForBrushIndex(
            CSGModel parentModel, CSGBrush brush, ShapePolygon polygon, Matrix4x4 localToWorld, float height
          , out ControlMesh newControlMesh, out Shape newShape
        )
        {
            Vector3 direction = haveForcedDirection ? forcedDirection : buildPlane.normal;

            if(!ShapePolygonUtility.GenerateControlMeshFromVertices(
                   polygon,
                   localToWorld,
                   GeometryUtility.RotatePointIntoPlaneSpace(buildPlane, direction),
                   height,
                   settings.planeTexgen,
                   null,
                   true,
                   out newControlMesh,
                   out newShape
               ))
            {
                return false;
            }

            Vector3 parentScale    = parentTransform.lossyScale;
            bool    parentInverted = Math.Sign(parentScale.x) * Math.Sign(parentScale.y) * Math.Sign(parentScale.z) < 0;

            if(parentInverted)
            {
                ControlMeshUtility.InvertControlMesh(newControlMesh);
                ControlMeshUtility.InvertShape(newShape);
            }

            brush.Shape       = newShape;
            brush.ControlMesh = newControlMesh;
            InternalCSGModelManager.ValidateBrush(brush, true);
            ControlMeshUtility.RebuildShape(brush);

            return true;
        }

        private void PaintEdgeSides(Camera camera, Vector3 start, Vector3 end)
        {
            Color wireframeColor = ColorSettings.BoundsOutlines;

            Vector3 delta  = end - start;
            Vector3 center = (end + start) * 0.5f;

            Vector3 xdistance = Vector3.Project(delta, gridTangent);
            Vector3 zdistance = Vector3.Project(delta, gridBinormal);

            Vector3 point0 = start + xdistance + zdistance;
            Vector3 point1 = start + xdistance;
            Vector3 point2 = start;
            Vector3 point3 = start + zdistance;

            Vector3[] points = new Vector3[] { point0, point1, point1, point2, point2, point3, point3, point0 };

            PaintUtility.DrawDottedLines(points, wireframeColor, 4.0f);

            Vector3 endPoint = camera.transform.position;

            points = new Vector3[] { point0, point1, point2, point3 };
            int   closest_index    = -1;
            float closest_distance = float.NegativeInfinity;

            for(int i = 0; i < points.Length; i++)
            {
                float distance = (points[i] - endPoint).sqrMagnitude;

                if(distance > closest_distance)
                {
                    closest_index    = i;
                    closest_distance = distance;
                }
            }

            int indexA1 = (closest_index + 1) % 4;
            int indexA2 = (indexA1 + 1) % 4;
            int indexB1 = (closest_index + 3) % 4;
            int indexB2 = (indexB1 + 3) % 4;

            Vector3 edgeCenterA = (points[indexA1] + points[indexA2]) * 0.5f;
            Vector3 edgeCenterB = (points[indexB1] + points[indexB2]) * 0.5f;
            float   edgeLengthA = GeometryUtility.CleanLength((points[indexA1] - points[indexA2]).magnitude);
            float   edgeLengthB = GeometryUtility.CleanLength((points[indexB1] - points[indexB2]).magnitude);

            if(Mathf.Abs(edgeLengthA) > 0 && Mathf.Abs(edgeLengthB) > 0)
            {
                PaintSideLength(camera, edgeCenterA, center, edgeLengthA, (indexA1 & 1) == 1 ? "Z:" : "X:");
                PaintSideLength(camera, edgeCenterB, center, edgeLengthB, (indexB1 & 1) == 1 ? "X:" : "Z:");
            }
        }

        private void Paint(SceneView sceneView, int id)
        {
            Camera    camera     = sceneView.camera;
            Color     temp       = Handles.color;
            Matrix4x4 origMatrix = Handles.matrix;

            Handles.matrix = MathConstants.identityMatrix;
            Quaternion rotation = camera.transform.rotation;

            Vector3[] realVertices = settings.GetVertices();

            if(realVertices != null && realVertices.Length > 0)
            {
                Color wireframeColor    = ColorSettings.WireframeOutline;
                Color topWireframeColor = ColorSettings.BoundsEdgeHover;

                ArrayUtility.Add(ref realVertices, worldPosition);
                Vector3[] curvedVertices = settings.GetCurvedVertices(buildPlane, CurveSides);
                ArrayUtility.Add(ref curvedVertices, worldPosition);
                ShapePolygonUtility.RemoveDuplicatePoints(ref curvedVertices);

                if(curvedVertices.Length >= 3)
                {
                    List<ShapePolygon> newPolygons = ShapePolygonUtility.CreateCleanPolygonsFromVertices(
                        curvedVertices,
                        MathConstants.zeroVector3,
                        buildPlane
                    );

                    if(newPolygons != null)
                    {
                        Matrix4x4 matrix = Matrix4x4.TRS(
                            buildPlane.pointOnPlane
                          , Quaternion.FromToRotation(MathConstants.upVector3, buildPlane.normal)
                          , MathConstants.oneVector3
                        );

                        Color color = ColorSettings.ShapeDrawingFill;

                        for(int i = 0; i < newPolygons.Count; i++)
                        {
                            PaintUtility.DrawPolygon(matrix, newPolygons[i].vertices, color);
                        }
                    }
                    else
                    {
                        wireframeColor = Color.red;
                    }
                }

                for(int i = 1; i < curvedVertices.Length; i++)
                {
                    PaintUtility.DrawLine(
                        curvedVertices[i - 1], curvedVertices[i], GUIConstants.OLD_LINE_SCALE, wireframeColor
                    );

                    PaintUtility.DrawDottedLine(curvedVertices[i - 1], curvedVertices[i], wireframeColor, 4.0f);
                }

                if(curvedVertices.Length > 3)
                {
                    PaintUtility.DrawLine(
                        curvedVertices[curvedVertices.Length - 1], curvedVertices[0], GUIConstants.OLD_LINE_SCALE
                      , wireframeColor
                    );

                    PaintUtility.DrawDottedLine(
                        curvedVertices[curvedVertices.Length - 1], curvedVertices[0], wireframeColor, 4.0f
                    );
                }

                bool isReversed = BuildPlaneIsReversed;

                Vector3 forward = camera.transform.forward;

                if(Event.current.button != 1)
                    //GUIUtility.hotControl == id || GUIUtility.hotControl == 0)
                {
                    PaintUtility.DrawLine(
                        realVertices[realVertices.Length - 2], worldPosition, GUIConstants.OLD_LINE_SCALE
                      , topWireframeColor
                    );

                    PaintUtility.DrawDottedLine(
                        realVertices[realVertices.Length - 2], worldPosition, topWireframeColor, 4.0f
                    );

                    Vector3 origin   = (realVertices[realVertices.Length - 2] + worldPosition) * 0.5f;
                    Vector3 delta    = realVertices[realVertices.Length - 2] - worldPosition;
                    float   distance = delta.magnitude;

                    PaintEdgeSides(camera, realVertices[realVertices.Length - 2], worldPosition);

                    Vector3 sideways = Vector3.Cross(delta.normalized, forward);

                    Vector2 textCenter2D = HandleUtility.WorldToGUIPoint(origin);

                    Vector2 sideways2D = (HandleUtility.WorldToGUIPoint(origin + sideways * 10) - textCenter2D)
                       .normalized;

                    Vector2 fromCenter = MathConstants.zeroVector2;

                    if(realVertices.Length > 0)
                    {
                        Vector3 min = new Vector3(
                            float.PositiveInfinity, float.PositiveInfinity, float.PositiveInfinity
                        );

                        Vector3 max = new Vector3(
                            float.NegativeInfinity, float.NegativeInfinity, float.NegativeInfinity
                        );

                        for(int i = 0; i < realVertices.Length; i++)
                        {
                            min.x = Mathf.Min(min.x, realVertices[i].x);
                            min.y = Mathf.Min(min.y, realVertices[i].y);
                            min.z = Mathf.Min(min.z, realVertices[i].z);
                            max.x = Mathf.Max(max.x, realVertices[i].x);
                            max.y = Mathf.Max(max.y, realVertices[i].y);
                            max.z = Mathf.Max(max.z, realVertices[i].z);
                        }

                        Vector3 center = (min + max) * 0.5f;
                        fromCenter = (textCenter2D - HandleUtility.WorldToGUIPoint(center)).normalized;
                    }

                    if(sideways2D == MathConstants.zeroVector2)
                    {
                        sideways2D = fromCenter;

                        if(sideways2D == MathConstants.zeroVector2)
                        {
                            sideways2D = MathConstants.upVector3;
                        }
                    }
                    else
                    {
                        if(isReversed)
                        {
                            sideways2D = -sideways2D;
                        }
                    }

                    textCenter2D += sideways2D * (hover_text_distance * 2);

                    Ray textCenterRay = HandleUtility.GUIPointToWorldRay(textCenter2D);

                    Vector3 textCenter = textCenterRay.origin
                      + textCenterRay.direction * ((camera.farClipPlane + camera.nearClipPlane) * 0.5f);

                    Handles.color = Color.black;
                    Handles.DrawLine(origin, textCenter);

                    PaintUtility.DrawScreenText(
                        textCenter2D,
                        Units.ToRoundedDistanceString(Mathf.Abs(distance))
                    );

                    PaintUtility.DrawDottedLine(worldPosition, realVertices[0], topWireframeColor, 4.0f);
                }

                Handles.color = wireframeColor;

                for(int i = 0; i < realVertices.Length; i++)
                {
                    float handleSize       = CSG_HandleUtility.GetHandleSize(realVertices[i]);
                    float scaledHandleSize = handleSize * GUIConstants.HANDLE_SCALE;
                    PaintUtility.SquareDotCap(id, realVertices[i], rotation, scaledHandleSize);
                }
            }

            if(Event.current.button != 1)
            {
                Handles.color = Handles.selectedColor;

                //if ((camera != null) && camera.pixelRect.Contains(Event.current.mousePosition))
                {
                    float handleSize       = CSG_HandleUtility.GetHandleSize(worldPosition);
                    float scaledHandleSize = handleSize * GUIConstants.HANDLE_SCALE;
                    PaintUtility.SquareDotCap(id, worldPosition, rotation, scaledHandleSize);
                }
            }

            Handles.matrix = origMatrix;
            Handles.color  = temp;
        }

        protected override void CreateControlIDs()
        {
            base.CreateControlIDs();

            if(settings.VertexLength > 0 &&
               (editMode == EditMode.EditShape ||
                   editMode == EditMode.ExtrudeShape))
            {
                //settings.vertexIDs = new int[settings.VertexLength];
                for(int i = 0; i < settings.curvePointHandles.Length; i++)
                {
                    settings.curvePointHandles[i].ID = GUIUtility.GetControlID(
                        ShapeBuilderPointHash, FocusType.Passive
                    );
                    //settings.vertexIDs[i] = GUIUtility.GetControlID(ShapeBuilderPointHash, FocusType.Passive);
                }

                //settings.tangentIDs = new int[settings.VertexLength * 2];
                for(int i = 0; i < settings.curveTangentHandles.Length; i++)
                {
                    settings.curveTangentHandles[i].ID = GUIUtility.GetControlID(
                        ShapeBuilderTangentHash, FocusType.Passive
                    );
                }

                //settings.edgeIDs = new int[settings.VertexLength];
                for(int i = 0; i < settings.curveEdgeHandles.Length; i++)
                {
                    settings.curveEdgeHandles[i].ID = GUIUtility.GetControlID(ShapeBuilderEdgeHash, FocusType.Passive);
                }
            }
        }

        public override void PerformDeselectAll()
        {
            if(editMode != EditMode.EditShape ||
               !settings.DeselectAll())
            {
                Cancel();
            }
        }

        public override void PerformDelete()
        {
            if(editMode != EditMode.EditShape &&
               editMode != EditMode.ExtrudeShape)
            {
                Cancel();

                return;
            }

            Undo.RecordObject(this, "Deleted vertices");
            settings.DeleteSelectedVertices();

            if(settings.VertexLength < 3)
            {
                Cancel();
            }
        }

        private bool havePlane = false;

        protected override void HandleCreateShapeEvents(SceneView sceneView, Rect sceneRect)
        {
            Camera camera           = sceneView.camera;
            Event  current          = Event.current;
            bool   pointOnEdge      = false;
            bool   vertexOnGeometry = false;

            CSGBrush vertexOnBrush   = null;
            CSGPlane vertexOnPlane   = buildPlane;
            CSGPlane hoverBuildPlane = buildPlane;

            if(camera != null && (GUIUtility.hotControl == shapeId || GUIUtility.hotControl == 0) &&
               camera.pixelRect.Contains(current.mousePosition))
            {
                if(!hoverDefaultPlane.HasValue ||
                   settings.VertexLength == 0)
                {
                    bool forceGrid = CSGGrid.ForceGrid;
                    CSGGrid.ForceGrid = false;
                    hoverDefaultPlane = CSGGrid.CurrentGridPlane;
                    CSGGrid.ForceGrid = forceGrid;
                    geometryModel     = null;
                }

                if(settings.VertexLength == 0)
                {
                    havePlane = false;
                }

                if(editMode == EditMode.CreatePlane && !havePlane)
                {
                    if(settings.VertexLength >= 3)
                    {
                        hoverBuildPlane = GeometryUtility.CalcPolygonPlane(settings.GetVertices());

                        if(hoverBuildPlane.normal.sqrMagnitude != 0)
                        {
                            buildPlane = hoverBuildPlane;
                            editMode   = EditMode.CreateShape;
                            havePlane  = true;
                        }
                    }

                    LegacyBrushIntersection intersection;

                    if(!IgnoreDepthForRayCasts(camera) && !havePlane &&
                       EditorWindow.mouseOverWindow == sceneView &&
                       SceneQueryUtility.FindWorldIntersection(camera, current.mousePosition, out intersection))
                    {
                        worldPosition    = intersection.worldIntersection;
                        hoverBuildPlane  = intersection.worldPlane;
                        buildPlane       = hoverBuildPlane;
                        havePlane        = true;
                        vertexOnGeometry = true;

                        vertexOnBrush = intersection.brush;
                        vertexOnPlane = hoverBuildPlane;
                    }
                    else
                    {
                        hoverBuildPlane = hoverDefaultPlane.Value;

                        Ray mouseRay = HandleUtility.GUIPointToWorldRay(current.mousePosition);
                        worldPosition    = hoverBuildPlane.RayIntersection(mouseRay);
                        vertexOnGeometry = false;
                        vertexOnBrush    = null;

                        if(hoverBuildPlane.normal.sqrMagnitude != 0)
                        {
                            vertexOnPlane = hoverBuildPlane;
                            buildPlane    = hoverBuildPlane;
                        }
                    }

                    ResetVisuals();

                    if(snapFunction != null)
                    {
                        CSGBrush snappedOnBrush;

                        worldPosition = snapFunction(
                            camera, worldPosition, hoverBuildPlane, ref visualSnappedEdges, out snappedOnBrush
                          , generatedBrushes
                        );

                        if(snappedOnBrush != null)
                        {
                            pointOnEdge = visualSnappedEdges != null &&
                                visualSnappedEdges.Count > 0;

                            vertexOnBrush    = snappedOnBrush;
                            vertexOnGeometry = true;
                        }
                    }
                }
                else
                {
                    Ray mouseRay = HandleUtility.GUIPointToWorldRay(current.mousePosition);
                    worldPosition = hoverBuildPlane.RayIntersection(mouseRay);

                    ResetVisuals();

                    if(snapFunction != null)
                    {
                        CSGBrush snappedOnBrush;

                        worldPosition = snapFunction(
                            camera, worldPosition, hoverBuildPlane, ref visualSnappedEdges, out snappedOnBrush
                          , generatedBrushes
                        );

                        if(snappedOnBrush != null)
                        {
                            pointOnEdge = visualSnappedEdges != null &&
                                visualSnappedEdges.Count > 0;

                            vertexOnBrush = snappedOnBrush;
                        }
                    }
                }

                if(geometryModel == null && vertexOnBrush != null)
                {
                    if(vertexOnBrush.ChildData != null && vertexOnBrush.ChildData.Model)
                    {
                        geometryModel = vertexOnBrush.ChildData.Model;
                    }
                }

                if(settings.VertexLength > 2)
                {
                    Vector3 first2D      = camera.WorldToScreenPoint(settings.GetPosition(0));
                    Vector3 current2D    = camera.WorldToScreenPoint(worldPosition);
                    float   distance     = (current2D - first2D).magnitude;
                    float   snapDistance = 2.0f * handle_on_distance;

                    if(distance < snapDistance)
                    {
                        worldPosition = settings.GetPosition(0);
                        onLastPoint   = true;
                    }
                    else
                    {
                        onLastPoint = false;
                    }
                }
                else
                {
                    onLastPoint = false;
                }

                if(worldPosition != prevWorldPosition)
                {
                    prevWorldPosition = worldPosition;

                    if(current.type != EventType.Repaint)
                    {
                        CSG_EditorGUIUtility.RepaintAll();
                    }
                }

                visualSnappedGrid  = CSGGrid.FindAllGridEdgesThatTouchPoint(camera, worldPosition);
                visualSnappedBrush = vertexOnBrush;
            }

            CSGGrid.SetForcedGrid(camera, hoverBuildPlane);

            if(!SceneDragToolManager.IsDraggingObjectInScene &&
               current.type == EventType.Repaint)
            {
                if(settings.realEdge != null)
                {
                    Handles.color = ColorSettings.WireframeOutline;
                    PaintUtility.DrawDottedLines(settings.realEdge, Handles.secondaryColor, 4.0f);
                }

                PaintSnapVisualisation();
                Paint(sceneView, shapeId);
            }

            EventType type = current.GetTypeForControl(shapeId);

            switch(type)
            {
                case EventType.Layout:
                {
                    return;
                }

                case EventType.ValidateCommand:
                case EventType.KeyDown:
                {
                    if(GUIUtility.hotControl != shapeId)
                    {
                        return;
                    }

                    if(Keys.performActionKey.IsKeyPressed() ||
                       Keys.deleteSelectionKey.IsKeyPressed() ||
                       Keys.cancelActionKey.IsKeyPressed())
                    {
                        Event.current.Use();
                    }

                    return;
                }
                case EventType.KeyUp:
                {
                    if(GUIUtility.hotControl != shapeId)
                    {
                        return;
                    }

                    if(Keys.freeBuilderMode.IsKeyPressed() ||
                       Keys.performActionKey.IsKeyPressed())
                    {
                        HotKeyReleased();
                        Event.current.Use();

                        return;
                    }

                    if(Keys.deleteSelectionKey.IsKeyPressed() ||
                       Keys.cancelActionKey.IsKeyPressed())
                    {
                        PerformDeselectAll();
                        Event.current.Use();

                        return;
                    }

                    return;
                }

                case EventType.MouseDown:
                {
                    if(!sceneRect.Contains(Event.current.mousePosition))
                    {
                        break;
                    }

                    if((GUIUtility.hotControl != 0 &&
                           GUIUtility.hotControl != shapeId &&
                           GUIUtility.hotControl != shapeEditId) ||
                       Event.current.modifiers != EventModifiers.None ||
                       Event.current.button != 0 ||
                       (Tools.viewTool != ViewTool.None && Tools.viewTool != ViewTool.Pan))
                    {
                        return;
                    }

                    Event.current.Use();

                    if(!settings.HaveVertices)
                    {
                        if(GUIUtility.hotControl == 0 && shapeId != -1)
                        {
                            CalculateWorldSpaceTangents(camera);
                            GUIUtility.hotControl             = shapeId;
                            GUIUtility.keyboardControl        = shapeId;
                            EditorGUIUtility.editingTextField = false;
                        }
                    }

                    if(GUIUtility.hotControl == shapeId)
                    {
                        if(!float.IsNaN(worldPosition.x) && !float.IsInfinity(worldPosition.x) &&
                           !float.IsNaN(worldPosition.y) && !float.IsInfinity(worldPosition.y) &&
                           !float.IsNaN(worldPosition.z) && !float.IsInfinity(worldPosition.z))
                        {
                            if(hoverBuildPlane.normal.sqrMagnitude != 0)
                            {
                                buildPlane = hoverBuildPlane;
                            }

                            CalculateWorldSpaceTangents(camera);

                            if(!settings.HaveVertices)
                            {
                                planeOnGeometry = !pointOnEdge && vertexOnGeometry;
                                geometryPlane   = buildPlane;
                            }
                            else
                            {
                                if(!pointOnEdge && vertexOnGeometry)
                                {
                                    planeOnGeometry = true;
                                }

                                if(vertexOnGeometry)
                                {
                                    CSGPlane plane    = hoverDefaultPlane.Value;
                                    float    distance = plane.Distance(worldPosition);
                                    plane.d           += distance;
                                    hoverDefaultPlane =  plane;

                                    for(int i = 0; i < settings.VertexLength; i++)
                                    {
                                        if(!settings.onGeometryVertices[i])
                                        {
                                            Vector3 guipoint =
                                                camera.WorldToScreenPoint(
                                                    settings.GetPosition(i)
                                                ); // HandleUtility.WorldToGUIPoint();

                                            Ray cameraRay =
                                                camera.ScreenPointToRay(
                                                    guipoint
                                                ); // HandleUtility.GUIPointToWorldRay(guipoint);

                                            settings.SetPosition(i, plane.RayIntersection(cameraRay));
                                            settings.onGeometryVertices[i] = true;
                                        }
                                    }
                                }
                            }

                            settings.AddVertex(worldPosition, vertexOnBrush, vertexOnPlane, vertexOnGeometry);
                            CSG_EditorGUIUtility.RepaintAll();
                        }

                        CSG_EditorGUIUtility.RepaintAll();
                    }

                    return;
                }
                case EventType.MouseMove:
                {
                    clickCount = 0;

                    break;
                }
                case EventType.MouseDrag:
                {
                    clickCount = 0;

                    if(Tools.viewTool != ViewTool.None && Tools.viewTool != ViewTool.Pan)
                    {
                        break;
                    }

                    if(GUIUtility.hotControl == shapeId && Event.current.button == 0)
                    {
                        Event.current.Use();
                    }

                    return;
                }
                case EventType.MouseUp:
                {
                    if(GUIUtility.hotControl != shapeId ||
                       Event.current.button != 0 ||
                       (Tools.viewTool != ViewTool.None && Tools.viewTool != ViewTool.Pan))
                    {
                        return;
                    }

                    Event.current.Use();

                    if(Event.current.button == 0)
                    {
                        if(onLastPoint)
                        {
                            if(settings.VertexLength < 3)
                            {
                                Cancel();
                            }
                            else
                            {
                                //settings.DeleteVertex(settings.VertexLength - 1);
                                editMode = EditMode.CreateShape;
                                HotKeyReleased();
                            }
                        }

                        return;
                    }

                    return;
                }
            }
        }

        private static void DrawDotControlState(
            SelectState renderState, HandlePointCurve2D[] curvePointHandles, Vector3[] vertices, Quaternion rotation
          , HandleConstraints[] state, int colorState
        )
        {
            for(int i = 0; i < vertices.Length; i++)
            {
                SelectState vertexState = curvePointHandles[i].State;
                int         id          = curvePointHandles[i].ID;

                if(vertexState != renderState)
                {
                    continue;
                }

                float handleSize = CSG_HandleUtility.GetHandleSize(vertices[i]);

                if(state != null)
                {
                    if(state[i] == HandleConstraints.Mirrored)
                    {
                        Handles.color = ColorSettings.AlignedCurveStateColor[colorState];

                        PaintUtility.CircleDotCap(
                            id, vertices[i], rotation, handleSize * GUIConstants.HANDLE_SCALE * 1.25f
                        );
                    }
                    else
                    {
                        Handles.color = ColorSettings.BrokenCurveStateColor[colorState];

                        PaintUtility.DiamondDotCap(
                            id, vertices[i], rotation, handleSize * GUIConstants.HANDLE_SCALE * 1.25f
                        );
                    }
                }
                else
                {
                    Handles.color = ColorSettings.PointInnerStateColor[colorState];
                    PaintUtility.SquareDotCap(id, vertices[i], rotation, handleSize * GUIConstants.HANDLE_SCALE);
                }
            }
        }

        private static void DrawDotControlState(
            SelectState renderState, int[] dotIds, SelectState[] selectionStates, Vector3[] vertices
          , Quaternion rotation, HandleConstraints[] state, int colorState
        )
        {
            for(int i = 0; i < vertices.Length; i++)
            {
                SelectState vertexState = selectionStates[i];
                int         id          = dotIds[i];

                if(vertexState != renderState)
                {
                    continue;
                }

                float handleSize = CSG_HandleUtility.GetHandleSize(vertices[i]);

                if(state != null)
                {
                    if(state[i] == HandleConstraints.Mirrored)
                    {
                        Handles.color = ColorSettings.AlignedCurveStateColor[colorState];

                        PaintUtility.CircleDotCap(
                            id, vertices[i], rotation, handleSize * GUIConstants.HANDLE_SCALE * 1.25f
                        );
                    }
                    else
                    {
                        Handles.color = ColorSettings.BrokenCurveStateColor[colorState];

                        PaintUtility.DiamondDotCap(
                            id, vertices[i], rotation, handleSize * GUIConstants.HANDLE_SCALE * 1.25f
                        );
                    }
                }
                else
                {
                    Handles.color = ColorSettings.PointInnerStateColor[colorState];
                    PaintUtility.SquareDotCap(id, vertices[i], rotation, handleSize * GUIConstants.HANDLE_SCALE);
                }
            }
        }

        private static void DrawDotControlStates(
            HandlePointCurve2D[] curvePointHandles, Vector3[] vertices, Quaternion rotation
          , HandleConstraints[] state = null
        )
        {
            DrawDotControlState(SelectState.None, curvePointHandles, vertices, rotation, state, 0);
            DrawDotControlState(SelectState.Hovering, curvePointHandles, vertices, rotation, state, 1);
            DrawDotControlState(SelectState.Selected, curvePointHandles, vertices, rotation, state, 2);

            DrawDotControlState(
                SelectState.Selected | SelectState.Hovering, curvePointHandles, vertices, rotation, state, 3
            );
        }

        private static void DrawDotControlStates(
            int[] dotIds, SelectState[] selectionStates, Vector3[] vertices, Quaternion rotation
          , HandleConstraints[] state = null
        )
        {
            DrawDotControlState(SelectState.None, dotIds, selectionStates, vertices, rotation, state, 0);
            DrawDotControlState(SelectState.Hovering, dotIds, selectionStates, vertices, rotation, state, 1);
            DrawDotControlState(SelectState.Selected, dotIds, selectionStates, vertices, rotation, state, 2);

            DrawDotControlState(
                SelectState.Selected | SelectState.Hovering, dotIds, selectionStates, vertices, rotation, state, 3
            );
        }

        protected override void HandleEditShapeEvents(SceneView sceneView, Rect sceneRect)
        {
            Camera camera = sceneView.camera;

            Vector3[] curvedVertices = null;
            int[][]   curvedEdges    = null;

            //List<ShapePolygon> polygons = null;
            if(Event.current.type == EventType.Layout ||
               Event.current.type == EventType.Repaint)
            {
                curvedVertices = settings.GetCurvedVertices(buildPlane, CurveSides, out curvedEdges);
                ShapePolygonUtility.RemoveDuplicatePoints(ref curvedVertices);
            }

            if(Event.current.type == EventType.MouseDown ||
               Event.current.type == EventType.MouseDrag)
            {
                if(GUIUtility.hotControl == 0 && forceDragHandle)
                {
                    if(StartExtrudeMode(camera))
                    {
                        GrabHeightHandle(sceneView, 1);
                        forceDragHandle = false;
                    }
                }
            }

            HandleHeightHandles(sceneView, sceneRect, true);

            if(Event.current.type != EventType.Layout &&
               (Tools.viewTool == ViewTool.None || Tools.viewTool == ViewTool.Pan))
            {
                int nearestControl    = HandleUtility.nearestControl;
                int foundVertexIndex  = -1;
                int foundTangentIndex = -1;
                int foundEdgeIndex    = -1;

                for(int i = 0; i < settings.VertexLength; i++)
                {
                    int vertexID = settings.curvePointHandles[i].ID;

                    //var vertexID = settings.vertexIDs[i];
                    if(vertexID == nearestControl)
                    {
                        foundVertexIndex = i;

                        break;
                    }

                    int edgeID = settings.curveEdgeHandles[i].ID;

                    if(edgeID == nearestControl)
                    {
                        foundEdgeIndex = i;

                        break;
                    }
                }

                for(int i = 0; i < settings.curveTangentHandles.Length; i++)
                {
                    int tangentID = settings.curveTangentHandles[i].ID;

                    if(tangentID == nearestControl)
                    {
                        foundTangentIndex = i;

                        break;
                    }
                }

                settings.UnHoverAll();

                if(foundVertexIndex != -1)
                {
                    settings.HoverOnVertex(foundVertexIndex);
                }

                if(foundTangentIndex != -1)
                {
                    settings.HoverOnTangent(foundTangentIndex);
                }

                if(foundEdgeIndex != -1)
                {
                    settings.HoverOnEdge(foundEdgeIndex);
                }

                if(foundVertexIndex != settings.prevHoverVertex ||
                   foundTangentIndex != settings.prevHoverTangent ||
                   foundEdgeIndex != settings.prevHoverEdge)
                {
                    CSG_EditorGUIUtility.RepaintAll();
                    settings.prevHoverVertex  = foundVertexIndex;
                    settings.prevHoverTangent = foundTangentIndex;
                    settings.prevHoverEdge    = foundEdgeIndex;
                }
            }

            // render edges
            if(!SceneDragToolManager.IsDraggingObjectInScene && settings.curveEdgeHandles != null &&
               settings.curveEdgeHandles.Length > 0 &&
               Event.current.GetTypeForControl(settings.curveEdgeHandles[0].ID) == EventType.Repaint)
            {
                Color     temp       = Handles.color;
                Matrix4x4 origMatrix = Handles.matrix;

                Handles.matrix = MathConstants.identityMatrix;

                if(settings.realEdge != null)
                {
                    Handles.color = ColorSettings.PointInnerStateColor[0];
                    PaintUtility.DrawDottedLines(settings.realEdge, ColorSettings.PointInnerStateColor[0], 4.0f);
                }

                if(visualSnappedEdges != null)
                {
                    PaintUtility.DrawLines(
                        visualSnappedEdges.ToArray(), GUIConstants.OLD_THICK_LINE_SCALE, ColorSettings.SnappedEdges
                    );
                }

                if(visualSnappedGrid != null)
                {
                    Matrix4x4 _origMatrix = Handles.matrix;
                    Handles.matrix = MathConstants.identityMatrix;
                    PaintUtility.DrawDottedLines(visualSnappedGrid.ToArray(), ColorSettings.SnappedEdges);
                    Handles.matrix = _origMatrix;
                }

                if(visualSnappedBrush)
                {
                    if(visualSnappedBrush.compareTransformation != null &&
                       visualSnappedBrush.ChildData != null &&
                       visualSnappedBrush.ChildData.ModelTransform)
                    {
                        Matrix4x4 brush_transformation = visualSnappedBrush.compareTransformation.localToWorldMatrix;

                        CSGRenderer.DrawOutlines(
                            visualSnappedBrush.brushNodeID, brush_transformation, ColorSettings.SelectedOutlines
                          , ColorSettings.SelectedOutlines, ColorSettings.SelectedOutlines
                          , ColorSettings.SelectedOutlines
                        );
                    }
                }

                Color wireframeColor = ColorSettings.PointInnerStateColor[0];

                int hover_index = -1;
                wireframeColor = ColorSettings.PointInnerStateColor[1];

                for(int i = 0; i < settings.VertexLength; i++)
                {
                    SelectState state = settings.curveEdgeHandles[i].State;

                    if((state & (SelectState.Selected | SelectState.Hovering)) != SelectState.Hovering)
                    {
                        continue;
                    }

                    int[] indices = curvedEdges[i];

                    for(int j = 0; j < indices.Length - 1; j++)
                    {
                        int index0 = indices[j];
                        int index1 = indices[j + 1];

                        if(index0 < 0 || index0 >= curvedVertices.Length ||
                           index1 < 0 || index1 >= curvedVertices.Length)
                        {
                            continue;
                        }

                        Vector3 vertex1 = curvedVertices[index0];
                        Vector3 vertex2 = curvedVertices[index1];

                        PaintUtility.DrawLine(vertex1, vertex2, GUIConstants.OLD_LINE_SCALE, wireframeColor);
                        PaintUtility.DrawDottedLine(vertex1, vertex2, wireframeColor, 4.0f);
                    }
                }

                if(hover_index > -1)
                {
                    Vector3 forward = camera.transform.forward;

                    for(int i = 0; i < settings.VertexLength; i++)
                    {
                        int prev_index = (i + settings.VertexLength - 1) % settings.VertexLength;
                        int curr_index = i;
                        int next_index = (i + 1) % settings.VertexLength;

                        if(haveDragged)
                        {
                            if(prev_index != hover_index &&
                               curr_index != hover_index &&
                               next_index != hover_index)
                            {
                                continue;
                            }
                        }
                        else
                        {
                            if(curr_index != hover_index)
                            {
                                continue;
                            }
                        }

                        int[]   indices = curvedEdges[curr_index];
                        Vector3 origin;

                        if(indices.Length < 2)
                        {
                            continue;
                        }

                        if(indices.Length == 2)
                        {
                            int index0 = indices[0];
                            int index1 = indices[1];

                            if(index0 < 0 || index0 >= curvedVertices.Length ||
                               index1 < 0 || index1 >= curvedVertices.Length)
                            {
                                continue;
                            }

                            origin = (curvedVertices[index0] +
                                curvedVertices[index1]) * 0.5f;
                        }
                        else if((indices.Length & 1) == 0)
                        {
                            int index0 = indices[indices.Length / 2 - 1];
                            int index1 = indices[indices.Length / 2];

                            if(index0 < 0 || index0 >= curvedVertices.Length ||
                               index1 < 0 || index1 >= curvedVertices.Length)
                            {
                                continue;
                            }

                            origin = (curvedVertices[index0] +
                                curvedVertices[index1]) * 0.5f;
                        }
                        else
                        {
                            origin = curvedVertices[indices[indices.Length / 2]];
                        }

                        Vector3 vertex1 = settings.GetPosition(curr_index);
                        Vector3 vertex2 = settings.GetPosition(next_index);

                        Vector3 delta    = vertex1 - vertex2;
                        Vector3 sideways = Vector3.Cross(delta.normalized, forward);
                        float   distance = delta.magnitude;

                        Vector2 textCenter2D = HandleUtility.WorldToGUIPoint(origin);

                        textCenter2D +=
                            (HandleUtility.WorldToGUIPoint(origin + sideways * 10) - textCenter2D).normalized
                          * (hover_text_distance * 2);

                        Ray textCenterRay = HandleUtility.GUIPointToWorldRay(textCenter2D);

                        Vector3 textCenter = textCenterRay.origin + textCenterRay.direction
                          * ((camera.farClipPlane + camera.nearClipPlane) * 0.5f);

                        Handles.color = Color.black;
                        Handles.DrawLine(origin, textCenter);

                        PaintUtility.DrawScreenText(
                            textCenter2D,
                            Units.ToRoundedDistanceString(Mathf.Abs(distance))
                        );
                    }
                }

                Handles.matrix = origMatrix;
                Handles.color  = temp;
            }

            // render vertices
            if(!SceneDragToolManager.IsDraggingObjectInScene &&
               settings.curveEdgeHandles.Length > 0 &&
               Event.current.GetTypeForControl(settings.curvePointHandles[0].ID) == EventType.Repaint)
            {
                Color     temp       = Handles.color;
                Matrix4x4 origMatrix = Handles.matrix;

                Handles.matrix = MathConstants.identityMatrix;
                Quaternion rotation = camera.transform.rotation;

                if(settings.realEdge != null)
                {
                    Handles.color = ColorSettings.PointInnerStateColor[0];
                    PaintUtility.DrawDottedLines(settings.realEdge, ColorSettings.PointInnerStateColor[0], 4.0f);
                }

                if(visualSnappedEdges != null)
                {
                    PaintUtility.DrawLines(
                        visualSnappedEdges.ToArray(), GUIConstants.OLD_THICK_LINE_SCALE, ColorSettings.SnappedEdges
                    );
                }

                if(visualSnappedGrid != null)
                {
                    Matrix4x4 _origMatrix = Handles.matrix;
                    Handles.matrix = MathConstants.identityMatrix;
                    PaintUtility.DrawDottedLines(visualSnappedGrid.ToArray(), ColorSettings.SnappedEdges);
                    Handles.matrix = _origMatrix;
                }

                if(visualSnappedBrush)
                {
                    if(visualSnappedBrush.compareTransformation != null &&
                       visualSnappedBrush.ChildData != null &&
                       visualSnappedBrush.ChildData.ModelTransform)
                    {
                        Matrix4x4 brush_transformation = visualSnappedBrush.compareTransformation.localToWorldMatrix;

                        CSGRenderer.DrawOutlines(
                            visualSnappedBrush.brushNodeID, brush_transformation, ColorSettings.SelectedOutlines
                          , ColorSettings.SelectedOutlines, ColorSettings.SelectedOutlines
                          , ColorSettings.SelectedOutlines
                        );
                    }
                }

                int nearControl = HandleUtility.nearestControl;
                int hotControl  = GUIUtility.hotControl;
                int hover_index = -1;

                for(int i = 0, j = 0; i < settings.VertexLength; i++, j += 2)
                {
                    if(settings.curvePointHandles[i].ID == hotControl ||
                       settings.curveTangentHandles[j + 0].ID == hotControl ||
                       settings.curveTangentHandles[j + 1].ID == hotControl ||
                       (hotControl == 0 &&
                           (settings.curvePointHandles[i].ID == nearControl ||
                               settings.curveTangentHandles[j + 0].ID == nearControl ||
                               settings.curveTangentHandles[j + 1].ID == nearControl
                           )))
                    {
                        hover_index = i;
                    }
                }

                if(settings.curve.tangents.Length == settings.curve.points.Length * 2 &&
                   settings.curveTangentHandles.Length == settings.curve.tangents.Length)
                {
                    int curvedEdgeCount = 0;

                    for(int i = 0; i < settings.curveTangentHandles.Length; i += 2)
                    {
                        if(settings.curveTangentHandles[i + 0].Constraint != HandleConstraints.Straight)
                        {
                            curvedEdgeCount++;
                        }

                        if(settings.curveTangentHandles[i + 1].Constraint != HandleConstraints.Straight)
                        {
                            curvedEdgeCount++;
                        }
                    }

                    if(settings.realTangent == null ||
                       settings.realTangent.Length != curvedEdgeCount)
                    {
                        settings.realTangent          = new Vector3[curvedEdgeCount];
                        settings.realTangentIDs       = new int[curvedEdgeCount];
                        settings.realTangentSelection = new SelectState[curvedEdgeCount];
                        settings.realTangentState     = new HandleConstraints[curvedEdgeCount];
                    }

                    int counter = 0;

                    for(int i = 0, j = 0; j < settings.curveTangentHandles.Length; i++, j += 2)
                    {
                        HandleConstraints state =
                            settings.curveTangentHandles[j + 0].Constraint == HandleConstraints.Mirrored &&
                            settings.curveTangentHandles[j + 1].Constraint == HandleConstraints.Mirrored
                                ? HandleConstraints.Mirrored
                                : HandleConstraints.Broken;

                        if(settings.curveTangentHandles[j + 0].Constraint != HandleConstraints.Straight)
                        {
                            settings.realTangent[counter] =
                                settings.curve.points[i] - settings.curve.tangents[j + 0].tangent;

                            settings.realTangentIDs[counter]       = settings.curveTangentHandles[j + 0].ID;
                            settings.realTangentSelection[counter] = settings.curveTangentHandles[j + 0].State;
                            settings.realTangentState[counter]     = state;
                            PaintUtility.DrawLine(settings.curve.points[i], settings.realTangent[counter], Color.black);

                            PaintUtility.DrawDottedLine(
                                settings.curve.points[i], settings.realTangent[counter], Color.black, 4.0f
                            );

                            counter++;
                        }

                        if(settings.curveTangentHandles[j + 1].Constraint != HandleConstraints.Straight)
                        {
                            settings.realTangent[counter] =
                                settings.curve.points[i] - settings.curve.tangents[j + 1].tangent;

                            settings.realTangentIDs[counter]       = settings.curveTangentHandles[j + 1].ID;
                            settings.realTangentSelection[counter] = settings.curveTangentHandles[j + 1].State;
                            settings.realTangentState[counter]     = state;
                            PaintUtility.DrawLine(settings.curve.points[i], settings.realTangent[counter], Color.black);

                            PaintUtility.DrawDottedLine(
                                settings.curve.points[i], settings.realTangent[counter], Color.black, 4.0f
                            );

                            counter++;
                        }
                    }

                    if(counter > 0)
                    {
                        DrawDotControlStates(
                            settings.realTangentIDs, settings.realTangentSelection, settings.realTangent, rotation
                          , settings.realTangentState
                        );
                    }
                }

                DrawDotControlStates(settings.curvePointHandles, settings.GetVertices(), rotation);

                if(hover_index > -1)
                {
                    Vector3 forward = camera.transform.forward;

                    for(int i = 0; i < settings.VertexLength; i++)
                    {
                        int curr_index = i;
                        int next_index = (i + 1) % settings.VertexLength;

                        if(curr_index != hover_index &&
                           next_index != hover_index)
                        {
                            continue;
                        }

                        if(curvedEdges == null || curr_index >= curvedEdges.Length)
                        {
                            continue;
                        }

                        int[] indices = curvedEdges[curr_index];

                        if(indices.Length < 3)
                        {
                            continue;
                        }

                        Vector3 origin;

                        if((indices.Length & 1) == 0)
                        {
                            origin = (curvedVertices[indices[indices.Length / 2 - 1]] +
                                curvedVertices[indices[indices.Length / 2]]) * 0.5f;
                        }
                        else
                        {
                            origin = curvedVertices[indices[indices.Length / 2]];
                        }

                        Vector3 vertex1 = settings.GetPosition(curr_index);
                        Vector3 vertex2 = settings.GetPosition(next_index);

                        Vector3 delta    = vertex1 - vertex2;
                        Vector3 sideways = Vector3.Cross(delta.normalized, forward);
                        float   distance = delta.magnitude;

                        Vector2 textCenter2D = HandleUtility.WorldToGUIPoint(origin);

                        textCenter2D +=
                            (HandleUtility.WorldToGUIPoint(origin + sideways * 10) - textCenter2D).normalized
                          * (hover_text_distance * 2);

                        Ray textCenterRay = HandleUtility.GUIPointToWorldRay(textCenter2D);

                        Vector3 textCenter = textCenterRay.origin + textCenterRay.direction
                          * ((camera.farClipPlane + camera.nearClipPlane) * 0.5f);

                        Handles.color = Color.black;
                        Handles.DrawLine(origin, textCenter);

                        PaintUtility.DrawScreenText(
                            textCenter2D,
                            Units.ToRoundedDistanceString(Mathf.Abs(distance))
                        );
                    }
                }

                Handles.matrix = origMatrix;
                Handles.color  = temp;
            }

            switch(Event.current.type) //.GetTypeForControl(base.shapeId))
            {
                case EventType.ValidateCommand:
                case EventType.KeyDown:
                {
                    //if (GUIUtility.hotControl != base.shapeId)
                    //	return;
                    if(Keys.insertPoint.IsKeyPressed())
                    {
                        Event.current.Use();

                        break;
                    }

                    break;
                }
                case EventType.KeyUp:
                {
                    //if (GUIUtility.hotControl != base.shapeId)
                    //	return;
                    if(Keys.insertPoint.IsKeyPressed())
                    {
                        SplitSelectedEdge();
                        Event.current.Use();

                        return;
                    }

                    break;
                }
            }

            if(Event.current != null &&
               Event.current.type == EventType.Repaint)
            {
                Handles.color = ColorSettings.WireframeOutline;

                for(int i = 0; i < settings.VertexLength; i++)
                {
                    int curr_index = i;

                    if(curvedEdges == null || curr_index >= curvedEdges.Length)
                    {
                        continue;
                    }

                    int[] indices = curvedEdges[curr_index];

                    if(indices.Length < 3)
                    {
                        continue;
                    }

                    for(int j = 1; j < indices.Length - 1; j++)
                    {
                        float handleSize = CSG_HandleUtility.GetHandleSize(curvedVertices[indices[j]]) * 0.5f;
                        PaintUtility.SquareDotCap(-1, curvedVertices[indices[j]], Quaternion.identity, handleSize);
                    }
                }
            }

            // edges
            for(int i = 0; i < settings.VertexLength; i++)
            {
                int id = settings.curveEdgeHandles[i].ID;

                switch(Event.current.GetTypeForControl(id))
                { /*
                    case EventType.Repaint:
                    {
                        if (camera == null ||
                            (Tools.viewTool != ViewTool.None && Tools.viewTool != ViewTool.Pan))
                            break;

                        if (curvedEdges != null &&
                            curvedEdges.Length > i)
                        {
                            var indices = curvedEdges[i];
                            if (indices != null &&
                                indices.Length > 0)
                            {
                                for (int j = 0; j < indices.Length - 1; j++)
                                {
                                    var index0 = indices[j];
                                    var index1 = indices[j + 1];

                                    if (index0 < 0 || index0 >= curvedVertices.Length ||
                                        index1 < 0 || index1 >= curvedVertices.Length)
                                        continue;

                                    var vertex1 = curvedVertices[index0];
                                    var vertex2 = curvedVertices[index1];

                                    PaintUtility.DrawLine(vertex1, vertex2, Color.red);
                                }
                            }
                        }
                        break;
                    }*/
                    case EventType.Layout:
                    {
                        if(camera == null ||
                           (Tools.viewTool != ViewTool.None && Tools.viewTool != ViewTool.Pan))
                        {
                            break;
                        }

                        CSGPlane cameraPlane = CSG_HandleUtility.GetNearPlane(camera);

                        Matrix4x4 origMatrix = Handles.matrix;
                        Handles.matrix = MathConstants.identityMatrix;

                        if(curvedEdges != null &&
                           curvedEdges.Length > i)
                        {
                            int[] indices = curvedEdges[i];

                            if(indices != null &&
                               indices.Length > 0)
                            {
                                Vector2 mousePoint = Event.current.mousePosition;
                                float   distance   = float.PositiveInfinity;

                                for(int j = 0; j < indices.Length - 1; j++)
                                {
                                    int index0 = indices[j];
                                    int index1 = indices[j + 1];

                                    if(index0 < 0 || index0 >= curvedVertices.Length ||
                                       index1 < 0 || index1 >= curvedVertices.Length)
                                    {
                                        continue;
                                    }

                                    Vector3 vertex1 = curvedVertices[index0];
                                    Vector3 vertex2 = curvedVertices[index1];

                                    float line_distance = CameraUtility.DistanceToLine(
                                        cameraPlane, mousePoint, vertex1, vertex2
                                    );

                                    if(line_distance < distance)
                                    {
                                        distance = line_distance;
                                    }
                                }

                                HandleUtility.AddControl(id, distance);
                            }
                        }

                        Handles.matrix = origMatrix;

                        break;
                    }

                    case EventType.ValidateCommand:
                    case EventType.KeyDown:
                    {
                        if(GUIUtility.hotControl == id)
                        {
                            if(Keys.cancelActionKey.IsKeyPressed())
                            {
                                Event.current.Use();

                                break;
                            }
                        }

                        break;
                    }
                    case EventType.KeyUp:
                    {
                        if(GUIUtility.hotControl == id)
                        {
                            if(Keys.cancelActionKey.IsKeyPressed())
                            {
                                GUIUtility.hotControl             = 0;
                                GUIUtility.keyboardControl        = 0;
                                EditorGUIUtility.editingTextField = false;
                                Event.current.Use();

                                break;
                            }
                        }

                        break;
                    }

                    case EventType.MouseDown:
                    {
                        if(!sceneRect.Contains(Event.current.mousePosition))
                        {
                            break;
                        }

                        if(GUIUtility.hotControl == 0 &&
                           HandleUtility.nearestControl == id && Event.current.button == 0 &&
                           //(Event.current.modifiers == EventModifiers.None) &&
                           (Tools.viewTool == ViewTool.None || Tools.viewTool == ViewTool.Pan))
                        {
                            GUIUtility.hotControl             = id;
                            GUIUtility.keyboardControl        = id;
                            EditorGUIUtility.editingTextField = false;
                            EditorGUIUtility.SetWantsMouseJumping(1);
                            Event.current.Use();
                            clickMousePosition = Event.current.mousePosition;
                            prevDragDifference = MathConstants.zeroVector3;
                            haveDragged        = false;
                            settings.realEdge  = null;
                            settings.CopyBackupVertices();

                            break;
                        }

                        break;
                    }
                    case EventType.MouseMove:
                    {
                        clickCount = 0;

                        break;
                    }
                    case EventType.MouseDrag:
                    {
                        clickCount = 0;

                        if(GUIUtility.hotControl == id && Event.current.button == 0 &&
                           (Tools.viewTool == ViewTool.None || Tools.viewTool == ViewTool.Pan))
                        {
                            if(!haveDragged)
                            {
                                float diff = (Event.current.mousePosition - clickMousePosition).magnitude;

                                if(diff < 1)
                                {
                                    Event.current.Use();

                                    break;
                                }
                            }

                            CSGGrid.SetForcedGrid(camera, buildPlane);

                            Undo.RecordObject(this, "Modify shape");

                            if(!settings.IsEdgeSelected(i))
                            {
                                settings.DeselectAll();
                                settings.SelectEdge(i, SelectionType.Replace);
                            }

                            Vector3 startDragPoint =
                                buildPlane.RayIntersection(HandleUtility.GUIPointToWorldRay(clickMousePosition));

                            Vector3 intersectionPoint = buildPlane.RayIntersection(
                                HandleUtility.GUIPointToWorldRay(Event.current.mousePosition)
                            );

                            Vector3 difference = intersectionPoint - startDragPoint;

                            int     k            = (i + settings.VertexLength - 1) % settings.VertexLength;
                            int     j            = (i + 1) % settings.VertexLength;
                            int     l            = (i + 2) % settings.VertexLength;
                            Vector3 movedVertex1 = settings.backupCurve.points[i] + difference;
                            Vector3 movedVertex2 = settings.backupCurve.points[j] + difference;

                            settings.realEdge = new Vector3[]
                            {
                                settings.backupCurve.points[k], movedVertex1, movedVertex1, movedVertex2
                              , movedVertex2, settings.backupCurve.points[l]
                            };

                            if(snapFunction != null)
                            {
                                ResetVisuals();
                                CSGBrush snappedOnBrush1;
                                CSGBrush snappedOnBrush2;

                                Vector3 vertexDifference1 = snapFunction(
                                        camera, movedVertex1, buildPlane, ref visualSnappedEdges, out snappedOnBrush1
                                      , generatedBrushes
                                    )
                                    //point_moved1
                                  - settings.backupCurve.points[i];

                                Vector3 vertexDifference2 = snapFunction(
                                        camera, movedVertex2, buildPlane, ref visualSnappedEdges, out snappedOnBrush2
                                      , generatedBrushes
                                    )
                                    //point_moved2
                                  - settings.backupCurve.points[j];

                                if((vertexDifference1 - difference).sqrMagnitude
                                 < (vertexDifference2 - difference).sqrMagnitude)
                                {
                                    difference = vertexDifference1;
                                }
                                else
                                {
                                    difference = vertexDifference2;
                                }

                                float snap_distance = float.PositiveInfinity;

                                for(int p = 0; p < settings.backupCurve.points.Length; p++)
                                {
                                    if(p == i)
                                    {
                                        continue;
                                    }

                                    float handleSize = CSG_HandleUtility.GetHandleSize(settings.backupCurve.points[p]);
                                    float scaledHandleSize = handleSize * GUIConstants.HANDLE_SCALE * handle_extension;

                                    float distance = GeometryUtility.DistancePointToCircle(
                                        sceneView, movedVertex1, settings.backupCurve.points[p], scaledHandleSize
                                    );

                                    if(distance < handle_on_distance)
                                    {
                                        if(distance < snap_distance)
                                        {
                                            snap_distance = distance;

                                            difference = settings.backupCurve.points[p]
                                              - settings.backupCurve.points[i];
                                        }
                                    }

                                    distance = GeometryUtility.DistancePointToCircle(
                                        sceneView, movedVertex2, settings.backupCurve.points[p], scaledHandleSize
                                    );

                                    if(distance < handle_on_distance)
                                    {
                                        if(distance < snap_distance)
                                        {
                                            snap_distance = distance;

                                            difference = settings.backupCurve.points[p]
                                              - settings.backupCurve.points[j];
                                        }
                                    }
                                }
                            }

                            if(prevDragDifference != difference)
                            {
                                for(int p = 0; p < settings.backupCurve.points.Length; p++)
                                {
                                    if(settings.IsVertexSelected(p))
                                    {
                                        settings.SetPosition(p, difference + settings.backupCurve.points[p]);
                                    }
                                }

                                prevDragDifference = difference;
                                CSG_EditorGUIUtility.RepaintAll();
                            }

                            if(editMode == EditMode.ExtrudeShape)
                            {
                                StartExtrudeMode(camera, false);
                            }

                            UpdateBaseShape(false);

                            GUI.changed = true;
                            Event.current.Use();
                            haveDragged = true;

                            break;
                        }

                        break;
                    }
                    case EventType.MouseUp:
                    {
                        if(GUIUtility.hotControl == id && Event.current.button == 0 &&
                           (Tools.viewTool == ViewTool.None || Tools.viewTool == ViewTool.Pan))
                        {
                            GUIUtility.hotControl             = 0;
                            GUIUtility.keyboardControl        = 0;
                            EditorGUIUtility.editingTextField = false;
                            EditorGUIUtility.SetWantsMouseJumping(0);
                            Event.current.Use();

                            settings.realEdge = null;
                            ResetVisuals();

                            if(!haveDragged)
                            {
                                if(Event.current.modifiers == EventModifiers.None)
                                {
                                    clickCount++;

                                    if(clickCount > 1)
                                    {
                                        if((clickCount & 2) == 2)
                                        {
                                            ToggleSubdivisionOnSelectedEdges();
                                        }

                                        break;
                                    }
                                }

                                SelectionType selectionType = SelectionUtility.GetEventSelectionType();

                                if(selectionType == SelectionType.Replace)
                                {
                                    settings.DeselectAll();
                                }

                                settings.SelectEdge(i, selectionType);
                            }
                            else
                            {
                                List<int> removeVertices = new List<int>();

                                for(int p0 = settings.VertexLength - 1, p1 = 0
                                    ; p1 < settings.VertexLength
                                    ; p0 = p1, p1++)
                                {
                                    if((settings.GetPosition(p0) - settings.GetPosition(p1)).sqrMagnitude == 0)
                                    {
                                        if(settings.IsVertexSelected(p0))
                                        {
                                            removeVertices.Add(p1);
                                        }
                                        else if(settings.IsVertexSelected(p1))
                                        {
                                            removeVertices.Add(p0);
                                        }
                                    }
                                }

                                if(removeVertices.Count > 0)
                                {
                                    Undo.RecordObject(this, "Deleted vertices");
                                }

                                for(int r = removeVertices.Count - 1; r >= 0; r--)
                                {
                                    settings.SelectVertex(removeVertices[r]);
                                }

                                removeVertices.Sort();

                                for(int r = removeVertices.Count - 1; r >= 0; r--)
                                {
                                    settings.DeleteVertex(removeVertices[r]);
                                }
                            }

                            if(haveDragged && settings.VertexLength < 3)
                            {
                                Cancel();
                            }

                            haveDragged = false;

                            break;
                        }

                        break;
                    }
                }
            }

            // tangents
            if(settings.curveTangentHandles != null &&
               settings.curve.tangents != null)
            {
                for(int i = 0; i < settings.curveTangentHandles.Length; i++)
                {
                    int id = settings.curveTangentHandles[i].ID;

                    switch(Event.current.GetTypeForControl(id))
                    {
                        case EventType.Layout:
                        {
                            if(Tools.viewTool != ViewTool.None && Tools.viewTool != ViewTool.Pan)
                            {
                                break;
                            }

                            if(i >= settings.curve.tangents.Length)
                            {
                                break;
                            }

                            Matrix4x4 origMatrix = Handles.matrix;
                            Handles.matrix = MathConstants.identityMatrix;
                            Vector3 tangent          = settings.curve.tangents[i].tangent;
                            Vector3 vertex           = settings.curve.points[i / 2] - tangent;
                            float   handleSize       = CSG_HandleUtility.GetHandleSize(vertex);
                            float   scaledHandleSize = handleSize * GUIConstants.HANDLE_SCALE * handle_extension;
                            HandleUtility.AddControl(id, HandleUtility.DistanceToCircle(vertex, scaledHandleSize));
                            Handles.matrix = origMatrix;

                            break;
                        }

                        case EventType.ValidateCommand:
                        case EventType.KeyDown:
                        {
                            if(GUIUtility.hotControl == id)
                            {
                                if(Keys.cancelActionKey.IsKeyPressed())
                                {
                                    Event.current.Use();

                                    break;
                                }
                            }

                            break;
                        }
                        case EventType.KeyUp:
                        {
                            if(GUIUtility.hotControl == id)
                            {
                                if(Keys.cancelActionKey.IsKeyPressed())
                                {
                                    GUIUtility.hotControl             = 0;
                                    GUIUtility.keyboardControl        = 0;
                                    EditorGUIUtility.editingTextField = false;
                                    Event.current.Use();

                                    break;
                                }
                            }

                            break;
                        }

                        case EventType.MouseDown:
                        {
                            if(!sceneRect.Contains(Event.current.mousePosition))
                            {
                                break;
                            }

                            if(GUIUtility.hotControl == 0 &&
                               HandleUtility.nearestControl == id && Event.current.button == 0 &&
                               //(Event.current.modifiers == EventModifiers.None) &&
                               (Tools.viewTool == ViewTool.None || Tools.viewTool == ViewTool.Pan))
                            {
                                GUIUtility.hotControl             = id;
                                GUIUtility.keyboardControl        = id;
                                EditorGUIUtility.editingTextField = false;
                                EditorGUIUtility.SetWantsMouseJumping(1);
                                Event.current.Use();
                                clickMousePosition = Event.current.mousePosition;
                                prevDragDifference = MathConstants.zeroVector3;
                                haveDragged        = false;
                                settings.CopyBackupVertices();

                                break;
                            }

                            break;
                        }
                        case EventType.MouseMove:
                        {
                            clickCount = 0;

                            break;
                        }
                        case EventType.MouseDrag:
                        {
                            clickCount = 0;

                            if(GUIUtility.hotControl == id && Event.current.button == 0 &&
                               (Tools.viewTool == ViewTool.None || Tools.viewTool == ViewTool.Pan))
                            {
                                if(!haveDragged)
                                {
                                    float diff = (Event.current.mousePosition - clickMousePosition).magnitude;

                                    if(diff < 1)
                                    {
                                        Event.current.Use();

                                        break;
                                    }
                                }

                                CSGGrid.SetForcedGrid(camera, buildPlane);

                                Undo.RecordObject(this, "Modify shape curvature");

                                if(!settings.IsTangentSelected(i))
                                {
                                    settings.DeselectAll();
                                    settings.SelectTangent(i, SelectionType.Replace);
                                }

                                Ray mouseRay = HandleUtility.GUIPointToWorldRay(Event.current.mousePosition);

                                Vector3 vertex = settings.backupCurve.points[i / 2]
                                  - settings.backupCurve.tangents[i].tangent;

                                CSGPlane alignedPlane = new CSGPlane(CSGGrid.CurrentWorkGridPlane.normal, vertex);
                                worldPosition = buildPlane.Project(alignedPlane.RayIntersection(mouseRay));

                                if(float.IsInfinity(worldPosition.x) || float.IsNaN(worldPosition.x) ||
                                   float.IsInfinity(worldPosition.y) || float.IsNaN(worldPosition.y) ||
                                   float.IsInfinity(worldPosition.z) || float.IsNaN(worldPosition.z))
                                {
                                    worldPosition = vertex;
                                }

                                Vector3 difference = worldPosition - vertex;

                                CSGBrush snappedOnBrush = null;

                                if(snapFunction != null)
                                {
                                    difference = snapFunction(
                                        camera, worldPosition, buildPlane, ref visualSnappedEdges, out snappedOnBrush
                                      , generatedBrushes
                                    ) - vertex;
                                }

                                visualSnappedGrid = CSGGrid.FindAllGridEdgesThatTouchPoint(camera, difference + vertex);

                                ResetVisuals();

                                if(prevDragDifference != difference)
                                {
                                    for(int t = 0; t < settings.backupCurve.tangents.Length; t++)
                                    {
                                        if(settings.IsTangentSelected(t))
                                        {
                                            settings.SetTangent(
                                                t, settings.backupCurve.tangents[t].tangent - difference
                                            );
                                        }
                                    }

                                    prevDragDifference = difference;
                                    CSG_EditorGUIUtility.RepaintAll();
                                }

                                if(editMode == EditMode.ExtrudeShape)
                                {
                                    StartExtrudeMode(camera, false);
                                }

                                UpdateBaseShape(false);

                                GUI.changed = true;
                                Event.current.Use();
                                haveDragged = true;

                                break;
                            }

                            break;
                        }
                        case EventType.MouseUp:
                        {
                            if(GUIUtility.hotControl == id && Event.current.button == 0 &&
                               (Tools.viewTool == ViewTool.None || Tools.viewTool == ViewTool.Pan))
                            {
                                GUIUtility.hotControl             = 0;
                                GUIUtility.keyboardControl        = 0;
                                EditorGUIUtility.editingTextField = false;
                                EditorGUIUtility.SetWantsMouseJumping(0);
                                Event.current.Use();

                                ResetVisuals();

                                if(!haveDragged)
                                {
                                    SelectionType selectionType = SelectionUtility.GetEventSelectionType();

                                    if(selectionType == SelectionType.Replace)
                                    {
                                        settings.DeselectAll();
                                    }

                                    settings.SelectTangent(i, selectionType);
                                }

                                break;
                            }

                            break;
                        }
                    }
                }
            }

            // vertices
            for(int i = 0; i < settings.VertexLength; i++)
            {
                int id = settings.curvePointHandles[i].ID;

                //float length = (shape2D.GetPosition(i) - prevVertex).sqrMagnitude;
                //base.extrusionPoints += (shape2D.GetPosition(i) + prevVertex) * 0.5f * length;
                //totalLength += length;
                //prevVertex = shape2D.GetPosition(i);

                switch(Event.current.GetTypeForControl(id))
                {
                    case EventType.Layout:
                    {
                        if(Tools.viewTool != ViewTool.None && Tools.viewTool != ViewTool.Pan)
                        {
                            break;
                        }

                        Matrix4x4 origMatrix = Handles.matrix;
                        Handles.matrix = MathConstants.identityMatrix;
                        float handleSize       = CSG_HandleUtility.GetHandleSize(settings.GetPosition(i));
                        float scaledHandleSize = handleSize * GUIConstants.HANDLE_SCALE * handle_extension;

                        HandleUtility.AddControl(
                            id, HandleUtility.DistanceToCircle(settings.GetPosition(i), scaledHandleSize)
                        );

                        Handles.matrix = origMatrix;

                        break;
                    }

                    case EventType.ValidateCommand:
                    case EventType.KeyDown:
                    {
                        if(GUIUtility.hotControl == id)
                        {
                            if(Keys.cancelActionKey.IsKeyPressed())
                            {
                                Event.current.Use();

                                break;
                            }
                        }

                        break;
                    }
                    case EventType.KeyUp:
                    {
                        if(GUIUtility.hotControl == id)
                        {
                            if(Keys.cancelActionKey.IsKeyPressed())
                            {
                                GUIUtility.hotControl             = 0;
                                GUIUtility.keyboardControl        = 0;
                                EditorGUIUtility.editingTextField = false;
                                Event.current.Use();

                                break;
                            }
                        }

                        break;
                    }

                    case EventType.MouseDown:
                    {
                        if(!sceneRect.Contains(Event.current.mousePosition))
                        {
                            break;
                        }

                        if(GUIUtility.hotControl == 0 &&
                           HandleUtility.nearestControl == id && Event.current.button == 0 &&
                           //(Event.current.modifiers == EventModifiers.None) &&
                           (Tools.viewTool == ViewTool.None || Tools.viewTool == ViewTool.Pan))
                        {
                            GUIUtility.hotControl             = id;
                            GUIUtility.keyboardControl        = id;
                            EditorGUIUtility.editingTextField = false;
                            EditorGUIUtility.SetWantsMouseJumping(1);
                            Event.current.Use();
                            clickMousePosition = Event.current.mousePosition;
                            prevDragDifference = MathConstants.zeroVector3;
                            haveDragged        = false;
                            settings.CopyBackupVertices();

                            break;
                        }

                        break;
                    }
                    case EventType.MouseMove:
                    {
                        clickCount = 0;

                        break;
                    }
                    case EventType.MouseDrag:
                    {
                        clickCount = 0;

                        if(GUIUtility.hotControl == id && Event.current.button == 0 &&
                           (Tools.viewTool == ViewTool.None || Tools.viewTool == ViewTool.Pan))
                        {
                            if(!haveDragged)
                            {
                                float diff = (Event.current.mousePosition - clickMousePosition).magnitude;

                                if(diff < 1)
                                {
                                    Event.current.Use();

                                    break;
                                }
                            }

                            CSGGrid.SetForcedGrid(camera, buildPlane);

                            Undo.RecordObject(this, "Modify shape");

                            if(!settings.IsVertexSelected(i))
                            {
                                settings.DeselectAll();
                                settings.SelectVertex(i, SelectionType.Replace);
                            }

                            Ray mouseRay = HandleUtility.GUIPointToWorldRay(Event.current.mousePosition);

                            CSGPlane alignedPlane = new CSGPlane(
                                CSGGrid.CurrentWorkGridPlane.normal, settings.backupCurve.points[i]
                            );

                            Vector3 worldPosition = buildPlane.Project(alignedPlane.RayIntersection(mouseRay));

                            if(float.IsInfinity(worldPosition.x) || float.IsNaN(worldPosition.x) ||
                               float.IsInfinity(worldPosition.y) || float.IsNaN(worldPosition.y) ||
                               float.IsInfinity(worldPosition.z) || float.IsNaN(worldPosition.z))
                            {
                                worldPosition = settings.backupCurve.points[i];
                            }

                            Vector3 difference = worldPosition - settings.backupCurve.points[i];

                            ResetVisuals();
                            CSGBrush snappedOnBrush = null;

                            if(snapFunction != null)
                            {
                                difference = snapFunction(
                                    camera, worldPosition, buildPlane, ref visualSnappedEdges, out snappedOnBrush
                                  , generatedBrushes
                                ) - settings.backupCurve.points[i];
                            }

                            visualSnappedGrid = CSGGrid.FindAllGridEdgesThatTouchPoint(
                                camera, difference + settings.backupCurve.points[i]
                            );

                            {
                                int   snapToVertexIndex = -1;
                                float snapDistance      = float.PositiveInfinity;

                                for(int p = 0; p < settings.backupCurve.points.Length; p++)
                                {
                                    if(p == i)
                                    {
                                        continue;
                                    }

                                    float handleSize = CSG_HandleUtility.GetHandleSize(settings.backupCurve.points[p]);
                                    float scaledHandleSize = handleSize * GUIConstants.HANDLE_SCALE * handle_extension;

                                    float distance = HandleUtility.DistanceToCircle(
                                        settings.backupCurve.points[p], scaledHandleSize
                                    );

                                    if(distance < handle_on_distance)
                                    {
                                        if(distance < snapDistance)
                                        {
                                            snapDistance      = distance;
                                            snapToVertexIndex = p;
                                        }
                                    }
                                }

                                if(snapToVertexIndex != -1)
                                {
                                    difference = settings.backupCurve.points[snapToVertexIndex]
                                      - settings.backupCurve.points[i];
                                }
                            }

                            if(prevDragDifference != difference)
                            {
                                for(int p = 0; p < settings.backupCurve.points.Length; p++)
                                {
                                    if(settings.IsVertexSelected(p))
                                    {
                                        settings.SetPosition(p, difference + settings.backupCurve.points[p]);
                                    }
                                }

                                prevDragDifference = difference;
                                CSG_EditorGUIUtility.RepaintAll();
                            }

                            if(editMode == EditMode.ExtrudeShape)
                            {
                                StartExtrudeMode(camera, false);
                            }

                            UpdateBaseShape(false);

                            GUI.changed = true;
                            Event.current.Use();
                            haveDragged = true;

                            break;
                        }

                        break;
                    }
                    case EventType.MouseUp:
                    {
                        if(GUIUtility.hotControl != id || Event.current.button != 0 ||
                           (Tools.viewTool != ViewTool.None && Tools.viewTool != ViewTool.Pan))
                        {
                            continue;
                        }

                        GUIUtility.hotControl             = 0;
                        GUIUtility.keyboardControl        = 0;
                        EditorGUIUtility.editingTextField = false;
                        EditorGUIUtility.SetWantsMouseJumping(0);
                        Event.current.Use();

                        ResetVisuals();

                        if(!haveDragged)
                        {
                            if(Event.current.modifiers == EventModifiers.None)
                            {
                                clickCount++;

                                if(clickCount > 1)
                                {
                                    if((clickCount & 2) == 2)
                                    {
                                        ToggleSubdivisionOnVertex(i);
                                    }

                                    break;
                                }
                            }

                            SelectionType selectionType = SelectionUtility.GetEventSelectionType();

                            if(selectionType == SelectionType.Replace)
                            {
                                settings.DeselectAll();
                            }

                            settings.SelectVertex(i, selectionType);
                        }
                        else
                        {
                            List<int> removeVertices = new List<int>();

                            for(int p0 = settings.VertexLength - 1, p1 = 0; p1 < settings.VertexLength; p0 = p1, p1++)
                            {
                                if((settings.GetPosition(p0) - settings.GetPosition(p1)).sqrMagnitude == 0)
                                {
                                    if(settings.IsVertexSelected(p0))
                                    {
                                        removeVertices.Add(p1);
                                    }
                                    else if(settings.IsVertexSelected(p1))
                                    {
                                        removeVertices.Add(p0);
                                    }
                                }
                            }

                            if(removeVertices.Count > 0)
                            {
                                Undo.RecordObject(this, "Deleted vertices");
                            }

                            for(int r = removeVertices.Count - 1; r >= 0; r--)
                            {
                                settings.SelectVertex(removeVertices[r]);
                            }

                            removeVertices.Sort();

                            for(int r = removeVertices.Count - 1; r >= 0; r--)
                            {
                                settings.DeleteVertex(removeVertices[r]);
                            }
                        }

                        if(haveDragged && settings.VertexLength < 3)
                        {
                            Cancel();
                        }

                        break;
                    }
                }
            }
        }

        public bool OnShowGUI(bool isSceneGUI)
        {
            return GeneratorFreeDrawGUI.OnShowGUI(this, isSceneGUI);
        }
    }
}
