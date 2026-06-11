using System.Collections.Generic;
using System.Linq;
using InternalRealtimeCSG;
using RealtimeCSG.Components;
using RealtimeCSG.Legacy;
using UnityEditor;
using UnityEngine;

namespace RealtimeCSG
{
    internal static class SurfaceUtility
    {
        public static bool CanUnSmooth(SelectedBrushSurface[] selectedSurfaces)
        {
            if(selectedSurfaces == null || selectedSurfaces.Length == 0)
            {
                return false;
            }

            for(int s = 0; s < selectedSurfaces.Length; s++)
            {
                CSGBrush  brush        = selectedSurfaces[s].brush;
                int       surfaceIndex = selectedSurfaces[s].surfaceIndex;
                Shape     shape        = brush.shape;
                TexGen[]  texGens      = shape.texGens;
                Surface[] surfaces     = shape.surfaces;
                int       texGenIndex  = surfaces[surfaceIndex].TexGenIndex;

                if(texGenIndex >= texGens.Length)
                {
                    continue;
                }

                if(texGens[texGenIndex].SmoothingGroup > 0)
                {
                    return true;
                }
            }

            return false;
        }

        public static bool CanSmooth(SelectedBrushSurface[] selectedSurfaces)
        {
            if(selectedSurfaces == null || selectedSurfaces.Length == 0)
            {
                return false;
            }

            long foundSmoothGroup = -1;

            for(int s = 0; s < selectedSurfaces.Length; s++)
            {
                CSGBrush  brush        = selectedSurfaces[s].brush;
                int       surfaceIndex = selectedSurfaces[s].surfaceIndex;
                Shape     shape        = brush.shape;
                TexGen[]  texGens      = shape.texGens;
                Surface[] surfaces     = shape.surfaces;

                int texGenIndex = surfaces[surfaceIndex].TexGenIndex;

                if(texGens[texGenIndex].SmoothingGroup == 0)
                {
                    return true;
                }

                if(foundSmoothGroup == -1)
                {
                    foundSmoothGroup = texGens[texGenIndex].SmoothingGroup;

                    continue;
                }

                if(texGens[texGenIndex].SmoothingGroup != foundSmoothGroup)
                {
                    return true;
                }
            }

            return false;
        }

        public static void UnSmooth(SelectedBrushSurface[] selectedSurfaces)
        {
            if(selectedSurfaces == null || selectedSurfaces.Length == 0)
            {
                return;
            }

            Dictionary<CSGBrush, List<int>> brushSurfs = new Dictionary<CSGBrush, List<int>>();

            for(int s = 0; s < selectedSurfaces.Length; s++)
            {
                CSGBrush brush        = selectedSurfaces[s].brush;
                int      surfaceIndex = selectedSurfaces[s].surfaceIndex;

                if(!brushSurfs.TryGetValue(brush, out List<int> indices))
                {
                    indices = new List<int>();
                    brushSurfs.Add(brush, indices);
                }

                indices.Add(surfaceIndex);
            }

            if(brushSurfs.Count == 0)
            {
                return;
            }

            Undo.RecordObjects(brushSurfs.Keys.ToArray(), "Unsmoothing surfaces");

            foreach((CSGBrush brush, List<int> surfaceIndices) in brushSurfs)
            {
                Shape     shape    = brush.shape;
                TexGen[]  texGens  = shape.texGens;
                Surface[] surfaces = shape.surfaces;

                for(int s = 0; s < surfaceIndices.Count; s++)
                {
                    int texGenIndex = surfaces[surfaceIndices[s]].TexGenIndex;
                    texGens[texGenIndex].SmoothingGroup = 0;
                }

                InternalCSGModelManager.CheckSurfaceModifications(brush);
            }

            InternalCSGModelManager.CheckForChanges();
        }

        public static void Smooth(SelectedBrushSurface[] selectedSurfaces)
        {
            if(selectedSurfaces == null || selectedSurfaces.Length == 0)
            {
                return;
            }

            // Try to find a good smoothing group index
            uint smoothingGroupIndex = FindUnusedSmoothingGroupIndex();

            Dictionary<CSGBrush, List<int>> brushSurfs = new Dictionary<CSGBrush, List<int>>();

            for(int s = 0; s < selectedSurfaces.Length; s++)
            {
                CSGBrush brush        = selectedSurfaces[s].brush;
                int      surfaceIndex = selectedSurfaces[s].surfaceIndex;

                if(!brushSurfs.TryGetValue(brush, out List<int> indices))
                {
                    indices = new List<int>();
                    brushSurfs.Add(brush, indices);
                }

                indices.Add(surfaceIndex);
            }

            Undo.RecordObjects(brushSurfs.Keys.ToArray(), "Smoothing surfaces");

            foreach((CSGBrush brush, List<int> surfaceIndices) in brushSurfs)
            {
                Shape     shape    = brush.shape;
                TexGen[]  texGens  = shape.texGens;
                Surface[] surfaces = shape.surfaces;

                for(int s = 0; s < surfaceIndices.Count; s++)
                {
                    int texGenIndex = surfaces[surfaceIndices[s]].TexGenIndex;
                    texGens[texGenIndex].SmoothingGroup = smoothingGroupIndex;
                }

                InternalCSGModelManager.CheckSurfaceModifications(brush);
            }

            InternalCSGModelManager.CheckForChanges();
        }

        public static void SetSurfaceTexGenFlags(SelectedBrushSurface[] selectedBrushSurfaces, TexGenFlags flag, bool set)
        {
            using(new UndoGroup(selectedBrushSurfaces, "discarding surface"))
            {
                for(int i = 0; i < selectedBrushSurfaces.Length; i++)
                {
                    CSGBrush brush        = selectedBrushSurfaces[i].brush;
                    int      surfaceIndex = selectedBrushSurfaces[i].surfaceIndex;
                    int      texGenIndex  = brush.shape.surfaces[surfaceIndex].TexGenIndex;

                    if(set)
                    {
                        brush.shape.texGenFlags[texGenIndex] |= flag;
                    }
                    else
                    {
                        brush.shape.texGenFlags[texGenIndex] &= ~flag;
                    }
                }
            }
        }

        public static void SetTextureLock(SelectedBrushSurface[] selectedBrushSurfaces, bool value)
        {
            using(new UndoGroup(selectedBrushSurfaces, "setting texture lock"))
            {
                for(int i = 0; i < selectedBrushSurfaces.Length; i++)
                {
                    CSGBrush    brush        = selectedBrushSurfaces[i].brush;
                    int         surfaceIndex = selectedBrushSurfaces[i].surfaceIndex;
                    int         texGenIndex  = brush.shape.surfaces[surfaceIndex].TexGenIndex;
                    TexGenFlags oldFlags     = brush.shape.texGenFlags[texGenIndex];
                    TexGenFlags newFlags;

                    if(value)
                    {
                        newFlags = oldFlags & ~TexGenFlags.WorldSpaceTexture;
                    }
                    else
                    {
                        newFlags = oldFlags | TexGenFlags.WorldSpaceTexture;
                    }

                    if(oldFlags == newFlags)
                    {
                        continue;
                    }

                    Vector2 oldTextureCoord = ConvertModelToTextureSpace(brush, surfaceIndex, Vector3.zero);

                    brush.shape.texGenFlags[texGenIndex] = newFlags;
                    InternalCSGModelManager.SetBrushMeshSurfaces(brush);

                    Vector2 newTextureCoord = ConvertModelToTextureSpace(brush, surfaceIndex, Vector3.zero);

                    brush.shape.texGens[texGenIndex].Translation -= newTextureCoord - oldTextureCoord;
                }
            }
        }

