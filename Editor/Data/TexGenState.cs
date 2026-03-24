using System;
using UnityEngine;
using UnityEditor;
using RealtimeCSG.Legacy;
using RealtimeCSG.Components;

namespace RealtimeCSG
{
    [Serializable]
    internal sealed class TexGenState
    {
        [SerializeField] public int[] surfaceControlID;
        [SerializeField] public SelectState[] surfaceSelectState;

        [NonSerialized] public TexGen[] backupTexGens;
        [NonSerialized] public Vector3 rayIntersectionWorldPoint;
        [NonSerialized] public Vector2 rayIntersectionSurfacePoint;

        public TexGenState(CSGBrush brush)
        {
            Shape shape = brush.Shape;
            AllocateSurfaces(shape.Surfaces.Length);
        }

        public void UpdateTexGenState(CSGBrush brush)
        {
            Shape shape = brush.Shape;

            if(surfaceSelectState == null || surfaceControlID == null)
            {
                AllocateSurfaces(shape.Surfaces.Length);

                return;
            }

            while(shape.Surfaces.Length < surfaceSelectState.Length)
            {
                ArrayUtility.RemoveAt(ref surfaceSelectState, surfaceSelectState.Length - 1);
            }

            while(shape.Surfaces.Length < surfaceControlID.Length)
            {
                ArrayUtility.RemoveAt(ref surfaceControlID, surfaceControlID.Length - 1);
            }

            while(shape.Surfaces.Length > surfaceSelectState.Length)
            {
                ArrayUtility.Add(ref surfaceSelectState, SelectState.None);
            }

            while(shape.Surfaces.Length > surfaceControlID.Length)
            {
                ArrayUtility.Add(ref surfaceControlID, -1);
            }
        }

        private void AllocateSurfaces(int surfaceCount)
        {
            surfaceControlID   = new int[surfaceCount];
            surfaceSelectState = new SelectState[surfaceCount];
        }

        public void UpdateLayout(CSGBrush brush)
        {
            Shape shape        = brush.Shape;
            int   surfaceCount = shape.Surfaces.Length;

            if(surfaceControlID.Length != surfaceCount)
            {
                AllocateSurfaces(surfaceCount);
            }
        }

        public bool HaveSurfaceSelection
        {
            get
            {
                for(int p = 0; p < surfaceSelectState.Length; p++)
                {
                    if((surfaceSelectState[p] & SelectState.Selected) == SelectState.Selected)
                    {
                        return true;
                    }
                }

                return false;
            }
        }

        public void DeselectAll()
        {
            for(int p = 0; p < surfaceSelectState.Length; p++)
            {
                surfaceSelectState[p] &= ~SelectState.Selected;
            }
        }

        public void SelectAll()
        {
            for(int p = 0; p < surfaceSelectState.Length; p++)
            {
                surfaceSelectState[p] |= SelectState.Selected;
            }
        }

        public void UnHoverAll()
        {
            for(int p = 0; p < surfaceSelectState.Length; p++)
            {
                surfaceSelectState[p] &= ~SelectState.Hovering;
            }
        }

        public bool IsSurfaceSelected(int surfaceIndex)
        {
            SelectState newState = surfaceSelectState[surfaceIndex];

            return (newState & SelectState.Selected) == SelectState.Selected;
        }

        public bool SelectSurface(int surfaceIndex, SelectionType selectionType)
        {
            SelectState oldState = surfaceSelectState[surfaceIndex];

            if((oldState & SelectState.Hovering) != SelectState.Hovering)
            {
                return false;
            }

            SelectState newState = oldState;

            switch(selectionType)
            {
                case SelectionType.Subtractive:
                    newState &= ~SelectState.Selected;

                    break;
                case SelectionType.Toggle:
                    newState ^= SelectState.Selected;

                    break;
                case SelectionType.Replace:
                case SelectionType.Additive:
                default:
                    newState |= SelectState.Selected;

                    break;
            }

            if(oldState == newState)
            {
                return false;
            }

            surfaceSelectState[surfaceIndex] = newState;

            return true;
        }
    };
}
