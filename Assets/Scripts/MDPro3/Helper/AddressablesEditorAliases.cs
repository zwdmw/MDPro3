#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.AddressableAssets.ResourceLocators;
using UnityEngine.ResourceManagement.ResourceLocations;
using UnityEngine.ResourceManagement.ResourceProviders;

namespace MDPro3
{
    public static class AddressablesEditorAliases
    {
        private const string LocatorId = "MDPro3EditorAliases";
        private const string ItemsKey = "Items";
        private const string ItemsAssetKey = "Items.asset";
        private const string ItemsAssetPath = "Assets/ScriptableObjects/Items.asset";
        private const string SceneMainKey = "SceneMain";
        private const string SceneMainPath = "Assets/Main.unity";

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Register()
        {
            RegisterAssetDatabaseProvider();
            RegisterAliases();

            if (Program.items == null)
                Program.items = AssetDatabase.LoadAssetAtPath<Items>(ItemsAssetPath);
        }

        private static void RegisterAssetDatabaseProvider()
        {
            var providerId = typeof(AssetDatabaseProvider).FullName;
            if (Addressables.ResourceManager.ResourceProviders.Any(provider => provider.ProviderId == providerId))
                return;

            Addressables.ResourceManager.ResourceProviders.Add(new AssetDatabaseProvider(-1f));
        }

        private static void RegisterAliases()
        {
            if (Addressables.ResourceLocators.Any(locator => locator.LocatorId == LocatorId))
                return;

            var assetProviderId = typeof(AssetDatabaseProvider).FullName;
            var sceneProviderId = typeof(SceneProvider).FullName;
            var itemsLocation = new ResourceLocationBase(ItemsKey, ItemsAssetPath, assetProviderId, typeof(Items));
            var sceneLocation = new ResourceLocationBase(SceneMainKey, SceneMainPath, sceneProviderId, typeof(SceneInstance));
            var map = new ResourceLocationMap(LocatorId, 8);
            var registered = new HashSet<string>();
            map.Add(ItemsKey, itemsLocation);
            map.Add(ItemsAssetKey, itemsLocation);
            map.Add(SceneMainKey, sceneLocation);
            registered.Add(ItemsKey);
            registered.Add(ItemsAssetKey);
            registered.Add(SceneMainKey);
            AddGameObjectAlias(map, registered, "ChatItemMe", "Assets/Prefabs/ChatItemMe.prefab", assetProviderId);
            AddGameObjectAlias(map, registered, "ChatItemOp", "Assets/Prefabs/ChatItemOp.prefab", assetProviderId);
            AddGameObjectAlias(map, registered, "ChatItemSystem", "Assets/Prefabs/ChatItemSystem.prefab", assetProviderId);
            AddGameObjectAlias(map, registered, "DeckEditUI", "Assets/Prefabs/UIWidges/DeckEditUI.prefab", assetProviderId);
            AddGameObjectAlias(map, registered, "DeckEditUIMobile", "Assets/Prefabs/UIWidges/DeckEditUIMobile.prefab", assetProviderId);
            AddPrefabAliases(map, registered, assetProviderId);
            AddScriptableObjectAliases(map, registered, assetProviderId);
            AddFontAliases(map, registered, assetProviderId);
            AddAddressablesFolderAliases(map, registered, assetProviderId);
            Addressables.AddResourceLocator(map);
        }

        private static void AddGameObjectAlias(ResourceLocationMap map, HashSet<string> registered, string key, string assetPath, string providerId)
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(assetPath) == null)
            {
                Debug.LogWarning($"Addressables editor alias skipped missing asset: {key} -> {assetPath}");
                return;
            }

