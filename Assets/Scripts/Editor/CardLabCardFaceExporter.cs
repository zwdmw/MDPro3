#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using MDPro3;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class CardLabCardFaceExporter
{
    private static readonly List<int> Codes = new();
    private static string outputDir;
    private static string manifestPath;
    private static double deadline;
    private static bool started;
    private static bool exiting;

    public static void Export()
    {
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

            deadline = EditorApplication.timeSinceStartup + 180.0;
            EditorSceneManager.OpenScene("Assets/Main.unity");
            EditorApplication.update += Update;
            EditorApplication.EnterPlaymode();
        }
        catch (Exception ex)
        {
            WriteManifest(false, ex.ToString(), new List<Dictionary<string, object>>());
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

    private static void Update()
    {
        if (exiting)
            return;

        if (EditorApplication.timeSinceStartup > deadline)
        {
            Finish(1, "Timed out waiting for MDPro3 renderer", new List<Dictionary<string, object>>());
            return;
        }

        if (!EditorApplication.isPlaying || started)
            return;

        if (Program.instance == null || Program.instance.cardRenderer == null || TextureManager.container == null)
            return;

        started = true;
        _ = ExportAsync();
    }

    private static async Task ExportAsync()
    {
        var outputs = new List<Dictionary<string, object>>();
        try
        {
            await Task.Delay(500);
            foreach (var code in Codes.Distinct())
            {
                var tex = await TextureManager.LoadCardAsync(code, false);
                if (tex == null)
                    throw new InvalidOperationException($"MDPro3 renderer returned null texture for {code}");

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
                    ["found_art"] = TextureManager.lastCardFoundArt,
                    ["render_succeed"] = TextureManager.lastCardRenderSucceed
                });
            }
            Finish(0, string.Empty, outputs);
        }
        catch (Exception ex)
        {
            Finish(1, ex.ToString(), outputs);
        }
    }

    private static void Finish(int exitCode, string error, List<Dictionary<string, object>> outputs)
    {
        if (exiting)
            return;
        exiting = true;
        EditorApplication.update -= Update;
        WriteManifest(exitCode == 0, error, outputs);
        EditorApplication.Exit(exitCode);
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
