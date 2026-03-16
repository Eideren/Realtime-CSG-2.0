using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using InternalRealtimeCSG;
using RealtimeCSG.Legacy;

namespace RealtimeCSG
{
    [Serializable]
    internal sealed class ShapePolygon
    {
        public ShapePolygon()
        {
        }

        public ShapePolygon(Vector3[] vertices)
        {
            this.vertices = vertices;
        }

        // NOTE: an edge is defined as [vertex n, vertex (n+1)%vertices.length]
        public Vector3[] vertices;
        public TexGen[] edgeTexgens;
    }

    [Serializable]
    internal struct ShapeEdge
    {
        public int polygonIndex;
        public int edgeIndex;
    }

    internal static class ShapePolygonUtility
    {
        public static bool IntersectsWithShapePolygon2D(ShapePolygon shapePolygon, Ray ray)
        {
            CSGPlane  polygonPlane = new CSGPlane(MathConstants.upVector3, 0);
            Vector3   intersection = polygonPlane.RayIntersection(ray);
            Vector3[] vertices     = shapePolygon.vertices;

            for(int v0 = vertices.Length - 1, v1 = 0; v1 < vertices.Length; v0 = v1, v1++)
            {
                Vector3 vertex0 = vertices[v0];
                Vector3 vertex1 = vertices[v1];
                Vector3 delta   = vertex1 - vertex0;
                float   length  = delta.sqrMagnitude;

                if(length <= MathConstants.EqualityEpsilonSqr)
                {
                    continue;
                }

                Vector3  tangent = delta / Mathf.Sqrt(length);
                Vector3  normal  = Vector3.Cross(MathConstants.upVector3, tangent);
                CSGPlane plane   = new CSGPlane(normal, vertex0);

                if(plane.Distance(intersection) < MathConstants.DistanceEpsilon)
                {
                    return false;
                }
            }

            return true;
        }

        public static void RemoveDuplicatePoints(ref Vector3[] vertices)
        {
            // remove any points that are too close to one another
            for(int j = vertices.Length - 1, i = vertices.Length - 2; i >= 0; j = i, i--)
            {
                if((vertices[j] - vertices[i]).magnitude < MathConstants.DistanceEpsilon)
                {
                    ArrayUtility.RemoveAt(ref vertices, j);
                }
            }

            while(vertices.Length > 3
               && (vertices[0] - vertices[vertices.Length - 1]).magnitude < MathConstants.DistanceEpsilon)
            {
                int lastIndex = vertices.Length - 1;
                ArrayUtility.RemoveAt(ref vertices, lastIndex);
            }
        }

        public static void RemoveDuplicatePoints(ShapePolygon shapePolygon)
        {
            Vector3[] vertices    = shapePolygon.vertices;
            TexGen[]  edgeTexgens = shapePolygon.edgeTexgens;

            // remove any points that are too close to one another
            for(int j = vertices.Length - 1, i = vertices.Length - 2; i >= 0; j = i, i--)
            {
                if((vertices[j] - vertices[i]).magnitude < MathConstants.DistanceEpsilon)
                {
                    ArrayUtility.RemoveAt(ref vertices, j);
                    ArrayUtility.RemoveAt(ref edgeTexgens, j);
                }
            }

            while(vertices.Length > 3
               && (vertices[0] - vertices[vertices.Length - 1]).magnitude < MathConstants.DistanceEpsilon)
            {
                int lastIndex = vertices.Length - 1;
                ArrayUtility.RemoveAt(ref vertices, lastIndex);
                ArrayUtility.RemoveAt(ref edgeTexgens, lastIndex);
            }

            shapePolygon.vertices    = vertices;
            shapePolygon.edgeTexgens = edgeTexgens;
        }

        public static List<ShapePolygon> CreateCleanPolygonsFromVertices(
            Vector3[] vertices,
            Vector3 origin,
            CSGPlane buildPlane
        )
        {
            return CreateCleanPolygonsFromVertices(
                vertices,
                origin,
                buildPlane,
                out Vector3[] _
            );
        }

