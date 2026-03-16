#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEngine;
using RealtimeCSG.Foundation;
using RealtimeCSG.Components;

namespace RealtimeCSG
{
#region delegates

#region Tree delegates

    internal delegate bool GenerateModelDelegate(
        int userID,
        out int generatedModelNodeID
    );

    internal delegate bool SetModelDelegate(
        int modelNodeID,
        bool isEnabled
    );

    internal delegate bool SetDirtyDelegate(int nodeID);

    internal delegate bool SetModelEnabledDelegate(
        int modelNodeID,
        bool isEnabled
    );

#endregion

#region Selection delegates

    internal delegate bool RayCastMultiDelegate(
        int modelCount,
        CSGModel[] models,
        Vector3 rayStart,
        Vector3 rayEnd,
        bool ignoreInvisiblePolygons,
        out LegacyBrushIntersection[] intersections,
        CSGBrush[] ignoreBrushes = null
    );

    internal delegate bool RayCastIntoModelMultiDelegate(
        CSGModel model,
        Vector3 rayStart,
        Vector3 rayEnd,
        bool ignoreInvisiblePolygons,
        out LegacyBrushIntersection[] intersections,
        CSGBrush[] ignoreBrushes = null
    );

    internal delegate bool RayCastIntoModelDelegate(
        CSGModel model,
        Vector3 rayStart,
        Vector3 rayEnd,
        bool ignoreInvisiblePolygons,
        out LegacyBrushIntersection intersection,
        CSGBrush[] ignoreBrushes = null
    );

    internal delegate bool RayCastIntoBrushDelegate(
        int brushNodeID,
        Vector3 rayStart,
        Vector3 rayEnd,
        Matrix4x4 modelTransformation,
        out LegacyBrushIntersection intersection,
        bool ignoreInvisiblePolygons
    );

    internal delegate bool RayCastIntoBrushSurfaceDelegate(
        int brushNodeID,
        int surfaceIndex,
        Vector3 rayStart,
        Vector3 rayEnd,
        Matrix4x4 modelTransformation,
        out LegacySurfaceIntersection intersection
    );

    internal delegate bool GetItemsInFrustumDelegate(
        CSGModel model,
        Plane[] planes,
        HashSet<GameObject> gameObjects
    );

#endregion

    internal delegate bool SetChildNodesDelegate(
        int nodeID,
        int childCount,
        int[] childrenNodeIDs
    );

    internal delegate bool DestroyNodeDelegate(int operationNodeID);

    internal delegate bool DestroyNodesDelegate(int[] operationNodeIDs);

#region Operation delegates

    internal delegate bool GenerateOperationDelegate(
        int userID,
        out int generatedOperationNodeID
    );

    internal delegate bool SetOperationOperationTypeDelegate(
        int operationNodeID,
        CSGOperationType operation
    );

#endregion

#region Brush delegates

    internal delegate bool GenerateBrushDelegate(
        int userID,
        out int generatedBrushNodeID
    );

    internal delegate int GetBrushMeshIDDelegate(int brushNodeID);
    internal delegate bool SetBrushMeshIDDelegate(int brushNodeID, int brushMeshIndex);

    internal delegate CSGTreeBrushFlags GetBrushFlagsDelegate(int brushNodeID);
    internal delegate bool SetBrushFlagsDelegate(int brushNodeID, CSGTreeBrushFlags flags);

    internal delegate bool SetBrushOperationTypeDelegate(
        int brushNodeID,
        CSGOperationType operation
    );

    internal delegate bool SetBrushToModelSpaceDelegate(
        int brushNodeID,
        Matrix4x4 localToModelSpace
    );

    internal delegate int CreateBrushMeshDelegate(int userID, BrushMesh brushMesh);

    internal delegate bool UpdateBrushMeshDelegate(
        int brushMeshIndex,
        BrushMesh brushMesh
    );

    internal delegate bool DestroyBrushMeshDelegate(int brushMeshIndex);

#endregion

#region Misc

    internal delegate List<List<Vector2>> ConvexPartitionDelegate(Vector2[] points);

#endregion

#region TexGen manipulation delegates

    internal delegate bool GetSurfaceMinMaxTexCoordsDelegate(
        int brushNodeID,
        int surfaceIndex,
        Matrix4x4 modelLocalToWorldMatrix,
        out Vector2 minTextureCoordinate,
        out Vector2 maxTextureCoordinate
    );

