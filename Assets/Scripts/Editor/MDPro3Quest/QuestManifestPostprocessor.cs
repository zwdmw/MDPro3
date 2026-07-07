using System;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using UnityEditor.Android;

namespace MDPro3.EditorTools
{
    public sealed class QuestManifestPostprocessor : IPostGenerateGradleAndroidProject
    {
        private static readonly XNamespace AndroidNamespace = "http://schemas.android.com/apk/res/android";

        public int callbackOrder => 1000;

        public void OnPostGenerateGradleAndroidProject(string path)
        {
            var candidates = new[]
            {
                Path.Combine(path, "src", "main", "AndroidManifest.xml"),
                Path.Combine(path, "unityLibrary", "src", "main", "AndroidManifest.xml"),
                Path.Combine(path, "xrmanifest.androidlib", "AndroidManifest.xml"),
                Path.Combine(path, "unityLibrary", "xrmanifest.androidlib", "AndroidManifest.xml")
            };

            foreach (var manifestPath in candidates.Where(File.Exists))
                PatchManifest(manifestPath);
        }

        private static void PatchManifest(string manifestPath)
        {
            var document = XDocument.Load(manifestPath);
            var manifest = document.Element("manifest");
            if (manifest == null)
                throw new InvalidOperationException("AndroidManifest.xml does not contain a manifest root: " + manifestPath);

            AddOrUpdateUsesFeature(manifest, "android.hardware.vr.headtracking", required: true, version: "1");
            AddOrUpdateUsesFeature(manifest, "oculus.software.vr", required: true, version: null);
            RemoveUsesFeature(manifest, "com.oculus.feature.PASSTHROUGH");
            RemoveUsesFeature(manifest, "oculus.software.handtracking");
            AddOrUpdateUsesPermission(manifest, "org.khronos.openxr.permission.OPENXR");
            AddOrUpdateUsesPermission(manifest, "org.khronos.openxr.permission.OPENXR_SYSTEM");
            RemoveUsesPermission(manifest, "com.oculus.permission.HAND_TRACKING");
            RemoveUsesPermission(manifest, "com.oculus.permission.USE_SCENE");
            RemoveUsesPermission(manifest, "horizonos.permission.USE_SCENE");
            RemoveUsesPermission(manifest, "com.oculus.permission.USE_ANCHOR_API");
            RemoveUsesPermission(manifest, "horizonos.permission.USE_ANCHOR_API");
            EnsureOpenXrQueries(manifest);

            var application = manifest.Element("application");
            if (application != null)
            {
                RemoveMetaData(application, "com.oculus.handtracking.version");
                RemoveMetaData(application, "com.oculus.handtracking.frequency");
                AddOrUpdateMetaData(application, "com.oculus.supportedDevices", "quest2|questpro|quest3|quest3s");
                RemoveMetaData(application, "com.oculus.ossplash.background");

                var activity = FindLaunchActivity(application);
                if (activity != null)
                {
                    activity.SetAttributeValue(AndroidNamespace + "screenOrientation", "landscape");
                    activity.SetAttributeValue(AndroidNamespace + "resizeableActivity", "false");
                    AddOrUpdateMetaData(activity, "com.oculus.vr.focusaware", "true");
                    RemoveMetaData(activity, "com.oculus.handtracking.version");
                    var intentFilter = activity.Elements("intent-filter").FirstOrDefault();
                    if (intentFilter != null)
                    {
                        AddCategory(intentFilter, "com.oculus.intent.category.VR");
                        AddCategory(intentFilter, "org.khronos.openxr.intent.category.IMMERSIVE_HMD");
                    }
                }
            }

            document.Save(manifestPath);
        }

        private static XElement FindLaunchActivity(XElement application)
        {
            return application.Elements("activity")
                .FirstOrDefault(activity => activity.Elements("intent-filter").Any(IsMainIntentFilter))
                ?? application.Elements("activity").FirstOrDefault();
        }

        private static bool IsMainIntentFilter(XElement intentFilter)
        {
            var hasMain = intentFilter.Elements("action")
                .Any(element => (string)element.Attribute(AndroidNamespace + "name") == "android.intent.action.MAIN");
            var hasLauncher = intentFilter.Elements("category")
                .Any(element => (string)element.Attribute(AndroidNamespace + "name") == "android.intent.category.LAUNCHER");
            return hasMain && hasLauncher;
        }

