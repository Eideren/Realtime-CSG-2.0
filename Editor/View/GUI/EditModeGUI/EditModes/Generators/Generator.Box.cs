using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using InternalRealtimeCSG;
using RealtimeCSG.Legacy;
using RealtimeCSG.Components;

namespace RealtimeCSG
{
    // TODO:
    //	use CSGBounds handle to resize box in edit mode (can move all sides, not just 3 points)
    internal sealed class GeneratorBox : GeneratorExtrudedBase, IGenerator
    {
        [NonSerialized] private Vector3 worldPosition;
        [NonSerialized] private Vector3 prevWorldPosition;
        [NonSerialized] private bool onLastPoint = false;
        [NonSerialized] private CSGPlane geometryPlane = new CSGPlane(0, 1, 0, 0);
        [NonSerialized] private CSGPlane? hoverDefaultPlane;

        [NonSerialized] private CSGPlane[] firstSnappedPlanes = null;
        [NonSerialized] private Vector3[] firstSnappedEdges = null;
        [NonSerialized] private CSGBrush firstSnappedBrush = null;

        [SerializeField] private GeneratorBoxSettings settings = new GeneratorBoxSettings();

        protected override IShapeSettings ShapeSettings
        {
            get => settings;
        }

        public float Width
        {
            get
            {
                if(editMode == EditMode.CreatePlane ||
                   settings.vertices.Length == 0)
                {
                    return 0;
                }

                Vector3 corner1         = settings.vertices[0];
                Vector3 corner2         = settings.vertices.Length >= 2 ? settings.vertices[1] : worldPosition;
                Vector3 delta           = corner2 - corner1;
                Vector3 projected_width = Vector3.Project(delta, gridTangent);
                float   width           = projected_width.magnitude;

                if(float.IsInfinity(width) || float.IsNaN(width))
                {
                    width = 1.0f;
                }

                width *= Mathf.Sign(Vector3.Dot(projected_width, gridTangent));

                return GeometryUtility.CleanLength(width);
            }
            set
            {
                if(editMode == EditMode.CreatePlane ||
                   settings.vertices.Length == 0)
                {
                    return;
                }

                Vector3 corner1          = settings.vertices[0];
                Vector3 corner2          = settings.vertices.Length >= 2 ? settings.vertices[1] : worldPosition;
                Vector3 delta            = corner2 - corner1;
                Vector3 projected_length = Vector3.Project(delta, gridBinormal);
                float   width            = projected_length.magnitude;

                if(float.IsInfinity(width) || float.IsNaN(width))
                {
                    width = 1.0f;
                }

                width   *= Mathf.Sign(Vector3.Dot(projected_length, gridBinormal));
                corner2 =  corner1 + width * gridBinormal + value * gridTangent;

                if(settings.vertices.Length == 1)
                {
                    settings.AddPoint(corner2);
                }
                else
                {
                    settings.vertices[1] = corner2;
                }

                if(editMode == EditMode.ExtrudeShape)
                {
                    Undo.RecordObject(this, "Modified Width");
                    CenterExtrusionPoints(buildPlane);
                    UpdateBaseShape();
                }
            }
        }

        public float Length
        {
            get
            {
                if(editMode == EditMode.CreatePlane ||
                   settings.vertices.Length == 0)
                {
                    return 0;
                }

                Vector3 corner1          = settings.vertices[0];
                Vector3 corner2          = settings.vertices.Length >= 2 ? settings.vertices[1] : worldPosition;
                Vector3 delta            = corner2 - corner1;
                Vector3 projected_length = Vector3.Project(delta, gridBinormal);
                float   length           = projected_length.magnitude;

                if(float.IsInfinity(length) || float.IsNaN(length))
                {
                    length = 1.0f;
                }

                length *= Mathf.Sign(Vector3.Dot(projected_length, gridBinormal));

                return GeometryUtility.CleanLength(length);
            }
            set
            {
                if(editMode == EditMode.CreatePlane ||
                   settings.vertices.Length == 0)
                {
                    return;
                }

                Vector3 corner1         = settings.vertices[0];
                Vector3 corner2         = settings.vertices.Length >= 2 ? settings.vertices[1] : worldPosition;
                Vector3 delta           = corner2 - corner1;
                Vector3 projected_width = Vector3.Project(delta, gridTangent);
                float   length          = projected_width.magnitude;

                if(float.IsInfinity(length) || float.IsNaN(length))
                {
                    length = 1.0f;
                }

                length  *= Mathf.Sign(Vector3.Dot(projected_width, gridTangent));
                corner2 =  corner1 + value * gridBinormal + length * gridTangent;

                if(settings.vertices.Length == 1)
                {
                    settings.AddPoint(corner2);
                }
                else
                {
                    settings.vertices[1] = corner2;
                }

                if(editMode == EditMode.ExtrudeShape)
                {
                    Undo.RecordObject(this, "Modified Length");
                    CenterExtrusionPoints(buildPlane);
                    UpdateBaseShape();
                }
            }
        }

