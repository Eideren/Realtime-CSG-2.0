using System;
using System.Collections.Generic;
using InternalRealtimeCSG;
using RealtimeCSG.Components;
using RealtimeCSG.Legacy;
using UnityEditor;
using UnityEngine;

namespace RealtimeCSG
{
    [Serializable]
    internal sealed class Outline2D : IShapeSettings
    {
        public Curve2D curve = new Curve2D();
        public HandlePointCurve2D[] curvePointHandles;
        public HandleTangentCurve2D[] curveTangentHandles;
        public HandleEdgeCurve2D[] curveEdgeHandles;

        public Curve2D backupCurve = new Curve2D();

        public TexGen planeTexgen;

        public bool[] onGeometryVertices = Array.Empty<bool>();
        public CSGBrush[] onBrushVertices = Array.Empty<CSGBrush>();

        [NonSerialized] public Vector3[] realEdge = null;
        [NonSerialized] public Vector3[] realTangent = null;
        [NonSerialized] public int[] realTangentIDs = null;
        [NonSerialized] public SelectState[] realTangentSelection = null;
        [NonSerialized] public HandleConstraints[] realTangentState = null;

        [NonSerialized] public int prevHoverVertex = -1;
        [NonSerialized] public int prevHoverTangent = -1;
        [NonSerialized] public int prevHoverEdge = -1;

        public bool HaveVertices
        {
            get => curve.points.Length > 0;
        }

        public void Init(Vector3[] meshVertices, int[] indices)
        {
            curve.points      = new Vector3[indices.Length];
            curvePointHandles = new HandlePointCurve2D[indices.Length];
            curveEdgeHandles  = new HandleEdgeCurve2D[indices.Length];

            curve.tangents      = new TangentCurve2D[indices.Length * 2];
            curveTangentHandles = new HandleTangentCurve2D[indices.Length * 2];

            for(int i = 0, j = 0; i < indices.Length; i++, j += 2)
            {
                curve.points[i] = meshVertices[indices[i]];

                curve.tangents[j].constraint     = HandleConstraints.Straight;
                curve.tangents[j + 1].constraint = HandleConstraints.Straight;
                curve.tangents[j].tangent        = MathConstants.leftVector3;
                curve.tangents[j + 1].tangent    = MathConstants.rightVector3;
            }

            onGeometryVertices = new bool[indices.Length];
            onBrushVertices    = new CSGBrush[indices.Length];
        }

        public void Reset()
        {
            curve.points      = Array.Empty<Vector3>();
            curvePointHandles = Array.Empty<HandlePointCurve2D>();
            curveEdgeHandles  = Array.Empty<HandleEdgeCurve2D>();

            curve.tangents      = Array.Empty<TangentCurve2D>();
            curveTangentHandles = Array.Empty<HandleTangentCurve2D>();

            backupCurve.points   = Array.Empty<Vector3>();
            backupCurve.tangents = Array.Empty<TangentCurve2D>();

            onGeometryVertices = Array.Empty<bool>();
            onBrushVertices    = Array.Empty<CSGBrush>();
        }

        public void CalculatePlane(ref CSGPlane plane)
        {
            plane = GeometryUtility.CalcPolygonPlane(curve.points);
        }

        public void MoveShape(Vector3 offset)
        {
            for(int i = 0; i < curve.points.Length; i++)
            {
                curve.points[i] = backupCurve.points[i] + offset;
            }
        }

        public void Negated()
        {
            Array.Reverse(curve.points);
            Array.Reverse(curvePointHandles);
            Array.Reverse(curveEdgeHandles);

            Array.Reverse(curve.tangents);
            Array.Reverse(curveTangentHandles);

            Array.Reverse(onGeometryVertices);
            Array.Reverse(onBrushVertices);
        }

