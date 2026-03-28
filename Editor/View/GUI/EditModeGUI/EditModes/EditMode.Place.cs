using System;
using System.Collections.Generic;
using System.Linq;
using InternalRealtimeCSG;
using RealtimeCSG.Components;
using RealtimeCSG.Helpers;
using RealtimeCSG.Legacy;
using UnityEditor;
using UnityEngine;
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
            get => _hoverOnTarget != -1 || _hoverOnBoundsCenter != -1 || _hoverOnBoundsEdge != -1;
        }

        public bool HaveSelection
        {
            get => brushes.Length != 0 || topTransforms.Length != 0;
        }

        public bool HaveBrushSelection
        {
            get => brushes.Length != 0;
        }

        private const float HOVER_TEXT_DISTANCE = 40.0f;

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

        private ToolEditMode _toolEditMode = ToolEditMode.None;

#endregion

#region Input

        private bool _cloneDragKeyPressed;
        private bool _mouseIsDragging;
        private bool _firstMove;
        private Camera _draggingOnCamera;

#endregion

#region Misc state

        [SerializeField] private bool toolIsEnabled;
        [SerializeField] private bool shouldHideTool;

#endregion

#region Hover Targets

        private int _hoverOnTarget = -1;
        private int _hoverOnBoundsCenter = -1;
        private int _hoverOnBoundsEdge = -1;

#endregion

#region Dragging

        private Camera _startCamera;
        private CSGPlane _movePlane;

        private Vector3 _hoverIntersectionPoint = MathConstants.zeroVector3;
        private Vector3 _originalPoint = MathConstants.zeroVector3;
        private Vector3 _extraDeltaMovement = MathConstants.zeroVector3;
        private Vector3 _worldDeltaMovement = MathConstants.zeroVector3;
        private Vector2 _realMousePosition;
        private Vector3[] _snapVertices;

        private EventModifiers _prevModifiers = EventModifiers.None;

#endregion

#region Rotation

        [SerializeField] private bool prevPivotModeSet;
        [SerializeField] private PivotMode prevPivotMode;
        [SerializeField] private bool worldSpacePivotCenterSet;
        [SerializeField] private Vector3 worldSpacePivotCenter;
        private Vector3 _boundsCenter;
        private Vector3 _rotateCenter;
        private Vector3 _rotateNormal;
        private Vector3 _rotateTangent;

        private Vector3 _rotateStart;

        private Vector3 _rotateMousePosition;
        private float _rotateStartAngle;
        private float _rotateCurrentAngle;
        private float _rotateCurrentSnappedAngle;
        private float _rotateRadius;
        private PrincipleAxis _rotateAxis;

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

                _ignorePivotChange = true;
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

                _ignorePivotChange = true;
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
        [SerializeField] private AABB targetLocalBounds;

        [SerializeField] private Vector3[] renderLocalCenterPoints = new Vector3[6];
        [SerializeField] private float[] renderLocalCenterPointSizes = new float[6];
        [SerializeField] private Color[] renderLocalCenterPointColors = new Color[12];

        [SerializeField] private Vector3[] renderLocalEdgePoints = new Vector3[12];
        [SerializeField] private float[] renderLocalEdgePointSizes = new float[12];
        [SerializeField] private Color[] renderLocalEdgePointColors = new Color[24];

        [SerializeField] private AABB originalLocalTargetBounds;
        [SerializeField] private Vector3[] originalLocalPoints = new Vector3[8];
        [SerializeField] private Vector3[] originalLocalCenterPoints = new Vector3[6];
        [SerializeField] private Vector3[] originalLocalEdgePoints = new Vector3[12];

        [SerializeField] private bool[] backfaced = new bool[6];      // [-X, +X, -Y, +Y, -Z, +Z]
        [SerializeField] private CSGPlane[] planes = new CSGPlane[6]; // [-X, +X, -Y, +Y, -Z, +Z]

#endregion

#region Transforms

        [SerializeField] private Transform activeTransform;
        [SerializeField] private Vector3 prevActiveTransformPosition = Vector3.zero;
        [SerializeField] private Quaternion prevActiveTransformRotation = Quaternion.identity;
        [SerializeField] private Vector3 prevActiveTransformScale = Vector3.one;
        [SerializeField] private Matrix4x4 prevInverseMatrix = Matrix4x4.identity;
        [SerializeField] private PivotRotation prevToolsRotation = PivotRotation.Local;
        [SerializeField] private SpaceMatrices activeSpaceMatrices = new SpaceMatrices();

#endregion

