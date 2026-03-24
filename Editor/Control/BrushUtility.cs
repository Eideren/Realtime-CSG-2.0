using System;
using UnityEngine;
using RealtimeCSG;
using RealtimeCSG.Components;
using GeometryUtility = RealtimeCSG.GeometryUtility;

namespace InternalRealtimeCSG
{
    [Serializable]
    internal enum HandleConstraints
    {
        Straight // Tangents are ignored (used for straight lines)
      , Broken   // Both tangents are assumed to go in different directions
      , Mirrored // Both tangents are aligned and mirror each other
    }

    [Serializable]
    internal struct TangentCurve2D
    {
        public Vector3 tangent;
        public HandleConstraints constraint;
    }

    [Serializable]
    internal class Curve2D
    {
        public Vector3[] points;
        public TangentCurve2D[] tangents;
    }

    public static class BrushUtility
    {
        public static void SetPivotToLocalCenter(CSGBrush brush)
        {
            if(!brush)
            {
                return;
            }

            Vector3 localCenter = BoundsUtilities.GetLocalCenter(brush);
            Vector3 worldCenter = brush.transform.localToWorldMatrix.MultiplyPoint(localCenter);

            SetPivot(brush, worldCenter);
        }

        public static void SetPivot(CSGBrush brush, Vector3 newCenter)
        {
            if(!brush)
            {
                return;
            }

            Transform transform  = brush.transform;
            Vector3   realCenter = transform.position;
            Vector3   difference = newCenter - realCenter;

            if(difference.magnitude < MathConstants.ConsideredZero)
            {
                return;
            }

            transform.position += difference;

            GeometryUtility.MoveControlMeshVertices(brush, -difference);
            SurfaceUtility.TranslateSurfacesInWorldSpace(brush, -difference);
            ControlMeshUtility.RebuildShape(brush);
        }

        public static void TranslatePivot(CSGBrush[] brushes, Vector3 offset)
        {
            if(brushes == null || brushes.Length == 0 || offset.magnitude < MathConstants.ConsideredZero)
            {
                return;
            }

            for(int i = 0; i < brushes.Length; i++)
            {
                brushes[i].transform.position += offset;
            }

            GeometryUtility.MoveControlMeshVertices(brushes, -offset);
            SurfaceUtility.TranslateSurfacesInWorldSpace(brushes, -offset);
            ControlMeshUtility.RebuildShapes(brushes);
        }
    }
}
