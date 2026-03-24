using System;
using System.Linq;
using InternalRealtimeCSG;
using RealtimeCSG.Components;
using RealtimeCSG.Foundation;
using RealtimeCSG.Legacy;
using UnityEditor;
using UnityEngine;

namespace RealtimeCSG
{
    [Serializable]
    internal struct ExtrusionPoint
    {
        [NonSerialized]
        public int id;

        public Vector3 position;
    }

    internal abstract class GeneratorExtrudedBase : GeneratorBase
    {
        protected const float HANDLE_EXTENSION = 1.2f;

        private const CSGOperationType INVALID_CSG_OPERATION_TYPE = (CSGOperationType)99;

        [SerializeField] private Vector3[] outlineVertices;
        [SerializeField] private ShapePolygon[] polygons;
        [SerializeField] private ShapeEdge[] shapeEdges;
        [SerializeField] protected ExtrusionPoint[] extrusionPoints = new ExtrusionPoint[2];

        [SerializeField] internal bool haveForcedDirection;
        [SerializeField] internal Vector3 forcedDirection;

        [SerializeField] internal bool smearTextures = true;
        [NonSerialized] internal bool forceDragHandle;
        [NonSerialized] internal CSGOperationType? forceDragSource;
        [NonSerialized] internal bool commitExtrusionAfterRelease;

        [NonSerialized] private CSGPlane _movePlane;
        [NonSerialized] private Vector3 _movePolygonDirection;

        [NonSerialized] private bool _firstClick;
        [NonSerialized] private Vector3 _dragPositionStart = Vector3.zero;
        [NonSerialized] private Vector3 _heightHandleOffset = Vector3.zero;
        [NonSerialized] private Vector2 _heightPosition = Vector2.zero;

        public float DefaultHeight
        {
            get => CSGSettings.DefaultShapeHeight;
            set => CSGSettings.DefaultShapeHeight = value;
        }

        // used by GUI to decide to show Height
        public bool HaveHeight
        {
            get => (editMode == EditMode.ExtrudeShape || editMode == EditMode.EditShape) && extrusionPoints.Length <= 2;
        }

        public bool HaveExtrusion
        {
            get => extrusionPoints.Length > 2 || (extrusionPoints.Length == 2 && (extrusionPoints[0].position - extrusionPoints[1].position).sqrMagnitude > MathConstants.EqualityEpsilonSqr);
        }

        private static float GetSegmentLength(Vector3 point1, Vector3 point2, Vector3 direction)
        {
            float height   = (point1 - point2).magnitude;
            float distance = new CSGPlane(direction, point1).Distance(point2);

            if(float.IsInfinity(distance) || float.IsNaN(distance))
            {
                distance = 1.0f;
            }

            height *= Mathf.Sign(distance);

            return GeometryUtility.CleanLength(height);
        }

        public float Height
        {
            get
            {
                if(!HaveHeight)
                {
                    return 0;
                }

                Vector3 direction = haveForcedDirection ? forcedDirection : buildPlane.normal;

                return GetSegmentLength(extrusionPoints[0].position, extrusionPoints[1].position, direction);
            }
            set
            {
                if(!HaveHeight)
                {
                    return;
                }

                Vector3 direction  = haveForcedDirection ? forcedDirection : buildPlane.normal;
                float   height     = GetSegmentLength(extrusionPoints[0].position, extrusionPoints[1].position, direction);
                float   cleanValue = GeometryUtility.CleanLength(value);

                if(height == cleanValue)
                {
                    return;
                }

                if(editMode == EditMode.EditShape)
                {
                    if(value == 0)
                    {
                        return;
                    }

                    Camera camera = Camera.current;
                    StartExtrudeMode(camera);
                }

                Undo.RecordObject(this, "Modified Shape Height");
                extrusionPoints[1].position = extrusionPoints[0].position + direction * cleanValue;
                UpdateBaseShape();
            }
        }

        public override void Reset()
        {
            base.Reset();
            commitExtrusionAfterRelease = false;
            extrusionPoints             = new ExtrusionPoint[2];
            extrusionPoints[0].position = MathConstants.zeroVector3;
            extrusionPoints[1].position = MathConstants.zeroVector3;
            haveForcedDirection         = false;

            smearTextures   = true;
            polygons        = null;
            shapeEdges      = null;
            forceDragSource = null;
        }

