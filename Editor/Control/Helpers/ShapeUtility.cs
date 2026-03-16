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

            for(int i = 0; i < shape.TexGens.Length; i++)
            {
                if((shape.TexGens[i].Scale.x >= -MathConstants.MinimumScale
                    && shape.TexGens[i].Scale.x <= MathConstants.MinimumScale) ||
                   (shape.TexGens[i].Scale.y >= -MathConstants.MinimumScale
                    && shape.TexGens[i].Scale.y <= MathConstants.MinimumScale))
                {
                    dirty = true;

                    if(shape.TexGens[i].Scale.x == 0 &&
                       shape.TexGens[i].Scale.y == 0)
                    {
                        shape.TexGens[i].Scale.x = 1.0f;
                        shape.TexGens[i].Scale.y = 1.0f;
                    }

                    if(shape.TexGens[i].Scale.x < 0)
                    {
                        shape.TexGens[i].Scale.x = -Mathf.Max(
                            MathConstants.MinimumScale, Mathf.Abs(shape.TexGens[i].Scale.x)
                        );
                    }
                    else
                    {
                        shape.TexGens[i].Scale.x = Mathf.Max(
                            MathConstants.MinimumScale, Mathf.Abs(shape.TexGens[i].Scale.x)
                        );
                    }

                    if(shape.TexGens[i].Scale.y < 0)
                    {
                        shape.TexGens[i].Scale.y = -Mathf.Max(
                            MathConstants.MinimumScale, Mathf.Abs(shape.TexGens[i].Scale.y)
                        );
                    }
                    else
                    {
                        shape.TexGens[i].Scale.y = Mathf.Max(
                            MathConstants.MinimumScale, Mathf.Abs(shape.TexGens[i].Scale.y)
                        );
                    }
                }
            }

            for(int i = 0; i < shape.Surfaces.Length; i++)
            {
                if(shape.Surfaces[i].Tangent == MathConstants.zeroVector3 ||
                   shape.Surfaces[i].BiNormal == MathConstants.zeroVector3)
                {
                    dirty = true;
                    Vector3 normal = shape.Surfaces[i].Plane.normal;
                    GeometryUtility.CalculateTangents(normal, out Vector3 tangent, out Vector3 binormal);
                    shape.Surfaces[i].Tangent  = tangent;
                    shape.Surfaces[i].BiNormal = binormal;
                }
            }

            return dirty;
        }

#endregion
    }
}