        public static void SetMaterials(SelectedBrushSurface[] selectedBrushSurfaces, Material material)
        {
            using(new UndoGroup(selectedBrushSurfaces, "Setting materials"))
            {
                for(int i = 0; i < selectedBrushSurfaces.Length; i++)
                {
                    CSGBrush brush        = selectedBrushSurfaces[i].brush;
                    int      surfaceIndex = selectedBrushSurfaces[i].surfaceIndex;
                    int      texGenIndex  = brush.shape.surfaces[surfaceIndex].TexGenIndex;

                    brush.shape.texGens[texGenIndex].RenderMaterial = material;
                }
            }
        }

        public static void MultiplyScale(SelectedBrushSurface[] selectedBrushSurfaces, float scalar)
        {
            using(new UndoGroup(selectedBrushSurfaces, "Multiplying scale"))
            {
                for(int i = 0; i < selectedBrushSurfaces.Length; i++)
                {
                    CSGBrush brush        = selectedBrushSurfaces[i].brush;
                    Shape    shape        = brush.shape;
                    int      surfaceIndex = selectedBrushSurfaces[i].surfaceIndex;
                    int      texGenIndex  = brush.shape.surfaces[surfaceIndex].TexGenIndex;

                    shape.texGens[texGenIndex].Scale.x *= scalar;
                    shape.texGens[texGenIndex].Scale.y *= scalar;
                }
            }
        }

        public static void FlipX(SelectedBrushSurface[] selectedBrushSurfaces)
        {
            using(new UndoGroup(selectedBrushSurfaces, "Reversing texture coordinates of surface in X direction"))
            {
                for(int i = 0; i < selectedBrushSurfaces.Length; i++)
                {
                    CSGBrush brush        = selectedBrushSurfaces[i].brush;
                    int      surfaceIndex = selectedBrushSurfaces[i].surfaceIndex;
                    int      texGenIndex  = brush.shape.surfaces[surfaceIndex].TexGenIndex;

                    brush.shape.texGens[texGenIndex].Scale.x       *= -1;
                    brush.shape.texGens[texGenIndex].Translation.x *= -1;
                }
            }
        }

        public static void FlipY(SelectedBrushSurface[] selectedBrushSurfaces)
        {
            using(new UndoGroup(selectedBrushSurfaces, "Reversing texture coordinates of surface in Y direction"))
            {
                for(int i = 0; i < selectedBrushSurfaces.Length; i++)
                {
                    CSGBrush brush        = selectedBrushSurfaces[i].brush;
                    int      surfaceIndex = selectedBrushSurfaces[i].surfaceIndex;
                    int      texGenIndex  = brush.shape.surfaces[surfaceIndex].TexGenIndex;

                    brush.shape.texGens[texGenIndex].Scale.y       *= -1;
                    brush.shape.texGens[texGenIndex].Translation.y *= -1;
                }
            }
        }

        public static void FlipXY(SelectedBrushSurface[] selectedBrushSurfaces)
        {
            using(new UndoGroup(selectedBrushSurfaces, "Reversing texture coordinates of surface in both X and Y direction"))
            {
                for(int i = 0; i < selectedBrushSurfaces.Length; i++)
                {
                    CSGBrush brush        = selectedBrushSurfaces[i].brush;
                    int      surfaceIndex = selectedBrushSurfaces[i].surfaceIndex;
                    int      texGenIndex  = brush.shape.surfaces[surfaceIndex].TexGenIndex;

                    brush.shape.texGens[texGenIndex].Scale.x       *= -1;
                    brush.shape.texGens[texGenIndex].Scale.y       *= -1;
                    brush.shape.texGens[texGenIndex].Translation.x *= -1;
                    brush.shape.texGens[texGenIndex].Translation.y *= -1;
                }
            }
        }

        public static void SetScaleX(SelectedBrushSurface[] selectedBrushSurfaces, float scaleX)
        {
            using(new UndoGroup(selectedBrushSurfaces, "Setting X scale of surface"))
            {
                for(int i = 0; i < selectedBrushSurfaces.Length; i++)
                {
                    CSGBrush brush        = selectedBrushSurfaces[i].brush;
                    int      surfaceIndex = selectedBrushSurfaces[i].surfaceIndex;
                    int      texGenIndex  = brush.shape.surfaces[surfaceIndex].TexGenIndex;

                    brush.shape.texGens[texGenIndex].Scale.x = scaleX;
                }
            }
        }

        public static void SetScaleY(SelectedBrushSurface[] selectedBrushSurfaces, float scaleY)
        {
            using(new UndoGroup(selectedBrushSurfaces, "Setting Y scale of surface"))
            {
                for(int i = 0; i < selectedBrushSurfaces.Length; i++)
                {
                    CSGBrush brush        = selectedBrushSurfaces[i].brush;
                    int      surfaceIndex = selectedBrushSurfaces[i].surfaceIndex;
                    int      texGenIndex  = brush.shape.surfaces[surfaceIndex].TexGenIndex;

                    brush.shape.texGens[texGenIndex].Scale.y = scaleY;
                }
            }
        }

        public static void SetScale(SelectedBrushSurface[] selectedBrushSurfaces, Vector2 scale)
        {
            SetScaleX(selectedBrushSurfaces, scale.x);
            SetScaleY(selectedBrushSurfaces, scale.y);
        }

        public static void SetScale(SelectedBrushSurface[] selectedBrushSurfaces, float scale)
        {
            SetScale(selectedBrushSurfaces, new Vector2(scale, scale));
        }

        public static void SetTranslationX(SelectedBrushSurface[] selectedBrushSurfaces, float translationX)
        {
            using(new UndoGroup(selectedBrushSurfaces, "Setting X translation of surface"))
            {
                for(int i = 0; i < selectedBrushSurfaces.Length; i++)
                {
                    CSGBrush brush        = selectedBrushSurfaces[i].brush;
                    int      surfaceIndex = selectedBrushSurfaces[i].surfaceIndex;
                    int      texGenIndex  = brush.shape.surfaces[surfaceIndex].TexGenIndex;

                    brush.shape.texGens[texGenIndex].Translation.x = translationX;
                }
            }
        }

        public static void SetTranslationY(SelectedBrushSurface[] selectedBrushSurfaces, float translationY)
        {
            using(new UndoGroup(selectedBrushSurfaces, "Setting Y translation of surface"))
            {
                for(int i = 0; i < selectedBrushSurfaces.Length; i++)
                {
                    CSGBrush brush        = selectedBrushSurfaces[i].brush;
                    int      surfaceIndex = selectedBrushSurfaces[i].surfaceIndex;
                    int      texGenIndex  = brush.shape.surfaces[surfaceIndex].TexGenIndex;

                    brush.shape.texGens[texGenIndex].Translation.y = translationY;
                }
            }
        }

        private const float MAX_ANGLE = 360 * 10.0f;

        public static void SetRotationAngle(SelectedBrushSurface[] selectedBrushSurfaces, float angle)
        {
            angle %= MAX_ANGLE;

            using(new UndoGroup(selectedBrushSurfaces, "Setting rotation angle of surface"))
            {
                for(int i = 0; i < selectedBrushSurfaces.Length; i++)
                {
                    CSGBrush brush        = selectedBrushSurfaces[i].brush;
                    int      surfaceIndex = selectedBrushSurfaces[i].surfaceIndex;
                    int      texGenIndex  = brush.shape.surfaces[surfaceIndex].TexGenIndex;

                    brush.shape.texGens[texGenIndex].RotationAngle = angle;
                }
            }
        }

