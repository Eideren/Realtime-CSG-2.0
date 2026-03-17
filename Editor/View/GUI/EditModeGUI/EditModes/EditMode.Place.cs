using System;
using System.Linq;
using UnityEditor;
using UnityEngine;
using InternalRealtimeCSG;
using System.Collections.Generic;
using RealtimeCSG.Legacy;
using RealtimeCSG.Components;
using Object = UnityEngine.Object;

namespace RealtimeCSG
{
    internal sealed class EditModePlace : ScriptableObject, IEditMode
    {
        public bool UsesUnitySelection
        {
            get => true;
        }

        public bool IgnoreUnityRect
        {
            get => false;
        }

        private static readonly int objectTargetHash = "objectTarget".GetHashCode();
        private static readonly int boundsCenterTargetHash = "boundsCenterTarget".GetHashCode();
        private static readonly int boundsEdgesTargetHash = "boundsEdgesTarget".GetHashCode();

        private bool HaveTarget
        {
            get => hoverOnTarget != -1 || hoverOnBoundsCenter != -1 || hoverOnBoundsEdge != -1;
        }

        public bool HaveSelection
        {
            get => brushes.Length != 0 || topTransforms.Length != 0;
        }

        public bool HaveBrushSelection
        {
            get => brushes.Length != 0;
        }

        private const float hover_text_distance = 40.0f;

        [SerializeField] private Vector3[] projectedBounds = new Vector3[8];
        [SerializeField] private List<GameObject> dragGameObjects = new List<GameObject>();
        [SerializeField] private int hoverOverBrushIndex;

        [SerializeField] private PrefabSourceAlignment sourceSurfaceAlignment = PrefabSourceAlignment.AlignedFront;

        [SerializeField]
        private PrefabDestinationAlignment destinationSurfaceAlignment = PrefabDestinationAlignment.AlignToSurface;

#region Tool edit modes

        [Serializable]
        private enum ToolEditMode
        {
            None
          , CloneDragging
          , MovingObject
          , SizingBounds
          , RotatingBounds
        }

        private ToolEditMode toolEditMode = ToolEditMode.None;

#endregion

#region Input

        private bool cloneDragKeyPressed = false;
        private bool mouseIsDragging = false;
        private bool firstMove = false;
        private Camera draggingOnCamera = null;

#endregion

#region Misc state

        [SerializeField] private bool toolIsEnabled = false;
        [SerializeField] private bool shouldHideTool = false;

#endregion

#region Hover Targets

        private int hoverOnTarget = -1;
        private int hoverOnBoundsCenter = -1;
        private int hoverOnBoundsEdge = -1;

#endregion

#region Dragging

        private Camera startCamera;
        private CSGPlane movePlane;

        private Vector3 hoverIntersectionPoint = MathConstants.zeroVector3;
        private Vector3 originalPoint = MathConstants.zeroVector3;
        private Vector3 extraDeltaMovement = MathConstants.zeroVector3;
        private Vector3 worldDeltaMovement = MathConstants.zeroVector3;
        private Vector2 realMousePosition;
        private Vector3[] snapVertices = null;

        private EventModifiers prevModifiers = EventModifiers.None;
        //bool			prevYMode				= false;

#endregion

#region Rotation

        [SerializeField] private bool prevPivotModeSet;
        [SerializeField] private PivotMode prevPivotMode;
        [SerializeField] private bool worldSpacePivotCenterSet;
        [SerializeField] private Vector3 worldSpacePivotCenter;
        private Vector3 boundsCenter;
        private Vector3 rotateCenter;
        private Vector3 rotateNormal;
        private Vector3 rotateTangent;

        private Vector3 rotateStart;

        //Plane			rotatePlane;
        private Vector3 rotateMousePosition;
        private float rotateStartAngle;
        private float rotateCurrentAngle;
        private float rotateCurrentSnappedAngle;
        private float rotateRadius;
        private PrincipleAxis rotateAxis;

#endregion

#region Pivot

        public Vector3 WorldSpacePivotCenter
        {
            get => worldSpacePivotCenterSet ? worldSpacePivotCenter : Vector3.zero;
            set
            {
                if(worldSpacePivotCenterSet && worldSpacePivotCenter == value)
                {
                    return;
                }

                ignorePivotChange = true;
                SetPivotCenter(value);
            }
        }

        public Vector3 LocalSpacePivotCenter
        {
            get
            {
                if(!worldSpacePivotCenterSet || !Selection.activeTransform)
                {
                    return Vector3.zero;
                }

                if(activeSpaceMatrices == null)
                {
                    activeSpaceMatrices = SpaceMatrices.Create(activeTransform);
                }

                return Selection.activeTransform.InverseTransformVector(worldSpacePivotCenter);
            }
            set
            {
                if(!Selection.activeTransform)
                {
                    return;
                }

                Vector3 worldSpaceValue = Selection.activeTransform.TransformVector(value);

                if(worldSpacePivotCenter == worldSpaceValue)
                {
                    return;
                }

                ignorePivotChange = true;
                SetPivotCenter(worldSpaceValue);
            }
        }

#endregion

#region Bounds

        [SerializeField] private int[] targetBoundCenterIDs = new int[6];
        [SerializeField] private int[] targetBoundEdgeIDs = new int[12];
        [SerializeField] private Vector3[] targetLocalPoints = new Vector3[8];
        [SerializeField] private Vector3[] targetLocalCenterPoints = new Vector3[6];
        [SerializeField] private Vector3[] targetLocalEdgePoints = new Vector3[12];
        [SerializeField] private Vector3[] targetLocalDirections = new Vector3[6];
        [SerializeField] private AABB targetLocalBounds = new AABB();

        [SerializeField] private Vector3[] renderLocalCenterPoints = new Vector3[6];
        [SerializeField] private float[] renderLocalCenterPointSizes = new float[6];
        [SerializeField] private Color[] renderLocalCenterPointColors = new Color[12];

        [SerializeField] private Vector3[] renderLocalEdgePoints = new Vector3[12];
        [SerializeField] private float[] renderLocalEdgePointSizes = new float[12];
        [SerializeField] private Color[] renderLocalEdgePointColors = new Color[24];

        [SerializeField] private AABB originalLocalTargetBounds = new AABB();
        [SerializeField] private Vector3[] originalLocalPoints = new Vector3[8];
        [SerializeField] private Vector3[] originalLocalCenterPoints = new Vector3[6];
        [SerializeField] private Vector3[] originalLocalEdgePoints = new Vector3[12];

        [SerializeField] private bool[] backfaced = new bool[6];      // [-X, +X, -Y, +Y, -Z, +Z]
        [SerializeField] private CSGPlane[] planes = new CSGPlane[6]; // [-X, +X, -Y, +Y, -Z, +Z]

#endregion

#region Transforms

        [SerializeField] private Transform activeTransform = null;
        [SerializeField] private Vector3 prevActiveTransformPosition = Vector3.zero;
        [SerializeField] private Quaternion prevActiveTransformRotation = Quaternion.identity;
        [SerializeField] private Vector3 prevActiveTransformScale = Vector3.one;
        [SerializeField] private Matrix4x4 prevInverseMatrix = Matrix4x4.identity;
        [SerializeField] private PivotRotation prevToolsRotation = PivotRotation.Local;
        [SerializeField] private SpaceMatrices activeSpaceMatrices = new SpaceMatrices();

#endregion

#region TargetInfo

        [SerializeField] private Transform[] topTransforms = new Transform[0];
        [SerializeField] private Vector3[] backupPositions = new Vector3[0];       // transforms
        [SerializeField] private Quaternion[] backupRotations = new Quaternion[0]; // transforms
        private bool ignoreSetTargets = false;

        [SerializeField] private CSGBrush[] brushes = new CSGBrush[0];
        [SerializeField] private Transform[] parentModelTransforms = new Transform[0]; // brush
        [SerializeField] private int[] brushControlIDs = new int[0];                   // brush
        [SerializeField] private Shape[] backupShapes = null;                          // brush
        [SerializeField] private ControlMesh[] backupControlMeshes = null;             // brush

#endregion

        public void OnEnableTool()
        {
            toolIsEnabled = true;
            Tools.hidden  = shouldHideTool;
            ResetTool();
        }

        public void OnDisableTool()
        {
            toolIsEnabled = false;
            Tools.hidden  = false;
            ResetTool();
        }

        public void SetTargets(FilteredSelection filteredSelection)
        {
            if(ignoreSetTargets)
            {
                return;
            }

            CSGBrush[]  newBrushes       = filteredSelection.GetAllContainedBrushes().ToArray();
            Transform[] newTopTransforms = filteredSelection.GetAllTopTransforms().ToArray();

            if(brushes.Length == newBrushes.Length &&
               topTransforms.Length == newTopTransforms.Length)
            {
                bool different = false;

                for(int i = 0; i < brushes.Length; i++)
                {
                    if(!ArrayUtility.Contains(brushes, newBrushes[i]))
                    {
                        different = true;

                        break;
                    }
                }

                for(int i = 0; i < topTransforms.Length; i++)
                {
                    if(!ArrayUtility.Contains(topTransforms, newTopTransforms[i]))
                    {
                        different = true;

                        break;
                    }
                }

                if(!different)
                {
                    return;
                }
            }

            brushes       = newBrushes;
            topTransforms = newTopTransforms;

            backupPositions = new Vector3[topTransforms.Length];
            backupRotations = new Quaternion[topTransforms.Length];

            parentModelTransforms = new Transform[brushes.Length];
            brushControlIDs       = new int[brushes.Length];
            backupShapes          = new Shape[brushes.Length];
            backupControlMeshes   = new ControlMesh[brushes.Length];

            for(int i = 0; i < brushes.Length; i++)
            {
                backupShapes[i]        = brushes[i].Shape.Clone();
                backupControlMeshes[i] = brushes[i].ControlMesh.Clone();

                if(brushes[i].ChildData == null ||
                   brushes[i].ChildData.ModelTransform == null)
                {
                    parentModelTransforms[i] = null;
                }
                else
                {
                    parentModelTransforms[i] = brushes[i].ChildData.ModelTransform;
                }
            }

            UpdateTargetInfo();

            worldSpacePivotCenterSet = false;
            worldSpacePivotCenter    = Vector3.zero;

            shouldHideTool = (CSGSettings.SnapNonCSGObjects && Tools.current == Tool.Move)
             || filteredSelection.nodeTargets.Length > 0;

            if(toolIsEnabled)
            {
                Tools.hidden = shouldHideTool;
            }

            lastLineMeshGeneration--;
        }

        private void ResetTool()
        {
            prevActiveTransformPosition = Vector3.zero;
            prevActiveTransformRotation = Quaternion.identity;
            prevActiveTransformScale    = Vector3.one;
            prevInverseMatrix           = Matrix4x4.identity;

            prevPivotModeSet         = false;
            prevPivotMode            = PivotMode.Center;
            worldSpacePivotCenterSet = false;
            worldSpacePivotCenter    = Vector3.zero;

            toolEditMode        = ToolEditMode.None;
            cloneDragKeyPressed = false;

            startCamera = null;

            hoverOnTarget       = -1;
            hoverOnBoundsCenter = -1;
            hoverOnBoundsEdge   = -1;

            originalPoint      = MathConstants.zeroVector3;
            worldDeltaMovement = MathConstants.zeroVector3;

            prevModifiers = EventModifiers.None;
            //prevYMode				= false;

            CSGGrid.ForceGrid = false;
        }

        private void UpdateTargetBounds()
        {
            activeTransform = Selection.activeTransform;

            if(activeTransform == null)
            {
                return;
            }

            if(activeSpaceMatrices == null)
            {
                activeSpaceMatrices = SpaceMatrices.Create(activeTransform);
            }

            if(Tools.pivotRotation == PivotRotation.Local)
            {
                targetLocalBounds = BoundsUtilities.GetLocalBounds(brushes, activeSpaceMatrices.activeWorldToLocal);
            }
            else
            {
                targetLocalBounds = BoundsUtilities.GetBounds(brushes);
            }

            if(shouldHideTool && brushes.Length == 0 &&
               activeTransform.GetComponent<CSGNode>() == null)
            {
                Bounds     bounds     = new Bounds();
                bool       found      = false;
                MeshFilter meshFilter = activeTransform.GetComponent<MeshFilter>();

                if(meshFilter != null &&
                   meshFilter.sharedMesh)
                {
                    bounds = meshFilter.sharedMesh.bounds;
                    found  = true;
                }

                MeshCollider meshCollider = activeTransform.GetComponent<MeshCollider>();

                if(meshCollider != null &&
                   meshCollider.sharedMesh)
                {
                    bounds = meshCollider.sharedMesh.bounds;
                    found  = true;
                }

                if(found)
                {
                    if(Tools.pivotRotation == PivotRotation.Local)
                    {
                        Matrix4x4 transformToGridspace =
                            activeSpaceMatrices.activeWorldToLocal * activeTransform.localToWorldMatrix;

                        targetLocalBounds.Extend(
                            transformToGridspace.MultiplyPoint(new Vector3(bounds.min.x, bounds.min.y, bounds.min.z))
                        );

                        targetLocalBounds.Extend(
                            transformToGridspace.MultiplyPoint(new Vector3(bounds.max.x, bounds.min.y, bounds.min.z))
                        );

                        targetLocalBounds.Extend(
                            transformToGridspace.MultiplyPoint(new Vector3(bounds.max.x, bounds.min.y, bounds.max.z))
                        );

                        targetLocalBounds.Extend(
                            transformToGridspace.MultiplyPoint(new Vector3(bounds.min.x, bounds.min.y, bounds.max.z))
                        );

                        targetLocalBounds.Extend(
                            transformToGridspace.MultiplyPoint(new Vector3(bounds.min.x, bounds.max.y, bounds.min.z))
                        );

                        targetLocalBounds.Extend(
                            transformToGridspace.MultiplyPoint(new Vector3(bounds.max.x, bounds.max.y, bounds.min.z))
                        );

                        targetLocalBounds.Extend(
                            transformToGridspace.MultiplyPoint(new Vector3(bounds.max.x, bounds.max.y, bounds.max.z))
                        );

                        targetLocalBounds.Extend(
                            transformToGridspace.MultiplyPoint(new Vector3(bounds.min.x, bounds.max.y, bounds.max.z))
                        );
                    }
                    else
                    {
                        Matrix4x4 transformToGridspace = activeTransform.localToWorldMatrix;

                        targetLocalBounds.Extend(
                            transformToGridspace.MultiplyPoint(new Vector3(bounds.min.x, bounds.min.y, bounds.min.z))
                        );

                        targetLocalBounds.Extend(
                            transformToGridspace.MultiplyPoint(new Vector3(bounds.max.x, bounds.min.y, bounds.min.z))
                        );

                        targetLocalBounds.Extend(
                            transformToGridspace.MultiplyPoint(new Vector3(bounds.max.x, bounds.min.y, bounds.max.z))
                        );

                        targetLocalBounds.Extend(
                            transformToGridspace.MultiplyPoint(new Vector3(bounds.min.x, bounds.min.y, bounds.max.z))
                        );

                        targetLocalBounds.Extend(
                            transformToGridspace.MultiplyPoint(new Vector3(bounds.min.x, bounds.max.y, bounds.min.z))
                        );

                        targetLocalBounds.Extend(
                            transformToGridspace.MultiplyPoint(new Vector3(bounds.max.x, bounds.max.y, bounds.min.z))
                        );

                        targetLocalBounds.Extend(
                            transformToGridspace.MultiplyPoint(new Vector3(bounds.max.x, bounds.max.y, bounds.max.z))
                        );

                        targetLocalBounds.Extend(
                            transformToGridspace.MultiplyPoint(new Vector3(bounds.min.x, bounds.max.y, bounds.max.z))
                        );
                    }
                }
                else if(targetLocalBounds.IsEmpty())
                {
                    if(Tools.pivotRotation == PivotRotation.Local)
                    {
                        Matrix4x4 transformToGridspace = activeSpaceMatrices.activeWorldToLocal;
                        targetLocalBounds.Extend(transformToGridspace.MultiplyPoint(activeTransform.position));
                    }
                    else
                    {
                        targetLocalBounds.Extend(activeTransform.position);
                    }
                }
            }

            BoundsUtilities.GetBoundsCornerPoints(targetLocalBounds, targetLocalPoints);

            float centerX = (targetLocalBounds.MinX + targetLocalBounds.MaxX) * 0.5f;
            float centerY = (targetLocalBounds.MinY + targetLocalBounds.MaxY) * 0.5f;
            float centerZ = (targetLocalBounds.MinZ + targetLocalBounds.MaxZ) * 0.5f;

            targetLocalCenterPoints[0] = new Vector3(targetLocalBounds.MinX, centerY, centerZ);
            targetLocalCenterPoints[1] = new Vector3(targetLocalBounds.MaxX, centerY, centerZ);
            targetLocalCenterPoints[2] = new Vector3(centerX, targetLocalBounds.MinY, centerZ);
            targetLocalCenterPoints[3] = new Vector3(centerX, targetLocalBounds.MaxY, centerZ);
            targetLocalCenterPoints[4] = new Vector3(centerX, centerY, targetLocalBounds.MinZ);
            targetLocalCenterPoints[5] = new Vector3(centerX, centerY, targetLocalBounds.MaxZ);

            for(int i = 0; i < BoundsUtilities.AABBEdgeIndices.Length; i++)
            {
                int     index1 = BoundsUtilities.AABBEdgeIndices[i][0];
                int     index2 = BoundsUtilities.AABBEdgeIndices[i][1];
                Vector3 point1 = targetLocalPoints[index1];
                Vector3 point2 = targetLocalPoints[index2];
                targetLocalEdgePoints[i] = (point1 + point2) * 0.5f;
            }

            targetLocalDirections[0] = new Vector3(-1, 0, 0);
            targetLocalDirections[1] = new Vector3(1, 0, 0);
            targetLocalDirections[2] = new Vector3(0, -1, 0);
            targetLocalDirections[3] = new Vector3(0, 1, 0);
            targetLocalDirections[4] = new Vector3(0, 0, -1);
            targetLocalDirections[5] = new Vector3(0, 0, 1);

            lastLineMeshGeneration--;
        }

