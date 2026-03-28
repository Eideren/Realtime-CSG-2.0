using System.Collections.Generic;
using InternalRealtimeCSG;
using RealtimeCSG.Components;
using RealtimeCSG.Legacy;
using UnityEngine;

namespace RealtimeCSG
{
    internal enum CubeSide
    {
        NegativeX
      , NegativeY
      , NegativeZ
      , PositiveX
      , PositiveY
      , PositiveZ
    }

    internal sealed class BoundsUtilities
    {
        public static bool HasFrameBounds(FilteredSelection filteredSelection)
        {
            return filteredSelection.nodeTargets.Length > 0;
        }

        public static Bounds OnGetFrameBounds(FilteredSelection filteredSelection)
        {
            HashSet<CSGBrush> brushes = filteredSelection.GetAllContainedBrushes();
            Bounds            bounds  = new Bounds();

            if(brushes.Count == 0)
            {
                foreach(CSGNode node in filteredSelection.nodeTargets)
                {
                    bounds.Encapsulate(node.transform.position);
                }
            }
            else
            {
                foreach(CSGBrush brush in brushes)
                {
                    Transform   transform   = brush.GetComponent<Transform>();
                    ControlMesh controlMesh = brush.controlMesh;
                    bounds.center = transform.TransformPoint(controlMesh.vertices[0]);
                    bounds.size   = MathConstants.zeroVector3;

                    for(int i = 1; i < controlMesh.vertices.Length; i++)
                    {
                        bounds.Encapsulate(transform.TransformPoint(controlMesh.vertices[i]));
                    }
                }
            }

            return bounds;
        }

        public static AABB GetBounds(CSGBrush[] brushes)
        {
            Vector3 min = MathConstants.positiveInfinityVector3;
            Vector3 max = MathConstants.negativeInfinityVector3;

            for(int b = 0; b < brushes.Length; b++)
            {
                if(!brushes[b])
                {
                    continue;
                }

                ControlMesh controlMesh = brushes[b].controlMesh;

                if(controlMesh == null || controlMesh.vertices == null)
                {
                    continue;
                }

                Matrix4x4 matrix = brushes[b].transform.localToWorldMatrix;

                for(int p = 0; p < controlMesh.vertices.Length; p++)
                {
                    Vector3 point = matrix.MultiplyPoint(controlMesh.vertices[p]);
                    min.x = Mathf.Min(min.x, point.x);
                    min.y = Mathf.Min(min.y, point.y);
                    min.z = Mathf.Min(min.z, point.z);

                    max.x = Mathf.Max(max.x, point.x);
                    max.y = Mathf.Max(max.y, point.y);
                    max.z = Mathf.Max(max.z, point.z);
                }
            }

            AABB bounds = new AABB();
            bounds.MinX = min.x;
            bounds.MinY = min.y;
            bounds.MinZ = min.z;
            bounds.MaxX = max.x;
            bounds.MaxY = max.y;
            bounds.MaxZ = max.z;

            return bounds;
        }

        public static AABB GetBounds(Vector3[] points)
        {
            Vector3 min = MathConstants.positiveInfinityVector3;
            Vector3 max = MathConstants.negativeInfinityVector3;

            for(int p = 0; p < points.Length; p++)
            {
                Vector3 point = points[p];
                min.x = Mathf.Min(min.x, point.x);
                min.y = Mathf.Min(min.y, point.y);
                min.z = Mathf.Min(min.z, point.z);

                max.x = Mathf.Max(max.x, point.x);
                max.y = Mathf.Max(max.y, point.y);
                max.z = Mathf.Max(max.z, point.z);
            }

            AABB bounds = new AABB();
            bounds.MinX = min.x;
            bounds.MinY = min.y;
            bounds.MinZ = min.z;
            bounds.MaxX = max.x;
            bounds.MaxY = max.y;
            bounds.MaxZ = max.z;

            return bounds;
        }

        public static AABB GetBounds(Vector3[] worldPoints, Quaternion rotation, Vector3 scale, Vector3 center)
        {
            if(worldPoints == null || worldPoints.Length == 0)
            {
                return new AABB();
            }

            Vector3 min = MathConstants.positiveInfinityVector3;
            Vector3 max = MathConstants.negativeInfinityVector3;

            Quaternion inverseRotation = Quaternion.Inverse(rotation);

            Matrix4x4 rotationMatrix        = Matrix4x4.TRS(Vector3.zero, rotation, Vector3.one);
            Matrix4x4 inverseRotationMatrix = Matrix4x4.TRS(Vector3.zero, inverseRotation, Vector3.one);
            Matrix4x4 scaleMatrix           = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, scale);
            Matrix4x4 moveMatrix            = Matrix4x4.TRS(center, Quaternion.identity, Vector3.one);
            Matrix4x4 inverseMoveMatrix     = Matrix4x4.TRS(-center, Quaternion.identity, Vector3.one);