        public static void AddRotationAngle(SelectedBrushSurface[] selectedBrushSurfaces, float angle)
        {
            using(new UndoGroup(selectedBrushSurfaces, "Adding to rotation angle of surface"))
            {
                for(int i = 0; i < selectedBrushSurfaces.Length; i++)
                {
                    CSGBrush brush        = selectedBrushSurfaces[i].brush;
                    int      surfaceIndex = selectedBrushSurfaces[i].surfaceIndex;
                    int      texGenIndex  = brush.shape.surfaces[surfaceIndex].TexGenIndex;

                    brush.shape.texGens[texGenIndex].RotationAngle = (brush.shape.texGens[texGenIndex].RotationAngle + angle) % MAX_ANGLE;
                }
            }
        }

        public static void JustifyLayout(SelectedBrushSurface[] selectedBrushSurfaces, int justifyX, int justifyY)
        {
            using(new UndoGroup(selectedBrushSurfaces, "Justifying surface in XY direction"))
            {
                for(int i = 0; i < selectedBrushSurfaces.Length; i++)
                {
                    CSGBrush brush = selectedBrushSurfaces[i].brush;

                    if(brush.childData == null || brush.childData.modelTransform == null)
                    {
                        continue;
                    }

                    Transform modelTransform          = brush.childData.modelTransform;
                    Matrix4x4 modelLocalToWorldMatrix = modelTransform.localToWorldMatrix;
                    Shape     shape                   = brush.shape;
                    int       surfaceIndex            = selectedBrushSurfaces[i].surfaceIndex;
                    int       texGenIndex             = brush.shape.surfaces[surfaceIndex].TexGenIndex;

                    GetSurfaceMinMaxTexCoords(brush.brushNodeID, surfaceIndex, modelLocalToWorldMatrix, out Vector2 minCoordinate, out Vector2 maxCoordinate);

                    switch(justifyX)
                    {
                        case 1:
                        {
                            shape.texGens[texGenIndex].Translation.x -= maxCoordinate.x - 1;

                            break;
                        }
                        case 0:
                        {
                            shape.texGens[texGenIndex].Translation.x -=
                                (maxCoordinate.x + minCoordinate.x) * 0.5f + 0.5f;

                            break;
                        }
                        case -1:
                        {
                            shape.texGens[texGenIndex].Translation.x -= minCoordinate.x;

                            break;
                        }
                    }

                    switch(justifyY)
                    {
                        case -1:
                        {
                            shape.texGens[texGenIndex].Translation.y -= maxCoordinate.y - 1;

                            break;
                        }
                        case 0:
                        {
                            shape.texGens[texGenIndex].Translation.y -=
                                (maxCoordinate.y + minCoordinate.y) * 0.5f + 0.5f;

                            break;
                        }
                        case 1:
                        {
                            shape.texGens[texGenIndex].Translation.y -= minCoordinate.y;

                            break;
                        }
                    }
                }
            }
        }

        public static void JustifyLayoutX(SelectedBrushSurface[] selectedBrushSurfaces, int justifyX)
        {
            using(new UndoGroup(selectedBrushSurfaces, "Justifying surface in X direction"))
            {
                for(int i = 0; i < selectedBrushSurfaces.Length; i++)
                {
                    CSGBrush brush = selectedBrushSurfaces[i].brush;

                    if(brush.childData == null || brush.childData.modelTransform == null)
                    {
                        continue;
                    }

                    Transform modelTransform          = brush.childData.modelTransform;
                    Matrix4x4 modelLocalToWorldMatrix = modelTransform.localToWorldMatrix;
                    Shape     shape                   = brush.shape;
                    int       surfaceIndex            = selectedBrushSurfaces[i].surfaceIndex;
                    int       texGenIndex             = brush.shape.surfaces[surfaceIndex].TexGenIndex;

                    GetSurfaceMinMaxTexCoords(brush.brushNodeID, surfaceIndex, modelLocalToWorldMatrix, out Vector2 minCoordinate, out Vector2 maxCoordinate);

                    switch(justifyX)
                    {
                        case 1:
                        {
                            shape.texGens[texGenIndex].Translation.x -= maxCoordinate.x - 1;

                            break;
                        }
                        case 0:
                        {
                            shape.texGens[texGenIndex].Translation.x -=
                                (maxCoordinate.x + minCoordinate.x) * 0.5f + 0.5f;

                            break;
                        }
                        case -1:
                        {
                            shape.texGens[texGenIndex].Translation.x -= minCoordinate.x;

                            break;
                        }
                    }
                }
            }
        }

        public static void JustifyLayoutY(SelectedBrushSurface[] selectedBrushSurfaces, int justifyY)
        {
            using(new UndoGroup(selectedBrushSurfaces, "Justifying surface in Y direction"))
            {
                for(int i = 0; i < selectedBrushSurfaces.Length; i++)
                {
                    CSGBrush brush = selectedBrushSurfaces[i].brush;

                    if(brush.childData == null || brush.childData.modelTransform == null)
                    {
                        continue;
                    }

                    Transform modelTransform          = brush.childData.modelTransform;
                    Matrix4x4 modelLocalToWorldMatrix = modelTransform.localToWorldMatrix;
                    Shape     shape                   = brush.shape;
                    int       surfaceIndex            = selectedBrushSurfaces[i].surfaceIndex;
                    int       texGenIndex             = brush.shape.surfaces[surfaceIndex].TexGenIndex;

                    GetSurfaceMinMaxTexCoords(brush.brushNodeID, surfaceIndex, modelLocalToWorldMatrix, out Vector2 minCoordinate, out Vector2 maxCoordinate);

                    switch(justifyY)
                    {
                        case -1:
                        {
                            shape.texGens[texGenIndex].Translation.y -= maxCoordinate.y - 1;

                            break;
                        }
                        case 0:
                        {
                            shape.texGens[texGenIndex].Translation.y -= (maxCoordinate.y + minCoordinate.y) * 0.5f + 0.5f;

                            break;
                        }
                        case 1:
                        {
                            shape.texGens[texGenIndex].Translation.y -= minCoordinate.y;

                            break;
                        }
                    }
                }
            }
        }

        public static void FitSurface(SelectedBrushSurface[] selectedBrushSurfaces)
        {
            using(new UndoGroup(selectedBrushSurfaces, "Fitting surface texture coordinates"))
            {
                for(int i = 0; i < selectedBrushSurfaces.Length; i++)
                {
                    CSGBrush brush = selectedBrushSurfaces[i].brush;

                    if(brush.childData == null || brush.childData.modelTransform == null)
                    {
                        continue;
                    }

                    Transform modelTransform          = brush.childData.modelTransform;
                    Matrix4x4 modelLocalToWorldMatrix = modelTransform.localToWorldMatrix;
                    Shape     shape                   = brush.shape;
                    int       surfaceIndex            = selectedBrushSurfaces[i].surfaceIndex;
                    int       texGenIndex             = brush.shape.surfaces[surfaceIndex].TexGenIndex;

                    FitSurface(brush.brushNodeID, surfaceIndex, modelLocalToWorldMatrix, ref shape.texGens[texGenIndex]);
                }
            }
        }

        public static void FitSurfaceX(SelectedBrushSurface[] selectedBrushSurfaces)
        {
            using(new UndoGroup(selectedBrushSurfaces, "Fitting surface texture coordinates in X direction"))
            {
                for(int i = 0; i < selectedBrushSurfaces.Length; i++)
                {
                    CSGBrush brush = selectedBrushSurfaces[i].brush;

                    if(brush.childData == null || brush.childData.modelTransform == null)
                    {
                        continue;
                    }

                    Transform modelTransform          = brush.childData.modelTransform;
                    Matrix4x4 modelLocalToWorldMatrix = modelTransform.localToWorldMatrix;
                    Shape     shape                   = brush.shape;
                    int       surfaceIndex            = selectedBrushSurfaces[i].surfaceIndex;
                    int       texGenIndex             = brush.shape.surfaces[surfaceIndex].TexGenIndex;

                    FitSurfaceX(brush.brushNodeID, surfaceIndex, modelLocalToWorldMatrix, ref shape.texGens[texGenIndex]);
                }
            }
        }

