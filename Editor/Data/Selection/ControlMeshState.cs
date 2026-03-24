using System;
using UnityEngine;
using InternalRealtimeCSG;
using UnityEditor;
using RealtimeCSG.Legacy;
using RealtimeCSG.Components;
using UnityEngine.Serialization;

namespace RealtimeCSG
{
    [Flags, Serializable]
    internal enum SelectState : byte
    {
        None = 0
      , Hovering = 1
      , Selected = 2
    }

    internal sealed class SceneMeshState
    {
        public Camera camera;
        public bool[] worldPointBackfaced;
        public float[] worldPointSizes;         // render
        public float[] polygonCenterPointSizes; // render
        public Color[] worldPointColors;        // render
    }

    // TODO: simplify this

    [Serializable]
    internal partial class ControlMeshState
    {
        [SerializeField] public Transform brushTransform;
        [SerializeField] public Transform modelTransform;

        // backup stuff, used while editing

        [SerializeField] public Vector3[] backupPoints;
        [SerializeField] public Vector3[] backupPolygonCenterPoints;
        [SerializeField] public CSGPlane[] backupPolygonCenterPlanes;

        // ids

        [SerializeField] public int[] pointControlId;
        [SerializeField] public int[] edgeControlId;
        [SerializeField] public int[] polygonControlId;

        // geometry helpers

        [FormerlySerializedAs("WorldPoints"), SerializeField]
        public Vector3[] worldPoints;

        [SerializeField] public int[] edges;
        [SerializeField] public int[] edgeStateToHalfEdge;
        [SerializeField] public int[] halfEdgeToEdgeStates; // indicesToEdges[edge-index] = index into state 'edges'
        [SerializeField] public int[] edgeSurfaces;

        [SerializeField] public Vector3[] polygonCenterPoints;
        [SerializeField] public CSGPlane[] polygonCenterPlanes;
        public int[][] polygonPointIndices;

        // rendering helpers

        [SerializeField] public Color[] edgeColors;          // render
        [SerializeField] public Color[] polygonColors;       // render
        [SerializeField] public Color[] polygonCenterColors; // render

        [SerializeField] public Vector3 brushCenter; // render

        private SceneMeshState[] _cameraStates = Array.Empty<SceneMeshState>(); // render

        public ControlMeshState(CSGBrush brush)
        {
            UpdateTransforms(brush);

            ControlMesh controlMesh = brush.ControlMesh;

            if(controlMesh == null)
            {
                brush.ControlMesh = new ControlMesh();
                controlMesh       = brush.ControlMesh;
            }

            if(controlMesh.Vertices == null)
            {
                controlMesh.Vertices = Array.Empty<Vector3>();
            }

            if(controlMesh.Edges == null)
            {
                controlMesh.Edges = Array.Empty<HalfEdge>();
            }

            if(controlMesh.Polygons == null)
            {
                controlMesh.Polygons = Array.Empty<Polygon>();
            }

            AllocatePoints(controlMesh.Vertices.Length);
            AllocateEdges(controlMesh.Edges.Length);
            AllocatePolygons(controlMesh.Polygons.Length);
        }

        private void AllocatePoints(int pointCount)
        {
            pointControlId   = new int[pointCount];
            selection.points = new SelectState[pointCount];
            worldPoints      = new Vector3[pointCount];
        }

        private void AllocateEdges(int edgeCount)
        {
            edges                = new int[edgeCount];
            edgeSurfaces         = new int[edgeCount];
            halfEdgeToEdgeStates = new int[edgeCount];
            edgeStateToHalfEdge  = new int[edgeCount / 2];
            edgeColors           = new Color[edgeCount / 2];
            selection.edges      = new SelectState[edgeCount / 2];
            edgeControlId        = new int[edgeCount / 2];
        }

