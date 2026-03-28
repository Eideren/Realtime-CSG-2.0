using System;
using System.Collections.Generic;
using System.Linq;
using InternalRealtimeCSG;
using RealtimeCSG.Components;
using RealtimeCSG.Legacy;
using UnityEditor;
using UnityEngine;

namespace RealtimeCSG
{
    [Serializable]
    internal sealed class MergePoint
    {
        public MergePoint(short i1, short i2)
        {
            if(i1 > i2)
            {
                index1 = i1;
                index2 = i2;
            }
            else
            {
                index1 = i2;
                index2 = i1;
            }
        }

        public short index1;
        public short index2;
    }

    internal sealed class AcceptableTopology
    {
        public readonly HashSet<short> acceptablePoints = new HashSet<short>();
        public readonly HashSet<int> acceptableEdges = new HashSet<int>();
        public readonly HashSet<short> acceptablePolygons = new HashSet<short>();

        public void Clear()
        {
            acceptablePoints.Clear();
            acceptableEdges.Clear();
            acceptablePolygons.Clear();
        }
    }

    internal static class ControlMeshUtility
    {
        private static bool m_OutputToLog = false;
        private static readonly HashSet<string> prevOutput = new HashSet<string>();

        public static bool Validate(ControlMesh controlMesh, Shape shape)
        {
            prevOutput.Clear();

            if(controlMesh.edges == null)
            {
                if(!m_OutputToLog)
                {
                    return false;
                }

                const string text = "no edges found in polygon";

                if(prevOutput.Add(text))
                {
                    Debug.Log(text);
                }

                return false;
            }

            if(shape == null || shape.surfaces.Length < 4)
            {
                if(!m_OutputToLog)
                {
                    return false;
                }

                const string text = "shape == null || shape.Surfaces.Length < 4";

                if(prevOutput.Add(text))
                {
                    Debug.Log(text);
                }

                return false;
            }

            bool fail = false;

            for(int i = 0; i < controlMesh.edges.Length; i++)
            {
                HalfEdge edge         = controlMesh.edges[i];
                short    polygonIndex = edge.polygonIndex;

                if(polygonIndex < 0 || polygonIndex >= controlMesh.polygons.Length)
                {
                    if(!m_OutputToLog)
                    {
                        return false;
                    }

                    string text = $"{i}: no valid polygon-index found ({polygonIndex})";

                    if(prevOutput.Add(text))
                    {
                        Debug.Log(text);
                    }

                    fail = true;

                    continue;
                }

                int[] edgeIndices = controlMesh.polygons[polygonIndex].edgeIndices;

                if(edgeIndices == null)
                {
                    if(!m_OutputToLog)
                    {
                        return false;
                    }

                    string text = $"{polygonIndex}: polygon has no edges";

                    if(prevOutput.Add(text))
                    {
                        Debug.Log(text);
                    }

                    fail = true;

                    continue;
                }

                int edgeIndex = Array.IndexOf(edgeIndices, i);

                if(edgeIndex == -1)
                {
                    if(!m_OutputToLog)
                    {
                        return false;
                    }

                    string text = $"{i}: edge not found in polygon";

                    if(prevOutput.Add(text))
                    {
                        Debug.Log(text);
                    }

                    fail = true;

                    continue;
                }

                if(edgeIndices[edgeIndex] != i)
                {
                    if(!m_OutputToLog)
                    {
                        return false;
                    }

                    string text = $"{i}: polygon.edges[edgeIndex] != i";

                    if(prevOutput.Add(text))
                    {
                        Debug.Log(text);
                    }

                    fail = true;

                    continue;
                }

                int prev = controlMesh.GetPrevEdgeIndex(i);

                if(prev < 0 || prev >= controlMesh.edges.Length)
                {
                    if(!m_OutputToLog)
                    {
                        return false;
                    }

                    string text = $"{i}: prev < 0 || prev >= controlMesh.edges.Length";

                    if(prevOutput.Add(text))
                    {
                        Debug.Log(text);
                    }

                    fail = true;

                    continue;
                }

                HalfEdge prevEdge         = controlMesh.edges[prev];
                short    prevPolygonIndex = prevEdge.polygonIndex;

                if(prevPolygonIndex < 0 || prevPolygonIndex >= controlMesh.polygons.Length)
                {
                    if(!m_OutputToLog)
                    {
                        return false;
                    }

                    string text = $"{i}: prev_polygon_index < 0 || prev_polygon_index >= controlMesh.polygons.Length";

                    if(prevOutput.Add(text))
                    {
                        Debug.Log(text);
                    }

                    fail = true;

                    continue;
                }

                int curr = controlMesh.GetNextEdgeIndex(prev);

                if(curr != i)
                {
                    if(!m_OutputToLog)
                    {
                        return false;
                    }

                    string text = $"{i}: prev->next != i";

                    if(prevOutput.Add(text))
                    {
                        Debug.Log(text);
                    }

                    fail = true;
                }

                int twin = edge.twinIndex;

                if(twin < 0)
                {
                    if(!m_OutputToLog)
                    {
                        return false;
                    }

                    string text = $"{i}: twin ({twin}) < 0";

                    if(prevOutput.Add(text))
                    {
                        Debug.Log(text);
                    }

                    fail = true;
                }
                else if(twin >= controlMesh.edges.Length)
                {
                    if(!m_OutputToLog)
                    {
                        return false;
                    }

                    string text = $"{i}: twin ({twin}) >= edges.Length ({controlMesh.edges.Length})";

                    if(prevOutput.Add(text))
                    {
                        Debug.Log(text);
                    }

                    fail = true;
                }
                else
                {
                    short twinVertexIndex = controlMesh.edges[twin].vertexIndex;
                    short prevVertexIndex = controlMesh.edges[prev].vertexIndex;

                    if(twinVertexIndex != prevVertexIndex)
                    {
                        if(!m_OutputToLog)
                        {
                            return false;
                        }

                        string text = $"edges[twin({twin})].vertexIndex ({controlMesh.edges[twin].vertexIndex}) != edges[prev({prev})].vertexIndex ({controlMesh.edges[prev].vertexIndex})";

                        if(prevOutput.Add(text))
                        {
                            Debug.Log(text);
                        }

                        fail = true;
                    }

                    if(controlMesh.edges[twin].twinIndex != i)
                    {
                        if(!m_OutputToLog)
                        {
                            return false;
                        }

                        string text = $"{i}: edges[twin].twinIndex ({controlMesh.edges[twin].twinIndex}) != i ({i})";

                        if(prevOutput.Add(text))
                        {
                            Debug.Log(text);
                        }

                        fail = true;
                    }
                }
            }

            int texGenCount = shape.texGens.Length;

            if(m_UsedTexGens.Length < texGenCount)
            {
                m_UsedTexGens     = new bool[texGenCount];
                m_IndexConversion = new int[texGenCount];
            }

            Array.Clear(m_UsedTexGens, 0, m_UsedTexGens.Length);

            for(int p = 0; p < controlMesh.polygons.Length; p++)
            {
                int texGenIndex = controlMesh.polygons[p].texGenIndex;

                if(texGenIndex < 0)
                {
                    if(!m_OutputToLog)
                    {
                        return false;
                    }

                    string text = $"{p}: texGenIndex < 0";

                    if(prevOutput.Add(text))
                    {
                        Debug.Log(text);
                    }

                    fail = true;

                    continue;
                }

                m_UsedTexGens[texGenIndex] = true;
            }

            for(int t = 0; t < shape.texGens.Length; t++)
            {
                if(m_UsedTexGens[t])
                {
                    continue;
                }

                if(!m_OutputToLog)
                {
                    return false;
                }

                string text = $"texGenIndex {t} is not used";

                if(prevOutput.Add(text))
                {
                    Debug.Log(text);
                }

                fail = true;
            }

            Polygon[] polygons = controlMesh.polygons;

            if(shape.surfaces.Length != polygons.Length)
            {
                if(!m_OutputToLog)
                {
                    return false;
                }

                string text = $"this.Shape.Surfaces.Length ({shape.surfaces.Length}) != this.polygons.Length ({polygons.Length})";

                if(prevOutput.Add(text))
                {
                    Debug.Log(text);
                }

                fail = true;
            }

            for(int p = 0; p < polygons.Length; p++)
            {
                int[] edgeIndices = polygons[p].edgeIndices;

                for(int e1 = edgeIndices.Length - 1, e2 = 0; e2 < edgeIndices.Length; e1 = e2, e2++)
                {
                    int   polyEdgeIndex1    = edgeIndices[e1];
                    int   polyEdgeIndex2    = edgeIndices[e2];
                    short foundPolygonIndex = controlMesh.GetEdgePolygonIndex(polyEdgeIndex1);

                    if(foundPolygonIndex != p)
                    {
                        if(!m_OutputToLog)
                        {
                            return false;
                        }

                        string text = $"{p}: this.GetEdgePolygonIndex(edge_index{{{polyEdgeIndex1}}}){{{foundPolygonIndex}}} != polygon_index{{{p}}}";

                        if(prevOutput.Add(text))
                        {
                            Debug.Log(text);
                        }

                        fail = true;
                    }
                    else
                    {
                        short twinPolygonIndex = controlMesh.GetEdgePolygonIndex(controlMesh.GetTwinEdgeIndex(polyEdgeIndex1));

                        if(twinPolygonIndex == foundPolygonIndex)
                        {
                            if(!m_OutputToLog)
                            {
                                return false;
                            }

                            string text = $"{p}: twin_polygon_index{{{twinPolygonIndex}}} == found_polygon_index{{{foundPolygonIndex}}}";

                            if(prevOutput.Add(text))
                            {
                                Debug.Log(text);
                            }

                            fail = true;
                        }
                    }

                    short twinVertexIndex = controlMesh.GetTwinEdgeVertexIndex(polyEdgeIndex2);
                    short vertexIndex     = controlMesh.GetVertexIndex(polyEdgeIndex1);

                    if(twinVertexIndex == vertexIndex)
                    {
                        continue;
                    }

                    if(!m_OutputToLog)
                    {
                        return false;
                    }

                    string text2 = $"{p}: this.GetTwinEdgeVertexIndex({polyEdgeIndex2}) ({twinVertexIndex}) != this.GetVertexIndex({polyEdgeIndex1}) ({vertexIndex})";

                    if(prevOutput.Add(text2))
                    {
                        Debug.Log(text2);
                    }

                    fail = true;
                }
            }

            return !fail;
        }

        private static void AddNewPolygon(ControlMesh controlMesh, Shape shape, Polygon newPolygon)
        {
            ArrayUtility.Add(ref controlMesh.polygons, newPolygon);

            Surface newSurface = new Surface
            {
                Plane       = GeometryUtility.CalcPolygonPlane(controlMesh, (short)(controlMesh.polygons.Length - 1))
              , TexGenIndex = newPolygon.texGenIndex
            };

            GeometryUtility.CalculateTangents(newSurface.Plane.Normal, out newSurface.Tangent, out newSurface.BiNormal);

            ArrayUtility.Add(ref shape.surfaces, newSurface);
        }

        private static void AddNewPolygon(ControlMesh controlMesh, Shape shape, Polygon newPolygon, int surfaceIndex)
        {
            ArrayUtility.Add(ref controlMesh.polygons, newPolygon);
            Surface oldSurface = shape.surfaces[surfaceIndex];

            Surface newSurface = new Surface
            {
                BiNormal    = oldSurface.BiNormal
              , Plane       = oldSurface.Plane
              , Tangent     = oldSurface.Tangent
              , TexGenIndex = newPolygon.texGenIndex
            };

            ArrayUtility.Add(ref shape.surfaces, newSurface);
        }

        private static int[] GetEdgeRange(int[] edges, int fromValue, int toValue, int additionalStorage = 0)
        {
            int fromEdgeIndex = ArrayUtility.IndexOf(edges, fromValue);
            int toEdgeIndex   = ArrayUtility.IndexOf(edges, toValue);

            if(fromEdgeIndex == -1 || toEdgeIndex == -1)
            {
                return null;
            }

            if(fromEdgeIndex < toEdgeIndex)
            {
                int   length = toEdgeIndex - fromEdgeIndex + 1;
                int[] result = new int[length + additionalStorage];
                Array.Copy(edges, fromEdgeIndex, result, 0, length);

                return result;
            }
            else
            {
                int   offsetA = edges.Length - fromEdgeIndex;
                int   offsetB = toEdgeIndex + 1;
                int   length  = offsetA + offsetB;
                int[] result  = new int[length + additionalStorage];

                if(offsetA > 0)
                {
                    Array.Copy(edges, fromEdgeIndex, result, 0, offsetA);
                }

                Array.Copy(edges, 0, result, offsetA, offsetB);

                return result;
            }
        }

        public static int InsertEdgeBetweenEdges(ControlMesh controlMesh, Shape shape, int edgeIndex1, int edgeIndex2)
        {
            // create an edge between the two edges that splits an existing polygon into two

            if(edgeIndex1 == -1 || edgeIndex2 == -1)
            {
                Debug.LogWarning("edgeIndex1 == -1 || edgeIndex2 == -1");

                return -1; //edgeIndex
            }

            short edgePolygonIndex = controlMesh.GetEdgePolygonIndex(edgeIndex1);

            if(edgePolygonIndex == -1)
            {
                Debug.LogWarning("polygon_index == -1");

                return -1; //edgeIndex
            }

            if(controlMesh.GetEdgePolygonIndex(edgeIndex2) != edgePolygonIndex)
            {
                Debug.LogWarning("controlMesh.GetEdgePolygonIndex(edgeIndex2) != polygon_index");

                return -1; //edgeIndex
            }

            int[] edgeIndices = controlMesh.polygons[edgePolygonIndex].edgeIndices;

            int nextIndex1 = controlMesh.GetNextEdgeIndex(edgeIndex1);
            int nextIndex2 = controlMesh.GetNextEdgeIndex(edgeIndex2);

            if(nextIndex1 == edgeIndex2 || nextIndex2 == edgeIndex1)
            {
                Debug.LogWarning("next_index1 == edge_index2 || next_index2 == edge_index1");

                return -1; //edgeIndex
            }

            short vertexIndex1 = controlMesh.GetVertexIndex(edgeIndex1);
            short vertexIndex2 = controlMesh.GetVertexIndex(edgeIndex2);

            short oldPolygonIndex = edgePolygonIndex;
            short newPolygonIndex = (short)controlMesh.polygons.Length;

            int newEdgeIndex1 = controlMesh.edges.Length;
            int newEdgeIndex2 = controlMesh.edges.Length + 1;

            HalfEdge newEdge1 = new HalfEdge(newPolygonIndex, newEdgeIndex2, vertexIndex2);
            HalfEdge newEdge2 = new HalfEdge(oldPolygonIndex, newEdgeIndex1, vertexIndex1);

            // edge_index_2 leads to new_edge_2 leads to next_index_1 (new_polygon)
            int[] oldEdges = GetEdgeRange(edgeIndices, nextIndex1, edgeIndex2, 1);

            if(oldEdges == null)
            {
                Debug.LogWarning("old_edges == null");

                return -1; //edgeIndex
            }

            oldEdges[oldEdges.Length - 1] = newEdgeIndex2;

            // edge_index_1 leads to new_edge_1 leads to next_index_2 (old_polygon)
            int[] newEdges = GetEdgeRange(edgeIndices, nextIndex2, edgeIndex1, 1);

            if(newEdges == null)
            {
                Debug.LogWarning("new_edges == null");

                return -1; //edgeIndex
            }

            newEdges[newEdges.Length - 1] = newEdgeIndex1;

            ArrayUtility.Add(ref controlMesh.edges, newEdge1);
            ArrayUtility.Add(ref controlMesh.edges, newEdge2);

            for(int i = 0; i < oldEdges.Length; i++)
            {
                controlMesh.edges[oldEdges[i]].polygonIndex = oldPolygonIndex;
            }

            for(int i = 0; i < newEdges.Length; i++)
            {
                controlMesh.edges[newEdges[i]].polygonIndex = newPolygonIndex;
            }

            int     newTexGen  = CopyTexGen(shape, controlMesh.polygons[oldPolygonIndex].texGenIndex);
            Polygon newPolygon = new Polygon(newEdges, newTexGen);
            AddNewPolygon(controlMesh, shape, newPolygon, oldPolygonIndex);
            controlMesh.polygons[edgePolygonIndex].edgeIndices = oldEdges;

            return newEdgeIndex1;
        }

