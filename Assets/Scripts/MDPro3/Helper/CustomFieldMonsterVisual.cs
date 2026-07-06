using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using GLTFast;
using UnityEngine;

namespace MDPro3
{
    public sealed class CustomFieldMonsterVisual : MonoBehaviour
    {
        public const string ModelFolder = "Picture/Art3D";

        private const float TargetHeight = 7f;
        private const string GlbExtension = ".glb";
        private static readonly HashSet<string> FailedModelPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        private int currentCode;
        private Coroutine loadCoroutine;
        private GameObject currentModel;
        public Action<int> ModelReady;
        public Action<int> ModelUnavailable;

        public static bool HasModel(int code)
        {
            return FindUsableModelPath(code) != null;
        }

        public static string FindModelPath(int code)
        {
            var path = FindModelPathInFolder(ModelFolder, code);
            if (path != null)
                return path;

            return FindModelPathInFolder(CustomGlbMateLoader.ModelFolder, code);
        }

        public void Refresh(int code, bool visible)
        {
            if (!visible || code <= 0 || FindUsableModelPath(code) == null)
            {
                Hide();
                return;
            }

            if (currentCode == code)
            {
                if (currentModel != null && loadCoroutine == null)
                    currentModel.SetActive(true);
                return;
            }

            Hide();
            currentCode = code;
            loadCoroutine = StartCoroutine(LoadModel(code));
        }

        public void Hide()
        {
            currentCode = 0;

            if (loadCoroutine != null)
            {
                StopCoroutine(loadCoroutine);
                loadCoroutine = null;
            }

            if (currentModel != null)
            {
                Destroy(currentModel);
                currentModel = null;
            }
        }

        private IEnumerator LoadModel(int code)
        {
            var path = FindUsableModelPath(code);
            if (path == null)
            {
                loadCoroutine = null;
                ModelUnavailable?.Invoke(code);
                yield break;
            }

            var root = new GameObject("FieldMonsterGLB_" + code);
            var modelRoot = new GameObject("Model");
            root.transform.SetParent(transform, false);
            root.transform.localPosition = Vector3.zero;
            root.transform.localRotation = Quaternion.identity;
            root.transform.localScale = Vector3.one;
            SetLayerRecursively(root, gameObject.layer);
            root.SetActive(false);
            modelRoot.transform.SetParent(root.transform, false);
            currentModel = root;

            GltfImport importer = null;
            try
            {
                Task<bool> loadTask;
                try
                {
                    importer = new GltfImport();
                    loadTask = importer.Load(new Uri(Path.GetFullPath(path)).AbsoluteUri);
                }
                catch (Exception exception)
                {
                    FailModelLoad(code, path, root, importer, exception);
                    importer = null;
                    yield break;
                }

                while (!loadTask.IsCompleted)
                    yield return null;

                if (currentCode != code || loadTask.IsFaulted || loadTask.IsCanceled || !loadTask.Result)
                {
                    FailModelLoad(code, path, root, importer, loadTask.Exception);
                    importer = null;
                    yield break;
                }

                Task<bool> instantiateTask;
                try
                {
                    instantiateTask = importer.InstantiateMainSceneAsync(modelRoot.transform);
                }
                catch (Exception exception)
                {
                    FailModelLoad(code, path, root, importer, exception);
                    importer = null;
                    yield break;
                }

                while (!instantiateTask.IsCompleted)
                    yield return null;

                if (currentCode != code || instantiateTask.IsFaulted || instantiateTask.IsCanceled || !instantiateTask.Result)
                {
                    FailModelLoad(code, path, root, importer, instantiateTask.Exception);
                    importer = null;
                    yield break;
                }

                try
                {
                    NormalizeModel(root.transform, modelRoot.transform);
                    SetLayerRecursively(root, gameObject.layer);
                    DisableColliders(root);
                }
                catch (Exception exception)
                {
                    FailModelLoad(code, path, root, importer, exception);
                    importer = null;
                    yield break;
                }

                root.AddComponent<GltfImportHolder>().SetImporter(importer);
                importer = null;
                currentModel.SetActive(true);
                ModelReady?.Invoke(code);
            }
            finally
            {
                importer?.Dispose();
            }

            loadCoroutine = null;
        }

        private void FailModelLoad(int code, string path, GameObject root, GltfImport importer, Exception exception)
        {
            MarkModelPathFailed(path);
            if (exception != null)
                Debug.LogWarning("Field monster GLB load failed: " + path + " " + exception);
            else
                Debug.LogWarning("Field monster GLB load failed: " + path);

            if (root != null)
                Destroy(root);
            if (currentModel == root)
                currentModel = null;
            currentCode = 0;
            loadCoroutine = null;
            importer?.Dispose();
            ModelUnavailable?.Invoke(code);
        }

