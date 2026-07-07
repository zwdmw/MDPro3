using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.Android;
#if UNITY_2021_2_OR_NEWER
using UnityEditor.Build;
#endif
using UnityEditor.Build.Reporting;
using UnityEditor.XR.Management;
using UnityEditor.XR.Management.Metadata;
using UnityEditor.XR.OpenXR.Features;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.XR.Management;
using UnityEngine.XR.OpenXR;
using UnityEngine.XR.OpenXR.Features.Meta;
using UnityEngine.XR.OpenXR.Features.MetaQuestSupport;

namespace MDPro3.EditorTools
{
    public static class QuestBuild
    {
        private const string DefaultOutputDirectory = "D:/game/MDPro3-Quest";
        private const string DefaultRuntimeRoot = "D:/game/MDPro3";
        private const string DefaultAioZipRoot = "D:/game/MDPro3-AndroidAIO";
        private const string DefaultPackageName = "com.ygo.mdpro3.quest";
        private const string OpenXRPackageSettingsPath = "Assets/XR/Settings/OpenXR Package Settings.asset";
        private const string OpenXRLoaderTypeName = "UnityEngine.XR.OpenXR.OpenXRLoader";
        private const string MetaQuestFeatureId = "com.unity.openxr.feature.metaquest";
        private const string CompositionLayersFeatureId = "com.unity.openxr.feature.compositionlayers";
        private const string ARSessionFeatureId = "com.unity.openxr.feature.arfoundation-meta-session";
        private const string ARCameraFeatureId = "com.unity.openxr.feature.arfoundation-meta-camera";
        private const string AROcclusionFeatureId = "com.unity.openxr.feature.arfoundation-meta-occlusion";
        private const string ARRaycastFeatureId = "com.unity.openxr.feature.arfoundation-meta-raycast";
        private const string OculusTouchFeatureId = "com.unity.openxr.feature.input.oculustouch";
        private const string MetaQuestPlusFeatureId = "com.unity.openxr.feature.input.metaquestplus";
        private const string MetaQuestProFeatureId = "com.unity.openxr.feature.input.metaquestpro";
        private const string HandInteractionFeatureId = "com.unity.openxr.feature.input.handinteraction";
        private const string HandInteractionPosesFeatureId = "com.unity.openxr.feature.input.handinteractionposes";
        private const string FoveatedRenderingFeatureId = "com.unity.openxr.feature.foveatedrendering";

        [MenuItem("MDPro3/Quest/Build Android APK")]
        public static void BuildQuestApkMenu()
        {
            BuildQuestApk(false);
        }

        [MenuItem("MDPro3/Quest/Build Android Development APK")]
        public static void BuildQuestDevelopmentApkMenu()
        {
            BuildQuestApk(true);
        }

        public static void BuildQuestApkCommandLine()
        {
            BuildQuestApk(HasCommandLineArg("-development"));
        }

        private static void BuildQuestApk(bool development)
        {
            if (!BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.Android, BuildTarget.Android))
                throw new InvalidOperationException("Android Build Support is not installed for this Unity editor.");

            Directory.CreateDirectory(DefaultOutputDirectory);
            ConfigureAndroidBuildEnvironment();
            ConfigureAndroidExternalTools();

            if (!EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Android, BuildTarget.Android))
                throw new InvalidOperationException("Failed to switch active build target to Android.");

            ConfigureAndroidPlayer();
            ConfigureOpenXRForQuest();
            ConfigureUrpForQuestPassthrough();
            ValidateRuntimePayloads();

            string[] scenes = EditorBuildSettings.scenes
                .Where(scene => scene.enabled)
                .Select(scene => scene.path)
                .ToArray();

            if (scenes.Length == 0)
                throw new InvalidOperationException("No enabled scenes were found in EditorBuildSettings.");