        private void AllocatePolygons(int polygonCount)
        {
            polygonControlId    = new int[polygonCount];
            selection.polygons  = new SelectState[polygonCount];
            polygonCenterPoints = new Vector3[polygonCount];
            polygonColors       = new Color[polygonCount];
            polygonCenterColors = new Color[polygonCount * 2];
            polygonCenterPlanes = new CSGPlane[polygonCount];
            polygonPointIndices = new int[polygonCount][];
        }

        public SceneMeshState GetCameraState(Camera camera, bool generate)
        {
            if(!camera)
            {
                return null;
            }

            if(_cameraStates != null)
            {
                for(int i = _cameraStates.Length - 1; i >= 0; i--)
                {
                    SceneMeshState sceneMeshState = _cameraStates[i];

                    if(sceneMeshState == null || ReferenceEquals(sceneMeshState.camera, null) || !sceneMeshState.camera)
                    {
                        ArrayUtility.RemoveAt(ref _cameraStates, i);

                        continue;
                    }

                    if(_cameraStates[i].camera == camera)
                    {
                        return _cameraStates[i];
                    }
                }
            }

            if(!generate)
            {
                return null;
            }

            int index;

            if(_cameraStates != null)
            {
                index = _cameraStates.Length;
                Array.Resize(ref _cameraStates, index + 1);
            }
            else
            {
                _cameraStates = new SceneMeshState[1];
                index         = 0;
            }

            _cameraStates[index]                         = new SceneMeshState();
            _cameraStates[index].camera                  = camera;
            _cameraStates[index].worldPointSizes         = new float[pointControlId.Length];
            _cameraStates[index].worldPointBackfaced     = new bool[pointControlId.Length];
            _cameraStates[index].worldPointColors        = new Color[pointControlId.Length * 2];
            _cameraStates[index].polygonCenterPointSizes = new float[polygonCenterPoints.Length];

            return _cameraStates[index];
        }

        public void UpdateTransforms(CSGBrush brush)
        {
            brushTransform = brush.GetComponent<Transform>();
            modelTransform = InternalCSGModelManager.FindModelTransform(brushTransform);
        }

        public void UpdateMesh(ControlMesh controlMesh, Vector3[] vertices = null)
        {
            if(controlMesh == null || controlMesh.Vertices == null || controlMesh.Edges == null || controlMesh.Polygons == null || polygonPointIndices == null)
            {
                return;
            }

            if(vertices == null)
            {
                vertices = controlMesh.Vertices;
            }

            if(!brushTransform)
            {
                return;
            }

            int pointCount = vertices.Length;

            if(worldPoints.Length != pointCount)
            {
                AllocatePoints(pointCount);
            }

            int edgeCount = controlMesh.Edges.Length;

            if(edges.Length != edgeCount)
            {
                AllocateEdges(edgeCount);
            }

            int polygonCount = controlMesh.Polygons.Length;

            if(polygonControlId.Length != polygonCount)
            {
                AllocatePolygons(polygonCount);
            }

            int index = 0;

            for(int e = 0; e < edgeCount; e++)
            {
                if(e >= controlMesh.Edges.Length ||
                   index >= edges.Length)
                {
                    continue;
                }

                int twin = controlMesh.Edges[e].TwinIndex;

                // if it's less than e then we've already handled our twin
                if(twin < e || twin >= controlMesh.Edges.Length)
                {
                    continue;
                }

                short polygonIndex = controlMesh.Edges[e].PolygonIndex;

                if(polygonIndex < 0 || polygonIndex >= controlMesh.Polygons.Length)
                {
                    continue;
                }

                short twinPolygonIndex = controlMesh.Edges[twin].PolygonIndex;

                if(twinPolygonIndex < 0 || twinPolygonIndex >= controlMesh.Polygons.Length)
                {
                    continue;
                }

                short vertexIndex1 = controlMesh.Edges[e].VertexIndex;
                short vertexIndex2 = controlMesh.Edges[twin].VertexIndex;

                if(vertexIndex1 < 0 || vertexIndex1 >= selection.points.Length || vertexIndex2 < 0 || vertexIndex2 >= selection.points.Length)
                {
                    continue;
                }

                edges[index]                   = vertexIndex1;
                edges[index + 1]               = vertexIndex2;
                edgeStateToHalfEdge[index / 2] = e;
                halfEdgeToEdgeStates[e]        = index;
                halfEdgeToEdgeStates[twin]     = index;
                edgeSurfaces[index]            = polygonIndex;
                edgeSurfaces[index + 1]        = twinPolygonIndex;

                index += 2;
            }

            bool polygonCountModified = false;

            while(polygonCount > polygonPointIndices.Length)
            {
                ArrayUtility.Add(ref polygonPointIndices, null);
                polygonCountModified = true;
            }

            while(polygonCount < polygonPointIndices.Length)
            {
                ArrayUtility.RemoveAt(ref polygonPointIndices, polygonPointIndices.Length - 1);
                polygonCountModified = true;
            }

            if(polygonCountModified)
            {
                for(int i = 0; i < polygonCount; i++)
                {
                    polygonPointIndices[i] = null;
                }
            }

            UpdatePoints(controlMesh, vertices);
        }

