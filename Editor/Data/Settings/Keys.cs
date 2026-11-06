using UnityEditor;
using UnityEngine;
using RealtimeCSG.Foundation;
using UnityEditor.ShortcutManagement;

namespace RealtimeCSG
{
    internal static class Keys
    {
        private const string shortcutsEnabledTag = "while RCSG is enabled";




        // Specific handling for these two keys, we cannot rebind keys baked into menu item declaration,
        // we'll just provide an additional key user can bind on top of the default one in those cases

        private const string idPopOutToolWindowKey = "RCSG/Tools: Popout Tool Window (besides Ctrl+F2)";

        [KeyDescription(idPopOutToolWindowKey)]
        public static readonly KeyEvent popOutToolWindowKey = new KeyEvent(idPopOutToolWindowKey, false, new KeyCodeWithModifier(KeyCode.F2, EventModifiers.Control));

        [Shortcut("RCSG/ Turn Realtime-CSG on or off (besides Ctrl+F3)", KeyCode.None)]
        private static void ToggleRealtimeCSG(ShortcutArguments args)
        {
            CSGSettings.SetRealtimeCSGEnabled(!CSGSettings.EnableRealtimeCSG);
        }

        [Shortcut(idPopOutToolWindowKey, KeyCode.None)]
        private static void PopoutToolWindow(ShortcutArguments args)
        {
            EditModeToolWindowSceneGUI.GetWindow();
        }




        // Specific handling for these two keys, the shortcut manager does not support binding to escape, return or keypad enter...
        
        private const string idCancelActionKey = "RCSG/Action: Cancel or Deselect (besides Escape)";
        [KeyDescription(idCancelActionKey)]
        public static readonly KeyEvent cancelActionKey = new KeyEvent
        (
            idCancelActionKey, false, new KeyCodeWithModifier(KeyCode.Escape)
        );

        private const string idPerformActionKey = "RCSG/Action: Perform (besides Enter/Return)";
        [KeyDescription(idPerformActionKey)]
        public static readonly KeyEvent performActionKey = new KeyEvent
        (
            idPerformActionKey,
            false,
            new KeyCodeWithModifier(KeyCode.Return),
            new KeyCodeWithModifier(KeyCode.KeypadEnter)
        );

        [Shortcut(idCancelActionKey, typeof(SceneView), shortcutsEnabledTag, KeyCode.None)]
        [Shortcut(idPerformActionKey, typeof(SceneView), shortcutsEnabledTag, KeyCode.None)]
        private static void DummyInputSwallower1(ShortcutArguments args)
        {
            // Shortcuts' function are handled in-context by RCSG instead of globally through static functions
            // This is just a dummy to ensure shortcuts are registered within the manager
        }




