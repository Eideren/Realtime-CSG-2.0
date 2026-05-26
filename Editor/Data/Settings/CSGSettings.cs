using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using InternalRealtimeCSG;
using RealtimeCSG.Components;
using RealtimeCSG.Legacy;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace RealtimeCSG
{
    [Serializable]
    public enum SnapMode
    {
        GridSnapping
      , RelativeSnapping
      , None
    }

    [Serializable]
    public enum ToolEditMode
    {
        Place
      , Generate
      , Edit
      , Clip
      , Surfaces
    }

    [Flags, Serializable]
    public enum HelperSurfaceFlags
    {
        ShowVisibleSurfaces = 1
      , ShowHiddenSurfaces = 2 // manually hidden surfaces
      , ShowCulledSurfaces = 4 // surfaces removed by CSG process
      , ShowColliderSurfaces = 8
      , ShowTriggerSurfaces = 16
      , ShowCastShadowsSurfaces = 32
      , ShowReceiveShadowsSurfaces = 64
    }

    [Serializable]
    public enum ClipMode
    {
        RemovePositive
      , RemoveNegative
      , Split
    }

    public enum PresetCategory
    {
        UV_Scale,
        Grid_Size,
        Angle_Snap,
        Scale_Snap
    }

    public class Preset
    {
        public Preset(string _label, float _value)
        {
            Label = _label;
            Value = _value;
        }

        public string Label;
        public float Value;
    }

    public static class CSGSettings
    {
        public static bool ShowVisibleSurfaces
        {
            get => (VisibleHelperSurfaces & HelperSurfaceFlags.ShowVisibleSurfaces) != 0;
        }

        public static bool ShowHiddenSurfaces
        {
            get => (VisibleHelperSurfaces & HelperSurfaceFlags.ShowHiddenSurfaces) != 0;
        }

        public static bool ShowCulledSurfaces
        {
            get => (VisibleHelperSurfaces & HelperSurfaceFlags.ShowCulledSurfaces) != 0;
        }

        public static bool ShowColliderSurfaces
        {
            get => (VisibleHelperSurfaces & HelperSurfaceFlags.ShowColliderSurfaces) != 0;
        }

        public static bool ShowTriggerSurfaces
        {
            get => (VisibleHelperSurfaces & HelperSurfaceFlags.ShowTriggerSurfaces) != 0;
        }

        public static bool ShowCastShadowsSurfaces
        {
            get => (VisibleHelperSurfaces & HelperSurfaceFlags.ShowCastShadowsSurfaces) != 0;
        }

        public static bool ShowReceiveShadowsSurfaces
        {
            get => (VisibleHelperSurfaces & HelperSurfaceFlags.ShowReceiveShadowsSurfaces) != 0;
        }

        internal static readonly HashSet<string> wireframeSceneviews = new HashSet<string>();
        internal static readonly Dictionary<SceneView, bool> sceneViewShown = new Dictionary<SceneView, bool>();
        internal static readonly Dictionary<Camera, SceneView> sceneViewLookup = new Dictionary<Camera, SceneView>();

        internal static void RegisterSceneView(SceneView sceneView)
        {
            if(sceneView)
            {
                return;
            }

            sceneViewLookup[sceneView.camera] = sceneView;
        }

        internal static bool Assume2DView(Camera camera)
        {
            return camera != null && camera.orthographic && IsWireframeShown(camera);
        }

        internal static bool IsWireframeShown(SceneView sceneView)
        {
            if(!sceneView)
            {
                return false;
            }

            if(sceneViewShown.TryGetValue(sceneView, out bool isShown))
            {
                return isShown;
            }

            string name = sceneView.name;

            name = name.Trim();

            if(string.IsNullOrEmpty(name))
            {
                sceneView.name = GetUniqueSceneviewName(GetKnownSceneviewNames());
            }

            isShown                   = wireframeSceneviews.Contains(sceneView.name);
            sceneViewShown[sceneView] = isShown;

            return isShown;
        }

        internal static bool IsWireframeShown(Camera camera)
        {
            return sceneViewLookup.TryGetValue(camera, out SceneView sceneView) && IsWireframeShown(sceneView);
        }

        internal static void UpdateWireframeModes()
        {
            List<SceneView> sceneViews = SortedSceneViews();

            foreach(SceneView sceneView in sceneViews)
            {
                if(IsWireframeShown(sceneView) && EnableRealtimeCSG)
                {
                    sceneView.SetSceneViewShaderReplace(ColorSettings.GetWireframeShader(), null);
                }
                else
                {
                    sceneView.SetSceneViewShaderReplace(null, null);
                }
            }
        }

        internal static void SetWireframeShown(SceneView sceneView, bool show)
        {
            if(!sceneView)
            {
                return;
            }

            string name = sceneView.name;

            name = name.Trim();

            if(string.IsNullOrEmpty(name))
            {
                sceneView.name = GetUniqueSceneviewName(GetKnownSceneviewNames());
            }

            if(show)
            {
                if(!wireframeSceneviews.Contains(sceneView.name))
                {
                    sceneView.SetSceneViewShaderReplace(ColorSettings.GetWireframeShader(), null);
                    wireframeSceneviews.Add(sceneView.name);
                }
            }
            else
            {
                if(wireframeSceneviews.Contains(sceneView.name))
                {
                    sceneView.SetSceneViewShaderReplace(null, null);
                    wireframeSceneviews.Remove(sceneView.name);
                }
            }

            sceneViewShown[sceneView] = show;
        }

        private static List<SceneView> SortedSceneViews()
        {
            return SceneView.sceneViews.Cast<SceneView>().OrderBy(item => item.position.y).ThenBy(item => item.position.x).ToList();
        }

        public const HelperSurfaceFlags DEFAULT_HELPER_SURFACE_FLAGS = HelperSurfaceFlags.ShowVisibleSurfaces;

        public static ToolEditMode EditMode = ToolEditMode.Generate;

        public static SnapMode ActiveSnappingMode
        {
            get
            {
                if(SelectionUtility.IsSnappingToggled)
                {
                    return SnapMode != SnapMode.None ? SnapMode.None : SnapMode.GridSnapping;
                }

                return SnapMode;
            }
        }

        public static bool ScaleSnapping
        {
            get => ActiveSnappingMode != SnapMode.None;
        }

        public static bool RotationSnapping
        {
            get => ActiveSnappingMode != SnapMode.None;
        }

        public static bool GridSnapping
        {
            get => ActiveSnappingMode == SnapMode.GridSnapping;
        }

        public static bool RelativeSnapping
        {
            get => ActiveSnappingMode == SnapMode.RelativeSnapping;
        }

        public static bool LockAxisX;
        public static bool LockAxisY;
        public static bool LockAxisZ;
        public static SnapMode SnapMode = SnapMode.GridSnapping;
        public static bool UniformGrid = true;
        public static bool GridVisible = true;
        public static HelperSurfaceFlags VisibleHelperSurfaces = DEFAULT_HELPER_SURFACE_FLAGS;
        public static Vector3 SnapVector = MathConstants.oneVector3;
        public static float SnapRotation = 15.0f;
        public static float SnapScale = 0.1f;

        public static DistanceUnit DistanceUnit = DistanceUnit.Meters;
        public static PixelUnit PixelUnit = PixelUnit.Relative;
        public static float DefaultShapeHeight = 1.0f;
        public static uint CurveSides = 10;
        public static ClipMode ClipMode = ClipMode.RemovePositive;
        public static Material DefaultMaterial = MaterialUtility.WallMaterial;

        private const TexGenFlags DEFAULT_TEXT_GEN_FLAGS_STATE = TexGenFlags.WorldSpaceTexture;
        public static TexGenFlags DefaultTexGenFlags = DEFAULT_TEXT_GEN_FLAGS_STATE;

        public static int MaxSphereSplits = 9;

        public static int CircleSides = 18;
        public static int MaxCircleSides = 144;
        public static float CircleOffset;
        public static bool CircleSmoothShading = true;
        public static bool CircleSingleSurfaceEnds = true;
        public static bool CircleDistanceToSide = true;
        public static bool CircleRecenter = true;

        public static int SphereSplits = 3;
        public static float SphereOffset;
        public static bool SphereSmoothShading = true;
        public static bool SphereDistanceToSide = true;
        public static bool ShowSceneInfo;
        public static bool HemiSphereMode = true;

        public static float LinearStairsStepLength = 0.30f;
        public static float LinearStairsStepHeight = 0.20f;
        public static float LinearStairsStepWidth = 1.0f;
        public static float LinearStairsLength = 16.0f;
        public static float LinearStairsHeight = 16.0f;
        public static float LinearStairsLengthOffset;
        public static float LinearStairsHeightOffset;
        public static StairsBottom LinearStairsBottom = StairsBottom.Filled;
        public static int LinearStairsTotalSteps = 4;

        public static bool AutoCommitExtrusion;

        public static bool SelectionVertex = true;
        public static bool SelectionEdge = true;
        public static bool SelectionSurface = true;
        public static bool HiddenSurfacesNotSelectable = true;

        public static bool ShowTooltips = true;

        public static Dictionary<PresetCategory, List<Preset>> Presets
        {
            get
            {
                if(m_presets == null)
                {
                    m_presets = new Dictionary<PresetCategory, List<Preset>>();
                }

                if (m_presets.Count == 0)
                {
                    for (int i = 0; i < Enum.GetValues(typeof(PresetCategory)).Length; i++)
                    {
                        PresetCategory category = (PresetCategory)i;

                        m_presets.TryAdd(category, new List<Preset>());

                        //Set Default Values
                        switch(category)
                        {
                            case PresetCategory.Grid_Size:
                                m_presets[category].Add(new Preset("", 1.0f));
                                m_presets[category].Add(new Preset("", 0.5f));
                                m_presets[category].Add(new Preset("", 0.25f));
                                m_presets[category].Add(new Preset("", 0.125f));
                                break;

                            case PresetCategory.Angle_Snap:
                                m_presets[category].Add(new Preset("", 45.0f));
                                m_presets[category].Add(new Preset("", 22.5f));
                                m_presets[category].Add(new Preset("", 11.25f));
                                m_presets[category].Add(new Preset("", 5.625f));
                                break;

                            case PresetCategory.Scale_Snap:
                                m_presets[category].Add(new Preset("", 1.0f));
                                m_presets[category].Add(new Preset("", 0.5f));
                                m_presets[category].Add(new Preset("", 0.25f));
                                m_presets[category].Add(new Preset("", 0.125f));
                                break;

                            case PresetCategory.UV_Scale:
                                m_presets[category].Add(new Preset("", 1.0f));
                                m_presets[category].Add(new Preset("", 0.5f));
                                m_presets[category].Add(new Preset("", 0.25f));
                                m_presets[category].Add(new Preset("", 0.125f));
                                break;
                        }
                    }
                }

                return m_presets;
            }

            set
            {
                m_presets = value;
            }
        }

        private static Dictionary<PresetCategory, List<Preset>> m_presets = new Dictionary<PresetCategory, List<Preset>>();
        public enum UpdateLightmapUVButtonState
        {
            BottomBar,
            InCSGToolWidow,
            Both,
            Hidden
        }

        static public UpdateLightmapUVButtonState UpdateLightmapUVButtonMode = UpdateLightmapUVButtonState.BottomBar;

        public static bool DefaultPreserveUVs
        {
            get => (CSGModel.DefaultSettings & ModelSettingsFlags.PreserveUVs) == ModelSettingsFlags.PreserveUVs;
            set
            {
                if(value)
                {
                    CSGModel.DefaultSettings |= ModelSettingsFlags.PreserveUVs;
                }
                else
                {
                    CSGModel.DefaultSettings &= ~ModelSettingsFlags.PreserveUVs;
                }
            }
        }

        public static bool SnapNonCSGObjects = true;

        public static bool AutoRigidbody
        {
            get => (CSGModel.DefaultSettings & ModelSettingsFlags.AutoUpdateRigidBody) == ModelSettingsFlags.AutoUpdateRigidBody;
            set
            {
                if(value)
                {
                    CSGModel.DefaultSettings |= ModelSettingsFlags.AutoUpdateRigidBody;
                }
                else
                {
                    CSGModel.DefaultSettings &= ~ModelSettingsFlags.AutoUpdateRigidBody;
                }
            }
        }

        public static Vector3 DefaultMoveOffset = Vector3.zero;
        public static Vector3 DefaultRotateOffset = Vector3.zero;

        private static Type m_UnitySnapType;
        private static PropertyInfo m_UnitySnapTypeMoveProperty;
        private static PropertyInfo m_UnitySnapTypeRotateProperty;

        public static bool EnableRealtimeCSG = true;

        internal static ShapeMode ShapeBuildMode = ShapeMode.Box;

        public static void SetRealtimeCSGEnabled(bool isEnabled)
        {
            EnableRealtimeCSG = isEnabled;

            if(!EnableRealtimeCSG)
            {
                Tools.hidden              = false;
                UnityGridManager.ShowGrid = GridVisible;
            }
            else
            {
                GridVisible = UnityGridManager.ShowGrid;
            }

            EditModeManager.UpdateTool();
            UpdateWireframeModes();
            Save();
        }

#region UpdateSnapSettings

        internal static void UpdateSnapSettings()
        {
            // get our snapping values from the EditorPrefs ...
            Vector3 moveSnapVector = SnapVector;
            float   rotationSnap   = SnapRotation;

            List<Type> types = null;

            // ... but unfortunately Unity also caches it internally
            // and doesn't expose it to us, so we're forced to use reflection ...
            if(m_UnitySnapType == null)
            {
                Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
                types = new List<Type>();

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
            }

            if(m_UnitySnapType == null)
            {
                m_UnitySnapType = types!.FirstOrDefault(t => t.FullName == "UnityEditor.SnapSettings");

                if(m_UnitySnapType != null)
                {
                    m_UnitySnapTypeMoveProperty   = m_UnitySnapType.GetProperty("move", BindingFlags.Public | BindingFlags.Static);
                    m_UnitySnapTypeRotateProperty = m_UnitySnapType.GetProperty("rotate", BindingFlags.Public | BindingFlags.Static);
                }
                else
                {
                    m_UnitySnapTypeMoveProperty   = null;
                    m_UnitySnapTypeRotateProperty = null;
                }
            }

            if(m_UnitySnapType != null)
            {
                if(m_UnitySnapTypeMoveProperty != null)
                {
                    m_UnitySnapTypeMoveProperty.SetValue(m_UnitySnapType, moveSnapVector, null);
                }

                if(m_UnitySnapTypeRotateProperty != null)
                {
                    m_UnitySnapTypeRotateProperty.SetValue(m_UnitySnapType, rotationSnap, null);
                }
            }
        }

#endregion

        private static HashSet<string> GetKnownSceneviewNames()
        {
            HashSet<string> knownNames = new HashSet<string>();

            for(int i = 0; i < SceneView.sceneViews.Count; i++)
            {
                SceneView sceneView = SceneView.sceneViews[i] as SceneView;

                if(!sceneView || string.IsNullOrEmpty(sceneView.name))
                {
                    continue;
                }

                knownNames.Add(sceneView.name);
            }

            return knownNames;
        }

        private static string GetUniqueSceneviewName(HashSet<string> knownNames)
        {
            int    count = 1;
            string name;

            do
            {
                name = $"View{count}";
                count++;
            }
            while(knownNames.Contains(name));

            return name;
        }

        private static void EnsureValidSceneviewNames(List<SceneView> sceneViews)
        {
            HashSet<string> knownNames = GetKnownSceneviewNames();
            HashSet<string> foundNames = new HashSet<string>();

            for(int i = 0; i < sceneViews.Count; i++)
            {
                SceneView sceneView = sceneViews[i];

                if(!sceneView)
                {
                    continue;
                }

                if(string.IsNullOrEmpty(sceneView.name) || foundNames.Contains(sceneView.name))
                {
                    sceneView.name = GetUniqueSceneviewName(knownNames);
                    knownNames.Add(sceneView.name);
                    foundNames.Add(sceneView.name);
                }
            }
        }

        private static void LoadWireframeSettings(string arrayString)
        {
            wireframeSceneviews.Clear();
            string[] items = arrayString.Split(':');

            for(int i = 0; i < items.Length; i++)
            {
                string name = items[i];

                if(string.IsNullOrEmpty(name.Trim()))
                {
                    continue;
                }

                wireframeSceneviews.Add(name.Trim());
            }
        }

        private static void LegacyLoadWireframeSettings(List<SceneView> sceneViews, string arrayString)
        {
            string[] items   = arrayString.Split(',');
            int[]    ids     = new int[items.Length];
            bool[]   enabled = new bool[items.Length];

            for(int j = items.Length - 1; j >= 0; j--)
            {
                string   item    = items[j];
                string[] subItem = item.Split(':');

                if(subItem.Length != 2 || !int.TryParse(subItem[0], out int id) || !bool.TryParse(subItem[1], out bool isEnabled))
                {
                    ids[j]     = 0;
                    enabled[j] = false;

                    continue;
                }

                ids[j]     = id;
                enabled[j] = isEnabled;
            }

            wireframeSceneviews.Clear();
            int[] wireframeInstanceIDs = new int[items.Length];

            if(sceneViews.Count != items.Length)
            {
                wireframeInstanceIDs = Array.Empty<int>();
            }
            else
            {
                bool foundAll = true;

                for(int j = 0; j < sceneViews.Count; j++)
                {
                    if(!ArrayUtility.Contains(ids, sceneViews[j].GetInstanceID()))
                    {
                        foundAll = false;

                        break;
                    }
                }

                if(foundAll)
                {
                    for(int j = ids.Length - 1; j >= 0; j--)
                    {
                        if(!enabled[j])
                        {
                            ArrayUtility.RemoveAt(ref wireframeInstanceIDs, j);

                            continue;
                        }

                        wireframeInstanceIDs[j] = ids[j];
                    }
                }
                else
                {
                    for(int j = sceneViews.Count - 1; j >= 0; j--)
                    {
                        if(!enabled[j])
                        {
                            ArrayUtility.RemoveAt(ref wireframeInstanceIDs, j);

                            continue;
                        }

                        wireframeInstanceIDs[j] = sceneViews[j].GetInstanceID();
                    }
                }
            }

            for(int i = 0; i < sceneViews.Count; i++)
            {
                if(ArrayUtility.Contains(wireframeInstanceIDs, sceneViews[i].GetInstanceID()))
                {
                    wireframeSceneviews.Add(sceneViews[i].name);
                }
            }
        }

        private static Rect GetRect(string name, Rect defaultRect)
        {
            Rect rect = new Rect();
            rect.x      = EditorPrefs.GetFloat($"{name}X", defaultRect.x);
            rect.y      = EditorPrefs.GetFloat($"{name}Y", defaultRect.y);
            rect.width  = EditorPrefs.GetFloat($"{name}Width", defaultRect.width);
            rect.height = EditorPrefs.GetFloat($"{name}Height", defaultRect.height);

            return rect;
        }

        private static void SetRect(string name, Rect rect)
        {
            EditorPrefs.SetFloat($"{name}X", rect.x);
            EditorPrefs.SetFloat($"{name}Y", rect.y);
            EditorPrefs.SetFloat($"{name}Width", rect.width);
            EditorPrefs.SetFloat($"{name}Height", rect.height);
        }

        private static Vector3 GetVector3(string name, Vector3 defaultVector)
        {
            return new Vector3(
                EditorPrefs.GetFloat($"{name}X", defaultVector.x),
                EditorPrefs.GetFloat($"{name}Y", defaultVector.y),
                EditorPrefs.GetFloat($"{name}Z", defaultVector.z)
            );
        }

        private static void SetVector3(string name, Vector3 vector)
        {
            EditorPrefs.SetFloat($"{name}X", vector.x);
            EditorPrefs.SetFloat($"{name}Y", vector.y);
            EditorPrefs.SetFloat($"{name}Z", vector.z);
        }

        private static T GetEnum<T>(string name, T defaultValue) where T : struct, IConvertible
        {
            int result = EditorPrefs.GetInt(name, Convert.ToInt32(defaultValue));

            return (T)(object)result;
        }

        private static void SetEnum<T>(string name, T value) where T : struct, IConvertible
        {
            EditorPrefs.SetInt(name, Convert.ToInt32(value));
        }

        private static Object GetAssetObject(string name, Object defaultValue)
        {
            string assetObjectGUID = EditorPrefs.GetString(name, null);

            if(assetObjectGUID == null)
            {
                return defaultValue;
            }

            string   assetPath   = AssetDatabase.GUIDToAssetPath(assetObjectGUID);
            Material assetObject = AssetDatabase.LoadAssetAtPath<Material>(assetPath);

            return assetObject ? assetObject : defaultValue;
        }

        private static void SetAssetObject(string name, Object value)
        {
            if(!value)
            {
                EditorPrefs.SetString(name, null);

                return;
            }

            string assetPath       = AssetDatabase.GetAssetPath(value);
            string assetObjectGUID = string.IsNullOrEmpty(assetPath) ? null : AssetDatabase.AssetPathToGUID(assetPath);
            EditorPrefs.SetString(name, assetObjectGUID);
        }

        private static Material GetMaterial(string name, Material defaultValue)
        {
            return GetAssetObject(name, defaultValue) as Material;
        }

        private static void SetMaterial(string name, Material value)
        {
            SetAssetObject(name, value);
        }

#region Reload

        public static void Reload()
        {
            LockAxisX = EditorPrefs.GetBool("LockAxisX", false);
            LockAxisY = EditorPrefs.GetBool("LockAxisY", false);
            LockAxisZ = EditorPrefs.GetBool("LockAxisZ", false);

            UniformGrid = EditorPrefs.GetBool("UniformGrid", true);
            EditMode    = GetEnum("EditMode", ToolEditMode.Generate);

            SnapVector          = GetVector3("MoveSnap", Vector3.one);
            DefaultMoveOffset   = GetVector3("DefaultMoveOffset", Vector3.zero);
            DefaultRotateOffset = GetVector3("DefaultRotateOffset", Vector3.zero);

            ShapeBuildMode     = GetEnum("ShapeBuildMode", ShapeMode.Box);
            DefaultTexGenFlags = GetEnum("DefaultTexGenFlags", DEFAULT_TEXT_GEN_FLAGS_STATE);

            GridVisible = EditorPrefs.GetBool("ShowGrid", true);

            SnapMode = (SnapMode)EditorPrefs.GetInt("SnapMode", (int)(EditorPrefs.GetBool("ForceSnapToGrid", true) ? SnapMode.GridSnapping : SnapMode.None));

            VisibleHelperSurfaces = GetEnum("HelperSurfaces", DEFAULT_HELPER_SURFACE_FLAGS);

            ClipMode          = GetEnum("ClipMode", ClipMode.RemovePositive);
            EnableRealtimeCSG = EditorPrefs.GetBool("EnableRealtimeCSG", true);

            DefaultMaterial = GetMaterial("DefaultMaterial", MaterialUtility.WallMaterial);

            SnapScale          = EditorPrefs.GetFloat("ScaleSnap", 0.1f);
            SnapRotation       = EditorPrefs.GetFloat("RotationSnap", 15.0f);
            DefaultShapeHeight = EditorPrefs.GetFloat("DefaultShapeHeight", 1.0f);
            CurveSides         = (uint)EditorPrefs.GetInt("CurveSides", 10);

            SelectionVertex  = EditorPrefs.GetBool("SelectionVertex", true);
            SelectionEdge    = EditorPrefs.GetBool("SelectionEdge", true);
            SelectionSurface = EditorPrefs.GetBool("SelectionSurface", true);

            HiddenSurfacesNotSelectable = EditorPrefs.GetBool("HiddenSurfacesNotSelectable", true);
            ShowTooltips                = EditorPrefs.GetBool("ShowTooltips", true);

            AutoRigidbody = EditorPrefs.GetBool("AutoRigidbody", (CSGModel.DefaultSettings & ModelSettingsFlags.AutoUpdateRigidBody) == ModelSettingsFlags.AutoUpdateRigidBody);

            DefaultPreserveUVs = EditorPrefs.GetBool("DefaultPreserveUVs", (CSGModel.DefaultSettings & ModelSettingsFlags.PreserveUVs) == ModelSettingsFlags.PreserveUVs);

            SnapNonCSGObjects = EditorPrefs.GetBool("SnapNonCSGObjects", true);

            AutoCommitExtrusion = EditorPrefs.GetBool("AutoCommitExtrusion", false);

            MaxSphereSplits = Mathf.Max(3, EditorPrefs.GetInt("MaxSphereSplits", 9));

            CircleSides             = Mathf.Max(3, EditorPrefs.GetInt("CircleSides", 18));
            MaxCircleSides          = Mathf.Max(3, EditorPrefs.GetInt("MaxCircleSides", 144));
            CircleOffset            = EditorPrefs.GetFloat("CircleOffset", 0);
            CircleSmoothShading     = EditorPrefs.GetBool("CircleSmoothShading", true);
            CircleSingleSurfaceEnds = EditorPrefs.GetBool("CircleSingleSurfaceEnds", true);
            CircleDistanceToSide    = EditorPrefs.GetBool("CircleDistanceToSide", true);
            CircleRecenter          = EditorPrefs.GetBool("CircleRecenter", true);

            SphereSplits         = Mathf.Max(1, EditorPrefs.GetInt("SphereSplits", 1));
            SphereOffset         = EditorPrefs.GetFloat("SphereOffset", 0);
            SphereSmoothShading  = EditorPrefs.GetBool("SphereSmoothShading", true);
            SphereDistanceToSide = EditorPrefs.GetBool("SphereDistanceToSide", true);
            HemiSphereMode       = EditorPrefs.GetBool("HemiSphereMode", false);

            LinearStairsStepLength = EditorPrefs.GetFloat("LinearStairsStepLength", 0.30f);
            LinearStairsStepHeight = EditorPrefs.GetFloat("LinearStairsStepHeight", 0.20f);
            LinearStairsStepWidth  = EditorPrefs.GetFloat("LinearStairsStepWidth", 1.0f);
            LinearStairsTotalSteps = EditorPrefs.GetInt("LinearStairsTotalSteps", 4);

            LinearStairsLength       = EditorPrefs.GetFloat("LinearStairsLength", 16.0f);
            LinearStairsHeight       = EditorPrefs.GetFloat("LinearStairsHeight", 16.0f);
            LinearStairsLengthOffset = EditorPrefs.GetFloat("LinearStairsLengthOffset", 0.0f);
            LinearStairsHeightOffset = EditorPrefs.GetFloat("LinearStairsHeightOffset", 0.0f);

            Dictionary<PresetCategory, List<Preset>> loadedPresets = new Dictionary<PresetCategory, List<Preset>>();

            try
            {
                loadedPresets = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<PresetCategory, List<Preset>>>(EditorPrefs.GetString("CSGPresets"));
            }
            catch
            {
                loadedPresets = new Dictionary<PresetCategory, List<Preset>>();
            }

            Presets = loadedPresets;
  
            UpdateLightmapUVButtonMode = (UpdateLightmapUVButtonState)EditorPrefs.GetInt("UpdateLightmapUVButtonMode", 1);

            DistanceUnit = GetEnum("DistanceUnit", DistanceUnit.Meters);

            ShowSceneInfo = EditorPrefs.GetBool("ShowSceneInfo", false);

            List<SceneView> sceneViews = SortedSceneViews();
            EnsureValidSceneviewNames(sceneViews);
            string arrayString = EditorPrefs.GetString("Wireframe", string.Empty);

            if(arrayString.Contains(','))
            {
                LegacyLoadWireframeSettings(sceneViews, arrayString);
            }
            else
            {
                LoadWireframeSettings(arrayString);
            }

            UpdateSnapSettings();
        }

#endregion

#region Save

        public static void Save()
        {
            EditorPrefs.SetBool("LockAxisX", LockAxisX);
            EditorPrefs.SetBool("LockAxisY", LockAxisY);
            EditorPrefs.SetBool("LockAxisZ", LockAxisZ);

            EditorPrefs.SetBool("ShowGrid", GridVisible);
            EditorPrefs.SetBool("UniformGrid", UniformGrid);

            SetEnum("EditMode", EditMode);
            SetEnum("ClipMode", ClipMode);
            SetEnum("HelperSurfaces", VisibleHelperSurfaces);
            SetEnum("DistanceUnit", DistanceUnit);
            SetEnum("ShapeBuildMode", ShapeBuildMode);
            SetEnum("DefaultTexGenFlags", DefaultTexGenFlags);

            SetVector3("MoveSnap", SnapVector);
            SetVector3("DefaultMoveOffset", DefaultMoveOffset);
            SetVector3("DefaultRotateOffset", DefaultRotateOffset);

            EditorPrefs.SetFloat("ScaleSnap", SnapScale);
            EditorPrefs.SetFloat("RotationSnap", SnapRotation);
            EditorPrefs.SetFloat("DefaultShapeHeight", DefaultShapeHeight);
            EditorPrefs.SetInt("CurveSides", (int)CurveSides);
            EditorPrefs.SetBool("EnableRealtimeCSG", EnableRealtimeCSG);

            EditorPrefs.SetBool("SelectionVertex", SelectionVertex);
            EditorPrefs.SetBool("SelectionEdge", SelectionEdge);
            EditorPrefs.SetBool("SelectionSurface", SelectionSurface);

            EditorPrefs.SetBool("HiddenSurfacesNotSelectable", HiddenSurfacesNotSelectable);

            EditorPrefs.SetBool("ShowTooltips", ShowTooltips);

            EditorPrefs.SetBool("AutoRigidbody", (CSGModel.DefaultSettings & ModelSettingsFlags.AutoUpdateRigidBody) == ModelSettingsFlags.AutoUpdateRigidBody);

            EditorPrefs.SetBool("DefaultPreserveUVs", (CSGModel.DefaultSettings & ModelSettingsFlags.PreserveUVs) == ModelSettingsFlags.PreserveUVs);

            EditorPrefs.SetBool("SnapNonCSGObjects", SnapNonCSGObjects);

            EditorPrefs.SetBool("AutoCommitExtrusion", AutoCommitExtrusion);

            EditorPrefs.SetInt("SnapMode", (int)SnapMode);

            EditorPrefs.SetInt("MaxSphereSplits", Mathf.Max(3, MaxSphereSplits));

            EditorPrefs.SetInt("CircleSides", Mathf.Max(3, CircleSides));
            EditorPrefs.SetInt("MaxCircleSides", Mathf.Max(3, MaxCircleSides));
            EditorPrefs.SetFloat("CircleOffset", CircleOffset);
            EditorPrefs.SetBool("CircleSmoothShading", CircleSmoothShading);
            EditorPrefs.SetBool("CircleSingleSurfaceEnds", CircleSingleSurfaceEnds);
            EditorPrefs.SetBool("CircleDistanceToSide", CircleDistanceToSide);
            EditorPrefs.SetBool("CircleRecenter", CircleRecenter);

            EditorPrefs.SetInt("SphereSplits", Mathf.Max(1, SphereSplits));
            EditorPrefs.SetFloat("SphereOffset", SphereOffset);
            EditorPrefs.SetBool("SphereSmoothShading", SphereSmoothShading);
            EditorPrefs.SetBool("SphereDistanceToSide", SphereDistanceToSide);
            EditorPrefs.SetBool("HemiSphereMode", HemiSphereMode);

            EditorPrefs.SetFloat("LinearStairsStepLength", LinearStairsStepLength);
            EditorPrefs.SetFloat("LinearStairsStepHeight", LinearStairsStepHeight);
            EditorPrefs.SetFloat("LinearStairsStepWidth", LinearStairsStepWidth);
            EditorPrefs.SetInt("LinearStairsTotalSteps", LinearStairsTotalSteps);
            EditorPrefs.SetFloat("LinearStairsLength", LinearStairsLength);
            EditorPrefs.SetFloat("LinearStairsHeight", LinearStairsHeight);
            EditorPrefs.SetFloat("LinearStairsLengthOffset", LinearStairsLengthOffset);
            EditorPrefs.SetFloat("LinearStairsHeightOffset", LinearStairsHeightOffset);

            EditorPrefs.SetString("CSGPresets", Newtonsoft.Json.JsonConvert.SerializeObject(Presets));
  
            EditorPrefs.SetInt("UpdateLightmapUVButtonMode", (int)UpdateLightmapUVButtonMode);

            SetMaterial("DefaultMaterial", DefaultMaterial);

            EditorPrefs.SetBool("ShowSceneInfo", ShowSceneInfo);

            StringBuilder   builder    = new StringBuilder();
            List<SceneView> sceneViews = SortedSceneViews();
            EnsureValidSceneviewNames(sceneViews);

            foreach(SceneView sceneView in sceneViews)
            {
                if(IsWireframeShown(sceneView))
                {
                    if(builder.Length != 0)
                    {
                        builder.Append(':');
                    }

                    builder.Append(sceneView.name);
                }
            }

            EditorPrefs.SetString("Wireframe", builder.ToString());
        }

#endregion
    }
}