        public void UpdatePoints(ControlMesh controlMesh, Vector3[] vertices = null)
        {
            if(controlMesh == null || controlMesh.Vertices == null || controlMesh.Edges == null || controlMesh.Polygons == null || polygonPointIndices == null)
            {
                return;
            }

            if(vertices == null)
            {
                vertices = controlMesh.Vertices;
            }

            if(!brushTransform)
            {
                return;
            }

            int pointCount = vertices.Length;

            if(worldPoints.Length != pointCount)
            {
                AllocatePoints(pointCount);
            }

            int edgeCount = controlMesh.Edges.Length;

            if(edges.Length != edgeCount)
            {
                AllocateEdges(edgeCount);
            }

            int polygonCount = controlMesh.Polygons.Length;

            if(polygonControlId.Length != polygonCount)
            {
                AllocatePolygons(polygonCount);
            }

            Matrix4x4 localToWorldMatrix = brushTransform.localToWorldMatrix;

            for(int p = 0; p < pointCount; p++)
            {
                Vector3 worldPoint = localToWorldMatrix.MultiplyPoint(vertices[p]);
                worldPoints[p] = worldPoint;
            }

            float brushTotalLength = 0.0f;
            brushCenter = MathConstants.zeroVector3;

            for(int p = 0; p < polygonCount; p++)
            {
                Vector3 localCenterPoint = MathConstants.zeroVector3;
                float   totalLength      = 0.0f;
                Polygon polygon          = controlMesh.Polygons[p];

                if(polygon == null)
                {
                    continue;
                }

                int[] edgeIndices = polygon.EdgeIndices;

                if(edgeIndices == null || edgeIndices.Length == 0)
                {
                    continue;
                }

                int halfEdgeIndex0 = edgeIndices[edgeIndices.Length - 1];

                if(halfEdgeIndex0 < 0 || halfEdgeIndex0 >= controlMesh.Edges.Length)
                {
                    continue;
                }

                short vertexIndex0 = controlMesh.Edges[halfEdgeIndex0].VertexIndex;

                if(vertexIndex0 < 0 || vertexIndex0 >= vertices.Length)
                {
                    continue;
                }

                Vector3 vertex0 = vertices[vertexIndex0];

                if(polygonPointIndices[p] == null || polygonPointIndices[p].Length != edgeIndices.Length)
                {
                    polygonPointIndices[p] = new int[edgeIndices.Length];
                }

                int[] newPointIndices = polygonPointIndices[p];

                for(int i = 0; i < edgeIndices.Length; i++)
                {
                    int halfEdgeIndex1 = edgeIndices[i];

                    if(halfEdgeIndex1 < 0 || halfEdgeIndex1 >= controlMesh.Edges.Length)
                    {
                        continue;
                    }

                    short vertexIndex1 = controlMesh.Edges[halfEdgeIndex1].VertexIndex;

                    if(vertexIndex1 < 0 || vertexIndex1 >= vertices.Length)
                    {
                        continue;
                    }

                    Vector3 vertex1 = vertices[vertexIndex1];
                    newPointIndices[i] = vertexIndex1;

                    float length = (vertex1 - vertex0).sqrMagnitude;
                    localCenterPoint += (vertex1 + vertex0) * 0.5f * length;
                    totalLength      += length;
                    brushTotalLength += length;

                    vertex0 = vertex1;
                }

                Vector3 worldCenterPoint = Mathf.Abs(totalLength) < MathConstants.EqualityEpsilon ? localToWorldMatrix.MultiplyPoint(vertex0) : localToWorldMatrix.MultiplyPoint(localCenterPoint / totalLength);

                brushCenter            += localCenterPoint;
                polygonCenterPoints[p] =  worldCenterPoint;
                polygonCenterPlanes[p] =  GeometryUtility.CalcPolygonPlane(controlMesh, (short)p);
            }

            if(Mathf.Abs(brushTotalLength) >= MathConstants.EqualityEpsilon)
            {
                brushCenter /= brushTotalLength;
            }

            brushCenter = localToWorldMatrix.MultiplyPoint(brushCenter);
        }

