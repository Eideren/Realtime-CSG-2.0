#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using RealtimeCSG.Components;
using RealtimeCSG.Foundation;
using RealtimeCSG.Legacy;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace RealtimeCSG
{
    /// <summary>
    /// This class defines an intersection into a specific surface of a brush
    /// </summary>
    [Serializable, StructLayout(LayoutKind.Sequential, Pack = 4)]
    public struct CSGSurfaceIntersection
    {
        public Plane localPlane;
        public Plane modelPlane;
        public Plane worldPlane;

        public Vector3 worldIntersection;
        public Vector2 surfaceIntersection;

        public float distance;

        public static readonly CSGSurfaceIntersection None = new CSGSurfaceIntersection
        {
            localPlane          = new Plane(Vector3.zero, 0)
          , modelPlane          = new Plane(Vector3.zero, 0)
          , worldPlane          = new Plane(Vector3.zero, 0)
          , worldIntersection   = Vector3.zero
          , surfaceIntersection = Vector2.zero
          , distance            = float.PositiveInfinity
        };
    }

    /// <summary>
    /// This class defines an intersection into a specific brush
    /// </summary>
    [Serializable, StructLayout(LayoutKind.Sequential, Pack = 4)]
    public struct CSGTreeBrushIntersection
    {
        public CSGTree tree;
        public CSGTreeBrush brush;
        public Int32 surfaceID;
        public Int32 brushUserID;

        public CSGSurfaceIntersection intersection;

        public static readonly CSGTreeBrushIntersection None = new CSGTreeBrushIntersection
        {
            tree         = (CSGTree)CSGTreeNode.InvalidNode
          , brush        = (CSGTreeBrush)CSGTreeNode.InvalidNode
          , brushUserID  = 0
          , surfaceID    = -1
          , intersection = CSGSurfaceIntersection.None
        };
    }

    internal static class NativeMethodBindings
    {
        private const string NATIVE_PLUGIN_NAME = CSGManager.NATIVE_PLUGIN_NAME;

#region Functionality to allow C# methods to be called from C++

        public delegate void StringLog([MarshalAs(UnmanagedType.LPStr)] string text, int uniqueObjectID);

        [return: MarshalAs(UnmanagedType.LPStr)]
        public delegate string ReturnStringMethod(int uniqueObjectID);

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack = 4)]
        private struct UnityMethods
        {
            public StringLog DebugLog;
            public StringLog DebugLogError;
            public StringLog DebugLogWarning;
            public ReturnStringMethod NameForUserID;
        }

        [DllImport(NATIVE_PLUGIN_NAME, CallingConvention = CallingConvention.Cdecl)]
        private static extern void RegisterMethods([In] ref UnityMethods unityMethods);

        [DllImport(NATIVE_PLUGIN_NAME, CallingConvention = CallingConvention.Cdecl)]
        private static extern void ClearMethods();

        public static void RegisterUnityMethods()
        {
            UnityMethods unityMethods;

            unityMethods.DebugLog = delegate(string message, int uniqueObjectID)
            {
                Debug.Log(message, uniqueObjectID != 0 ? EditorUtility.EntityIdToObject(uniqueObjectID) : null);
            };

            unityMethods.DebugLogError = delegate(string message, int uniqueObjectID)
            {
                Debug.LogError(
                    message, uniqueObjectID != 0 ? EditorUtility.EntityIdToObject(uniqueObjectID) : null
                );
            };

            unityMethods.DebugLogWarning = delegate(string message, int uniqueObjectID)
            {
                Debug.LogWarning(
                    message, uniqueObjectID != 0 ? EditorUtility.EntityIdToObject(uniqueObjectID) : null
                );
            };

            unityMethods.NameForUserID = delegate(int uniqueObjectID)
            {
                Object obj = uniqueObjectID != 0 ? EditorUtility.EntityIdToObject(uniqueObjectID) : null;

                return obj == null ? "<unknown>" : obj.name;
            };

            RegisterMethods(ref unityMethods);
        }

        public static void ClearUnityMethods()
        {
            ClearMethods();
            ClearAllNodes();
        }

#endregion

#region C++ Registration/Update functions

#region Diagnostics

        [DllImport(NATIVE_PLUGIN_NAME, CallingConvention = CallingConvention.Cdecl)]
        public static extern void LogDiagnostics();

        [DllImport(NATIVE_PLUGIN_NAME, CallingConvention = CallingConvention.Cdecl)]
        public static extern void RebuildAll();

#endregion

#region Scene event functions

        [DllImport(NATIVE_PLUGIN_NAME, CallingConvention = CallingConvention.Cdecl)]
        private static extern void ClearAllNodes();

#endregion

#region Polygon Convex Decomposition

        [DllImport(NATIVE_PLUGIN_NAME, CallingConvention = CallingConvention.Cdecl)]
        private static extern bool DecomposeStart(
            int vertexCount,
            [In] IntPtr vertices,
            out int polygonCount
        );

        [DllImport(NATIVE_PLUGIN_NAME, CallingConvention = CallingConvention.Cdecl)]
        private static extern bool DecomposeGetSizes(
            int polygonCount,
            [Out] IntPtr polygonSizes
        );

        [DllImport(NATIVE_PLUGIN_NAME, CallingConvention = CallingConvention.Cdecl)]
        private static extern bool DecomposeGetPolygon(
            int polygonIndex,
            int vertexSize,
            [Out] IntPtr vertices
        );

        private static List<List<Vector2>> ConvexPartition(Vector2[] points)
        {
            GCHandle pointsHandle = GCHandle.Alloc(points, GCHandleType.Pinned);
            IntPtr   pointsPtr    = pointsHandle.AddrOfPinnedObject();
            bool     result       = DecomposeStart(points.Length, pointsPtr, out int polygonCount);
            pointsHandle.Free();

            if(!result)
            {
                return null;
            }

            if(polygonCount == 0)
            {
                return null;
            }

            int[]    polygonSizes       = new int[polygonCount];
            GCHandle polygonSizesHandle = GCHandle.Alloc(polygonSizes, GCHandleType.Pinned);
            IntPtr   polygonSizesPtr    = polygonSizesHandle.AddrOfPinnedObject();
            result = DecomposeGetSizes(polygonCount, polygonSizesPtr);
            polygonSizesHandle.Free();

            if(!result)
            {
                return null;
            }

            List<List<Vector2>> polygons = new List<List<Vector2>>();

            for(int i = 0; i < polygonCount; i++)
            {
                int       vertexCount    = polygonSizes[i];
                Vector2[] vertices       = new Vector2[vertexCount];
                GCHandle  verticesHandle = GCHandle.Alloc(vertices, GCHandleType.Pinned);
                IntPtr    verticesPtr    = verticesHandle.AddrOfPinnedObject();
                result = DecomposeGetPolygon(i, vertexCount, verticesPtr);
                verticesHandle.Free();

                if(!result)
                {
                    return null;
                }

                polygons.Add(new List<Vector2>(vertices));
            }

            return polygons;
        }

#endregion

        [DllImport(NATIVE_PLUGIN_NAME, CallingConvention = CallingConvention.Cdecl)]
        private static extern bool SetDirty(int nodeID);