        public static short InsertControlpoint(ControlMesh controlMesh, Vector3 newPoint, int edgeIndex)
        {
            int      selfEdgeIndex    = edgeIndex;
            HalfEdge selfEdge         = controlMesh.edges[selfEdgeIndex];
            int[]    selfEdgeIndices  = controlMesh.polygons[selfEdge.polygonIndex].edgeIndices;
            int      selfPolygonIndex = ArrayUtility.IndexOf(selfEdgeIndices, selfEdgeIndex);

            int      twinEdgeIndex    = selfEdge.twinIndex;
            HalfEdge twinEdge         = controlMesh.edges[twinEdgeIndex];
            int[]    twinEdgeIndices  = controlMesh.polygons[twinEdge.polygonIndex].edgeIndices;
            int      twinPolygonIndex = ArrayUtility.IndexOf(twinEdgeIndices, twinEdgeIndex);

            short oldVertexIndex = selfEdge.vertexIndex;
            short newVertexIndex = (short)controlMesh.vertices.Length;
            ArrayUtility.Add(ref controlMesh.vertices, newPoint);

            //
            //        self
            //	o<--------------
            //   --------------->
            //        twin
            //

            //
            //   n-self    self
            //	o<----- x<------		x = new vertex
            //   ------>x ------>		o = old vertex
            //   n-twin     twin
            //

            int newSelfIndex = controlMesh.edges.Length;
            int newTwinIndex = newSelfIndex + 1;

            HalfEdge newEdge = new HalfEdge(selfEdge.polygonIndex, newTwinIndex, oldVertexIndex);
            HalfEdge newTwin = new HalfEdge(twinEdge.polygonIndex, newSelfIndex, newVertexIndex);

            ArrayUtility.Add(ref controlMesh.edges, newEdge);
            ArrayUtility.Add(ref controlMesh.edges, newTwin);

            controlMesh.edges[selfEdgeIndex].vertexIndex = newVertexIndex;
            ArrayUtility.Insert(ref selfEdgeIndices, selfPolygonIndex + 1, newSelfIndex);
            ArrayUtility.Insert(ref twinEdgeIndices, twinPolygonIndex, newTwinIndex);

            controlMesh.polygons[selfEdge.polygonIndex].edgeIndices = selfEdgeIndices;
            controlMesh.polygons[twinEdge.polygonIndex].edgeIndices = twinEdgeIndices;

            return newVertexIndex;
        }

        private static int FirstEdgeWithVertex(ControlMesh controlMesh, int vertexIndex)
        {
            for(int i = 0; i < controlMesh.edges.Length; i++)
            {
                if(controlMesh.edges[i].vertexIndex == vertexIndex)
                {
                    return i;
                }
            }

            return -1; //edgeIndex
        }

        private static void RemoveEdges(ControlMesh controlMesh, HashSet<int> edges)
        {
            if(edges.Count == 0)
            {
                return;
            }

            List<int> sortedRemoveEdges = new List<int>(edges);
            sortedRemoveEdges.Sort();

            for(int s = sortedRemoveEdges.Count - 1; s >= 0; s--)
            {
                int value = sortedRemoveEdges[s];
                ArrayUtility.RemoveAt(ref controlMesh.edges, value);
            }

            for(int p = 0; p < controlMesh.polygons.Length; p++)
            {
                int[] edgeIndices = controlMesh.polygons[p].edgeIndices;

                for(int i = 0; i < edgeIndices.Length; i++)
                {
                    int original = edgeIndices[i];

                    for(int s = sortedRemoveEdges.Count - 1; s >= 0; s--)
                    {
                        int value = sortedRemoveEdges[s];

                        if(original < value)
                        {
                            continue;
                        }

                        original -= s + 1;

                        break;
                    }

                    edgeIndices[i] = original;
                }
            }

            for(int i = 0; i < controlMesh.edges.Length; i++)
            {
                int original = controlMesh.edges[i].twinIndex;

                for(int s = sortedRemoveEdges.Count - 1; s >= 0; s--)
                {
                    int value = sortedRemoveEdges[s];

                    if(original < value)
                    {
                        continue;
                    }

                    original -= s + 1;

                    break;
                }

                controlMesh.edges[i].twinIndex = original;
            }
        }

        public static void CalculatePlanes(ControlMesh controlMesh, Shape shape)
        {
            for(short p = 0; p < controlMesh.polygons.Length; p++)
            {
                shape.surfaces[p].Plane = GeometryUtility.CalcPolygonPlane(controlMesh, p);
            }
        }

        private static void RemoveVertex(ControlMesh controlMesh, short vertexIndex)
        {
            ArrayUtility.RemoveAt(ref controlMesh.vertices, vertexIndex);

            for(int i = 0; i < controlMesh.edges.Length; i++)
            {
                if(controlMesh.edges[i].vertexIndex >= vertexIndex)
                {
                    controlMesh.edges[i].vertexIndex--;
                }
            }
        }

        private static void RemoveEdgeFromPolygon(ControlMesh controlMesh, int edgeIndex, ref HashSet<int> edges)
        {
            if(!edges.Add(edgeIndex))
            {
                return;
            }

            short polyIndex = controlMesh.edges[edgeIndex].polygonIndex;
            ArrayUtility.Remove(ref controlMesh.polygons[polyIndex].edgeIndices, edgeIndex);
        }

        private static void RemovePolygons(ControlMesh controlMesh, Shape shape, HashSet<short> polygons)
        {
            List<short> sortedRemovePolygons = new List<short>(polygons);
            sortedRemovePolygons.Sort();

            for(int s = sortedRemovePolygons.Count - 1; s >= 0; s--)
            {
                short value = sortedRemovePolygons[s];
                ArrayUtility.RemoveAt(ref controlMesh.polygons, value);
                ArrayUtility.RemoveAt(ref shape.surfaces, value);
            }

            for(int p = 0; p < controlMesh.polygons.Length; p++)
            {
                int[] edgeIndices = controlMesh.polygons[p].edgeIndices;

                for(int i = 0; i < edgeIndices.Length; i++)
                {
                    controlMesh.edges[edgeIndices[i]].polygonIndex = (short)p;
                }
            }
        }

        // assumes mergePoints is sorted
        public static void RemoveControlPoints(ControlMesh controlMesh, Shape shape, short[] sortedRemovePoints)
        {
            for(int i = sortedRemovePoints.Length - 1; i >= 0; i--)
            {
                RemoveControlPoint(controlMesh, shape, sortedRemovePoints[i]);
            }
        }

        private static void RemoveControlPoint(ControlMesh controlMesh, Shape shape, short indexOfVertexToRemove)
        {
            int firstEdgeIndex = FirstEdgeWithVertex(controlMesh, indexOfVertexToRemove);

            if(firstEdgeIndex == -1)
            {
                return;
            }

            List<int>    edgeIndices = new List<int>();
            HashSet<int> tested      = new HashSet<int>();

            int iterator = firstEdgeIndex;

            do
            {
                if(!tested.Add(iterator))
                {
                    break;
                }

                edgeIndices.Add(iterator);
                iterator = controlMesh.GetNextEdgeIndexAroundVertex(iterator);
            }
            while(iterator != firstEdgeIndex);

            {
                HashSet<int> edges = new HashSet<int>();

                short newPolygonIndex = (short)controlMesh.polygons.Length;

                List<int> newEdges = new List<int>();

                for(int i = 0, j = edgeIndices.Count - 1; j >= 0; i = j, j--)
                {
                    int   currIndex   = edgeIndices[i];
                    short vertexIndex = controlMesh.GetTwinEdgeVertexIndex(edgeIndices[j]);
                    int   twinIndex   = controlMesh.GetTwinEdgeIndex(currIndex);
                    int   edgeIndex   = controlMesh.edges.Length;

                    ArrayUtility.Add(ref controlMesh.edges, new HalfEdge(newPolygonIndex, twinIndex, vertexIndex));

                    newEdges.Add(edgeIndex);
                    edges.Add(currIndex);
                }

                for(int e = 0; e < newEdges.Count; e++)
                {
                    int index = newEdges[e];
                    controlMesh.edges[controlMesh.edges[index].twinIndex].twinIndex = index;
                }

                AddNewPolygon(controlMesh, shape, new Polygon(newEdges.ToArray(), CreateTexGen(shape)));

                HashSet<short> polygons = new HashSet<short>();

                foreach(int index in edges)
                {
                    short polygonIndex = controlMesh.edges[index].polygonIndex;
                    ArrayUtility.Remove(ref controlMesh.polygons[polygonIndex].edgeIndices, index);
                }

                for(int p = 0; p < controlMesh.polygons.Length; p++)
                {
                    int[] polyEdgeIndices = controlMesh.polygons[p].edgeIndices;

                    switch(polyEdgeIndices.Length)
                    {
                        case 0:
                        {
                            controlMesh.polygons[newPolygonIndex].texGenIndex = controlMesh.polygons[p].texGenIndex;
                            polygons.Add((short)p);

                            break;
                        }
                        case 1:
                        {
                            controlMesh.polygons[newPolygonIndex].texGenIndex = controlMesh.polygons[p].texGenIndex;
                            RemoveUnusedTexGens(controlMesh, shape);
                            int selfEdgeIndex0 = polyEdgeIndices[0];
                            int twinEdgeIndex0 = controlMesh.GetTwinEdgeIndex(selfEdgeIndex0);
                            RemoveEdgeFromPolygon(controlMesh, selfEdgeIndex0, ref edges);
                            RemoveEdgeFromPolygon(controlMesh, twinEdgeIndex0, ref edges);
                            polygons.Add((short)p);

                            break;
                        }
                        case 2:
                        {
                            controlMesh.polygons[newPolygonIndex].texGenIndex = controlMesh.polygons[p].texGenIndex;
                            RemoveUnusedTexGens(controlMesh, shape);
                            int selfEdgeIndex0 = polyEdgeIndices[0];
                            int selfEdgeIndex1 = polyEdgeIndices[1];
                            int twinEdgeIndex0 = controlMesh.GetTwinEdgeIndex(selfEdgeIndex0);
                            int twinEdgeIndex1 = controlMesh.GetTwinEdgeIndex(selfEdgeIndex1);
                            controlMesh.edges[twinEdgeIndex0].twinIndex = twinEdgeIndex1;
                            controlMesh.edges[twinEdgeIndex1].twinIndex = twinEdgeIndex0;
                            RemoveEdgeFromPolygon(controlMesh, selfEdgeIndex0, ref edges);
                            RemoveEdgeFromPolygon(controlMesh, selfEdgeIndex1, ref edges);
                            polygons.Add((short)p);

                            break;
                        }
                    }
                }

                if(polygons.Count != 0)
                {
                    int smallestTexGen = int.MaxValue;

                    foreach(short polygonIndex in polygons)
                    {
                        if(controlMesh.polygons[polygonIndex].texGenIndex >= 0)
                        {
                            smallestTexGen = Math.Min(controlMesh.polygons[polygonIndex].texGenIndex, smallestTexGen);
                        }
                    }

                    if(smallestTexGen != int.MaxValue)
                    {
                        controlMesh.polygons[newPolygonIndex].texGenIndex = smallestTexGen;
                    }

                    RemovePolygons(controlMesh, shape, polygons);
                }
                else
                {
                    controlMesh.polygons[newPolygonIndex].texGenIndex = CopyTexGen(shape, 0);
                }

                if(edges.Count != 0)
                {
                    RemoveEdges(controlMesh, edges);
                }

                // check if vertex is still being used or not
                // (would hide issues if we removed it when it's still being used somehow)
                firstEdgeIndex = FirstEdgeWithVertex(controlMesh, indexOfVertexToRemove);

                if(firstEdgeIndex == -1)
                {
                    RemoveVertex(controlMesh, indexOfVertexToRemove);
                }

                RemoveUnusedTexGens(controlMesh, shape);

                if(controlMesh.polygons.Length == 0 || controlMesh.edges.Length == 0 || controlMesh.vertices.Length == 0)
                {
                    if(controlMesh.polygons.Length != 0)
                    {
                        controlMesh.polygons = Array.Empty<Polygon>();
                    }

                    if(controlMesh.edges.Length != 0)
                    {
                        controlMesh.edges = Array.Empty<HalfEdge>();
                    }

                    if(controlMesh.vertices.Length != 0)
                    {
                        controlMesh.vertices = Array.Empty<Vector3>();
                    }

                    shape.Reset();
                }

                Validate(controlMesh, shape);
            }
        }

        private static int CreateTexGen(Shape shape)
        {
            TexGen texGen = new TexGen(CSGSettings.DefaultMaterial);
            ArrayUtility.Add(ref shape.texGens, texGen);
            ArrayUtility.Add(ref shape.texGenFlags, CSGSettings.DefaultTexGenFlags);

            return shape.texGens.Length - 1;
        }

        private static int CopyTexGen(Shape shape, int srcTexGenIndex)
        {
            TexGen texGen = shape.texGens[srcTexGenIndex];
            ArrayUtility.Add(ref shape.texGens, texGen);
            ArrayUtility.Add(ref shape.texGenFlags, shape.texGenFlags[srcTexGenIndex]);

            return shape.texGens.Length - 1;
        }

        private static bool[] m_UsedTexGens = Array.Empty<bool>();
        private static int[] m_IndexConversion = Array.Empty<int>();

        public static void RemoveUnusedTexGens(ControlMesh controlMesh, Shape shape)
        {
            int       texGenCount = shape.texGens.Length;
            Polygon[] polygons    = controlMesh.polygons;

            for(int p = 0; p < polygons.Length; p++)
            {
                if(polygons[p].texGenIndex < 0 || polygons[p].texGenIndex >= texGenCount)
                {
                    polygons[p].texGenIndex = CreateTexGen(shape);
                }
            }

            if(m_UsedTexGens.Length < texGenCount)
            {
                m_UsedTexGens     = new bool[texGenCount];
                m_IndexConversion = new int[texGenCount];
            }

            Array.Clear(m_UsedTexGens, 0, m_UsedTexGens.Length);

            for(int p = 0; p < polygons.Length; p++)
            {
                m_UsedTexGens[polygons[p].texGenIndex] = true;
            }

            int materialCount = texGenCount;

            for(int i = 0; i < materialCount; i++)
            {
                if(m_UsedTexGens[i])
                {
                    m_IndexConversion[i] = i;

                    continue;
                }

                int found = -1; //texgen

                for(int j = materialCount - 1; j > i; j--)
                {
                    if(!m_UsedTexGens[j])
                    {
                        continue;
                    }

                    found = j;

                    break;
                }

                if(found == -1) //texgen
                {
                    break;
                }

                m_IndexConversion[found] = i;

                shape.texGenFlags[i] = shape.texGenFlags[found];
                shape.texGens[i]     = shape.texGens[found];

                m_UsedTexGens[i]     = true;
                m_UsedTexGens[found] = false;
                materialCount--;
            }

            if(materialCount == texGenCount)
            {
                return;
            }

            for(int i = 0; i < controlMesh.polygons.Length; i++)
            {
                int texGenIndex = controlMesh.polygons[i].texGenIndex;
                controlMesh.polygons[i].texGenIndex = m_IndexConversion[texGenIndex];
            }

            for(int i = texGenCount - 1; i >= 0; i--)
            {
                if(m_UsedTexGens[i])
                {
                    break;
                }

                ArrayUtility.RemoveAt(ref shape.texGenFlags, i);
                ArrayUtility.RemoveAt(ref shape.texGens, i);
            }

            for(int i = 0, maxI = controlMesh.polygons.Length - 1; i < maxI; i++)
            {
                for(int j = i + 1, maxJ = controlMesh.polygons.Length; j < maxJ; j++)
                {
                    if(controlMesh.polygons[i].texGenIndex > controlMesh.polygons[j].texGenIndex)
                    {
                        (shape.texGenFlags[i], shape.texGenFlags[j]) = (shape.texGenFlags[j], shape.texGenFlags[i]);
                        (shape.texGens[i], shape.texGens[j])         = (shape.texGens[j], shape.texGens[i]);

                        (controlMesh.polygons[i].texGenIndex, controlMesh.polygons[j].texGenIndex) =
                            (controlMesh.polygons[j].texGenIndex, controlMesh.polygons[i].texGenIndex);
                    }
                }
            }

            for(int i = 0; i < controlMesh.polygons.Length; i++)
            {
                shape.surfaces[i].TexGenIndex = controlMesh.polygons[i].texGenIndex;
            }
        }

        public static void FixTexGens(ControlMesh controlMesh, Shape shape)
        {
            int texGenCount = shape.texGens.Length;

            if(m_UsedTexGens.Length < texGenCount)
            {
                m_UsedTexGens     = new bool[texGenCount];
                m_IndexConversion = new int[texGenCount];
            }

            Array.Clear(m_UsedTexGens, 0, m_UsedTexGens.Length);

            for(int p = 0; p < controlMesh.polygons.Length; p++)
            {
                int texGenIndex = controlMesh.polygons[p].texGenIndex;

                if(texGenIndex < 0 || texGenIndex >= shape.texGens.Length)
                {
                    texGenIndex = CreateTexGen(shape);

                    controlMesh.polygons[p].texGenIndex = texGenIndex;
                }

                m_UsedTexGens[texGenIndex] = true;
            }

            for(int t = shape.texGens.Length - 1; t >= 0; t--)
            {
                if(m_UsedTexGens[t])
                {
                    continue;
                }

                ArrayUtility.RemoveAt(ref shape.texGens, t);
                ArrayUtility.RemoveAt(ref shape.texGenFlags, t);

                // TODO: there's probably a better way of doing this ..
                for(int p = 0; p < controlMesh.polygons.Length; p++)
                {
                    if(controlMesh.polygons[p].texGenIndex > t)
                    {
                        controlMesh.polygons[p].texGenIndex--;
                    }
                }

                for(int s = 0; s < shape.surfaces.Length; s++)
                {
                    if(shape.surfaces[s].TexGenIndex > t)
                    {
                        shape.surfaces[s].TexGenIndex--;
                    }
                }
            }
        }