        public void AddVertex(Vector3 position, CSGBrush brush, CSGPlane plane, bool onGeometry)
        {
            if(curve.points.Length > 1)
            {
                if((curve.points[curve.points.Length - 1] - position).sqrMagnitude < MathConstants.EqualityEpsilonSqr)
                {
                    return;
                }

                if((curve.points[0] - position).sqrMagnitude < MathConstants.EqualityEpsilonSqr)
                {
                    return;
                }
            }

            TangentCurve2D leftTangentCurve = new TangentCurve2D
            {
                tangent    = MathConstants.leftVector3
              , constraint = HandleConstraints.Straight
            };

            TangentCurve2D rightTangentCurve = new TangentCurve2D
            {
                tangent    = MathConstants.rightVector3
              , constraint = HandleConstraints.Straight
            };

            HandlePointCurve2D handlePointCurve = new HandlePointCurve2D
            {
                id    = -1
              , state = SelectState.None
            };

            HandleTangentCurve2D leftTangentCurveHandle = new HandleTangentCurve2D
            {
                id         = -1
              , state      = SelectState.None
              , constraint = HandleConstraints.Straight
            };

            HandleTangentCurve2D rightTangentCurveHandle = new HandleTangentCurve2D
            {
                id         = -1
              , state      = SelectState.None
              , constraint = HandleConstraints.Straight
            };

            HandleEdgeCurve2D handleEdgeCurve = new HandleEdgeCurve2D
            {
                id     = -1
              , state  = SelectState.None
              , texgen = new TexGen(CSGSettings.DefaultMaterial)
            };

            ArrayUtility.Add(ref curve.points, position);
            ArrayUtility.Add(ref curvePointHandles, handlePointCurve);
            ArrayUtility.Add(ref curveEdgeHandles, handleEdgeCurve);

            ArrayUtility.Add(ref curve.tangents, rightTangentCurve);
            ArrayUtility.Add(ref curve.tangents, leftTangentCurve);
            ArrayUtility.Add(ref curveTangentHandles, rightTangentCurveHandle);
            ArrayUtility.Add(ref curveTangentHandles, leftTangentCurveHandle);

            ArrayUtility.Add(ref onGeometryVertices, onGeometry);
            ArrayUtility.Add(ref onBrushVertices, brush);
        }

        public void InsertVertexAfter(int i, Vector3 origin)
        {
            int               j             = (i + 1) * 2;
            int               k             = (i + 1) % curve.points.Length;
            HandleConstraints originalState = curveTangentHandles[i * 2 + 1].constraint;
            Vector3           tangent       = (curve.points[i] - curve.points[k]).normalized;

            TangentCurve2D leftTangentCurve = new TangentCurve2D
            {
                tangent    = tangent
              , constraint = originalState
            };

            TangentCurve2D rightTangentCurve = new TangentCurve2D
            {
                tangent    = -tangent
              , constraint = originalState
            };

            HandlePointCurve2D handlePointCurve = new HandlePointCurve2D
            {
                id      = -1
              , tangent = false
              , state   = SelectState.Selected
            };

            HandleTangentCurve2D leftTangentCurveHandle = new HandleTangentCurve2D
            {
                id         = -1
              , state      = SelectState.Selected
              , constraint = originalState
            };

            HandleTangentCurve2D rightTangentCurveHandle = new HandleTangentCurve2D
            {
                id         = -1
              , state      = SelectState.Selected
              , constraint = originalState
            };

            HandleEdgeCurve2D handleEdgeCurve = new HandleEdgeCurve2D
            {
                id     = -1
              , state  = curveEdgeHandles[i].state
              , texgen = curveEdgeHandles[i].texgen
            };

            ArrayUtility.Insert(ref curve.points, i + 1, origin);
            ArrayUtility.Insert(ref curvePointHandles, i + 1, handlePointCurve);
            ArrayUtility.Insert(ref curveEdgeHandles, i + 1, handleEdgeCurve);

            ArrayUtility.Insert(ref curve.tangents, j, rightTangentCurve);
            ArrayUtility.Insert(ref curve.tangents, j, leftTangentCurve);
            ArrayUtility.Insert(ref curveTangentHandles, j, rightTangentCurveHandle);
            ArrayUtility.Insert(ref curveTangentHandles, j, leftTangentCurveHandle);

            ArrayUtility.Insert(ref onGeometryVertices, i + 1, onGeometryVertices[i]);
            ArrayUtility.Insert(ref onBrushVertices, i + 1, onBrushVertices[i]);
        }