#region Models C++ functions

        [DllImport(NATIVE_PLUGIN_NAME, CallingConvention = CallingConvention.Cdecl)]
        private static extern bool GenerateTree(
            int userID,
            out int generatedTreeNodeID
        );

        [DllImport(NATIVE_PLUGIN_NAME, CallingConvention = CallingConvention.Cdecl)]
        private static extern bool SetTreeEnabled(
            int modelNodeID,
            bool isEnabled
        );

        [DllImport(NATIVE_PLUGIN_NAME, CallingConvention = CallingConvention.Cdecl)]
        private static extern bool RayCastMultiGet(
            int objectCount,
            [Out] IntPtr outputBrushIntersection
        );

        private static int m_PrevIntersectionCount = -1;
        private static CSGTreeBrushIntersection[] m_OutputIntersections;

        [DllImport(NATIVE_PLUGIN_NAME, CallingConvention = CallingConvention.Cdecl)]
        private static extern int RayCastIntoTreeMultiCount(
            int modelNodeID,
            [In] ref Vector3 worldRayStart,
            [In] ref Vector3 worldRayEnd,
            [In] ref Matrix4x4 modelLocalToWorldMatrix,
            int inFilterLayerParameter0,
            bool ignoreInvisiblePolygons,
            [In] IntPtr ignoreNodeIDs,
            int ignoreNodeIDCount
        );

        private static bool RayCastIntoModelMulti(
            CSGModel model,
            Vector3 worldRayStart,
            Vector3 worldRayEnd,
            bool ignoreInvisiblePolygons,
            out LegacyBrushIntersection[] intersections,
            CSGBrush[] ignoreBrushes = null
        )
        {
            int visibleLayers = Tools.visibleLayers;

            if(!ModelTraits.IsModelSelectable(model))
            {
                intersections = null;

                return false;
            }

            IntPtr   ignoreNodeIDsPtr    = IntPtr.Zero;
            GCHandle ignoreNodeIDsHandle = new GCHandle();

            if(ignoreBrushes != null)
            {
                List<int> ignoreNodeIDsList = new List<int>(ignoreBrushes.Length);

                for(int i = ignoreBrushes.Length - 1; i >= 0; i--)
                {
                    if(!ignoreBrushes[i])
                    {
                        continue;
                    }

                    if(ignoreBrushes[i].brushNodeID == CSGNode.InvalidNodeID)
                    {
                        continue;
                    }

                    ignoreNodeIDsList.Add(ignoreBrushes[i].brushNodeID);
                }

                int[] ignoreNodeIDs = ignoreNodeIDsList.ToArray();
                ignoreNodeIDsHandle = GCHandle.Alloc(ignoreNodeIDs, GCHandleType.Pinned);
                ignoreNodeIDsPtr    = ignoreNodeIDsHandle.AddrOfPinnedObject();
            }

            Matrix4x4 modelLocalToWorldMatrix = model.transform.localToWorldMatrix;

            int intersectionCount = RayCastIntoTreeMultiCount(
                model.modelNodeID,
                ref worldRayStart,
                ref worldRayEnd,
                ref modelLocalToWorldMatrix,
                0,
                ignoreInvisiblePolygons,
                ignoreNodeIDsPtr,
                ignoreBrushes == null ? 0 : ignoreBrushes.Length
            );

            if(ignoreNodeIDsHandle.IsAllocated)
            {
                ignoreNodeIDsHandle.Free();
            }

            if(intersectionCount == 0)
            {
                intersections = null;

                return false;
            }

            if(m_PrevIntersectionCount < intersectionCount)
            {
                m_OutputIntersections   = new CSGTreeBrushIntersection[intersectionCount];
                m_PrevIntersectionCount = intersectionCount;
            }

            GCHandle outputIntersectionsHandle = GCHandle.Alloc(m_OutputIntersections, GCHandleType.Pinned);
            IntPtr   outputIntersectionsPtr    = outputIntersectionsHandle.AddrOfPinnedObject();

            bool result = RayCastMultiGet(
                intersectionCount,
                outputIntersectionsPtr
            );

            outputIntersectionsHandle.Free();

            if(!result)
            {
                intersections = null;

                return false;
            }

            List<LegacyBrushIntersection> intersectionList = new List<LegacyBrushIntersection>();

            for(int i = 0; i < intersectionCount; i++)
            {
                Object        obj           = EditorUtility.EntityIdToObject(m_OutputIntersections[i].brushUserID);
                MonoBehaviour monoBehaviour = obj as MonoBehaviour;

                if(monoBehaviour == null || !monoBehaviour)
                {
                    continue;
                }

                GameObject gameObject = monoBehaviour.gameObject;

                if(((1 << gameObject.layer) & visibleLayers) == 0)
                {
                    continue;
                }

                float distance = (m_OutputIntersections[i].intersection.worldIntersection - worldRayStart).magnitude;

                intersectionList.Add(
                    new LegacyBrushIntersection
                    {
                        gameObject          = gameObject
                      , model               = model
                      , brushNodeID         = m_OutputIntersections[i].brush.NodeID
                      , surfaceIndex        = m_OutputIntersections[i].surfaceID
                      , localPlane          = new CSGPlane(m_OutputIntersections[i].intersection.localPlane)
                      , modelPlane          = new CSGPlane(m_OutputIntersections[i].intersection.modelPlane)
                      , worldPlane          = new CSGPlane(m_OutputIntersections[i].intersection.worldPlane)
                      , worldIntersection   = m_OutputIntersections[i].intersection.worldIntersection
                      , surfaceIntersection = m_OutputIntersections[i].intersection.surfaceIntersection
                      , distance            = distance
                    }
                );
            }

            if(intersectionList.Count == 0)
            {
                intersections = null;

                return true;
            }

            intersections = intersectionList.ToArray();

            return true;
        }

        private static readonly List<LegacyBrushIntersection> intersectionList = new List<LegacyBrushIntersection>();

        private static bool RayCastMulti(
            int modelCount,
            CSGModel[] models,
            Vector3 rayStart,
            Vector3 rayEnd,
            bool ignoreInvisiblePolygons,
            out LegacyBrushIntersection[] intersections,
            CSGBrush[] ignoreBrushes = null
        )
        {
            if(modelCount == 0)
            {
                intersections = null;

                return false;
            }

            IntPtr   ignoreNodeIDsPtr    = IntPtr.Zero;
            GCHandle ignoreNodeIDsHandle = new GCHandle();

            if(ignoreBrushes != null)
            {
                List<int> ignoreNodeIDsList = new List<int>(ignoreBrushes.Length);

                for(int i = ignoreBrushes.Length - 1; i >= 0; i--)
                {
                    if(!ignoreBrushes[i])
                    {
                        continue;
                    }

                    if(ignoreBrushes[i].brushNodeID == CSGNode.InvalidNodeID)
                    {
                        continue;
                    }

                    ignoreNodeIDsList.Add(ignoreBrushes[i].brushNodeID);
                }

                int[] ignoreNodeIDs = ignoreNodeIDsList.ToArray();
                ignoreNodeIDsHandle = GCHandle.Alloc(ignoreNodeIDs, GCHandleType.Pinned);
                ignoreNodeIDsPtr    = ignoreNodeIDsHandle.AddrOfPinnedObject();
            }

            GCHandle outputIntersectionsHandle = GCHandle.Alloc(m_OutputIntersections, GCHandleType.Pinned);

            int visibleLayers = Tools.visibleLayers;
            intersectionList.Clear();

            for(int m = 0; m < modelCount; m++)
            {
                CSGModel  model                   = models[m];
                Matrix4x4 modelLocalToWorldMatrix = model.transform.localToWorldMatrix;

                int intersectionCount = RayCastIntoTreeMultiCount(
                    model.modelNodeID,
                    ref rayStart,
                    ref rayEnd,
                    ref modelLocalToWorldMatrix,
                    0,
                    ignoreInvisiblePolygons,
                    ignoreNodeIDsPtr,
                    ignoreBrushes == null ? 0 : ignoreBrushes.Length
                );

                if(intersectionCount == 0)
                {
                    continue;
                }

                if(m_PrevIntersectionCount < intersectionCount)
                {
                    if(outputIntersectionsHandle.IsAllocated)
                    {
                        outputIntersectionsHandle.Free();
                    }

                    m_OutputIntersections     = new CSGTreeBrushIntersection[intersectionCount];
                    m_PrevIntersectionCount   = intersectionCount;
                    outputIntersectionsHandle = GCHandle.Alloc(m_OutputIntersections, GCHandleType.Pinned);
                }

                IntPtr outputIntersectionsPtr = outputIntersectionsHandle.AddrOfPinnedObject();

                bool result = RayCastMultiGet(
                    intersectionCount,
                    outputIntersectionsPtr
                );

                if(!result)
                {
                    continue;
                }

                for(int i = 0; i < intersectionCount; i++)
                {
                    Object        obj           = EditorUtility.EntityIdToObject(m_OutputIntersections[i].brushUserID);
                    MonoBehaviour monoBehaviour = obj as MonoBehaviour;

                    if(monoBehaviour == null || !monoBehaviour)
                    {
                        continue;
                    }

                    GameObject gameObject = monoBehaviour.gameObject;

                    if(((1 << gameObject.layer) & visibleLayers) == 0)
                    {
                        continue;
                    }

                    float distance = (m_OutputIntersections[i].intersection.worldIntersection - rayStart).magnitude;

                    intersectionList.Add(
                        new LegacyBrushIntersection
                        {
                            gameObject          = gameObject
                          , model               = model
                          , brushNodeID         = m_OutputIntersections[i].brush.NodeID
                          , surfaceIndex        = m_OutputIntersections[i].surfaceID
                          , localPlane          = new CSGPlane(m_OutputIntersections[i].intersection.localPlane)
                          , modelPlane          = new CSGPlane(m_OutputIntersections[i].intersection.modelPlane)
                          , worldPlane          = new CSGPlane(m_OutputIntersections[i].intersection.worldPlane)
                          , worldIntersection   = m_OutputIntersections[i].intersection.worldIntersection
                          , surfaceIntersection = m_OutputIntersections[i].intersection.surfaceIntersection
                          , distance            = distance
                        }
                    );
                }
            }

            if(outputIntersectionsHandle.IsAllocated)
            {
                outputIntersectionsHandle.Free();
            }

            if(ignoreNodeIDsHandle.IsAllocated)
            {
                ignoreNodeIDsHandle.Free();
            }

            if(intersectionList.Count == 0)
            {
                intersections = null;

                return false;
            }

            intersectionList.Sort((x, y) => (int)Mathf.Sign(y.distance - x.distance));

            intersections = intersectionList.ToArray();

            return true;
        }

        [DllImport(NATIVE_PLUGIN_NAME, CallingConvention = CallingConvention.Cdecl)]
        private static extern bool RayCastIntoBrush(
            int brushNodeID,
            [In] ref Vector3 rayStart,
            [In] ref Vector3 rayEnd,
            [In] ref Matrix4x4 modelLocalToWorldMatrix,
            bool ignoreInvisiblePolygons,
            out CSGTreeBrushIntersection outputBrushIntersection
        );

        private static bool RayCastIntoBrush(
            int brushNodeID,
            Vector3 rayStart,
            Vector3 rayEnd,
            Matrix4x4 modelLocalToWorldMatrix,
            out LegacyBrushIntersection intersection,
            bool ignoreInvisiblePolygons
        )
        {
            if(brushNodeID == CSGNode.InvalidNodeID || !RayCastIntoBrush(
                   brushNodeID,
                   ref rayStart,
                   ref rayEnd,
                   ref modelLocalToWorldMatrix,
                   ignoreInvisiblePolygons,
                   out CSGTreeBrushIntersection outputBrushIntersection
               ))
            {
                intersection = null;

                return false;
            }

            intersection = new LegacyBrushIntersection
            {
                gameObject          = null
              , model               = null
              , brushNodeID         = brushNodeID
              , surfaceIndex        = outputBrushIntersection.surfaceID
              , localPlane          = new CSGPlane(outputBrushIntersection.intersection.localPlane)
              , modelPlane          = new CSGPlane(outputBrushIntersection.intersection.modelPlane)
              , worldPlane          = new CSGPlane(outputBrushIntersection.intersection.worldPlane)
              , worldIntersection   = outputBrushIntersection.intersection.worldIntersection
              , surfaceIntersection = outputBrushIntersection.intersection.surfaceIntersection
            };

            return true;
        }

        [DllImport(NATIVE_PLUGIN_NAME, CallingConvention = CallingConvention.Cdecl)]
        private static extern bool RayCastIntoBrushSurface(
            int brushNodeID,
            int surfaceID,
            [In] ref Vector3 rayStart,
            [In] ref Vector3 rayEnd,
            [In] ref Matrix4x4 modelLocalToWorldMatrix,
            out CSGSurfaceIntersection outputSurfaceIntersection
        );

        private static bool RayCastIntoBrushSurface(
            int brushNodeID,
            int surfaceID,
            Vector3 rayStart,
            Vector3 rayEnd,
            Matrix4x4 modelLocalToWorldMatrix,
            out LegacySurfaceIntersection intersection
        )
        {
            if(brushNodeID == CSGNode.InvalidNodeID || !RayCastIntoBrushSurface(
                   brushNodeID,
                   surfaceID,
                   ref rayStart,
                   ref rayEnd,
                   ref modelLocalToWorldMatrix,
                   out CSGSurfaceIntersection outputSurfaceIntersection
               ))
            {
                intersection = null;

                return false;
            }

            float distance = (outputSurfaceIntersection.worldIntersection - rayStart).magnitude;

            intersection = new LegacySurfaceIntersection
            {
                localPlane          = new CSGPlane(outputSurfaceIntersection.localPlane)
              , modelPlane          = new CSGPlane(outputSurfaceIntersection.modelPlane)
              , worldPlane          = new CSGPlane(outputSurfaceIntersection.worldPlane)
              , surfaceIntersection = outputSurfaceIntersection.surfaceIntersection
              , worldIntersection   = outputSurfaceIntersection.worldIntersection
              , distance            = distance
            };

            return true;
        }

        [DllImport(NATIVE_PLUGIN_NAME, CallingConvention = CallingConvention.Cdecl)]
        private static extern int FindNodesInFrustum(
            int modelNodeID,
            int planeCount,
            [In] IntPtr planes
        );

        [DllImport(NATIVE_PLUGIN_NAME, CallingConvention = CallingConvention.Cdecl)]
        private static extern bool RetrieveUserIDsInFrustum(
            int objectIDCount,
            [Out] IntPtr objectIDs
        );

        private static bool GetItemsInFrustum(
            CSGModel model,
            Plane[] planes,
            HashSet<GameObject> gameObjects
        )
        {
            if(!ModelTraits.IsModelSelectable(model))
            {
                return false;
            }

            if(planes == null ||
               planes.Length != 6)
            {
                return false;
            }

            // TODO: should be full transformation
            Plane[] translatedPlanes = new Plane[planes.Length];

            for(int i = 0; i < planes.Length; i++)
            {
                Plane   plane       = planes[i];
                Vector3 normal      = plane.normal;
                float   d           = plane.distance;
                Vector3 translation = model.transform.position;

                d += normal.x * translation.x +
                    normal.y * translation.y +
                    normal.z * translation.z;

                translatedPlanes[i] = new Plane(normal, d);
            }

            GCHandle planesHandle = GCHandle.Alloc(translatedPlanes, GCHandleType.Pinned);
            IntPtr   planesPtr    = planesHandle.AddrOfPinnedObject();
            int      itemCount    = FindNodesInFrustum(model.modelNodeID, translatedPlanes.Length, planesPtr);
            planesHandle.Free();

            if(itemCount == 0)
            {
                return false;
            }

            int[]    ids       = new int[itemCount];
            GCHandle idsHandle = GCHandle.Alloc(ids, GCHandleType.Pinned);
            IntPtr   idsPtr    = idsHandle.AddrOfPinnedObject();
            bool     result    = RetrieveUserIDsInFrustum(ids.Length, idsPtr);
            idsHandle.Free();

            if(!result)
            {
                return false;
            }

            bool found = false;

            for(int i = ids.Length - 1; i >= 0; i--)
            {
                Object        obj        = EditorUtility.EntityIdToObject(ids[i]);
                MonoBehaviour brush      = obj as MonoBehaviour;
                GameObject    gameObject = brush != null ? brush.gameObject : null;

                if(gameObject == null)
                {
                    continue;
                }

                gameObjects.Add(gameObject);
                found = true;
            }

            return found;
        }

