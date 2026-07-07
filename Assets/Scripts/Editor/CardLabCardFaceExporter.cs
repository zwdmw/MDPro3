#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using MDPro3;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class CardLabCardFaceExporter
{
    private static readonly List<int> Codes = new();
    private static string outputDir;
    private static string manifestPath;

    public static void Export()
    {
        var outputs = new List<Dictionary<string, object>>();
        try
        {
            ParseArgs();
            if (Codes.Count == 0)
                throw new ArgumentException("Missing -cardlab-card-ids");
            if (string.IsNullOrEmpty(outputDir))
                throw new ArgumentException("Missing -cardlab-output-dir");

            Directory.CreateDirectory(outputDir);
            if (!string.IsNullOrEmpty(manifestPath))
                Directory.CreateDirectory(Path.GetDirectoryName(manifestPath));

            EditorSceneManager.OpenScene("Assets/Main.unity");
            var program = UnityEngine.Object.FindObjectOfType<Program>();
            if (program == null)
                throw new InvalidOperationException("Program object was not found in Assets/Main.unity");
            Program.instance = program;

            var container = AssetDatabase.LoadAssetAtPath<TextureContainer>("Assets/ScriptableObjects/TextureContainer.asset");
            if (container == null)
                container = AssetDatabase.LoadAssetAtPath<TextureContainer>("Assets/Resources/AddressableAliases/TextureContainer.asset");
            if (container == null)
                throw new InvalidOperationException("TextureContainer asset was not found");

            CardLabRuntimeBridge.Initialize(container);

            foreach (var code in Codes.Distinct())
            {
                var art = LoadArt(code);
                if (art == null)
                    throw new InvalidOperationException($"Artwork was not found for {code}");

                var tex = CardLabRuntimeBridge.RenderCardFace(code, art);
                if (tex == null)
                    throw new InvalidOperationException($"MDPro3 CardRenderer failed for {code}");

                var size = Settings.Data.SavedCardSize;
                if (size != null && size.Length > 1 && size[0] > 0 && size[1] > 0 && (size[0] != tex.width || size[1] != tex.height))
                    tex = TextureManager.ResizeTexture2D(tex, size[0], size[1]);

                var path = Path.Combine(outputDir, code + ".png");
                File.WriteAllBytes(path, tex.EncodeToPNG());
                outputs.Add(new Dictionary<string, object>
                {
                    ["card_id"] = code,
                    ["path"] = path,
                    ["width"] = tex.width,
                    ["height"] = tex.height,
                    ["found_art"] = true,
                    ["render_succeed"] = true
                });
            }

            WriteManifest(true, string.Empty, outputs);
            EditorApplication.Exit(0);
        }
        catch (Exception ex)
        {
            WriteManifest(false, ex.ToString(), outputs);
            EditorApplication.Exit(1);
        }
    }

    private static void ParseArgs()
    {
        var args = Environment.GetCommandLineArgs();
        for (var i = 0; i < args.Length; i++)
        {
            var key = args[i];
            if (key == "-cardlab-card-ids" && i + 1 < args.Length)
            {
                foreach (var raw in args[++i].Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries))
                    if (int.TryParse(raw.Trim(), out var code))
                        Codes.Add(code);
            }
            else if (key == "-cardlab-output-dir" && i + 1 < args.Length)
            {
                outputDir = args[++i];
            }
            else if (key == "-cardlab-manifest" && i + 1 < args.Length)
            {
                manifestPath = args[++i];
            }
        }
    }

    private static Texture2D LoadArt(int code)
    {
        foreach (var folder in new[] { Program.altArtPath, Program.artPath, "Expansions/art/" })
        {
            foreach (var extension in new[] { Program.pngExpansion, Program.jpgExpansion, ".jpeg" })
            {
                var path = Path.Combine(folder, code + extension);
                if (!File.Exists(path))
                    continue;

                var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                if (tex.LoadImage(File.ReadAllBytes(path)))
                    return tex;
            }
        }
        return null;
    }

    private static void WriteManifest(bool ok, string error, List<Dictionary<string, object>> outputs)
    {
        if (string.IsNullOrEmpty(manifestPath))
            return;

        var outputJson = string.Join(",\n", outputs.Select(item =>
            "{" +
            $"\"card_id\":{item["card_id"]}," +
            $"\"path\":\"{EscapeJson((string)item["path"])}\"," +
            $"\"width\":{item["width"]}," +
            $"\"height\":{item["height"]}," +
            $"\"found_art\":{item["found_art"].ToString().ToLowerInvariant()}," +
            $"\"render_succeed\":{item["render_succeed"].ToString().ToLowerInvariant()}" +
            "}"));

        var json =
            "{" +
            $"\"ok\":{ok.ToString().ToLowerInvariant()}," +
            $"\"error\":\"{EscapeJson(error ?? string.Empty)}\"," +
            $"\"outputs\":[{outputJson}]" +
            "}";
        File.WriteAllText(manifestPath, json);
    }

    private static string EscapeJson(string value)
    {
        return value
            .Replace("\\", "\\\\")
            .Replace("\"", "\\\"")
            .Replace("\r", "\\r")
            .Replace("\n", "\\n");
    }
}
#endif