#region TargetInfo

        [SerializeField] private Transform[] topTransforms = Array.Empty<Transform>();
        [SerializeField] private Vector3[] backupPositions = Array.Empty<Vector3>();       // transforms
        [SerializeField] private Quaternion[] backupRotations = Array.Empty<Quaternion>(); // transforms
        private bool _ignoreSetTargets;

        [SerializeField] private CSGBrush[] brushes = Array.Empty<CSGBrush>();
        [SerializeField] private Transform[] parentModelTransforms = Array.Empty<Transform>(); // brush
        [SerializeField] private int[] brushControlIDs = Array.Empty<int>();                   // brush
        [SerializeField] private Shape[] backupShapes;                                         // brush
        [SerializeField] private ControlMesh[] backupControlMeshes;                            // brush

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
            if(_ignoreSetTargets)
            {
                return;
            }

            CSGBrush[]  newBrushes       = filteredSelection.GetAllContainedBrushes().ToArray();
            Transform[] newTopTransforms = filteredSelection.GetAllTopTransforms().ToArray();

            if(brushes.Length == newBrushes.Length && topTransforms.Length == newTopTransforms.Length)
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
                backupShapes[i]        = brushes[i].shape.Clone();
                backupControlMeshes[i] = brushes[i].controlMesh.Clone();

                if(brushes[i].childData == null || brushes[i].childData.modelTransform == null)
                {
                    parentModelTransforms[i] = null;
                }
                else
                {
                    parentModelTransforms[i] = brushes[i].childData.modelTransform;
                }
            }

            UpdateTargetInfo();

            worldSpacePivotCenterSet = false;
            worldSpacePivotCenter    = Vector3.zero;

            shouldHideTool = (CSGSettings.SnapNonCSGObjects && Tools.current == Tool.Move) || filteredSelection.nodeTargets.Length > 0;

            if(toolIsEnabled)
            {
                Tools.hidden = shouldHideTool;
            }

            _lastLineMeshGeneration--;
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

            _toolEditMode        = ToolEditMode.None;
            _cloneDragKeyPressed = false;

            _startCamera = null;

            _hoverOnTarget       = -1;
            _hoverOnBoundsCenter = -1;
            _hoverOnBoundsEdge   = -1;

            _originalPoint      = MathConstants.zeroVector3;
            _worldDeltaMovement = MathConstants.zeroVector3;

            _prevModifiers = EventModifiers.None;

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

            targetLocalBounds = Tools.pivotRotation == PivotRotation.Local ? BoundsUtilities.GetLocalBounds(brushes, activeSpaceMatrices.activeWorldToLocal) : BoundsUtilities.GetBounds(brushes);

            if(shouldHideTool && brushes.Length == 0 && activeTransform.GetComponent<CSGNode>() == null)
            {
                Bounds     bounds     = new Bounds();
                bool       found      = false;
                MeshFilter meshFilter = activeTransform.GetComponent<MeshFilter>();

                if(meshFilter != null && meshFilter.sharedMesh)
                {
                    bounds = meshFilter.sharedMesh.bounds;
                    found  = true;
                }

                MeshCollider meshCollider = activeTransform.GetComponent<MeshCollider>();

                if(meshCollider != null && meshCollider.sharedMesh)
                {
                    bounds = meshCollider.sharedMesh.bounds;
                    found  = true;
                }

                if(found)
                {
                    if(Tools.pivotRotation == PivotRotation.Local)
                    {
                        Matrix4x4 transformToGridspace = activeSpaceMatrices.activeWorldToLocal * activeTransform.localToWorldMatrix;

                        targetLocalBounds.Extend(transformToGridspace.MultiplyPoint(new Vector3(bounds.min.x, bounds.min.y, bounds.min.z)));
                        targetLocalBounds.Extend(transformToGridspace.MultiplyPoint(new Vector3(bounds.max.x, bounds.min.y, bounds.min.z)));
                        targetLocalBounds.Extend(transformToGridspace.MultiplyPoint(new Vector3(bounds.max.x, bounds.min.y, bounds.max.z)));
                        targetLocalBounds.Extend(transformToGridspace.MultiplyPoint(new Vector3(bounds.min.x, bounds.min.y, bounds.max.z)));
                        targetLocalBounds.Extend(transformToGridspace.MultiplyPoint(new Vector3(bounds.min.x, bounds.max.y, bounds.min.z)));
                        targetLocalBounds.Extend(transformToGridspace.MultiplyPoint(new Vector3(bounds.max.x, bounds.max.y, bounds.min.z)));
                        targetLocalBounds.Extend(transformToGridspace.MultiplyPoint(new Vector3(bounds.max.x, bounds.max.y, bounds.max.z)));
                        targetLocalBounds.Extend(transformToGridspace.MultiplyPoint(new Vector3(bounds.min.x, bounds.max.y, bounds.max.z)));
                    }
                    else
                    {
                        Matrix4x4 transformToGridspace = activeTransform.localToWorldMatrix;

                        targetLocalBounds.Extend(transformToGridspace.MultiplyPoint(new Vector3(bounds.min.x, bounds.min.y, bounds.min.z)));
                        targetLocalBounds.Extend(transformToGridspace.MultiplyPoint(new Vector3(bounds.max.x, bounds.min.y, bounds.min.z)));
                        targetLocalBounds.Extend(transformToGridspace.MultiplyPoint(new Vector3(bounds.max.x, bounds.min.y, bounds.max.z)));
                        targetLocalBounds.Extend(transformToGridspace.MultiplyPoint(new Vector3(bounds.min.x, bounds.min.y, bounds.max.z)));
                        targetLocalBounds.Extend(transformToGridspace.MultiplyPoint(new Vector3(bounds.min.x, bounds.max.y, bounds.min.z)));
                        targetLocalBounds.Extend(transformToGridspace.MultiplyPoint(new Vector3(bounds.max.x, bounds.max.y, bounds.min.z)));
                        targetLocalBounds.Extend(transformToGridspace.MultiplyPoint(new Vector3(bounds.max.x, bounds.max.y, bounds.max.z)));
                        targetLocalBounds.Extend(transformToGridspace.MultiplyPoint(new Vector3(bounds.min.x, bounds.max.y, bounds.max.z)));
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

            for(int i = 0; i < BoundsUtilities.aabbEdgeIndices.Length; i++)
            {
                int     index1 = BoundsUtilities.aabbEdgeIndices[i][0];
                int     index2 = BoundsUtilities.aabbEdgeIndices[i][1];
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

            _lastLineMeshGeneration--;
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

            for(int i = 0; i < BoundsUtilities.aabbEdgeIndices.Length; i++)
            {
                int     index1 = BoundsUtilities.aabbEdgeIndices[i][0];
                int     index2 = BoundsUtilities.aabbEdgeIndices[i][1];
                Vector3 point1 = targetLocalPoints[index1];
                Vector3 point2 = targetLocalPoints[index2];
                renderLocalEdgePoints[i] = (point1 + point2) * 0.5f;
            }

            Vector3 cameraPosition  = activeSpaceMatrices.activeWorldToLocal.MultiplyPoint(camera.transform.position);
            Vector3 cameraDirection = activeSpaceMatrices.activeWorldToLocal.MultiplyVector(camera.transform.forward);
            bool    ortho           = camera.orthographic;

            for(int i = 0; i < BoundsUtilities.aabbSidePointIndices.Length; i++)
            {
                planes[i] = new CSGPlane(
                    targetLocalPoints[BoundsUtilities.aabbSidePointIndices[i][0]],
                    targetLocalPoints[BoundsUtilities.aabbSidePointIndices[i][1]],
                    targetLocalPoints[BoundsUtilities.aabbSidePointIndices[i][2]]
                );

                if(ortho)
                {
                    backfaced[i] = Mathf.Abs(Vector3.Dot(planes[i].Normal, cameraDirection)) > 0.9999f;
                }
                else
                {
                    backfaced[i] = Vector3.Dot(planes[i].Normal, cameraPosition - renderLocalCenterPoints[i]) < 0;
                }
            }

            int pt1 = -1, pt2 = -1;

            if(_hoverOnBoundsCenter != -1)
            {
                pt1 = _hoverOnBoundsCenter;
            }
            else if(ortho && _hoverOnBoundsEdge != -1)
            {
                pt1 = BoundsUtilities.aabbEdgeIndices[_hoverOnBoundsEdge][0];
                pt2 = BoundsUtilities.aabbEdgeIndices[_hoverOnBoundsEdge][1];
            }

            Matrix4x4 origMatrix = Handles.matrix;
            Handles.matrix = activeSpaceMatrices.activeLocalToWorld;

            for(int i = renderLocalCenterPoints.Length - 1; i >= 0; i--)
            {
                int   state  = (int)(i == pt1 || i == pt2 ? SelectState.Hovering : SelectState.None);
                Color color1 = ColorSettings.MeshEdgeOutline;
                Color color2 = ColorSettings.PolygonInnerStateColor[state];

                bool disabled = BoundsUtilities.aabbSideAxis[i] switch
                {
                    PrincipleAxis.X => CSGSettings.LockAxisX
                  , PrincipleAxis.Y => CSGSettings.LockAxisY
                  , PrincipleAxis.Z => CSGSettings.LockAxisZ
                  , var _           => false
                };

                if(disabled)
                {
                    color2 = Color.red;
                }

                float handleSize = CSGHandleUtility.GetHandleSize(renderLocalCenterPoints[i]);

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

                renderLocalCenterPointSizes[i] = handleSize;

                renderLocalCenterPointColors[i * 2 + 0] = color1;
                renderLocalCenterPointColors[i * 2 + 1] = color2;
            }

            for(int i = renderLocalEdgePoints.Length - 1; i >= 0; i--)
            {
                int   state  = (int)(i == _hoverOnBoundsEdge ? SelectState.Hovering : SelectState.None);
                Color color1 = ColorSettings.MeshEdgeOutline;
                Color color2 = ColorSettings.PointInnerStateColor[state];

                bool disabled1 = BoundsUtilities.aabbEdgeAxis[i][0] switch
                {
                    PrincipleAxis.X => CSGSettings.LockAxisX
                  , PrincipleAxis.Y => CSGSettings.LockAxisY
                  , PrincipleAxis.Z => CSGSettings.LockAxisZ
                  , _               => false
                };

                bool disabled2 = BoundsUtilities.aabbEdgeAxis[i][1] switch
                {
                    PrincipleAxis.X => CSGSettings.LockAxisX
                  , PrincipleAxis.Y => CSGSettings.LockAxisY
                  , PrincipleAxis.Z => CSGSettings.LockAxisZ
                  , var _           => false
                };

                if(disabled1 && disabled2)
                {
                    color2 = Color.red;
                }

                float handleSize = CSGHandleUtility.GetHandleSize(renderLocalEdgePoints[i]);
                int   side1      = BoundsUtilities.aabbEdgeSides[i][0];
                int   side2      = BoundsUtilities.aabbEdgeSides[i][1];

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
                backupShapes[i]        = brushes[i].shape.Clone();
                backupControlMeshes[i] = brushes[i].controlMesh.Clone();
            }

            _lastLineMeshGeneration--;
        }

        public bool UndoRedoPerformed()
        {
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

            if(Tools.pivotMode == PivotMode.Center || !Selection.activeTransform)
            {
                return;
            }

            UpdateTargetBounds();

            _ignorePivotChange = true;
            _boundsCenter      = activeSpaceMatrices.activeLocalToWorld.MultiplyPoint(targetLocalBounds.Center);
            SetPivotCenter(_boundsCenter);

            UpdatePivotCenter();
        }

        public void SetPivotCenter(Vector3 newCenter, bool registerUndo = true)
        {
            if(Tools.pivotMode == PivotMode.Center || !Selection.activeTransform)
            {
                return;
            }

            if(registerUndo)
            {
                List<Object> objects = new List<Object>(topTransforms.Length + brushes.Length);
                objects.AddRange(topTransforms);
                objects.AddRange(brushes);
                objects.Add(this);
                Undo.RegisterCompleteObjectUndo(objects.ToArray(), "Setting pivot");
            }

            newCenter = GridUtility.CleanPosition(newCenter);

            Vector3 realCenter = Selection.activeTransform.position;
            Vector3 difference = newCenter - realCenter;

            if(difference.x != 0.0f || difference.y != 0.0f || difference.z != 0.0f)
            {
                foreach(Transform topTransform in topTransforms)
                {
                    topTransform.position += difference;
                }

                GeometryUtility.MoveControlMeshVertices(brushes, -difference);
                SurfaceUtility.TranslateSurfacesInWorldSpace(brushes, -difference);
                ControlMeshUtility.RebuildShapes(brushes);
                InternalCSGModelManager.CheckForChanges();
                UpdateTargetInfo();
            }

            worldSpacePivotCenterSet = true;
            worldSpacePivotCenter    = newCenter;
        }

        private bool _haveCloned;

        public void MoveCenter(Vector3 oldCenter, Vector3 newCenter, bool registerUndo = true)
        {
            if(!Selection.activeTransform)
            {
                return;
            }

            if(registerUndo)
            {
                List<Object> objects = new List<Object>(topTransforms.Length + brushes.Length);
                objects.AddRange(topTransforms);
                objects.AddRange(brushes);
                objects.Add(this);
                Undo.RegisterCompleteObjectUndo(objects.ToArray(), "Moving Selection");
            }

            Vector3 difference = newCenter - oldCenter;

            if(!float.IsNaN(difference.x) &&
               !float.IsNaN(difference.y) &&
               !float.IsNaN(difference.z) &&
               !float.IsInfinity(difference.x) &&
               !float.IsInfinity(difference.y) &&
               !float.IsInfinity(difference.z))
            {
                if(difference != Vector3.zero)
                {
                    if(_cloneDragKeyPressed && !_haveCloned)
                    {
                        _haveCloned = true;

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

            if(_hoverOnBoundsCenter != -1)
            {
                Vector3 worldOrigin    = targetLocalCenterPoints[_hoverOnBoundsCenter];
                Vector3 worldDirection = targetLocalDirections[_hoverOnBoundsCenter];

                worldOrigin    = activeSpaceMatrices.activeLocalToWorld.MultiplyPoint(worldOrigin);
                worldDirection = activeSpaceMatrices.activeLocalToWorld.MultiplyVector(worldDirection);

                CSGGrid.SetupRayWorkPlane(camera, worldOrigin, worldDirection, ref _movePlane);
            }
            else
            {
                CSGGrid.SetupWorkPlane(camera, _originalPoint, ref _movePlane);
            }
        }

        private void UpdateGridOrientation(Vector2 mousePosition)
        {
            Ray     mouseRay     = HandleUtility.GUIPointToWorldRay(mousePosition);
            Vector3 intersection = _movePlane.RayIntersection(mouseRay);
            _originalPoint = GeometryUtility.ProjectPointOnPlane(_movePlane, intersection);

            UpdateGrid(_startCamera);
            _prevModifiers = Event.current.modifiers;
            UpdateTargetInfo();
        }

        private Vector3 SnapMovementToPlane(Vector3 offset)
        {
            if(Math.Abs(_movePlane.a) > 1 - MathConstants.NORMAL_EPSILON)
            {
                offset.x = 0.0f;
            }

            if(Math.Abs(_movePlane.b) > 1 - MathConstants.NORMAL_EPSILON)
            {
                offset.y = 0.0f;
            }

            if(Math.Abs(_movePlane.c) > 1 - MathConstants.NORMAL_EPSILON)
            {
                offset.z = 0.0f;
            }

            return offset;
        }

        private bool MoveRotateObjects(Transform[] transforms, Vector3 center, Quaternion rotationQuaternion, Vector3 offset)
        {
            // -rotateCenter
            // rotate with rotationQuaternion
            // +rotateCenter
            Matrix4x4 matrix2      = Matrix4x4.TRS(-center, MathConstants.identityQuaternion, MathConstants.oneVector3);
            Matrix4x4 matrix3      = Matrix4x4.TRS(center, rotationQuaternion, MathConstants.oneVector3);
            Matrix4x4 rotateMatrix = matrix3 * matrix2;

            Undo.RecordObjects(transforms, "Rotating brushes");

            for(int t = 0; t < transforms.Length; t++)
            {
                transforms[t].position = GridUtility.CleanPosition(rotateMatrix.MultiplyPoint(backupPositions[t] + offset));
                transforms[t].rotation = rotationQuaternion * backupRotations[t];
            }

            return false;
        }

        private void RotateObjects(Transform[] transforms, Vector3 center, Quaternion rotationQuaternion)
        {
            // -rotateCenter
            // rotate with rotationQuaternion
            // +rotateCenter
            Matrix4x4 matrix2      = Matrix4x4.TRS(-center, MathConstants.identityQuaternion, MathConstants.oneVector3);
            Matrix4x4 matrix3      = Matrix4x4.TRS(center, rotationQuaternion, MathConstants.oneVector3);
            Matrix4x4 rotateMatrix = matrix3 * matrix2;

            Undo.RecordObjects(transforms, "Rotating brushes");

            for(int t = 0; t < transforms.Length; t++)
            {
                transforms[t].position = GridUtility.CleanPosition(rotateMatrix.MultiplyPoint(backupPositions[t]));
                transforms[t].rotation = rotationQuaternion * backupRotations[t];
            }

            _lastLineMeshGeneration--;
            activeSpaceMatrices = null;
            UpdateTargetBounds();
            CSGEditorGUIUtility.RepaintAll();
        }

        private void RotateObjects(Transform[] transforms, Vector3 center, Vector3 normal, float angle)
        {
            Quaternion rotationQuaternion = Quaternion.AngleAxis(angle, normal);

            RotateObjects(transforms, center, rotationQuaternion);
        }

        private void RotateObjects(Transform[] transforms, Vector3 center, Vector3 normal, Vector3 pointStart, float startAngle, Vector3 currentPoint, out float currentAngle)
        {
            currentAngle = GeometryUtility.SignedAngle(center - pointStart, center - currentPoint, normal);
            currentAngle = GridUtility.SnappedAngle(currentAngle - startAngle) + startAngle;

            RotateObjects(transforms, center, normal, currentAngle - startAngle);
        }

        private bool MoveObjects(Camera camera, Transform[] transforms, Vector3 offset, out Vector3 snappedOffset, SnapMode snapMode)
        {
            // move an object on the grid
            switch(snapMode)
            {
                case SnapMode.GridSnapping:
                {
                    CSGPlane localPlane = GeometryUtility.TransformPlane(activeSpaceMatrices.activeWorldToLocal, _movePlane);

                    localPlane.Normal = GridUtility.CleanNormal(localPlane.Normal);
                    Vector3[] localPoints = originalLocalTargetBounds.GetAllCorners();

                    for(int i = 0; i < localPoints.Length; i++)
                    {
                        localPoints[i] = activeSpaceMatrices.activeLocalToWorld.MultiplyPoint(GeometryUtility.ProjectPointOnPlane(localPlane, localPoints[i]));
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

            if(offset != MathConstants.zeroVector3 && _cloneDragKeyPressed)
            {
                if(_toolEditMode == ToolEditMode.MovingObject)
                {
                    if(float.IsNaN(offset.x) || float.IsNaN(offset.y) || float.IsNaN(offset.z))
                    {
                        return false;
                    }

                    _toolEditMode = ToolEditMode.CloneDragging;

                    EditModeManager.CloneTargets(
                        delegate(Transform newTransform, Transform originalTransform)
                        {
                            newTransform.localScale    = originalTransform.localScale;
                            newTransform.localPosition = originalTransform.localPosition + offset;
                            newTransform.localRotation = originalTransform.localRotation;
                        }
                    );

                    UpdateTargetBounds();
                    CSGEditorGUIUtility.RepaintAll();

                    return true;
                }
            }

            return MoveObjects(transforms, offset);
        }

        private bool MoveObjects(Transform[] transforms, Vector3 offset)
        {
            if(float.IsNaN(offset.x) || float.IsNaN(offset.y) || float.IsNaN(offset.z))
            {
                return false;
            }

            Undo.RecordObjects(transforms, "Move objects");

            for(int t = 0; t < transforms.Length; t++)
            {
                transforms[t].position = GridUtility.CleanPosition(backupPositions[t] + offset);
            }

            UpdateTargetBounds();
            CSGEditorGUIUtility.RepaintAll();

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

        private void MoveBoundsCenter(Camera camera, int boundsCenterIndex, Vector3 offset, out Vector3 snappedOffset, SnapMode snapMode)
        {
            Vector3 worldOffset = offset;
            Vector3 localOffset = activeSpaceMatrices.activeWorldToLocal.MultiplyVector(worldOffset);

            // move a surface in the direction of it's normal
            switch(snapMode)
            {
                case SnapMode.GridSnapping:
                {
                    Vector3   worldLineOrg = activeSpaceMatrices.activeLocalToWorld.MultiplyPoint(originalLocalCenterPoints[boundsCenterIndex]);
                    Vector3   worldLineDir = activeSpaceMatrices.activeLocalToWorld.MultiplyVector(targetLocalDirections[boundsCenterIndex]);
                    Vector3[] worldPoints  = { activeSpaceMatrices.activeLocalToWorld.MultiplyPoint(originalLocalCenterPoints[boundsCenterIndex]) };

                    worldOffset = CSGGrid.SnapDeltaToRayGrid(camera, new Ray(worldLineOrg, worldLineDir), worldOffset, worldPoints, true);
                    localOffset = activeSpaceMatrices.activeWorldToLocal.MultiplyVector(worldOffset);

                    break;
                }
                case SnapMode.RelativeSnapping:
                {
                    Vector3 worldLineOrg = activeSpaceMatrices.activeLocalToWorld.MultiplyPoint(originalLocalCenterPoints[boundsCenterIndex]);
                    Vector3 worldLineDir = activeSpaceMatrices.activeLocalToWorld.MultiplyVector(targetLocalDirections[boundsCenterIndex]);

                    worldOffset = CSGGrid.SnapDeltaToRayRelative(camera, new Ray(worldLineOrg, worldLineDir), worldOffset);
                    localOffset = activeSpaceMatrices.activeWorldToLocal.MultiplyVector(worldOffset);

                    break;
                }
            }

            if(float.IsNaN(localOffset.x) || float.IsNaN(localOffset.y) || float.IsNaN(localOffset.z))
            {
                snappedOffset = offset;

                return;
            }

            snappedOffset = worldOffset;

            targetLocalCenterPoints[boundsCenterIndex] = GridUtility.CleanPosition(originalLocalCenterPoints[boundsCenterIndex] + localOffset);

            int[] boundPoints = BoundsUtilities.aabbSidePointIndices[boundsCenterIndex];

            for(int p = 0; p < boundPoints.Length; p++)
            {
                int index = boundPoints[p];
                targetLocalPoints[index] = GridUtility.CleanPosition(originalLocalPoints[index] + localOffset);
            }

            int index1, index2;

            switch(boundsCenterIndex)
            {
                default:
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

            float oldDeltaX = -new CSGPlane(targetLocalDirections[0], originalLocalCenterPoints[0]).Distance(originalLocalCenterPoints[1]);
            float oldDeltaY = -new CSGPlane(targetLocalDirections[2], originalLocalCenterPoints[2]).Distance(originalLocalCenterPoints[3]);
            float oldDeltaZ = -new CSGPlane(targetLocalDirections[4], originalLocalCenterPoints[4]).Distance(originalLocalCenterPoints[5]);

            float newDeltaX = oldDeltaX + ((boundsCenterIndex & 1) == 1 ? localOffset.x : -localOffset.x);
            float newDeltaY = oldDeltaY + ((boundsCenterIndex & 1) == 1 ? localOffset.y : -localOffset.y);
            float newDeltaZ = oldDeltaZ + ((boundsCenterIndex & 1) == 1 ? localOffset.z : -localOffset.z);

            Vector3 worldCenterBefore = (originalLocalCenterPoints[index1] + originalLocalCenterPoints[index2]) * 0.5f;
            Vector3 worldCenterAfter  = worldCenterBefore + localOffset * 0.5f;
            Vector3 worldScale        = new Vector3(newDeltaX / oldDeltaX, newDeltaY / oldDeltaY, newDeltaZ / oldDeltaZ);

            Undo.RecordObjects(brushes, "Scaling brushes");

            for(int t = 0; t < brushes.Length; t++)
            {
                CSGBrush brush = brushes[t];
                brush.controlMesh = backupControlMeshes[t].Clone();
                brush.shape       = backupShapes[t].Clone();
            }

            ControlMeshUtility.TransformBrushes(brushes, worldCenterBefore, worldCenterAfter, worldScale, activeSpaceMatrices);

            InternalCSGModelManager.CheckSurfaceModifications(brushes);

            _lastLineMeshGeneration--;
            UpdateTargetBounds();
            UpdateTargetBoundsHandles(camera);
            CSGEditorGUIUtility.RepaintAll();
        }

        private void MoveBoundsEdge(Camera camera, int edgeCenter, Vector3 offset, out Vector3 snappedOffset, SnapMode snapMode)
        {
            Vector3 worldOffset = offset;
            Vector3 localOffset;

            // move a surface in the direction of it's normal
            switch(snapMode)
            {
                case SnapMode.GridSnapping:
                {
                    Vector3[] worldPoints = { activeSpaceMatrices.activeLocalToWorld.MultiplyPoint(originalLocalEdgePoints[edgeCenter]) };

                    worldOffset = CSGGrid.SnapDeltaToGrid(camera, worldOffset, worldPoints, snapToSelf: true);

                    break;
                }
                case SnapMode.RelativeSnapping:
                {
                    worldOffset = CSGGrid.SnapDeltaRelative(camera, worldOffset);

                    break;
                }
                default:
                case SnapMode.None:
                {
                    worldOffset = CSGGrid.HandleLockedAxi(worldOffset);

                    break;
                }
            }

            localOffset = activeSpaceMatrices.activeWorldToLocal.MultiplyVector(worldOffset);

            if(float.IsNaN(localOffset.x) || float.IsNaN(localOffset.y) || float.IsNaN(localOffset.z))
            {
                snappedOffset = offset;

                return;
            }

            snappedOffset = worldOffset;

            int[]     sides      = BoundsUtilities.aabbEdgeSides[edgeCenter];
            Vector3[] tempPoints = new Vector3[targetLocalPoints.Length];

            for(int p = 0; p < tempPoints.Length; p++)
            {
                tempPoints[p] = originalLocalPoints[p];
            }

            for(int s = 0; s < sides.Length; s++)
            {
                int sideIndex = sides[s];

                Vector3 localLineOrg = originalLocalCenterPoints[sideIndex];
                Vector3 localLineDir = targetLocalDirections[sideIndex];
                Vector3 sideOffset   = GeometryUtility.ProjectPointOnInfiniteLine(localLineOrg + localOffset, localLineOrg, localLineDir) - localLineOrg;

                int[] sidePoints = BoundsUtilities.aabbSidePointIndices[sideIndex];

                for(int p = 0; p < sidePoints.Length; p++)
                {
                    int index = sidePoints[p];
                    tempPoints[index] = GridUtility.CleanPosition(tempPoints[index] + sideOffset);
                }
            }

            AABB newBounds = BoundsUtilities.GetBounds(tempPoints);

            Vector3 oldSize = originalLocalTargetBounds.Size;
            Vector3 newSize = newBounds.Size;

            Vector3 worldCenterBefore = originalLocalTargetBounds.Center;
            Vector3 worldCenterAfter  = newBounds.Center;

            Vector3 worldScale = new Vector3(newSize.x / oldSize.x, newSize.y / oldSize.y, newSize.z / oldSize.z);

            Undo.RecordObjects(brushes, "Scaling brushes");

            for(int t = 0; t < brushes.Length; t++)
            {
                CSGBrush brush = brushes[t];
                brush.controlMesh = backupControlMeshes[t].Clone();
                brush.shape       = backupShapes[t].Clone();
            }

            ControlMeshUtility.TransformBrushes(brushes, worldCenterBefore, worldCenterAfter, worldScale, activeSpaceMatrices);

            InternalCSGModelManager.CheckSurfaceModifications(brushes);

            _lastLineMeshGeneration--;
            UpdateTargetBounds();
            UpdateTargetBoundsHandles(camera);
            CSGEditorGUIUtility.RepaintAll();
        }

        private MouseCursor _currentCursor = MouseCursor.Arrow;

        private void UpdateMouseCursor()
        {
            _currentCursor = CursorUtility.GetSelectionCursor(SelectionUtility.GetEventSelectionType());
        }

        [NonSerialized] private bool _ignorePivotChange;

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
            else if(activeTransform != null && !_mouseIsDragging)
            {
                if(prevActiveTransformPosition != activeTransform.position ||
                   prevActiveTransformRotation != activeTransform.rotation ||
                   prevActiveTransformScale != activeTransform.lossyScale ||
                   prevToolsRotation != Tools.pivotRotation)
                {
                    if(!_ignorePivotChange)
                    {
                        Matrix4x4 transform = activeTransform.localToWorldMatrix * prevInverseMatrix;

                        if(!transform.isIdentity)
                        {
                            worldSpacePivotCenter = transform.MultiplyPoint(worldSpacePivotCenter);
                        }
                    }

                    _ignorePivotChange = false;

                    activeSpaceMatrices = SpaceMatrices.Create(activeTransform);

                    UpdateTargetBounds();
                    UpdateTargetBoundsHandles(camera);
                    CSGEditorGUIUtility.RepaintAll();

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
            _boundsCenter = localToWorld.MultiplyPoint(targetLocalBounds.Center);

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
                worldSpacePivotCenter = _boundsCenter;
            }
            else
            {
                worldSpacePivotCenter = Selection.activeTransform ? Selection.activeTransform.position : Vector3.zero;
            }
        }

        private void UpdateRotationCircle(SceneView sceneView, Matrix4x4 localToWorld, Vector2 mousePosition)
        {
            if(_hoverOnBoundsEdge == -1)
            {
                return;
            }

            Camera camera = sceneView.camera;

            if(camera == null)
            {
                return;
            }

            UpdateBoundsCenter(localToWorld);

            Vector3 worldVertex1 = localToWorld.MultiplyPoint(targetLocalPoints[BoundsUtilities.aabbEdgeIndices[_hoverOnBoundsEdge][0]]);
            Vector3 worldVertex2 = localToWorld.MultiplyPoint(targetLocalPoints[BoundsUtilities.aabbEdgeIndices[_hoverOnBoundsEdge][1]]);

            _rotateNormal = camera.orthographic ? camera.transform.forward.normalized : (worldVertex2 - worldVertex1).normalized;

            if(Tools.pivotRotation == PivotRotation.Global)
            {
                _rotateNormal = GeometryUtility.SnapToClosestAxis(_rotateNormal);
            }

            CSGPlane rotatePlane = new CSGPlane(_rotateNormal, _boundsCenter);
            _rotateCenter = rotatePlane.Project(worldSpacePivotCenter);
            Ray ray = HandleUtility.GUIPointToWorldRay(mousePosition);
            _rotateMousePosition = rotatePlane.RayIntersection(ray);

            _rotateStart = (worldVertex2 + worldVertex1) * 0.5f;
            _rotateStart = GeometryUtility.ProjectPointOnPlane(rotatePlane, _rotateStart);
            Vector3 delta = _rotateCenter - _rotateStart;
            _rotateTangent = -delta.normalized;

            _rotateStartAngle = GeometryUtility.SignedAngle(_rotateCenter - _rotateStart, _rotateCenter - _rotateMousePosition, _rotateNormal);

            _rotateCurrentSnappedAngle = _rotateStartAngle;

            float handleSize = CSGHandleUtility.GetHandleSize(_rotateCenter);
            float minSize    = 4 * handleSize * GUIConstants.MIN_ROTATE_RADIUS;
            _rotateRadius = Math.Max(delta.magnitude, minSize);

            _rotateAxis = BoundsUtilities.aabbEdgeTangentAxis[_hoverOnBoundsEdge];
        }

        private void CreateControlIDs(bool setControlIDs)
        {
            for(int t = 0; t < brushes.Length; t++)
            {
                if(setControlIDs)
                {
                    brushControlIDs[t] = GUIUtility.GetControlID(objectTargetHash, FocusType.Keyboard);
                }
            }

            for(int i = 0; i < targetBoundEdgeIDs.Length; i++)
            {
                if(setControlIDs)
                {
                    targetBoundEdgeIDs[i] = GUIUtility.GetControlID(boundsEdgesTargetHash, FocusType.Keyboard);
                }
            }

            for(int i = 0; i < targetBoundCenterIDs.Length; i++)
            {
                if(setControlIDs)
                {
                    targetBoundCenterIDs[i] = GUIUtility.GetControlID(boundsCenterTargetHash, FocusType.Keyboard);
                }
            }
        }

        private void RenderHighlightedEdge(int highlightEdge)
        {
            Vector3 localPoint1 = targetLocalPoints[BoundsUtilities.aabbEdgeIndices[highlightEdge][0]];
            Vector3 localPoint2 = targetLocalPoints[BoundsUtilities.aabbEdgeIndices[highlightEdge][1]];

            PaintUtility.DrawLine(activeSpaceMatrices.activeLocalToWorld, localPoint1, localPoint2, GUIConstants.OLD_THICK_LINE_SCALE, ColorSettings.BoundsEdgeHover);
            PaintUtility.DrawDottedLine(activeSpaceMatrices.activeLocalToWorld, localPoint2, localPoint1, ColorSettings.BoundsEdgeHover);
        }

        private void RenderRotationCircle(Camera camera)
        {
            if(!worldSpacePivotCenterSet)
            {
                return;
            }

            string name  = string.Empty;
            Color  color = ColorSettings.RotateCircleOutline;

            _rotateCurrentAngle = 0;

            switch(_rotateAxis)
            {
                default:
                case PrincipleAxis.X: name = "X"; break;
                case PrincipleAxis.Y: name = "Y"; break;
                case PrincipleAxis.Z: name = "Z"; break;
            }

            if(_mouseIsDragging)
            {
                PaintUtility.DrawRotateCircle(camera, _rotateCenter, _rotateNormal, _rotateTangent, _rotateRadius, _rotateCurrentAngle, _rotateStartAngle, _rotateCurrentSnappedAngle, color, name);
                PaintUtility.DrawRotateCirclePie(_rotateCenter, _rotateNormal, _rotateTangent, _rotateRadius, _rotateStartAngle, _rotateCurrentSnappedAngle, color);
            }
            else
            {
                PaintUtility.DrawRotateCircle(camera, _rotateCenter, _rotateNormal, _rotateTangent, _rotateRadius, _rotateCurrentAngle, _rotateStartAngle, _rotateStartAngle, color, name);
            }

            PaintUtility.DrawDottedLine(_rotateCenter, _rotateMousePosition, ColorSettings.BoundsEdgeHover);

            if((worldSpacePivotCenter - _rotateCenter).magnitude > MathConstants.EQUALITY_EPSILON)
            {
                PaintUtility.DrawDottedLine(worldSpacePivotCenter, _rotateCenter, ColorSettings.BoundsEdgeHover);
                PaintUtility.DrawProjectedPivot(camera, _rotateCenter, ColorSettings.BoundsEdgeHover);
            }
        }

        private void RenderArrowCap(int boundsCenterIndex)
        {
            Color origColor = Handles.color;
            Handles.color = ColorSettings.PolygonInnerStateColor[(int)(SelectState.Selected | SelectState.Hovering)];

            switch(BoundsUtilities.aabbSideAxis[boundsCenterIndex])
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

            Vector3 localOrigin    = activeSpaceMatrices.activeLocalToWorld.MultiplyPoint(targetLocalCenterPoints[boundsCenterIndex]);
            Vector3 localDirection = activeSpaceMatrices.activeLocalToWorld.MultiplyVector(targetLocalDirections[boundsCenterIndex]).normalized;

            PaintUtility.DrawArrowCap(localOrigin, localDirection, HandleUtility.GetHandleSize(localOrigin) * 0.75f);
            Handles.color = origColor;
        }

        private readonly LineMeshManager _zTestLineMeshManager = new LineMeshManager();
        private readonly LineMeshManager _noZTestLineMeshManager = new LineMeshManager();
        private int _lastLineMeshGeneration = -1;

        private void OnDestroy()
        {
            _zTestLineMeshManager.Destroy();
            _noZTestLineMeshManager.Destroy();
        }

        [NonSerialized] private Vector2 _prevMousePos;

        public void HandleEvents(SceneView sceneView, Rect sceneRect)
        {
            Camera camera   = sceneView.camera;
            bool   inCamera = camera != null && camera.pixelRect.Contains(Event.current.mousePosition);

            EventType originalEventType = Event.current.type;

            if(originalEventType == EventType.MouseMove)
            {
                _mouseIsDragging   = false;
                _draggingOnCamera  = null;
                _realMousePosition = Event.current.mousePosition;
            }
            else if(originalEventType == EventType.MouseDown)
            {
                _mouseIsDragging   = false;
                _draggingOnCamera  = camera;
                _realMousePosition = _prevMousePos = Event.current.mousePosition;
            }
            else if(originalEventType == EventType.MouseUp)
            {
                _draggingOnCamera = null;
            }
            else if(originalEventType == EventType.MouseDrag)
            {
                if(!_mouseIsDragging && (_prevMousePos - Event.current.mousePosition).magnitude > 4.0f)
                {
                    _mouseIsDragging = true;
                }

                _realMousePosition += Event.current.delta;
            }

            try
            {
                if(_draggingOnCamera != null)
                {
                    inCamera = false;
                }

                if(_draggingOnCamera == camera)
                {
                    inCamera = true;
                }

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
                            if(!Tools.viewToolActive)
                            {
                                Rect windowRect = new Rect(0, 0, sceneView.position.width, sceneView.position.height - CsgguiStyleUtility.BOTTOM_TOOL_BAR_HEIGHT);

                                EditorGUIUtility.AddCursorRect(windowRect, _currentCursor);
                            }
                        }

                        if(!_mouseIsDragging && (_toolEditMode == ToolEditMode.MovingObject || _toolEditMode == ToolEditMode.CloneDragging) && CSGGrid.ForceGrid && _startCamera != null && _startCamera == camera)
                        {
                            if((_prevModifiers & EventModifiers.Shift) != (Event.current.modifiers & EventModifiers.Shift))
                            {
                                activeSpaceMatrices = SpaceMatrices.Create(activeTransform);
                                UpdateGridOrientation(_realMousePosition);
                            }
                        }

                        if(Tools.current == Tool.Rotate && _hoverOnBoundsEdge != -1 && _mouseIsDragging)
                        {
                            CSGGrid.ForceGrid = false;
                        }

                        if(!_mouseIsDragging)
                        {
                            UpdateTargetBounds();
                        }
                    }

                    UpdateTargetBoundsHandles(camera);

                    Matrix4x4 oldMatrix = Handles.matrix;
                    Handles.matrix = MathConstants.identityMatrix;

                    if(_lastLineMeshGeneration != InternalCSGModelManager.MeshGeneration)
                    {
                        _lastLineMeshGeneration = InternalCSGModelManager.MeshGeneration;

                        Matrix4x4[] brushTransformations = new Matrix4x4[brushes.Length];
                        int[]       brushNodeIDs         = new int[brushes.Length];

                        for(int i = brushes.Length - 1; i >= 0; i--)
                        {
                            CSGBrush brush = brushes[i];

                            if(brush.brushNodeID == CSGNode.INVALID_NODE_ID || // could be a prefab
                               brush.compareTransformation == null ||
                               brush.childData == null ||
                               !brush.childData.modelTransform)
                            {
                                ArrayUtility.RemoveAt(ref brushTransformations, i);
                                ArrayUtility.RemoveAt(ref brushNodeIDs, i);

                                continue;
                            }

                            brushTransformations[i] = brush.compareTransformation.localToWorldMatrix;
                            brushNodeIDs[i]         = brush.brushNodeID;
                        }

                        CSGRenderer.DrawSelectedBrushes(_zTestLineMeshManager, _noZTestLineMeshManager, brushNodeIDs, brushTransformations, ColorSettings.SelectedOutlines, GUIConstants.LINE_SCALE);
                    }

                    MaterialUtility.LineAlphaMultiplier     = 1.0f;
                    MaterialUtility.LineDashMultiplier      = 2.0f;
                    MaterialUtility.LineThicknessMultiplier = 2.0f;
                    _noZTestLineMeshManager.Render(MaterialUtility.NoZTestGenericLine);
                    MaterialUtility.LineDashMultiplier      = 1.0f;
                    MaterialUtility.LineThicknessMultiplier = 1.0f;
                    _zTestLineMeshManager.Render(MaterialUtility.ZTestGenericLine);

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

                        if(!_mouseIsDragging || Tools.current != Tool.Rotate || _hoverOnBoundsEdge == -1)
                        {
                            PaintUtility.DrawLines(activeSpaceMatrices.activeLocalToWorld, targetLocalPoints, BoundsUtilities.aabbLineIndices, GUIConstants.OLD_LINE_SCALE * 2.0f, meshEdgeOutlineColor);
                            PaintUtility.DrawUnoccludedLines(activeSpaceMatrices.activeLocalToWorld, targetLocalPoints, BoundsUtilities.aabbLineIndices, meshEdgeOutlineColor);
                            PaintUtility.DrawDottedLines(activeSpaceMatrices.activeLocalToWorld, targetLocalPoints, BoundsUtilities.aabbLineIndices, boundOutlinesColor);
                            PaintUtility.DrawLines(activeSpaceMatrices.activeLocalToWorld, targetLocalPoints, BoundsUtilities.aabbLineIndices, GUIConstants.OLD_LINE_SCALE, boundOutlinesColor);
                        }

                        if(!sceneView.camera.orthographic && CSGSettings.GridVisible)
                        {
                            Handles.matrix = Matrix4x4.identity;

                            int[]     indices  = BoundsUtilities.aabbTopIndices;
                            Vector3[] vertices = new Vector3[indices.Length];

                            float max = float.NegativeInfinity;

                            if(!_mouseIsDragging || Tools.current != Tool.Rotate)
                            {
                                for(int i = 0; i < indices.Length; i++)
                                {
                                    Vector3 pt0 = activeSpaceMatrices.activeLocalToWorld.MultiplyPoint(targetLocalPoints[i]);
                                    Vector3 pt1 = CSGGrid.CurrentGridPlane.Project(pt0);

                                    max = Mathf.Max(max, pt0.y - pt1.y);
                                    PaintUtility.DrawDottedLine(pt0, pt1, boundOutlinesColor);
                                    vertices[i] = pt1;
                                }
                            }
                            else
                            {
                                for(int i = 0; i < indices.Length; i++)
                                {
                                    Vector3 pt0 = activeSpaceMatrices.activeLocalToWorld.MultiplyPoint(targetLocalPoints[i]);
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
                                PaintUtility.DrawDottedWirePolygon(vertices, boundOutlinesColor);
                            }
                        }

                        Handles.matrix = oldMatrix;

                        if((Tools.current == Tool.Scale || Tools.current == Tool.Rect) && (_toolEditMode == ToolEditMode.SizingBounds || _toolEditMode == ToolEditMode.None) && !_cloneDragKeyPressed)
                        {
                            if(_hoverOnBoundsEdge != -1 || _hoverOnBoundsCenter != -1 || !_mouseIsDragging)
                            {
                                if(_hoverOnBoundsCenter != -1)
                                {
                                    RenderArrowCap(_hoverOnBoundsCenter);
                                }

                                PaintUtility.DrawDoubleDots(camera, activeSpaceMatrices.activeLocalToWorld, renderLocalCenterPoints, renderLocalCenterPointSizes, renderLocalCenterPointColors, renderLocalCenterPoints.Length);
                                PaintUtility.DrawDoubleDots(camera, activeSpaceMatrices.activeLocalToWorld, renderLocalEdgePoints, renderLocalEdgePointSizes, renderLocalEdgePointColors, renderLocalEdgePoints.Length);
                            }

                            if(camera != null)
                            {
                                bool showAxisX;
                                bool showAxisY;
                                bool showAxisZ;

                                if(_hoverOnBoundsEdge != -1)
                                {
                                    showAxisX = false;
                                    showAxisY = false;
                                    showAxisZ = false;

                                    switch(BoundsUtilities.aabbEdgeAxis[_hoverOnBoundsEdge][0])
                                    {
                                        case PrincipleAxis.X: showAxisX = true; break;
                                        case PrincipleAxis.Y: showAxisY = true; break;
                                        case PrincipleAxis.Z: showAxisZ = true; break;
                                    }

                                    switch(BoundsUtilities.aabbEdgeAxis[_hoverOnBoundsEdge][1])
                                    {
                                        case PrincipleAxis.X: showAxisX = true; break;
                                        case PrincipleAxis.Y: showAxisY = true; break;
                                        case PrincipleAxis.Z: showAxisZ = true; break;
                                    }
                                }
                                else if(_hoverOnBoundsCenter != -1)
                                {
                                    showAxisX = false;
                                    showAxisY = false;
                                    showAxisZ = false;

                                    switch(BoundsUtilities.aabbSideAxis[_hoverOnBoundsCenter])
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
                            if(_hoverOnBoundsEdge != -1)
                            {
                                if(inCamera && !_mouseIsDragging)
                                {
                                    UpdateRotationCircle(sceneView, activeSpaceMatrices.activeLocalToWorld, _realMousePosition);
                                }

                                if(!_mouseIsDragging)
                                {
                                    RenderHighlightedEdge(_hoverOnBoundsEdge);
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

                            newPivot = PaintUtility.HandlePivot(camera, newPivot, Tools.handleRotation, ColorSettings.BoundsEdgeHover, Tools.pivotMode == PivotMode.Pivot);

                            if(EditorGUI.EndChangeCheck())
                            {
                                _ignorePivotChange = true;
                                SetPivotCenter(newPivot);
                            }

                            worldSpacePivotCenterSet = true;
                        }
                    }
                    else if(Tools.current == Tool.Move)
                    {
                        if(HaveSelection)
                        {
                            Vector3 newPosition = _boundsCenter;
                            Vector3 position    = newPosition;

                            void init()
                            {
                                _originalPoint = position;
                                UpdateTargetInfo();
                                _snapVertices = originalLocalTargetBounds.GetAllCorners();

                                for(int i = 0; i < _snapVertices.Length; i++)
                                {
                                    _snapVertices[i] = activeSpaceMatrices.activeLocalToWorld.MultiplyPoint(_snapVertices[i]);
                                }
                            }

                            CSGHandles.InitFunction shutdown = UpdateTargetInfo;
                            EditorGUI.BeginChangeCheck();

                            newPosition = CSGHandles.PositionHandle(camera, newPosition, Tools.handleRotation, CSGSettings.ActiveSnappingMode, _snapVertices, init, shutdown);

                            if(EditorGUI.EndChangeCheck())
                            {
                                MoveCenter(_originalPoint, newPosition);
                                UpdateTargetBounds();
                                UpdateTargetBoundsHandles(camera);
                            }
                        }
                        else
                        {
                            _snapVertices = null;
                        }
                    }
                }

                switch(Event.current.type)
                {
                    case EventType.ValidateCommand:
                    {
                        if(Keys.cloneDragActivate.IsKeyPressed()
                        || Keys.HandleSceneValidate(EditModeManager.CurrentTool, false)
                        || Keys.flipSelectionX.IsKeyPressed() || Keys.flipSelectionY.IsKeyPressed()
                        || Keys.flipSelectionZ.IsKeyPressed() || Keys.rotateSelectionLeft.IsKeyPressed()
                        || Keys.rotateSelectionRight.IsKeyPressed() || Keys.moveSelectionLeft.IsKeyPressed()
                        || Keys.moveSelectionRight.IsKeyPressed() || Keys.moveSelectionBack.IsKeyPressed()
                        || Keys.moveSelectionForward.IsKeyPressed() || Keys.moveSelectionDown.IsKeyPressed()
                        || Keys.moveSelectionUp.IsKeyPressed())
                        {
                            Event.current.Use();

                            break;
                        }

                        if(Keys.centerPivot.IsKeyPressed())
                        {
                            Event.current.Use();
                        }

                        break;
                    }

                    case EventType.KeyDown:
                    {
                        SceneQueryUtility.ClearDeepClick();

                        if(Tools.viewTool != ViewTool.FPS && Keys.cloneDragActivate.IsKeyPressed())
                        {
                            _cloneDragKeyPressed = true;
                            Event.current.Use();

                            break;
                        }

                        if(Keys.HandleSceneKeyDown(EditModeManager.CurrentTool, false)
                        || Keys.flipSelectionX.IsKeyPressed() || Keys.flipSelectionY.IsKeyPressed()
                        || Keys.flipSelectionZ.IsKeyPressed() || Keys.rotateSelectionLeft.IsKeyPressed()
                        || Keys.rotateSelectionRight.IsKeyPressed() || Keys.moveSelectionLeft.IsKeyPressed()
                        || Keys.moveSelectionRight.IsKeyPressed() || Keys.moveSelectionBack.IsKeyPressed()
                        || Keys.moveSelectionForward.IsKeyPressed() || Keys.moveSelectionDown.IsKeyPressed()
                        || Keys.moveSelectionUp.IsKeyPressed())
                        {
                            Event.current.Use();

                            break;
                        }

                        if(Keys.centerPivot.IsKeyPressed())
                        {
                            Event.current.Use();
                        }

                        break;
                    }

                    case EventType.KeyUp:
                    {
                        if(_cloneDragKeyPressed && Keys.cloneDragActivate.IsKeyPressed())
                        {
                            _cloneDragKeyPressed = false;
                            _haveCloned          = false;
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

                            Vector3 center = activeSpaceMatrices.activeLocalToWorld.MultiplyPoint(targetLocalBounds.Center);

                            RotateObjects(topTransforms, center, MathConstants.upVector3, CSGSettings.SnapRotation);
                            Event.current.Use();

                            break;
                        }

                        if(Keys.rotateSelectionRight.IsKeyPressed())
                        {
                            UpdateTargetInfo();

                            Vector3 center = activeSpaceMatrices.activeLocalToWorld.MultiplyPoint(targetLocalBounds.Center);

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
                        }

                        break;
                    }

                    case EventType.MouseDown:
                    {
                        if(!sceneRect.Contains(Event.current.mousePosition))
                        {
                            break;
                        }

                        if(GUIUtility.hotControl != 0 ||
                           Event.current.button != 0)
                        {
                            break;
                        }

                        _toolEditMode       = ToolEditMode.None;
                        _extraDeltaMovement = MathConstants.zeroVector3;
                        activeSpaceMatrices = SpaceMatrices.Create(activeTransform);

                        if(CSGSettings.LockAxisX && CSGSettings.LockAxisY && CSGSettings.LockAxisZ)
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

                        if(HaveTarget && (Tools.current == Tool.Rotate || Tools.current == Tool.Scale || Tools.current == Tool.Rect || Tools.current == Tool.Move))
                        {
                            int newControlID = -1;

                            if(_hoverOnTarget != -1)
                            {
                                if(Event.current.modifiers == EventModifiers.None ||
                                   Event.current.modifiers == EventModifiers.Shift ||
                                   Event.current.modifiers == EventModifiers.Control ||
                                   Event.current.modifiers == (EventModifiers.Shift | EventModifiers.Control))
                                {
                                    _toolEditMode = ToolEditMode.MovingObject;
                                    newControlID  = brushControlIDs[_hoverOnTarget];
                                }
                            }
                            else if(Tools.current != Tool.Move)
                            {
                                if(_hoverOnBoundsCenter != -1)
                                {
                                    if(Event.current.modifiers == EventModifiers.None)
                                    {
                                        _toolEditMode = ToolEditMode.SizingBounds;
                                        newControlID  = targetBoundCenterIDs[_hoverOnBoundsCenter];
                                    }
                                }
                                else if(_hoverOnBoundsEdge != -1)
                                {
                                    if(Event.current.modifiers == EventModifiers.None)
                                    {
                                        _toolEditMode              = ToolEditMode.RotatingBounds;
                                        newControlID               = targetBoundEdgeIDs[_hoverOnBoundsEdge];
                                        _rotateCurrentSnappedAngle = _rotateStartAngle;
                                    }
                                }
                            }

                            if(newControlID != -1)
                            {
                                GUIUtility.hotControl             = newControlID;
                                GUIUtility.keyboardControl        = newControlID;
                                EditorGUIUtility.editingTextField = false;
                                Event.current.Use();
                                _firstMove = true;
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

                        if(!_mouseIsDragging)
                        {
                            SelectionUtility.DoSelectionClick(sceneView);
                            Event.current.Use();
                        }

                        _mouseIsDragging = false;

                        break;
                    }

                    case EventType.Layout:
                    {
                        UpdateMouseCursor();

                        if(inCamera && !_mouseIsDragging && GUIUtility.hotControl == 0)
                        {
                            UpdateTargetBoundsHandles(camera);

                            activeTransform = Selection.activeTransform;

                            if(activeTransform != null)
                            {
                                activeSpaceMatrices = SpaceMatrices.Create(activeTransform);

                                _hoverOnTarget       = -1;
                                _hoverOnBoundsCenter = -1;
                                _hoverOnBoundsEdge   = -1;

                                Ray     worldRay  = HandleUtility.GUIPointToWorldRay(Event.current.mousePosition);
                                Vector3 rayStart  = worldRay.origin;
                                Vector3 rayVector = worldRay.direction * (camera.farClipPlane - camera.nearClipPlane);
                                Vector3 rayEnd    = rayStart + rayVector;

                                float minDistance  = float.PositiveInfinity;
                                int   closestBrush = -1;

                                for(int t = 0; t < brushes.Length; t++)
                                {
                                    CSGBrush  brush          = brushes[t];
                                    Transform modelTransform = parentModelTransforms[t];

                                    if(modelTransform == null)
                                    {
                                        continue;
                                    }

                                    Matrix4x4 modelTransformation = modelTransform.localToWorldMatrix;

                                    if(SceneQueryUtility.FindBrushIntersection(brush, modelTransformation, rayStart, rayEnd, out LegacyBrushIntersection intersection))
                                    {
                                        float distance = (intersection.worldIntersection - rayStart).magnitude;

                                        if(distance < minDistance)
                                        {
                                            minDistance             = distance;
                                            closestBrush            = t;
                                            hoverOverBrushIndex     = t;
                                            _hoverIntersectionPoint = intersection.worldIntersection;
                                        }
                                    }
                                }

                                if(closestBrush == -1 && brushes.Length > 0)
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
                                        Ray localRay = worldRay;
                                        localRay.direction = worldToLocal.MultiplyVector(localRay.direction);
                                        localRay.origin    = worldToLocal.MultiplyPoint(localRay.origin);

                                        if(bounds.IntersectRay(localRay))
                                        {
                                            closestBrush = 0;
                                        }
                                    }
                                }

                                if(closestBrush >= 0 && closestBrush <= brushes.Length)
                                {
                                    HandleUtility.AddControl(brushControlIDs[closestBrush], 3.0f);
                                }

                                MouseCursor newCursor    = MouseCursor.Arrow;
                                bool        foundControl = false;

                                Matrix4x4 matrix = activeSpaceMatrices.activeLocalToWorld;

                                int nearestControl;

                                if((Tools.current == Tool.Scale || Tools.current == Tool.Rect) && !_cloneDragKeyPressed)
                                {
                                    for(int t = 0; t < targetBoundCenterIDs.Length; t++)
                                    {
                                        float radius = renderLocalCenterPointSizes[t] * GUIConstants.HOVER_HANDLE_SCALE;

                                        if(radius == 0)
                                        {
                                            continue;
                                        }

                                        float distance = HandleUtility.DistanceToCircle(matrix.MultiplyPoint(targetLocalCenterPoints[t]), radius) * 0.5f;

                                        HandleUtility.AddControl(targetBoundCenterIDs[t], distance);
                                    }

                                    nearestControl = HandleUtility.nearestControl;

                                    if(nearestControl > 0)
                                    {
                                        for(int t = 0; t < targetBoundCenterIDs.Length; t++)
                                        {
                                            if(targetBoundCenterIDs[t] == nearestControl)
                                            {
                                                _hoverIntersectionPoint = matrix.MultiplyPoint(targetLocalCenterPoints[t]);
                                                _hoverOnBoundsCenter    = t;

                                                newCursor = CursorUtility.GetCursorForDirection(Matrix4x4.identity, targetLocalCenterPoints[t], targetLocalDirections[t]);

                                                foundControl = true;

                                                break;
                                            }
                                        }
                                    }
                                }

                                if(Tools.current == Tool.Scale || Tools.current == Tool.Rect
                                || Tools.current == Tool.Rotate)
                                {
                                    if(camera != null && !foundControl && !_cloneDragKeyPressed)
                                    {
                                        CSGPlane cameraPlane = CSGHandleUtility.GetNearPlane(camera);

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
                                            bool side1 = backfaced[BoundsUtilities.aabbEdgeSides[t][0]];
                                            bool side2 = backfaced[BoundsUtilities.aabbEdgeSides[t][1]];

                                            if(Tools.current == Tool.Rotate && side1 && side2)
                                            {
                                                continue;
                                            }

                                            Vector3 point1 = targetLocalPoints[BoundsUtilities.aabbEdgeIndices[t][0]];
                                            Vector3 point2 = targetLocalPoints[BoundsUtilities.aabbEdgeIndices[t][1]];

                                            float distance;

                                            if(Tools.current == Tool.Scale || Tools.current == Tool.Rect)
                                            {
                                                if(renderLocalEdgePointSizes[t] > 0 && !camera.orthographic)
                                                {
                                                    distance = HandleUtility.DistanceToCircle(matrix.MultiplyPoint(renderLocalEdgePoints[t]), renderLocalEdgePointSizes[t]) * 0.5f;
                                                }
                                                else
                                                {
                                                    distance = Mathf.Min(
                                                        CameraUtility.DistanceToLine(cameraPlane, mousePoint, point1, point2) * 3.0f,
                                                        HandleUtility.DistanceToCircle(
                                                            matrix.MultiplyPoint(renderLocalEdgePoints[t])
                                                          , renderLocalEdgePointSizes[t]
                                                        ) * 0.5f
                                                    );
                                                }
                                            }
                                            else
                                            {
                                                distance = CameraUtility.DistanceToLine(cameraPlane, mousePoint, matrix.MultiplyPoint(point1), matrix.MultiplyPoint(point2));
                                            }

                                            HandleUtility.AddControl(targetBoundEdgeIDs[t], distance);
                                        }
                                    }
                                }

                                nearestControl    = HandleUtility.nearestControl;
                                CSGGrid.ForceGrid = false;

                                if(nearestControl > 0 && !foundControl)
                                {
                                    if(Tools.current == Tool.Scale || Tools.current == Tool.Rect || Tools.current == Tool.View || Tools.current == Tool.Rotate)
                                    {
                                        for(int t = 0; t < targetBoundEdgeIDs.Length; t++)
                                        {
                                            if(targetBoundEdgeIDs[t] == nearestControl)
                                            {
                                                Vector3 point1 = targetLocalPoints[BoundsUtilities.aabbEdgeIndices[t][0]];
                                                Vector3 point2 = targetLocalPoints[BoundsUtilities.aabbEdgeIndices[t][1]];

                                                _hoverIntersectionPoint = activeSpaceMatrices.activeLocalToWorld.MultiplyPoint((point1 + point2) * 0.5f);

                                                if(Tools.current == Tool.Rotate || renderLocalEdgePointSizes[t] > 0)
                                                {
                                                    _hoverOnBoundsEdge = t;

                                                    int     side1         = BoundsUtilities.aabbEdgeSides[t][0];
                                                    int     side2         = BoundsUtilities.aabbEdgeSides[t][1];
                                                    Vector3 localSideDir1 = targetLocalDirections[side1];
                                                    Vector3 localSideDir2 = targetLocalDirections[side2];

                                                    Vector3 localNormal = Vector3.Cross(localSideDir1, localSideDir2);

                                                    _movePlane = new CSGPlane(
                                                        GridUtility.CleanNormal(
                                                            activeSpaceMatrices.activeLocalToWorld
                                                               .MultiplyVector(localNormal)
                                                        ),
                                                        activeSpaceMatrices.activeLocalToWorld.MultiplyPoint(_hoverIntersectionPoint)
                                                    );

                                                    if(Tools.current == Tool.Scale || Tools.current == Tool.Rect)
                                                    {
                                                        CSGGrid.SetForcedGrid(camera, _movePlane);
                                                    }

                                                    if(Tools.current == Tool.Rotate)
                                                    {
                                                        newCursor = MouseCursor.RotateArrow;
                                                    }
                                                }
                                                else
                                                {
                                                    for(int i = 0; i < 2; i++)
                                                    {
                                                        int side = BoundsUtilities.aabbEdgeSides[t][i];

                                                        if(renderLocalCenterPointSizes[side] == 0)
                                                        {
                                                            continue;
                                                        }

                                                        float radius = renderLocalCenterPointSizes[side] * GUIConstants.HOVER_HANDLE_SCALE;

                                                        if(radius != 0)
                                                        {
                                                            _hoverOnBoundsCenter = side;

                                                            newCursor = CursorUtility.GetCursorForDirection(Matrix4x4.identity, targetLocalCenterPoints[side], targetLocalDirections[side]);

                                                            break;
                                                        }
                                                    }
                                                }

                                                break;
                                            }
                                        }
                                    }

                                    if(Tools.current == Tool.Scale || Tools.current == Tool.Rect || Tools.current == Tool.Move)
                                    {
                                        for(int t = 0; t < brushes.Length; t++)
                                        {
                                            if(brushControlIDs[t] == nearestControl)
                                            {
                                                _hoverOnTarget = t;
                                                newCursor      = MouseCursor.MoveArrow;
                                                foundControl   = true;

                                                break;
                                            }
                                        }
                                    }
                                }

                                if(newCursor != _currentCursor)
                                {
                                    _currentCursor = newCursor;
                                    CSGEditorGUIUtility.RepaintAll();
                                }
                            }
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

                            if(_firstMove)
                            {
                                EditorGUIUtility.SetWantsMouseJumping(1);
                                _extraDeltaMovement = MathConstants.zeroVector3;
                                _originalPoint      = _hoverIntersectionPoint;
                                _startCamera        = camera;
                                UpdateGrid(_startCamera);
                            }

                            Ray mouseRay = HandleUtility.GUIPointToWorldRay(Event.current.mousePosition);

                            Vector3 worldIntersection = _movePlane.RayIntersection(mouseRay);

                            if(float.IsNaN(worldIntersection.x) || float.IsNaN(worldIntersection.y) || float.IsNaN(worldIntersection.z))
                            {
                                break;
                            }

                            Vector3 localLineOrg = originalLocalCenterPoints[i];
                            Vector3 localLineDir = targetLocalDirections[i];

                            Vector3 localIntersection = activeSpaceMatrices.activeWorldToLocal.MultiplyPoint(worldIntersection);

                            GridUtility.CleanPosition(worldIntersection);

                            worldIntersection = activeSpaceMatrices.activeLocalToWorld.MultiplyPoint(GeometryUtility.ProjectPointOnInfiniteLine(localIntersection, localLineOrg, localLineDir));

                            if(_firstMove)
                            {
                                _originalPoint      = worldIntersection;
                                _worldDeltaMovement = MathConstants.zeroVector3;
                                _firstMove          = false;
                            }
                            else
                            {
                                _worldDeltaMovement = SnapMovementToPlane(worldIntersection - _originalPoint);
                            }

                            if(float.IsNaN(_worldDeltaMovement.x) || float.IsNaN(_worldDeltaMovement.y) || float.IsNaN(_worldDeltaMovement.z))
                            {
                                break;
                            }

                            MoveBoundsCenter(camera, i, _worldDeltaMovement, out _worldDeltaMovement, CSGSettings.ActiveSnappingMode);

                            break;
                        }
                        case EventType.MouseUp:
                        {
                            if(Event.current.button != 0)
                            {
                                break;
                            }

                            EditorGUIUtility.SetWantsMouseJumping(0);
                            _startCamera  = null;
                            _toolEditMode = ToolEditMode.None;

                            UpdateTargetInfo();

                            GUIUtility.hotControl             = 0;
                            GUIUtility.keyboardControl        = 0;
                            EditorGUIUtility.editingTextField = false;
                            Event.current.Use();

                            CSGGrid.ForceGrid = false;
                            CSGEditorGUIUtility.RepaintAll();

                            break;
                        }
                        case EventType.Repaint:
                        {
                            if(!inCamera)
                            {
                                break;
                            }

                            Vector2 textCenter2D = Event.current.mousePosition;
                            textCenter2D.y += HOVER_TEXT_DISTANCE * 2;
                            Vector3 delta = _worldDeltaMovement;

                            if(Tools.pivotRotation == PivotRotation.Local)
                            {
                                delta = GridUtility.CleanPosition(activeSpaceMatrices.activeLocalToWorld.MultiplyVector(delta));
                            }

                            float length = delta.magnitude;

                            bool disabled = false;

                            if(_hoverOnBoundsCenter != -1)
                            {
                                disabled = BoundsUtilities.aabbSideAxis[_hoverOnBoundsCenter] switch
                                {
                                    PrincipleAxis.X => CSGSettings.LockAxisX
                                  , PrincipleAxis.Y => CSGSettings.LockAxisY
                                  , PrincipleAxis.Z => CSGSettings.LockAxisZ
                                  , var _           => disabled
                                };
                            }

                            if(disabled)
                            {
                                switch(BoundsUtilities.aabbSideAxis[_hoverOnBoundsCenter])
                                {
                                    case PrincipleAxis.X:
                                        PaintUtility.DrawScreenText(textCenter2D, "X axis is locked", CsgguiStyleUtility.RedTextArea); break;
                                    case PrincipleAxis.Y:
                                        PaintUtility.DrawScreenText(textCenter2D, "Y axis is locked", CsgguiStyleUtility.RedTextArea); break;
                                    case PrincipleAxis.Z:
                                        PaintUtility.DrawScreenText(textCenter2D, "Z axis is locked", CsgguiStyleUtility.RedTextArea); break;
                                }
                            }
                            else
                            {
                                PaintUtility.DrawScreenText(textCenter2D, $"Δ{Units.ToRoundedDistanceString(length)}");
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

                            if(_firstMove)
                            {
                                EditorGUIUtility.SetWantsMouseJumping(1);
                                _extraDeltaMovement = MathConstants.zeroVector3;
                                _originalPoint      = _hoverIntersectionPoint;
                                _startCamera        = camera;

                                UpdateGrid(_startCamera);

                                for(int b = 0; b < topTransforms.Length; b++)
                                {
                                    if(!topTransforms[b])
                                    {
                                        continue;
                                    }

                                    GameObject gameObject = topTransforms[b].gameObject;

                                    if(gameObject.activeInHierarchy && !CSGPrefabUtility.IsPrefabAsset(gameObject))
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
                                        sourceSurfaceAlignment      = topNode.prefabSourceAlignment;
                                        destinationSurfaceAlignment = topNode.prefabDestinationAlignment;
                                    }
                                }

                                BoundsUtilities.GetBoundsCornerPoints(targetLocalBounds, projectedBounds);
                            }

                            Ray     mouseRay          = HandleUtility.GUIPointToWorldRay(Event.current.mousePosition);
                            Vector3 worldIntersection = _movePlane.RayIntersection(mouseRay);

                            if(!float.IsNaN(worldIntersection.x) && !float.IsNaN(worldIntersection.y)
                            && !float.IsNaN(worldIntersection.z))
                            {
                                if((_prevModifiers & EventModifiers.Shift) != (SelectionUtility.CurrentModifiers & EventModifiers.Shift) && CSGGrid.ForceGrid && _startCamera != null)
                                {
                                    _prevModifiers = SelectionUtility.CurrentModifiers;
                                    UpdateGridOrientation(_realMousePosition);
                                    _extraDeltaMovement += _worldDeltaMovement;
                                }

                                worldIntersection = GeometryUtility.ProjectPointOnPlane(_movePlane, worldIntersection);

                                if(_firstMove)
                                {
                                    _originalPoint      = worldIntersection;
                                    _worldDeltaMovement = MathConstants.zeroVector3;
                                    _firstMove          = false;
                                }
                                else
                                {
                                    _worldDeltaMovement = worldIntersection - _originalPoint;
                                }
                            }

                            if(_worldDeltaMovement != MathConstants.zeroVector3 &&
                               _cloneDragKeyPressed &&
                               _toolEditMode == ToolEditMode.MovingObject &&
                               !float.IsNaN(_worldDeltaMovement.x) &&
                               !float.IsNaN(_worldDeltaMovement.y) &&
                               !float.IsNaN(_worldDeltaMovement.z))
                            {
                                _toolEditMode = ToolEditMode.CloneDragging;
                                topTransforms = EditModeManager.CloneTargets();

                                for(int b = 0; b < topTransforms.Length; b++)
                                {
                                    if(!topTransforms[b])
                                    {
                                        continue;
                                    }

                                    GameObject gameObject = topTransforms[b].gameObject;

                                    if(gameObject.activeInHierarchy && !CSGPrefabUtility.IsPrefabAsset(gameObject))
                                    {
                                        dragGameObjects.Add(gameObject);
                                    }
                                }
                            }

                            bool useRaySnapping = (Event.current.modifiers & EventModifiers.Shift) == EventModifiers.Shift;

                            if(useRaySnapping)
                            {
                                _ignoreSetTargets = true;

                                try
                                {
                                    if(hoverOverBrushIndex < backupRotations.Length)
                                    {
                                        Vector3    brushPosition = backupPositions[hoverOverBrushIndex];
                                        Quaternion brushRotation = backupRotations[hoverOverBrushIndex];
                                        SelectionUtility.HideObjectsRemoteOnly(dragGameObjects);

                                        LegacyBrushIntersection intersection = SceneQueryUtility.FindMeshIntersection(camera, Event.current.mousePosition);

                                        Vector3    normal        = intersection.worldPlane.Normal;
                                        Vector3    hoverPosition = intersection.worldIntersection;
                                        Quaternion hoverRotation = SelectionUtility.FindDragOrientation(sceneView, normal, sourceSurfaceAlignment, destinationSurfaceAlignment);

                                        CSGGrid.SetForcedGrid(camera, intersection.worldPlane);

                                        switch(CSGSettings.ActiveSnappingMode)
                                        {
                                            case SnapMode.GridSnapping:
                                            {
                                                Vector3[] localPoints = new Vector3[8];
                                                CSGPlane  localPlane  = intersection.worldPlane;

                                                for(int p = 0; p < localPoints.Length; p++)
                                                {
                                                    localPoints[p] = GeometryUtility.ProjectPointOnPlane(localPlane, hoverRotation * projectedBounds[p] + hoverPosition);
                                                }

                                                hoverPosition += CSGGrid.SnapDeltaToGrid(camera, MathConstants.zeroVector3, localPoints);

                                                break;
                                            }
                                            case SnapMode.RelativeSnapping:
                                            {
                                                hoverPosition += CSGGrid.SnapDeltaRelative(camera, MathConstants.zeroVector3);

                                                break;
                                            }
                                        }

                                        hoverPosition = GeometryUtility.ProjectPointOnPlane(intersection.worldPlane, hoverPosition);

                                        _worldDeltaMovement = hoverPosition - brushPosition;

                                        Undo.RecordObjects(topTransforms, "Drag objects");
                                        UpdateTargetBounds();
                                        CSGEditorGUIUtility.RepaintAll();
                                        Vector3    center   = GridUtility.CleanPosition(brushPosition + _worldDeltaMovement);
                                        Quaternion rotation = hoverRotation * Quaternion.Inverse(brushRotation);

                                        MoveRotateObjects(topTransforms, center, rotation, _worldDeltaMovement);
                                    }
                                }
                                finally
                                {
                                    _ignoreSetTargets = false;
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

                                    CSGEditorGUIUtility.RepaintAll();
                                }
                            }
                            else
                            {
                                if(float.IsNaN(_worldDeltaMovement.x) || float.IsNaN(_worldDeltaMovement.y) || float.IsNaN(_worldDeltaMovement.z))
                                {
                                    break;
                                }

                                SnapMode activeSnappingMode = CSGSettings.ActiveSnappingMode;

                                if(MoveObjects(camera, topTransforms, _worldDeltaMovement, out _worldDeltaMovement, activeSnappingMode))
                                {
                                    _originalPoint = worldIntersection;
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
                            _startCamera  = null;
                            _toolEditMode = ToolEditMode.None;

                            UpdateTargetInfo();

                            GUIUtility.hotControl             = 0;
                            GUIUtility.keyboardControl        = 0;
                            EditorGUIUtility.editingTextField = false;
                            Event.current.Use();

                            CSGGrid.ForceGrid = false;
                            CSGEditorGUIUtility.RepaintAll();

                            break;
                        }
                        case EventType.Repaint:
                        {
                            Vector2 textCenter2D = Event.current.mousePosition;
                            textCenter2D.y += HOVER_TEXT_DISTANCE * 2;

                            Vector3 movement = _worldDeltaMovement + _extraDeltaMovement;

                            bool lockX = Mathf.Abs(movement.x) < MathConstants.CONSIDERED_ZERO && (CSGSettings.LockAxisX || Mathf.Abs(_movePlane.a) >= 1 - MathConstants.EQUALITY_EPSILON);
                            bool lockY = Mathf.Abs(movement.y) < MathConstants.CONSIDERED_ZERO && (CSGSettings.LockAxisY || Mathf.Abs(_movePlane.b) >= 1 - MathConstants.EQUALITY_EPSILON);
                            bool lockZ = Mathf.Abs(movement.z) < MathConstants.CONSIDERED_ZERO && (CSGSettings.LockAxisZ || Mathf.Abs(_movePlane.c) >= 1 - MathConstants.EQUALITY_EPSILON);

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

                                if(_firstMove)
                                {
                                    EditorGUIUtility.SetWantsMouseJumping(1);
                                    _extraDeltaMovement = MathConstants.zeroVector3;
                                    _originalPoint      = _hoverIntersectionPoint;
                                    _startCamera        = camera;

                                    UpdateGrid(_startCamera);
                                }

                                Ray mouseRay = HandleUtility.GUIPointToWorldRay(_realMousePosition);

                                _firstMove = false;
                                CSGPlane rotatePlane = new CSGPlane(_rotateNormal, _boundsCenter);
                                _rotateMousePosition = rotatePlane.RayIntersection(mouseRay);

                                RotateObjects(topTransforms, _rotateCenter, _rotateNormal, _rotateStart, _rotateStartAngle, _rotateMousePosition, out _rotateCurrentSnappedAngle);

                                break;
                            }
                            case EventType.MouseUp:
                            {
                                if(Event.current.button != 0)
                                {
                                    break;
                                }

                                EditorGUIUtility.SetWantsMouseJumping(0);
                                _startCamera  = null;
                                _toolEditMode = ToolEditMode.None;

                                UpdateTargetInfo();

                                GUIUtility.hotControl             = 0;
                                GUIUtility.keyboardControl        = 0;
                                EditorGUIUtility.editingTextField = false;
                                Event.current.Use();

                                CSGGrid.ForceGrid = false;
                                CSGEditorGUIUtility.RepaintAll();

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

                                if(_firstMove)
                                {
                                    EditorGUIUtility.SetWantsMouseJumping(1);
                                    _originalPoint = _hoverIntersectionPoint;
                                    _startCamera   = camera;

                                    if(activeSpaceMatrices == null)
                                    {
                                        activeSpaceMatrices = SpaceMatrices.Create(activeTransform);
                                    }

                                    Vector3 localLineOrg  = originalLocalEdgePoints[i];
                                    int     side1         = BoundsUtilities.aabbEdgeSides[i][0];
                                    int     side2         = BoundsUtilities.aabbEdgeSides[i][1];
                                    Vector3 localSideDir1 = targetLocalDirections[side1];
                                    Vector3 localSideDir2 = targetLocalDirections[side2];
                                    Vector3 localNormal   = Vector3.Cross(localSideDir1, localSideDir2);

                                    _movePlane = new CSGPlane(GridUtility.CleanNormal(activeSpaceMatrices.activeLocalToWorld.MultiplyVector(localNormal)), activeSpaceMatrices.activeLocalToWorld.MultiplyPoint(localLineOrg));

                                    CSGGrid.SetForcedGrid(camera, _movePlane);
                                }

                                Ray mouseRay = HandleUtility.GUIPointToWorldRay(_realMousePosition);

                                Vector3 worldIntersection = _movePlane.RayIntersection(mouseRay);

                                if(float.IsNaN(worldIntersection.x) || float.IsNaN(worldIntersection.y) || float.IsNaN(worldIntersection.z))
                                {
                                    break;
                                }

                                worldIntersection = GeometryUtility.ProjectPointOnPlane(_movePlane, worldIntersection);

                                if(_firstMove)
                                {
                                    _originalPoint      = worldIntersection;
                                    _worldDeltaMovement = MathConstants.zeroVector3;
                                    _firstMove          = false;
                                }
                                else
                                {
                                    _worldDeltaMovement = SnapMovementToPlane(worldIntersection - _originalPoint);
                                }

                                if(float.IsNaN(_worldDeltaMovement.x) || float.IsNaN(_worldDeltaMovement.y) || float.IsNaN(_worldDeltaMovement.z))
                                {
                                    break;
                                }

                                MoveBoundsEdge(camera, i, _worldDeltaMovement, out _worldDeltaMovement, CSGSettings.ActiveSnappingMode);

                                break;
                            }
                            case EventType.MouseUp:
                            {
                                if(Event.current.button != 0)
                                {
                                    break;
                                }

                                EditorGUIUtility.SetWantsMouseJumping(0);
                                _startCamera  = null;
                                _toolEditMode = ToolEditMode.None;

                                UpdateTargetInfo();

                                GUIUtility.hotControl             = 0;
                                GUIUtility.keyboardControl        = 0;
                                EditorGUIUtility.editingTextField = false;
                                Event.current.Use();

                                CSGGrid.ForceGrid = false;
                                CSGEditorGUIUtility.RepaintAll();

                                break;
                            }
                            case EventType.Repaint:
                            {
                                Vector2 textCenter2D = _realMousePosition;
                                textCenter2D.y += HOVER_TEXT_DISTANCE * 2;
                                Vector3 delta = _worldDeltaMovement;

                                if(Tools.pivotRotation == PivotRotation.Local)
                                {
                                    delta = GridUtility.CleanPosition(activeSpaceMatrices.activeLocalToWorld.MultiplyVector(delta));
                                }

                                float length = delta.magnitude;

                                PaintUtility.DrawScreenText(textCenter2D, $"Δ{Units.ToRoundedDistanceString(length)}");

                                // TODO: show actual current length of this axis
                                break;
                            }
                        }
                    }
                }
            }
            finally
            {
                if(originalEventType == EventType.MouseUp || originalEventType == EventType.MouseMove)
                {
                    _mouseIsDragging = false;
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

        private static Vector2 m_ScrollPos;

        public void OnInspectorGUI(EditorWindow window, float height)
        {
            m_ScrollPos = EditorGUILayout.BeginScrollView(m_ScrollPos);

            EditmodePlaceGUI.OnInspectorGUI();
            EditorGUILayout.EndScrollView();
        }

        public Rect GetLastSceneGUIRect()
        {
            return EditmodePlaceGUI.GetLastSceneGUIRect();
        }

        public bool OnSceneGUI(Rect windowRect)
        {
            if(brushes == null || brushes.Length == 0)
            {
                return false;
            }

            EditmodePlaceGUI.OnSceneGUI(this);

            return false;
        }
    }
}
