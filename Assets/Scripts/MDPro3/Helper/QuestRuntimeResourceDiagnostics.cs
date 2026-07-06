using System;
using System.IO;
using System.Text;
using UnityEngine;

namespace MDPro3
{
    public static class QuestRuntimeResourceDiagnostics
    {
        private const string ReportPath = "Data/quest_resource_report.txt";

        public static void LogStartupState()
        {
#if !UNITY_EDITOR && UNITY_ANDROID
            var report = BuildReport();
            Debug.Log(report);

            try
            {
                Directory.CreateDirectory(Program.dataPath);
                File.WriteAllText(ReportPath, report, Encoding.UTF8);
            }
            catch (Exception ex)
            {
                Debug.LogWarning("Quest resource diagnostic report write failed: " + ex.Message);
            }
#endif
        }

        private static string BuildReport()
        {
            var sb = new StringBuilder(2048);
            sb.AppendLine("Quest runtime resource check");
            sb.AppendLine("version=" + Application.version);
            sb.AppendLine("buildGuid=" + Application.buildGUID);
            sb.AppendLine("persistentDataPath=" + Application.persistentDataPath);
            sb.AppendLine("cwd=" + Directory.GetCurrentDirectory());
            AppendFile(sb, "Data/items.txt");
            AppendFile(sb, "Data/lflist.conf");
            AppendFile(sb, "Data/cards_Lite.json");
            AppendFile(sb, "Data/script.zip");
            AppendDirectory(sb, "Data/Windbot");
            AppendDirectory(sb, "Data/Windbot/Decks");
            AppendDirectory(sb, "Data/Windbot/Dialogs");
            AppendDirectory(sb, "Android/MasterDuel");
            AppendDirectory(sb, "Android/MasterDuel/Wallpaper/front0001");
            AppendDirectory(sb, "Android/MasterDuel/Material");
            AppendDirectory(sb, "Android/MasterDuel/Effects");
            AppendDirectory(sb, Program.artPath);
            AppendDirectory(sb, Program.altArtPath);
            AppendDirectory(sb, Program.closeupPath);
            AppendDirectory(sb, "Picture/Art3D");
            AppendDirectory(sb, Program.deckPath);
            AppendDirectory(sb, Program.puzzlePath);
            return sb.ToString();
        }

        private static void AppendFile(StringBuilder sb, string path)
        {
            try
            {
                var exists = File.Exists(path);
                var length = exists ? new FileInfo(path).Length : 0L;
                sb.Append("file ");
                sb.Append(path);
                sb.Append(" exists=");
                sb.Append(exists);
                sb.Append(" bytes=");
                sb.AppendLine(length.ToString());
            }
            catch (Exception ex)
            {
                sb.AppendLine("file " + path + " error=" + ex.Message);
            }
        }

        private static void AppendDirectory(StringBuilder sb, string path)
        {
            try
            {
                var exists = Directory.Exists(path);
                var count = exists ? CountFiles(path, 10000) : 0;
                sb.Append("dir ");
                sb.Append(path);
                sb.Append(" exists=");
                sb.Append(exists);
                sb.Append(" files=");
                sb.AppendLine(count.ToString());
            }
            catch (Exception ex)
            {
                sb.AppendLine("dir " + path + " error=" + ex.Message);
            }
        }

        private static int CountFiles(string path, int limit)
        {
            var count = 0;
            foreach (var _ in Directory.EnumerateFiles(path, "*", SearchOption.AllDirectories))
            {
                count++;
                if (count >= limit)
                    break;
            }
            return count;
        }
    }
}