        public static void GetHandleSizes(Camera cam, ref float[] sizes, Vector3[] positions)
        {
            if(sizes.Length != positions.Length)
            {
                sizes = new float[positions.Length];
            }

            if(!cam)
            {
                for(int p = 0; p < sizes.Length; p++)
                {
                    sizes[p] = 20.0f;
                }

                return;
            }

            const float kHandleSize    = 80.0f / 20.0f;
            const float kHandleMaxSize = 0.0001f / 20.0f;

            Transform tr          = cam.transform;
            Vector3   camPos      = tr.position;
            Vector3   camForward  = tr.forward;
            Vector3   camRight    = tr.right;
            Matrix4x4 worldToClip = cam.projectionMatrix * cam.worldToCameraMatrix;
            float     width       = cam.pixelWidth * 0.5f;
            float     height      = cam.pixelHeight * 0.5f;

            float m00 = worldToClip.m00 * width;
            float m01 = worldToClip.m01 * width;
            float m02 = worldToClip.m02 * width;

            float m10 = worldToClip.m10 * height;
            float m11 = worldToClip.m11 * height;
            float m12 = worldToClip.m12 * height;

            float m30 = worldToClip.m30;
            float m31 = worldToClip.m31;
            float m32 = worldToClip.m32;
            float m33 = worldToClip.m33;

            Vector3 offset = camPos + camRight;

            for(int p = 0; p < positions.Length; p++)
            {
                float   distance = Vector3.Dot(positions[p] - camPos, camForward);
                Vector3 p0       = camForward * distance;
                Vector3 p2       = offset + p0;

                float w2  = m30 * p2.x + m31 * p2.y + m32 * p2.z + m33;
                float iw2 = -1.0f / w2;

                float ax = camRight.x * iw2;
                float ay = camRight.y * iw2;
                float az = camRight.z * iw2;

                float clipPointX = m00 * ax + m01 * ay + m02 * az;
                float clipPointY = m10 * ax + m11 * ay + m12 * az;

                float screenDist = Mathf.Sqrt(clipPointX * clipPointX + clipPointY * clipPointY);

                sizes[p] = kHandleSize / Mathf.Max(screenDist, kHandleMaxSize) * EditorGUIUtility.pixelsPerPoint;
            }
        }

