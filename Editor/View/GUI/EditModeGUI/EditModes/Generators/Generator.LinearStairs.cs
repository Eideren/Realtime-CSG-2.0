using System;
using UnityEditor;
using UnityEngine;
using InternalRealtimeCSG;
using RealtimeCSG.Helpers;
using RealtimeCSG.Legacy;
using RealtimeCSG.Components;

// TODO:
//	test on non straight walls/floors
//  optional one step less in height (so that the last step is on the wall it's put against
//	make it possible to move steps in slope direction
//	update key layout on website
//	update documentation
//	create icons
//	fix snapping on edges of other brushes
//	fix shouldn't snap on backfaced edges of other brushes
//	fix remaining UNDO issues

namespace RealtimeCSG
{
    [Serializable]
    internal sealed class GeneratorLinearStairs : GeneratorBase, IGenerator
    {
        private const int kMaxPoints = 3;

        protected override IShapeSettings ShapeSettings
        {
            get => settings;
        }

        protected override bool IgnoreForcedGridForTangents
        {
            get => true;
        }

        [SerializeField] private GeneratorLinearStairsSettings settings = new GeneratorLinearStairsSettings();

        [NonSerialized] private Vector3 worldPosition;
        [NonSerialized] private Vector3 prevWorldPosition;
        [NonSerialized] private CSGPlane? hoverDefaultPlane;

        [NonSerialized] private CSGPlane[] firstSnappedPlanes = null;
        [NonSerialized] private Vector3[] firstSnappedEdges = null;
        [NonSerialized] private CSGBrush firstSnappedBrush = null;

        [SerializeField] private Vector3 widthDirection;
        [SerializeField] private Vector3 lengthDirection;
        [SerializeField] private Vector3 heightDirection;

        internal static readonly int TopSlopLeftHash = "TopSlopLeft".GetHashCode();
        internal static readonly int TopSlopRightHash = "TopSlopRight".GetHashCode();
        internal static readonly int BottomSlopLeftHash = "BottomSlopLeft".GetHashCode();
        internal static readonly int BottomSlopRightHash = "BottomSlopRight".GetHashCode();

        private int topSlopeLeftID;
        private int topSlopeRightID;

        private int bottomSlopeLeftID;
        private int bottomSlopeRightID;

#region Settings

        public float StepDepth
        {
            get => settings.StepDepth;
            set
            {
                if(settings.StepDepth == value)
                {
                    return;
                }

                Undo.RecordObject(this, "Modified Linear Stairs Step Depth");
                settings.SetStepDepth(value);
                UpdateBaseShape(true);
            }
        }

        public float StepHeight
        {
            get => settings.StepHeight;
            set
            {
                if(settings.StepHeight == value)
                {
                    return;
                }

                Undo.RecordObject(this, "Modified Linear Stairs Step Height");
                settings.SetStepHeight(value);
                UpdateBaseShape(true);
            }
        }

        public int TotalSteps
        {
            get => settings.TotalSteps;
            set
            {
                if(settings.TotalSteps == value)
                {
                    return;
                }

                Undo.RecordObject(this, "Modified Linear Stairs Total Steps");
                settings.SetTotalSteps(value);
                UpdateBaseShape(true);
            }
        }

        public float StairsWidth
        {
            get => settings.StairsWidth;
            set
            {
                if(settings.StairsWidth == value)
                {
                    return;
                }

                Undo.RecordObject(this, "Modified Linear Stairs Width");
                settings.SetStairsWidth(value);

                if(editMode == EditMode.ExtrudeShape || editMode == EditMode.EditShape)
                {
                    Vector3 newPosition = settings.vertices[0] + settings.StairsDepth * lengthDirection
                      + value * widthDirection;

                    settings.SetPoint(1, newPosition);
                    UpdateBaseShape(true);
                }
            }
        }

        public float StairsDepth
        {
            get => settings.StairsDepth;
            set
            {
                if(settings.StairsDepth == value)
                {
                    return;
                }

                Undo.RecordObject(this, "Modified Linear Stairs Depth");
                settings.SetStairsDepth(value);

                if(editMode == EditMode.ExtrudeShape ||
                   editMode == EditMode.EditShape)
                {
                    Vector3 newPosition = settings.vertices[0] + value * lengthDirection
                      + settings.StairsWidth * widthDirection;

                    settings.SetPoint(1, newPosition);
                    UpdateBaseShape(true);
                }
            }
        }

