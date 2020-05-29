using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Urho3DExporter
{
    public class ExportAssets
    {
        private static readonly string assetsPrefix = "Assets/";

        internal static FileStream CreateFile(string urhoFileName)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(urhoFileName));
            return File.Open(urhoFileName, FileMode.Create, FileAccess.Write, FileShare.Read);
        }

        private static HashSet<string> _knownShaderNames = new HashSet<string>();

        private static int _id;

        public static List<T> Split<T>(IEnumerable<T> source, Func<T, bool> predicate, Action<T> ifTrue)
        {
            var ifFalse = new List<T>();
            foreach (var item in source)
            {
                if (predicate(item))
                    ifTrue(item);
                else
                    ifFalse.Add(item);
            }

            return ifFalse;
        }

        static string _prevFolder = "";

        //[MenuItem("CONTEXT/Terrain/Export Terrain To Urho3D")]
        //static void ExportTerrain(MenuCommand command)
        //{
        //    if (!ResolveDataPath(out var urhoDataPath)) return;
        //}

        [MenuItem("Assets/Export Assets and Scene To Urho3D")]
        private static void ExportToUrho()
        {
            var urhoDataPath = ResolveDataPath();
            if (urhoDataPath == null) return;

            if (urhoDataPath.ToString().StartsWith(Path.GetDirectoryName(Application.dataPath).FixDirectorySeparator(), StringComparison.InvariantCultureIgnoreCase))
            {
                EditorUtility.DisplayDialog("Error", "Selected path is inside Unity folder. Please select a different folder.", "Ok");
                return;
            }

            _prevFolder = urhoDataPath.ToString();

            AssetCollection assets;
            if (Selection.assetGUIDs.Length == 0)
            {
                assets = new AssetCollection(urhoDataPath, AssetDatabase.FindAssets("").Select(_ => AssetContext.Create(_, urhoDataPath)).Where(_ => _.Type != null));
            }
            else
            {
                var selectedAssets = new HashSet<string>();
                foreach (var assetGuiD in Selection.assetGUIDs)
                {
                    AddSelection(assetGuiD, selectedAssets);
                }
                assets = new AssetCollection(urhoDataPath, selectedAssets.Select(_ => AssetContext.Create(_, urhoDataPath)).Where(_ => _.Type != null));
            }
            _id = 0;

            List<AssetContext> other  = Split(assets, _ => _.Is3DAsset, _=> Process3DAsset(assets, _));
            other = Split(other, _ => _.Type == typeof(Texture3D) || _.Type == typeof(Texture2D) || _.Type == typeof(Cubemap), _ => new TextureExporter(assets).ExportAsset(_));
            other = Split(other, _ => _.Type == typeof(Material), _ => new MaterialExporter(assets).ExportAsset(_));
            var activeScene = EditorSceneManager.GetActiveScene();
            
            other = Split(other, _ => _.Type == typeof(SceneAsset), _ =>
            {
                if (_.AssetPath == activeScene.path)
                    ProcessSceneAsset(assets, _, activeScene);
            });
            foreach (var assetContext in other)
            {
                ProcessAsset(assets, assetContext);
            }
        }

        private static DestinationFolder ResolveDataPath()
        {
            var urhoDataPath = EditorUtility.SaveFolderPanel("Save assets to Data folder", _prevFolder, "");
            if (!string.IsNullOrEmpty(urhoDataPath))
            {
                return new DestinationFolder(urhoDataPath);
            }

            return null;
        }

        private static void AddSelection(string assetGuiD, HashSet<string> guids)
        {
            var path = AssetDatabase.GUIDToAssetPath(assetGuiD);
            var type = AssetDatabase.GetMainAssetTypeAtPath(path);
            if (type == null)
                return;
            if (type == typeof(DefaultAsset))
            {
                foreach (var findAsset in AssetDatabase.FindAssets("", new []{ path}))
                {
                    guids.Add(findAsset);
                }
            }
            else
            {
                guids.Add(assetGuiD);
            }
        }

        private static void ProcessSceneAsset(AssetCollection assets, AssetContext assetContext, Scene scene)
        {
            new SceneExporter(assets).ExportAsset(assetContext, scene);
        }

        private static void ProcessAsset(AssetCollection assets, AssetContext assetContext)
        {
            if (assetContext.Type == typeof(GameObject))
            {
                new PrefabExporter(assets).ExportAsset(assetContext);
            }
            else if (assetContext.Type == typeof(SceneAsset))
            {
                //new SceneExporter(assets).ExportAsset(assetContext);
            }
        }

        private static void Process3DAsset(AssetCollection assets, AssetContext assetContext)
        {
            new MeshExporter(assets).ExportAsset(assetContext);

            new PrefabExporter(assets).ExportAsset(assetContext);
        }
    }
}