        public static bool SplitEdgeLoop(ControlMesh controlMesh, Shape shape, List<int> edgeLoop)
        {
            int[] polygon1Edges = new int[edgeLoop.Count];
            int[] polygon2Edges = new int[edgeLoop.Count];
            short polygon1Index = (short)controlMesh.polygons.Length;
            short polygon2Index = (short)(controlMesh.polygons.Length + 1);

            for(int i = 0; i < edgeLoop.Count; i++)
            {
                int   currEdgeIndex   = edgeLoop[i];
                int   twinEdgeIndex   = controlMesh.GetTwinEdgeIndex(currEdgeIndex);
                short currVertexIndex = controlMesh.GetVertexIndex(currEdgeIndex);
                short twinVertexIndex = controlMesh.GetVertexIndex(twinEdgeIndex);

                HalfEdge newEdge1      = new HalfEdge(polygon1Index, currEdgeIndex, twinVertexIndex);
                HalfEdge newEdge2      = new HalfEdge(polygon2Index, twinEdgeIndex, currVertexIndex);
                int      newEdgeIndex1 = controlMesh.edges.Length;
                int      newEdgeIndex2 = controlMesh.edges.Length + 1;
                ArrayUtility.Add(ref controlMesh.edges, newEdge1);
                ArrayUtility.Add(ref controlMesh.edges, newEdge2);

                controlMesh.edges[currEdgeIndex].twinIndex = newEdgeIndex1;
                controlMesh.edges[twinEdgeIndex].twinIndex = newEdgeIndex2;

                polygon1Edges[edgeLoop.Count - 1 - i] = newEdgeIndex1;
                polygon2Edges[i]                      = newEdgeIndex2;
            }

            Polygon polygon1 = new Polygon(polygon1Edges, CreateTexGen(shape));
            Polygon polygon2 = new Polygon(polygon2Edges, CreateTexGen(shape));

            AddNewPolygon(controlMesh, shape, polygon1);
            AddNewPolygon(controlMesh, shape, polygon2);

            for(int i = 0; i < polygon2.edgeIndices.Length; i++)
            {
                int     edgeIndex       = polygon2.edgeIndices[i];
                short   edgeVertexIndex = controlMesh.GetVertexIndex(edgeIndex);
                Vector3 edgeVertex      = controlMesh.vertices[edgeVertexIndex];
                short   newVertexIndex  = (short)controlMesh.vertices.Length;

                ArrayUtility.Add(ref controlMesh.vertices, edgeVertex);

                int iterator = edgeIndex;

                do
                {
                    controlMesh.edges[iterator].vertexIndex = newVertexIndex;

                    iterator = controlMesh.GetNextEdgeIndexAroundVertex(iterator);
                }
                while(iterator != edgeIndex);
            }

            Validate(controlMesh, shape);

            return true;
        }

        private const float DISTANCE_EPSILON = 0.0001f;

        private struct VertexSide
        {
            public float distance;
            public int halfspace;
        }

        private struct TraversalSide
        {
            public VertexSide side;
            public int edgeIndex;
        }

        private struct PlaneTraversals
        {
            public TraversalSide traversal0;
            public TraversalSide traversal1;
        }

        public static bool CutMesh(ControlMesh controlMesh, Shape shape, CSGPlane cuttingPlane, ref List<int> intersectedEdges)
        {
            if(shape.surfaces == null || shape.surfaces.Length != controlMesh.polygons.Length)
            {
                CalculatePlanes(controlMesh, shape);
            }

            // TODO: make sure we don't cut the mesh on a single polygon
            // TODO: do not store controlMesh anymore and calculate it in the loop instead

            VertexSide[] vertexDistances = new VertexSide[controlMesh.vertices.Length];

            for(int p = 0; p < controlMesh.vertices.Length; p++)
            {
                float distance  = cuttingPlane.Distance(controlMesh.vertices[p]);
                short halfspace = (short)(distance < -DISTANCE_EPSILON ? -1 : distance > DISTANCE_EPSILON ? 1 : 0);
                vertexDistances[p].distance  = distance;
                vertexDistances[p].halfspace = halfspace;
            }

            if(intersectedEdges != null)
            {
                intersectedEdges.Clear();
            }

            PlaneTraversals[] intersections = new PlaneTraversals[8];

            for(int p = controlMesh.polygons.Length - 1; p >= 0; p--)
            {
                // TODO: use polygon AABB tests

                int[] edgeIndices = controlMesh.polygons[p].edgeIndices;

                PlaneTraversals intersection;

                {
                    int   index0       = edgeIndices.Length - 1;
                    int   edgeIndex0   = edgeIndices[index0];
                    short vertexIndex0 = controlMesh.edges[edgeIndex0].vertexIndex;

                    intersection.traversal1.side      = vertexDistances[vertexIndex0];
                    intersection.traversal1.edgeIndex = edgeIndex0;
                }

                int intersectionCount = 0;

                for(int index1 = 0; index1 < edgeIndices.Length; index1++)
                {
                    intersection.traversal0 = intersection.traversal1;

                    // TODO: this could be optimized if this.polygons and this.edges are combined
                    //      (index/edge_index would be equivalent, edge w/ vertexIndex in 'polygon', fewer cache misses)
                    int   edgeIndex1   = edgeIndices[index1];
                    short vertexIndex1 = controlMesh.edges[edgeIndex1].vertexIndex;

                    intersection.traversal1.side      = vertexDistances[vertexIndex1];
                    intersection.traversal1.edgeIndex = edgeIndex1;

                    if(intersection.traversal0.side.halfspace != intersection.traversal1.side.halfspace)
                    {
                        if(intersectionCount + 1 >= intersections.Length)
                        {
                            PlaneTraversals[] newIntersections = new PlaneTraversals[Mathf.Min(intersections.Length, 8) * 2];

                            if(intersections.Length > 0)
                            {
                                Array.Copy(intersections, newIntersections, intersections.Length);
                            }

                            intersections = newIntersections;
                        }

                        intersections[intersectionCount] = intersection;
                        intersectionCount++;
                    }
                    else if(intersectedEdges != null)
                    {
                        if(intersection.traversal0.side.halfspace == 0)
                        {
                            intersectedEdges.Add(edgeIndex1);
                        }
                    }
                }

                if(intersectionCount == 1)
                {
                    Debug.LogWarning("Number of edges criss-crossing the plane boundary is 1 (should not be possible)!?");

                    return false;
                }

                if(intersectionCount > 0)
                {
                    for(int i0 = intersectionCount - 1, i1 = 0; i1 < intersectionCount; i0 = i1, i1++)
                    {
                        if(intersections[i0].traversal1.side.halfspace != intersections[i1].traversal0.side.halfspace || intersections[i0].traversal1.side.halfspace != 0)
                        {
                            continue;
                        }

                        // Note: we know traversal0.side.halfspace and traversal1.side.halfspace are always different from each other.

                        if(intersections[i0].traversal0.side.halfspace != intersections[i1].traversal1.side.halfspace)
                        {
                            continue;
                        }

                        // possibilities: (can have multiple vertices on the plane between intersections)
                        //
                        //       outside				      outside
                        //								       0      1
                        //       1  0					        \    /
                        //  .....*..*....... intersect	 ........*..*.... intersect
                        //      /    \					         1  0
                        //     0      1
                        //        inside				      inside

                        intersectionCount--;

                        if(i0 > i1)
                        {
                            if(intersectionCount - i0 > 0)
                            {
                                Array.Copy(intersections, i0 + 1, intersections, i0, intersectionCount - i0);
                            }

                            intersectionCount--;

                            if(intersectionCount - i1 > 0)
                            {
                                Array.Copy(intersections, i1 + 1, intersections, i1, intersectionCount - i1);
                            }
                        }
                        else
                        {
                            if(intersectionCount - i1 > 0)
                            {
                                Array.Copy(intersections, i1 + 1, intersections, i1, intersectionCount - i1);
                            }

                            intersectionCount--;

                            if(intersectionCount - i0 > 0)
                            {
                                Array.Copy(intersections, i0 + 1, intersections, i0, intersectionCount - i0);
                            }
                        }
                    }
                }

                // Find all traversals that go straight from one side to the other side.
                //	Create a new intersection point there, split traversals into two traversals.
                if(intersectionCount > 0)
                {
                    for(int i0 = 0; i0 < intersectionCount; i0++)
                    {
                        // Note: we know traversal0.side.halfspace and traversal1.side.halfspace are always different from each other.

                        //float timeT = Time.realtimeSinceStartup;
                        if(intersections[i0].traversal0.side.halfspace == 0 || intersections[i0].traversal1.side.halfspace == 0)
                        {
                            continue;
                        }

                        // possibilities:
                        //
                        //       outside                      outside
                        //       0                                 1
                        //        \                               /
                        //  .......\......... intersect  ......../....... intersect
                        //          \                           /
                        //           1                         0
                        //        inside                      inside

                        // Calculate intersection of edge with plane split the edge into two, inserting the new vertex

                        int     edgeIndex0   = intersections[i0].traversal0.edgeIndex;
                        int     edgeIndex1   = intersections[i0].traversal1.edgeIndex;
                        short   vertexIndex0 = controlMesh.edges[edgeIndex0].vertexIndex;
                        short   vertexIndex1 = controlMesh.edges[edgeIndex1].vertexIndex;
                        Vector3 vertex0      = controlMesh.vertices[vertexIndex0];
                        Vector3 vertex1      = controlMesh.vertices[vertexIndex1];

                        Vector3 newVertex;

                        if(intersections[i0].traversal0.side.halfspace < 0)
                        {
                            // possibilities:
                            //
                            //       outside
                            //            1
                            //           /
                            //  ......../....... intersect
                            //         /
                            //        0
                            //       inside

                            Vector3 vector = vertex0 - vertex1;

                            float length = intersections[i0].traversal0.side.distance - intersections[i0].traversal1.side.distance;

                            float delta = intersections[i0].traversal0.side.distance / length;
                            newVertex = vertex0 - vector * delta;
                        }
                        else
                        {
                            // possibilities:
                            //
                            //       outside
                            //            0
                            //           /
                            //  ......../....... intersect
                            //         /
                            //        1
                            //       inside
                            Vector3 vector = vertex1 - vertex0;

                            float length = intersections[i0].traversal1.side.distance - intersections[i0].traversal0.side.distance;

                            float delta = intersections[i0].traversal1.side.distance / length;
                            newVertex = vertex1 - vector * delta;
                        }

                        //short new_vertex_index =
                        InsertControlpoint(controlMesh, newVertex, edgeIndex1);
                        int replacedEdgeIndex = controlMesh.GetNextEdgeIndex(edgeIndex1);
                        int newEdgeIndex      = controlMesh.GetNextEdgeIndex(edgeIndex0);

                        for(int i1 = i0 + 1; i1 < intersectionCount; i1++)
                        {
                            if(intersections[i1].traversal0.edgeIndex == edgeIndex1)
                            {
                                intersections[i1].traversal0.edgeIndex = replacedEdgeIndex;
                            }

                            if(intersections[i1].traversal1.edgeIndex == edgeIndex1)
                            {
                                intersections[i1].traversal1.edgeIndex = replacedEdgeIndex;
                            }
                        }

                        PlaneTraversals newIntersection = new PlaneTraversals
                        {
                            traversal0 = intersections[i0].traversal0
                          , traversal1 =
                            {
                                edgeIndex = newEdgeIndex
                              , side =
                                {
                                    distance  = 0
                                  , halfspace = 0
                                }
                            }
                        };

                        ArrayUtility.Add(ref vertexDistances, newIntersection.traversal1.side);

                        intersections[i0].traversal0 = newIntersection.traversal1;

                        ArrayUtility.Insert(ref intersections, i0, newIntersection);
                        i0++;
                        intersectionCount++;
                    }
                }

                // NOTE: from this point on traversal.Index may no longer be valid!

                if(intersectionCount <= 1)
                {
                    goto skip_to_next_polygon;
                }

                // check if intersection_count is even
                if((intersectionCount & 1) == 1)
                {
                    Debug.LogWarning("Found an uneven number of edge-plane traversals??");

                    return false;
                }

                if(intersectionCount != 4)
                {
                    return false;
                }

                if(intersections[0].traversal0.side.halfspace == 0)
                {
                    intersections[intersectionCount + 1] = intersections[0];
                    Array.Copy(intersections, 1, intersections, 0, intersectionCount);
                }

                // TODO: find all traversal pairs and create edges between them
                int indexOut, indexIn;

                if(intersections[0].traversal0.side.halfspace < 0)
                {
                    indexOut = intersections[1].traversal0.edgeIndex;
                    indexIn  = intersections[2].traversal1.edgeIndex;
                }
                else
                {
                    indexOut = intersections[2].traversal1.edgeIndex;
                    indexIn  = intersections[1].traversal0.edgeIndex;
                }

                int newEdge = InsertEdgeBetweenEdges(controlMesh, shape, indexOut, indexIn);

                if(newEdge == -1)
                {
                    return false;
                }

                if(intersectedEdges != null)
                {
                    intersectedEdges.Add(newEdge);
                    intersectedEdges.Add(controlMesh.GetTwinEdgeIndex(newEdge));
                }

                skip_to_next_polygon: ;
            }

            return true;
        }

        private static bool IsPolygonInsideHalfSpace(CSGPlane cuttingPlane, ControlMesh controlMesh, int polygonIndex)
        {
            HalfEdge[] edges       = controlMesh.edges;
            Vector3[]  vertices    = controlMesh.vertices;
            int[]      edgeIndices = controlMesh.polygons[polygonIndex].edgeIndices;
            bool       onPlane     = true;

            for(int e = 0; e < edgeIndices.Length; e++)
            {
                int     edgeIndex   = edgeIndices[e];
                short   vertexIndex = edges[edgeIndex].vertexIndex;
                Vector3 vertex      = vertices[vertexIndex];

                if(cuttingPlane.Distance(vertex) >= MathConstants.DISTANCE_EPSILON)
                {
                    return false;
                }

                if(cuttingPlane.Distance(vertex) < -MathConstants.DISTANCE_EPSILON)
                {
                    onPlane = false;
                }
            }

            if(!onPlane)
            {
                return true;
            }

            for(int e0 = 0; e0 < edgeIndices.Length; e0++)
            {
                int   edgeIndex        = edgeIndices[e0];
                int   twinIndex        = edges[edgeIndex].twinIndex;
                short twinPolygonIndex = edges[twinIndex].polygonIndex;
                int[] twinEdgeIndices  = controlMesh.polygons[twinPolygonIndex].edgeIndices;

                for(int e1 = 0; e1 < twinEdgeIndices.Length; e1++)
                {
                    short   vertexIndex = edges[twinEdgeIndices[e1]].vertexIndex;
                    Vector3 vertex      = vertices[vertexIndex];

                    if(cuttingPlane.Distance(vertex) >= MathConstants.DISTANCE_EPSILON)
                    {
                        return false;
                    }

                    if(cuttingPlane.Distance(vertex) < -MathConstants.DISTANCE_EPSILON)
                    {
                        break;
                    }
                }
            }

            return true;
        }