        protected override bool StartEditMode(Camera camera)
        {
            if(!base.StartEditMode(camera))
            {
                return false;
            }

            extrusionPoints = new ExtrusionPoint[2];

            extrusionPoints[1].position = extrusionPoints[0].position = brushPosition;

            return true;
        }

        protected override void CreateControlIDs()
        {
            for(int i = 0; i < extrusionPoints.Length; i++)
            {
                int newControlID = GUIUtility.GetControlID(shapeBuilderCenterHash, FocusType.Passive);
                extrusionPoints[i].id = newControlID;
            }
        }

        public override bool Commit(Camera camera)
        {
            isFinished = true;
            CleanupGrid();

            if(polygons == null || polygons.Length == 0)
            {
                Cancel();

                return false;
            }

            if(!HaveExtrusion)
            {
                if(editMode != EditMode.ExtrudeShape)
                {
                    if(!StartExtrudeMode(camera))
                    {
                        Cancel();

                        return false;
                    }

                    UpdateBaseShape(false);

                    extrusionPoints[1].position = extrusionPoints[0].position + buildPlane.normal.normalized * DefaultHeight;
                }
            }

            if(!UpdateExtrudedShape())
            {
                Cancel();

                return false;
            }

            if(extrusionPoints.Length == 2)
            {
                DefaultHeight = Height;
            }

            EndCommit();

            return true;
        }

        protected void ClearPolygons()
        {
            polygons   = null;
            shapeEdges = null;
        }

        protected void UpdatePolygons(Vector3[] vertices, ShapePolygon[] newPolygons)
        {
            outlineVertices = vertices.ToArray();

            if(vertices != null)
            {
                for(int i = 0; i < outlineVertices.Length; i++)
                {
                    outlineVertices[i] -= brushPosition;
                }
            }

            polygons = newPolygons;
        }

        protected void GenerateBrushesFromPolygons(ShapeEdge[] edges = null, bool inGridSpace = true)
        {
            shapeEdges = edges;
            editMode   = EditMode.ExtrudeShape;
            GenerateBrushObjects(polygons.Length * (extrusionPoints.Length - 1), inGridSpace);
        }

        protected void UpdateBrushOperation()
        {
            if(forceCurrentCSGOperationType == INVALID_CSG_OPERATION_TYPE)
            {
                if(HaveHeight)
                {
                    bool invertedWorld = parentModel != null && (parentModel.Settings & ModelSettingsFlags.InvertedWorld) == ModelSettingsFlags.InvertedWorld;

                    CSGOperationType desiredOperation;

                    if(planeOnGeometry)
                    {
                        if(forceDragSource.HasValue && forceDragSource.Value != CSGOperationType.Additive)
                        {
                            desiredOperation = Height > 0 ? CSGOperationType.Subtractive : CSGOperationType.Additive;
                        }
                        else
                        {
                            desiredOperation = Height > 0 ? CSGOperationType.Additive : CSGOperationType.Subtractive;
                        }
                    }
                    else
                    {
                        desiredOperation = invertedWorld ? CSGOperationType.Subtractive : CSGOperationType.Additive;
                    }

                    if(currentCSGOperationType != desiredOperation)
                    {
                        currentCSGOperationType = desiredOperation;
                        UpdateOperationType(currentCSGOperationType);
                    }
                }
            }
        }

