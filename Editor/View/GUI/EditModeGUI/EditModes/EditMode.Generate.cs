using System;
using UnityEditor;
using UnityEngine;
using System.Linq;
using RealtimeCSG.Foundation;
using RealtimeCSG.Legacy;
using RealtimeCSG.Components;

namespace RealtimeCSG
{
    [Serializable]
    internal enum ShapeMode
    {
        FreeDraw
      , Box
      , Sphere
      , Cylinder
       ,

//		SpiralStairs,
        LinearStairs
      , Last = LinearStairs
    }

    internal sealed class EditModeGenerate : ScriptableObject, IEditMode
    {
        public bool UsesUnitySelection
        {
            get => false;
        }

        public bool IgnoreUnityRect
        {
            get => true;
        }

        public static event Action ShapeCommitted = null;
        public static event Action ShapeCancelled = null;

        public ShapeMode BuilderMode
        {
            get => builderMode;
            set
            {
                if(builderMode == value)
                {
                    return;
                }

                builderMode                = value;
                CSGSettings.ShapeBuildMode = builderMode;
                CSGSettings.Save();
                ResetTool();
            }
        }

        private IGenerator InternalCurrentGenerator
        {
            get
            {
                switch(builderMode)
                {
                    default:
                    case ShapeMode.FreeDraw:
                    {
                        return freedrawGenerator;
                    }
                    case ShapeMode.Cylinder:
                    {
                        return cylinderGenerator;
                    }
                    case ShapeMode.Box:
                    {
                        return boxGenerator;
                    }
                    case ShapeMode.Sphere:
                    {
                        return sphereGenerator;
                    }
                    case ShapeMode.LinearStairs:
                    {
                        return linearStairsGenerator;
                    } /*
                    case ShapeMode.SpiralStairs:
                    {
                        return spiralStairsGenerator;
                    }*/
                }
            }
        }

        public IGenerator CurrentGenerator
        {
            get
            {
                IGenerator       generator = InternalCurrentGenerator;
                ScriptableObject obj       = generator as ScriptableObject;

                if(obj != null && obj)
                {
                    return generator;
                }

                ResetTool();

                return generator;
            }
        }

        [SerializeField] private CSGBrush[] brushes = new CSGBrush[0];

        [SerializeField] private ShapeMode builderMode = ShapeMode.FreeDraw;

        [SerializeField] private GeneratorFreeDraw freedrawGenerator;
        [SerializeField] private GeneratorCylinder cylinderGenerator;
        [SerializeField] private GeneratorLinearStairs linearStairsGenerator;
        [SerializeField] private GeneratorSpiralStairs spiralStairsGenerator;
        [SerializeField] private GeneratorSphere sphereGenerator;
        [SerializeField] private GeneratorBox boxGenerator;

        [NonSerialized] private bool isEnabled = false;
        [NonSerialized] private bool hideTool = false;

        public void SetTargets(FilteredSelection filteredSelection)
        {
            hideTool = filteredSelection.nodeTargets.Length > 0;
            brushes  = filteredSelection.GetAllContainedBrushes().ToArray();
            lastLineMeshGeneration--;

            if(isEnabled)
            {
                Tools.hidden = hideTool;
            }
        }

        private void OnEnable()
        {
            CSGSettings.Reload();
            builderMode = CSGSettings.ShapeBuildMode;
        }

        public void OnEnableTool()
        {
            isEnabled    = true;
            Tools.hidden = hideTool;
            ResetTool();
        }

        public void OnDisableTool()
        {
            // Ensures that a commit is propertly finished when the toolmode is changed
            if(CurrentGenerator.CanCommit)
            {
                GeneratorBase genBase = InternalCurrentGenerator as GeneratorBase;

                if(genBase != null)
                {
                    genBase.EndCommit();
                }
            }

            isEnabled    = false;
            Tools.hidden = false;
            ResetTool();
        }

