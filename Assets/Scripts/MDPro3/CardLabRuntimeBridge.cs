using System.IO;
using MDPro3.YGOSharp;
using UnityEngine;

namespace MDPro3
{
    public static class CardLabRuntimeBridge
    {
        public static void Initialize(TextureContainer textureContainer)
        {
            if (!Directory.Exists(Program.dataPath))
                Directory.CreateDirectory(Program.dataPath);

            Config.Initialize(Program.configPath);
            ZipHelper.Initialize();
            TextureManager.container = textureContainer;
            InterString.Initialize();
            StringHelper.Initialize();
            CardsManager.Initialize();
        }

        public static Texture2D RenderCardFace(int code, Texture2D art)
        {
            if (Program.instance == null || Program.instance.cardRenderer == null)
                return null;

            var renderer = Program.instance.cardRenderer;
            var renderTexture = renderer.renderTexture;
            if (renderTexture == null)
                return null;

            if (Program.instance.camera_ != null && Program.instance.camera_.cameraRenderTexture != null)
                Program.instance.camera_.cameraRenderTexture.targetTexture = renderTexture;

            RenderTexture.active = renderTexture;
            if (!renderer.RenderCard(code, art))
                return null;

            Canvas.ForceUpdateCanvases();
            if (Program.instance.camera_ != null && Program.instance.camera_.cameraRenderTexture != null)
                Program.instance.camera_.cameraRenderTexture.Render();

            RenderTexture.active = renderTexture;
            var output = new Texture2D(renderTexture.width, renderTexture.height, TextureFormat.RGB24, true);
            output.ReadPixels(new Rect(0, 0, renderTexture.width, renderTexture.height), 0, 0);
            output.Apply();
            output.name = "CardLab_" + code;
            return output;
        }
    }
}
