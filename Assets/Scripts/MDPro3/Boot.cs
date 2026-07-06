using System;
using System.IO;
using UnityEngine;
using ICSharpCode.SharpZipLib.Core;
using ICSharpCode.SharpZipLib.Zip;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.ResourceProviders;
using UnityEngine.SceneManagement;
using MDPro3.Utility;

namespace MDPro3
{
    public class Boot : MonoBehaviour
    {
        public Slider progressBar;
        public Text text;

        string title;
        string dots;
        float time;
        bool extracting;
        int totalNum;
        int nowNum;

        List<string> zips = new List<string>();

        void Start()
        {
            Application.targetFrameRate = 0;

#if !UNITY_EDITOR && !UNITY_ANDROID
            var appRoot = Directory.GetParent(Application.dataPath)?.FullName;
            if (!string.IsNullOrEmpty(appRoot))
            {
                Environment.CurrentDirectory = appRoot;
                Directory.SetCurrentDirectory(appRoot);
                Debug.Log("MDPro3 working directory: " + appRoot);
            }
#endif

#if !UNITY_EDITOR && UNITY_ANDROID
            Environment.CurrentDirectory = Application.persistentDataPath;
            Directory.SetCurrentDirectory(Application.persistentDataPath);
            BetterStreamingAssets.Initialize();
            QuestRuntimeResourceDiagnostics.LogStartupState();
            if (VersionCheck())
            {
                var paths = BetterStreamingAssets.GetFiles("\\", "*.zip");
                foreach (var zip in paths)
                    zips.Add(Path.GetFileName(zip).Replace(".zip", ""));

                StartCoroutine(CheckFile());
            }
#else
            InitializeLanguage();
            StartCoroutine(LoadMainSceneAsync());
#endif
        }

        void Update()
        {
            time += Time.deltaTime;
            if (time > 0.33f)
            {
                time = 0;
                dots += ".";
                if (dots == "....")
                    dots = "";
            }
            if (extracting && totalNum != 0)
            {
                float progress = (float)nowNum / totalNum;
                progressBar.value = progress;
            }
            if (totalNum == 0)
                text.text = title + dots;
            else
                text.text = title + "(" + nowNum + Program.slash + totalNum + ")";
        }

        bool InitializeLanguage()
        {
            if (!Directory.Exists(Program.dataPath))
            {
                Directory.CreateDirectory(Program.dataPath);
                Config.Initialize(Program.configPath);
                ApplyQuestRuntimeDefaults();
                Config.Save();
                return true;
            }
            else
            {
                Config.Initialize(Program.configPath);
                ApplyQuestRuntimeDefaults();
                Config.Save();
                if(Config.Get("Version", "Version") == "Version")
                    return true;
                InterString.Initialize();
                return false;
            }
        }

        IEnumerator CheckFile()
        {
            //V1.2.0 Delete Folder MonsterCutin
            if(Application.version == "1.2.0.0")
            {
                if(Config.Get("Android-V1.2.0.0_install", "0") == "0")
                {
                    //Program.ClearDirectoryRecursively(new DirectoryInfo("Android/MonsterCutin"));
                    //Program.ClearDirectoryRecursively(new DirectoryInfo("Android/MasterDuel/Mate"));
                    DeleteDirectoryIfExists("Android/MonsterCutin");
                    DeleteDirectoryIfExists("Android/MasterDuel/Mate");
                }
            }

            IEnumerator enumerator;
            foreach (string zip in zips)
            {
                if (ShouldInstallPayload(zip))
                {
                    enumerator = Check(zip);
                    while (enumerator.MoveNext())
                        yield return enumerator.Current;
                    Config.Set(zip + "_install", "1");
                    Config.Set(zip + "_install_build", GetPayloadBuildStamp());
                    Config.Save();
                    GC.Collect();
                }
            }
            yield return null;
            StartCoroutine(LoadMainSceneAsync());
        }

