using System.Linq;
using UnityEditor;
using UnityEngine;
using RealtimeCSG.Foundation;
using UnityEditor.ShortcutManagement;

namespace RealtimeCSG
{
    internal static class Keys
    {
        // type is initialized from top to bottom, keep this at the top as other statics rely on this being assigned
        private static readonly InSceneView inSceneViewContext = new();
        private static readonly InNonConsumingSceneView inNonConsumingSceneView = new(inSceneViewContext);

        // Specific handling for these two keys, we cannot rebind keys baked into menu item declaration,
        // we'll just provide an additional key user can bind on top of the default one in those cases

        [Shortcut("RCSG/ Turn Realtime-CSG on or off (besides Ctrl+F3)")]
        private static void ToggleRealtimeCSG(ShortcutArguments args)
        {
            CSGSettings.SetRealtimeCSGEnabled(!CSGSettings.EnableRealtimeCSG);
        }

        private const string idPopOutToolWindowKey = "RCSG/Tools: Popout Tool Window (besides Ctrl+F2)";
        public static readonly KeyEvent popOutToolWindowKey = new KeyEvent(idPopOutToolWindowKey, null, false, new KeyCodeWithModifier(KeyCode.F2, EventModifiers.Control));

        [Shortcut(idPopOutToolWindowKey)]
        private static void PopoutToolWindow(ShortcutArguments args)
        {
            EditModeToolWindowSceneGUI.GetWindow();
        }




        // Specific handling for these two keys, the shortcut manager does not support binding to escape, return or keypad enter...
        
        private const string idCancelActionKey = "RCSG/Action: Cancel or Deselect (besides Escape)";
        public static readonly KeyEvent cancelActionKey = new KeyEvent
        (
            idCancelActionKey, inSceneViewContext, false, new KeyCodeWithModifier(KeyCode.Escape)
        );

        private const string idPerformActionKey = "RCSG/Action: Perform (besides Enter/Return)";
        public static readonly KeyEvent performActionKey = new KeyEvent
        (
            idPerformActionKey,
            inSceneViewContext,
            false,
            new KeyCodeWithModifier(KeyCode.Return),
            new KeyCodeWithModifier(KeyCode.KeypadEnter)
        );

        [Shortcut(idCancelActionKey, typeof(InSceneView))]
        [Shortcut(idPerformActionKey, typeof(InSceneView))]
        private static void DummyInputSwallower1(ShortcutArguments args)
        {
            // Shortcuts' function are handled in-context by RCSG instead of globally through static functions
            // This is just a dummy to ensure shortcuts are registered within the manager
        }




        private const string idOperationMoveBefore = "RCSG/Selection: Move operation before";
        private const string idOperationMoveAfter = "RCSG/Selection: Move operation after";

        [Shortcut(idOperationMoveBefore, typeof(InNonConsumingSceneView), KeyCode.PageUp)]
        private static void MoveBefore(ShortcutArguments args)
        {
            var trs = Selection.transforms;
            trs = trs.OrderBy(x => x.GetSiblingIndex()).ToArray();

            foreach (var transform in trs)
            {
                var i = transform.GetSiblingIndex();
                if (i > 0)
                    transform.SetSiblingIndex(i - 1);
            }

            InternalCSGModelManager.UpdateHierarchy();
        }

