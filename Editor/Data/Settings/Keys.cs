using UnityEditor;
using UnityEngine;
using RealtimeCSG.Foundation;

namespace RealtimeCSG
{
    internal static class Keys
    {
        [KeyDescription("Generate/Free draw")]
        public static readonly KeyEvent freeBuilderMode = new KeyEvent(KeyCode.N, true);

        [KeyDescription("Generate/Cylinder")]
        public static readonly KeyEvent cylinderBuilderMode = new KeyEvent(KeyCode.C, true);

        [KeyDescription("Generate/Box")]
        public static readonly KeyEvent boxBuilderMode = new KeyEvent(KeyCode.B, true);

        [KeyDescription("Generate/Sphere")]
        public static readonly KeyEvent sphereBuilderMode = new KeyEvent(KeyCode.K, true);

        [KeyDescription("Generate/Linear Stairs")]
        public static readonly KeyEvent linearStairsBuilderMode = new KeyEvent(KeyCode.L, true);

        [KeyDescription("Generate/Spiral Stairs")]
        public static readonly KeyEvent spiralStairsBuilderMode = new KeyEvent(KeyCode.P, true);

        [KeyDescription("Tools/Switch to Object mode")]
        public static readonly KeyEvent switchToObjectEditMode = new KeyEvent(KeyCode.F1, EventModifiers.Alt);

        [KeyDescription("Tools/Switch to Generate mode")]
        public static readonly KeyEvent switchToGenerateEditMode = new KeyEvent(KeyCode.F2, EventModifiers.Alt);

        [KeyDescription("Tools/Switch to Mesh mode")]
        public static readonly KeyEvent switchToMeshEditMode = new KeyEvent(KeyCode.F3, EventModifiers.Alt);

        [KeyDescription("Tools/Switch to Clip mode")]
        public static readonly KeyEvent switchToClipEditMode = new KeyEvent(KeyCode.F4, EventModifiers.Alt);

        [KeyDescription("Tools/Switch to Surface mode")]
        public static readonly KeyEvent switchToSurfaceEditMode = new KeyEvent(KeyCode.F5, EventModifiers.Alt);

        [KeyDescription("Object mode/Center pivot")]
        public static readonly KeyEvent centerPivot = new KeyEvent(KeyCode.R, EventModifiers.Control);

        [KeyDescription("Mesh mode/Merge edge points")]
        public static readonly KeyEvent mergeEdgePoints = new KeyEvent(KeyCode.M);

        [KeyDescription("Surface mode/Smear or copy material")]
        public static readonly KeyEvent copyMaterialTexGen = new KeyEvent(KeyCode.G, true);

        [KeyDescription("Clip mode/Next clip mode")]
        public static readonly KeyEvent cycleClipModes = new KeyEvent(KeyCode.Tab);

        [KeyDescription("Free draw/Insert point")]
        public static readonly KeyEvent insertPoint = new KeyEvent(KeyCode.I);

        [KeyDescription("Selection/Delete")]
        public static readonly KeyEvent deleteSelectionKey = new KeyEvent(KeyCode.Delete, KeyCode.Backspace);

        [KeyDescription("Selection/Clone Drag")]
        public static readonly KeyEvent cloneDragActivate = new KeyEvent(
            new KeyCodeWithModifier(KeyCode.D, EventModifiers.None, true),
            new KeyCodeWithModifier(KeyCode.D, EventModifiers.Shift, true),
            new KeyCodeWithModifier(KeyCode.D, EventModifiers.Control | EventModifiers.Shift, true)
        );

        [KeyDescription("Selection/SnapToGrid")]
        public static readonly KeyEvent snapToGridKey = new KeyEvent(KeyCode.End, EventModifiers.Control);

        [KeyDescription("Selection/QuickHide")]
        public static readonly KeyEvent quickHideSelectedObjectsKey = new KeyEvent(KeyCode.H, EventModifiers.None);

        [KeyDescription("Selection/QuickUnhide")]
        public static readonly KeyEvent quickHideUnselectedObjectsKey = new KeyEvent(KeyCode.H, EventModifiers.Control);

        [KeyDescription("Selection/ToggleVisibility")]
        public static readonly KeyEvent toggleSelectedObjectVisibilityKey = new KeyEvent(KeyCode.H, EventModifiers.Shift);

        [KeyDescription("Selection/UnhideAll")]
        public static readonly KeyEvent unHideAllObjectsKey = new KeyEvent(KeyCode.U, EventModifiers.None);

        [KeyDescription("Selection/Rotate clockwise")]
        public static readonly KeyEvent rotateSelectionLeft = new KeyEvent(KeyCode.Comma, EventModifiers.Control);

        [KeyDescription("Selection/Rotate anti-clockwise")]
        public static readonly KeyEvent rotateSelectionRight = new KeyEvent(KeyCode.Period, EventModifiers.Control);

