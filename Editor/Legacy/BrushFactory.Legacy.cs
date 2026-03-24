using InternalRealtimeCSG;
using UnityEngine;
using RealtimeCSG.Components;
using RealtimeCSG.Foundation;
using System.Linq;

namespace RealtimeCSG.Legacy
{
    public sealed partial class BrushFactory
    {
#if UNITY_EDITOR

#region CreateBrush (internal)

        internal static CSGBrush CreateBrush(Transform parent, string brushName, ControlMesh controlMesh, Shape shape)
        {
            GameObject gameObject = OperationsUtility.CreateGameObject(parent, brushName, false);

            if(!gameObject)
            {
                return null;
            }

            CSGBrush brush = gameObject.AddComponent<CSGBrush>();

            if(!brush)
            {
                return null;
            }

            brush.ControlMesh = controlMesh;
            brush.Shape       = shape;

            if(brush.ControlMesh != null)
            {
                brush.ControlMesh.SetDirty();
            }

            if(brush.Shape != null)
            {
                ShapeUtility.EnsureInitialized(brush.Shape);
            }

            return brush;
        }

#endregion

#region CreateBrushComponent (internal)

        internal static CSGBrush CreateBrushComponent(GameObject gameObject, ControlMesh controlMesh, Shape shape)
        {
            CSGBrush brush = gameObject.AddComponent<CSGBrush>();

            if(!brush)
            {
                return null;
            }

            brush.ControlMesh = controlMesh;
            brush.Shape       = shape;

            if(brush.ControlMesh != null)
            {
                brush.ControlMesh.SetDirty();
            }

            if(brush.Shape != null)
            {
                ShapeUtility.EnsureInitialized(brush.Shape);
            }

            return brush;
        }

#endregion

        // Create a brush from an array of planes that define the convex space.
        // optionally it's possible, for each plane, to supply a material and a texture-matrix that defines how the texture is translated/rotated/scaled.
        public static CSGBrush CreateBrushFromPlanes(Transform parent, string brushName, Plane[] planes, Vector3[] tangents = null, Vector3[] binormals = null, Material[] materials = null, Matrix4x4[] textureMatrices = null, TextureMatrixSpace textureMatrixSpace = TextureMatrixSpace.WorldSpace)
        {
            if(!CreateControlMeshFromPlanes(out ControlMesh controlMesh, out Shape shape, planes, tangents, binormals, materials, textureMatrices, textureMatrixSpace))
            {
                return null;
            }

            return CreateBrush(parent, brushName, controlMesh, shape);
        }

        public static CSGBrush CreateBrushFromPlanes(string brushName, Plane[] planes, Vector3[] tangents = null, Vector3[] binormals = null, Material[] materials = null, Matrix4x4[] textureMatrices = null, TextureMatrixSpace textureMatrixSpace = TextureMatrixSpace.WorldSpace)
        {
            return CreateBrushFromPlanes(null, brushName, planes, tangents, binormals, materials, textureMatrices, textureMatrixSpace);
        }

        public static CSGBrush CreateBrushFromPlanes(Plane[] planes, Vector3[] tangents = null, Vector3[] binormals = null, Material[] materials = null, Matrix4x4[] textureMatrices = null, TextureMatrixSpace textureMatrixSpace = TextureMatrixSpace.WorldSpace)
        {
            return CreateBrushFromPlanes(null, "Brush", planes, tangents, binormals, materials, textureMatrices, textureMatrixSpace);
        }

        public static CSGBrush CreateBrushFromPlanes(GameObject gameObject, Plane[] planes, Vector3[] tangents = null, Vector3[] binormals = null, Material[] materials = null, Matrix4x4[] textureMatrices = null, TextureMatrixSpace textureMatrixSpace = TextureMatrixSpace.WorldSpace, uint[] smoothingGroups = null, TexGenFlags[] texGenFlags = null)
        {
            if(!CreateControlMeshFromPlanes(out ControlMesh controlMesh, out Shape shape, planes, tangents, binormals, materials, textureMatrices, textureMatrixSpace, smoothingGroups, texGenFlags))
            {
                return null;
            }

            return CreateBrushComponent(gameObject, controlMesh, shape);
        }