        private static string FindUsableModelPath(int code)
        {
            var path = FindModelPath(code);
            if (path == null)
                return null;

            return FailedModelPaths.Contains(NormalizeModelPath(path)) ? null : path;
        }

        private static void MarkModelPathFailed(string path)
        {
            if (!string.IsNullOrEmpty(path))
                FailedModelPaths.Add(NormalizeModelPath(path));
        }

        private static string NormalizeModelPath(string path)
        {
            return Path.GetFullPath(path);
        }

        private static string FindModelPathInFolder(string folder, int code)
        {
            if (!Directory.Exists(folder))
                return null;

            var exactPath = Path.Combine(folder, code + GlbExtension);
            if (File.Exists(exactPath))
                return exactPath;

            return Directory.GetFiles(folder, code + "_*" + GlbExtension).FirstOrDefault();
        }

        private static void NormalizeModel(Transform root, Transform modelRoot)
        {
            modelRoot.localPosition = Vector3.zero;
            modelRoot.localRotation = Quaternion.identity;
            modelRoot.localScale = Vector3.one;

            if (!TryGetBoundsInSpace(root, modelRoot, out var bounds))
                return;

            var height = Mathf.Max(bounds.size.y, 0.001f);
            modelRoot.localScale = Vector3.one * (TargetHeight / height);

            if (TryGetBoundsInSpace(root, modelRoot, out bounds))
                modelRoot.localPosition -= new Vector3(bounds.center.x, bounds.min.y, bounds.center.z);
        }

        private static bool TryGetBoundsInSpace(Transform space, Transform content, out Bounds bounds)
        {
            var renderers = content.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0)
            {
                bounds = default;
                return false;
            }

            var initialized = false;
            bounds = default;
            var matrix = space.worldToLocalMatrix;

            foreach (var renderer in renderers)
            {
                var rendererBounds = renderer.bounds;
                var min = rendererBounds.min;
                var max = rendererBounds.max;

                EncapsulatePoint(ref bounds, ref initialized, matrix.MultiplyPoint3x4(new Vector3(min.x, min.y, min.z)));
                EncapsulatePoint(ref bounds, ref initialized, matrix.MultiplyPoint3x4(new Vector3(min.x, min.y, max.z)));
                EncapsulatePoint(ref bounds, ref initialized, matrix.MultiplyPoint3x4(new Vector3(min.x, max.y, min.z)));
                EncapsulatePoint(ref bounds, ref initialized, matrix.MultiplyPoint3x4(new Vector3(min.x, max.y, max.z)));
                EncapsulatePoint(ref bounds, ref initialized, matrix.MultiplyPoint3x4(new Vector3(max.x, min.y, min.z)));
                EncapsulatePoint(ref bounds, ref initialized, matrix.MultiplyPoint3x4(new Vector3(max.x, min.y, max.z)));
                EncapsulatePoint(ref bounds, ref initialized, matrix.MultiplyPoint3x4(new Vector3(max.x, max.y, min.z)));
                EncapsulatePoint(ref bounds, ref initialized, matrix.MultiplyPoint3x4(new Vector3(max.x, max.y, max.z)));
            }

            return initialized;
        }

        private static void EncapsulatePoint(ref Bounds bounds, ref bool initialized, Vector3 point)
        {
            if (!initialized)
            {
                bounds = new Bounds(point, Vector3.zero);
                initialized = true;
            }
            else
            {
                bounds.Encapsulate(point);
            }
        }

        private static void DisableColliders(GameObject root)
        {
            foreach (var collider in root.GetComponentsInChildren<Collider>(true))
                collider.enabled = false;
        }

        private static void SetLayerRecursively(GameObject root, int layer)
        {
            if (root == null)
                return;

            root.layer = layer;
            var transform = root.transform;
            for (var i = 0; i < transform.childCount; i += 1)
                SetLayerRecursively(transform.GetChild(i).gameObject, layer);
        }

        private void OnDestroy()
        {
            Hide();
        }

        private sealed class GltfImportHolder : MonoBehaviour
        {
            private object importer;

            public void SetImporter(object value)
            {
                importer = value;
            }

            private void OnDestroy()
            {
                if (importer is IDisposable disposable)
                    disposable.Dispose();
            }
        }
    }
}
