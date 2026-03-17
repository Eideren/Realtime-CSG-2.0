using System;
using System.Collections.Generic;
using InternalRealtimeCSG;
using RealtimeCSG.Components;
using RealtimeCSG.Foundation;
using RealtimeCSG.Legacy;
using UnityEditor;
using UnityEngine;

namespace RealtimeCSG
{
    internal sealed class GeneratorFreeDraw : GeneratorExtrudedBase, IGenerator
    {
        [NonSerialized] private Vector3 _worldPosition;
        [NonSerialized] private Vector3 _prevWorldPosition;
        [NonSerialized] private bool _onLastPoint;
        [NonSerialized] private CSGPlane _geometryPlane = new CSGPlane(0, 1, 0, 0);
        [NonSerialized] private CSGPlane? _hoverDefaultPlane;

        [NonSerialized] private bool _haveDragged;
        [NonSerialized] private bool _generateSmoothing = true;

        [NonSerialized] private Vector3 _prevDragDifference;
        [NonSerialized] private Vector2 _clickMousePosition;
        [NonSerialized] private int _clickCount;

        // free-draw specific
        private static readonly int shapeBuilderEdgeHash = "CSGShapeBuilderEdge".GetHashCode();
        private const float HANDLE_ON_DISTANCE = 4.0f;

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
        private float _alpha = 1.0f;

        public float Alpha
        {
            get => _alpha;
            set
            {
                if(_alpha == value)
                {
                    return;
                }

                Undo.RecordObject(this, "Modified Shape Subdivision");
                _alpha = value;
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
            _haveDragged    = false;
        }

        public void GenerateFromPolygon(
            Camera camera, CSGBrush brush, CSGPlane plane, Vector3 direction, Vector3[] meshVertices, int[] indices
          , uint[] smoothingGroups, bool drag, CSGOperationType dragSource, bool commitAfterRelease
        )
        {
            _generateSmoothing = false;
            Init();

            this.commitExtrusionAfterRelease = commitAfterRelease;
            forceDragHandle                  = drag;
            this.forceDragSource             = dragSource;
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
            _geometryPlane = plane;
            buildPlane     = plane;
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

            extrusionPoints[1].position =
                extrusionPoints[0].position = brushPosition;

            for(int i = 0; i < indices.Length; i++)
            {
                settings.onGeometryVertices[i]                     = true;
                settings.onBrushVertices[i]                        = brush;
                settings.curveEdgeHandles[i].texgen.SmoothingGroup = smoothingGroups[i];
            }
        }

        public override void Reset()
        {
            settings.Reset();
            base.Reset();
            _onLastPoint       = false;
            _hoverDefaultPlane = null;
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

                editMode = EditMode.CreateShape;
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

            switch(settings.curveTangentHandles[index * 2 + 0].constraint)
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
                    if((settings.curveEdgeHandles[a].state & SelectState.Selected) == 0)
                    {
                        continue;
                    }

                    if(state == null)
                    {
                        state = settings.curveTangentHandles[a * 2 + 1].constraint;
                    }

                    if(state.Value != settings.curveTangentHandles[a * 2 + 1].constraint)
                    {
                        return null;
                    }

                    if(state.Value != settings.curveTangentHandles[b * 2 + 0].constraint)
                    {
                        return null;
                    }
                }

                for(int b = settings.curveEdgeHandles.Length - 1, a = 0
                    ; a < settings.curveEdgeHandles.Length
                    ; b = a, a++)
                {
                    if((settings.curvePointHandles[a].state & SelectState.Selected) == 0)
                    {
                        continue;
                    }

                    if((settings.curveEdgeHandles[a].state & SelectState.Selected) == 0 !=
                       ((settings.curveEdgeHandles[b].state & SelectState.Selected) == 0))
                    {
                        continue;
                    }

                    if(state == null)
                    {
                        state = settings.curveTangentHandles[a * 2 + 0].constraint;
                    }

                    if(state.Value != settings.curveTangentHandles[a * 2 + 0].constraint)
                    {
                        return null;
                    }

                    if(state.Value != settings.curveTangentHandles[a * 2 + 1].constraint)
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
                    if((settings.curvePointHandles[a].state & SelectState.Selected) == 0)
                    {
                        continue;
                    }

                    if(((settings.curveEdgeHandles[a].state & SelectState.Selected) == 0 &&
                           (settings.curveEdgeHandles[b].state & SelectState.Selected) == 0) ||
                       value.Value == HandleConstraints.Mirrored)
                    {
                        settings.SetPointConstraint(buildPlane, a, value.Value);
                    }
                }

                for(int b = 0, a = settings.curveEdgeHandles.Length - 1; a >= 0; b = a, a--)
                {
                    if((settings.curveEdgeHandles[a].state & SelectState.Selected) == 0)
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
                if((settings.curveEdgeHandles[a].state & SelectState.Selected) == 0)
                {
                    continue;
                }

                if(settings.curveTangentHandles[a * 2 + 1].constraint != HandleConstraints.Straight ||
                   settings.curveTangentHandles[b * 2 + 0].constraint != HandleConstraints.Straight)
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
                    if((settings.curveEdgeHandles[a].state & SelectState.Selected) == 0)
                    {
                        continue;
                    }

                    if(settings.curveTangentHandles[a * 2 + 1].constraint != HandleConstraints.Mirrored ||
                       settings.curveTangentHandles[b * 2 + 0].constraint != HandleConstraints.Mirrored)
                    {
                        state = HandleConstraints.Straight;

                        break;
                    }
                }
            }

