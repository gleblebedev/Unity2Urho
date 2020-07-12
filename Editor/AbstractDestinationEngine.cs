using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using UnityEditor;
using UnityToCustomEngineExporter.Editor.Urho3D;
using Object = UnityEngine.Object;

namespace UnityToCustomEngineExporter.Editor
{
    public abstract class AbstractDestinationEngine
    {
        private readonly CancellationToken _cancellationToken;
        private readonly HashSet<string> _visitedAssetPaths = new HashSet<string>();

        public AbstractDestinationEngine(CancellationToken cancellationToken)
        {
            _cancellationToken = cancellationToken;
        }

        public IEnumerable<ProgressBarReport> ExportAssets(string[] assetGUIDs, PrefabContext prefabContext)
        {
            yield return "Preparing " + assetGUIDs.Length + " assets to export";
            foreach (var guid in assetGUIDs)
                EditorTaskScheduler.Default.ScheduleForegroundTask(() =>
                    ExportAssetsAtPath(AssetDatabase.GUIDToAssetPath(guid), prefabContext));
        }

        public void ScheduleAssetExport(Object asset, PrefabContext prefabContext)
        {
            EditorTaskScheduler.Default.ScheduleForegroundTask(() => ExportAsset(asset, prefabContext));
        }

        public void ScheduleAssetExportAtPath(string assetPath, PrefabContext prefabContext)
        {
            EditorTaskScheduler.Default.ScheduleForegroundTask(() => ExportAssetsAtPath(assetPath, prefabContext));
        }

        protected abstract void ExportAssetBlock(string assetPath, Type mainType, Object[] assets, PrefabContext prefabContext);

        protected abstract IEnumerable<ProgressBarReport> ExportDynamicAsset(Object asset, PrefabContext prefabContext);

        private IEnumerable<ProgressBarReport> ExportAsset(Object asset, PrefabContext prefabContext)
        {
            var assetPath = AssetDatabase.GetAssetPath(asset);
            if (string.IsNullOrWhiteSpace(assetPath))
                return ExportDynamicAsset(asset, prefabContext);
            if (assetPath == "Library/unity default resources" || assetPath == "Resources/unity_builtin_extra")
                return ExportUnityDefaultResource(asset, assetPath);
            return ExportAssetsAtPath(assetPath, prefabContext);
        }

        private IEnumerable<ProgressBarReport> ExportUnityDefaultResource(Object asset, string assetPath)
        {
            yield return asset.name;
            ExportAssetBlock(assetPath, asset.GetType(), new[] {asset}, null);
        }

        private IEnumerable<ProgressBarReport> ExportAssetsAtPath(string assetPath, PrefabContext prefabContext)
        {
            if (string.IsNullOrWhiteSpace(assetPath))
                yield break;
            if (_cancellationToken.IsCancellationRequested)
                yield break;
            if (!_visitedAssetPaths.Add(assetPath))
                yield break;
            if (!File.Exists(assetPath) && !Directory.Exists(assetPath))
                yield break;
            var attrs = File.GetAttributes(assetPath);
            if (attrs.HasFlag(FileAttributes.Directory))
            {
                foreach (var guid in AssetDatabase.FindAssets("", new[] {assetPath}))
                    EditorTaskScheduler.Default.ScheduleForegroundTask(() =>
                        ExportAssetsAtPath(AssetDatabase.GUIDToAssetPath(guid), prefabContext));
                yield break;
            }

            yield return "Loading " + assetPath;
            var assets = AssetDatabase.LoadAllAssetsAtPath(assetPath);
            yield return "Exporting " + assetPath;
            ExportAssetBlock(assetPath, AssetDatabase.GetMainAssetTypeAtPath(assetPath), assets, prefabContext);
        }
    }
}