using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using GLTFast;
using UnityEngine;

namespace MDPro3
{
    public static class CustomGlbMateLoader
    {
        public const string ModelFolder = "CustomModels";
        public const int DefaultSampleCode = 900000001;

        public readonly struct ModelInfo
        {
            public readonly int code;
            public readonly string name;
            public readonly string path;

            public ModelInfo(int code, string name, string path)
            {
                this.code = code;
                this.name = name;
                this.path = path;
            }
        }

        public static IEnumerable<ModelInfo> EnumerateModels()
        {
            if (!Directory.Exists(ModelFolder))
                yield break;

            foreach (var path in Directory.GetFiles(ModelFolder, "*.glb"))
            {
                var name = Path.GetFileNameWithoutExtension(path);
                var firstPart = name.Split('_').FirstOrDefault();
                var code = int.TryParse(firstPart, out var parsedCode) ? parsedCode : DefaultSampleCode;
                yield return new ModelInfo(code, name, path);
            }
        }

        public static bool HasModel(int code)
        {
            return FindModelPath(code) != null;
        }

        public static string FindModelPath(int code)
        {
            if (!Directory.Exists(ModelFolder))
                return null;

            var exactPath = Path.Combine(ModelFolder, code + ".glb");
            if (File.Exists(exactPath))
                return exactPath;

            return Directory.GetFiles(ModelFolder, code + "_*.glb").FirstOrDefault();
        }

        public static IEnumerator<Mate> LoadMateAsync(int code)
        {
            var path = FindModelPath(code);
            if (path == null)
                yield break;

            var mateRoot = new GameObject("CustomGlbMate_" + code);
            var modelRoot = new GameObject("Model");
            modelRoot.transform.SetParent(mateRoot.transform, false);

            var importer = new GltfImport();
            var uri = new Uri(Path.GetFullPath(path)).AbsoluteUri;
            var loadTask = importer.Load(uri);
            while (!loadTask.IsCompleted)
                yield return null;

            if (!loadTask.Result)
            {
                UnityEngine.Object.Destroy(mateRoot);
                yield break;
            }

            var instantiateTask = importer.InstantiateMainSceneAsync(modelRoot.transform);
            while (!instantiateTask.IsCompleted)
                yield return null;

            if (!instantiateTask.Result)
            {
                UnityEngine.Object.Destroy(mateRoot);
                yield break;
            }

            mateRoot.AddComponent<GltfImportHolder>().SetImporter(importer);
            NormalizeModel(modelRoot.transform);

            var mate = mateRoot.AddComponent<Mate>();
            mate.type = Mate.MateType.MasterDuel;
            mate.code = code;
            yield return mate;
        }

        private static void NormalizeModel(Transform modelRoot)
        {
            if (!TryGetRendererBounds(modelRoot, out var bounds))
                return;

            modelRoot.position = -bounds.center;
            var height = Mathf.Max(bounds.size.y, 0.001f);
            modelRoot.localScale = Vector3.one * (10f / height);

            if (TryGetRendererBounds(modelRoot, out bounds))
                modelRoot.position += Vector3.up * -bounds.min.y;
        }

        private static bool TryGetRendererBounds(Transform root, out Bounds bounds)
        {
            var renderers = root.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0)
            {
                bounds = default;
                return false;
            }

            bounds = renderers[0].bounds;
            for (var i = 1; i < renderers.Length; i++)
                bounds.Encapsulate(renderers[i].bounds);
            return true;
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