        // find piece of this control-mesh that is completely separate,
        // remove it and return it as a new control-mesh.
        public static bool FindAndDetachSeparatePiece(ControlMesh controlMesh, Shape shape, CSGPlane cuttingPlane, out ControlMesh newControlMesh, out Shape newShape)
        {
            HashSet<short> foundPolygons = new HashSet<short>();
            List<int>      checkEdges    = new List<int>();
            HashSet<int>   checkedEdges  = new HashSet<int>();

            short startPolygon = -1;

            for(int i = controlMesh.polygons.Length - 1; i >= 0; i--)
            {
                if(!IsPolygonInsideHalfSpace(cuttingPlane, controlMesh, i))
                {
                    continue;
                }

                startPolygon = (short)i;

                break;
            }

            if(startPolygon == -1)
            {
                newControlMesh = null;
                newShape       = null;

                return false;
            }

            foundPolygons.Add(startPolygon);

            int[] polygonEdges = controlMesh.polygons[startPolygon].edgeIndices;

            for(int i = 0; i < polygonEdges.Length; i++)
            {
                int edgeIndex = polygonEdges[i];
                checkedEdges.Add(edgeIndex);
                int edgeTwinIndex = controlMesh.GetTwinEdgeIndex(edgeIndex);
                checkEdges.Add(edgeTwinIndex);
            }

            while(checkEdges.Count > 0)
            {
                int checkEdge = checkEdges[checkEdges.Count - 1];
                checkEdges.RemoveAt(checkEdges.Count - 1);
                checkedEdges.Add(checkEdge);

                short checkEdgePolygon = controlMesh.GetEdgePolygonIndex(checkEdge);

                if(!foundPolygons.Add(checkEdgePolygon))
                {
                    continue;
                }

                if(foundPolygons.Count == controlMesh.polygons.Length)
                {
                    newControlMesh = null;
                    newShape       = null;

                    return false;
                }

                polygonEdges = controlMesh.polygons[checkEdgePolygon].edgeIndices;

                for(int i = 0; i < polygonEdges.Length; i++)
                {
                    int edgeIndex     = polygonEdges[i];
                    int edgeTwinIndex = controlMesh.GetTwinEdgeIndex(edgeIndex);

                    if(!checkedEdges.Contains(edgeTwinIndex))
                    {
                        checkEdges.Add(edgeTwinIndex);
                    }
                }
            }

            short[] lookupVertexIndicesA  = new short[controlMesh.vertices.Length];
            int[]   lookupEdgeIndicesA    = new int [controlMesh.edges.Length];
            short[] lookupPolygonIndicesA = new short[controlMesh.polygons.Length];

            short[] lookupVertexIndicesB  = new short[controlMesh.vertices.Length];
            int[]   lookupEdgeIndicesB    = new int [controlMesh.edges.Length];
            short[] lookupPolygonIndicesB = new short[controlMesh.polygons.Length];

            foreach(short polygonIndex in foundPolygons)
            {
                lookupPolygonIndicesA[polygonIndex] = 1;

                polygonEdges = controlMesh.polygons[polygonIndex].edgeIndices;

                for(int i = 0; i < polygonEdges.Length; i++)
                {
                    int edgeIndex = polygonEdges[i];
                    lookupEdgeIndicesA[edgeIndex] = 1;
                    short vertexIndex = controlMesh.GetVertexIndex(edgeIndex);
                    lookupVertexIndicesA[vertexIndex] = 1;
                }
            }

            short vertexLengthA = 0;
            short vertexLengthB = 0;

            List<Vector3> newVerticesA = new List<Vector3>(controlMesh.vertices.Length);
            List<Vector3> newVerticesB = new List<Vector3>(controlMesh.vertices.Length);

            for(int i = 0; i < lookupVertexIndicesA.Length; i++)
            {
                if(lookupVertexIndicesA[i] == 0)
                {
                    lookupVertexIndicesA[i] = -1;
                    lookupVertexIndicesB[i] = vertexLengthB;
                    vertexLengthB++;
                    newVerticesB.Add(controlMesh.vertices[i]);
                }
                else
                {
                    lookupVertexIndicesB[i] = -1;
                    lookupVertexIndicesA[i] = vertexLengthA;
                    vertexLengthA++;
                    newVerticesA.Add(controlMesh.vertices[i]);
                }
            }

            Vector3[] newVertexArrayA = newVerticesA.ToArray();
            Vector3[] newVertexArrayB = newVerticesB.ToArray();

            short polygonLengthA = 0;
            short polygonLengthB = 0;

            for(int i = 0; i < lookupPolygonIndicesA.Length; i++)
            {
                if(lookupPolygonIndicesA[i] == 0)
                {
                    lookupPolygonIndicesA[i] = -1;
                    lookupPolygonIndicesB[i] = polygonLengthB;
                    polygonLengthB++;
                }
                else
                {
                    lookupPolygonIndicesB[i] = -1;
                    lookupPolygonIndicesA[i] = polygonLengthA;
                    polygonLengthA++;
                }
            }

            int edgeLengthA = 0;
            int edgeLengthB = 0;

            for(int i = 0; i < lookupEdgeIndicesA.Length; i++)
            {
                if(lookupEdgeIndicesA[i] == 0)
                {
                    lookupEdgeIndicesA[i] = -1;
                    lookupEdgeIndicesB[i] = edgeLengthB;
                    edgeLengthB++;
                }
                else
                {
                    lookupEdgeIndicesB[i] = -1;
                    lookupEdgeIndicesA[i] = edgeLengthA;
                    edgeLengthA++;
                }
            }

            HalfEdge[] newEdgeArrayA = new HalfEdge[edgeLengthA];
            HalfEdge[] newEdgeArrayB = new HalfEdge[edgeLengthB];

            {
                int counterA = 0;
                int counterB = 0;

                for(int i = 0; i < lookupEdgeIndicesA.Length; i++)
                {
                    HalfEdge originalEdge = controlMesh.edges[i];

                    if(lookupEdgeIndicesA[i] == -1)
                    {
                        newEdgeArrayB[counterB] =
                            new HalfEdge(
                                lookupPolygonIndicesB[originalEdge.polygonIndex],
                                lookupEdgeIndicesB[originalEdge.twinIndex],
                                lookupVertexIndicesB[originalEdge.vertexIndex],
                                originalEdge.hardEdge
                            );

                        counterB++;
                    }
                    else
                    {
                        newEdgeArrayA[counterA] =
                            new HalfEdge(
                                lookupPolygonIndicesA[originalEdge.polygonIndex],
                                lookupEdgeIndicesA[originalEdge.twinIndex],
                                lookupVertexIndicesA[originalEdge.vertexIndex],
                                originalEdge.hardEdge
                            );

                        counterA++;
                    }
                }
            }

            List<TexGen>      newTexgenListA   = new List<TexGen>(polygonLengthA);
            List<TexGen>      newTexgenListB   = new List<TexGen>(polygonLengthB);
            List<TexGenFlags> newFlagsListA    = new List<TexGenFlags>(polygonLengthA);
            List<TexGenFlags> newFlagsListB    = new List<TexGenFlags>(polygonLengthB);
            int[]             newTexgenLookupA = new int[shape.texGens.Length];
            int[]             newTexgenLookupB = new int[shape.texGens.Length];
            Polygon[]         newPolygonArrayA = new Polygon[polygonLengthA];
            Polygon[]         newPolygonArrayB = new Polygon[polygonLengthB];
            Surface[]         newPlaneArrayA   = new Surface[polygonLengthA];
            Surface[]         newPlaneArrayB   = new Surface[polygonLengthB];

            {
                int counterA = 0;
                int counterB = 0;

                for(int i = 0; i < lookupPolygonIndicesA.Length; i++)
                {
                    int[] originalEdges   = controlMesh.polygons[i].edgeIndices;
                    int[] newPolygonEdges = new int[originalEdges.Length];

                    if(lookupPolygonIndicesA[i] == -1)
                    {
                        for(int e = 0; e < newPolygonEdges.Length; e++)
                        {
                            newPolygonEdges[e] = lookupEdgeIndicesB[originalEdges[e]];
                        }

                        int texgenIndex = controlMesh.polygons[i].texGenIndex;

                        if(texgenIndex < 0 || texgenIndex >= newTexgenLookupB.Length)
                        {
                            Debug.LogWarning("texgen_index < 0 || texgen_index >= new_texgen_lookupB.Length");
                        }

                        if(newTexgenLookupB[texgenIndex] == 0)
                        {
                            newTexgenListB.Add(shape.texGens[texgenIndex]);
                            newFlagsListB.Add(shape.texGenFlags[texgenIndex]);
                            newTexgenLookupB[texgenIndex] = newTexgenListB.Count;
                            texgenIndex                   = newTexgenListB.Count - 1;
                        }
                        else
                        {
                            texgenIndex = newTexgenLookupB[texgenIndex] - 1;
                        }

                        newPolygonArrayB[counterB] = new Polygon(newPolygonEdges, texgenIndex);
                        newPlaneArrayB[counterB]   = shape.surfaces[i];
                        counterB++;
                    }
                    else
                    {
                        for(int e = 0; e < newPolygonEdges.Length; e++)
                        {
                            newPolygonEdges[e] = lookupEdgeIndicesA[originalEdges[e]];
                        }

                        int texgenIndex = controlMesh.polygons[i].texGenIndex;

                        if(texgenIndex < 0 || texgenIndex >= newTexgenLookupB.Length)
                        {
                            Debug.LogWarning("texgen_index < 0 || texgen_index >= new_texgen_lookupB.Length");
                        }

                        if(newTexgenLookupA[texgenIndex] == 0)
                        {
                            newTexgenListA.Add(shape.texGens[texgenIndex]);
                            newFlagsListA.Add(shape.texGenFlags[texgenIndex]);
                            newTexgenLookupA[texgenIndex] = newTexgenListA.Count;
                            texgenIndex                   = newTexgenListA.Count - 1;
                        }
                        else
                        {
                            texgenIndex = newTexgenLookupA[texgenIndex] - 1;
                        }

                        newPolygonArrayA[counterA] = new Polygon(newPolygonEdges, texgenIndex);
                        newPlaneArrayA[counterA]   = shape.surfaces[i];
                        counterA++;
                    }
                }
            }

            newControlMesh = new ControlMesh
            {
                vertices = newVertexArrayA
              , polygons = newPolygonArrayA
              , edges    = newEdgeArrayA
            };

            newShape = new Shape
            {
                surfaces    = newPlaneArrayA
              , texGens     = newTexgenListA.ToArray()
              , texGenFlags = newFlagsListA.ToArray()
            };

            Validate(newControlMesh, newShape);

            controlMesh.vertices = newVertexArrayB;
            controlMesh.polygons = newPolygonArrayB;
            controlMesh.edges    = newEdgeArrayB;
            shape.surfaces       = newPlaneArrayB;
            shape.texGens        = newTexgenListB.ToArray();
            shape.texGenFlags    = newFlagsListB.ToArray();

            return true;
        }

        private static int SafeEdge(ControlMesh controlMesh, int[] polygonEdges, short vertexIndex)
        {
            for(int e = 0; e < controlMesh.edges.Length; e++)
            {
                int newEdgeIndex = polygonEdges[e];

                if(vertexIndex == controlMesh.edges[newEdgeIndex].vertexIndex)
                {
                    return newEdgeIndex;
                }
            }

            return -1; //edgeIndex
        }

        private static void ClipOffTriangle(ControlMesh controlMesh, Shape shape, short[] vertexIndices, short polygonIndex)
        {
            int[] edgeIndices = controlMesh.polygons[polygonIndex].edgeIndices;

            if(edgeIndices.Length == 3)
            {
                return;
            }

            int[] edges = { SafeEdge(controlMesh, edgeIndices, vertexIndices[0]), SafeEdge(controlMesh, edgeIndices, vertexIndices[1]), SafeEdge(controlMesh, edgeIndices, vertexIndices[2]) };

            if(edges[0] == -1 || edges[1] == -1 || edges[2] == -1)
            {
                return;
            }

            int[] indices = { ArrayUtility.IndexOf(edgeIndices, edges[0]), ArrayUtility.IndexOf(edgeIndices, edges[1]), ArrayUtility.IndexOf(edgeIndices, edges[2]) };

            for(int i = 0; i < indices.Length - 1; i++)
            {
                for(int j = i + 1; j < indices.Length; j++)
                {
                    if(indices[i] <= indices[j])
                    {
                        continue;
                    }

                    int t = edges[i];
                    edges[i]   = edges[j];
                    edges[j]   = t;
                    t          = indices[i];
                    indices[i] = indices[j];
                    indices[j] = t;
                }
            }

            if(indices[0] + 1 != indices[1])
            {
                int t0 = edges[0];
                int t1 = edges[1];
                int t2 = edges[2];

                edges[0] = t1;
                edges[1] = t2;
                edges[2] = t0;
            }
            else if(indices[1] + 1 != indices[2])
            {
                int t0 = edges[0];
                int t1 = edges[1];
                int t2 = edges[2];

                edges[0] = t2;
                edges[1] = t0;
                edges[2] = t1;
            }

            int prevEdgeIndex = edges[0];
            int currEdgeIndex = edges[1];
            int nextEdgeIndex = edges[2];

            if(controlMesh.GetPrevEdgeIndex(currEdgeIndex) != prevEdgeIndex || controlMesh.GetNextEdgeIndex(currEdgeIndex) != nextEdgeIndex)
            {
                return;
            }

            short prevVertexIndex = controlMesh.GetVertexIndex(prevEdgeIndex);
            short nextVertexIndex = controlMesh.GetVertexIndex(nextEdgeIndex);

            int nextTwinIndex = controlMesh.GetTwinEdgeIndex(nextEdgeIndex);

            int   newIndex0       = controlMesh.edges.Length;
            int   newIndex1       = controlMesh.edges.Length + 1;
            short newPolygonIndex = (short)controlMesh.polygons.Length;

            int[] newPolygonEdges = { currEdgeIndex, newIndex0, newIndex1 };

            Polygon newPolygon = new Polygon(newPolygonEdges, CopyTexGen(shape, controlMesh.polygons[polygonIndex].texGenIndex));

            ArrayUtility.Remove(ref controlMesh.polygons[polygonIndex].edgeIndices, currEdgeIndex);

            bool oldHardEdge = controlMesh.edges[nextEdgeIndex].hardEdge;

            ArrayUtility.Add(ref controlMesh.edges, new HalfEdge(newPolygonIndex, nextTwinIndex, nextVertexIndex, oldHardEdge)); // new index 0
            ArrayUtility.Add(ref controlMesh.edges, new HalfEdge(newPolygonIndex, nextEdgeIndex, prevVertexIndex, false));       // new index 1

            controlMesh.edges[nextEdgeIndex].twinIndex    = newIndex1;
            controlMesh.edges[nextEdgeIndex].hardEdge     = false;
            controlMesh.edges[nextTwinIndex].twinIndex    = newIndex0;
            controlMesh.edges[currEdgeIndex].polygonIndex = newPolygonIndex;

            AddNewPolygon(controlMesh, shape, newPolygon);

            if(controlMesh.polygons[polygonIndex].edgeIndices.Length == 3)
            {
                shape.surfaces[polygonIndex].Plane = GeometryUtility.CalcPolygonPlane(controlMesh, polygonIndex);
            }
        }

        private sealed class Triangle
        {
            public short[] vertexIndices;
            public CSGPlane plane;
        }

        private sealed class DataCache
        {
            public void Clear()
            {
                hardEdges.Clear();
                triangleIndices.Clear();
                allTriangles.Clear();
                path = new TriangulationPath();
            }

            public readonly Dictionary<uint, short> hardEdges = new Dictionary<uint, short>();
            public readonly Dictionary<ulong, short> triangleIndices = new Dictionary<ulong, short>();
            public readonly List<Triangle> allTriangles = new List<Triangle>();
            public TriangulationPath path;
        }

        private sealed class TriangulationPath
        {
            public TriangulationPath[] subPaths;
            public short vertexIndex;
            public short[] triangles;
            public float heuristic;
        }

        private static Triangle FindTriangleWithEdge(short[] triangles, DataCache cache, short vertexIndex0, short vertexIndex1)
        {
            if(triangles == null)
            {
                return null;
            }

            for(int i = 0; i < triangles.Length; i++)
            {
                Triangle triangle         = cache.allTriangles[triangles[i]];
                short[]  vertexIndices    = triangle.vertexIndices;
                short    edge0VertexIndex = vertexIndices[0];
                short    edge1VertexIndex = vertexIndices[1];
                short    edge2VertexIndex = vertexIndices[2];

                if((edge0VertexIndex != vertexIndex0 || edge1VertexIndex != vertexIndex1) &&
                   (edge0VertexIndex != vertexIndex0 || edge2VertexIndex != vertexIndex1) &&
                   (edge1VertexIndex != vertexIndex0 || edge0VertexIndex != vertexIndex1) &&
                   (edge1VertexIndex != vertexIndex0 || edge2VertexIndex != vertexIndex1) &&
                   (edge2VertexIndex != vertexIndex0 || edge0VertexIndex != vertexIndex1) &&
                   (edge2VertexIndex != vertexIndex0 || edge1VertexIndex != vertexIndex1))
                {
                    continue;
                }

                return triangle;
            }

            return null;
        }

        private static class VertPair
        {
            public static uint Create(short a, short b)
            {
                uint a32 = (uint)a;
                uint b32 = (uint)b;

                return a < b ? a32 | (b32 << 16) : b32 | (a32 << 16);
            }
        }

        private static class TriangleIndex
        {
            public static ulong Create(short a, short b, short c)
            {
                ulong a32 = (ulong)a;
                ulong b32 = (ulong)b;
                ulong c32 = (ulong)c;

                if(a < b)
                {
                    if(c > b)
                    {
                        return a32 | (b32 << 16) | (c32 << 32);
                    }

                    if(c > a)
                    {
                        return a32 | (c32 << 16) | (b32 << 32);
                    }

                    return c32 | (a32 << 16) | (b32 << 32);
                }

                if(c > a)
                {
                    return b32 | (a32 << 16) | (c32 << 32);
                }

                if(c > b)
                {
                    return b32 | (c32 << 16) | (a32 << 32);
                }

                return c32 | (b32 << 16) | (a32 << 32);
            }
        }