        protected bool UpdateExtrudedShape(bool registerUndo = true)
        {
            if(polygons == null || polygons.Length == 0)
            {
                return false;
            }

            bool failures          = false;
            bool modifiedHierarchy = false;

            if(HaveExtrusion)
            {
                UpdateBrushOperation();

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
                        Undo.RecordObjects(generatedGameObjects, "Extruded shape");
                    }

                    int brushIndex = 0;

                    for(int slice = 0; slice < extrusionPoints.Length - 1; slice++)
                    {
                        for(int p = 0; p < polygons.Length; p++)
                        {
                            CSGBrush brush = generatedBrushes[brushIndex];
                            brushIndex++;

                            if(!brush || !brush.gameObject)
                            {
                                continue;
                            }

                            Vector3 direction = haveForcedDirection ? forcedDirection : buildPlane.normal;

                            float distance = new CSGPlane(direction, extrusionPoints[slice].position).Distance(extrusionPoints[slice + 1].position);

                            if(float.IsInfinity(distance) || float.IsNaN(distance))
                            {
                                distance = 1.0f;
                            }

                            Matrix4x4 poly2dToWorldMatrix = brush.transform.worldToLocalMatrix * Matrix4x4.TRS(extrusionPoints[slice].position, Quaternion.FromToRotation(MathConstants.upVector3, buildPlane.normal), Vector3.one);

                            if(!CreateControlMeshForBrushIndex(brush, polygons[p], poly2dToWorldMatrix, distance, out ControlMesh _, out Shape _))
                            {
                                failures = true;

                                if(brush.gameObject.activeSelf)
                                {
                                    modifiedHierarchy = true;
                                    brush.gameObject.SetActive(false);
                                }

                                continue;
                            }

                            if(!brush.gameObject.activeSelf)
                            {
                                modifiedHierarchy = true;
                                brush.gameObject.SetActive(true);
                            }

                            brush.ControlMesh.SetDirty();

                            if(registerUndo)
                            {
                                EditorUtility.SetDirty(brush);
                            }
                        }
                    }
                }
            }
            else
            {
                if(generatedGameObjects != null)
                {
                    if(registerUndo)
                    {
                        Undo.RecordObjects(generatedGameObjects, "Extruded brush");
                    }

                    InternalCSGModelManager.SkipCheckForChanges = false;
                    int brushIndex = 0;

                    for(int slice = 0; slice < extrusionPoints.Length - 1; slice++)
                    {
                        for(int p = 0; p < polygons.Length; p++)
                        {
                            if(p >= generatedBrushes.Length)
                            {
                                continue;
                            }

                            CSGBrush brush = generatedBrushes[brushIndex];
                            brushIndex++;
                            brush.ControlMesh.SetDirty();

                            if(registerUndo)
                            {
                                EditorUtility.SetDirty(brush);
                            }
                        }
                    }

                    HideGenerateBrushes();
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

            if(shapeEdges != null && smearTextures)
            {
                CSGBrush lastBrush        = null;
                int      lastSurfaceIndex = -1;

                for(int slice = 0; slice < extrusionPoints.Length - 1; slice++)
                {
                    for(int se = 0; se < shapeEdges.Length; se++)
                    {
                        int brushIndex   = shapeEdges[se].polygonIndex + slice * shapeEdges.Length;
                        int surfaceIndex = shapeEdges[se].edgeIndex;

                        if(brushIndex < 0 || brushIndex >= generatedBrushes.Length || surfaceIndex == -1)
                        {
                            continue;
                        }

                        CSGBrush brush = generatedBrushes[brushIndex];

                        if(brush && brush.brushNodeID != CSGNode.InvalidNodeID)
                        {
                            if(lastBrush && lastBrush.brushNodeID != CSGNode.InvalidNodeID)
                            {
                                SurfaceUtility.CopyLastMaterial(brush, surfaceIndex, lastBrush, lastSurfaceIndex, false);
                            }
                            else
                            {
                                brush.Shape.TexGens[surfaceIndex].Translation   = Vector3.zero;
                                brush.Shape.TexGens[surfaceIndex].Scale         = Vector2.one;
                                brush.Shape.TexGens[surfaceIndex].RotationAngle = 0;
                            }

                            lastBrush        = brush;
                            lastSurfaceIndex = surfaceIndex;
                        }
                    }
                }
            }

            InternalCSGModelManager.RefreshMeshes();

            return !failures;
        }

        protected void PaintHeightMessage(Camera camera, Vector3 start, Vector3 end, Vector3 normal, float distance)
        {
            if(Mathf.Abs(distance) <= MathConstants.EqualityEpsilon)
            {
                return;
            }

            if(camera == null)
            {
                return;
            }

            Vector3 middlePoint = (end + start) * 0.5f;
            Vector3 screenPoint = camera.WorldToScreenPoint(middlePoint);

            if(screenPoint.z < 0)
            {
                return;
            }

            Vector2 textCenter2Da = HandleUtility.WorldToGUIPoint(middlePoint + normal * 10.0f);
            Vector2 textCenter2Db = HandleUtility.WorldToGUIPoint(middlePoint);
            Vector2 normal2D      = (textCenter2Db - textCenter2Da).normalized;

            Vector2 textCenter2D = textCenter2Db;
            textCenter2D += normal2D * (HOVER_TEXT_DISTANCE * 2);

            Ray     textCenterRay = HandleUtility.GUIPointToWorldRay(textCenter2D);
            Vector3 textCenter    = textCenterRay.origin + textCenterRay.direction * ((camera.farClipPlane + camera.nearClipPlane) * 0.5f);

            PaintUtility.DrawLine(middlePoint, textCenter, Color.black);
            PaintUtility.DrawDottedLine(middlePoint, textCenter, ColorSettings.SnappedEdges);
            PaintUtility.DrawScreenText(textCenter2D, $"Y: {Units.ToRoundedDistanceString(Mathf.Abs(distance))}");
        }

        public override AABB GetShapeBounds()
        {
            return GetShapeBounds(Quaternion.identity);
        }

        public AABB GetShapeBounds(Quaternion rotation)
        {
            AABB    bounds    = ShapeSettings.CalculateBounds(rotation, gridTangent, gridBinormal);
            Vector3 direction = haveForcedDirection ? forcedDirection : buildPlane.normal;

            if(editMode == EditMode.ExtrudeShape)
            {
                bounds.Extrude(rotation * (direction * Height));
            }

            return bounds;
        }

        private Vector3 GetHeightHandlePosition(SceneView sceneView, Vector3 point)
        {
            Ray      mouseRay      = HandleUtility.GUIPointToWorldRay(_heightPosition);
            CSGPlane alignedPlane  = new CSGPlane(sceneView.camera.transform.forward, point);
            Vector3  planePosition = alignedPlane.RayIntersection(mouseRay);

            Vector3 worldPosition = GeometryUtility.ProjectPointOnInfiniteLine(planePosition, brushPosition, _movePolygonDirection);

            return worldPosition;
        }

        public bool IsMouseOverShapePolygons(Matrix4x4 matrix)
        {
            Matrix4x4 inverseMatrix = matrix.inverse;
            Ray       mouseRay      = HandleUtility.GUIPointToWorldRay(Event.current.mousePosition);
            mouseRay.origin    = inverseMatrix.MultiplyPoint(mouseRay.origin);
            mouseRay.direction = inverseMatrix.MultiplyVector(mouseRay.direction);

            if(polygons == null)
            {
                return false;
            }

            for(int i = 0; i < polygons.Length; i++)
            {
                if(ShapePolygonUtility.IntersectsWithShapePolygon2D(polygons[i], mouseRay))
                {
                    return true;
                }
            }

            return false;
        }

        protected void GrabHeightHandle(SceneView sceneView, int index, bool ignoreFirstMouseUp = false)
        {
            Camera camera = sceneView.camera;

            if(camera == null)
            {
                return;
            }

            bool assume2DView = CSGSettings.Assume2DView(camera);

            _firstClick                       = ignoreFirstMouseUp;
            editMode                          = EditMode.ExtrudeShape;
            GUIUtility.hotControl             = extrusionPoints[index].id;
            GUIUtility.keyboardControl        = extrusionPoints[index].id;
            EditorGUIUtility.editingTextField = false;
            EditorGUIUtility.SetWantsMouseJumping(1);

            Vector3 surfaceDirection   = buildPlane.normal;
            Vector3 closestAxisForward = GeometryUtility.SnapToClosestAxis(-camera.transform.forward);
            Vector3 closestAxisArrow   = GeometryUtility.SnapToClosestAxis(surfaceDirection);
            Vector3 tangent;
            float   dot = Mathf.Abs(Vector3.Dot(closestAxisForward, closestAxisArrow));

            if(dot != 1)
            {
                Vector3 v1, v2;

                if(closestAxisForward.x == 0 && closestAxisForward.y == 0)
                {
                    v1 = new Vector3(1, 0, 0);
                    v2 = new Vector3(0, 1, 0);
                }
                else if(closestAxisForward.x == 0 && closestAxisForward.z == 0)
                {
                    v1 = new Vector3(1, 0, 0);
                    v2 = new Vector3(0, 0, 1);
                }
                else
                {
                    v1 = new Vector3(0, 1, 0);
                    v2 = new Vector3(0, 0, 1);
                }

                Vector3 backward = -camera.transform.forward;
                float   dot1     = Vector3.Dot(backward, v1);
                float   dot2     = Vector3.Dot(backward, v2);

                tangent = dot1 < dot2 ? v1 : v2;
            }
            else
            {
                tangent = GeometryUtility.SnapToClosestAxis(Vector3.Cross(surfaceDirection, -camera.transform.forward));
            }

            Vector3 normal = Vector3.Cross(surfaceDirection, tangent);

            if(assume2DView)
            {
                normal = -camera.transform.forward;
            }

            if(normal == MathConstants.zeroVector3)
            {
                normal = GeometryUtility.SnapToClosestAxis(-camera.transform.forward);
            }

            _movePlane = new CSGPlane(normal, extrusionPoints[index].position);

            if(!assume2DView && Mathf.Abs(_movePlane.Distance(camera.transform.position)) < 2.0f)
            {
                Vector3 newTangent = Vector3.Cross(normal, closestAxisForward);

                if(newTangent != MathConstants.zeroVector3)
                {
                    tangent    = newTangent;
                    normal     = Vector3.Cross(surfaceDirection, tangent);
                    _movePlane = new CSGPlane(normal, extrusionPoints[index].position);
                }
            }

            _movePolygonDirection = haveForcedDirection ? forcedDirection : buildPlane.normal;

            if(!isFinished)
            {
                CSGGrid.SetForcedGrid(camera, _movePlane);
            }

            CSGPlane plane = new CSGPlane(buildPlane.normal, extrusionPoints[index].position);

            _heightPosition     = Event.current.mousePosition;
            _heightHandleOffset = plane.Distance(GetHeightHandlePosition(sceneView, extrusionPoints[index].position)) * _movePolygonDirection;

            if(float.IsInfinity(_heightHandleOffset.x) || float.IsNaN(_heightHandleOffset.x) ||
               float.IsInfinity(_heightHandleOffset.y) || float.IsNaN(_heightHandleOffset.y) ||
               float.IsInfinity(_heightHandleOffset.z) || float.IsNaN(_heightHandleOffset.z))
            {
                _heightHandleOffset = Vector3.zero;
            }
        }

        protected void CenterExtrusionPoints(CSGPlane plane)
        {
            if(!HaveHeight)
            {
                return;
            }

            Vector3 center = GetShapeBounds().Center;
            brushPosition = plane.Project(center);
            float height0 = plane.Distance(extrusionPoints[0].position);
            float height1 = plane.Distance(extrusionPoints[1].position);
            extrusionPoints[0].position = brushPosition + plane.normal * height0;
            extrusionPoints[1].position = brushPosition + plane.normal * height1;
        }

        protected void HandleHeightHandles(SceneView sceneView, Rect sceneRect, bool showHeightValue)
        {
            Camera camera = sceneView.camera;

            for(int p = 0; p < extrusionPoints.Length; p++)
            {
                EventType type = Event.current.GetTypeForControl(extrusionPoints[p].id);

                switch(type)
                {
                    case EventType.Repaint:
                    {
                        if(SceneDragToolManager.IsDraggingObjectInScene)
                        {
                            break;
                        }

                        bool      isSelected = extrusionPoints[p].id == GUIUtility.keyboardControl;
                        Color     temp       = Handles.color;
                        Matrix4x4 origMatrix = Handles.matrix;

                        Handles.matrix = MathConstants.identityMatrix;
                        Quaternion rotation = camera.transform.rotation;

                        SelectState state = SelectState.None;

                        if(isSelected)
                        {
                            state |= SelectState.Selected;
                            state |= SelectState.Hovering;
                        }
                        else if(HandleUtility.nearestControl == extrusionPoints[p].id)
                        {
                            state |= SelectState.Hovering;
                        }

                        if(polygons != null && outlineVertices.Length >= 3)
                        {
                            Color wireframeColor    = ColorSettings.WireframeOutline;
                            Color topWireframeColor = ColorSettings.WireframeOutline;

                            if(!shapeIsValid)
                            {
                                wireframeColor = Color.red;
                            }

                            Color surfaceColor = ColorSettings.ShapeDrawingFill;

                            if(GUIUtility.hotControl == extrusionPoints[p].id)
                            {
                                topWireframeColor = ColorSettings.BoundsEdgeHover;

                                surfaceColor = ColorSettings.PolygonInnerStateColor[(int)(SelectState.Selected | SelectState.Hovering)];

                                surfaceColor.a *= 0.5f;
                            }
                            else if(GUIUtility.hotControl == 0 && HandleUtility.nearestControl == extrusionPoints[p].id)
                            {
                                topWireframeColor =  ColorSettings.BoundsEdgeHover;
                                surfaceColor      =  ColorSettings.PolygonInnerStateColor[(int)SelectState.Selected];
                                surfaceColor.a    *= 0.5f;
                            }

                            Matrix4x4 poly2dToWorldMatrix = Matrix4x4.TRS(extrusionPoints[p].position, Quaternion.FromToRotation(MathConstants.upVector3, buildPlane.normal), MathConstants.oneVector3);

                            for(int i = 0; i < polygons.Length; i++)
                            {
                                PaintUtility.DrawPolygon(poly2dToWorldMatrix, polygons[i].vertices, surfaceColor);
                            }

                            poly2dToWorldMatrix = Matrix4x4.TRS(extrusionPoints[p].position, Quaternion.identity, MathConstants.oneVector3);

                            for(int i = 1; i < outlineVertices.Length; i++)
                            {
                                PaintUtility.DrawLine(poly2dToWorldMatrix, outlineVertices[i - 1], outlineVertices[i], GUIConstants.OLD_LINE_SCALE, topWireframeColor);
                                PaintUtility.DrawDottedLine(poly2dToWorldMatrix, outlineVertices[i - 1], outlineVertices[i], topWireframeColor);
                            }

                            PaintUtility.DrawLine(poly2dToWorldMatrix, outlineVertices[outlineVertices.Length - 1], outlineVertices[0], GUIConstants.OLD_LINE_SCALE, topWireframeColor);
                            PaintUtility.DrawDottedLine(poly2dToWorldMatrix, outlineVertices[outlineVertices.Length - 1], outlineVertices[0], topWireframeColor);

                            if(p > 0)
                            {
                                Vector3 prevOffset = extrusionPoints[p - 1].position;

                                Matrix4x4 prevPoly2dToWorldMatrix = Matrix4x4.TRS(prevOffset, Quaternion.identity, MathConstants.oneVector3);

                                for(int i = 0; i < outlineVertices.Length; i++)
                                {
                                    Vector3 from = prevPoly2dToWorldMatrix.MultiplyPoint(outlineVertices[i]);
                                    Vector3 to   = poly2dToWorldMatrix.MultiplyPoint(outlineVertices[i]);
                                    PaintUtility.DrawLine(from, to, GUIConstants.OLD_LINE_SCALE, wireframeColor);
                                    PaintUtility.DrawDottedLine(from, to, wireframeColor);
                                }
                            }
                        }

                        Color color = ColorSettings.PolygonInnerStateColor[(int)state];

                        if(!shapeIsValid)
                        {
                            color = Color.red;
                        }

                        float handleSize       = CSGHandleUtility.GetHandleSize(extrusionPoints[p].position);
                        float scaledHandleSize = handleSize * GUIConstants.HANDLE_SCALE;

                        if(p > 0)
                        {
                            PaintUtility.DrawDottedLine(extrusionPoints[p - 1].position, extrusionPoints[p].position, color);
                        }

                        Handles.color = color;

                        PaintUtility.SquareDotCap(extrusionPoints[p].id, extrusionPoints[p].position, rotation, scaledHandleSize);

                        Vector3 direction = haveForcedDirection ? forcedDirection : buildPlane.normal;

                        float distance = new CSGPlane(direction, extrusionPoints[p].position).Distance(extrusionPoints[1 - p].position);

                        if(distance <= MathConstants.DistanceEpsilon)
                        {
                            PaintUtility.DrawArrowCap(extrusionPoints[p].position, direction, HandleUtility.GetHandleSize(extrusionPoints[p].position));
                        }

                        if(distance > -MathConstants.DistanceEpsilon)
                        {
                            PaintUtility.DrawArrowCap(extrusionPoints[p].position, -direction, HandleUtility.GetHandleSize(extrusionPoints[p].position));
                        }

                        Handles.matrix = origMatrix;
                        Handles.color  = temp;

                        if(p > 0 && showHeightValue)
                        {
                            float length = GetSegmentLength(extrusionPoints[p].position, extrusionPoints[p - 1].position, direction);

                            PaintHeightMessage(camera, extrusionPoints[p - 1].position, extrusionPoints[p].position, gridTangent, length);
                        }

                        break;
                    }

                    case EventType.Layout:
                    {
                        if(Tools.viewTool != ViewTool.None && Tools.viewTool != ViewTool.Pan)
                        {
                            break;
                        }

                        Matrix4x4 poly2dToWorldMatrix = Matrix4x4.TRS(extrusionPoints[p].position, Quaternion.FromToRotation(MathConstants.upVector3, buildPlane.normal), MathConstants.oneVector3);
                        bool      forceOverHandle     = IsMouseOverShapePolygons(poly2dToWorldMatrix);

                        HandleUtility.AddControl(extrusionPoints[p].id, forceOverHandle ? 3.0f : float.PositiveInfinity);

                        Matrix4x4 origMatrix = Handles.matrix;
                        Handles.matrix = MathConstants.identityMatrix;
                        float handleSize       = CSGHandleUtility.GetHandleSize(extrusionPoints[p].position);
                        float scaledHandleSize = handleSize * GUIConstants.HANDLE_SCALE * HANDLE_EXTENSION;
                        float distanceToCircle = HandleUtility.DistanceToCircle(extrusionPoints[p].position, scaledHandleSize);

                        HandleUtility.AddControl(extrusionPoints[p].id, distanceToCircle);

                        Vector3 direction = haveForcedDirection ? forcedDirection : buildPlane.normal;
                        float   distance  = new CSGPlane(direction, extrusionPoints[p].position).Distance(extrusionPoints[1 - p].position);

                        if(distance <= MathConstants.DistanceEpsilon)
                        {
                            PaintUtility.AddArrowCapControl(extrusionPoints[p].id, extrusionPoints[p].position, direction, HandleUtility.GetHandleSize(extrusionPoints[p].position));
                        }

                        if(distance > -MathConstants.DistanceEpsilon)
                        {
                            PaintUtility.AddArrowCapControl(extrusionPoints[p].id, extrusionPoints[p].position, -direction, HandleUtility.GetHandleSize(extrusionPoints[p].position));
                        }

                        if(generatedGameObjects != null && generatedGameObjects.Length > 0)
                        {
                            for(int g = generatedGameObjects.Length - 1; g >= 0; g--)
                            {
                                if(generatedGameObjects[g])
                                {
                                    continue;
                                }

                                ArrayUtility.RemoveAt(ref generatedGameObjects, g);
                            }

                            if(generatedGameObjects == null || generatedGameObjects.Length == 0)
                            {
                                Cancel();
                            }
                        }

                        Handles.matrix = origMatrix;

                        break;
                    }

                    case EventType.MouseDown:
                    {
                        if(!sceneRect.Contains(Event.current.mousePosition))
                        {
                            break;
                        }

                        if((Tools.viewTool != ViewTool.None && Tools.viewTool != ViewTool.Pan) || Event.current.modifiers != EventModifiers.None)
                        {
                            break;
                        }

                        if(GUIUtility.hotControl == 0 && HandleUtility.nearestControl == extrusionPoints[p].id && Event.current.button == 0)
                        {
                            if(editMode != EditMode.ExtrudeShape && !StartExtrudeMode(camera))
                            {
                                Cancel();
                            }
                            else
                            {
                                UpdateBaseShape(false);
                                _dragPositionStart = extrusionPoints[p].position;
                                GrabHeightHandle(sceneView, p);
                                BeginExtrusion();
                                Event.current.Use();
                            }
                        }

                        break;
                    }

                    case EventType.MouseDrag:
                    case EventType.MouseMove:
                    {
                        if(Tools.viewTool != ViewTool.None && Tools.viewTool != ViewTool.Pan)
                        {
                            break;
                        }

                        if(GUIUtility.hotControl == extrusionPoints[p].id)
                        {
                            Undo.RecordObject(this, "Extrude shape");
                            _heightPosition += Event.current.delta;

                            Vector3 worldPosition = GetHeightHandlePosition(sceneView, extrusionPoints[p].position)
                              - _heightHandleOffset;

                            if(float.IsInfinity(worldPosition.x) || float.IsNaN(worldPosition.x) ||
                               float.IsInfinity(worldPosition.y) || float.IsNaN(worldPosition.y) ||
                               float.IsInfinity(worldPosition.z) || float.IsNaN(worldPosition.z))
                            {
                                worldPosition = extrusionPoints[p].position;
                            }

                            ResetVisuals();

                            if(raySnapFunction != null)
                            {
                                worldPosition = raySnapFunction(camera, worldPosition, new Ray(brushPosition, _movePolygonDirection), ref visualSnappedEdges, out CSGBrush snappedOnBrush);

                                visualSnappedBrush = snappedOnBrush;
                            }

                            visualSnappedGrid = CSGGrid.FindAllGridEdgesThatTouchPoint(camera, worldPosition);

                            extrusionPoints[p].position = GeometryUtility.ProjectPointOnInfiniteLine(worldPosition, brushPosition, _movePolygonDirection);

                            if(p == 0)
                            {
                                MoveShape(extrusionPoints[0].position - _dragPositionStart);
                                UpdateBaseShape();
                            }

                            UpdateBrushPosition();
                            UpdateExtrudedShape();

                            GUI.changed = true;
                            Event.current.Use();
                        }

                        break;
                    }
                    case EventType.MouseUp:
                    {
                        forceDragHandle = false;

                        if(GUIUtility.hotControl == extrusionPoints[p].id && Event.current.button == 0 && (Tools.viewTool == ViewTool.None || Tools.viewTool == ViewTool.Pan))
                        {
                            EndExtrusion();

                            if(_firstClick)
                            {
                                _firstClick = false;

                                break;
                            }

                            GUIUtility.hotControl             = 0;
                            GUIUtility.keyboardControl        = 0;
                            EditorGUIUtility.editingTextField = false;
                            Event.current.Use();

                            ResetVisuals();
                            CleanupGrid();

                            if(!HaveExtrusion)
                            {
                                RevertToEditVertices();
                            }

                            if(commitExtrusionAfterRelease)
                            {
                                CSGBrush[] prevGeneratedBrushes = generatedBrushes;
                                Commit(camera);

                                // did we switch to edit mode?
                                if(EditModeManager.EditMode == ToolEditMode.Edit && prevGeneratedBrushes != null)
                                {
                                    EditModeManager.UpdateTool();
                                    EditModeManager.UpdateSelection(true);
                                    EditModeMeshEdit tool = EditModeManager.ActiveTool as EditModeMeshEdit;

                                    if(tool)
                                    {
                                        CSGBrush brush        = prevGeneratedBrushes[0];
                                        int      polygonCount = brush.ControlMesh.Polygons.Length;
                                        tool.SelectPolygon(brush, polygonCount - 2); // select front most polygon
                                    }
                                }
                            }
                        }

                        break;
                    }
                }
            }

            EventType shapeType = Event.current.GetTypeForControl(shapeId);
            HandleKeyboard(shapeType);
        }

        public override void HandleEvents(SceneView sceneView, Rect sceneRect)
        {
            base.HandleEvents(sceneView, sceneRect);

            if(GUIUtility.hotControl == 0 && Event.current.type == EventType.MouseUp && Event.current.modifiers == EventModifiers.None && !mouseIsDragging && Event.current.button == 0 && (Tools.viewTool == ViewTool.None || Tools.viewTool == ViewTool.Pan))
            {
                ResetVisuals();
                Event.current.Use();

                if(editMode == EditMode.ExtrudeShape)
                {
                    Camera camera = sceneView.camera;
                    Commit(camera);
                }
                else
                {
                    PerformDeselectAll();
                }
            }
        }

        internal virtual void BeginExtrusion()
        {
        }

        internal virtual void EndExtrusion()
        {
        }

        internal abstract bool StartExtrudeMode(Camera camera, bool showErrorMessage = true);

        internal abstract bool CreateControlMeshForBrushIndex(CSGBrush brush, ShapePolygon polygon, Matrix4x4 localToWorld, float height, out ControlMesh newControlMesh, out Shape newShape);
    }
}
