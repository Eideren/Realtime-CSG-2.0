using System;
using System.Collections.Generic;
using InternalRealtimeCSG;
using RealtimeCSG.Components;
using RealtimeCSG.Legacy;
using UnityEditor;
using UnityEngine;

namespace RealtimeCSG
{
    internal sealed class GeneratorSphere : GeneratorBase, IGenerator
    {
        [NonSerialized] private Vector3 _worldPosition;
        [NonSerialized] private Vector3 _prevWorldPosition;
        [NonSerialized] private CSGPlane? _hoverDefaultPlane;

        [NonSerialized] private CSGPlane[] _firstSnappedPlanes;
        [NonSerialized] private Vector3[] _firstSnappedEdges;
        [NonSerialized] private CSGBrush _firstSnappedBrush;
        [NonSerialized] private bool _hadSphere;
        [NonSerialized] private bool _prevIsHemisphere;

        [NonSerialized] private int _prevSplits = -1;
        [NonSerialized] private ControlMesh _splitControlMesh;
        [NonSerialized] private Shape _splitShape;
        [NonSerialized] private uint? _sphereSmoothingGroup;

        // sphere specific
        [SerializeField] private GeneratorSphereSettings settings = new GeneratorSphereSettings();

        protected override IShapeSettings ShapeSettings
        {
            get => settings;
        }

        public int SphereSplits
        {
            get => settings.SphereSplits;
            set
            {
                if(settings.SphereSplits == value)
                {
                    return;
                }

                Undo.RecordObject(this, "Modified Sphere Splits");
                settings.SphereSplits = value;
                UpdateBaseShape();
            }
        }

        public float SphereOffset
        {
            get => settings.SphereOffset;
            set
            {
                if(settings.SphereOffset == value)
                {
                    return;
                }

                Undo.RecordObject(this, "Modified Sphere Offset");
                settings.SphereOffset = value;
                UpdateBaseShape();
            }
        }

        public bool SphereSmoothShading
        {
            get => settings.SphereSmoothShading;
            set
            {
                if(settings.SphereSmoothShading == value)
                {
                    return;
                }

                Undo.RecordObject(this, "Modified Sphere Smoothing");
                settings.SphereSmoothShading = value;
                UpdateBaseShape();
            }
        }

        public bool IsHemiSphere
        {
            get => settings.IsHemiSphere;
            set
            {
                if(settings.IsHemiSphere == value)
                {
                    return;
                }

                Undo.RecordObject(this, "Modified Sphere Smoothing");
                settings.IsHemiSphere = value;
                UpdateBaseShape();
            }
        }