        public void UpdateHandles(Camera camera, ControlMesh controlMesh)
        {
            if(controlMesh == null || controlMesh.Vertices == null || controlMesh.Edges == null || controlMesh.Polygons == null || polygonPointIndices == null)
            {
                return;
            }

            SceneMeshState cameraState = GetCameraState(camera, true);

            Vector3 cameraPosition = camera.transform.position;
            bool    cameraOrtho    = camera.orthographic;

            GetHandleSizes(camera, ref cameraState.polygonCenterPointSizes, polygonCenterPoints);
            GetHandleSizes(camera, ref cameraState.worldPointSizes, worldPoints);

            if(cameraState.worldPointBackfaced.Length != worldPoints.Length)
            {
                cameraState.worldPointBackfaced = new bool[worldPoints.Length];
            }

            for(int p = 0; p < cameraState.worldPointBackfaced.Length; p++)
            {
                cameraState.worldPointBackfaced[p] = true;
            }

            for(int p = 0; p < polygonCenterPoints.Length; p++)
            {
                float   handleSize = cameraState.polygonCenterPointSizes[p];
                Vector3 delta1     = (polygonCenterPoints[p] - brushCenter).normalized;
                Vector3 delta2     = (polygonCenterPoints[p] - cameraPosition).normalized;
                float   dot        = Vector3.Dot(delta1, delta2);

                bool polygonBackfaced = Mathf.Abs(dot) > 1 - MathConstants.AngleEpsilon;

                if(cameraOrtho && polygonBackfaced)
                {
                    handleSize = 0;
                }
                else if(dot > 0)
                {
                    handleSize *= GUIConstants.BACK_HANDLE_SCALE;
                }
                else
                {
                    handleSize *= GUIConstants.HANDLE_SCALE;

                    if(polygonPointIndices != null && p < polygonPointIndices.Length)
                    {
                        int[] indices = polygonPointIndices[p];

                        if(indices != null)
                        {
                            for(int i = 0; i < indices.Length; i++)
                            {
                                if(indices[i] >= cameraState.worldPointBackfaced.Length)
                                {
                                    polygonPointIndices[p] = null;

                                    break;
                                }

                                cameraState.worldPointBackfaced[indices[i]] = false;
                            }
                        }
                    }
                }

                cameraState.polygonCenterPointSizes[p] = handleSize;
            }

            for(int p = 0; p < cameraState.worldPointSizes.Length; p++)
            {
                float handleSize = cameraState.worldPointSizes[p];

                if(cameraState.worldPointBackfaced[p])
                {
                    handleSize *= GUIConstants.BACK_HANDLE_SCALE;
                }
                else
                {
                    handleSize *= GUIConstants.HANDLE_SCALE;
                }

                cameraState.worldPointSizes[p] = handleSize;
            }
        }

        public bool UpdateColors(Camera camera, ControlMesh controlMesh)
        {
            if(controlMesh == null)
            {
                return false;
            }

            SceneMeshState cameraState = GetCameraState(camera, false);

            if(cameraState == null)
            {
                return false;
            }

            bool valid = controlMesh.Valid;

            Vector3 cameraPosition = camera.transform.position;
            bool    cameraOrtho    = camera.orthographic;

            int polygonCount = polygonCenterPoints.Length;

            for(int j = 0, p = 0; p < polygonCount; p++, j += 2)
            {
                int   state = (int)selection.polygons[p];
                Color color1, color2;

                if(valid)
                {
                    color1 = ColorSettings.MeshEdgeOutline;
                    color2 = ColorSettings.PolygonInnerStateColor[state];
                }
                else
                {
                    color1 = ColorSettings.outerInvalidColor;
                    color2 = ColorSettings.innerInvalidColor;
                }

                Vector3 delta1 = (polygonCenterPoints[p] - brushCenter).normalized;
                Vector3 delta2 = (polygonCenterPoints[p] - cameraPosition).normalized;
                float   dot    = Vector3.Dot(delta1, delta2);

                if(!cameraOrtho || Mathf.Abs(dot) <= 1 - MathConstants.AngleEpsilon)
                {
                    color1.a *= GUIConstants.BACKFACE_TRANSPARENCY;
                    color2.a *= GUIConstants.BACKFACE_TRANSPARENCY;
                }

                if(state == (int)SelectState.None)
                {
                    polygonColors[p] = Color.clear;
                }
                else
                {
                    Color polygonColor = ColorSettings.PointInnerStateColor[state];
                    polygonColor.a   *= 0.3f;
                    polygonColors[p] =  polygonColor;
                }

                polygonCenterColors[j + 0] = color1;
                polygonCenterColors[j + 1] = color2;
            }

            int edgeCount = edges.Length;

            for(int j = 0, e = 0; j < edgeCount; e++, j += 2)
            {
                int state = (int)selection.edges[e];

                if(valid)
                {
                    Color color = ColorSettings.PointInnerStateColor[state];
                    edgeColors[e] = color;
                }
                else
                {
                    edgeColors[e] = ColorSettings.InvalidInnerStateColor[state];
                }
            }

            if(cameraState.worldPointColors.Length != selection.points.Length * 2)
            {
                cameraState.worldPointColors = new Color[selection.points.Length * 2];
            }

            for(int j = 0, p = 0; p < selection.points.Length; p++, j += 2)
            {
                int   state = (int)selection.points[p];
                Color color1, color2;

                if(valid)
                {
                    color1 = ColorSettings.MeshEdgeOutline;
                    color2 = ColorSettings.PointInnerStateColor[state];
                }
                else
                {
                    color1 = ColorSettings.MeshEdgeOutline;
                    color2 = ColorSettings.InvalidInnerStateColor[state];
                }

                if(cameraState.worldPointBackfaced[p])
                {
                    color1.a *= GUIConstants.BACKFACE_TRANSPARENCY;
                    color2.a *= GUIConstants.BACKFACE_TRANSPARENCY;
                }
                else
                {
                    color2.a = 1.0f;
                }

                cameraState.worldPointColors[j + 0] = color1;
                cameraState.worldPointColors[j + 1] = color2;
            }

            return true;
        }

