using System;
using System.IO;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;
using MDPro3.YGOSharp;
using MDPro3.YGOSharp.OCGWrapper.Enums;
using MDPro3.Utility;
using System.Collections;
using zFramework.Internal;
using UnityEngine.UI;
using DG.Tweening;

namespace MDPro3
{
    public class TextureLoader : MonoBehaviour
    {
        public static TextureLoader Instance;

        private static readonly ConcurrentDictionary<int, TextureData> _cachedArts = new();
        private static readonly ConcurrentDictionary<int, TextureData> _cachedCards = new();
        private const string importedCardFacePath = "Picture/Card/";

        public const int MAX_LOADING_THREADS = 16;
        private static readonly ConcurrentDictionary<int, Task<Texture2D>> _loadingCoroutines = new();

        private void Awake()
        {
            Instance = this;
        }

        public static async Task<Texture2D> LoadPicFromFileAsync(string path)
        {
            if (!File.Exists(path))
                return null;

            int threadIndex = 0;
            while(!_loadingCoroutines.TryAdd(threadIndex, null))
            {
                threadIndex++;
                if(threadIndex == MAX_LOADING_THREADS)
                {
                    await TaskUtility.WaitOneFrame();
                    threadIndex = 0;
                }
            }

            string fullPath;
#if !UNITY_EDITOR && UNITY_ANDROID
            fullPath = "file://" + Application.persistentDataPath + Program.slash + path;
#else
            fullPath = Environment.CurrentDirectory + Program.slash + path;
#endif
            using var request = UnityWebRequestTexture.GetTexture(fullPath);
            var send = request.SendWebRequest();
            await TaskUtility.WaitUntil(() => send.isDone);
            if (!Application.isPlaying)
                return null;

            _loadingCoroutines.TryRemove(threadIndex, out _);

            if (request.result == UnityWebRequest.Result.Success)
                return DownloadHandlerTexture.GetContent(request);
            else
            {
                Debug.LogWarningFormat("Pic File [{0}] not fount.", path);
                return null;
            }
        }

        public static async Task<Texture2D> LoadArtAsync(int code, bool cache)
        {
            await TaskUtility.WaitWhile(() => TextureManager.container == null);

            if (_cachedArts.TryGetValue(code, out var textureData))
            {
                await TaskUtility.WaitUntil(() => textureData.loaded);
                textureData.AddReference();
                if(cache)
                    textureData.notDelete = cache;
                if (textureData.texture == null)
                    return TextureManager.container.unknownArt.texture;
                else
                    return textureData.texture;
            }

            textureData = new TextureData() 
            {
                texture = null,
                loaded = false,
                notDelete = cache,
                referenceCount = 1,
            };
            _cachedArts.TryAdd(code, textureData);

            if (!Directory.Exists(Program.artPath))
                Directory.CreateDirectory(Program.artPath);
            if (!Directory.Exists(Program.altArtPath))
                Directory.CreateDirectory(Program.altArtPath);

            var path = Program.altArtPath + code;

            if (File.Exists(path + Program.jpgExpansion))
                path += Program.jpgExpansion;
            else if (File.Exists(path + Program.pngExpansion))
                path += Program.pngExpansion;
            else if (File.Exists(Program.artPath + code.ToString() + Program.jpgExpansion))
                path = Program.artPath + code.ToString() + Program.jpgExpansion;
            else if (File.Exists(Program.artPath + code.ToString() + Program.pngExpansion))
                path = Program.artPath + code.ToString() + Program.pngExpansion;
            else if (TryResolveExpansionPicturePath("art", code, out var expansionArtPath))
                path = expansionArtPath;
            else if (TryResolveExpansionPicturePath("pics", code, out var expansionCardPath))
            {
                var expansionCardTask = LoadPicFromFileAsync(expansionCardPath);
                await TaskUtility.WaitUntil(() => expansionCardTask.IsCompleted);
                var expansionArtTask = ConvertCardPictureToArt(code, expansionCardTask.Result);
                await TaskUtility.WaitUntil(() => expansionArtTask.IsCompleted);

                textureData.texture = expansionArtTask.Result;
                textureData.loaded = true;
                if (expansionArtTask.Result != null)
                    return expansionArtTask.Result;
                return TextureManager.container.unknownArt.texture;
            }
            else
            {
                Task<Texture2D> loadTask;
                loadTask = LoadArtFromZipArt(code);
                await TaskUtility.WaitUntil(() => loadTask.IsCompleted);
                if (loadTask.Result == null)
                {
                    loadTask = LoadArtFromZipPics(code);
                    await TaskUtility.WaitUntil(() => loadTask.IsCompleted);
                }

                textureData.texture = loadTask.Result;
                textureData.loaded = true;
                if (loadTask.Result == null)
                    return TextureManager.container.unknownArt.texture;
                else
                    return loadTask.Result;
            }

            var task = LoadPicFromFileAsync(path);
            await TaskUtility.WaitUntil(() => task.IsCompleted);

            textureData.texture = task.Result;
            textureData.loaded = true;
            if (task.Result != null)
                return task.Result;
            else
                return TextureManager.container.unknownArt.texture;
        }