            Matrix4x4 combinedMatrix = moveMatrix * rotationMatrix * scaleMatrix * inverseRotationMatrix * inverseMoveMatrix;

            for(int p = 0; p < worldPoints.Length; p++)
            {
                Vector3 point = inverseRotation * combinedMatrix.MultiplyPoint(worldPoints[p]);

                min.x = Mathf.Min(min.x, point.x);
                min.y = Mathf.Min(min.y, point.y);
                min.z = Mathf.Min(min.z, point.z);

                max.x = Mathf.Max(max.x, point.x);
                max.y = Mathf.Max(max.y, point.y);
                max.z = Mathf.Max(max.z, point.z);
            }

            AABB bounds = new AABB();
            bounds.MinX = min.x;
            bounds.MinY = min.y;
            bounds.MinZ = min.z;
            bounds.MaxX = max.x;
            bounds.MaxY = max.y;
            bounds.MaxZ = max.z;

            return bounds;
        }

        public static Vector3 GetLocalCenter(CSGBrush brush)
        {
            Vector3 min = MathConstants.positiveInfinityVector3;
            Vector3 max = MathConstants.negativeInfinityVector3;

            if(!brush)
            {
                return min;
            }

            ControlMesh controlMesh = brush.controlMesh;

            if(controlMesh == null || controlMesh.vertices == null)
            {
                return min;
            }

            for(int p = 0; p < controlMesh.vertices.Length; p++)
            {
                Vector3 point = controlMesh.vertices[p];
                min.x = Mathf.Min(min.x, point.x);
                min.y = Mathf.Min(min.y, point.y);
                min.z = Mathf.Min(min.z, point.z);

                max.x = Mathf.Max(max.x, point.x);
                max.y = Mathf.Max(max.y, point.y);
                max.z = Mathf.Max(max.z, point.z);
            }

            return new Vector3(min.x * 0.5f + max.x * 0.5f, min.y * 0.5f + max.y * 0.5f, min.z * 0.5f + max.z * 0.5f);
        }

        public static Vector3 GetCenter(CSGBrush[] brushes)
        {
            Vector3 min = MathConstants.positiveInfinityVector3;
            Vector3 max = MathConstants.negativeInfinityVector3;

            for(int b = 0; b < brushes.Length; b++)
            {
                if(!brushes[b])
                {
                    continue;
                }

                ControlMesh controlMesh = brushes[b].controlMesh;

                if(controlMesh == null)
                {
                    continue;
                }

                Matrix4x4 matrix = brushes[b].transform.localToWorldMatrix;

                for(int p = 0; p < controlMesh.vertices.Length; p++)
                {
                    Vector3 point = matrix.MultiplyPoint(controlMesh.vertices[p]);
                    min.x = Mathf.Min(min.x, point.x);
                    min.y = Mathf.Min(min.y, point.y);
                    min.z = Mathf.Min(min.z, point.z);

                    max.x = Mathf.Max(max.x, point.x);
                    max.y = Mathf.Max(max.y, point.y);
                    max.z = Mathf.Max(max.z, point.z);
                }
            }

            return float.IsInfinity(min.x) ? min : new Vector3(min.x * 0.5f + max.x * 0.5f, min.y * 0.5f + max.y * 0.5f, min.z * 0.5f + max.z * 0.5f);
        }

        public static AABB GetLocalBounds(CSGBrush[] brushes, Matrix4x4 worldToLocal)
        {
            Vector3 min = MathConstants.positiveInfinityVector3;
            Vector3 max = MathConstants.negativeInfinityVector3;

            for(int b = 0; b < brushes.Length; b++)
            {
                ControlMesh controlMesh = brushes[b].controlMesh;

                if(controlMesh == null || controlMesh.vertices == null)
                {
                    continue;
                }

                Matrix4x4 brushToGridspace = worldToLocal * brushes[b].transform.localToWorldMatrix;

                for(int p = 0; p < controlMesh.vertices.Length; p++)
                {
                    Vector3 point = brushToGridspace.MultiplyPoint(controlMesh.vertices[p]);
                    min.x = Mathf.Min(min.x, point.x);
                    min.y = Mathf.Min(min.y, point.y);
                    min.z = Mathf.Min(min.z, point.z);

                    max.x = Mathf.Max(max.x, point.x);
                    max.y = Mathf.Max(max.y, point.y);
                    max.z = Mathf.Max(max.z, point.z);
                }
            }

            AABB bounds = new AABB();
            bounds.MinX = min.x;
            bounds.MinY = min.y;
            bounds.MinZ = min.z;
            bounds.MaxX = max.x;
            bounds.MaxY = max.y;
            bounds.MaxZ = max.z;

            return bounds;
        }