        private void UpdateTargetBoundsHandles(Camera camera)
        {
            if(camera == null)
            {
                return;
            }

            activeTransform = Selection.activeTransform;

            if(activeTransform == null)
            {
                return;
            }

            if(activeSpaceMatrices == null)
            {
                activeSpaceMatrices = SpaceMatrices.Create(activeTransform);
            }

            float centerX = (targetLocalBounds.MinX + targetLocalBounds.MaxX) * 0.5f;
            float centerY = (targetLocalBounds.MinY + targetLocalBounds.MaxY) * 0.5f;
            float centerZ = (targetLocalBounds.MinZ + targetLocalBounds.MaxZ) * 0.5f;

            float minX = targetLocalPoints[0].x;
            float maxX = targetLocalPoints[1].x;
            float minY = targetLocalPoints[0].y;
            float maxY = targetLocalPoints[4].y;
            float minZ = targetLocalPoints[0].z;
            float maxZ = targetLocalPoints[2].z;

            renderLocalCenterPoints[0] = new Vector3(minX, centerY, centerZ);
            renderLocalCenterPoints[1] = new Vector3(maxX, centerY, centerZ);
            renderLocalCenterPoints[2] = new Vector3(centerX, minY, centerZ);
            renderLocalCenterPoints[3] = new Vector3(centerX, maxY, centerZ);
            renderLocalCenterPoints[4] = new Vector3(centerX, centerY, minZ);
            renderLocalCenterPoints[5] = new Vector3(centerX, centerY, maxZ);

            for(int i = 0; i < BoundsUtilities.AABBEdgeIndices.Length; i++)
            {
                int     index1 = BoundsUtilities.AABBEdgeIndices[i][0];
                int     index2 = BoundsUtilities.AABBEdgeIndices[i][1];
                Vector3 point1 = targetLocalPoints[index1];
                Vector3 point2 = targetLocalPoints[index2];
                renderLocalEdgePoints[i] = (point1 + point2) * 0.5f;
            }

            Vector3 camera_position  = activeSpaceMatrices.activeWorldToLocal.MultiplyPoint(camera.transform.position);
            Vector3 camera_direction = activeSpaceMatrices.activeWorldToLocal.MultiplyVector(camera.transform.forward);
            bool    ortho            = camera.orthographic;

            for(int i = 0; i < BoundsUtilities.AABBSidePointIndices.Length; i++)
            {
                planes[i] = new CSGPlane(
                    targetLocalPoints[BoundsUtilities.AABBSidePointIndices[i][0]],
                    targetLocalPoints[BoundsUtilities.AABBSidePointIndices[i][1]],
                    targetLocalPoints[BoundsUtilities.AABBSidePointIndices[i][2]]
                );

                if(ortho)
                {
                    backfaced[i] = Mathf.Abs(Vector3.Dot(planes[i].normal, camera_direction)) > 0.9999f;
                }
                else
                {
                    backfaced[i] = Vector3.Dot(planes[i].normal, camera_position - renderLocalCenterPoints[i]) < 0;
                }
            }

            //if (!camera.orthographic)
            {
                int point1 = -1, point2 = -1;

                if(hoverOnBoundsCenter != -1)
                {
                    point1 = hoverOnBoundsCenter;
                }
                else if(ortho &&
                        hoverOnBoundsEdge != -1)
                {
                    point1 = BoundsUtilities.AABBEdgeIndices[hoverOnBoundsEdge][0];
                    point2 = BoundsUtilities.AABBEdgeIndices[hoverOnBoundsEdge][1];
                }

                Matrix4x4 origMatrix = Handles.matrix;
                Handles.matrix = activeSpaceMatrices.activeLocalToWorld;

                for(int i = renderLocalCenterPoints.Length - 1; i >= 0; i--)
                {
                    int   state  = (int)(i == point1 || i == point2 ? SelectState.Hovering : SelectState.None);
                    Color color1 = ColorSettings.MeshEdgeOutline;
                    Color color2 = ColorSettings.PolygonInnerStateColor[state];

                    bool disabled = false;

                    switch(BoundsUtilities.AABBSideAxis[i])
                    {
                        case PrincipleAxis.X: disabled = CSGSettings.LockAxisX; break;
                        case PrincipleAxis.Y: disabled = CSGSettings.LockAxisY; break;
                        case PrincipleAxis.Z: disabled = CSGSettings.LockAxisZ; break;
                    }

                    if(disabled)
                    {
                        color2 = Color.red;
                    }

                    float handleSize = CSG_HandleUtility.GetHandleSize(renderLocalCenterPoints[i]);

                    if(backfaced[i])
                    {
                        if(!ortho)
                        {
                            color1.a   *= GUIConstants.BACKFACE_TRANSPARENCY;
                            color2.a   *= GUIConstants.BACKFACE_TRANSPARENCY;
                            handleSize *= GUIConstants.BACK_HANDLE_SCALE;
                        }
                        else
                        {
                            handleSize = 0;
                        }
                    }
                    else
                    {
                        handleSize *= GUIConstants.HANDLE_SCALE;
                    }

                    renderLocalCenterPointSizes[i]          = handleSize;
                    renderLocalCenterPointColors[i * 2 + 0] = color1;
                    renderLocalCenterPointColors[i * 2 + 1] = color2;
                }

                for(int i = renderLocalEdgePoints.Length - 1; i >= 0; i--)
                {
                    int   state  = (int)(i == hoverOnBoundsEdge ? SelectState.Hovering : SelectState.None);
                    Color color1 = ColorSettings.MeshEdgeOutline;
                    Color color2 = ColorSettings.PointInnerStateColor[state];

                    bool disabled1 = false;

                    switch(BoundsUtilities.AABBEdgeAxis[i][0])
                    {
                        case PrincipleAxis.X: disabled1 = CSGSettings.LockAxisX; break;
                        case PrincipleAxis.Y: disabled1 = CSGSettings.LockAxisY; break;
                        case PrincipleAxis.Z: disabled1 = CSGSettings.LockAxisZ; break;
                    }

                    bool disabled2 = false;

                    switch(BoundsUtilities.AABBEdgeAxis[i][1])
                    {
                        case PrincipleAxis.X: disabled2 = CSGSettings.LockAxisX; break;
                        case PrincipleAxis.Y: disabled2 = CSGSettings.LockAxisY; break;
                        case PrincipleAxis.Z: disabled2 = CSGSettings.LockAxisZ; break;
                    }

                    if(disabled1 && disabled2)
                    {
                        color2 = Color.red;
                    }

                    float handleSize = CSG_HandleUtility.GetHandleSize(renderLocalEdgePoints[i]);
                    int   side1      = BoundsUtilities.AABBEdgeSides[i][0];
                    int   side2      = BoundsUtilities.AABBEdgeSides[i][1];

                    if(ortho)
                    {
                        if(backfaced[side1] || backfaced[side2])
                        {
                            handleSize = 0;
                        }
                        else
                        {
                            handleSize *= GUIConstants.HANDLE_SCALE;
                        }
                    }
                    else
                    {
                        if(backfaced[side1] && backfaced[side2])
                        {
                            color1.a   *= GUIConstants.BACKFACE_TRANSPARENCY;
                            color2.a   *= GUIConstants.BACKFACE_TRANSPARENCY;
                            handleSize *= GUIConstants.BACK_HANDLE_SCALE;
                        }
                        else
                        {
                            handleSize *= GUIConstants.HANDLE_SCALE;
                        }
                    }

                    renderLocalEdgePointSizes[i]          = handleSize;
                    renderLocalEdgePointColors[i * 2 + 0] = color1;
                    renderLocalEdgePointColors[i * 2 + 1] = color2;
                }

                Handles.matrix = origMatrix;
            }
        }

        private void UpdateTargetInfo()
        {
            Camera camera = Camera.current;

            if(topTransforms.Length != backupPositions.Length)
            {
                backupPositions = new Vector3[topTransforms.Length];
                backupRotations = new Quaternion[topTransforms.Length];
            }

            for(int t = 0; t < topTransforms.Length; t++)
            {
                if(!topTransforms[t])
                {
                    continue;
                }

                backupPositions[t] = topTransforms[t].position;
                backupRotations[t] = topTransforms[t].rotation;
            }

            UpdateTargetBounds();
            UpdateTargetBoundsHandles(camera);
            originalLocalTargetBounds = targetLocalBounds;

            for(int i = 0; i < 6; i++)
            {
                originalLocalCenterPoints[i] = targetLocalCenterPoints[i];
            }

            for(int i = 0; i < 12; i++)
            {
                originalLocalEdgePoints[i] = targetLocalEdgePoints[i];
            }

            for(int i = 0; i < 8; i++)
            {
                originalLocalPoints[i] = targetLocalPoints[i];
            }

            for(int i = 0; i < brushes.Length; i++)
            {
                backupShapes[i]        = brushes[i].Shape.Clone();
                backupControlMeshes[i] = brushes[i].ControlMesh.Clone();
            }

            lastLineMeshGeneration--;
        }

        public bool UndoRedoPerformed()
        {
            //CSGModelManager.Refresh(forceHierarchyUpdate: true);
            return false;
        }

        public bool DeselectAll()
        {
            SceneQueryUtility.ClearDeepClick();
            Selection.activeTransform = null;

            return false;
        }

        public void RecenterPivot()
        {
            // Can't see pivots in any other mode but rotate, so shouldn't be (accidentally) modifying it
            if(Tools.current != Tool.Rotate)
            {
                return;
            }

            if(Tools.pivotMode == PivotMode.Center ||
               !Selection.activeTransform)
            {
                return;
            }

            UpdateTargetBounds();

            ignorePivotChange = true;
            boundsCenter      = activeSpaceMatrices.activeLocalToWorld.MultiplyPoint(targetLocalBounds.Center);
            SetPivotCenter(boundsCenter);
            UpdatePivotCenter();
        }

        public void SetPivotCenter(Vector3 newCenter, bool registerUndo = true)
        {
            if(Tools.pivotMode == PivotMode.Center ||
               !Selection.activeTransform)
            {
                return;
            }

            if(registerUndo)
            {
                List<Object> objects = new List<UnityEngine.Object>(topTransforms.Length + brushes.Length);
                objects.AddRange(topTransforms);
                objects.AddRange(brushes);
                objects.Add(this);
                Undo.RegisterCompleteObjectUndo(objects.ToArray(), "Setting pivot");
            }

            newCenter = GridUtility.CleanPosition(newCenter);

            Vector3 realCenter = Selection.activeTransform.position;
            Vector3 difference = newCenter - realCenter;

            //Selection.activeTransform.position = newCenter;

            if(difference.x != 0.0f || difference.y != 0.0f || difference.z != 0.0f)
            {
                foreach(Transform topTransform in topTransforms)
                {
                    topTransform.position += difference;
                }

                GeometryUtility.MoveControlMeshVertices(brushes, -difference);
                SurfaceUtility.TranslateSurfacesInWorldSpace(brushes, -difference);
                ControlMeshUtility.RebuildShapes(brushes);
                InternalCSGModelManager.CheckForChanges(false);
                UpdateTargetInfo();
            }

            worldSpacePivotCenterSet = true;
            worldSpacePivotCenter    = newCenter;
        }

        private bool haveCloned = false;

        public void MoveCenter(Vector3 oldCenter, Vector3 newCenter, bool registerUndo = true)
        {
            if(!Selection.activeTransform)
            {
                return;
            }

            if(registerUndo)
            {
                List<Object> objects = new List<UnityEngine.Object>(topTransforms.Length + brushes.Length);
                objects.AddRange(topTransforms);
                objects.AddRange(brushes);
                objects.Add(this);
                Undo.RegisterCompleteObjectUndo(objects.ToArray(), "Moving Selection");
            }

            Vector3 difference = newCenter - oldCenter;

            //Selection.activeTransform.position = newCenter;
            if(!float.IsNaN(difference.x) &&
               !float.IsNaN(difference.y) &&
               !float.IsNaN(difference.z) &&
               !float.IsInfinity(difference.x) &&
               !float.IsInfinity(difference.y) &&
               !float.IsInfinity(difference.z))
            {
                if(difference != Vector3.zero)
                {
                    if(cloneDragKeyPressed && !haveCloned)
                    {
                        haveCloned = true;

                        topTransforms = EditModeManager.CloneTargets(
                            delegate(Transform newTransform, Transform originalTransform)
                            {
                                newTransform.localScale    = originalTransform.localScale;
                                newTransform.localPosition = originalTransform.localPosition + difference;
                                newTransform.localRotation = originalTransform.localRotation;
                            }
                        );
                    }
                }

                for(int i = 0; i < topTransforms.Length; i++)
                {
                    topTransforms[i].position = backupPositions[i] + difference;
                }
            }
        }