#endregion

        [DllImport(NATIVE_PLUGIN_NAME, CallingConvention = CallingConvention.Cdecl)]
        private static extern bool SetChildNodes(int nodeID, int childCount, [In] IntPtr childrenNodeIDs);

        private static bool SetChildNodes(int nodeID, int childCount, int[] childrenNodeIDs)
        {
            GCHandle childrenNodeIDsHandle = GCHandle.Alloc(childrenNodeIDs, GCHandleType.Pinned);
            IntPtr   childrenNodeIDsPtr    = childrenNodeIDsHandle.AddrOfPinnedObject();
            bool     result                = SetChildNodes(nodeID, childCount, childrenNodeIDsPtr);
            childrenNodeIDsHandle.Free();

            return result;
        }

        [DllImport(NATIVE_PLUGIN_NAME, CallingConvention = CallingConvention.Cdecl)]
        private static extern bool DestroyNode(int nodeID);

        [DllImport(NATIVE_PLUGIN_NAME, CallingConvention = CallingConvention.Cdecl)]
        private static extern bool DestroyNodes(
            int nodeCount,
            [In] IntPtr nodeIDs
        );

        private static bool DestroyNodes(
            int nodeCount,
            int[] nodeIDs
        )
        {
            GCHandle nodeIDsHandle = GCHandle.Alloc(nodeIDs, GCHandleType.Pinned);
            IntPtr   nodeIDsPtr    = nodeIDsHandle.AddrOfPinnedObject();

            bool result = DestroyNodes(nodeCount, nodeIDsPtr);

            nodeIDsHandle.Free();

            return result;
        }