        public bool HaveSelection
        {
            get
            {
                for(int p = 0; p < selection.points.Length; p++)
                {
                    if((selection.points[p] & SelectState.Selected) == SelectState.Selected)
                    {
                        return true;
                    }
                }

                for(int e = 0; e < selection.edges.Length; e++)
                {
                    if((selection.edges[e] & SelectState.Selected) == SelectState.Selected)
                    {
                        return true;
                    }
                }

                for(int e = 0; e < selection.polygons.Length; e++)
                {
                    if((selection.polygons[e] & SelectState.Selected) == SelectState.Selected)
                    {
                        return true;
                    }
                }

                return false;
            }
        }

        public bool HaveEdgeSelection
        {
            get
            {
                for(int e = 0; e < selection.edges.Length; e++)
                {
                    if(IsEdgeSelectedIndirectly(e))
                    {
                        return true;
                    }
                }

                return false;
            }
        }

        public bool SelectAll()
        {
            bool hadSelection = false;

            for(int p = 0; p < selection.points.Length; p++)
            {
                hadSelection        =  hadSelection || (selection.points[p] & SelectState.Selected) != SelectState.Selected;
                selection.points[p] |= SelectState.Selected;
            }

            for(int e = 0; e < selection.edges.Length; e++)
            {
                hadSelection       =  hadSelection || (selection.edges[e] & SelectState.Selected) != SelectState.Selected;
                selection.edges[e] |= SelectState.Selected;
            }

            for(int p = 0; p < selection.polygons.Length; p++)
            {
                hadSelection          =  hadSelection || (selection.polygons[p] & SelectState.Selected) != SelectState.Selected;
                selection.polygons[p] |= SelectState.Selected;
            }

            return hadSelection;
        }

        public bool DeSelectAll()
        {
            bool hadSelection = false;

            for(int p = 0; p < selection.points.Length; p++)
            {
                hadSelection        =  hadSelection || (selection.points[p] & SelectState.Selected) == SelectState.Selected;
                selection.points[p] &= ~SelectState.Selected;
            }

            for(int e = 0; e < selection.edges.Length; e++)
            {
                hadSelection       =  hadSelection || (selection.edges[e] & SelectState.Selected) == SelectState.Selected;
                selection.edges[e] &= ~SelectState.Selected;
            }

            for(int p = 0; p < selection.polygons.Length; p++)
            {
                hadSelection          =  hadSelection || (selection.polygons[p] & SelectState.Selected) == SelectState.Selected;
                selection.polygons[p] &= ~SelectState.Selected;
            }

            return hadSelection;
        }