        public static bool SetBrushFromPlanes(CSGBrush brush, Plane[] planes, Vector3[] tangents = null, Vector3[] binormals = null, Material[] materials = null, Matrix4x4[] textureMatrices = null, TextureMatrixSpace textureMatrixSpace = TextureMatrixSpace.WorldSpace)
        {
            if(!brush)
            {
                return false;
            }

            if(!CreateControlMeshFromPlanes(out ControlMesh controlMesh, out Shape shape, planes, tangents, binormals, materials, textureMatrices, textureMatrixSpace))
            {
                return false;
            }

            brush.ControlMesh = controlMesh;
            brush.Shape       = shape;

            if(brush.ControlMesh != null)
            {
                brush.ControlMesh.SetDirty();
            }

            if(brush.Shape != null)
            {
                ShapeUtility.EnsureInitialized(brush.Shape);
            }

            return true;
        }

#region CreateCubeControlMesh (internal)

        internal static bool CreateCubeControlMesh(out ControlMesh controlMesh, out Shape shape, Vector3 min, Vector3 max, Material material = null)
        {
            if(min.x > max.x)
            {
                (min.x, max.x) = (max.x, min.x);
            }

            if(min.y > max.y)
            {
                (min.y, max.y) = (max.y, min.y);
            }

            if(min.z > max.z)
            {
                (min.z, max.z) = (max.z, min.z);
            }

            if(min.x == max.x || min.y == max.y || min.z == max.z)
            {
                shape       = null;
                controlMesh = null;

                return false;
            }

            controlMesh = new ControlMesh();

            controlMesh.Vertices = new[]
            {
                new Vector3(min.x, min.y, min.z) // 0
              , new Vector3(min.x, max.y, min.z) // 1
              , new Vector3(max.x, max.y, min.z) // 2
              , new Vector3(max.x, min.y, min.z) // 3
              , new Vector3(min.x, min.y, max.z) // 4
              , new Vector3(max.x, min.y, max.z) // 5
              , new Vector3(max.x, max.y, max.z) // 6
              , new Vector3(min.x, max.y, max.z) // 7
            };

            controlMesh.Edges = new[]
            {
                new HalfEdge
                {
                    PolygonIndex = 0
                  , TwinIndex    = 21
                  , VertexIndex  = 0
                } //  0
              , new HalfEdge
                {
                    PolygonIndex = 0
                  , TwinIndex    = 8
                  , VertexIndex  = 1
                } //  1
              , new HalfEdge
                {
                    PolygonIndex = 0
                  , TwinIndex    = 12
                  , VertexIndex  = 2
                } //  2
              , new HalfEdge
                {
                    PolygonIndex = 0
                  , TwinIndex    = 17
                  , VertexIndex  = 3
                } //  3
              , new HalfEdge
                {
                    PolygonIndex = 1
                  , TwinIndex    = 10
                  , VertexIndex  = 4
                } //  4
              , new HalfEdge
                {
                    PolygonIndex = 1
                  , TwinIndex    = 23
                  , VertexIndex  = 5
                } //  5
              , new HalfEdge
                {
                    PolygonIndex = 1
                  , TwinIndex    = 19
                  , VertexIndex  = 6
                } //  6
              , new HalfEdge
                {
                    PolygonIndex = 1
                  , TwinIndex    = 14
                  , VertexIndex  = 7
                } //  7
              , new HalfEdge
                {
                    PolygonIndex = 2
                  , TwinIndex    = 1
                  , VertexIndex  = 0
                } //  8
              , new HalfEdge
                {
                    PolygonIndex = 2
                  , TwinIndex    = 20
                  , VertexIndex  = 4
                } //  9
              , new HalfEdge
                {
                    PolygonIndex = 2
                  , TwinIndex    = 4
                  , VertexIndex  = 7
                } // 10
              , new HalfEdge
                {
                    PolygonIndex = 2
                  , TwinIndex    = 13
                  , VertexIndex  = 1
                } // 11
              , new HalfEdge
                {
                    PolygonIndex = 3
                  , TwinIndex    = 2
                  , VertexIndex  = 1
                } // 12
              , new HalfEdge
                {
                    PolygonIndex = 3
                  , TwinIndex    = 11
                  , VertexIndex  = 7
                } // 13
              , new HalfEdge
                {
                    PolygonIndex = 3
                  , TwinIndex    = 7
                  , VertexIndex  = 6
                } // 14
              , new HalfEdge
                {
                    PolygonIndex = 3
                  , TwinIndex    = 18
                  , VertexIndex  = 2
                } // 15
              , new HalfEdge
                {
                    PolygonIndex = 4
                  , TwinIndex    = 22
                  , VertexIndex  = 3
                } // 16
              , new HalfEdge
                {
                    PolygonIndex = 4
                  , TwinIndex    = 3
                  , VertexIndex  = 2
                } // 17
              , new HalfEdge
                {
                    PolygonIndex = 4
                  , TwinIndex    = 15
                  , VertexIndex  = 6
                } // 18
              , new HalfEdge
                {
                    PolygonIndex = 4
                  , TwinIndex    = 6
                  , VertexIndex  = 5
                } // 19
              , new HalfEdge
                {
                    PolygonIndex = 5
                  , TwinIndex    = 9
                  , VertexIndex  = 0
                } // 20
              , new HalfEdge
                {
                    PolygonIndex = 5
                  , TwinIndex    = 0
                  , VertexIndex  = 3
                } // 21
              , new HalfEdge
                {
                    PolygonIndex = 5
                  , TwinIndex    = 16
                  , VertexIndex  = 5
                } // 22
              , new HalfEdge
                {
                    PolygonIndex = 5
                  , TwinIndex    = 5
                  , VertexIndex  = 4
                } // 23
            };

            controlMesh.Polygons = new[]
            {
                // left/right
                new Polygon(new[] { 0, 1, 2, 3 }, 0) // 0
              , new Polygon(new[] { 4, 5, 6, 7 }, 1) // 1

                // front/back
              , new Polygon(new[] { 8, 9, 10, 11 }, 2)   // 2
              , new Polygon(new[] { 12, 13, 14, 15 }, 3) // 3

                // top/down
              , new Polygon(new[] { 16, 17, 18, 19 }, 4) // 4
              , new Polygon(new[] { 20, 21, 22, 23 }, 5) // 5
            };

            shape = new Shape();

            shape.Surfaces = new Surface[6];

            shape.Surfaces[0].TexGenIndex = 0;
            shape.Surfaces[1].TexGenIndex = 1;
            shape.Surfaces[2].TexGenIndex = 2;
            shape.Surfaces[3].TexGenIndex = 3;
            shape.Surfaces[4].TexGenIndex = 4;
            shape.Surfaces[5].TexGenIndex = 5;

            shape.Surfaces[0].Plane = GeometryUtility.CalcPolygonPlane(controlMesh, 0);
            shape.Surfaces[1].Plane = GeometryUtility.CalcPolygonPlane(controlMesh, 1);
            shape.Surfaces[2].Plane = GeometryUtility.CalcPolygonPlane(controlMesh, 2);
            shape.Surfaces[3].Plane = GeometryUtility.CalcPolygonPlane(controlMesh, 3);
            shape.Surfaces[4].Plane = GeometryUtility.CalcPolygonPlane(controlMesh, 4);
            shape.Surfaces[5].Plane = GeometryUtility.CalcPolygonPlane(controlMesh, 5);

            GeometryUtility.CalculateTangents(shape.Surfaces[0].Plane.normal, out shape.Surfaces[0].Tangent, out shape.Surfaces[0].BiNormal);
            GeometryUtility.CalculateTangents(shape.Surfaces[1].Plane.normal, out shape.Surfaces[1].Tangent, out shape.Surfaces[1].BiNormal);
            GeometryUtility.CalculateTangents(shape.Surfaces[2].Plane.normal, out shape.Surfaces[2].Tangent, out shape.Surfaces[2].BiNormal);
            GeometryUtility.CalculateTangents(shape.Surfaces[3].Plane.normal, out shape.Surfaces[3].Tangent, out shape.Surfaces[3].BiNormal);
            GeometryUtility.CalculateTangents(shape.Surfaces[4].Plane.normal, out shape.Surfaces[4].Tangent, out shape.Surfaces[4].BiNormal);
            GeometryUtility.CalculateTangents(shape.Surfaces[5].Plane.normal, out shape.Surfaces[5].Tangent, out shape.Surfaces[5].BiNormal);

            if(material == null)
            {
                material = CSGSettings.DefaultMaterial;
            }

            shape.TexGens = new TexGen[6];

            shape.TexGens[0].RenderMaterial = material;
            shape.TexGens[1].RenderMaterial = material;
            shape.TexGens[2].RenderMaterial = material;
            shape.TexGens[3].RenderMaterial = material;
            shape.TexGens[4].RenderMaterial = material;
            shape.TexGens[5].RenderMaterial = material;

            shape.TexGens[0].Scale = MathConstants.oneVector3;
            shape.TexGens[1].Scale = MathConstants.oneVector3;
            shape.TexGens[2].Scale = MathConstants.oneVector3;
            shape.TexGens[3].Scale = MathConstants.oneVector3;
            shape.TexGens[4].Scale = MathConstants.oneVector3;
            shape.TexGens[5].Scale = MathConstants.oneVector3;

            shape.TexGenFlags = new[] { CSGSettings.DefaultTexGenFlags, CSGSettings.DefaultTexGenFlags, CSGSettings.DefaultTexGenFlags, CSGSettings.DefaultTexGenFlags, CSGSettings.DefaultTexGenFlags, CSGSettings.DefaultTexGenFlags };

            ShapeUtility.EnsureInitialized(shape);
            controlMesh.Valid = ControlMeshUtility.Validate(controlMesh, shape);

            return controlMesh.Valid;
        }

#endregion

#region SetBrushCubeMesh (public)

