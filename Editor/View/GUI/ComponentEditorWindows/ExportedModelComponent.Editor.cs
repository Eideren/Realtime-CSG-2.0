using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using InternalRealtimeCSG;
using UnityEngine.SceneManagement;

namespace RealtimeCSG
{
    [CustomEditor(typeof(CSGModelExported)), CanEditMultipleObjects]
    internal sealed class ExportedModelComponentEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode)
            {
                return;
            }

            GUILayout.BeginVertical(GUI.skin.box);

            GUILayout.Label("A hidden CSG model, and all it's brushes and operations, is contained by this object. This will automatically be removed at build time.", CsgguiStyleUtility.WrapLabel);

            GUILayout.Space(10);

            if(GUILayout.Button("Revert back to CSG model"))
            {
                Undo.IncrementCurrentGroup();
                int groupIndex = Undo.GetCurrentGroup();
                Undo.SetCurrentGroupName("Reverted to former CSG model");

                try
                {
                    List<Object>   selection    = new List<Object>();
                    HashSet<Scene> updateScenes = new HashSet<Scene>();

                    foreach(Object o in targets)
                    {
                        CSGModelExported exportedModel = o as CSGModelExported;

                        if(!exportedModel)
                        {
                            continue;
                        }

                        exportedModel.disarm    = true;
                        exportedModel.hideFlags = HideFlags.DontSaveInBuild;
                        updateScenes.Add(exportedModel.gameObject.scene);

                        if(exportedModel.containedModel)
                        {
                            Undo.RecordObject(exportedModel.containedModel, "Revert model");
                            selection.Add(exportedModel.containedModel.gameObject);
                            exportedModel.containedModel.transform.SetParent(exportedModel.transform.parent, true);

                            exportedModel.containedModel.transform.SetSiblingIndex(exportedModel.transform.GetSiblingIndex());

                            exportedModel.containedModel.gameObject.SetActive(true);
                            exportedModel.containedModel.gameObject.hideFlags = HideFlags.None;
                            EditorUtility.SetDirty(exportedModel.containedModel);
                        }
                        else
                        {
                            MeshInstanceManager.ReverseExport(exportedModel);
                            selection.Add(exportedModel.gameObject);
                            EditorUtility.SetDirty(exportedModel);
                        }

                        Undo.DestroyObjectImmediate(exportedModel);
                    }

                    Selection.objects = selection.ToArray();

                    InternalCSGModelManager.SkipCheckForChanges = true;

                    BrushOutlineManager.ClearOutlines();
                    InternalCSGModelManager.ForceRebuildAll();

                    foreach(Scene scene in updateScenes)
                    {
                        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);
                    }
                }
                finally
                {
                    InternalCSGModelManager.SkipCheckForChanges = false;
                    Undo.CollapseUndoOperations(groupIndex);
                }
            }

            GUILayout.Space(30);

            if(GUILayout.Button("Remove hidden CSG model"))
            {
                Undo.IncrementCurrentGroup();
                int groupIndex = Undo.GetCurrentGroup();
                Undo.SetCurrentGroupName("Removed hidden CSG model");
                Undo.RecordObject(this, "Removed hidden CSG model");

                foreach(Object o in targets)
                {
                    CSGModelExported exportedModel = o as CSGModelExported;

                    if(!exportedModel)
                    {
                        continue;
                    }

                    exportedModel.DestroyModel(true);
                    Undo.DestroyObjectImmediate(exportedModel);
                }

                Undo.CollapseUndoOperations(groupIndex);
            }

            GUILayout.Space(10);
            GUILayout.EndVertical();
        }
    }
}