#region Operation C++ functions

        [DllImport(NATIVE_PLUGIN_NAME, CallingConvention = CallingConvention.Cdecl)]
        private static extern bool GenerateBranch(
            int userID,
            out int generatedOperationNodeID
        );

        [DllImport(NATIVE_PLUGIN_NAME, CallingConvention = CallingConvention.Cdecl)]
        private static extern bool SetBranchOperationType(
            int operationNodeID,
            CSGOperationType operation
        );

#endregion

#region Brush C++ functions

        [DllImport(NATIVE_PLUGIN_NAME, CallingConvention = CallingConvention.Cdecl)]
        private static extern bool GenerateBrush(
            int userID,
            out int generatedBrushNodeID
        );

        [DllImport(NATIVE_PLUGIN_NAME, CallingConvention = CallingConvention.Cdecl)]
        private static extern int GetBrushMeshID(int brushNodeID);

        [DllImport(NATIVE_PLUGIN_NAME, CallingConvention = CallingConvention.Cdecl)]
        private static extern bool SetBrushMeshID(int brushNodeID, int brushMeshIndex);

        [DllImport(NATIVE_PLUGIN_NAME, CallingConvention = CallingConvention.Cdecl)]
        private static extern CSGTreeBrushFlags GetBrushFlags(int brushNodeID);

        [DllImport(NATIVE_PLUGIN_NAME, CallingConvention = CallingConvention.Cdecl)]
        private static extern bool SetBrushFlags(int brushNodeID, CSGTreeBrushFlags flags);

        [DllImport(NATIVE_PLUGIN_NAME, CallingConvention = CallingConvention.Cdecl)]
        private static extern bool SetBrushOperationType(
            int brushNodeID,
            CSGOperationType operation
        );

        [DllImport(NATIVE_PLUGIN_NAME, CallingConvention = CallingConvention.Cdecl)]
        private static extern int CreateBrushMesh(
            int userID,
            int vertexCount,
            [In] IntPtr vertices,
            int halfEdgeCount,
            [In] IntPtr halfEdges,
            int polygonCount,
            [In] IntPtr polygons
        );

        private static int CreateBrushMesh(int userID, BrushMesh brushMesh)
        {
            if(brushMesh == null)
            {
                throw new ArgumentNullException(nameof(brushMesh));
            }

            if(brushMesh.vertices == null || brushMesh.halfEdges == null || brushMesh.polygons == null
            || brushMesh.vertices.Length < 5 || brushMesh.halfEdges.Length < 16 || brushMesh.polygons.Length < 5)
            {
                return CSGNode.InvalidNodeID;
            }

            GCHandle verticesHandle  = GCHandle.Alloc(brushMesh.vertices, GCHandleType.Pinned);
            GCHandle halfEdgesHandle = GCHandle.Alloc(brushMesh.halfEdges, GCHandleType.Pinned);
            GCHandle polygonsHandle  = GCHandle.Alloc(brushMesh.polygons, GCHandleType.Pinned);
            IntPtr   verticesPtr     = verticesHandle.AddrOfPinnedObject();
            IntPtr   halfEdgesPtr    = halfEdgesHandle.AddrOfPinnedObject();
            IntPtr   polygonsPtr     = polygonsHandle.AddrOfPinnedObject();

            int result = CreateBrushMesh(
                userID,
                brushMesh.vertices.Length, verticesPtr,
                brushMesh.halfEdges.Length, halfEdgesPtr,
                brushMesh.polygons.Length, polygonsPtr
            );

            polygonsHandle.Free();
            halfEdgesHandle.Free();
            verticesHandle.Free();

            return result;
        }

        [DllImport(NATIVE_PLUGIN_NAME, CallingConvention = CallingConvention.Cdecl)]
        private static extern bool UpdateBrushMesh(
            int brushMeshIndex,
            int vertexCount,
            [In] IntPtr vertices,
            int halfEdgeCount,
            [In] IntPtr halfEdges,
            int polygonCount,
            [In] IntPtr polygons
        );

        private static bool UpdateBrushMesh(
            int brushMeshIndex,
            BrushMesh brushMesh
        )
        {
            if(brushMesh == null)
            {
                throw new ArgumentNullException(nameof(brushMesh));
            }

            if(brushMesh.vertices.Length < 5 || brushMesh.halfEdges.Length < 16 || brushMesh.polygons.Length < 5)
            {
                return false;
            }

            GCHandle verticesHandle  = GCHandle.Alloc(brushMesh.vertices, GCHandleType.Pinned);
            GCHandle halfEdgesHandle = GCHandle.Alloc(brushMesh.halfEdges, GCHandleType.Pinned);
            GCHandle polygonsHandle  = GCHandle.Alloc(brushMesh.polygons, GCHandleType.Pinned);
            IntPtr   verticesPtr     = verticesHandle.AddrOfPinnedObject();
            IntPtr   halfEdgesPtr    = halfEdgesHandle.AddrOfPinnedObject();
            IntPtr   polygonsPtr     = polygonsHandle.AddrOfPinnedObject();

            bool result = UpdateBrushMesh(
                brushMeshIndex,
                brushMesh.vertices.Length, verticesPtr,
                brushMesh.halfEdges.Length, halfEdgesPtr,
                brushMesh.polygons.Length, polygonsPtr
            );

            polygonsHandle.Free();
            halfEdgesHandle.Free();
            verticesHandle.Free();

            return result;
        }

        [DllImport(NATIVE_PLUGIN_NAME, CallingConvention = CallingConvention.Cdecl)]
        private static extern bool DestroyBrushMesh(int brushMeshIndex);

        [DllImport(NATIVE_PLUGIN_NAME, CallingConvention = CallingConvention.Cdecl)]
        public static extern bool GetBrushBounds(
            int brushNodeID,
            out AABB bounds
        );

        [DllImport(NATIVE_PLUGIN_NAME, CallingConvention = CallingConvention.Cdecl)]
        private static extern bool SetNodeLocalTransformation(
            int brushNodeID,
            [In] ref Matrix4x4 localToModelSpace
        );

        private static bool SetBrushToModelSpace(
            int brushNodeID,
            Matrix4x4 localToModelSpace
        )
        {
            return SetNodeLocalTransformation(brushNodeID, ref localToModelSpace);
        }