        private const string idFreeBuilderMode = "RCSG/Generate: Free draw";
        private const string idCylinderBuilderMode = "RCSG/Generate: Cylinder";
        private const string idBoxBuilderMode = "RCSG/Generate: Box";
        private const string idSphereBuilderMode = "RCSG/Generate: Sphere";
        private const string idLinearStairsBuilderMode = "RCSG/Generate: Linear Stairs";
        private const string idSpiralStairsBuilderMode = "RCSG/Generate: Spiral Stairs";
        private const string idSwitchToObjectEditMode = "RCSG/Tools: Switch to Object mode";
        private const string idSwitchToGenerateEditMode = "RCSG/Tools: Switch to Generate mode";
        private const string idSwitchToMeshEditMode = "RCSG/Tools: Switch to Mesh mode";
        private const string idSwitchToClipEditMode = "RCSG/Tools: Switch to Clip mode";
        private const string idSwitchToSurfaceEditMode = "RCSG/Tools: Switch to Surface mode";
        private const string idCenterPivot = "RCSG/Object: mode/Center pivot";
        private const string idMergeEdgePoints = "RCSG/Mesh: mode/Merge edge points";
        private const string idCopyMaterialTexGen = "RCSG/Surface: mode/Smear or copy material";
        private const string idCycleClipModes = "RCSG/Clip: mode/Next clip mode";
        private const string idInsertPoint = "RCSG/Free: draw/Insert point";
        private const string idDeleteSelectionKey = "RCSG/Selection: Delete";
        private const string idCloneDragActivate = "RCSG/Selection: Clone Drag";
        private const string idSnapToGridKey = "RCSG/Selection: SnapToGrid";
        private const string idQuickHideSelectedObjectsKey = "RCSG/Selection: QuickHide";
        private const string idQuickHideUnselectedObjectsKey = "RCSG/Selection: QuickUnhide";
        private const string idToggleSelectedObjectVisibilityKey = "RCSG/Selection: ToggleVisibility";
        private const string idUnHideAllObjectsKey = "RCSG/Selection: UnhideAll";
        private const string idRotateSelectionLeft = "RCSG/Selection: Rotate clockwise";
        private const string idRotateSelectionRight = "RCSG/Selection: Rotate anti-clockwise";
        private const string idMoveSelectionLeft = "RCSG/Selection: Move left";
        private const string idMoveSelectionRight = "RCSG/Selection: Move right";
        private const string idMoveSelectionBack = "RCSG/Selection: Move back";
        private const string idMoveSelectionForward = "RCSG/Selection: Move forward";
        private const string idMoveSelectionDown = "RCSG/Selection: Move down";
        private const string idMoveSelectionUp = "RCSG/Selection: Move up";
        private const string idFlipSelectionX = "RCSG/Selection: Flip on X axis";
        private const string idFlipSelectionY = "RCSG/Selection: Flip on Y axis";
        private const string idFlipSelectionZ = "RCSG/Selection: Flip on Z axis";
        private const string idMakeSelectedPassThroughKey = "RCSG/Selection: Set as PassThrough";
        private const string idMakeSelectedAdditiveKey = "RCSG/Selection: Set as Additive";
        private const string idMakeSelectedSubtractiveKey = "RCSG/Selection: Set as Subtractive";
        private const string idMakeSelectedIntersectingKey = "RCSG/Selection: Set as Intersecting";
        private const string idHalfGridSizeKey = "RCSG/Grid: Half grid size";
        private const string idDoubleGridSizeKey = "RCSG/Grid: Double grid size";
        private const string idToggleShowGridKey = "RCSG/Grid: Toggle grid rendering";
        private const string idToggleSnappingKey = "RCSG/Grid: Toggle snapping";
        
        [KeyDescription(idFreeBuilderMode)]
        public static readonly KeyEvent freeBuilderMode = new KeyEvent(idFreeBuilderMode, true);

        [KeyDescription(idCylinderBuilderMode)]
        public static readonly KeyEvent cylinderBuilderMode = new KeyEvent(idCylinderBuilderMode, true);

        [KeyDescription(idBoxBuilderMode)]
        public static readonly KeyEvent boxBuilderMode = new KeyEvent(idBoxBuilderMode, true);

        [KeyDescription(idSphereBuilderMode)]
        public static readonly KeyEvent sphereBuilderMode = new KeyEvent(idSphereBuilderMode, true);

        [KeyDescription(idLinearStairsBuilderMode)]
        public static readonly KeyEvent linearStairsBuilderMode = new KeyEvent(idLinearStairsBuilderMode, true);

        /*[KeyDescription(idSpiralStairsBuilderMode)]
        public static readonly KeyEvent spiralStairsBuilderMode = new KeyEvent(idSpiralStairsBuilderMode, true);*/

        [KeyDescription(idSwitchToObjectEditMode)]
        public static readonly KeyEvent switchToObjectEditMode = new KeyEvent(idSwitchToObjectEditMode);

        [KeyDescription(idSwitchToGenerateEditMode)]
        public static readonly KeyEvent switchToGenerateEditMode = new KeyEvent(idSwitchToGenerateEditMode);

        [KeyDescription(idSwitchToMeshEditMode)]
        public static readonly KeyEvent switchToMeshEditMode = new KeyEvent(idSwitchToMeshEditMode);

        [KeyDescription(idSwitchToClipEditMode)]
        public static readonly KeyEvent switchToClipEditMode = new KeyEvent(idSwitchToClipEditMode);

        [KeyDescription(idSwitchToSurfaceEditMode)]
        public static readonly KeyEvent switchToSurfaceEditMode = new KeyEvent(idSwitchToSurfaceEditMode);

        [KeyDescription(idCenterPivot)]
        public static readonly KeyEvent centerPivot = new KeyEvent(idCenterPivot);

