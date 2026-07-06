using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.AddressableAssets.ResourceLocators;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceLocations;
using UnityEngine.ResourceManagement.ResourceProviders;

namespace MDPro3
{
    public static class AddressablesResourceAliases
    {
        private const string LocatorId = "MDPro3ResourceAliases";
        private const string DynamicLocatorId = "MDPro3DynamicAddressableAliases";
        private const string AliasRoot = "AddressableAliases";

        private static bool registered;
        private static bool transformRegistered;
        private static Func<IResourceLocation, string> previousInternalIdTransform;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        public static void Register()
        {
            if (registered)
                return;

            RegisterInternalIdTransform();

            var provider = new ResourcesAliasProvider();
            if (Addressables.ResourceManager.ResourceProviders.All(x => x.ProviderId != provider.ProviderId))
                Addressables.ResourceManager.ResourceProviders.Insert(0, provider);

            var dynamicProvider = new DynamicAliasProvider();
            if (Addressables.ResourceManager.ResourceProviders.All(x => x.ProviderId != dynamicProvider.ProviderId))
                Addressables.ResourceManager.ResourceProviders.Insert(0, dynamicProvider);

            if (Addressables.ResourceLocators.All(x => x.LocatorId != DynamicLocatorId))
                Addressables.AddResourceLocator(new DynamicAliasLocator(dynamicProvider.ProviderId));

            if (Addressables.ResourceLocators.Any(x => x.LocatorId == LocatorId))
            {
                registered = true;
                return;
            }

            var map = new ResourceLocationMap(LocatorId, 256);
            var seen = new HashSet<string>();
            foreach (var asset in Resources.LoadAll<UnityEngine.Object>(AliasRoot))
            {
                if (asset == null || string.IsNullOrEmpty(asset.name))
                    continue;

                AddAlias(map, seen, asset.name, asset.GetType(), provider.ProviderId);
                if (asset is Material)
                    AddAlias(map, seen, asset.name + ".mat", asset.name, asset.GetType(), provider.ProviderId);
                if (asset is GameObject)
                    AddAlias(map, seen, asset.name, typeof(GameObject), provider.ProviderId);
                if (asset is ScriptableObject)
                    AddAlias(map, seen, asset.name, typeof(ScriptableObject), provider.ProviderId);
            }

            Addressables.AddResourceLocator(map);
            registered = true;
        }

        private static void RegisterInternalIdTransform()
        {
            if (transformRegistered)
                return;

            previousInternalIdTransform = Addressables.InternalIdTransformFunc;
            Addressables.InternalIdTransformFunc = TransformInternalId;
            transformRegistered = true;
        }

        private static string TransformInternalId(IResourceLocation location)
        {
            var id = previousInternalIdTransform != null
                ? previousInternalIdTransform(location)
                : location.InternalId;

#if !UNITY_EDITOR && UNITY_ANDROID
            if (string.IsNullOrEmpty(id))
                return id;

            var normalized = id.Replace('\\', '/');
            if (normalized.StartsWith(Program.rootWindows64, StringComparison.OrdinalIgnoreCase)
                || normalized.StartsWith(Program.rootAndroid, StringComparison.OrdinalIgnoreCase))
                return ABLoader.ResolveAssetBundlePath(normalized);
#endif
            return id;
        }

        private static void AddAlias(ResourceLocationMap map, HashSet<string> seen, string key, Type type, string providerId)
        {
            AddAlias(map, seen, key, key, type, providerId);
        }

        private static void AddAlias(ResourceLocationMap map, HashSet<string> seen, string key, string resourceName, Type type, string providerId)
        {
            var id = key + "|" + type.FullName;
            if (!seen.Add(id))
                return;

            map.Add(key, new ResourceLocationBase(key, AliasRoot + "/" + resourceName, providerId, type));
        }

        private sealed class ResourcesAliasProvider : ResourceProviderBase
        {
            public override string ProviderId => "MDPro3.ResourcesAliasProvider";