        public static bool SetBrushCubeMesh(CSGBrush brush, Vector3 size)
        {
            if(!brush)
            {
                return false;
            }

            CreateCubeControlMesh(out ControlMesh controlMesh, out Shape shape, size);

            brush.ControlMesh = controlMesh;
            brush.Shape       = shape;

            if(brush.ControlMesh != null)
            {
                brush.ControlMesh.SetDirty();
            }

            if(brush.Shape != null)
            {
                ShapeUtility.EnsureInitialized(brush.Shape);
            }

            return true;
        }

        public static bool SetBrushCubeMesh(CSGBrush brush)
        {
            return SetBrushCubeMesh(brush, Vector3.one);
        }

#endregion

#region CreateCubeBrush (public)

        public static CSGBrush CreateCubeBrush(Transform parent, string brushName, Vector3 size)
        {
            CreateCubeControlMesh(out ControlMesh controlMesh, out Shape shape, size);

            return CreateBrush(parent, brushName, controlMesh, shape);
        }

        public static CSGBrush CreateCubeBrush(string brushName, Vector3 size)
        {
            return CreateCubeBrush(null, brushName, size);
        }

        public static CSGBrush CreateCubeBrush(Vector3 size)
        {
            return CreateCubeBrush(null, "Brush", size);
        }

#endregion