        private void ResetTool()
        {
            CSGGrid.ForceGrid = false;

            if(!freedrawGenerator)
            {
                freedrawGenerator                 = CreateInstance<GeneratorFreeDraw>();
                freedrawGenerator.snapFunction    = EditModeManager.SnapPointToGrid;
                freedrawGenerator.raySnapFunction = EditModeManager.SnapPointToRay;
                freedrawGenerator.shapeCancelled  = OnShapeCancelledEvent;
                freedrawGenerator.shapeCommitted  = OnShapeCommittedEvent;
            }

            if(!cylinderGenerator)
            {
                cylinderGenerator                 = CreateInstance<GeneratorCylinder>();
                cylinderGenerator.snapFunction    = EditModeManager.SnapPointToGrid;
                cylinderGenerator.raySnapFunction = EditModeManager.SnapPointToRay;
                cylinderGenerator.shapeCancelled  = OnShapeCancelledEvent;
                cylinderGenerator.shapeCommitted  = OnShapeCommittedEvent;
            }

            if(!boxGenerator)
            {
                boxGenerator                 = CreateInstance<GeneratorBox>();
                boxGenerator.snapFunction    = EditModeManager.SnapPointToGrid;
                boxGenerator.raySnapFunction = EditModeManager.SnapPointToRay;
                boxGenerator.shapeCancelled  = OnShapeCancelledEvent;
                boxGenerator.shapeCommitted  = OnShapeCommittedEvent;
            }

            if(!sphereGenerator)
            {
                sphereGenerator                 = CreateInstance<GeneratorSphere>();
                sphereGenerator.snapFunction    = EditModeManager.SnapPointToGrid;
                sphereGenerator.raySnapFunction = EditModeManager.SnapPointToRay;
                sphereGenerator.shapeCancelled  = OnShapeCancelledEvent;
                sphereGenerator.shapeCommitted  = OnShapeCommittedEvent;
            }

            if(!linearStairsGenerator)
            {
                linearStairsGenerator                 = CreateInstance<GeneratorLinearStairs>();
                linearStairsGenerator.snapFunction    = EditModeManager.SnapPointToGrid;
                linearStairsGenerator.raySnapFunction = EditModeManager.SnapPointToRay;
                linearStairsGenerator.shapeCancelled  = OnShapeCancelledEvent;
                linearStairsGenerator.shapeCommitted  = OnShapeCommittedEvent;
            }

            if(!spiralStairsGenerator)
            {
                spiralStairsGenerator                 = CreateInstance<GeneratorSpiralStairs>();
                spiralStairsGenerator.snapFunction    = EditModeManager.SnapPointToGrid;
                spiralStairsGenerator.raySnapFunction = EditModeManager.SnapPointToRay;
                spiralStairsGenerator.shapeCancelled  = OnShapeCancelledEvent;
                spiralStairsGenerator.shapeCommitted  = OnShapeCommittedEvent;
            }

            IGenerator generator = InternalCurrentGenerator;

            if(generator != null)
            {
                ScriptableObject obj = generator as ScriptableObject;

                if(obj)
                {
                    generator.Init();
                }
            }
        }

        public bool HotKeyReleased()
        {
            if(CurrentGenerator == null)
            {
                return false;
            }

            return CurrentGenerator.HotKeyReleased();
        }

        private void OnShapeCancelledEvent()
        {
            CurrentGenerator.Init();
            ShapeCancelled.Invoke();
        }

        private void OnShapeCommittedEvent()
        {
            ShapeCommitted.Invoke();
        }

        public bool UndoRedoPerformed()
        {
            return CurrentGenerator.UndoRedoPerformed();
        }

        public bool DeselectAll()
        {
            CurrentGenerator.PerformDeselectAll();

            return true;
        }

        private void SetOperationType(CSGOperationType operationType)
        {
            CurrentGenerator.CurrentCSGOperationType = operationType;
        }

        private void OnDestroy()
        {
            zTestLineMeshManager.Destroy();
            noZTestLineMeshManager.Destroy();
        }

        private LineMeshManager zTestLineMeshManager = new LineMeshManager();
        private LineMeshManager noZTestLineMeshManager = new LineMeshManager();
        private int lastLineMeshGeneration = -1;

