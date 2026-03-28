using UnityEngine;

namespace RealtimeCSG.Components
{
#if UNITY_EDITOR
    internal static class ComponentUpgrader
    {
        public static void UpgradeWhenNecessary(CSGModel model)
        {
            if(model.version >= CSGModel.CURRENT_VERSION)
            {
                return;
            }

            if(model.version < 1.0f)
            {
                model.version = 1.0f;
            }

            if(model.version == 1.0f)
            {
                model.angleError = 1;
                model.areaError  = 1;
                model.hardAngle  = 60;
                model.packMargin = 20;
                model.version    = 1.1f;
            }

            model.angleError = Mathf.Clamp(model.angleError, CSGModel.MIN_ANGLE_ERROR, CSGModel.MAX_ANGLE_ERROR);
            model.areaError  = Mathf.Clamp(model.areaError, CSGModel.MIN_AREA_ERROR, CSGModel.MAX_AREA_ERROR);

            model.version = CSGModel.CURRENT_VERSION;
        }

        public static void UpgradeWhenNecessary(CSGBrush brush)
        {
            if(brush.version >= CSGBrush.CURRENT_VERSION)
            {
                return;
            }

            if(brush.version < 1.0f)
            {
                brush.version = 1.0f;
            }

            if(brush.version == 1.0f)
            {
#pragma warning disable 618 // Type is now obsolete
                if(brush.shape.materials != null && brush.shape.materials.Length > 0)
                {
                    // update textures
                    if(brush.shape.texGens != null)
                    {
                        for(int i = 0; i < brush.shape.texGens.Length; i++)
                        {
                            brush.shape.texGens[i].RenderMaterial = null;
                        }

#pragma warning disable 618 // Type is now obsolete
                        for(int i = 0; i < Mathf.Min(brush.shape.materials.Length, brush.shape.texGens.Length); i++)
                        {
#pragma warning disable 618 // Type is now obsolete
                            brush.shape.texGens[i].RenderMaterial = brush.shape.materials[i];
                        }
                    }
                }
            }

            if(brush.version == 2.0f)
            {
                if(brush.CompareTag("EditorOnly"))
                {
                    brush.tag = "Untagged";
                }
            }

            brush.version = CSGBrush.CURRENT_VERSION;
        }

        public static void UpgradeWhenNecessary(CSGOperation operation)
        {
            if(operation.version >= CSGOperation.CURRENT_VERSION)
            {
                return;
            }

            if(operation.version < 1.0f)
            {
                operation.version = 1.0f;
            }

            if(operation.version == 1.0f)
            {
                if(operation.CompareTag("EditorOnly"))
                {
                    operation.tag = "Untagged";
                }
            }

            operation.version = CSGOperation.CURRENT_VERSION;
        }
    }
#endif
}
