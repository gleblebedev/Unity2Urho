using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

namespace UnityToCustomEngineExporter.Editor
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

        public static DateTime GetLastWriteTimeUtc(UnityEngine.Object asset)
        {
            if (asset == null)
                return DateTime.MinValue;
            var relPath = GetRelPathFromAsset(asset);
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
            var relPath = GetRelPathFromAssetPath(assetPath);
            return GetLastWriteTimeUtcFromRelPath(relPath);
        }

        public static DateTime MaxDateTime(params DateTime[] dateTimes)
        {
            var max = DateTime.MinValue;
            foreach (var dateTime in dateTimes)
            {
                if (dateTime > max)
                    max = dateTime;
            }

            return max;
        }
    }
}