            AddAlias(map, registered, key, assetPath, providerId, typeof(GameObject));
        }

        private static void AddPrefabAliases(ResourceLocationMap map, HashSet<string> registered, string providerId)
        {
            AddTypedAliases<GameObject>(map, registered, providerId, "t:Prefab", new[] { "Assets/Prefabs", "Assets/ScriptableObjects/DuelPrefabs" });
        }

        private static void AddScriptableObjectAliases(ResourceLocationMap map, HashSet<string> registered, string providerId)
        {
            foreach (var assetPath in Directory.GetFiles("Assets/ScriptableObjects", "*.asset", SearchOption.AllDirectories))
            {
                var type = AssetDatabase.GetMainAssetTypeAtPath(assetPath);
                if (type == null || !typeof(ScriptableObject).IsAssignableFrom(type))
                    continue;

                AddAlias(map, registered, Path.GetFileNameWithoutExtension(assetPath), NormalizeAssetPath(assetPath), providerId, type);
            }
        }

        private static void AddAddressablesFolderAliases(ResourceLocationMap map, HashSet<string> registered, string providerId)
        {
            foreach (var assetPath in Directory.GetFiles("Assets/Addressables", "*.*", SearchOption.AllDirectories))
            {
                if (assetPath.EndsWith(".meta", StringComparison.OrdinalIgnoreCase))
                    continue;

                var normalized = NormalizeAssetPath(assetPath);
                var key = Path.GetFileNameWithoutExtension(normalized);
                if (string.IsNullOrEmpty(key))
                    continue;

                var mainType = AssetDatabase.GetMainAssetTypeAtPath(normalized);
                if (mainType == null)
                    continue;

                if (AssetDatabase.LoadAssetAtPath<Sprite>(normalized) != null)
                    AddAlias(map, registered, key, normalized, providerId, typeof(Sprite));
                if (AssetDatabase.LoadAssetAtPath<Texture>(normalized) != null)
                    AddAlias(map, registered, key, normalized, providerId, typeof(Texture));
                if (AssetDatabase.LoadAssetAtPath<AudioClip>(normalized) != null)
                    AddAlias(map, registered, key, normalized, providerId, typeof(AudioClip));
                if (AssetDatabase.LoadAssetAtPath<TextAsset>(normalized) != null)
                    AddAlias(map, registered, key, normalized, providerId, typeof(TextAsset));
                if (AssetDatabase.LoadAssetAtPath<Material>(normalized) != null)
                    AddAlias(map, registered, key, normalized, providerId, typeof(Material));
                if (AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(normalized) != null)
                    AddAlias(map, registered, key, normalized, providerId, typeof(RuntimeAnimatorController));

                if (mainType != typeof(Sprite)
                    && mainType != typeof(Texture)
                    && mainType != typeof(Texture2D)
                    && mainType != typeof(AudioClip)
                    && mainType != typeof(TextAsset)
                    && mainType != typeof(Material)
                    && mainType != typeof(RuntimeAnimatorController))
                    AddAlias(map, registered, key, normalized, providerId, mainType);
            }
        }

        private static void AddFontAliases(ResourceLocationMap map, HashSet<string> registered, string providerId)
        {
            foreach (var assetPath in Directory.GetFiles("Assets/Fonts", "*.*", SearchOption.AllDirectories))
            {
                if (assetPath.EndsWith(".meta", StringComparison.OrdinalIgnoreCase))
                    continue;

                var normalized = NormalizeAssetPath(assetPath);
                var key = Path.GetFileNameWithoutExtension(normalized);
                if (string.IsNullOrEmpty(key))
                    continue;

                if (AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(normalized) != null)
                    AddAlias(map, registered, key, normalized, providerId, typeof(TMP_FontAsset));
                if (AssetDatabase.LoadAssetAtPath<Font>(normalized) != null)
                    AddAlias(map, registered, key, normalized, providerId, typeof(Font));
                if (AssetDatabase.LoadAssetAtPath<Material>(normalized) != null)
                    AddAlias(map, registered, key, normalized, providerId, typeof(Material));
            }
        }

        private static void AddTypedAliases<T>(ResourceLocationMap map, HashSet<string> registered, string providerId, string filter, string[] folders)
            where T : UnityEngine.Object
        {
            foreach (var guid in AssetDatabase.FindAssets(filter, folders))
            {
                var assetPath = AssetDatabase.GUIDToAssetPath(guid);
                if (string.IsNullOrEmpty(assetPath))
                    continue;

                var key = Path.GetFileNameWithoutExtension(assetPath);
                if (string.IsNullOrEmpty(key))
                    continue;

                if (AssetDatabase.LoadAssetAtPath<T>(assetPath) != null)
                    AddAlias(map, registered, key, assetPath, providerId, typeof(T));
            }
        }

        private static void AddAlias(ResourceLocationMap map, HashSet<string> registered, string key, string assetPath, string providerId, Type type)
        {
            var registrationKey = key + "|" + type.FullName;
            if (!registered.Add(registrationKey))
                return;

            map.Add(key, new ResourceLocationBase(key, assetPath, providerId, type));
        }

        private static string NormalizeAssetPath(string assetPath)
        {
            return assetPath.Replace('\\', '/');
        }
    }
}
#endif
