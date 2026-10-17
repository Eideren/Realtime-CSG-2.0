using System;
using System.Collections.Generic;
using RealtimeCSG.Foundation;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace RealtimeCSG
{
    internal static class CSGEditorGUIUtility
    {
        public static bool PassThroughButton(bool passThrough, bool mixedValues)
        {
            CSGSkin rcsgSkin = CsgguiStyleUtility.Skin;
            Color   oldColor = GUI.color;
            GUI.color = Color.white;

            bool pressed = false;
            GUILayout.BeginVertical();

            {
                GUIContent content;
                GUIStyle   style;

                if(!mixedValues && GUI.enabled && passThrough)
                {
                    content = rcsgSkin.passThroughOn;
                    style   = CsgguiStyleUtility.SelectedIconLabelStyle;
                }
                else
                {
                    content = rcsgSkin.passThrough;
                    style   = CsgguiStyleUtility.UnselectedIconLabelStyle;
                }

                if(GUILayout.Button(content, style))
                {
                    pressed = true;
                }

                TooltipUtility.SetToolTip(CsgguiStyleUtility.passThroughTooltip);
            }

            GUILayout.EndVertical();

            GUI.color = oldColor;

            return pressed;
        }

        public static CSGOperationType ChooseOperation(CSGOperationType operation, bool mixedValues)
        {
            CSGSkin rcsgSkin = CsgguiStyleUtility.Skin;

            if(rcsgSkin == null)
            {
                return operation;
            }

            Color oldColor = GUI.color;
            GUI.color = Color.white;

            GUILayout.BeginVertical();

            try
            {
                bool haveSelection = !mixedValues && GUI.enabled;

                for(int i = 0; i < CsgguiStyleUtility.OPERATION_TYPE_COUNT; i++)
                {
                    GUIContent content;

                    GUIStyle style;

                    if(!haveSelection || (int)operation != i)
                    {
                        content = rcsgSkin.operationNames[i];
                        style   = CsgguiStyleUtility.UnselectedIconLabelStyle;
                    }
                    else
                    {
                        content = rcsgSkin.operationNamesOn[i];
                        style   = CsgguiStyleUtility.SelectedIconLabelStyle;
                    }

                    if(content == null || style == null)
                    {
                        continue;
                    }

                    if(GUILayout.Button(content, style))
                    {
                        operation   = (CSGOperationType)i;
                        GUI.changed = true;
                    }

                    TooltipUtility.SetToolTip(CsgguiStyleUtility.operationTooltip[i]);
                }
            }
            finally
            {
                GUILayout.EndVertical();
            }

            GUI.color = oldColor;

            return operation;
        }

        private static void CalcSize(ref Rect[] rects, out Rect bounds, out int xCount, GUIContent[] contents, float yOffset, float areaWidth = -1)
        {
            if(areaWidth <= 0)
            {
                areaWidth = EditorGUIUtility.currentViewWidth;
            }

            Rect position = new Rect();

            if(rects == null || rects.Length != contents.Length)
            {
                rects = new Rect[contents.Length];
            }

            GUISkin  skin       = GUI.skin;
            GUIStyle buttonSkin = skin.button;

            float textWidth = buttonSkin.CalcSize(contents[0]).x;

            for(int i = 1; i < contents.Length; i++)
            {
                float width = buttonSkin.CalcSize(contents[i]).x;

                if(width > textWidth)
                {
                    textWidth = width;
                }
            }

            RectOffset margin         = buttonSkin.margin;
            RectOffset padding        = buttonSkin.padding;
            int        paddingWidth   = padding.left + padding.right;
            float      minButtonWidth = textWidth + paddingWidth + margin.horizontal;
            float      screenWidth    = areaWidth - margin.horizontal;
            float      countValue     = Mathf.Clamp(screenWidth / minButtonWidth, 1, contents.Length);
            xCount = Mathf.FloorToInt(countValue);

            float realButtonWidth = screenWidth / xCount;

            if(xCount == contents.Length)
            {
                realButtonWidth = screenWidth / countValue;
            }

            position.x      = 0;
            position.y      = yOffset;
            position.width  = realButtonWidth;
            position.height = 15;

            bounds       = new Rect();
            bounds.width = areaWidth;

            xCount--;
            int count = 0;

            while(count < contents.Length)
            {
                position.y++;
                position.x = 2;

                for(int x = 0; x <= xCount; x++)
                {
                    position.x++;

                    rects[count] = position;

                    position.x += realButtonWidth - 1;

                    count++;

                    if(count >= contents.Length)
                    {
                        break;
                    }
                }

                position.y += 16;
            }

            bounds.height = position.y - yOffset;
        }

        public static int ToolbarWrapped(int selected, ref Rect[] rects, out Rect bounds, GUIContent[] contents, ToolTip[] tooltips = null, float yOffset = 0, float areaWidth = -1)
        {
            if(areaWidth <= 0)
            {
                areaWidth = EditorGUIUtility.currentViewWidth;
            }

            CalcSize(ref rects, out bounds, out int xCount, contents, yOffset, areaWidth);

            GUIStyle leftStyle   = EditorStyles.miniButtonLeft;
            GUIStyle middleStyle = EditorStyles.miniButtonMid;
            GUIStyle rightStyle  = EditorStyles.miniButtonRight;
            GUIStyle singleStyle = EditorStyles.miniButton;

            int count = 0;

            while(count < contents.Length)
            {
                int last = Mathf.Min(xCount, contents.Length - 1 - count);

                for(int x = 0; x <= xCount; x++)
                {
                    GUIStyle style = x > 0 ? x < last ? middleStyle : rightStyle : x < last ? leftStyle : singleStyle;

                    if(GUI.Toggle(rects[count], selected == count, contents[count], style))
                    {
                        if(selected != count)
                        {
                            selected    = count;
                            GUI.changed = true;
                        }
                    }

                    if(tooltips != null)
                    {
                        TooltipUtility.SetToolTip(tooltips[count], rects[count]);
                    }

                    count++;

                    if(count >= contents.Length)
                    {
                        break;
                    }
                }
            }

            return selected;
        }

        internal const int MATERIAL_SMALL_SIZE = 48;
        internal const int MATERIAL_LARGE_SIZE = 100;

        [NonSerialized] private static MaterialEditor m_MaterialEditor;

        private static readonly GUILayoutOption materialSmallWidth = GUILayout.Width(MATERIAL_SMALL_SIZE);
        private static readonly GUILayoutOption materialSmallHeight = GUILayout.Height(MATERIAL_SMALL_SIZE);
        private static readonly GUILayoutOption materialLargeWidth = GUILayout.Width(MATERIAL_LARGE_SIZE);
        private static readonly GUILayoutOption materialLargeHeight = GUILayout.Height(MATERIAL_LARGE_SIZE);

        private static Material GetDragMaterial()
        {
            if(DragAndDrop.objectReferences != null && DragAndDrop.objectReferences.Length > 0)
            {
                List<Material> dragMaterials = new List<Material>();

                foreach(Object obj in DragAndDrop.objectReferences)
                {
                    Material dragMaterial = obj as Material;

                    if(dragMaterial == null)
                    {
                        continue;
                    }

                    dragMaterials.Add(dragMaterial);
                }

                if(dragMaterials.Count == 1)
                {
                    return dragMaterials[0];
                }
            }

            return null;
        }

        public static Material MaterialImage(Material material, bool small = true)
        {
            bool showMixedValue = EditorGUI.showMixedValue;
            EditorGUI.showMixedValue = false;

            GUILayoutOption width  = small ? materialSmallWidth : materialLargeWidth;
            GUILayoutOption height = small ? materialSmallHeight : materialLargeHeight;

            GUILayout.BeginHorizontal(CsgguiStyleUtility.EmptyMaterialStyle, width, height);

            Editor editor = m_MaterialEditor;
            Editor.CreateCachedEditor(material, typeof(MaterialEditor), ref editor);
            m_MaterialEditor = editor as MaterialEditor;

            if(m_MaterialEditor)
            {
                Rect rect = GUILayoutUtility.GetRect(small ? MATERIAL_SMALL_SIZE : MATERIAL_LARGE_SIZE, small ? MATERIAL_SMALL_SIZE : MATERIAL_LARGE_SIZE);

                EditorGUI.showMixedValue = showMixedValue;
                m_MaterialEditor.OnPreviewGUI(rect, GUIStyle.none);
                EditorGUI.showMixedValue = false;
            }
            else
            {
                GUILayout.Box(new GUIContent(), CsgguiStyleUtility.EmptyMaterialStyle, width, height);
            }

            GUILayout.EndHorizontal();

            Rect    currentArea  = GUILayoutUtility.GetLastRect();
            Vector2 currentPoint = Event.current.mousePosition;

            if(currentArea.Contains(currentPoint))
            {
                if(Event.current.type == EventType.DragUpdated && GetDragMaterial() != null)
                {
                    DragAndDrop.visualMode = DragAndDropVisualMode.Generic;
                    Event.current.Use();
                }

                if(Event.current.type == EventType.DragPerform)
                {
                    Material newMaterial = GetDragMaterial();

                    if(newMaterial != null)
                    {
                        material    = newMaterial;
                        GUI.changed = true;
                        Event.current.Use();

                        return material;
                    }
                }
            }

            return material;
        }

        public static void RepaintAll()
        {
            SceneView.RepaintAll();
        }

        private static readonly GUIContent vectorXContent = new GUIContent("X");
        private static readonly GUIContent vectorYContent = new GUIContent("Y");
        private static readonly GUIContent vectorZContent = new GUIContent("Z");

        private const float WIDTH_22_VALUE = 22;
        private static readonly GUILayoutOption width22 = GUILayout.Width(WIDTH_22_VALUE);

        private static readonly GUIContent angleUnitLabel = new GUIContent("°");

        public static Vector3 EulerDegreeField(Vector3 value, GUILayoutOption[] options = null)
        {
            bool        modified         = false;
            const float vectorLabelWidth = 12;

            Vector3 realValue          = value;
            float   originalLabelWidth = EditorGUIUtility.labelWidth;
            EditorGUIUtility.labelWidth = vectorLabelWidth;
            GUILayout.BeginHorizontal();

            EditorGUI.BeginChangeCheck();

            value.x = EditorGUILayout.FloatField(vectorXContent, value.x, options);

            if(EditorGUI.EndChangeCheck())
            {
                realValue.x = value.x; // don't want to introduce math errors unless we actually modify something
                modified    = true;
            }

            EditorGUI.BeginChangeCheck();

            value.y = EditorGUILayout.FloatField(vectorYContent, value.y, options);

            if(EditorGUI.EndChangeCheck())
            {
                realValue.y = value.y; // don't want to introduce math errors unless we actually modify something
                modified    = true;
            }

            EditorGUI.BeginChangeCheck();

            value.z = EditorGUILayout.FloatField(vectorZContent, value.z, options);

            if(EditorGUI.EndChangeCheck())
            {
                realValue.z = value.z; // don't want to introduce math errors unless we actually modify something
                modified    = true;
            }

            GUILayout.Label(angleUnitLabel, EditorStyles.miniLabel, width22);

            GUILayout.EndHorizontal();
            EditorGUIUtility.labelWidth = originalLabelWidth;
            GUI.changed                 = modified;

            return realValue;
        }

        public static Vector3 DistanceVector3Field(Vector3 value, bool multiLine, GUILayoutOption[] options = null)
        {
            DistanceUnit distanceUnit = CSGSettings.DistanceUnit;
            DistanceUnit nextUnit     = Units.CycleToNextUnit(distanceUnit);
            GUIContent   unitText     = Units.GetUnitGUIContent(distanceUnit);

            bool modified          = false;
            bool clickedUnitButton = false;

            float areaWidth = EditorGUIUtility.currentViewWidth;

            const float minWidth         = 65;
            const float vectorLabelWidth = 12;

            const float allWidth = 12 * 3 + WIDTH_22_VALUE * 3 + minWidth * 3;

            Vector3 realValue = value;
            multiLine = multiLine || allWidth >= areaWidth;

            if(multiLine)
            {
                GUILayout.BeginVertical();
            }

            float originalLabelWidth = EditorGUIUtility.labelWidth;
            EditorGUIUtility.labelWidth = vectorLabelWidth;
            GUILayout.BeginHorizontal();

            EditorGUI.BeginChangeCheck();

            value.x = Units.DistanceUnitToUnity(distanceUnit, EditorGUILayout.DoubleField(vectorXContent, Units.UnityToDistanceUnit(distanceUnit, value.x), options));

            if(EditorGUI.EndChangeCheck())
            {
                realValue.x = value.x; // don't want to introduce math errors unless we actually modify something
                modified    = true;
            }

            if(multiLine)
            {
                clickedUnitButton = GUILayout.Button(unitText, EditorStyles.miniLabel, width22) || clickedUnitButton;
            }

            if(multiLine)
            {
                GUILayout.EndHorizontal();
                GUILayout.BeginHorizontal();
            }

            EditorGUI.BeginChangeCheck();

            value.y = Units.DistanceUnitToUnity(distanceUnit, EditorGUILayout.DoubleField(vectorYContent, Units.UnityToDistanceUnit(distanceUnit, value.y), options));

            if(EditorGUI.EndChangeCheck())
            {
                realValue.y = value.y; // don't want to introduce math errors unless we actually modify something
                modified    = true;
            }

            if(multiLine)
            {
                clickedUnitButton = GUILayout.Button(unitText, EditorStyles.miniLabel, width22) || clickedUnitButton;
            }

            if(multiLine)
            {
                GUILayout.EndHorizontal();
                GUILayout.BeginHorizontal();
            }

            EditorGUI.BeginChangeCheck();

            value.z = Units.DistanceUnitToUnity(distanceUnit, EditorGUILayout.DoubleField(vectorZContent, Units.UnityToDistanceUnit(distanceUnit, value.z), options));

            if(EditorGUI.EndChangeCheck())
            {
                realValue.z = value.z; // don't want to introduce math errors unless we actually modify something
                modified    = true;
            }

            clickedUnitButton = GUILayout.Button(unitText, EditorStyles.miniLabel, width22) || clickedUnitButton;

            GUILayout.EndHorizontal();
            EditorGUIUtility.labelWidth = originalLabelWidth;

            if(multiLine)
            {
                GUILayout.EndVertical();
            }

            if(clickedUnitButton)
            {
                distanceUnit             = nextUnit;
                CSGSettings.DistanceUnit = distanceUnit;

                CSGSettings.UpdateSnapSettings();
                CSGSettings.Save();
                RepaintAll();
            }

            GUI.changed = modified;

            return realValue;
        }
    }
}
