#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEngine;
using RealtimeCSG.Foundation;
using RealtimeCSG.Components;

namespace RealtimeCSG
{
#region delegates

#region Tree delegates

    internal delegate bool GenerateModelDelegate(int userID, out int generatedModelNodeID);
    internal delegate bool SetDirtyDelegate(int nodeID);
    internal delegate bool SetModelEnabledDelegate(int modelNodeID, bool isEnabled);

#endregion

#region Selection delegates

    internal delegate bool RayCastMultiDelegate(int modelCount, CSGModel[] models, Vector3 rayStart, Vector3 rayEnd, bool ignoreInvisiblePolygons, out LegacyBrushIntersection[] intersections, CSGBrush[] ignoreBrushes = null);
    internal delegate bool RayCastIntoModelMultiDelegate(CSGModel model, Vector3 rayStart, Vector3 rayEnd, bool ignoreInvisiblePolygons, out LegacyBrushIntersection[] intersections, CSGBrush[] ignoreBrushes = null);
    internal delegate bool RayCastIntoBrushDelegate(int brushNodeID, Vector3 rayStart, Vector3 rayEnd, Matrix4x4 modelTransformation, out LegacyBrushIntersection intersection, bool ignoreInvisiblePolygons);

    internal delegate bool RayCastIntoBrushSurfaceDelegate(int brushNodeID, int surfaceIndex, Vector3 rayStart, Vector3 rayEnd, Matrix4x4 modelTransformation, out LegacySurfaceIntersection intersection);
    internal delegate bool GetItemsInFrustumDelegate(CSGModel model, Plane[] planes, HashSet<GameObject> gameObjects);

#endregion

    internal delegate bool SetChildNodesDelegate(int nodeID, int childCount, int[] childrenNodeIDs);
    internal delegate bool DestroyNodeDelegate(int operationNodeID);
    internal delegate bool DestroyNodesDelegate(int[] operationNodeIDs);

#region Operation delegates

    internal delegate bool GenerateOperationDelegate(int userID, out int generatedOperationNodeID);
    internal delegate bool SetOperationOperationTypeDelegate(int operationNodeID, CSGOperationType operation);

#endregion

#region Brush delegates

    internal delegate bool GenerateBrushDelegate(int userID, out int generatedBrushNodeID);
    internal delegate int GetBrushMeshIDDelegate(int brushNodeID);
    internal delegate bool SetBrushMeshIDDelegate(int brushNodeID, int brushMeshIndex);
    internal delegate CSGTreeBrushFlags GetBrushFlagsDelegate(int brushNodeID);
    internal delegate bool SetBrushFlagsDelegate(int brushNodeID, CSGTreeBrushFlags flags);
    internal delegate bool SetBrushOperationTypeDelegate(int brushNodeID, CSGOperationType operation);
    internal delegate bool SetBrushToModelSpaceDelegate(int brushNodeID, Matrix4x4 localToModelSpace);
    internal delegate int CreateBrushMeshDelegate(int userID, BrushMesh brushMesh);
    internal delegate bool UpdateBrushMeshDelegate(int brushMeshIndex, BrushMesh brushMesh);
    internal delegate bool DestroyBrushMeshDelegate(int brushMeshIndex);

#endregion

#region Misc

    internal delegate List<List<Vector2>> ConvexPartitionDelegate(Vector2[] points);

#endregion

#region TexGen manipulation delegates

    internal delegate bool GetSurfaceMinMaxTexCoordsDelegate(int brushNodeID, int surfaceIndex, Matrix4x4 modelLocalToWorldMatrix, out Vector2 minTextureCoordinate, out Vector2 maxTextureCoordinate);
    internal delegate bool ConvertWorldToTextureCoordDelegate(int brushNodeID, int surfaceIndex, Matrix4x4 modelTransformation, Vector3 worldCoordinate, out Vector2 textureCoordinate);
    internal delegate bool ConvertTextureToWorldCoordDelegate(int brushNodeID, int surfaceIndex, float textureCoordinateU, float textureCoordinateV, ref Matrix4x4 modelTransformation, ref float worldCoordinateX, ref float worldCoordinateY, ref float worldCoordinateZ);

#endregion

#region Outlines

    internal delegate ulong GetBrushOutlineGenerationDelegate(int brushNodeID);
    internal delegate bool GetBrushOutlineDelegate(int brushNodeID, out GeometryWireframe geometryWireframe);
    internal delegate bool GetSurfaceOutlineDelegate(int brushNodeID, int surfaceIndex, out GeometryWireframe outline);

#endregion

#region Meshes

    internal delegate void ResetCSGDelegate();
    internal delegate bool UpdateAllModelMeshesDelegate();
    internal delegate bool GetMeshDescriptionsDelegate(CSGModel model, ref GeneratedMeshDescription[] meshDescriptions);
    internal delegate GeneratedMeshContents GetModelMeshDelegate(int modelNodeID, GeneratedMeshDescription meshDescription);
    internal delegate bool GetModelMeshNoAllocDelegate(int modelNodeID, GeneratedMeshDescription meshDescription, ref GeneratedMeshContents generatedMeshData);

#endregion

#endregion

    internal sealed class NativeMethods
    {
        public ResetCSGDelegate resetCSG;
        public ConvexPartitionDelegate convexPartition;

        public SetDirtyDelegate setDirty;
        public SetChildNodesDelegate setChildNodes;
        public DestroyNodeDelegate destroyNode;
        public DestroyNodesDelegate destroyNodes;

        public GenerateModelDelegate generateModel;
        public SetModelEnabledDelegate setModelEnabled;

        public GenerateOperationDelegate generateOperation;
        public SetOperationOperationTypeDelegate setOperationOperationType;

        public GenerateBrushDelegate generateBrush;
        public GetBrushMeshIDDelegate getBrushMeshID;
        public SetBrushMeshIDDelegate setBrushMeshID;
        public GetBrushFlagsDelegate getBrushFlags;
        public SetBrushFlagsDelegate setBrushFlags;
        public SetBrushOperationTypeDelegate setBrushOperationType;
        public SetBrushToModelSpaceDelegate setBrushToModelSpace;

        public CreateBrushMeshDelegate createBrushMesh;
        public UpdateBrushMeshDelegate updateBrushMesh;
        public DestroyBrushMeshDelegate destroyBrushMesh;

        public UpdateAllModelMeshesDelegate updateAllModelMeshes;
        public GetMeshDescriptionsDelegate getMeshDescriptions;

        public GetModelMeshDelegate getModelMesh;
        public GetModelMeshNoAllocDelegate getModelMeshNoAlloc;

        public RayCastMultiDelegate rayCastMulti;
        public RayCastIntoModelMultiDelegate rayCastIntoModelMulti;
        public RayCastIntoBrushDelegate rayCastIntoBrush;
        public RayCastIntoBrushSurfaceDelegate rayCastIntoBrushSurface;
        public GetItemsInFrustumDelegate getItemsInFrustum;

        public GetSurfaceMinMaxTexCoordsDelegate getSurfaceMinMaxTexCoords;
        public ConvertWorldToTextureCoordDelegate convertModelToTextureSpace;
        public ConvertTextureToWorldCoordDelegate convertTextureToModelSpace;

        public GetBrushOutlineGenerationDelegate getBrushOutlineGeneration;
        public GetBrushOutlineDelegate getBrushOutline;
        public GetSurfaceOutlineDelegate getSurfaceOutline;
    }
}
#endif