        public static void FitSurfaceY(SelectedBrushSurface[] selectedBrushSurfaces)
        {
            using(new UndoGroup(selectedBrushSurfaces, "Fitting surface texture coordinates in Y direction"))
            {
                for(int i = 0; i < selectedBrushSurfaces.Length; i++)
                {
                    CSGBrush brush = selectedBrushSurfaces[i].brush;

                    if(brush.childData == null || brush.childData.modelTransform == null)
                    {
                        continue;
                    }

                    Transform modelTransform          = brush.childData.modelTransform;
                    Matrix4x4 modelLocalToWorldMatrix = modelTransform.localToWorldMatrix;
                    Shape     shape                   = brush.shape;
                    int       surfaceIndex            = selectedBrushSurfaces[i].surfaceIndex;
                    int       texGenIndex             = brush.shape.surfaces[surfaceIndex].TexGenIndex;

                    FitSurfaceY(brush.brushNodeID, surfaceIndex, modelLocalToWorldMatrix, ref shape.texGens[texGenIndex]);
                }
            }
        }

        public static void ResetSurface(SelectedBrushSurface[] selectedBrushSurfaces)
        {
            using(new UndoGroup(selectedBrushSurfaces, "Resetting surface texture coordinates"))
            {
                for(int i = 0; i < selectedBrushSurfaces.Length; i++)
                {
                    CSGBrush brush        = selectedBrushSurfaces[i].brush;
                    Shape    shape        = brush.shape;
                    int      surfaceIndex = selectedBrushSurfaces[i].surfaceIndex;
                    int      texGenIndex  = brush.shape.surfaces[surfaceIndex].TexGenIndex;

                    shape.texGens[texGenIndex].RotationAngle = 0;
                    shape.texGens[texGenIndex].Translation   = MathConstants.zeroVector2;
                    shape.texGens[texGenIndex].Scale         = MathConstants.oneVector2;
                }
            }
        }

        public static void ResetSurfaceX(SelectedBrushSurface[] selectedBrushSurfaces)
        {
            using(new UndoGroup(selectedBrushSurfaces, "Resetting surface texture coordinates in X direction"))
            {
                for(int i = 0; i < selectedBrushSurfaces.Length; i++)
                {
                    CSGBrush brush        = selectedBrushSurfaces[i].brush;
                    Shape    shape        = brush.shape;
                    int      surfaceIndex = selectedBrushSurfaces[i].surfaceIndex;
                    int      texGenIndex  = brush.shape.surfaces[surfaceIndex].TexGenIndex;

                    shape.texGens[texGenIndex].Translation.x = 0;
                    shape.texGens[texGenIndex].Scale.x       = 1;
                }
            }
        }

        public static void ResetSurfaceY(SelectedBrushSurface[] selectedBrushSurfaces)
        {
            using(new UndoGroup(selectedBrushSurfaces, "Resetting surface texture coordinates in Y direction"))
            {
                for(int i = 0; i < selectedBrushSurfaces.Length; i++)
                {
                    CSGBrush brush        = selectedBrushSurfaces[i].brush;
                    Shape    shape        = brush.shape;
                    int      surfaceIndex = selectedBrushSurfaces[i].surfaceIndex;
                    int      texGenIndex  = brush.shape.surfaces[surfaceIndex].TexGenIndex;

                    shape.texGens[texGenIndex].Translation.y = 0;
                    shape.texGens[texGenIndex].Scale.y       = 1;
                }
            }
        }

        private static void CopyLastMaterialInternal(CSGBrush dstBrush, int dstSurfaceIndex, CSGBrush srcBrush, int srcSurfaceIndex)
        {
            Shape srcShape = srcBrush.shape;
            Shape dstShape = dstBrush.shape;

            TexGen[]      srcTexGens     = srcShape.texGens;
            TexGenFlags[] srcTexGenFlags = srcShape.texGenFlags;
            Surface[]     srcSurfaces    = srcShape.surfaces;
            TexGen[]      dstTexGens     = dstShape.texGens;
            TexGenFlags[] dstTexGenFlags = dstShape.texGenFlags;
            Surface[]     dstSurfaces    = dstShape.surfaces;

            int dstTexGenIndex = dstShape.surfaces[dstSurfaceIndex].TexGenIndex;
            int srcTexGenIndex = srcShape.surfaces[srcSurfaceIndex].TexGenIndex;

            Vector3 srcBrushScale = srcBrush.transform.lossyScale;
            Vector3 dstBrushScale = dstBrush.transform.lossyScale;

            Matrix4x4 srcBrushFromWorld = srcBrush.transform.worldToLocalMatrix;
            Matrix4x4 dstBrushFromWorld = dstBrush.transform.worldToLocalMatrix;

            Matrix4x4 srcWorldFromModel = srcBrush.childData.modelTransform.localToWorldMatrix;
            Matrix4x4 srcModelFromWorld = srcBrush.childData.modelTransform.worldToLocalMatrix;

            Matrix4x4 dstWorldFromModel = dstBrush.childData.modelTransform.localToWorldMatrix;
            Matrix4x4 dstModelFromWorld = dstBrush.childData.modelTransform.worldToLocalMatrix;

            Matrix4x4 srcBrushFromModel = srcBrushFromWorld * srcWorldFromModel;
            Matrix4x4 dstBrushFromModel = dstBrushFromWorld * dstWorldFromModel;

            CSGPlane srcLocalPlane = srcShape.surfaces[srcSurfaceIndex].Plane;
            CSGPlane dstLocalPlane = dstShape.surfaces[dstSurfaceIndex].Plane;

            CSGPlane srcWorldPlane = GeometryUtility.InverseTransformPlane(srcBrushFromWorld, srcLocalPlane);
            CSGPlane dstWorldPlane = GeometryUtility.InverseTransformPlane(dstBrushFromWorld, dstLocalPlane);

            Vector3 dstNormal = dstWorldPlane.Normal;
            Vector3 srcNormal = srcWorldPlane.Normal;

            Vector3 srcWorldPoint1, srcWorldPoint2;
            Vector3 dstWorldPoint1, dstWorldPoint2;
            Vector3 edgeDirection = Vector3.Cross(srcNormal, dstNormal);
            float   det           = edgeDirection.sqrMagnitude;

            if(det < MathConstants.ALIGNMENT_TEST_EPSILON)
            {
                Vector3 tangent = GeometryUtility.CalculateTangent(srcNormal);
                // Find 2 pairs of points on each plane, assumes planes are perfectly aligned
                srcWorldPoint1 = srcWorldPlane.PointOnPlane;
                srcWorldPoint2 = GeometryUtility.ProjectPointOnPlane(srcWorldPlane, srcWorldPoint1 + tangent);

                dstWorldPoint1 = GeometryUtility.ProjectPointOnPlane(dstWorldPlane, srcWorldPoint1);
                dstWorldPoint2 = GeometryUtility.ProjectPointOnPlane(dstWorldPlane, srcWorldPoint2);
            }
            else
            {
                // Find two points on the intersection between two planes
                srcWorldPoint1 = (Vector3.Cross(edgeDirection, srcNormal) * dstWorldPlane.d + Vector3.Cross(dstNormal, edgeDirection) * srcWorldPlane.d) / det;

                srcWorldPoint2 = srcWorldPoint1 + edgeDirection.normalized;

                dstWorldPoint1 = srcWorldPoint1;
                dstWorldPoint2 = srcWorldPoint2;
            }

            bool flipX = Vector3.Dot(dstNormal, srcNormal) < 0;

            Vector3 srcModelPoint1 = srcModelFromWorld.MultiplyPoint(srcWorldPoint1);
            Vector3 srcModelPoint2 = srcModelFromWorld.MultiplyPoint(srcWorldPoint2);

            Vector3 dstModelPoint1 = dstModelFromWorld.MultiplyPoint(dstWorldPoint1);
            Vector3 dstModelPoint2 = dstModelFromWorld.MultiplyPoint(dstWorldPoint2);

            Vector3 scale = dstBrushScale;
            scale.x /= srcBrushScale.x;

            if(float.IsNaN(scale.x) || float.IsInfinity(scale.x))
            {
                scale.x = 1;
            }

            scale.y /= srcBrushScale.y;

            if(float.IsNaN(scale.y) || float.IsInfinity(scale.y))
            {
                scale.y = 1;
            }

            scale.z /= srcBrushScale.z;

            if(float.IsNaN(scale.z) || float.IsInfinity(scale.z))
            {
                scale.z = 1;
            }

            if(AlignTextureSpaces(
                   srcBrushFromModel, srcTexGens[srcTexGenIndex], srcTexGenFlags[srcTexGenIndex]
                 , ref srcSurfaces[srcSurfaceIndex], srcModelPoint1, srcModelPoint2,
                   dstBrushFromModel, ref dstTexGens[dstTexGenIndex], dstTexGenFlags[dstTexGenIndex]
                 , ref dstSurfaces[dstSurfaceIndex], dstModelPoint1, dstModelPoint2, flipX, scale
               ))
            {
                if(dstTexGens.Length != dstTexGenFlags.Length)
                {
                    Debug.LogWarning("brush.Shape.TexGens.Length != brush.Shape.TexGenFlags.Length");
                }
                else
                {
                    // NOTE: Need to copy array for undo to capture the change
                    dstShape.texGens     = dstTexGens.ToArray();
                    dstShape.texGenFlags = dstTexGenFlags.ToArray();
                    dstShape.surfaces    = dstSurfaces.ToArray();
                    InternalCSGModelManager.SetBrushMeshSurfaces(dstBrush);
                }
            }
        }