        // TODO: make this non-recursive
        private static float SplitAtEdge(ControlMesh controlMesh, Shape shape, out short[] triangles, DataCache cache, short[] vertexIndices, int vertexIndicesLength)
        {
            if(vertexIndicesLength == 3)
            {
                short vi0 = vertexIndices[0];
                short vi1 = vertexIndices[1];
                short vi2 = vertexIndices[2];

                ulong    triangleIndex = TriangleIndex.Create(vi0, vi1, vi2);
                Triangle triangle;

                if(!cache.triangleIndices.TryGetValue(triangleIndex, out short index))
                {
                    triangle = new Triangle { vertexIndices = new[] { vi0, vi1, vi2 } };

                    if(vi0 < 0 || vi0 >= controlMesh.vertices.Length || vi1 < 0 || vi1 >= controlMesh.vertices.Length || vi2 < 0 || vi2 >= controlMesh.vertices.Length)
                    {
                        triangles = null;

                        return float.PositiveInfinity;
                    }

                    Vector3 p0 = controlMesh.vertices[vi0];
                    Vector3 p1 = controlMesh.vertices[vi1];
                    Vector3 p2 = controlMesh.vertices[vi2];

                    triangle.plane = new CSGPlane(p0, p1, p2);

                    index = (short)cache.allTriangles.Count;
                    cache.allTriangles.Add(triangle);
                    cache.triangleIndices[triangleIndex] = index;
                }
                else
                {
                    triangle = cache.allTriangles[index];
                }

                triangles = new[] { index };

                uint pair0 = VertPair.Create(vi0, vi1);
                uint pair1 = VertPair.Create(vi1, vi2);
                uint pair2 = VertPair.Create(vi2, vi0);

                float triangleHeuristic = 0.0f;

                Dictionary<uint, short> hardEdges = cache.hardEdges;

                Vector3 trianglePlaneNormal = triangle.plane.Normal;

                if(hardEdges.TryGetValue(pair0, out short polygonIndex))
                {
                    if(polygonIndex < 0 || polygonIndex >= shape.surfaces.Length)
                    {
                        return float.PositiveInfinity;
                    }

                    Vector3 normal = shape.surfaces[polygonIndex].Plane.Normal;
                    float   error  = Vector3.Dot(normal, trianglePlaneNormal) - 1;
                    triangleHeuristic += error * error;
                }

                if(hardEdges.TryGetValue(pair1, out polygonIndex))
                {
                    if(polygonIndex < 0 || polygonIndex >= shape.surfaces.Length)
                    {
                        return float.PositiveInfinity;
                    }

                    Vector3 normal = shape.surfaces[polygonIndex].Plane.Normal;
                    float   error  = Vector3.Dot(normal, trianglePlaneNormal) - 1;
                    triangleHeuristic += error * error;
                }

                if(hardEdges.TryGetValue(pair2, out polygonIndex))
                {
                    if(polygonIndex < 0 || polygonIndex >= shape.surfaces.Length)
                    {
                        return float.PositiveInfinity;
                    }

                    Vector3 normal = shape.surfaces[polygonIndex].Plane.Normal;
                    float   error  = Vector3.Dot(normal, trianglePlaneNormal) - 1;
                    triangleHeuristic += error * error;
                }

                return triangleHeuristic;
            }

            TriangulationPath curLeftPath  = null;
            TriangulationPath curRightPath = null;
            float             curHeuristic = float.PositiveInfinity;
            short[]           tempEdges    = null;

            for(int startPoint = 0; startPoint < vertexIndicesLength - 2; startPoint++)
            {
                for(int offset = 2; offset < vertexIndicesLength - 1; offset++)
                {
                    int endPoint = (startPoint + offset) % vertexIndicesLength;
                    int t0, t1;

                    if(endPoint < startPoint)
                    {
                        t0 = endPoint;
                        t1 = startPoint;
                    }
                    else
                    {
                        t0 = startPoint;
                        t1 = endPoint;
                    }

                    short vertexIndex0 = vertexIndices[t0];
                    short vertexIndex1 = vertexIndices[t1];

                    TriangulationPath leftPath   = cache.path;
                    int               startIndex = -1;

                    // try to find the triangulation in the cache
                    for(int i = t0; i <= t1; i++)
                    {
                        if(leftPath.subPaths == null)
                        {
                            startIndex = i;

                            break;
                        }

                        short index = vertexIndices[i];
                        bool  found = false;

                        for(int j = 0; j < leftPath.subPaths.Length; j++)
                        {
                            if(leftPath.subPaths[j].vertexIndex != index)
                            {
                                continue;
                            }

                            found    = true;
                            leftPath = leftPath.subPaths[j];

                            break;
                        }

                        if(found)
                        {
                            continue;
                        }

                        startIndex = i;

                        break;
                    }

                    float   leftHeuristic;
                    short[] leftTriangles;

                    if(startIndex != -1 || leftPath.triangles == null)
                    {
                        int length0 = t1 - t0 + 1;

                        if(tempEdges == null || tempEdges.Length < length0)
                        {
                            tempEdges = new short[length0];
                        }

                        Array.Copy(vertexIndices, t0, tempEdges, 0, t1 - t0 + 1);

                        // triangulate for the given vertices
                        leftHeuristic = SplitAtEdge(controlMesh, shape, out leftTriangles, cache, tempEdges, length0);

                        // store the found triangulation in the cache
                        if(startIndex != -1)
                        {
                            for(int i = startIndex; i <= t1; i++)
                            {
                                TriangulationPath newSubPath = new TriangulationPath { vertexIndex = vertexIndices[i] };

                                if(leftPath.subPaths == null)
                                {
                                    leftPath.subPaths = new[] { newSubPath };
                                }
                                else
                                {
                                    ArrayUtility.Add(ref leftPath.subPaths, newSubPath);
                                }

                                leftPath = newSubPath;
                            }
                        }

                        leftPath.triangles = leftTriangles;
                        leftPath.heuristic = leftHeuristic;
                    }
                    else
                    {
                        leftHeuristic = leftPath.heuristic;
                        leftTriangles = leftPath.triangles;
                    }

                    float newHeuristic = leftHeuristic;

                    if(newHeuristic >= curHeuristic + MathConstants.EQUALITY_EPSILON)
                    {
                        continue;
                    }

                    int offsetB = vertexIndicesLength - t1;
                    int length1 = t0 + 1 + offsetB;

                    if(tempEdges == null || tempEdges.Length < length1)
                    {
                        tempEdges = new short[length1];
                    }

                    Array.Copy(vertexIndices, t1, tempEdges, 0, offsetB);
                    Array.Copy(vertexIndices, 0, tempEdges, offsetB, t0 + 1);

                    TriangulationPath rightPath = cache.path;
                    startIndex = -1;

                    // try to find the triangulation in the cache
                    for(int i = 0; i < length1; i++)
                    {
                        if(rightPath.subPaths == null)
                        {
                            startIndex = i;

                            break;
                        }

                        short index = tempEdges[i];
                        bool  found = false;

                        for(int j = 0; j < rightPath.subPaths.Length; j++)
                        {
                            if(rightPath.subPaths[j].vertexIndex == index)
                            {
                                found     = true;
                                rightPath = rightPath.subPaths[j];

                                break;
                            }
                        }

                        if(!found)
                        {
                            startIndex = i;

                            break;
                        }
                    }

                    float   rightHeuristic;
                    short[] rightTriangles;

                    if(startIndex != -1 || rightPath.triangles == null)
                    {
                        // triangulate for the given vertices
                        rightHeuristic = SplitAtEdge(controlMesh, shape, out rightTriangles, cache, tempEdges, length1);

                        // store the found triangulation in the cache
                        if(startIndex != -1)
                        {
                            for(int i = startIndex; i < tempEdges.Length; i++)
                            {
                                TriangulationPath newSubPath = new TriangulationPath { vertexIndex = tempEdges[i] };

                                if(rightPath.subPaths == null)
                                {
                                    rightPath.subPaths = new[] { newSubPath };
                                }
                                else
                                {
                                    ArrayUtility.Add(ref rightPath.subPaths, newSubPath);
                                }

                                rightPath = newSubPath;
                            }
                        }

                        rightPath.triangles = rightTriangles;
                        rightPath.heuristic = rightHeuristic;
                    }
                    else
                    {
                        rightHeuristic = rightPath.heuristic;
                        rightTriangles = rightPath.triangles;
                    }

                    newHeuristic += rightHeuristic;

                    if(newHeuristic >= curHeuristic + MathConstants.EQUALITY_EPSILON)
                    {
                        continue;
                    }

                    Triangle leftTriangle  = FindTriangleWithEdge(leftTriangles, cache, vertexIndex0, vertexIndex1);
                    Triangle rightTriangle = FindTriangleWithEdge(rightTriangles, cache, vertexIndex0, vertexIndex1);

                    if(leftTriangle == null || rightTriangle == null)
                    {
                        continue;
                    }

                    CSGPlane leftPlane  = leftTriangle.plane;
                    CSGPlane rightPlane = rightTriangle.plane;
                    float    error      = Vector3.Dot(leftPlane.Normal, rightPlane.Normal) - 1;
                    newHeuristic += error * error;

                    if(!(newHeuristic < curHeuristic - MathConstants.EQUALITY_EPSILON))
                    {
                        continue;
                    }

                    curLeftPath  = leftPath;
                    curRightPath = rightPath;
                    curHeuristic = newHeuristic;
                }
            }

            if(curLeftPath != null && curRightPath != null)
            {
                triangles = new short[curLeftPath.triangles.Length + curRightPath.triangles.Length];
                Array.Copy(curLeftPath.triangles, triangles, curLeftPath.triangles.Length);
                Array.Copy(curRightPath.triangles, 0, triangles, curLeftPath.triangles.Length, curRightPath.triangles.Length);
            }
            else
            {
                curHeuristic = float.PositiveInfinity;
                triangles    = null;
            }

            return curHeuristic;
        }

        private static readonly List<CSGPlane> polygonPlanes = new List<CSGPlane>();
        private static readonly Dictionary<int, Triangle[]> polyTriangles = new Dictionary<int, Triangle[]>();
        private static readonly List<Triangle> newTriangles = new List<Triangle>();
        private static readonly DataCache dataCache = new DataCache();
        private static short[] m_PolyVerts = Array.Empty<short>();
        private static readonly HashSet<short> ownedPolygons = new HashSet<short>();
        private static readonly HashSet<int> removeEdges = new HashSet<int>();
        private static readonly HashSet<short> removePolygons = new HashSet<short>();

        public static bool Triangulate(CSGBrush brush, ControlMesh controlMesh, Shape shape)
        {
            if(controlMesh == null || controlMesh.polygons == null)
            {
                return false;
            }

            polygonPlanes.Clear();

            for(int p = 0; p < controlMesh.polygons.Length; p++)
            {
                CSGPlane plane = GeometryUtility.CalcPolygonPlane(controlMesh, (short)p);
                polygonPlanes.Add(plane);
                shape.surfaces[p].Plane = plane;
            }

            polyTriangles.Clear();
            newTriangles.Clear();
            dataCache.Clear();

            for(int p = controlMesh.polygons.Length - 1; p >= 0; p--)
            {
                int[] edgeIndices = controlMesh.polygons[p].edgeIndices;

                if(edgeIndices.Length > 16)
                {
                    continue;
                }

                CSGPlane polygonPlane = polygonPlanes[p];
                bool     isPlanar     = true;

                for(int e = 0; e < edgeIndices.Length; e++)
                {
                    Vector3 vertex = controlMesh.GetVertex(edgeIndices[e]);

                    if(!(polygonPlane.Distance(vertex) > MathConstants.DISTANCE_EPSILON))
                    {
                        continue;
                    }

                    isPlanar = false;

                    break;
                }

                if(isPlanar)
                {
                    continue;
                }

                // TODO: only update the 'polygons' that have been modified

                dataCache.Clear();

                for(int e = 0; e < edgeIndices.Length; e++)
                {
                    HalfEdge curEdge      = controlMesh.edges[edgeIndices[e]];
                    HalfEdge twinEdge     = controlMesh.edges[curEdge.twinIndex];
                    short    polygonIndex = twinEdge.polygonIndex;
                    uint     pair         = VertPair.Create(curEdge.vertexIndex, twinEdge.vertexIndex);
                    dataCache.hardEdges[pair] = polygonIndex;
                }

                if(m_PolyVerts.Length < edgeIndices.Length)
                {
                    m_PolyVerts = new short[edgeIndices.Length];
                }

                for(int i = 0; i < edgeIndices.Length; i++)
                {
                    m_PolyVerts[i] = controlMesh.edges[edgeIndices[i]].vertexIndex;
                }

                SplitAtEdge(controlMesh, shape, out short[] foundTriangles, dataCache, m_PolyVerts, edgeIndices.Length);

                newTriangles.Clear();

                while(foundTriangles != null && foundTriangles.Length > 0)
                {
                    if(foundTriangles.Length == 1)
                    {
                        newTriangles.Add(dataCache.allTriangles[foundTriangles[0]]);

                        break;
                    }

                    int found          = -1;
                    int foundEdgeCount = 0;

                    for(int t = 0; t < foundTriangles.Length; t++)
                    {
                        Triangle triangle  = dataCache.allTriangles[foundTriangles[t]];
                        uint     vertPair1 = VertPair.Create(triangle.vertexIndices[0], triangle.vertexIndices[1]);
                        uint     vertPair2 = VertPair.Create(triangle.vertexIndices[1], triangle.vertexIndices[2]);
                        uint     vertPair3 = VertPair.Create(triangle.vertexIndices[2], triangle.vertexIndices[0]);

                        int hardEdgeCount = 0;

                        if(dataCache.hardEdges.ContainsKey(vertPair1))
                        {
                            hardEdgeCount++;
                        }

                        if(dataCache.hardEdges.ContainsKey(vertPair2))
                        {
                            hardEdgeCount++;
                        }

                        if(dataCache.hardEdges.ContainsKey(vertPair3))
                        {
                            hardEdgeCount++;
                        }

                        if(hardEdgeCount <= foundEdgeCount)
                        {
                            continue;
                        }

                        foundEdgeCount = hardEdgeCount;
                        found          = t;
                    }

                    if(found == -1)
                    {
                        Debug.LogWarning("Failed to find appropriate triangle to clip during triangulation");

                        break;
                    }

                    {
                        Triangle triangle  = dataCache.allTriangles[foundTriangles[found]];
                        uint     vertPair1 = VertPair.Create(triangle.vertexIndices[0], triangle.vertexIndices[1]);
                        uint     vertPair2 = VertPair.Create(triangle.vertexIndices[1], triangle.vertexIndices[2]);
                        uint     vertPair3 = VertPair.Create(triangle.vertexIndices[2], triangle.vertexIndices[0]);

                        newTriangles.Add(triangle);
                        ArrayUtility.RemoveAt(ref foundTriangles, found);

                        dataCache.hardEdges.TryAdd(vertPair1, -1);
                        dataCache.hardEdges.TryAdd(vertPair2, -1);
                        dataCache.hardEdges.TryAdd(vertPair3, -1);
                    }
                }

                polyTriangles.Add(p, newTriangles.ToArray());
            }

            ownedPolygons.Clear();

            foreach(KeyValuePair<int, Triangle[]> pair in polyTriangles)
            {
                short      polygonIndex = (short)pair.Key;
                Triangle[] triangles    = pair.Value;

                if(triangles == null)
                {
                    continue;
                }

                ownedPolygons.Clear();
                ownedPolygons.Add(polygonIndex);
                int currentLastIndex = controlMesh.polygons.Length;

                for(int t = 0; t < triangles.Length; t++)
                {
                    ClipOffTriangle(controlMesh, shape, triangles[t].vertexIndices, polygonIndex);
                }

                for(int i = currentLastIndex; i < controlMesh.polygons.Length; i++)
                {
                    polygonPlanes.Add(GeometryUtility.CalcPolygonPlane(controlMesh, (short)i));
                    ownedPolygons.Add((short)i);
                }
            }

            CSGPlane[] polygonPlaneArray = polygonPlanes.ToArray();

            for(int i = 0; i < polygonPlaneArray.Length; i++)
            {
                polygonPlaneArray[i] = GeometryUtility.CalcPolygonPlane(controlMesh, (short)i);
            }

            // combine triangles into planar (possibly self-intersecting) polygons
            removeEdges.Clear();
            removePolygons.Clear();

            for(short p = (short)(controlMesh.polygons.Length - 1); p >= 0; p--)
            {
                retry_polygon:
                int[] currEdges = controlMesh.polygons[p].edgeIndices;

                for(int e = 0; e < currEdges.Length; e++)
                {
                    int currIndex = currEdges[e];

                    if(currIndex < 0 || currIndex >= controlMesh.edges.Length)
                    {
                        Debug.LogWarning($"edge with index {currIndex} is invalid", brush);

                        continue;
                    }

                    int twinIndex = controlMesh.GetTwinEdgeIndex(currIndex);

                    if(twinIndex < 0 || twinIndex >= controlMesh.edges.Length)
                    {
                        Debug.LogWarning($"edge {currIndex} with twin-index {twinIndex} is invalid", brush);

                        continue;
                    }

                    if(controlMesh.edges[currIndex].hardEdge || controlMesh.edges[twinIndex].hardEdge)
                    {
                        continue;
                    }

                    short twinPolygonIndex = controlMesh.GetEdgePolygonIndex(twinIndex);

                    // internal edge, that on both sides leads to same polygon
                    // this can happen when joining enough polygons together
                    if(p == twinPolygonIndex)
                    {
                        // remove both edges from polygon
                        int srcIndex1 = ArrayUtility.IndexOf(currEdges, currIndex);
                        int srcIndex2 = ArrayUtility.IndexOf(currEdges, twinIndex);

                        if(srcIndex1 == -1 || srcIndex2 == -1)
                        {
                            Debug.LogWarning("src_index1 == -1 || src_index2 == -1");

                            continue;
                        }

                        if(srcIndex1 > srcIndex2)
                        {
                            ArrayUtility.RemoveAt(ref controlMesh.polygons[p].edgeIndices, srcIndex1);
                            ArrayUtility.RemoveAt(ref controlMesh.polygons[p].edgeIndices, srcIndex2);
                        }
                        else
                        {
                            ArrayUtility.RemoveAt(ref controlMesh.polygons[p].edgeIndices, srcIndex2);
                            ArrayUtility.RemoveAt(ref controlMesh.polygons[p].edgeIndices, srcIndex1);
                        }

                        removeEdges.Add(currIndex);
                        removeEdges.Add(twinIndex);

                        // polygon can still have other internal edges / edges to
                        // other polygons on same plane
                        goto retry_polygon;
                    }

                    if(p != twinPolygonIndex)
                    {
                        continue;
                    }

                    int srcIndex = ArrayUtility.IndexOf(currEdges, currIndex);
                    int dstIndex = ArrayUtility.IndexOf(controlMesh.polygons[twinPolygonIndex].edgeIndices, twinIndex);
                    removeEdges.Add(currIndex);
                    removeEdges.Add(twinIndex);
                    ArrayUtility.RemoveAt(ref controlMesh.polygons[twinPolygonIndex].edgeIndices, dstIndex);

                    if(srcIndex < 0 || srcIndex >= currEdges.Length)
                    {
                        Debug.LogWarning("src_index < 0 || src_index >= curr_edges.Length");

                        continue;
                    }

                    int iterator = (srcIndex + 1) % currEdges.Length;

                    while(iterator != srcIndex)
                    {
                        int srcEdgeIndex = currEdges[iterator];
                        controlMesh.edges[srcEdgeIndex].polygonIndex = twinPolygonIndex;

                        ArrayUtility.Insert(ref controlMesh.polygons[twinPolygonIndex].edgeIndices, dstIndex, srcEdgeIndex);

                        iterator = (iterator + 1) % currEdges.Length;
                        dstIndex++;
                    }

                    removePolygons.Add(p);

                    break;
                }
            }

            RemoveEdges(controlMesh, removeEdges);
            removeEdges.Clear();

            List<short> sortedRemovePolygons = new List<short>(removePolygons);
            sortedRemovePolygons.Sort();

            for(int p = sortedRemovePolygons.Count - 1; p >= 0; p--)
            {
                short polygonIndex = sortedRemovePolygons[p];
                ArrayUtility.RemoveAt(ref controlMesh.polygons, polygonIndex);
                ArrayUtility.RemoveAt(ref shape.surfaces, polygonIndex);

                for(int i = 0; i < controlMesh.edges.Length; i++)
                {
                    if(controlMesh.edges[i].polygonIndex >= polygonIndex)
                    {
                        controlMesh.edges[i].polygonIndex--;
                    }
                }
            }

            CalculatePlanes(controlMesh, shape);
            Validate(controlMesh, shape);

            return true;
        }

