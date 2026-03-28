using System.Collections.Generic;
using System.Globalization;
using System.Text;
using InternalRealtimeCSG;
using RealtimeCSG.Components;
using RealtimeCSG.Foundation;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace RealtimeCSG
{
    internal sealed partial class SceneViewBottomBarGUI
    {
        private static readonly int bottomBarEditorOverlayHash = "BottomBarEditorOverlay".GetHashCode();
        private static readonly int bottomBarGUIHash = "BottomBarGUI".GetHashCode();

        public static void ShowGUI(SceneView sceneView, bool haveOffset = true)
        {
            InitStyles();
            CsgguiStyleUtility.InitStyles();

            if(sceneView != null)
            {
                float height = sceneView.rootVisualElement.contentRect.height;
                float width  = sceneView.rootVisualElement.contentRect.width;

                Rect bottomBarRect;

                bottomBarRect = haveOffset
                    ? new Rect(0, height - (CsgguiStyleUtility.BOTTOM_TOOL_BAR_HEIGHT + 18), width, CsgguiStyleUtility.BOTTOM_TOOL_BAR_HEIGHT)
                    : new Rect(0, height - (CsgguiStyleUtility.BOTTOM_TOOL_BAR_HEIGHT + 1), width, CsgguiStyleUtility.BOTTOM_TOOL_BAR_HEIGHT);

                try
                {
                    Handles.BeginGUI();

                    bool prevGUIChanged = GUI.changed;

                    if(Event.current.type == EventType.Repaint)
                    {
                        CsgguiStyleUtility.BottomToolBarStyle.Draw(bottomBarRect, false, false, false, false);
                    }

                    OnBottomBarGUI(sceneView, bottomBarRect);
                    GUI.changed = prevGUIChanged || GUI.changed;

                    int       controlID = GUIUtility.GetControlID(bottomBarGUIHash, FocusType.Keyboard, bottomBarRect);
                    EventType type      = Event.current.GetTypeForControl(controlID);

                    switch(type)
                    {
                        case EventType.MouseDown:
                        {
                            if(bottomBarRect.Contains(Event.current.mousePosition))
                            {
                                GUIUtility.hotControl      = controlID;
                                GUIUtility.keyboardControl = controlID;
                                Event.current.Use();
                            }

                            break;
                        }
                        case EventType.MouseMove:
                        {
                            if(bottomBarRect.Contains(Event.current.mousePosition))
                            {
                                Event.current.Use();
                            }

                            break;
                        }
                        case EventType.MouseUp:
                        {
                            if(GUIUtility.hotControl == controlID)
                            {
                                GUIUtility.hotControl      = 0;
                                GUIUtility.keyboardControl = 0;
                                Event.current.Use();
                            }

                            break;
                        }
                        case EventType.MouseDrag:
                        {
                            if(GUIUtility.hotControl == controlID)
                            {
                                Event.current.Use();
                            }

                            break;
                        }
                        case EventType.ScrollWheel:
                        {
                            if(bottomBarRect.Contains(Event.current.mousePosition))
                            {
                                Event.current.Use();
                            }

                            break;
                        }
                    }
                }
                finally
                {
                    Handles.EndGUI();
                }
            }
        }

        private static Rect m_CurrentRect;

        private static void OnBottomBarGUI(SceneView sceneView, Rect barSize)
        {
            SnapMode           snapMode          = CSGSettings.SnapMode;
            bool               uniformGrid       = CSGSettings.UniformGrid;
            Vector3            moveSnapVector    = CSGSettings.SnapVector;
            float              rotationSnap      = CSGSettings.SnapRotation;
            float              scaleSnap         = CSGSettings.SnapScale;
            bool               showGrid          = CSGSettings.GridVisible;
            bool               lockAxisX         = CSGSettings.LockAxisX;
            bool               lockAxisY         = CSGSettings.LockAxisY;
            bool               lockAxisZ         = CSGSettings.LockAxisZ;
            DistanceUnit       distanceUnit      = CSGSettings.DistanceUnit;
            HelperSurfaceFlags helperSurfaces    = CSGSettings.VisibleHelperSurfaces;
            bool               showWireframe     = CSGSettings.IsWireframeShown(sceneView);
            CSGSkin            skin              = CsgguiStyleUtility.Skin;
            bool               updateSurfaces    = false;
            bool               wireframeModified = false;

            float viewWidth = sceneView.rootVisualElement.contentRect.width;

            float layoutHeight = barSize.height;
            float layoutX      = 6.0f;

            bool modified = false;
            GUI.changed = false;

            m_CurrentRect.width  =  27;
            m_CurrentRect.y      =  0;
            m_CurrentRect.height =  layoutHeight - m_CurrentRect.y;
            m_CurrentRect.y      += barSize.y;
            m_CurrentRect.x      =  layoutX;
            layoutX              += m_CurrentRect.width;

#region "Grid" button

            showGrid = GUI.Toggle(m_CurrentRect, showGrid, showGrid ? skin.gridIconOn : skin.gridIcon, EditorStyles.toolbarButton);

            //(x:6.00, y:0.00, width:27.00, height:18.00)
            TooltipUtility.SetToolTip(showGridTooltip, m_CurrentRect);

#endregion

            if(viewWidth >= 800)
            {
                layoutX += 6; //(x:33.00, y:0.00, width:6.00, height:6.00)
            }

            Color prevBackgroundColor   = GUI.backgroundColor;
            Color lockedBackgroundColor = skin.lockedBackgroundColor;

            if(lockAxisX)
            {
                GUI.backgroundColor = lockedBackgroundColor;
            }

#region "X" lock button

            m_CurrentRect.width  =  17;
            m_CurrentRect.y      =  0;
            m_CurrentRect.height =  layoutHeight - m_CurrentRect.y;
            m_CurrentRect.y      += barSize.y;
            m_CurrentRect.x      =  layoutX;
            layoutX              += m_CurrentRect.width;

            lockAxisX = !GUI.Toggle(m_CurrentRect, !lockAxisX, xLabel, skin.xToolbarButton);

            //(x:39.00, y:0.00, width:17.00, height:18.00)
            TooltipUtility.SetToolTip(lockAxisX ? xTooltipOn : xTooltipOff, m_CurrentRect);

            GUI.backgroundColor = prevBackgroundColor;

#endregion

#region "Y" lock button

            m_CurrentRect.x =  layoutX;
            layoutX         += m_CurrentRect.width;

            if(lockAxisY)
            {
                GUI.backgroundColor = lockedBackgroundColor;
            }

            lockAxisY = !GUI.Toggle(m_CurrentRect, !lockAxisY, yLabel, skin.yToolbarButton);

            //(x:56.00, y:0.00, width:17.00, height:18.00)
            TooltipUtility.SetToolTip(lockAxisY ? yTooltipOn : yTooltipOff, m_CurrentRect);

            GUI.backgroundColor = prevBackgroundColor;

#endregion

#region "Z" lock button

            m_CurrentRect.x =  layoutX;
            layoutX         += m_CurrentRect.width;

            if(lockAxisZ)
            {
                GUI.backgroundColor = lockedBackgroundColor;
            }

            lockAxisZ = !GUI.Toggle(m_CurrentRect, !lockAxisZ, zLabel, skin.zToolbarButton);

            //(x:56.00, y:0.00, width:17.00, height:18.00)
            TooltipUtility.SetToolTip(lockAxisZ ? zTooltipOn : zTooltipOff, m_CurrentRect);

            GUI.backgroundColor = prevBackgroundColor;

#endregion

            modified = GUI.changed || modified;

            if(viewWidth >= 800)
            {
                layoutX += 6; // (x:91.00, y:0.00, width:6.00, height:6.00)
            }

#region "SnapMode" button

            GUI.changed = false;

            {
                m_CurrentRect.width  =  27;
                m_CurrentRect.y      =  0;
                m_CurrentRect.height =  layoutHeight - m_CurrentRect.y;
                m_CurrentRect.y      += barSize.y;
                m_CurrentRect.x      =  layoutX;
                layoutX              += m_CurrentRect.width;

                switch(snapMode)
                {
                    case SnapMode.GridSnapping:
                    {
                        bool newValue = GUI.Toggle(m_CurrentRect, true, CsgguiStyleUtility.Skin.gridSnapIconOn, EditorStyles.toolbarButton);

                        if(GUI.changed)
                        {
                            snapMode = newValue ? SnapMode.GridSnapping : SnapMode.RelativeSnapping;
                        }

                        //(x:97.00, y:0.00, width:27.00, height:18.00)
                        TooltipUtility.SetToolTip(gridSnapModeTooltip, m_CurrentRect);

                        break;
                    }
                    case SnapMode.RelativeSnapping:
                    {
                        bool newValue = GUI.Toggle(m_CurrentRect, true, CsgguiStyleUtility.Skin.relSnapIconOn, EditorStyles.toolbarButton);

                        if(GUI.changed)
                        {
                            snapMode = newValue ? SnapMode.RelativeSnapping : SnapMode.None;
                        }

                        //(x:97.00, y:0.00, width:27.00, height:18.00)
                        TooltipUtility.SetToolTip(relativeSnapModeTooltip, m_CurrentRect);

                        break;
                    }
                    default:
                    case SnapMode.None:
                    {
                        bool newValue = GUI.Toggle(m_CurrentRect, snapMode != SnapMode.None, CsgguiStyleUtility.Skin.noSnapIconOn, EditorStyles.toolbarButton);

                        if(GUI.changed)
                        {
                            snapMode = newValue ? SnapMode.GridSnapping : SnapMode.None;
                        }

                        //(x:97.00, y:0.00, width:27.00, height:18.00)
                        TooltipUtility.SetToolTip(noSnappingModeTooltip, m_CurrentRect);

                        break;
                    }
                }
            }

            modified = GUI.changed || modified;

#endregion

            if(viewWidth >= 460)
            {
                if(snapMode != SnapMode.None)
                {
#region "Position" label

                    if(viewWidth >= 500)
                    {
                        if(viewWidth >= 865)
                        {
                            m_CurrentRect.width  =  44;
                            m_CurrentRect.y      =  1;
                            m_CurrentRect.height =  layoutHeight - m_CurrentRect.y;
                            m_CurrentRect.y      += barSize.y;
                            m_CurrentRect.x      =  layoutX;
                            layoutX              += m_CurrentRect.width;

                            uniformGrid = GUI.Toggle(m_CurrentRect, uniformGrid, positionLargeLabel, m_MiniTextStyle);
                            //(x:128.00, y:2.00, width:44.00, height:16.00)
                        }
                        else
                        {
                            m_CurrentRect.width  =  22;
                            m_CurrentRect.y      =  1;
                            m_CurrentRect.height =  layoutHeight - m_CurrentRect.y;
                            m_CurrentRect.y      += barSize.y;
                            m_CurrentRect.x      =  layoutX;
                            layoutX              += m_CurrentRect.width;

                            uniformGrid = GUI.Toggle(m_CurrentRect, uniformGrid, positionSmallLabel, m_MiniTextStyle);
                            //(x:127.00, y:2.00, width:22.00, height:16.00)
                        }

                        TooltipUtility.SetToolTip(positionTooltip, m_CurrentRect);
                    }

#endregion

                    layoutX += 2;

#region "Position" field

                    if(uniformGrid || viewWidth < 515)
                    {
                        EditorGUI.showMixedValue =
                            !(moveSnapVector.x == moveSnapVector.y && moveSnapVector.x == moveSnapVector.z);

                        GUI.changed = false;

                        {
                            m_CurrentRect.width  =  70;
                            m_CurrentRect.y      =  3;
                            m_CurrentRect.height =  layoutHeight - (m_CurrentRect.y - 1);
                            m_CurrentRect.y      += barSize.y;
                            m_CurrentRect.x      =  layoutX;
                            layoutX              += m_CurrentRect.width;

                            moveSnapVector.x = Units.DistanceUnitToUnity(
                                distanceUnit
                              , EditorGUI.DoubleField(
                                    m_CurrentRect, Units.UnityToDistanceUnit(distanceUnit, moveSnapVector.x)
                                  , m_TextInputStyle
                                )
                            );
                            //(x:176.00, y:3.00, width:70.00, height:16.00)
                        }

                        if(GUI.changed)
                        {
                            modified         = true;
                            moveSnapVector.y = moveSnapVector.x;
                            moveSnapVector.z = moveSnapVector.x;
                        }

                        EditorGUI.showMixedValue = false;
                    }
                    else
                    {
                        GUI.changed = false;

                        m_CurrentRect.width  =  70;
                        m_CurrentRect.y      =  3;
                        m_CurrentRect.height =  layoutHeight - (m_CurrentRect.y - 1);
                        m_CurrentRect.y      += barSize.y;
                        m_CurrentRect.x      =  layoutX;
                        layoutX              += m_CurrentRect.width;
                        layoutX++;

                        moveSnapVector.x = Units.DistanceUnitToUnity(distanceUnit, EditorGUI.DoubleField(m_CurrentRect, Units.UnityToDistanceUnit(distanceUnit, moveSnapVector.x), m_TextInputStyle));
                        //(x:175.00, y:3.00, width:70.00, height:16.00)

                        m_CurrentRect.x =  layoutX;
                        layoutX         += m_CurrentRect.width;
                        layoutX++;

                        // NOTE: moveSnapVector.y and moveSnapVector.z have accidentally been reversed relatively to unity's coordinate system, so we swap them here
                        moveSnapVector.z = Units.DistanceUnitToUnity(distanceUnit, EditorGUI.DoubleField(m_CurrentRect, Units.UnityToDistanceUnit(distanceUnit, moveSnapVector.z), m_TextInputStyle));

                        //(x:247.00, y:3.00, width:70.00, height:16.00)

                        m_CurrentRect.x =  layoutX;
                        layoutX         += m_CurrentRect.width;

                        moveSnapVector.y = Units.DistanceUnitToUnity(distanceUnit, EditorGUI.DoubleField(m_CurrentRect, Units.UnityToDistanceUnit(distanceUnit, moveSnapVector.y), m_TextInputStyle));
                        //(x:319.00, y:3.00, width:70.00, height:16.00)

                        modified = GUI.changed || modified;
                    }

#endregion

                    layoutX++;

#region "Position" Unit

                    DistanceUnit nextUnit = Units.CycleToNextUnit(distanceUnit);
                    GUIContent   unitText = Units.GetUnitGUIContent(distanceUnit);

                    m_CurrentRect.width  =  22;
                    m_CurrentRect.y      =  2;
                    m_CurrentRect.height =  layoutHeight - m_CurrentRect.y;
                    m_CurrentRect.y      += barSize.y;
                    m_CurrentRect.x      =  layoutX;
                    layoutX              += m_CurrentRect.width;

                    //(x:393.00, y:2.00, width:13.00, height:16.00)
                    if(GUI.Button(m_CurrentRect, unitText, m_MiniTextStyle))
                    {
                        distanceUnit = nextUnit;
                        modified     = true;
                    }

#endregion

                    layoutX += 2;

#region "Position" +/-

                    if(viewWidth >= 700)
                    {
                        m_CurrentRect.width  =  19;
                        m_CurrentRect.y      =  2;
                        m_CurrentRect.height =  layoutHeight - (m_CurrentRect.y + 1);
                        m_CurrentRect.y      += barSize.y;
                        m_CurrentRect.x      =  layoutX;
                        layoutX              += m_CurrentRect.width;

                        if(GUI.Button(m_CurrentRect, positionPlusLabel, EditorStyles.miniButtonLeft))
                        {
                            GridUtility.DoubleGridSize();
                            moveSnapVector = CSGSettings.SnapVector;
                        }

                        //(x:410.00, y:2.00, width:19.00, height:15.00)
                        TooltipUtility.SetToolTip(positionPlusTooltip, m_CurrentRect);

                        m_CurrentRect.width  =  17;
                        m_CurrentRect.y      =  2;
                        m_CurrentRect.height =  layoutHeight - (m_CurrentRect.y + 1);
                        m_CurrentRect.y      += barSize.y;
                        m_CurrentRect.x      =  layoutX;
                        layoutX              += m_CurrentRect.width;

                        if(GUI.Button(m_CurrentRect, positionMinusLabel, EditorStyles.miniButtonRight))
                        {
                            GridUtility.HalfGridSize();
                            moveSnapVector = CSGSettings.SnapVector;
                        }

                        //(x:429.00, y:2.00, width:17.00, height:15.00)
                        TooltipUtility.SetToolTip(positionMinnusTooltip, m_CurrentRect);
                    }

#endregion

                    layoutX += 2;

#region "Angle" label

                    if(viewWidth >= 750)
                    {
                        if(viewWidth >= 865)
                        {
                            m_CurrentRect.width  =  31;
                            m_CurrentRect.y      =  1;
                            m_CurrentRect.height =  layoutHeight - m_CurrentRect.y;
                            m_CurrentRect.y      += barSize.y;
                            m_CurrentRect.x      =  layoutX;
                            layoutX              += m_CurrentRect.width;

                            GUI.Label(m_CurrentRect, angleLargeLabel, m_MiniTextStyle);
                            //(x:450.00, y:2.00, width:31.00, height:16.00)
                        }
                        else
                        {
                            m_CurrentRect.width  =  22;
                            m_CurrentRect.y      =  1;
                            m_CurrentRect.height =  layoutHeight - m_CurrentRect.y;
                            m_CurrentRect.y      += barSize.y;
                            m_CurrentRect.x      =  layoutX;
                            layoutX              += m_CurrentRect.width;

                            GUI.Label(m_CurrentRect, angleSmallLabel, m_MiniTextStyle);
                            //(x:355.00, y:2.00, width:22.00, height:16.00)
                        }

                        TooltipUtility.SetToolTip(angleTooltip, m_CurrentRect);
                    }

#endregion

                    layoutX += 2;

#region "Angle" field

                    GUI.changed = false;

                    m_CurrentRect.width  =  70;
                    m_CurrentRect.y      =  3;
                    m_CurrentRect.height =  layoutHeight - (m_CurrentRect.y - 1);
                    m_CurrentRect.y      += barSize.y;
                    m_CurrentRect.x      =  layoutX;
                    layoutX              += m_CurrentRect.width;

                    rotationSnap = EditorGUI.FloatField(m_CurrentRect, rotationSnap, m_TextInputStyle);

                    //(x:486.00, y:3.00, width:70.00, height:16.00)
                    if(viewWidth <= 750)
                    {
                        TooltipUtility.SetToolTip(angleTooltip, m_CurrentRect);
                    }

                    modified = GUI.changed || modified;

#endregion

                    layoutX++;

#region "Angle" Unit

                    if(viewWidth >= 370)
                    {
                        m_CurrentRect.width  =  14;
                        m_CurrentRect.y      =  1;
                        m_CurrentRect.height =  layoutHeight - m_CurrentRect.y;
                        m_CurrentRect.y      += barSize.y;
                        m_CurrentRect.x      =  layoutX;
                        layoutX              += m_CurrentRect.width;

                        GUI.Label(m_CurrentRect, angleUnitLabel, m_MiniTextStyle);
                    }

#endregion

                    layoutX += 2;

#region "Angle" +/-

                    if(viewWidth >= 700)
                    {
                        m_CurrentRect.width  =  19;
                        m_CurrentRect.y      =  1;
                        m_CurrentRect.height =  layoutHeight - (m_CurrentRect.y + 1);
                        m_CurrentRect.y      += barSize.y;
                        m_CurrentRect.x      =  layoutX;
                        layoutX              += m_CurrentRect.width;

                        if(GUI.Button(m_CurrentRect, anglePlusLabel, EditorStyles.miniButtonLeft))
                        {
                            rotationSnap *= 2.0f;
                            modified     =  true;
                        }

                        //(x:573.00, y:2.00, width:19.00, height:15.00)
                        TooltipUtility.SetToolTip(anglePlusTooltip, m_CurrentRect);

                        m_CurrentRect.width  =  17;
                        m_CurrentRect.y      =  1;
                        m_CurrentRect.height =  layoutHeight - (m_CurrentRect.y + 1);
                        m_CurrentRect.y      += barSize.y;
                        m_CurrentRect.x      =  layoutX;
                        layoutX              += m_CurrentRect.width;

                        if(GUI.Button(m_CurrentRect, angleMinusLabel, EditorStyles.miniButtonRight))
                        {
                            rotationSnap /= 2.0f;
                            modified     =  true;
                        }

                        //(x:592.00, y:2.00, width:17.00, height:15.00)
                        TooltipUtility.SetToolTip(angleMinnusTooltip, m_CurrentRect);
                    }

#endregion

                    layoutX += 2;

#region "Scale" label

                    if(viewWidth >= 750)
                    {
                        if(viewWidth >= 865)
                        {
                            m_CurrentRect.width  =  31;
                            m_CurrentRect.y      =  1;
                            m_CurrentRect.height =  layoutHeight - m_CurrentRect.y;
                            m_CurrentRect.y      += barSize.y;
                            m_CurrentRect.x      =  layoutX;
                            layoutX              += m_CurrentRect.width;

                            GUI.Label(m_CurrentRect, scaleLargeLabel, m_MiniTextStyle);
                            //(x:613.00, y:2.00, width:31.00, height:16.00)
                        }
                        else
                        {
                            m_CurrentRect.width  =  19;
                            m_CurrentRect.y      =  1;
                            m_CurrentRect.height =  layoutHeight - m_CurrentRect.y;
                            m_CurrentRect.y      += barSize.y;
                            m_CurrentRect.x      =  layoutX;
                            layoutX              += m_CurrentRect.width;

                            GUI.Label(m_CurrentRect, scaleSmallLabel, m_MiniTextStyle);
                            //(x:495.00, y:2.00, width:19.00, height:16.00)
                        }

                        TooltipUtility.SetToolTip(scaleTooltip, m_CurrentRect);
                    }

#endregion

                    layoutX += 2;

#region "Scale" field

                    GUI.changed = false;

                    m_CurrentRect.width  =  70;
                    m_CurrentRect.y      =  3;
                    m_CurrentRect.height =  layoutHeight - (m_CurrentRect.y - 1);
                    m_CurrentRect.y      += barSize.y;
                    m_CurrentRect.x      =  layoutX;
                    layoutX              += m_CurrentRect.width;

                    scaleSnap = EditorGUI.FloatField(m_CurrentRect, scaleSnap, m_TextInputStyle);

                    //(x:648.00, y:3.00, width:70.00, height:16.00)
                    if(viewWidth <= 750)
                    {
                        TooltipUtility.SetToolTip(scaleTooltip, m_CurrentRect);
                    }

                    modified = GUI.changed || modified;

#endregion

                    layoutX++;

#region "Scale" Unit

                    if(viewWidth >= 370)
                    {
                        m_CurrentRect.width  =  15;
                        m_CurrentRect.y      =  1;
                        m_CurrentRect.height =  layoutHeight - m_CurrentRect.y;
                        m_CurrentRect.y      += barSize.y;
                        m_CurrentRect.x      =  layoutX;
                        layoutX              += m_CurrentRect.width;

                        GUI.Label(m_CurrentRect, scaleUnitLabel, m_MiniTextStyle);
                        //(x:722.00, y:2.00, width:15.00, height:16.00)
                    }

#endregion

                    layoutX += 2;

#region "Scale" +/-

                    if(viewWidth >= 700)
                    {
                        m_CurrentRect.width  =  19;
                        m_CurrentRect.y      =  2;
                        m_CurrentRect.height =  layoutHeight - (m_CurrentRect.y + 1);
                        m_CurrentRect.y      += barSize.y;
                        m_CurrentRect.x      =  layoutX;
                        layoutX              += m_CurrentRect.width;

                        if(GUI.Button(m_CurrentRect, scalePlusLabel, EditorStyles.miniButtonLeft))
                        {
                            scaleSnap *= 10.0f;
                            modified  =  true;
                        }

                        //(x:741.00, y:2.00, width:19.00, height:15.00)
                        TooltipUtility.SetToolTip(scalePlusTooltip, m_CurrentRect);

                        m_CurrentRect.width  =  17;
                        m_CurrentRect.y      =  2;
                        m_CurrentRect.height =  layoutHeight - (m_CurrentRect.y + 1);
                        m_CurrentRect.y      += barSize.y;
                        m_CurrentRect.x      =  layoutX;
                        layoutX              += m_CurrentRect.width;

                        if(GUI.Button(m_CurrentRect, scaleMinusLabel, EditorStyles.miniButtonRight))
                        {
                            scaleSnap /= 10.0f;
                            modified  =  true;
                        }

                        //(x:760.00, y:2.00, width:17.00, height:15.00)
                        TooltipUtility.SetToolTip(scaleMinnusTooltip, m_CurrentRect);
                    }

#endregion
                }
            }

            float prevLayoutX = layoutX;

            layoutX = viewWidth;

#region "Rebuild"

            m_CurrentRect.width  =  27;
            m_CurrentRect.y      =  0;
            m_CurrentRect.height =  layoutHeight - m_CurrentRect.y;
            m_CurrentRect.y      += barSize.y;
            layoutX              -= m_CurrentRect.width;
            m_CurrentRect.x      =  layoutX;

            if(GUI.Button(m_CurrentRect, CsgguiStyleUtility.Skin.rebuildIcon, EditorStyles.toolbarButton))
            {
                // TODO: make logging optional
                Debug.Log("Starting complete rebuild");

                StringBuilder text = new StringBuilder();

                MaterialUtility.ResetMaterialTypeLookup();

                InternalCSGModelManager.SkipCheckForChanges = true;
                CSGSettings.Reload();
                UnityCompilerDefineManager.UpdateUnityDefines();

                InternalCSGModelManager.HierarchyValidateTime = 0.0;
                InternalCSGModelManager.UpdateHierarchyTime   = 0.0;

                double startTime = EditorApplication.timeSinceStartup;
                InternalCSGModelManager.ForceRebuildAll();
                InternalCSGModelManager.OnHierarchyModified();
                double hierarchyUpdateEndTime = EditorApplication.timeSinceStartup;

                text.AppendFormat(CultureInfo.InvariantCulture, "Full hierarchy rebuild in {0:F} ms. ", (hierarchyUpdateEndTime - startTime) * 1000);

                NativeMethodBindings.RebuildAll();
                double csgEndTime = EditorApplication.timeSinceStartup;

                text.AppendFormat(CultureInfo.InvariantCulture, "Full CSG rebuild done in {0:F} ms. ", (csgEndTime - hierarchyUpdateEndTime) * 1000);

                InternalCSGModelManager.RemoveForcedUpdates(); // we already did this in rebuild all
                InternalCSGModelManager.UpdateMeshes(text, true);

                updateSurfaces = true;
                UpdateLoop.ResetUpdateRoutine();
                CSGSettings.Save();
                InternalCSGModelManager.SkipCheckForChanges = false;

                text.AppendFormat(CultureInfo.InvariantCulture, "{0} brushes. ", CSGManager.TreeBrushCount);

                Debug.Log(text.ToString());
            }

            //(x:1442.00, y:0.00, width:27.00, height:18.00)
            TooltipUtility.SetToolTip(rebuildTooltip, m_CurrentRect);

#endregion

            if(viewWidth >= 800)
            {
                layoutX -= 6; //(x:1436.00, y:0.00, width:6.00, height:6.00)
            }

#region "Helper Surface Flags" Mask

            if(viewWidth >= 250)
            {
                GUI.changed = false;

                prevLayoutX += 8;  // extra space
                prevLayoutX += 26; // width of "Show wireframe" button

                m_CurrentRect.width = Mathf.Max(20, Mathf.Min(165, viewWidth - prevLayoutX - m_CurrentRect.width));

                m_CurrentRect.y      =  0;
                m_CurrentRect.height =  layoutHeight - m_CurrentRect.y;
                m_CurrentRect.y      += barSize.y;
                layoutX              -= m_CurrentRect.width;
                m_CurrentRect.x      =  layoutX;

                SurfaceVisibilityPopup.Button(sceneView, m_CurrentRect);

                //(x:1267.00, y:2.00, width:165.00, height:16.00)
                TooltipUtility.SetToolTip(helperSurfacesTooltip, m_CurrentRect);

                if(GUI.changed)
                {
                    updateSurfaces = true;
                    modified       = true;
                }
            }

#endregion

#region "Show wireframe" button

            GUI.changed          =  false;
            m_CurrentRect.width  =  26;
            m_CurrentRect.y      =  0;
            m_CurrentRect.height =  layoutHeight - m_CurrentRect.y;
            m_CurrentRect.y      += barSize.y;
            layoutX              -= m_CurrentRect.width;
            m_CurrentRect.x      =  layoutX;

            //(x:1237.00, y:0.00, width:26.00, height:18.00)
            showWireframe = GUI.Toggle(m_CurrentRect, showWireframe, showWireframe ? CsgguiStyleUtility.Skin.wireframe : CsgguiStyleUtility.Skin.wireframeOn, EditorStyles.toolbarButton);

            //(x:1237.00, y:0.00, width:26.00, height:18.00)
            TooltipUtility.SetToolTip(showWireframeTooltip, m_CurrentRect);

            if(GUI.changed)
            {
                wireframeModified = true;
                modified          = true;
            }

#endregion

#region Capture mouse clicks in empty space

            Vector2 mousePoint = Event.current.mousePosition;
            int     controlID  = GUIUtility.GetControlID(bottomBarEditorOverlayHash, FocusType.Passive, barSize);

            switch(Event.current.GetTypeForControl(controlID))
            {
                case EventType.MouseDown:
                {
                    if(barSize.Contains(mousePoint))
                    {
                        GUIUtility.hotControl      = controlID;
                        GUIUtility.keyboardControl = controlID;
                        Event.current.Use();
                    }

                    break;
                }
                case EventType.MouseMove:
                {
                    if(barSize.Contains(mousePoint))
                    {
                        Event.current.Use();
                    }

                    break;
                }
                case EventType.MouseUp:
                {
                    if(GUIUtility.hotControl == controlID)
                    {
                        GUIUtility.hotControl      = 0;
                        GUIUtility.keyboardControl = 0;
                        Event.current.Use();
                    }

                    break;
                }
                case EventType.MouseDrag:
                {
                    if(GUIUtility.hotControl == controlID)
                    {
                        Event.current.Use();
                    }

                    break;
                }
                case EventType.ScrollWheel:
                {
                    if(barSize.Contains(mousePoint))
                    {
                        Event.current.Use();
                    }

                    break;
                }
            }

#endregion

#region Store modified values

            rotationSnap     = Mathf.Max(1.0f, Mathf.Abs(360 + rotationSnap % 360) % 360);
            moveSnapVector.x = Mathf.Max(1.0f / 1024.0f, moveSnapVector.x);
            moveSnapVector.y = Mathf.Max(1.0f / 1024.0f, moveSnapVector.y);
            moveSnapVector.z = Mathf.Max(1.0f / 1024.0f, moveSnapVector.z);

            scaleSnap = Mathf.Max(MathConstants.MINIMUM_SCALE, scaleSnap);

            CSGSettings.SnapMode              = snapMode;
            CSGSettings.SnapVector            = moveSnapVector;
            CSGSettings.SnapRotation          = rotationSnap;
            CSGSettings.SnapScale             = scaleSnap;
            CSGSettings.UniformGrid           = uniformGrid;
            CSGSettings.GridVisible           = showGrid;
            CSGSettings.LockAxisX             = lockAxisX;
            CSGSettings.LockAxisY             = lockAxisY;
            CSGSettings.LockAxisZ             = lockAxisZ;
            CSGSettings.DistanceUnit          = distanceUnit;
            CSGSettings.VisibleHelperSurfaces = helperSurfaces;

            if(wireframeModified)
            {
                CSGSettings.SetWireframeShown(sceneView, showWireframe);
            }

            if(updateSurfaces)
            {
                MeshInstanceManager.UpdateHelperSurfaceVisibility(true);
            }

            if(modified)
            {
                GUI.changed = true;
                CSGSettings.UpdateSnapSettings();
                CSGSettings.Save();
                CSGEditorGUIUtility.RepaintAll();
            }

#endregion
        }
    }
}