        public static void GetBoundsCornerPoints(AABB inputBounds, Vector3[] outputPoints)
        {
            if(outputPoints.Length != 8)
            {
                return;
            }

            outputPoints[0] = new Vector3(inputBounds.MinX, inputBounds.MinY, inputBounds.MinZ);
            outputPoints[1] = new Vector3(inputBounds.MaxX, inputBounds.MinY, inputBounds.MinZ);
            outputPoints[2] = new Vector3(inputBounds.MaxX, inputBounds.MinY, inputBounds.MaxZ);
            outputPoints[3] = new Vector3(inputBounds.MinX, inputBounds.MinY, inputBounds.MaxZ);

            outputPoints[4] = new Vector3(inputBounds.MinX, inputBounds.MaxY, inputBounds.MinZ);
            outputPoints[5] = new Vector3(inputBounds.MaxX, inputBounds.MaxY, inputBounds.MinZ);
            outputPoints[6] = new Vector3(inputBounds.MaxX, inputBounds.MaxY, inputBounds.MaxZ);
            outputPoints[7] = new Vector3(inputBounds.MinX, inputBounds.MaxY, inputBounds.MaxZ);
        }

        public static void GetBoundsSidePoints(AABB inputBounds, Vector3[] outputPoints)
        {
            if(outputPoints.Length != 6)
            {
                return;
            }

            Vector3 center = inputBounds.Center;

            outputPoints[0] = new Vector3(inputBounds.MinX, center.y, center.z);
            outputPoints[1] = new Vector3(inputBounds.MaxX, center.y, center.z);
            outputPoints[2] = new Vector3(center.x, inputBounds.MinY, center.z);
            outputPoints[3] = new Vector3(center.x, inputBounds.MaxY, center.z);
            outputPoints[4] = new Vector3(center.x, center.y, inputBounds.MinZ);
            outputPoints[5] = new Vector3(center.x, center.y, inputBounds.MaxZ);
        }

        public static Vector3 GetBoundsSidePoint(AABB inputBounds, int index)
        {
            Vector3 center = inputBounds.Center;

            return index switch
            {
                0 => new Vector3(inputBounds.MinX, center.y, center.z)
              , 1 => new Vector3(inputBounds.MaxX, center.y, center.z)
              , 2 => new Vector3(center.x, inputBounds.MinY, center.z)
              , 3 => new Vector3(center.x, inputBounds.MaxY, center.z)
              , 4 => new Vector3(center.x, center.y, inputBounds.MinZ)
              , 5 => new Vector3(center.x, center.y, inputBounds.MaxZ)
              , _ => Vector3.zero
            };
        }

        public static void GetBoundsEdgePoints(AABB inputBounds, Vector3[] outputPoints)
        {
            if(outputPoints.Length != 12)
            {
                return;
            }

            Vector3 center = inputBounds.Center;

            outputPoints[0] = new Vector3(center.x, inputBounds.MinY, inputBounds.MinZ);
            outputPoints[1] = new Vector3(inputBounds.MaxX, inputBounds.MinY, center.z);
            outputPoints[2] = new Vector3(center.x, inputBounds.MinY, inputBounds.MaxZ);
            outputPoints[3] = new Vector3(inputBounds.MinX, inputBounds.MinY, center.z);

            outputPoints[4] = new Vector3(center.x, inputBounds.MaxY, inputBounds.MinZ);
            outputPoints[5] = new Vector3(inputBounds.MaxX, inputBounds.MaxY, center.z);
            outputPoints[6] = new Vector3(center.x, inputBounds.MaxY, inputBounds.MaxZ);
            outputPoints[7] = new Vector3(inputBounds.MinX, inputBounds.MaxY, center.z);

            outputPoints[8]  = new Vector3(inputBounds.MinX, center.y, inputBounds.MinZ);
            outputPoints[9]  = new Vector3(inputBounds.MaxX, center.y, inputBounds.MinZ);
            outputPoints[10] = new Vector3(inputBounds.MaxX, center.y, inputBounds.MaxZ);
            outputPoints[11] = new Vector3(inputBounds.MinX, center.y, inputBounds.MaxZ);
        }