        private sealed class PointIntersection
        {
            public PointIntersection(short vertexIndex, List<int> planes)
            {
                planeIndices.AddRange(planes);
                this.vertexIndex = vertexIndex;
                edgeIndices.Clear();
            }

            public readonly List<int> edgeIndices = new List<int>();
            public readonly List<int> planeIndices = new List<int>();
            public short vertexIndex;

            public bool ContainsPlanes(int[] planeIndex)
            {
                if(planeIndices.Count < 3)
                {
                    return false;
                }

                int i = 0;

                for(int n = 0; n < 3; n++)
                {
                    while(true)
                    {
                        if(i == planeIndices.Count || planeIndices[i] > planeIndex[n])
                        {
                            return false;
                        }

                        if(planeIndices[i] == planeIndex[n])
                        {
                            break;
                        }

                        i++;
                    }

                    i++;
                }

                return true;
            }
        }

        private sealed class EdgeIntersection
        {
            public EdgeIntersection(short vertexIndex, short vertexIndexB, int edgeIndex, int planeIndexA, int planeIndexB)
            {
                this.vertexIndex  = vertexIndex;
                this.vertexIndexB = vertexIndexB;
                this.edgeIndex    = edgeIndex;
                twinIndex         = -1;
                nextIndex         = -1;

                planeIndices[0] = planeIndexA;
                planeIndices[1] = planeIndexB;
            }

            public readonly short vertexIndex;
            public readonly short vertexIndexB;
            public readonly int edgeIndex;
            public int twinIndex;
            public int nextIndex;
            public readonly int[] planeIndices = new int[2];

            public override string ToString()
            {
                return $"vA: {vertexIndex}, vB: {vertexIndexB}, pA: {planeIndices[0]}, pB: {planeIndices[1]}, eI: {edgeIndex}, tI: {twinIndex}, nI: {nextIndex}";
            }
        }

        public static ControlMesh CreateFromShape(Shape shape, float distanceEpsilon = MathConstants.DISTANCE_EPSILON)
        {
            int surfaceCount = shape.surfaces.Length;

            if(shape.texGens == null)
            {
                shape.texGens = new TexGen[shape.surfaces.Length];
            }
            else
            {
                while(shape.texGens.Length < shape.surfaces.Length)
                {
                    ArrayUtility.Add(ref shape.texGens, shape.texGens[shape.texGens.Length - 1]);
                }
            }

            if(shape.texGenFlags == null)
            {
                shape.texGenFlags = new TexGenFlags[shape.surfaces.Length];

                for(int i = 0; i < shape.texGenFlags.Length; i++)
                {
                    shape.texGenFlags[i] = CSGSettings.DefaultTexGenFlags;
                }
            }
            else
            {
                while(shape.texGenFlags.Length < shape.surfaces.Length)
                {
                    ArrayUtility.Add(ref shape.texGenFlags, shape.texGenFlags[shape.texGenFlags.Length - 1]);
                }
            }

            for(int s = 0; s < shape.surfaces.Length; s++)
            {
                Vector3 normal = shape.surfaces[s].Plane.Normal;
                GeometryUtility.CalculateTangents(normal, out Vector3 tangent, out Vector3 binormal);

                float scaleX = shape.surfaces[s].Tangent.magnitude;
                float scaleY = shape.surfaces[s].BiNormal.magnitude;

                if(Mathf.Abs(scaleX) < MathConstants.EQUALITY_EPSILON)
                {
                    scaleX = 1;
                }

                if(Mathf.Abs(scaleY) < MathConstants.EQUALITY_EPSILON)
                {
                    scaleY = 1;
                }

                shape.surfaces[s].Tangent  /= scaleX;
                shape.surfaces[s].BiNormal /= scaleY;

                if(Vector3.Dot(shape.surfaces[s].Tangent, tangent) < 0)
                {
                    scaleX = -scaleX;
                }

                if(Vector3.Dot(shape.surfaces[s].BiNormal, binormal) < 0)
                {
                    scaleY = -scaleY;
                }

                shape.surfaces[s].Tangent  = tangent;
                shape.surfaces[s].BiNormal = binormal;

                shape.texGens[s].Scale.x *= scaleX;
                shape.texGens[s].Scale.y *= scaleY;
            }

            List<PointIntersection> pointIntersections = new List<PointIntersection>(surfaceCount * surfaceCount);
            List<Vector3>           vertices           = new List<Vector3>(4 * surfaceCount);

            List<int>[] planeVertices = new List<int>[surfaceCount];

            for(int i = 0; i < surfaceCount; i++)
            {
                planeVertices[i] = new List<int>();
            }

            // rotate/scale the planes into object space
            CSGPlane[] planes = new CSGPlane[surfaceCount];

            for(int i = 0; i < surfaceCount; i++)
            {
                planes[i] = shape.surfaces[i].Plane;
            }

            {
                List<int> intersectingPlanes = new List<int>(3 * surfaceCount);

                int[]      curPlaneIndex = new int[3];
                CSGPlane[] curPlane      = new CSGPlane[3];

                // Find all point intersections where 3 (or more planes) intersect
                for(int p1 = 0; p1 < surfaceCount - 2; p1++)
                {
                    curPlaneIndex[0] = p1;
                    curPlane[0]      = planes[curPlaneIndex[0]];

                    for(int p2 = p1 + 1; p2 < surfaceCount - 1; p2++)
                    {
                        curPlaneIndex[1] = p2;
                        curPlane[1]      = planes[curPlaneIndex[1]];

                        for(int p3 = p2 + 1; p3 < surfaceCount; p3++)
                        {
                            curPlaneIndex[2] = p3;
                            curPlane[2]      = planes[curPlaneIndex[2]];

                            bool found = false;

                            // Fix for situation where duplicate plane triplets are
                            // generated due to precision issues
                            for(int i = pointIntersections.Count - 1; i >= 0; i--)
                            {
                                if(!pointIntersections[i].ContainsPlanes(curPlaneIndex))
                                {
                                    continue;
                                }

                                found = true;

                                break;
                            }

                            if(found)
                            {
                                continue;
                            }

                            // Calculate the intersection
                            Vector3 vertex = CSGPlane.Intersection(curPlane[0], curPlane[1], curPlane[2]);

                            // Check if the intersection is valid
                            if(float.IsNaN(vertex.x) || float.IsInfinity(vertex.x) || float.IsNaN(vertex.y) || float.IsInfinity(vertex.y) || float.IsNaN(vertex.z) || float.IsInfinity(vertex.z))
                            {
                                continue;
                            }

                            intersectingPlanes.Clear();
                            intersectingPlanes.Add(curPlaneIndex[0]);
                            intersectingPlanes.Add(curPlaneIndex[1]);
                            intersectingPlanes.Add(curPlaneIndex[2]);

                            for(int p4 = 0; p4 < surfaceCount; p4++)
                            {
                                if(p4 == curPlaneIndex[0] || p4 == curPlaneIndex[1] || p4 == curPlaneIndex[2])
                                {
                                    continue;
                                }

                                CSGPlane plane4 = planes[p4];
                                float    side   = plane4.Distance(vertex);

                                if(side < -distanceEpsilon) // inside
                                {
                                    continue;
                                }

                                if(side > distanceEpsilon) // outside
                                    // Intersection is outside of other planes
                                {
                                    goto SkipIntersection;
                                }

                                // intersects
                                if(p4 < curPlaneIndex[2])
                                    // Already found this vertex
                                {
                                    goto SkipIntersection;
                                }

                                // We've found another plane which goes trough our found intersection point
                                intersectingPlanes.Add(p4);
                            }

                            {
                                // in case we have a situation where plane intersections create vertices *really* close to each other
                                for(int p = 0; p < pointIntersections.Count; p++)
                                {
                                    PointIntersection pointIntersection = pointIntersections[p];
                                    Vector3           delta             = vertex - vertices[pointIntersection.vertexIndex];

                                    if(Mathf.Abs(delta.x) < distanceEpsilon && Mathf.Abs(delta.y) < distanceEpsilon && Mathf.Abs(delta.z) < distanceEpsilon)
                                    {
                                        for(int n = 0; n < intersectingPlanes.Count; n++)
                                        {
                                            if(!pointIntersection.planeIndices.Contains(intersectingPlanes[n]))
                                            {
                                                pointIntersection.planeIndices.Add(intersectingPlanes[n]);
                                            }
                                        }

                                        goto SkipIntersection;
                                    }
                                }

                                short vertexIndex = (short)vertices.Count;
                                vertices.Add(vertex);

                                for(int i = 0; i < intersectingPlanes.Count; i++)
                                {
                                    planeVertices[intersectingPlanes[i]].Add(pointIntersections.Count);
                                }

                                // Add intersection point to our list
                                pointIntersections.Add(new PointIntersection(vertexIndex, intersectingPlanes));
                            }

                            SkipIntersection: ;
                        }
                    }
                }

                intersectingPlanes.Clear();
            }

            if(pointIntersections.Count == 0)
            {
#if DEBUG
                Debug.LogWarning("No valid plane intersections found in brush");
#endif
                return null;
            }

            bool needToCleanUpPlanesAgain = false;
            CleanUpPlanesAgain:

            // Check if we have planes that intersect with too few points / or are duplicates
            for(int p = planeVertices.Length - 1; p >= 0; p--)
            {
                if(planeVertices.Length >= 3)
                {
                    bool haveDuplicatePlane = false;

                    for(int p2 = 0; p2 < p; p2++)
                    {
                        if(planeVertices[p].Count != planeVertices[p2].Count)
                        {
                            continue;
                        }

                        haveDuplicatePlane = true;

                        for(int j = 0; j < planeVertices[p].Count; j++)
                        {
                            if(planeVertices[p] != planeVertices[p2])
                            {
                                haveDuplicatePlane = false;

                                break;
                            }
                        }
                    }

                    if(!haveDuplicatePlane)
                    {
                        continue;
                    }
#if DEBUG
                    Debug.LogWarning("Found duplicate plane in brush");
#endif
                }
                else
                {
#if DEBUG
                    Debug.LogWarning("Found plane in brush that only intersects a single edge");
#endif
                }

                ArrayUtility.RemoveAt(ref planeVertices, p);
                ArrayUtility.RemoveAt(ref shape.surfaces, p);
                ArrayUtility.RemoveAt(ref shape.texGens, p);
                ArrayUtility.RemoveAt(ref shape.texGenFlags, p);

                for(int i = 0; i < pointIntersections.Count; i++)
                {
                    PointIntersection intersection = pointIntersections[i];
                    List<int>         planeIndices = intersection.planeIndices;

                    for(int j = planeIndices.Count - 1; j >= 0; j--)
                    {
                        if(planeIndices[j] < p)
                        {
                            continue;
                        }

                        if(planeIndices[j] == p)
                        {
                            planeIndices.RemoveAt(j);
                        }
                        else
                        {
                            planeIndices[j]--;
                        }
                    }
                }
            }

            // Check if we have points that intersect with too few planes
            for(int i = pointIntersections.Count - 1; i >= 0; i--)
            {
                PointIntersection intersection = pointIntersections[i];
                List<int>         planeIndices = intersection.planeIndices;

                if(planeIndices.Count >= 3)
                {
                    continue;
                }

                for(int p = planeVertices.Length - 1; p >= 0; p--)
                {
                    List<int> verticesForPlane = planeVertices[p];

                    for(int v = verticesForPlane.Count - 1; v >= 0; v--)
                    {
                        if(verticesForPlane[v] < i)
                        {
                            continue;
                        }

                        if(verticesForPlane[v] == i)
                        {
                            verticesForPlane.RemoveAt(v);
                            needToCleanUpPlanesAgain = true;
                        }
                        else
                        {
                            verticesForPlane[v]--;
                        }
                    }
                }
            }

            if(needToCleanUpPlanesAgain)
            {
                needToCleanUpPlanesAgain = false;

                goto CleanUpPlanesAgain;
            }

            if(planeVertices.Length < 4)
            {
#if DEBUG
                Debug.LogWarning("Not enough valid planes to create brush");
#endif
                return null;
            }

#if DEBUG
            float[][] vertexPlanes = new float[planes.Length][];

            for(int p = 0; p < planes.Length; p++)
            {
                vertexPlanes[p] = new float[vertices.Count];

                for(int v = 0; v < vertices.Count; v++)
                {
                    vertexPlanes[p][v] = planes[p].Distance(vertices[v]);

                    if(vertexPlanes[p][v] > -MathConstants.DISTANCE_EPSILON && vertexPlanes[p][v] < MathConstants.DISTANCE_EPSILON)
                    {
                        vertexPlanes[p][v] = 0;
                    }
                }
            }
#endif
            List<EdgeIntersection>[] planeEdges        = new List<EdgeIntersection>[surfaceCount];
            List<EdgeIntersection>   edgeIntersections = new List<EdgeIntersection>(surfaceCount * surfaceCount * 2);

            int edgeCount = 0;

            for(int i = 0; i < surfaceCount; i++)
            {
                planeEdges[i] = new List<EdgeIntersection>();
            }

            for(int p = 0; p < planeVertices.Length; p++)
            {
                List<int> verticesForPlane = planeVertices[p];

                for(int v0 = 0; v0 < verticesForPlane.Count - 1; v0++)
                {
                    int       point0         = verticesForPlane[v0];
                    List<int> planesIndices0 = pointIntersections[point0].planeIndices;

                    for(int i0 = 0; i0 < planesIndices0.Count; i0++)
                    {
                        int planeIndex0 = planesIndices0[i0];

                        if(planeIndex0 <= p)
                        {
                            continue;
                        }

                        for(int v1 = v0 + 1; v1 < verticesForPlane.Count; v1++)
                        {
                            int       point1         = verticesForPlane[v1];
                            List<int> planesIndices1 = pointIntersections[point1].planeIndices;

                            for(int i1 = 0; i1 < planesIndices1.Count; i1++)
                            {
                                int planeIndex1 = planesIndices1[i1];

                                if(planeIndex0 != planeIndex1)
                                {
                                    continue;
                                }

                                // Create our found intersection edge
                                int edgeIndex0 = edgeCount;
                                edgeCount++;
                                int edgeIndex1 = edgeCount;
                                edgeCount++;

                                int intersectionIndex0 = edgeIntersections.Count;

                                EdgeIntersection intersection0 = new EdgeIntersection(pointIntersections[point0].vertexIndex, pointIntersections[point1].vertexIndex, edgeIndex0, p, planeIndex1);

                                edgeIntersections.Add(intersection0);

                                int intersectionIndex1 = edgeIntersections.Count;

                                EdgeIntersection intersection1 = new EdgeIntersection(pointIntersections[point1].vertexIndex, pointIntersections[point0].vertexIndex, edgeIndex1, planeIndex1, p);

                                edgeIntersections.Add(intersection1);

                                edgeIntersections[intersectionIndex0].twinIndex = intersectionIndex1;
                                edgeIntersections[intersectionIndex1].twinIndex = intersectionIndex0;

                                planeEdges[p].Add(intersection0);
                                planeEdges[planeIndex1].Add(intersection1);

                                // Add it to our points
                                pointIntersections[point0].edgeIndices.Add(intersectionIndex0);
                                pointIntersections[point1].edgeIndices.Add(intersectionIndex1);
                            }
                        }
                    }
                }
            }

            int[]  polygonFirst  = new int[surfaceCount];
            AABB[] polygonBounds = new AABB[surfaceCount];

            for(int i = 0; i < surfaceCount; i++)
            {
                polygonFirst[i]  = -1;
                polygonBounds[i] = new AABB();
            }

            AABB bounds = new AABB();

            for(int i = pointIntersections.Count - 1; i >= 0; i--)
            {
                List<int> pointEdgeIndices = pointIntersections[i].edgeIndices;

                // Make sure that we have at least 2 edges ...
                // This may happen when a plane only intersects at a single edge.
                if(pointEdgeIndices.Count <= 2)
                {
                    short vertexIndex1 = pointIntersections[i].vertexIndex;

                    for(int p = 0; p < pointIntersections.Count; p++)
                    {
                        if(pointIntersections[p].vertexIndex > vertexIndex1)
                        {
                            pointIntersections[p].vertexIndex--;
                        }
                    }

                    pointIntersections.RemoveAt(i);
                    vertices.RemoveAt(vertexIndex1);

                    continue;
                }

                short   vertexIndex = pointIntersections[i].vertexIndex;
                Vector3 vertex      = vertices[vertexIndex];

                for(int j = 0; j < pointEdgeIndices.Count - 1; j++)
                {
                    int edge1Index = pointEdgeIndices[j];

                    for(int k = j + 1; k < pointEdgeIndices.Count; k++)
                    {
                        int edge2Index = pointEdgeIndices[k];

                        int planeOrder1, planeOrder2;

                        // Determine if and which of our 2 planes are identical
                        if(edgeIntersections[edge1Index].planeIndices[0] == edgeIntersections[edge2Index].planeIndices[0])
                        {
                            planeOrder1 = 0;
                            planeOrder2 = 0;
                        }
                        else if(edgeIntersections[edge1Index].planeIndices[0] == edgeIntersections[edge2Index].planeIndices[1])
                        {
                            planeOrder1 = 0;
                            planeOrder2 = 1;
                        }
                        else if(edgeIntersections[edge1Index].planeIndices[1] == edgeIntersections[edge2Index].planeIndices[0])
                        {
                            planeOrder1 = 1;
                            planeOrder2 = 0;
                        }
                        else if(edgeIntersections[edge1Index].planeIndices[1] == edgeIntersections[edge2Index].planeIndices[1])
                        {
                            planeOrder1 = 1;
                            planeOrder2 = 1;
                        }
                        else
                        {
                            continue;
                        }

                        int ingoingEdgeIndex;
                        int outgoingEdgeIndex;

                        CSGPlane sharedPlane = planes[edgeIntersections[edge1Index].planeIndices[planeOrder1]];
                        CSGPlane edge1Plane  = planes[edgeIntersections[edge1Index].planeIndices[1 - planeOrder1]];
                        CSGPlane edge2Plane  = planes[edgeIntersections[edge2Index].planeIndices[1 - planeOrder2]];

                        Vector3 direction = Vector3.Cross(sharedPlane.Normal, edge1Plane.Normal);

                        // Determine the orientation of our two edges to determine
                        // which edge is in-going, and which one is out-going
                        float alignment = Vector3.Dot(direction, edge2Plane.Normal);

                        if(alignment < 0)
                        {
                            ingoingEdgeIndex  = edge2Index;
                            outgoingEdgeIndex = edgeIntersections[edge1Index].twinIndex;
                        }
                        else
                        {
                            ingoingEdgeIndex  = edge1Index;
                            outgoingEdgeIndex = edgeIntersections[edge2Index].twinIndex;
                        }

                        // Link the out-going half-edge to the in-going half-edge
                        edgeIntersections[ingoingEdgeIndex].nextIndex = outgoingEdgeIndex;

                        // Add reference to polygon to half-edge, and make sure our
                        // polygon has a reference to a half-edge
                        // Since a half-edge, in this case, serves as a circular
                        // linked list this just works.
                        int polygonIndex = edgeIntersections[edge1Index].planeIndices[planeOrder1];

                        polygonFirst[polygonIndex]       = outgoingEdgeIndex;
                        polygonBounds[polygonIndex].MinX = Mathf.Min(polygonBounds[polygonIndex].MinX, vertex.x);
                        polygonBounds[polygonIndex].MaxX = Mathf.Min(polygonBounds[polygonIndex].MaxX, vertex.x);
                        polygonBounds[polygonIndex].MinY = Mathf.Min(polygonBounds[polygonIndex].MinY, vertex.y);
                        polygonBounds[polygonIndex].MaxY = Mathf.Min(polygonBounds[polygonIndex].MaxY, vertex.y);
                        polygonBounds[polygonIndex].MinZ = Mathf.Min(polygonBounds[polygonIndex].MinZ, vertex.z);
                        polygonBounds[polygonIndex].MaxZ = Mathf.Min(polygonBounds[polygonIndex].MaxZ, vertex.z);
                    }
                }

                // Add the intersection point to the area of our bounding box
                bounds.MinX = Mathf.Min(bounds.MinX, vertex.x);
                bounds.MaxX = Mathf.Min(bounds.MaxX, vertex.x);
                bounds.MinY = Mathf.Min(bounds.MinY, vertex.y);
                bounds.MaxY = Mathf.Min(bounds.MaxY, vertex.y);
                bounds.MinZ = Mathf.Min(bounds.MinZ, vertex.z);
                bounds.MaxZ = Mathf.Min(bounds.MaxZ, vertex.z);
            }

            List<int> polygonEdgeIndices = new List<int>(edgeCount);

            ControlMesh newMesh = new ControlMesh { edges = new HalfEdge[edgeCount] };

            List<Polygon>  polygons = new List<Polygon>();
            List<HalfEdge> edges    = new List<HalfEdge>(edgeCount);

            for(int i = 0; i < edgeCount; i++)
            {
                edges.Add(new HalfEdge());
            }

            for(int i = 0; i < edgeIntersections.Count; i++)
            {
                int      edgeIndex = edgeIntersections[i].edgeIndex;
                HalfEdge edge      = edges[edgeIndex];
                edge.twinIndex    = edgeIntersections[edgeIntersections[i].twinIndex].edgeIndex;
                edge.polygonIndex = -1;
                edge.vertexIndex  = edgeIntersections[i].vertexIndex;
                edge.hardEdge     = true;
                edges[edgeIndex]  = edge;
            }

            for(int s = 0; s < surfaceCount; s++)
            {
                int firstIntersectionIndex = polygonFirst[s];

                if(firstIntersectionIndex == -1)
                {
#if DEBUG
                    Debug.LogWarning($"Polygon {s} has no found edges");
#endif
                    continue;
                }

                short polygonIndex = (short)polygons.Count;

                {
                    polygonEdgeIndices.Clear();
                    int  intersectionIteratorIndex = firstIntersectionIndex;
                    uint polygonCount              = 0;

                    do
                    {
                        int      edgeIndex = edgeIntersections[intersectionIteratorIndex].edgeIndex;
                        HalfEdge edge      = edges[edgeIndex];
                        edge.polygonIndex = polygonIndex;
                        edges[edgeIndex]  = edge;

                        polygonEdgeIndices.Add(edgeIndex);

                        intersectionIteratorIndex = edgeIntersections[intersectionIteratorIndex].nextIndex;

                        if(intersectionIteratorIndex == -1)
                        {
#if DEBUG
                            Debug.LogWarning("intersectionIteratorIndex == -1");
#endif
                            break;
                        }

                        polygonCount++;

                        if(polygonCount <= 10000)
                        {
                            continue;
                        }

#if DEBUG
                        Debug.LogError("loop_counter > 10000");
#endif
                        return null;
                    }
                    while(intersectionIteratorIndex != firstIntersectionIndex);
                }

                if(polygonEdgeIndices.Count < 3)
                {
#if DEBUG
                    Debug.LogWarning($"Not enough edges found ({polygonEdgeIndices.Count}) for polygon {s}");

                    continue;
#endif
                }

                {
                    polygons.Add(new Polygon(polygonEdgeIndices.ToArray(), s));
                }
            }

            newMesh.vertices = vertices.ToArray();
            newMesh.polygons = polygons.ToArray();
            newMesh.edges    = edges.ToArray();

            RemoveUnusedTexGens(newMesh, shape);

            return newMesh;
        }