        public float SphereRadius
        {
            get => settings.SphereRadius;
            set
            {
                if(settings.SphereRadius == value)
                {
                    return;
                }

                Undo.RecordObject(this, "Modified Sphere Radius");
                settings.SphereRadius = value;
                UpdateBaseShape();
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

        public override void Init()
        {
            base.Init();
            Reset();
        }

        public override void Reset()
        {
            settings.Reset();
            base.Reset();

            _firstSnappedPlanes   = null;
            _firstSnappedEdges    = null;
            _firstSnappedBrush    = null;
            _hadSphere            = false;
            _prevSplits           = -1;
            _prevIsHemisphere     = IsHemiSphere;
            _splitControlMesh     = null;
            _splitShape           = null;
            _sphereSmoothingGroup = null;
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
                        settings.AddPoint(_worldPosition);
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

        private void PaintRadiusMessage(Camera camera)
        {
            if(settings.vertices == null || settings.vertices.Length == 0)
            {
                return;
            }

            Vector3 endPosition = settings.vertices.Length == 1 ? _worldPosition : settings.vertices[1];
            Vector3 centerPoint = settings.vertices[0];
            Vector3 delta       = (endPosition - centerPoint).normalized;

            PaintUtility.DrawLength(
                camera, "radius: ", HandleUtility.GetHandleSize(centerPoint), Matrix4x4.identity
              , Vector3.Cross(buildPlane.normal, delta), centerPoint, endPosition, Color.white
            );
        }

        private void PaintSquare(Camera camera)
        {
            Color wireframeColor = ColorSettings.BoundsOutlines;

            Vector3 endPosition = settings.vertices.Length == 1 ? _worldPosition : settings.vertices[1];
            Vector3 centerPoint = settings.vertices[0];
            float   height      = (endPosition - centerPoint).magnitude;

            Vector3 upVector = buildPlane.normal * height;

            Vector3 point0 = settings.vertices[0] + gridTangent * settings.sphereRadius
              + gridBinormal * settings.sphereRadius;

            Vector3 point1 = settings.vertices[0] + gridTangent * settings.sphereRadius
              - gridBinormal * settings.sphereRadius;

            Vector3 point2 = settings.vertices[0] - gridTangent * settings.sphereRadius
              - gridBinormal * settings.sphereRadius;

            Vector3 point3 = settings.vertices[0] - gridTangent * settings.sphereRadius
              + gridBinormal * settings.sphereRadius;

            Vector3 point4 = point0;
            Vector3 point5 = point1;
            Vector3 point6 = point2;
            Vector3 point7 = point3;

            Vector3 point8 = point0 + upVector;
            Vector3 point9 = point1 + upVector;
            Vector3 pointA = point2 + upVector;
            Vector3 pointB = point3 + upVector;

            if(!IsHemiSphere)
            {
                point4 -= upVector;
                point5 -= upVector;
                point6 -= upVector;
                point7 -= upVector;
            }

            Vector3[] points =
            {
                point8, point9, point9, pointA, pointA, pointB, pointB, point8, point4, point5, point5, point6
              , point6, point7, point7, point4, point8, point4, point9, point5, pointA, point6, pointB, point7
            };

            PaintUtility.DrawDottedLines(points, wireframeColor);

            if(settings.vertices.Length == 0)
            {
                return;
            }

            Vector3[] volume = new Vector3[8];

            AABB localBounds = new AABB();
            localBounds.Reset();
            localBounds.Extend(toGridQuaternion * (point0 + upVector));
            localBounds.Extend(toGridQuaternion * (point1 + upVector));
            localBounds.Extend(toGridQuaternion * (point2 + upVector));
            localBounds.Extend(toGridQuaternion * (point3 + upVector));

            if(IsHemiSphere)
            {
                localBounds.Extend(toGridQuaternion * point3);
            }
            else
            {
                localBounds.Extend(toGridQuaternion * (point3 - upVector));
            }

            BoundsUtilities.GetBoundsCornerPoints(localBounds, volume);

            PaintUtility.RenderBoundsSizes(
                toGridQuaternion, fromGridQuaternion, camera, volume, Color.white, Color.white, Color.white, true
              , true, true
            );
        }

        private void PaintCircle(SceneView sceneView, int id)
        {
            Camera    camera     = sceneView.camera;
            Color     temp       = Handles.color;
            Matrix4x4 origMatrix = Handles.matrix;

            Handles.matrix = MathConstants.identityMatrix;
            Quaternion rotation = camera.transform.rotation;

            Vector3[] realVertices = settings.GetVertices(
                buildPlane, _worldPosition, gridTangent, out bool isValid
            );

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

                    PaintUtility.DrawDottedLine(realVertices[i - 1], realVertices[i], wireframeColor);
                }

                PaintUtility.DrawLine(
                    realVertices[realVertices.Length - 1], realVertices[0], GUIConstants.OLD_LINE_SCALE, wireframeColor
                );

                PaintUtility.DrawDottedLine(realVertices[realVertices.Length - 1], realVertices[0], wireframeColor);

                if(realVertices.Length >= 3)
                {
                    Color color = ColorSettings.ShapeDrawingFill;
                    PaintUtility.DrawPolygon(MathConstants.identityMatrix, realVertices, color);
                }

                PaintSquare(camera);
            }

            if(settings.vertices != null && settings.vertices.Length > 0)
            {
                Handles.color = ColorSettings.PointInnerStateColor[0];

                for(int i = 0; i < settings.vertices.Length; i++)
                {
                    float handleSize       = CSGHandleUtility.GetHandleSize(settings.vertices[i]);
                    float scaledHandleSize = handleSize * GUIConstants.HANDLE_SCALE;
                    PaintUtility.SquareDotCap(id, settings.vertices[i], rotation, scaledHandleSize);
                }

                PaintRadiusMessage(camera);
            }

            Handles.color = ColorSettings.PointInnerStateColor[3];

            {
                float handleSize       = CSGHandleUtility.GetHandleSize(_worldPosition);
                float scaledHandleSize = handleSize * GUIConstants.HANDLE_SCALE;
                PaintUtility.SquareDotCap(id, _worldPosition, rotation, scaledHandleSize);
            }

            Handles.matrix = origMatrix;
            Handles.color  = temp;
        }

        private void CreateSnappedPlanes()
        {
            _firstSnappedPlanes = new CSGPlane[_firstSnappedEdges.Length / 2];

            for(int i = 0; i < _firstSnappedEdges.Length; i += 2)
            {
                Vector3 point0 = _firstSnappedEdges[i + 0];
                Vector3 point1 = _firstSnappedEdges[i + 1];

                Vector3 binormal = (point1 - point0).normalized;
                Vector3 tangent  = buildPlane.normal;
                Vector3 normal   = Vector3.Cross(binormal, tangent);

                CSGPlane worldPlane = new CSGPlane(normal, point0);

                // note, we use 'inverse' of the worldToLocalMatrix because to transform a plane we'd need to do an inverse,
                // and using the already inversed matrix we don't need to do a costly inverse.
                CSGPlane localPlane = GeometryUtility.InverseTransformPlane(
                    _firstSnappedBrush.transform.localToWorldMatrix, worldPlane
                );

                Vector3[] vertices        = _firstSnappedBrush.ControlMesh.Vertices;
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
                    _firstSnappedPlanes[i / 2] = worldPlane.Negated();
                }
                else
                {
                    _firstSnappedPlanes[i / 2] = worldPlane;
                }
            }
        }

