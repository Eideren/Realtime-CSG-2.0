using System;
using System.Collections.Generic;
using InternalRealtimeCSG;
using RealtimeCSG.Legacy;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace RealtimeCSG
{
    internal static class CSGGrid
    {
        private static Material m_GridMaterial;

        private static readonly int gridSizeID = Shader.PropertyToID("_GridSize");
        private static readonly int gridSpacingID = Shader.PropertyToID("_GridSpacing");
        private static readonly int gridColorID = Shader.PropertyToID("_GridColor");
        private static readonly int gridCenterColorID = Shader.PropertyToID("_CenterColor");
        private static readonly int cull = Shader.PropertyToID("_Cull");
        private static readonly int zWrite = Shader.PropertyToID("_ZWrite");
        private static readonly int zTest = Shader.PropertyToID("_ZTest");
        private static readonly int gridLineThickness = Shader.PropertyToID("_GridLineThickness");
        private static readonly int centerLineThickness = Shader.PropertyToID("_CenterLineThickness");

        private static Material GridMaterial
        {
            get
            {
                if(!m_GridMaterial)
                {
                    Shader shader = Shader.Find("Hidden/CSG/internal/Grid");

                    if(shader == null)
                    {
                        return null;
                    }

                    m_GridMaterial = new Material(shader);
                    m_GridMaterial.SetInt(cull, (int)CullMode.Off);
                    m_GridMaterial.SetInt(zWrite, 0);
                    m_GridMaterial.SetInt(zTest, (int)CompareFunction.LessEqual);
                    m_GridMaterial.SetFloat(gridLineThickness, 1.0f * EditorGUIUtility.pixelsPerPoint);
                    m_GridMaterial.SetFloat(centerLineThickness, 2.0f * EditorGUIUtility.pixelsPerPoint);
                    m_GridMaterial.hideFlags = HideFlags.HideAndDontSave | HideFlags.DontUnloadUnusedAsset;
                }

                return m_GridMaterial;
            }
        }

        private static Mesh m_GridMesh;

        private static Mesh GridMesh
        {
            get
            {
                if(!m_GridMesh)
                {
                    m_GridMesh = new Mesh
                    {
                        name = "Plane"
                      , vertices =
                            new[] { new Vector3(-1, -1, 0), new Vector3(1, -1, 0), new Vector3(1, 1, 0), new Vector3(-1, 1, 0) }
                      , triangles = new[] { 0, 1, 2, 0, 2, 3 }
                      , hideFlags = HideFlags.HideAndDontSave | HideFlags.DontUnloadUnusedAsset
                    };

                    m_GridMesh.RecalculateBounds();
                }

                return m_GridMesh;
            }
        }

        private static void DrawGrid(Camera camera, Vector3 cameraPosition, Vector3 gridCenter, Quaternion gridRotation, GridAxis axis, float alpha, GridMode gridMode)
        {
            if(alpha <= 0.03f)
            {
                return;
            }

            // find the nearest point on the plane
            Vector3 normal = gridRotation * MathConstants.upVector3;
            float   d      = Vector3.Dot(normal, gridCenter);
            float   t      = (Vector3.Dot(normal, cameraPosition) - d) / Vector3.Dot(normal, normal);

            float snapX = CSGSettings.SnapVector.x;
            float snapY = CSGSettings.SnapVector.y;
            float snapZ = CSGSettings.SnapVector.z;

            // calculate a point on the camera at the same distance as the camera is to the grid, in world-space
            Vector3 forward         = camera.cameraToWorldMatrix.MultiplyVector(MathConstants.forwardVector3);
            Vector3 projectedCenter = cameraPosition - t * forward;

            // calculate the snap sizes relatively to that point in the center (this makes them relative to camera position, but not rotation)
            Vector3 sideways          = camera.cameraToWorldMatrix.MultiplyVector(MathConstants.leftVector3);
            Vector3 screenPointCenter = camera.WorldToScreenPoint(projectedCenter);
            Vector3 screenPointAxisX  = camera.WorldToScreenPoint(projectedCenter + sideways * snapX);
            Vector3 screenPointAxisY  = camera.WorldToScreenPoint(projectedCenter + sideways * snapY);
            Vector3 screenPointAxisZ  = camera.WorldToScreenPoint(projectedCenter + sideways * snapZ);
            float   pixelSizeX        = (screenPointAxisX - screenPointCenter).magnitude; // size in pixels
            float   pixelSizeY        = (screenPointAxisY - screenPointCenter).magnitude; // size in pixels
            float   pixelSizeZ        = (screenPointAxisZ - screenPointCenter).magnitude; // size in pixels

            float screenPixelSize;

            switch(axis)
            {
                default:
                case GridAxis.AxisXZ:
                {
                    screenPixelSize = Mathf.Min(pixelSizeX, pixelSizeZ);

                    break;
                } //X/Z
                case GridAxis.AxisYZ:
                {
                    screenPixelSize = Mathf.Min(pixelSizeY, pixelSizeZ);

                    break;
                } //Y/Z
                case GridAxis.AxisXY:
                {
                    screenPixelSize = Mathf.Min(pixelSizeX, pixelSizeY);

                    break;
                } //X/Y
            }

            const float minPixelSize = 64.0f;

            float gridLevelPow = minPixelSize / screenPixelSize;

            Material gridMaterial = GridMaterial;
            Mesh     gridMesh     = GridMesh;

            if(!gridMaterial || !gridMesh)
            {
                return;
            }

            gridMaterial.SetFloat(gridSizeID, Mathf.Max(0, gridLevelPow));
            gridMaterial.SetVector(gridSpacingID, CSGSettings.SnapVector);

            Color gridColor = ColorSettings.GridColorW;
            gridColor.a *= alpha;
            gridMaterial.SetColor(gridCenterColorID, gridColor);

            gridColor   =  ColorSettings.GridColor1W;
            gridColor.a *= alpha;
            gridMaterial.SetColor(gridColorID, gridColor);

            camera.depthTextureMode = DepthTextureMode.Depth;

            gridMaterial.SetInt(cull, (int)CullMode.Off);
            gridMaterial.SetInt(zWrite, 0);
            gridMaterial.SetInt(zTest, (int)CompareFunction.LessEqual);

            gridMaterial.SetPass(0);
            Matrix4x4 gridMatrix = Matrix4x4.TRS(gridCenter, gridRotation, Vector3.one);
            Graphics.DrawMeshNow(gridMesh, gridMatrix, 0);
        }

        public static bool ForceGrid;
        private static Vector3 m_ForcedGridCenter = MathConstants.zeroVector3;

        public static Vector3 ForcedGridCenter
        {
            get => m_ForcedGridCenter;
            set => m_ForcedGridCenter = value;
        }

        private static Quaternion m_ForcedGridRotation = MathConstants.identityQuaternion;

        public static Quaternion ForcedGridRotation
        {
            get => m_ForcedGridRotation;
            set => m_ForcedGridRotation = value;
        }

        public static CSGPlane CurrentGridPlane
        {
            get
            {
                UpdateGridOrientation(Camera.current);

                return gridOrientation.gridPlane;
            }
        }

        public static CSGPlane CurrentWorkGridPlane
        {
            get
            {
                UpdateGridOrientation(Camera.current);

                return gridOrientation.gridWorkPlane;
            }
        }

        public static Vector3 CurrentWorkGridCenter
        {
            get
            {
                UpdateGridOrientation(Camera.current);

                return gridOrientation.gridWorkCenter;
            }
        }

        public static Vector3 CurrentGridSnapVector
        {
            get
            {
                UpdateGridOrientation(Camera.current);

                return gridOrientation.gridSnapVector;
            }
        }

        internal sealed class GridOrientation
        {
            public Camera gridCamera;
            public Vector3 gridCameraPosition;
            public Vector3 gridCameraSnapped;

            public Vector3 gridCenter = MathConstants.zeroVector3;
            public Quaternion gridRotation = MathConstants.identityQuaternion;
            public CSGPlane gridPlane;

            public Quaternion gridOrthoXRotation = MathConstants.identityQuaternion;
            public Quaternion gridOrthoYRotation = MathConstants.identityQuaternion;
            public Quaternion gridOrthoZRotation = MathConstants.identityQuaternion;

            public Vector3 gridWorkCenter = MathConstants.zeroVector3;
            public Quaternion gridWorkRotation = MathConstants.identityQuaternion;
            public Quaternion gridWorkInvRotation = MathConstants.identityQuaternion;
            public CSGPlane gridWorkPlane;

            public Vector3 gridSnapVector;
            public Vector3 gridSnapScale;

            public bool gridOrtho;
            public bool gridOrthoXVisible;
            public bool gridOrthoYVisible;
            public bool gridOrthoZVisible;
            public float gridOrthoXAlpha;
            public float gridOrthoYAlpha;
            public float gridOrthoZAlpha;
        }

        internal static GridOrientation gridOrientation;

        private static void OnRender(Camera camera)
        {
            UpdateGridOrientation(camera);

            if(gridOrientation.gridOrtho)
            {
                if(gridOrientation.gridOrthoXVisible)
                {
                    DrawGrid(
                        gridOrientation.gridCamera, gridOrientation.gridCameraPosition,
                        gridOrientation.gridCenter, gridOrientation.gridOrthoXRotation,
                        GridAxis.AxisYZ, gridOrientation.gridOrthoXAlpha, GridMode.Ortho
                    );
                }

                if(gridOrientation.gridOrthoYVisible)
                {
                    DrawGrid(
                        gridOrientation.gridCamera, gridOrientation.gridCameraPosition,
                        gridOrientation.gridCenter, gridOrientation.gridOrthoYRotation,
                        GridAxis.AxisXZ, gridOrientation.gridOrthoYAlpha, GridMode.Ortho
                    );
                }

                if(gridOrientation.gridOrthoZVisible)
                {
                    DrawGrid(
                        gridOrientation.gridCamera, gridOrientation.gridCameraPosition,
                        gridOrientation.gridCenter, gridOrientation.gridOrthoZRotation,
                        GridAxis.AxisXY, gridOrientation.gridOrthoZAlpha, GridMode.Ortho
                    );
                }
            }
            else
            {
                Vector3 forward     = gridOrientation.gridRotation * MathConstants.forwardVector3;
                Vector3 workForward = gridOrientation.gridWorkRotation * MathConstants.forwardVector3;

                if(ForceGrid && !((forward - workForward).magnitude < 0.001f || (forward + workForward).magnitude < 0.001f))
                {
                    DrawGrid(
                        gridOrientation.gridCamera, gridOrientation.gridCameraPosition,
                        gridOrientation.gridWorkCenter, gridOrientation.gridWorkRotation,
                        GridAxis.AxisXZ, 0.75f, GridMode.WorkPlane
                    );

                    DrawGrid(
                        gridOrientation.gridCamera, gridOrientation.gridCameraPosition,
                        gridOrientation.gridCenter, gridOrientation.gridRotation,
                        GridAxis.AxisXZ, 0.125f, GridMode.Regular
                    );
                }
                else
                {
                    DrawGrid(
                        gridOrientation.gridCamera, gridOrientation.gridCameraPosition,
                        gridOrientation.gridCenter, gridOrientation.gridRotation,
                        GridAxis.AxisXZ, 1.0f, GridMode.Regular
                    );
                }
            }
        }

        public static void RenderGrid(Camera camera)
        {
            if(Event.current.type != EventType.Repaint)
            {
                return;
            }

            OnRender(camera);
        }

        public static void UpdateGridOrientation(Camera camera)
        {
            if(camera == null)
            {
                return;
            }

            Vector3 cameraPosition = camera.cameraToWorldMatrix.MultiplyPoint(MathConstants.zeroVector3);
            Vector3 cameraForward  = camera.cameraToWorldMatrix.MultiplyVector(MathConstants.forwardVector3);

            gridOrientation                    = new GridOrientation();
            gridOrientation.gridCamera         = camera;
            gridOrientation.gridCameraPosition = cameraPosition;

            if(Tools.pivotRotation == PivotRotation.Local)
            {
                Transform activeTransform = Selection.activeTransform;

                if(activeTransform != null)
                {
                    Vector3   parentCenter = MathConstants.zeroVector3;
                    Transform parent       = activeTransform.parent;

                    if(parent != null)
                    {
                        parentCenter = parent.position;
                    }

                    gridOrientation.gridRotation = Tools.handleRotation;
                    gridOrientation.gridCenter   = parentCenter;
                }
            }

            gridOrientation.gridOrthoXVisible = false;
            gridOrientation.gridOrthoYVisible = false;
            gridOrientation.gridOrthoZVisible = false;
            gridOrientation.gridOrtho         = false;
            gridOrientation.gridOrthoXAlpha   = 0.0f;
            gridOrientation.gridOrthoYAlpha   = 0.0f;
            gridOrientation.gridOrthoZAlpha   = 0.0f;

            gridOrientation.gridWorkCenter   = gridOrientation.gridCenter;
            gridOrientation.gridWorkRotation = gridOrientation.gridRotation;

            if(camera.orthographic)
            {
                gridOrientation.gridOrtho = true;

                Vector3 dots = new Vector3(
                    Mathf.Clamp01(Mathf.Abs(Vector3.Dot(cameraForward, gridOrientation.gridRotation * MathConstants.rightVector3)) - 0.6f),
                    Mathf.Clamp01(Mathf.Abs(Vector3.Dot(cameraForward, gridOrientation.gridRotation * MathConstants.upVector3)) - 0.3f),
                    Mathf.Clamp01(Mathf.Abs(Vector3.Dot(cameraForward, gridOrientation.gridRotation * MathConstants.forwardVector3)) - 0.6f)
                ).normalized;

                dots.x *= dots.x;
                dots.y *= dots.y;
                dots.z *= dots.z;

                if(dots.x > 0.5f)
                {
                    Quaternion rotation = Quaternion.AngleAxis(90.0f, MathConstants.forwardVector3);
                    gridOrientation.gridOrthoXRotation = gridOrientation.gridRotation * rotation;
                    gridOrientation.gridOrthoXVisible  = true;
                    gridOrientation.gridOrthoXAlpha    = dots.x;
                }

                if(dots.y > 0.5f)
                {
                    gridOrientation.gridOrthoYRotation = gridOrientation.gridRotation;
                    gridOrientation.gridOrthoYVisible  = true;
                    gridOrientation.gridOrthoYAlpha    = dots.y;
                }

                if(dots.z > 0.5f)
                {
                    Quaternion rotation = Quaternion.AngleAxis(90.0f, MathConstants.leftVector3);
                    gridOrientation.gridOrthoZRotation = gridOrientation.gridRotation * rotation;
                    gridOrientation.gridOrthoZVisible  = true;
                    gridOrientation.gridOrthoZAlpha    = dots.z;
                }

                if(dots.y > dots.z)
                {
                    gridOrientation.gridWorkRotation = dots.y > dots.x ? gridOrientation.gridOrthoYRotation : gridOrientation.gridOrthoXRotation;
                }
                else
                {
                    gridOrientation.gridWorkRotation = dots.z > dots.x ? gridOrientation.gridOrthoZRotation : gridOrientation.gridOrthoXRotation;
                }

                gridOrientation.gridPlane = new CSGPlane(gridOrientation.gridWorkRotation, gridOrientation.gridWorkCenter);
            }
            else
            {
                gridOrientation.gridPlane = new CSGPlane(gridOrientation.gridWorkRotation, gridOrientation.gridWorkCenter);

                if(ForceGrid)
                {
                    gridOrientation.gridWorkCenter   = ForcedGridCenter;
                    gridOrientation.gridWorkRotation = ForcedGridRotation;
                }
            }

            gridOrientation.gridWorkInvRotation = Quaternion.Inverse(gridOrientation.gridWorkRotation);

            // find point on the plane that is nearest to camera
            Vector3 normal   = gridOrientation.gridWorkRotation * MathConstants.upVector3;
            float   d        = Vector3.Dot(normal, gridOrientation.gridCenter);
            Vector3 position = new CSGPlane(normal, d).Project(gridOrientation.gridCameraPosition);
            gridOrientation.gridCameraSnapped = position;

            gridOrientation.gridWorkPlane = new CSGPlane(normal, position);

            Vector3 euler = gridOrientation.gridWorkInvRotation.eulerAngles;
            euler.x = Mathf.Round(euler.x / 90) * 90;
            euler.y = Mathf.Round(euler.y / 90) * 90;
            euler.z = Mathf.Round(euler.z / 90) * 90;

            gridOrientation.gridSnapVector = Quaternion.Euler(euler) * CSGSettings.SnapVector;

            Vector3 snapScale = Quaternion.Euler(euler) * new Vector3(CSGSettings.LockAxisX ? 0 : 1, CSGSettings.LockAxisY ? 0 : 1, CSGSettings.LockAxisZ ? 0 : 1);

            snapScale.x = Mathf.Abs(snapScale.x);
            snapScale.y = Mathf.Abs(snapScale.y);
            snapScale.z = Mathf.Abs(snapScale.z);

            gridOrientation.gridSnapScale = snapScale;
        }

        public static Matrix4x4 ToGridSpaceMatrix()
        {
            return Matrix4x4.TRS(MathConstants.zeroVector3, gridOrientation.gridWorkInvRotation, MathConstants.oneVector3);
        }

        public static Matrix4x4 FromGridSpaceMatrix()
        {
            return Matrix4x4.TRS(MathConstants.zeroVector3, gridOrientation.gridWorkRotation, MathConstants.oneVector3);
        }

        public static Quaternion ToGridSpaceQuaternion()
        {
            return gridOrientation.gridWorkInvRotation;
        }

        public static Quaternion FromGridSpaceQuaternion()
        {
            return gridOrientation.gridWorkRotation;
        }

        public static Vector3 PointToGridSpace(Vector3 pos)
        {
            return gridOrientation.gridWorkInvRotation * (pos - gridOrientation.gridWorkCenter);
        }

        public static Vector3 VectorToGridSpace(Vector3 pos)
        {
            return gridOrientation.gridWorkInvRotation * pos;
        }

        public static CSGPlane PlaneToGridSpace(CSGPlane p)
        {
            return new CSGPlane(VectorToGridSpace(p.Normal), PointToGridSpace(p.PointOnPlane));
        }

        public static Vector3 PointFromGridSpace(Vector3 pos)
        {
            return gridOrientation.gridWorkRotation * pos + gridOrientation.gridWorkCenter;
        }

        public static Vector3 VectorFromGridSpace(Vector3 pos)
        {
            return gridOrientation.gridWorkRotation * pos;
        }

        public static CSGPlane PlaneFromGridSpace(CSGPlane p)
        {
            return new CSGPlane(VectorFromGridSpace(p.Normal), PointFromGridSpace(p.PointOnPlane));
        }

        private static Vector3 DeltaSnapRoundPosition(Vector3 currentPosition, Vector3 snapVector)
        {
            double posX  = currentPosition.x;
            double posY  = currentPosition.y;
            double posZ  = currentPosition.z;
            double snapX = snapVector.x;
            double snapY = snapVector.y;
            double snapZ = snapVector.z;

            double snappedX = Math.Round(posX / snapX) * snapX;
            double snappedY = Math.Round(posY / snapY) * snapY;
            double snappedZ = Math.Round(posZ / snapZ) * snapZ;

            currentPosition.x = (float)(snappedX - posX);
            currentPosition.y = (float)(snappedY - posY);
            currentPosition.z = (float)(snappedZ - posZ);

            return currentPosition;
        }

        private static Vector3 SnapRoundPosition(Vector3 currentPosition, Vector3 snapVector)
        {
            double snapX = snapVector.x;
            double snapY = snapVector.y;
            double snapZ = snapVector.z;

            currentPosition.x = (float)(Math.Round(currentPosition.x / snapX) * snapX);
            currentPosition.y = (float)(Math.Round(currentPosition.y / snapY) * snapY);
            currentPosition.z = (float)(Math.Round(currentPosition.z / snapZ) * snapZ);

            return currentPosition;
        }

        private static Vector3 SnapFloorPosition(Vector3 currentPosition, Vector3 snapVector)
        {
            currentPosition.x = Mathf.FloorToInt(currentPosition.x / snapVector.x) * snapVector.x;
            currentPosition.y = Mathf.FloorToInt(currentPosition.y / snapVector.y) * snapVector.y;
            currentPosition.z = Mathf.FloorToInt(currentPosition.z / snapVector.z) * snapVector.z;

            return currentPosition;
        }

        private static Vector3 SnapCeilPosition(Vector3 currentPosition, Vector3 snapVector)
        {
            currentPosition.x = Mathf.FloorToInt(currentPosition.x / snapVector.x + 1) * snapVector.x;
            currentPosition.y = Mathf.FloorToInt(currentPosition.y / snapVector.y + 1) * snapVector.y;
            currentPosition.z = Mathf.FloorToInt(currentPosition.z / snapVector.z + 1) * snapVector.z;

            return currentPosition;
        }

        public static List<Vector3> FindAllGridEdgesThatTouchPoint(Camera camera, Vector3 point)
        {
            List<Vector3> lines = new List<Vector3>();

            UpdateGridOrientation(camera);

            if(gridOrientation == null)
            {
                return lines;
            }

            Vector3   snapVector       = gridOrientation.gridSnapVector;
            Vector3   gridPoint        = PointToGridSpace(point);
            Vector3   snappedGridPoint = SnapRoundPosition(gridPoint, snapVector);
            const int lineSize         = 10000;

            CSGPlane gridPlane = new CSGPlane(CurrentWorkGridPlane.Normal, point);

            if(Math.Abs(gridPoint.x - snappedGridPoint.x) < MathConstants.EQUALITY_EPSILON)
            {
                Vector3 pointA = new Vector3(-lineSize, gridPoint.y, gridPoint.z);
                Vector3 pointB = new Vector3(lineSize, gridPoint.y, gridPoint.z);

                lines.Add(gridPlane.Project(PointFromGridSpace(pointA)));
                lines.Add(gridPlane.Project(PointFromGridSpace(pointB)));
            }

            if(Math.Abs(gridPoint.y - snappedGridPoint.y) < MathConstants.EQUALITY_EPSILON)
            {
                Vector3 pointA = new Vector3(gridPoint.x, -lineSize, gridPoint.z);
                Vector3 pointB = new Vector3(gridPoint.x, lineSize, gridPoint.z);

                lines.Add(gridPlane.Project(PointFromGridSpace(pointA)));
                lines.Add(gridPlane.Project(PointFromGridSpace(pointB)));
            }

            if(Math.Abs(gridPoint.z - snappedGridPoint.z) < MathConstants.EQUALITY_EPSILON)
            {
                Vector3 pointA = new Vector3(gridPoint.x, gridPoint.y, -lineSize);
                Vector3 pointB = new Vector3(gridPoint.x, gridPoint.y, lineSize);

                lines.Add(gridPlane.Project(PointFromGridSpace(pointA)));
                lines.Add(gridPlane.Project(PointFromGridSpace(pointB)));
            }

            return lines;
        }

        public static Vector3 ForceSnapToGrid(Camera camera, Vector3 worldPoint)
        {
            return worldPoint + SnapDeltaToGrid(camera, MathConstants.zeroVector3, new[] { worldPoint });
        }

        public static Vector3 ForceSnapDeltaToGrid(Camera camera, Vector3 worldDeltaMovement, Vector3 worldPoint)
        {
            return SnapDeltaToGrid(camera, worldDeltaMovement, new[] { worldPoint });
        }

        public static Vector3 HandleLockedAxi(Vector3 worldDeltaMovement)
        {
            if(gridOrientation == null)
            {
                return worldDeltaMovement;
            }

            Vector3 snapScale              = gridOrientation.gridSnapScale;
            Vector3 gridLocalDeltaMovement = VectorToGridSpace(worldDeltaMovement);

            gridLocalDeltaMovement.x *= snapScale.x;
            gridLocalDeltaMovement.y *= snapScale.y;
            gridLocalDeltaMovement.z *= snapScale.z;

            return VectorFromGridSpace(gridLocalDeltaMovement);
        }

        public static Vector3 SnapLocalPointToWorldGridDelta(Camera camera, Matrix4x4 pointLocalToWorld, Matrix4x4 pointWorldToLocal, Vector3[] localPoints)
        {
            UpdateGridOrientation(camera);

            if(gridOrientation == null || localPoints == null || localPoints.Length == 0)
            {
                return Vector3.zero;
            }

            Matrix4x4 worldToGridLocal      = Matrix4x4.TRS(-gridOrientation.gridWorkCenter, Quaternion.identity, Vector3.one) * ToGridSpaceMatrix();
            Matrix4x4 gridLocalToWorld      = FromGridSpaceMatrix() * Matrix4x4.TRS(gridOrientation.gridWorkCenter, Quaternion.identity, Vector3.one);
            Matrix4x4 pointLocalToGridLocal = pointLocalToWorld * worldToGridLocal;

            Vector3[] gridLocalPoints;

            if(localPoints.Length > 1)
            {
                AABB bounds = new AABB();
                bounds.Reset();

                for(int i = 0; i < localPoints.Length; i++)
                {
                    Vector3 localPoint = pointLocalToGridLocal.MultiplyPoint(localPoints[i]);

                    if(float.IsNaN(localPoint.x) || float.IsNaN(localPoint.y) || float.IsNaN(localPoint.z) || float.IsInfinity(localPoint.x) || float.IsInfinity(localPoint.y) || float.IsInfinity(localPoint.z))
                    {
                        continue;
                    }

                    bounds.Extend(localPoint);
                }

                gridLocalPoints = bounds.GetCorners();
            }
            else
            {
                Vector3 localGridSpacePoint = pointLocalToGridLocal.MultiplyPoint(localPoints[0]);

                if(float.IsNaN(localGridSpacePoint.x)
                || float.IsNaN(localGridSpacePoint.y)
                || float.IsNaN(localGridSpacePoint.z)
                || float.IsInfinity(localGridSpacePoint.x)
                || float.IsInfinity(localGridSpacePoint.y)
                || float.IsInfinity(localGridSpacePoint.z))
                {
                    gridLocalPoints = Array.Empty<Vector3>();
                }
                else
                {
                    gridLocalPoints = new[] { localGridSpacePoint };
                }
            }

            Vector3 snappedDeltaMovement = Vector3.zero;
            Vector3 snapVector           = gridOrientation.gridSnapVector;

            for(int i = 0; i < gridLocalPoints.Length; i++)
            {
                Vector3 foundDeltaMovement = DeltaSnapRoundPosition(gridLocalPoints[i], snapVector);

                if(i == 0 || Math.Abs(foundDeltaMovement.x) < Mathf.Abs(snappedDeltaMovement.x))
                {
                    snappedDeltaMovement.x = foundDeltaMovement.x;
                }

                if(i == 0 || Math.Abs(foundDeltaMovement.y) < Mathf.Abs(snappedDeltaMovement.y))
                {
                    snappedDeltaMovement.y = foundDeltaMovement.y;
                }

                if(i == 0 || Math.Abs(foundDeltaMovement.z) < Mathf.Abs(snappedDeltaMovement.z))
                {
                    snappedDeltaMovement.z = foundDeltaMovement.z;
                }
            }

            Vector3 scaleVector = gridOrientation.gridSnapScale;
            snappedDeltaMovement.x *= scaleVector.x;
            snappedDeltaMovement.y *= scaleVector.y;
            snappedDeltaMovement.z *= scaleVector.z;
            Vector3 worldDeltaMovement = gridLocalToWorld.MultiplyVector(snappedDeltaMovement);

            return worldDeltaMovement;
        }

        public static Vector3 SnapDeltaToGrid(Camera camera, Vector3 worldDeltaMovement, Vector3[] worldPoints, bool snapToGridPlane = true, bool snapToSelf = false)
        {
            UpdateGridOrientation(camera);

            if(gridOrientation == null || worldPoints == null || worldPoints.Length == 0)
            {
                return worldDeltaMovement;
            }

            CSGPlane worldPlane  = gridOrientation.gridWorkPlane;
            Vector3  scaleVector = gridOrientation.gridSnapScale;
            Vector3  snapVector  = gridOrientation.gridSnapVector;

            Vector3  gridLocalDeltaMovement = VectorToGridSpace(worldDeltaMovement);
            CSGPlane gridLocalPlane         = PlaneToGridSpace(worldPlane);

            if(snapToGridPlane)
            {
                scaleVector.x *= Mathf.Abs(gridLocalPlane.a) >= 1 - MathConstants.EQUALITY_EPSILON ? 0 : 1;
                scaleVector.y *= Mathf.Abs(gridLocalPlane.b) >= 1 - MathConstants.EQUALITY_EPSILON ? 0 : 1;
                scaleVector.z *= Mathf.Abs(gridLocalPlane.c) >= 1 - MathConstants.EQUALITY_EPSILON ? 0 : 1;
            }

            Vector3 snappedDeltaMovement = gridLocalDeltaMovement;

            if(Mathf.Abs(scaleVector.x) < MathConstants.EQUALITY_EPSILON)
            {
                snappedDeltaMovement.x = 0;
            }

            if(Mathf.Abs(scaleVector.y) < MathConstants.EQUALITY_EPSILON)
            {
                snappedDeltaMovement.y = 0;
            }

            if(Mathf.Abs(scaleVector.z) < MathConstants.EQUALITY_EPSILON)
            {
                snappedDeltaMovement.z = 0;
            }

            Vector3[] gridLocalPoints;

            if(worldPoints.Length > 1)
            {
                AABB bounds = new AABB();
                bounds.Reset();

                for(int i = 0; i < worldPoints.Length; i++)
                {
                    Vector3 localPoint = PointToGridSpace(worldPoints[i]);

                    if(snapToGridPlane)
                    {
                        localPoint = GeometryUtility.ProjectPointOnPlane(gridLocalPlane, localPoint);
                    }

                    if(float.IsNaN(localPoint.x)
                    || float.IsNaN(localPoint.y)
                    || float.IsNaN(localPoint.z)
                    || float.IsInfinity(localPoint.x)
                    || float.IsInfinity(localPoint.y)
                    || float.IsInfinity(localPoint.z))
                    {
                        continue;
                    }

                    bounds.Extend(localPoint);
                }

                gridLocalPoints = bounds.GetCorners();
            }
            else
            {
                Vector3 localGridSpacePoint = PointToGridSpace(worldPoints[0]);
                Vector3 projectedPoint      = localGridSpacePoint;

                if(snapToGridPlane)
                {
                    projectedPoint = GeometryUtility.ProjectPointOnPlane(gridLocalPlane, localGridSpacePoint);
                }

                if(float.IsNaN(projectedPoint.x)
                || float.IsNaN(projectedPoint.y)
                || float.IsNaN(projectedPoint.z)
                || float.IsInfinity(projectedPoint.x)
                || float.IsInfinity(projectedPoint.y)
                || float.IsInfinity(projectedPoint.z))
                {
                    gridLocalPoints = Array.Empty<Vector3>();
                }
                else
                {
                    gridLocalPoints = new[] { projectedPoint };
                }
            }

            for(int i = 0; i < gridLocalPoints.Length; i++)
            {
                Vector3 oldPoint = gridLocalPoints[i];
                Vector3 newPoint = gridLocalPoints[i] + gridLocalDeltaMovement;

                if(snapToGridPlane)
                {
                    newPoint = GeometryUtility.ProjectPointOnPlane(gridLocalPlane, newPoint);
                }

                newPoint = GridUtility.CleanPosition(newPoint);

                Vector3 snappedNewPoint = SnapRoundPosition(newPoint, snapVector);

                if(snapToGridPlane)
                {
                    snappedNewPoint = GeometryUtility.ProjectPointOnPlane(gridLocalPlane, snappedNewPoint);
                }

                snappedNewPoint = GridUtility.CleanPosition(snappedNewPoint);

                Vector3 foundDeltaMovement = snappedNewPoint - oldPoint;

                foundDeltaMovement.x *= scaleVector.x;
                foundDeltaMovement.y *= scaleVector.y;
                foundDeltaMovement.z *= scaleVector.z;

                if(i == 0 || Math.Abs(foundDeltaMovement.x) < Mathf.Abs(snappedDeltaMovement.x))
                {
                    snappedDeltaMovement.x = foundDeltaMovement.x;
                }

                if(i == 0 || Math.Abs(foundDeltaMovement.y) < Mathf.Abs(snappedDeltaMovement.y))
                {
                    snappedDeltaMovement.y = foundDeltaMovement.y;
                }

                if(i == 0 || Math.Abs(foundDeltaMovement.z) < Mathf.Abs(snappedDeltaMovement.z))
                {
                    snappedDeltaMovement.z = foundDeltaMovement.z;
                }
            }

            if(snapToSelf)
            {
                Vector3 snapDelta = snappedDeltaMovement - gridLocalDeltaMovement;

                if(Mathf.Abs(snapDelta.x) > Mathf.Abs(gridLocalDeltaMovement.x))
                {
                    snappedDeltaMovement.x = 0;
                }

                if(Mathf.Abs(snapDelta.y) > Mathf.Abs(gridLocalDeltaMovement.y))
                {
                    snappedDeltaMovement.y = 0;
                }

                if(Mathf.Abs(snapDelta.z) > Mathf.Abs(gridLocalDeltaMovement.z))
                {
                    snappedDeltaMovement.z = 0;
                }
            }

            worldDeltaMovement = VectorFromGridSpace(snappedDeltaMovement);

            return worldDeltaMovement;
        }

        public static Vector3 SnapDeltaRelative(Camera camera, Vector3 worldDeltaMovement, bool snapToGridPlane = true)
        {
            UpdateGridOrientation(camera);

            if(gridOrientation == null)
            {
                return worldDeltaMovement;
            }

            CSGPlane worldPlane  = gridOrientation.gridWorkPlane;
            Vector3  scaleVector = gridOrientation.gridSnapScale;
            Vector3  snapVector  = gridOrientation.gridSnapVector;

            Vector3  gridLocalDeltaMovement = VectorToGridSpace(worldDeltaMovement);
            CSGPlane gridLocalPlane         = PlaneToGridSpace(worldPlane);

            if(snapToGridPlane)
            {
                scaleVector.x *= Mathf.Abs(gridLocalPlane.a) >= 1 - MathConstants.EQUALITY_EPSILON ? 0 : 1;
                scaleVector.y *= Mathf.Abs(gridLocalPlane.b) >= 1 - MathConstants.EQUALITY_EPSILON ? 0 : 1;
                scaleVector.z *= Mathf.Abs(gridLocalPlane.c) >= 1 - MathConstants.EQUALITY_EPSILON ? 0 : 1;
            }

            Vector3 snappedDeltaMovement = gridLocalDeltaMovement;

            if(snapToGridPlane)
            {
                snappedDeltaMovement = GeometryUtility.ProjectPointOnPlane(gridLocalPlane, snappedDeltaMovement);
            }

            snappedDeltaMovement = GridUtility.CleanPosition(snappedDeltaMovement);
            snappedDeltaMovement = SnapRoundPosition(snappedDeltaMovement, snapVector);

            if(snapToGridPlane)
            {
                snappedDeltaMovement = GeometryUtility.ProjectPointOnPlane(gridLocalPlane, snappedDeltaMovement);
            }

            snappedDeltaMovement = GridUtility.CleanPosition(snappedDeltaMovement);

            snappedDeltaMovement.x *= scaleVector.x;
            snappedDeltaMovement.y *= scaleVector.y;
            snappedDeltaMovement.z *= scaleVector.z;

            worldDeltaMovement = VectorFromGridSpace(snappedDeltaMovement);

            return worldDeltaMovement;
        }

        public static Vector3 ForceSnapToRay(Camera camera, Ray worldRay, Vector3 worldPoint)
        {
            return worldPoint + SnapDeltaToRayGrid(camera, worldRay, MathConstants.zeroVector3, new[] { worldPoint });
        }

        public static Vector3 ForceSnapDeltaToRay(Camera camera, Ray worldRay, Vector3 worldDeltaMovement, Vector3 worldPoint)
        {
            return SnapDeltaToRayGrid(camera, worldRay, worldDeltaMovement, new[] { worldPoint });
        }

        public static Vector3 SnapDeltaToRayGrid(Camera camera, Ray worldRay, Vector3 worldDeltaMovement, Vector3[] worldPoints, bool snapToSelf = false)
        {
            UpdateGridOrientation(camera);

            if(gridOrientation == null || worldPoints == null || worldPoints.Length == 0)
            {
                return worldDeltaMovement;
            }

            Vector3 snapVector  = gridOrientation.gridSnapVector;
            Vector3 scaleVector = gridOrientation.gridSnapScale;

            Vector3 localDeltaMovement = VectorToGridSpace(worldDeltaMovement);
            Vector3 localLineDir       = VectorToGridSpace(worldRay.direction);
            Vector3 localLineOrg       = PointToGridSpace(worldRay.origin);

            scaleVector.x *= Mathf.Abs(localLineDir.y) >= 1 - MathConstants.EQUALITY_EPSILON || Mathf.Abs(localLineDir.z) >= 1 - MathConstants.EQUALITY_EPSILON ? 0 : 1;
            scaleVector.y *= Mathf.Abs(localLineDir.x) >= 1 - MathConstants.EQUALITY_EPSILON || Mathf.Abs(localLineDir.z) >= 1 - MathConstants.EQUALITY_EPSILON ? 0 : 1;
            scaleVector.z *= Mathf.Abs(localLineDir.x) >= 1 - MathConstants.EQUALITY_EPSILON || Mathf.Abs(localLineDir.y) >= 1 - MathConstants.EQUALITY_EPSILON ? 0 : 1;

            Vector3 snappedDeltaMovement = localDeltaMovement;

            if(Mathf.Abs(scaleVector.x) < MathConstants.EQUALITY_EPSILON)
            {
                snappedDeltaMovement.x = 0;
            }

            if(Mathf.Abs(scaleVector.y) < MathConstants.EQUALITY_EPSILON)
            {
                snappedDeltaMovement.y = 0;
            }

            if(Mathf.Abs(scaleVector.z) < MathConstants.EQUALITY_EPSILON)
            {
                snappedDeltaMovement.z = 0;
            }

            Vector3[] localPoints;

            if(worldPoints.Length > 1)
            {
                AABB bounds = new AABB();
                bounds.Reset();

                for(int i = 0; i < worldPoints.Length; i++)
                {
                    Vector3 localPoint = GeometryUtility.ProjectPointOnInfiniteLine(PointToGridSpace(worldPoints[i]), localLineOrg, localLineDir);

                    bounds.Extend(localPoint);
                }

                localPoints = bounds.GetCorners();
            }
            else
            {
                localPoints = new[] { GeometryUtility.ProjectPointOnInfiniteLine(PointToGridSpace(worldPoints[0]), localLineOrg, localLineDir) };
            }

            for(int i = 0; i < localPoints.Length; i++)
            {
                Vector3 oldPoint        = localPoints[i];
                Vector3 newPoint        = GeometryUtility.ProjectPointOnInfiniteLine(oldPoint + localDeltaMovement, localLineOrg, localLineDir);
                Vector3 snappedNewPoint = SnapRoundPosition(newPoint, snapVector);

                snappedNewPoint = GridUtility.CleanPosition(GeometryUtility.ProjectPointOnInfiniteLine(snappedNewPoint, localLineOrg, localLineDir));

                Vector3 foundDeltaMovement = snappedNewPoint - oldPoint;

                foundDeltaMovement.x *= scaleVector.x;
                foundDeltaMovement.y *= scaleVector.y;
                foundDeltaMovement.z *= scaleVector.z;

                if(i == 0 || Math.Abs(foundDeltaMovement.x) < Mathf.Abs(snappedDeltaMovement.x))
                {
                    snappedDeltaMovement.x = foundDeltaMovement.x;
                }

                if(i == 0 || Math.Abs(foundDeltaMovement.y) < Mathf.Abs(snappedDeltaMovement.y))
                {
                    snappedDeltaMovement.y = foundDeltaMovement.y;
                }

                if(i == 0 || Math.Abs(foundDeltaMovement.z) < Mathf.Abs(snappedDeltaMovement.z))
                {
                    snappedDeltaMovement.z = foundDeltaMovement.z;
                }
            }

            if(snapToSelf)
            {
                Vector3 snapDelta = snappedDeltaMovement - localDeltaMovement;

                if(Mathf.Abs(snapDelta.x) > Mathf.Abs(localDeltaMovement.x))
                {
                    snappedDeltaMovement.x = 0;
                }

                if(Mathf.Abs(snapDelta.y) > Mathf.Abs(localDeltaMovement.y))
                {
                    snappedDeltaMovement.y = 0;
                }

                if(Mathf.Abs(snapDelta.z) > Mathf.Abs(localDeltaMovement.z))
                {
                    snappedDeltaMovement.z = 0;
                }
            }

            worldDeltaMovement = VectorFromGridSpace(snappedDeltaMovement);

            return worldDeltaMovement;
        }

        public static Vector3 SnapDeltaToRayRelative(Camera camera, Ray worldRay, Vector3 worldDeltaMovement)
        {
            UpdateGridOrientation(camera);

            if(gridOrientation == null)
            {
                return worldDeltaMovement;
            }

            Vector3 snapVector = gridOrientation.gridSnapVector;

            Vector3 localDeltaMovement = VectorToGridSpace(worldDeltaMovement);
            float   magnitude          = localDeltaMovement.magnitude;

            if(magnitude == 0)
            {
                return worldDeltaMovement;
            }

            Vector3 direction = localDeltaMovement.normalized;

            double snapY = snapVector.y;
            magnitude = (float)(Math.Round(magnitude / snapY) * snapY);

            Vector3 snappedDeltaMovement = direction * magnitude;

            worldDeltaMovement = VectorFromGridSpace(snappedDeltaMovement);

            return worldDeltaMovement;
        }

        public static bool SetupWorkPlane(Camera camera, Vector3 worldCenterPoint, ref CSGPlane workPlane)
        {
            if(camera == null || !camera)
            {
                return false;
            }

            if(camera.orthographic)
            {
                ForceGrid = false;
                workPlane = CurrentWorkGridPlane;

                return true;
            }

            Vector3 normal = CurrentGridPlane.Normal;

            workPlane = new CSGPlane(GridUtility.CleanNormal(normal), worldCenterPoint);

            return SetForcedGrid(camera, workPlane);
        }

        public static bool SetupWorkPlane(Camera camera, Vector3 worldCenterPoint, Vector3 worldDirection, ref CSGPlane workPlane)
        {
            if(camera == null || !camera)
            {
                return false;
            }

            if(camera.orthographic)
            {
                ForceGrid = false;
                workPlane = CurrentWorkGridPlane;

                return true;
            }

            workPlane = new CSGPlane(GridUtility.CleanNormal(worldDirection), worldCenterPoint);

            return SetForcedGrid(camera, workPlane);
        }

        public static bool SetupRayWorkPlane(Camera camera, Vector3 worldOrigin, Vector3 worldDirection, ref CSGPlane outWorkPlane)
        {
            if(camera == null || !camera)
            {
                return false;
            }

            Vector3 tangent;
            Vector3 cameraBackwards      = -camera.transform.forward;
            Vector3 closestAxisForward   = GeometryUtility.SnapToClosestAxis(cameraBackwards);
            Vector3 closestAxisDirection = GeometryUtility.SnapToClosestAxis(worldDirection);

            if(Vector3.Dot(closestAxisForward, closestAxisDirection) != 0)
            {
                float dot1 = Mathf.Abs(Vector3.Dot(cameraBackwards, MathConstants.rightVector3));
                float dot2 = Mathf.Abs(Vector3.Dot(cameraBackwards, MathConstants.upVector3));
                float dot3 = Mathf.Abs(Vector3.Dot(cameraBackwards, MathConstants.forwardVector3));

                if(dot1 < dot2)
                {
                    tangent = dot1 < dot3 ? MathConstants.rightVector3 : MathConstants.forwardVector3;
                }
                else
                {
                    tangent = dot2 < dot3 ? MathConstants.upVector3 : MathConstants.forwardVector3;
                }
            }
            else
            {
                tangent = Vector3.Cross(worldDirection, closestAxisForward);
            }

            Vector3 normal = !camera.orthographic ? Vector3.Cross(worldDirection, tangent) : cameraBackwards;

            outWorkPlane = new CSGPlane(GridUtility.CleanNormal(normal), worldOrigin);

            return SetForcedGrid(camera, outWorkPlane);
        }

        public static Vector3 CubeProject(Camera camera, CSGPlane plane, Vector3 pos)
        {
            UpdateGridOrientation(camera);
            Vector3 closestAxis  = GeometryUtility.SnapToClosestAxis(plane.Normal);
            Vector3 intersection = plane.LineIntersection(pos, pos + closestAxis);

            if(float.IsNaN(intersection.x) || float.IsInfinity(intersection.x) ||
               float.IsNaN(intersection.y) || float.IsInfinity(intersection.y) ||
               float.IsNaN(intersection.z) || float.IsInfinity(intersection.z))
            {
                // should never happen, but if all else fails just do a projection ..
                intersection = plane.Project(pos);
            }

            return intersection;
        }

        public static bool SetForcedGrid(Camera camera, CSGPlane plane)
        {
            if(float.IsNaN(plane.a) || float.IsInfinity(plane.a) ||
               float.IsNaN(plane.b) || float.IsInfinity(plane.b) ||
               float.IsNaN(plane.c) || float.IsInfinity(plane.c) ||
               float.IsNaN(plane.d) || float.IsInfinity(plane.d) ||
               plane.Normal.sqrMagnitude < MathConstants.NORMAL_EPSILON)
            {
                Debug.LogWarning("Invalid plane passed to SetForcedGrid");

                return false;
            }

            // cube-project the center of the grid so that it lies on the plane

            UpdateGridOrientation(camera);
            Vector3 center = CubeProject(camera, plane, gridOrientation.gridCenter);

            Vector3 normal = Quaternion.Inverse(gridOrientation.gridRotation) * plane.Normal;

            if(normal.sqrMagnitude < MathConstants.NORMAL_EPSILON)
            {
                return false;
            }

            Vector3    tangent = Vector3.Cross(normal, Vector3.Cross(normal, GeometryUtility.CalculateTangent(normal)).normalized).normalized;
            Quaternion q       = gridOrientation.gridRotation * Quaternion.LookRotation(tangent, normal);

            ForceGrid          = true;
            ForcedGridCenter   = center;
            ForcedGridRotation = q;

            return true;
        }
    }
}
