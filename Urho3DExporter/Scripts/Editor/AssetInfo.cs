using System;
using System.IO;
using System.Threading;
using UnityEditor;
using UnityEngine;
using Object = System.Object;

namespace Assets.Scripts.UnityToCustomEngineExporter.Editor
{
    public class AssetInfo
    {
        public string Guid { get;  }
        public AssetInfo(string guid)
        {
            Guid = guid;
            AssetPath = AssetDatabase.GUIDToAssetPath(guid);
            if (!string.IsNullOrWhiteSpace(AssetPath))
            {
                MainAssetType = AssetDatabase.GetMainAssetTypeAtPath(AssetPath);
                RelPath = GetRelPathFromAssetPath(AssetPath);
                FullPath = Path.Combine(Application.dataPath, RelPath);
            }
        }

        public string FullPath { get; }

        public string RelPath { get; }

        public static string GetRelPathFromAssetPath(string assetPath)
        {
            if (assetPath.StartsWith("Assets/", StringComparison.InvariantCultureIgnoreCase))
                return assetPath.Substring("Assets/".Length);
            return assetPath;
        }
        public static string GetRelPathFromAsset(UnityEngine.Object asset)
        {
            if (asset == null)
                return null;
            var path = AssetDatabase.GetAssetPath(asset);
            return GetRelPathFromAssetPath(path);
        }
        public static string GetRelPathFromAsset(UnityEngine.SceneManagement.Scene asset)
        {
            var path = asset.path;
            return GetRelPathFromAssetPath(path);
        }
        public Type MainAssetType { get; }

        public string AssetPath { get; }

        public override string ToString()
        {
            if (string.IsNullOrWhiteSpace(AssetPath))
                return Guid;
            return AssetPath;
        }
    }
}