        public void HandleEvents(SceneView sceneView, Rect sceneRect)
        {
            if(CurrentGenerator == null)
            {
                return;
            }

            CurrentGenerator.HandleEvents(sceneView, sceneRect);

            switch(Event.current.type)
            {
                case EventType.ValidateCommand:
                {
                    if(!EditorGUIUtility.editingTextField && Keys.makeSelectedAdditiveKey.IsKeyPressed())
                    {
                        Event.current.Use();

                        break;
                    }

                    if(!EditorGUIUtility.editingTextField && Keys.makeSelectedSubtractiveKey.IsKeyPressed())
                    {
                        Event.current.Use();

                        break;
                    }

                    if(!EditorGUIUtility.editingTextField && Keys.makeSelectedIntersectingKey.IsKeyPressed())
                    {
                        Event.current.Use();

                        break;
                    }

                    if(!EditorGUIUtility.editingTextField && Keys.cancelActionKey.IsKeyPressed())
                    {
                        Event.current.Use();

                        break;
                    }

                    if(Keys.HandleSceneValidate(EditModeManager.CurrentTool, false))
                    {
                        Event.current.Use();
                        HandleUtility.Repaint();

                        break;
                    }

                    break;
                }

                case EventType.KeyDown:
                {
                    if(!EditorGUIUtility.editingTextField && Keys.makeSelectedAdditiveKey.IsKeyPressed())
                    {
                        Event.current.Use();

                        break;
                    }

                    if(!EditorGUIUtility.editingTextField && Keys.makeSelectedSubtractiveKey.IsKeyPressed())
                    {
                        Event.current.Use();

                        break;
                    }

                    if(!EditorGUIUtility.editingTextField && Keys.makeSelectedIntersectingKey.IsKeyPressed())
                    {
                        Event.current.Use();

                        break;
                    }

                    if(!EditorGUIUtility.editingTextField && Keys.cancelActionKey.IsKeyPressed())
                    {
                        Event.current.Use();

                        break;
                    }

                    if(Keys.HandleSceneKeyDown(EditModeManager.CurrentTool, false))
                    {
                        Event.current.Use();
                        HandleUtility.Repaint();

                        break;
                    }

                    break;
                }

                case EventType.KeyUp:
                {
                    if(!EditorGUIUtility.editingTextField && Keys.makeSelectedAdditiveKey.IsKeyPressed())
                    {
                        SetOperationType(CSGOperationType.Additive);
                        Event.current.Use();

                        break;
                    }

                    if(!EditorGUIUtility.editingTextField && Keys.makeSelectedSubtractiveKey.IsKeyPressed())
                    {
                        SetOperationType(CSGOperationType.Subtractive);
                        Event.current.Use();

                        break;
                    }

                    if(!EditorGUIUtility.editingTextField && Keys.makeSelectedIntersectingKey.IsKeyPressed())
                    {
                        SetOperationType(CSGOperationType.Intersecting);
                        Event.current.Use();

                        break;
                    }

                    if(!EditorGUIUtility.editingTextField && Keys.cancelActionKey.IsKeyPressed())
                    {
                        CurrentGenerator.PerformDeselectAll();
                        Event.current.Use();

                        break;
                    }

                    if(Keys.HandleSceneKeyUp(EditModeManager.CurrentTool, false))
                    {
                        Event.current.Use();
                        HandleUtility.Repaint();

                        break;
                    }

                    break;
                }

                case EventType.Repaint:
                {
                    if(lastLineMeshGeneration != InternalCSGModelManager.MeshGeneration)
                    {
                        lastLineMeshGeneration = InternalCSGModelManager.MeshGeneration;

                        Matrix4x4[] brushTransformation = new Matrix4x4[brushes.Length];
                        int[]       brushNodeIDs        = new int[brushes.Length];

                        for(int i = brushes.Length - 1; i >= 0; i--)
                        {
                            CSGBrush brush = brushes[i];

                            if(brush.brushNodeID == CSGNode.InvalidNodeID || // could be a prefab
                               brush.compareTransformation == null ||
                               brush.ChildData == null ||
                               brush.ChildData.ModelTransform == null ||
                               !brush.ChildData.ModelTransform)
                            {
                                ArrayUtility.RemoveAt(ref brushTransformation, i);
                                ArrayUtility.RemoveAt(ref brushNodeIDs, i);

                                continue;
                            }

                            brushTransformation[i] = brush.compareTransformation.localToWorldMatrix;
                            brushNodeIDs[i]        = brush.brushNodeID;
                        }

                        CSGRenderer.DrawSelectedBrushes(
                            zTestLineMeshManager, noZTestLineMeshManager, brushNodeIDs, brushTransformation,
                            ColorSettings.SelectedOutlines, GUIConstants.LINE_SCALE
                        );
                    }

                    MaterialUtility.LineAlphaMultiplier     = 1.0f;
                    MaterialUtility.LineDashMultiplier      = 2.0f;
                    MaterialUtility.LineThicknessMultiplier = 2.0f;
                    noZTestLineMeshManager.Render(MaterialUtility.NoZTestGenericLine);
                    MaterialUtility.LineThicknessMultiplier = 1.0f;
                    zTestLineMeshManager.Render(MaterialUtility.ZTestGenericLine);

                    break;
                }
            }
        }

        public void OnInspectorGUI(EditorWindow window, float height)
        {
            EditModeGenerateGUI.OnInspectorGUI(this, window, height);
        }

        public Rect GetLastSceneGUIRect()
        {
            return EditModeGenerateGUI.GetLastSceneGUIRect(this);
        }

        public bool OnSceneGUI(Rect windowRect)
        {
            return EditModeGenerateGUI.OnSceneGUI(windowRect, this);
        }

        public void GenerateFromPolygon(
            Camera camera, CSGBrush brush, CSGPlane plane, Vector3 direction, Vector3[] meshVertices, int[] indices
          , uint[] smoothingGroups, bool drag, CSGOperationType forceDragSource, bool autoCommitExtrusion
        )
        {
            BuilderMode = ShapeMode.FreeDraw;

            freedrawGenerator.GenerateFromPolygon(
                camera, brush, plane, direction, meshVertices, indices, smoothingGroups, drag, forceDragSource
              , autoCommitExtrusion
            );
        }
    }
}