        [Shortcut(idOperationMoveAfter, typeof(InNonConsumingSceneView), KeyCode.PageDown)]
        private static void MoveAfter(ShortcutArguments args)
        {
            var trs = Selection.transforms;
            trs = trs.OrderByDescending(x => x.GetSiblingIndex()).ToArray();

            foreach (var transform in trs)
            {
                var i = transform.GetSiblingIndex();
                if (i < transform.parent.childCount - 1)
                    transform.SetSiblingIndex(i + 1);
            }

            InternalCSGModelManager.UpdateHierarchy();
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

        public static readonly KeyEvent freeBuilderMode = new KeyEvent(idFreeBuilderMode, inSceneViewContext, true);
        public static readonly KeyEvent cylinderBuilderMode = new KeyEvent(idCylinderBuilderMode, inSceneViewContext, true);
        public static readonly KeyEvent boxBuilderMode = new KeyEvent(idBoxBuilderMode, inSceneViewContext, true);
        public static readonly KeyEvent sphereBuilderMode = new KeyEvent(idSphereBuilderMode, inSceneViewContext, true);
        public static readonly KeyEvent linearStairsBuilderMode = new KeyEvent(idLinearStairsBuilderMode, inSceneViewContext, true);
        //public static readonly KeyEvent spiralStairsBuilderMode = new KeyEvent(idSpiralStairsBuilderMode, true);
        public static readonly KeyEvent switchToObjectEditMode = new KeyEvent(idSwitchToObjectEditMode, inSceneViewContext);
        public static readonly KeyEvent switchToGenerateEditMode = new KeyEvent(idSwitchToGenerateEditMode, inSceneViewContext);
        public static readonly KeyEvent switchToMeshEditMode = new KeyEvent(idSwitchToMeshEditMode, inSceneViewContext);
        public static readonly KeyEvent switchToClipEditMode = new KeyEvent(idSwitchToClipEditMode, inSceneViewContext);
        public static readonly KeyEvent switchToSurfaceEditMode = new KeyEvent(idSwitchToSurfaceEditMode, inSceneViewContext);
        public static readonly KeyEvent centerPivot = new KeyEvent(idCenterPivot, inSceneViewContext);
        public static readonly KeyEvent mergeEdgePoints = new KeyEvent(idMergeEdgePoints, inSceneViewContext);
        public static readonly KeyEvent copyMaterialTexGen = new KeyEvent(idCopyMaterialTexGen, inSceneViewContext, true);
        public static readonly KeyEvent cycleClipModes = new KeyEvent(idCycleClipModes, inSceneViewContext);
        public static readonly KeyEvent insertPoint = new KeyEvent(idInsertPoint, inSceneViewContext);
        public static readonly KeyEvent deleteSelectionKey = new KeyEvent(idDeleteSelectionKey, inSceneViewContext);
        public static readonly KeyEvent cloneDragActivate = new KeyEvent(idCloneDragActivate, inSceneViewContext, true);
        public static readonly KeyEvent snapToGridKey = new KeyEvent(idSnapToGridKey, inSceneViewContext);
        public static readonly KeyEvent quickHideSelectedObjectsKey = new KeyEvent(idQuickHideSelectedObjectsKey, inSceneViewContext);
        public static readonly KeyEvent quickHideUnselectedObjectsKey = new KeyEvent(idQuickHideUnselectedObjectsKey, inSceneViewContext);
        public static readonly KeyEvent toggleSelectedObjectVisibilityKey = new KeyEvent(idToggleSelectedObjectVisibilityKey, inSceneViewContext);
        public static readonly KeyEvent unHideAllObjectsKey = new KeyEvent(idUnHideAllObjectsKey, inSceneViewContext);
        public static readonly KeyEvent rotateSelectionLeft = new KeyEvent(idRotateSelectionLeft, inSceneViewContext);
        public static readonly KeyEvent rotateSelectionRight = new KeyEvent(idRotateSelectionRight, inSceneViewContext);
        public static readonly KeyEvent moveSelectionLeft = new KeyEvent(idMoveSelectionLeft, inSceneViewContext);
        public static readonly KeyEvent moveSelectionRight = new KeyEvent(idMoveSelectionRight, inSceneViewContext);
        public static readonly KeyEvent moveSelectionBack = new KeyEvent(idMoveSelectionBack, inSceneViewContext);
        public static readonly KeyEvent moveSelectionForward = new KeyEvent(idMoveSelectionForward, inSceneViewContext);
        public static readonly KeyEvent moveSelectionDown = new KeyEvent(idMoveSelectionDown, inSceneViewContext);
        public static readonly KeyEvent moveSelectionUp = new KeyEvent(idMoveSelectionUp, inSceneViewContext);
        public static readonly KeyEvent flipSelectionX = new KeyEvent(idFlipSelectionX, inSceneViewContext);
        public static readonly KeyEvent flipSelectionY = new KeyEvent(idFlipSelectionY, inSceneViewContext);
        public static readonly KeyEvent flipSelectionZ = new KeyEvent(idFlipSelectionZ, inSceneViewContext);
        public static readonly KeyEvent makeSelectedPassThroughKey = new KeyEvent(idMakeSelectedPassThroughKey, inSceneViewContext);
        public static readonly KeyEvent makeSelectedAdditiveKey = new KeyEvent(idMakeSelectedAdditiveKey, inSceneViewContext);
        public static readonly KeyEvent makeSelectedSubtractiveKey = new KeyEvent(idMakeSelectedSubtractiveKey, inSceneViewContext);
        public static readonly KeyEvent makeSelectedIntersectingKey = new KeyEvent(idMakeSelectedIntersectingKey, inSceneViewContext);
        public static readonly KeyEvent halfGridSizeKey = new KeyEvent(idHalfGridSizeKey, inSceneViewContext);
        public static readonly KeyEvent doubleGridSizeKey = new KeyEvent(idDoubleGridSizeKey, inSceneViewContext);
        public static readonly KeyEvent toggleShowGridKey = new KeyEvent(idToggleShowGridKey, inSceneViewContext);
        public static readonly KeyEvent toggleSnappingKey = new KeyEvent(idToggleSnappingKey, inSceneViewContext);

        [Shortcut(idFreeBuilderMode, typeof(InSceneView), KeyCode.N)]
        [Shortcut(idCylinderBuilderMode, typeof(InSceneView), KeyCode.C)]
        [Shortcut(idBoxBuilderMode, typeof(InSceneView), KeyCode.B)]
        [Shortcut(idSphereBuilderMode, typeof(InSceneView), KeyCode.K)]
        [Shortcut(idLinearStairsBuilderMode, typeof(InSceneView), KeyCode.L)]
        //[Shortcut(idSpiralStairsBuilderMode, typeof(ShortcutsAndSceneview), KeyCode.P)] // This one is unused
        [Shortcut(idSwitchToObjectEditMode, typeof(InSceneView), KeyCode.F1, ShortcutModifiers.Alt)]
        [Shortcut(idSwitchToGenerateEditMode, typeof(InSceneView), KeyCode.F2, ShortcutModifiers.Alt)]
        [Shortcut(idSwitchToMeshEditMode, typeof(InSceneView), KeyCode.F3, ShortcutModifiers.Alt)]
        [Shortcut(idSwitchToClipEditMode, typeof(InSceneView), KeyCode.F4, ShortcutModifiers.Alt)]
        [Shortcut(idSwitchToSurfaceEditMode, typeof(InSceneView), KeyCode.F5, ShortcutModifiers.Alt)]
        [Shortcut(idCenterPivot, typeof(InSceneView), KeyCode.R, ShortcutModifiers.Control)]
        [Shortcut(idMergeEdgePoints, typeof(InSceneView), KeyCode.M)]
        [Shortcut(idCopyMaterialTexGen, typeof(InSceneView), KeyCode.G)]
        [Shortcut(idCycleClipModes, typeof(InSceneView), KeyCode.Tab)]
        [Shortcut(idInsertPoint, typeof(InSceneView), KeyCode.I)]
        [Shortcut(idDeleteSelectionKey, typeof(InSceneView), KeyCode.Delete)] // This one had KeyCode.Backspace as well, cannot bind multiple keys from here
        [Shortcut(idCloneDragActivate, typeof(InSceneView), KeyCode.D)] // This one had none, shift and control modifier bound, cannot bind multiple keys from here
        [Shortcut(idSnapToGridKey, typeof(InSceneView), KeyCode.End, ShortcutModifiers.Control)]
        [Shortcut(idQuickHideSelectedObjectsKey, typeof(InSceneView), KeyCode.H)]
        [Shortcut(idQuickHideUnselectedObjectsKey, typeof(InSceneView), KeyCode.H, ShortcutModifiers.Control)]
        [Shortcut(idToggleSelectedObjectVisibilityKey, typeof(InSceneView), KeyCode.H, ShortcutModifiers.Shift)]
        [Shortcut(idUnHideAllObjectsKey, typeof(InSceneView), KeyCode.U)]
        [Shortcut(idRotateSelectionLeft, typeof(InSceneView), KeyCode.Comma, ShortcutModifiers.Control)]
        [Shortcut(idRotateSelectionRight, typeof(InSceneView), KeyCode.Period, ShortcutModifiers.Control)]
        [Shortcut(idMoveSelectionLeft, typeof(InSceneView), KeyCode.LeftArrow, ShortcutModifiers.Control | ShortcutModifiers.Shift)]
        [Shortcut(idMoveSelectionRight, typeof(InSceneView), KeyCode.RightArrow, ShortcutModifiers.Control | ShortcutModifiers.Shift)]
        [Shortcut(idMoveSelectionBack, typeof(InSceneView), KeyCode.DownArrow, ShortcutModifiers.Control | ShortcutModifiers.Shift)]
        [Shortcut(idMoveSelectionForward, typeof(InSceneView), KeyCode.UpArrow, ShortcutModifiers.Control | ShortcutModifiers.Shift)]
        [Shortcut(idMoveSelectionDown, typeof(InSceneView), KeyCode.PageDown, ShortcutModifiers.Control | ShortcutModifiers.Shift)]
        [Shortcut(idMoveSelectionUp, typeof(InSceneView), KeyCode.PageUp, ShortcutModifiers.Control | ShortcutModifiers.Shift)]
        [Shortcut(idFlipSelectionX, typeof(InSceneView), KeyCode.X, ShortcutModifiers.Control | ShortcutModifiers.Shift)]
        [Shortcut(idFlipSelectionY, typeof(InSceneView), KeyCode.Y, ShortcutModifiers.Control | ShortcutModifiers.Shift)]
        [Shortcut(idFlipSelectionZ, typeof(InSceneView), KeyCode.Z, ShortcutModifiers.Control | ShortcutModifiers.Shift)]
        [Shortcut(idMakeSelectedPassThroughKey, typeof(InSceneView), KeyCode.Question)]
        [Shortcut(idMakeSelectedAdditiveKey, typeof(InSceneView), KeyCode.Equals)] // This one had KeyCode.KeypadPlus, cannot bind multiple keys from here
        [Shortcut(idMakeSelectedSubtractiveKey, typeof(InSceneView), KeyCode.Minus)] // This one had KeyCode.KeypadMinus, cannot bind multiple keys from here
        [Shortcut(idMakeSelectedIntersectingKey, typeof(InSceneView), KeyCode.Backslash)] // This one had KeyCode.KeypadDivide, cannot bind multiple keys from here
        [Shortcut(idHalfGridSizeKey, typeof(InSceneView), KeyCode.LeftBracket)]
        [Shortcut(idDoubleGridSizeKey, typeof(InSceneView), KeyCode.RightBracket)]
        [Shortcut(idToggleShowGridKey, typeof(InSceneView), KeyCode.G, ShortcutModifiers.Shift)]
        [Shortcut(idToggleSnappingKey, typeof(InSceneView), KeyCode.T, ShortcutModifiers.Shift)]
        private static void DummyInputSwallower2(ShortcutArguments args)
        {
            // Shortcuts' function are handled in-context by RCSG instead of globally through static functions
            // This is just a dummy to ensure shortcuts are registered within the manager
        }

        public static bool SceneViewShortcutsActive
        {
            get => inSceneViewContext.Active;
            set => inSceneViewContext.Active = value;
        }

        static Keys()
        {
            EditorApplication.delayCall += static () =>
            {
                ShortcutManager.RegisterContext(inSceneViewContext);
                ShortcutManager.RegisterContext(inNonConsumingSceneView);
            };
        }

        private class InSceneView : IConsumableShortcutContext
        {
            private int _frameConsumed = -1;
            public bool Active { get; set; }

            public bool CanRaiseEvent => Active 
                                         && EditorWindow.focusedWindow is SceneView
                                         && Tools.viewTool != ViewTool.FPS;

            bool IShortcutContext.active => CanRaiseEvent && _frameConsumed == Time.frameCount;

            public void Consume() => _frameConsumed = Time.frameCount;
        }

        private class InNonConsumingSceneView : IShortcutContext
        {
            private readonly InSceneView _parent;

            public InNonConsumingSceneView(InSceneView parent) => _parent = parent;

            bool IShortcutContext.active => _parent.Active
                                            && EditorWindow.focusedWindow is SceneView
                                            && Tools.viewTool != ViewTool.FPS;
        }
        
        internal interface IConsumableShortcutContext : IShortcutContext
        {
            /// <summary>
            /// Whether the current context is valid
            /// </summary>
            bool CanRaiseEvent { get; }

            /// <summary>
            /// Mark this context as having consumed the key event
            /// </summary>
            void Consume();
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
