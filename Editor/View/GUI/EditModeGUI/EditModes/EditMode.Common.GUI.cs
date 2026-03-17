using UnityEditor;
using UnityEngine;
using RealtimeCSG;
using RealtimeCSG.Legacy;
using RealtimeCSG.Components;

namespace InternalRealtimeCSG
{
    internal sealed partial class EditModeCommonGUI
    {
        public static bool IndentableButton(GUIContent content, ref Rect rect, GUIStyle style = null)
        {
            Rect indentRect = EditorGUI.IndentedRect(rect);
            rect.y += 20;

            return style == null ? GUI.Button(indentRect, content) : GUI.Button(indentRect, content, style);
        }

        public static bool IndentableButton(GUIContent content, GUIStyle style = null)
        {
            return style == null
                ? GUI.Button(EditorGUI.IndentedRect(EditorGUILayout.GetControlRect()), content)
                : GUI.Button(EditorGUI.IndentedRect(EditorGUILayout.GetControlRect()), content, style);
        }

        public static void UpdateLightmapUVButton(ref Rect rect)
        {
            CsgguiStyleUtility.InitStyles();

            if(IndentableButton(updateLightmapUVContent, ref rect, CsgguiStyleUtility.RedButton))
            {
                CSGModelManager.BuildLightmapUvs();
            }
        }

        public static void UpdateLightmapUVButton()
        {
            CsgguiStyleUtility.InitStyles();

            if(IndentableButton(updateLightmapUVContent, CsgguiStyleUtility.RedButton))
            {
                CSGModelManager.BuildLightmapUvs();
            }
        }

        public static bool NeedLightmapUVUpdate(CSGModel[] models)
        {
            bool needLightmapUVUpdate = false;

            for(int m = 0; m < models.Length; m++)
            {
                if(!models[m])
                {
                    continue;
                }

                needLightmapUVUpdate = needLightmapUVUpdate
                 || (MeshInstanceManager.NeedToGenerateLightmapUVsForModel(models[m]) && !models[m].AutoRebuildUVs);
            }

            return needLightmapUVUpdate;
        }

        public static bool UpdateButtons(CSGModel[] models)
        {
            if(NeedLightmapUVUpdate(models))
            {
                UpdateLightmapUVButton();

                return true;
            }

            return false;
        }

        public static bool UpdateButtons(CSGModel[] models, ref Rect rect)
        {
            if(NeedLightmapUVUpdate(models))
            {
                UpdateLightmapUVButton(ref rect);

                return true;
            }

            return false;
        }

        public class SurfaceFlagState
        {
            public bool? noRender = false;
            public bool? noCollision = false;
            public bool? noCastShadows = false;
            public bool? noReceiveShadows = false;

            public void Init(SelectedBrushSurface[] selectedBrushSurfaces)
            {
                if(selectedBrushSurfaces.Length > 0)
                {
                    for(int i = 0; i < selectedBrushSurfaces.Length; i++)
                    {
                        CSGBrush brush        = selectedBrushSurfaces[i].brush;
                        int      surfaceIndex = selectedBrushSurfaces[i].surfaceIndex;

                        if(surfaceIndex >= brush.Shape.Surfaces.Length)
                        {
                            Debug.LogWarning("surface_index >= brush.Shape.Surfaces.Length");

                            continue;
                        }

                        int texGenIndex = brush.Shape.Surfaces[surfaceIndex].TexGenIndex;

                        if(texGenIndex >= brush.Shape.TexGens.Length)
                        {
                            Debug.LogWarning("texGen_index >= brush.Shape.TexGens.Length");

                            continue;
                        }

                        TexGenFlags texGenFlags = brush.Shape.TexGenFlags[texGenIndex];
                        bool surfaceNoRender = (texGenFlags & TexGenFlags.NoRender) == TexGenFlags.NoRender;
                        bool surfaceNoCollision = (texGenFlags & TexGenFlags.NoCollision) == TexGenFlags.NoCollision;

                        bool surfaceNoCastShadows =
                            (texGenFlags & TexGenFlags.NoCastShadows) == TexGenFlags.NoCastShadows;

                        bool surfaceNoReceiveShadows =
                            (texGenFlags & TexGenFlags.NoReceiveShadows) == TexGenFlags.NoReceiveShadows;

                        if(i == 0)
                        {
                            noRender         = surfaceNoRender;
                            noCollision      = surfaceNoCollision;
                            noCastShadows    = surfaceNoCastShadows;
                            noReceiveShadows = surfaceNoReceiveShadows;
                        }
                        else
                        {
                            if(noRender.HasValue && noRender.Value != surfaceNoRender)
                            {
                                noRender = surfaceNoRender;
                            }

                            if(noCollision.HasValue && noCollision.Value != surfaceNoCollision)
                            {
                                noCollision = surfaceNoCollision;
                            }

                            if(noCastShadows.HasValue && noCastShadows.Value != surfaceNoCastShadows)
                            {
                                noCastShadows = surfaceNoCastShadows;
                            }

                            if(noReceiveShadows.HasValue && noReceiveShadows.Value != surfaceNoReceiveShadows)
                            {
                                noReceiveShadows = surfaceNoReceiveShadows;
                            }
                        }
                    }
                }
            }
        }