#endregion

#region TexGen manipulation C++ functions

        [DllImport(NATIVE_PLUGIN_NAME, CallingConvention = CallingConvention.Cdecl)]
        private static extern bool GetSurfaceMinMaxTexCoords(
            int brushNodeID,
            int surfaceID,
            Matrix4x4 modelLocalToWorldMatrix,
            out Vector2 minTextureCoordinate,
            out Vector2 maxTextureCoordinate
        );

        [DllImport(NATIVE_PLUGIN_NAME, CallingConvention = CallingConvention.Cdecl)]
        private static extern bool ConvertWorldToTextureCoord(
            int brushNodeID,
            int surfaceID,
            Matrix4x4 modelLocalToWorldMatrix,
            Vector3 worldCoordinate,
            out Vector2 textureCoordinate
        );

        [DllImport(NATIVE_PLUGIN_NAME, CallingConvention = CallingConvention.Cdecl)]
        private static extern bool ConvertTextureToWorldCoord(
            int brushNodeID,
            int surfaceID,
            float textureCoordinateU,
            float textureCoordinateV,
            // workaround for mac-osx related bug
            ref Matrix4x4 modelLocalToWorldMatrix,
            ref float worldCoordinateX,
            ref float worldCoordinateY,
            ref float worldCoordinateZ
        );

#endregion