        [KeyDescription("Selection/Move left")]
        public static readonly KeyEvent moveSelectionLeft = new KeyEvent(KeyCode.LeftArrow, EventModifiers.Control | EventModifiers.Shift);

        [KeyDescription("Selection/Move right")]
        public static readonly KeyEvent moveSelectionRight = new KeyEvent(KeyCode.RightArrow, EventModifiers.Control | EventModifiers.Shift);

        [KeyDescription("Selection/Move back")]
        public static readonly KeyEvent moveSelectionBack = new KeyEvent(KeyCode.DownArrow, EventModifiers.Control | EventModifiers.Shift);

        [KeyDescription("Selection/Move forward")]
        public static readonly KeyEvent moveSelectionForward = new KeyEvent(KeyCode.UpArrow, EventModifiers.Control | EventModifiers.Shift);

        [KeyDescription("Selection/Move down")]
        public static readonly KeyEvent moveSelectionDown = new KeyEvent(KeyCode.PageDown, EventModifiers.Control | EventModifiers.Shift);

        [KeyDescription("Selection/Move up")]
        public static readonly KeyEvent moveSelectionUp = new KeyEvent(KeyCode.PageUp, EventModifiers.Control | EventModifiers.Shift);

        [KeyDescription("Selection/Flip on X axis")]
        public static readonly KeyEvent flipSelectionX = new KeyEvent(KeyCode.X, EventModifiers.Control | EventModifiers.Shift);

        [KeyDescription("Selection/Flip on Y axis")]
        public static readonly KeyEvent flipSelectionY = new KeyEvent(KeyCode.Y, EventModifiers.Control | EventModifiers.Shift);

        [KeyDescription("Selection/Flip on Z axis")]
        public static readonly KeyEvent flipSelectionZ = new KeyEvent(KeyCode.Z, EventModifiers.Control | EventModifiers.Shift);

        [KeyDescription("Selection/Set as PassThrough")]
        public static readonly KeyEvent makeSelectedPassThroughKey = new KeyEvent(KeyCode.Question);

        [KeyDescription("Selection/Set as Additive")]
        public static readonly KeyEvent makeSelectedAdditiveKey = new KeyEvent(KeyCode.Equals, KeyCode.KeypadPlus);

        [KeyDescription("Selection/Set as Subtractive")]
        public static readonly KeyEvent makeSelectedSubtractiveKey = new KeyEvent(KeyCode.Minus, KeyCode.KeypadMinus);

        [KeyDescription("Selection/Set as Intersecting")]
        public static readonly KeyEvent makeSelectedIntersectingKey = new KeyEvent(KeyCode.Backslash, KeyCode.KeypadDivide);

        [KeyDescription("Grid/Half grid size")]
        public static readonly KeyEvent halfGridSizeKey = new KeyEvent(KeyCode.LeftBracket);

        [KeyDescription("Grid/Double grid size")]
        public static readonly KeyEvent doubleGridSizeKey = new KeyEvent(KeyCode.RightBracket);

        [KeyDescription("Grid/Toggle grid rendering")]
        public static readonly KeyEvent toggleShowGridKey = new KeyEvent(KeyCode.G, EventModifiers.Shift);

        [KeyDescription("Grid/Toggle snapping")]
        public static readonly KeyEvent toggleSnappingKey = new KeyEvent(KeyCode.T, EventModifiers.Shift);

        [KeyDescription("Action/Cancel or Deselect")]
        public static readonly KeyEvent cancelActionKey = new KeyEvent(KeyCode.Escape);

        [KeyDescription("Action/Perform")]
        public static readonly KeyEvent performActionKey = new KeyEvent(KeyCode.Return, KeyCode.KeypadEnter);

        public static bool HandleSceneValidate(IEditMode tool, bool checkForTextEditing = true)
        {
            if(EditorGUIUtility.editingTextField && checkForTextEditing)
            {
                return false;
            }

            if(makeSelectedPassThroughKey.IsKeyPressed() && tool.UsesUnitySelection)
            {
                return true;
            }

            if(makeSelectedAdditiveKey.IsKeyPressed() && tool.UsesUnitySelection)
            {
                return true;
            }

            if(makeSelectedSubtractiveKey.IsKeyPressed() && tool.UsesUnitySelection)
            {
                return true;
            }

            if(makeSelectedIntersectingKey.IsKeyPressed() && tool.UsesUnitySelection)
            {
                return true;
            }

            if(toggleSelectedObjectVisibilityKey.IsKeyPressed() && tool.UsesUnitySelection)
            {
                return true;
            }

            if(quickHideSelectedObjectsKey.IsKeyPressed() && tool.UsesUnitySelection)
            {
                return true;
            }

            if(quickHideUnselectedObjectsKey.IsKeyPressed() && tool.UsesUnitySelection)
            {
                return true;
            }

            if(unHideAllObjectsKey.IsKeyPressed() && tool.UsesUnitySelection)
            {
                return true;
            }

            if(cancelActionKey.IsKeyPressed())
            {
                return true;
            }

            if(halfGridSizeKey.IsKeyPressed())
            {
                return true;
            }

            if(doubleGridSizeKey.IsKeyPressed())
            {
                return true;
            }

            if(toggleShowGridKey.IsKeyPressed())
            {
                return true;
            }

            if(toggleSnappingKey.IsKeyPressed())
            {
                return true;
            }

            return false;
        }