        private static void AddOrUpdateUsesPermission(XElement manifest, string name)
        {
            var permission = manifest.Elements("uses-permission")
                .FirstOrDefault(element => (string)element.Attribute(AndroidNamespace + "name") == name);
            if (permission == null)
            {
                permission = new XElement("uses-permission");
                manifest.Add(permission);
            }

            permission.SetAttributeValue(AndroidNamespace + "name", name);
        }

        private static void RemoveUsesPermission(XElement manifest, string name)
        {
            foreach (var permission in manifest.Elements("uses-permission")
                         .Where(element => (string)element.Attribute(AndroidNamespace + "name") == name)
                         .ToArray())
            {
                permission.Remove();
            }
        }

        private static void EnsureOpenXrQueries(XElement manifest)
        {
            var queries = manifest.Element("queries");
            if (queries == null)
            {
                queries = new XElement("queries");
                manifest.Add(queries);
            }

            if (!queries.Elements("provider").Any(element =>
                    ((string)element.Attribute(AndroidNamespace + "authorities") ?? string.Empty)
                    .Contains("org.khronos.openxr.runtime_broker")))
            {
                queries.Add(new XElement(
                    "provider",
                    new XAttribute(
                        AndroidNamespace + "authorities",
                        "org.khronos.openxr.runtime_broker;org.khronos.openxr.system_runtime_broker")));
            }

            AddQueryIntent(queries, "org.khronos.openxr.OpenXRRuntimeService");
            AddQueryIntent(queries, "org.khronos.openxr.OpenXRApiLayerService");
        }

        private static void AddQueryIntent(XElement queries, string actionName)
        {
            var exists = queries.Elements("intent")
                .SelectMany(element => element.Elements("action"))
                .Any(element => (string)element.Attribute(AndroidNamespace + "name") == actionName);
            if (exists)
                return;

            queries.Add(new XElement(
                "intent",
                new XElement(
                    "action",
                    new XAttribute(AndroidNamespace + "name", actionName))));
        }

        private static void AddOrUpdateUsesFeature(XElement manifest, string name, bool required, string version)
        {
            var feature = manifest.Elements("uses-feature")
                .FirstOrDefault(element => (string)element.Attribute(AndroidNamespace + "name") == name);
            if (feature == null)
            {
                feature = new XElement("uses-feature");
                manifest.Add(feature);
            }

            feature.SetAttributeValue(AndroidNamespace + "name", name);
            feature.SetAttributeValue(AndroidNamespace + "required", required ? "true" : "false");
            if (!string.IsNullOrEmpty(version))
                feature.SetAttributeValue(AndroidNamespace + "version", version);
        }

        private static void RemoveUsesFeature(XElement manifest, string name)
        {
            foreach (var feature in manifest.Elements("uses-feature")
                         .Where(element => (string)element.Attribute(AndroidNamespace + "name") == name)
                         .ToArray())
            {
                feature.Remove();
            }
        }

        private static void AddOrUpdateMetaData(XElement parent, string name, string value)
        {
            var metaData = parent.Elements("meta-data")
                .FirstOrDefault(element => (string)element.Attribute(AndroidNamespace + "name") == name);
            if (metaData == null)
            {
                metaData = new XElement("meta-data");
                parent.Add(metaData);
            }

            metaData.SetAttributeValue(AndroidNamespace + "name", name);
            metaData.SetAttributeValue(AndroidNamespace + "value", value);
        }

        private static void RemoveMetaData(XElement parent, string name)
        {
            foreach (var metaData in parent.Elements("meta-data")
                         .Where(element => (string)element.Attribute(AndroidNamespace + "name") == name)
                         .ToArray())
            {
                metaData.Remove();
            }
        }

        private static void AddCategory(XElement intentFilter, string name)
        {
            var category = intentFilter.Elements("category")
                .FirstOrDefault(element => (string)element.Attribute(AndroidNamespace + "name") == name);
            if (category != null)
                return;

            intentFilter.Add(new XElement(
                "category",
                new XAttribute(AndroidNamespace + "name", name)));
        }

        private static void RemoveCategory(XElement intentFilter, string name)
        {
            foreach (var category in intentFilter.Elements("category")
                         .Where(element => (string)element.Attribute(AndroidNamespace + "name") == name)
                         .ToArray())
            {
                category.Remove();
            }
        }
    }
}