        public static List<int> FindEdgeLoop(ControlMesh controlMesh, ref List<int> selectedEdges)
        {
            if(selectedEdges.Count == 0)
            {
                return null;
            }

            List<int> edgeLoop        = new List<int>();
            int       startEdgeIndex  = selectedEdges.Count - 1;
            int       selectedEdge    = selectedEdges[startEdgeIndex];
            int       startTwinIndex  = selectedEdges.IndexOf(controlMesh.GetTwinEdgeIndex(selectedEdge));
            short     startVertexCurr = controlMesh.GetVertexIndex(selectedEdge);
            short     startVertexPrev = controlMesh.GetTwinEdgeVertexIndex(selectedEdge);
            short     prevVertex      = startVertexCurr;

            edgeLoop.Add(selectedEdge);
            selectedEdges.RemoveAt(startEdgeIndex);

            if(startTwinIndex != -1)
            {
                selectedEdges.RemoveAt(startTwinIndex);
            }

            while(true)
            {
                bool found = false;

                for(int currentEdgeIndex = selectedEdges.Count - 1; currentEdgeIndex >= 0; currentEdgeIndex--)
                {
                    int   currentEdge       = selectedEdges[currentEdgeIndex];
                    short currentVertexCurr = controlMesh.GetVertexIndex(currentEdge);
                    short currentVertexPrev = controlMesh.GetTwinEdgeVertexIndex(currentEdge);

                    if(currentVertexPrev != prevVertex)
                    {
                        continue;
                    }

                    edgeLoop.Add(currentEdge);
                    selectedEdges.RemoveAt(currentEdgeIndex);

                    int currentTwinIndex = selectedEdges.IndexOf(controlMesh.GetTwinEdgeIndex(currentEdge));

                    if(currentTwinIndex != -1)
                    {
                        selectedEdges.RemoveAt(currentTwinIndex);
                    }

                    found = true;

                    if(currentVertexCurr == startVertexPrev)
                    {
                        return edgeLoop;
                    }

                    prevVertex = currentVertexCurr;

                    break;
                }

                if(!found)
                {
                    return null;
                }
            }
        }

        public static ControlMesh EnsureValidControlMesh(CSGBrush brush)
        {
            if(brush == null)
            {
                return null;
            }

            ControlMesh controlMesh = brush.controlMesh;

            if(controlMesh == null ||
               controlMesh.vertices == null || controlMesh.vertices.Length == 0 ||
               controlMesh.edges == null || controlMesh.edges.Length == 0 ||
               controlMesh.polygons == null || controlMesh.polygons.Length == 0 ||
               brush.shape == null)
            {
                BrushFactory.CreateCubeControlMesh(out brush.controlMesh, out brush.shape, Vector3.one);
            }

            if(brush.shape.surfaces == null || brush.shape.surfaces.Length == 0)
            {
                CalculatePlanes(controlMesh, brush.shape);
            }

            return controlMesh;
        }

        public static void FindAllAcceptableEdgesAroundEdge(ControlMesh controlMesh, AcceptableTopology acceptableTopology, int centerEdge, int currentOverPoint)
        {
            acceptableTopology.Clear();

            int centerTwinEdge = controlMesh.GetTwinEdgeIndex(centerEdge);

            short polygonIndex1 = controlMesh.GetEdgePolygonIndex(centerEdge);
            short polygonIndex2 = controlMesh.GetEdgePolygonIndex(centerTwinEdge);

            if(polygonIndex1 >= 0)
            {
                acceptableTopology.acceptablePolygons.Add(polygonIndex1);
            }

            if(polygonIndex2 >= 0)
            {
                acceptableTopology.acceptablePolygons.Add(polygonIndex2);
            }

            foreach(short polygonIndex in acceptableTopology.acceptablePolygons)
            {
                int[] edgeIndices = controlMesh.polygons[polygonIndex].edgeIndices;

                for(int e = 0; e < edgeIndices.Length; e++)
                {
                    int edgeIndex = edgeIndices[e];

                    if(controlMesh.GetTwinEdgeVertexIndex(edgeIndex) == currentOverPoint)
                    {
                        continue;
                    }

                    acceptableTopology.acceptableEdges.Add(edgeIndex);
                    acceptableTopology.acceptablePoints.Add(controlMesh.GetVertexIndex(edgeIndex));
                }
            }

            acceptableTopology.acceptableEdges.Remove(centerEdge);
            acceptableTopology.acceptableEdges.Remove(centerTwinEdge);
            acceptableTopology.acceptablePoints.Remove(controlMesh.GetVertexIndex(centerEdge));
            acceptableTopology.acceptablePoints.Remove(controlMesh.GetVertexIndex(centerTwinEdge));
        }

        public static void FindAllAcceptableEdgesAroundPoint(ControlMesh controlMesh, AcceptableTopology acceptableTopology, int centerPointEdge, int currentOverPoint)
        {
            acceptableTopology.Clear();

            int iterator = centerPointEdge;

            do
            {
                short polygonIndex = controlMesh.GetEdgePolygonIndex(iterator);

                if(acceptableTopology.acceptablePolygons.Add(polygonIndex))
                {
                    int[] edgeIndices = controlMesh.polygons[polygonIndex].edgeIndices;

                    for(int e = 0; e < edgeIndices.Length; e++)
                    {
                        int edgeIndex = edgeIndices[e];

                        if(edgeIndex == iterator ||
                           controlMesh.GetTwinEdgeVertexIndex(edgeIndex) == currentOverPoint)
                        {
                            continue;
                        }

                        acceptableTopology.acceptableEdges.Add(edgeIndex);
                        acceptableTopology.acceptablePoints.Add(controlMesh.GetVertexIndex(edgeIndex));
                    }
                }

                iterator = controlMesh.GetNextEdgeIndexAroundVertex(iterator);
            }
            while(iterator != centerPointEdge);

            iterator = centerPointEdge;

            do
            {
                int   twinIterator = controlMesh.GetTwinEdgeIndex(iterator);
                short vertex1      = controlMesh.GetVertexIndex(iterator);
                short vertex2      = controlMesh.GetVertexIndex(twinIterator);
                acceptableTopology.acceptablePoints.Remove(vertex1);
                acceptableTopology.acceptablePoints.Remove(vertex2);
                acceptableTopology.acceptableEdges.Remove(iterator);
                acceptableTopology.acceptableEdges.Remove(twinIterator);
                iterator = controlMesh.GetNextEdgeIndexAroundVertex(iterator);
            }
            while(iterator != centerPointEdge);
        }

        public static bool RebuildShapes(CSGBrush[] brushes)
        {
            bool result = true;

            for(int i = 0; i < brushes.Length; i++)
            {
                result = RebuildShape(brushes[i]) && result;
            }

            return result;
        }

