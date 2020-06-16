using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using UnityEditor;
using UnityEngine;

namespace Urho3DExporter
{
    public class AssetContext
    {
        private static readonly HashSet<string> _supported3DFormats = new HashSet<string>
        {
            ".fbx",
            ".obj",
            ".dae",
            ".3ds",
            ".dxf",
            ".max",
            ".ma",
            ".mb",
            ".blend"
        };

        private string _contentFolderName;
        private string _urhoFileName;
        private string _fullPath;
        public string UrhoAssetName { get; private set; }

        public string FullPath
        {
            get => _fullPath;
            private set => _fullPath = value.FixDirectorySeparator();
        }

        public string RelPath { get; private set; }

        public Type Type { get; private set; }

        public string AssetPath { get; private set; }

        public string Guid { get; private set; }

        public string ContentFolderName
        {
            get => _contentFolderName;
            private set => _contentFolderName = value.FixDirectorySeparator();
        }

        public bool Is3DAsset { get; private set; }

        public DestinationFolder DestinationFolder { get; set; }

        public string FileExtension { get; private set; }

        public DateTime LastWriteTimeUtc { get; set; } = DateTime.MaxValue;

        public static string GetRelPathFromAssetPath(string assetPath)
        {
            if (assetPath.StartsWith("Assets/", StringComparison.InvariantCultureIgnoreCase))
                return assetPath.Substring("Assets/".Length);
            return assetPath;
        }

        public static AssetContext Create(string guid, DestinationFolder urhoDataFolder)
        {
            var res = new AssetContext
            {
                Guid = guid
            };
            res.AssetPath = AssetDatabase.GUIDToAssetPath(guid);
            if (!string.IsNullOrEmpty(res.AssetPath))
            {
                res.Type = AssetDatabase.GetMainAssetTypeAtPath(res.AssetPath);
                res.RelPath = GetRelPathFromAssetPath(res.AssetPath);
                res.FullPath = Path.Combine(Application.dataPath, res.RelPath);
                if (File.Exists(res.FullPath))
                {
                    res.LastWriteTimeUtc = File.GetLastWriteTimeUtc(res.FullPath);
                }
                res.FileExtension = Path.GetExtension(res.AssetPath).ToLower();
                res.UrhoAssetName = res.RelPath;
                if (res.Type == typeof(Material))
                {
                    res.UrhoAssetName = RepaceExtension(res.UrhoAssetName, ".xml");
                }
                else if (res.Type == typeof(Mesh))
                {
                    res.UrhoAssetName = RepaceExtension(res.UrhoAssetName, ".mdl");
                    res.Is3DAsset = true;
                }
                else if (res.Type == typeof(GameObject))
                {
                    res.UrhoAssetName = RepaceExtension(res.UrhoAssetName, ".xml");
                    res.Is3DAsset = DetectMeshAsset(res);
                }
                else if (res.Type == typeof(SceneAsset))
                {
                    res.UrhoAssetName = RepaceExtension(res.UrhoAssetName, ".xml");
                }
                else if (res.Type == typeof(Cubemap))
                {
                    res.UrhoAssetName = RepaceExtension(res.UrhoAssetName, ".xml");
                }

                res.DestinationFolder = urhoDataFolder;
                {
                    var dotIndex = res.UrhoAssetName.LastIndexOf('.');
                    var lastSlash = res.UrhoAssetName.LastIndexOf('/');
                    if (dotIndex > lastSlash)
                        res.ContentFolderName = res.UrhoAssetName.Substring(0, dotIndex);
                    else
                        res.ContentFolderName = res.UrhoAssetName + ".Content";
                }
            }

            return res;
        }

        private static bool DetectMeshAsset(AssetContext res)
        {
            if (string.Equals(res.FileExtension, ".asset", StringComparison.InvariantCultureIgnoreCase))
                if (res.Type == typeof(Mesh))
                    return true;
            return _supported3DFormats.Contains(res.FileExtension);
        }

        private static string RepaceExtension(string resUrhoAssetName, string newExt)
        {
            var ext = Path.GetExtension(resUrhoAssetName);
            return resUrhoAssetName.Substring(0, resUrhoAssetName.Length - ext.Length) + newExt;
        }

        public XmlTextWriter CreateXml()
        {
            return DestinationFolder.CreateXml(UrhoAssetName, DateTime.MaxValue);
        }

        public static string ReplaceExt(string path, string newExt)
        {
            var dot = path.LastIndexOf('.');
            var slash = Math.Max(path.LastIndexOf('/'), path.LastIndexOf('\\'));
            if (dot <= slash)
                return path + newExt;
            return path.Substring(0, dot) + newExt;
        }
    }
}