        public static async Task<Texture2D> LoadCardAsync(int code, bool cache = false)
        {
            await TaskUtility.WaitWhile(() => TextureManager.container == null);

            if (_cachedCards.TryGetValue(code, out var textureData))
            {
                await TaskUtility.WaitUntil(() => textureData.loaded);
                textureData.AddReference();
                if (cache)
                    textureData.notDelete = cache;
                if (textureData.texture == null)
                    return TextureManager.container.unknownCard.texture;
                else
                    return textureData.texture;
            }

            textureData = new TextureData()
            {
                texture = null,
                loaded = false,
                notDelete = cache,
                referenceCount = 1,
            };
            _cachedCards.TryAdd(code, textureData);

            var cardFacePath = ResolveImportedCardFacePath(code);
            if (!string.IsNullOrEmpty(cardFacePath))
            {
                var fileTask = LoadPicFromFileAsync(cardFacePath);
                await TaskUtility.WaitUntil(() => fileTask.IsCompleted);
                if (!Application.isPlaying)
                    return null;

                if (fileTask.Result != null)
                {
                    fileTask.Result.name = "ImportedCardFace_" + code;
                    textureData.texture = fileTask.Result;
                    textureData.loaded = true;
                    return fileTask.Result;
                }
            }

            var data = CardsManager.Get(code, true);
            if (data.Id == 0)
            {
                textureData.texture = TextureManager.container.unknownCard.texture;
                textureData.loaded = true;
                return TextureManager.container.unknownCard.texture;
            }

            var task = LoadArtAsync(code, false);
            await TaskUtility.WaitUntil(() => task.IsCompleted);

            if (!Program.instance.cardRenderer.RenderCard(code, task.Result))
            {
                textureData.texture = TextureManager.container.unknownCard.texture;
                textureData.loaded = true;
                return TextureManager.container.unknownCard.texture;
            }
            var returnValue = new Texture2D(RenderTexture.active.width, RenderTexture.active.height, TextureFormat.RGB24, true);
            returnValue.ReadPixels(new Rect(0, 0, RenderTexture.active.width, RenderTexture.active.height), 0, 0);
            returnValue.Apply();
            returnValue.name = "Card_" + code;
            await TaskUtility.WaitOneFrame();

            textureData.texture = returnValue;
            textureData.loaded = true;

            DeleteArt(code);
            return returnValue;
        }

        private static string ResolveImportedCardFacePath(int code)
        {
            foreach (var folder in new[] { importedCardFacePath, Program.cardPicPath })
            {
                var jpgPath = folder + code + Program.jpgExpansion;
                if (File.Exists(jpgPath))
                    return jpgPath;

                var pngPath = folder + code + Program.pngExpansion;
                if (File.Exists(pngPath))
                    return pngPath;
            }

            return null;
        }