        public float StairsHeight
        {
            get => settings.StairsHeight;
            set
            {
                if(settings.StairsDepth == value)
                {
                    return;
                }

                Undo.RecordObject(this, "Modified Linear Stairs Height");
                settings.SetStairsHeight(value);

                if(editMode == EditMode.ExtrudeShape || editMode == EditMode.EditShape)
                {
                    Vector3 newPosition = settings.vertices[1] + value * heightDirection;
                    settings.SetPoint(2, newPosition);
                    UpdateBaseShape(true);
                }
            }
        }

        public float ExtraDepth
        {
            get
            {
                float floatTotalSteps =
                    Mathf.Max(0, settings.StairsHeight - settings.ExtraHeight) / settings.StepHeight;

                return Mathf.Max(0, settings.StairsDepth - floatTotalSteps * settings.StepDepth);
            }
            set
            {
                if(settings.ExtraDepth == value)
                {
                    return;
                }

                Undo.RecordObject(this, "Modified Linear Stairs Extra Depth");
                settings.SetExtraDepth(value);

                if(editMode != EditMode.CreatePlane &&
                   settings.vertices.Length > 1)
                {
                    UpdateBaseShape(true);
                }
            }
        }

        public float ExtraHeight
        {
            get => settings.ExtraHeight;
            set
            {
                if(settings.ExtraHeight == value)
                {
                    return;
                }

                Undo.RecordObject(this, "Modified Linear Stairs Extra Height");
                settings.SetExtraHeight(value);

                if(editMode != EditMode.CreatePlane &&
                   settings.vertices.Length > 1)
                {
                    UpdateBaseShape(true);
                }
            }
        }

        public StairsBottom StairsBottom
        {
            get => settings.StairsBottom;
            set
            {
                if(settings.StairsBottom == value)
                {
                    return;
                }

                Undo.RecordObject(this, "Modified Stairs Bottom");
                settings.StairsBottom = value;

                if(editMode != EditMode.CreatePlane &&
                   settings.vertices.Length > 1)
                {
                    UpdateBaseShape(true);
                }
            }
        }

#endregion

#region Glue

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
            hoverDefaultPlane  = null;
            firstSnappedPlanes = null;
            firstSnappedEdges  = null;
            firstSnappedBrush  = null;
        }

        public bool HotKeyReleased()
        {
            Camera camera = Camera.current;
            ResetVisuals();

            CSGGrid.SetForcedGrid(
                camera, new CSGPlane(fromGridQuaternion * MathConstants.upVector3, settings.bounds.Min)
            );

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
                        Cancel();

                        return false;
                    }

                    if(settings.vertices.Length == 2)
                    {
                        settings.AddPoint(worldPosition);
                    }