            var output = Path.Combine(DefaultOutputDirectory, development ? "MDPro3-Quest-Development.apk" : "MDPro3-Quest.apk");
            var options = BuildOptions.None;
            if (development)
                options |= BuildOptions.Development | BuildOptions.ConnectWithProfiler;

            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = output,
                target = BuildTarget.Android,
                options = options
            });

            if (report.summary.result != BuildResult.Succeeded)
                throw new InvalidOperationException("Quest Android build failed: " + report.summary.result);

            Debug.Log("MDPro3 Quest APK built: " + output);
        }

        private static void ConfigureAndroidPlayer()
        {
#if UNITY_2021_2_OR_NEWER
            var namedTarget = NamedBuildTarget.Android;
            PlayerSettings.SetScriptingBackend(namedTarget, ScriptingImplementation.IL2CPP);
            PlayerSettings.SetApplicationIdentifier(namedTarget, DefaultPackageName);
#else
            PlayerSettings.SetScriptingBackend(BuildTargetGroup.Android, ScriptingImplementation.IL2CPP);
            PlayerSettings.SetApplicationIdentifier(BuildTargetGroup.Android, DefaultPackageName);
#endif
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
            PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel32;
            PlayerSettings.Android.targetSdkVersion = AndroidSdkVersions.AndroidApiLevelAuto;
            PlayerSettings.Android.useCustomKeystore = false;
            PlayerSettings.Android.forceInternetPermission = true;
            PlayerSettings.Android.forceSDCardPermission = false;
#if UNITY_6000_0_OR_NEWER
            PlayerSettings.Android.applicationEntry = AndroidApplicationEntry.GameActivity;
#endif
            PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.Android, false);
            PlayerSettings.SetGraphicsAPIs(BuildTarget.Android, new[] { GraphicsDeviceType.Vulkan });
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.LandscapeLeft;
            PlayerSettings.allowedAutorotateToPortrait = false;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
            PlayerSettings.allowedAutorotateToLandscapeLeft = true;
            PlayerSettings.allowedAutorotateToLandscapeRight = true;
            PlayerSettings.stripEngineCode = true;
            PlayerSettings.colorSpace = ColorSpace.Linear;

            if (string.IsNullOrWhiteSpace(PlayerSettings.productName))
                PlayerSettings.productName = "MDPro3 Quest";
        }

        private static void ConfigureOpenXRForQuest()
        {
            var generalSettings = GetOrCreateXRGeneralSettingsAsset();
            if (!generalSettings.HasSettingsForBuildTarget(BuildTargetGroup.Android))
                generalSettings.CreateDefaultSettingsForBuildTarget(BuildTargetGroup.Android);
            if (!generalSettings.HasManagerSettingsForBuildTarget(BuildTargetGroup.Android))
                generalSettings.CreateDefaultManagerSettingsForBuildTarget(BuildTargetGroup.Android);

            var androidSettings = generalSettings.SettingsForBuildTarget(BuildTargetGroup.Android);
            androidSettings.InitManagerOnStart = true;

            var manager = generalSettings.ManagerSettingsForBuildTarget(BuildTargetGroup.Android);
            if (manager == null)
                throw new InvalidOperationException("Failed to create Android XR Manager settings.");

            manager.automaticLoading = true;
            manager.automaticRunning = true;

            if (!XRPackageMetadataStore.AssignLoader(manager, OpenXRLoaderTypeName, BuildTargetGroup.Android))
                throw new InvalidOperationException("Failed to assign OpenXR loader for Android.");

            EnsureOpenXRPackageSettingsRegistered();

            var openXrSettings = OpenXRSettings.GetSettingsForBuildTargetGroup(BuildTargetGroup.Android);
            if (openXrSettings == null)
                throw new InvalidOperationException("Failed to create Android OpenXR settings.");

            FeatureHelpers.RefreshFeatures(BuildTargetGroup.Android);

            openXrSettings.renderMode = OpenXRSettings.RenderMode.SinglePassInstanced;
            openXrSettings.latencyOptimization = OpenXRSettings.LatencyOptimization.PrioritizeInputPolling;
            openXrSettings.symmetricProjection = false;
            openXrSettings.optimizeBufferDiscards = true;

            SetOpenXRFeatureEnabled(MetaQuestFeatureId, enabled: true, required: true);
            SetOpenXRFeatureEnabled(CompositionLayersFeatureId, enabled: true, required: true);
            SetOpenXRFeatureEnabled(ARSessionFeatureId, enabled: false, required: false);
            SetOpenXRFeatureEnabled(ARCameraFeatureId, enabled: false, required: false);
            SetOpenXRFeatureEnabled(AROcclusionFeatureId, enabled: false, required: false);
            SetOpenXRFeatureEnabled(ARRaycastFeatureId, enabled: false, required: false);
            SetOpenXRFeatureEnabled(OculusTouchFeatureId, enabled: true, required: true);
            SetOpenXRFeatureEnabled(MetaQuestPlusFeatureId, enabled: true, required: false);
            SetOpenXRFeatureEnabled(MetaQuestProFeatureId, enabled: true, required: false);
            SetOpenXRFeatureEnabled(HandInteractionFeatureId, enabled: false, required: false);
            SetOpenXRFeatureEnabled(HandInteractionPosesFeatureId, enabled: false, required: false);
            SetOpenXRFeatureEnabled(FoveatedRenderingFeatureId, enabled: true, required: false);

            ConfigureMetaQuestFeature(openXrSettings);
            ConfigureFoveatedRenderingFeature(openXrSettings);
            DisableCameraPassthroughFeature(openXrSettings);

            EditorUtility.SetDirty(generalSettings);
            EditorUtility.SetDirty(androidSettings);
            EditorUtility.SetDirty(manager);
            EditorUtility.SetDirty(openXrSettings);
            AssetDatabase.SaveAssets();
        }

        private static XRGeneralSettingsPerBuildTarget GetOrCreateXRGeneralSettingsAsset()
        {
            if (EditorBuildSettings.TryGetConfigObject<XRGeneralSettingsPerBuildTarget>(XRGeneralSettings.k_SettingsKey, out var settings) && settings != null)
                return settings;

            var guids = AssetDatabase.FindAssets("t:XRGeneralSettingsPerBuildTarget");
            if (guids.Length > 0)
            {
                var path = AssetDatabase.GUIDToAssetPath(guids[0]);
                settings = AssetDatabase.LoadAssetAtPath<XRGeneralSettingsPerBuildTarget>(path);
                if (settings != null)
                {
                    EditorBuildSettings.AddConfigObject(XRGeneralSettings.k_SettingsKey, settings, true);
                    return settings;
                }
            }

            EnsureAssetFolder("Assets/XR/Settings");
            settings = ScriptableObject.CreateInstance<XRGeneralSettingsPerBuildTarget>();
            AssetDatabase.CreateAsset(settings, "Assets/XR/Settings/XRGeneralSettingsPerBuildTarget.asset");
            EditorBuildSettings.AddConfigObject(XRGeneralSettings.k_SettingsKey, settings, true);
            AssetDatabase.SaveAssets();
            return settings;
        }

        private static void EnsureOpenXRPackageSettingsRegistered()
        {
            if (EditorBuildSettings.TryGetConfigObject<UnityEngine.Object>(Constants.k_SettingsKey, out var configObject) && configObject != null)
                return;

            var settingsAsset = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(OpenXRPackageSettingsPath);
            if (settingsAsset == null)
            {
                EnsureAssetFolder("Assets/XR/Settings");
                var packageSettingsType = Type.GetType("UnityEditor.XR.OpenXR.OpenXRPackageSettings, Unity.XR.OpenXR.Editor");
                var getOrCreate = packageSettingsType?.GetMethod("GetOrCreateInstance", BindingFlags.Public | BindingFlags.Static);
                settingsAsset = getOrCreate?.Invoke(null, null) as UnityEngine.Object;
            }

            if (settingsAsset == null)
                throw new FileNotFoundException("OpenXR package settings asset was not found or could not be created.", OpenXRPackageSettingsPath);

            EditorBuildSettings.AddConfigObject(Constants.k_SettingsKey, settingsAsset, true);
        }

        private static void SetOpenXRFeatureEnabled(string featureId, bool enabled, bool required)
        {
            var feature = FeatureHelpers.GetFeatureWithIdForBuildTarget(BuildTargetGroup.Android, featureId);
            if (feature == null)
            {
                if (required)
                    throw new InvalidOperationException("OpenXR feature was not found: " + featureId);
                return;
            }

            feature.enabled = enabled;
            EditorUtility.SetDirty(feature);
        }

        private static void ConfigureMetaQuestFeature(OpenXRSettings openXrSettings)
        {
            var questFeature = openXrSettings.GetFeature<MetaQuestFeature>();
            if (questFeature == null)
                throw new InvalidOperationException("Meta Quest OpenXR feature was not found.");

            questFeature.AddTargetDevice("quest2", "Quest 2", true);
            questFeature.AddTargetDevice("cambria", "Quest Pro", true);
            questFeature.AddTargetDevice("eureka", "Quest 3", true);
            questFeature.AddTargetDevice("quest3s", "Quest 3S", true);
            questFeature.ForceRemoveInternetPermission = false;

            SetSerializedBool(questFeature, "m_symmetricProjection", false);
            SetSerializedBool(questFeature, "m_optimizeBufferDiscards", true);

            EditorUtility.SetDirty(questFeature);
        }

        private static void ConfigureFoveatedRenderingFeature(OpenXRSettings openXrSettings)
        {
            if (openXrSettings == null)
                return;

            openXrSettings.foveatedRenderingApi = OpenXRSettings.BackendFovationApi.SRPFoveation;
            var feature = openXrSettings.GetFeature<UnityEngine.XR.OpenXR.Features.FoveatedRenderingFeature>();
            if (feature != null)
                SetSerializedBool(feature, "enableSubsampledLayout", true);

            EditorUtility.SetDirty(openXrSettings);
            if (feature != null)
                EditorUtility.SetDirty(feature);
        }

        private static void DisableCameraPassthroughFeature(OpenXRSettings openXrSettings)
        {
            var cameraFeature = openXrSettings.GetFeature<ARCameraFeature>();
            if (cameraFeature == null)
                return;

            cameraFeature.cameraImageSupportEnabled = false;
            cameraFeature.passthroughPreSplashScreen = false;
            EditorUtility.SetDirty(cameraFeature);
        }

        private static void ConfigureUrpForQuestPassthrough()
        {
            foreach (string guid in AssetDatabase.FindAssets("t:UniversalRenderPipelineAsset"))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var asset = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(path);
                if (asset == null)
                    continue;

                asset.supportsHDR = false;
                SetSerializedBool(asset, "m_SupportsHDR", false);
                SetSerializedBool(asset, "m_SupportsTerrainHoles", false);
                SetSerializedBool(asset, "m_RequireOpaqueTexture", false);
                EditorUtility.SetDirty(asset);
            }

            foreach (string guid in AssetDatabase.FindAssets("t:UniversalRendererData"))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var rendererData = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(path);
                if (rendererData == null)
                    continue;

                rendererData.intermediateTextureMode = IntermediateTextureMode.Auto;
                rendererData.postProcessData = null;
                EditorUtility.SetDirty(rendererData);
            }

            AssetDatabase.SaveAssets();
        }

        private static void SetSerializedBool(UnityEngine.Object target, string propertyName, bool value)
        {
            var serialized = new SerializedObject(target);
            var property = serialized.FindProperty(propertyName);
            if (property == null || property.propertyType != SerializedPropertyType.Boolean)
                return;

            property.boolValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void EnsureAssetFolder(string assetFolder)
        {
            var parts = assetFolder.Replace('\\', '/').Split('/');
            if (parts.Length == 0 || parts[0] != "Assets")
                throw new ArgumentException("Asset folder must start with Assets: " + assetFolder);

            var current = "Assets";
            for (var i = 1; i < parts.Length; i++)
            {
                var next = current + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next))
                    AssetDatabase.CreateFolder(current, parts[i]);
                current = next;
            }
        }

        private static void ConfigureAndroidBuildEnvironment()
        {
            string gradleHome = Path.Combine(DefaultOutputDirectory, "GradleHome");
            string tempRoot = Path.Combine(DefaultOutputDirectory, "Temp");

            Directory.CreateDirectory(gradleHome);
            Directory.CreateDirectory(tempRoot);

            Environment.SetEnvironmentVariable("GRADLE_USER_HOME", gradleHome);
            Environment.SetEnvironmentVariable("TEMP", tempRoot);
            Environment.SetEnvironmentVariable("TMP", tempRoot);
            Environment.SetEnvironmentVariable("TMPDIR", tempRoot);
        }

        private static void ConfigureAndroidExternalTools()
        {
            string editorDirectory = Path.GetDirectoryName(EditorApplication.applicationPath);
            if (string.IsNullOrEmpty(editorDirectory))
                throw new InvalidOperationException("Failed to resolve Unity editor directory.");

            string androidPlayer = Path.Combine(editorDirectory, "Data", "PlaybackEngines", "AndroidPlayer");
            string jdk = Path.Combine(androidPlayer, "OpenJDK");
            string sdk = Path.Combine(androidPlayer, "SDK");
            string ndk = Path.Combine(androidPlayer, "NDK");
            string gradle = Path.Combine(androidPlayer, "Tools", "gradle");

            RequireDirectory(jdk, "OpenJDK");
            RequireDirectory(sdk, "Android SDK");
            RequireDirectory(ndk, "Android NDK");
            RequireDirectory(gradle, "Gradle");
            RequireFile(Path.Combine(jdk, "bin", "java.exe"), "OpenJDK java.exe");
            RequireFile(Path.Combine(sdk, "platform-tools", "adb.exe"), "Android SDK adb.exe");
            RequireFile(Path.Combine(ndk, "source.properties"), "Android NDK source.properties");

            EditorPrefs.SetBool("JdkUseEmbedded", false);
            EditorPrefs.SetBool("SdkUseEmbedded", false);
            EditorPrefs.SetBool("NdkUseEmbedded", false);
            EditorPrefs.SetBool("GradleUseEmbedded", true);
            EditorPrefs.SetString("AndroidJavaRoot", jdk);
            EditorPrefs.SetString("AndroidSDKRoot", sdk);
            EditorPrefs.SetString("AndroidNDKRoot", ndk);
            EditorPrefs.SetString("AndroidGradleRoot", gradle);
            AndroidExternalToolsSettings.jdkRootPath = jdk;
            AndroidExternalToolsSettings.sdkRootPath = sdk;
            AndroidExternalToolsSettings.ndkRootPath = ndk;

            Environment.SetEnvironmentVariable("JAVA_HOME", jdk);
            Environment.SetEnvironmentVariable("ANDROID_HOME", sdk);
            Environment.SetEnvironmentVariable("ANDROID_SDK_ROOT", sdk);
            Environment.SetEnvironmentVariable("ANDROID_NDK_ROOT", ndk);
            Environment.SetEnvironmentVariable("ANDROID_NDK_HOME", ndk);
        }

        private static void RequireDirectory(string path, string label)
        {
            if (!Directory.Exists(path))
                throw new DirectoryNotFoundException(label + " directory was not found: " + path);
        }

        private static void RequireFile(string path, string label)
        {
            if (!File.Exists(path))
                throw new FileNotFoundException(label + " was not found.", path);
        }

        private static void ValidateRuntimePayloads()
        {
            string streamingAssets = Path.Combine(Application.dataPath, "StreamingAssets");
            string androidZip = Path.Combine(streamingAssets, "Android.zip");
            if (File.Exists(androidZip))
            {
                RequireZipEntry(androidZip, "Android/MasterDuel/");
                RequireZipEntry(androidZip, "Android/MasterDuel/Wallpaper/front0001");
                RequireZipEntry(androidZip, "Android/MasterDuel/Material/");
                RequireZipEntry(androidZip, "Android/MasterDuel/Effects/");
                return;
            }

            ValidateExternalRuntimeSource(DefaultRuntimeRoot, DefaultAioZipRoot);
            Debug.Log("Quest runtime resources will be deployed externally from: " + DefaultRuntimeRoot + " / " + DefaultAioZipRoot);
        }

        private static void ValidateExternalRuntimeSource(string runtimeRoot, string aioZipRoot)
        {
            RequireDirectory(runtimeRoot, "Quest runtime root");
            RequireDirectory(aioZipRoot, "Quest AIO zip root");

            if (HasRuntimeDataFolder(runtimeRoot))
            {
                RequireFile(Path.Combine(runtimeRoot, "Data", "items.txt"), "Quest Data/items.txt");
                RequireFile(Path.Combine(runtimeRoot, "Data", "lflist.conf"), "Quest Data/lflist.conf");
                RequireFile(Path.Combine(runtimeRoot, "Data", "cards_Lite.json"), "Quest Data/cards_Lite.json");
                RequireDirectory(Path.Combine(runtimeRoot, "Data", "Windbot", "Decks"), "Quest Windbot decks");
                RequireDirectory(Path.Combine(runtimeRoot, "Data", "Windbot", "Dialogs"), "Quest Windbot dialogs");
            }
            else
            {
                string dataZip = Path.Combine(aioZipRoot, "Data-V1.4.4F.zip");
                RequireFile(dataZip, "Quest Data payload zip");
                RequireZipEntry(dataZip, "Data/items.txt");
                RequireZipEntry(dataZip, "Data/lflist.conf");
                RequireZipEntry(dataZip, "Data/cards_Lite.json");
                RequireZipEntry(dataZip, "Data/Windbot/Decks/");
                RequireZipEntry(dataZip, "Data/Windbot/Dialogs/");
            }

            string androidRoot = Path.Combine(runtimeRoot, "Android", "MasterDuel");
            if (Directory.Exists(androidRoot))
            {
                RequireDirectory(androidRoot, "Quest Android/MasterDuel");
                RequireDirectory(Path.Combine(androidRoot, "Wallpaper", "front0001"), "Quest Android wallpaper assets");
                RequireDirectory(Path.Combine(androidRoot, "Material"), "Quest Android material assets");
                RequireDirectory(Path.Combine(androidRoot, "Effects"), "Quest Android effect assets");
            }
            else
            {
                string androidZip = Path.Combine(aioZipRoot, "Android-V1.4.4F.zip");
                RequireFile(androidZip, "Quest Android payload zip");
                RequireZipEntry(androidZip, "Android/MasterDuel/");
                RequireZipEntry(androidZip, "Android/MasterDuel/Wallpaper/front0001");
                RequireZipEntry(androidZip, "Android/MasterDuel/Material/");
                RequireZipEntry(androidZip, "Android/MasterDuel/Effects/");
            }
        }

        private static bool HasRuntimeDataFolder(string runtimeRoot)
        {
            return File.Exists(Path.Combine(runtimeRoot, "Data", "items.txt"))
                && File.Exists(Path.Combine(runtimeRoot, "Data", "lflist.conf"))
                && File.Exists(Path.Combine(runtimeRoot, "Data", "cards_Lite.json"))
                && Directory.Exists(Path.Combine(runtimeRoot, "Data", "Windbot", "Decks"))
                && Directory.Exists(Path.Combine(runtimeRoot, "Data", "Windbot", "Dialogs"));
        }

        private static void RequireZipEntry(string zipPath, string entryPrefix)
        {
            using var archive = ZipFile.OpenRead(zipPath);
            bool found = archive.Entries.Any(entry => entry.FullName.StartsWith(entryPrefix, StringComparison.OrdinalIgnoreCase));
            if (!found)
                throw new InvalidDataException("Quest Android payload is missing required entry: " + entryPrefix);
        }

        private static bool HasCommandLineArg(string arg)
        {
            return Environment.GetCommandLineArgs().Any(a => string.Equals(a, arg, StringComparison.OrdinalIgnoreCase));
        }
    }
}