        public void UnHoverAll()
        {
            for(int p = 0; p < selection.points.Length; p++)
            {
                selection.points[p] &= ~SelectState.Hovering;
            }

            for(int e = 0; e < selection.edges.Length; e++)
            {
                selection.edges[e] &= ~SelectState.Hovering;
            }

            for(int p = 0; p < selection.polygons.Length; p++)
            {
                selection.polygons[p] &= ~SelectState.Hovering;
            }
        }

        public bool IsPointSelected(int pointIndex)
        {
            SelectState newState = selection.points[pointIndex];

            return (newState & SelectState.Selected) == SelectState.Selected;
        }

        public bool IsEdgeSelected(int edgeIndex)
        {
            SelectState newState = selection.edges[edgeIndex];

            return (newState & SelectState.Selected) == SelectState.Selected;
        }

        public bool IsPolygonSelected(int polygonIndex)
        {
            SelectState newState = selection.polygons[polygonIndex];

            return (newState & SelectState.Selected) == SelectState.Selected;
        }

        public bool IsPointSelectedIndirectly(int pointIndex)
        {
            if((selection.points[pointIndex] & SelectState.Selected) == SelectState.Selected)
            {
                return true;
            }

            for(int p = 0; p < polygonPointIndices.Length; p++)
            {
                if((selection.polygons[p] & SelectState.Selected) != SelectState.Selected)
                {
                    continue;
                }

                int[] indices = polygonPointIndices[p];

                for(int i = 0; i < indices.Length; i++)
                {
                    if(indices[i] == pointIndex)
                    {
                        return true;
                    }
                }
            }

            for(int e = 0, e2 = 0; e < selection.edges.Length; e++, e2 += 2)
            {
                if((selection.edges[e] & SelectState.Selected) != SelectState.Selected)
                {
                    continue;
                }

                int pointIndex1 = edges[e2 + 0];
                int pointIndex2 = edges[e2 + 1];

                if(pointIndex1 == pointIndex || pointIndex2 == pointIndex)
                {
                    return true;
                }
            }

            return false;
        }

        public bool IsEdgeSelectedIndirectly(int edgeIndex)
        {
            if((selection.edges[edgeIndex] & SelectState.Selected) == SelectState.Selected)
            {
                return true;
            }

            int pointIndex1 = edges[edgeIndex * 2 + 0];
            int pointIndex2 = edges[edgeIndex * 2 + 1];

            if((selection.points[pointIndex1] & SelectState.Selected) == SelectState.Selected || (selection.points[pointIndex2] & SelectState.Selected) == SelectState.Selected)
            {
                return true;
            }

            int polygonIndex1 = edgeSurfaces[edgeIndex * 2 + 0];
            int polygonIndex2 = edgeSurfaces[edgeIndex * 2 + 1];

            return (selection.polygons[polygonIndex1] & SelectState.Selected) == SelectState.Selected || (selection.polygons[polygonIndex2] & SelectState.Selected) == SelectState.Selected;
        }

        public bool IsPolygonSelectedIndirectly(int polygonIndex)
        {
            return (selection.polygons[polygonIndex] & SelectState.Selected) == SelectState.Selected;
        }

        public int PolygonCount
        {
            get => selection.polygons.Length;
        }

        private static bool Select(ref SelectState state, SelectionType selectionType, bool onlyOnHover = true)
        {
            SelectState oldState = state;

            if(onlyOnHover && (oldState & SelectState.Hovering) != SelectState.Hovering)
            {
                return false;
            }

            SelectState newState = oldState;

            switch(selectionType)
            {
                case SelectionType.Subtractive: newState &= ~SelectState.Selected; break;
                case SelectionType.Toggle:      newState ^= SelectState.Selected; break;
                case SelectionType.Replace:
                case SelectionType.Additive: newState |= SelectState.Selected; break;

                default:
                    throw new ArgumentOutOfRangeException(nameof(selectionType), selectionType, null);
            }

            if(oldState == newState)
            {
                return false;
            }

            state = newState;

            return true;
        }