        public void SetTangent(int index, Vector3 tangent)
        {
            if(curveTangentHandles[index].constraint == HandleConstraints.Mirrored)
            {
                if((index & 1) == 1)
                {
                    curve.tangents[(index + curve.tangents.Length - 1) % curve.tangents.Length].tangent = -tangent;
                }
                else
                {
                    curve.tangents[(index + curve.tangents.Length + 1) % curve.tangents.Length].tangent = -tangent;
                }
            }

            curvePointHandles[index / 2].tangent = true;
            curve.tangents[index].tangent        = tangent;
        }

        public void SetPosition(int index, Vector3 position)
        {
            curve.points[index] = position;
        }

        public Vector3 GetPosition(int index)
        {
            return curve.points[index];
        }

        public Vector3[] GetVertices()
        {
            return curve.points;
        }

        public int VertexLength
        {
            get => curve.points.Length;
        }

        public bool HaveSelectedEdges
        {
            get
            {
                if(curveEdgeHandles == null)
                {
                    return false;
                }

                for(int i = 0; i < curveEdgeHandles.Length; i++)
                {
                    if((curveEdgeHandles[i].state & SelectState.Selected) != 0)
                    {
                        return true;
                    }
                }

                return false;
            }
        }

        public bool HaveSelectedVertices
        {
            get
            {
                if(curvePointHandles == null)
                {
                    return false;
                }

                for(int i = 0; i < curvePointHandles.Length; i++)
                {
                    if((curvePointHandles[i].state & SelectState.Selected) != 0)
                    {
                        return true;
                    }
                }

                return false;
            }
        }

        public void DeleteSelectedVertices()
        {
            for(int p = curvePointHandles.Length - 1; p >= 0; p--)
            {
                if((curvePointHandles[p].state & SelectState.Selected) != SelectState.Selected)
                {
                    continue;
                }

                ArrayUtility.RemoveAt(ref curve.points, p);
                ArrayUtility.RemoveAt(ref curvePointHandles, p);
                ArrayUtility.RemoveAt(ref curveEdgeHandles, p);

                if(backupCurve != null && backupCurve.points != null && p < backupCurve.points.Length)
                {
                    ArrayUtility.RemoveAt(ref backupCurve.points, p);
                }

                int t = p * 2;
                ArrayUtility.RemoveAt(ref curve.tangents, t + 1);
                ArrayUtility.RemoveAt(ref curve.tangents, t + 0);

                ArrayUtility.RemoveAt(ref curveTangentHandles, t + 1);
                ArrayUtility.RemoveAt(ref curveTangentHandles, t + 0);

                if(backupCurve != null && backupCurve.tangents != null && t + 1 < backupCurve.tangents.Length)
                {
                    ArrayUtility.RemoveAt(ref backupCurve.tangents, t + 1);
                }

                if(backupCurve != null && backupCurve.tangents != null && t + 0 < backupCurve.tangents.Length)
                {
                    ArrayUtility.RemoveAt(ref backupCurve.tangents, t + 0);
                }

                ArrayUtility.RemoveAt(ref onGeometryVertices, p);
                ArrayUtility.RemoveAt(ref onBrushVertices, p);
            }
        }

