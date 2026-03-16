using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using InternalRealtimeCSG;
using RealtimeCSG.Components;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace RealtimeCSG
{
    internal sealed class RectangleSelectionManager
    {
        private static Type m_UnitySceneViewType;
        private static Type m_UnityRectSelectionType;
        private static Type m_UnityEnumSelectionType;

        private static object m_SelectionType_Additive;
        private static object m_SelectionType_Subtractive;
        private static object m_SelectionType_Normal;

        private static FieldInfo m_RectSelection_Field;
        private static FieldInfo m_RectSelecting_Field;
        private static FieldInfo m_RectSelectionID_Field;
        private static FieldInfo m_SelectStartPoint_Field;
        private static FieldInfo m_SelectMousePoint_Field;
        private static FieldInfo m_SelectionStart_Field;
        private static FieldInfo m_LastSelection_Field;
        private static FieldInfo m_CurrentSelection_Field;
        private static MethodInfo m_UpdateSelection_Method;

        private static bool m_Initialized;
        private static bool m_ReflectionSucceeded;

        private static void InitReflectedData()
        {
            if(m_Initialized)
            {
                return;
            }

            m_Initialized         = true;
            m_ReflectionSucceeded = false;
            m_UnitySceneViewType  = typeof(SceneView);

            Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
            List<Type> types      = new List<Type>();

            foreach(Assembly assembly in assemblies)
            {
                try
                {
                    types.AddRange(assembly.GetTypes());
                }
                catch
                {
                }
            }

            m_UnityRectSelectionType = types.FirstOrDefault(t => t.FullName == "UnityEditor.RectSelection");

            m_UnityEnumSelectionType =
                types.FirstOrDefault(t => t.FullName == "UnityEditor.RectSelection+SelectionType");

            if(m_UnitySceneViewType != null)
            {
                m_RectSelection_Field = m_UnitySceneViewType.GetField(
                    "m_RectSelection", BindingFlags.NonPublic | BindingFlags.Instance
                );
            }
            else
            {
                m_RectSelection_Field = null;
            }

            if(m_UnityRectSelectionType != null)
            {
                m_RectSelecting_Field = m_UnityRectSelectionType.GetField(
                    "m_RectSelecting", BindingFlags.NonPublic | BindingFlags.Instance
                );

                m_RectSelectionID_Field = m_UnityRectSelectionType.GetField(
                    "s_RectSelectionID", BindingFlags.NonPublic | BindingFlags.Static
                );

                m_SelectStartPoint_Field = m_UnityRectSelectionType.GetField(
                    "m_SelectStartPoint", BindingFlags.NonPublic | BindingFlags.Instance
                );

                m_SelectionStart_Field = m_UnityRectSelectionType.GetField(
                    "m_SelectionStart", BindingFlags.NonPublic | BindingFlags.Instance
                );

                m_LastSelection_Field = m_UnityRectSelectionType.GetField(
                    "m_LastSelection", BindingFlags.NonPublic | BindingFlags.Instance
                );

                m_CurrentSelection_Field = m_UnityRectSelectionType.GetField(
                    "m_CurrentSelection", BindingFlags.NonPublic | BindingFlags.Instance
                );

                m_SelectMousePoint_Field = m_UnityRectSelectionType.GetField(
                    "m_SelectMousePoint", BindingFlags.NonPublic | BindingFlags.Instance
                );

                if(m_UnityEnumSelectionType != null)
                {
                    m_SelectionType_Additive    = Enum.Parse(m_UnityEnumSelectionType, "Additive");
                    m_SelectionType_Subtractive = Enum.Parse(m_UnityEnumSelectionType, "Subtractive");
                    m_SelectionType_Normal      = Enum.Parse(m_UnityEnumSelectionType, "Normal");

                    m_UpdateSelection_Method = m_UnityRectSelectionType.GetMethod(
                        "UpdateSelection", BindingFlags.NonPublic | BindingFlags.Static,
                        null,
                        new[] { typeof(Object[]), typeof(Object[]), m_UnityEnumSelectionType, typeof(bool) },
                        null
                    );
                }
            }

            m_ReflectionSucceeded = m_RectSelectionID_Field != null &&
                m_RectSelection_Field != null &&
                m_RectSelecting_Field != null &&
                m_SelectStartPoint_Field != null &&
                m_SelectMousePoint_Field != null &&
                m_UpdateSelection_Method != null;
        }

        private static readonly HashSet<GameObject> rectFoundGameObjects = new HashSet<GameObject>();
        private static Vector2 m_PrevStartGUIPoint;
        private static Vector2 m_PrevMouseGUIPoint;
        private static Vector2 m_PrevStartScreenPoint;
        private static Vector2 m_PrevMouseScreenPoint;

        private static bool m_RectClickDown;
        private static bool m_MouseDragged;
        private static Vector2 m_ClickMousePosition = MathConstants.zeroVector2;

        private static readonly List<Object> foundObjects = new List<Object>();

        // Update rectangle selection using reflection
        // This is hacky & dangerous
        // LOOK AWAY NOW!
        internal static void Update(SceneView sceneView)
        {
            InitReflectedData();

            if(!m_ReflectionSucceeded)
            {
                m_PrevStartGUIPoint    = new Vector2(float.PositiveInfinity, float.PositiveInfinity);
                m_PrevMouseGUIPoint    = m_PrevStartGUIPoint;
                m_PrevStartScreenPoint = MathConstants.zeroVector2;
                m_PrevMouseScreenPoint = MathConstants.zeroVector2;
                rectFoundGameObjects.Clear();

                return;
            }

            int rectSelectionIDInstance = (int)m_RectSelectionID_Field.GetValue(null);

            // check if we're rect-selecting
            if(GUIUtility.hotControl == rectSelectionIDInstance)
            {
                EventType typeForControl = Event.current.GetTypeForControl(rectSelectionIDInstance);

                if(typeForControl == EventType.Used ||
                   Event.current.commandName == "ModifierKeysChanged")
                {
                    // m_RectSelection field of SceneView
                    object rectSelectionInstance = m_RectSelection_Field.GetValue(sceneView);

                    // m_RectSelecting field of RectSelection instance
                    bool rectSelectingInstance = (bool)m_RectSelecting_Field.GetValue(rectSelectionInstance);

                    if(rectSelectingInstance)
                    {
                        // m_SelectStartPoint of RectSelection instance
                        Vector2 selectStartPointInstance =
                            (Vector2)m_SelectStartPoint_Field.GetValue(rectSelectionInstance);

                        // m_SelectMousePoint of RectSelection instance
                        Vector2 selectMousePointInstance =
                            (Vector2)m_SelectMousePoint_Field.GetValue(rectSelectionInstance);

                        // determine if our frustum changed since the last time
                        bool modified   = false;
                        bool needUpdate = false;

                        if(m_PrevStartGUIPoint != selectStartPointInstance)
                        {
                            m_PrevStartGUIPoint    = selectStartPointInstance;
                            m_PrevStartScreenPoint = Event.current.mousePosition;
                            needUpdate             = true;
                        }

                        if(m_PrevMouseGUIPoint != selectMousePointInstance)
                        {
                            m_PrevMouseGUIPoint    = selectMousePointInstance;
                            m_PrevMouseScreenPoint = Event.current.mousePosition;
                            needUpdate             = true;
                        }

                        if(needUpdate)
                        {
                            Rect rect = CameraUtility.PointsToRect(m_PrevStartScreenPoint, m_PrevMouseScreenPoint);

                            if(rect.width > 3 && rect.height > 3)
                            {
                                Frustum frustum = CameraUtility.GetCameraSubFrustumGUI(sceneView.camera, rect);

                                // Find all the brushes (and it's gameObjects) that are in the frustum
                                if(SceneQueryUtility.GetItemsInFrustum(frustum.Planes, rectFoundGameObjects))
                                {
                                    modified = true;
                                }
                                else
                                {
                                    if(rectFoundGameObjects != null &&
                                       rectFoundGameObjects.Count > 0)
                                    {
                                        rectFoundGameObjects.Clear();
                                        modified = true;
                                    }
                                }
                            }
                        }

                        GameObject[] currentSelection = null;

                        Dictionary<GameObject, bool> lastSelectionInstance =
                            (Dictionary<GameObject, bool>)m_LastSelection_Field.GetValue(rectSelectionInstance);

                        Object[] selectionStartInstance =
                            (Object[])m_SelectionStart_Field.GetValue(rectSelectionInstance);

                        if(modified && rectFoundGameObjects != null && rectFoundGameObjects.Count > 0)
                        {
                            if(EditModeManager.ActiveTool == null)
                            {
                                if(EditModeManager.EditMode != ToolEditMode.Place ||
                                   EditModeManager.EditMode != ToolEditMode.Edit)
                                {
                                    EditModeManager.EditMode = ToolEditMode.Place;
                                }
                            }

                            foreach(GameObject obj in rectFoundGameObjects)
                            {
                                // if it hasn't already been added, add the obj
                                lastSelectionInstance.TryAdd(obj, false);

                                // Remove models that we may have selected when we should be selecting it's brushes
                                CSGModel[] models = obj.GetComponentsInParent<CSGModel>(true);
                                CSGModel   model  = models.Length == 0 ? null : models[0];

                                if(model != null)
                                {
                                    GameObject modelObj = model.gameObject;

                                    if(model != null &&
                                       modelObj != obj &&
                                       lastSelectionInstance.ContainsKey(modelObj) &&
                                       !ArrayUtility.Contains(selectionStartInstance, modelObj))
                                    {
                                        lastSelectionInstance.Remove(modelObj);
                                        modified = true;
                                    }
                                }
                            }

                            currentSelection = lastSelectionInstance.Keys.ToArray();
                            m_CurrentSelection_Field.SetValue(rectSelectionInstance, currentSelection);
                        }

                        for(int j = selectionStartInstance.Length - 1; j >= 0; j--)
                        {
                            GameObject obj = selectionStartInstance[j] as GameObject;

                            if(obj == null)
                            {
                                continue;
                            }

                            if(obj.GetComponent<GeneratedMeshInstance>() != null)
                            {
                                ArrayUtility.RemoveAt(ref selectionStartInstance, j);
                                lastSelectionInstance.Remove(obj);
                                m_SelectionStart_Field.SetValue(rectSelectionInstance, selectionStartInstance);
                                modified = true;
                            }
                        }

                        if(Event.current.commandName == "ModifierKeysChanged" || modified)
                        {
                            if(currentSelection == null || modified)
                            {
                                currentSelection = lastSelectionInstance.Keys.ToArray();
                            }

                            GameObject[] found = currentSelection;

                            for(int j = found.Length - 1; j >= 0; j--)
                            {
                                GameObject obj = found[j];

                                if(obj == null || obj.GetComponent<GeneratedMeshInstance>() != null)
                                {
                                    ArrayUtility.RemoveAt(ref found, j);
                                    lastSelectionInstance.Remove(obj);

                                    m_SelectionStart_Field.SetValue(rectSelectionInstance, selectionStartInstance);
                                }
                            }

                            object selectionTypeNormal = m_SelectionType_Normal;

                            if(Event.current.shift)
                            {
                                selectionTypeNormal = m_SelectionType_Additive;
                            }
                            else if(EditorGUI.actionKey)
                            {
                                selectionTypeNormal = m_SelectionType_Subtractive;
                            }

                            // calling static method UpdateSelection of RectSelection
                            m_UpdateSelection_Method.Invoke(
                                null,
                                new[] { selectionStartInstance, found, selectionTypeNormal, rectSelectingInstance }
                            );
                        }
                    }
                }
            }

            if(GUIUtility.hotControl != rectSelectionIDInstance)
            {
                m_PrevStartGUIPoint = MathConstants.zeroVector2;
                m_PrevMouseGUIPoint = MathConstants.zeroVector2;
                rectFoundGameObjects.Clear();
            }

            EventType eventType = Event.current.GetTypeForControl(rectSelectionIDInstance);

            int hotControl = GUIUtility.hotControl;

            if(hotControl == rectSelectionIDInstance &&
               EditModeManager.ActiveTool.IgnoreUnityRect)
            {
                hotControl            = 0;
                GUIUtility.hotControl = 0;
            }

            switch(eventType)
            {
                case EventType.MouseDown:
                {
                    m_RectClickDown = Event.current.button == 0 && hotControl == rectSelectionIDInstance;
                    Object[] selectedObjects = Selection.objects;
                    foundObjects.Clear();

                    foreach(Object obj in selectedObjects)
                    {
                        Component  component  = obj as Component;
                        GameObject gameObject = obj as GameObject;
                        Transform  transform  = obj as Transform;

                        if(!(component && component.GetComponent<GeneratedMeshes>()) &&
                           !(gameObject && gameObject.GetComponent<GeneratedMeshes>()) &&
                           !(transform && transform.GetComponent<Transform>()))
                        {
                            foundObjects.Add(obj);
                        }
                    }

                    m_ClickMousePosition = Event.current.mousePosition;
                    m_MouseDragged       = false;

                    break;
                }
                case EventType.MouseUp:
                {
                    m_RectClickDown = false;
                    foundObjects.Clear();

                    break;
                }
                case EventType.MouseMove:
                {
                    m_RectClickDown = false;
                    foundObjects.Clear();

                    break;
                }
                case EventType.Used:
                {
                    if(m_ClickMousePosition != Event.current.mousePosition)
                    {
                        m_MouseDragged = true;
                    }

                    if(!m_MouseDragged && m_RectClickDown &&
                       Event.current.button == 0)
                    {
                        // m_RectSelection field of SceneView
                        object rectSelectionInstance = m_RectSelection_Field.GetValue(sceneView);

                        bool rectSelectingInstance = (bool)m_RectSelecting_Field.GetValue(rectSelectionInstance);

                        if(!rectSelectingInstance)
                        {
                            // make sure GeneratedMeshes are not part of our selection
                            if(Selection.gameObjects != null)
                            {
                                Object[] selectedObjects = Selection.objects;

                                if(foundObjects.Count != selectedObjects.Length)
                                {
                                    Selection.objects = foundObjects.ToArray();
                                }
                            }

                            SelectionUtility.DoSelectionClick(sceneView);
                            Event.current.Use();
                        }
                    }

                    m_RectClickDown = false;
                    foundObjects.Clear();

                    break;
                }

                case EventType.ValidateCommand:
                {
                    if(Event.current.commandName == "SelectAll")
                    {
                        Event.current.Use();

                        break;
                    }

                    if(Keys.HandleSceneValidate(EditModeManager.CurrentTool))
                    {
                        Event.current.Use();
                        HandleUtility.Repaint();
                    }

                    break;
                }
                case EventType.ExecuteCommand:
                {
                    if(Event.current.commandName == "SelectAll")
                    {
                        List<Object> transforms = new List<Object>();

                        for(int sceneIndex = 0; sceneIndex < SceneManager.sceneCount; sceneIndex++)
                        {
                            Scene scene = SceneManager.GetSceneAt(sceneIndex);

                            foreach(GameObject gameObject in scene.GetRootGameObjects())
                            {
                                foreach(Transform transform in gameObject.GetComponentsInChildren<Transform>())
                                {
                                    if((transform.hideFlags & (HideFlags.NotEditable | HideFlags.HideInHierarchy))
                                    == (HideFlags.NotEditable | HideFlags.HideInHierarchy))
                                    {
                                        continue;
                                    }

                                    transforms.Add(transform.gameObject);
                                }
                            }
                        }

                        Selection.objects = transforms.ToArray();

                        Event.current.Use();
                    }

                    break;
                }

                case EventType.KeyDown:
                {
                    if(Keys.HandleSceneKeyDown(EditModeManager.CurrentTool))
                    {
                        Event.current.Use();
                        HandleUtility.Repaint();
                    }

                    break;
                }

                case EventType.KeyUp:
                {
                    if(Keys.HandleSceneKeyUp(EditModeManager.CurrentTool))
                    {
                        Event.current.Use();
                        HandleUtility.Repaint();
                    }

                    break;
                }
            }
        }
    }
}
