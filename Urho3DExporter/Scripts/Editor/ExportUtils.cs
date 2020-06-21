using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

namespace Assets.Scripts.UnityToCustomEngineExporter.Editor
{
    public static class ExportUtils
    {
        static ExportUtils()
        {
        }

        public static string ReplaceExtension(string assetUrhoAssetName, string newExt)
        {
            if (assetUrhoAssetName == null)
                return null;
            var lastDot = assetUrhoAssetName.LastIndexOf('.');
            var lastSlash = assetUrhoAssetName.LastIndexOf('/');
            if (lastDot > lastSlash) return assetUrhoAssetName.Substring(0, lastDot) + newExt;

            return assetUrhoAssetName + newExt;
        }

        public static DateTime GetLastWriteTimeUtc(UnityEngine.Object asset)
        {
            var relPath = AssetInfo.GetRelPathFromAsset(asset);
            return GetLastWriteTimeUtcFromRelPath(relPath);
        }

        public static string SafeFileName(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                return name;
            }
            foreach (var invalidFileNameChar in Path.GetInvalidFileNameChars())
            {
                name = name.Replace(invalidFileNameChar, '_');
            }

            return name;
        }

        private static DateTime GetLastWriteTimeUtcFromRelPath(string relPath)
        {
            if (string.IsNullOrWhiteSpace(relPath))
                return DateTime.MaxValue;
            var file = Path.Combine(Application.dataPath, relPath);
            if (!File.Exists(file))
                return DateTime.MaxValue;
            return File.GetLastWriteTimeUtc(file);
        }

        public static DateTime GetLastWriteTimeUtc(string assetPath)
        {
            var relPath = AssetInfo.GetRelPathFromAssetPath(assetPath);
            return GetLastWriteTimeUtcFromRelPath(relPath);
        }
    }
}