        public static List<ShapePolygon> CreateCleanPolygonsFromVertices(
            Vector3[] vertices,
            Vector3 origin,
            CSGPlane buildPlane,
            out Vector3[] projectedVertices
        )
        {
            if(vertices.Length < 3)
            {
                projectedVertices = null;

                return null;
            }

            Matrix4x4 m          = Matrix4x4.TRS(-origin, Quaternion.identity, Vector3.one);
            Vector2[] vertices2d = GeometryUtility.RotatePlaneTo2D(m, vertices, buildPlane);

            projectedVertices = GeometryUtility.ToVector3XZ(vertices2d);

            return CreateCleanSubPolygonsFromVertices(vertices2d);
        }

        private static List<ShapePolygon> CreateCleanSubPolygonsFromVertices(Vector2[] vertices2d)
        {
            if(vertices2d.Length < 3)
            {
                return null;
            }

            for(int i = 0; i < vertices2d.Length - 2; i++)
            {
                for(int j = i + 2; j < vertices2d.Length; j++)
                {
                    if((vertices2d[j] - vertices2d[i]).magnitude < MathConstants.DistanceEpsilon)
                    {
                        List<ShapePolygon> combinedPolygons = null;

                        int rightLength = vertices2d.Length - j;
                        int otherLength = i + rightLength;

                        if(otherLength > 2)
                        {
                            Vector2[] otherVertices = new Vector2[otherLength];

                            if(i > 0)
                            {
                                Array.Copy(vertices2d, 0, otherVertices, 0, i);
                            }

                            Array.Copy(vertices2d, j, otherVertices, i, rightLength);
                            combinedPolygons = CreateCleanSubPolygonsFromVertices(otherVertices);
                        }

                        int centerLength = j - i;

                        if(centerLength > 2)
                        {
                            Vector2[] firstVertices = new Vector2[centerLength];

                            Array.Copy(vertices2d, i, firstVertices, 0, centerLength);

                            List<ShapePolygon> firstPolygons =
                                CreateCleanSubPolygonsFromVertices(firstVertices);

                            if(combinedPolygons != null)
                            {
                                combinedPolygons.AddRange(firstPolygons);
                            }
                            else
                            {
                                combinedPolygons = firstPolygons;
                            }
                        }

                        return combinedPolygons;
                    }
                }
            }

            float polygonSign = GeometryUtility.CalcPolygonSign(vertices2d);

            if(polygonSign == 0)
            {
                return null;
            }

            if(polygonSign < 0)
            {
                Array.Reverse(vertices2d);
            }

            List<List<Vector2>> outlines;

            try
            {
                outlines = InternalCSGModelManager.External.convexPartition(vertices2d);
            }
            catch
            {
                outlines = null;
            }

            if(outlines == null)
            {
                return null;
            }

            List<ShapePolygon> polygons = new List<ShapePolygon>();

            for(int b = 0; b < outlines.Count; b++)
            {
                if(GeometryUtility.IsNonConvex(outlines[b]))
                {
                    return null;
                }

                polygons.Add(new ShapePolygon(GeometryUtility.ToVector3XZReversed(outlines[b])));
            }

            return polygons;
        }