            public override Type GetDefaultType(IResourceLocation location)
            {
                return location.ResourceType ?? typeof(UnityEngine.Object);
            }

            public override void Provide(ProvideHandle provideHandle)
            {
                var path = provideHandle.Location.InternalId;
                var type = provideHandle.Type ?? provideHandle.Location.ResourceType ?? typeof(UnityEngine.Object);
                var asset = Resources.Load(path, type);
                if (asset == null && type != typeof(UnityEngine.Object))
                    asset = Resources.Load<UnityEngine.Object>(path);

                provideHandle.Complete(asset, asset != null,
                    asset == null ? new Exception("Missing Resources alias: " + provideHandle.Location.PrimaryKey + " -> " + path) : null);
            }
        }

        private sealed class DynamicAliasLocator : IResourceLocator
        {
            private readonly string providerId;

            public DynamicAliasLocator(string providerId)
            {
                this.providerId = providerId;
            }

            public string LocatorId => DynamicLocatorId;

            public IEnumerable<object> Keys => Array.Empty<object>();

            public IEnumerable<IResourceLocation> AllLocations => Array.Empty<IResourceLocation>();

            public bool Locate(object key, Type type, out IList<IResourceLocation> locations)
            {
                locations = null;
                if (key is not string address)
                    return false;

                var candidates = GetAddressableCandidates(address);
                if (candidates == null || candidates.Length == 0)
                    return false;

                locations = new List<IResourceLocation>
                {
                    new ResourceLocationBase(address, string.Join("\n", candidates), providerId, type ?? typeof(UnityEngine.Object))
                };
                return true;
            }
        }

        private sealed class DynamicAliasProvider : ResourceProviderBase
        {
            private static readonly Dictionary<string, UnityEngine.Object> FallbackAssets = new Dictionary<string, UnityEngine.Object>();

            public override string ProviderId => "MDPro3.DynamicAddressableAliasProvider";

            public override Type GetDefaultType(IResourceLocation location)
            {
                return location.ResourceType ?? typeof(UnityEngine.Object);
            }

            public override void Provide(ProvideHandle provideHandle)
            {
                var candidates = provideHandle.Location.InternalId
                    .Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);
                TryLoadCandidate(provideHandle, candidates, 0);
            }

            private static void TryLoadCandidate(ProvideHandle provideHandle, string[] candidates, int index)
            {
                if (index >= candidates.Length)
                {
                    var fallback = GetFallbackAsset(provideHandle.Location.PrimaryKey, provideHandle.Type ?? provideHandle.Location.ResourceType);
                    if (fallback != null)
                    {
                        provideHandle.Complete(fallback, true, null);
                        return;
                    }

                    provideHandle.Complete<UnityEngine.Object>(null, true, null);
                    return;
                }

                var candidate = candidates[index];
                var requestedType = provideHandle.Type ?? provideHandle.Location.ResourceType ?? typeof(UnityEngine.Object);
                var locationHandle = Addressables.LoadResourceLocationsAsync(candidate, requestedType);
                locationHandle.Completed += result =>
                {
                    var found = result.Status == AsyncOperationStatus.Succeeded
                        && result.Result != null
                        && result.Result.Count > 0;
                    Addressables.Release(result);

                    if (found)
                        LoadCandidate(provideHandle, candidates, index, candidate, requestedType);
                    else
                        TryLoadObjectLocation(provideHandle, candidates, index, candidate, requestedType);
                };
            }

            private static void TryLoadObjectLocation(
                ProvideHandle provideHandle,
                string[] candidates,
                int index,
                string candidate,
                Type requestedType)
            {
                var locationHandle = Addressables.LoadResourceLocationsAsync(candidate, typeof(UnityEngine.Object));
                locationHandle.Completed += result =>
                {
                    var found = result.Status == AsyncOperationStatus.Succeeded
                        && result.Result != null
                        && result.Result.Count > 0;
                    Addressables.Release(result);

                    if (found)
                        LoadCandidate(provideHandle, candidates, index, candidate, requestedType);
                    else
                        TryLoadCandidate(provideHandle, candidates, index + 1);
                };
            }