        public static bool CopyLastMaterial(CSGBrush dstBrush, int dstSurfaceIndex, CSGBrush srcBrush, int srcSurfaceIndex, bool registerUndo = true)
        {
            if(srcSurfaceIndex < 0 || srcSurfaceIndex >= srcBrush.shape.surfaces.Length || !srcBrush)
            {
                return false;
            }

            if(srcBrush == dstBrush && srcSurfaceIndex == dstSurfaceIndex)
            {
                return false;
            }

            Shape srcShape       = srcBrush.shape;
            Shape dstShape       = dstBrush.shape;
            int   srcTexGenIndex = srcShape.surfaces[srcSurfaceIndex].TexGenIndex;
            int   dstTexGenIndex = dstShape.surfaces[dstSurfaceIndex].TexGenIndex;

            if(srcTexGenIndex < 0 || srcTexGenIndex >= srcBrush.shape.texGens.Length)
            {
                return false;
            }

            if(registerUndo)
            {
                SelectedBrushSurface[] dstBrushSurface = { new SelectedBrushSurface(dstBrush, dstSurfaceIndex) };
                Undo.IncrementCurrentGroup();

                using(new UndoGroup(dstBrushSurface, "Copy materials", true))
                {
                    dstBrush.shape.texGens[dstTexGenIndex].RenderMaterial = srcShape.texGens[srcTexGenIndex].RenderMaterial;

                    CopyLastMaterialInternal(dstBrush, dstSurfaceIndex, srcBrush, srcSurfaceIndex);
                }
            }
            else
            {
                dstBrush.shape.texGens[dstTexGenIndex].RenderMaterial = srcShape.texGens[srcTexGenIndex].RenderMaterial;

                CopyLastMaterialInternal(dstBrush, dstSurfaceIndex, srcBrush, srcSurfaceIndex);
            }

            return true;
        }

        public static bool RotateSurfaces(SelectedBrushSurface[] selectedSurfaces, RotationCircle rotationCircle)
        {
            if(selectedSurfaces.Length == 0)
            {
                return false;
            }

            Dictionary<CSGBrush, List<int>> brushSurfs = new Dictionary<CSGBrush, List<int>>();

            for(int s = 0; s < selectedSurfaces.Length; s++)
            {
                CSGBrush brush        = selectedSurfaces[s].brush;
                int      surfaceIndex = selectedSurfaces[s].surfaceIndex;

                if(!brushSurfs.TryGetValue(brush, out List<int> indices))
                {
                    indices = new List<int>();
                    brushSurfs.Add(brush, indices);
                }

                indices.Add(surfaceIndex);
            }

            bool modified = false;

            foreach(KeyValuePair<CSGBrush, List<int>> pair in brushSurfs)
            {
                CSGBrush  brush          = pair.Key;
                List<int> surfaceIndices = pair.Value;

                if(brush.childData == null || brush.childData.modelTransform == null)
                {
                    continue;
                }

                Vector3 brushLocalNormal = brush.hierarchyItem.transform.InverseTransformVector(rotationCircle.rotateSurfaceNormal);

                Shape shape = brush.shape;

                for(int s = 0; s < surfaceIndices.Count; s++)
                {
                    int surfaceIndex = surfaceIndices[s];

                    if(Mathf.Abs(Vector3.Dot(brushLocalNormal, shape.surfaces[surfaceIndex].Plane.Normal)) > MathConstants.ANGLE_EPSILON)
                    {
                        int texGenIndex = shape.surfaces[surfaceIndex].TexGenIndex;

                        RotateTextureCoordAroundWorldPoint(brush, surfaceIndex, rotationCircle.rotateCenterPoint, rotationCircle.rotateCurrentSnappedAngle);

                        shape.texGens[texGenIndex].RotationAngle %= 360.0f;

                        modified = true;
                    }
                }
            }

            return modified;
        }

        private static readonly Dictionary<CSGBrush, List<int>> brushSurfaces = new Dictionary<CSGBrush, List<int>>();

        public static bool TranslateSurfaces(SelectedBrushSurface[] selectedSurfaces, Vector3 oldWorldPosition, Vector3 newWorldPosition)
        {
            if(selectedSurfaces == null || selectedSurfaces.Length == 0)
            {
                return false;
            }

            brushSurfaces.Clear();

            for(int s = 0; s < selectedSurfaces.Length; s++)
            {
                CSGBrush brush        = selectedSurfaces[s].brush;
                int      surfaceIndex = selectedSurfaces[s].surfaceIndex;

                if(!brushSurfaces.TryGetValue(brush, out List<int> indices))
                {
                    indices = new List<int>(brush.shape.surfaces.Length);
                    brushSurfaces.Add(brush, indices);
                }

                indices.Add(surfaceIndex);
            }

            bool worldModified = false;

            foreach(KeyValuePair<CSGBrush, List<int>> pair in brushSurfaces)
            {
                CSGBrush  brush          = pair.Key;
                List<int> surfaceIndices = pair.Value;

                if(brush.childData == null || brush.childData.modelTransform == null)
                {
                    continue;
                }

                Vector3     point1            = brush.hierarchyItem.transform.InverseTransformPoint(oldWorldPosition);
                Vector3     point2            = brush.hierarchyItem.transform.InverseTransformPoint(newWorldPosition);
                ControlMesh targetControlMesh = brush.controlMesh;
                Shape       targetShape       = brush.shape;

                bool brushModified = false;

                for(int s = 0; s < surfaceIndices.Count; s++)
                {
                    int surfaceIndex = surfaceIndices[s];
                    int texGenIndex  = targetShape.surfaces[surfaceIndex].TexGenIndex;

                    brushModified = TranslateTextureCoordInLocalSpace(ref targetShape.texGens[texGenIndex], ref targetShape.surfaces[surfaceIndex], point1, point2) || brushModified;
                }

                if(brushModified)
                {
                    targetControlMesh.SetDirty();
                    worldModified = true;
                }
            }

            return worldModified;
        }

