using System;
using System.Collections.Generic;
using System.IO;
using Ionic.Zip;

namespace MDPro3
{
    public class ZipHelper
    {
        public static List<ZipFile> zips = new List<ZipFile>();

        public static void Initialize()
        {
            zips.Clear();

            if (!Directory.Exists("Expansions"))
                Directory.CreateDirectory("Expansions");
            foreach (var zip in GetExpansionFiles("*.ypk"))
                zips.Add(new ZipFile(zip));
            foreach (var zip in GetExpansionFiles("*.zip"))
                zips.Add(new ZipFile(zip));

            zips.Add(new ZipFile("Data/script.zip"));//Make "Data/script.zip" the last one to read.
        }

        public static string[] GetExpansionDatabaseFiles()
        {
            var files = GetExpansionFiles("*.cdb");
            Array.Sort(files, CompareExpansionDatabases);
            return files;
        }

        public static string[] GetExpansionFiles(string pattern)
        {
            if (!Directory.Exists("Expansions"))
                return new string[0];

            var files = Directory.GetFiles("Expansions", pattern);
            Array.Sort(files, StringComparer.OrdinalIgnoreCase);
            return files;
        }

        private static int CompareExpansionDatabases(string left, string right)
        {
            var priority = GetExpansionDatabasePriority(left).CompareTo(GetExpansionDatabasePriority(right));
            if (priority != 0)
                return priority;

            var time = File.GetLastWriteTimeUtc(left).CompareTo(File.GetLastWriteTimeUtc(right));
            if (time != 0)
                return time;

            return string.Compare(left, right, StringComparison.OrdinalIgnoreCase);
        }

        private static int GetExpansionDatabasePriority(string path)
        {
            var name = Path.GetFileNameWithoutExtension(path).ToLowerInvariant();
            if (name == "cards")
                return 0;
            if (name == "ifanime")
                return 100;
            if (name.StartsWith("ifzcg", StringComparison.OrdinalIgnoreCase))
                return 300 + GetTrailingNumber(name);

            return 200;
        }

        private static int GetTrailingNumber(string text)
        {
            var multiplier = 1;
            var value = 0;
            var found = false;
            for (var i = text.Length - 1; i >= 0; i--)
            {
                var c = text[i];
                if (c < '0' || c > '9')
                    break;

                found = true;
                value += (c - '0') * multiplier;
                multiplier *= 10;
            }

            return found ? value : 0;
        }
        public static void Dispose()
        {
            foreach (var zip in zips)
                zip.Dispose();
        }

        public static List<string> GetAllCdbTempPath()
        {
            var returnValue = new List<string>();
            foreach (var zip in zips)
            {
                if (zip.Name.ToLower().EndsWith("script.zip"))
                    continue;
                foreach (var file in zip.EntryFileNames)
                {
                    if (file.ToLower().EndsWith(".cdb"))
                    {
                        var e = zip[file];
                        if (!Directory.Exists(Program.tempFolder))
                            Directory.CreateDirectory(Program.tempFolder);
                        var tempFile = Path.Combine(Path.GetFullPath(Program.tempFolder), file);
                        e.Extract(Path.GetFullPath(Program.tempFolder), ExtractExistingFileAction.OverwriteSilently);
                        returnValue.Add(tempFile);
                    }
                }
            }
            return returnValue;
        }
    }
}
