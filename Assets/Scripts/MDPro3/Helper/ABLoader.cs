using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using Willow;
using UnityEngine.Playables;
using Willow.InGameField;
using UnityEngine.Rendering;
using System;
using YgomGame;

namespace MDPro3
{
    public class ABLoader : MonoBehaviour
    {
        public static Dictionary<string, GameObject> cachedAB = new Dictionary<string, GameObject>();
        public static Dictionary<string, List<GameObject>> cachedABFolder = new Dictionary<string, List<GameObject>>();
        public static Dictionary<string, Material> cachedPMat = new Dictionary<string, Material>();
        private static readonly Dictionary<string, string> resolvedPaths = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        private static readonly HashSet<string> missingBundleWarnings = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private static readonly HashSet<string> invalidBundleWarnings = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private static readonly HashSet<string> runtimeFallbackWarnings = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int ColorId = Shader.PropertyToID("_Color");
        private static Texture2D fallbackProtectorTexture;

        private static Material CreateFallbackMaterial(string materialName)
        {
            var shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (shader == null)
                shader = Shader.Find("Unlit/Texture");
            if (shader == null)
                shader = Shader.Find("Sprites/Default");
            if (shader == null)
                shader = Shader.Find("Standard");

            var material = new Material(shader);
            material.name = materialName;
            material.hideFlags = HideFlags.DontUnloadUnusedAsset;
            if (material.HasProperty(BaseColorId))
                material.SetColor(BaseColorId, Color.white);
            if (material.HasProperty(ColorId))
                material.SetColor(ColorId, Color.white);
            return material;
        }

        private static Material CreateFallbackProfileFrameMaterial(string code)
        {
            var material = CreateFallbackMaterial("ProfileFrameMatFallback" + code);
            TextureManager.ChangeProfileFrameMaterialWrapMode(material);
            return material;
        }

        private static Material CreateFallbackProtectorMaterial(string code)
        {
            var material = CreateFallbackMaterial("ProtectorFallback" + code);
            var sprite = TextureManager.container != null ? TextureManager.container.cardBackDefault : null;
            var texture = sprite != null && sprite.texture != null
                ? sprite.texture
                : GetFallbackProtectorTexture();
            if (texture != null)
            {
                material.mainTexture = texture;
                if (material.HasProperty("_BaseMap"))
                    material.SetTexture("_BaseMap", texture);
                if (material.HasProperty("_MainTex"))
                    material.SetTexture("_MainTex", texture);
            }
            material.renderQueue = 3000;
            return material;
        }

        private static Texture2D GetFallbackProtectorTexture()
        {
            if (fallbackProtectorTexture != null)
                return fallbackProtectorTexture;

            const int width = 256;
            const int height = 356;
            fallbackProtectorTexture = new Texture2D(width, height, TextureFormat.RGBA32, false)
            {
                name = "FallbackProtectorTexture",
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear,
                hideFlags = HideFlags.DontUnloadUnusedAsset
            };

            var center = new Vector2(width * 0.5f, height * 0.5f);
            for (var y = 0; y < height; y++)
            {
                for (var x = 0; x < width; x++)
                {
                    var edge = x < 14 || x >= width - 14 || y < 14 || y >= height - 14;
                    var innerEdge = x < 26 || x >= width - 26 || y < 26 || y >= height - 26;
                    var delta = new Vector2(x, y) - center;
                    var swirl = Mathf.Sin(delta.magnitude * 0.075f + Mathf.Atan2(delta.y, delta.x) * 3.5f);
                    var t = Mathf.Clamp01(0.45f + swirl * 0.35f);
                    var color = Color.Lerp(new Color(0.12f, 0.04f, 0.20f, 1f), new Color(0.55f, 0.16f, 0.78f, 1f), t);
                    if (innerEdge)
                        color = new Color(0.20f, 0.12f, 0.08f, 1f);
                    if (edge)
                        color = new Color(0.52f, 0.38f, 0.18f, 1f);
                    fallbackProtectorTexture.SetPixel(x, y, color);
                }
            }

            fallbackProtectorTexture.Apply(false, true);
            return fallbackProtectorTexture;
        }

        public static string ResolveAssetBundlePath(string path)
        {
            if (string.IsNullOrEmpty(path))
                return path;

            var normalized = path.Replace('\\', '/');
#if !UNITY_EDITOR && UNITY_ANDROID
            const string filePrefix = "file://";
            if (normalized.StartsWith(filePrefix, StringComparison.OrdinalIgnoreCase))
                normalized = normalized.Substring(filePrefix.Length);

            if (normalized.StartsWith(Program.rootWindows64, StringComparison.OrdinalIgnoreCase))
                normalized = Program.rootAndroid + normalized.Substring(Program.rootWindows64.Length);

            if (!Path.IsPathRooted(normalized))
                normalized = Path.Combine(Application.persistentDataPath, normalized);

            return ResolveExistingPathCaseInsensitive(normalized);
#elif UNITY_STANDALONE_WIN && !UNITY_EDITOR
            if (!Path.IsPathRooted(normalized))
                normalized = Path.Combine(Application.dataPath, normalized);
            return normalized;
#else
            return normalized;
#endif
        }

