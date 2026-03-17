using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using InternalRealtimeCSG;
using RealtimeCSG.Components;
using RealtimeCSG.Foundation;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace RealtimeCSG
{
    internal sealed partial class CSGModelComponentInspectorGUI
    {
        private class MeshData
        {
            public int vertexCount;
            public int triangleCount;
            public long geometryHashValue;
            public long surfaceHashValue;
            public Mesh mesh;
        }

#region Workarounds

        private static Type m_ProbesType;
        private static object m_ProbesInstance;
        private static MethodInfo m_ProbesInitializeMethod;
        private static MethodInfo m_ProbesOnGUIMethod;
        private static MethodInfo m_SceneViewIsUsingDeferredRenderingPath;
        private static bool m_HaveReflected;

        private static void InitReflection() // le *sigh*
        {
            if(m_HaveReflected)
            {
                return;
            }

            m_HaveReflected = true;
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

            m_ProbesType     = types.FirstOrDefault(t => t.FullName == "UnityEditor.RendererEditorBase+Probes");
            m_ProbesInstance = Activator.CreateInstance(m_ProbesType!);

            MethodInfo[] methods = m_ProbesType.GetMethods(BindingFlags.Instance | BindingFlags.NonPublic);

            for(int i = 0; i < methods.Length; i++)
            {
                //internal void Initialize(SerializedObject serializedObject)
                if(methods[i].Name == "Initialize")
                {
                    if(methods[i].GetParameters().Length == 1)
                    {
                        m_ProbesInitializeMethod = methods[i];
                    }
                }
                else //internal void OnGUI(UnityEngine.Object[] selection, Renderer renderer, bool useMiniStyle)
                {
                    if(methods[i].Name == "OnGUI")
                    {
                        if(methods[i].GetParameters().Length == 3)
                        {
                            m_ProbesOnGUIMethod = methods[i];
                        }
                    }
                }
            }

            methods = typeof(SceneView).GetMethods(
                BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Static
            );

            for(int i = 0; i < methods.Length; i++)
            {
                if(methods[i].Name == "IsUsingDeferredRenderingPath")
                {
                    m_SceneViewIsUsingDeferredRenderingPath = methods[i];
                }
            }
        }

        internal static Camera GetMainCamera()
        {
            // main camera, if we have any
            Camera mainCamera = Camera.main;

            if(mainCamera != null)
            {
                return mainCamera;
            }

            // if we have one camera, return it
            Camera[] allCameras = Camera.allCameras;

            if(allCameras != null && allCameras.Length == 1)
            {
                return allCameras[0];
            }

            // otherwise no "main" camera
            return null;
        }

        internal static RenderingPath GetSceneViewRenderingPath()
        {
            Camera mainCamera = GetMainCamera();

            return mainCamera != null ? mainCamera.renderingPath : RenderingPath.UsePlayerSettings;
        }

        internal static bool IsUsingDeferredRenderingPath()
        {
            if(m_SceneViewIsUsingDeferredRenderingPath != null)
            {
                object ret = m_SceneViewIsUsingDeferredRenderingPath.Invoke(null, null);

                return (bool)ret;
            }

            return false;
        }

#endregion

        private static bool m_ProbesInitialized;
        private static Object[] m_ProbesTargets;
        private static SerializedObject m_ProbesSerializedObject;

        private static void UpdateTargets(CSGModel[] models)
        {
            m_ProbesInitialized      = false;
            m_ProbesTargets          = null;
            m_ProbesSerializedObject = null;

            if(models.Length == 0)
            {
                return;
            }

            m_ProbesTargets = MeshInstanceManager.FindRenderers(models);

            if(m_ProbesTargets.Length == 0 || m_ProbesTargets == null)
            {
                return;
            }

            m_ProbesSerializedObject = new SerializedObject(m_ProbesTargets);

            InitReflection();

            if(m_ProbesInstance != null &&
               m_ProbesInitializeMethod != null &&
               m_ProbesOnGUIMethod != null)
            {
                if(m_ProbesTargets.Length > 0)
                {
                    m_ProbesInitializeMethod.Invoke(m_ProbesInstance, new object[] { m_ProbesSerializedObject });
                    m_ProbesInitialized = true;
                }
            }
        }

        private static GUIStyle m_PopupStyle;

        private static Material m_DummyMaterial;
        private static bool m_LocalStyles;

        public static void OnInspectorGUI(Object[] targets)
        {
            InitReflection();

            if(!m_LocalStyles)
            {
                m_PopupStyle            =  new GUIStyle(EditorStyles.popup);
                m_PopupStyle.margin.top += 2;
                m_LocalStyles           =  true;
            }

            bool updateMeshes = false;

            CSGModel[] models = new CSGModel[targets.Length];

            for(int i = targets.Length - 1; i >= 0; i--)
            {
                models[i] = targets[i] as CSGModel;

                if(!models[i])
                {
                    ArrayUtility.RemoveAt(ref models, i);
                }
            }

            if(models.Length == 0)
            {
                return;
            }

            float? AngleError = models[0].angleError;
            float? AreaError  = models[0].areaError;
            float? HardAngle  = models[0].hardAngle;
            float? PackMargin = models[0].packMargin;

            StaticEditorFlags StaticFlags = GameObjectUtility.GetStaticEditorFlags(models[0].gameObject);
            bool? GenerateLightMaps = (StaticFlags & StaticEditorFlags.ContributeGI) == StaticEditorFlags.ContributeGI;
            ModelSettingsFlags Settings = models[0].Settings;
            VertexChannelFlags VertexChannels = models[0].VertexChannels;
            MeshColliderCookingOptions CookingOptions = models[0].MeshColliderCookingOptions;
            ExportType? ExportType = models[0].exportType;
            OriginType? OriginType = models[0].originType;
            ReceiveGI? MeshReceiveGI = models[0].ReceiveGI;

            bool? ExportColliders = models[0].exportColliders;
            bool? VertexChannelTangent = (VertexChannels & VertexChannelFlags.Tangent) == VertexChannelFlags.Tangent;
            bool? VertexChannelNormal = (VertexChannels & VertexChannelFlags.Normal) == VertexChannelFlags.Normal;
            bool? VertexChannelUV0 = (VertexChannels & VertexChannelFlags.UV0) == VertexChannelFlags.UV0;
            bool? InvertedWorld = (Settings & ModelSettingsFlags.InvertedWorld) == ModelSettingsFlags.InvertedWorld;
            bool? NoCollider = (Settings & ModelSettingsFlags.NoCollider) == ModelSettingsFlags.NoCollider;
            bool? IsTrigger = (Settings & ModelSettingsFlags.IsTrigger) == ModelSettingsFlags.IsTrigger;

            bool? SetToConvex = (Settings & ModelSettingsFlags.SetColliderConvex)
             == ModelSettingsFlags.SetColliderConvex;

            bool? AutoGenerateRigidBody = (Settings & ModelSettingsFlags.AutoUpdateRigidBody)
             == ModelSettingsFlags.AutoUpdateRigidBody;

            bool? DoNotRender = (Settings & ModelSettingsFlags.DoNotRender) == ModelSettingsFlags.DoNotRender;

            bool? TwoSidedShadows =
                (Settings & ModelSettingsFlags.TwoSidedShadows) == ModelSettingsFlags.TwoSidedShadows;

            bool? AutoRebuildUVs = (Settings & ModelSettingsFlags.AutoRebuildUVs) == ModelSettingsFlags.AutoRebuildUVs;
            bool? PreserveUVs    = (Settings & ModelSettingsFlags.PreserveUVs) == ModelSettingsFlags.PreserveUVs;

            bool? StitchLightmapSeams = (Settings & ModelSettingsFlags.StitchLightmapSeams)
             == ModelSettingsFlags.StitchLightmapSeams;

            bool? IgnoreNormals = (Settings & ModelSettingsFlags.IgnoreNormals) == ModelSettingsFlags.IgnoreNormals;

            bool? CookForFasterSimulation = (CookingOptions & MeshColliderCookingOptions.CookForFasterSimulation)
             == MeshColliderCookingOptions.CookForFasterSimulation;

            bool? EnableMeshCleaning = (CookingOptions & MeshColliderCookingOptions.EnableMeshCleaning)
             == MeshColliderCookingOptions.EnableMeshCleaning;

            bool? WeldColocatedVertices = (CookingOptions & MeshColliderCookingOptions.WeldColocatedVertices)
             == MeshColliderCookingOptions.WeldColocatedVertices;

            int?   MinimumChartSize  = models[0].minimumChartSize;
            float? AutoUVMaxDistance = models[0].autoUVMaxDistance;
            float? AutoUVMaxAngle    = models[0].autoUVMaxAngle;

            float?          ScaleInLightmap             = models[0].scaleInLightmap;
            bool?           ShowGeneratedMeshes         = models[0].ShowGeneratedMeshes;
            PhysicsMaterial defaultPhysicsMaterial      = models[0].DefaultPhysicsMaterial;
            bool            defaultPhysicsMaterialMixed = false;

            for(int i = 1; i < models.Length; i++)
            {
                Settings       = models[i].Settings;
                VertexChannels = models[i].VertexChannels;
                CookingOptions = models[i].MeshColliderCookingOptions;

                StaticEditorFlags currStaticFlags = GameObjectUtility.GetStaticEditorFlags(models[i].gameObject);

                bool currGenerateLightMaps =
                    (currStaticFlags & StaticEditorFlags.ContributeGI) == StaticEditorFlags.ContributeGI;

                ExportType currExportType = models[i].exportType;
                OriginType currOriginType = models[i].originType;
                ReceiveGI  currReceiveGI  = models[i].ReceiveGI;

                bool  currExportColliders = models[i].exportColliders;
                float currAngleError      = models[i].angleError;
                float currAreaError       = models[i].areaError;
                float currHardAngle       = models[i].hardAngle;
                float currPackMargin      = models[i].packMargin;

                bool currVertexChannelTangent =
                    (VertexChannels & VertexChannelFlags.Tangent) == VertexChannelFlags.Tangent;

                bool currVertexChannelNormal =
                    (VertexChannels & VertexChannelFlags.Normal) == VertexChannelFlags.Normal;

                bool currVertexChannelUV0 = (VertexChannels & VertexChannelFlags.UV0) == VertexChannelFlags.UV0;

                bool currInvertedWorld =
                    (Settings & ModelSettingsFlags.InvertedWorld) == ModelSettingsFlags.InvertedWorld;

                bool currNoCollider = (Settings & ModelSettingsFlags.NoCollider) == ModelSettingsFlags.NoCollider;
                bool currIsTrigger  = (Settings & ModelSettingsFlags.IsTrigger) == ModelSettingsFlags.IsTrigger;

                bool currSetToConvex = (Settings & ModelSettingsFlags.SetColliderConvex)
                 == ModelSettingsFlags.SetColliderConvex;

                bool currAutoGenerateRigidBody = (Settings & ModelSettingsFlags.AutoUpdateRigidBody)
                 == ModelSettingsFlags.AutoUpdateRigidBody;

                bool currDoNotRender = (Settings & ModelSettingsFlags.DoNotRender) == ModelSettingsFlags.DoNotRender;

                bool currTwoSidedShadows = (Settings & ModelSettingsFlags.TwoSidedShadows)
                 == ModelSettingsFlags.TwoSidedShadows;

                bool currAutoRebuildUVs =
                    (Settings & ModelSettingsFlags.AutoRebuildUVs) == ModelSettingsFlags.AutoRebuildUVs;

                bool currPreserveUVs = (Settings & ModelSettingsFlags.PreserveUVs) == ModelSettingsFlags.PreserveUVs;

                bool currStitchLightmapSeams = (Settings & ModelSettingsFlags.StitchLightmapSeams)
                 == ModelSettingsFlags.StitchLightmapSeams;

                bool currCookForFasterSimulation = (CookingOptions & MeshColliderCookingOptions.CookForFasterSimulation)
                 == MeshColliderCookingOptions.CookForFasterSimulation;

                bool currEnableMeshCleaning = (CookingOptions & MeshColliderCookingOptions.EnableMeshCleaning)
                 == MeshColliderCookingOptions.EnableMeshCleaning;

                bool currWeldColocatedVertices = (CookingOptions & MeshColliderCookingOptions.WeldColocatedVertices)
                 == MeshColliderCookingOptions.WeldColocatedVertices;

                int             currMinimumChartSize       = models[i].minimumChartSize;
                float           currAutoUVMaxDistance      = models[i].autoUVMaxDistance;
                float           currAutoUVMaxAngle         = models[i].autoUVMaxAngle;
                bool            currShowGeneratedMeshes    = models[i].ShowGeneratedMeshes;
                float           currScaleInLightmap        = models[i].scaleInLightmap;
                PhysicsMaterial currdefaultPhysicsMaterial = models[i].DefaultPhysicsMaterial;

                if(VertexChannelTangent.HasValue && VertexChannelTangent.Value != currVertexChannelTangent)
                {
                    VertexChannelTangent = null;
                }

                if(VertexChannelNormal.HasValue && VertexChannelNormal.Value != currVertexChannelNormal)
                {
                    VertexChannelNormal = null;
                }

                if(VertexChannelUV0.HasValue && VertexChannelUV0.Value != currVertexChannelUV0)
                {
                    VertexChannelUV0 = null;
                }

                if(ExportType.HasValue && ExportType.Value != currExportType)
                {
                    ExportType = null;
                }

                if(OriginType.HasValue && OriginType.Value != currOriginType)
                {
                    OriginType = null;
                }

                if(MeshReceiveGI.HasValue && MeshReceiveGI.Value != currReceiveGI)
                {
                    MeshReceiveGI = null;
                }

                if(ExportColliders.HasValue && ExportColliders.Value != currExportColliders)
                {
                    ExportColliders = null;
                }

                if(InvertedWorld.HasValue && InvertedWorld.Value != currInvertedWorld)
                {
                    InvertedWorld = null;
                }

                if(NoCollider.HasValue && NoCollider.Value != currNoCollider)
                {
                    NoCollider = null;
                }

                if(IsTrigger.HasValue && IsTrigger.Value != currIsTrigger)
                {
                    IsTrigger = null;
                }

                if(SetToConvex.HasValue && SetToConvex.Value != currSetToConvex)
                {
                    SetToConvex = null;
                }

                if(AutoGenerateRigidBody.HasValue && AutoGenerateRigidBody.Value != currAutoGenerateRigidBody)
                {
                    AutoGenerateRigidBody = null;
                }

                if(DoNotRender.HasValue && DoNotRender.Value != currDoNotRender)
                {
                    DoNotRender = null;
                }

                if(TwoSidedShadows.HasValue && TwoSidedShadows.Value != currTwoSidedShadows)
                {
                    TwoSidedShadows = null;
                }

                if(AutoRebuildUVs.HasValue && AutoRebuildUVs.Value != currAutoRebuildUVs)
                {
                    AutoRebuildUVs = null;
                }

                if(PreserveUVs.HasValue && PreserveUVs.Value != currPreserveUVs)
                {
                    PreserveUVs = null;
                }

                if(StitchLightmapSeams.HasValue && StitchLightmapSeams.Value != currStitchLightmapSeams)
                {
                    StitchLightmapSeams = null;
                }

                if(CookForFasterSimulation.HasValue && CookForFasterSimulation.Value != currCookForFasterSimulation)
                {
                    CookForFasterSimulation = null;
                }

                if(EnableMeshCleaning.HasValue && EnableMeshCleaning.Value != currEnableMeshCleaning)
                {
                    EnableMeshCleaning = null;
                }

                if(WeldColocatedVertices.HasValue && WeldColocatedVertices.Value != currWeldColocatedVertices)
                {
                    WeldColocatedVertices = null;
                }

                if(ScaleInLightmap.HasValue && ScaleInLightmap.Value != currScaleInLightmap)
                {
                    ScaleInLightmap = null;
                }

                if(ShowGeneratedMeshes.HasValue && ShowGeneratedMeshes.Value != currShowGeneratedMeshes)
                {
                    ShowGeneratedMeshes = null;
                }

                if(MinimumChartSize.HasValue && MinimumChartSize.Value != currMinimumChartSize)
                {
                    MinimumChartSize = null;
                }

                if(AutoUVMaxDistance.HasValue && AutoUVMaxDistance.Value != currAutoUVMaxDistance)
                {
                    AutoUVMaxDistance = null;
                }

                if(AutoUVMaxAngle.HasValue && AutoUVMaxAngle.Value != currAutoUVMaxAngle)
                {
                    AutoUVMaxAngle = null;
                }

                if(AngleError.HasValue && AngleError.Value != currAngleError)
                {
                    AngleError = null;
                }

                if(AreaError.HasValue && AreaError.Value != currAreaError)
                {
                    AreaError = null;
                }

                if(HardAngle.HasValue && HardAngle.Value != currHardAngle)
                {
                    HardAngle = null;
                }

                if(PackMargin.HasValue && PackMargin.Value != currPackMargin)
                {
                    PackMargin = null;
                }

                if(GenerateLightMaps.HasValue && GenerateLightMaps.Value != currGenerateLightMaps)
                {
                    GenerateLightMaps = null;
                }

                if(defaultPhysicsMaterial != currdefaultPhysicsMaterial)
                {
                    defaultPhysicsMaterialMixed = true;
                }
            }

            bool behaviourVisible    = SessionState.GetBool("CSGModel.Behaviour", true);
            bool exportVisible       = SessionState.GetBool("CSGModel.Export", true);
            bool physicsVisible      = SessionState.GetBool("CSGModel.Physics", true);
            bool renderingVisible    = SessionState.GetBool("CSGModel.Rendering", true);
            bool UVSettingsVisible   = SessionState.GetBool("CSGModel.UVSettings", true);
            bool meshAdvancedVisible = SessionState.GetBool("CSGModel.MeshAdvanced", true);
            bool statisticsVisible   = SessionState.GetBool("CSGModel.Statistics", true);

            GUILayout.BeginVertical(GUI.skin.box);

            {
                EditorGUI.BeginChangeCheck();
                behaviourVisible = EditorGUILayout.Foldout(behaviourVisible, "Behaviour");

                if(EditorGUI.EndChangeCheck())
                {
                    SessionState.SetBool("CSGModel.Behaviour", behaviourVisible);
                }

                if(behaviourVisible)
                {
                    EditorGUI.indentLevel++;

                    {
                        bool invertedWorld = InvertedWorld.HasValue && InvertedWorld.Value;
                        EditorGUI.BeginChangeCheck();

                        {
                            EditorGUI.showMixedValue = !InvertedWorld.HasValue;
                            invertedWorld            = EditorGUILayout.Toggle(invertedWorldContent, invertedWorld);
                        }

                        if(EditorGUI.EndChangeCheck())
                        {
                            for(int i = 0; i < models.Length; i++)
                            {
                                CSGModel model = models[i];

                                if(invertedWorld)
                                {
                                    model.Settings |= ModelSettingsFlags.InvertedWorld;
                                }
                                else
                                {
                                    model.Settings &= ~ModelSettingsFlags.InvertedWorld;
                                }
                            }

                            GUI.changed = true;
                        }
                    }

                    EditorGUI.indentLevel--;
                }
            }

            GUILayout.EndVertical();

            if(behaviourVisible)
            {
                GUILayout.Space(10);
            }

            if(models != null && models.Length == 1)
            {
                GUILayout.BeginVertical(GUI.skin.box);

                {
                    EditorGUI.BeginChangeCheck();
                    exportVisible = EditorGUILayout.Foldout(exportVisible, exportLabel);

                    if(EditorGUI.EndChangeCheck())
                    {
                        SessionState.SetBool("CSGModel.Export", exportVisible);
                    }

                    if(exportVisible)
                    {
                        {
                            GUILayout.BeginHorizontal();

                            {
                                GUILayout.Space(20);
                                EditorGUI.BeginChangeCheck();

                                {
                                    EditorGUI.showMixedValue = !OriginType.HasValue;

                                    OriginType = (OriginType)EditorGUILayout.EnumPopup(
                                        exportOriginLabel, OriginType ?? Components.OriginType.ModelCenter, m_PopupStyle
                                    );

                                    EditorGUI.showMixedValue = false;
                                }

                                if(EditorGUI.EndChangeCheck() && OriginType.HasValue)
                                {
                                    for(int i = 0; i < models.Length; i++)
                                    {
                                        models[i].originType = OriginType.Value;
                                    }
                                }
                            }

                            GUILayout.EndHorizontal();
                            GUILayout.BeginHorizontal();

                            {
                                GUILayout.Space(20);
                                bool exportColliderToggle = ExportColliders ?? true;
                                EditorGUI.BeginChangeCheck();

                                {
                                    EditorGUI.showMixedValue = !OriginType.HasValue;

                                    exportColliderToggle = EditorGUILayout.Toggle(
                                        exportColliderLabel, exportColliderToggle
                                    );

                                    EditorGUI.showMixedValue = false;
                                }

                                if(EditorGUI.EndChangeCheck() && OriginType.HasValue)
                                {
                                    for(int i = 0; i < models.Length; i++)
                                    {
                                        models[i].exportColliders = exportColliderToggle;
                                    }

                                    ExportColliders = exportColliderToggle;
                                }
                            }

                            GUILayout.EndHorizontal();
                            GUILayout.BeginHorizontal();

                            {
                                GUILayout.Space(20);
                                EditorGUI.BeginDisabledGroup(!ExportType.HasValue);

                                {
                                    if(EditModeCommonGUI.IndentableButton(exportToButtonLabel) && ExportType.HasValue)
                                    {
                                        MeshInstanceManager.Export(
                                            models[0], ExportType.Value, ExportColliders ?? true
                                        );
                                    }
                                }

                                EditorGUI.EndDisabledGroup();
                                EditorGUI.BeginChangeCheck();

                                {
                                    EditorGUI.showMixedValue = !ExportType.HasValue;

                                    ExportType = (ExportType)EditorGUILayout.EnumPopup(
                                        ExportType ?? Components.ExportType.FBX, m_PopupStyle
                                    );

                                    EditorGUI.showMixedValue = false;
                                }

                                if(EditorGUI.EndChangeCheck() && ExportType.HasValue)
                                {
                                    for(int i = 0; i < models.Length; i++)
                                    {
                                        models[i].exportType = ExportType.Value;
                                    }
                                }
                            }

                            GUILayout.EndHorizontal();
                        }
                    }
                }

                GUILayout.EndVertical();

                if(exportVisible)
                {
                    GUILayout.Space(10);
                }
            }

            GUILayout.BeginVertical(GUI.skin.box);

            {
                EditorGUI.BeginChangeCheck();
                physicsVisible = EditorGUILayout.Foldout(physicsVisible, "Physics");

                if(EditorGUI.EndChangeCheck())
                {
                    SessionState.SetBool("CSGModel.Physics", physicsVisible);
                }

                if(physicsVisible)
                {
                    EditorGUI.indentLevel++;

                    {
                        bool colliderValue = NoCollider.HasValue && NoCollider.Value;
                        EditorGUI.BeginChangeCheck();

                        {
                            EditorGUI.showMixedValue = !NoCollider.HasValue;
                            colliderValue            = !EditorGUILayout.Toggle(generateColliderContent, !colliderValue);
                        }

                        if(EditorGUI.EndChangeCheck())
                        {
                            for(int i = 0; i < models.Length; i++)
                            {
                                CSGModel model = models[i];

                                if(colliderValue)
                                {
                                    model.Settings |= ModelSettingsFlags.NoCollider;
                                }
                                else
                                {
                                    model.Settings &= ~ModelSettingsFlags.NoCollider;
                                }
                            }

                            GUI.changed  = true;
                            NoCollider   = colliderValue;
                            updateMeshes = true;
                        }
                    }

                    bool haveNoCollider = NoCollider.HasValue && NoCollider.Value;
                    EditorGUI.BeginDisabledGroup(haveNoCollider);

                    {
                        bool triggerValueMixed = haveNoCollider || !IsTrigger.HasValue;
                        bool triggerValue      = IsTrigger.HasValue && IsTrigger.Value;

                        {
                            EditorGUI.BeginChangeCheck();

                            {
                                EditorGUI.showMixedValue = triggerValueMixed;
                                triggerValue             = EditorGUILayout.Toggle(modelIsTriggerContent, triggerValue);
                            }

                            if(EditorGUI.EndChangeCheck())
                            {
                                for(int i = 0; i < models.Length; i++)
                                {
                                    CSGModel model = models[i];

                                    if(triggerValue)
                                    {
                                        model.Settings |= ModelSettingsFlags.IsTrigger;
                                    }
                                    else
                                    {
                                        model.Settings &= ~ModelSettingsFlags.IsTrigger;
                                    }
                                }

                                GUI.changed  = true;
                                updateMeshes = true;
                            }
                        }

                        bool setConvexValueMixed = haveNoCollider || !SetToConvex.HasValue;
                        bool setConvexValue      = !haveNoCollider && (SetToConvex.HasValue && SetToConvex.Value);

                        {
                            EditorGUI.BeginChangeCheck();

                            {
                                EditorGUI.showMixedValue = setConvexValueMixed;
                                Color prevColor = GUI.color;

                                if(!setConvexValue && triggerValue)
                                {
                                    Color color = new Color(1, 0.25f, 0.25f);
                                    GUI.color = color;
                                }

                                setConvexValue = EditorGUILayout.Toggle(colliderSetToConvexContent, setConvexValue);
                                GUI.color      = prevColor;
                            }

                            if(EditorGUI.EndChangeCheck())
                            {
                                for(int i = 0; i < models.Length; i++)
                                {
                                    if(setConvexValue)
                                    {
                                        models[i].Settings |= ModelSettingsFlags.SetColliderConvex;
                                    }
                                    else
                                    {
                                        models[i].Settings &= ~ModelSettingsFlags.SetColliderConvex;
                                    }
                                }

                                GUI.changed  = true;
                                updateMeshes = true;
                            }
                        }

                        {
                            EditorGUI.BeginChangeCheck();

                            {
                                EditorGUI.showMixedValue = defaultPhysicsMaterialMixed;
                                GUILayout.BeginHorizontal();
                                EditorGUILayout.PrefixLabel(defaultPhysicsMaterialContent);

                                defaultPhysicsMaterial = EditorGUILayout.ObjectField(
                                    defaultPhysicsMaterial, typeof(PhysicsMaterial), true
                                ) as PhysicsMaterial;

                                GUILayout.EndHorizontal();
                            }

                            if(EditorGUI.EndChangeCheck())
                            {
                                if(!defaultPhysicsMaterial)
                                {
                                    defaultPhysicsMaterial = MaterialUtility.DefaultPhysicsMaterial;
                                }

                                for(int i = 0; i < models.Length; i++)
                                {
                                    models[i].DefaultPhysicsMaterial = defaultPhysicsMaterial;
                                }

                                GUI.changed  = true;
                                updateMeshes = true;
                            }
                        }

                        if(!haveNoCollider && !setConvexValue && triggerValue)
                        {
                            Color prevColor = GUI.color;
                            Color color     = new Color(1, 0.25f, 0.25f);
                            GUI.color = color;
                            GUILayout.Label("Warning:\r\nFor performance reasons colliders need to\r\nbe convex!");

                            GUI.color = prevColor;
                        }
                    }

                    EditorGUI.EndDisabledGroup();

                    {
                        bool autoRigidbody = AutoGenerateRigidBody.HasValue && AutoGenerateRigidBody.Value;
                        EditorGUI.BeginChangeCheck();

                        {
                            EditorGUI.showMixedValue = !AutoGenerateRigidBody.HasValue;
                            autoRigidbody = !EditorGUILayout.Toggle(colliderAutoRigidBodyContent, !autoRigidbody);
                        }

                        if(EditorGUI.EndChangeCheck())
                        {
                            for(int i = 0; i < models.Length; i++)
                            {
                                if(autoRigidbody)
                                {
                                    models[i].Settings |= ModelSettingsFlags.AutoUpdateRigidBody;
                                }
                                else
                                {
                                    models[i].Settings &= ~ModelSettingsFlags.AutoUpdateRigidBody;
                                }
                            }

                            GUI.changed = true;
                        }
                    }

                    EditorGUI.indentLevel--;
                }
            }

            GUILayout.EndVertical();

            if(physicsVisible)
            {
                GUILayout.Space(10);
            }

            GUILayout.BeginVertical(GUI.skin.box);

            {
                EditorGUI.BeginChangeCheck();
                renderingVisible = EditorGUILayout.Foldout(renderingVisible, "Rendering");

                if(EditorGUI.EndChangeCheck())
                {
                    SessionState.SetBool("CSGModel.Rendering", renderingVisible);
                }

                if(renderingVisible)
                {
                    EditorGUI.indentLevel++;

                    {
                        bool donotrenderValue = DoNotRender.HasValue && DoNotRender.Value;

                        EditorGUI.BeginChangeCheck();

                        {
                            EditorGUI.showMixedValue = !DoNotRender.HasValue;

                            donotrenderValue = EditorGUILayout.Toggle(doNotRenderContent, donotrenderValue);
                        }

                        if(EditorGUI.EndChangeCheck())
                        {
                            for(int i = 0; i < models.Length; i++)
                            {
                                CSGModel model = models[i];

                                if(donotrenderValue)
                                {
                                    model.Settings |= ModelSettingsFlags.DoNotRender;
                                }
                                else
                                {
                                    model.Settings &= ~ModelSettingsFlags.DoNotRender;
                                }
                            }

                            GUI.changed  = true;
                            DoNotRender  = donotrenderValue;
                            updateMeshes = true;
                        }
                    }

                    //EditorGUI.EndDisabledGroup();
                    {
                        bool twoSidedShadowsValue = TwoSidedShadows.HasValue && TwoSidedShadows.Value;

                        EditorGUI.BeginChangeCheck();

                        {
                            EditorGUI.showMixedValue = !TwoSidedShadows.HasValue;

                            twoSidedShadowsValue = EditorGUILayout.Toggle(twoSidedShadowsContent, twoSidedShadowsValue);
                        }

                        if(EditorGUI.EndChangeCheck())
                        {
                            for(int i = 0; i < models.Length; i++)
                            {
                                CSGModel model = models[i];

                                if(twoSidedShadowsValue)
                                {
                                    model.Settings |= ModelSettingsFlags.TwoSidedShadows;
                                }
                                else
                                {
                                    model.Settings &= ~ModelSettingsFlags.TwoSidedShadows;
                                }
                            }

                            GUI.changed  = true;
                            updateMeshes = true;
                        }
                    }

                    GUILayout.Space(10);

                    EditorGUI.showMixedValue = false;
                    UpdateTargets(models);

                    if(m_ProbesInstance != null &&
                       m_ProbesOnGUIMethod != null &&
                       m_ProbesTargets != null &&
                       m_ProbesSerializedObject != null &&
                       m_ProbesInitialized)
                    {
                        GUILayout.Space(10);

                        try
                        {
                            m_ProbesSerializedObject.UpdateIfRequiredOrScript();

                            m_ProbesOnGUIMethod.Invoke(
                                m_ProbesInstance, new object[] { m_ProbesTargets, (Renderer)m_ProbesTargets[0], false }
                            );

                            m_ProbesSerializedObject.ApplyModifiedProperties();
                        }
                        catch
                        {
                        }
                    }

                    EditorGUI.indentLevel--;
                }
            }

            GUILayout.EndVertical();

            if(renderingVisible)
            {
                GUILayout.Space(10);
            }

            GUILayout.BeginVertical(GUI.skin.box);

            {
                EditorGUI.BeginChangeCheck();
                UVSettingsVisible = EditorGUILayout.Foldout(UVSettingsVisible, "Lighting");

                if(EditorGUI.EndChangeCheck())
                {
                    SessionState.SetBool("CSGModel.UVSettings", UVSettingsVisible);
                }

                if(UVSettingsVisible)
                {
                    EditorGUI.indentLevel++;

                    {
                        EditorGUI.BeginChangeCheck();

                        {
                            EditorGUI.showMixedValue = !MeshReceiveGI.HasValue;

                            MeshReceiveGI = (ReceiveGI)EditorGUILayout.EnumPopup(
                                receiveGIContent, MeshReceiveGI ?? ReceiveGI.LightProbes, m_PopupStyle
                            );
                        }

                        if(EditorGUI.EndChangeCheck() && MeshReceiveGI.HasValue)
                        {
                            for(int i = 0; i < models.Length; i++)
                            {
                                CSGModel model = models[i];
                                model.ReceiveGI = MeshReceiveGI.Value;
                            }

                            GUI.changed  = true;
                            updateMeshes = true;
                        }
                    }

                    {
                        if(DoNotRender.HasValue && DoNotRender.Value)
                        {
                            Color prevColor = GUI.color;
                            Color color     = new Color(1, 0.25f, 0.25f);
                            GUI.color = color;
                            EditorGUILayout.LabelField("Rendering is disabled");
                            GUI.color = prevColor;
                        }
                        else
                        {
                            bool enable = !GenerateLightMaps.HasValue
                             || (GenerateLightMaps.HasValue && !GenerateLightMaps.Value);

                            GUIContent enableOrDisableButtonText = !GenerateLightMaps.HasValue
                                ? enableLightmapsForAllContent
                                : !GenerateLightMaps.Value
                                    ? enableLightmapsContent
                                    : disableLightmapsContent;

                            if(EditModeCommonGUI.IndentableButton(enableOrDisableButtonText))
                            {
                                for(int i = 0; i < models.Length; i++)
                                {
                                    StaticEditorFlags oldStaticFlags =
                                        GameObjectUtility.GetStaticEditorFlags(models[i].gameObject);

                                    StaticEditorFlags newStaticFlags;

                                    if(enable)
                                    {
                                        newStaticFlags = oldStaticFlags | StaticEditorFlags.ContributeGI;
                                    }
                                    else
                                    {
                                        newStaticFlags = oldStaticFlags & ~StaticEditorFlags.ContributeGI;
                                    }

                                    if(oldStaticFlags != newStaticFlags)
                                    {
                                        GameObjectUtility.SetStaticEditorFlags(models[i].gameObject, newStaticFlags);
                                        MeshInstanceManager.ClearUVs(models[i]);
                                    }
                                }
                            }

                            if(!GenerateLightMaps.HasValue || (GenerateLightMaps.HasValue && GenerateLightMaps.Value))
                            {
                                if(!DoNotRender.HasValue)
                                {
                                    Color prevColor = GUI.color;
                                    Color color     = new Color(1, 0.25f, 0.25f);
                                    GUI.color = color;
                                    EditorGUILayout.LabelField("Not all models have their rendering enabled");
                                    GUI.color = prevColor;
                                }

                                EditorGUILayout.LabelField("Unity UV Generation");
                                EditorGUI.indentLevel++;

                                {
                                    {
                                        float angleError = AngleError ?? 0;
                                        EditorGUI.BeginChangeCheck();

                                        {
                                            EditorGUI.showMixedValue = !AngleError.HasValue;

                                            angleError = EditorGUILayout.Slider(
                                                angleErrorContent, angleError, CSGModel.MinAngleError
                                              , CSGModel.MaxAngleError
                                            );
                                        }

                                        if(EditorGUI.EndChangeCheck())
                                        {
                                            for(int i = 0; i < models.Length; i++)
                                            {
                                                models[i].angleError = angleError;
                                                MeshInstanceManager.Refresh(models[i], onlyFastRefreshes: false);
                                            }

                                            GUI.changed  = true;
                                            updateMeshes = true;
                                        }
                                    }

                                    {
                                        float areaError = AreaError ?? 0;
                                        EditorGUI.BeginChangeCheck();

                                        {
                                            EditorGUI.showMixedValue = !AreaError.HasValue;

                                            areaError = EditorGUILayout.Slider(
                                                areaErrorContent, areaError, CSGModel.MinAreaError
                                              , CSGModel.MaxAreaError
                                            );
                                        }

                                        if(EditorGUI.EndChangeCheck())
                                        {
                                            for(int i = 0; i < models.Length; i++)
                                            {
                                                models[i].areaError = areaError;
                                                MeshInstanceManager.Refresh(models[i], onlyFastRefreshes: false);
                                            }

                                            GUI.changed  = true;
                                            updateMeshes = true;
                                        }
                                    }

                                    {
                                        float hardAngle = HardAngle ?? 0;
                                        EditorGUI.BeginChangeCheck();

                                        {
                                            EditorGUI.showMixedValue = !HardAngle.HasValue;
                                            hardAngle = EditorGUILayout.Slider(hardAngleContent, hardAngle, 0, 360);
                                        }

                                        if(EditorGUI.EndChangeCheck())
                                        {
                                            for(int i = 0; i < models.Length; i++)
                                            {
                                                models[i].hardAngle = hardAngle;
                                                MeshInstanceManager.Refresh(models[i], onlyFastRefreshes: false);
                                            }

                                            GUI.changed  = true;
                                            updateMeshes = true;
                                        }
                                    }

                                    {
                                        float packMargin = PackMargin ?? 0;
                                        EditorGUI.BeginChangeCheck();

                                        {
                                            EditorGUI.showMixedValue = !PackMargin.HasValue;

                                            packMargin = EditorGUILayout.FloatField(
                                                packMarginContent, (int)(packMargin * 1024.0f) / 1024.0f
                                            );
                                        }

                                        if(EditorGUI.EndChangeCheck())
                                        {
                                            for(int i = 0; i < models.Length; i++)
                                            {
                                                models[i].packMargin = packMargin;
                                                MeshInstanceManager.Refresh(models[i], onlyFastRefreshes: false);
                                            }

                                            GUI.changed  = true;
                                            updateMeshes = true;
                                        }
                                    }

                                    UnwrapParam uvGenerationSettings;
                                    UnwrapParam.SetDefaults(out uvGenerationSettings);

                                    if(!AngleError.HasValue || AngleError.Value != uvGenerationSettings.angleError ||
                                       !AreaError.HasValue || AreaError.Value != uvGenerationSettings.areaError ||
                                       !HardAngle.HasValue || HardAngle.Value != uvGenerationSettings.hardAngle ||
                                       !PackMargin.HasValue || PackMargin.Value != uvGenerationSettings.packMargin)
                                    {
                                        if(EditModeCommonGUI.IndentableButton(resetContent))
                                        {
                                            for(int i = 0; i < models.Length; i++)
                                            {
                                                models[i].angleError = uvGenerationSettings.angleError;
                                                models[i].areaError  = uvGenerationSettings.areaError;
                                                models[i].hardAngle  = uvGenerationSettings.hardAngle;
                                                models[i].packMargin = uvGenerationSettings.packMargin * 1024.0f;
                                                MeshInstanceManager.Refresh(models[i], onlyFastRefreshes: false);
                                            }

                                            GUI.changed  = true;
                                            updateMeshes = true;
                                        }
                                    }
                                }

                                {
                                    bool autoRebuildUvs = AutoRebuildUVs ?? false;
                                    EditorGUI.BeginChangeCheck();

                                    {
                                        EditorGUI.showMixedValue = !AutoRebuildUVs.HasValue;
                                        autoRebuildUvs = EditorGUILayout.Toggle(autoRebuildUVsContent, autoRebuildUvs);
                                    }

                                    if(EditorGUI.EndChangeCheck())
                                    {
                                        for(int i = 0; i < models.Length; i++)
                                        {
                                            if(autoRebuildUvs)
                                            {
                                                models[i].Settings |= ModelSettingsFlags.AutoRebuildUVs;
                                            }
                                            else
                                            {
                                                models[i].Settings &= ~ModelSettingsFlags.AutoRebuildUVs;
                                            }

                                            MeshInstanceManager.Refresh(models[i], onlyFastRefreshes: false);
                                        }

                                        GUI.changed = true;
                                    }
                                }

                                EditModeCommonGUI.UpdateButtons(models);
                                EditorGUI.indentLevel--;
                                GUILayout.Space(10);
                                EditorGUILayout.LabelField("Unity UV Charting Control");
                                EditorGUI.indentLevel++;

                                {
                                    {
                                        bool preserveUVs = PreserveUVs ?? false;
                                        EditorGUI.BeginChangeCheck();

                                        {
                                            EditorGUI.showMixedValue = !PreserveUVs.HasValue;
                                            preserveUVs = EditorGUILayout.Toggle(preserveUVsContent, preserveUVs);
                                        }

                                        if(EditorGUI.EndChangeCheck())
                                        {
                                            for(int i = 0; i < models.Length; i++)
                                            {
                                                if(preserveUVs)
                                                {
                                                    models[i].Settings |= ModelSettingsFlags.PreserveUVs;
                                                }
                                                else
                                                {
                                                    models[i].Settings &= ~ModelSettingsFlags.PreserveUVs;
                                                }

                                                MeshInstanceManager.Refresh(models[i], onlyFastRefreshes: false);
                                            }

                                            GUI.changed  = true;
                                            PreserveUVs  = preserveUVs;
                                            updateMeshes = true;
                                        }
                                    }

                                    EditorGUI.indentLevel++;

                                    bool disabledAutoUVs = PreserveUVs ?? false;

                                    using(new EditorGUI.DisabledScope(disabledAutoUVs))
                                    {
                                        {
                                            float autoUVMaxDistance = AutoUVMaxDistance ?? 0.0f;
                                            EditorGUI.BeginChangeCheck();

                                            {
                                                EditorGUI.showMixedValue = !AutoUVMaxDistance.HasValue;

                                                autoUVMaxDistance = EditorGUILayout.FloatField(
                                                    autoUVMaxDistanceContent, autoUVMaxDistance
                                                );

                                                if(autoUVMaxDistance < 0.0f)
                                                {
                                                    autoUVMaxDistance = 0.0f;
                                                }
                                            }

                                            if(EditorGUI.EndChangeCheck())
                                            {
                                                for(int i = 0; i < models.Length; i++)
                                                {
                                                    models[i].autoUVMaxDistance = autoUVMaxDistance;
                                                    MeshInstanceManager.Refresh(models[i], onlyFastRefreshes: false);
                                                }

                                                GUI.changed  = true;
                                                updateMeshes = true;
                                            }
                                        }

                                        {
                                            float autoUVMaxAngle = AutoUVMaxAngle ?? 0.0f;
                                            EditorGUI.BeginChangeCheck();

                                            {
                                                EditorGUI.showMixedValue = !AutoUVMaxAngle.HasValue;

                                                autoUVMaxAngle = EditorGUILayout.Slider(
                                                    autoUVMaxAngleContent, autoUVMaxAngle, 0, 180
                                                );
                                            }

                                            if(EditorGUI.EndChangeCheck())
                                            {
                                                for(int i = 0; i < models.Length; i++)
                                                {
                                                    models[i].autoUVMaxAngle = autoUVMaxAngle;
                                                    MeshInstanceManager.Refresh(models[i], onlyFastRefreshes: false);
                                                }

                                                GUI.changed  = true;
                                                updateMeshes = true;
                                            }
                                        }
                                    }

                                    EditorGUI.indentLevel--;

                                    {
                                        bool ignoreNormals = IgnoreNormals ?? false;
                                        EditorGUI.BeginChangeCheck();

                                        {
                                            EditorGUI.showMixedValue = !IgnoreNormals.HasValue;
                                            ignoreNormals = EditorGUILayout.Toggle(ignoreNormalsContent, ignoreNormals);
                                        }

                                        if(EditorGUI.EndChangeCheck())
                                        {
                                            for(int i = 0; i < models.Length; i++)
                                            {
                                                if(ignoreNormals)
                                                {
                                                    models[i].Settings |= ModelSettingsFlags.IgnoreNormals;
                                                }
                                                else
                                                {
                                                    models[i].Settings &= ~ModelSettingsFlags.IgnoreNormals;
                                                }

                                                MeshInstanceManager.Refresh(models[i], onlyFastRefreshes: false);
                                            }

                                            GUI.changed  = true;
                                            updateMeshes = true;
                                        }
                                    }

                                    {
                                        int minimumChartSize = MinimumChartSize ?? 4;
                                        EditorGUI.BeginChangeCheck();

                                        {
                                            EditorGUI.showMixedValue = !MinimumChartSize.HasValue;

                                            minimumChartSize = EditorGUILayout.IntPopup(
                                                minimumChartSizeContent, minimumChartSize, minimumChartSizeStrings
                                              , minimumChartSizeValues
                                            );
                                        }

                                        if(EditorGUI.EndChangeCheck())
                                        {
                                            for(int i = 0; i < models.Length; i++)
                                            {
                                                models[i].minimumChartSize = minimumChartSize;
                                                MeshInstanceManager.Refresh(models[i], onlyFastRefreshes: false);
                                            }

                                            GUI.changed  = true;
                                            updateMeshes = true;
                                        }
                                    }
                                }

                                EditorGUI.indentLevel--;
                                GUILayout.Space(10);
                                EditorGUILayout.LabelField("Lightmap Settings");
                                EditorGUI.indentLevel++;

                                {
                                    {
                                        float scaleInLightmap = ScaleInLightmap ?? 1.0f;
                                        EditorGUI.BeginChangeCheck();

                                        {
                                            EditorGUI.showMixedValue = !ScaleInLightmap.HasValue;

                                            scaleInLightmap = EditorGUILayout.FloatField(
                                                scaleInLightmapContent, scaleInLightmap
                                            );
                                        }

                                        if(EditorGUI.EndChangeCheck())
                                        {
                                            for(int i = 0; i < models.Length; i++)
                                            {
                                                models[i].scaleInLightmap = scaleInLightmap;
                                                MeshInstanceManager.Refresh(models[i], onlyFastRefreshes: false);
                                            }

                                            GUI.changed  = true;
                                            updateMeshes = true;
                                        }
                                    }

                                    {
                                        bool stitchLightmapSeams = StitchLightmapSeams ?? false;
                                        EditorGUI.BeginChangeCheck();

                                        {
                                            EditorGUI.showMixedValue = !StitchLightmapSeams.HasValue;

                                            stitchLightmapSeams = EditorGUILayout.Toggle(
                                                stitchLightmapSeamsContent, stitchLightmapSeams
                                            );
                                        }

                                        if(EditorGUI.EndChangeCheck())
                                        {
                                            for(int i = 0; i < models.Length; i++)
                                            {
                                                if(stitchLightmapSeams)
                                                {
                                                    models[i].Settings |= ModelSettingsFlags.StitchLightmapSeams;
                                                }
                                                else
                                                {
                                                    models[i].Settings &= ~ModelSettingsFlags.StitchLightmapSeams;
                                                }

                                                MeshInstanceManager.Refresh(models[i], onlyFastRefreshes: false);
                                            }

                                            GUI.changed  = true;
                                            updateMeshes = true;
                                        }
                                    }
                                }

                                EditorGUI.indentLevel -= 2;
                                EditorGUI.indentLevel++;
                            }
                        }
                    }

                    EditorGUI.indentLevel--;
                }
            }

            GUILayout.EndVertical();

            if(UVSettingsVisible)
            {
                GUILayout.Space(10);
            }

            GUILayout.BeginVertical(GUI.skin.box);

            {
                EditorGUI.BeginChangeCheck();
                meshAdvancedVisible = EditorGUILayout.Foldout(meshAdvancedVisible, "Mesh (advanced)");

                if(EditorGUI.EndChangeCheck())
                {
                    SessionState.SetBool("CSGModel.MeshAdvanced", meshAdvancedVisible);
                }

                if(meshAdvancedVisible)
                {
                    EditorGUI.indentLevel++;

                    {
                        bool showGeneratedMeshes = ShowGeneratedMeshes ?? false;
                        EditorGUI.BeginChangeCheck();

                        {
                            EditorGUI.showMixedValue = !ShowGeneratedMeshes.HasValue;

                            showGeneratedMeshes = EditorGUILayout.Toggle(
                                showGeneratedMeshesContent, showGeneratedMeshes
                            );
                        }

                        if(EditorGUI.EndChangeCheck())
                        {
                            Undo.RecordObjects(models, "Setting ShowGeneratedMeshes to " + showGeneratedMeshes);

                            for(int i = 0; i < models.Length; i++)
                            {
                                models[i].ShowGeneratedMeshes = showGeneratedMeshes;
                                MeshInstanceManager.UpdateGeneratedMeshesVisibility(models[i]);
                            }

                            // Workaround for unity not refreshing the hierarchy when changing hideflags
                            EditorApplication.RepaintHierarchyWindow();
                            EditorApplication.DirtyHierarchyWindowSorting();
                        }

                        GUILayout.Space(10);

                        EditorGUI.BeginDisabledGroup(NoCollider ?? true);
                        EditorGUILayout.LabelField(meshColliderCookingContent);
                        EditorGUI.indentLevel++;

                        {
                            bool cookForFasterSimulation = CookForFasterSimulation ?? false;
                            bool enableMeshCleaning      = EnableMeshCleaning ?? false;
                            bool weldColocatedVertices   = WeldColocatedVertices ?? false;
                            EditorGUI.BeginChangeCheck();

                            {
                                EditorGUI.showMixedValue = !VertexChannelTangent.HasValue;

                                cookForFasterSimulation = EditorGUILayout.Toggle(
                                    cookForFasterSimulationContent, cookForFasterSimulation
                                );

                                EditorGUI.showMixedValue = !VertexChannelNormal.HasValue;

                                enableMeshCleaning = EditorGUILayout.Toggle(
                                    enableMeshCleaningContent, enableMeshCleaning
                                );

                                EditorGUI.showMixedValue = !VertexChannelUV0.HasValue;

                                weldColocatedVertices = EditorGUILayout.Toggle(
                                    weldColocatedVerticesContent, weldColocatedVertices
                                );
                            }

                            if(EditorGUI.EndChangeCheck())
                            {
                                for(int i = 0; i < models.Length; i++)
                                {
                                    MeshColliderCookingOptions meshColliderCookingOptions =
                                        models[i].MeshColliderCookingOptions;

                                    meshColliderCookingOptions &= ~(MeshColliderCookingOptions.CookForFasterSimulation |
                                        MeshColliderCookingOptions.EnableMeshCleaning |
                                        MeshColliderCookingOptions.WeldColocatedVertices);

                                    if(cookForFasterSimulation)
                                    {
                                        meshColliderCookingOptions |=
                                            MeshColliderCookingOptions.CookForFasterSimulation;
                                    }

                                    if(enableMeshCleaning)
                                    {
                                        meshColliderCookingOptions |= MeshColliderCookingOptions.EnableMeshCleaning;
                                    }

                                    if(weldColocatedVertices)
                                    {
                                        meshColliderCookingOptions |= MeshColliderCookingOptions.WeldColocatedVertices;
                                    }

                                    models[i].MeshColliderCookingOptions = meshColliderCookingOptions;
                                }

                                GUI.changed = true;
                            }
                        }

                        EditorGUI.EndDisabledGroup();
                        EditorGUI.indentLevel--;

                        GUILayout.Space(10);

                        EditorGUILayout.LabelField("Used Vertex Channels");
                        EditorGUI.indentLevel++;

                        {
                            if(DoNotRender.HasValue && DoNotRender.Value)
                            {
                                Color prevColor = GUI.color;
                                Color color     = new Color(1, 0.25f, 0.25f);
                                GUI.color = color;
                                EditorGUILayout.LabelField("Rendering is disabled");
                                GUI.color = prevColor;
                            }
                            else
                            {
                                if(!DoNotRender.HasValue)
                                {
                                    Color prevColor = GUI.color;
                                    Color color     = new Color(1, 0.25f, 0.25f);
                                    GUI.color = color;
                                    EditorGUILayout.LabelField("Not all models have their rendering enabled");
                                    GUI.color = prevColor;
                                }

                                bool vertexChannelTangent = VertexChannelTangent ?? false;
                                bool vertexChannelNormal  = VertexChannelNormal ?? false;
                                bool vertexChannelUV0     = VertexChannelUV0 ?? false;
                                EditorGUI.BeginChangeCheck();

                                {
                                    EditorGUI.showMixedValue = !VertexChannelTangent.HasValue;

                                    vertexChannelTangent = EditorGUILayout.Toggle(
                                        vertexChannelTangentContent, vertexChannelTangent
                                    );

                                    EditorGUI.showMixedValue = !VertexChannelNormal.HasValue;

                                    vertexChannelNormal = EditorGUILayout.Toggle(
                                        vertexChannelNormalContent, vertexChannelNormal
                                    );

                                    EditorGUI.showMixedValue = !VertexChannelUV0.HasValue;

                                    vertexChannelUV0 = EditorGUILayout.Toggle(
                                        vertexChannelUV1Content, vertexChannelUV0
                                    );
                                }

                                if(EditorGUI.EndChangeCheck())
                                {
                                    for(int i = 0; i < models.Length; i++)
                                    {
                                        VertexChannelFlags vertexChannel = models[i].VertexChannels;

                                        vertexChannel &= ~(VertexChannelFlags.Tangent | VertexChannelFlags.Normal
                                          | VertexChannelFlags.UV0);

                                        if(vertexChannelTangent)
                                        {
                                            vertexChannel |= VertexChannelFlags.Tangent;
                                        }

                                        if(vertexChannelNormal)
                                        {
                                            vertexChannel |= VertexChannelFlags.Normal;
                                        }

                                        if(vertexChannelUV0)
                                        {
                                            vertexChannel |= VertexChannelFlags.UV0;
                                        }

                                        models[i].VertexChannels = vertexChannel;
                                    }

                                    GUI.changed = true;
                                }
                            }
                        }

                        EditorGUI.indentLevel--;
                    }

                    EditorGUI.indentLevel--;
                }

                GUILayout.EndVertical();

                if(models != null && models.Length == 1)
                {
                    if(meshAdvancedVisible)
                    {
                        GUILayout.Space(10);
                    }

                    GUILayout.BeginVertical(GUI.skin.box);
                    EditorGUI.BeginChangeCheck();
                    statisticsVisible = EditorGUILayout.Foldout(statisticsVisible, "Statistics");

                    if(EditorGUI.EndChangeCheck())
                    {
                        SessionState.SetBool("CSGModel.Statistics", statisticsVisible);
                    }

                    if(statisticsVisible)
                    {
                        if(models[0].generatedMeshes == null ||
                           !models[0].generatedMeshes)
                        {
                            GUILayout.Label("Could not find model cache for this model.");
                        }
                        else
                        {
                            GeneratedMeshes meshContainer = models[0].generatedMeshes;

                            int totalTriangles = 0;
                            int totalVertices  = 0;
                            int totalMeshes    = 0;

                            Dictionary<Material, List<MeshData>> materialMeshes =
                                new Dictionary<Material, List<MeshData>>();

                            foreach(GeneratedMeshInstance instance in meshContainer.MeshInstances)
                            {
                                Mesh mesh = instance.SharedMesh;

                                if(!mesh || !MeshInstanceManager.HasVisibleMeshRenderer(instance))
                                {
                                    continue;
                                }

                                if(!instance.RenderMaterial)
                                {
                                    GeneratedMeshDescription meshDescription = instance.MeshDescription;

                                    if(meshDescription.surfaceParameter > 0)
                                    {
                                        instance.RenderMaterial  = null;
                                        instance.PhysicsMaterial = null;
                                        Object obj = EditorUtility.EntityIdToObject(meshDescription.surfaceParameter);

                                        if(obj)
                                        {
                                            switch(meshDescription.meshQuery.LayerParameterIndex)
                                            {
                                                case LayerParameterIndex.LayerParameter1:
                                                {
                                                    instance.RenderMaterial = obj as Material;

                                                    break;
                                                }
                                                case LayerParameterIndex.LayerParameter2:
                                                {
                                                    instance.PhysicsMaterial = obj as PhysicsMaterial;

                                                    break;
                                                }
                                            }
                                        }
                                    }

                                    if(!instance.RenderMaterial)
                                    {
                                        if(!m_DummyMaterial)
                                        {
                                            m_DummyMaterial = new Material(MaterialUtility.FloorMaterial);
                                        }

                                        instance.RenderMaterial = m_DummyMaterial;
                                    }
                                }

                                if(!materialMeshes.TryGetValue(instance.RenderMaterial, out List<MeshData> meshes))
                                {
                                    meshes                                  = new List<MeshData>();
                                    materialMeshes[instance.RenderMaterial] = meshes;
                                }

                                MeshData meshData = new MeshData();
                                meshData.mesh              = mesh;
                                meshData.vertexCount       = mesh.vertexCount;
                                meshData.triangleCount     = mesh.triangles.Length / 3;
                                meshData.geometryHashValue = instance.MeshDescription.geometryHashValue;
                                meshData.surfaceHashValue  = instance.MeshDescription.surfaceHashValue;
                                meshes.Add(meshData);

                                totalVertices  += meshData.vertexCount;
                                totalTriangles += meshData.triangleCount;
                                totalMeshes++;
                            }

                            EditorGUI.indentLevel++;
                            EditorGUILayout.Space();
                            EditorGUILayout.LabelField("total:");

                            EditorGUILayout.LabelField(
                                $"vertices: {totalVertices}  triangles: {totalTriangles}  materials: {materialMeshes.Count}  meshes: {totalMeshes}"
                            );

                            GUILayout.Space(10);
                            EditorGUILayout.LabelField("meshes:");

                            foreach(KeyValuePair<Material, List<MeshData>> item in materialMeshes)
                            {
                                Material       material = item.Key;
                                List<MeshData> meshes   = item.Value;

                                if(material == m_DummyMaterial)
                                {
                                    material = null;
                                }

                                GUILayout.BeginHorizontal();

                                {
                                    EditorGUI.BeginDisabledGroup(true);

                                    {
                                        EditorGUILayout.ObjectField(material, typeof(Material), true);
                                    }

                                    GUILayout.BeginVertical();

                                    {
                                        for(int i = 0; i < meshes.Count; i++)
                                        {
                                            EditorGUILayout.ObjectField(meshes[i].mesh, typeof(Mesh), true);

                                            EditorGUILayout.LabelField($"geometryHash {meshes[i].geometryHashValue:X}");

                                            EditorGUILayout.LabelField($"surfaceHash {meshes[i].surfaceHashValue:X}");

                                            EditorGUILayout.LabelField(
                                                $"vertices {meshes[i].vertexCount}  triangles {meshes[i].triangleCount}"
                                            );
                                        }
                                    }

                                    GUILayout.EndVertical();
                                    EditorGUI.EndDisabledGroup();
                                }

                                GUILayout.EndHorizontal();
                                EditorGUILayout.Space();
                            }

                            EditorGUI.indentLevel--;
                        }
                    }

                    GUILayout.EndVertical();
                }
            }

            EditorGUI.showMixedValue = false;

            if(updateMeshes)
            {
                InternalCSGModelManager.DoForcedMeshUpdate();
                InternalCSGModelManager.UpdateMeshes();
                UpdateLoop.ResetUpdateRoutine();

                for(int i = 0; i < models.Length; i++)
                {
                    MeshInstanceManager.ClearUVs(models[i]);
                    CSGTreeNode.Encapsulate(models[i].modelNodeID).SetDirty();
                }
            }
        }
    }
}