        bool ShouldInstallPayload(string zip)
        {
            if (PayloadAppearsInstalled(zip))
            {
                if (Config.Get(zip + "_install", "0") == "0"
                    || Config.Get(zip + "_install_build", "") != GetPayloadBuildStamp())
                {
                    Config.Set(zip + "_install", "1");
                    Config.Set(zip + "_install_build", GetPayloadBuildStamp());
                    Config.Save();
                }
                return false;
            }

            if (Config.Get(zip + "_install", "0") == "0")
                return true;

            var installedBuild = Config.Get(zip + "_install_build", "");
            if (installedBuild != GetPayloadBuildStamp())
                return true;

            return false;
        }

        static string GetPayloadBuildStamp()
        {
            return Application.version;
        }

        bool PayloadAppearsInstalled(string zip)
        {
            if (string.IsNullOrEmpty(zip))
                return false;

            switch (zip)
            {
                case "Data":
                    return File.Exists(Path.Combine(Program.dataPath, "items.txt"))
                        && File.Exists(Path.Combine(Program.dataPath, "cards_Lite.json"));
                case "Deck":
                    return DirectoryHasFile("Deck", "*.ydk", SearchOption.TopDirectoryOnly);
                case "Puzzle":
                    return DirectoryHasFile("Puzzle", "*.lua", SearchOption.TopDirectoryOnly);
                case "Picture_Closeup":
                    return File.Exists(Path.Combine("Picture", "Closeup", "89631139.png"))
                        || DirectoryHasFile(Path.Combine("Picture", "Closeup"), "*.png", SearchOption.TopDirectoryOnly);
                case "Picture_Art3D":
                    return DirectoryHasFile(Path.Combine("Picture", "Art3D"), "*.glb", SearchOption.TopDirectoryOnly);
                case "Android":
                    return Directory.Exists(Path.Combine("Android", "MasterDuel"));
                case "Sound":
                    return DirectoryHasFile("Sound", "*", SearchOption.AllDirectories);
            }

            var outputFolder = zip.Contains("_") ? zip.Split('_')[0] : zip;
            return Directory.Exists(outputFolder) && DirectoryHasFile(outputFolder, "*", SearchOption.AllDirectories);
        }

        static bool DirectoryHasFile(string path, string pattern, SearchOption searchOption)
        {
            if (string.IsNullOrEmpty(path) || !Directory.Exists(path))
                return false;

            try
            {
                foreach (var file in Directory.EnumerateFiles(path, pattern, searchOption))
                    return true;
            }
            catch (Exception ex)
            {
                Debug.LogWarning("Payload file check failed for " + path + ": " + ex.Message);
            }

            return false;
        }
        IEnumerator Check(string type)
        {
            var zipName = type + ".zip";
            title = InterString.Get("正在读取[?]", zipName);
            nowNum = 0;
            totalNum = 0;
            progressBar.value = 0;

            var outPath = "";
            if (type.Contains("_"))
                outPath = type.Split('_')[0];
            if (outPath.Length > 0 && !Directory.Exists(outPath))
                Directory.CreateDirectory(outPath);

            title = InterString.Get("正在解压[?]", zipName);
            using (var zipStream = OpenStreamingAssetRead(zipName))
            {
                IEnumerator enumerator = ExtractZipStream(zipStream, outPath);
                while (enumerator.MoveNext())
                {
                    yield return enumerator.Current;
                }
            }
        }

        Stream OpenStreamingAssetRead(string path)
        {
#if !UNITY_EDITOR && UNITY_ANDROID
            return BetterStreamingAssets.OpenRead(path);
#else
            return File.OpenRead(Path.Combine(Application.streamingAssetsPath, path));
#endif
        }
        IEnumerator LoadMainSceneAsync()
        {
            nowNum = 0;
            totalNum = 0;
            progressBar.value = 0;

            Config.Initialize(Program.configPath);
            Config.Set("Version", Application.version[..5]);
            ApplyQuestRuntimeDefaults();
            Config.Save();

            title = InterString.Get("正在初始化");
            var ini = Addressables.InitializeAsync();
            while (!ini.IsDone)
            {
                progressBar.value = ini.PercentComplete;
                yield return null;
            }
            AddressablesResourceAliases.Register();

            title = InterString.Get("正在读取数据");
            Program.items = Resources.Load<Items>("Items");
            if (Program.items == null)
            {
                var handle = Addressables.LoadAssetAsync<Items>("Items");
                while (!handle.IsDone)
                {
                    progressBar.value = handle.PercentComplete;
                    yield return null;
                }
                Program.items = handle.Result;
            }

            title = InterString.Get("正在进入游戏");
            var load = SceneManager.LoadSceneAsync("Main");
            while (!load.isDone)
            {
                yield return null;
                progressBar.value = load.progress;
            }
        }