        public static bool GenerateControlMeshFromVertices(
            ShapePolygon shape2DPolygon,
            Matrix4x4 localToWorld,
            Vector3 direction,
            float height,
            TexGen capTexgen,
            bool? smooth,
            bool singleSurfaceEnds,
            out ControlMesh controlMesh,
            out Shape shape
        )
        {
            if(shape2DPolygon == null)
            {
                controlMesh = null;
                shape       = null;

                return false;
            }

            Vector3[] vertices = shape2DPolygon.vertices;

            if(vertices.Length < 3 || height == 0.0f)
            {
                controlMesh = null;
                shape       = null;

                return false;
            }

            Vector3 from;
            Vector3 to;

            if(height > 0)
            {
                from = direction * height;
                to   = MathConstants.zeroVector3;
            }
            else
            {
                from = MathConstants.zeroVector3;
                to   = direction * height;
            }

            int count           = vertices.Length;
            int doubleCount     = count * 2;
            int extraPoints     = 0;
            int extraEdges      = 0;
            int endsPolygons    = 2;
            int startEdgeOffset = doubleCount;

            if(!singleSurfaceEnds)
            {
                extraPoints     =  2;
                extraEdges      =  4 * count;
                endsPolygons    =  doubleCount;
                startEdgeOffset += extraEdges;
            }

            Vector3[]  dstPoints   = new Vector3 [doubleCount + extraPoints];
            HalfEdge[] dstEdges    = new HalfEdge[count * 6 + extraEdges];
            Polygon[]  dstPolygons = new Polygon [count + endsPolygons];

            Vector3 center1 = MathConstants.zeroVector3;
            Vector3 center2 = MathConstants.zeroVector3;

            for(int i = 0; i < count; i++)
            {
                Vector3 point1 = vertices[i];
                Vector3 point2 = vertices[(count + i - 1) % count];

                point1 += @from;
                point2 += to;

                // swap y/z to solve texgen issues
                dstPoints[i].x = point1.x;
                dstPoints[i].y = point1.y;
                dstPoints[i].z = point1.z;

                center1 += dstPoints[i];

                dstEdges[i].VertexIndex = (short)i;
                dstEdges[i].HardEdge    = true;

                // swap y/z to solve texgen issues
                dstPoints[i + count].x =  point2.x;
                dstPoints[i + count].y =  point2.y;
                dstPoints[i + count].z =  point2.z;
                center2                += dstPoints[i + count];

                dstEdges[i + count].VertexIndex = (short)(i + count);
                dstEdges[i + count].HardEdge    = true;
            }

            if(!singleSurfaceEnds)
            {
                dstPoints[doubleCount]     = center1 / count;
                dstPoints[doubleCount + 1] = center2 / count;

                int   edgeOffset   = doubleCount;
                short polygonIndex = (short)count;

                // 'top'
                for(int i = 0, j = count - 1; i < count; j = i, i++)
                {
                    int jm = j % count;
                    int im = i % count;

                    int edgeOut0 = edgeOffset + jm * 2 + 1;
                    int edgeIn0  = edgeOffset + im * 2 + 0;
                    int edgeOut1 = edgeOffset + im * 2 + 1;

                    dstEdges[edgeIn0].VertexIndex = (short)doubleCount;
                    dstEdges[edgeIn0].HardEdge    = true;
                    dstEdges[edgeIn0].TwinIndex   = edgeOut1;

                    dstEdges[edgeOut1].VertexIndex = (short)im;
                    dstEdges[edgeOut1].HardEdge    = true;
                    dstEdges[edgeOut1].TwinIndex   = edgeIn0;

                    dstEdges[im].PolygonIndex       = polygonIndex;
                    dstEdges[edgeIn0].PolygonIndex  = polygonIndex;
                    dstEdges[edgeOut0].PolygonIndex = polygonIndex;

                    dstPolygons[polygonIndex] = new Polygon(new[] { im, edgeIn0, edgeOut0 }, polygonIndex);
                    polygonIndex++;
                }

                edgeOffset = doubleCount * 2;

                // 'bottom'
                for(int i = 0, j = count - 1; j >= 0; i = j, j--)
                {
                    int jm = (count + count - j) % count;
                    int im = (count + count - i) % count;

                    int edgeOut0 = edgeOffset + jm * 2 + 1;
                    int edgeIn0  = edgeOffset + im * 2 + 0;
                    int edgeOut1 = edgeOffset + im * 2 + 1;

                    dstEdges[edgeIn0].VertexIndex = (short)(doubleCount + 1);
                    dstEdges[edgeIn0].HardEdge    = true;
                    dstEdges[edgeIn0].TwinIndex   = edgeOut1;

                    dstEdges[edgeOut1].VertexIndex = (short)(im + count);
                    dstEdges[edgeOut1].HardEdge    = true;
                    dstEdges[edgeOut1].TwinIndex   = edgeIn0;

                    dstEdges[im + count].PolygonIndex = polygonIndex;
                    dstEdges[edgeIn0].PolygonIndex    = polygonIndex;
                    dstEdges[edgeOut0].PolygonIndex   = polygonIndex;

                    dstPolygons[polygonIndex] = new Polygon(new[] { im + count, edgeIn0, edgeOut0 }, polygonIndex);

                    polygonIndex++;
                }
            }
            else
            {
                int[] polygon0Edges = new int[count];
                int[] polygon1Edges = new int[count];

                for(int i = 0; i < count; i++)
                {
                    dstEdges[i].PolygonIndex         = (short)(count + 0);
                    dstEdges[i + count].PolygonIndex = (short)(count + 1);
                    polygon0Edges[i]                 = i;
                    polygon1Edges[count - (i + 1)]   = i + count;
                }

                dstPolygons[count + 0] = new Polygon(polygon0Edges, count + 0);
                dstPolygons[count + 1] = new Polygon(polygon1Edges, count + 1);
            }

            for(int v0 = count - 1, v1 = 0; v1 < count; v0 = v1, v1++)
            {
                short polygonIndex = (short)v1;

                int nextOffset = startEdgeOffset + (v1 + 1) % count * 4;
                int currOffset = startEdgeOffset + v1 * 4;
                int prevOffset = startEdgeOffset + (v1 + count - 1) % count * 4;

                int nextTwin = nextOffset + 1;
                int prevTwin = prevOffset + 3;

                dstEdges[v1].TwinIndex         = currOffset + 0;
                dstEdges[v1 + count].TwinIndex = currOffset + 2;

                dstEdges[currOffset + 0].PolygonIndex = polygonIndex;
                dstEdges[currOffset + 1].PolygonIndex = polygonIndex;
                dstEdges[currOffset + 2].PolygonIndex = polygonIndex;
                dstEdges[currOffset + 3].PolygonIndex = polygonIndex;

                dstEdges[currOffset + 0].TwinIndex = v1;
                dstEdges[currOffset + 1].TwinIndex = prevTwin;
                dstEdges[currOffset + 2].TwinIndex = v1 + count;
                dstEdges[currOffset + 3].TwinIndex = nextTwin;

                dstEdges[currOffset + 0].VertexIndex = (short)v0;
                dstEdges[currOffset + 1].VertexIndex = (short)(v1 + count);
                dstEdges[currOffset + 2].VertexIndex = (short)((v1 + 1) % count + count);
                dstEdges[currOffset + 3].VertexIndex = (short)v1;

                dstEdges[currOffset + 0].HardEdge = true;
                dstEdges[currOffset + 1].HardEdge = true;
                dstEdges[currOffset + 2].HardEdge = true;
                dstEdges[currOffset + 3].HardEdge = true;

                dstPolygons[polygonIndex] = new Polygon(
                    new[] { currOffset + 0, currOffset + 1, currOffset + 2, currOffset + 3 }, polygonIndex
                );
            }

            for(int i = 0; i < dstPoints.Length; i++)
            {
                dstPoints[i] = localToWorld.MultiplyPoint(dstPoints[i]);
            }

            controlMesh = new ControlMesh
            {
                Vertices = dstPoints
              , Edges    = dstEdges
              , Polygons = dstPolygons
            };

            controlMesh.SetDirty();

            shape = new Shape(dstPolygons.Length);

            for(int i = 0; i < shape.TexGenFlags.Length; i++)
            {
                shape.TexGenFlags[i] = CSGSettings.DefaultTexGenFlags;
            }

            uint smoothinggroup = smooth.HasValue && smooth.Value ? SurfaceUtility.FindUnusedSmoothingGroupIndex() : 0;

            int containedMaterialCount = 0;

            if(shape2DPolygon.edgeTexgens != null)
            {
                containedMaterialCount = shape2DPolygon.edgeTexgens.Length;
            }

            if(capTexgen.RenderMaterial == null)
            {
                capTexgen.RenderMaterial = CSGSettings.DefaultMaterial;
            }

            for(int i = 0; i < dstPolygons.Length; i++)
            {
                if(i < containedMaterialCount)
                {
                    shape.TexGens[i] = shape2DPolygon.edgeTexgens[i];
                }
                else
                {
                    shape.TexGens[i] = capTexgen;
                }

                shape.Surfaces[i].TexGenIndex = i;

                if(smooth.HasValue)
                {
                    shape.TexGens[i].SmoothingGroup = i < count ? smoothinggroup : 0;
                }
            }

            for(int s = 0; s < dstPolygons.Length; s++)
            {
                shape.Surfaces[s].Plane = GeometryUtility.CalcPolygonPlane(controlMesh, (short)s);
                Vector3 normal = shape.Surfaces[s].Plane.normal;
                GeometryUtility.CalculateTangents(normal, out Vector3 tangent, out Vector3 binormal);
                shape.Surfaces[s].Tangent     = tangent;
                shape.Surfaces[s].BiNormal    = binormal;
                shape.Surfaces[s].TexGenIndex = s;
            }

            controlMesh.Valid = ControlMeshUtility.Validate(controlMesh, shape);

            if(controlMesh.Valid)
            {
                return true;
            }

            controlMesh = null;
            shape       = null;

            return false;
        }