        private static string ResolveExistingPathCaseInsensitive(string path)
        {
#if !UNITY_EDITOR && UNITY_ANDROID
            path = Path.GetFullPath(path);
            if (File.Exists(path) || Directory.Exists(path))
                return path;

            lock (resolvedPaths)
            {
                if (resolvedPaths.TryGetValue(path, out var cached) && (File.Exists(cached) || Directory.Exists(cached)))
                    return cached;
            }

            var root = Path.GetPathRoot(path);
            if (string.IsNullOrEmpty(root))
                return path;

            var current = root;
            var relative = path.Substring(root.Length);
            var parts = relative.Split(new[] { Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var part in parts)
            {
                var candidate = Path.Combine(current, part);
                if (File.Exists(candidate) || Directory.Exists(candidate))
                {
                    current = candidate;
                    continue;
                }

                if (!Directory.Exists(current))
                    return path;

                var found = false;
                foreach (var entry in Directory.EnumerateFileSystemEntries(current))
                {
                    if (!string.Equals(Path.GetFileName(entry), part, StringComparison.OrdinalIgnoreCase))
                        continue;

                    current = entry;
                    found = true;
                    break;
                }

                if (!found)
                {
                    var alias = ResolveGeneratedAddressablesBundleAlias(path);
                    if (!string.Equals(alias, path, StringComparison.OrdinalIgnoreCase))
                    {
                        lock (resolvedPaths)
                            resolvedPaths[path] = alias;
                    }
                    return alias;
                }
            }

            if (!File.Exists(current) && !Directory.Exists(current))
            {
                var alias = ResolveGeneratedAddressablesBundleAlias(path);
                if (!string.Equals(alias, path, StringComparison.OrdinalIgnoreCase))
                {
                    lock (resolvedPaths)
                        resolvedPaths[path] = alias;
                    return alias;
                }
            }

            lock (resolvedPaths)
                resolvedPaths[path] = current;
            return current;
#else
            return path;
#endif
        }

        private static string ResolveGeneratedAddressablesBundleAlias(string path)
        {
#if !UNITY_EDITOR && UNITY_ANDROID
            var directory = Path.GetDirectoryName(path);
            if (string.IsNullOrEmpty(directory) || !Directory.Exists(directory))
                return path;

            var fileName = Path.GetFileName(path);
            if (string.IsNullOrEmpty(fileName))
                return path;

            string suffix = null;
            if (fileName.EndsWith("_monoscripts.bundle", StringComparison.OrdinalIgnoreCase))
                suffix = "_monoscripts.bundle";
            else if (fileName.EndsWith("_unitybuiltinassets.bundle", StringComparison.OrdinalIgnoreCase))
                suffix = "_unitybuiltinassets.bundle";

            if (suffix == null)
                return path;

            foreach (var candidate in Directory.EnumerateFiles(directory, "*" + suffix))
            {
                Debug.LogWarningFormat("Addressables generated bundle alias: {0} -> {1}", path, candidate);
                return candidate;
            }
#endif
            return path;
        }

        private static bool TryResolveExistingAssetBundleFile(string path, out string resolvedPath)
        {
            resolvedPath = ResolveAssetBundlePath(path);
            if (!string.IsNullOrEmpty(resolvedPath) && File.Exists(resolvedPath))
                return true;

            var aliasPath = ResolveKnownAndroidBundleAlias(resolvedPath);
            if (!string.Equals(aliasPath, resolvedPath, StringComparison.OrdinalIgnoreCase) && File.Exists(aliasPath))
            {
                Debug.LogWarningFormat("AssetBundle known Android alias: {0} -> {1}", resolvedPath, aliasPath);
                resolvedPath = aliasPath;
                return true;
            }

            lock (missingBundleWarnings)
            {
                if (missingBundleWarnings.Add(path))
                    Debug.LogWarningFormat("AssetBundle file missing: {0} -> {1}", path, resolvedPath);
            }
            return false;
        }

        private static string ResolveKnownAndroidBundleAlias(string resolvedPath)
        {
#if !UNITY_EDITOR && UNITY_ANDROID
            if (string.IsNullOrEmpty(resolvedPath))
                return resolvedPath;

            var normalized = resolvedPath.Replace('\\', '/');
            var persistentRoot = Application.persistentDataPath.Replace('\\', '/').TrimEnd('/') + "/";
            string relative = normalized.StartsWith(persistentRoot, StringComparison.OrdinalIgnoreCase)
                ? normalized.Substring(persistentRoot.Length)
                : normalized;

            string aliasRelative = null;
            if (relative.EndsWith("Android/MasterDuel/BG/timer/timer_c001", StringComparison.OrdinalIgnoreCase))
                aliasRelative = "Android/MasterDuel/BG/Timer/Timer_013";
            else if (relative.EndsWith("Android/MasterDuel/BG/timer/phasebutton_c001", StringComparison.OrdinalIgnoreCase))
                aliasRelative = "Android/MasterDuel/BG/Timer/PhaseButton_013";
            else if (relative.EndsWith("Android/MasterDuel/BG/Timer/PlayableGuide_C001_Near", StringComparison.OrdinalIgnoreCase))
                aliasRelative = "Android/MasterDuel/BG/Timer/PlayableGuide_c001_near_Mat13";
            else if (relative.EndsWith("Android/MasterDuel/BG/Timer/PlayableGuide_C001_Far", StringComparison.OrdinalIgnoreCase))
                aliasRelative = "Android/MasterDuel/BG/Timer/PlayableGuide_c001_far_Mat13";

            if (aliasRelative == null)
                return resolvedPath;

            return Path.Combine(Application.persistentDataPath, aliasRelative);
#else
            return resolvedPath;
#endif
        }

        private static void WarnInvalidBundleOnce(string resolvedPath, string context, Exception exception = null)
        {
            var key = resolvedPath + "|" + context;
            lock (invalidBundleWarnings)
            {
                if (!invalidBundleWarnings.Add(key))
                    return;
            }

            if (exception == null)
                Debug.LogWarningFormat("AssetBundle load skipped: {0} ({1})", resolvedPath, context);
            else
                Debug.LogWarningFormat("AssetBundle load skipped: {0} ({1}: {2})", resolvedPath, context, exception.Message);
        }

        private static AssetBundle SafeLoadAssetBundleFromFile(string resolvedPath, string context)
        {
            try
            {
                var ab = AssetBundle.LoadFromFile(resolvedPath);
                if (ab == null)
                    WarnInvalidBundleOnce(resolvedPath, context + " returned null");
                return ab;
            }
            catch (Exception e)
            {
                WarnInvalidBundleOnce(resolvedPath, context, e);
                return null;
            }
        }

        private static bool TryStartAssetBundleLoadFromFileAsync(string resolvedPath, string context, out AssetBundleCreateRequest request)
        {
            request = null;
            try
            {
                request = AssetBundle.LoadFromFileAsync(resolvedPath);
                if (request == null)
                    WarnInvalidBundleOnce(resolvedPath, context + " returned null request");
                return request != null;
            }
            catch (Exception e)
            {
                WarnInvalidBundleOnce(resolvedPath, context, e);
                return false;
            }
        }

        private static AssetBundle FinishAssetBundleLoadFromFileAsync(AssetBundleCreateRequest request, string resolvedPath, string context)
        {
            if (request == null)
                return null;

            try
            {
                var ab = request.assetBundle;
                if (ab == null)
                    WarnInvalidBundleOnce(resolvedPath, context + " returned null");
                return ab;
            }
            catch (Exception e)
            {
                WarnInvalidBundleOnce(resolvedPath, context, e);
                return null;
            }
        }

        private static string[] GetAllAssetNamesSafe(AssetBundle ab, string context)
        {
            if (ab == null)
                return Array.Empty<string>();

            try
            {
                return ab.GetAllAssetNames();
            }
            catch (Exception e)
            {
                WarnInvalidBundleOnce(context, "GetAllAssetNames", e);
                return Array.Empty<string>();
            }
        }

        private static T LoadAssetSafe<T>(AssetBundle ab, string assetName, string context) where T : UnityEngine.Object
        {
            if (ab == null || string.IsNullOrEmpty(assetName))
                return null;

            try
            {
                return ab.LoadAsset<T>(assetName);
            }
            catch (Exception e)
            {
                WarnInvalidBundleOnce(context + "|" + assetName, "LoadAsset<" + typeof(T).Name + ">", e);
                return null;
            }
        }

        private static UnityEngine.Object[] LoadAllAssetsSafe(AssetBundle ab, string context)
        {
            if (ab == null)
                return Array.Empty<UnityEngine.Object>();

            try
            {
                return ab.LoadAllAssets();
            }
            catch (Exception e)
            {
                WarnInvalidBundleOnce(context, "LoadAllAssets", e);
                return Array.Empty<UnityEngine.Object>();
            }
        }

        public static Material LoadMaterialFromFile(string path, string assetName)
        {
            if (!TryResolveExistingAssetBundleFile(path, out var resolvedPath))
                return null;

            var ab = SafeLoadAssetBundleFromFile(resolvedPath, "LoadMaterialFromFile");
            if (ab == null)
                return null;

            var material = LoadAssetSafe<Material>(ab, assetName, resolvedPath);
            ab.Unload(false);
            return material;
        }

        private static GameObject LoadFirstGameObject(AssetBundle ab)
        {
            if (ab == null)
                return null;

            foreach (var assetName in GetAllAssetNamesSafe(ab, ab.name))
            {
                var prefab = LoadAssetSafe<GameObject>(ab, assetName, ab.name);
                if (prefab != null)
                    return prefab;
            }
            return null;
        }

        private static List<GameObject> LoadGameObjects(AssetBundle ab)
        {
            var prefabs = new List<GameObject>();
            if (ab == null)
                return prefabs;

            foreach (var assetName in GetAllAssetNamesSafe(ab, ab.name))
            {
                var prefab = LoadAssetSafe<GameObject>(ab, assetName, ab.name);
                if (prefab != null)
                    prefabs.Add(prefab);
            }
            return prefabs;
        }

        public static IEnumerator CacheFromFileAsync(string path)
        {
            if (!TryResolveExistingAssetBundleFile(path, out var resolvedPath))
                yield break;

            if (!TryStartAssetBundleLoadFromFileAsync(resolvedPath, "CacheFromFileAsync", out var abr))
                yield break;
            while (!abr.isDone)
                yield return null;
            FinishAssetBundleLoadFromFileAsync(abr, resolvedPath, "CacheFromFileAsync");
        }
        public static GameObject LoadFromFile(string path, bool cache = false)
        {
            GameObject returnValue;
            if (cachedAB.TryGetValue(path, out returnValue))
            {
                returnValue = InstantiateRuntimeGameObject(returnValue, path);
                return returnValue;
            }

            if (!TryResolveExistingAssetBundleFile(Program.root + path, out var resolvedPath))
            {
                var fallback = CreateRuntimeFallbackForMissingFile(path);
                if (fallback != null)
                    return fallback;
                return null;
            }

            AssetBundle ab;
            ab = SafeLoadAssetBundleFromFile(resolvedPath, "LoadFromFile " + path);
            var prefab = LoadFirstGameObject(ab);
            if (prefab != null)
            {
                if (cache)
                    cachedAB[path] = prefab;
                returnValue = InstantiateRuntimeGameObject(prefab, path);
                ab.Unload(false);
                return returnValue;
            }
            if (ab != null)
                ab.Unload(false);
            var emptyBundleFallback = CreateRuntimeFallbackForMissingFile(path);
            if (emptyBundleFallback != null)
                return emptyBundleFallback;
            return null;
        }

        private static GameObject CreateRuntimeFallbackForMissingFile(string path)
        {
            if (IsAttackLineEffectPath(path))
            {
                LogRuntimeFallbackCreated(path);
                return RuntimeDuelFallbackFactory.CreateAttackLine();
            }

            if (IsTargetLineEffectPath(path))
            {
                LogRuntimeFallbackCreated(path);
                return RuntimeDuelFallbackFactory.CreateSimpleLine("FallbackTargetLine", new Color(1f, 0.85f, 0.2f, 0.95f));
            }

            if (IsEquipLineEffectPath(path))
            {
                LogRuntimeFallbackCreated(path);
                return RuntimeDuelFallbackFactory.CreateSimpleLine("FallbackEquipLine", new Color(0.2f, 1f, 0.55f, 0.95f));
            }

            if (IsDuelDeckAppearancePath(path))
            {
                LogRuntimeFallbackCreated(path);
                return RuntimeDuelFallbackFactory.CreateDeckAppearance();
            }

            if (IsDuelChainSpotPath(path))
            {
                LogRuntimeFallbackCreated(path);
                return RuntimeDuelFallbackFactory.CreateChainSpot();
            }

            if (IsSummonSynchroTimelinePath(path))
            {
                LogRuntimeFallbackCreated(path);
                return RuntimeDuelFallbackFactory.CreateSummonSynchroTimeline(path);
            }

            if (IsDuelTextTimelinePath(path))
            {
                LogRuntimeFallbackCreated(path);
                return RuntimeDuelFallbackFactory.CreateDuelText(path);
            }

            if (IsDuelCardMoveTimelinePath(path))
            {
                LogRuntimeFallbackCreated(path);
                return RuntimeDuelFallbackFactory.CreateDummyCardTimeline(path);
            }

            if (IsDuelEffectPath(path))
            {
                LogRuntimeFallbackCreated(path);
                return RuntimeDuelFallbackFactory.CreateSimpleEffect(path);
            }

            return null;
        }

        private static void LogRuntimeFallbackCreated(string path)
        {
            lock (runtimeFallbackWarnings)
            {
                if (runtimeFallbackWarnings.Add(path))
                    Debug.LogWarningFormat("AssetBundle runtime fallback created: {0}", path);
            }
        }

        private static string NormalizeBundlePath(string path)
        {
            return string.IsNullOrEmpty(path) ? string.Empty : path.Replace('\\', '/');
        }

        private static bool PathContains(string path, string value)
        {
            return NormalizeBundlePath(path).IndexOf(value, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static bool PathEndsWith(string path, string value)
        {
            return NormalizeBundlePath(path).EndsWith(value, StringComparison.OrdinalIgnoreCase);
        }

        private static GameObject InstantiateRuntimeGameObject(GameObject prefab, string sourcePath, bool suppressOptionalScenery = true)
        {
            if (prefab == null)
                return null;

            var instance = Instantiate(prefab);
            if (suppressOptionalScenery)
                SuppressQuestOptionalSceneryAtSpawn(instance, sourcePath);
            return instance;
        }

        private static void SuppressQuestOptionalSceneryAtSpawn(GameObject instance, string sourcePath)
        {
#if !UNITY_EDITOR && UNITY_ANDROID
            if (instance == null)
                return;

            var combined = (NormalizeBundlePath(sourcePath) + "/" + instance.name).ToLowerInvariant();
            if (!IsQuestOptionalSceneryPath(combined) && !HasQuestOptionalSceneryName(instance.transform))
                return;

            foreach (var renderer in instance.GetComponentsInChildren<Renderer>(true))
                if (renderer != null)
                    renderer.enabled = false;
            foreach (var collider in instance.GetComponentsInChildren<Collider>(true))
                if (collider != null)
                    collider.enabled = false;
            instance.SetActive(false);
#endif
        }

        private static bool HasQuestOptionalSceneryName(Transform root)
        {
            if (root == null)
                return false;

            if (IsQuestOptionalSceneryPath(root.name))
                return true;

            foreach (Transform child in root)
                if (HasQuestOptionalSceneryName(child))
                    return true;
            return false;
        }

        private static bool IsQuestOptionalSceneryPath(string path)
        {
            if (string.IsNullOrEmpty(path))
                return false;

            var normalized = NormalizeBundlePath(path).ToLowerInvariant();
            return normalized.Contains("/outside/") ||
                normalized.Contains("outside") ||
                normalized.Contains("scenery") ||
                normalized.Contains("landscape") ||
                normalized.Contains("tree") ||
                normalized.Contains("leaf") ||
                normalized.Contains("foliage") ||
                normalized.Contains("grass") ||
                normalized.Contains("bush") ||
                normalized.Contains("shrub") ||
                normalized.Contains("flower") ||
                normalized.Contains("rabbit") ||
                normalized.Contains("bunny") ||
                normalized.Contains("usagi") ||
                IsQuestOptionalMateSceneryToken(normalized) ||
                normalized.Contains("stand") ||
                normalized.Contains("mascot") ||
                normalized.Contains("prop") ||
                normalized.Contains("accessory") ||
                normalized.Contains("ornament") ||
                normalized.Contains("decoration") ||
                normalized.Contains("rock") ||
                normalized.Contains("stone") ||
                normalized.Contains("cliff") ||
                normalized.Contains("mountain") ||
                normalized.Contains("cloud") ||
                normalized.Contains("sky") ||
                normalized.Contains("leafshadow") ||
                normalized.Contains("treeshadow");
        }

        private static bool IsQuestOptionalMateSceneryToken(string value)
        {
            if (string.IsNullOrEmpty(value))
                return false;

            return value.Contains("/mate/") ||
                value.Contains("\\mate\\") ||
                value.Contains("duelmate") ||
                value.Contains("mate_") ||
                value.Contains("_mate") ||
                value.EndsWith("/mate") ||
                value.EndsWith("\\mate");
        }

        private static bool IsAttackLineEffectPath(string path)
        {
            return PathEndsWith(path, "MasterDuel/Effects/Other/fxp_atk_select_arrow_001");
        }

        private static bool IsTargetLineEffectPath(string path)
        {
            return PathEndsWith(path, "MasterDuel/Effects/Other/fxp_target_arrow_001");
        }

        private static bool IsEquipLineEffectPath(string path)
        {
            return PathEndsWith(path, "MasterDuel/Effects/Other/fxp_equip_arrow_001");
        }

        private static bool IsDuelDeckAppearancePath(string path)
        {
            return PathEndsWith(path, "MasterDuel/Timeline/DuelDeckAppearance");
        }

        private static bool IsDuelChainSpotPath(string path)
        {
            return PathEndsWith(path, "MasterDuel/Timeline/DuelChain/ChainSpot");
        }

        private static bool IsSummonSynchroTimelinePath(string path)
        {
            return PathContains(path, "MasterDuel/Timeline/summon/summonsynchro/");
        }

        private static bool IsDuelTextTimelinePath(string path)
        {
            return PathContains(path, "MasterDuel/Timeline/DuelText/");
        }

        private static bool IsDuelCardMoveTimelinePath(string path)
        {
            return PathContains(path, "MasterDuel/Timeline/DuelCardMove/");
        }

        private static bool IsDuelEffectPath(string path)
        {
            return PathContains(path, "MasterDuel/Effects/");
        }
        public static IEnumerator<GameObject> LoadFromFileAsync(string path, bool cache = false, bool copy = true, bool suppressOptionalScenery = true)
        {
            GameObject returnValue;
            if (cachedAB.TryGetValue(path, out returnValue))
            {
                if (copy)
                {
                    returnValue = InstantiateRuntimeGameObject(returnValue, path, suppressOptionalScenery);
                    yield return returnValue;
                }
                yield break;
            }

            if (!TryResolveExistingAssetBundleFile(Program.root + path, out var resolvedPath))
            {
                var missingFallback = CreateRuntimeFallbackForMissingFile(path);
                if (missingFallback != null)
                    yield return missingFallback;
                yield break;
            }

            if (!TryStartAssetBundleLoadFromFileAsync(resolvedPath, "LoadFromFileAsync " + path, out var abr))
            {
                var startFallback = CreateRuntimeFallbackForMissingFile(path);
                if (startFallback != null)
                    yield return startFallback;
                yield break;
            }
            while (!abr.isDone)
                yield return null;
            AssetBundle ab = FinishAssetBundleLoadFromFileAsync(abr, resolvedPath, "LoadFromFileAsync " + path);
            var prefab = LoadFirstGameObject(ab);
            if (prefab != null)
            {
                if (cache)
                    cachedAB[path] = prefab;
                returnValue = prefab;
            }
            if (ab != null)
                ab.Unload(false);
            if (returnValue == null)
            {
                var emptyFallback = CreateRuntimeFallbackForMissingFile(path);
                if (emptyFallback != null)
                {
                    yield return emptyFallback;
                    yield break;
                }
            }
            if (copy && returnValue != null)
                yield return InstantiateRuntimeGameObject(returnValue, path, suppressOptionalScenery);
            else if (!copy && returnValue != null)
                yield return returnValue;
        }
        public static GameObject LoadFromFolder(string path, string abName = "GameObject", bool cache = false, bool suppressOptionalScenery = true)
        {
            GameObject returnValue = new GameObject(abName);
            if (suppressOptionalScenery)
                SuppressQuestOptionalSceneryAtSpawn(returnValue, path);
            if (cachedABFolder.TryGetValue(path, out var cachedPrefabs))
            {
                foreach (var prefab in cachedPrefabs)
                {
                    var go = InstantiateRuntimeGameObject(prefab, path, suppressOptionalScenery);
                    go.transform.SetParent(returnValue.transform, false);
                }
                return returnValue;
            }

            List<AssetBundle> bundles = new List<AssetBundle>();

            DirectoryInfo dir = new DirectoryInfo(ResolveAssetBundlePath(Program.root + path));
            if (!dir.Exists)
                return returnValue;

            FileInfo[] files = dir.GetFiles("*");
            for (int i = 0; i < files.Length; i++)
            {
                var bundle = SafeLoadAssetBundleFromFile(files[i].FullName, "LoadFromFolder " + path);
                if (bundle != null)
                    bundles.Add(bundle);
            }
            List<GameObject> cached = new List<GameObject>();
            foreach (AssetBundle bundle in bundles)
            {
                cached.AddRange(LoadGameObjects(bundle));
            }
            if (cache)
                if (!cachedABFolder.ContainsKey(path))
                    cachedABFolder.Add(path, cached);
            foreach (var prefab in cached)
            {
                var go = InstantiateRuntimeGameObject(prefab, path, suppressOptionalScenery);
                go.transform.SetParent(returnValue.transform, false);
            }

            foreach (AssetBundle bundle in bundles)
                if (bundle != null)
                    bundle.Unload(false);

            return returnValue;
        }
        public static IEnumerator<GameObject> LoadFromFolderAsync(string path, string abName = "GameObject", bool cache = false, bool copy = true, bool suppressOptionalScenery = true)
        {
            GameObject returnValue = new GameObject(abName);
            if (suppressOptionalScenery)
                SuppressQuestOptionalSceneryAtSpawn(returnValue, path);
            if (cachedABFolder.TryGetValue(path, out var cachedPrefabs))
            {
                if (copy)
                {
                    foreach (var prefab in cachedPrefabs)
                    {
                        var go = InstantiateRuntimeGameObject(prefab, path, suppressOptionalScenery);
                        go.transform.SetParent(returnValue.transform, false);
                    }
                    yield return returnValue;
                }
                else
                    Destroy(returnValue);
                yield break;
            }

            List<AssetBundle> bundles = new List<AssetBundle>();

            DirectoryInfo dir = new DirectoryInfo(ResolveAssetBundlePath(Program.root + path));
            if (!dir.Exists)
            {
                if (copy)
                    yield return returnValue;
                else
                    Destroy(returnValue);
                yield break;
            }

            FileInfo[] files = dir.GetFiles("*");
            for (int i = 0; i < files.Length; i++)
            {
                if (!TryStartAssetBundleLoadFromFileAsync(files[i].FullName, "LoadFromFolderAsync " + path, out var abr))
                    continue;
                while (!abr.isDone)
                    yield return null;
                var bundle = FinishAssetBundleLoadFromFileAsync(abr, files[i].FullName, "LoadFromFolderAsync " + path);
                if (bundle != null)
                    bundles.Add(bundle);
            }
            var cached = new List<GameObject>();
            foreach (AssetBundle bundle in bundles)
            {
                cached.AddRange(LoadGameObjects(bundle));
            }

            if (cache)
                if (!cachedABFolder.ContainsKey(path))
                    cachedABFolder.Add(path, cached);
            foreach (AssetBundle bundle in bundles)
                if (bundle != null)
                    bundle.Unload(false);
            if (copy)
            {
                foreach (var prefab in cached)
                {
                    var go = InstantiateRuntimeGameObject(prefab, path, suppressOptionalScenery);
                    go.transform.SetParent(returnValue.transform, false);
                }
                yield return returnValue;
            }
            else
            {
                Destroy(returnValue);
                yield return null;
            }
        }

        static readonly object pMatLock = new object();
        static bool loadingPMat;
        public static IEnumerator<Material> LoadProtectorMaterial(string code)
        {
            if (code == Items.randomCode.ToString())
                code = Program.items.GetRandomItem(Items.ItemType.Protector).id.ToString();

            if (cachedPMat.TryGetValue(code, out var material))
            {
                if (material != null)
                {
                    yield return material;
                    yield break;
                }
                else
                    cachedPMat.Remove(code);
            }
            while (true)
            {
                lock (pMatLock)
                {
                    if (!loadingPMat)
                    {
                        loadingPMat = true;
                        break;
                    }
                }
                yield return null;
            }

            var folder = ResolveAssetBundlePath(Program.root + "MasterDuel/Protector/" + code);
            if (!Directory.Exists(folder))
            {
                lock (pMatLock)
                    loadingPMat = false;
                yield return CreateFallbackProtectorMaterial(code);
                yield break;
            }
            var files = Directory.GetFiles(folder);

            AssetBundle matAB = null;
            List<AssetBundle> abs = new List<AssetBundle>();
            foreach (var file in files)
            {
                var resolvedFile = ResolveAssetBundlePath(file);
                if (!TryStartAssetBundleLoadFromFileAsync(resolvedFile, "LoadProtectorMaterial " + code, out var abr))
                    continue;
                while(!abr.isDone)
                    yield return null;
                var bundle = FinishAssetBundleLoadFromFileAsync(abr, resolvedFile, "LoadProtectorMaterial " + code);
                if (bundle != null)
                    abs.Add(bundle);
                if (bundle != null && Path.GetFileName(file) == code)
                    matAB = bundle;
            }
            if(matAB == null)
            {
                foreach (var ab in abs)
                    if (ab != null)
                        ab.Unload(false);
                lock (pMatLock)
                    loadingPMat = false;
                yield return CreateFallbackProtectorMaterial(code);
                yield break;
            }

            material = LoadAssetSafe<Material>(matAB, "PMat", "Protector " + code);
            if (material == null)
                material = CreateFallbackProtectorMaterial(code);
            material.renderQueue = 3000;
            foreach (var ab in abs)
                if (ab != null)
                    ab.Unload(false);

            if (cachedPMat.ContainsKey(code))
                material = cachedPMat[code];
            else
                cachedPMat.Add(code, material);
            lock (pMatLock)
            {
                loadingPMat = false;
            }
            yield return material;
        }
        public static IEnumerator<Material> LoadFrameMaterial(string code)
        {
            if (code == Items.randomCode.ToString())
                code = Items.lastRandomFrameID;

            if (!TryResolveExistingAssetBundleFile(Program.root + "MasterDuel/Frame/ProfileFrameMat" + code, out var framePath))
            {
                yield return CreateFallbackProfileFrameMaterial(code);
                yield break;
            }

            if (!TryStartAssetBundleLoadFromFileAsync(framePath, "LoadFrameMaterial " + code, out var abr))
            {
                yield return CreateFallbackProfileFrameMaterial(code);
                yield break;
            }
            while (!abr.isDone)
                yield return null;
            var ab = FinishAssetBundleLoadFromFileAsync(abr, framePath, "LoadFrameMaterial " + code);
            if (ab == null)
            {
                yield return CreateFallbackProfileFrameMaterial(code);
                yield break;
            }
            var material = LoadAssetSafe<Material>(ab, "ProfileFrameMat" + code, framePath);
            ab.Unload(false);
            if (material == null)
            {
                yield return CreateFallbackProfileFrameMaterial(code);
                yield break;
            }
            TextureManager.ChangeProfileFrameMaterialWrapMode(material);
            yield return material;
        }
        public static IEnumerator<Material> LoadMaterialAsync(string path)
        {
            if (!TryResolveExistingAssetBundleFile(Program.root + path, out var resolvedPath))
            {
                yield return CreateFallbackMaterial(Path.GetFileName(path) + "_Fallback");
                yield break;
            }

            if (!TryStartAssetBundleLoadFromFileAsync(resolvedPath, "LoadMaterialAsync " + path, out var abr))
            {
                yield return CreateFallbackMaterial(Path.GetFileName(path) + "_Fallback");
                yield break;
            }
            while (!abr.isDone) 
                yield return null;
            var ab = FinishAssetBundleLoadFromFileAsync(abr, resolvedPath, "LoadMaterialAsync " + path);
            if (ab == null)
            {
                yield return CreateFallbackMaterial(Path.GetFileName(path) + "_Fallback");
                yield break;
            }
            var matetial = LoadAssetSafe<Material>(ab, Path.GetFileName(path), resolvedPath);
            ab.Unload(false);
            if (matetial == null)
                matetial = CreateFallbackMaterial(Path.GetFileName(path) + "_Fallback");
            yield return matetial;
        }

        public static IEnumerator<Mate> LoadMateAsync(int code)
        {
            if (CustomGlbMateLoader.HasModel(code))
            {
                var customMate = CustomGlbMateLoader.LoadMateAsync(code);
                while (customMate.MoveNext())
                    yield return null;
                yield return customMate.Current;
                yield break;
            }

            Items.Item item = new Items.Item();
            foreach (var mate in Program.items.mates)
            {
                if (mate.id == code)
                {
                    item = mate;
                    break;
                }
            }
            Mate.MateType type = Mate.MateType.MasterDuel;
            if (item.id == 0 && File.Exists(ResolveAssetBundlePath(Program.root + "CrossDuel/" + code + ".bundle")))
                type = Mate.MateType.CrossDuel;
            Mate returnValue = null;
            if (type == Mate.MateType.CrossDuel)
            {
                if (!TryResolveExistingAssetBundleFile(Program.root + "CrossDuel/" + code + ".bundle", out var matePath))
                {
                    Debug.LogWarningFormat("Mate load failed: CrossDuel bundle missing. code={0}", code);
                    yield break;
                }

                if (!TryStartAssetBundleLoadFromFileAsync(matePath, "LoadMateAsync CrossDuel " + code, out var abr))
                {
                    Debug.LogWarningFormat("Mate load failed: CrossDuel bundle did not start loading. code={0} path={1}", code, matePath);
                    yield break;
                }
                while (!abr.isDone)
                    yield return null;
                var ab = FinishAssetBundleLoadFromFileAsync(abr, matePath, "LoadMateAsync CrossDuel " + code);
                var all = LoadAllAssetsSafe(ab, matePath);
                if (ab != null)
                    ab.Unload(false);
                foreach (var asset in all)
                {
                    if (asset is NamedAssetContainer container)
                    {
                        container.TryGet<GameObject>("prefab", out var prefab);
                        container.TryGet<NamedAssetContainer>("Timelines", out var timelines);
                        container.TryGet<ParameterContainer>("Settings", out var settings);
                        if (prefab == null || timelines == null)
                            continue;
                        var mateGo = InstantiateRuntimeGameObject(prefab, matePath, false);
                        mateGo.AddComponent<FieldParamEventController_AnimationEventReceiver>();
                        foreach (var s in timelines.AllNamedAssetNames())
                        {
                            timelines.TryGet<GameObject>(s, out var timeline);
                            if (timeline == null)
                                continue;
                            var newT = Instantiate(timeline);
                            newT.transform.SetParent(mateGo.transform, false);
                            newT.SetActive(true);
                            for (int i = 0; i < newT.transform.childCount; i++)
                            {
                                if (newT.transform.GetChild(i).GetComponent<Volume>() != null)
                                    Destroy(newT.transform.GetChild(i).gameObject);
                                if (newT.transform.GetChild(i).name == "UIBattleDownAni")
                                    Destroy(newT.transform.GetChild(i).gameObject);
                            }
                            var controller = newT.GetComponent<CustomTimelineController>();
                            if (controller == null || controller.checkReplacer == null)
                                continue;
                            var bindTrackInfo = controller.checkReplacer.m_bindTrackInfo;
                            var director = newT.transform.GetChild(0).GetComponent<PlayableDirector>();

                            if (director == null)
                                continue;
                            Dictionary<string, PlayableBinding> bindingDict = new Dictionary<string, PlayableBinding>();
                            foreach (PlayableBinding pb in director.playableAsset.outputs)
                                foreach (var bind in bindTrackInfo)
                                    if (pb.streamName == bind.m_name
                                        && director.GetGenericBinding(pb.sourceObject) == null)
                                        director.SetGenericBinding(pb.sourceObject, mateGo.GetComponent<Animator>());
                        }
                        returnValue = mateGo.AddComponent<Mate>();
                    }
                }
                if (returnValue == null)
                    Debug.LogWarningFormat("Mate load failed: CrossDuel bundle has no supported mate prefab/timelines. code={0} path={1} assets={2}", code, matePath, all.Length);
            }
            else
            {
                bool mateInFolder = false;
                var matePath = Program.items.GetPathByCode(code.ToString(), Items.ItemType.Mate);
                IEnumerator<GameObject> ie;
                if (matePath.EndsWith("_Folder"))
                {
                    mateInFolder = true;
                    ie = LoadFromFolderAsync("MasterDuel/" + matePath.Replace("_Folder", string.Empty), suppressOptionalScenery: false);
                }
                else
                    ie = LoadFromFileAsync("MasterDuel/" + matePath, suppressOptionalScenery: false);
                while (ie.MoveNext())
                    yield return null;
                var mateGo = ie.Current;
                if (mateGo == null)
                {
                    Debug.LogWarningFormat("Mate load failed: MasterDuel bundle produced no GameObject. code={0} path={1}", code, matePath);
                    yield break;
                }
                if(mateInFolder)
                {
                    bool foundMateRoot = false;
                    for (int i = 0; i < mateGo.transform.childCount; i++)
                    {
                        if (mateGo.transform.GetChild(i).GetComponent<CharacterCollision>() != null)
                        {
                            var source = mateGo;
                            mateGo = InstantiateRuntimeGameObject(source.transform.GetChild(i).gameObject, matePath, false);
                            Destroy(source);
                            foundMateRoot = true;
                            break;
                        }
                    }
                    if (!foundMateRoot)
                        Debug.LogWarningFormat("Mate load warning: MasterDuel folder had no CharacterCollision child. code={0} path={1}", code, matePath);
                }
                returnValue = mateGo.AddComponent<Mate>();
            }
            if (returnValue == null)
            {
                Debug.LogWarningFormat("Mate load failed: no Mate component created. code={0} type={1}", code, type);
                yield break;
            }
            returnValue.type = type;
            returnValue.code = code;
            yield return returnValue;
        }


    }
}