        public void DeleteVertex(int v)
        {
            if(v >= curve.points.Length)
            {
                return;
            }

            ArrayUtility.RemoveAt(ref curve.points, v);
            ArrayUtility.RemoveAt(ref curvePointHandles, v);
            ArrayUtility.RemoveAt(ref curveEdgeHandles, v);

            if(backupCurve != null && backupCurve.points != null && v < backupCurve.points.Length)
            {
                ArrayUtility.RemoveAt(ref backupCurve.points, v);
            }

            int t = v * 2;
            ArrayUtility.RemoveAt(ref curve.tangents, t + 1);
            ArrayUtility.RemoveAt(ref curve.tangents, t + 0);

            ArrayUtility.RemoveAt(ref curveTangentHandles, t + 1);
            ArrayUtility.RemoveAt(ref curveTangentHandles, t + 0);

            if(backupCurve != null && backupCurve.tangents != null && t + 1 < backupCurve.tangents.Length)
            {
                ArrayUtility.RemoveAt(ref backupCurve.tangents, t + 1);
            }

            if(backupCurve != null && backupCurve.tangents != null && t + 0 < backupCurve.tangents.Length)
            {
                ArrayUtility.RemoveAt(ref backupCurve.tangents, t + 0);
            }

            ArrayUtility.RemoveAt(ref onGeometryVertices, v);
            ArrayUtility.RemoveAt(ref onBrushVertices, v);
        }

        private bool Select(ref SelectState state, SelectionType selectionType, bool onlyOnHover = true)
        {
            SelectState oldState = state;

            if(onlyOnHover && (oldState & SelectState.Hovering) != SelectState.Hovering)
            {
                return false;
            }

            SelectState newState = oldState;

            if(selectionType == SelectionType.Subtractive)
            {
                newState &= ~SelectState.Selected;
            }
            else if(selectionType == SelectionType.Toggle)
            {
                newState ^= SelectState.Selected;
            }
            else
            {
                newState |= SelectState.Selected;
            }

            if(oldState == newState)
            {
                return false;
            }

            state = newState;

            return true;
        }

        public bool SelectVertex(int index)
        {
            bool changed = Select(ref curvePointHandles[index].state, SelectionType.Additive, false);

            return changed;
        }

        public bool SelectVertex(int index, SelectionType selectionType, bool onlyOnHover = true)
        {
            bool changed = Select(ref curvePointHandles[index].state, selectionType, onlyOnHover);

            if(changed && !IsVertexSelected(index))
            {
                index = (index - 1 + curveEdgeHandles.Length) % curveEdgeHandles.Length;
            }

            return Select(ref curveEdgeHandles[index].state, SelectionType.Subtractive, false) || changed;
        }

        public bool SelectTangent(int index, SelectionType selectionType, bool onlyOnHover = true)
        {
            return Select(ref curveTangentHandles[index].state, selectionType, onlyOnHover);
        }

        public bool SelectEdge(int index, SelectionType selectionType, bool onlyOnHover = true)
        {
            bool changed = Select(ref curveEdgeHandles[index].state, selectionType, onlyOnHover);
            changed = Select(ref curvePointHandles[index].state, selectionType, false) || changed;
            index   = (index + 1) % curvePointHandles.Length;

            if(selectionType == SelectionType.Toggle)
            {
                selectionType = SelectionType.Additive;
            }

            return Select(ref curvePointHandles[index].state, selectionType, false) || changed;
        }

        private bool HoverOn(ref SelectState state)
        {
            if((state & SelectState.Hovering) == SelectState.Hovering)
            {
                return false;
            }

            state |= SelectState.Hovering;

            return true;
        }

        public bool HoverOnVertex(int index)
        {
            return HoverOn(ref curvePointHandles[index].state);
        }

        public bool HoverOnTangent(int index)
        {
            return HoverOn(ref curveTangentHandles[index].state);
        }

        public bool HoverOnEdge(int index)
        {
            return HoverOn(ref curveEdgeHandles[index].state);
        }

        public void UnHoverAll()
        {
            for(int p = 0; p < curvePointHandles.Length; p++)
            {
                curvePointHandles[p].state &= ~SelectState.Hovering;
            }

            for(int p = 0; p < curveTangentHandles.Length; p++)
            {
                curveTangentHandles[p].state &= ~SelectState.Hovering;
            }

            for(int e = 0; e < curveEdgeHandles.Length; e++)
            {
                curveEdgeHandles[e].state &= ~SelectState.Hovering;
            }
        }

