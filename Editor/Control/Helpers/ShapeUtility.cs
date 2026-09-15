using UnityEngine;
using InternalRealtimeCSG;
using RealtimeCSG.Legacy;

namespace RealtimeCSG
{
    internal static class ShapeUtility
    {
#region CheckMaterials

        public static bool CheckMaterials(Shape shape)
        {
            return shape.CheckMaterials();
        }

#endregion

#region EnsureInitialized

        public static bool EnsureInitialized(Shape shape)
        {
            bool dirty = CheckMaterials(shape);

            for(int i = 0; i < shape.texGens.Length; i++)
            {
                if((shape.texGens[i].Scale.x > -MathConstants.MINIMUM_SCALE && shape.texGens[i].Scale.x < MathConstants.MINIMUM_SCALE) || (shape.texGens[i].Scale.y > -MathConstants.MINIMUM_SCALE && shape.texGens[i].Scale.y < MathConstants.MINIMUM_SCALE))
                {
                    dirty = true;

                    if(shape.texGens[i].Scale.x == 0 && shape.texGens[i].Scale.y == 0)
                    {
                        shape.texGens[i].Scale.x = CSGSettings.InitalUVScale.x;
                        shape.texGens[i].Scale.y = CSGSettings.InitalUVScale.y;
                    }

                    if(shape.texGens[i].Scale.x < 0)
                    {
                        shape.texGens[i].Scale.x = -Mathf.Max(MathConstants.MINIMUM_SCALE, Mathf.Abs(shape.texGens[i].Scale.x));
                    }
                    else
                    {
                        shape.texGens[i].Scale.x = Mathf.Max(MathConstants.MINIMUM_SCALE, Mathf.Abs(shape.texGens[i].Scale.x));
                    }

                    if(shape.texGens[i].Scale.y < 0)
                    {
                        shape.texGens[i].Scale.y = -Mathf.Max(MathConstants.MINIMUM_SCALE, Mathf.Abs(shape.texGens[i].Scale.y));
                    }
                    else
                    {
                        shape.texGens[i].Scale.y = Mathf.Max(MathConstants.MINIMUM_SCALE, Mathf.Abs(shape.texGens[i].Scale.y));
                    }
                }
            }

            for(int i = 0; i < shape.surfaces.Length; i++)
            {
                if(shape.surfaces[i].Tangent == MathConstants.zeroVector3 || shape.surfaces[i].BiNormal == MathConstants.zeroVector3)
                {
                    dirty = true;
                    Vector3 normal = shape.surfaces[i].Plane.Normal;
                    GeometryUtility.CalculateTangents(normal, out Vector3 tangent, out Vector3 binormal);
                    shape.surfaces[i].Tangent  = tangent;
                    shape.surfaces[i].BiNormal = binormal;
                }
            }

            return dirty;
        }

#endregion
    }
}