        private void UpdateGrid(Camera camera)
        {
            if(!HaveTarget || camera == null || !camera)
            {
                return;
            }

            if(activeSpaceMatrices == null)
            {
                activeSpaceMatrices = SpaceMatrices.Create(activeTransform);
            }

            if(hoverOnBoundsCenter != -1)
            {
                Vector3 worldOrigin    = targetLocalCenterPoints[hoverOnBoundsCenter];
                Vector3 worldDirection = targetLocalDirections[hoverOnBoundsCenter];

                worldOrigin    = activeSpaceMatrices.activeLocalToWorld.MultiplyPoint(worldOrigin);
                worldDirection = activeSpaceMatrices.activeLocalToWorld.MultiplyVector(worldDirection);

                CSGGrid.SetupRayWorkPlane(camera, worldOrigin, worldDirection, ref movePlane);
            }
            else
            {
                CSGGrid.SetupWorkPlane(camera, originalPoint, ref movePlane);
            }
        }

        private void UpdateGridOrientation(Vector2 mousePosition)
        {
            Ray     mouseRay     = HandleUtility.GUIPointToWorldRay(mousePosition);
            Vector3 intersection = movePlane.RayIntersection(mouseRay);
            originalPoint = GeometryUtility.ProjectPointOnPlane(movePlane, intersection);

            UpdateGrid(startCamera);
            prevModifiers = Event.current.modifiers;
            //prevYMode		= RealtimeCSG.Grid.YMoveModeActive;
            UpdateTargetInfo();
        }

        private Vector3 SnapMovementToPlane(Vector3 offset)
        {
            if(Math.Abs(movePlane.a) > 1 - MathConstants.NormalEpsilon)
            {
                offset.x = 0.0f;
            }

            if(Math.Abs(movePlane.b) > 1 - MathConstants.NormalEpsilon)
            {
                offset.y = 0.0f;
            }

            if(Math.Abs(movePlane.c) > 1 - MathConstants.NormalEpsilon)
            {
                offset.z = 0.0f;
            }

            return offset;
        }

        private bool MoveRotateObjects(
            Transform[] transforms, Vector3 center, Quaternion rotationQuaternion, Vector3 offset
        )
        {
            // -rotateCenter
            // rotate with rotationQuaternion
            // +rotateCenter
            Matrix4x4 matrix2 = Matrix4x4.TRS(-center, MathConstants.identityQuaternion, MathConstants.oneVector3);
            Matrix4x4 matrix3 = Matrix4x4.TRS(center, rotationQuaternion, MathConstants.oneVector3);
            Matrix4x4 rotate_matrix = matrix3 * matrix2;

            Undo.RecordObjects(transforms, "Rotating brushes");

            for(int t = 0; t < transforms.Length; t++)
            {
                transforms[t].position =
                    GridUtility.CleanPosition(rotate_matrix.MultiplyPoint(backupPositions[t] + offset));

                transforms[t].rotation = rotationQuaternion * backupRotations[t];
            }

            return false;
        }

        private void RotateObjects(Transform[] transforms, Vector3 center, Quaternion rotationQuaternion)
        {
            // -rotateCenter
            // rotate with rotationQuaternion
            // +rotateCenter
            Matrix4x4 matrix2 = Matrix4x4.TRS(-center, MathConstants.identityQuaternion, MathConstants.oneVector3);
            Matrix4x4 matrix3 = Matrix4x4.TRS(center, rotationQuaternion, MathConstants.oneVector3);
            Matrix4x4 rotate_matrix = matrix3 * matrix2;

            Undo.RecordObjects(transforms, "Rotating brushes");

            for(int t = 0; t < transforms.Length; t++)
            {
                transforms[t].position = GridUtility.CleanPosition(rotate_matrix.MultiplyPoint(backupPositions[t]));
                transforms[t].rotation = rotationQuaternion * backupRotations[t];
            }

            lastLineMeshGeneration--;
            activeSpaceMatrices = null;
            UpdateTargetBounds();
            CSG_EditorGUIUtility.RepaintAll();
        }

        private void RotateObjects(Transform[] transforms, Vector3 center, Vector3 normal, float angle)
        {
            Quaternion rotationQuaternion = Quaternion.AngleAxis(angle, normal);

            RotateObjects(transforms, center, rotationQuaternion);
        }

        private void RotateObjects(
            Transform[] transforms, Vector3 center, Vector3 normal, Vector3 pointStart, float startAngle
          , Vector3 currentPoint, out float currentAngle
        )
        {
            currentAngle = GeometryUtility.SignedAngle(center - pointStart, center - currentPoint, normal);
            currentAngle = GridUtility.SnappedAngle(currentAngle - startAngle) + startAngle;

            RotateObjects(transforms, center, normal, currentAngle - startAngle);
        }

        private bool MoveObjects(
            Camera camera, Transform[] transforms, Vector3 offset, out Vector3 snappedOffset, SnapMode snapMode
        )
        {
            // move an object on the grid
            switch(snapMode)
            {
                case SnapMode.GridSnapping:
                {
                    CSGPlane localPlane = GeometryUtility.TransformPlane(
                        activeSpaceMatrices.activeWorldToLocal, movePlane
                    );

                    localPlane.normal = GridUtility.CleanNormal(localPlane.normal);
                    Vector3[] localPoints = originalLocalTargetBounds.GetAllCorners();

                    for(int i = 0; i < localPoints.Length; i++)
                    {
                        localPoints[i] = activeSpaceMatrices.activeLocalToWorld.MultiplyPoint(
                            GeometryUtility.ProjectPointOnPlane(localPlane, localPoints[i])
                        );
                    }

                    offset = CSGGrid.SnapDeltaToGrid(camera, offset, localPoints, snapToSelf: true);

                    break;
                }
                case SnapMode.RelativeSnapping:
                {
                    offset = CSGGrid.SnapDeltaRelative(camera, offset);

                    break;
                }
                default:
                case SnapMode.None:
                {
                    offset = CSGGrid.HandleLockedAxi(offset);

                    break;
                }
            }

            snappedOffset = offset;

            if(offset != MathConstants.zeroVector3 &&
               cloneDragKeyPressed)
            {
                if(toolEditMode == ToolEditMode.MovingObject)
                {
                    if(float.IsNaN(offset.x) ||
                       float.IsNaN(offset.y) ||
                       float.IsNaN(offset.z))
                    {
                        return false;
                    }

                    toolEditMode = ToolEditMode.CloneDragging;

                    transforms = EditModeManager.CloneTargets(
                        delegate(Transform newTransform, Transform originalTransform)
                        {
                            newTransform.localScale    = originalTransform.localScale;
                            newTransform.localPosition = originalTransform.localPosition + offset;
                            newTransform.localRotation = originalTransform.localRotation;
                        }
                    );

                    UpdateTargetBounds();
                    CSG_EditorGUIUtility.RepaintAll();

                    return true;
                }
            }

            return MoveObjects(transforms, offset);
        }

        private bool MoveObjects(Transform[] transforms, Vector3 offset)
        {
            if(float.IsNaN(offset.x) ||
               float.IsNaN(offset.y) ||
               float.IsNaN(offset.z))
            {
                return false;
            }

            Undo.RecordObjects(transforms, "Move objects");

            for(int t = 0; t < transforms.Length; t++)
            {
                transforms[t].position = GridUtility.CleanPosition(backupPositions[t] + offset);
                //transforms[t].rotation = backupRotations[t];
            }

            UpdateTargetBounds();
            CSG_EditorGUIUtility.RepaintAll();

            return false;
        }

        internal void CloneMoveByOffset(Vector3 offset)
        {
            int groupId = Undo.GetCurrentGroup();
            Undo.IncrementCurrentGroup();
            topTransforms = EditModeManager.CloneTargets();
            MoveByOffset(offset);
            Undo.CollapseUndoOperations(groupId);
        }

        internal void MoveByOffset(Vector3 offset)
        {
            UpdateTargetInfo();
            MoveObjects(topTransforms, offset);
        }

        internal void CloneRotateByOffset(Quaternion rotation)
        {
            int groupId = Undo.GetCurrentGroup();
            Undo.IncrementCurrentGroup();
            topTransforms = EditModeManager.CloneTargets();
            RotateByOffset(rotation);
            Undo.CollapseUndoOperations(groupId);
        }

        internal void RotateByOffset(Quaternion rotation)
        {
            UpdateTargetInfo();
            Vector3 pivotCenter = WorldSpacePivotCenter;
            RotateObjects(topTransforms, pivotCenter, rotation);
        }

        private void MoveBoundsCenter(
            Camera camera, int boundsCenterIndex, Vector3 offset, out Vector3 snappedOffset, SnapMode snapMode
        )
        {
            Vector3 worldOffset = offset;
            Vector3 localOffset = activeSpaceMatrices.activeWorldToLocal.MultiplyVector(worldOffset);

            // move a surface in the direction of it's normal
            switch(snapMode)
            {
                case SnapMode.GridSnapping:
                {
                    Vector3 worldLineOrg =
                        activeSpaceMatrices.activeLocalToWorld.MultiplyPoint(
                            originalLocalCenterPoints[boundsCenterIndex]
                        );

                    Vector3 worldLineDir =
                        activeSpaceMatrices.activeLocalToWorld.MultiplyVector(targetLocalDirections[boundsCenterIndex]);

                    Vector3[] worldPoints = new Vector3[]
                    {
                        activeSpaceMatrices.activeLocalToWorld.MultiplyPoint(
                            originalLocalCenterPoints[boundsCenterIndex]
                        )
                    };

                    worldOffset = CSGGrid.SnapDeltaToRayGrid(
                        camera, new Ray(worldLineOrg, worldLineDir), worldOffset, worldPoints, true
                    );

                    localOffset = activeSpaceMatrices.activeWorldToLocal.MultiplyVector(worldOffset);

                    break;
                }
                case SnapMode.RelativeSnapping:
                {
                    Vector3 worldLineOrg =
                        activeSpaceMatrices.activeLocalToWorld.MultiplyPoint(
                            originalLocalCenterPoints[boundsCenterIndex]
                        );

                    Vector3 worldLineDir =
                        activeSpaceMatrices.activeLocalToWorld.MultiplyVector(targetLocalDirections[boundsCenterIndex]);

                    worldOffset = CSGGrid.SnapDeltaToRayRelative(
                        camera, new Ray(worldLineOrg, worldLineDir), worldOffset
                    );

                    localOffset = activeSpaceMatrices.activeWorldToLocal.MultiplyVector(worldOffset);

                    break;
                }
            }

            if(float.IsNaN(localOffset.x) ||
               float.IsNaN(localOffset.y) ||
               float.IsNaN(localOffset.z))
            {
                snappedOffset = offset;

                return;
            }

            snappedOffset = worldOffset;

            targetLocalCenterPoints[boundsCenterIndex] =
                GridUtility.CleanPosition(originalLocalCenterPoints[boundsCenterIndex] + localOffset);

            int[] boundPoints = BoundsUtilities.AABBSidePointIndices[boundsCenterIndex];

            for(int p = 0; p < boundPoints.Length; p++)
            {
                int index = boundPoints[p];
                targetLocalPoints[index] = GridUtility.CleanPosition(originalLocalPoints[index] + localOffset);
            }

            int index1, index2;

            switch(boundsCenterIndex)
            {
                default:
                case 0:
                case 1:
                    index1 = 0;
                    index2 = 1;

                    break;
                case 2:
                case 3:
                    index1 = 2;
                    index2 = 3;

                    break;
                case 4:
                case 5:
                    index1 = 4;
                    index2 = 5;

                    break;
            }

            float old_delta_x =
                -new CSGPlane(targetLocalDirections[0], originalLocalCenterPoints[0]).Distance(
                    originalLocalCenterPoints[1]
                );

            float old_delta_y =
                -new CSGPlane(targetLocalDirections[2], originalLocalCenterPoints[2]).Distance(
                    originalLocalCenterPoints[3]
                );

            float old_delta_z =
                -new CSGPlane(targetLocalDirections[4], originalLocalCenterPoints[4]).Distance(
                    originalLocalCenterPoints[5]
                );

            float new_delta_x = old_delta_x + ((boundsCenterIndex & 1) == 1 ? localOffset.x : -localOffset.x);
            float new_delta_y = old_delta_y + ((boundsCenterIndex & 1) == 1 ? localOffset.y : -localOffset.y);
            float new_delta_z = old_delta_z + ((boundsCenterIndex & 1) == 1 ? localOffset.z : -localOffset.z);

            Vector3 world_center_before =
                (originalLocalCenterPoints[index1] + originalLocalCenterPoints[index2]) * 0.5f;

            Vector3 world_center_after = world_center_before + localOffset * 0.5f;

            Vector3 world_scale = new Vector3(
                new_delta_x / old_delta_x, new_delta_y / old_delta_y, new_delta_z / old_delta_z
            );

            Undo.RecordObjects(brushes, "Scaling brushes");

            for(int t = 0; t < brushes.Length; t++)
            {
                CSGBrush brush = brushes[t];
                brush.ControlMesh = backupControlMeshes[t].Clone();
                brush.Shape       = backupShapes[t].Clone();
            }

            ControlMeshUtility.TransformBrushes(
                brushes, world_center_before, world_center_after, world_scale, activeSpaceMatrices
            );

            InternalCSGModelManager.CheckSurfaceModifications(brushes);

            lastLineMeshGeneration--;
            UpdateTargetBounds();
            UpdateTargetBoundsHandles(camera);
            CSG_EditorGUIUtility.RepaintAll();
        }