    internal delegate bool GetSurfaceMinMaxWorldCoordDelegate(
        int brushNodeID,
        int surfaceIndex,
        out Vector3 minWorldCoordinate,
        out Vector3 maxWorldCoordinate
    );

    internal delegate bool ConvertWorldToTextureCoordDelegate(
        int brushNodeID,
        int surfaceIndex,
        Matrix4x4 modelTransformation,
        Vector3 worldCoordinate,
        out Vector2 textureCoordinate
    );

    internal delegate bool ConvertTextureToWorldCoordDelegate(
        int brushNodeID,
        int surfaceIndex,
        float textureCoordinateU,
        float textureCoordinateV,
        // workaround for mac-osx related bug
        ref Matrix4x4 modelTransformation,
        ref float worldCoordinateX,
        ref float worldCoordinateY,
        ref float worldCoordinateZ
    );

#endregion

#region Outlines

    internal delegate ulong GetBrushOutlineGenerationDelegate(int brushNodeID);

    internal delegate bool GetBrushOutlineDelegate(
        int brushNodeID,
        out GeometryWireframe geometryWireframe
    );

    internal delegate bool GetSurfaceOutlineDelegate(
        int brushNodeID,
        int surfaceIndex,
        out GeometryWireframe outline
    );

#endregion

#region Meshes

    internal delegate void ResetCSGDelegate();

    internal delegate bool UpdateAllModelMeshesDelegate();
    internal delegate bool ModelMeshesNeedUpdateDelegate();

    internal delegate bool GetMeshDescriptionsDelegate(
        CSGModel model,
        ref GeneratedMeshDescription[] meshDescriptions
    );

    internal delegate GeneratedMeshContents GetModelMeshDelegate(
        int modelNodeID, GeneratedMeshDescription meshDescription
    );

    internal delegate bool GetModelMeshNoAllocDelegate(
        int modelNodeID, GeneratedMeshDescription meshDescription, ref GeneratedMeshContents generatedMeshData
    );

#endregion

#endregion

    internal sealed class NativeMethods
    {
        public ResetCSGDelegate ResetCSG;
        public ConvexPartitionDelegate ConvexPartition;

        public SetDirtyDelegate SetDirty;
        public SetChildNodesDelegate SetChildNodes;
        public DestroyNodeDelegate DestroyNode;
        public DestroyNodesDelegate DestroyNodes;

        public GenerateModelDelegate GenerateModel;
        public SetModelEnabledDelegate SetModelEnabled;

        public GenerateOperationDelegate GenerateOperation;
        public SetOperationOperationTypeDelegate SetOperationOperationType;

        public GenerateBrushDelegate GenerateBrush;
        public GetBrushMeshIDDelegate GetBrushMeshID;
        public SetBrushMeshIDDelegate SetBrushMeshID;
        public GetBrushFlagsDelegate GetBrushFlags;
        public SetBrushFlagsDelegate SetBrushFlags;
        public SetBrushOperationTypeDelegate SetBrushOperationType;
        public SetBrushToModelSpaceDelegate SetBrushToModelSpace;

        public CreateBrushMeshDelegate CreateBrushMesh;
        public UpdateBrushMeshDelegate UpdateBrushMesh;
        public DestroyBrushMeshDelegate DestroyBrushMesh;

        public UpdateAllModelMeshesDelegate UpdateAllModelMeshes;
        public GetMeshDescriptionsDelegate GetMeshDescriptions;

        public GetModelMeshDelegate GetModelMesh;
        public GetModelMeshNoAllocDelegate GetModelMeshNoAlloc;

        public RayCastMultiDelegate RayCastMulti;
        public RayCastIntoModelMultiDelegate RayCastIntoModelMulti;
        public RayCastIntoBrushDelegate RayCastIntoBrush;
        public RayCastIntoBrushSurfaceDelegate RayCastIntoBrushSurface;
        public GetItemsInFrustumDelegate GetItemsInFrustum;

        public GetSurfaceMinMaxTexCoordsDelegate GetSurfaceMinMaxTexCoords;
        public ConvertWorldToTextureCoordDelegate ConvertModelToTextureSpace;
        public ConvertTextureToWorldCoordDelegate ConvertTextureToModelSpace;

        public GetBrushOutlineGenerationDelegate GetBrushOutlineGeneration;
        public GetBrushOutlineDelegate GetBrushOutline;
        public GetSurfaceOutlineDelegate GetSurfaceOutline;
    }
}
#endif