        [KeyDescription(idMergeEdgePoints)]
        public static readonly KeyEvent mergeEdgePoints = new KeyEvent(idMergeEdgePoints);

        [KeyDescription(idCopyMaterialTexGen)]
        public static readonly KeyEvent copyMaterialTexGen = new KeyEvent(idCopyMaterialTexGen, true);

        [KeyDescription(idCycleClipModes)]
        public static readonly KeyEvent cycleClipModes = new KeyEvent(idCycleClipModes);

        [KeyDescription(idInsertPoint)]
        public static readonly KeyEvent insertPoint = new KeyEvent(idInsertPoint);

        [KeyDescription(idDeleteSelectionKey)]
        public static readonly KeyEvent deleteSelectionKey = new KeyEvent(idDeleteSelectionKey);

        [KeyDescription(idCloneDragActivate)]
        public static readonly KeyEvent cloneDragActivate = new KeyEvent(idCloneDragActivate, true);

        [KeyDescription(idSnapToGridKey)]
        public static readonly KeyEvent snapToGridKey = new KeyEvent(idSnapToGridKey);

        [KeyDescription(idQuickHideSelectedObjectsKey)]
        public static readonly KeyEvent quickHideSelectedObjectsKey = new KeyEvent(idQuickHideSelectedObjectsKey);

        [KeyDescription(idQuickHideUnselectedObjectsKey)]
        public static readonly KeyEvent quickHideUnselectedObjectsKey = new KeyEvent(idQuickHideUnselectedObjectsKey);

        [KeyDescription(idToggleSelectedObjectVisibilityKey)]
        public static readonly KeyEvent toggleSelectedObjectVisibilityKey = new KeyEvent(idToggleSelectedObjectVisibilityKey);

        [KeyDescription(idUnHideAllObjectsKey)]
        public static readonly KeyEvent unHideAllObjectsKey = new KeyEvent(idUnHideAllObjectsKey);

        [KeyDescription(idRotateSelectionLeft)]
        public static readonly KeyEvent rotateSelectionLeft = new KeyEvent(idRotateSelectionLeft);

        [KeyDescription(idRotateSelectionRight)]
        public static readonly KeyEvent rotateSelectionRight = new KeyEvent(idRotateSelectionRight);

        [KeyDescription(idMoveSelectionLeft)]
        public static readonly KeyEvent moveSelectionLeft = new KeyEvent(idMoveSelectionLeft);

        [KeyDescription(idMoveSelectionRight)]
        public static readonly KeyEvent moveSelectionRight = new KeyEvent(idMoveSelectionRight);

        [KeyDescription(idMoveSelectionBack)]
        public static readonly KeyEvent moveSelectionBack = new KeyEvent(idMoveSelectionBack);

        [KeyDescription(idMoveSelectionForward)]
        public static readonly KeyEvent moveSelectionForward = new KeyEvent(idMoveSelectionForward);

        [KeyDescription(idMoveSelectionDown)]
        public static readonly KeyEvent moveSelectionDown = new KeyEvent(idMoveSelectionDown);

        [KeyDescription(idMoveSelectionUp)]
        public static readonly KeyEvent moveSelectionUp = new KeyEvent(idMoveSelectionUp);

        [KeyDescription(idFlipSelectionX)]
        public static readonly KeyEvent flipSelectionX = new KeyEvent(idFlipSelectionX);

        [KeyDescription(idFlipSelectionY)]
        public static readonly KeyEvent flipSelectionY = new KeyEvent(idFlipSelectionY);

        [KeyDescription(idFlipSelectionZ)]
        public static readonly KeyEvent flipSelectionZ = new KeyEvent(idFlipSelectionZ);

        [KeyDescription(idMakeSelectedPassThroughKey)]
        public static readonly KeyEvent makeSelectedPassThroughKey = new KeyEvent(idMakeSelectedPassThroughKey);

        [KeyDescription(idMakeSelectedAdditiveKey)]
        public static readonly KeyEvent makeSelectedAdditiveKey = new KeyEvent(idMakeSelectedAdditiveKey);

        [KeyDescription(idMakeSelectedSubtractiveKey)]
        public static readonly KeyEvent makeSelectedSubtractiveKey = new KeyEvent(idMakeSelectedSubtractiveKey);