        public static void OnSurfaceFlagButtons(
            SurfaceFlagState state, SelectedBrushSurface[] selectedBrushSurfaces, bool isSceneGUI = false
        )
        {
            GUIStyle leftStyle  = isSceneGUI ? EditorStyles.miniButtonLeft : GUI.skin.button;
            GUIStyle rightStyle = isSceneGUI ? EditorStyles.miniButtonRight : GUI.skin.button;

            GUILayout.BeginHorizontal(CsgguiStyleUtility.contentEmpty);

            {
                if(!isSceneGUI)
                {
                    GUILayout.Label(contentShadows, EditModeSurfaceGUI.largeLabelWidth);
                }
                else
                {
                    GUILayout.Label(contentShadows, EditorStyles.miniLabel, EditModeSurfaceGUI.smallLabelWidth);
                }

                EditorGUI.BeginChangeCheck();

                {
                    // TODO: implement support
                    EditorGUI.showMixedValue = !state.noReceiveShadows.HasValue;

                    state.noReceiveShadows = !GUILayout.Toggle(
                        !(state.noReceiveShadows ?? state.noRender ?? true), contentReceiveShadowsSurfaces, leftStyle
                    );

                    TooltipUtility.SetToolTip(toolTipReceiveShadowsSurfaces);
                }

                if(EditorGUI.EndChangeCheck())
                {
                    SurfaceUtility.SetSurfaceTexGenFlags(
                        selectedBrushSurfaces, TexGenFlags.NoReceiveShadows, state.noReceiveShadows.Value
                    );
                }

                EditorGUI.BeginChangeCheck();

                {
                    // TODO: implement support
                    EditorGUI.showMixedValue = !state.noCastShadows.HasValue;

                    state.noCastShadows = !GUILayout.Toggle(
                        !(state.noCastShadows ?? true), contentCastShadowsSurfaces, rightStyle
                    );

                    TooltipUtility.SetToolTip(toolTipCastShadowsSurfaces);
                }

                if(EditorGUI.EndChangeCheck())
                {
                    SurfaceUtility.SetSurfaceTexGenFlags(
                        selectedBrushSurfaces, TexGenFlags.NoCastShadows, state.noCastShadows.Value
                    );
                }
            }

            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal(CsgguiStyleUtility.contentEmpty);

            {
                EditorGUI.BeginChangeCheck();

                {
                    EditorGUI.showMixedValue = !state.noRender.HasValue;
                    state.noRender = !GUILayout.Toggle(!(state.noRender ?? true), contentVisibleSurfaces, leftStyle);
                    TooltipUtility.SetToolTip(toolTipVisibleSurfaces);
                }

                if(EditorGUI.EndChangeCheck())
                {
                    SurfaceUtility.SetSurfaceTexGenFlags(
                        selectedBrushSurfaces, TexGenFlags.NoRender, state.noRender.Value
                    );
                }

                EditorGUI.BeginChangeCheck();

                {
                    EditorGUI.showMixedValue = !state.noCollision.HasValue;

                    state.noCollision = !GUILayout.Toggle(
                        !(state.noCollision ?? true), contentCollisionSurfaces, rightStyle
                    );

                    TooltipUtility.SetToolTip(toolTipCollisionSurfaces);
                }

                if(EditorGUI.EndChangeCheck())
                {
                    SurfaceUtility.SetSurfaceTexGenFlags(
                        selectedBrushSurfaces, TexGenFlags.NoCollision, state.noCollision.Value
                    );
                }
            }

            GUILayout.EndHorizontal();
            EditorGUI.showMixedValue = false;
        }