            for(int b = 0, a = settings.curveEdgeHandles.Length - 1; a >= 0; b = a, a--)
            {
                if((settings.curveEdgeHandles[a].state & SelectState.Selected) == 0)
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
            Vector3[] curvedVertices = settings.GetCurvedVertices(buildPlane, CurveSides, out int[][] curvedEdges);

            if(curvedEdges == null)
            {
                return;
            }

            for(int i = settings.curveEdgeHandles.Length - 1; i >= 0; i--)
            {
                if((settings.curveEdgeHandles[i].state & SelectState.Selected) == 0)
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
            Vector3[] outlineVertices = settings.GetCurvedVertices(buildPlane, CurveSides, out int[][] curvedEdges);

            if(curvedEdges == null)
            {
                return false;
            }

            ShapePolygonUtility.RemoveDuplicatePoints(ref outlineVertices);

            if(_generateSmoothing)
            {
                HashSet<uint> usedSmoothingGroupIndices = SurfaceUtility.GetUsedSmoothingGroupIndices();

                for(int i = 0; i < settings.curveEdgeHandles.Length; i++)
                {
                    settings.curveEdgeHandles[i].texgen.SmoothingGroup = 0;
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
                        settings.curveEdgeHandles[i].texgen.SmoothingGroup = 0;
                    }

                    if(settings.curveEdgeHandles[i].texgen.SmoothingGroup != 0)
                    {
                        continue;
                    }

                    settings.curveEdgeHandles[i].texgen.SmoothingGroup = smoothingGroup;

                    if((i == 0 &&
                           settings.curveTangentHandles[i * 2 + 0].constraint == HandleConstraints.Mirrored) ||
                       settings.curveTangentHandles[i * 2 + 1].constraint == HandleConstraints.Mirrored)
                    {
                        int last = settings.curveEdgeHandles.Length - 1;
                        settings.curveEdgeHandles[last].texgen.SmoothingGroup = smoothingGroup;
                    }

                    if((i + 1) * 2 + 1 < settings.curveTangentHandles.Length &&
                       (settings.curveTangentHandles[(i + 1) * 2 + 0].constraint == HandleConstraints.Mirrored ||
                           settings.curveTangentHandles[(i + 1) * 2 + 1].constraint == HandleConstraints.Mirrored))
                    {
                        settings.curveEdgeHandles[i + 1].texgen.SmoothingGroup = smoothingGroup;
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
                TexGen texGen = settings.curveEdgeHandles[i].texgen;

                if(i >= curvedEdges.Length)
                {
                    continue;
                }

                if(_generateSmoothing &&
                   curvedEdges[i].Length <= 2)
                {
                    texGen.SmoothingGroup = 0;
                }

                for(int j = 0; j < curvedEdges[i].Length - 1; j++, n++)
                {
                    curvedEdgeTexgens[n] = texGen;
                }
            }

            List<ShapePolygon> newPolygons = ShapePolygonUtility.CreateCleanPolygonsFromVertices(
                outlineVertices,
                brushPosition,
                buildPlane,
                out Vector3[] projectedVertices
            );

            if(newPolygons == null)
            {
                if(registerUndo)
                {
                    EditModeManager.ShowMessage("Could not create brush from given 2D shape");
                }

                return true;
            }

            UpdatePolygons(outlineVertices, newPolygons.ToArray());

            if(editMode != EditMode.EditShape &&
               editMode != EditMode.ExtrudeShape)
            {
                return false;
            }

            if(!HaveExtrusion)
            {
                return true;
            }

            ShapePolygonUtility.FixMaterials(projectedVertices, newPolygons, curvedEdgeTexgens, curvedShapeEdges);

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
            {
                CSGPlane realPlane = _geometryPlane;

                return Vector3.Dot(realPlane.normal, buildPlane.normal) < 0;
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

            Vector3[] outlineVertices = settings.GetCurvedVertices(buildPlane, CurveSides, out int[][] curvedEdges);
            ShapePolygonUtility.RemoveDuplicatePoints(ref outlineVertices);

            settings.UpdateEdgeMaterials(buildPlane.normal);

            HashSet<uint> usedSmoothingGroupIndices = SurfaceUtility.GetUsedSmoothingGroupIndices();
            List<TexGen>  curvedEdgeTexgensList     = new List<TexGen>();

            if(curvedEdges != null)
            {
                if(_generateSmoothing)
                {
                    settings.UpdateSmoothingGroups(usedSmoothingGroupIndices);
                }

                for(int i = 0; i < settings.curveEdgeHandles.Length; i++)
                {
                    if(i >= curvedEdges.Length)
                    {
                        continue;
                    }

                    TexGen texGen = settings.curveEdgeHandles[i].texgen;

                    if(_generateSmoothing && curvedEdges[i].Length <= 2)
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

            List<ShapePolygon> newPolygons = ShapePolygonUtility.CreateCleanPolygonsFromVertices(
                outlineVertices,
                brushPosition,
                buildPlane,
                out Vector3[] projectedVertices
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

            ShapePolygonUtility.FixMaterials(projectedVertices, newPolygons, curvedEdgeTexgens, curvedShapeEdges);

            EditModeManager.ResetMessage();

            UpdatePolygons(outlineVertices, newPolygons.ToArray());
            GenerateBrushesFromPolygons(curvedShapeEdges);

            return true;
        }

        internal override bool CreateControlMeshForBrushIndex(
            CSGBrush brush, ShapePolygon polygon, Matrix4x4 localToWorld, float height
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
            Vector3 point3 = start + zdistance;

            Vector3[] points = { point0, point1, point1, start, start, point3, point3, point0 };

            PaintUtility.DrawDottedLines(points, wireframeColor);

            Vector3 endPoint = camera.transform.position;

            points = new[] { point0, point1, start, point3 };
            int   closestIndex    = -1;
            float closestDistance = float.NegativeInfinity;

            for(int i = 0; i < points.Length; i++)
            {
                float distance = (points[i] - endPoint).sqrMagnitude;

                if(distance > closestDistance)
                {
                    closestIndex    = i;
                    closestDistance = distance;
                }
            }

            int indexA1 = (closestIndex + 1) % 4;
            int indexA2 = (indexA1 + 1) % 4;
            int indexB1 = (closestIndex + 3) % 4;
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

                ArrayUtility.Add(ref realVertices, _worldPosition);
                Vector3[] curvedVertices = settings.GetCurvedVertices(buildPlane, CurveSides);
                ArrayUtility.Add(ref curvedVertices, _worldPosition);
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

                    PaintUtility.DrawDottedLine(curvedVertices[i - 1], curvedVertices[i], wireframeColor);
                }

                if(curvedVertices.Length > 3)
                {
                    PaintUtility.DrawLine(
                        curvedVertices[curvedVertices.Length - 1], curvedVertices[0], GUIConstants.OLD_LINE_SCALE
                      , wireframeColor
                    );

                    PaintUtility.DrawDottedLine(
                        curvedVertices[curvedVertices.Length - 1], curvedVertices[0], wireframeColor
                    );
                }

                bool isReversed = BuildPlaneIsReversed;

                Vector3 forward = camera.transform.forward;

                if(Event.current.button != 1)
                {
                    PaintUtility.DrawLine(
                        realVertices[realVertices.Length - 2], _worldPosition, GUIConstants.OLD_LINE_SCALE
                      , topWireframeColor
                    );

                    PaintUtility.DrawDottedLine(
                        realVertices[realVertices.Length - 2], _worldPosition, topWireframeColor
                    );

                    Vector3 origin   = (realVertices[realVertices.Length - 2] + _worldPosition) * 0.5f;
                    Vector3 delta    = realVertices[realVertices.Length - 2] - _worldPosition;
                    float   distance = delta.magnitude;

                    PaintEdgeSides(camera, realVertices[realVertices.Length - 2], _worldPosition);

                    Vector3 sideways     = Vector3.Cross(delta.normalized, forward);
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

                    textCenter2D += sideways2D * (HOVER_TEXT_DISTANCE * 2);

                    Ray textCenterRay = HandleUtility.GUIPointToWorldRay(textCenter2D);

                    Vector3 textCenter = textCenterRay.origin
                      + textCenterRay.direction * ((camera.farClipPlane + camera.nearClipPlane) * 0.5f);

                    Handles.color = Color.black;
                    Handles.DrawLine(origin, textCenter);

                    PaintUtility.DrawScreenText(
                        textCenter2D,
                        Units.ToRoundedDistanceString(Mathf.Abs(distance))
                    );

                    PaintUtility.DrawDottedLine(_worldPosition, realVertices[0], topWireframeColor);
                }

                Handles.color = wireframeColor;

                for(int i = 0; i < realVertices.Length; i++)
                {
                    float handleSize       = CSGHandleUtility.GetHandleSize(realVertices[i]);
                    float scaledHandleSize = handleSize * GUIConstants.HANDLE_SCALE;
                    PaintUtility.SquareDotCap(id, realVertices[i], rotation, scaledHandleSize);
                }
            }

            if(Event.current.button != 1)
            {
                Handles.color = Handles.selectedColor;

                float handleSize       = CSGHandleUtility.GetHandleSize(_worldPosition);
                float scaledHandleSize = handleSize * GUIConstants.HANDLE_SCALE;
                PaintUtility.SquareDotCap(id, _worldPosition, rotation, scaledHandleSize);
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
                for(int i = 0; i < settings.curvePointHandles.Length; i++)
                {
                    settings.curvePointHandles[i].id = GUIUtility.GetControlID(
                        shapeBuilderPointHash, FocusType.Passive
                    );
                }

                for(int i = 0; i < settings.curveTangentHandles.Length; i++)
                {
                    settings.curveTangentHandles[i].id = GUIUtility.GetControlID(
                        shapeBuilderTangentHash, FocusType.Passive
                    );
                }

                for(int i = 0; i < settings.curveEdgeHandles.Length; i++)
                {
                    settings.curveEdgeHandles[i].id = GUIUtility.GetControlID(shapeBuilderEdgeHash, FocusType.Passive);
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

        private bool _havePlane;

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
                if(!_hoverDefaultPlane.HasValue ||
                   settings.VertexLength == 0)
                {
                    bool forceGrid = CSGGrid.ForceGrid;
                    CSGGrid.ForceGrid  = false;
                    _hoverDefaultPlane = CSGGrid.CurrentGridPlane;
                    CSGGrid.ForceGrid  = forceGrid;
                    geometryModel      = null;
                }

                if(settings.VertexLength == 0)
                {
                    _havePlane = false;
                }

                if(editMode == EditMode.CreatePlane && !_havePlane)
                {
                    if(settings.VertexLength >= 3)
                    {
                        hoverBuildPlane = GeometryUtility.CalcPolygonPlane(settings.GetVertices());

                        if(hoverBuildPlane.normal.sqrMagnitude != 0)
                        {
                            buildPlane = hoverBuildPlane;
                            editMode   = EditMode.CreateShape;
                            _havePlane = true;
                        }
                    }

                    if(!IgnoreDepthForRayCasts(camera) && !_havePlane &&
                       EditorWindow.mouseOverWindow == sceneView &&
                       SceneQueryUtility.FindWorldIntersection(
                           camera, current.mousePosition, out LegacyBrushIntersection intersection
                       ))
                    {
                        _worldPosition   = intersection.worldIntersection;
                        hoverBuildPlane  = intersection.worldPlane;
                        buildPlane       = hoverBuildPlane;
                        _havePlane       = true;
                        vertexOnGeometry = true;

                        vertexOnBrush = intersection.brush;
                        vertexOnPlane = hoverBuildPlane;
                    }
                    else
                    {
                        hoverBuildPlane = _hoverDefaultPlane.Value;

                        Ray mouseRay = HandleUtility.GUIPointToWorldRay(current.mousePosition);
                        _worldPosition = hoverBuildPlane.RayIntersection(mouseRay);

                        if(hoverBuildPlane.normal.sqrMagnitude != 0)
                        {
                            vertexOnPlane = hoverBuildPlane;
                            buildPlane    = hoverBuildPlane;
                        }
                    }

                    ResetVisuals();

                    if(snapFunction != null)
                    {
                        _worldPosition = snapFunction(
                            camera, _worldPosition, hoverBuildPlane, ref visualSnappedEdges, out CSGBrush snappedOnBrush
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
                    _worldPosition = hoverBuildPlane.RayIntersection(mouseRay);

                    ResetVisuals();

                    if(snapFunction != null)
                    {
                        _worldPosition = snapFunction(
                            camera, _worldPosition, hoverBuildPlane, ref visualSnappedEdges, out CSGBrush snappedOnBrush
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
                    Vector3     first2D      = camera.WorldToScreenPoint(settings.GetPosition(0));
                    Vector3     current2D    = camera.WorldToScreenPoint(_worldPosition);
                    float       distance     = (current2D - first2D).magnitude;
                    const float snapDistance = 2.0f * HANDLE_ON_DISTANCE;

                    if(distance < snapDistance)
                    {
                        _worldPosition = settings.GetPosition(0);
                        _onLastPoint   = true;
                    }
                    else
                    {
                        _onLastPoint = false;
                    }
                }
                else
                {
                    _onLastPoint = false;
                }

                if(_worldPosition != _prevWorldPosition)
                {
                    _prevWorldPosition = _worldPosition;

                    if(current.type != EventType.Repaint)
                    {
                        CSGEditorGUIUtility.RepaintAll();
                    }
                }

                visualSnappedGrid  = CSGGrid.FindAllGridEdgesThatTouchPoint(camera, _worldPosition);
                visualSnappedBrush = vertexOnBrush;
            }

            CSGGrid.SetForcedGrid(camera, hoverBuildPlane);

            if(!SceneDragToolManager.IsDraggingObjectInScene &&
               current.type == EventType.Repaint)
            {
                if(settings.realEdge != null)
                {
                    Handles.color = ColorSettings.WireframeOutline;
                    PaintUtility.DrawDottedLines(settings.realEdge, Handles.secondaryColor);
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
                        if(!float.IsNaN(_worldPosition.x) && !float.IsInfinity(_worldPosition.x) &&
                           !float.IsNaN(_worldPosition.y) && !float.IsInfinity(_worldPosition.y) &&
                           !float.IsNaN(_worldPosition.z) && !float.IsInfinity(_worldPosition.z))
                        {
                            if(hoverBuildPlane.normal.sqrMagnitude != 0)
                            {
                                buildPlane = hoverBuildPlane;
                            }

                            CalculateWorldSpaceTangents(camera);

                            if(!settings.HaveVertices)
                            {
                                planeOnGeometry = !pointOnEdge && vertexOnGeometry;
                                _geometryPlane  = buildPlane;
                            }
                            else
                            {
                                if(!pointOnEdge && vertexOnGeometry)
                                {
                                    planeOnGeometry = true;
                                }

                                if(vertexOnGeometry)
                                {
                                    CSGPlane plane    = _hoverDefaultPlane.Value;
                                    float    distance = plane.Distance(_worldPosition);
                                    plane.d            += distance;
                                    _hoverDefaultPlane =  plane;

                                    for(int i = 0; i < settings.VertexLength; i++)
                                    {
                                        if(!settings.onGeometryVertices[i])
                                        {
                                            Vector3 guipoint  = camera.WorldToScreenPoint(settings.GetPosition(i));
                                            Ray     cameraRay = camera.ScreenPointToRay(guipoint);

                                            settings.SetPosition(i, plane.RayIntersection(cameraRay));
                                            settings.onGeometryVertices[i] = true;
                                        }
                                    }
                                }
                            }

                            settings.AddVertex(_worldPosition, vertexOnBrush, vertexOnPlane, vertexOnGeometry);
                            CSGEditorGUIUtility.RepaintAll();
                        }

                        CSGEditorGUIUtility.RepaintAll();
                    }

                    return;
                }
                case EventType.MouseMove:
                {
                    _clickCount = 0;

                    break;
                }
                case EventType.MouseDrag:
                {
                    _clickCount = 0;

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
                        if(_onLastPoint)
                        {
                            if(settings.VertexLength < 3)
                            {
                                Cancel();
                            }
                            else
                            {
                                editMode = EditMode.CreateShape;
                                HotKeyReleased();
                            }
                        }
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
                SelectState vertexState = curvePointHandles[i].state;
                int         id          = curvePointHandles[i].id;

                if(vertexState != renderState)
                {
                    continue;
                }

                float handleSize = CSGHandleUtility.GetHandleSize(vertices[i]);

                if(state != null)
                {
                    if(state[i] == HandleConstraints.Mirrored)
                    {
                        Handles.color = ColorSettings.AlignedCurveStateColor[colorState];

                        PaintUtility.CircleDotCap(
                            vertices[i], handleSize * GUIConstants.HANDLE_SCALE * 1.25f
                        );
                    }
                    else
                    {
                        Handles.color = ColorSettings.BrokenCurveStateColor[colorState];

                        PaintUtility.DiamondDotCap(
                            vertices[i], handleSize * GUIConstants.HANDLE_SCALE * 1.25f
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

                float handleSize = CSGHandleUtility.GetHandleSize(vertices[i]);

                if(state != null)
                {
                    if(state[i] == HandleConstraints.Mirrored)
                    {
                        Handles.color = ColorSettings.AlignedCurveStateColor[colorState];

                        PaintUtility.CircleDotCap(
                            vertices[i], handleSize * GUIConstants.HANDLE_SCALE * 1.25f
                        );
                    }
                    else
                    {
                        Handles.color = ColorSettings.BrokenCurveStateColor[colorState];

                        PaintUtility.DiamondDotCap(
                            vertices[i], handleSize * GUIConstants.HANDLE_SCALE * 1.25f
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

            if(Event.current.type == EventType.Layout || Event.current.type == EventType.Repaint)
            {
                curvedVertices = settings.GetCurvedVertices(buildPlane, CurveSides, out curvedEdges);
                ShapePolygonUtility.RemoveDuplicatePoints(ref curvedVertices);
            }

            if(Event.current.type == EventType.MouseDown || Event.current.type == EventType.MouseDrag)
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
                    int vertexID = settings.curvePointHandles[i].id;

                    if(vertexID == nearestControl)
                    {
                        foundVertexIndex = i;

                        break;
                    }

                    int edgeID = settings.curveEdgeHandles[i].id;

                    if(edgeID == nearestControl)
                    {
                        foundEdgeIndex = i;

                        break;
                    }
                }

                for(int i = 0; i < settings.curveTangentHandles.Length; i++)
                {
                    int tangentID = settings.curveTangentHandles[i].id;

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
                    CSGEditorGUIUtility.RepaintAll();
                    settings.prevHoverVertex  = foundVertexIndex;
                    settings.prevHoverTangent = foundTangentIndex;
                    settings.prevHoverEdge    = foundEdgeIndex;
                }
            }

            // render edges
            if(!SceneDragToolManager.IsDraggingObjectInScene && settings.curveEdgeHandles != null &&
               settings.curveEdgeHandles.Length > 0 &&
               Event.current.GetTypeForControl(settings.curveEdgeHandles[0].id) == EventType.Repaint)
            {
                Color     temp   = Handles.color;
                Matrix4x4 matrix = Handles.matrix;

                Handles.matrix = MathConstants.identityMatrix;

                if(settings.realEdge != null)
                {
                    Handles.color = ColorSettings.PointInnerStateColor[0];
                    PaintUtility.DrawDottedLines(settings.realEdge, ColorSettings.PointInnerStateColor[0]);
                }

                if(visualSnappedEdges != null)
                {
                    PaintUtility.DrawLines(
                        visualSnappedEdges.ToArray(), GUIConstants.OLD_THICK_LINE_SCALE, ColorSettings.SnappedEdges
                    );
                }

                if(visualSnappedGrid != null)
                {
                    Matrix4x4 origMatrix = Handles.matrix;
                    Handles.matrix = MathConstants.identityMatrix;
                    PaintUtility.DrawDottedLines(visualSnappedGrid.ToArray(), ColorSettings.SnappedEdges);
                    Handles.matrix = origMatrix;
                }

                if(visualSnappedBrush)
                {
                    if(visualSnappedBrush.compareTransformation != null &&
                       visualSnappedBrush.ChildData != null &&
                       visualSnappedBrush.ChildData.ModelTransform)
                    {
                        Matrix4x4 brushTransformation = visualSnappedBrush.compareTransformation.localToWorldMatrix;

                        CSGRenderer.DrawOutlines(
                            visualSnappedBrush.brushNodeID, brushTransformation, ColorSettings.SelectedOutlines
                          , ColorSettings.SelectedOutlines, ColorSettings.SelectedOutlines
                          , ColorSettings.SelectedOutlines
                        );
                    }
                }

                //int   hoverIndex     = -1;
                Color wireframeColor = ColorSettings.PointInnerStateColor[1];

                for(int i = 0; i < settings.VertexLength; i++)
                {
                    SelectState state = settings.curveEdgeHandles[i].state;

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
                        PaintUtility.DrawDottedLine(vertex1, vertex2, wireframeColor);
                    }
                }

                /*if(hoverIndex > -1)
                {
                    Vector3 forward = camera.transform.forward;

                    for(int i = 0; i < settings.VertexLength; i++)
                    {
                        int prevIndex = (i + settings.VertexLength - 1) % settings.VertexLength;
                        int currIndex = i;
                        int nextIndex = (i + 1) % settings.VertexLength;

                        if(_haveDragged)
                        {
                            if(prevIndex != hoverIndex &&
                               currIndex != hoverIndex &&
                               nextIndex != hoverIndex)
                            {
                                continue;
                            }
                        }
                        else
                        {
                            if(currIndex != hoverIndex)
                            {
                                continue;
                            }
                        }

                        int[]   indices = curvedEdges[currIndex];
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

                        Vector3 vertex1 = settings.GetPosition(currIndex);
                        Vector3 vertex2 = settings.GetPosition(nextIndex);

                        Vector3 delta    = vertex1 - vertex2;
                        Vector3 sideways = Vector3.Cross(delta.normalized, forward);
                        float   distance = delta.magnitude;

                        Vector2 textCenter2D = HandleUtility.WorldToGUIPoint(origin);

                        textCenter2D +=
                            (HandleUtility.WorldToGUIPoint(origin + sideways * 10) - textCenter2D).normalized
                          * (HOVER_TEXT_DISTANCE * 2);

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
                }*/

                Handles.matrix = matrix;
                Handles.color  = temp;
            }

            // render vertices
            if(!SceneDragToolManager.IsDraggingObjectInScene &&
               settings.curveEdgeHandles.Length > 0 &&
               Event.current.GetTypeForControl(settings.curvePointHandles[0].id) == EventType.Repaint)
            {
                Color     temp   = Handles.color;
                Matrix4x4 matrix = Handles.matrix;

                Handles.matrix = MathConstants.identityMatrix;
                Quaternion rotation = camera.transform.rotation;

                if(settings.realEdge != null)
                {
                    Handles.color = ColorSettings.PointInnerStateColor[0];
                    PaintUtility.DrawDottedLines(settings.realEdge, ColorSettings.PointInnerStateColor[0]);
                }

                if(visualSnappedEdges != null)
                {
                    PaintUtility.DrawLines(
                        visualSnappedEdges.ToArray(), GUIConstants.OLD_THICK_LINE_SCALE, ColorSettings.SnappedEdges
                    );
                }

                if(visualSnappedGrid != null)
                {
                    Matrix4x4 origMatrix = Handles.matrix;
                    Handles.matrix = MathConstants.identityMatrix;
                    PaintUtility.DrawDottedLines(visualSnappedGrid.ToArray(), ColorSettings.SnappedEdges);
                    Handles.matrix = origMatrix;
                }

                if(visualSnappedBrush)
                {
                    if(visualSnappedBrush.compareTransformation != null &&
                       visualSnappedBrush.ChildData != null &&
                       visualSnappedBrush.ChildData.ModelTransform)
                    {
                        Matrix4x4 brushTransformation = visualSnappedBrush.compareTransformation.localToWorldMatrix;

                        CSGRenderer.DrawOutlines(
                            visualSnappedBrush.brushNodeID, brushTransformation, ColorSettings.SelectedOutlines
                          , ColorSettings.SelectedOutlines, ColorSettings.SelectedOutlines
                          , ColorSettings.SelectedOutlines
                        );
                    }
                }

                int nearControl = HandleUtility.nearestControl;
                int hotControl  = GUIUtility.hotControl;
                int hoverIndex  = -1;

                for(int i = 0, j = 0; i < settings.VertexLength; i++, j += 2)
                {
                    if(settings.curvePointHandles[i].id == hotControl ||
                       settings.curveTangentHandles[j + 0].id == hotControl ||
                       settings.curveTangentHandles[j + 1].id == hotControl ||
                       (hotControl == 0 &&
                           (settings.curvePointHandles[i].id == nearControl ||
                               settings.curveTangentHandles[j + 0].id == nearControl ||
                               settings.curveTangentHandles[j + 1].id == nearControl
                           )))
                    {
                        hoverIndex = i;
                    }
                }

                if(settings.curve.tangents.Length == settings.curve.points.Length * 2 &&
                   settings.curveTangentHandles.Length == settings.curve.tangents.Length)
                {
                    int curvedEdgeCount = 0;

                    for(int i = 0; i < settings.curveTangentHandles.Length; i += 2)
                    {
                        if(settings.curveTangentHandles[i + 0].constraint != HandleConstraints.Straight)
                        {
                            curvedEdgeCount++;
                        }

                        if(settings.curveTangentHandles[i + 1].constraint != HandleConstraints.Straight)
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
                            settings.curveTangentHandles[j + 0].constraint == HandleConstraints.Mirrored &&
                            settings.curveTangentHandles[j + 1].constraint == HandleConstraints.Mirrored
                                ? HandleConstraints.Mirrored
                                : HandleConstraints.Broken;

                        if(settings.curveTangentHandles[j + 0].constraint != HandleConstraints.Straight)
                        {
                            settings.realTangent[counter] =
                                settings.curve.points[i] - settings.curve.tangents[j + 0].tangent;

                            settings.realTangentIDs[counter]       = settings.curveTangentHandles[j + 0].id;
                            settings.realTangentSelection[counter] = settings.curveTangentHandles[j + 0].state;
                            settings.realTangentState[counter]     = state;
                            PaintUtility.DrawLine(settings.curve.points[i], settings.realTangent[counter], Color.black);

                            PaintUtility.DrawDottedLine(
                                settings.curve.points[i], settings.realTangent[counter], Color.black
                            );

                            counter++;
                        }

                        if(settings.curveTangentHandles[j + 1].constraint != HandleConstraints.Straight)
                        {
                            settings.realTangent[counter] =
                                settings.curve.points[i] - settings.curve.tangents[j + 1].tangent;

                            settings.realTangentIDs[counter]       = settings.curveTangentHandles[j + 1].id;
                            settings.realTangentSelection[counter] = settings.curveTangentHandles[j + 1].state;
                            settings.realTangentState[counter]     = state;
                            PaintUtility.DrawLine(settings.curve.points[i], settings.realTangent[counter], Color.black);

                            PaintUtility.DrawDottedLine(
                                settings.curve.points[i], settings.realTangent[counter], Color.black
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

                if(hoverIndex > -1)
                {
                    Vector3 forward = camera.transform.forward;

                    for(int i = 0; i < settings.VertexLength; i++)
                    {
                        int currIndex = i;
                        int nextIndex = (i + 1) % settings.VertexLength;

                        if(currIndex != hoverIndex &&
                           nextIndex != hoverIndex)
                        {
                            continue;
                        }

                        if(curvedEdges == null || currIndex >= curvedEdges.Length)
                        {
                            continue;
                        }

                        int[] indices = curvedEdges[currIndex];

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

                        Vector3 vertex1 = settings.GetPosition(currIndex);
                        Vector3 vertex2 = settings.GetPosition(nextIndex);

                        Vector3 delta    = vertex1 - vertex2;
                        Vector3 sideways = Vector3.Cross(delta.normalized, forward);
                        float   distance = delta.magnitude;

                        Vector2 textCenter2D = HandleUtility.WorldToGUIPoint(origin);

                        textCenter2D +=
                            (HandleUtility.WorldToGUIPoint(origin + sideways * 10) - textCenter2D).normalized
                          * (HOVER_TEXT_DISTANCE * 2);

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

                Handles.matrix = matrix;
                Handles.color  = temp;
            }

            switch(Event.current.type)
            {
                case EventType.ValidateCommand:
                case EventType.KeyDown:
                {
                    if(Keys.insertPoint.IsKeyPressed())
                    {
                        Event.current.Use();
                    }

                    break;
                }
                case EventType.KeyUp:
                {
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
                    int currIndex = i;

                    if(curvedEdges == null || currIndex >= curvedEdges.Length)
                    {
                        continue;
                    }

                    int[] indices = curvedEdges[currIndex];

                    if(indices.Length < 3)
                    {
                        continue;
                    }

                    for(int j = 1; j < indices.Length - 1; j++)
                    {
                        float handleSize = CSGHandleUtility.GetHandleSize(curvedVertices[indices[j]]) * 0.5f;
                        PaintUtility.SquareDotCap(-1, curvedVertices[indices[j]], Quaternion.identity, handleSize);
                    }
                }
            }

            // edges
            for(int i = 0; i < settings.VertexLength; i++)
            {
                int id = settings.curveEdgeHandles[i].id;

                switch(Event.current.GetTypeForControl(id))
                {
                    case EventType.Layout:
                    {
                        if(camera == null || (Tools.viewTool != ViewTool.None && Tools.viewTool != ViewTool.Pan))
                        {
                            break;
                        }

                        CSGPlane cameraPlane = CSGHandleUtility.GetNearPlane(camera);

                        Matrix4x4 origMatrix = Handles.matrix;
                        Handles.matrix = MathConstants.identityMatrix;

                        if(curvedEdges != null && curvedEdges.Length > i)
                        {
                            int[] indices = curvedEdges[i];

                            if(indices != null && indices.Length > 0)
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

                                    float lineDistance = CameraUtility.DistanceToLine(
                                        cameraPlane, mousePoint, vertex1, vertex2
                                    );

                                    if(lineDistance < distance)
                                    {
                                        distance = lineDistance;
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
                           (Tools.viewTool == ViewTool.None || Tools.viewTool == ViewTool.Pan))
                        {
                            GUIUtility.hotControl             = id;
                            GUIUtility.keyboardControl        = id;
                            EditorGUIUtility.editingTextField = false;
                            EditorGUIUtility.SetWantsMouseJumping(1);
                            Event.current.Use();
                            _clickMousePosition = Event.current.mousePosition;
                            _prevDragDifference = MathConstants.zeroVector3;
                            _haveDragged        = false;
                            settings.realEdge   = null;
                            settings.CopyBackupVertices();
                        }

                        break;
                    }
                    case EventType.MouseMove:
                    {
                        _clickCount = 0;

                        break;
                    }
                    case EventType.MouseDrag:
                    {
                        _clickCount = 0;

                        if(GUIUtility.hotControl == id && Event.current.button == 0 &&
                           (Tools.viewTool == ViewTool.None || Tools.viewTool == ViewTool.Pan))
                        {
                            if(!_haveDragged)
                            {
                                float diff = (Event.current.mousePosition - _clickMousePosition).magnitude;

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
                                buildPlane.RayIntersection(HandleUtility.GUIPointToWorldRay(_clickMousePosition));

                            Vector3 intersectionPoint = buildPlane.RayIntersection(
                                HandleUtility.GUIPointToWorldRay(Event.current.mousePosition)
                            );

                            Vector3 difference = intersectionPoint - startDragPoint;

                            int     k            = (i + settings.VertexLength - 1) % settings.VertexLength;
                            int     j            = (i + 1) % settings.VertexLength;
                            int     l            = (i + 2) % settings.VertexLength;
                            Vector3 movedVertex1 = settings.backupCurve.points[i] + difference;
                            Vector3 movedVertex2 = settings.backupCurve.points[j] + difference;

                            settings.realEdge = new[]
                            {
                                settings.backupCurve.points[k], movedVertex1, movedVertex1, movedVertex2
                              , movedVertex2, settings.backupCurve.points[l]
                            };

                            if(snapFunction != null)
                            {
                                ResetVisuals();

                                Vector3 vertexDifference1 = snapFunction(
                                        camera, movedVertex1, buildPlane, ref visualSnappedEdges, out CSGBrush _
                                      , generatedBrushes
                                    )
                                    //point_moved1
                                  - settings.backupCurve.points[i];

                                Vector3 vertexDifference2 = snapFunction(
                                        camera, movedVertex2, buildPlane, ref visualSnappedEdges, out CSGBrush _
                                      , generatedBrushes
                                    )
                                    //point_moved2
                                  - settings.backupCurve.points[j];

                                difference = (vertexDifference1 - difference).sqrMagnitude
                                  < (vertexDifference2 - difference).sqrMagnitude
                                        ? vertexDifference1
                                        : vertexDifference2;

                                float snapDistance = float.PositiveInfinity;

                                for(int p = 0; p < settings.backupCurve.points.Length; p++)
                                {
                                    if(p == i)
                                    {
                                        continue;
                                    }

                                    float handleSize = CSGHandleUtility.GetHandleSize(settings.backupCurve.points[p]);
                                    float scaledHandleSize = handleSize * GUIConstants.HANDLE_SCALE * HANDLE_EXTENSION;

                                    float distance = GeometryUtility.DistancePointToCircle(
                                        sceneView, movedVertex1, settings.backupCurve.points[p], scaledHandleSize
                                    );

                                    if(distance < HANDLE_ON_DISTANCE)
                                    {
                                        if(distance < snapDistance)
                                        {
                                            snapDistance = distance;

                                            difference = settings.backupCurve.points[p]
                                              - settings.backupCurve.points[i];
                                        }
                                    }

                                    distance = GeometryUtility.DistancePointToCircle(
                                        sceneView, movedVertex2, settings.backupCurve.points[p], scaledHandleSize
                                    );

                                    if(distance < HANDLE_ON_DISTANCE)
                                    {
                                        if(distance < snapDistance)
                                        {
                                            snapDistance = distance;

                                            difference = settings.backupCurve.points[p]
                                              - settings.backupCurve.points[j];
                                        }
                                    }
                                }
                            }

                            if(_prevDragDifference != difference)
                            {
                                for(int p = 0; p < settings.backupCurve.points.Length; p++)
                                {
                                    if(settings.IsVertexSelected(p))
                                    {
                                        settings.SetPosition(p, difference + settings.backupCurve.points[p]);
                                    }
                                }

                                _prevDragDifference = difference;
                                CSGEditorGUIUtility.RepaintAll();
                            }

                            if(editMode == EditMode.ExtrudeShape)
                            {
                                StartExtrudeMode(camera, false);
                            }

                            UpdateBaseShape(false);

                            GUI.changed = true;
                            Event.current.Use();
                            _haveDragged = true;
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

                            if(!_haveDragged)
                            {
                                if(Event.current.modifiers == EventModifiers.None)
                                {
                                    _clickCount++;

                                    if(_clickCount > 1)
                                    {
                                        if((_clickCount & 2) == 2)
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

                            if(_haveDragged && settings.VertexLength < 3)
                            {
                                Cancel();
                            }

                            _haveDragged = false;
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
                    int id = settings.curveTangentHandles[i].id;

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
                            float   handleSize       = CSGHandleUtility.GetHandleSize(vertex);
                            float   scaledHandleSize = handleSize * GUIConstants.HANDLE_SCALE * HANDLE_EXTENSION;
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
                               (Tools.viewTool == ViewTool.None || Tools.viewTool == ViewTool.Pan))
                            {
                                GUIUtility.hotControl             = id;
                                GUIUtility.keyboardControl        = id;
                                EditorGUIUtility.editingTextField = false;
                                EditorGUIUtility.SetWantsMouseJumping(1);
                                Event.current.Use();
                                _clickMousePosition = Event.current.mousePosition;
                                _prevDragDifference = MathConstants.zeroVector3;
                                _haveDragged        = false;
                                settings.CopyBackupVertices();
                            }

                            break;
                        }
                        case EventType.MouseMove:
                        {
                            _clickCount = 0;

                            break;
                        }
                        case EventType.MouseDrag:
                        {
                            _clickCount = 0;

                            if(GUIUtility.hotControl == id && Event.current.button == 0 &&
                               (Tools.viewTool == ViewTool.None || Tools.viewTool == ViewTool.Pan))
                            {
                                if(!_haveDragged)
                                {
                                    float diff = (Event.current.mousePosition - _clickMousePosition).magnitude;

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
                                _worldPosition = buildPlane.Project(alignedPlane.RayIntersection(mouseRay));

                                if(float.IsInfinity(_worldPosition.x) || float.IsNaN(_worldPosition.x) ||
                                   float.IsInfinity(_worldPosition.y) || float.IsNaN(_worldPosition.y) ||
                                   float.IsInfinity(_worldPosition.z) || float.IsNaN(_worldPosition.z))
                                {
                                    _worldPosition = vertex;
                                }

                                Vector3 difference = _worldPosition - vertex;

                                if(snapFunction != null)
                                {
                                    difference = snapFunction(
                                        camera, _worldPosition, buildPlane, ref visualSnappedEdges, out CSGBrush _
                                      , generatedBrushes
                                    ) - vertex;
                                }

                                visualSnappedGrid = CSGGrid.FindAllGridEdgesThatTouchPoint(camera, difference + vertex);

                                ResetVisuals();

                                if(_prevDragDifference != difference)
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

                                    _prevDragDifference = difference;
                                    CSGEditorGUIUtility.RepaintAll();
                                }

                                if(editMode == EditMode.ExtrudeShape)
                                {
                                    StartExtrudeMode(camera, false);
                                }

                                UpdateBaseShape(false);

                                GUI.changed = true;
                                Event.current.Use();
                                _haveDragged = true;
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

                                if(!_haveDragged)
                                {
                                    SelectionType selectionType = SelectionUtility.GetEventSelectionType();

                                    if(selectionType == SelectionType.Replace)
                                    {
                                        settings.DeselectAll();
                                    }

                                    settings.SelectTangent(i, selectionType);
                                }
                            }

                            break;
                        }
                    }
                }
            }

            // vertices
            for(int i = 0; i < settings.VertexLength; i++)
            {
                int id = settings.curvePointHandles[i].id;

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
                        float handleSize       = CSGHandleUtility.GetHandleSize(settings.GetPosition(i));
                        float scaledHandleSize = handleSize * GUIConstants.HANDLE_SCALE * HANDLE_EXTENSION;

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
                           (Tools.viewTool == ViewTool.None || Tools.viewTool == ViewTool.Pan))
                        {
                            GUIUtility.hotControl             = id;
                            GUIUtility.keyboardControl        = id;
                            EditorGUIUtility.editingTextField = false;
                            EditorGUIUtility.SetWantsMouseJumping(1);
                            Event.current.Use();
                            _clickMousePosition = Event.current.mousePosition;
                            _prevDragDifference = MathConstants.zeroVector3;
                            _haveDragged        = false;
                            settings.CopyBackupVertices();
                        }

                        break;
                    }
                    case EventType.MouseMove:
                    {
                        _clickCount = 0;

                        break;
                    }
                    case EventType.MouseDrag:
                    {
                        _clickCount = 0;

                        if(GUIUtility.hotControl == id && Event.current.button == 0 &&
                           (Tools.viewTool == ViewTool.None || Tools.viewTool == ViewTool.Pan))
                        {
                            if(!_haveDragged)
                            {
                                float diff = (Event.current.mousePosition - _clickMousePosition).magnitude;

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

                            if(snapFunction != null)
                            {
                                difference = snapFunction(
                                    camera, worldPosition, buildPlane, ref visualSnappedEdges, out CSGBrush _
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

                                    float handleSize = CSGHandleUtility.GetHandleSize(settings.backupCurve.points[p]);
                                    float scaledHandleSize = handleSize * GUIConstants.HANDLE_SCALE * HANDLE_EXTENSION;

                                    float distance = HandleUtility.DistanceToCircle(
                                        settings.backupCurve.points[p], scaledHandleSize
                                    );

                                    if(distance < HANDLE_ON_DISTANCE)
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

                            if(_prevDragDifference != difference)
                            {
                                for(int p = 0; p < settings.backupCurve.points.Length; p++)
                                {
                                    if(settings.IsVertexSelected(p))
                                    {
                                        settings.SetPosition(p, difference + settings.backupCurve.points[p]);
                                    }
                                }

                                _prevDragDifference = difference;
                                CSGEditorGUIUtility.RepaintAll();
                            }

                            if(editMode == EditMode.ExtrudeShape)
                            {
                                StartExtrudeMode(camera, false);
                            }

                            UpdateBaseShape(false);

                            GUI.changed = true;
                            Event.current.Use();
                            _haveDragged = true;
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

                        if(!_haveDragged)
                        {
                            if(Event.current.modifiers == EventModifiers.None)
                            {
                                _clickCount++;

                                if(_clickCount > 1)
                                {
                                    if((_clickCount & 2) == 2)
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

                        if(_haveDragged && settings.VertexLength < 3)
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