        public static bool TranslateSurfacesInWorldSpace(CSGBrush[] brushes, Vector3 offset)
        {
            if(brushes == null || brushes.Length == 0)
            {
                return false;
            }

            bool worldModified = false;

            for(int b = 0; b < brushes.Length; b++)
            {
                CSGBrush brush = brushes[b];

                if(brush.childData == null || brush.childData.modelTransform == null)
                {
                    continue;
                }

                Vector3     point1      = brush.hierarchyItem.transform.InverseTransformPoint(MathConstants.zeroVector3);
                Vector3     point2      = brush.hierarchyItem.transform.InverseTransformPoint(offset);
                ControlMesh controlMesh = brush.controlMesh;
                Shape       shape       = brush.shape;

                bool brushModified = false;

                for(int s = 0; s < shape.surfaces.Length; s++)
                {
                    int texGenIndex = shape.surfaces[s].TexGenIndex;

                    if((shape.texGenFlags[texGenIndex] & TexGenFlags.WorldSpaceTexture) != TexGenFlags.WorldSpaceTexture)
                    {
                        brushModified = TranslateTextureCoordInLocalSpace(ref shape.texGens[texGenIndex], ref shape.surfaces[s], point1, point2) || brushModified;
                    }
                }

                if(brushModified)
                {
                    controlMesh.SetDirty();
                    worldModified = true;
                }
            }

            return worldModified;
        }

        public static bool TranslateSurfacesInWorldSpace(CSGBrush brush, Vector3 offset)
        {
            if(!brush)
            {
                return false;
            }

            bool worldModified = false;

            if(brush.childData == null || brush.childData.modelTransform == null)
            {
                return false;
            }

            Vector3     point1      = brush.hierarchyItem.transform.InverseTransformPoint(MathConstants.zeroVector3);
            Vector3     point2      = brush.hierarchyItem.transform.InverseTransformPoint(offset);
            ControlMesh controlMesh = brush.controlMesh;
            Shape       shape       = brush.shape;

            bool brushModified = false;

            for(int s = 0; s < shape.surfaces.Length; s++)
            {
                int texGenIndex = shape.surfaces[s].TexGenIndex;

                if((shape.texGenFlags[texGenIndex] & TexGenFlags.WorldSpaceTexture) != TexGenFlags.WorldSpaceTexture)
                {
                    brushModified = TranslateTextureCoordInLocalSpace(ref shape.texGens[texGenIndex], ref shape.surfaces[s], point1, point2) || brushModified;
                }
            }

            if(brushModified)
            {
                controlMesh.SetDirty();
                worldModified = true;
            }

            return worldModified;
        }

#region FindUnusedSmoothingGroupIndex

        // TODO: optimize this
        internal static HashSet<uint> GetUsedSmoothingGroupIndices()
        {
            HashSet<uint> foundGroupIndices = new HashSet<uint>();

            for(int i = 0; i < InternalCSGModelManager.Brushes.Count; i++)
            {
                CSGBrush brush   = InternalCSGModelManager.Brushes[i];
                TexGen[] texGens = brush.shape.texGens;

                for(int t = 0; t < texGens.Length; t++)
                {
                    if(texGens[t].SmoothingGroup == 0)
                    {
                        continue;
                    }

                    foundGroupIndices.Add(texGens[t].SmoothingGroup);
                }
            }

            return foundGroupIndices;
        }

        // TODO: optimize this
        internal static uint FindUnusedSmoothingGroupIndex(HashSet<uint> foundGroupIndices)
        {
            if(foundGroupIndices.Count == 0)
            {
                return 1;
            }

            List<uint> list = new List<uint>(foundGroupIndices);
            list.Sort();

            if(list[0] != 1)
            {
                return 1;
            }

            if(list.Count == list[list.Count - 1])
            {
                return list[list.Count - 1] + 1;
            }

            uint prevIndex = 1;

            for(int i = 1; i < list.Count; i++)
            {
                uint diff = list[i] - prevIndex;

                if(diff > 1)
                {
                    return prevIndex + 1;
                }

                prevIndex = list[i];
            }

            return list[list.Count - 1] + 1;
        }

        public static uint FindUnusedSmoothingGroupIndex()
        {
            HashSet<uint> foundGroupIndices = GetUsedSmoothingGroupIndices();

            return FindUnusedSmoothingGroupIndex(foundGroupIndices);
        }

#endregion

#region Convert..xx..Coord

        public static bool FitSurface(int brushNodeId, int surfaceIndex, Matrix4x4 modelLocalToWorldMatrix, ref TexGen surfaceTexGen)
        {
            if(!GetSurfaceMinMaxTexCoords(brushNodeId, surfaceIndex, modelLocalToWorldMatrix, out Vector2 min, out Vector2 max))
            {
                return false;
            }

            float sizeX = max.x - min.x;

            if(sizeX < MathConstants.EQUALITY_EPSILON)
            {
                sizeX = 1.0f;
            }

            float sizeY = max.y - min.y;

            if(sizeY < MathConstants.EQUALITY_EPSILON)
            {
                sizeY = 1.0f;
            }

            Vector2 prevScale       = surfaceTexGen.Scale;
            Vector2 prevTranslation = surfaceTexGen.Translation;

            Vector2 scale       = new Vector2(prevScale.x / sizeX, prevScale.y / sizeY);
            Vector2 translation = new Vector2((prevTranslation.x - min.x) / sizeX, (prevTranslation.y - min.y) / sizeY);

            surfaceTexGen.RotationAngle = 0;
            surfaceTexGen.Scale         = scale;
            surfaceTexGen.Translation   = translation;

            return true;
        }

        public static bool FitSurfaceX(int brushNodeId, int surfaceIndex, Matrix4x4 modelLocalToWorldMatrix, ref TexGen surfaceTexGen)
        {
            if(!GetSurfaceMinMaxTexCoords(brushNodeId, surfaceIndex, modelLocalToWorldMatrix, out Vector2 min, out Vector2 max))
            {
                return false;
            }

            float sizeX = max.x - min.x;

            if(sizeX < MathConstants.EQUALITY_EPSILON)
            {
                sizeX = 1.0f;
            }

            Vector2 prevScale       = surfaceTexGen.Scale;
            Vector2 prevTranslation = surfaceTexGen.Translation;

            surfaceTexGen.Scale.x       = prevScale.x / sizeX;
            surfaceTexGen.Translation.x = (prevTranslation.x - min.x) / sizeX;

            return true;
        }