                    if(settings.vertices.Length == 3)
                    {
                        settings.bounds = GetShapeBounds();
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

#endregion

#region Orientation, Location and Bounds

        protected override void MoveShape(Vector3 offset)
        {
            settings.MoveShape(offset);
        }

        public Quaternion GetPlaneRotation()
        {
            Vector3 xznormal = buildPlane.normal;

            if(xznormal.y < 0.5f)
            {
                xznormal.y = 0;
                xznormal.Normalize();

                return Quaternion.LookRotation(xznormal, MathConstants.upVector3);
            }

            return fromGridQuaternion;
        }

        public Quaternion GetWorldToLocalRotation()
        {
            Vector3 xznormal = buildPlane.normal;

            if(xznormal.y < 0.5f)
            {
                xznormal.y = 0;
                xznormal.Normalize();

                return Quaternion.Inverse(Quaternion.LookRotation(xznormal, MathConstants.upVector3));
            }

            return toGridQuaternion;
        }

        public override AABB GetShapeBounds()
        {
            Quaternion worldToLocalRotation = GetWorldToLocalRotation();

            return GetShapeBounds(worldToLocalRotation);
        }

        public AABB GetShapeBounds(Quaternion rotation)
        {
            if(editMode == EditMode.ExtrudeShape ||
               editMode == EditMode.EditShape)
            {
                return settings.bounds;
            }

            AABB bounds = ShapeSettings.CalculateBounds(rotation, gridTangent, gridBinormal);

            if(settings.vertices.Length < 3)
            {
                bounds.Extend(rotation * worldPosition);
            }

            return bounds;
        }

#endregion

        public bool OnShowGUI(bool isSceneGUI)
        {
            return GeneratorLinearStairsGUI.OnShowGUI(this, isSceneGUI);
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
                CSGPlane localPlane = GeometryUtility.InverseTransformPlane(
                    firstSnappedBrush.transform.localToWorldMatrix, worldPlane
                );

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

#region Paint Helpers

        private void PaintSquare()
        {
            if(settings.vertices.Length < 1)
            {
                return;
            }

            Vector3 corner1          = settings.vertices[0];
            Vector3 corner2          = settings.vertices.Length >= 2 ? settings.vertices[1] : worldPosition;
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
            Quaternion worldToLocalRotation = GetWorldToLocalRotation();
            Quaternion localToWorldRotation = Quaternion.Inverse(worldToLocalRotation);
            AABB       localBounds          = GetShapeBounds(worldToLocalRotation);

            Vector3[] volume = new Vector3[8];
            BoundsUtilities.GetBoundsCornerPoints(localBounds, volume);

            Color     boundOutlinesColor = ColorSettings.MeshEdgeOutline;
            Matrix4x4 localToWorld       = Matrix4x4.TRS(Vector3.zero, localToWorldRotation, Vector3.one);

            PaintUtility.DrawDottedLines(
                localToWorld, volume, BoundsUtilities.AABBLineIndices, boundOutlinesColor, 4.0f
            );

            PaintUtility.DrawLines(
                localToWorld, volume, BoundsUtilities.AABBLineIndices, GUIConstants.OLD_LINE_SCALE, boundOutlinesColor
            );

            PaintUtility.RenderBoundsSizes(
                worldToLocalRotation, localToWorldRotation, camera, volume, Color.white, Color.white, Color.white, true
              , true, true
            );
        }

        private void PaintShape(SceneView sceneView, int id)
        {
            Camera     camera   = sceneView.camera;
            Quaternion rotation = camera.transform.rotation;

            Color     temp       = Handles.color;
            Matrix4x4 origMatrix = Handles.matrix;

            {
                Handles.matrix = MathConstants.identityMatrix;

                Handles.color = ColorSettings.PointInnerStateColor[3];

                {
                    float handleSize       = CSG_HandleUtility.GetHandleSize(worldPosition);
                    float scaledHandleSize = handleSize * GUIConstants.HANDLE_SCALE;
                    PaintUtility.SquareDotCap(id, worldPosition, rotation, scaledHandleSize);
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
            }

            Handles.matrix = origMatrix;
            Handles.color  = temp;
        }

#endregion

        public override bool Commit(Camera camera)
        {
            isFinished = true;
            CleanupGrid();

            if(!UpdateBaseShape(true))
            {
                Cancel();

                return false;
            }

            EndCommit();

            return true;
        }

#region Events

        protected override void CreateControlIDs()
        {
            base.CreateControlIDs();

            //if (Event.current.type == EventType.Layout)
            {
                topSlopeLeftID  = GUIUtility.GetControlID(TopSlopLeftHash, FocusType.Keyboard);
                topSlopeRightID = GUIUtility.GetControlID(TopSlopRightHash, FocusType.Keyboard);

                bottomSlopeLeftID  = GUIUtility.GetControlID(BottomSlopLeftHash, FocusType.Keyboard);
                bottomSlopeRightID = GUIUtility.GetControlID(BottomSlopRightHash, FocusType.Keyboard);

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

        protected override void HandleEditShapeEvents(SceneView sceneView, Rect sceneRect)
        {
            Camera    camera     = sceneView.camera;
            Matrix4x4 origMatrix = Handles.matrix;
            Color     origColor  = Handles.color;
            Handles.matrix = Matrix4x4.identity;

            if(settings.vertices.Length < kMaxPoints)
            {
                if(editMode == EditMode.ExtrudeShape ||
                   editMode == EditMode.EditShape)
                {
                    editMode = EditMode.CreatePlane;
                }
            }

            EditorGUI.BeginChangeCheck();

            {
                Quaternion worldToLocalRotation = GetWorldToLocalRotation();
                Quaternion localToWorldRotation = Quaternion.Inverse(worldToLocalRotation);
                AABB       newBounds            = settings.bounds;

                widthDirection  = /*worldToLocalRotation * fromGridQuaternion */ MathConstants.rightVector3;
                heightDirection = /*worldToLocalRotation * fromGridQuaternion */ MathConstants.upVector3;
                lengthDirection = /*worldToLocalRotation * fromGridQuaternion */ MathConstants.forwardVector3;

                float lengthOffset = ExtraDepth;
                float heightOffset = ExtraHeight;

                EditorGUI.BeginChangeCheck();

                {
                    newBounds = CSGHandles.Box(camera, newBounds, worldToLocalRotation, false);
                }

                if(EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(this, "Modified Bounds");
                    settings.SetBounds(newBounds);
                }

                float width  = settings.StairsWidth;
                float height = settings.StairsHeight;
                float length = settings.StairsDepth;

                //var currentModel = parentModel ? parentModel : SelectionUtility.LastUsedModel;
                //var modelRotation = Quaternion.Inverse(currentModel.transform.rotation);

                ignoreModelRotation = true;
                brushRotation       = GetPlaneRotation();

                brushPosition = localToWorldRotation *
                    settings.bounds.Min;

                SnapMode activeSnappingMode = CSGSettings.ActiveSnappingMode;

                int nearestControl = HandleUtility.nearestControl;
                int hotControl     = GUIUtility.hotControl;

                Matrix4x4 localToWorld = Matrix4x4.TRS(brushPosition, localToWorldRotation, Vector3.one);
                Matrix4x4 worldToLocal = Matrix4x4.Inverse(localToWorld);

                Vector3 topFrom = new Vector3(0, height, lengthOffset);
                Vector3 topTo   = new Vector3(width, height, lengthOffset);

                Vector3 bottomFrom = new Vector3(0, heightOffset, length);
                Vector3 bottomTo   = new Vector3(width, heightOffset, length);

                SelectState topSelectState = hotControl == topSlopeLeftID || hotControl == topSlopeRightID
                    ?
                    SelectState.Selected
                    :
                    nearestControl == topSlopeLeftID || nearestControl == topSlopeRightID
                        ? SelectState.Hovering
                        : SelectState.None;

                Color topColor = ColorSettings.PointInnerStateColor[(int)topSelectState];

                PaintUtility.DrawLine(localToWorld, topFrom, topTo, GUIConstants.OLD_THICK_LINE_SCALE, topColor);

                SelectState bottomSelectState = hotControl == bottomSlopeLeftID || hotControl == bottomSlopeRightID
                    ?
                    SelectState.Selected
                    :
                    nearestControl == bottomSlopeLeftID || nearestControl == bottomSlopeRightID
                        ? SelectState.Hovering
                        : SelectState.None;

                Color bottomColor = ColorSettings.PointInnerStateColor[(int)bottomSelectState];
                PaintUtility.DrawLine(localToWorld, bottomFrom, bottomTo, GUIConstants.OLD_THICK_LINE_SCALE, bottomColor);

                PaintUtility.DrawLine(
                    localToWorld, topFrom, bottomFrom, GUIConstants.OLD_THICK_LINE_SCALE, ColorSettings.MeshEdgeOutline
                );

                PaintUtility.DrawLine(
                    localToWorld, topTo, bottomTo, GUIConstants.OLD_THICK_LINE_SCALE, ColorSettings.MeshEdgeOutline
                );

                PaintUtility.DrawDottedLine(localToWorld, topFrom, bottomFrom, ColorSettings.MeshEdgeOutline, 4.0f);
                PaintUtility.DrawDottedLine(localToWorld, topTo, bottomTo, ColorSettings.MeshEdgeOutline, 4.0f);

                const float kSlopeDotSize = 0.3f;

                settings.CalcTotalSteps();

                {
                    Vector3 topDirection = localToWorld.MultiplyVector(MathConstants.forwardVector3);
                    Handles.color = topColor;

                    EditorGUI.BeginChangeCheck();

                    {
                        Vector3 point = localToWorld.MultiplyPoint(new Vector3(width * 0.25f, height, lengthOffset));
                        float   size  = HandleUtility.GetHandleSize(point) * kSlopeDotSize;

                        Vector3 temp = CSGSlider1D.Do(
                            camera, topSlopeLeftID, point, topDirection, size, CSGHandles.CircleDotCap
                          , activeSnappingMode, null, null, null
                        );

                        lengthOffset = worldToLocal.MultiplyPoint(temp).z;
                    }

                    if(EditorGUI.EndChangeCheck())
                    {
                        Undo.RecordObject(this, "Modified Length");
                        settings.SetExtraDepth(lengthOffset);
                    }

                    EditorGUI.BeginChangeCheck();

                    {
                        Vector3 point = localToWorld.MultiplyPoint(new Vector3(width * 0.75f, height, lengthOffset));
                        float   size  = HandleUtility.GetHandleSize(point) * kSlopeDotSize;

                        Vector3 temp = CSGSlider1D.Do(
                            camera, topSlopeRightID, point, topDirection, size, CSGHandles.CircleDotCap
                          , activeSnappingMode, null, null, null
                        );

                        lengthOffset = worldToLocal.MultiplyPoint(temp).z;
                    }

                    if(EditorGUI.EndChangeCheck())
                    {
                        Undo.RecordObject(this, "Modified Length");
                        settings.SetExtraDepth(lengthOffset);
                    }
                }

                {
                    Vector3 bottomDirection = localToWorld.MultiplyVector(MathConstants.upVector3);

                    Handles.color = bottomColor;
                    EditorGUI.BeginChangeCheck();

                    {
                        Vector3 point = localToWorld.MultiplyPoint(new Vector3(width * 0.25f, heightOffset, length));
                        float   size  = HandleUtility.GetHandleSize(point) * kSlopeDotSize;

                        Vector3 temp = CSGSlider1D.Do(
                            camera, bottomSlopeLeftID, point, bottomDirection, size, CSGHandles.CircleDotCap
                          , activeSnappingMode, null, null, null
                        );

                        heightOffset = worldToLocal.MultiplyPoint(temp).y;
                    }

                    if(EditorGUI.EndChangeCheck())
                    {
                        Undo.RecordObject(this, "Modified Height");
                        settings.SetExtraHeight(heightOffset);
                    }

                    EditorGUI.BeginChangeCheck();

                    {
                        Vector3 point = localToWorld.MultiplyPoint(new Vector3(width * 0.75f, heightOffset, length));
                        float   size  = HandleUtility.GetHandleSize(point) * kSlopeDotSize;

                        Vector3 temp = CSGSlider1D.Do(
                            camera, bottomSlopeRightID, point, bottomDirection, size, CSGHandles.CircleDotCap
                          , activeSnappingMode, null, null, null
                        );

                        heightOffset = worldToLocal.MultiplyPoint(temp).y;
                    }

                    if(EditorGUI.EndChangeCheck())
                    {
                        Undo.RecordObject(this, "Modified Height");
                        settings.SetExtraHeight(heightOffset);
                    }
                }
            }

            if(EditorGUI.EndChangeCheck())
            {
                UpdateBaseShape(true);
            }

            Handles.matrix = origMatrix;
            Handles.color  = origColor;
        }

        private void UpdateSizes()
        {
            if(settings.vertices.Length == 0)
            {
                return;
            }

            Quaternion worldToLocalRotation = GetWorldToLocalRotation();
            Quaternion localToWorldRotation = Quaternion.Inverse(worldToLocalRotation);
            AABB       localBounds          = GetShapeBounds(worldToLocalRotation);

            widthDirection  = /*worldToLocalRotation * fromGridQuaternion */ MathConstants.rightVector3;
            heightDirection = /*worldToLocalRotation * fromGridQuaternion */ MathConstants.upVector3;
            lengthDirection = /*worldToLocalRotation * fromGridQuaternion */ MathConstants.forwardVector3;

            settings.SetBounds(localBounds);

            //var currentModel = parentModel ? parentModel : SelectionUtility.LastUsedModel;
            //var modelRotation = Quaternion.Inverse(currentModel.transform.rotation);

            ignoreModelRotation = true;
            brushRotation       = GetPlaneRotation(); // * fromGridQuaternion;
            brushPosition       = localToWorldRotation * localBounds.Min;
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

                    if(!assume2DView && !havePlane &&
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
                          , generatedBrushes, true
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
                }
                else
                {
                    Ray mouseRay = HandleUtility.GUIPointToWorldRay(Event.current.mousePosition);

                    if(settings.vertices.Length == 2)
                    {
                        Vector3 startPoint = settings.vertices[1];

                        Vector3 forward = camera.transform.forward;

                        if(Vector3.Dot(forward, gridBinormal) < Vector3.Dot(forward, gridTangent))
                        {
                            hoverBuildPlane = new CSGPlane(gridBinormal, startPoint);
                        }
                        else
                        {
                            hoverBuildPlane = new CSGPlane(gridTangent, startPoint);
                        }

                        worldPosition = hoverBuildPlane.RayIntersection(mouseRay);

                        // the third point is always straight up from the second point
                        //worldPosition = startPoint + (Vector3.Dot(worldPosition - startPoint, gridNormal) * gridNormal);

                        CSGGrid.SetForcedGrid(camera, hoverBuildPlane);
                        ResetVisuals();

                        if(raySnapFunction != null)
                        {
                            Ray      ray = new Ray(startPoint, gridNormal);
                            CSGBrush snappedOnBrush;

                            worldPosition = raySnapFunction(
                                camera, worldPosition, ray, ref visualSnappedEdges, out snappedOnBrush
                            );

                            if(snappedOnBrush != null)
                            {
                                pointOnEdge = visualSnappedEdges != null &&
                                    visualSnappedEdges.Count > 0;

                                vertexOnBrush = snappedOnBrush;
                            }
                        }

                        worldPosition = GeometryUtility.ProjectPointOnInfiniteLine(
                            worldPosition, startPoint, gridNormal
                        );
                    }
                    else
                    {
                        worldPosition = hoverBuildPlane.RayIntersection(mouseRay);

                        CSGGrid.SetForcedGrid(camera, hoverBuildPlane);
                        ResetVisuals();

                        if(snapFunction != null)
                        {
                            CSGBrush snappedOnBrush;

                            worldPosition = snapFunction(
                                camera, worldPosition, hoverBuildPlane, ref visualSnappedEdges, out snappedOnBrush
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

                    if(settings.vertices.Length > 0)
                    {
                        if((settings.vertices[0] - worldPosition).magnitude > MathConstants.EqualityEpsilon)
                        {
                            UpdateSizes();
                            UpdateBaseShape(true);
                        }
                    }

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

                    if(GUIUtility.hotControl == shapeId &&
                       settings.vertices.Length < kMaxPoints)
                    {
                        if(!float.IsNaN(worldPosition.x) && !float.IsInfinity(worldPosition.x) &&
                           !float.IsNaN(worldPosition.y) && !float.IsInfinity(worldPosition.y) &&
                           !float.IsNaN(worldPosition.z) && !float.IsInfinity(worldPosition.z))
                        {
                            if(settings.vertices.Length < 2 &&
                               hoverBuildPlane.normal.sqrMagnitude != 0)
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
                                    plane.d           += distance;
                                    hoverDefaultPlane =  plane;

                                    for(int i = 0; i < settings.vertices.Length; i++)
                                    {
                                        if(!settings.onGeometryVertices[i])
                                        {
                                            settings.SetPoint(
                                                i, GeometryUtility.ProjectPointOnPlane(plane, settings.vertices[i])
                                            );

                                            settings.onGeometryVertices[i] = true;
                                        }
                                    }
                                }
                            }

                            ArrayUtility.Add(ref settings.onGeometryVertices, vertexOnGeometry);
                            settings.AddPoint(worldPosition);

                            UpdateSizes();
                            CSG_EditorGUIUtility.RepaintAll();

                            if(settings.vertices.Length == kMaxPoints)
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

                        if(settings.vertices.Length == kMaxPoints)
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

#endregion

#region Create Stairs

        internal override bool UpdateBaseShape(bool registerUndo = true)
        {
            if(editMode == EditMode.CreatePlane)
            {
                return false;
            }

            float length = StairsDepth;

            if(length <= 0)
            {
                InternalCSGModelManager.SkipCheckForChanges = false;
                HideGenerateBrushes();

                return false;
            }

            Quaternion worldToLocalRotation = GetWorldToLocalRotation();
            Quaternion localToWorldRotation = Quaternion.Inverse(worldToLocalRotation);
            brushPosition = localToWorldRotation * settings.bounds.Min;

            int totalSteps = TotalSteps;

            if(!GenerateBrushObjects(totalSteps))
            {
                InternalCSGModelManager.SkipCheckForChanges = false;
                HideGenerateBrushes();

                return false;
            }

            if(registerUndo)
            {
                Undo.RecordObjects(generatedBrushes, "Created Linear Stairs");
            }

            if(!GenerateStairs(
                   generatedBrushes, totalSteps, StepDepth, StepHeight, StepDepth, StairsDepth, StairsWidth
                 , StairsHeight, ExtraDepth, ExtraHeight, StairsBottom
               ))
            {
                InternalCSGModelManager.SkipCheckForChanges = false;
                HideGenerateBrushes();

                return false;
            }

            if(registerUndo)
            {
                MarkAllBrushesDirty();
            }

            try
            {
                InternalCSGModelManager.SkipCheckForChanges = true;

                if(registerUndo)
                {
                    EditorUtility.SetDirty(this);
                }

                InternalCSGModelManager.External.SetDirty(parentModel.modelNodeID);
                InternalCSGModelManager.CheckForChanges(true);
                //DebugEditorWindow.PrintDebugInfo();
            }
            finally
            {
                InternalCSGModelManager.SkipCheckForChanges = false;
            }

            return true;
        }

        private bool GenerateStairs(
            CSGBrush[] stepBrushes, int totalSteps, float stepLength, float stepHeight, float stepDepth
          , float stairsDepth, float stairsWidth, float stairsHeight, float extraDepth, float extraHeight
          , StairsBottom stairsBottom
        )
        {
            //var currentModel = parentModel ? parentModel : SelectionUtility.LastUsedModel;
            //var modelRotation = Quaternion.Inverse(currentModel.transform.rotation);

            stairsDepth = Math.Max(0, stairsDepth);

            Vector3 parentScale    = parentTransform.lossyScale;
            bool    parentInverted = Math.Sign(parentScale.x) * Math.Sign(parentScale.y) * Math.Sign(parentScale.z) < 0;

            bool success = true;

            for(int stepIndex = 0; stepIndex < totalSteps; stepIndex++)
            {
                CSGBrush brush = stepBrushes[stepIndex];

                if(!brush)
                {
                    continue;
                }

                float   curStepDepth;
                float   curStepHeight;
                float   curStepY;
                Vector3 extraLength;
                Vector3 lengthPos;

                switch(stairsBottom)
                {
                    default:
                    case StairsBottom.Filled:
                    {
                        curStepHeight = Mathf.Min(stairsHeight, stepIndex == 0 ? extraHeight + stepHeight : stepHeight);
                        curStepY      = stepIndex == 0 ? stepHeight * stepIndex : extraHeight + stepHeight * stepIndex;
                        extraLength   = lengthDirection * (stepLength * stepIndex);
                        curStepDepth  = stairsDepth;
                        lengthPos     = Vector3.zero;

                        break;
                    }
                    case StairsBottom.Steps:
                    {
                        curStepHeight = stepHeight;
                        curStepY      = extraHeight + stepHeight * stepIndex;
                        extraLength   = Vector3.zero;
                        curStepDepth  = stepIndex == totalSteps - 1 ? stepDepth + extraDepth : stepDepth;

                        lengthPos = stepIndex == totalSteps - 1
                            ? Vector3.zero
                            : lengthDirection * Mathf.Max(0, (totalSteps - (stepIndex + 1)) * stepDepth + extraDepth);

                        break;
                    }
                }

                Vector3 heightPos = heightDirection * curStepY;

                Vector3 widthSize  = widthDirection * stairsWidth;
                Vector3 lengthSize = lengthDirection * curStepDepth - extraLength;
                Vector3 heightSize = heightDirection * curStepHeight;

                Vector3 size     = widthSize + heightSize + lengthSize;
                Vector3 position = totalSteps == 1 ? heightPos + lengthPos + brushPosition : heightPos + lengthPos;

                if(parentInverted)
                {
                    size *= -1;
                }

                ControlMesh newControlMesh;
                Shape       newShape;

                if(!BrushFactory.CreateCubeControlMesh(out newControlMesh, out newShape, Vector3.zero, size))
                {
                    success = false;

                    if(brush.gameObject.activeSelf)
                    {
                        brush.gameObject.SetActive(false);
                    }

                    continue;
                }

                if(!brush.gameObject.activeSelf)
                {
                    brush.gameObject.SetActive(true);
                }

                brush.Shape               = newShape;
                newControlMesh.Generation = brush.ControlMesh.Generation + 1;
                brush.ControlMesh         = newControlMesh;

                if(totalSteps != 1)
                {
                    brush.transform.localPosition = position;
                }

                //brush.transform.localRotation = Quaternion.identity;
                SurfaceUtility.TranslateSurfacesInWorldSpace(brush, -position);
            }

            return success;
        }

#endregion
    }
}