        public bool SelectPoint(int pointIndex, SelectionType selectionType, bool onlyOnHover = true)
        {
            return pointIndex < selection.points.Length && Select(ref selection.points[pointIndex], selectionType, onlyOnHover);
        }

        public bool SelectEdge(int edgeIndex, SelectionType selectionType, bool onlyOnHover = true)
        {
            return Select(ref selection.edges[edgeIndex], selectionType, onlyOnHover);
        }

        public bool SelectPolygon(int polygonIndex, SelectionType selectionType, bool onlyOnHover = true)
        {
            return polygonPointIndices[polygonIndex] != null && Select(ref selection.polygons[polygonIndex], selectionType, onlyOnHover);
        }

        public static bool DeselectAll(ControlMeshState[] controlMeshStates)
        {
            bool hadSelection = false;

            for(int t = 0; t < controlMeshStates.Length; t++)
            {
                hadSelection = controlMeshStates[t].DeSelectAll() || hadSelection;
            }

            return hadSelection;
        }

        public static bool SelectAll(ControlMeshState[] controlMeshStates)
        {
            bool hadSelection = false;

            for(int t = 0; t < controlMeshStates.Length; t++)
            {
                hadSelection = controlMeshStates[t].SelectAll() || hadSelection;
            }

            return hadSelection;
        }

        public static void SelectFrustumPoints(ControlMeshState[] controlMeshStates, PointSelection[] selectedPoints, SelectionType selectionType, bool onlyOnHover = true)
        {
            if(selectionType == SelectionType.Replace)
            {
                for(int t = 0; t < controlMeshStates.Length; t++)
                {
                    ControlMeshState meshState = controlMeshStates[t];

                    if(meshState == null)
                    {
                        return;
                    }

                    meshState.DeSelectAll();
                }
            }

            int maxPoints = 0;

            for(int t = 0; t < controlMeshStates.Length; t++)
            {
                ControlMeshState meshState = controlMeshStates[t];

                if(meshState == null)
                {
                    return;
                }

                maxPoints = Mathf.Max(maxPoints, meshState.worldPoints.Length);
            }

            byte[] foundPoints = new byte[maxPoints];

            for(int t = 0; t < controlMeshStates.Length; t++)
            {
                ControlMeshState meshState = controlMeshStates[t];

                if(meshState == null)
                {
                    return;
                }

                Array.Clear(foundPoints, 0, foundPoints.Length);

                for(int i = 0; i < selectedPoints.Length; i++)
                {
                    int brushNodeID = selectedPoints[i].brushNodeID;

                    if(brushNodeID != t)
                    {
                        continue;
                    }

                    int pointIndex = selectedPoints[i].pointIndex;
                    foundPoints[pointIndex] = 1;

                    if(CSGSettings.SelectionVertex)
                    {
                        meshState.SelectPoint(pointIndex, selectionType, onlyOnHover);
                    }
                }

                if(CSGSettings.SelectionEdge)
                {
                    for(int e = 0; e < meshState.edges.Length; e += 2)
                    {
                        int pointIndex1 = meshState.edges[e + 0];
                        int pointIndex2 = meshState.edges[e + 1];

                        if(foundPoints[pointIndex1] == 1 && foundPoints[pointIndex2] == 1)
                        {
                            meshState.SelectEdge(e / 2, selectionType, onlyOnHover);
                        }
                    }
                }

                if(CSGSettings.SelectionSurface)
                {
                    for(int p = 0; p < meshState.PolygonCount; p++)
                    {
                        int[] indices = meshState.polygonPointIndices[p];

                        for(int i = 0; i < indices.Length; i++)
                        {
                            if(foundPoints[indices[i]] != 1)
                            {
                                goto SkipPolygon;
                            }
                        }

                        meshState.SelectPolygon(p, selectionType, onlyOnHover);
                        SkipPolygon: ;
                    }
                }
            }
        }
    }
}
