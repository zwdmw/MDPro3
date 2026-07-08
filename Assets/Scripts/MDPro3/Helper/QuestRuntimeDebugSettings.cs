using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace MDPro3
{
    public static class QuestRuntimeDebugSettings
    {
        private const string DebugFlagPath = "Data/quest_debug.flag";
        private const string AutoDuelFlagPath = "Data/quest_auto_duel.flag";
        private const string AutoCaptureFlagPath = "Data/quest_auto_capture.flag";
        private const string DebugDirectory = "QuestDebug";

        private static bool initialized;
        private static bool commandLineRead;
        private static bool summaryLogged;
        private static readonly HashSet<string> loggedFlagPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        public static bool Enabled { get; private set; }
        public static bool VerboseDiagnostics { get; private set; }
        public static bool EventLog { get; private set; }
        public static bool AutoCapture { get; private set; }
        public static bool AutoEnterSolo { get; private set; }
        public static bool AutoFrameDuelView { get; private set; }

        public static void Initialize()
        {
            if (!commandLineRead)
            {
                commandLineRead = true;
                ReadCommandLine();
            }

            ReadFlags();
            initialized = true;
            if (AutoEnterSolo || AutoCapture || VerboseDiagnostics || EventLog || AutoFrameDuelView)
                Enabled = true;

            LogSummaryIfNeeded();
        }

        private static void ReadFlags()
        {
            ReadFlag(DebugFlagPath, enableDebug: true);
            ReadFlag(AutoDuelFlagPath, autoDuel: true);
            ReadFlag(AutoCaptureFlagPath, autoCapture: true);
            ReadFlag(Path.Combine(DebugDirectory, "autoduel.flag"), autoDuel: true);
            ReadFlag(Path.Combine(DebugDirectory, "capture.flag"), autoCapture: true);
            ReadFlag(Path.Combine(DebugDirectory, "verbose.flag"), verbose: true);
            ReadFlag(Path.Combine(DebugDirectory, "frameview.flag"), frameView: true);
        }

        public static void LogSummaryIfNeeded()
        {
            if (!initialized)
                Initialize();
            if (summaryLogged || !Enabled)
                return;

            summaryLogged = true;
            Debug.LogFormat(
                "Quest debug settings: enabled={0}, autoSolo={1}, autoCapture={2}, verbose={3}, eventLog={4}, autoFrameView={5}",
                Enabled,
                AutoEnterSolo,
                AutoCapture,
                VerboseDiagnostics,
                EventLog,
                AutoFrameDuelView);
        }

        private static void ReadCommandLine()
        {
            try
            {
                var args = Environment.GetCommandLineArgs();
                foreach (var raw in args)
                {
                    var arg = NormalizeToken(raw);
                    switch (arg)
                    {
                        case "--quest-debug":
                            Enabled = true;
                            EventLog = true;
                            break;
                        case "--quest-verbose":
                            Enabled = true;
                            VerboseDiagnostics = true;
                            EventLog = true;
                            break;
                        case "--quest-event-log":
                            Enabled = true;
                            EventLog = true;
                            break;
                        case "--quest-auto-capture":
                            Enabled = true;
                            AutoCapture = true;
                            break;
                        case "--quest-auto-duel":
                            Enabled = true;
                            AutoEnterSolo = true;
                            AutoCapture = true;
                            EventLog = true;
                            break;
                        case "--quest-frame-duel-view":
                            Enabled = true;
                            AutoFrameDuelView = true;
                            break;
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning("Quest debug command line parse failed: " + ex.Message);
            }
        }

        private static void ReadFlag(
            string path,
            bool enableDebug = false,
            bool autoDuel = false,
            bool autoCapture = false,
            bool verbose = false,
            bool frameView = false)
        {
            foreach (var candidate in EnumerateFlagCandidates(path))
            {
                try
                {
                    if (!File.Exists(candidate))
                        continue;

                    Enabled |= enableDebug || autoDuel || autoCapture || verbose || frameView;
                    AutoEnterSolo |= autoDuel;
                    AutoCapture |= autoCapture || autoDuel;
                    VerboseDiagnostics |= verbose;
                    EventLog |= enableDebug || autoDuel || verbose;
                    AutoFrameDuelView |= frameView;

                    var content = File.ReadAllText(candidate);
                    ApplyFlagContent(content);
                    if (loggedFlagPaths.Add(candidate))
                        Debug.Log("Quest debug flag detected: " + candidate);
                }
                catch (Exception ex)
                {
                    Debug.LogWarning("Quest debug flag read failed: " + candidate + " / " + ex.Message);
                }
            }
        }

        private static IEnumerable<string> EnumerateFlagCandidates(string relativePath)
        {
            if (string.IsNullOrEmpty(relativePath))
                yield break;

            if (Path.IsPathRooted(relativePath))
            {
                yield return NormalizePath(relativePath);
                yield break;
            }

            var emitted = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var root in EnumerateFlagRoots())
            {
                if (string.IsNullOrEmpty(root))
                    continue;

                string candidate;
                try
                {
                    candidate = NormalizePath(Path.Combine(root, relativePath));
                }
                catch
                {
                    continue;
                }

                if (emitted.Add(candidate))
                    yield return candidate;
            }
        }

        private static IEnumerable<string> EnumerateFlagRoots()
        {
            yield return Directory.GetCurrentDirectory();

            if (!string.IsNullOrEmpty(Application.persistentDataPath))
                yield return Application.persistentDataPath;

            var appRoot = GetApplicationRoot();
            if (!string.IsNullOrEmpty(appRoot))
                yield return appRoot;
        }

        private static string GetApplicationRoot()
        {
            try
            {
                var dataPath = Application.dataPath;
                if (string.IsNullOrEmpty(dataPath))
                    return string.Empty;

                var parent = Directory.GetParent(dataPath);
                return parent == null ? string.Empty : parent.FullName;
            }
            catch
            {
                return string.Empty;
            }
        }

        private static string NormalizePath(string path)
        {
            try
            {
                return Path.GetFullPath(path);
            }
            catch
            {
                return path;
            }
        }

        private static void ApplyFlagContent(string content)
        {
            if (string.IsNullOrWhiteSpace(content))
                return;

            var separators = new[] { '\r', '\n', ' ', '\t', ',', ';' };
            foreach (var raw in content.Split(separators, StringSplitOptions.RemoveEmptyEntries))
            {
                var token = NormalizeToken(raw);
                switch (token)
                {
                    case "debug":
                    case "enabled":
                    case "on":
                        Enabled = true;
                        EventLog = true;
                        break;
                    case "verbose":
                    case "diagnostics":
                        Enabled = true;
                        VerboseDiagnostics = true;
                        EventLog = true;
                        break;
                    case "events":
                    case "eventlog":
                    case "event_log":
                        Enabled = true;
                        EventLog = true;
                        break;
                    case "autoduel":
                    case "auto_duel":
                    case "autosolo":
                    case "auto_solo":
                    case "solo":
                        Enabled = true;
                        AutoEnterSolo = true;
                        AutoCapture = true;
                        EventLog = true;
                        break;
                    case "capture":
                    case "screenshot":
                    case "screenshots":
                        Enabled = true;
                        AutoCapture = true;
                        break;
                    case "frameview":
                    case "frame_view":
                    case "autoview":
                    case "auto_view":
                        Enabled = true;
                        AutoFrameDuelView = true;
                        break;
                }
            }
        }

        private static string NormalizeToken(string value)
        {
            return string.IsNullOrWhiteSpace(value)
                ? string.Empty
                : value.Trim().TrimStart('/').ToLowerInvariant();
        }
    }
}