        public static void OnSurfaceFlagButtons(
            Rect rect, SurfaceFlagState state, SelectedBrushSurface[] selectedBrushSurfaces
        )
        {
            GUIStyle leftStyle  = EditorStyles.miniButtonLeft;
            GUIStyle rightStyle = EditorStyles.miniButtonRight;

            Rect tempRect = rect;

            {
                tempRect.Set(rect.x + 4, rect.y + 1, 45, 16);
                GUI.Label(tempRect, contentShadows, EditorStyles.miniLabel);

                EditorGUI.BeginChangeCheck();

                {
                    bool mixed   = !state.noReceiveShadows.HasValue;
                    bool enabled = !(state.noReceiveShadows ?? state.noRender ?? true);
                    EditorGUI.showMixedValue = mixed;
                    tempRect.Set(rect.x + 53, rect.y + 1, 90 - 4, 15);
                    state.noReceiveShadows = !GUI.Toggle(tempRect, enabled, contentReceiveShadowsSurfaces, leftStyle);
                    TooltipUtility.SetToolTip(toolTipReceiveShadowsSurfaces, tempRect);
                }

                if(EditorGUI.EndChangeCheck())
                {
                    SurfaceUtility.SetSurfaceTexGenFlags(
                        selectedBrushSurfaces, TexGenFlags.NoReceiveShadows, state.noReceiveShadows.Value
                    );
                }

                EditorGUI.BeginChangeCheck();

                {
                    bool mixed   = !state.noCastShadows.HasValue;
                    bool enabled = !(state.noCastShadows ?? true);
                    EditorGUI.showMixedValue = mixed;
                    tempRect.Set(rect.x + 143 - 4, rect.y + 1, 74 - 4, 15);
                    state.noCastShadows = !GUI.Toggle(tempRect, enabled, contentCastShadowsSurfaces, rightStyle);
                    TooltipUtility.SetToolTip(toolTipCastShadowsSurfaces, tempRect);
                }

                if(EditorGUI.EndChangeCheck())
                {
                    SurfaceUtility.SetSurfaceTexGenFlags(
                        selectedBrushSurfaces, TexGenFlags.NoCastShadows, state.noCastShadows.Value
                    );
                }
            }

            {
                EditorGUI.BeginChangeCheck();

                {
                    bool mixed   = !state.noRender.HasValue;
                    bool enabled = !(state.noRender ?? true);
                    EditorGUI.showMixedValue = mixed;
                    tempRect.Set(rect.x + 4, rect.y + 18, 94, 15);
                    state.noRender = !GUI.Toggle(tempRect, enabled, contentVisibleSurfaces, leftStyle);
                    TooltipUtility.SetToolTip(toolTipVisibleSurfaces, tempRect);
                }

                if(EditorGUI.EndChangeCheck())
                {
                    SurfaceUtility.SetSurfaceTexGenFlags(
                        selectedBrushSurfaces, TexGenFlags.NoRender, state.noRender.Value
                    );
                }

                EditorGUI.BeginChangeCheck();

                {
                    bool mixed   = !state.noCollision.HasValue;
                    bool enabled = !(state.noCollision ?? true);
                    EditorGUI.showMixedValue = mixed;
                    tempRect.Set(rect.x + 98, rect.y + 18, 112, 15);
                    state.noCollision = !GUI.Toggle(tempRect, enabled, contentCollisionSurfaces, rightStyle);
                    TooltipUtility.SetToolTip(toolTipCollisionSurfaces, tempRect);
                }

                if(EditorGUI.EndChangeCheck())
                {
                    SurfaceUtility.SetSurfaceTexGenFlags(
                        selectedBrushSurfaces, TexGenFlags.NoCollision, state.noCollision.Value
                    );
                }
            }

            EditorGUI.showMixedValue = false;
        }