            private static void LoadCandidate(
                ProvideHandle provideHandle,
                string[] candidates,
                int index,
                string candidate,
                Type requestedType)
            {
                var handle = Addressables.LoadAssetAsync<UnityEngine.Object>(candidate);
                handle.Completed += result =>
                {
                    if (result.Status != AsyncOperationStatus.Succeeded || result.Result == null)
                    {
                        if (result.IsValid())
                            Addressables.Release(result);
                        TryLoadCandidate(provideHandle, candidates, index + 1);
                        return;
                    }

                    var asset = CoerceAsset(result.Result, requestedType);
                    if (asset == null)
                    {
                        if (result.IsValid())
                            Addressables.Release(result);
                        TryLoadCandidate(provideHandle, candidates, index + 1);
                        return;
                    }

                    provideHandle.Complete(asset, true, null);
                };
            }

            private static UnityEngine.Object CoerceAsset(UnityEngine.Object asset, Type requestedType)
            {
                if (asset == null)
                    return null;

                if (requestedType == null
                    || requestedType == typeof(UnityEngine.Object)
                    || requestedType.IsAssignableFrom(asset.GetType()))
                    return asset;

                if (requestedType == typeof(Sprite) && asset is Texture2D texture)
                {
                    var sprite = Sprite.Create(
                        texture,
                        new Rect(0, 0, texture.width, texture.height),
                        new Vector2(0.5f, 0.5f));
                    sprite.name = asset.name;
                    return sprite;
                }

                return null;
            }

            private static UnityEngine.Object GetFallbackAsset(string key, Type requestedType)
            {
                if (requestedType == null || requestedType == typeof(UnityEngine.Object))
                    requestedType = typeof(Sprite);

                if (requestedType != typeof(Sprite)
                    && requestedType != typeof(Texture)
                    && requestedType != typeof(Texture2D))
                    return null;

                var cacheKey = key + "|" + requestedType.FullName;
                if (FallbackAssets.TryGetValue(cacheKey, out var cached))
                    return cached;

                var texture = new Texture2D(4, 4, TextureFormat.RGBA32, false)
                {
                    name = "AddressableFallback_" + key,
                    hideFlags = HideFlags.DontUnloadUnusedAsset
                };
                var pixels = new Color[16];
                for (var i = 0; i < pixels.Length; i++)
                    pixels[i] = new Color(0.18f, 0.18f, 0.18f, 1f);
                texture.SetPixels(pixels);
                texture.Apply();

                UnityEngine.Object asset = texture;
                if (requestedType == typeof(Sprite))
                {
                    var sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f));
                    sprite.name = texture.name;
                    sprite.hideFlags = HideFlags.DontUnloadUnusedAsset;
                    asset = sprite;
                }

                FallbackAssets[cacheKey] = asset;
                return asset;
            }
        }

        private static string[] GetAddressableCandidates(string key)
        {
            if (string.IsNullOrEmpty(key)
                || key.EndsWith("_HD", StringComparison.OrdinalIgnoreCase)
                || key.EndsWith("_SD", StringComparison.OrdinalIgnoreCase)
                || key.EndsWith(".png", StringComparison.OrdinalIgnoreCase))
                return null;

            if (StartsWithAny(key,
                "ProfileIcon",
                "ProfileFrame",
                "DeckCase",
                "FieldIcon",
                "FieldObjIcon",
                "FieldAvatarBaseIcon"))
            {
                return new[]
                {
                    key + "_HD",
                    key + "_SD",
                    key + ".png"
                };
            }

            return null;
        }

        private static bool StartsWithAny(string value, params string[] prefixes)
        {
            foreach (var prefix in prefixes)
                if (value.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                    return true;
            return false;
        }
    }
}