#region C++ Mesh functions

        [DllImport(NATIVE_PLUGIN_NAME, CallingConvention = CallingConvention.Cdecl)]
        private static extern bool UpdateAllTreeMeshes();

        [DllImport(NATIVE_PLUGIN_NAME, CallingConvention = CallingConvention.Cdecl)]
        private static extern bool GenerateMeshDescriptions(
            int modelNodeID,
            int meshTypeCount,
            [In] IntPtr meshTypes,
            VertexChannelFlags vertexChannelMask,
            out int meshDescriptionCount
        );

        [DllImport(NATIVE_PLUGIN_NAME, CallingConvention = CallingConvention.Cdecl)]
        private static extern bool GetMeshDescriptions(
            int modelNodeID,
            int meshDescriptionCount,
            [Out] IntPtr meshDescriptions
        );

        private static bool GetMeshDescriptions(
            int modelNodeID,
            MeshQuery[] meshTypes,
            VertexChannelFlags vertexChannelMask,
            ref GeneratedMeshDescription[] meshDescriptions
        )
        {
            GCHandle nativeMeshTypeHandle = GCHandle.Alloc(meshTypes, GCHandleType.Pinned);
            IntPtr   nativeMeshTypePtr    = nativeMeshTypeHandle.AddrOfPinnedObject();

            bool result = GenerateMeshDescriptions(
                modelNodeID,
                meshTypes.Length,
                nativeMeshTypePtr,
                vertexChannelMask,
                out int meshDescriptionCount
            );

            nativeMeshTypeHandle.Free();

            if(!result)
            {
                return false;
            }

            if(meshDescriptionCount != meshDescriptions.Length)
            {
                meshDescriptions = new GeneratedMeshDescription[meshDescriptionCount];
            }

            if(meshDescriptionCount == 0)
            {
                return true;
            }

            GCHandle meshDescriptionsHandle = GCHandle.Alloc(meshDescriptions, GCHandleType.Pinned);
            IntPtr   meshDescriptionsPtr    = meshDescriptionsHandle.AddrOfPinnedObject();

            result = GetMeshDescriptions(modelNodeID, meshDescriptionCount, meshDescriptionsPtr);

            meshDescriptionsHandle.Free();

            return result;
        }

        private static bool GetMeshDescriptions(CSGModel model, ref GeneratedMeshDescription[] meshDescriptions)
        {
            return GetMeshDescriptions(
                model.modelNodeID, InternalCSGModelManager.GetMeshTypesForModel(model), model.VertexChannels
              , ref meshDescriptions
            );
        }

        [DllImport(NATIVE_PLUGIN_NAME, CallingConvention = CallingConvention.Cdecl)]
        private static extern bool GetGeneratedMesh(
            int modelNodeID,
            int meshIndex,
            int subMeshIndex,
            int indexCount,
            [Out] IntPtr indices,
            int vertexCount,
            [Out] IntPtr positions,
            [Out] IntPtr tangents,
            [Out] IntPtr normals,
            [Out] IntPtr uvs,
            out Vector3 boundsCenter,
            out Vector3 boundsSize
        ); // singular value

        private static bool GetModelMesh(
            int modelNodeID,
            int meshIndex,
            int subMeshIndex,
            int indexCount,
            int[] indices,
            int vertexCount,
            Vector3[] positions,
            Vector4[] tangents,
            Vector3[] normals,
            Vector2[] uv0,
            out Vector3 boundsCenter,
            out Vector3 boundsSize
        )
        {
            if(indices == null || indexCount > indices.Length ||      // may not be null
               positions == null || vertexCount > positions.Length || // may not be null
               (tangents != null && vertexCount > tangents.Length) || // may be null
               (normals != null && vertexCount > normals.Length) ||   // may be null
               (uv0 != null && vertexCount > uv0.Length))             // may be null
            {
                boundsCenter = Vector3.zero;
                boundsSize   = Vector3.zero;

                return false;
            }

            GCHandle indicesHandle  = GCHandle.Alloc(indices, GCHandleType.Pinned);
            GCHandle positionHandle = GCHandle.Alloc(positions, GCHandleType.Pinned);
            GCHandle tangentHandle  = new GCHandle();
            GCHandle normalHandle   = new GCHandle();
            GCHandle uv0Handle      = new GCHandle();

            IntPtr indicesPtr  = indicesHandle.AddrOfPinnedObject();
            IntPtr positionPtr = positionHandle.AddrOfPinnedObject();
            IntPtr tangentPtr  = IntPtr.Zero;
            IntPtr normalPtr   = IntPtr.Zero;
            IntPtr uv0Ptr      = IntPtr.Zero;

            if(tangents != null)
            {
                tangentHandle = GCHandle.Alloc(tangents, GCHandleType.Pinned);
                tangentPtr    = tangentHandle.AddrOfPinnedObject();
            }

            if(normals != null)
            {
                normalHandle = GCHandle.Alloc(normals, GCHandleType.Pinned);
                normalPtr    = normalHandle.AddrOfPinnedObject();
            }

            if(uv0 != null)
            {
                uv0Handle = GCHandle.Alloc(uv0, GCHandleType.Pinned);
                uv0Ptr    = uv0Handle.AddrOfPinnedObject();
            }

            bool result = GetGeneratedMesh(
                modelNodeID,
                meshIndex,
                subMeshIndex,
                indexCount,
                indicesPtr,
                vertexCount,
                positionPtr,
                tangentPtr,
                normalPtr,
                uv0Ptr,
                out boundsCenter,
                out boundsSize
            );

            if(uv0 != null)
            {
                uv0Handle.Free();
            }

            if(normals != null)
            {
                normalHandle.Free();
            }

            if(tangents != null)
            {
                tangentHandle.Free();
            }

            positionHandle.Free();
            indicesHandle.Free();

            return result;
        }

        private static GeneratedMeshContents GetModelMesh(int modelNodeID, GeneratedMeshDescription meshDescription)
        {
            GeneratedMeshContents generatedMesh = new GeneratedMeshContents();
            // create our arrays on the C# side with the correct size
            VertexChannelFlags usedVertexChannels = meshDescription.meshQuery.UsedVertexChannels;
            int                vertexCount        = meshDescription.vertexCount;
            int                indexCount         = meshDescription.indexCount;
            int                meshIndex          = meshDescription.meshQueryIndex;
            int                subMeshIndex       = meshDescription.subMeshQueryIndex;

            generatedMesh.description = meshDescription;

            generatedMesh.tangents =
                (usedVertexChannels & VertexChannelFlags.Tangent) != 0 ? new Vector4[vertexCount] : null;

            generatedMesh.normals =
                (usedVertexChannels & VertexChannelFlags.Normal) != 0 ? new Vector3[vertexCount] : null;

            generatedMesh.uv0 = (usedVertexChannels & VertexChannelFlags.UV0) != 0 ? new Vector2[vertexCount] : null;
            generatedMesh.positions = new Vector3[vertexCount];
            generatedMesh.indices = new int[indexCount];

            if(!GetModelMesh(
                   modelNodeID,
                   meshIndex,
                   subMeshIndex,
                   indexCount,
                   generatedMesh.indices,
                   vertexCount,
                   generatedMesh.positions,
                   generatedMesh.tangents,
                   generatedMesh.normals,
                   generatedMesh.uv0,
                   out Vector3 boundsCenter,
                   out Vector3 boundsSize
               ))
            {
                return null;
            }

            generatedMesh.bounds = new Bounds(boundsCenter, boundsSize);

            return generatedMesh;
        }

        private static bool GetModelMeshNoAlloc(
            int modelNodeID, GeneratedMeshDescription meshDescription, ref GeneratedMeshContents generatedMesh
        )
        {
            // create our arrays on the C# side with the correct size
            VertexChannelFlags usedVertexChannels = meshDescription.meshQuery.UsedVertexChannels;
            int                vertexCount        = meshDescription.vertexCount;
            int                indexCount         = meshDescription.indexCount;
            int                meshIndex          = meshDescription.meshQueryIndex;
            int                subMeshIndex       = meshDescription.subMeshQueryIndex;

            Vector4[] tangents = (usedVertexChannels & VertexChannelFlags.Tangent) != 0 ? generatedMesh.tangents : null;
            Vector3[] normals = (usedVertexChannels & VertexChannelFlags.Normal) != 0 ? generatedMesh.normals : null;
            Vector2[] uv0 = (usedVertexChannels & VertexChannelFlags.UV0) != 0 ? generatedMesh.uv0 : null;
            Vector3[] positions = generatedMesh.positions;
            int[]     indices = generatedMesh.indices;

            if(!GetModelMesh(
                   modelNodeID,
                   meshIndex,
                   subMeshIndex,
                   indexCount,
                   indices,
                   vertexCount,
                   positions,
                   tangents,
                   normals,
                   uv0,
                   out Vector3 boundsCenter,
                   out Vector3 boundsSize
               ))
            {
                return false;
            }

            generatedMesh.description = meshDescription;
            generatedMesh.bounds      = new Bounds(boundsCenter, boundsSize);

            return true;
        }

#endregion