        public static Vector3 GetBoundsEdgePoint(AABB inputBounds, int index)
        {
            Vector3 center = inputBounds.Center;

            return index switch
            {
                0  => new Vector3(center.x, inputBounds.MinY, inputBounds.MinZ)
              , 1  => new Vector3(inputBounds.MaxX, inputBounds.MinY, center.z)
              , 2  => new Vector3(center.x, inputBounds.MinY, inputBounds.MaxZ)
              , 3  => new Vector3(inputBounds.MinX, inputBounds.MinY, center.z)
              , 4  => new Vector3(center.x, inputBounds.MaxY, inputBounds.MinZ)
              , 5  => new Vector3(inputBounds.MaxX, inputBounds.MaxY, center.z)
              , 6  => new Vector3(center.x, inputBounds.MaxY, inputBounds.MaxZ)
              , 7  => new Vector3(inputBounds.MinX, inputBounds.MaxY, center.z)
              , 8  => new Vector3(inputBounds.MinX, center.y, inputBounds.MinZ)
              , 9  => new Vector3(inputBounds.MaxX, center.y, inputBounds.MinZ)
              , 10 => new Vector3(inputBounds.MaxX, center.y, inputBounds.MaxZ)
              , 11 => new Vector3(inputBounds.MinX, center.y, inputBounds.MaxZ)
              , _  => Vector3.zero
            };
        }

        // useful for drawing lines
        public static readonly int[] aabbLineIndices = { 0, 1, 1, 2, 2, 3, 3, 0, 4, 5, 5, 6, 6, 7, 7, 4, 0, 4, 1, 5, 2, 6, 3, 7 };

        public static readonly int[][] aabbEdgeIndices =
        {
            new[] { 0, 1 }, //  0  {-X -Y -Z} {+X -Y -Z}
            new[] { 1, 2 }  //  1  {+X -Y -Z} {+X -Y +Z}
          , new[] { 3, 2 }  //  2  {-X -Y +Z} {+X -Y +Z}
          , new[] { 0, 3 }  //  3  {-X -Y -Z} {-X -Y +Z}
          , new[] { 4, 5 }  //  4  {-X +Y -Z} {+X +Y -Z}
          , new[] { 5, 6 }  //  5  {+X +Y -Z} {+X +Y +Z}
          , new[] { 7, 6 }  //  6  {-X +Y +Z} {+X +Y +Z}
          , new[] { 4, 7 }  //  7  {-X +Y -Z} {-X +Y +Z}
          , new[] { 0, 4 }  //  8  {-X -Y -Z} {-X +Y -Z}
          , new[] { 1, 5 }  //  9  {+X -Y -Z} {+X +Y -Z}
          , new[] { 2, 6 }  // 10  {+X -Y +Z} {+X +Y +Z}
          , new[] { 3, 7 }  // 11  {-X -Y +Z} {-X +Y +Z}
        };

        public static readonly int[] aabbTopIndices = { 0, 1, 2, 3 };

        public static readonly int[][] aabbSidePointIndices =
        {
            new[] { 0, 3, 7, 4 }, // 0
            new[] { 5, 6, 2, 1 }  // 1
          , new[] { 0, 1, 2, 3 }  // 2
          , new[] { 7, 6, 5, 4 }  // 3
          , new[] { 4, 5, 1, 0 }  // 4
          , new[] { 3, 2, 6, 7 }  // 5
        };

        public static readonly int[] aabbyAxi = { 8, 9, 10, 11 };
        public static readonly int[] aabbxAxi = { 0, 2, 4, 6 };
        public static readonly int[] aabbzAxi = { 1, 3, 5, 7 };

        public static readonly PrincipleAxis[][] aabbEdgeAxis =
        {
            new[] { PrincipleAxis.Y, PrincipleAxis.Z }, // 0,1
            new[] { PrincipleAxis.X, PrincipleAxis.Y }  // 1,2
          , new[] { PrincipleAxis.Y, PrincipleAxis.Z }  // 2,3
          , new[] { PrincipleAxis.X, PrincipleAxis.Y }  // 3,0
          , new[] { PrincipleAxis.Y, PrincipleAxis.Z }  // 4,5
          , new[] { PrincipleAxis.X, PrincipleAxis.Y }  // 5,6
          , new[] { PrincipleAxis.Y, PrincipleAxis.Z }  // 6,7
          , new[] { PrincipleAxis.X, PrincipleAxis.Y }  // 7,4
          , new[] { PrincipleAxis.X, PrincipleAxis.Z }  // 0,4
          , new[] { PrincipleAxis.X, PrincipleAxis.Z }  // 1,5
          , new[] { PrincipleAxis.X, PrincipleAxis.Z }  // 2,6
          , new[] { PrincipleAxis.X, PrincipleAxis.Z }  // 3,7
        };