        public static bool FitSurfaceY(int brushNodeId, int surfaceIndex, Matrix4x4 modelLocalToWorldMatrix, ref TexGen surfaceTexGen)
        {
            if(!GetSurfaceMinMaxTexCoords(brushNodeId, surfaceIndex, modelLocalToWorldMatrix, out Vector2 min, out Vector2 max))
            {
                return false;
            }

            float sizeY = max.y - min.y;

            if(sizeY < MathConstants.EQUALITY_EPSILON)
            {
                sizeY = 1.0f;
            }

            Vector2 prevScale       = surfaceTexGen.Scale;
            Vector2 prevTranslation = surfaceTexGen.Translation;

            surfaceTexGen.Scale.y       = prevScale.y / sizeY;
            surfaceTexGen.Translation.y = (prevTranslation.y - min.y) / sizeY;

            return true;
        }

        public static bool GetSurfaceMinMaxTexCoords(int brushNodeId, int surfaceIndex, Matrix4x4 modelLocalToWorldMatrix, out Vector2 minTextureCoordinate, out Vector2 maxTextureCoordinate)
        {
            if(brushNodeId != CSGNode.INVALID_NODE_ID && surfaceIndex >= 0 &&
               InternalCSGModelManager.External.getSurfaceMinMaxTexCoords != null &&
               InternalCSGModelManager.External.getSurfaceMinMaxTexCoords(brushNodeId, surfaceIndex, modelLocalToWorldMatrix, out minTextureCoordinate, out maxTextureCoordinate))
            {
                return true;
            }

            minTextureCoordinate = MathConstants.zeroVector2;
            maxTextureCoordinate = MathConstants.zeroVector2;

            return false;
        }

        public static Vector2 ConvertModelToTextureSpace(CSGBrush brush, int surfaceIndex, Vector3 worldCoordinate)
        {
            if(brush.brushNodeID == CSGNode.INVALID_NODE_ID || brush.shape == null ||
               surfaceIndex < 0 || surfaceIndex >= brush.shape.surfaces.Length ||
               InternalCSGModelManager.External.convertModelToTextureSpace == null ||
               brush.childData == null ||
               brush.childData.modelTransform == null)
            {
                return MathConstants.zeroVector2;
            }

            Transform modelTransform    = brush.childData.modelTransform;
            Matrix4x4 modelToWorldSpace = modelTransform.localToWorldMatrix;

            return InternalCSGModelManager.External.convertModelToTextureSpace(brush.brushNodeID, surfaceIndex, modelToWorldSpace, worldCoordinate, out Vector2 texcoordCoordinate)
                ? texcoordCoordinate
                : MathConstants.zeroVector2;
        }

        public static Vector3 ConvertTextureToModelSpace(CSGBrush brush, int surfaceIndex, Vector2 texcoordCoordinate)
        {
            if(brush == null || brush.brushNodeID == CSGNode.INVALID_NODE_ID || brush.shape == null || surfaceIndex < 0 || surfaceIndex >= brush.shape.surfaces.Length || InternalCSGModelManager.External.convertTextureToModelSpace == null)
            {
                return MathConstants.zeroVector3;
            }

            if(brush.childData == null || brush.childData.modelTransform == null)
            {
                return MathConstants.zeroVector2;
            }

            Transform modelTransform    = brush.childData.modelTransform;
            Matrix4x4 modelToWorldSpace = modelTransform.localToWorldMatrix;

            Vector3 worldCoordinate = MathConstants.zeroVector3;

            return !InternalCSGModelManager.External.convertTextureToModelSpace(
                brush.brushNodeID,
                surfaceIndex,
                texcoordCoordinate.x,
                texcoordCoordinate.y,
                ref modelToWorldSpace,
                ref worldCoordinate.x,
                ref worldCoordinate.y,
                ref worldCoordinate.z
            )
                ? MathConstants.zeroVector3
                : worldCoordinate;
        }

        public static Matrix4x4 GenerateModelSpaceToPlaneSpaceMatrix(Surface surface, TexGenFlags texGenFlags, Matrix4x4 localFromModel)
        {
            Matrix4x4 planeFromLocal = surface.GenerateLocalBrushSpaceToPlaneSpaceMatrix();

            return (texGenFlags & TexGenFlags.WorldSpaceTexture) == TexGenFlags.None
                ? planeFromLocal * localFromModel
                : GeometryUtility.Multiply3x3To4x4(planeFromLocal, localFromModel);
        }

        public static Matrix4x4 GetLocalToTextureSpaceMatrix(TexGen texGen, Surface surface)
        {
            Matrix4x4 planeFromLocalSpace   = surface.GenerateLocalBrushSpaceToPlaneSpaceMatrix();
            Matrix4x4 textureFromPlaneSpace = texGen.GeneratePlaneSpaceToTextureSpaceMatrix();
            Matrix4x4 textureFromLocalSpace = textureFromPlaneSpace * planeFromLocalSpace;

            return textureFromLocalSpace;
        }

        public static Matrix4x4 GetModelToTextureSpaceMatrix(TexGen texGen, TexGenFlags texGenFlags, Surface surface, Matrix4x4 localFromModel)
        {
            Matrix4x4 planeFromWorldSpace   = GenerateModelSpaceToPlaneSpaceMatrix(surface, texGenFlags, localFromModel);
            Matrix4x4 textureFromPlaneSpace = texGen.GeneratePlaneSpaceToTextureSpaceMatrix();
            Matrix4x4 textureFromWorldSpace = textureFromPlaneSpace * planeFromWorldSpace;

            return textureFromWorldSpace;
        }

        public static bool ConvertModelToTextureCoord(ref TexGen texGen, TexGenFlags texGenFlags, ref Surface surface, Matrix4x4 localFromModel, Vector3 localPoint, out Vector2 textureSpacePoint)
        {
            Matrix4x4 srcLocalSpaceToTextureSpace = GetModelToTextureSpaceMatrix(texGen, texGenFlags, surface, localFromModel);

            Vector3 textureSpacePoint3D = srcLocalSpaceToTextureSpace.MultiplyPoint(localPoint);
            textureSpacePoint = new Vector2(textureSpacePoint3D.x, textureSpacePoint3D.y);

            return true;
        }

        public static bool RotateTextureCoordAroundWorldPoint(CSGBrush brush, int surfaceIndex, Vector3 worldCenter, float angle)
        {
            Shape     shape          = brush.shape;
            Matrix4x4 modelFromWorld = brush.childData.modelTransform.worldToLocalMatrix;
            Matrix4x4 worldFromModel = brush.childData.modelTransform.localToWorldMatrix;
            Matrix4x4 localFromWorld = brush.transform.worldToLocalMatrix;
            Matrix4x4 localFromModel = localFromWorld * worldFromModel;
            int       texgenIndex    = shape.surfaces[surfaceIndex].TexGenIndex;

            Vector3 modelCenter = modelFromWorld.MultiplyPoint(worldCenter);

            if(!ConvertModelToTextureCoord(ref shape.texGens[texgenIndex], shape.texGenFlags[texgenIndex], ref shape.surfaces[surfaceIndex], localFromModel, modelCenter, out Vector2 originalTextureCoordinate))
            {
                return false;
            }

            GeometryUtility.CalculateTangents(shape.surfaces[surfaceIndex].Plane.Normal, out Vector3 tangent, out Vector3 _);

            if(Vector3.Dot(shape.surfaces[surfaceIndex].Tangent, tangent) < 0)
            {
                shape.texGens[texgenIndex].RotationAngle += angle;
            }
            else
            {
                shape.texGens[texgenIndex].RotationAngle -= angle;
            }

            if(!ConvertModelToTextureCoord(ref shape.texGens[texgenIndex], shape.texGenFlags[texgenIndex], ref shape.surfaces[surfaceIndex], localFromModel, modelCenter, out Vector2 newTextureCoordinate))
            {
                return false;
            }

            shape.texGens[texgenIndex].Translation += originalTextureCoordinate - newTextureCoordinate;

            return true;
        }