        private static void DeleteArt(int code)
        {
            if(_cachedArts.TryGetValue(code, out var art))
                if (art.Delete())
                {
                    _cachedArts.TryRemove(code, out _);
                    DestroyImmediate(art.texture);
                }
        }

        public static void DeleteCard(int code)
        {
            if (_cachedCards.TryGetValue(code, out var card))
                if (card.Delete())
                {
                    _cachedCards.TryRemove(code, out _);
                    DestroyImmediate(card.texture);
                }
        }

        public static void DeleteCache()
        {
            foreach(var art in  _cachedArts.Values)
                Destroy(art.texture);
            foreach (var card in _cachedCards.Values)
                Destroy(card.texture);
            _cachedArts.Clear();
            _cachedCards.Clear();
        }

        private static async Task<Texture2D> LoadArtFromZipArt(int code)
        {
            Texture2D returnValue = new Texture2D(0, 0);
            foreach (var zip in ZipHelper.zips)
            {
                if (zip.Name.ToLower().EndsWith("script.zip"))
                    continue;
                foreach (var file in zip.EntryFileNames)
                {
                    foreach (var extName in new[] { Program.pngExpansion, Program.jpgExpansion })
                    {
                        var picPath = $"art/{code}{extName}";
                        if (file.ToLower() == picPath)
                        {
                            MemoryStream stream = new();
                            var entry = zip[picPath];
                            entry.Extract(stream);
                            await TaskUtility.WaitOneFrame();
                            returnValue.LoadImage(stream.ToArray());
                            return returnValue;
                        }
                    }
                }
            }
            return null;
        }

        private static bool TryResolveExpansionPicturePath(string folderName, int code, out string path)
        {
            path = null;
            if (string.IsNullOrEmpty(folderName))
                return false;

            var baseFolder = Path.Combine(Program.expansionsPath, folderName).Replace('\\', '/').TrimEnd('/') + "/";
            foreach (var extension in new[] { Program.jpgExpansion, Program.pngExpansion, ".jpeg" })
            {
                var candidate = baseFolder + code + extension;
                if (File.Exists(candidate))
                {
                    path = candidate;
                    return true;
                }
            }

            return false;
        }

        private static async Task<Texture2D> ConvertCardPictureToArt(int code, Texture2D texture)
        {
            if (texture == null)
                return null;

            var data = CardsManager.Get(code);
            Task<Texture2D> task;
            if (code >= 120000000 && code < 130000000)
            {
                if (data.HasType(CardType.Monster))
                    task = GetArtFromRushDuelMonsterCard(texture);
                else
                    task = GetArtFromRushDuelSpellCard(texture);
            }
            else if (data.HasType(CardType.Pendulum))
                task = GetArtFromPendulumCard(texture);
            else
                task = GetArtFromCard(texture);

            await TaskUtility.WaitUntil(() => task.IsCompleted);
            return task.Result;
        }

        private static async Task<Texture2D> LoadArtFromZipPics(int code)
        {
            Texture2D returnValue = new Texture2D(0, 0);
            foreach (var zip in ZipHelper.zips)
            {
                if (zip.Name.ToLower().EndsWith("script.zip"))
                    continue;
                foreach (var file in zip.EntryFileNames)
                {
                    foreach (var extName in new[] { Program.pngExpansion, Program.jpgExpansion })
                    {
                        var picPath = $"pics/{code}{extName}";
                        if (file.ToLower() == picPath)
                        {
                            var data = CardsManager.Get(code);
                            MemoryStream stream = new MemoryStream();
                            var entry = zip[picPath];
                            entry.Extract(stream);
                            await TaskUtility.WaitOneFrame();

                            returnValue.LoadImage(stream.ToArray());
                            var task = ConvertCardPictureToArt(code, returnValue);
                            await TaskUtility.WaitUntil(() => task.IsCompleted);
                            return task.Result;
                        }
                    }
                }
            }
            return null;
        }