        protected override void HandleEditShapeEvents(SceneView sceneView, Rect sceneRect)
        {
            Camera camera = sceneView.camera;

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
                    Matrix4x4 matrix = Handles.matrix;
                    Handles.matrix = MathConstants.identityMatrix;
                    PaintUtility.DrawDottedLines(visualSnappedGrid.ToArray(), ColorSettings.SnappedEdges);
                    Handles.matrix = matrix;
                }

                if(visualSnappedBrush != null)
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

                Matrix4x4 origMatrix = Handles.matrix;
                Handles.matrix = MathConstants.identityMatrix;

                Vector3[] realVertices = settings.GetVertices(
                    buildPlane, _worldPosition, gridTangent, out bool isValid
                );

                if(editMode == EditMode.EditShape)
                {
                    shapeIsValid = isValid;
                }

                Color wireframeColor = ColorSettings.WireframeOutline;

                if(!shapeIsValid || !isValid)
                {
                    wireframeColor = Color.red;
                }

                if(realVertices.Length > 0)
                {
                    if(realVertices.Length >= 3)
                    {
                        Color color = ColorSettings.ShapeDrawingFill;
                        PaintUtility.DrawPolygon(MathConstants.identityMatrix, realVertices, color);
                    }

                    for(int i = 1; i < realVertices.Length; i++)
                    {
                        PaintUtility.DrawLine(
                            realVertices[i - 1], realVertices[i], GUIConstants.OLD_LINE_SCALE, wireframeColor
                        );

                        PaintUtility.DrawDottedLine(realVertices[i - 1], realVertices[i], wireframeColor);
                    }

                    PaintUtility.DrawLine(
                        realVertices[realVertices.Length - 1], realVertices[0], GUIConstants.OLD_LINE_SCALE
                      , wireframeColor
                    );

                    PaintUtility.DrawDottedLine(realVertices[realVertices.Length - 1], realVertices[0], wireframeColor);

                    PaintSquare(camera);
                    PaintRadiusMessage(camera);
                }

                Handles.matrix = origMatrix;
            }

            EventType shapeType = Event.current.GetTypeForControl(shapeId);
            HandleKeyboard(shapeType);