        public static void FastExtractZipFile(string file, string dir, string password = "")
        {
            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            FastZip zip = new FastZip();
            zip.Password = password;
            zip.ExtractZip(file, dir, "");
        }

        IEnumerator ExtractZipFile(byte[] data, string outFolder)
        {
            using (MemoryStream mstrm = new MemoryStream(data))
            {
                IEnumerator enumerator = ExtractZipStream(mstrm, outFolder);
                while (enumerator.MoveNext())
                    yield return enumerator.Current;
            }
        }

        IEnumerator ExtractZipStream(Stream data, string outFolder)
        {
            ZipFile zf = null;
            var outputRoot = string.IsNullOrEmpty(outFolder) ? Directory.GetCurrentDirectory() : Path.GetFullPath(outFolder);
            if (!Directory.Exists(outputRoot))
                Directory.CreateDirectory(outputRoot);
            var outputRootWithSlash = outputRoot.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;
            byte[] buffer = new byte[81920];

            try
            {
                zf = new ZipFile(data);
                zf.IsStreamOwner = false;
                int count = 0;
                foreach (ZipEntry zipEntry in zf)
                    count++;
                totalNum = count;
                nowNum = 0;
                extracting = true;
                foreach (ZipEntry zipEntry in zf)
                {
                    nowNum++;
                    if (!zipEntry.IsFile)
                        continue;

                    string entryFileName = zipEntry.Name.Replace('\\', '/');
                    string fullZipToPath = Path.GetFullPath(Path.Combine(outputRoot, entryFileName));
                    if (!fullZipToPath.StartsWith(outputRootWithSlash, System.StringComparison.OrdinalIgnoreCase))
                        throw new IOException("Zip entry escapes output folder: " + entryFileName);

                    string directoryName = Path.GetDirectoryName(fullZipToPath);
                    if (directoryName.Length > 0)
                        Directory.CreateDirectory(directoryName);
                    using (Stream zipStream = zf.GetInputStream(zipEntry))
                    using (FileStream streamWriter = File.Create(fullZipToPath))
                    {
                        StreamUtils.Copy(zipStream, streamWriter, buffer);
                    }
                    yield return null;
                }
            }
            finally
            {
                if (zf != null)
                    zf.Close();
            }
        }
        void ApplyQuestRuntimeDefaults()
        {
#if !UNITY_EDITOR && UNITY_ANDROID
            var previousLanguage = Config.Get(Language.ConfigName, Language.SimplifiedChinese);
            var previousCardLanguage = Config.Get(Language.CardConfigName, Language.SimplifiedChinese);
            Config.Set(Language.ConfigName, Language.SimplifiedChinese);
            Config.Set(Language.CardConfigName, Language.SimplifiedChinese);
            if (previousLanguage != Language.SimplifiedChinese || previousCardLanguage != Language.SimplifiedChinese)
            {
                Debug.LogFormat(
                    "Quest runtime language override: Language {0}->{1}, CardLanguage {2}->{3}",
                    previousLanguage,
                    Language.SimplifiedChinese,
                    previousCardLanguage,
                    Language.SimplifiedChinese);
            }

            if (Config.Get("QuestCloseupDefaultsV3", "0") == "0")
            {
                Config.SetBool("DuelCloseup", false);
                Config.SetBool("WatchCloseup", false);
                Config.SetBool("ReplayCloseup", false);
                Config.Set("QuestCloseupDefaultsV3", "1");
            }
#endif
        }