        public static bool TranslateTextureCoordInLocalSpace(ref TexGen texGen, ref Surface surface, Vector3 from, Vector3 to)
        {
            Matrix4x4 srcLocalSpaceToTextureSpace = GetLocalToTextureSpaceMatrix(texGen, surface);

            Vector3 textureSpaceFrom = srcLocalSpaceToTextureSpace.MultiplyPoint(from);
            Vector3 textureSpaceTo   = srcLocalSpaceToTextureSpace.MultiplyPoint(to);

            Vector2 delta;
            delta.x = textureSpaceFrom.x - textureSpaceTo.x;
            delta.y = textureSpaceFrom.y - textureSpaceTo.y;

            texGen.Translation.x += delta.x;
            texGen.Translation.y += delta.y;

            return true;
        }

        public static bool AlignTextureSpaces(Matrix4x4 srcBrushFromModel, TexGen srcTexGen, TexGenFlags srcTexGenFlags, ref Surface srcSurface, Vector3 srcModelPoint1, Vector3 srcModelPoint2, Matrix4x4 dstBrushFromModel, ref TexGen dstTexGen, TexGenFlags dstTexGenFlags, ref Surface dstSurface, Vector3 dstModelPoint1, Vector3 dstModelPoint2, bool flipX, Vector3 scale)
        {
            Matrix4x4 srcPlaneFromModel = GenerateModelSpaceToPlaneSpaceMatrix(srcSurface, srcTexGenFlags, srcBrushFromModel);
            Matrix4x4 dstPlaneFromModel = GenerateModelSpaceToPlaneSpaceMatrix(dstSurface, dstTexGenFlags, dstBrushFromModel);

            Matrix4x4 srcTextureFromPlane = srcTexGen.GeneratePlaneSpaceToTextureSpaceMatrix();
            Matrix4x4 srcTextureFromModel = srcTextureFromPlane * srcPlaneFromModel;

            Vector2 srcTexcoord1 = srcTextureFromModel.MultiplyPoint(srcModelPoint1);

            // Reset destination shape to simplify calculations
            dstTexGen.Translation   = Vector2.zero;
            dstTexGen.RotationAngle = 0;
            dstTexGen.Scale         = srcTexGen.Scale;

            if(flipX)
            {
                dstTexGen.Scale.x = -dstTexGen.Scale.x;
            }

            // Align dstTexcoord1 with srcTexcoord1 on dstTexGen
            {
                dstTexGen.Translation = Vector2.zero;
                Matrix4x4 dstTextureFromPlane = dstTexGen.GeneratePlaneSpaceToTextureSpaceMatrix();
                Matrix4x4 dstTextureFromModel = dstTextureFromPlane * dstPlaneFromModel;

                Vector2 dstTexcoord1 = dstTextureFromModel.MultiplyPoint(dstModelPoint1);
                Vector2 offset       = srcTexcoord1 - dstTexcoord1;
                dstTexGen.Translation = offset;
            }

            // Make dstTexcoord2 match srcTexcoord2 while rotating around dstTexcoord1/srcTexcoord1
            {
                dstTexGen.RotationAngle = 0;
                Matrix4x4 dstTextureFromPlane = dstTexGen.GeneratePlaneSpaceToTextureSpaceMatrix();
                Matrix4x4 dstTextureFromModel = dstTextureFromPlane * dstPlaneFromModel;

                Vector2 dstTexcoord1 = dstTextureFromModel.MultiplyPoint(dstModelPoint1);
                Vector2 dstTexcoord2 = dstTextureFromModel.MultiplyPoint(dstModelPoint2);
                Vector2 srcTexcoord2 = srcTextureFromModel.MultiplyPoint(srcModelPoint2);

                Vector2 vec1  = (dstTexcoord2 - dstTexcoord1).normalized;
                Vector2 vec2  = (srcTexcoord2 - dstTexcoord1).normalized;
                float   angle = GeometryUtility.SignedAngle2D(vec2, vec1);

                if(dstTexGen.Scale.x < 0 != dstTexGen.Scale.y < 0)
                {
                    angle = -angle;
                }

                dstTexGen.RotationAngle = angle;
            }

            if(scale != Vector3.one)
            {
                Matrix4x4 dstTextureFromPlane = dstTexGen.GeneratePlaneSpaceToTextureSpaceMatrix();
                dstTexGen.Scale = dstTextureFromPlane.MultiplyVector(scale);
            }

            // Re-align dstTexcoord1 with srcTexcoord1 on dstTexGen
            {
                dstTexGen.Translation = Vector2.zero;
                Matrix4x4 dstTextureFromPlane = dstTexGen.GeneratePlaneSpaceToTextureSpaceMatrix();
                Matrix4x4 dstTextureFromModel = dstTextureFromPlane * dstPlaneFromModel;

                Vector2 dstTexcoord1 = dstTextureFromModel.MultiplyPoint(dstModelPoint1);
                Vector2 offset       = srcTexcoord1 - dstTexcoord1;
                dstTexGen.Translation = offset;
            }

            return true;
        }

        public static bool AlignTextureSpaces(Matrix4x4 srcTextureMatrix, bool textureMatrixInPlaneSpace, ref TexGen dstTexGen, ref Surface dstSurface)
        {
            if(InternalCSGModelManager.External == null)
            {
                return false;
            }

            dstTexGen.Translation   = MathConstants.zeroVector2;
            dstTexGen.Scale         = new Vector2(-1, 1);
            dstTexGen.RotationAngle = 0;

            Matrix4x4 dstTextureSpaceToLocalSpace = GetLocalToTextureSpaceMatrix(dstTexGen, dstSurface).inverse;

            if(!textureMatrixInPlaneSpace)
            {
                srcTextureMatrix *= dstTextureSpaceToLocalSpace;
            }

            Vector3 tangent  = MathConstants.rightVector3;
            Vector3 binormal = MathConstants.upVector3;
            Vector3 normal   = MathConstants.forwardVector3;

            CSGPlane textureSpacePlane = new CSGPlane(normal, MathConstants.zeroVector3);

            {
                Vector3 uVector       = textureSpacePlane.Project(srcTextureMatrix.GetRow(1)).normalized;
                float   rotationAngle = GeometryUtility.SignedAngle(uVector, binormal, normal);
                dstTexGen.RotationAngle = rotationAngle;

                Quaternion invRotation    = Quaternion.AngleAxis(-dstTexGen.RotationAngle, normal);
                Matrix4x4  rotationMatrix = Matrix4x4.TRS(MathConstants.zeroVector3, invRotation, MathConstants.oneVector3);

                srcTextureMatrix *= rotationMatrix;
            }

            {
                Vector3 uVector          = textureSpacePlane.Project(srcTextureMatrix.GetRow(0));
                Vector3 vVector          = textureSpacePlane.Project(srcTextureMatrix.GetRow(1));
                float   uVectorMagnitude = Vector3.Dot(uVector, tangent);
                float   vVectorMagnitude = Vector3.Dot(vVector, binormal);

                dstTexGen.Scale = new Vector2(uVectorMagnitude, vVectorMagnitude);

                Vector3   invScale    = new Vector3(1.0f / dstTexGen.Scale.x, 1.0f / dstTexGen.Scale.y, 1.0f);
                Matrix4x4 scaleMatrix = Matrix4x4.TRS(MathConstants.zeroVector3, MathConstants.identityQuaternion, invScale);

                srcTextureMatrix *= scaleMatrix;
            }

            {
                Vector4 translation = srcTextureMatrix.GetColumn(3);
                translation.x /= translation.w;
                translation.y /= translation.w;

                dstTexGen.Translation = translation;
            }

            return true;
        }

#endregion
    }
}