        public bool DeselectAll()
        {
            bool hadSelection = false;

            for(int p = 0; p < curvePointHandles.Length; p++)
            {
                hadSelection = hadSelection || (curvePointHandles[p].state & SelectState.Selected) == SelectState.Selected;

                curvePointHandles[p].state &= ~SelectState.Selected;
            }

            for(int p = 0; p < curveTangentHandles.Length; p++)
            {
                hadSelection = hadSelection || (curveTangentHandles[p].state & SelectState.Selected) == SelectState.Selected;

                curveTangentHandles[p].state &= ~SelectState.Selected;
            }

            for(int e = 0; e < curveEdgeHandles.Length; e++)
            {
                hadSelection = hadSelection || (curveEdgeHandles[e].state & SelectState.Selected) == SelectState.Selected;

                curveEdgeHandles[e].state &= ~SelectState.Selected;
            }

            return hadSelection;
        }

        public bool IsVertexSelected(int index)
        {
            return (curvePointHandles[index].state & SelectState.Selected) == SelectState.Selected;
        }

        public bool IsTangentSelected(int index)
        {
            return (curveTangentHandles[index].state & SelectState.Selected) == SelectState.Selected;
        }

        public bool IsEdgeSelected(int index)
        {
            return (curveEdgeHandles[index].state & SelectState.Selected) == SelectState.Selected;
        }

        public void CopyBackupVertices()
        {
            backupCurve.points = new Vector3[curve.points.Length];
            Array.Copy(curve.points, backupCurve.points, curve.points.Length);

            backupCurve.tangents = new TangentCurve2D[curve.tangents.Length];
            Array.Copy(curve.tangents, backupCurve.tangents, curve.tangents.Length);
        }

        public void UpdateEdgeMaterials(Vector3 extrusionDirection)
        {
            for(int i = 0; i < curve.points.Length; i++)
            {
                UpdateEdgeMaterial(i, extrusionDirection);
            }
        }

        public void UpdateEdgeMaterial(int edgeIndex, Vector3 extrusionDirection)
        {
            int vertexIndex = (edgeIndex + curve.points.Length - 1) % curve.points.Length;

            Vector3 vertex0 = curve.points[vertexIndex];
            Vector3 vertex1 = curve.points[edgeIndex];

            HashSet<CSGBrush> uniqueBrushes = new HashSet<CSGBrush>();

            foreach(CSGBrush brush in onBrushVertices)
            {
                if(brush)
                {
                    uniqueBrushes.Add(brush);
                }
            }

            int[] planeIndices = new int[2];

            foreach(CSGBrush brush in uniqueBrushes)
            {
                int       planeIndex = 0;
                Shape     shape      = brush.Shape;
                Surface[] surfaces   = shape.Surfaces;
                TexGen[]  texgens    = shape.TexGens;

                Matrix4x4 worldToLocalMatrix = brush.transform.worldToLocalMatrix;
                Vector3   localVertex0       = worldToLocalMatrix.MultiplyPoint(vertex0);
                Vector3   localVertex1       = worldToLocalMatrix.MultiplyPoint(vertex1);
                Vector3   localDirection     = worldToLocalMatrix.MultiplyVector(extrusionDirection);

                for(int surfaceIndex = 0; surfaceIndex < surfaces.Length; surfaceIndex++)
                {
                    Surface surface = surfaces[surfaceIndex];
                    float   dist1   = Mathf.Abs(surface.Plane.Distance(localVertex0));

                    if(dist1 > MathConstants.DistanceEpsilon)
                    {
                        continue;
                    }

                    float dist2 = Mathf.Abs(surface.Plane.Distance(localVertex1));

                    if(dist2 > MathConstants.DistanceEpsilon)
                    {
                        continue;
                    }

                    planeIndices[planeIndex] = surfaceIndex;
                    planeIndex++;

                    if(planeIndex == 2)
                    {
                        float alignment1 = Mathf.Abs(Vector3.Dot(surfaces[planeIndices[0]].Plane.normal, localDirection));
                        float alignment2 = Mathf.Abs(Vector3.Dot(surfaces[planeIndices[1]].Plane.normal, localDirection));

                        int texGenIndex = alignment1 < alignment2 ? surfaces[planeIndices[0]].TexGenIndex : surfaces[planeIndices[1]].TexGenIndex;

                        curveEdgeHandles[vertexIndex].texgen = texgens[texGenIndex];

                        break;
                    }
                }
            }
        }

