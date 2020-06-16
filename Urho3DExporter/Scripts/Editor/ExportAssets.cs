using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Urho3DExporter
{
    public class ExportAssets
    {
        private static readonly string assetsPrefix = "Assets/";

        private static HashSet<string> _knownShaderNames = new HashSet<string>();

        private static int _id;

        public static SplitResult<T> Split<T>(IEnumerable<T> source, Func<T, bool> predicate)
        {
            var res = new SplitResult<T>();
            res.Rejected = new List<T>();
            res.Selected = new List<T>();
            foreach (var item in source)
                if (predicate(item))
                    res.Selected.Add(item);
                else
                    res.Rejected.Add(item);
            return res;
        }


        //[MenuItem("CONTEXT/Terrain/Export Terrain To Urho3D")]
        //static void ExportTerrain(MenuCommand command)
        //{
        //    if (!ResolveDataPath(out var urhoDataPath)) return;
        //}

        public static IEnumerable<ProgressBarReport> ExportToUrho(string targetPath, DestinationFolder urhoDataPath,
            bool exportSelected,
            bool exportSceneAsPrefab, bool skipDisabled)
        {
            if (string.IsNullOrWhiteSpace(targetPath))
                yield break;

            AssetCollection assets;
            if (Selection.assetGUIDs.Length == 0 || !exportSelected)
            {
                assets = new AssetCollection(urhoDataPath,
                    AssetDatabase.FindAssets("").Select(_ => AssetContext.Create(_, urhoDataPath))
                        .Where(_ => _.Type != null));
            }
            else
            {
                var selectedAssets = new HashSet<string>();
                foreach (var assetGuiD in Selection.assetGUIDs) AddSelection(assetGuiD, selectedAssets);

                var enumerable = selectedAssets.Select(_ => AssetContext.Create(_, urhoDataPath)).ToList();
                var assetContexts = enumerable.Where(_ => _.Type != null).ToList();
                assets = new AssetCollection(urhoDataPath, assetContexts);
            }

            _id = 0;

            var textureMetadataCollection = new TextureMetadataCollection();
            foreach (var report in textureMetadataCollection.Populate(urhoDataPath))
            {
                yield return report;
            }

            IEnumerable<AssetContext> other = assets;
            {
                var splitResult = Split(other, _ => _.Is3DAsset);
                foreach (var assetContext in splitResult.Selected)
                {
                    yield return new ProgressBarReport(assetContext.AssetPath);
                    Process3DAsset(assets, assetContext, textureMetadataCollection, skipDisabled);
                }
                other = splitResult.Rejected;
            }

            {
                var exporter = new CubemapExporter();
                var splitResult = Split(other, _ => _.Type == typeof(Cubemap));
                foreach (var assetContext in splitResult.Selected)
                {
                    yield return new ProgressBarReport(assetContext.AssetPath);
                    exporter.ExportAsset(assetContext);
                }
                other = splitResult.Rejected;
            }
            Lazy<TextureExporter> textureExporter = new Lazy<TextureExporter>(() => new TextureExporter(assets, textureMetadataCollection));
            {
                var splitResult = Split(other, _ => _.Type == typeof(Texture3D) || _.Type == typeof(Texture2D));
                foreach (var assetContext in splitResult.Selected)
                {
                    yield return new ProgressBarReport(assetContext.AssetPath);
                    textureExporter.Value.ExportAsset(assetContext);
                }
                other = splitResult.Rejected;
            }
            {
                var splitResult = Split(other, _ => _.Type == typeof(Material));
                foreach (var assetContext in splitResult.Selected)
                {
                    yield return new ProgressBarReport(assetContext.AssetPath);
                    new MaterialExporter(assets, textureMetadataCollection).ExportAsset(assetContext);
                }
                other = splitResult.Rejected;
            }

            var activeScene = SceneManager.GetActiveScene();
            {
                var splitResult = Split(other, _ => _.Type == typeof(SceneAsset));
                foreach (var assetContext in splitResult.Selected)
                {
                    if (assetContext.AssetPath == activeScene.path)
                    {
                        yield return new ProgressBarReport(assetContext.AssetPath);
                        //yield return new ProgressBarReport(_.AssetPath);
                        ProcessSceneAsset(assets, assetContext, activeScene, exportSceneAsPrefab, skipDisabled);
                    }
                }
                other = splitResult.Rejected;
            }

            foreach (var assetContext in other)
            {
                yield return new ProgressBarReport(assetContext.AssetPath);
                ProcessAsset(assets, assetContext, skipDisabled);
            }
        }

        internal static FileStream CreateFile(string urhoFileName)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(urhoFileName));
            return File.Open(urhoFileName, FileMode.Create, FileAccess.Write, FileShare.Read);
        }

        private static void AddSelection(string assetGuiD, HashSet<string> guids)
        {
            var path = AssetDatabase.GUIDToAssetPath(assetGuiD);
            var type = AssetDatabase.GetMainAssetTypeAtPath(path);
            if (type == null)
                return;
            if (type == typeof(DefaultAsset))
                foreach (var findAsset in AssetDatabase.FindAssets("", new[] {path}))
                    guids.Add(findAsset);
            else
                guids.Add(assetGuiD);
        }

        private static void ProcessSceneAsset(AssetCollection assets, AssetContext assetContext, Scene scene, bool exportSceneAsPrefab, bool skipDisabled)
        {
            new SceneExporter(assets, exportSceneAsPrefab, skipDisabled).ExportAsset(assetContext, scene);
        }

        private static void ProcessAsset(AssetCollection assets, AssetContext assetContext, bool skipDisabled)
        {
            if (assetContext.Type == typeof(GameObject))
            {
                new PrefabExporter(assets, skipDisabled).ExportAsset(assetContext);
            }
            else if (assetContext.Type == typeof(SceneAsset))
            {
                //new SceneExporter(assets).ExportAsset(assetContext);
            }
        }

        private static void Process3DAsset(AssetCollection assets, AssetContext assetContext, TextureMetadataCollection textureMetadataCollection, bool skipDisabled)
        {
            if (assetContext.Type == typeof(Mesh))
            {
                new MeshExporter(assets).ExportAsset(assetContext);
            }
            else if (assetContext.Type == typeof(GameObject))
            {
                new MeshExporter(assets).ExportAsset(assetContext);
                var allAssets = AssetDatabase.LoadAllAssetsAtPath(assetContext.AssetPath);
                foreach (var asset in allAssets)
                {
                    if (asset is Material material)
                    {
                        new MaterialExporter(assets, textureMetadataCollection).ExportMaterial(assetContext, material);
                    }
                }
                new PrefabExporter(assets, skipDisabled).ExportAsset(assetContext);
            }
            else
            {
                throw new NotImplementedException("Unknown asset type "+assetContext.Type);
            }
        }
    }
}