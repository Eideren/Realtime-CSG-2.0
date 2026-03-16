using UnityEditor;
using UnityEngine;

namespace RealtimeCSG
{
    internal interface IEditMode
    {
        bool UsesUnitySelection { get; }
        bool IgnoreUnityRect { get; }

        void HandleEvents(SceneView sceneView, Rect rect);

        Rect GetLastSceneGUIRect();
        bool OnSceneGUI(Rect windowRect);
        void OnInspectorGUI(EditorWindow window, float height);

        void OnDisableTool();
        void OnEnableTool();
        bool UndoRedoPerformed();
        bool DeselectAll();

        void SetTargets(FilteredSelection filteredSelection);
    }
}