        bool VersionCheck()
        {
            var firstInstall = InitializeLanguage();

            var installVersion = Application.version;
            var installedVersion = Config.Get("Version", "Version");
            if (installedVersion == "Version")
                firstInstall = true;

            if(firstInstall)
            {
                if((installVersion.Length > 5 || !installVersion.EndsWith("0")) && !HasAndroidFirstInstallPayload())
                {
                    title = "不能直接安装更新包。Can not install update apk directly.";
                    DeleteDirectoryIfExists(Program.dataPath);
                    return false;
                }
                else
                    return true;
            }
            else //firstInstall
            {
                if(installVersion == installedVersion)
                    return true;

                if (installVersion.Length > 5)
                {
                    if(installVersion.EndsWith("0"))
                    {
                        if (InstallNext(installedVersion, installVersion))
                            return true;
                        else if (installVersion[..5] == installedVersion)
                            return true;
                        else
                        {
                            title = InterString.Get("当前更新包需要的版本：「[?]」。", VersionPre(installVersion));
                            title += InterString.Get("已安装版本：「[?]」。", installedVersion);
                            return false;
                        }
                    }
                    else
                    {
                        if (installVersion.Substring(0, 5) == installedVersion)
                            return true;
                        else
                        {
                            title = InterString.Get("当前更新包需要的版本：「[?]」。", installVersion.Substring(0, 5));
                            title += InterString.Get("已安装版本：「[?]」。", installedVersion);
                            return false;
                        }
                    }
                }
                else
                {
                    if (installVersion.EndsWith("0"))
                        return true;
                    else
                    {
                        if(VersionPre(installVersion) == installedVersion.Substring(0, 5))
                            return true;
                        else
                        {
                            title = InterString.Get("当前更新包需要的版本：「[?]」。", VersionPre(installVersion));
                            title += InterString.Get("已安装版本：「[?]」。", installedVersion);
                            return false;
                        }
                    }
                }
            }
        }

        bool InstallNext(string installedVersion, string installVersion)
        {
            var installedInt = GetVersionInt(installedVersion);
            var installInt = GetVersionInt(installVersion);
            if(installInt -  installedInt == 1)
                return true;
            else
                return false;
        }

        string VersionPre(string version)
        {
            var versionInt = GetVersionInt(version);
            string returnValue = (versionInt - 1).ToString("D3");
            return returnValue.Substring(0, 1) + "." + returnValue.Substring(1, 1) + "." + returnValue.Substring(2, 1);
        }

        int GetVersionInt(string version)
        {
            string textInt = version.Substring(0, 1) + version.Substring(2, 1) + version.Substring(4, 1);
            return int.Parse(textInt);
        }

        bool HasAndroidFirstInstallPayload()
        {
#if !UNITY_EDITOR && UNITY_ANDROID
            return BetterStreamingAssets.FileExists("Data.zip") || HasExternalRuntimePayload();
#else
            return false;
#endif
        }

        bool HasExternalRuntimePayload()
        {
#if !UNITY_EDITOR && UNITY_ANDROID
            bool hasData = File.Exists(Path.Combine(Program.dataPath, "items.txt"))
                && File.Exists(Path.Combine(Program.dataPath, "lflist.conf"))
                && File.Exists(Path.Combine(Program.dataPath, "cards_Lite.json"));
            bool hasWindbot = Directory.Exists(Path.Combine(Program.dataPath, "Windbot", "Decks"))
                && Directory.Exists(Path.Combine(Program.dataPath, "Windbot", "Dialogs"));
            bool hasAndroid = Directory.Exists(Path.Combine("Android", "MasterDuel"));
            if (!hasData || !hasWindbot || !hasAndroid)
                Debug.LogWarning($"Quest external runtime payload incomplete. Data={hasData}, Windbot={hasWindbot}, Android={hasAndroid}, cwd={Directory.GetCurrentDirectory()}");
            return hasData && hasWindbot && hasAndroid;
#else
            return false;
#endif
        }

        void DeleteDirectoryIfExists(string path)
        {
            if (Directory.Exists(path))
                Directory.Delete(path, true);
        }
    }
}
