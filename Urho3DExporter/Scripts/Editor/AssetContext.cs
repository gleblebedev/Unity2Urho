using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Urho3DExporter
{
    public class AssetContext
    {
        public string UrhoFileName
        {
            get { return _urhoFileName; }
            private set { _urhoFileName = value.FixDirectorySeparator(); }
        }

        public string UrhoAssetName { get; private set; }

        public string FullPath
        {
            get { return _fullPath; }
            private set { _fullPath = value.FixDirectorySeparator(); }
        }

        public string RelPath { get; private set; }

        public Type Type { get; private set; }

        public string AssetPath { get; private set; }

        public string Guid { get; private set; }

        public string ContentFolder
        {
            get { return _contentFolder; }
            private set { _contentFolder = value.FixDirectorySeparator(); }
        }

        public bool Is3DAsset { get; private set; }

        private static readonly HashSet<string> _supported3DFormats = new HashSet<string>()
        {
            ".fbx",
            ".obj",
            ".dae",
            ".3ds",
            ".dxf",
            ".max",
            ".ma",
            ".mb",
            ".blend",
        };

        private string _contentFolder;
        private string _urhoFileName;
        private string _fullPath;

        public static AssetContext Create(string guid, string urhoDataFolder)
        {
            var res = new AssetContext
            {
                Guid = guid
            };
            res.AssetPath = AssetDatabase.GUIDToAssetPath(guid);
            if (!string.IsNullOrEmpty(res.AssetPath))
            {
                res.Type = AssetDatabase.GetMainAssetTypeAtPath(res.AssetPath);
                res.RelPath = res.AssetPath;
                if (res.RelPath.StartsWith("Assets/", StringComparison.InvariantCultureIgnoreCase))
                    res.RelPath = res.RelPath.Substring("Assets/".Length);
                res.FullPath = Path.Combine(Application.dataPath, res.RelPath);
                res.FileExtension = System.IO.Path.GetExtension(res.AssetPath).ToLower();
                res.UrhoAssetName = res.RelPath;
                if (res.Type == typeof(Material))
                    res.UrhoAssetName = RepaceExtension(res.UrhoAssetName, ".xml");
                else if (res.Type == typeof(GameObject))
                {
                    res.UrhoAssetName = RepaceExtension(res.UrhoAssetName, ".xml");
                    res.Is3DAsset = _supported3DFormats.Contains(res.FileExtension);
                }
                else if (res.Type == typeof(SceneAsset)) res.UrhoAssetName = RepaceExtension(res.UrhoAssetName, ".xml");
                res.UrhoFileName = System.IO.Path.Combine(urhoDataFolder, res.UrhoAssetName);
                res.UrhoDataFolder = urhoDataFolder;
                if (res.Is3DAsset)
                {
                    res.ContentFolder = res.UrhoFileName.Substring(0, res.UrhoFileName.Length - ".xml".Length);
                }
            }

            return res;
        }

        public string UrhoDataFolder { get; set; }

        public string FileExtension { get; private set; }

        private static string RepaceExtension(string resUrhoAssetName, string newExt)
        {
            var ext = System.IO.Path.GetExtension(resUrhoAssetName);
            return resUrhoAssetName.Substring(0, resUrhoAssetName.Length - ext.Length) + newExt;
        }
    }
}