        public static void FixMaterials(
            Vector3[] originalVertices,
            List<ShapePolygon> polygons,
            TexGen[] edgeTexgens = null,
            ShapeEdge[] shapeEdges = null
        )
        {
            if(shapeEdges != null)
            {
                for(int e = 0; e < shapeEdges.Length; e++)
                {
                    shapeEdges[e].edgeIndex    = -1;
                    shapeEdges[e].polygonIndex = -1;
                }
            }

            for(int p = 0; p < polygons.Count; p++)
            {
                ShapePolygon shapePolygon = polygons[p];
                Vector3[]    vertices3d   = shapePolygon.vertices;
                TexGen[]     shapeTexgens = new TexGen[vertices3d.Length];

                if(edgeTexgens != null)
                {
                    int[] indices = new int[vertices3d.Length];

                    for(int n0 = 0; n0 < vertices3d.Length; n0++)
                    {
                        indices[n0] = -1;

                        for(int n1 = 0; n1 < originalVertices.Length; n1++)
                        {
                            float diff = (vertices3d[n0] - originalVertices[n1]).sqrMagnitude;

                            if(diff > MathConstants.EqualityEpsilonSqr)
                            {
                                continue;
                            }

                            indices[n0] = n1;

                            break;
                        }
                    }

                    for(int n0 = indices.Length - 1, n1 = 0; n1 < indices.Length; n0 = n1, n1++)
                    {
                        int vertex0 = indices[n0] % edgeTexgens.Length;
                        int vertex1 = indices[n1] % edgeTexgens.Length;

                        if(vertex0 == -1 || vertex1 == -1)
                        {
                            shapeTexgens[n1] = new TexGen(CSGSettings.DefaultMaterial); //n0
                        }
                        else if(Mathf.Abs(vertex1 - vertex0) == 1 ||
                                (vertex0 == vertices3d.Length - 1 && vertex1 == 0))
                        {
                            if(shapeEdges != null)
                            {
                                shapeEdges[vertex0].polygonIndex = p;
                                shapeEdges[vertex0].edgeIndex    = n1;
                            }

                            shapeTexgens[n1] = edgeTexgens[vertex0];
                        }
                        else
                        {
                            shapeTexgens[n1] = new TexGen(edgeTexgens[vertex0].RenderMaterial); //n0
                        }
                    }
                }
                else
                {
                    for(int n0 = 0; n0 < vertices3d.Length; n0++)
                    {
                        shapeTexgens[n0] = new TexGen(CSGSettings.DefaultMaterial); //n0
                    }
                }

                shapePolygon.edgeTexgens = shapeTexgens;

                RemoveDuplicatePoints(shapePolygon);
            }
        }
    }
}
