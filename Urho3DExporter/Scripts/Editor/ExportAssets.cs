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

        [MenuItem("Assets/Export To Urho3D")]
        private static void ExportToUrho()
        {
            string urhoDataPath = EditorUtility.SaveFolderPanel("Save assets to Data folder", _prevFolder, "");
            if (string.IsNullOrEmpty(urhoDataPath))
            {
                return;
            }

            if (urhoDataPath.StartsWith(Path.GetDirectoryName(Application.dataPath).Replace('\\','/'), StringComparison.InvariantCultureIgnoreCase))
            {
                EditorUtility.DisplayDialog("Error",
                    "Selected path is inside Unity folder. Please select a different folder.", "Ok");
                return;
            }

            _prevFolder = urhoDataPath;

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
            //string urhoDataPath = @"C:\Temp\Data\";

            //foreach (var type in assets.Select(_ => _.Type).Distinct()) Debug.Log(type.FullName);

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
            //foreach (var s in guids2)
            //{
            //    var path = AssetDatabase.GUIDToAssetPath(s);
            //    if (path.StartsWith(assetsPrefix))
            //    {
            //        path = path.Substring(assetsPrefix.Length);
            //    }

            //    if (path.StartsWith("PolygonSciFiCity"))
            //    {
            //        if (path.EndsWith(".prefab", true, CultureInfo.InvariantCulture))
            //        {
            //            ExportPrefab(s, path, @"C:\Temp\Data\");
            //        }

            //        if (path.EndsWith(".fbx", true, CultureInfo.InvariantCulture))
            //        {
            //            ExportModel(s, path, @"C:\Temp\Data\");
            //        }

            //        if (path.EndsWith(".mat", true, CultureInfo.InvariantCulture))
            //        {
            //            ExportMaterial(s, path, @"C:\Temp\Data\");
            //        }

            //        if (path.EndsWith(".png", true, CultureInfo.InvariantCulture) ||
            //            path.EndsWith(".dds", true, CultureInfo.InvariantCulture) ||
            //            path.EndsWith(".tga", true, CultureInfo.InvariantCulture))
            //        {
            //            CopyFileIfNew(s, path, @"C:\Temp\Data\");
            //        }
            //    }
            //}
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
            if (File.Exists(assetContext.UrhoFileName))
                return;
            Directory.CreateDirectory(Path.GetDirectoryName(assetContext.UrhoFileName));
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
            Directory.CreateDirectory(assetContext.ContentFolder);

            new MeshExporter(assets).ExportAsset(assetContext);

            if (File.Exists(assetContext.UrhoFileName))
                return;

            new PrefabExporter(assets).ExportAsset(assetContext);
        }



        public static FileStream CreateFile(string path, string dataFolder, string extension)
        {
            var fileName = Path.Combine(Path.Combine(dataFolder, Path.GetDirectoryName(path)),
                Path.GetFileNameWithoutExtension(path) + extension);
            Directory.CreateDirectory(Path.GetDirectoryName(fileName));
            var xmlFile = File.Open(fileName, FileMode.Create, FileAccess.Write, FileShare.Read);
            return xmlFile;
        }

      
    }
}