#region Outline C++ functions

        [DllImport(NATIVE_PLUGIN_NAME, CallingConvention = CallingConvention.Cdecl)]
        private static extern ulong GetBrushOutlineGeneration(int brushNodeID);

        [DllImport(NATIVE_PLUGIN_NAME, CallingConvention = CallingConvention.Cdecl)]
        private static extern bool GetBrushOutlineSizes(
            int brushNodeID,
            out int vertexCount,
            out int visibleOuterLineCount,
            out int visibleInnerLineCount,
            out int invisibleOuterLineCount,
            out int invisibleInnerLineCount,
            out int invalidLineCount
        );

        [DllImport(NATIVE_PLUGIN_NAME, CallingConvention = CallingConvention.Cdecl)]
        private static extern bool GetBrushOutlineValues(
            int brushNodeID,
            int vertexCount,
            [Out] IntPtr vertices,
            int visibleOuterLineCount,
            [Out] IntPtr visibleOuterLines,
            int visibleInnerLineCount,
            [Out] IntPtr visibleInnerLines,
            int invisibleOuterLineCount,
            [Out] IntPtr invisibleOuteLines,
            int invisibleInnerLineCount,
            [Out] IntPtr invisibleInnerLines,
            int invalidLineCount,
            [Out] IntPtr invalidLines
        );

        private static bool GetBrushOutline(
            int brushNodeID,
            out GeometryWireframe outline
        )
        {
            if(!GetBrushOutlineSizes(
                   brushNodeID,
                   out int vertexCount,
                   out int visibleOuterLineCount,
                   out int visibleInnerLineCount,
                   out int invisibleOuterLineCount,
                   out int invisibleInnerLineCount,
                   out int invalidLineCount
               ) ||
               vertexCount == 0 ||
               (visibleOuterLineCount == 0 && invisibleOuterLineCount == 0 &&
                   visibleInnerLineCount == 0 && invisibleInnerLineCount == 0 &&
                   invalidLineCount == 0))
            {
                outline = null;

                return false;
            }

            outline                     = new GeometryWireframe();
            outline.vertices            = null;
            outline.visibleOuterLines   = null;
            outline.visibleInnerLines   = null;
            outline.invisibleOuterLines = null;
            outline.invisibleInnerLines = null;
            outline.invalidLines        = null;

            if(outline.vertices == null || outline.vertices.Length != vertexCount)
            {
                outline.vertices = new Vector3[vertexCount];
            }

            if(visibleOuterLineCount > 0 &&
               (outline.visibleOuterLines == null || outline.visibleOuterLines.Length != visibleOuterLineCount))
            {
                outline.visibleOuterLines = new int[visibleOuterLineCount];
            }

            if(visibleInnerLineCount > 0 &&
               (outline.visibleInnerLines == null || outline.visibleInnerLines.Length != visibleInnerLineCount))
            {
                outline.visibleInnerLines = new int[visibleInnerLineCount];
            }

            if(invisibleOuterLineCount > 0 &&
               (outline.invisibleOuterLines == null || outline.invisibleOuterLines.Length != invisibleOuterLineCount))
            {
                outline.invisibleOuterLines = new int[invisibleOuterLineCount];
            }

            if(invisibleInnerLineCount > 0 &&
               (outline.invisibleInnerLines == null || outline.invisibleInnerLines.Length != invisibleInnerLineCount))
            {
                outline.invisibleInnerLines = new int[invisibleInnerLineCount];
            }

            if(invalidLineCount > 0 &&
               (outline.invalidLines == null || outline.invalidLines.Length != invalidLineCount))
            {
                outline.invalidLines = new int[invalidLineCount];
            }

            GCHandle verticesHandle            = GCHandle.Alloc(outline.vertices, GCHandleType.Pinned);
            GCHandle visibleOuterLinesHandle   = GCHandle.Alloc(outline.visibleOuterLines, GCHandleType.Pinned);
            GCHandle visibleInnerLinesHandle   = GCHandle.Alloc(outline.visibleInnerLines, GCHandleType.Pinned);
            GCHandle invisibleOuterLinesHandle = GCHandle.Alloc(outline.invisibleOuterLines, GCHandleType.Pinned);
            GCHandle invisibleInnerLinesHandle = GCHandle.Alloc(outline.invisibleInnerLines, GCHandleType.Pinned);
            GCHandle invalidLinesHandle        = GCHandle.Alloc(outline.invalidLines, GCHandleType.Pinned);

            IntPtr verticesPtr            = verticesHandle.AddrOfPinnedObject();
            IntPtr visibleOuterLinesPtr   = visibleOuterLinesHandle.AddrOfPinnedObject();
            IntPtr visibleInnerLinesPtr   = visibleInnerLinesHandle.AddrOfPinnedObject();
            IntPtr invisibleOuterLinesPtr = invisibleOuterLinesHandle.AddrOfPinnedObject();
            IntPtr invisibleInnerLinesPtr = invisibleInnerLinesHandle.AddrOfPinnedObject();
            IntPtr invalidLinesPtr        = invalidLinesHandle.AddrOfPinnedObject();

            if(!GetBrushOutlineValues(
                   brushNodeID,
                   vertexCount,
                   verticesPtr,
                   visibleOuterLineCount,
                   visibleOuterLinesPtr,
                   visibleInnerLineCount,
                   visibleInnerLinesPtr,
                   invisibleOuterLineCount,
                   invisibleOuterLinesPtr,
                   invisibleInnerLineCount,
                   invisibleInnerLinesPtr,
                   invalidLineCount,
                   invalidLinesPtr
               ))
            {
                return false;
            }

            verticesHandle.Free();
            visibleOuterLinesHandle.Free();
            visibleInnerLinesHandle.Free();
            invisibleOuterLinesHandle.Free();
            invisibleInnerLinesHandle.Free();
            invalidLinesHandle.Free();

            return true;
        }

        [DllImport(NATIVE_PLUGIN_NAME, CallingConvention = CallingConvention.Cdecl)]
        private static extern bool GetSurfaceOutlineSizes(
            int brushNodeID,
            int surfaceID,
            out int vertexCount,
            out int visibleOuterLineCount,
            out int visibleInnerLineCount,
            out int visibleTriangleCount,
            out int invisibleOuterLineCount,
            out int invisibleInnerLineCount,
            out int invalidLineCount
        );

        [DllImport(NATIVE_PLUGIN_NAME, CallingConvention = CallingConvention.Cdecl)]
        private static extern bool GetSurfaceOutlineValues(
            int brushNodeID,
            int surfaceID,
            int vertexCount,
            [Out] IntPtr vertices,
            int visibleOuterLineCount,
            [Out] IntPtr visibleOuterLines,
            int visibleInnerLineCount,
            [Out] IntPtr visibleInnerLines,
            int visibleTriangleCount,
            [Out] IntPtr visibleTriangles,
            int invisibleOuterLineCount,
            [Out] IntPtr invisibleOuterLines,
            int invisibleInnerLineCount,
            [Out] IntPtr invisibleInnerLines,
            int invalidLineCount,
            [Out] IntPtr invalidLines
        );

        private static bool GetSurfaceOutline(
            int brushNodeID,
            int surfaceID,
            out GeometryWireframe outline
        )
        {
            if(!GetSurfaceOutlineSizes(
                   brushNodeID,
                   surfaceID,
                   out int vertexCount,
                   out int visibleOuterLineCount,
                   out int visibleInnerLineCount,
                   out int visibleTriangleCount,
                   out int invisibleOuterLineCount,
                   out int invisibleInnerLineCount,
                   out int invalidLineCount
               ))
            {
                outline = null;

                return false;
            }

            if(vertexCount == 0 ||
               (visibleOuterLineCount == 0 && invisibleOuterLineCount == 0 &&
                   visibleInnerLineCount == 0 && invisibleInnerLineCount == 0 &&
                   visibleTriangleCount == 0 &&
                   invalidLineCount == 0))
            {
                vertexCount             = 0;
                visibleOuterLineCount   = 0;
                visibleInnerLineCount   = 0;
                visibleTriangleCount    = 0;
                invisibleOuterLineCount = 0;
                invisibleInnerLineCount = 0;
                invalidLineCount        = 0;
                outline                 = null;

                return false;
            }

            outline = new GeometryWireframe();

            if(outline.vertices == null || outline.vertices.Length != vertexCount)
            {
                outline.vertices = new Vector3[vertexCount];
            }

            if(visibleOuterLineCount > 0 &&
               (outline.visibleOuterLines == null || outline.visibleOuterLines.Length != visibleOuterLineCount))
            {
                outline.visibleOuterLines = new int[visibleOuterLineCount];
            }

            if(visibleInnerLineCount > 0 &&
               (outline.visibleInnerLines == null || outline.visibleInnerLines.Length != visibleInnerLineCount))
            {
                outline.visibleInnerLines = new int[visibleInnerLineCount];
            }

            if(visibleTriangleCount > 0 &&
               (outline.visibleTriangles == null || outline.visibleTriangles.Length != visibleTriangleCount))
            {
                outline.visibleTriangles = new int[visibleTriangleCount];
            }

            if(invisibleOuterLineCount > 0 &&
               (outline.invisibleOuterLines == null || outline.invisibleOuterLines.Length != invisibleOuterLineCount))
            {
                outline.invisibleOuterLines = new int[invisibleOuterLineCount];
            }

            if(invisibleInnerLineCount > 0 &&
               (outline.invisibleInnerLines == null || outline.invisibleInnerLines.Length != invisibleInnerLineCount))
            {
                outline.invisibleInnerLines = new int[invisibleInnerLineCount];
            }

            if(invalidLineCount > 0 &&
               (outline.invalidLines == null || outline.invalidLines.Length != invalidLineCount))
            {
                outline.invalidLines = new int[invalidLineCount];
            }

            GCHandle verticesHandle            = GCHandle.Alloc(outline.vertices, GCHandleType.Pinned);
            GCHandle visibleOuterLinesHandle   = GCHandle.Alloc(outline.visibleOuterLines, GCHandleType.Pinned);
            GCHandle visibleInnerLinesHandle   = GCHandle.Alloc(outline.visibleInnerLines, GCHandleType.Pinned);
            GCHandle visibleTrianglesHandle    = GCHandle.Alloc(outline.visibleTriangles, GCHandleType.Pinned);
            GCHandle invisibleOuterLinesHandle = GCHandle.Alloc(outline.invisibleOuterLines, GCHandleType.Pinned);
            GCHandle invisibleInnerLinesHandle = GCHandle.Alloc(outline.invisibleInnerLines, GCHandleType.Pinned);
            GCHandle invalidLinesHandle        = GCHandle.Alloc(outline.invalidLines, GCHandleType.Pinned);

            IntPtr verticesPtr            = verticesHandle.AddrOfPinnedObject();
            IntPtr visibleOuterLinesPtr   = visibleOuterLinesHandle.AddrOfPinnedObject();
            IntPtr visibleInnerLinesPtr   = visibleInnerLinesHandle.AddrOfPinnedObject();
            IntPtr visibleTrianglesPtr    = visibleTrianglesHandle.AddrOfPinnedObject();
            IntPtr invisibleOuterLinesPtr = invisibleOuterLinesHandle.AddrOfPinnedObject();
            IntPtr invisibleInnerLinesPtr = invisibleInnerLinesHandle.AddrOfPinnedObject();
            IntPtr invalidLinesPtr        = invalidLinesHandle.AddrOfPinnedObject();

            if(!GetSurfaceOutlineValues(
                   brushNodeID,
                   surfaceID,
                   vertexCount,
                   verticesPtr,
                   visibleOuterLineCount,
                   visibleOuterLinesPtr,
                   visibleInnerLineCount,
                   visibleInnerLinesPtr,
                   visibleTriangleCount,
                   visibleTrianglesPtr,
                   invisibleOuterLineCount,
                   invisibleOuterLinesPtr,
                   invisibleInnerLineCount,
                   invisibleInnerLinesPtr,
                   invalidLineCount,
                   invalidLinesPtr
               ))
            {
                return false;
            }

            verticesHandle.Free();
            visibleOuterLinesHandle.Free();
            visibleInnerLinesHandle.Free();
            visibleTrianglesHandle.Free();
            invisibleOuterLinesHandle.Free();
            invisibleInnerLinesHandle.Free();
            invalidLinesHandle.Free();

            return true;
        }