        public static bool HandleSceneKeyDown(IEditMode tool, bool checkForTextEditing = true)
        {
            if(EditorGUIUtility.editingTextField && checkForTextEditing)
            {
                return false;
            }

            if(makeSelectedPassThroughKey.IsKeyPressed() && tool.UsesUnitySelection)
            {
                return true;
            }

            if(makeSelectedAdditiveKey.IsKeyPressed() && tool.UsesUnitySelection)
            {
                return true;
            }

            if(makeSelectedSubtractiveKey.IsKeyPressed() && tool.UsesUnitySelection)
            {
                return true;
            }

            if(makeSelectedIntersectingKey.IsKeyPressed() && tool.UsesUnitySelection)
            {
                return true;
            }

            if(toggleSelectedObjectVisibilityKey.IsKeyPressed() && tool.UsesUnitySelection)
            {
                return true;
            }

            if(quickHideSelectedObjectsKey.IsKeyPressed() && tool.UsesUnitySelection)
            {
                return true;
            }

            if(quickHideUnselectedObjectsKey.IsKeyPressed() && tool.UsesUnitySelection)
            {
                return true;
            }

            if(unHideAllObjectsKey.IsKeyPressed() && tool.UsesUnitySelection)
            {
                return true;
            }

            if(cancelActionKey.IsKeyPressed())
            {
                return true;
            }

            if(halfGridSizeKey.IsKeyPressed())
            {
                return true;
            }

            if(doubleGridSizeKey.IsKeyPressed())
            {
                return true;
            }

            if(toggleShowGridKey.IsKeyPressed())
            {
                return true;
            }

            if(toggleSnappingKey.IsKeyPressed())
            {
                return true;
            }

            return false;
        }

        public static bool HandleSceneKeyUp(IEditMode tool, bool checkForTextEditing = true)
        {
            if(EditorGUIUtility.editingTextField && checkForTextEditing)
            {
                return false;
            }

            if(makeSelectedPassThroughKey.IsKeyPressed() && tool.UsesUnitySelection)
            {
                OperationsUtility.SetPassThroughOnSelected();

                return true;
            }

            if(makeSelectedAdditiveKey.IsKeyPressed() && tool.UsesUnitySelection)
            {
                OperationsUtility.ModifyOperationsOnSelected(CSGOperationType.Additive);

                return true;
            }

            if(makeSelectedSubtractiveKey.IsKeyPressed() && tool.UsesUnitySelection)
            {
                OperationsUtility.ModifyOperationsOnSelected(CSGOperationType.Subtractive);

                return true;
            }

            if(makeSelectedIntersectingKey.IsKeyPressed() && tool.UsesUnitySelection)
            {
                OperationsUtility.ModifyOperationsOnSelected(CSGOperationType.Intersecting);

                return true;
            }

            if(toggleSelectedObjectVisibilityKey.IsKeyPressed() && tool.UsesUnitySelection)
            {
                SelectionUtility.ToggleSelectedObjectVisibility();

                return true;
            }

            if(quickHideSelectedObjectsKey.IsKeyPressed() && tool.UsesUnitySelection)
            {
                SelectionUtility.HideSelectedObjects();

                return true;
            }

            if(quickHideUnselectedObjectsKey.IsKeyPressed() && tool.UsesUnitySelection)
            {
                SelectionUtility.HideUnselectedObjects();

                return true;
            }

            if(unHideAllObjectsKey.IsKeyPressed() && tool.UsesUnitySelection)
            {
                SelectionUtility.UnHideAll();

                return true;
            }

            if(cancelActionKey.IsKeyPressed())
            {
                SelectionUtility.DeselectAll();

                return true;
            }

            if(halfGridSizeKey.IsKeyPressed())
            {
                GridUtility.HalfGridSize();

                return true;
            }

            if(doubleGridSizeKey.IsKeyPressed())
            {
                GridUtility.DoubleGridSize();

                return true;
            }

            if(toggleShowGridKey.IsKeyPressed())
            {
                GridUtility.ToggleShowGrid();

                return true;
            }

            if(toggleSnappingKey.IsKeyPressed())
            {
                GridUtility.ToggleSnapToGrid();

                return true;
            }

            return false;
        }
    }
}