        public void TryFindPlaneMaterial(CSGPlane buildPlane)
        {
            HashSet<CSGBrush> uniqueBrushes = new HashSet<CSGBrush>();

            foreach(CSGBrush brush in onBrushVertices)
            {
                if(brush)
                {
                    uniqueBrushes.Add(brush);
                }
            }

            foreach(CSGBrush brush in uniqueBrushes)
            {
                Shape     shape    = brush.Shape;
                Surface[] surfaces = shape.Surfaces;
                TexGen[]  texgens  = shape.TexGens;

                Matrix4x4 worldToLocalMatrix = brush.transform.worldToLocalMatrix;
                Vector3   localPosition      = worldToLocalMatrix.MultiplyPoint(buildPlane.pointOnPlane);
                Vector3   localDirection     = worldToLocalMatrix.MultiplyVector(buildPlane.normal);

                for(int surfaceIndex = 0; surfaceIndex < surfaces.Length; surfaceIndex++)
                {
                    Surface surface = surfaces[surfaceIndex];
                    float   dist    = Mathf.Abs(surface.Plane.Distance(localPosition));

                    if(dist > MathConstants.DistanceEpsilon)
                    {
                        continue;
                    }

                    float alignment = Mathf.Abs(Vector3.Dot(surface.Plane.normal, localDirection));

                    if(alignment < 1 - MathConstants.NormalEpsilon)
                    {
                        continue;
                    }

                    int texGenIndex = surface.TexGenIndex;
                    planeTexgen = texgens[texGenIndex];

                    break;
                }
            }
        }

        private Vector2 Centroid(Vector2[] vertices)
        {
            Vector2 centroid   = Vector2.zero;
            float   signedArea = 0.0f;

            // For all vertices except last
            for(int j = vertices.Length - 1, i = 0; i < vertices.Length; j = i, ++i)
            {
                Vector2 currentVertex = vertices[j];
                Vector2 nextVertex    = vertices[i];

                float partialSignedArea = currentVertex.x * nextVertex.y - nextVertex.x * currentVertex.y;

                signedArea += partialSignedArea;
                centroid.x += (currentVertex.x + nextVertex.x) * partialSignedArea;
                centroid.y += (currentVertex.y + nextVertex.y) * partialSignedArea;
            }

            signedArea *= 0.5f;
            centroid.x /= 6.0f * signedArea;
            centroid.y /= 6.0f * signedArea;

            return centroid;
        }

        public Vector3 GetCenter(CSGPlane plane)
        {
            Vector2[] vertices2d = GeometryUtility.RotatePlaneTo2D(curve.points, plane);
            Vector2   centroid2d = Centroid(vertices2d);

            return plane.Project(GeometryUtility.Rotate2DToPlane(new[] { centroid2d }, plane)[0]);
        }