        private void MoveBoundsEdge(
            Camera camera, int edgeCenter, Vector3 offset, out Vector3 snappedOffset, SnapMode snapMode
        )
        {
            Vector3 worldOffset = offset;
            Vector3 localOffset = activeSpaceMatrices.activeWorldToLocal.MultiplyVector(worldOffset);

            // move a surface in the direction of it's normal
            switch(snapMode)
            {
                case SnapMode.GridSnapping:
                {
                    Vector3[] worldPoints = new Vector3[]
                    {
                        activeSpaceMatrices.activeLocalToWorld.MultiplyPoint(originalLocalEdgePoints[edgeCenter])
                    };

                    worldOffset = CSGGrid.SnapDeltaToGrid(camera, worldOffset, worldPoints, snapToSelf: true);
                    localOffset = activeSpaceMatrices.activeWorldToLocal.MultiplyVector(worldOffset);

                    break;
                }
                case SnapMode.RelativeSnapping:
                {
                    worldOffset = CSGGrid.SnapDeltaRelative(camera, worldOffset);
                    localOffset = activeSpaceMatrices.activeWorldToLocal.MultiplyVector(worldOffset);

                    break;
                }
                default:
                case SnapMode.None:
                {
                    worldOffset = CSGGrid.HandleLockedAxi(worldOffset);
                    localOffset = activeSpaceMatrices.activeWorldToLocal.MultiplyVector(worldOffset);

                    break;
                }
            }

            if(float.IsNaN(localOffset.x) ||
               float.IsNaN(localOffset.y) ||
               float.IsNaN(localOffset.z))
            {
                snappedOffset = offset;

                return;
            }

            snappedOffset = worldOffset;

            int[]     sides      = BoundsUtilities.AABBEdgeSides[edgeCenter];
            Vector3[] tempPoints = new Vector3[targetLocalPoints.Length];

            for(int p = 0; p < tempPoints.Length; p++)
            {
                tempPoints[p] = originalLocalPoints[p];
            }

            for(int s = 0; s < sides.Length; s++)
            {
                int     sideIndex    = sides[s];
                Vector3 localLineOrg = originalLocalCenterPoints[sideIndex];
                Vector3 localLineDir = targetLocalDirections[sideIndex];

                Vector3 sideOffset = GeometryUtility.ProjectPointOnInfiniteLine(
                    localLineOrg + localOffset, localLineOrg, localLineDir
                ) - localLineOrg;

                int[] sidePoints = BoundsUtilities.AABBSidePointIndices[sideIndex];

                for(int p = 0; p < sidePoints.Length; p++)
                {
                    int index = sidePoints[p];
                    tempPoints[index] = GridUtility.CleanPosition(tempPoints[index] + sideOffset);
                }
            }

            AABB newBounds = BoundsUtilities.GetBounds(tempPoints);
            /*
            var centerX = (newBounds.MinX + newBounds.MaxX) * 0.5f;
            var centerY = (newBounds.MinY + newBounds.MaxY) * 0.5f;
            var centerZ = (newBounds.MinZ + newBounds.MaxZ) * 0.5f;

            targetLocalCenterPoints[0] = new Vector3(newBounds.MinX, centerY, centerZ);
            targetLocalCenterPoints[1] = new Vector3(newBounds.MaxX, centerY, centerZ);
            targetLocalCenterPoints[2] = new Vector3(centerX, newBounds.MinY, centerZ);
            targetLocalCenterPoints[3] = new Vector3(centerX, newBounds.MaxY, centerZ);
            targetLocalCenterPoints[4] = new Vector3(centerX, centerY, newBounds.MinZ);
            targetLocalCenterPoints[5] = new Vector3(centerX, centerY, newBounds.MaxZ);

            for (int i = 0; i < BoundsUtilities.AABBEdgeIndices.Length; i++)
            {
                var index1 = BoundsUtilities.AABBEdgeIndices[i][0];
                var index2 = BoundsUtilities.AABBEdgeIndices[i][1];
                var point1 = targetLocalPoints[index1];
                var point2 = targetLocalPoints[index2];
                targetLocalEdgePoints[i] = (point1 + point2) * 0.5f;
            }
            */
            Vector3 old_size = originalLocalTargetBounds.Size;
            Vector3 new_size = newBounds.Size;

            Vector3 world_center_before = originalLocalTargetBounds.Center;
            Vector3 world_center_after  = newBounds.Center;

            Vector3 world_scale = new Vector3(
                new_size.x / old_size.x, new_size.y / old_size.y, new_size.z / old_size.z
            );

            Undo.RecordObjects(brushes, "Scaling brushes");

            for(int t = 0; t < brushes.Length; t++)
            {
                CSGBrush brush = brushes[t];
                brush.ControlMesh = backupControlMeshes[t].Clone();
                brush.Shape       = backupShapes[t].Clone();
            }

            ControlMeshUtility.TransformBrushes(
                brushes, world_center_before, world_center_after, world_scale, activeSpaceMatrices
            );

            InternalCSGModelManager.CheckSurfaceModifications(brushes);

            lastLineMeshGeneration--;
            UpdateTargetBounds();
            UpdateTargetBoundsHandles(camera);
            CSG_EditorGUIUtility.RepaintAll();
        }

        private MouseCursor currentCursor = MouseCursor.Arrow;

        private void UpdateMouseCursor()
        {
            currentCursor = CursorUtility.GetSelectionCursor(SelectionUtility.GetEventSelectionType());
        }

        [NonSerialized] private bool ignorePivotChange = false;

        private void UpdateTransforms(Camera camera)
        {
            if(activeSpaceMatrices == null)
            {
                activeTransform = Selection.activeTransform;

                if(activeTransform != null)
                {
                    activeSpaceMatrices = SpaceMatrices.Create(activeTransform);
                }
            }
            else if(activeTransform != null && !mouseIsDragging)
            {
                if(prevActiveTransformPosition != activeTransform.position ||
                   prevActiveTransformRotation != activeTransform.rotation ||
                   prevActiveTransformScale != activeTransform.lossyScale ||
                   prevToolsRotation != Tools.pivotRotation)
                {
                    if(!ignorePivotChange)
                    {
                        Matrix4x4 transform = activeTransform.localToWorldMatrix * prevInverseMatrix;

                        if(!transform.isIdentity)
                        {
                            worldSpacePivotCenter = transform.MultiplyPoint(worldSpacePivotCenter);
                        }
                    }

                    ignorePivotChange = false;

                    activeSpaceMatrices = SpaceMatrices.Create(activeTransform);

                    UpdateTargetBounds();
                    UpdateTargetBoundsHandles(camera);
                    CSG_EditorGUIUtility.RepaintAll();

                    prevInverseMatrix           = activeTransform.worldToLocalMatrix;
                    prevActiveTransformPosition = activeTransform.position;
                    prevActiveTransformRotation = activeTransform.rotation;
                    prevActiveTransformScale    = activeTransform.lossyScale;
                    prevToolsRotation           = Tools.pivotRotation;
                }
            }
        }

        private void UpdateBoundsCenter(Matrix4x4 localToWorld)
        {
            boundsCenter = localToWorld.MultiplyPoint(targetLocalBounds.Center);

            if(!prevPivotModeSet || prevPivotMode != Tools.pivotMode)
            {
                prevPivotMode            = Tools.pivotMode;
                prevPivotModeSet         = true;
                worldSpacePivotCenterSet = false;
            }

            UpdatePivotCenter();
        }

        private void UpdatePivotCenter()
        {
            if(worldSpacePivotCenterSet)
            {
                return;
            }

            worldSpacePivotCenterSet = true;

            if(Tools.pivotMode == PivotMode.Center)
            {
                worldSpacePivotCenter = boundsCenter;
            }
            else
            {
                if(Selection.activeTransform)
                {
                    worldSpacePivotCenter = Selection.activeTransform.position;
                }
                else
                {
                    worldSpacePivotCenter = Vector3.zero;
                }
            }
        }

        private void UpdateRotationCircle(SceneView sceneView, Matrix4x4 localToWorld, Vector2 mousePosition)
        {
            if(hoverOnBoundsEdge == -1)
            {
                return;
            }

            Camera camera = sceneView.camera;

            if(camera == null)
            {
                return;
            }

            UpdateBoundsCenter(localToWorld);

            Vector3 worldVertex1 = localToWorld.MultiplyPoint(
                targetLocalPoints[BoundsUtilities.AABBEdgeIndices[hoverOnBoundsEdge][0]]
            );

            Vector3 worldVertex2 = localToWorld.MultiplyPoint(
                targetLocalPoints[BoundsUtilities.AABBEdgeIndices[hoverOnBoundsEdge][1]]
            );

            if(camera.orthographic)
            {
                rotateNormal = camera.transform.forward.normalized;
            }
            else
            {
                rotateNormal = (worldVertex2 - worldVertex1).normalized;
            }

            if(Tools.pivotRotation == PivotRotation.Global)
            {
                rotateNormal = GeometryUtility.SnapToClosestAxis(rotateNormal);
            }

            CSGPlane rotatePlane = new CSGPlane(rotateNormal, boundsCenter);
            rotateCenter = rotatePlane.Project(worldSpacePivotCenter);
            Ray ray = HandleUtility.GUIPointToWorldRay(mousePosition);
            rotateMousePosition = rotatePlane.RayIntersection(ray);

            rotateStart = (worldVertex2 + worldVertex1) * 0.5f;
            rotateStart = GeometryUtility.ProjectPointOnPlane(rotatePlane, rotateStart);
            Vector3 delta = rotateCenter - rotateStart;
            rotateTangent = -delta.normalized;

            rotateStartAngle = GeometryUtility.SignedAngle(
                rotateCenter - rotateStart, rotateCenter - rotateMousePosition, rotateNormal
            );

            rotateCurrentSnappedAngle = rotateStartAngle;

            float handleSize = CSG_HandleUtility.GetHandleSize(rotateCenter);
            float minSize    = 4 * handleSize * GUIConstants.MIN_ROTATE_RADIUS;
            rotateRadius = Math.Max(delta.magnitude, minSize);

            rotateAxis = BoundsUtilities.AABBEdgeTangentAxis[hoverOnBoundsEdge];
        }

        private void CreateControlIDs(bool setControlIDs)
        {
            for(int t = 0; t < brushes.Length; t++)
            {
                int controlID = GUIUtility.GetControlID(objectTargetHash, FocusType.Keyboard);

                if(setControlIDs)
                {
                    brushControlIDs[t] = controlID;
                }
            }

            for(int i = 0; i < targetBoundEdgeIDs.Length; i++)
            {
                int controlID = GUIUtility.GetControlID(boundsEdgesTargetHash, FocusType.Keyboard);

                if(setControlIDs)
                {
                    targetBoundEdgeIDs[i] = controlID;
                }
            }

            for(int i = 0; i < targetBoundCenterIDs.Length; i++)
            {
                int controlID = GUIUtility.GetControlID(boundsCenterTargetHash, FocusType.Keyboard);

                if(setControlIDs)
                {
                    targetBoundCenterIDs[i] = controlID;
                }
            }
        }

        private void RenderHighlightedEdge(int highlightEdge)
        {
            Vector3 localPoint1 = targetLocalPoints[BoundsUtilities.AABBEdgeIndices[highlightEdge][0]];
            Vector3 localPoint2 = targetLocalPoints[BoundsUtilities.AABBEdgeIndices[highlightEdge][1]];

            PaintUtility.DrawLine(
                activeSpaceMatrices.activeLocalToWorld, localPoint1, localPoint2, GUIConstants.OLD_THICK_LINE_SCALE
              , ColorSettings.BoundsEdgeHover
            );

            PaintUtility.DrawDottedLine(
                activeSpaceMatrices.activeLocalToWorld, localPoint2, localPoint1, ColorSettings.BoundsEdgeHover
            );
        }

        private void RenderRotationCircle(Camera camera)
        {
            if(!worldSpacePivotCenterSet)
            {
                return;
            }

            string name  = string.Empty;
            Color  color = ColorSettings.RotateCircleOutline;
            rotateCurrentAngle = 0;

            switch(rotateAxis)
            {
                default:
                case PrincipleAxis.X: name = "X"; /*color = ColorSettings.XAxis;*/ break;
                case PrincipleAxis.Y: name = "Y"; /*color = ColorSettings.YAxis;*/ break;
                case PrincipleAxis.Z: name = "Z"; /*color = ColorSettings.ZAxis;*/ break;
            }

            if(mouseIsDragging)
            {
                PaintUtility.DrawRotateCircle(
                    camera, rotateCenter, rotateNormal, rotateTangent, rotateRadius, rotateCurrentAngle
                  , rotateStartAngle, rotateCurrentSnappedAngle,
                    color, name
                ); //, ColorSettings.RotateCircleHatches, name);

                PaintUtility.DrawRotateCirclePie(
                    rotateCenter, rotateNormal, rotateTangent, rotateRadius, rotateCurrentAngle, rotateStartAngle
                  , rotateCurrentSnappedAngle,
                    color
                ); //, RotateCirclePieFill, ColorSettings.RotateCirclePieOutline);
            }
            else
            {
                PaintUtility.DrawRotateCircle(
                    camera, rotateCenter, rotateNormal, rotateTangent, rotateRadius, rotateCurrentAngle
                  , rotateStartAngle, rotateStartAngle,
                    color, name
                ); //, , ColorSettings.RotateCircleHatches, name);
            }

            PaintUtility.DrawDottedLine(rotateCenter, rotateMousePosition, ColorSettings.BoundsEdgeHover);

            if((worldSpacePivotCenter - rotateCenter).magnitude > MathConstants.EqualityEpsilon)
            {
                PaintUtility.DrawDottedLine(worldSpacePivotCenter, rotateCenter, ColorSettings.BoundsEdgeHover);
                PaintUtility.DrawProjectedPivot(camera, rotateCenter, ColorSettings.BoundsEdgeHover);
            }
        }

        private void RenderArrowCap(int boundsCenterIndex)
        {
            Color origColor = Handles.color;
            Handles.color = ColorSettings.PolygonInnerStateColor[(int)(SelectState.Selected | SelectState.Hovering)];

            switch(BoundsUtilities.AABBSideAxis[boundsCenterIndex])
            {
                case PrincipleAxis.X:
                    if(CSGSettings.LockAxisX)
                    {
                        Handles.color = Color.red;
                    }

                    break;
                case PrincipleAxis.Y:
                    if(CSGSettings.LockAxisY)
                    {
                        Handles.color = Color.red;
                    }

                    break;
                case PrincipleAxis.Z:
                    if(CSGSettings.LockAxisZ)
                    {
                        Handles.color = Color.red;
                    }

                    break;
            }

            Vector3 localOrigin =
                activeSpaceMatrices.activeLocalToWorld.MultiplyPoint(targetLocalCenterPoints[boundsCenterIndex]);

            Vector3 localDirection = activeSpaceMatrices.activeLocalToWorld
               .MultiplyVector(targetLocalDirections[boundsCenterIndex])
               .normalized;

            PaintUtility.DrawArrowCap(localOrigin, localDirection, HandleUtility.GetHandleSize(localOrigin) * 0.75f);
            Handles.color = origColor;
        }

        private LineMeshManager zTestLineMeshManager = new LineMeshManager();
        private LineMeshManager noZTestLineMeshManager = new LineMeshManager();
        private int lastLineMeshGeneration = -1;

        private void OnDestroy()
        {
            zTestLineMeshManager.Destroy();
            noZTestLineMeshManager.Destroy();
        }

        [NonSerialized] private Vector2 prevMousePos;