#endregion

        public static void RegisterExternalMethods()
        {
            NativeMethods methods = new NativeMethods();
            methods.resetCSG = ClearAllNodes;

            methods.convexPartition = ConvexPartition;

            methods.setDirty      = SetDirty;
            methods.setChildNodes = SetChildNodes;
            methods.destroyNode   = DestroyNode;
            methods.destroyNodes  = nodeIDs => DestroyNodes(nodeIDs.Length, nodeIDs);

            methods.generateModel   = GenerateTree;
            methods.setModelEnabled = SetTreeEnabled;

            methods.rayCastMulti            = RayCastMulti;
            methods.rayCastIntoModelMulti   = RayCastIntoModelMulti;
            methods.rayCastIntoBrush        = RayCastIntoBrush;
            methods.rayCastIntoBrushSurface = RayCastIntoBrushSurface;
            methods.getItemsInFrustum       = GetItemsInFrustum;

            methods.generateOperation         = GenerateBranch;
            methods.setOperationOperationType = SetBranchOperationType;

            methods.generateBrush         = GenerateBrush;
            methods.getBrushMeshID        = GetBrushMeshID;
            methods.setBrushMeshID        = SetBrushMeshID;
            methods.getBrushFlags         = GetBrushFlags;
            methods.setBrushFlags         = SetBrushFlags;
            methods.setBrushOperationType = SetBrushOperationType;
            methods.setBrushToModelSpace  = SetBrushToModelSpace;

            methods.createBrushMesh  = CreateBrushMesh;
            methods.updateBrushMesh  = UpdateBrushMesh;
            methods.destroyBrushMesh = DestroyBrushMesh;

            methods.getSurfaceMinMaxTexCoords  = GetSurfaceMinMaxTexCoords;
            methods.convertModelToTextureSpace = ConvertWorldToTextureCoord;
            methods.convertTextureToModelSpace = ConvertTextureToWorldCoord;

            methods.getBrushOutlineGeneration = GetBrushOutlineGeneration;
            methods.getBrushOutline           = GetBrushOutline;
            methods.getSurfaceOutline         = GetSurfaceOutline;

            methods.updateAllModelMeshes = UpdateAllTreeMeshes;
            methods.getMeshDescriptions  = GetMeshDescriptions;

            methods.getModelMesh        = GetModelMesh;
            methods.getModelMeshNoAlloc = GetModelMeshNoAlloc;

            InternalCSGModelManager.External = methods;
        }

        public static void ClearExternalMethods()
        {
            InternalCSGModelManager.External = null;
        }

#endregion
    }
}
#endif