        /// <summary>Converts a <see cref="RealtimeCSG.Legacy.ControlMesh"/>/<see cref="RealtimeCSG.Legacy.Shape"/> pair into a <see cref="RealtimeCSG.Foundation.BrushMesh"/>.</summary>
        /// <param name="controlMesh">A legacy <see cref="RealtimeCSG.Legacy.ControlMesh"/> that describes the shape of the <see cref="RealtimeCSG.Foundation.BrushMesh"/>.</param>
        /// <param name="shape">A legacy <see cref="RealtimeCSG.Legacy.Shape"/> that describes the surfaces in the <see cref="RealtimeCSG.Foundation.BrushMesh"/>.</param>
        /// <param name="defaultPhysicsMaterial"></param>
        /// <returns>A new <see cref="RealtimeCSG.Foundation.BrushMesh"/></returns>
        public static BrushMesh GenerateFromControlMesh(ControlMesh controlMesh, Shape shape, PhysicsMaterial defaultPhysicsMaterial = null)
        {
            if(controlMesh == null || shape == null)
            {
                return null;
            }

            if(!ControlMeshUtility.Validate(controlMesh, shape))
            {
                return null;
            }

            Vector3[]  vertices    = controlMesh.Vertices;
            HalfEdge[] srcEdges    = controlMesh.Edges;
            Polygon[]  srcPolygons = controlMesh.Polygons;

            if(vertices == null || srcEdges == null || srcPolygons == null)
            {
                return null;
            }

            Surface[]     surfaces    = shape.Surfaces;
            TexGen[]      texgens     = shape.TexGens;
            TexGenFlags[] texgenFlags = shape.TexGenFlags;

            if(surfaces == null || texgens == null || texgenFlags == null)
            {
                return null;
            }

            int polygonCount = surfaces.Length;

            if(polygonCount != srcPolygons.Length || polygonCount != texgens.Length || polygonCount != texgenFlags.Length || polygonCount != surfaces.Length)
            {
                return null;
            }

            BrushMesh brushMesh = new BrushMesh
            {
                polygons = new BrushMesh.Polygon[polygonCount]
              , vertices = vertices.ToArray()
            };

            for(int i = 0; i < polygonCount; i++)
            {
                Surface     surface     = surfaces[i];
                int         texGenIndex = surface.TexGenIndex;
                TexGen      texGen      = texgens[texGenIndex];
                TexGenFlags flags       = texgenFlags[texGenIndex];

                if(!texGen.PhysicsMaterial)
                {
                    texGen.PhysicsMaterial = defaultPhysicsMaterial;
                }

                brushMesh.polygons[i].polygonID = i;
                brushMesh.polygons[i].surface   = CreateSurfaceDescription(surface, texGen, flags);
                brushMesh.polygons[i].layers    = CreateSurfaceLayer(texGen, flags);
            }

            int counter = 0;

            for(int i = 0; i < polygonCount; i++)
            {
                int[] edgeIndices = srcPolygons[i].EdgeIndices;
                brushMesh.polygons[i].firstEdge =  counter;
                brushMesh.polygons[i].edgeCount =  edgeIndices.Length;
                counter                         += edgeIndices.Length;
            }

            brushMesh.halfEdges = new BrushMesh.HalfEdge[counter];

            counter = 0;

            for(int i = 0; i < polygonCount; i++)
            {
                int[] edgeIndices = srcPolygons[i].EdgeIndices;

                for(int v = 0; v < edgeIndices.Length; v++)
                {
                    HalfEdge edge = srcEdges[edgeIndices[v]];
                    brushMesh.halfEdges[counter + v].vertexIndex = edge.VertexIndex;

                    int   twinIndex        = edge.TwinIndex;
                    short twinPolygonIndex = srcEdges[twinIndex].PolygonIndex;
                    int[] twinEdges        = srcPolygons[twinPolygonIndex].EdgeIndices;
                    bool  found            = false;

                    for(int t = 0; t < twinEdges.Length; t++)
                    {
                        if(twinEdges[t] == twinIndex)
                        {
                            twinIndex = t + brushMesh.polygons[twinPolygonIndex].firstEdge;
                            found     = true;

                            break;
                        }
                    }

                    if(!found)
                    {
                        return null;
                    }

                    brushMesh.halfEdges[counter + v].twinIndex = twinIndex;
                }

                counter += edgeIndices.Length;
            }

            return brushMesh;
        }

