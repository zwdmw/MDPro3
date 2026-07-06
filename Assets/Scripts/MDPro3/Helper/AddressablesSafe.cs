using System;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace MDPro3
{
    public static class AddressablesSafe
    {
        private const string AliasRoot = "AddressableAliases/";

        public static void InstantiateAsync(string key, Transform parent, Action<GameObject> completed, Action failed = null)
        {
            AddressablesResourceAliases.Register();

            var handle = Addressables.InstantiateAsync(key, parent, false);
            handle.Completed += result =>
            {
                if (result.Status == AsyncOperationStatus.Succeeded && result.Result != null)
                {
                    completed?.Invoke(result.Result);
                    return;
                }

                var message = result.OperationException != null ? result.OperationException.Message : "null result";
                if (result.IsValid())
                    Addressables.Release(result);

                var fallback = InstantiateFromResources(key, parent);
                if (fallback != null)
                {
                    Debug.LogWarning("AddressablesSafe: instantiated Resources alias for " + key + " after Addressables failed: " + message);
                    completed?.Invoke(fallback);
                    return;
                }

                Debug.LogError("AddressablesSafe: failed to instantiate " + key + ": " + message);
                failed?.Invoke();
            };
        }

        public static void LoadAssetAsync<T>(string key, Action<T> completed, Action failed = null) where T : UnityEngine.Object
        {
            AddressablesResourceAliases.Register();

            var handle = Addressables.LoadAssetAsync<T>(key);
            handle.Completed += result =>
            {
                if (result.Status == AsyncOperationStatus.Succeeded && result.Result != null)
                {
                    completed?.Invoke(result.Result);
                    return;
                }

                var message = result.OperationException != null ? result.OperationException.Message : "null result";
                if (result.IsValid())
                    Addressables.Release(result);

                var fallback = LoadFromResources<T>(key);
                if (fallback != null)
                {
                    Debug.LogWarning("AddressablesSafe: loaded Resources alias for " + key + " after Addressables failed: " + message);
                    completed?.Invoke(fallback);
                    return;
                }

                Debug.LogError("AddressablesSafe: failed to load " + key + ": " + message);
                failed?.Invoke();
            };
        }

        private static GameObject InstantiateFromResources(string key, Transform parent)
        {
            var prefab = LoadFromResources<GameObject>(key);
            if (prefab == null)
                return null;

            var instance = UnityEngine.Object.Instantiate(prefab, parent, false);
            instance.name = prefab.name;
            return instance;
        }

        private static T LoadFromResources<T>(string key) where T : UnityEngine.Object
        {
            var asset = Resources.Load<T>(AliasRoot + key);
            if (asset == null)
                asset = Resources.Load<T>(key);
            return asset;
        }
    }
}