        public AABB CalculateBounds(Quaternion rotation, Vector3 gridTangent, Vector3 gridBinormal)
        {
            AABB bounds = new AABB();
            bounds.Reset();
            Vector3 min = new Vector3(float.PositiveInfinity, float.PositiveInfinity, float.PositiveInfinity);
            Vector3 max = new Vector3(float.NegativeInfinity, float.NegativeInfinity, float.NegativeInfinity);

            for(int i = 0; i < VertexLength; i++)
            {
                Vector3 pos = rotation * curve.points[i];

                min.x = Mathf.Min(min.x, pos.x);
                min.y = Mathf.Min(min.y, pos.y);
                min.z = Mathf.Min(min.z, pos.z);

                max.x = Mathf.Max(max.x, pos.x);
                max.y = Mathf.Max(max.y, pos.y);
                max.z = Mathf.Max(max.z, pos.z);
            }

            bounds.Min = min;
            bounds.Max = max;

            return bounds;
        }

        public void SetPointConstraint(CSGPlane buildPlane, int pointIndex, HandleConstraints state)
        {
            SetPointConstaintSide(buildPlane, pointIndex, 0, state);
            SetPointConstaintSide(buildPlane, pointIndex, 1, state);
        }

        public void SetPointConstaintSide(CSGPlane buildPlane, int pointIndex, int side, HandleConstraints state)
        {
            if(curveTangentHandles[pointIndex * 2 + side].constraint == state)
            {
                return;
            }

            curveTangentHandles[pointIndex * 2 + side].constraint = state;

            if(state != HandleConstraints.Straight && curveTangentHandles[pointIndex * 2 + (1 - side)].constraint != HandleConstraints.Straight)
            {
                if(state == HandleConstraints.Broken && curveTangentHandles[pointIndex * 2 + (1 - side)].constraint == HandleConstraints.Mirrored)
                {
                    curveTangentHandles[pointIndex * 2 + (1 - side)].constraint = HandleConstraints.Broken;
                }

                curvePointHandles[pointIndex].tangent         = true;
                curve.tangents[pointIndex * 2 + side].tangent = -curve.tangents[pointIndex * 2 + (1 - side)].tangent;

                return;
            }

            switch(state)
            {
                case HandleConstraints.Broken:
                case HandleConstraints.Mirrored:
                {
                    if(curvePointHandles[pointIndex].tangent)
                    {
                        break;
                    }

                    int count = curve.points.Length;
                    int prev  = (pointIndex + count - 1) % count;
                    int next  = (pointIndex + count + 1) % count;

                    Vector3 vertex0 = curve.points[prev];
                    Vector3 vertex1 = curve.points[pointIndex];
                    Vector3 vertex2 = curve.points[next];

                    Vector3 centerA  = (vertex0 + vertex1 + vertex2) / 3;
                    Vector3 deltaA   = centerA - vertex1;
                    Vector3 tangentA = Vector3.Cross(buildPlane.normal, deltaA);

                    if(side == 0)
                    {
                        tangentA = -tangentA;
                    }

                    curvePointHandles[pointIndex].tangent         = true;
                    curve.tangents[pointIndex * 2 + side].tangent = tangentA;

                    break;
                }
            }
        }

        private static Vector3 PointOnBezier(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t)
        {
            return (1 - t) * (1 - t) * (1 - t) * p0 + 3 * t * (1 - t) * (1 - t) * p1 + 3 * t * t * (1 - t) * p2 + t * t * t * p3;
        }

