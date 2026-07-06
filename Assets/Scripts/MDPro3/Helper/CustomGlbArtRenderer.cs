using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using GLTFast;
using MDPro3.Utility;
using UnityEngine;

namespace MDPro3
{
    public static class CustomGlbArtRenderer
    {
        public const string ModelFolder = "Picture/Art3D";
        public const string LegacyModelFolder = "Picture";

        private const int TextureSize = 512;
        private const string GlbExtension = ".glb";
        private static readonly Vector3 RenderOrigin = new Vector3(10000f, 10000f, 10000f);

        public static bool HasModel(int code)
        {
            return FindModelPath(code) != null;
        }

        public static string FindModelPath(int code)
        {
            var artModel = FindModelPathInFolder(ModelFolder, code);
            if (artModel != null)
                return artModel;

            artModel = FindModelPathInFolder(LegacyModelFolder, code);
            if (artModel != null)
                return artModel;

            return FindModelPathInFolder(CustomGlbMateLoader.ModelFolder, code);
        }

        public static async Task<Texture2D> RenderArtAsync(int code)
        {
            var path = FindModelPath(code);
            if (path == null)
                return null;

            var root = new GameObject("CustomGlbArt_" + code);
            var modelRoot = new GameObject("Model");
            root.hideFlags = HideFlags.HideAndDontSave;
            modelRoot.hideFlags = HideFlags.HideAndDontSave;
            root.transform.position = RenderOrigin;
            modelRoot.transform.SetParent(root.transform, false);

            var importer = new GltfImport();
            try
            {
                var uri = new Uri(Path.GetFullPath(path)).AbsoluteUri;
                var loadTask = importer.Load(uri);
                await TaskUtility.WaitUntil(() => loadTask.IsCompleted);
                if (!Application.isPlaying || loadTask.IsFaulted || !loadTask.Result)
                    return null;

                var instantiateTask = importer.InstantiateMainSceneAsync(modelRoot.transform);
                await TaskUtility.WaitUntil(() => instantiateTask.IsCompleted);
                if (!Application.isPlaying || instantiateTask.IsFaulted || !instantiateTask.Result)
                    return null;

                NormalizeModel(modelRoot.transform, RenderOrigin);
                var texture = RenderModel();
                texture.name = "Art3D_" + code;
                return texture;
            }
            catch (Exception exception)
            {
                Debug.LogWarningFormat("Failed to render GLB art [{0}]: {1}", path, exception);
                return null;
            }
            finally
            {
                importer.Dispose();
                DestroyObject(root);
            }
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

        private static Texture2D RenderModel()
        {
            var renderTexture = new RenderTexture(TextureSize, TextureSize, 24, RenderTextureFormat.ARGB32);
            var previousActive = RenderTexture.active;

            var cameraObject = new GameObject("CustomGlbArtCamera");
            var keyLightObject = new GameObject("CustomGlbArtKeyLight");
            var fillLightObject = new GameObject("CustomGlbArtFillLight");
            cameraObject.hideFlags = HideFlags.HideAndDontSave;
            keyLightObject.hideFlags = HideFlags.HideAndDontSave;
            fillLightObject.hideFlags = HideFlags.HideAndDontSave;

            try
            {
                var camera = cameraObject.AddComponent<Camera>();
                camera.transform.position = RenderOrigin + new Vector3(0f, 0.15f, -6f);
                camera.transform.LookAt(RenderOrigin + new Vector3(0f, 0.15f, 0f));
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = Color.clear;
                camera.orthographic = true;
                camera.orthographicSize = 1.85f;
                camera.nearClipPlane = 0.01f;
                camera.farClipPlane = 20f;
                camera.targetTexture = renderTexture;

                var keyLight = keyLightObject.AddComponent<Light>();
                keyLight.type = LightType.Directional;
                keyLight.intensity = 1.35f;
                keyLight.transform.rotation = Quaternion.Euler(35f, -35f, 0f);

                var fillLight = fillLightObject.AddComponent<Light>();
                fillLight.type = LightType.Directional;
                fillLight.intensity = 0.45f;
                fillLight.transform.rotation = Quaternion.Euler(20f, 140f, 0f);

                camera.Render();
                RenderTexture.active = renderTexture;
                var texture = new Texture2D(TextureSize, TextureSize, TextureFormat.RGBA32, false);
                texture.ReadPixels(new Rect(0, 0, TextureSize, TextureSize), 0, 0);
                texture.Apply();
                return texture;
            }
            finally
            {
                RenderTexture.active = previousActive;
                renderTexture.Release();
                DestroyObject(renderTexture);
                DestroyObject(cameraObject);
                DestroyObject(keyLightObject);
                DestroyObject(fillLightObject);
            }
        }

        private static void NormalizeModel(Transform modelRoot, Vector3 origin)
        {
            if (!TryGetRendererBounds(modelRoot, out var bounds))
                return;

            var maxSize = Mathf.Max(bounds.size.x, bounds.size.y, bounds.size.z, 0.001f);
            modelRoot.localScale = Vector3.one * (3.2f / maxSize);

            if (TryGetRendererBounds(modelRoot, out bounds))
                modelRoot.position += origin - bounds.center;
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

        private static void DestroyObject(UnityEngine.Object value)
        {
            if (value == null)
                return;

            if (Application.isPlaying)
                UnityEngine.Object.Destroy(value);
            else
                UnityEngine.Object.DestroyImmediate(value);
        }
    }
}