        public override void PerformDeselectAll()
        {
            Cancel();
        }

        public override void PerformDelete()
        {
            Cancel();
        }

        public override void Reset()
        {
            settings.Reset();
            base.Reset();
            onLastPoint        = false;
            hoverDefaultPlane  = null;
            firstSnappedPlanes = null;
            firstSnappedEdges  = null;
            firstSnappedBrush  = null;
        }

        public bool HotKeyReleased()
        {
            Camera camera = Camera.current;

            ResetVisuals();

            switch(editMode)
            {
                default:
                {
                    return true;
                }
                case EditMode.CreateShape:
                {
                    if(settings.vertices.Length == 1)
                    {
                        settings.AddPoint(worldPosition);
                    }

                    if((settings.vertices[0] - settings.vertices[1]).magnitude <= MathConstants.EqualityEpsilon)
                    {
                        Cancel();

                        return false;
                    }

                    StartEditMode(camera);

                    return true;
                }
                case EditMode.CreatePlane:
                {
                    Cancel();

                    return false;
                }
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
            List<ShapePolygon> newPolygons = null;

            Vector3[] outlineVertices = settings.GetVertices(
                buildPlane, worldPosition, gridTangent, gridBinormal, out shapeIsValid
            );

            if(shapeIsValid)
            {
                newPolygons = ShapePolygonUtility.CreateCleanPolygonsFromVertices(
                    outlineVertices, brushPosition, buildPlane
                );

                shapeIsValid = newPolygons != null;
            }
            //CSGBrushEditorManager.ResetMessage();

            if(shapeIsValid && newPolygons != null)
            {
                UpdatePolygons(outlineVertices, newPolygons.ToArray());
            }

            if(editMode != EditMode.EditShape &&
               editMode != EditMode.ExtrudeShape)
            {
                return false;
            }

            if(!shapeIsValid || !HaveExtrusion)
            {
                return true;
            }

            GenerateBrushesFromPolygons(inGridSpace: false);
            UpdateExtrudedShape();

            return true;
        }

        private bool BuildPlaneIsReversed
        {
            get
            {
                CSGPlane realPlane = geometryPlane;
                //if (!planeOnGeometry)
                //	realPlane = RealtimeCSG.Grid.CurrentGridPlane;

                if(Vector3.Dot(realPlane.normal, buildPlane.normal) < 0)
                {
                    return true;
                }

                return false;
            }
        }

        internal override bool StartExtrudeMode(Camera camera, bool showErrorMessage = true)
        {
            // reverse buildPlane if it's different
            if(BuildPlaneIsReversed)
            {
                buildPlane = buildPlane.Negated();
                CalculateWorldSpaceTangents(camera);
            }

            Vector3[] outlineVertices = settings.GetVertices(
                buildPlane, worldPosition, gridTangent, gridBinormal, out shapeIsValid
            );

            if(!shapeIsValid)
            {
                ClearPolygons();
                //if (showErrorMessage)
                //	CSGBrushEditorManager.ShowMessage("Could not create brush from given 2D shape");
                HideGenerateBrushes();

                return false;
            }

            List<ShapePolygon> newPolygons =
                ShapePolygonUtility.CreateCleanPolygonsFromVertices(outlineVertices, brushPosition, buildPlane);

            if(newPolygons == null)
            {
                shapeIsValid = false;
                ClearPolygons();
                //if (showErrorMessage)
                //	CSGBrushEditorManager.ShowMessage("Could not create brush from given 2D shape");
                HideGenerateBrushes();

                return false;
            }

            EditModeManager.ResetMessage();
            shapeIsValid = true;

            UpdatePolygons(outlineVertices, newPolygons.ToArray());
            GenerateBrushesFromPolygons(inGridSpace: false);

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
                   new TexGen(),
                   false,
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

        private void PaintSquare()
        {
            //var wireframeColor = ColorSettings.BoundsOutlines;

            if(settings.vertices.Length < 1)
            {
                return;
            }

            Vector3 corner1 = settings.vertices[0];
            Vector3 corner2 = settings.vertices.Length >= 2 ? settings.vertices[1] : worldPosition;
            //Vector3 center				= (corner2 + corner1) * 0.5f;
            Vector3 delta            = corner2 - corner1;
            Vector3 projected_width  = Vector3.Project(delta, gridTangent);
            Vector3 projected_length = Vector3.Project(delta, gridBinormal);

            Vector3 point0 = corner1 + projected_width + projected_length;
            Vector3 point1 = corner1 + projected_width;
            Vector3 point2 = corner1;
            Vector3 point3 = corner1 + projected_length;

            Vector3[] points = new Vector3[] { point0, point1, point1, point2, point2, point3, point3, point0 };

            Color color = ColorSettings.ShapeDrawingFill;
            PaintUtility.DrawPolygon(MathConstants.identityMatrix, points, color);
        }

        private void PaintBounds(Camera camera)
        {
            if(HaveHeight)
            {
                AABB localBounds = GetShapeBounds(toGridQuaternion);

                Vector3[] volume = new Vector3[8];
                BoundsUtilities.GetBoundsCornerPoints(localBounds, volume);

                PaintUtility.RenderBoundsSizes(
                    toGridQuaternion, fromGridQuaternion, camera, volume, Color.white, Color.white, Color.white, true
                  , true, true
                );
            }
        }

        private void PaintShape(SceneView sceneView, int id)
        {
            Camera    camera     = sceneView.camera;
            Color     temp       = Handles.color;
            Matrix4x4 origMatrix = Handles.matrix;

            Handles.matrix = MathConstants.identityMatrix;
            Quaternion rotation = camera.transform.rotation;

            bool isValid;

            Vector3[] realVertices = settings.GetVertices(
                buildPlane, worldPosition, gridTangent, gridBinormal, out isValid
            );

            if(editMode == EditMode.EditShape)
            {
                shapeIsValid = isValid;
            }

            if(realVertices != null && realVertices.Length >= 3)
            {
                Color wireframeColor = ColorSettings.WireframeOutline;

                if(!shapeIsValid || !isValid)
                {
                    wireframeColor = Color.red;
                }

                for(int i = 1; i < realVertices.Length; i++)
                {
                    PaintUtility.DrawLine(
                        realVertices[i - 1], realVertices[i], GUIConstants.OLD_LINE_SCALE, wireframeColor
                    );

                    PaintUtility.DrawDottedLine(realVertices[i - 1], realVertices[i], wireframeColor, 4.0f);
                }

                PaintUtility.DrawLine(
                    realVertices[realVertices.Length - 1], realVertices[0], GUIConstants.OLD_LINE_SCALE, wireframeColor
                );

                PaintUtility.DrawDottedLine(
                    realVertices[realVertices.Length - 1], realVertices[0], wireframeColor, 4.0f
                );

                //var color = ColorSettings.ShapeDrawingFill;
                //PaintUtility.DrawPolygon(MathConstants.identityMatrix, realVertices, color);
            }

            if(settings.vertices != null && settings.vertices.Length > 0)
            {
                Handles.color = ColorSettings.PointInnerStateColor[0];

                for(int i = 0; i < settings.vertices.Length; i++)
                {
                    float handleSize       = CSG_HandleUtility.GetHandleSize(settings.vertices[i]);
                    float scaledHandleSize = handleSize * GUIConstants.HANDLE_SCALE;
                    PaintUtility.SquareDotCap(id, settings.vertices[i], rotation, scaledHandleSize);
                }

                PaintSquare();
                PaintBounds(camera);
            }

            Handles.color = ColorSettings.PointInnerStateColor[3];

            {
                float handleSize       = CSG_HandleUtility.GetHandleSize(worldPosition);
                float scaledHandleSize = handleSize * GUIConstants.HANDLE_SCALE;
                PaintUtility.SquareDotCap(id, worldPosition, rotation, scaledHandleSize);
            }

            Handles.matrix = origMatrix;
            Handles.color  = temp;
        }

        protected override void CreateControlIDs()
        {
            base.CreateControlIDs();

            if(settings.vertices.Length > 0)
            {
                if(settings.vertexIDs == null ||
                   settings.vertexIDs.Length != settings.vertices.Length)
                {
                    settings.vertexIDs = new int[settings.vertices.Length];
                }

                for(int i = 0; i < settings.vertices.Length; i++)
                {
                    settings.vertexIDs[i] = GUIUtility.GetControlID(ShapeBuilderPointHash, FocusType.Passive);
                }
            }
        }

        private void CreateSnappedPlanes()
        {
            firstSnappedPlanes = new CSGPlane[firstSnappedEdges.Length / 2];

            for(int i = 0; i < firstSnappedEdges.Length; i += 2)
            {
                Vector3 point0 = firstSnappedEdges[i + 0];
                Vector3 point1 = firstSnappedEdges[i + 1];

                Vector3 binormal = (point1 - point0).normalized;
                Vector3 tangent  = buildPlane.normal;
                Vector3 normal   = Vector3.Cross(binormal, tangent);

                CSGPlane worldPlane = new CSGPlane(normal, point0);
                // note, we use 'inverse' of the worldToLocalMatrix because to transform a plane we'd need to do an inverse, 
                // and using the already inversed matrix we don't need to do a costly inverse.

                Matrix4x4 transform = firstSnappedBrush.transform.localToWorldMatrix;

                CSGPlane  localPlane      = GeometryUtility.InverseTransformPlane(transform, worldPlane);
                Vector3[] vertices        = firstSnappedBrush.ControlMesh.Vertices;
                bool      planeIsInversed = false;

                for(int v = 0; v < vertices.Length; v++)
                {
                    if(localPlane.Distance(vertices[v]) > MathConstants.DistanceEpsilon)
                    {
                        planeIsInversed = true;

                        break;
                    }
                }

                if(planeIsInversed)
                {
                    firstSnappedPlanes[i / 2] = worldPlane.Negated();
                }
                else
                {
                    firstSnappedPlanes[i / 2] = worldPlane;
                }
            }
        }

        protected override void HandleCreateShapeEvents(SceneView sceneView, Rect sceneRect)
        {
            if(settings.vertices.Length < 2)
            {
                if(editMode == EditMode.ExtrudeShape ||
                   editMode == EditMode.EditShape)
                {
                    editMode = EditMode.CreatePlane;
                }
            }

            bool     pointOnEdge      = false;
            bool     havePlane        = false;
            bool     vertexOnGeometry = false;
            CSGBrush vertexOnBrush    = null;

            CSGPlane hoverBuildPlane = buildPlane;
            Camera   camera          = sceneView.camera;

            if(camera != null &&
               camera.pixelRect.Contains(Event.current.mousePosition))
            {
                if(!hoverDefaultPlane.HasValue ||
                   settings.vertices.Length == 0)
                {
                    bool forceGrid = CSGGrid.ForceGrid;
                    CSGGrid.ForceGrid  = false;
                    hoverDefaultPlane  = CSGGrid.CurrentGridPlane;
                    CSGGrid.ForceGrid  = forceGrid;
                    firstSnappedEdges  = null;
                    firstSnappedBrush  = null;
                    firstSnappedPlanes = null;
                    geometryModel      = null;
                }

                if(editMode == EditMode.CreatePlane)
                {
                    LegacyBrushIntersection intersection;

                    if(!IgnoreDepthForRayCasts(camera) && !havePlane &&
                       EditorWindow.mouseOverWindow == sceneView &&
                       SceneQueryUtility.FindWorldIntersection(camera, Event.current.mousePosition, out intersection))
                    {
                        worldPosition   = intersection.worldIntersection;
                        hoverBuildPlane = intersection.worldPlane;
                        vertexOnBrush   = intersection.brush;

                        vertexOnGeometry = true;
                    }
                    else
                    {
                        hoverBuildPlane = hoverDefaultPlane.Value;
                        vertexOnBrush   = null;

                        Ray mouseRay = HandleUtility.GUIPointToWorldRay(Event.current.mousePosition);
                        worldPosition    = hoverBuildPlane.RayIntersection(mouseRay);
                        vertexOnGeometry = false;
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

                    if(settings.vertices.Length == 1)
                    {
                        if(hoverBuildPlane.normal != MathConstants.zeroVector3)
                        {
                            editMode  = EditMode.CreateShape;
                            havePlane = true;
                        }
                    }
                    else if(settings.vertices.Length == 2)
                    {
                        onLastPoint = true;
                    }
                }
                else
                {
                    Ray mouseRay = HandleUtility.GUIPointToWorldRay(Event.current.mousePosition);
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

                if(worldPosition != prevWorldPosition)
                {
                    prevWorldPosition = worldPosition;

                    if(Event.current.type != EventType.Repaint)
                    {
                        CSG_EditorGUIUtility.RepaintAll();
                    }
                }

                visualSnappedGrid  = CSGGrid.FindAllGridEdgesThatTouchPoint(camera, worldPosition);
                visualSnappedBrush = vertexOnBrush;
            }

            CSGGrid.SetForcedGrid(camera, hoverBuildPlane);

            if(!SceneDragToolManager.IsDraggingObjectInScene &&
               Event.current.type == EventType.Repaint)
            {
                PaintSnapVisualisation();
                PaintShape(sceneView, shapeId);
            }

            EventType type = Event.current.GetTypeForControl(shapeId);

            switch(type)
            {
                case EventType.Layout:
                {
                    return;
                }

                case EventType.ValidateCommand:
                case EventType.KeyDown:
                {
                    if(GUIUtility.hotControl == shapeId)
                    {
                        if(Keys.performActionKey.IsKeyPressed() ||
                           Keys.deleteSelectionKey.IsKeyPressed() ||
                           Keys.cancelActionKey.IsKeyPressed())
                        {
                            Event.current.Use();
                        }
                    }

                    return;
                }
                case EventType.KeyUp:
                {
                    if(GUIUtility.hotControl == shapeId)
                    {
                        if(Keys.boxBuilderMode.IsKeyPressed() ||
                           Keys.performActionKey.IsKeyPressed())
                        {
                            HotKeyReleased();
                            Event.current.Use();

                            return;
                        }

                        if(Keys.deleteSelectionKey.IsKeyPressed() ||
                           Keys.cancelActionKey.IsKeyPressed())
                        {
                            Cancel();
                            Event.current.Use();

                            return;
                        }
                    }

                    return;
                }

                case EventType.MouseDown:
                {
                    if(!sceneRect.Contains(Event.current.mousePosition))
                    {
                        break;
                    }

                    if(Tools.viewTool != ViewTool.None && Tools.viewTool != ViewTool.Pan)
                    {
                        return;
                    }

                    if((GUIUtility.hotControl != 0 && GUIUtility.hotControl != shapeEditId
                        && GUIUtility.hotControl != shapeId) ||
                       Event.current.button != 0)
                    {
                        return;
                    }

                    Event.current.Use();

                    if(settings.vertices.Length == 0)
                    {
                        if((GUIUtility.hotControl == 0 ||
                               GUIUtility.hotControl == shapeEditId) && shapeId != -1)
                        {
                            CalculateWorldSpaceTangents(camera);
                            GUIUtility.hotControl             = shapeId;
                            GUIUtility.keyboardControl        = shapeId;
                            EditorGUIUtility.editingTextField = false;
                        }
                    }

                    if(GUIUtility.hotControl == shapeId && settings.vertices.Length < 2)
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

                            if(settings.vertices.Length == 0)
                            {
                                if(pointOnEdge)
                                {
                                    firstSnappedEdges  = visualSnappedEdges.ToArray();
                                    firstSnappedBrush  = visualSnappedBrush;
                                    firstSnappedPlanes = null;
                                }
                                else
                                {
                                    firstSnappedBrush  = null;
                                    firstSnappedEdges  = null;
                                    firstSnappedPlanes = null;
                                }

                                geometryPlane   = buildPlane;
                                planeOnGeometry = vertexOnGeometry;
                            }
                            else
                            {
                                if(firstSnappedEdges != null)
                                {
                                    if(firstSnappedPlanes == null)
                                    {
                                        CreateSnappedPlanes();
                                    }

                                    bool outside = true;

                                    for(int i = 0; i < firstSnappedPlanes.Length; i++)
                                    {
                                        if(firstSnappedPlanes[i].Distance(worldPosition)
                                        <= MathConstants.DistanceEpsilon)
                                        {
                                            outside = false;

                                            break;
                                        }
                                    }

                                    planeOnGeometry = !outside;
                                }

                                if(vertexOnGeometry)
                                {
                                    CSGPlane plane    = hoverDefaultPlane.Value;
                                    float    distance = plane.Distance(worldPosition);

                                    if(float.IsInfinity(distance) || float.IsNaN(distance))
                                    {
                                        distance = 1.0f;
                                    }

                                    plane.d           += distance;
                                    hoverDefaultPlane =  plane;

                                    for(int i = 0; i < settings.vertices.Length; i++)
                                    {
                                        if(!settings.onGeometryVertices[i])
                                        {
                                            settings.vertices[i] = GeometryUtility.ProjectPointOnPlane(
                                                plane, settings.vertices[i]
                                            );

                                            settings.onGeometryVertices[i] = true;
                                        }
                                    }
                                }
                            }

                            ArrayUtility.Add(ref settings.onGeometryVertices, vertexOnGeometry);
                            settings.AddPoint(worldPosition);
                            CSG_EditorGUIUtility.RepaintAll();

                            if(settings.vertices.Length == 2)
                            {
                                HotKeyReleased();
                            }
                        }
                    }

                    return;
                }
                case EventType.MouseDrag:
                {
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
                    if(GUIUtility.hotControl != shapeId)
                    {
                        return;
                    }

                    if(Tools.viewTool != ViewTool.None && Tools.viewTool != ViewTool.Pan)
                    {
                        return;
                    }

                    if(Event.current.button == 0)
                    {
                        Event.current.Use();

                        ResetVisuals();

                        if(onLastPoint)
                        {
                            GUIUtility.hotControl             = 0;
                            GUIUtility.keyboardControl        = 0;
                            EditorGUIUtility.editingTextField = false;

                            editMode = EditMode.CreateShape;
                            HotKeyReleased();
                        }
                    }

                    return;
                }
            }

            return;
        }

        internal override void BeginExtrusion()
        {
            settings.CopyBackupVertices();
        }

        internal override void EndExtrusion()
        {
        }

        protected override void HandleEditShapeEvents(SceneView sceneView, Rect sceneRect)
        {
            Camera camera = sceneView.camera;

            if(settings.vertices.Length < 2)
            {
                if(editMode == EditMode.ExtrudeShape ||
                   editMode == EditMode.EditShape)
                {
                    editMode = EditMode.CreatePlane;
                }
            }

            if(!SceneDragToolManager.IsDraggingObjectInScene &&
               Event.current.type == EventType.Repaint)
            {
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

                if(visualSnappedBrush != null)
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

                Matrix4x4 origMatrix = Handles.matrix;
                Handles.matrix = MathConstants.identityMatrix;
                PaintBounds(camera);
                Handles.matrix = origMatrix;
            }

            HandleHeightHandles(sceneView, sceneRect, false);

            for(int i = 0; i < settings.vertices.Length; i++)
            {
                int       id         = settings.vertexIDs[i];
                EventType point_type = Event.current.GetTypeForControl(id);

                switch(point_type)
                {
                    case EventType.Repaint:
                    {
                        if(SceneDragToolManager.IsDraggingObjectInScene)
                        {
                            break;
                        }

                        bool      isSelected = id == GUIUtility.keyboardControl;
                        Color     temp       = Handles.color;
                        Matrix4x4 origMatrix = Handles.matrix;

                        Handles.matrix = MathConstants.identityMatrix;
                        Quaternion rotation = camera.transform.rotation;

                        if(isSelected)
                        {
                            Handles.color = ColorSettings.PointInnerStateColor[3];
                        }
                        else if(HandleUtility.nearestControl == id)
                        {
                            Handles.color = ColorSettings.PointInnerStateColor[1];
                        }
                        else
                        {
                            Handles.color = ColorSettings.PointInnerStateColor[0];
                        }

                        float handleSize       = CSG_HandleUtility.GetHandleSize(settings.vertices[i]);
                        float scaledHandleSize = handleSize * GUIConstants.HANDLE_SCALE;
                        PaintUtility.SquareDotCap(id, settings.vertices[i], rotation, scaledHandleSize);

                        Handles.matrix = origMatrix;
                        Handles.color  = temp;

                        break;
                    }

                    case EventType.Layout:
                    {
                        if(Tools.viewTool != ViewTool.None && Tools.viewTool != ViewTool.Pan)
                        {
                            break;
                        }

                        Matrix4x4 origMatrix = Handles.matrix;
                        Handles.matrix = MathConstants.identityMatrix;
                        float handleSize       = CSG_HandleUtility.GetHandleSize(settings.vertices[i]);
                        float scaledHandleSize = handleSize * GUIConstants.HANDLE_SCALE;

                        HandleUtility.AddControl(
                            id, HandleUtility.DistanceToCircle(settings.vertices[i], scaledHandleSize)
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

                        if(Tools.viewTool != ViewTool.None && Tools.viewTool != ViewTool.Pan)
                        {
                            break;
                        }

                        if(GUIUtility.hotControl == 0 && HandleUtility.nearestControl == id
                        && Event.current.button == 0)
                        {
                            GUIUtility.hotControl             = id;
                            GUIUtility.keyboardControl        = id;
                            EditorGUIUtility.editingTextField = false;
                            EditorGUIUtility.SetWantsMouseJumping(1);
                            Event.current.Use();

                            break;
                        }

                        break;
                    }
                    case EventType.MouseDrag:
                    {
                        if(Tools.viewTool != ViewTool.None && Tools.viewTool != ViewTool.Pan)
                        {
                            break;
                        }

                        if(GUIUtility.hotControl == id && Event.current.button == 0)
                        {
                            Undo.RecordObject(this, "Modify shape");

                            Ray mouseRay = HandleUtility.GUIPointToWorldRay(Event.current.mousePosition);
                            CSGGrid.SetForcedGrid(camera, buildPlane);

                            CSGPlane alignedPlane = new CSGPlane(
                                CSGGrid.CurrentWorkGridPlane.normal, settings.vertices[0]
                            );

                            Vector3 worldPosition = buildPlane.Project(alignedPlane.RayIntersection(mouseRay));

                            if(float.IsInfinity(worldPosition.x) || float.IsNaN(worldPosition.x) ||
                               float.IsInfinity(worldPosition.y) || float.IsNaN(worldPosition.y) ||
                               float.IsInfinity(worldPosition.z) || float.IsNaN(worldPosition.z))
                            {
                                worldPosition = settings.vertices[i];
                            }

                            ResetVisuals();

                            if(snapFunction != null)
                            {
                                CSGBrush snappedOnBrush;

                                worldPosition = snapFunction(
                                    camera, worldPosition, buildPlane, ref visualSnappedEdges, out snappedOnBrush
                                  , generatedBrushes
                                );
                            }

                            visualSnappedGrid = CSGGrid.FindAllGridEdgesThatTouchPoint(camera, worldPosition);

                            settings.vertices[i] = worldPosition;

                            CenterExtrusionPoints(buildPlane);
                            UpdateBaseShape();

                            if(editMode == EditMode.ExtrudeShape)
                            {
                                StartExtrudeMode(camera);
                                UpdateBaseShape();
                            }

                            UpdateExtrudedShape();

                            GUI.changed = true;
                            Event.current.Use();

                            break;
                        }

                        break;
                    }
                    case EventType.MouseUp:
                    {
                        if(GUIUtility.hotControl != id)
                        {
                            break;
                        }

                        if(Tools.viewTool != ViewTool.None && Tools.viewTool != ViewTool.Pan)
                        {
                            break;
                        }

                        if(Event.current.button == 0)
                        {
                            GUIUtility.hotControl             = 0;
                            GUIUtility.keyboardControl        = 0;
                            EditorGUIUtility.editingTextField = false;
                            EditorGUIUtility.SetWantsMouseJumping(0);
                            Event.current.Use();

                            ResetVisuals();

                            //if (Length == 0 || Width == 0)
                            //{
                            //	Cancel();
                            //}
                            break;
                        }

                        break;
                    }
                }
            }
        }

        public bool OnShowGUI(bool isSceneGUI)
        {
            return GeneratorBoxGUI.OnShowGUI(this, isSceneGUI);
        }
    }
}
