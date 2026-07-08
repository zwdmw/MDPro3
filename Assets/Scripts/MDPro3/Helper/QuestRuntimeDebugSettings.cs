using System;
using System.Collections.Generic;
using System.Globalization;
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
        public static string DebugViewPreset { get; private set; }
        public static Vector3 DebugViewOffset { get; private set; }
        public static bool HasDebugViewOffset { get; private set; }
        public static Vector3 DebugViewLookAt { get; private set; }
        public static bool HasDebugViewLookAt { get; private set; }
        public static float DebugViewYawDegrees { get; private set; }
        public static bool HasDebugViewYaw { get; private set; }
        public static float DebugViewScale { get; private set; } = 1f;
        public static bool HasDebugViewScale { get; private set; }

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
            ReadFlag(Path.Combine(DebugDirectory, "settings.flag"), enableDebug: true);
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
                "Quest debug settings: enabled={0}, autoSolo={1}, autoCapture={2}, verbose={3}, eventLog={4}, autoFrameView={5}, view={6}, offset={7}, lookAt={8}, yaw={9}, scale={10}",
                Enabled,
                AutoEnterSolo,
                AutoCapture,
                VerboseDiagnostics,
                EventLog,
                AutoFrameDuelView,
                string.IsNullOrEmpty(DebugViewPreset) ? "<default>" : DebugViewPreset,
                HasDebugViewOffset ? DebugViewOffset.ToString() : "<preset>",
                HasDebugViewLookAt ? DebugViewLookAt.ToString() : "<preset>",
                HasDebugViewYaw ? DebugViewYawDegrees.ToString("F1", CultureInfo.InvariantCulture) : "<preset>",
                HasDebugViewScale ? DebugViewScale.ToString("F2", CultureInfo.InvariantCulture) : "<preset>");
        }

        private static void ReadCommandLine()
        {
            try
            {
                var args = Environment.GetCommandLineArgs();
                foreach (var raw in args)
                {
                    var arg = NormalizeToken(raw);
                    if (TryApplyAssignmentToken(arg))
                        continue;

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
                    AutoCapture |= autoCapture;
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

            var separators = new[] { '\r', '\n', ' ', '\t', ';' };
            foreach (var raw in content.Split(separators, StringSplitOptions.RemoveEmptyEntries))
            {
                var token = NormalizeToken(raw);
                if (TryApplyAssignmentToken(token))
                    continue;

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

        private static bool TryApplyAssignmentToken(string token)
        {
            if (string.IsNullOrWhiteSpace(token))
                return false;

            if (TryGetAssignmentValue(token, "--quest-view=", out var view)
                || TryGetAssignmentValue(token, "view=", out view)
                || TryGetAssignmentValue(token, "preset=", out view))
            {
                Enabled = true;
                AutoFrameDuelView = true;
                DebugViewPreset = view;
                return true;
            }

            if (TryGetAssignmentValue(token, "--quest-view-offset=", out var offset)
                || TryGetAssignmentValue(token, "offset=", out offset)
                || TryGetAssignmentValue(token, "viewoffset=", out offset))
            {
                if (TryParseVector3(offset, out var value))
                {
                    Enabled = true;
                    AutoFrameDuelView = true;
                    DebugViewOffset = value;
                    HasDebugViewOffset = true;
                }
                return true;
            }

            if (TryGetAssignmentValue(token, "--quest-view-lookat=", out var lookAt)
                || TryGetAssignmentValue(token, "lookat=", out lookAt)
                || TryGetAssignmentValue(token, "target=", out lookAt))
            {
                if (TryParseVector3(lookAt, out var value))
                {
                    Enabled = true;
                    AutoFrameDuelView = true;
                    DebugViewLookAt = value;
                    HasDebugViewLookAt = true;
                }
                return true;
            }

            if (TryGetAssignmentValue(token, "--quest-view-yaw=", out var yaw)
                || TryGetAssignmentValue(token, "yaw=", out yaw))
            {
                if (TryParseFloat(yaw, out var value))
                {
                    Enabled = true;
                    AutoFrameDuelView = true;
                    DebugViewYawDegrees = value;
                    HasDebugViewYaw = true;
                }
                return true;
            }

            if (TryGetAssignmentValue(token, "--quest-view-scale=", out var scale)
                || TryGetAssignmentValue(token, "scale=", out scale)
                || TryGetAssignmentValue(token, "worldscale=", out scale))
            {
                if (TryParseFloat(scale, out var value))
                {
                    Enabled = true;
                    AutoFrameDuelView = true;
                    DebugViewScale = Mathf.Max(0.0001f, value);
                    HasDebugViewScale = true;
                }
                return true;
            }

            return false;
        }

        private static bool TryGetAssignmentValue(string token, string prefix, out string value)
        {
            value = string.Empty;
            if (string.IsNullOrEmpty(token) || string.IsNullOrEmpty(prefix))
                return false;
            if (!token.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                return false;

            value = token.Substring(prefix.Length).Trim();
            return !string.IsNullOrEmpty(value);
        }

        private static bool TryParseVector3(string text, out Vector3 value)
        {
            value = Vector3.zero;
            if (string.IsNullOrWhiteSpace(text))
                return false;

            var parts = text.Split(new[] { ',', ':', '|' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length != 3)
                return false;

            if (!TryParseFloat(parts[0], out var x)
                || !TryParseFloat(parts[1], out var y)
                || !TryParseFloat(parts[2], out var z))
                return false;

            value = new Vector3(x, y, z);
            return true;
        }

        private static bool TryParseFloat(string text, out float value)
        {
            return float.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out value);
        }

        private static string NormalizeToken(string value)
        {
            return string.IsNullOrWhiteSpace(value)
                ? string.Empty
                : value.Trim().TrimStart('/').ToLowerInvariant();
        }
    }
}