            for(int i = 1; i < settings.vertices.Length; i++)
            {
                int       id        = settings.vertexIDs[i];
                EventType pointType = Event.current.GetTypeForControl(id);

                switch(pointType)
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

                        float handleSize       = CSGHandleUtility.GetHandleSize(settings.vertices[i]);
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
                        float handleSize       = CSGHandleUtility.GetHandleSize(settings.vertices[i]);
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
                                worldPosition = snapFunction(
                                    camera, worldPosition, buildPlane, ref visualSnappedEdges, out CSGBrush _
                                  , generatedBrushes, true
                                );
                            }

                            visualSnappedGrid = CSGGrid.FindAllGridEdgesThatTouchPoint(camera, worldPosition);

                            settings.vertices[i] = worldPosition;

                            UpdateBaseShape();

                            GUI.changed = true;
                            Event.current.Use();
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

                            if(SphereRadius == 0)
                            {
                                Cancel();
                            }
                        }

                        break;
                    }
                }
            }
        }

        protected override void HandleCreateShapeEvents(SceneView sceneView, Rect sceneRect)
        {
            bool     pointOnEdge      = false;
            bool     havePlane        = false;
            bool     vertexOnGeometry = false;
            CSGBrush vertexOnBrush    = null;

            CSGPlane hoverBuildPlane = buildPlane;
            Camera   camera          = sceneView.camera;
            bool     assume2DView    = CSGSettings.Assume2DView(camera);

            if(camera != null &&
               camera.pixelRect.Contains(Event.current.mousePosition))
            {
                if(!_hoverDefaultPlane.HasValue ||
                   settings.vertices.Length == 0)
                {
                    bool forceGrid = CSGGrid.ForceGrid;
                    CSGGrid.ForceGrid   = false;
                    _hoverDefaultPlane  = CSGGrid.CurrentGridPlane;
                    CSGGrid.ForceGrid   = forceGrid;
                    _firstSnappedEdges  = null;
                    _firstSnappedBrush  = null;
                    _firstSnappedPlanes = null;
                    geometryModel       = null;
                }

                if(editMode == EditMode.CreatePlane)
                {
                    if(!assume2DView && !havePlane &&
                       EditorWindow.mouseOverWindow == sceneView &&
                       SceneQueryUtility.FindWorldIntersection(
                           camera, Event.current.mousePosition, out LegacyBrushIntersection intersection
                       ))
                    {
                        _worldPosition  = intersection.worldIntersection;
                        hoverBuildPlane = intersection.worldPlane;
                        vertexOnBrush   = intersection.brush;

                        vertexOnGeometry = true;
                    }
                    else
                    {
                        hoverBuildPlane = _hoverDefaultPlane.Value;

                        Ray mouseRay = HandleUtility.GUIPointToWorldRay(Event.current.mousePosition);
                        _worldPosition = hoverBuildPlane.RayIntersection(mouseRay);
                    }

                    ResetVisuals();

                    if(snapFunction != null)
                    {
                        _worldPosition = snapFunction(
                            camera, _worldPosition, hoverBuildPlane, ref visualSnappedEdges, out CSGBrush snappedOnBrush
                          , generatedBrushes, true
                        );

                        if(snappedOnBrush != null)
                        {
                            pointOnEdge = visualSnappedEdges != null && visualSnappedEdges.Count > 0;

                            vertexOnBrush    = snappedOnBrush;
                            vertexOnGeometry = true;
                        }
                    }

                    if(settings.vertices.Length == 1)
                    {
                        if(hoverBuildPlane.normal != MathConstants.zeroVector3)
                        {
                            editMode = EditMode.CreateShape;
                        }
                    }
                }
                else
                {
                    Ray mouseRay = HandleUtility.GUIPointToWorldRay(Event.current.mousePosition);
                    _worldPosition = hoverBuildPlane.RayIntersection(mouseRay);

                    ResetVisuals();

                    if(snapFunction != null)
                    {
                        _worldPosition = snapFunction(
                            camera, _worldPosition, hoverBuildPlane, ref visualSnappedEdges, out CSGBrush snappedOnBrush
                          , generatedBrushes, true
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

                if(_worldPosition != _prevWorldPosition)
                {
                    _prevWorldPosition = _worldPosition;

                    if(settings.vertices.Length > 0)
                    {
                        if(_hadSphere || (settings.vertices[0] - _worldPosition).magnitude
                         > MathConstants.EqualityEpsilon)
                        {
                            _hadSphere = true;
                            UpdateBaseShape();
                        }
                    }

                    if(Event.current.type != EventType.Repaint)
                    {
                        CSGEditorGUIUtility.RepaintAll();
                    }
                }

                visualSnappedGrid  = CSGGrid.FindAllGridEdgesThatTouchPoint(camera, _worldPosition);
                visualSnappedBrush = vertexOnBrush;
            }

            CSGGrid.SetForcedGrid(camera, hoverBuildPlane);

            if(!SceneDragToolManager.IsDraggingObjectInScene && Event.current.type == EventType.Repaint)
            {
                PaintSnapVisualisation();
                PaintCircle(sceneView, shapeId);
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
                        if(Keys.cylinderBuilderMode.IsKeyPressed() ||
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
                        if(!float.IsNaN(_worldPosition.x) && !float.IsInfinity(_worldPosition.x) &&
                           !float.IsNaN(_worldPosition.y) && !float.IsInfinity(_worldPosition.y) &&
                           !float.IsNaN(_worldPosition.z) && !float.IsInfinity(_worldPosition.z))
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
                                    _firstSnappedEdges = visualSnappedEdges.ToArray();
                                    _firstSnappedBrush = visualSnappedBrush;
                                }
                                else
                                {
                                    _firstSnappedBrush = null;
                                    _firstSnappedEdges = null;
                                }

                                _firstSnappedPlanes = null;
                                planeOnGeometry     = vertexOnGeometry;
                            }
                            else
                            {
                                if(_firstSnappedEdges != null)
                                {
                                    if(_firstSnappedPlanes == null)
                                    {
                                        CreateSnappedPlanes();
                                    }

                                    bool outside = true;

                                    for(int i = 0; i < _firstSnappedPlanes.Length; i++)
                                    {
                                        if(_firstSnappedPlanes[i].Distance(_worldPosition)
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
                                    CSGPlane plane    = _hoverDefaultPlane.Value;
                                    float    distance = plane.Distance(_worldPosition);
                                    plane.d            += distance;
                                    _hoverDefaultPlane =  plane;

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
                            settings.AddPoint(_worldPosition);
                            CSGEditorGUIUtility.RepaintAll();

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

                        if(settings.vertices.Length == 2)
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
                    settings.vertexIDs[i] = GUIUtility.GetControlID(shapeBuilderPointHash, FocusType.Passive);
                }
            }
        }

        public override void HandleEvents(SceneView sceneView, Rect sceneRect)
        {
            base.HandleEvents(sceneView, sceneRect);

            if(Event.current.type == EventType.MouseUp)
            {
                if(GUIUtility.hotControl == 0 &&
                   Event.current.modifiers == EventModifiers.None &&
                   !mouseIsDragging && Event.current.button == 0 &&
                   (Tools.viewTool == ViewTool.None || Tools.viewTool == ViewTool.Pan))
                {
                    ResetVisuals();
                    Event.current.Use();
                    Commit(sceneView.camera);
                }
            }
        }

        public bool OnShowGUI(bool isSceneGUI)
        {
            return GeneratorSphereGUI.OnShowGUI(this, isSceneGUI);
        }

        public override AABB GetShapeBounds()
        {
            return ShapeSettings.CalculateBounds(Quaternion.identity, gridTangent, gridBinormal);
        }

        public override bool Commit(Camera camera)
        {
            isFinished = true;
            CleanupGrid();

            if(SphereRadius == 0 || generatedBrushes == null || generatedBrushes.Length < 1 ||
               !UpdateBaseShape())
            {
                Cancel();

                return false;
            }

            EndCommit();

            return true;
        }

        private bool GenerateSphere(float radius, int splits, out ControlMesh controlMesh, out Shape shape)
        {
            if(_prevSplits != splits || _prevIsHemisphere != IsHemiSphere || _splitControlMesh == null
            || _splitShape == null)
            {
                _splitControlMesh = null;
                _splitShape       = null;
                BrushFactory.CreateCubeControlMesh(out _splitControlMesh, out _splitShape, Vector3.one);

                Vector3[] axi = { MathConstants.upVector3, MathConstants.leftVector3, MathConstants.forwardVector3 };

                List<int> intersectedEdges = new List<int>();
                float     step             = 1.0f / (splits + 1);

                for(int i = 0; i < axi.Length; i++)
                {
                    Vector3 normal = axi[i];
                    float   offset = 0.5f - step;

                    while(offset > 0.0f)
                    {
                        ControlMeshUtility.CutMesh(
                            _splitControlMesh, _splitShape, new CSGPlane(-normal, -offset), ref intersectedEdges
                        );

                        if(i != 0 || !IsHemiSphere)
                        {
                            ControlMeshUtility.CutMesh(
                                _splitControlMesh, _splitShape, new CSGPlane(normal, -offset), ref intersectedEdges
                            );
                        }

                        offset -= step;
                    }

                    if(i != 0 || !IsHemiSphere)
                    {
                        if((splits & 1) == 1)
                        {
                            ControlMeshUtility.CutMesh(
                                _splitControlMesh, _splitShape, new CSGPlane(normal, 0), ref intersectedEdges
                            );
                        }
                    }
                }

                if(IsHemiSphere)
                {
                    CSGPlane cuttingPlane = new CSGPlane(MathConstants.upVector3, 0);
                    intersectedEdges.Clear();

                    if(ControlMeshUtility.CutMesh(_splitControlMesh, _splitShape, cuttingPlane, ref intersectedEdges))
                    {
                        List<int> edgeLoop = ControlMeshUtility.FindEdgeLoop(_splitControlMesh, ref intersectedEdges);

                        if(edgeLoop != null)
                        {
                            if(ControlMeshUtility.SplitEdgeLoop(_splitControlMesh, _splitShape, edgeLoop))
                            {
                                ControlMeshUtility.FindAndDetachSeparatePiece(
                                    _splitControlMesh, _splitShape, cuttingPlane, out ControlMesh _, out Shape _
                                );
                            }
                        }
                    }
                }

                // Spherize the cube
                for(int i = 0; i < _splitControlMesh.Vertices.Length; i++)
                {
                    Vector3 v  = _splitControlMesh.Vertices[i] * 2.0f;
                    float   x2 = v.x * v.x;
                    float   y2 = v.y * v.y;
                    float   z2 = v.z * v.z;
                    Vector3 s;
                    s.x = v.x * Mathf.Sqrt(1f - y2 * 0.5f - z2 * 0.5f + y2 * z2 / 3.0f);
                    s.y = v.y * Mathf.Sqrt(1f - z2 * 0.5f - x2 * 0.5f + z2 * x2 / 3.0f);
                    s.z = v.z * Mathf.Sqrt(1f - x2 * 0.5f - y2 * 0.5f + x2 * y2 / 3.0f);

                    _splitControlMesh.Vertices[i] = s;
                }

                if(!ControlMeshUtility.Triangulate(null, _splitControlMesh, _splitShape))
                {
                    Debug.LogWarning("!ControlMeshUtility.IsConvex");
                    controlMesh = null;
                    shape       = null;

                    return false;
                }

                ControlMeshUtility.FixTexGens(_splitControlMesh, _splitShape);

                if(!ControlMeshUtility.IsConvex(_splitControlMesh, _splitShape))
                {
                    Debug.LogWarning("!ControlMeshUtility.IsConvex");
                    controlMesh = null;
                    shape       = null;

                    return false;
                }

                ControlMeshUtility.UpdateTangents(_splitControlMesh, _splitShape);

                _prevSplits       = splits;
                _prevIsHemisphere = IsHemiSphere;
            }

            if(_splitControlMesh == null || _splitShape == null || !_splitControlMesh.Valid)
            {
                Debug.LogWarning("splitControlMesh == null || splitShape == null || !splitControlMesh.IsValid");
                controlMesh = null;
                shape       = null;

                return false;
            }

            controlMesh = _splitControlMesh.Clone();
            shape       = _splitShape.Clone();

            for(int i = 0; i < controlMesh.Vertices.Length; i++)
            {
                Vector3 vertex = controlMesh.Vertices[i];
                vertex                  *= radius;
                controlMesh.Vertices[i] =  vertex;
            }

            for(int i = 0; i < shape.Surfaces.Length; i++)
            {
                CSGPlane plane = shape.Surfaces[i].Plane;
                plane.d                 *= radius;
                shape.Surfaces[i].Plane =  plane;
            }

            bool smoothShading = SphereSmoothShading;

            if(!_sphereSmoothingGroup.HasValue && smoothShading)
            {
                _sphereSmoothingGroup = SurfaceUtility.FindUnusedSmoothingGroupIndex();
            }

            for(int i = 0; i < shape.TexGenFlags.Length; i++)
            {
                shape.TexGens[i].SmoothingGroup = smoothShading ? _sphereSmoothingGroup.Value : 0;
            }

            Material defaultMaterial = CSGSettings.DefaultMaterial;

            for(int s = 0; s < shape.Surfaces.Length; s++)
            {
                int texGenIndex = shape.Surfaces[s].TexGenIndex;

                Vector3    axis     = GeometryUtility.SnapToClosestAxis(shape.Surfaces[s].Plane.normal);
                Quaternion rotation = Quaternion.FromToRotation(axis, MathConstants.backVector3);
                Matrix4x4  matrix   = Matrix4x4.TRS(MathConstants.zeroVector3, rotation, MathConstants.oneVector3);

                SurfaceUtility.AlignTextureSpaces(matrix, false, ref shape.TexGens[texGenIndex], ref shape.Surfaces[s]);

                shape.TexGens[texGenIndex].RenderMaterial = defaultMaterial;
            }

            return true;
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
            if(settings.vertices == null ||
               settings.vertices.Length == 0)
            {
                editMode = EditMode.CreatePlane;
            }

            if(editMode == EditMode.CreatePlane)
            {
                return false;
            }

            float radius = SphereRadius;

            if(editMode == EditMode.CreateShape)
            {
                brushPosition = settings.vertices[0];
                radius        = (settings.vertices[0] - _worldPosition).magnitude;
            }

            GenerateBrushObjects(1);

            if(radius == 0 || generatedBrushes == null || generatedBrushes.Length < 1)
            {
                InternalCSGModelManager.SkipCheckForChanges = false;
                HideGenerateBrushes();

                return false;
            }

            bool failures          = false;
            bool modifiedHierarchy = false;

            if(generatedGameObjects != null && generatedGameObjects.Length > 0)
            {
                for(int i = generatedGameObjects.Length - 1; i >= 0; i--)
                {
                    if(generatedGameObjects[i])
                    {
                        continue;
                    }

                    ArrayUtility.RemoveAt(ref generatedGameObjects, i);
                }
            }

            if(generatedGameObjects == null || generatedGameObjects.Length == 0)
            {
                Cancel();

                return false;
            }

            if(generatedGameObjects != null && generatedGameObjects.Length > 0)
            {
                if(registerUndo)
                {
                    Undo.RecordObjects(generatedGameObjects, "Created Sphere");
                }

                CSGBrush brush = generatedBrushes[0];

                if(brush && brush.gameObject)
                {
                    if(GenerateSphere(
                           radius, settings.SphereSplits, out ControlMesh newControlMesh, out Shape newShape
                       ))
                    {
                        if(!brush.gameObject.activeSelf)
                        {
                            modifiedHierarchy = true;
                            brush.gameObject.SetActive(true);
                        }

                        brush.Shape       = newShape;
                        brush.ControlMesh = newControlMesh;

                        if(registerUndo)
                        {
                            EditorUtility.SetDirty(brush);
                        }

                        ControlMeshUtility.UpdateBrushMesh(brush, brush.ControlMesh, brush.Shape);
                    }
                    else
                    {
                        failures = true;

                        if(brush.gameObject.activeSelf)
                        {
                            modifiedHierarchy = true;
                            brush.gameObject.SetActive(false);
                        }
                    }
                }
            }

            try
            {
                InternalCSGModelManager.SkipCheckForChanges = true;

                if(registerUndo)
                {
                    EditorUtility.SetDirty(this);
                }

                InternalCSGModelManager.CheckForChanges(modifiedHierarchy);
            }
            finally
            {
                InternalCSGModelManager.SkipCheckForChanges = false;
            }

            return !failures;
        }
    }
}