        [KeyDescription(idMakeSelectedIntersectingKey)]
        public static readonly KeyEvent makeSelectedIntersectingKey = new KeyEvent(idMakeSelectedIntersectingKey);

        [KeyDescription(idHalfGridSizeKey)]
        public static readonly KeyEvent halfGridSizeKey = new KeyEvent(idHalfGridSizeKey);

        [KeyDescription(idDoubleGridSizeKey)]
        public static readonly KeyEvent doubleGridSizeKey = new KeyEvent(idDoubleGridSizeKey);

        [KeyDescription(idToggleShowGridKey)]
        public static readonly KeyEvent toggleShowGridKey = new KeyEvent(idToggleShowGridKey);

        [KeyDescription(idToggleSnappingKey)]
        public static readonly KeyEvent toggleSnappingKey = new KeyEvent(idToggleSnappingKey);

        [Shortcut(idFreeBuilderMode, typeof(SceneView), shortcutsEnabledTag, KeyCode.N)]
        [Shortcut(idCylinderBuilderMode, typeof(SceneView), shortcutsEnabledTag, KeyCode.C)]
        [Shortcut(idBoxBuilderMode, typeof(SceneView), shortcutsEnabledTag, KeyCode.B)]
        [Shortcut(idSphereBuilderMode, typeof(SceneView), shortcutsEnabledTag, KeyCode.K)]
        [Shortcut(idLinearStairsBuilderMode, typeof(SceneView), shortcutsEnabledTag, KeyCode.L)]
        //[Shortcut(idSpiralStairsBuilderMode, typeof(SceneView), shortcutsEnabledTag, KeyCode.P)] // This one is unused
        [Shortcut(idSwitchToObjectEditMode, typeof(SceneView), shortcutsEnabledTag, KeyCode.F1, ShortcutModifiers.Alt)]
        [Shortcut(idSwitchToGenerateEditMode, typeof(SceneView), shortcutsEnabledTag, KeyCode.F2, ShortcutModifiers.Alt)]
        [Shortcut(idSwitchToMeshEditMode, typeof(SceneView), shortcutsEnabledTag, KeyCode.F3, ShortcutModifiers.Alt)]
        [Shortcut(idSwitchToClipEditMode, typeof(SceneView), shortcutsEnabledTag, KeyCode.F4, ShortcutModifiers.Alt)]
        [Shortcut(idSwitchToSurfaceEditMode, typeof(SceneView), shortcutsEnabledTag, KeyCode.F5, ShortcutModifiers.Alt)]
        [Shortcut(idCenterPivot, typeof(SceneView), shortcutsEnabledTag, KeyCode.R, ShortcutModifiers.Control)]
        [Shortcut(idMergeEdgePoints, typeof(SceneView), shortcutsEnabledTag, KeyCode.M)]
        [Shortcut(idCopyMaterialTexGen, typeof(SceneView), shortcutsEnabledTag, KeyCode.G)]
        [Shortcut(idCycleClipModes, typeof(SceneView), shortcutsEnabledTag, KeyCode.Tab)]
        [Shortcut(idInsertPoint, typeof(SceneView), shortcutsEnabledTag, KeyCode.I)]
        [Shortcut(idDeleteSelectionKey, typeof(SceneView), shortcutsEnabledTag, KeyCode.Delete)] // This one had KeyCode.Backspace as well, cannot bind multiple keys from here
        [Shortcut(idCloneDragActivate, typeof(SceneView), shortcutsEnabledTag, KeyCode.D)] // This one had none, shift and control modifier bound, cannot bind multiple keys from here
        [Shortcut(idSnapToGridKey, typeof(SceneView), shortcutsEnabledTag, KeyCode.End, ShortcutModifiers.Control)]
        [Shortcut(idQuickHideSelectedObjectsKey, typeof(SceneView), shortcutsEnabledTag, KeyCode.H)]
        [Shortcut(idQuickHideUnselectedObjectsKey, typeof(SceneView), shortcutsEnabledTag, KeyCode.H, ShortcutModifiers.Control)]
        [Shortcut(idToggleSelectedObjectVisibilityKey, typeof(SceneView), shortcutsEnabledTag, KeyCode.H, ShortcutModifiers.Shift)]
        [Shortcut(idUnHideAllObjectsKey, typeof(SceneView), shortcutsEnabledTag, KeyCode.U)]
        [Shortcut(idRotateSelectionLeft, typeof(SceneView), shortcutsEnabledTag, KeyCode.Comma, ShortcutModifiers.Control)]
        [Shortcut(idRotateSelectionRight, typeof(SceneView), shortcutsEnabledTag, KeyCode.Period, ShortcutModifiers.Control)]
        [Shortcut(idMoveSelectionLeft, typeof(SceneView), shortcutsEnabledTag, KeyCode.LeftArrow, ShortcutModifiers.Control | ShortcutModifiers.Shift)]
        [Shortcut(idMoveSelectionRight, typeof(SceneView), shortcutsEnabledTag, KeyCode.RightArrow, ShortcutModifiers.Control | ShortcutModifiers.Shift)]
        [Shortcut(idMoveSelectionBack, typeof(SceneView), shortcutsEnabledTag, KeyCode.DownArrow, ShortcutModifiers.Control | ShortcutModifiers.Shift)]
        [Shortcut(idMoveSelectionForward, typeof(SceneView), shortcutsEnabledTag, KeyCode.UpArrow, ShortcutModifiers.Control | ShortcutModifiers.Shift)]
        [Shortcut(idMoveSelectionDown, typeof(SceneView), shortcutsEnabledTag, KeyCode.PageDown, ShortcutModifiers.Control | ShortcutModifiers.Shift)]
        [Shortcut(idMoveSelectionUp, typeof(SceneView), shortcutsEnabledTag, KeyCode.PageUp, ShortcutModifiers.Control | ShortcutModifiers.Shift)]
        [Shortcut(idFlipSelectionX, typeof(SceneView), shortcutsEnabledTag, KeyCode.X, ShortcutModifiers.Control | ShortcutModifiers.Shift)]
        [Shortcut(idFlipSelectionY, typeof(SceneView), shortcutsEnabledTag, KeyCode.Y, ShortcutModifiers.Control | ShortcutModifiers.Shift)]
        [Shortcut(idFlipSelectionZ, typeof(SceneView), shortcutsEnabledTag, KeyCode.Z, ShortcutModifiers.Control | ShortcutModifiers.Shift)]
        [Shortcut(idMakeSelectedPassThroughKey, typeof(SceneView), shortcutsEnabledTag, KeyCode.Question)]
        [Shortcut(idMakeSelectedAdditiveKey, typeof(SceneView), shortcutsEnabledTag, KeyCode.Equals)] // This one had KeyCode.KeypadPlus, cannot bind multiple keys from here
        [Shortcut(idMakeSelectedSubtractiveKey, typeof(SceneView), shortcutsEnabledTag, KeyCode.Minus)] // This one had KeyCode.KeypadMinus, cannot bind multiple keys from here
        [Shortcut(idMakeSelectedIntersectingKey, typeof(SceneView), shortcutsEnabledTag, KeyCode.Backslash)] // This one had KeyCode.KeypadDivide, cannot bind multiple keys from here
        [Shortcut(idHalfGridSizeKey, typeof(SceneView), shortcutsEnabledTag, KeyCode.LeftBracket)]
        [Shortcut(idDoubleGridSizeKey, typeof(SceneView), shortcutsEnabledTag, KeyCode.RightBracket)]
        [Shortcut(idToggleShowGridKey, typeof(SceneView), shortcutsEnabledTag, KeyCode.G, ShortcutModifiers.Shift)]
        [Shortcut(idToggleSnappingKey, typeof(SceneView), shortcutsEnabledTag, KeyCode.T, ShortcutModifiers.Shift)]
        private static void DummyInputSwallower2(ShortcutArguments args)
        {
            // Shortcuts' function are handled in-context by RCSG instead of globally through static functions
            // This is just a dummy to ensure shortcuts are registered within the manager
        }

        private static bool shortcutsActive = false;

        public static bool ShortcutsActive
        {
            get => shortcutsActive;
            set
            {
                if (shortcutsActive == value)
                    return;

                shortcutsActive = value;
                // shortcut manager is not yet ready when reloading, delay the call
                if (value)
                    EditorApplication.delayCall += static () => ShortcutManager.RegisterTag(shortcutsEnabledTag);
                else
                    EditorApplication.delayCall += static () => ShortcutManager.UnregisterTag(shortcutsEnabledTag);
            }
        }

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