        public void HandleEvents(SceneView sceneView, Rect sceneRect)
        {
            Camera camera   = sceneView.camera;
            bool   inCamera = camera != null && camera.pixelRect.Contains(Event.current.mousePosition);

            EventType originalEventType = Event.current.type;

            if(originalEventType == EventType.MouseMove)
            {
                mouseIsDragging   = false;
                draggingOnCamera  = null;
                realMousePosition = Event.current.mousePosition;
            }
            else if(originalEventType == EventType.MouseDown)
            {
                mouseIsDragging   = false;
                draggingOnCamera  = camera;
                realMousePosition = prevMousePos = Event.current.mousePosition;
            }
            else if(originalEventType == EventType.MouseUp)
            {
                draggingOnCamera = null;
            }
            else if(originalEventType == EventType.MouseDrag)
            {
                if(!mouseIsDragging && (prevMousePos - Event.current.mousePosition).magnitude > 4.0f)
                {
                    mouseIsDragging = true;
                }

                realMousePosition += Event.current.delta;
            }

            try
            {
                if(draggingOnCamera != null)
                {
                    inCamera = false;
                }

                if(draggingOnCamera == camera)
                {
                    inCamera = true;
                }

                //if (Event.current.type == EventType.Layout)
                CreateControlIDs(inCamera);

                UpdateTransforms(camera);

                if(Selection.activeTransform != null)
                {
                    if(activeSpaceMatrices != null)
                    {
                        UpdateBoundsCenter(activeSpaceMatrices.activeLocalToWorld);
                    }
                }

                if(Event.current.type == EventType.Repaint)
                {
                    if(!SceneDragToolManager.IsDraggingObjectInScene)
                    {
                        if(sceneView)
                        {
#if UNITY_2020_2_OR_NEWER
                            if(!Tools.viewToolActive)
#else
							if (Tools.current != Tool.View && !Event.current.alt && Event.current.button != 1 && Event.current.button != 2)
#endif
                            {
                                Rect windowRect = new Rect(
                                    0, 0, sceneView.position.width
                                  , sceneView.position.height - CSG_GUIStyleUtility.BottomToolBarHeight
                                );

                                EditorGUIUtility.AddCursorRect(windowRect, currentCursor);
                            }
                        }

                        if(!mouseIsDragging &&
                           (toolEditMode == ToolEditMode.MovingObject ||
                               toolEditMode == ToolEditMode.CloneDragging) &&
                           CSGGrid.ForceGrid &&
                           startCamera != null &&
                           startCamera == camera)
                        {
                            if((prevModifiers & EventModifiers.Shift)
                            != (Event.current.modifiers & EventModifiers.Shift)
                               //|| prevYMode != RealtimeCSG.Grid.YMoveModeActive
                              )
                            {
                                activeSpaceMatrices = SpaceMatrices.Create(activeTransform);
                                UpdateGridOrientation(realMousePosition);
                            }
                        }

                        if(Tools.current == Tool.Rotate &&
                           hoverOnBoundsEdge != -1 &&
                           mouseIsDragging)
                        {
                            CSGGrid.ForceGrid = false;
                        }

                        if(!mouseIsDragging)
                        {
                            UpdateTargetBounds();
                        }
                    }

                    UpdateTargetBoundsHandles(camera);

                    Matrix4x4 oldMatrix = Handles.matrix;
                    Handles.matrix = MathConstants.identityMatrix;

                    if(lastLineMeshGeneration != InternalCSGModelManager.MeshGeneration)
                    {
                        lastLineMeshGeneration = InternalCSGModelManager.MeshGeneration;

                        Matrix4x4[] brushTransformations = new Matrix4x4[brushes.Length];
                        int[]       brushNodeIDs         = new int[brushes.Length];

                        for(int i = brushes.Length - 1; i >= 0; i--)
                        {
                            CSGBrush brush = brushes[i];

                            if(brush.brushNodeID == CSGNode.InvalidNodeID || // could be a prefab
                               brush.compareTransformation == null ||
                               brush.ChildData == null ||
                               !brush.ChildData.ModelTransform)
                            {
                                ArrayUtility.RemoveAt(ref brushTransformations, i);
                                ArrayUtility.RemoveAt(ref brushNodeIDs, i);

                                continue;
                            }

                            brushTransformations[i] = brush.compareTransformation.localToWorldMatrix;
                            brushNodeIDs[i]         = brush.brushNodeID;
                        }

                        CSGRenderer.DrawSelectedBrushes(
                            zTestLineMeshManager, noZTestLineMeshManager, brushNodeIDs, brushTransformations
                          , ColorSettings.SelectedOutlines, GUIConstants.LINE_SCALE
                        );
                    }

                    MaterialUtility.LineAlphaMultiplier     = 1.0f;
                    MaterialUtility.LineDashMultiplier      = 2.0f;
                    MaterialUtility.LineThicknessMultiplier = 2.0f;
                    noZTestLineMeshManager.Render(MaterialUtility.NoZTestGenericLine);
                    MaterialUtility.LineDashMultiplier      = 1.0f;
                    MaterialUtility.LineThicknessMultiplier = 1.0f;
                    zTestLineMeshManager.Render(MaterialUtility.ZTestGenericLine);

                    if(!SceneDragToolManager.IsDraggingObjectInScene)
                    {
                        if(activeTransform == null)
                        {
                            Handles.matrix = oldMatrix;

                            return;
                        }

                        if(activeSpaceMatrices == null)
                        {
                            activeSpaceMatrices = SpaceMatrices.Create(activeTransform);
                        }

                        Color meshEdgeOutlineColor = ColorSettings.MeshEdgeOutline;
                        Color boundOutlinesColor   = ColorSettings.BoundsOutlines;

                        if(!mouseIsDragging || Tools.current != Tool.Rotate || hoverOnBoundsEdge == -1)
                        {
                            PaintUtility.DrawLines(
                                activeSpaceMatrices.activeLocalToWorld, targetLocalPoints
                              , BoundsUtilities.AABBLineIndices, GUIConstants.OLD_LINE_SCALE * 2.0f, meshEdgeOutlineColor
                            );

                            PaintUtility.DrawUnoccludedLines(
                                activeSpaceMatrices.activeLocalToWorld, targetLocalPoints
                              , BoundsUtilities.AABBLineIndices, meshEdgeOutlineColor
                            );

                            PaintUtility.DrawDottedLines(
                                activeSpaceMatrices.activeLocalToWorld, targetLocalPoints
                              , BoundsUtilities.AABBLineIndices, boundOutlinesColor, 4.0f
                            );

                            PaintUtility.DrawLines(
                                activeSpaceMatrices.activeLocalToWorld, targetLocalPoints
                              , BoundsUtilities.AABBLineIndices, GUIConstants.OLD_LINE_SCALE, boundOutlinesColor
                            );
                        }

                        if(!sceneView.camera.orthographic && CSGSettings.GridVisible)
                        {
                            Handles.matrix = Matrix4x4.identity;

                            int[]     indices  = BoundsUtilities.AABBTopIndices;
                            Vector3[] vertices = new Vector3[indices.Length];

                            float max = float.NegativeInfinity;

                            if(!mouseIsDragging || Tools.current != Tool.Rotate)
                            {
                                for(int i = 0; i < indices.Length; i++)
                                {
                                    Vector3 pt0 =
                                        activeSpaceMatrices.activeLocalToWorld.MultiplyPoint(targetLocalPoints[i]);

                                    Vector3 pt1 = CSGGrid.CurrentGridPlane.Project(pt0);
                                    max = Mathf.Max(max, pt0.y - pt1.y);
                                    PaintUtility.DrawDottedLine(pt0, pt1, boundOutlinesColor, 4.0f);
                                    vertices[i] = pt1;
                                }
                            }
                            else
                            {
                                for(int i = 0; i < indices.Length; i++)
                                {
                                    Vector3 pt0 =
                                        activeSpaceMatrices.activeLocalToWorld.MultiplyPoint(targetLocalPoints[i]);

                                    Vector3 pt1 = CSGGrid.CurrentGridPlane.Project(pt0);
                                    max         = Mathf.Max(max, pt0.y - pt1.y);
                                    vertices[i] = pt1;
                                }
                            }

                            if(max > 0)
                            {
                                PaintUtility.DrawPolygon(vertices, ColorSettings.ShadowColor);
                                PaintUtility.DrawDottedWirePolygon(vertices, ColorSettings.ShadowOutlineColor);
                                PaintUtility.DrawWirePolygon(vertices, ColorSettings.ShadowOutlineColor);
                            }
                            else
                            {
                                //PaintUtility.DrawPolygon(vertices, ShapeDrawingFill);
                                PaintUtility.DrawDottedWirePolygon(vertices, boundOutlinesColor);
                                //PaintUtility.DrawWirePolygon(vertices, boundOutlinesColor);
                            }
                        }

                        Handles.matrix = oldMatrix;

                        if( //hoverOnBoundsEdge == -1 &&
                           (Tools.current == Tool.Scale || Tools.current == Tool.Rect) &&
                           (toolEditMode == ToolEditMode.SizingBounds || toolEditMode == ToolEditMode.None) &&
                           !cloneDragKeyPressed)
                        {
                            if( //!RealtimeCSG.Grid.YMoveModeActive &&
                               hoverOnBoundsEdge != -1 ||
                               hoverOnBoundsCenter != -1 ||
                               !mouseIsDragging)
                            {
                                if(hoverOnBoundsCenter != -1)
                                {
                                    RenderArrowCap(hoverOnBoundsCenter);
                                }

                                PaintUtility.DrawDoubleDots(
                                    camera, activeSpaceMatrices.activeLocalToWorld,
                                    renderLocalCenterPoints,
                                    renderLocalCenterPointSizes,
                                    renderLocalCenterPointColors,
                                    renderLocalCenterPoints.Length
                                );

                                PaintUtility.DrawDoubleDots(
                                    camera, activeSpaceMatrices.activeLocalToWorld,
                                    renderLocalEdgePoints,
                                    renderLocalEdgePointSizes,
                                    renderLocalEdgePointColors,
                                    renderLocalEdgePoints.Length
                                );
                            }

                            if(camera != null)
                            {
                                bool showAxisX;
                                bool showAxisY;
                                bool showAxisZ;

                                if(hoverOnBoundsEdge != -1)
                                {
                                    showAxisX = false;
                                    showAxisY = false;
                                    showAxisZ = false;

                                    switch(BoundsUtilities.AABBEdgeAxis[hoverOnBoundsEdge][0])
                                    {
                                        case PrincipleAxis.X: showAxisX = true; break;
                                        case PrincipleAxis.Y: showAxisY = true; break;
                                        case PrincipleAxis.Z: showAxisZ = true; break;
                                    }

                                    switch(BoundsUtilities.AABBEdgeAxis[hoverOnBoundsEdge][1])
                                    {
                                        case PrincipleAxis.X: showAxisX = true; break;
                                        case PrincipleAxis.Y: showAxisY = true; break;
                                        case PrincipleAxis.Z: showAxisZ = true; break;
                                    }
                                }
                                else if(hoverOnBoundsCenter != -1)
                                {
                                    showAxisX = false;
                                    showAxisY = false;
                                    showAxisZ = false;

                                    switch(BoundsUtilities.AABBSideAxis[hoverOnBoundsCenter])
                                    {
                                        case PrincipleAxis.X: showAxisX = true; break;
                                        case PrincipleAxis.Y: showAxisY = true; break;
                                        case PrincipleAxis.Z: showAxisZ = true; break;
                                    }
                                }
                                else
                                {
                                    showAxisX = true;
                                    showAxisY = true;
                                    showAxisZ = true;
                                }

                                PaintUtility.RenderBoundsSizes(
                                    activeSpaceMatrices.activeWorldToLocal, activeSpaceMatrices.activeLocalToWorld,
                                    camera, targetLocalPoints,
                                    CSGSettings.LockAxisX ? Color.red : Color.white,
                                    CSGSettings.LockAxisY ? Color.red : Color.white,
                                    CSGSettings.LockAxisZ ? Color.red : Color.white,
                                    showAxisX, showAxisY, showAxisZ
                                );
                            }
                        }
                        else if(Tools.current == Tool.Rotate)
                        {
                            if(hoverOnBoundsEdge != -1)
                            {
                                if(inCamera && !mouseIsDragging)
                                {
                                    UpdateRotationCircle(
                                        sceneView, activeSpaceMatrices.activeLocalToWorld, realMousePosition
                                    );
                                }

                                if(!mouseIsDragging)
                                {
                                    RenderHighlightedEdge(hoverOnBoundsEdge);
                                }

                                RenderRotationCircle(camera);
                            }
                        }
                    }
                }

                if(Selection.activeTransform != null)
                {
                    if(Tools.current == Tool.Rotate)
                    {
                        if(worldSpacePivotCenterSet && HaveBrushSelection)
                        {
                            Vector3 newPivot = worldSpacePivotCenter;
                            EditorGUI.BeginChangeCheck();

                            {
                                Quaternion rotation = Tools.handleRotation;

                                newPivot = PaintUtility.HandlePivot(
                                    camera, newPivot, rotation,
                                    ColorSettings.BoundsEdgeHover, Tools.pivotMode == PivotMode.Pivot
                                );
                            }

                            if(EditorGUI.EndChangeCheck())
                            {
                                ignorePivotChange = true;
                                SetPivotCenter(newPivot);
                            }

                            worldSpacePivotCenterSet = true;
                        }
                    }
                    else if(Tools.current == Tool.Move)
                    {
                        if(HaveSelection)
                        {
                            Vector3 newPosition = boundsCenter;

                            Helpers.CSGHandles.InitFunction init = delegate
                            {
                                originalPoint = newPosition;
                                UpdateTargetInfo();
                                snapVertices = originalLocalTargetBounds.GetAllCorners();

                                for(int i = 0; i < snapVertices.Length; i++)
                                {
                                    snapVertices[i] =
                                        activeSpaceMatrices.activeLocalToWorld.MultiplyPoint(snapVertices[i]);
                                }
                            };

                            Helpers.CSGHandles.InitFunction shutdown = delegate { UpdateTargetInfo(); };
                            EditorGUI.BeginChangeCheck();

                            {
                                SnapMode activeSnappingMode = CSGSettings.ActiveSnappingMode;

                                newPosition = Helpers.CSGHandles.PositionHandle(
                                    camera, newPosition,
                                    Tools.handleRotation, activeSnappingMode, snapVertices, init, shutdown
                                );
                            }

                            if(EditorGUI.EndChangeCheck())
                            {
                                MoveCenter(originalPoint, newPosition);
                                UpdateTargetBounds();
                                UpdateTargetBoundsHandles(camera);
                            }
                        }
                        else
                        {
                            snapVertices = null;
                        }
                    }
                }

                switch(Event.current.type)
                {
                    case EventType.ValidateCommand:
                    {
                        if(Keys.cloneDragActivate.IsKeyPressed())
                        {
                            Event.current.Use();

                            break;
                        }

                        if(Keys.HandleSceneValidate(EditModeManager.CurrentTool, false))
                        {
                            Event.current.Use();

                            break;
                        }

                        if(Keys.flipSelectionX.IsKeyPressed())
                        {
                            Event.current.Use();

                            break;
                        }

                        if(Keys.flipSelectionY.IsKeyPressed())
                        {
                            Event.current.Use();

                            break;
                        }

                        if(Keys.flipSelectionZ.IsKeyPressed())
                        {
                            Event.current.Use();

                            break;
                        }

                        if(Keys.rotateSelectionLeft.IsKeyPressed())
                        {
                            Event.current.Use();

                            break;
                        }

                        if(Keys.rotateSelectionRight.IsKeyPressed())
                        {
                            Event.current.Use();

                            break;
                        }

                        if(Keys.moveSelectionLeft.IsKeyPressed())
                        {
                            Event.current.Use();

                            break;
                        }

                        if(Keys.moveSelectionRight.IsKeyPressed())
                        {
                            Event.current.Use();

                            break;
                        }

                        if(Keys.moveSelectionBack.IsKeyPressed())
                        {
                            Event.current.Use();

                            break;
                        }

                        if(Keys.moveSelectionForward.IsKeyPressed())
                        {
                            Event.current.Use();

                            break;
                        }

                        if(Keys.moveSelectionDown.IsKeyPressed())
                        {
                            Event.current.Use();

                            break;
                        }

                        if(Keys.moveSelectionUp.IsKeyPressed())
                        {
                            Event.current.Use();

                            break;
                        }

                        if(Keys.centerPivot.IsKeyPressed())
                        {
                            Event.current.Use();

                            break;
                        }

                        break;
                    }

                    case EventType.KeyDown:
                    {
                        SceneQueryUtility.ClearDeepClick();

                        if(Tools.viewTool != ViewTool.FPS && Keys.cloneDragActivate.IsKeyPressed())
                        {
                            cloneDragKeyPressed = true;
                            Event.current.Use();

                            break;
                        }

                        if(Keys.HandleSceneKeyDown(EditModeManager.CurrentTool, false))
                        {
                            Event.current.Use();

                            break;
                        }

                        if(Keys.flipSelectionX.IsKeyPressed())
                        {
                            Event.current.Use();

                            break;
                        }

                        if(Keys.flipSelectionY.IsKeyPressed())
                        {
                            Event.current.Use();

                            break;
                        }

                        if(Keys.flipSelectionZ.IsKeyPressed())
                        {
                            Event.current.Use();

                            break;
                        }

                        if(Keys.rotateSelectionLeft.IsKeyPressed())
                        {
                            Event.current.Use();

                            break;
                        }

                        if(Keys.rotateSelectionRight.IsKeyPressed())
                        {
                            Event.current.Use();

                            break;
                        }

                        if(Keys.moveSelectionLeft.IsKeyPressed())
                        {
                            Event.current.Use();

                            break;
                        }

                        if(Keys.moveSelectionRight.IsKeyPressed())
                        {
                            Event.current.Use();

                            break;
                        }

                        if(Keys.moveSelectionBack.IsKeyPressed())
                        {
                            Event.current.Use();

                            break;
                        }

                        if(Keys.moveSelectionForward.IsKeyPressed())
                        {
                            Event.current.Use();

                            break;
                        }

                        if(Keys.moveSelectionDown.IsKeyPressed())
                        {
                            Event.current.Use();

                            break;
                        }

                        if(Keys.moveSelectionUp.IsKeyPressed())
                        {
                            Event.current.Use();

                            break;
                        }

                        if(Keys.centerPivot.IsKeyPressed())
                        {
                            Event.current.Use();

                            break;
                        }

                        break;
                    }

                    case EventType.KeyUp:
                    {
                        if(cloneDragKeyPressed && Keys.cloneDragActivate.IsKeyPressed())
                        {
                            cloneDragKeyPressed = false;
                            haveCloned          = false;
                            Event.current.Use();

                            break;
                        }

                        if(Keys.HandleSceneKeyUp(EditModeManager.CurrentTool, false))
                        {
                            Event.current.Use();

                            break;
                        }

                        if(Keys.flipSelectionX.IsKeyPressed())
                        {
                            FlipX();
                            Event.current.Use();

                            break;
                        }

                        if(Keys.flipSelectionY.IsKeyPressed())
                        {
                            FlipY();
                            Event.current.Use();

                            break;
                        }

                        if(Keys.flipSelectionZ.IsKeyPressed())
                        {
                            FlipZ();
                            Event.current.Use();

                            break;
                        }

                        if(Keys.rotateSelectionLeft.IsKeyPressed())
                        {
                            UpdateTargetInfo();

                            Vector3 center =
                                activeSpaceMatrices.activeLocalToWorld.MultiplyPoint(targetLocalBounds.Center);

                            RotateObjects(topTransforms, center, MathConstants.upVector3, CSGSettings.SnapRotation);
                            Event.current.Use();

                            break;
                        }

                        if(Keys.rotateSelectionRight.IsKeyPressed())
                        {
                            UpdateTargetInfo();

                            Vector3 center =
                                activeSpaceMatrices.activeLocalToWorld.MultiplyPoint(targetLocalBounds.Center);

                            RotateObjects(topTransforms, center, MathConstants.upVector3, -CSGSettings.SnapRotation);
                            Event.current.Use();

                            break;
                        }

                        if(Keys.moveSelectionLeft.IsKeyPressed())
                        {
                            UpdateTargetInfo();
                            MoveObjects(topTransforms, MathConstants.rightVector3 * CSGSettings.SnapVector.x);
                            Event.current.Use();

                            break;
                        }

                        if(Keys.moveSelectionRight.IsKeyPressed())
                        {
                            UpdateTargetInfo();
                            MoveObjects(topTransforms, MathConstants.leftVector3 * CSGSettings.SnapVector.x);
                            Event.current.Use();

                            break;
                        }

                        if(Keys.moveSelectionBack.IsKeyPressed())
                        {
                            UpdateTargetInfo();
                            MoveObjects(topTransforms, MathConstants.forwardVector3 * CSGSettings.SnapVector.z);
                            Event.current.Use();

                            break;
                        }

                        if(Keys.moveSelectionForward.IsKeyPressed())
                        {
                            UpdateTargetInfo();
                            MoveObjects(topTransforms, MathConstants.backVector3 * CSGSettings.SnapVector.z);
                            Event.current.Use();

                            break;
                        }

                        if(Keys.moveSelectionDown.IsKeyPressed())
                        {
                            UpdateTargetInfo();
                            MoveObjects(topTransforms, MathConstants.downVector3 * CSGSettings.SnapVector.y);
                            Event.current.Use();

                            break;
                        }

                        if(Keys.moveSelectionUp.IsKeyPressed())
                        {
                            UpdateTargetInfo();
                            MoveObjects(topTransforms, MathConstants.upVector3 * CSGSettings.SnapVector.y);
                            Event.current.Use();

                            break;
                        }

                        if(Keys.centerPivot.IsKeyPressed())
                        {
                            RecenterPivot();

                            break;
                        }

                        break;
                    }

                    case EventType.MouseDown:
                    {
                        if(!sceneRect.Contains(Event.current.mousePosition))
                        {
                            break;
                        }

                        //mouseWasDownNot0 = Event.current.button != 0;
                        //mouseIsDownNot0  = (Event.current.button != 0);
                        if(GUIUtility.hotControl != 0 ||
                           Event.current.button != 0)
                        {
                            break;
                        }

                        toolEditMode        = ToolEditMode.None;
                        extraDeltaMovement  = MathConstants.zeroVector3;
                        activeSpaceMatrices = SpaceMatrices.Create(activeTransform);

                        if(CSGSettings.LockAxisX &&
                           CSGSettings.LockAxisY &&
                           CSGSettings.LockAxisZ)
                        {
                            EditModeManager.ShowMessage("All axi are disabled (X Y Z), cannot move.");

                            break;
                        }

                        if(CSGSettings.SnapVector == MathConstants.zeroVector3)
                        {
                            EditModeManager.ShowMessage("Positional snapping is set to zero, cannot move.");

                            break;
                        }

                        UpdateTargetInfo();

                        if(HaveTarget &&
                           (Tools.current == Tool.Rotate ||
                               Tools.current == Tool.Scale ||
                               Tools.current == Tool.Rect ||
                               Tools.current == Tool.Move)) // && Event.current.modifiers == EventModifiers.None)
                        {
                            int newControlID = -1;

                            if(hoverOnTarget != -1)
                            {
                                if(Event.current.modifiers == EventModifiers.None ||
                                   Event.current.modifiers == EventModifiers.Shift ||
                                   Event.current.modifiers == EventModifiers.Control ||
                                   Event.current.modifiers == (EventModifiers.Shift | EventModifiers.Control))
                                {
                                    toolEditMode = ToolEditMode.MovingObject;
                                    newControlID = brushControlIDs[hoverOnTarget];
                                }
                            }
                            else if(Tools.current != Tool.Move)
                            {
                                if(hoverOnBoundsCenter != -1)
                                {
                                    if(Event.current.modifiers == EventModifiers.None)
                                    {
                                        toolEditMode = ToolEditMode.SizingBounds;
                                        newControlID = targetBoundCenterIDs[hoverOnBoundsCenter];
                                    }
                                }
                                else if(hoverOnBoundsEdge != -1)
                                {
                                    if(Event.current.modifiers == EventModifiers.None)
                                    {
                                        toolEditMode              = ToolEditMode.RotatingBounds;
                                        newControlID              = targetBoundEdgeIDs[hoverOnBoundsEdge];
                                        rotateCurrentSnappedAngle = rotateStartAngle;
                                    }
                                }
                            }

                            if(newControlID != -1)
                            {
                                GUIUtility.hotControl             = newControlID;
                                GUIUtility.keyboardControl        = newControlID;
                                EditorGUIUtility.editingTextField = false;
                                Event.current.Use();
                                firstMove = true;

                                break;
                            }
                        }

                        break;
                    }

                    case EventType.MouseUp:
                    {
                        if(Event.current.button != 0)
                        {
                            break;
                        }

                        if(!mouseIsDragging) // && GUIUtility.hotControl == 0)
                        {                    /*
                            if (hoverOnTarget != -1 &&
                                hoverOnBoundsCenter != -1 &&
                                hoverOnBoundsEdge != -1)*/
                            {
                                SelectionUtility.DoSelectionClick(sceneView);
                            }
                        }

                        mouseIsDragging = false;

                        break;
                    }

                    case EventType.Layout:
                    {
                        UpdateMouseCursor();

                        try
                        {
                            if(inCamera && !mouseIsDragging &&
                               GUIUtility.hotControl == 0)
                            {
                                //var forward = camera.transform.forward.normalized;
                                //var view_is_2D = camera.orthographic &&
                                //				 (
                                //					(Mathf.Abs(forward.x) >= 1.0f - MathConstants.EqualityEpsilon) ||
                                //					(Mathf.Abs(forward.y) >= 1.0f - MathConstants.EqualityEpsilon) ||
                                //					(Mathf.Abs(forward.z) >= 1.0f - MathConstants.EqualityEpsilon)
                                //				 );

                                UpdateTargetBoundsHandles(camera);

                                activeTransform = Selection.activeTransform;

                                if(activeTransform != null)
                                {
                                    activeSpaceMatrices = SpaceMatrices.Create(activeTransform);

                                    hoverOnTarget       = -1;
                                    hoverOnBoundsCenter = -1;
                                    hoverOnBoundsEdge   = -1;

                                    Ray     world_ray = HandleUtility.GUIPointToWorldRay(Event.current.mousePosition);
                                    Vector3 ray_start = world_ray.origin;

                                    Vector3 ray_vector = world_ray.direction
                                      * (camera.farClipPlane - camera.nearClipPlane);

                                    Vector3 ray_end = ray_start + ray_vector;

                                    float min_distance  = float.PositiveInfinity;
                                    int   closest_brush = -1;

                                    for(int t = 0; t < brushes.Length; t++)
                                    {
                                        CSGBrush  brush           = brushes[t];
                                        Transform model_transform = parentModelTransforms[t];

                                        if(model_transform == null)
                                        {
                                            continue;
                                        }

                                        Matrix4x4 modelTransformation = model_transform.localToWorldMatrix;

                                        LegacyBrushIntersection intersection;

                                        if(SceneQueryUtility.FindBrushIntersection(
                                               brush, modelTransformation, ray_start, ray_end, out intersection
                                           ))
                                        {
                                            float distance = (intersection.worldIntersection - ray_start).magnitude;

                                            if(distance < min_distance)
                                            {
                                                min_distance           = distance;
                                                closest_brush          = t;
                                                hoverOverBrushIndex    = t;
                                                hoverIntersectionPoint = intersection.worldIntersection;
                                            }
                                        }
                                    }

                                    if(closest_brush == -1 && brushes.Length > 0)
                                    {
                                        Matrix4x4 worldToLocal = activeSpaceMatrices.activeWorldToLocal;
                                        Bounds    bounds       = new Bounds();
                                        bounds.center = targetLocalBounds.Center;
                                        bounds.size   = targetLocalBounds.Size;

                                        if(bounds.size.x != 0 &&
                                           bounds.size.y != 0 &&
                                           bounds.size.z != 0 &&
                                           !float.IsNaN(bounds.size.x) &&
                                           !float.IsNaN(bounds.size.y) &&
                                           !float.IsNaN(bounds.size.z) &&
                                           !float.IsInfinity(bounds.size.x) &&
                                           !float.IsInfinity(bounds.size.y) &&
                                           !float.IsInfinity(bounds.size.z))
                                        {
                                            Ray local_ray = world_ray;
                                            local_ray.direction = worldToLocal.MultiplyVector(local_ray.direction);
                                            local_ray.origin    = worldToLocal.MultiplyPoint(local_ray.origin);

                                            if(bounds.IntersectRay(local_ray))
                                            {
                                                closest_brush = 0;
                                            }
                                        }
                                    }

                                    if(closest_brush >= 0 && closest_brush <= brushes.Length)
                                    {
                                        HandleUtility.AddControl(brushControlIDs[closest_brush], 3.0f);
                                    }

                                    MouseCursor newCursor      = MouseCursor.Arrow;
                                    bool        foundControl   = false;
                                    int         nearestControl = -1;

                                    //if (!camera.orthographic)
                                    {
                                        Matrix4x4 matrix = activeSpaceMatrices.activeLocalToWorld;

                                        if( //!RealtimeCSG.Grid.YMoveModeActive &&
                                           (Tools.current == Tool.Scale ||
                                               Tools.current == Tool.Rect) &&
                                           !cloneDragKeyPressed)
                                        {
                                            for(int t = 0; t < targetBoundCenterIDs.Length; t++)
                                            {
                                                float radius = renderLocalCenterPointSizes[t]
                                                  * GUIConstants.HOVER_HANDLE_SCALE;

                                                if(radius == 0)
                                                {
                                                    continue;
                                                }

                                                float distance = HandleUtility.DistanceToCircle(
                                                    matrix.MultiplyPoint(targetLocalCenterPoints[t]), radius
                                                ) * 0.5f;

                                                HandleUtility.AddControl(targetBoundCenterIDs[t], distance);
                                            }

                                            nearestControl = HandleUtility.nearestControl;

                                            if(nearestControl > 0)
                                            {
                                                for(int t = 0; t < targetBoundCenterIDs.Length; t++)
                                                {
                                                    if(targetBoundCenterIDs[t] == nearestControl)
                                                    {
                                                        hoverIntersectionPoint =
                                                            matrix.MultiplyPoint(targetLocalCenterPoints[t]);

                                                        hoverOnBoundsCenter = t;

                                                        newCursor = CursorUtility.GetCursorForDirection(
                                                            Matrix4x4.identity, targetLocalCenterPoints[t]
                                                          , targetLocalDirections[t]
                                                        );

                                                        foundControl = true;

                                                        break;
                                                    }
                                                }
                                            }
                                        }

                                        if(Tools.current == Tool.Scale || Tools.current == Tool.Rect
                                        || Tools.current == Tool.Rotate)
                                        {
                                            if(camera != null && !foundControl && !cloneDragKeyPressed)
                                            {
                                                CSGPlane cameraPlane = CSG_HandleUtility.GetNearPlane(camera);

                                                /*
                                                 * TODO:
                                                 * 1. determine on which polygon we're on
                                                 * 2. determine which edge we're close too
                                                 * 3. determine the opposite direction on the polygon
                                                 * 4. create new second point that goes in that direction in the length of the bounding box
                                                 * 5. determine the size between the 2 points as WorldToGUIPoint
                                                 * 6. divide by 3, this is your maximum distance to the line
                                                 */
                                                Vector2 mousePoint = Event.current.mousePosition;

                                                for(int t = 0; t < targetBoundEdgeIDs.Length; t++)
                                                {
                                                    bool side1 = backfaced[BoundsUtilities.AABBEdgeSides[t][0]];
                                                    bool side2 = backfaced[BoundsUtilities.AABBEdgeSides[t][1]];

                                                    if(Tools.current == Tool.Rotate &&
                                                       side1 && side2)
                                                    {
                                                        continue;
                                                    }

                                                    Vector3 point1 =
                                                        targetLocalPoints[BoundsUtilities.AABBEdgeIndices[t][0]];

                                                    Vector3 point2 =
                                                        targetLocalPoints[BoundsUtilities.AABBEdgeIndices[t][1]];

                                                    float distance;

                                                    if(Tools.current == Tool.Scale ||
                                                       Tools.current == Tool.Rect)
                                                    {
                                                        if(renderLocalEdgePointSizes[t] > 0 && !camera.orthographic)
                                                        {
                                                            distance = HandleUtility.DistanceToCircle(
                                                                matrix.MultiplyPoint(renderLocalEdgePoints[t])
                                                              , renderLocalEdgePointSizes[t]
                                                            ) * 0.5f;

                                                            HandleUtility.AddControl(targetBoundEdgeIDs[t], distance);
                                                        }
                                                        else
                                                        {
                                                            distance = Mathf.Min(
                                                                CameraUtility.DistanceToLine(
                                                                    cameraPlane, mousePoint, point1, point2
                                                                ) * 3.0f,
                                                                HandleUtility.DistanceToCircle(
                                                                    matrix.MultiplyPoint(renderLocalEdgePoints[t])
                                                                  , renderLocalEdgePointSizes[t]
                                                                ) * 0.5f
                                                            );

                                                            HandleUtility.AddControl(targetBoundEdgeIDs[t], distance);
                                                        }
                                                    }
                                                    else
                                                    {
                                                        distance = CameraUtility.DistanceToLine(
                                                            cameraPlane, mousePoint, matrix.MultiplyPoint(point1)
                                                          , matrix.MultiplyPoint(point2)
                                                        );

                                                        HandleUtility.AddControl(targetBoundEdgeIDs[t], distance);
                                                    }
                                                }
                                            }
                                        }

                                        nearestControl    = HandleUtility.nearestControl;
                                        CSGGrid.ForceGrid = false;

                                        if(nearestControl > 0 && !foundControl)
                                        {
                                            //if (Tools.current == Tool.Rotate ||
                                            //	view_is_2D)
                                            if( //!RealtimeCSG.Grid.YMoveModeActive &&
                                               Tools.current == Tool.Scale || Tools.current == Tool.Rect
                                            || Tools.current == Tool.View || Tools.current == Tool.Rotate)
                                            {
                                                for(int t = 0; t < targetBoundEdgeIDs.Length; t++)
                                                {
                                                    if(targetBoundEdgeIDs[t] == nearestControl)
                                                    {
                                                        Vector3 point1 =
                                                            targetLocalPoints[BoundsUtilities.AABBEdgeIndices[t][0]];

                                                        Vector3 point2 =
                                                            targetLocalPoints[BoundsUtilities.AABBEdgeIndices[t][1]];

                                                        hoverIntersectionPoint =
                                                            activeSpaceMatrices.activeLocalToWorld.MultiplyPoint(
                                                                (point1 + point2) * 0.5f
                                                            );

                                                        if(Tools.current == Tool.Rotate ||
                                                           renderLocalEdgePointSizes[t] > 0)
                                                        {
                                                            hoverOnBoundsEdge = t;

                                                            int     side1         = BoundsUtilities.AABBEdgeSides[t][0];
                                                            int     side2         = BoundsUtilities.AABBEdgeSides[t][1];
                                                            Vector3 localSideDir1 = targetLocalDirections[side1];
                                                            Vector3 localSideDir2 = targetLocalDirections[side2];

                                                            Vector3 localNormal = Vector3.Cross(
                                                                localSideDir1, localSideDir2
                                                            );

                                                            movePlane = new CSGPlane(
                                                                GridUtility.CleanNormal(
                                                                    activeSpaceMatrices.activeLocalToWorld
                                                                       .MultiplyVector(localNormal)
                                                                ),
                                                                activeSpaceMatrices.activeLocalToWorld.MultiplyPoint(
                                                                    hoverIntersectionPoint
                                                                )
                                                            );

                                                            if(Tools.current == Tool.Scale ||
                                                               Tools.current == Tool.Rect)
                                                            {
                                                                CSGGrid.SetForcedGrid(camera, movePlane);
                                                            }

                                                            if(Tools.current == Tool.Rotate)
                                                            {
                                                                newCursor = MouseCursor.RotateArrow; /*
                                                            else
                                                                newCursor = CursorUtility.GetCursorForDirection(activeSpaceMatrices.activeLocalToWorld,
                                                                                                targetLocalCenterPoints[side], targetLocalDirections[side]);*/
                                                            }

                                                            foundControl = true;
                                                        }
                                                        else
                                                        {
                                                            for(int i = 0; i < 2; i++)
                                                            {
                                                                int side = BoundsUtilities.AABBEdgeSides[t][i];

                                                                if(renderLocalCenterPointSizes[side] == 0)
                                                                {
                                                                    continue;
                                                                }

                                                                float radius = renderLocalCenterPointSizes[side]
                                                                  * GUIConstants.HOVER_HANDLE_SCALE;

                                                                if(radius != 0)
                                                                {
                                                                    hoverOnBoundsCenter = side;
                                                                    foundControl        = true;

                                                                    newCursor = CursorUtility.GetCursorForDirection(
                                                                        Matrix4x4.identity
                                                                      , targetLocalCenterPoints[side]
                                                                      , targetLocalDirections[side]
                                                                    );

                                                                    break;
                                                                }
                                                            }
                                                        }

                                                        break;
                                                    }
                                                }
                                            }

                                            if(Tools.current == Tool.Scale ||
                                               Tools.current == Tool.Rect ||
                                               Tools.current == Tool.Move)
                                            {
                                                for(int t = 0; t < brushes.Length; t++)
                                                {
                                                    if(brushControlIDs[t] == nearestControl)
                                                    {
                                                        hoverOnTarget = t;
                                                        newCursor     = MouseCursor.MoveArrow;
                                                        foundControl  = true;

                                                        break;
                                                    }
                                                }
                                            }
                                        }
                                    }

                                    if(newCursor != currentCursor)
                                    {
                                        currentCursor = newCursor;
                                        CSG_EditorGUIUtility.RepaintAll();
                                    }
                                }
                            }
                        }
                        catch
                        {
                            throw;
                        }

                        break;
                    }
                }

                int currentHotControl = GUIUtility.hotControl;

                for(int i = 0; i < targetBoundCenterIDs.Length; i++)
                {
                    int targetBoundCenterID = targetBoundCenterIDs[i];

                    if(currentHotControl != targetBoundCenterID)
                    {
                        continue;
                    }

                    EventType type = Event.current.GetTypeForControl(targetBoundCenterID);

                    switch(type)
                    {
                        case EventType.MouseDrag:
                        {
                            if(Event.current.button != 0)
                            {
                                break;
                            }

                            if(CSGSettings.SnapVector == MathConstants.zeroVector3)
                            {
                                EditModeManager.ShowMessage("Positional snapping is set to zero, cannot move.");

                                break;
                            }

                            EditModeManager.ResetMessage();

                            if(firstMove)
                            {
                                EditorGUIUtility.SetWantsMouseJumping(1);
                                extraDeltaMovement = MathConstants.zeroVector3;
                                originalPoint      = hoverIntersectionPoint;
                                startCamera        = camera;
                                UpdateGrid(startCamera);
                            }

                            Ray mouseRay = HandleUtility.GUIPointToWorldRay(Event.current.mousePosition);

                            Vector3 worldIntersection = movePlane.RayIntersection(mouseRay);

                            if(float.IsNaN(worldIntersection.x) || float.IsNaN(worldIntersection.y)
                            || float.IsNaN(worldIntersection.z))
                            {
                                break;
                            }

                            Vector3 localLineOrg = originalLocalCenterPoints[i];
                            Vector3 localLineDir = targetLocalDirections[i];

                            Vector3 localIntersection =
                                activeSpaceMatrices.activeWorldToLocal.MultiplyPoint(worldIntersection);

                            worldIntersection = GridUtility.CleanPosition(worldIntersection);

                            worldIntersection = activeSpaceMatrices.activeLocalToWorld.MultiplyPoint(
                                GeometryUtility.ProjectPointOnInfiniteLine(
                                    localIntersection, localLineOrg, localLineDir
                                )
                            );

                            if(firstMove)
                            {
                                originalPoint      = worldIntersection;
                                worldDeltaMovement = MathConstants.zeroVector3;
                                firstMove          = false;
                            }
                            else
                            {
                                worldDeltaMovement = SnapMovementToPlane(worldIntersection - originalPoint);
                            }

                            if(float.IsNaN(worldDeltaMovement.x) || float.IsNaN(worldDeltaMovement.y)
                            || float.IsNaN(worldDeltaMovement.z))
                            {
                                break;
                            }

                            SnapMode activeSnappingMode = CSGSettings.ActiveSnappingMode;
                            MoveBoundsCenter(camera, i, worldDeltaMovement, out worldDeltaMovement, activeSnappingMode);

                            break;
                        }
                        case EventType.MouseUp:
                        {
                            if(Event.current.button != 0)
                            {
                                break;
                            }

                            EditorGUIUtility.SetWantsMouseJumping(0);
                            startCamera  = null;
                            toolEditMode = ToolEditMode.None;

                            UpdateTargetInfo();

                            GUIUtility.hotControl             = 0;
                            GUIUtility.keyboardControl        = 0;
                            EditorGUIUtility.editingTextField = false;
                            Event.current.Use();

                            CSGGrid.ForceGrid = false;
                            CSG_EditorGUIUtility.RepaintAll();

                            break;
                        }
                        case EventType.Repaint:
                        {
                            if(!inCamera)
                            {
                                break;
                            }

                            Vector2 textCenter2D = Event.current.mousePosition;
                            textCenter2D.y += hover_text_distance * 2;
                            Vector3 delta = worldDeltaMovement;

                            if(Tools.pivotRotation == PivotRotation.Local)
                            {
                                delta = GridUtility.CleanPosition(
                                    activeSpaceMatrices.activeLocalToWorld.MultiplyVector(delta)
                                );
                            }

                            float length = delta.magnitude;

                            bool disabled = false;

                            if(hoverOnBoundsCenter != -1)
                            {
                                switch(BoundsUtilities.AABBSideAxis[hoverOnBoundsCenter])
                                {
                                    case PrincipleAxis.X: disabled = CSGSettings.LockAxisX; break;
                                    case PrincipleAxis.Y: disabled = CSGSettings.LockAxisY; break;
                                    case PrincipleAxis.Z: disabled = CSGSettings.LockAxisZ; break;
                                }
                            }

                            if(disabled)
                            {
                                switch(BoundsUtilities.AABBSideAxis[hoverOnBoundsCenter])
                                {
                                    case PrincipleAxis.X:
                                        PaintUtility.DrawScreenText(
                                            textCenter2D, "X axis is locked", CSG_GUIStyleUtility.redTextArea
                                        ); break;
                                    case PrincipleAxis.Y:
                                        PaintUtility.DrawScreenText(
                                            textCenter2D, "Y axis is locked", CSG_GUIStyleUtility.redTextArea
                                        ); break;
                                    case PrincipleAxis.Z:
                                        PaintUtility.DrawScreenText(
                                            textCenter2D, "Z axis is locked", CSG_GUIStyleUtility.redTextArea
                                        ); break;
                                }
                            }
                            else
                            {
                                PaintUtility.DrawScreenText(
                                    textCenter2D,
                                    "Δ" + Units.ToRoundedDistanceString(length)
                                );
                            }

                            break;
                        }
                    }
                }

                for(int i = 0; i < brushControlIDs.Length; i++)
                {
                    int brushControlID = brushControlIDs[i];

                    if(currentHotControl != brushControlID)
                    {
                        continue;
                    }

                    EventType type = Event.current.GetTypeForControl(brushControlID);

                    switch(type)
                    {
                        case EventType.MouseDrag:
                        {
                            if(Event.current.button != 0)
                            {
                                break;
                            }

                            if(CSGSettings.SnapVector == MathConstants.zeroVector3)
                            {
                                EditModeManager.ShowMessage("Positional snapping is set to zero, cannot move.");

                                break;
                            }

                            EditModeManager.ResetMessage();

                            if(firstMove)
                            {
                                EditorGUIUtility.SetWantsMouseJumping(1);
                                extraDeltaMovement = MathConstants.zeroVector3;
                                originalPoint      = hoverIntersectionPoint;
                                startCamera        = camera;
                                UpdateGrid(startCamera);

                                for(int b = 0; b < topTransforms.Length; b++)
                                {
                                    if(!topTransforms[b])
                                    {
                                        continue;
                                    }

                                    GameObject gameObject = topTransforms[b].gameObject;

                                    if(gameObject.activeInHierarchy &&
                                       !CSGPrefabUtility.IsPrefabAsset(gameObject))
                                    {
                                        dragGameObjects.Add(gameObject);
                                    }
                                }

                                sourceSurfaceAlignment      = PrefabSourceAlignment.AlignedFront;
                                destinationSurfaceAlignment = PrefabDestinationAlignment.AlignToSurface;

                                if(activeTransform != null)
                                {
                                    CSGNode topNode = activeTransform.GetComponentInChildren<CSGNode>();

                                    if(topNode)
                                    {
                                        sourceSurfaceAlignment      = topNode.PrefabSourceAlignment;
                                        destinationSurfaceAlignment = topNode.PrefabDestinationAlignment;
                                    }
                                }

                                BoundsUtilities.GetBoundsCornerPoints(targetLocalBounds, projectedBounds);
                            }

                            Ray     mouseRay          = HandleUtility.GUIPointToWorldRay(Event.current.mousePosition);
                            Vector3 worldIntersection = movePlane.RayIntersection(mouseRay);

                            if(!float.IsNaN(worldIntersection.x) && !float.IsNaN(worldIntersection.y)
                            && !float.IsNaN(worldIntersection.z))
                            {
                                if((prevModifiers & EventModifiers.Shift)
                                != (SelectionUtility.CurrentModifiers & EventModifiers.Shift) &&
                                   //|| prevYMode != RealtimeCSG.Grid.YMoveModeActive
                                   CSGGrid.ForceGrid && startCamera != null)
                                {
                                    prevModifiers = SelectionUtility.CurrentModifiers;
                                    //prevYMode = RealtimeCSG.Grid.YMoveModeActive;
                                    UpdateGridOrientation(realMousePosition);
                                    extraDeltaMovement += worldDeltaMovement;
                                }

                                worldIntersection = GeometryUtility.ProjectPointOnPlane(movePlane, worldIntersection);

                                if(firstMove)
                                {
                                    originalPoint      = worldIntersection;
                                    worldDeltaMovement = MathConstants.zeroVector3;
                                    firstMove          = false;
                                }
                                else
                                {
                                    worldDeltaMovement = //SnapMovementToPlane
                                        worldIntersection - originalPoint;
                                }
                            }

                            if(worldDeltaMovement != MathConstants.zeroVector3 &&
                               cloneDragKeyPressed &&
                               toolEditMode == ToolEditMode.MovingObject &&
                               !float.IsNaN(worldDeltaMovement.x) &&
                               !float.IsNaN(worldDeltaMovement.y) &&
                               !float.IsNaN(worldDeltaMovement.z))
                            {
                                toolEditMode  = ToolEditMode.CloneDragging;
                                topTransforms = EditModeManager.CloneTargets();

                                for(int b = 0; b < topTransforms.Length; b++)
                                {
                                    if(!topTransforms[b])
                                    {
                                        continue;
                                    }

                                    GameObject gameObject = topTransforms[b].gameObject;

                                    if(gameObject.activeInHierarchy &&
                                       !CSGPrefabUtility.IsPrefabAsset(gameObject))
                                    {
                                        dragGameObjects.Add(gameObject);
                                    }
                                }
                            }

                            bool useRaySnapping =
                                (Event.current.modifiers & EventModifiers.Shift) == EventModifiers.Shift;

                            if(useRaySnapping)
                            {
                                ignoreSetTargets = true;

                                try
                                {
                                    if(hoverOverBrushIndex < backupRotations.Length)
                                    {
                                        Vector3    brushPosition = backupPositions[hoverOverBrushIndex];
                                        Quaternion brushRotation = backupRotations[hoverOverBrushIndex];
                                        SelectionUtility.HideObjectsRemoteOnly(dragGameObjects);

                                        LegacyBrushIntersection intersection = SceneQueryUtility.FindMeshIntersection(
                                            camera, Event.current.mousePosition
                                        );

                                        Vector3 normal = intersection.worldPlane.normal;

                                        Vector3 hoverPosition = intersection.worldIntersection;

                                        Quaternion hoverRotation = SelectionUtility.FindDragOrientation(
                                            sceneView, normal, sourceSurfaceAlignment, destinationSurfaceAlignment
                                        );

                                        CSGGrid.SetForcedGrid(camera, intersection.worldPlane);

                                        SnapMode activeSnappingMode = CSGSettings.ActiveSnappingMode;

                                        switch(activeSnappingMode)
                                        {
                                            case SnapMode.GridSnapping:
                                            {
                                                Vector3[] localPoints = new Vector3[8];
                                                CSGPlane  localPlane  = intersection.worldPlane;

                                                for(int p = 0; p < localPoints.Length; p++)
                                                {
                                                    localPoints[p] = GeometryUtility.ProjectPointOnPlane(
                                                        localPlane, hoverRotation * projectedBounds[p] + hoverPosition
                                                    );
                                                }

                                                hoverPosition += CSGGrid.SnapDeltaToGrid(
                                                    camera, MathConstants.zeroVector3, localPoints
                                                );

                                                break;
                                            }
                                            case SnapMode.RelativeSnapping:
                                            {
                                                hoverPosition += CSGGrid.SnapDeltaRelative(
                                                    camera, MathConstants.zeroVector3
                                                );

                                                break;
                                            }
                                        }

                                        hoverPosition = GeometryUtility.ProjectPointOnPlane(
                                            intersection.worldPlane, hoverPosition
                                        ); // + (normal * 0.01f);

                                        worldDeltaMovement = hoverPosition - brushPosition;

                                        Undo.RecordObjects(topTransforms, "Drag objects");
                                        UpdateTargetBounds();
                                        CSG_EditorGUIUtility.RepaintAll();
                                        Vector3 center = GridUtility.CleanPosition(brushPosition + worldDeltaMovement);
                                        Quaternion rotation = hoverRotation * Quaternion.Inverse(brushRotation);

                                        MoveRotateObjects(topTransforms, center, rotation, worldDeltaMovement);
                                    }
                                }
                                finally
                                {
                                    ignoreSetTargets = false;
                                    SelectionUtility.ShowObjectsAndUpdate(dragGameObjects);

                                    if(!UpdateLoop.IsActive())
                                    {
                                        UpdateLoop.ResetUpdateRoutine();
                                    }

                                    for(int b = 0; b < brushes.Length; b++)
                                    {
                                        if(brushes[b])
                                        {
                                            BrushOutlineManager.ForceUpdateOutlines(brushes[b].brushNodeID);
                                        }
                                    }

                                    CSG_EditorGUIUtility.RepaintAll();
                                }
                            }
                            else
                            {
                                if(float.IsNaN(worldDeltaMovement.x) || float.IsNaN(worldDeltaMovement.y)
                                || float.IsNaN(worldDeltaMovement.z))
                                {
                                    break;
                                }

                                SnapMode activeSnappingMode = CSGSettings.ActiveSnappingMode;

                                if(MoveObjects(
                                       camera, topTransforms, worldDeltaMovement, out worldDeltaMovement
                                     , activeSnappingMode
                                   ))
                                {
                                    originalPoint = worldIntersection;
                                }
                            }

                            break;
                        }
                        case EventType.MouseUp:
                        {
                            if(Event.current.button != 0)
                            {
                                break;
                            }

                            EditorGUIUtility.SetWantsMouseJumping(0);
                            startCamera  = null;
                            toolEditMode = ToolEditMode.None;

                            UpdateTargetInfo();

                            GUIUtility.hotControl             = 0;
                            GUIUtility.keyboardControl        = 0;
                            EditorGUIUtility.editingTextField = false;
                            Event.current.Use();

                            CSGGrid.ForceGrid = false;
                            CSG_EditorGUIUtility.RepaintAll();

                            break;
                        }
                        case EventType.Repaint:
                        {
                            Vector2 textCenter2D = Event.current.mousePosition;
                            textCenter2D.y += hover_text_distance * 2;

                            Vector3 movement = worldDeltaMovement + extraDeltaMovement;

                            bool lockX = Mathf.Abs(movement.x) < MathConstants.ConsideredZero && (CSGSettings.LockAxisX
                             || Mathf.Abs(movePlane.a) >= 1 - MathConstants.EqualityEpsilon);

                            bool lockY = Mathf.Abs(movement.y) < MathConstants.ConsideredZero && (CSGSettings.LockAxisY
                             || Mathf.Abs(movePlane.b) >= 1 - MathConstants.EqualityEpsilon);

                            bool lockZ = Mathf.Abs(movement.z) < MathConstants.ConsideredZero && (CSGSettings.LockAxisZ
                             || Mathf.Abs(movePlane.c) >= 1 - MathConstants.EqualityEpsilon);

                            string text = Units.ToRoundedDistanceString(movement, lockX, lockY, lockZ);
                            PaintUtility.DrawScreenText(textCenter2D, text);

                            break;
                        }
                    }
                }

                if(Tools.current == Tool.Rotate)
                {
                    for(int i = 0; i < targetBoundEdgeIDs.Length; i++)
                    {
                        int targetBoundEdgeID = targetBoundEdgeIDs[i];

                        if(currentHotControl != targetBoundEdgeID)
                        {
                            continue;
                        }

                        EventType type = Event.current.GetTypeForControl(targetBoundEdgeID);

                        switch(type)
                        {
                            case EventType.MouseDrag:
                            {
                                if(Event.current.button != 0)
                                {
                                    break;
                                }

                                if(CSGSettings.SnapRotation % 360 == 0)
                                {
                                    EditModeManager.ShowMessage("Rotational snapping is set to zero, cannot rotate.");

                                    break;
                                }

                                EditModeManager.ResetMessage();

                                if(firstMove)
                                {
                                    EditorGUIUtility.SetWantsMouseJumping(1);
                                    extraDeltaMovement = MathConstants.zeroVector3;
                                    originalPoint      = hoverIntersectionPoint;
                                    startCamera        = camera;
                                    UpdateGrid(startCamera);
                                }

                                Ray mouseRay = HandleUtility.GUIPointToWorldRay(realMousePosition);

                                firstMove = false;
                                CSGPlane rotatePlane = new CSGPlane(rotateNormal, boundsCenter);
                                rotateMousePosition = rotatePlane.RayIntersection(mouseRay);

                                RotateObjects(
                                    topTransforms, rotateCenter, rotateNormal, rotateStart, rotateStartAngle
                                  , rotateMousePosition, out rotateCurrentSnappedAngle
                                );

                                break;
                            }
                            case EventType.MouseUp:
                            {
                                if(Event.current.button != 0)
                                {
                                    break;
                                }

                                EditorGUIUtility.SetWantsMouseJumping(0);
                                startCamera  = null;
                                toolEditMode = ToolEditMode.None;

                                UpdateTargetInfo();

                                GUIUtility.hotControl             = 0;
                                GUIUtility.keyboardControl        = 0;
                                EditorGUIUtility.editingTextField = false;
                                Event.current.Use();

                                CSGGrid.ForceGrid = false;
                                CSG_EditorGUIUtility.RepaintAll();

                                break;
                            }
                            case EventType.Repaint:
                            {
                                break;
                            }
                        }
                    }
                }
                else
                {
                    for(int i = 0; i < targetBoundEdgeIDs.Length; i++)
                    {
                        int targetBoundEdgeID = targetBoundEdgeIDs[i];

                        if(currentHotControl != targetBoundEdgeID)
                        {
                            continue;
                        }

                        EventType type = Event.current.GetTypeForControl(targetBoundEdgeID);

                        switch(type)
                        {
                            case EventType.MouseDrag:
                            {
                                if(Event.current.button != 0)
                                {
                                    break;
                                }

                                if(CSGSettings.SnapVector == MathConstants.zeroVector3)
                                {
                                    EditModeManager.ShowMessage("Positional snapping is set to zero, cannot move.");

                                    break;
                                }

                                EditModeManager.ResetMessage();

                                if(firstMove)
                                {
                                    EditorGUIUtility.SetWantsMouseJumping(1);
                                    originalPoint = hoverIntersectionPoint;
                                    startCamera   = camera;

                                    if(activeSpaceMatrices == null)
                                    {
                                        activeSpaceMatrices = SpaceMatrices.Create(activeTransform);
                                    }

                                    Vector3 localLineOrg  = originalLocalEdgePoints[i];
                                    int     side1         = BoundsUtilities.AABBEdgeSides[i][0];
                                    int     side2         = BoundsUtilities.AABBEdgeSides[i][1];
                                    Vector3 localSideDir1 = targetLocalDirections[side1];
                                    Vector3 localSideDir2 = targetLocalDirections[side2];
                                    Vector3 localNormal   = Vector3.Cross(localSideDir1, localSideDir2);

                                    movePlane = new CSGPlane(
                                        GridUtility.CleanNormal(
                                            activeSpaceMatrices.activeLocalToWorld.MultiplyVector(localNormal)
                                        ),
                                        activeSpaceMatrices.activeLocalToWorld.MultiplyPoint(localLineOrg)
                                    );

                                    CSGGrid.SetForcedGrid(camera, movePlane);
                                }

                                Ray mouseRay = HandleUtility.GUIPointToWorldRay(realMousePosition);

                                Vector3 worldIntersection = movePlane.RayIntersection(mouseRay);

                                if(float.IsNaN(worldIntersection.x) || float.IsNaN(worldIntersection.y)
                                || float.IsNaN(worldIntersection.z))
                                {
                                    break;
                                }

                                worldIntersection = GeometryUtility.ProjectPointOnPlane(movePlane, worldIntersection);

                                if(firstMove)
                                {
                                    originalPoint      = worldIntersection;
                                    worldDeltaMovement = MathConstants.zeroVector3;
                                    firstMove          = false;
                                }
                                else
                                {
                                    worldDeltaMovement = SnapMovementToPlane(worldIntersection - originalPoint);
                                }

                                if(float.IsNaN(worldDeltaMovement.x) || float.IsNaN(worldDeltaMovement.y)
                                || float.IsNaN(worldDeltaMovement.z))
                                {
                                    break;
                                }

                                SnapMode activeSnappingMode = CSGSettings.ActiveSnappingMode;

                                MoveBoundsEdge(
                                    camera, i, worldDeltaMovement, out worldDeltaMovement, activeSnappingMode
                                );

                                break;
                            }
                            case EventType.MouseUp:
                            {
                                if(Event.current.button != 0)
                                {
                                    break;
                                }

                                EditorGUIUtility.SetWantsMouseJumping(0);
                                startCamera  = null;
                                toolEditMode = ToolEditMode.None;

                                UpdateTargetInfo();

                                GUIUtility.hotControl             = 0;
                                GUIUtility.keyboardControl        = 0;
                                EditorGUIUtility.editingTextField = false;
                                Event.current.Use();

                                CSGGrid.ForceGrid = false;
                                CSG_EditorGUIUtility.RepaintAll();

                                break;
                            }
                            case EventType.Repaint:
                            {
                                Vector2 textCenter2D = realMousePosition;
                                textCenter2D.y += hover_text_distance * 2;
                                Vector3 delta = worldDeltaMovement;

                                if(Tools.pivotRotation == PivotRotation.Local)
                                {
                                    delta = GridUtility.CleanPosition(
                                        activeSpaceMatrices.activeLocalToWorld.MultiplyVector(delta)
                                    );
                                }

                                float length = delta.magnitude;

                                PaintUtility.DrawScreenText(
                                    textCenter2D,
                                    "Δ" + Units.ToRoundedDistanceString(length)
                                );

                                // TODO: show actual current length of this axis
                                break;
                            }
                        }
                    }
                }
            }
            finally
            {
                if(originalEventType == EventType.MouseUp ||
                   originalEventType == EventType.MouseMove)
                {
                    mouseIsDragging = false;
                }
            }
        }

        public void FlipX()
        {
            BrushOperations.FlipX(brushes);
            UpdateTargetInfo();
        }

        public void FlipY()
        {
            BrushOperations.FlipY(brushes);
            UpdateTargetInfo();
        }

        public void FlipZ()
        {
            BrushOperations.FlipZ(brushes);
            UpdateTargetInfo();
        }

        private static Vector2 scrollPos;

        public void OnInspectorGUI(EditorWindow window, float height)
        {
            scrollPos = EditorGUILayout.BeginScrollView(scrollPos);

            {
                EditmodePlaceGUI.OnInspectorGUI(window, height);
            }

            EditorGUILayout.EndScrollView();
        }

        public Rect GetLastSceneGUIRect()
        {
            return EditmodePlaceGUI.GetLastSceneGUIRect(this);
        }

        public bool OnSceneGUI(Rect windowRect)
        {
            if(brushes == null || brushes.Length == 0)
            {
                return false;
            }

            EditmodePlaceGUI.OnSceneGUI(windowRect, this);

            return false;
        }
    }
}