        public static TexGenFlags OnSurfaceFlagButtons(TexGenFlags texGenFlags, bool isSceneGUI = false)
        {
            GUIStyle leftStyle  = EditorStyles.miniButtonLeft;
            GUIStyle rightStyle = EditorStyles.miniButtonRight;

            bool noRender      = (texGenFlags & TexGenFlags.NoRender) == TexGenFlags.NoRender;
            bool noCollision   = (texGenFlags & TexGenFlags.NoCollision) == TexGenFlags.NoCollision;
            bool noCastShadows = (texGenFlags & TexGenFlags.NoCastShadows) == TexGenFlags.NoCastShadows;

            bool noReceiveShadows =
                noRender || (texGenFlags & TexGenFlags.NoReceiveShadows) == TexGenFlags.NoReceiveShadows;

            GUILayout.BeginVertical(CsgguiStyleUtility.contentEmpty);

            {
                GUILayout.BeginHorizontal(CsgguiStyleUtility.contentEmpty);

                {
                    GUILayout.Label(contentShadows, EditorStyles.miniLabel, EditModeSurfaceGUI.smallLabelWidth);
                    EditorGUI.BeginChangeCheck();

                    {
                        // TODO: implement support
                        noReceiveShadows = !GUILayout.Toggle(
                            !noReceiveShadows, contentReceiveShadowsSurfaces, leftStyle
                        );

                        TooltipUtility.SetToolTip(toolTipReceiveShadowsSurfaces);
                    }

                    if(EditorGUI.EndChangeCheck())
                    {
                        if(noReceiveShadows)
                        {
                            texGenFlags |= TexGenFlags.NoReceiveShadows;
                        }
                        else
                        {
                            texGenFlags &= ~TexGenFlags.NoReceiveShadows;
                        }

                        GUI.changed = true;
                    }

                    EditorGUI.BeginChangeCheck();

                    {
                        // TODO: implement support
                        noCastShadows = !GUILayout.Toggle(!noCastShadows, contentCastShadowsSurfaces, rightStyle);
                        TooltipUtility.SetToolTip(toolTipCastShadowsSurfaces);
                    }

                    if(EditorGUI.EndChangeCheck())
                    {
                        if(noCastShadows)
                        {
                            texGenFlags |= TexGenFlags.NoCastShadows;
                        }
                        else
                        {
                            texGenFlags &= ~TexGenFlags.NoCastShadows;
                        }

                        GUI.changed = true;
                    }
                }

                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal(CsgguiStyleUtility.contentEmpty);

                {
                    EditorGUI.BeginChangeCheck();

                    {
                        noRender = !GUILayout.Toggle(!noRender, contentVisibleSurfaces, leftStyle);
                        TooltipUtility.SetToolTip(toolTipVisibleSurfaces);
                    }

                    if(EditorGUI.EndChangeCheck())
                    {
                        if(noRender)
                        {
                            texGenFlags |= TexGenFlags.NoRender;
                        }
                        else
                        {
                            texGenFlags &= ~TexGenFlags.NoRender;
                        }

                        GUI.changed = true;
                    }

                    EditorGUI.BeginChangeCheck();

                    {
                        noCollision = !GUILayout.Toggle(!noCollision, contentCollisionSurfaces, rightStyle);
                        TooltipUtility.SetToolTip(toolTipCollisionSurfaces);
                    }

                    if(EditorGUI.EndChangeCheck())
                    {
                        if(noCollision)
                        {
                            texGenFlags |= TexGenFlags.NoCollision;
                        }
                        else
                        {
                            texGenFlags &= ~TexGenFlags.NoCollision;
                        }

                        GUI.changed = true;
                    }
                }

                GUILayout.EndHorizontal();
            }

            GUILayout.EndVertical();

            return texGenFlags;
        }

        public static void StartToolGUI()
        {
            if(UpdateButtons(InternalCSGModelManager.Models))
            {
                GUILayout.Space(10);
            }
        }

        public static Rect StartToolGUI(Rect rect)
        {
            if(UpdateButtons(InternalCSGModelManager.Models, ref rect))
            {
                rect.y += 10;
            }

            return rect;
        }
    }
}
