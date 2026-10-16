using System;
using System.Collections.Generic;
using InternalRealtimeCSG;
using RealtimeCSG.Components;
using RealtimeCSG.Legacy;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace RealtimeCSG
{
    internal sealed partial class EditModeSurfaceGUI
    {
        private static readonly int sceneViewSurfaceOverlayHash = "SceneViewSurfaceOverlay".GetHashCode();

        [NonSerialized]
        private static MaterialEditor m_MaterialEditor;

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

        private static void OnGUIContentsJustify(bool isSceneGUI, SelectedBrushSurface[] selectedBrushSurfaces)
        {
            GUILayout.BeginVertical(CsgguiStyleUtility.contentEmpty);

            if(!isSceneGUI)
            {
                GUILayout.BeginHorizontal(CsgguiStyleUtility.contentEmpty);
                EditorGUILayout.LabelField(contentJustifyLabel, largeLabelWidth);
            }
            else
            {
                GUILayout.Label(contentJustifyLabel);
            }

            GUILayout.BeginVertical(CsgguiStyleUtility.contentEmpty);

            GUILayout.BeginHorizontal(CsgguiStyleUtility.contentEmpty);

            if(GUILayout.Button(contentJustifyUpLeft, justifyButtonLayout))
            {
                SurfaceUtility.JustifyLayout(selectedBrushSurfaces, -1, -1);
            }

            TooltipUtility.SetToolTip(toolTipJustifyUpLeft);

            if(GUILayout.Button(contentJustifyUp, justifyButtonLayout))
            {
                SurfaceUtility.JustifyLayoutY(selectedBrushSurfaces, -1);
            }

            TooltipUtility.SetToolTip(toolTipJustifyUp);

            if(GUILayout.Button(contentJustifyUpRight, justifyButtonLayout))
            {
                SurfaceUtility.JustifyLayout(selectedBrushSurfaces, 1, -1);
            }

            TooltipUtility.SetToolTip(toolTipJustifyUpRight);

            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal(CsgguiStyleUtility.contentEmpty);

            if(GUILayout.Button(contentJustifyLeft, justifyButtonLayout))
            {
                SurfaceUtility.JustifyLayoutX(selectedBrushSurfaces, -1);
            }

            TooltipUtility.SetToolTip(toolTipJustifyLeft);

            if(GUILayout.Button(contentJustifyCenter, justifyButtonLayout))
            {
                SurfaceUtility.JustifyLayout(selectedBrushSurfaces, 0, 0);
            }

            TooltipUtility.SetToolTip(toolTipJustifyCenter);

            if(GUILayout.Button(contentJustifyRight, justifyButtonLayout))
            {
                SurfaceUtility.JustifyLayoutX(selectedBrushSurfaces, 1);
            }

            TooltipUtility.SetToolTip(toolTipJustifyRight);

            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal(CsgguiStyleUtility.contentEmpty);

            if(GUILayout.Button(contentJustifyDownLeft, justifyButtonLayout))
            {
                SurfaceUtility.JustifyLayout(selectedBrushSurfaces, -1, 1);
            }

            TooltipUtility.SetToolTip(toolTipJustifyDownLeft);

            if(GUILayout.Button(contentJustifyDown, justifyButtonLayout))
            {
                SurfaceUtility.JustifyLayoutY(selectedBrushSurfaces, 1);
            }

            TooltipUtility.SetToolTip(toolTipJustifyDown);

            if(GUILayout.Button(contentJustifyDownRight, justifyButtonLayout))
            {
                SurfaceUtility.JustifyLayout(selectedBrushSurfaces, 1, 1);
            }

            TooltipUtility.SetToolTip(toolTipJustifyDownRight);

            GUILayout.EndHorizontal();

            GUILayout.EndVertical();

            if(!isSceneGUI)
            {
                GUILayout.EndHorizontal();
            }

            GUILayout.EndVertical();
        }

        private static void OnGUIContentsJustifyScene(Rect rect, SelectedBrushSurface[] selectedBrushSurfaces)
        {
            Rect tempRect = rect;
            tempRect.x      = rect.x + 4;
            tempRect.y      = rect.y + 1;
            tempRect.width  = 213;
            tempRect.height = 16;
            GUI.Label(tempRect, contentJustifyLabel);

            tempRect.width  = 20;
            tempRect.height = 23;

            tempRect.y = rect.y + 20;

            tempRect.x = rect.x + 4;

            if(GUI.Button(tempRect, contentJustifyUpLeft))
            {
                SurfaceUtility.JustifyLayout(selectedBrushSurfaces, -1, -1);
            }

            TooltipUtility.SetToolTip(toolTipJustifyUpLeft, tempRect);

            tempRect.x = rect.x + 30;

            if(GUI.Button(tempRect, contentJustifyUp))
            {
                SurfaceUtility.JustifyLayoutY(selectedBrushSurfaces, -1);
            }

            TooltipUtility.SetToolTip(toolTipJustifyUp, tempRect);

            tempRect.x = rect.x + 56;

            if(GUI.Button(tempRect, contentJustifyUpRight))
            {
                SurfaceUtility.JustifyLayout(selectedBrushSurfaces, 1, -1);
            }

            TooltipUtility.SetToolTip(toolTipJustifyUpRight, tempRect);

            tempRect.y = rect.y + 45;

            tempRect.x = rect.x + 4;

            if(GUI.Button(tempRect, contentJustifyLeft))
            {
                SurfaceUtility.JustifyLayoutX(selectedBrushSurfaces, -1);
            }

            TooltipUtility.SetToolTip(toolTipJustifyLeft, tempRect);

            tempRect.x = rect.x + 30;

            if(GUI.Button(tempRect, contentJustifyCenter))
            {
                SurfaceUtility.JustifyLayout(selectedBrushSurfaces, 0, 0);
            }

            TooltipUtility.SetToolTip(toolTipJustifyCenter, tempRect);

            tempRect.x = rect.x + 56;

            if(GUI.Button(tempRect, contentJustifyRight))
            {
                SurfaceUtility.JustifyLayoutX(selectedBrushSurfaces, 1);
            }

            TooltipUtility.SetToolTip(toolTipJustifyRight, tempRect);

            tempRect.y = rect.y + 70;

            tempRect.x = rect.x + 4;

            if(GUI.Button(tempRect, contentJustifyDownLeft))
            {
                SurfaceUtility.JustifyLayout(selectedBrushSurfaces, -1, 1);
            }

            TooltipUtility.SetToolTip(toolTipJustifyDownLeft, tempRect);

            tempRect.x = rect.x + 30;

            if(GUI.Button(tempRect, contentJustifyDown))
            {
                SurfaceUtility.JustifyLayoutY(selectedBrushSurfaces, 1);
            }

            TooltipUtility.SetToolTip(toolTipJustifyDown, tempRect);

            tempRect.x = rect.x + 56;

            if(GUI.Button(tempRect, contentJustifyDownRight))
            {
                SurfaceUtility.JustifyLayout(selectedBrushSurfaces, 1, 1);
            }

            TooltipUtility.SetToolTip(toolTipJustifyDownRight, tempRect);
        }

        private static void OnGUIContentsMaterialImage(Material material, bool mixedValues, SelectedBrushSurface[] selectedBrushSurfaces)
        {
            GUILayout.BeginHorizontal(materialWidth, materialHeight);

            Editor editor = m_MaterialEditor;
            Editor.CreateCachedEditor(material, typeof(MaterialEditor), ref editor);
            m_MaterialEditor = editor as MaterialEditor;

            if(m_MaterialEditor != null)
            {
                Rect rect = GUILayoutUtility.GetRect(MATERIAL_SIZE, MATERIAL_SIZE);
                EditorGUI.showMixedValue = mixedValues;
                m_MaterialEditor.OnPreviewGUI(rect, GUIStyle.none);
                EditorGUI.showMixedValue = false;
            }
            else
            {
                GUILayout.Box(new GUIContent(), CsgguiStyleUtility.EmptyMaterialStyle, materialWidth, materialHeight);
            }

            GUILayout.EndHorizontal();
            Rect    currentArea  = GUILayoutUtility.GetLastRect();
            Vector2 currentPoint = Event.current.mousePosition;

            if(currentArea.Contains(currentPoint))
            {
                if(Event.current.type == EventType.DragUpdated && GetDragMaterial())
                {
                    DragAndDrop.visualMode = DragAndDropVisualMode.Generic;
                    Event.current.Use();
                }

                if(Event.current.type == EventType.DragPerform)
                {
                    Material newMaterial = GetDragMaterial();

                    if(newMaterial)
                    {
                        SurfaceUtility.SetMaterials(selectedBrushSurfaces, newMaterial);
                        CSGSettings.DefaultMaterial = newMaterial;
                        CSGSettings.Save();
                        GUI.changed = true;
                        Event.current.Use();
                    }
                }
            }
        }

        private static void OnGUIContentsMaterialImage(Rect currentArea, Material material, bool mixedValues, SelectedBrushSurface[] selectedBrushSurfaces)
        {
            Editor editor = m_MaterialEditor;
            Editor.CreateCachedEditor(material, typeof(MaterialEditor), ref editor);
            m_MaterialEditor = editor as MaterialEditor;

            if(m_MaterialEditor != null)
            {
                EditorGUI.showMixedValue = mixedValues;
                m_MaterialEditor.OnPreviewGUI(currentArea, GUIStyle.none);
                EditorGUI.showMixedValue = false;
            }
            else
            {
                GUI.Box(currentArea, GUIContent.none, CsgguiStyleUtility.EmptyMaterialStyle);
            }

            Vector2 currentPoint = Event.current.mousePosition;

            if(currentArea.Contains(currentPoint))
            {
                if(Event.current.type == EventType.DragUpdated && GetDragMaterial())
                {
                    DragAndDrop.visualMode = DragAndDropVisualMode.Generic;
                    Event.current.Use();
                }

                if(Event.current.type == EventType.DragPerform)
                {
                    Material newMaterial = GetDragMaterial();

                    if(newMaterial)
                    {
                        SurfaceUtility.SetMaterials(selectedBrushSurfaces, newMaterial);
                        CSGSettings.DefaultMaterial = newMaterial;
                        CSGSettings.Save();
                        GUI.changed = true;
                        Event.current.Use();
                    }
                }
            }
        }

        private sealed class SurfaceState
        {
            public Material material;
            public TexGen currentTexGen;

            public bool haveTexgen;

            public bool multipleTranslationX;
            public bool multipleTranslationY;
            public bool multipleScaleX;
            public bool multipleScaleY;
            public bool multipleRotationAngle;
            public bool multipleMaterials;
            public bool? textureLocked;

            public bool foundHelperMaterial;

            public RenderSurfaceType? firstRenderSurfaceType;
            public Material firstMaterial;

            public bool enabled;
            public bool canSmooth;
            public bool canUnSmooth;
            public SelectedBrushSurface[] selectedBrushSurfaces;

            public readonly EditModeCommonGUI.SurfaceFlagState surfaceFlagState = new EditModeCommonGUI.SurfaceFlagState();

            public void Init(EditModeSurface tool)
            {
                selectedBrushSurfaces = tool == null ? Array.Empty<SelectedBrushSurface>() : tool.GetSelectedSurfaces();
                enabled               = selectedBrushSurfaces.Length > 0;
                material              = null;
                currentTexGen         = new TexGen();

                surfaceFlagState.Init(m_SurfaceState.selectedBrushSurfaces);

                haveTexgen = false;

                multipleTranslationX  = !enabled;
                multipleTranslationY  = !enabled;
                multipleScaleX        = !enabled;
                multipleScaleY        = !enabled;
                multipleRotationAngle = !enabled;
                multipleMaterials     = !enabled;
                textureLocked         = null;

                foundHelperMaterial    = false;
                firstRenderSurfaceType = null;
                firstMaterial          = null;

                if(selectedBrushSurfaces.Length > 0)
                {
                    for(int i = 0; i < selectedBrushSurfaces.Length; i++)
                    {
                        CSGBrush brush = selectedBrushSurfaces[i].brush;

                        if(!brush)
                        {
                            continue;
                        }

                        int surfaceIndex = selectedBrushSurfaces[i].surfaceIndex;

                        if(surfaceIndex >= brush.shape.surfaces.Length)
                        {
                            Debug.LogWarning("surface_index >= brush.Shape.Surfaces.Length");

                            continue;
                        }

                        int texGenIndex = brush.shape.surfaces[surfaceIndex].TexGenIndex;

                        if(texGenIndex >= brush.shape.texGens.Length)
                        {
                            Debug.LogWarning("texGen_index >= brush.Shape.TexGens.Length");

                            continue;
                        }

                        CSGModel model = brush.childData.model;

                        Material    foundMaterial;
                        TexGenFlags texGenFlags = brush.shape.texGenFlags[texGenIndex];

                        if(model && !model.IsRenderable)
                        {
                            foundHelperMaterial = true;

                            if(!firstRenderSurfaceType.HasValue)
                            {
                                firstRenderSurfaceType = ModelTraits.GetModelSurfaceType(model);
                            }

                            foundMaterial = null;
                        }
                        else if((texGenFlags & TexGenFlags.NoRender) == TexGenFlags.NoRender)
                        {
                            foundHelperMaterial = true;

                            if(!firstRenderSurfaceType.HasValue)
                            {
                                if((texGenFlags & TexGenFlags.NoCastShadows) != TexGenFlags.NoCastShadows)
                                {
                                    firstRenderSurfaceType = RenderSurfaceType.ShadowOnly;
                                }
                                else if((texGenFlags & TexGenFlags.NoCollision) != TexGenFlags.NoCollision)
                                {
                                    firstRenderSurfaceType = RenderSurfaceType.Collider;
                                }
                                else
                                {
                                    firstRenderSurfaceType = RenderSurfaceType.Hidden;
                                }
                            }

                            foundMaterial = null;
                        }
                        else
                        {
                            Material surfaceMaterial = brush.shape.texGens[texGenIndex].RenderMaterial;

                            if(!foundHelperMaterial)
                            {
                                RenderSurfaceType surfaceType = MaterialUtility.GetMaterialSurfaceType(surfaceMaterial);

                                if(!firstRenderSurfaceType.HasValue)
                                {
                                    firstRenderSurfaceType = surfaceType;
                                }

                                foundHelperMaterial = surfaceType != RenderSurfaceType.Normal;
                            }

                            foundMaterial = surfaceMaterial;
                        }

                        if((texGenFlags & TexGenFlags.WorldSpaceTexture) == TexGenFlags.WorldSpaceTexture)
                        {
                            if(i == 0)
                            {
                                textureLocked = false;
                            }
                            else if(textureLocked.HasValue && textureLocked.Value)
                            {
                                textureLocked = null;
                            }
                        }
                        else
                        {
                            if(i == 0)
                            {
                                textureLocked = true;
                            }
                            else if(textureLocked.HasValue && !textureLocked.Value)
                            {
                                textureLocked = null;
                            }
                        }

                        if(foundMaterial != material)
                        {
                            if(!material)
                            {
                                firstMaterial = foundMaterial;
                                material      = foundMaterial;
                            }
                            else
                            {
                                multipleMaterials = true;
                            }
                        }

                        if(!haveTexgen)
                        {
                            currentTexGen = brush.shape.texGens[texGenIndex];
                            haveTexgen    = true;
                        }
                        else
                        {
                            if(!multipleScaleX || !multipleScaleY)
                            {
                                Vector2 scale = brush.shape.texGens[texGenIndex].Scale;
                                multipleScaleX = multipleScaleX || currentTexGen.Scale.x != scale.x;
                                multipleScaleY = multipleScaleY || currentTexGen.Scale.y != scale.y;
                            }

                            if(!multipleTranslationX || !multipleTranslationY)
                            {
                                Vector2 translation = brush.shape.texGens[texGenIndex].Translation;

                                multipleTranslationX = multipleTranslationX || currentTexGen.Translation.x != translation.x;
                                multipleTranslationY = multipleTranslationY || currentTexGen.Translation.y != translation.y;
                            }

                            if(!multipleRotationAngle)
                            {
                                float rotationAngle = brush.shape.texGens[texGenIndex].RotationAngle;
                                multipleRotationAngle = currentTexGen.RotationAngle != rotationAngle;
                            }
                        }
                    }

                    if(foundHelperMaterial && !firstMaterial)
                    {
                        firstMaterial = firstRenderSurfaceType.HasValue ? MaterialUtility.GetSurfaceMaterial(firstRenderSurfaceType.Value) : MaterialUtility.HiddenMaterial;
                    }
                }

                if(currentTexGen.Scale.x == 0.0f)
                {
                    currentTexGen.Scale.x = 1.0f;
                }

                if(currentTexGen.Scale.y == 0.0f)
                {
                    currentTexGen.Scale.y = 1.0f;
                }

                const float scaleRound = 10000.0f;
                currentTexGen.Scale.x       = Mathf.RoundToInt(currentTexGen.Scale.x * scaleRound) / scaleRound;
                currentTexGen.Scale.y       = Mathf.RoundToInt(currentTexGen.Scale.y * scaleRound) / scaleRound;
                currentTexGen.Translation.x = Mathf.RoundToInt(currentTexGen.Translation.x * scaleRound) / scaleRound;
                currentTexGen.Translation.y = Mathf.RoundToInt(currentTexGen.Translation.y * scaleRound) / scaleRound;
                currentTexGen.RotationAngle = Mathf.RoundToInt(currentTexGen.RotationAngle * scaleRound) / scaleRound;

                canSmooth   = SurfaceUtility.CanSmooth(m_SurfaceState.selectedBrushSurfaces);
                canUnSmooth = SurfaceUtility.CanUnSmooth(m_SurfaceState.selectedBrushSurfaces);
            }
        }

        private static SurfaceState m_SurfaceState;
        private const int LIGHT_MAP_UV_BUTTON_OFFSET = 20;
        private const int PRESET_ROW_OFFSET = 18;

        private const int BOX_OFFSET = 4;
        private const int BOX_WIDTH = 213;

        private const int MATERIAL_VIEW_BOX_HEIGHT = 134;
        private const int TEXTURE_LOCKED_BOX_HEIGHT = 22;
        private const int SELECTION_BOX_HEIGHT = 22;
        private const int TRANSFORMATION_BOX_HEIGHT = 58;
        private const int UV_COMMANDS_BOX_HEIGHT = 76 + PRESET_ROW_OFFSET;
        private const int SURFACE_FLAGS_BOX_HEIGHT = 56;

        private const int SURFACE_EDIT_TITLE_HEIGHT = 16;

        internal const int SURFACE_EDIT_WINDOW_HEIGHT = SURFACE_EDIT_TITLE_HEIGHT
          + MATERIAL_VIEW_BOX_HEIGHT + BOX_OFFSET
          + TEXTURE_LOCKED_BOX_HEIGHT + BOX_OFFSET
          + SELECTION_BOX_HEIGHT + BOX_OFFSET
          + TRANSFORMATION_BOX_HEIGHT + BOX_OFFSET
          + UV_COMMANDS_BOX_HEIGHT + BOX_OFFSET
          + SURFACE_FLAGS_BOX_HEIGHT + BOX_OFFSET
          + 13;

        private static void OnGUIContents(Rect rect, EditModeSurface tool, bool needLightmapUVUpdate)
        {
            if(Event.current.type == EventType.Layout)
            {
                m_SurfaceState = new SurfaceState();
                m_SurfaceState.Init(tool);
            }

            if(m_SurfaceState == null)
            {
                return;
            }

            GUIStyle leftStyle   = EditorStyles.miniButtonLeft;
            GUIStyle middleStyle = EditorStyles.miniButtonMid;
            GUIStyle rightStyle  = EditorStyles.miniButtonRight;

            int uvButtonOffset = needLightmapUVUpdate ? LIGHT_MAP_UV_BUTTON_OFFSET : 0;

            Rect tempRect = rect;
            int  offset   = 0;

            tempRect.Set(rect.x + 4, rect.y + offset, BOX_WIDTH, MATERIAL_VIEW_BOX_HEIGHT + uvButtonOffset);

            GUI.Box(tempRect, GUIContent.none, GUI.skin.box);

            if(needLightmapUVUpdate)
            {
                tempRect.Set(rect.x + 8, rect.y + offset + 4, 204, 16);
                EditModeCommonGUI.UpdateLightmapUVButton(ref tempRect);
                rect.y += uvButtonOffset;
            }

            EditorGUI.BeginDisabledGroup(!m_SurfaceState.enabled);

            EditorGUI.BeginChangeCheck();

            EditorGUI.showMixedValue = m_SurfaceState.multipleMaterials;
            tempRect.Set(rect.x + 8, rect.y + offset + 4, 204, 16);

            Material newMaterial = EditorGUI.ObjectField(tempRect, m_SurfaceState.material, typeof(Material), true) as Material;

            EditorGUI.showMixedValue = false;

            if(EditorGUI.EndChangeCheck())
            {
                if(newMaterial)
                {
                    SurfaceUtility.SetMaterials(m_SurfaceState.selectedBrushSurfaces, newMaterial);
                    CSGSettings.DefaultMaterial = newMaterial;
                    CSGSettings.Save();
                }
            }

            tempRect.Set(rect.x + 9, rect.y + offset + 26, 100, 100);

            OnGUIContentsMaterialImage(tempRect, m_SurfaceState.firstMaterial, m_SurfaceState.multipleMaterials, m_SurfaceState.selectedBrushSurfaces);

            tempRect.Set(rect.x + 131 - 9, rect.y + offset + 28, 74, 91);
            OnGUIContentsJustifyScene(tempRect, m_SurfaceState.selectedBrushSurfaces);

            rect.y -= uvButtonOffset;

            EditorGUI.EndDisabledGroup();
            offset += MATERIAL_VIEW_BOX_HEIGHT + uvButtonOffset;

            EditorGUI.BeginDisabledGroup(!m_SurfaceState.enabled);

            offset += BOX_OFFSET;
            tempRect.Set(rect.x + 4, rect.y + offset, BOX_WIDTH, TEXTURE_LOCKED_BOX_HEIGHT); //138
            GUI.Box(tempRect, GUIContent.none, GUI.skin.box);

            EditorGUI.BeginChangeCheck();

            EditorGUI.showMixedValue = !m_SurfaceState.textureLocked.HasValue;
            tempRect.Set(rect.x + 8, rect.y + offset + 3, 205, 16);

            m_SurfaceState.textureLocked = EditorGUI.ToggleLeft(tempRect, contentLockTexture, m_SurfaceState.textureLocked.HasValue && m_SurfaceState.textureLocked.Value);

            TooltipUtility.SetToolTip(toolTipLockTexture, tempRect);

            if(EditorGUI.EndChangeCheck())
            {
                SurfaceUtility.SetTextureLock(m_SurfaceState.selectedBrushSurfaces, m_SurfaceState.textureLocked.Value);
            }

            offset += TEXTURE_LOCKED_BOX_HEIGHT;

            offset += BOX_OFFSET;
            tempRect.Set(rect.x + 4, rect.y + offset, BOX_WIDTH, SELECTION_BOX_HEIGHT);
            GUI.Box(tempRect, GUIContent.none, GUI.skin.box);

            tempRect.Set(rect.x + 8, rect.y + offset + 3, 205, 16);
            bool clicked = GUI.Button(tempRect, contentSelectAllSurfaces);
            TooltipUtility.SetToolTip(tooltipSelectAllSurfaces, tempRect);

            if(clicked)
            {
                tool.SelectAllSurfacesOfSelectedBrushes();
            }

            offset += SELECTION_BOX_HEIGHT;
            offset += BOX_OFFSET;

            tempRect.Set(rect.x + 4, rect.y + offset, BOX_WIDTH, TRANSFORMATION_BOX_HEIGHT); //164
            GUI.Box(tempRect, GUIContent.none, GUI.skin.box);

            tempRect.Set(rect.x + 8, rect.y + offset + 3, 56, 16);
            EditorGUI.LabelField(tempRect, contentUVScale, EditorStyles.miniLabel);
            TooltipUtility.SetToolTip(toolTipScaleUV, tempRect);

            EditorGUI.BeginChangeCheck();

            EditorGUI.showMixedValue = m_SurfaceState.multipleScaleX;
            tempRect.Set(rect.x + 68, rect.y + offset + 3, 70, 16);

            m_SurfaceState.currentTexGen.Scale.x = EditorGUI.FloatField(tempRect, m_SurfaceState.currentTexGen.Scale.x);

            if(EditorGUI.EndChangeCheck())
            {
                SurfaceUtility.SetScaleX(m_SurfaceState.selectedBrushSurfaces, m_SurfaceState.currentTexGen.Scale.x);
            }

            EditorGUI.BeginChangeCheck();

            EditorGUI.showMixedValue = m_SurfaceState.multipleScaleY;
            tempRect.Set(rect.x + 142, rect.y + offset + 3, 70, 16);

            m_SurfaceState.currentTexGen.Scale.y = EditorGUI.FloatField(tempRect, m_SurfaceState.currentTexGen.Scale.y);

            if(EditorGUI.EndChangeCheck())
            {
                SurfaceUtility.SetScaleY(m_SurfaceState.selectedBrushSurfaces, m_SurfaceState.currentTexGen.Scale.y);
            }

            tempRect.Set(rect.x + 8, rect.y + offset + 21, 56, 16);
            EditorGUI.LabelField(tempRect, contentOffset, EditorStyles.miniLabel);
            TooltipUtility.SetToolTip(toolTipOffsetUV, tempRect);

            EditorGUI.BeginChangeCheck();

            EditorGUI.showMixedValue = m_SurfaceState.multipleTranslationX;
            tempRect.Set(rect.x + 68, rect.y + offset + 21, 70, 16);

            m_SurfaceState.currentTexGen.Translation.x = EditorGUI.FloatField(tempRect, m_SurfaceState.currentTexGen.Translation.x);

            if(EditorGUI.EndChangeCheck())
            {
                SurfaceUtility.SetTranslationX(m_SurfaceState.selectedBrushSurfaces, m_SurfaceState.currentTexGen.Translation.x);
            }

            EditorGUI.BeginChangeCheck();

            EditorGUI.showMixedValue = m_SurfaceState.multipleTranslationY;
            tempRect.Set(rect.x + 142, rect.y + offset + 21, 70, 16);

            m_SurfaceState.currentTexGen.Translation.y = EditorGUI.FloatField(tempRect, m_SurfaceState.currentTexGen.Translation.y);

            if(EditorGUI.EndChangeCheck())
            {
                SurfaceUtility.SetTranslationY(m_SurfaceState.selectedBrushSurfaces, m_SurfaceState.currentTexGen.Translation.y);
            }

            tempRect.Set(rect.x + 8, rect.y + offset + 39, 56, 16);
            EditorGUI.LabelField(tempRect, contentRotate, EditorStyles.miniLabel);
            TooltipUtility.SetToolTip(toolTipRotation, tempRect);

            EditorGUI.BeginChangeCheck();

            EditorGUI.showMixedValue = m_SurfaceState.multipleRotationAngle;
            tempRect.Set(rect.x + 68, rect.y + offset + 39, 57, 16);

            m_SurfaceState.currentTexGen.RotationAngle = EditorGUI.FloatField(tempRect, m_SurfaceState.currentTexGen.RotationAngle);

            if(EditorGUI.EndChangeCheck())
            {
                SurfaceUtility.SetRotationAngle(m_SurfaceState.selectedBrushSurfaces, m_SurfaceState.currentTexGen.RotationAngle);
            }

            tempRect.Set(rect.x + 129, rect.y + offset + 39, 42, 15);

            if(GUI.Button(tempRect, contentRotate90Negative, leftStyle))
            {
                SurfaceUtility.AddRotationAngle(m_SurfaceState.selectedBrushSurfaces, -90.0f);
            }

            TooltipUtility.SetToolTip(toolTipRotate90Negative, tempRect);

            tempRect.x = rect.x + 171;

            if(GUI.Button(tempRect, contentRotate90Positive, rightStyle))
            {
                SurfaceUtility.AddRotationAngle(m_SurfaceState.selectedBrushSurfaces, +90.0f);
            }

            TooltipUtility.SetToolTip(toolTipRotate90Positive, tempRect);

            offset += TRANSFORMATION_BOX_HEIGHT;

            offset += BOX_OFFSET;
            tempRect.Set(rect.x + 4, rect.y + offset, BOX_WIDTH, UV_COMMANDS_BOX_HEIGHT);
            GUI.Box(tempRect, GUIContent.none, GUI.skin.box);

            tempRect.Set(rect.x + 8, rect.y + offset + 3, 45, 16);
            GUI.Label(tempRect, contentFit, EditorStyles.miniLabel);

            tempRect.Set(rect.x + 57, rect.y + offset + 3, 49, 15);

            if(GUI.Button(tempRect, contentFitX, leftStyle))
            {
                SurfaceUtility.FitSurfaceX(m_SurfaceState.selectedBrushSurfaces);
            }

            TooltipUtility.SetToolTip(toolTipFitX, tempRect);

            tempRect.Set(rect.x + 106, rect.y + offset + 3, 57, 15);

            if(GUI.Button(tempRect, contentFitXY, middleStyle))
            {
                SurfaceUtility.FitSurface(m_SurfaceState.selectedBrushSurfaces);
            }

            TooltipUtility.SetToolTip(toolTipFitXY, tempRect);

            tempRect.Set(rect.x + 164, rect.y + offset + 3, 49, 15);

            if(GUI.Button(tempRect, contentFitY, rightStyle))
            {
                SurfaceUtility.FitSurfaceY(m_SurfaceState.selectedBrushSurfaces);
            }

            TooltipUtility.SetToolTip(toolTipFitY, tempRect);

            tempRect.Set(rect.x + 8, rect.y + offset + 21, 45, 16);
            GUI.Label(tempRect, contentReset, EditorStyles.miniLabel);

            tempRect.Set(rect.x + 57, rect.y + offset + 21, 50, 15);

            if(GUI.Button(tempRect, contentResetX, leftStyle))
            {
                SurfaceUtility.ResetSurfaceX(m_SurfaceState.selectedBrushSurfaces);
            }

            TooltipUtility.SetToolTip(toolTipResetX, tempRect);

            tempRect.Set(rect.x + 107, rect.y + offset + 21, 58, 15);

            if(GUI.Button(tempRect, contentResetXY, rightStyle))
            {
                SurfaceUtility.ResetSurface(m_SurfaceState.selectedBrushSurfaces);
            }

            TooltipUtility.SetToolTip(toolTipResetXY, tempRect);

            tempRect.Set(rect.x + 164, rect.y + offset + 21, 49, 15);

            if(GUI.Button(tempRect, contentResetY, rightStyle))
            {
                SurfaceUtility.ResetSurfaceY(m_SurfaceState.selectedBrushSurfaces);
            }

            TooltipUtility.SetToolTip(toolTipResetY, tempRect);

            tempRect.Set(rect.x + 8, rect.y + offset + 39, 45, 16);
            GUI.Label(tempRect, contentFlip, EditorStyles.miniLabel);

            tempRect.Set(rect.x + 57, rect.y + offset + 39, 49, 15);

            if(GUI.Button(tempRect, contentFlipX, leftStyle))
            {
                SurfaceUtility.FlipX(m_SurfaceState.selectedBrushSurfaces);
            }

            TooltipUtility.SetToolTip(toolTipFlipX, tempRect);

            tempRect.Set(rect.x + 106, rect.y + offset + 39, 57, 15);

            if(GUI.Button(tempRect, contentFlipXY, middleStyle))
            {
                SurfaceUtility.FlipXY(m_SurfaceState.selectedBrushSurfaces);
            }

            TooltipUtility.SetToolTip(toolTipFlipXY, tempRect);

            tempRect.Set(rect.x + 164, rect.y + offset + 39, 49, 15);

            if(GUI.Button(tempRect, contentFlipY, rightStyle))
            {
                SurfaceUtility.FlipY(m_SurfaceState.selectedBrushSurfaces);
            }

            TooltipUtility.SetToolTip(toolTipFlipY, tempRect);

            tempRect.Set(rect.x + 8, rect.y + offset + 57, 45, 16);
            GUI.Label(tempRect, contentScale, EditorStyles.miniLabel);

            tempRect.Set(rect.x + 57, rect.y + offset + 57, 79, 15);

            if(GUI.Button(tempRect, contentDoubleScale, leftStyle))
            {
                SurfaceUtility.MultiplyScale(m_SurfaceState.selectedBrushSurfaces, 2.0f);
            }

            TooltipUtility.SetToolTip(toolTipDoubleScale, tempRect);

            tempRect.Set(rect.x + 136, rect.y + offset + 57, 77, 15);

            if(GUI.Button(tempRect, contentHalfScale, rightStyle))
            {
                SurfaceUtility.MultiplyScale(m_SurfaceState.selectedBrushSurfaces, 0.5f);
            }

            TooltipUtility.SetToolTip(toolTipHalfScale, tempRect);

            if (CSGSettings.Presets[PresetCategory.UV_Scale].Count > 0)
            {
                tempRect.Set(rect.x + 8, rect.y + offset + 75, 45, 16);
                GUI.Label(tempRect, "Preset", EditorStyles.miniLabel);

                float ButtonStartingOffset = rect.x + 57;
                float ButtonAreaWidth = BOX_WIDTH - ButtonStartingOffset + 4;

                float PresetButtonWidth = ButtonAreaWidth / (float)CSGSettings.Presets[PresetCategory.UV_Scale].Count;

                for (int i = 0; i < CSGSettings.Presets[PresetCategory.UV_Scale].Count; i++)
                {
                    GUIStyle presetStyle = middleStyle;

                    if (i == 0)
                    {
                        presetStyle = leftStyle;
                    }
                    else if (i == CSGSettings.Presets[PresetCategory.UV_Scale].Count - 1)
                    {
                        presetStyle = rightStyle;
                    }

                    string PresetString = CSGSettings.Presets[PresetCategory.UV_Scale][i].Label;

                    if (string.IsNullOrEmpty(CSGSettings.Presets[PresetCategory.UV_Scale][i].Label) || string.IsNullOrWhiteSpace(CSGSettings.Presets[PresetCategory.UV_Scale][i].Label))
                    {
                        double RoundedPreset = Math.Round(CSGSettings.Presets[PresetCategory.UV_Scale][i].Value, 3);
                        PresetString = RoundedPreset.ToString();
                        if (RoundedPreset < 1.0d)
                        {
                            PresetString = PresetString.Replace("0.", ".");
                        }
                    }

                    //Determine if all selected surfaces use the same UV scale
                    bool AllSurfacesHaveSameUVScale = true;
                    Vector2 FirstFoundScale = new Vector2(float.MaxValue, float.MaxValue);
                    foreach (SelectedBrushSurface _surface in m_SurfaceState.selectedBrushSurfaces)
                    {
                        CSGBrush brush = _surface.brush;
                        int surfaceIndex = _surface.surfaceIndex;
                        int texGenIndex = brush.shape.surfaces[surfaceIndex].TexGenIndex;

                        if (FirstFoundScale == new Vector2(float.MaxValue, float.MaxValue))
                        {
                            FirstFoundScale = brush.shape.texGens[texGenIndex].Scale;
                        }
                        else if (brush.shape.texGens[texGenIndex].Scale != FirstFoundScale)
                        {
                            AllSurfacesHaveSameUVScale = false;
                            break;
                        }
                    }

                    tempRect.Set(ButtonStartingOffset + (PresetButtonWidth * i), rect.y + offset + 75, PresetButtonWidth, 15);
                    if (GUI.Toggle(tempRect, AllSurfacesHaveSameUVScale && FirstFoundScale == new Vector2(CSGSettings.Presets[PresetCategory.UV_Scale][i].Value, CSGSettings.Presets[PresetCategory.UV_Scale][i].Value), PresetString, presetStyle))
                    {
                        if (GUI.changed)
                        {
                            SurfaceUtility.SetScale(m_SurfaceState.selectedBrushSurfaces, CSGSettings.Presets[PresetCategory.UV_Scale][i].Value);
                        }
                    }
                }
            }
            else
            {
                //Remove space that was allocated for presets
                offset -= 18;
            }

            offset += UV_COMMANDS_BOX_HEIGHT;

            offset += BOX_OFFSET;
            tempRect.Set(rect.x + 4, rect.y + offset, BOX_WIDTH, SURFACE_FLAGS_BOX_HEIGHT);
            GUI.Box(tempRect, GUIContent.none, GUI.skin.box);

            tempRect.Set(rect.x + 8 - 4, rect.y + offset + 2, 205, 15);

            EditModeCommonGUI.OnSurfaceFlagButtons(tempRect, m_SurfaceState.surfaceFlagState, m_SurfaceState.selectedBrushSurfaces);

            EditorGUI.BeginDisabledGroup(!m_SurfaceState.canSmooth);

            tempRect.Set(rect.x + 8, rect.y + offset + 38, 94, 15);

            if(GUI.Button(tempRect, contentSmoothSurfaces, leftStyle))
            {
                SurfaceUtility.Smooth(m_SurfaceState.selectedBrushSurfaces);
            }

            TooltipUtility.SetToolTip(toolTipSmoothSurfaces, tempRect);

            EditorGUI.EndDisabledGroup();
            EditorGUI.BeginDisabledGroup(!m_SurfaceState.canUnSmooth);

            tempRect.Set(rect.x + 102, rect.y + offset + 38, 112, 15);

            if(GUI.Button(tempRect, contentUnSmoothSurfaces, rightStyle))
            {
                SurfaceUtility.UnSmooth(m_SurfaceState.selectedBrushSurfaces);
            }

            TooltipUtility.SetToolTip(toolTipUnSmoothSurfaces, tempRect);

            EditorGUI.EndDisabledGroup();

            EditorGUI.EndDisabledGroup();
            EditorGUI.showMixedValue = false;
        }

        private static void OnGUIContents(bool isSceneGUI, EditModeSurface tool)
        {
            EditModeCommonGUI.StartToolGUI();

            if (Event.current.type == EventType.Layout)
            {
                m_SurfaceState = new SurfaceState();
                m_SurfaceState.Init(tool);
            }

            if(m_SurfaceState == null)
            {
                return;
            }

            GUIStyle leftStyle   = isSceneGUI ? EditorStyles.miniButtonLeft : GUI.skin.button;
            GUIStyle middleStyle = isSceneGUI ? EditorStyles.miniButtonMid : GUI.skin.button;
            GUIStyle rightStyle  = isSceneGUI ? EditorStyles.miniButtonRight : GUI.skin.button;

            EditorGUI.BeginDisabledGroup(!m_SurfaceState.enabled);

            GUILayout.BeginVertical(CsgguiStyleUtility.contentEmpty);

            EditorGUILayout.Space();

            GUILayout.BeginVertical(GUIStyle.none);

            EditorGUI.BeginChangeCheck();

            EditorGUI.showMixedValue = !m_SurfaceState.textureLocked.HasValue;

            m_SurfaceState.textureLocked = EditorGUILayout.ToggleLeft(contentLockTexture, m_SurfaceState.textureLocked.HasValue && m_SurfaceState.textureLocked.Value);

            TooltipUtility.SetToolTip(toolTipLockTexture);

            if(EditorGUI.EndChangeCheck())
            {
                SurfaceUtility.SetTextureLock(m_SurfaceState.selectedBrushSurfaces, m_SurfaceState.textureLocked.Value);
            }

            GUILayout.EndVertical();

            GUILayout.BeginVertical(GUIStyle.none);

            GUILayout.BeginHorizontal(CsgguiStyleUtility.contentEmpty);

            EditorGUILayout.LabelField(contentUVScale, largeLabelWidth);
            TooltipUtility.SetToolTip(toolTipScaleUV);

            GUILayout.BeginHorizontal(CsgguiStyleUtility.contentEmpty);

            EditorGUILayout.LabelField(contentUSymbol, unitWidth);
            EditorGUI.BeginChangeCheck();

            EditorGUI.showMixedValue = m_SurfaceState.multipleScaleX;

            m_SurfaceState.currentTexGen.Scale.x = EditorGUILayout.FloatField(m_SurfaceState.currentTexGen.Scale.x, minFloatFieldWidth);

            if(EditorGUI.EndChangeCheck())
            {
                SurfaceUtility.SetScaleX(m_SurfaceState.selectedBrushSurfaces, m_SurfaceState.currentTexGen.Scale.x);
            }

            EditorGUILayout.LabelField(contentVSymbol, unitWidth);
            EditorGUI.BeginChangeCheck();

            EditorGUI.showMixedValue = m_SurfaceState.multipleScaleY;

            m_SurfaceState.currentTexGen.Scale.y = EditorGUILayout.FloatField(m_SurfaceState.currentTexGen.Scale.y, minFloatFieldWidth);

            if(EditorGUI.EndChangeCheck())
            {
                SurfaceUtility.SetScaleY(m_SurfaceState.selectedBrushSurfaces, m_SurfaceState.currentTexGen.Scale.y);
            }

            GUILayout.EndHorizontal();

            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal(CsgguiStyleUtility.contentEmpty);

            EditorGUILayout.LabelField(contentOffset, largeLabelWidth);
            TooltipUtility.SetToolTip(toolTipOffsetUV);

            GUILayout.BeginHorizontal(CsgguiStyleUtility.contentEmpty);

            EditorGUILayout.LabelField(contentUSymbol, unitWidth);
            EditorGUI.BeginChangeCheck();

            EditorGUI.showMixedValue = m_SurfaceState.multipleTranslationX;

            m_SurfaceState.currentTexGen.Translation.x = EditorGUILayout.FloatField(m_SurfaceState.currentTexGen.Translation.x, minFloatFieldWidth);

            if(EditorGUI.EndChangeCheck())
            {
                SurfaceUtility.SetTranslationX(m_SurfaceState.selectedBrushSurfaces, m_SurfaceState.currentTexGen.Translation.x);
            }

            EditorGUILayout.LabelField(contentVSymbol, unitWidth);
            EditorGUI.BeginChangeCheck();

            EditorGUI.showMixedValue = m_SurfaceState.multipleTranslationY;

            m_SurfaceState.currentTexGen.Translation.y = EditorGUILayout.FloatField(m_SurfaceState.currentTexGen.Translation.y, minFloatFieldWidth);

            if(EditorGUI.EndChangeCheck())
            {
                SurfaceUtility.SetTranslationY(m_SurfaceState.selectedBrushSurfaces, m_SurfaceState.currentTexGen.Translation.y);
            }

            GUILayout.EndHorizontal();

            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal(CsgguiStyleUtility.contentEmpty);

            EditorGUILayout.LabelField(contentRotate, largeLabelWidth);
            TooltipUtility.SetToolTip(toolTipRotation);

            GUILayout.BeginVertical(CsgguiStyleUtility.contentEmpty);

            GUILayout.BeginHorizontal(CsgguiStyleUtility.contentEmpty);

            EditorGUI.BeginChangeCheck();

            EditorGUI.showMixedValue = m_SurfaceState.multipleRotationAngle;

            m_SurfaceState.currentTexGen.RotationAngle = EditorGUILayout.FloatField(m_SurfaceState.currentTexGen.RotationAngle, minFloatFieldWidth);

            EditorGUILayout.LabelField(contentAngleSymbol, unitWidth);

            if(EditorGUI.EndChangeCheck())
            {
                SurfaceUtility.SetRotationAngle(m_SurfaceState.selectedBrushSurfaces, m_SurfaceState.currentTexGen.RotationAngle);
            }

            GUILayout.EndHorizontal();

            GUILayoutOption[] buttonWidth = Array.Empty<GUILayoutOption>();
            GUILayout.BeginHorizontal(CsgguiStyleUtility.contentEmpty);

            if(GUILayout.Button(contentRotate90Negative, leftStyle, buttonWidth))
            {
                SurfaceUtility.AddRotationAngle(m_SurfaceState.selectedBrushSurfaces, -90.0f);
            }

            TooltipUtility.SetToolTip(toolTipRotate90Negative);

            if(GUILayout.Button(contentRotate90Positive, rightStyle, buttonWidth))
            {
                SurfaceUtility.AddRotationAngle(m_SurfaceState.selectedBrushSurfaces, +90.0f);
            }

            TooltipUtility.SetToolTip(toolTipRotate90Positive);

            GUILayout.EndHorizontal();
            GUILayout.EndVertical();

            GUILayout.EndHorizontal();

            GUILayout.EndVertical();

            EditorGUILayout.Space();

            GUILayout.BeginVertical(GUIStyle.none);

            GUILayout.BeginHorizontal(CsgguiStyleUtility.contentEmpty);

            GUILayout.Label(contentFit, largeLabelWidth);

            if(GUILayout.Button(contentFitX, leftStyle))
            {
                SurfaceUtility.FitSurfaceX(m_SurfaceState.selectedBrushSurfaces);
            }

            TooltipUtility.SetToolTip(toolTipFitX);

            if(GUILayout.Button(contentFitXY, middleStyle))
            {
                SurfaceUtility.FitSurface(m_SurfaceState.selectedBrushSurfaces);
            }

            TooltipUtility.SetToolTip(toolTipFitXY);

            if(GUILayout.Button(contentFitY, rightStyle))
            {
                SurfaceUtility.FitSurfaceY(m_SurfaceState.selectedBrushSurfaces);
            }

            TooltipUtility.SetToolTip(toolTipFitY);

            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal(CsgguiStyleUtility.contentEmpty);

            GUILayout.Label(contentReset, largeLabelWidth);

            if(GUILayout.Button(contentResetX, leftStyle))
            {
                SurfaceUtility.ResetSurfaceX(m_SurfaceState.selectedBrushSurfaces);
            }

            TooltipUtility.SetToolTip(toolTipResetX);

            if(GUILayout.Button(contentResetXY, middleStyle))
            {
                SurfaceUtility.ResetSurface(m_SurfaceState.selectedBrushSurfaces);
            }

            TooltipUtility.SetToolTip(toolTipResetXY);

            if(GUILayout.Button(contentResetY, rightStyle))
            {
                SurfaceUtility.ResetSurfaceY(m_SurfaceState.selectedBrushSurfaces);
            }

            TooltipUtility.SetToolTip(toolTipResetY);

            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal(CsgguiStyleUtility.contentEmpty);

            GUILayout.Label(contentFlip, largeLabelWidth);

            if(GUILayout.Button(contentFlipX, leftStyle))
            {
                SurfaceUtility.FlipX(m_SurfaceState.selectedBrushSurfaces);
            }

            TooltipUtility.SetToolTip(toolTipFlipX);

            if(GUILayout.Button(contentFlipXY, middleStyle))
            {
                SurfaceUtility.FlipXY(m_SurfaceState.selectedBrushSurfaces);
            }

            TooltipUtility.SetToolTip(toolTipFlipXY);

            if(GUILayout.Button(contentFlipY, rightStyle))
            {
                SurfaceUtility.FlipY(m_SurfaceState.selectedBrushSurfaces);
            }

            TooltipUtility.SetToolTip(toolTipFlipY);

            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal(CsgguiStyleUtility.contentEmpty);

            GUILayout.Label(contentScale, largeLabelWidth);

            if(GUILayout.Button(contentDoubleScale, leftStyle))
            {
                SurfaceUtility.MultiplyScale(m_SurfaceState.selectedBrushSurfaces, 2.0f);
            }

            TooltipUtility.SetToolTip(toolTipDoubleScale);

            if(GUILayout.Button(contentHalfScale, rightStyle))
            {
                SurfaceUtility.MultiplyScale(m_SurfaceState.selectedBrushSurfaces, 0.5f);
            }

            TooltipUtility.SetToolTip(toolTipHalfScale);

            GUILayout.EndHorizontal();

            GUILayout.EndVertical();

            EditorGUILayout.Space();

            OnGUIContentsJustify(false, m_SurfaceState.selectedBrushSurfaces);

            EditorGUILayout.Space();

            GUILayout.BeginVertical(GUIStyle.none);

            EditModeCommonGUI.OnSurfaceFlagButtons(m_SurfaceState.surfaceFlagState, m_SurfaceState.selectedBrushSurfaces);

            GUILayout.BeginHorizontal(CsgguiStyleUtility.contentEmpty);

            EditorGUI.BeginDisabledGroup(!m_SurfaceState.canSmooth);

            if(GUILayout.Button(contentSmoothSurfaces, leftStyle))
            {
                SurfaceUtility.Smooth(m_SurfaceState.selectedBrushSurfaces);
            }

            TooltipUtility.SetToolTip(toolTipSmoothSurfaces);

            EditorGUI.EndDisabledGroup();
            EditorGUI.BeginDisabledGroup(!m_SurfaceState.canUnSmooth);

            if(GUILayout.Button(contentUnSmoothSurfaces, rightStyle))
            {
                SurfaceUtility.UnSmooth(m_SurfaceState.selectedBrushSurfaces);
            }

            TooltipUtility.SetToolTip(toolTipUnSmoothSurfaces);

            EditorGUI.EndDisabledGroup();

            GUILayout.EndHorizontal();

            GUILayout.EndVertical();

            EditorGUILayout.Space();
            GUILayout.BeginHorizontal(CsgguiStyleUtility.contentEmpty);

            EditorGUILayout.LabelField(contentMaterial, largeLabelWidth);
            GUILayout.BeginVertical(CsgguiStyleUtility.contentEmpty);

            EditorGUI.BeginChangeCheck();

            EditorGUI.showMixedValue = m_SurfaceState.multipleMaterials;

            Material newMaterial = EditorGUILayout.ObjectField(m_SurfaceState.material, typeof(Material), true) as Material;

            EditorGUI.showMixedValue = false;

            if(EditorGUI.EndChangeCheck())
            {
                if(!newMaterial)
                {
                    newMaterial = MaterialUtility.MissingMaterial;
                }

                SurfaceUtility.SetMaterials(m_SurfaceState.selectedBrushSurfaces, newMaterial);
            }

            GUILayout.Space(2);
            GUILayout.BeginHorizontal(CsgguiStyleUtility.contentEmpty);

            GUILayout.Space(5);

            OnGUIContentsMaterialImage(m_SurfaceState.firstMaterial, m_SurfaceState.multipleMaterials, m_SurfaceState.selectedBrushSurfaces);

            GUILayout.EndHorizontal();
            GUILayout.EndVertical();

            GUILayout.EndHorizontal();

            GUILayout.EndVertical();

            EditorGUI.EndDisabledGroup();
            EditorGUI.showMixedValue = false;
        }

        private static Rect m_LastGuiRect;

        public static Rect GetLastSceneGUIRect(EditModeSurface tool)
        {
            return m_LastGuiRect;
        }

        private static Vector2 m_ScrollbarPosition = Vector2.zero;

        private static readonly Rect sceneGUIRect = new Rect(0, 0, 232, 0);

        public static void OnSceneGUI(Rect windowRect, EditModeSurface tool)
        {
            InitLocalStyles();

            float maxHeight = windowRect.height - 80;

            CsgguiStyleUtility.ResetGUIState();

            GUIStyle windowStyle = GUI.skin.window;

            CSGModel[] models               = InternalCSGModelManager.Models;
            bool       needLightmapUVUpdate = EditModeCommonGUI.NeedLightmapUVUpdate(models);
          
            if (CSGSettings.UpdateLightmapUVButtonMode != CSGSettings.UpdateLightmapUVButtonState.InCSGToolWidow && CSGSettings.UpdateLightmapUVButtonMode != CSGSettings.UpdateLightmapUVButtonState.Both)
            {
                needLightmapUVUpdate = false;
            }

            int        originalHeight       = SURFACE_EDIT_WINDOW_HEIGHT + (needLightmapUVUpdate ? LIGHT_MAP_UV_BUTTON_OFFSET : 0) - (CSGSettings.Presets[PresetCategory.UV_Scale].Count <= 0 ? PRESET_ROW_OFFSET : 0);
            float      visibleHeight        = Math.Min(originalHeight, maxHeight);
            float      y                    = windowRect.height - visibleHeight; // 252

            Rect currentArea = sceneGUIRect;
            currentArea.y      = y;
            currentArea.height = visibleHeight;

            //Padding to prevent clipping of left and bottom edge of surfaces window
            currentArea.x += 10;
            currentArea.y -= 10;

            Rect      boxArea               = currentArea;
            const int scrollbarWidth        = 10;
            const int scrollbarRightOffset  = 7;
            const int scrollbarBottomOffset = 2;
            Rect      scrollbarArea         = currentArea;
            float     scrollHeight          = originalHeight - visibleHeight;
            bool      haveScrollbar         = scrollHeight > 0;

            if(haveScrollbar)
            {
                scrollbarArea.y      += SURFACE_EDIT_TITLE_HEIGHT;
                scrollbarArea.height -= SURFACE_EDIT_TITLE_HEIGHT + scrollbarBottomOffset;
                scrollbarArea.x      += scrollbarArea.width - scrollbarRightOffset;
                scrollbarArea.width  =  scrollbarWidth;
                boxArea.width        += scrollbarWidth;
            }

            GUI.Box(boxArea, m_ContentSurfacesLabel, windowStyle);

            int windowControlID = GUIUtility.GetControlID(sceneViewSurfaceOverlayHash, FocusType.Keyboard, boxArea);

            currentArea.x     += 5;
            currentArea.width += 2;
            currentArea.y     += SURFACE_EDIT_TITLE_HEIGHT;
            Rect internalScrollbarArea = currentArea;
            currentArea.height -= SURFACE_EDIT_TITLE_HEIGHT;

            if(haveScrollbar)
            {
                internalScrollbarArea.height =  originalHeight - SURFACE_EDIT_TITLE_HEIGHT;
                internalScrollbarArea.width  -= 50;
                m_ScrollbarPosition          =  GUI.BeginScrollView(currentArea, m_ScrollbarPosition, internalScrollbarArea);
            }
            else
            {
                m_ScrollbarPosition.y = 0;
            }

            currentArea.y      += 8;
            currentArea.height -= 8;
            OnGUIContents(currentArea, tool, needLightmapUVUpdate);

            if(haveScrollbar)
            {
                GUI.EndScrollView(true);
            }

            m_LastGuiRect = boxArea;

            Rect buttonArea = boxArea;
            buttonArea.x      += buttonArea.width - 17;
            buttonArea.y      += 2;
            buttonArea.height =  13;
            buttonArea.width  =  13;

            /*
            if(GUI.Button(buttonArea, GUIContent.none, "SearchCancelButton"))
            {
                EditModeToolWindowSceneGUI.GetWindow();
            }
            */

            TooltipUtility.SetToolTip(CsgguiStyleUtility.popOutTooltip, buttonArea);

            switch(Event.current.GetTypeForControl(windowControlID))
            {
                case EventType.MouseDown:
                {
                    if(boxArea.Contains(Event.current.mousePosition))
                    {
                        GUIUtility.hotControl      = windowControlID;
                        GUIUtility.keyboardControl = windowControlID;
                        Event.current.Use();
                    }

                    break;
                }
                case EventType.MouseMove:
                {
                    if(boxArea.Contains(Event.current.mousePosition))
                    {
                        Event.current.Use();
                    }

                    break;
                }
                case EventType.MouseUp:
                {
                    if(GUIUtility.hotControl == windowControlID)
                    {
                        GUIUtility.hotControl      = 0;
                        GUIUtility.keyboardControl = 0;
                        Event.current.Use();
                    }

                    break;
                }
                case EventType.MouseDrag:
                {
                    if(GUIUtility.hotControl == windowControlID)
                    {
                        Event.current.Use();
                    }

                    break;
                }
                case EventType.ScrollWheel:
                {
                    if(boxArea.Contains(Event.current.mousePosition))
                    {
                        Event.current.Use();
                    }

                    break;
                }
            }
        }

        public static void OnInspectorGUI(EditorWindow window, float height)
        {
            m_LastGuiRect = Rect.MinMaxRect(-1, -1, -1, -1);
            EditModeSurface tool = EditModeManager.ActiveTool as EditModeSurface;

            InitLocalStyles();
            OnGUIContents(false, tool);
        }
    }
}