        /// <summary>Generate a <see cref="RealtimeCSG.Legacy.ControlMesh"/>/<see cref="RealtimeCSG.Legacy.Shape"/> pair from the given planes (and optional other values)</summary>
        /// <remarks><note>Keep in mind that the planes encapsulate the geometry we're generating, so it can only be <i>convex</i>.</note></remarks>
        /// <param name="controlMesh">The generated <see cref="RealtimeCSG.Legacy.ControlMesh"/></param>
        /// <param name="shape">The generated <see cref="RealtimeCSG.Legacy.Shape"/></param>
        /// <param name="planes">The geometric planes of all the surfaces that define this convex shape</param>
        /// <param name="tangents">The tangents for each plane (optional)</param>
        /// <param name="binormals">The binormals for each plane (optional)</param>
        /// <param name="materials">The materials for each plane (optional)</param>
        /// <param name="textureMatrices">The texture matrices for each plane (optional)</param>
        /// <param name="textureMatrixSpace">The texture matrix space for each plane (optional)</param>
        /// <param name="smoothingGroups">The smoothing groups for each plane (optional)</param>
        /// <param name="texGenFlags">The <see cref="RealtimeCSG.Legacy.TexGenFlags"/> for each plane (optional)</param>
        /// <returns>*true* on success, *false* on failure</returns>
        public static bool CreateControlMeshFromPlanes(out ControlMesh controlMesh, out Shape shape, Plane[] planes, Vector3[] tangents = null, Vector3[] binormals = null, Material[] materials = null, Matrix4x4[] textureMatrices = null, TextureMatrixSpace textureMatrixSpace = TextureMatrixSpace.WorldSpace, uint[] smoothingGroups = null, TexGenFlags[] texGenFlags = null)
        {
            controlMesh = null;
            shape       = null;

            if(planes == null)
            {
                Debug.LogError("The planes array is not allowed to be null");

                return false;
            }

            if(planes.Length < 4)
            {
                Debug.LogError("The planes array must have at least 4 planes");

                return false;
            }

            if(materials == null)
            {
                materials = new Material[planes.Length];

                for(int i = 0; i < materials.Length; i++)
                {
                    materials[i] = CSGSettings.DefaultMaterial;
                }
            }

            if(planes.Length != materials.Length
            || (textureMatrices != null && planes.Length != textureMatrices.Length)
            || (tangents != null && tangents.Length != textureMatrices.Length)
            || (binormals != null && binormals.Length != textureMatrices.Length)
            || (smoothingGroups != null && smoothingGroups.Length != materials.Length))
            {
                Debug.LogError("All non null arrays need to be of equal length");

                return false;
            }

            shape             = new Shape();
            shape.TexGenFlags = new TexGenFlags[planes.Length];
            shape.Surfaces    = new Surface[planes.Length];
            shape.TexGens     = new TexGen[planes.Length];

            for(int i = 0; i < planes.Length; i++)
            {
                shape.Surfaces[i].Plane = new CSGPlane(planes[i].normal, -planes[i].distance);

                Vector3 tangent, binormal;

                if(tangents != null && binormals != null)
                {
                    tangent  = tangents[i];
                    binormal = binormals[i];
                }
                else
                {
                    GeometryUtility.CalculateTangents(planes[i].normal, out tangent, out binormal);
                }

                shape.Surfaces[i].Tangent     = -tangent;
                shape.Surfaces[i].BiNormal    = -binormal;
                shape.Surfaces[i].TexGenIndex = i;
                shape.TexGens[i]              = new TexGen(materials[i]);

                if(smoothingGroups != null)
                {
                    shape.TexGens[i].SmoothingGroup = smoothingGroups[i];
                }

                if(texGenFlags != null)
                {
                    shape.TexGenFlags[i] = texGenFlags[i];
                }
                else
                {
                    shape.TexGenFlags[i] = CSGSettings.DefaultTexGenFlags;
                }
            }

            controlMesh = ControlMeshUtility.CreateFromShape(shape);

            if(controlMesh == null)
            {
                return false;
            }

            if(!ControlMeshUtility.Validate(controlMesh, shape))
            {
                return false;
            }

            if(textureMatrices != null)
            {
                int n = 0;

                for(int i = 0; i < planes.Length; i++)
                {
                    if(shape.Surfaces[n].TexGenIndex != i)
                    {
                        continue;
                    }

                    shape.Surfaces[n].TexGenIndex = n;

                    SurfaceUtility.AlignTextureSpaces(textureMatrices[i], textureMatrixSpace == TextureMatrixSpace.PlaneSpace, ref shape.TexGens[n], ref shape.Surfaces[n]);

                    n++;
                }
            }

            return true;
        }

        /// <summary>
        /// Creates a cube <see cref="RealtimeCSG.Legacy.ControlMesh"/>/<see cref="RealtimeCSG.Legacy.Shape"/> pair with <paramref name="size"/> and optional <paramref name="material"/>
        /// </summary>
        /// <param name="controlMesh">The generated <see cref="RealtimeCSG.Legacy.ControlMesh"/></param>
        /// <param name="shape">The generated <see cref="RealtimeCSG.Legacy.Shape"/></param>
        /// <param name="size">The size of the cube</param>
        /// <param name="material">The [UnityEngine.Material](https://docs.unity3d.com/ScriptReference/Material.html) that will be set to all surfaces of the cube (optional)</param>
        /// <returns>*true* on success, *false* on failure</returns>
        public static bool CreateCubeControlMesh(out ControlMesh controlMesh, out Shape shape, Vector3 size, Material material = null)
        {
            Vector3 halfSize = size * 0.5f;

            return CreateCubeControlMesh(out controlMesh, out shape, halfSize, -halfSize, material);
        }

#endif
    }
}