        public static IEnumerator LoadCardToRawImage(int code, RawImage rawImage)
        {
            if (TextureManager.ShouldUsePlainCardUiTextures())
            {
                if (rawImage != null)
                {
                    rawImage.material = null;
                    rawImage.texture = TextureManager.container.GetCardUnloadTexture(CardsManager.Get(code));
                }

                var plainTask = LoadCardAsync(code, false);
                while (!plainTask.IsCompleted)
                    yield return null;

                TextureManager.ApplyCardTextureToRawImage(rawImage, plainTask.Result);
                yield break;
            }

            rawImage.material = TextureManager.GetCardMaterial(code);
            rawImage.material
                .SetTexture("_LoadingTex", TextureManager.container
                .GetCardUnloadTexture(CardsManager.Get(code)));
            rawImage.material.SetFloat("_LoadingBlend", 1f);

            var task = LoadCardAsync(code, false);
            while(!task.IsCompleted)
                yield return null;

            if(rawImage != null)
            {
                rawImage.texture = task.Result;
                rawImage.material.DOFloat(0f, "_LoadingBlend", 0.1f);
            }
        }

        #region Crop Texture
        private static async Task<Texture2D> GetArtFromCard(Texture2D cardPic)
        {
            var startX = Mathf.CeilToInt(cardPic.width * 0.13f);
            var startY = Mathf.CeilToInt(cardPic.height * 0.3f);
            var width = Mathf.CeilToInt(cardPic.width * 0.87f);
            var height = Mathf.CeilToInt(cardPic.height * 0.81f);
            var task = GetCroppingTex(cardPic, startX, startY, width, height);
            while (!task.IsCompleted)
                await TaskUtility.WaitOneFrame();
            return task.Result;
        }
        private static async Task<Texture2D> GetArtFromPendulumCard(Texture2D cardPic)
        {
            var startX = Mathf.CeilToInt(cardPic.width * 0.067f);
            var startY = Mathf.CeilToInt(cardPic.height * 0.38f);
            var width = Mathf.CeilToInt(cardPic.width * 0.933f);
            var height = Mathf.CeilToInt(cardPic.height * 0.81f);
            var task = GetCroppingTex(cardPic, startX, startY, width, height);
            while (!task.IsCompleted)
                await TaskUtility.WaitOneFrame();
            return task.Result;
        }
        private static async Task<Texture2D> GetArtFromRushDuelMonsterCard(Texture2D cardPic)
        {
            var startX = Mathf.CeilToInt(cardPic.width * 0.067f);
            var startY = Mathf.CeilToInt(cardPic.height * 0.29f);
            var width = Mathf.CeilToInt(cardPic.width * 0.933f);
            var height = Mathf.CeilToInt(cardPic.height * 0.90f);
            var task = GetCroppingTex(cardPic, startX, startY, width, height);
            while (!task.IsCompleted)
                await TaskUtility.WaitOneFrame();
            return task.Result;
        }
        private static async Task<Texture2D> GetArtFromRushDuelSpellCard(Texture2D cardPic)
        {
            var startX = Mathf.CeilToInt(cardPic.width * 0.067f);
            var startY = Mathf.CeilToInt(cardPic.height * 0.29f);
            var width = Mathf.CeilToInt(cardPic.width * 0.933f);
            var height = Mathf.CeilToInt(cardPic.height * 0.90f);
            var task = GetCroppingTex(cardPic, startX, startY, width, height);
            while (!task.IsCompleted)
                await TaskUtility.WaitOneFrame();
            return task.Result;
        }
        private static async Task<Texture2D> GetCroppingTex(Texture2D texture, int startX, int startY, int width, int height)
        {
            var returnValue = new Texture2D(width - startX, height - startY);
            var pix = new Color[returnValue.width * returnValue.height];
            var index = 0;
            for (var y = startY; y < height; y++)
                for (var x = startX; x < width; x++)
                    pix[index++] = texture.GetPixel(x, y);

            await TaskUtility.WaitOneFrame();
            returnValue.SetPixels(pix);
            await TaskUtility.WaitOneFrame();
            returnValue.Apply();
            return returnValue;
        }
        #endregion

    }
}