        public static bool RebuildShapeFrom(CSGBrush brush, ControlMesh srcControlMesh, Shape srcShape)
        {
            if(brush == null || srcControlMesh == null || srcShape == null)
            {
                return false;
            }

            // subdivide polygons using ear-clipping
            if(!Triangulate(brush, srcControlMesh, srcShape))
            {
                srcControlMesh.Valid = false;

                return false;
            }

            FixTexGens(srcControlMesh, srcShape);

            if(!IsConvex(srcControlMesh, srcShape))
            {
                srcControlMesh.Valid = false;

                return false;
            }

            UpdateTangents(srcControlMesh, srcShape);

            if(!UpdateBrushMesh(brush, srcControlMesh, srcShape))
            {
                srcControlMesh.Valid = false;

                return false;
            }

            srcControlMesh.generation = -1;
            brush.controlMesh.Valid   = true;

            return true;
        }

        public static bool RebuildShape(CSGBrush brush)
        {
            if(brush == null || brush.controlMesh == null || brush.shape == null)
            {
                return false;
            }

            return RebuildShapeFrom(brush, brush.controlMesh.Clone(), brush.shape.Clone());
        }

        internal static bool IsConvex(ControlMesh controlMesh, Shape srcShape)
        {
            bool isConvex = true;

            if(srcShape == null || controlMesh == null)
            {
                return false;
            }

            for(int j = 0; isConvex && j < controlMesh.vertices.Length; j++)
            {
                Vector3 point = controlMesh.vertices[j];

                for(int i = 0; i < srcShape.surfaces.Length; i++)
                {
                    CSGPlane plane = srcShape.surfaces[i].Plane;

                    if(plane.Distance(point) > MathConstants.DISTANCE_EPSILON)
                    {
                        isConvex = false;

                        break;
                    }
                }
            }

            return isConvex;
        }

        public static bool BuildMesh(CSGBrush brush, ControlMesh controlMesh, Shape srcShape)
        {
            if(srcShape == null || controlMesh == null)
            {
                return false;
            }

            if(IsConvex(controlMesh, srcShape))
            {
                return UpdateBrushMesh(brush, controlMesh, srcShape);
            }

            controlMesh.Valid = false;

            return false;
        }

        internal static void UpdateTangents(ControlMesh controlMesh, Shape srcShape)
        {
            RemoveUnusedTexGens(controlMesh, srcShape);

            for(int i = 0; i < srcShape.surfaces.Length; i++)
            {
                Vector3 normal = srcShape.surfaces[i].Plane.Normal;

                GeometryUtility.CalculateTangents(normal, out Vector3 tangent, out Vector3 binormal);

                srcShape.surfaces[i].Tangent     = tangent;
                srcShape.surfaces[i].BiNormal    = binormal;
                srcShape.surfaces[i].TexGenIndex = controlMesh.polygons[i].texGenIndex;
            }

            ShapeUtility.CheckMaterials(srcShape);
        }

        internal static bool UpdateBrushMesh(CSGBrush brush, ControlMesh controlMesh, Shape srcShape)
        {
            if(srcShape.surfaces.Length > controlMesh.polygons.Length)
            {
                return false;
            }

            brush.shape       = srcShape.Clone();
            brush.controlMesh = controlMesh.Clone();
            brush.EnsureInitialized();

            if(brush.brushNodeID != CSGNode.INVALID_NODE_ID)
            {
                InternalCSGModelManager.CheckSurfaceModifications(brush, true);
                EditorUtility.SetDirty(brush);
            }

            return true;
        }

        // note: expect values in worldspace
        public static void TransformBrushes(CSGBrush[] brushes, Vector3 centerBefore, Vector3 centerAfter, Vector3 scale, SpaceMatrices spaceMatrices)
        {
            bool invertBrush = (((scale.x < 0 ? 1 : 0) + (scale.y < 0 ? 1 : 0) + (scale.z < 0 ? 1 : 0)) & 1) == 1;

            PivotRotation pivotRotation = Tools.pivotRotation;

            for(int t = 0; t < brushes.Length; t++)
            {
                CSGBrush    brush          = brushes[t];
                Transform   brushTransform = brushes[t].transform;
                Shape       shape          = brush.shape;
                ControlMesh controlMesh    = brush.controlMesh;

                Matrix4x4 brushLocalToWorld = brushTransform.localToWorldMatrix;
                Matrix4x4 brushWorldToLocal = brushTransform.worldToLocalMatrix;

                Vector3[] vertices = controlMesh.vertices;

                for(int p = 0; p < controlMesh.vertices.Length; p++)
                {
                    Vector3 pt1 = brushLocalToWorld.MultiplyPoint(vertices[p]);

                    if(pivotRotation == PivotRotation.Local)
                    {
                        pt1 = spaceMatrices.activeWorldToLocal.MultiplyPoint(pt1);
                    }

                    // world space
                    pt1 -= centerBefore;
                    pt1.Scale(scale);
                    pt1 += centerAfter;

                    if(pivotRotation == PivotRotation.Local)
                    {
                        pt1 = spaceMatrices.activeLocalToWorld.MultiplyPoint(pt1);
                    }

                    pt1 = brushWorldToLocal.MultiplyPoint(pt1);

                    vertices[p] = GridUtility.CleanPosition(pt1);
                }

                if(invertBrush)
                {
                    InvertControlMesh(controlMesh);
                    InvertShape(shape);
                    controlMesh.Valid = Validate(controlMesh, shape);
                }

                CalculatePlanes(controlMesh, shape);
                brush.EnsureInitialized();

                brush.controlMesh.SetDirty();
                EditorUtility.SetDirty(brush);
            }
        }

        public static void InvertControlMesh(ControlMesh controlMesh)
        {
            for(int p = 0; p < controlMesh.polygons.Length; p++)
            {
                Array.Reverse(controlMesh.polygons[p].edgeIndices);
            }

            short[] vertexIndices = new short[controlMesh.edges.Length];

            for(int e = 0; e < controlMesh.edges.Length; e++)
            {
                vertexIndices[e] = controlMesh.edges[e].vertexIndex;
            }

            for(int e = 0; e < controlMesh.edges.Length; e++)
            {
                int twinIndex = controlMesh.GetTwinEdgeIndex(e);
                controlMesh.edges[e].vertexIndex = vertexIndices[twinIndex];
            }
        }

        public static void InvertShape(Shape shape)
        {
            for(int s = 0; s < shape.surfaces.Length; s++)
            {
                shape.surfaces[s].Plane = shape.surfaces[s].Plane.Negated();
            }
        }

        public static short[] FindDuplicateVerticesToRemove(CSGBrush brush, ControlMeshState meshState)
        {
            ControlMesh    targetControlMesh     = brush.controlMesh;
            HalfEdge[]     edges                 = targetControlMesh.edges;
            Vector3[]      vertices              = targetControlMesh.vertices;
            HashSet<short> foundVerticesToRemove = new HashSet<short>();

            for(int index1 = 0; index1 < vertices.Length; index1++)
            {
                Vector3 vertex1 = vertices[index1];

                for(int index2 = index1 + 1; index2 < vertices.Length; index2++)
                {
                    Vector3 vertex2 = vertices[index2];

                    if(!((vertex1 - vertex2).magnitude < MathConstants.EQUALITY_EPSILON))
                    {
                        continue;
                    }

                    bool found = false;

                    for(int e = 0; e < edges.Length; e++)
                    {
                        short vertexIndex1 = edges[e].vertexIndex;
                        int   twinIndex    = edges[e].twinIndex;
                        short vertexIndex2 = edges[twinIndex].vertexIndex;

                        if(vertexIndex1 == index1 && vertexIndex2 == index2)
                        {
                            found = true;

                            break;
                        }

                        if(vertexIndex1 != index2 || vertexIndex2 != index1)
                        {
                            continue;
                        }

                        found = true;

                        break;
                    }

                    if(found)
                    {
                        foundVerticesToRemove.Add((short)index1);
                    }
                }
            }

            if(foundVerticesToRemove.Count == 0)
            {
                return null;
            }

            short[] foundVerticesToRemoveArray = foundVerticesToRemove.ToArray();
            Array.Sort(foundVerticesToRemoveArray);

            return foundVerticesToRemoveArray;
        }

        public static short[] FindDuplicateVerticesToRemove(CSGBrush brush)
        {
            ControlMesh    targetControlMesh     = brush.controlMesh;
            HalfEdge[]     edges                 = targetControlMesh.edges;
            Vector3[]      vertices              = targetControlMesh.vertices;
            HashSet<short> foundVerticesToRemove = new HashSet<short>();

            for(int index1 = 0; index1 < vertices.Length; index1++)
            {
                Vector3 vertex1 = vertices[index1];

                for(int index2 = index1 + 1; index2 < vertices.Length; index2++)
                {
                    Vector3 vertex2 = vertices[index2];

                    if(!((vertex1 - vertex2).magnitude < MathConstants.EQUALITY_EPSILON))
                    {
                        continue;
                    }

                    bool found = false;

                    for(int e = 0; e < edges.Length; e++)
                    {
                        short vertexIndex1 = edges[e].vertexIndex;
                        int   twinIndex    = edges[e].twinIndex;
                        short vertexIndex2 = edges[twinIndex].vertexIndex;

                        if(vertexIndex1 == index1 && vertexIndex2 == index2)
                        {
                            found = true;

                            break;
                        }

                        if(vertexIndex1 != index2 || vertexIndex2 != index1)
                        {
                            continue;
                        }

                        found = true;

                        break;
                    }

                    if(found)
                    {
                        foundVerticesToRemove.Add((short)index1);
                    }
                }
            }

            if(foundVerticesToRemove.Count == 0)
            {
                return null;
            }

            short[] foundVerticesToRemoveArray = foundVerticesToRemove.ToArray();
            Array.Sort(foundVerticesToRemoveArray);

            return foundVerticesToRemoveArray;
        }

        public static void MergeDuplicatePoints(CSGBrush[] brushes, ControlMeshState[] controlMeshStates)
        {
            if(brushes == null || controlMeshStates == null)
            {
                return;
            }

            for(int b = 0; b < brushes.Length; b++)
            {
                CSGBrush brush = brushes[b];

                if(!brush)
                {
                    continue;
                }

                if(b >= controlMeshStates.Length)
                {
                    continue;
                }

                ControlMeshState meshState = controlMeshStates[b];

                if(meshState == null)
                {
                    continue;
                }

                short[] deletePoints = FindDuplicateVerticesToRemove(brush, meshState);

                if(deletePoints == null)
                {
                    continue;
                }

                ControlMesh controlMesh = brush.controlMesh.Clone();
                Shape       shape       = brush.shape.Clone();

                RemoveControlPoints(controlMesh, shape, deletePoints);
                controlMesh.SetDirty();

                RebuildShapeFrom(brush, controlMesh, shape);

                EditorUtility.SetDirty(brush);
            }
        }

        public static bool MergeDuplicatePoints(CSGBrush brush, ref ControlMesh controlMesh, ref Shape shape)
        {
            if(!brush || controlMesh == null)
            {
                return false;
            }

            short[] deletePoints = FindDuplicateVerticesToRemove(brush);

            if(deletePoints == null)
            {
                return false;
            }

            controlMesh = controlMesh.Clone();
            shape       = shape.Clone();
            RemoveControlPoints(controlMesh, shape, deletePoints);

            return true;
        }

        public static void MergeHoverPolygonPoints(CSGBrush brush, int polygonIndex)
        {
            if(brush == null)
            {
                return;
            }

            ControlMesh controlMesh = brush.controlMesh;
            Shape       shape       = brush.shape;
            int[]       edgeIndices = controlMesh.polygons[polygonIndex].edgeIndices.ToArray();

            HashSet<short> deletePoints = new HashSet<short>();

            for(int e = edgeIndices.Length - 1; e >= 0; e--)
            {
                int halfEdgeIndex = edgeIndices[e];
                int twinEdgeIndex = controlMesh.GetTwinEdgeIndex(halfEdgeIndex);

                short vertexIndex1 = controlMesh.edges[halfEdgeIndex].vertexIndex;
                short vertexIndex2 = controlMesh.edges[twinEdgeIndex].vertexIndex;

                Vector3 vertex1 = controlMesh.vertices[vertexIndex1];
                Vector3 vertex2 = controlMesh.vertices[vertexIndex2];

                Vector3 newPoint = (vertex2 + vertex1) * 0.5f;

                // insert points on all edges, in between points
                InsertControlpoint(controlMesh, newPoint, halfEdgeIndex);

                deletePoints.Add(vertexIndex1);
                deletePoints.Add(vertexIndex2);
            }

            // remove all old edge points
            short[] controlPoints = deletePoints.ToArray();
            Array.Sort(controlPoints);
            RemoveControlPoints(controlMesh, shape, controlPoints);

            controlMesh.SetDirty();
            RebuildShape(brush);
        }

        public static void MergeHoverEdgePoints(CSGBrush brush, ControlMeshState controlMeshState, int edgeIndex)
        {
            ControlMesh controlMesh = brush.controlMesh;
            Shape       shape       = brush.shape;

            int halfEdgeIndex = controlMeshState.edgeStateToHalfEdge[edgeIndex];
            int twinEdgeIndex = controlMesh.GetTwinEdgeIndex(halfEdgeIndex);

            short vertexIndex1 = controlMesh.edges[halfEdgeIndex].vertexIndex;
            short vertexIndex2 = controlMesh.edges[twinEdgeIndex].vertexIndex;

            Vector3 vertex1 = controlMesh.vertices[vertexIndex1];
            Vector3 vertex2 = controlMesh.vertices[vertexIndex2];

            Vector3 newPoint = (vertex2 + vertex1) * 0.5f;

            // insert points on all edges, in between points
            InsertControlpoint(controlMesh, newPoint, halfEdgeIndex);

            short[] controlPoints = vertexIndex1 > vertexIndex2
                ? new[] { vertexIndex2, vertexIndex1 }
                : new[] { vertexIndex1, vertexIndex2 };

            // remove all old edge points
            RemoveControlPoints(controlMesh, shape, controlPoints);
            controlMesh.SetDirty();

            RebuildShape(brush);
        }

        public static void MergeSelectedEdgePoints(CSGBrush[] brushes, ControlMeshState[] controlMeshStates)
        {
            if(brushes == null || controlMeshStates == null)
            {
                return;
            }

            for(int b = 0; b < brushes.Length; b++)
            {
                CSGBrush brush = brushes[b];

                if(!brush || b >= controlMeshStates.Length)
                {
                    continue;
                }

                ControlMeshState meshState = controlMeshStates[b];

                if(meshState == null)
                {
                    continue;
                }

                ControlMesh controlMesh = brush.controlMesh;
                Shape       shape       = brush.shape;

                HashSet<short> deletePoints = new HashSet<short>();

                for(int e = meshState.selection.edges.Length - 1; e >= 0; e--)
                {
                    if((meshState.selection.edges[e] & SelectState.Selected) != SelectState.Selected)
                    {
                        continue;
                    }

                    int halfEdgeIndex = meshState.edgeStateToHalfEdge[e];
                    int twinEdgeIndex = controlMesh.GetTwinEdgeIndex(halfEdgeIndex);

                    short vertexIndex1 = controlMesh.edges[halfEdgeIndex].vertexIndex;
                    short vertexIndex2 = controlMesh.edges[twinEdgeIndex].vertexIndex;

                    Vector3 vertex1 = controlMesh.vertices[vertexIndex1];
                    Vector3 vertex2 = controlMesh.vertices[vertexIndex2];

                    Vector3 newPoint = (vertex2 + vertex1) * 0.5f;

                    // insert points on all edges, in between points
                    InsertControlpoint(controlMesh, newPoint, halfEdgeIndex);

                    deletePoints.Add(vertexIndex1);
                    deletePoints.Add(vertexIndex2);
                }

                // remove all old edge points
                short[] controlPoints = deletePoints.ToArray();
                Array.Sort(controlPoints);
                RemoveControlPoints(controlMesh, shape, controlPoints);

                controlMesh.SetDirty();
                RebuildShape(brush);
            }
        }

        public static bool DeleteSelectedPoints(CSGBrush[] brushes, ControlMeshState[] controlMeshStates)
        {
            bool modified = false;

            for(int t = brushes.Length - 1; t >= 0; t--)
            {
                CSGBrush         targetBrush       = brushes[t];
                ControlMesh      targetControlMesh = targetBrush.controlMesh;
                Shape            targetShape       = targetBrush.shape;
                ControlMeshState meshState         = controlMeshStates[t];

                HashSet<short> controlPoints = meshState.GetSelectedPointIndices();

                if(controlPoints == null)
                {
                    continue;
                }

                short[] controlPointArray = controlPoints.ToArray();
                Array.Sort(controlPointArray);
                RemoveControlPoints(targetControlMesh, targetShape, controlPointArray);

                if(targetShape.surfaces.Length < 4)
                {
                    Undo.DestroyObjectImmediate(brushes[t].gameObject);
                    ArrayUtility.RemoveAt(ref brushes, t);
                    modified = true;
                }
                else
                {
                    targetControlMesh.SetDirty();
                    RebuildShape(targetBrush);
                }
            }

            return modified;
        }
    }
}