        private Vector3[] CurvedEdges(uint curveSides, out int[][] curvedEdges)
        {
            List<Vector3>    newPoints = new List<Vector3>();
            Vector3[]        points    = curve.points;
            TangentCurve2D[] tangents  = curve.tangents;
            int              length    = points.Length;
            curvedEdges = new int[length][];

            for(int i = 0; i < points.Length; i++)
            {
                int     index1        = i;
                Vector3 p1            = points[index1];
                int     index2        = (i + 1) % points.Length;
                Vector3 p2            = points[index2];
                int     tangentIndex1 = index1 * 2 + 1;
                int     tangentIndex2 = index2 * 2 + 0;

                if(curveSides == 0 || tangentIndex1 >= curveTangentHandles.Length || (curveTangentHandles[tangentIndex1].constraint == HandleConstraints.Straight && curveTangentHandles[tangentIndex2].constraint == HandleConstraints.Straight))
                {
                    curvedEdges[i] = new[] { newPoints.Count, newPoints.Count + 1 };
                    newPoints.Add(p1);

                    continue;
                }

                Vector3 p0, p3;

                if(curveTangentHandles[tangentIndex1].constraint != HandleConstraints.Straight)
                {
                    p0 = p1 - tangents[tangentIndex1].tangent;
                }
                else
                {
                    p0 = p1;
                }

                if(curveTangentHandles[tangentIndex2].constraint != HandleConstraints.Straight)
                {
                    p3 = p2 - tangents[tangentIndex2].tangent;
                }
                else
                {
                    p3 = p2;
                }

                int firstIndex = newPoints.Count;
                newPoints.Add(p1);

                for(int n = 1; n < curveSides; n++)
                {
                    newPoints.Add(PointOnBezier(p1, p0, p3, p2, n / (float)curveSides));
                }

                int pointCount = newPoints.Count - firstIndex + 1;
                curvedEdges[i] = new int[pointCount];

                for(int j = 0; j < pointCount; j++)
                {
                    curvedEdges[i][j] = j + firstIndex;
                }
            }

            int[] lastIndices = curvedEdges[curvedEdges.Length - 1];

            if(lastIndices.Length > 0)
            {
                lastIndices[lastIndices.Length - 1] = 0;
            }

            return newPoints.ToArray();
        }

        public Vector3[] GetCurvedVertices(CSGPlane buildPlane, uint curveSides, out int[][] curvedEdges)
        {
            if(curve.points.Length < 3)
            {
                curvedEdges = null;

                return GetVertices();
            }

            Vector3[] vertices3d = CurvedEdges(curveSides, out curvedEdges);
            Vector2[] vertices2d = GeometryUtility.RotatePlaneTo2D(vertices3d, buildPlane);

            if((vertices2d[0] - vertices2d[vertices2d.Length - 1]).sqrMagnitude < MathConstants.EqualityEpsilonSqr)
            {
                if(vertices2d.Length == 3)
                {
                    curvedEdges = null;

                    return GetVertices();
                }
            }

            return GeometryUtility.Rotate2DToPlane(vertices2d, buildPlane);
        }

        public Vector3[] GetCurvedVertices(CSGPlane buildPlane, uint curveSides)
        {
            return GetCurvedVertices(buildPlane, curveSides, out int[][] _);
        }

        public void UpdateSmoothingGroups(HashSet<uint> usedSmoothingGroupIndices)
        {
            for(int i = 0; i < curveEdgeHandles.Length; i++)
            {
                curveEdgeHandles[i].texgen.SmoothingGroup = 0;
            }

            for(int i = 0; i < curveEdgeHandles.Length; i++)
            {
                if(curveEdgeHandles[i].texgen.SmoothingGroup != 0)
                {
                    continue;
                }

                uint smoothingGroup = SurfaceUtility.FindUnusedSmoothingGroupIndex(usedSmoothingGroupIndices);
                usedSmoothingGroupIndices.Add(smoothingGroup);
                curveEdgeHandles[i].texgen.SmoothingGroup = smoothingGroup;

                if((i == 0 && curveTangentHandles[i * 2 + 0].constraint == HandleConstraints.Mirrored) || curveTangentHandles[i * 2 + 1].constraint == HandleConstraints.Mirrored)
                {
                    int last = curveEdgeHandles.Length - 1;
                    curveEdgeHandles[last].texgen.SmoothingGroup = smoothingGroup;
                }

                if(i < curveEdgeHandles.Length - 1 && i < curve.points.Length - 1 && (curveTangentHandles[(i + 1) * 2 + 0].constraint == HandleConstraints.Mirrored || curveTangentHandles[(i + 1) * 2 + 1].constraint == HandleConstraints.Mirrored))
                {
                    curveEdgeHandles[i + 1].texgen.SmoothingGroup = smoothingGroup;
                }
            }
        }
    }
}