        public static readonly PrincipleAxis[] aabbEdgeTangentAxis =
        {
            PrincipleAxis.X, // 0,1
            PrincipleAxis.Z  // 1,2
          , PrincipleAxis.X  // 2,3
          , PrincipleAxis.Z  // 3,0
          , PrincipleAxis.X  // 4,5
          , PrincipleAxis.Z  // 5,6
          , PrincipleAxis.X  // 6,7
          , PrincipleAxis.Z  // 7,4
          , PrincipleAxis.Y  // 0,4
          , PrincipleAxis.Y  // 1,5
          , PrincipleAxis.Y  // 2,6
          , PrincipleAxis.Y  // 3,7
        };

        public static readonly Vector3[] aabbEdgeTangents =
        {
            new Vector3(1, 0, 0), // 0,1
            new Vector3(0, 0, 1)  // 1,2
          , new Vector3(1, 0, 0)  // 2,3
          , new Vector3(0, 0, 1)  // 3,0
          , new Vector3(1, 0, 0)  // 4,5
          , new Vector3(0, 0, 1)  // 5,6
          , new Vector3(1, 0, 0)  // 6,7
          , new Vector3(0, 0, 1)  // 7,4
          , new Vector3(0, 1, 0)  // 0,4
          , new Vector3(0, 1, 0)  // 1,5
          , new Vector3(0, 1, 0)  // 2,6
          , new Vector3(0, 1, 0)  // 3,7
        };

        public static readonly CubeSide[][] aabbEdgeCubeSides =
        {
            // sides  normal1 normal2
            new[] { CubeSide.NegativeZ, CubeSide.NegativeY }, // 4,2 ( 0, 0,-1) ( 0,-1, 0)
            new[] { CubeSide.PositiveX, CubeSide.NegativeY }  // 1,2 ( 1, 0, 0) ( 0,-1, 0)
          , new[] { CubeSide.PositiveZ, CubeSide.NegativeY }  // 5,2 ( 0, 0, 1) ( 0,-1, 0)
          , new[] { CubeSide.NegativeX, CubeSide.NegativeY }  // 0,2 (-1, 0, 0) ( 0,-1, 0)
          , new[] { CubeSide.NegativeZ, CubeSide.PositiveY }  // 4,3 ( 0, 0,-1) ( 0, 1, 0)
          , new[] { CubeSide.PositiveX, CubeSide.PositiveY }  // 1,3 ( 1, 0, 0) ( 0, 1, 0)
          , new[] { CubeSide.PositiveZ, CubeSide.PositiveY }  // 5,3 ( 0, 0, 1) ( 0, 1, 0)
          , new[] { CubeSide.NegativeX, CubeSide.PositiveY }  // 0,3 (-1, 0, 0) ( 0, 1, 0)
          , new[] { CubeSide.NegativeX, CubeSide.NegativeZ }  // 0,4 (-1, 0, 0) ( 0, 0,-1)
          , new[] { CubeSide.PositiveX, CubeSide.NegativeZ }  // 1,4 ( 1, 0, 0) ( 0, 0,-1)
          , new[] { CubeSide.PositiveX, CubeSide.PositiveZ }  // 1,5 ( 1, 0, 0) ( 0, 0, 1)
          , new[] { CubeSide.NegativeX, CubeSide.PositiveZ }  // 0,5 (-1, 0, 0) ( 0, 0, 1)
        };

        // for each side returns the two side - indices
        public static readonly int[][] aabbEdgeSides = { new[] { 4, 2 }, new[] { 1, 2 }, new[] { 5, 2 }, new[] { 0, 2 }, new[] { 4, 3 }, new[] { 1, 3 }, new[] { 5, 3 }, new[] { 0, 3 }, new[] { 0, 4 }, new[] { 1, 4 }, new[] { 1, 5 }, new[] { 0, 5 } };

        public static readonly PrincipleAxis[] aabbSideAxis = { PrincipleAxis.X, PrincipleAxis.X, PrincipleAxis.Y, PrincipleAxis.Y, PrincipleAxis.Z, PrincipleAxis.Z };

        public static readonly Vector3[] aabbSideNormal =
        {
            new Vector3(-1, 0, 0), // 0
            new Vector3(1, 0, 0)   // 1
          , new Vector3(0, -1, 0)  // 2
          , new Vector3(0, 1, 0)   // 3
          , new Vector3(0, 0, -1)  // 4
          , new Vector3(0, 0, 1)   // 5
        };
    }
}
