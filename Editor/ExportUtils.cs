using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace UnityToCustomEngineExporter.Editor
{
    public static class ExportUtils
    {
        public static string GetGUID(this Object asset)
        {
            if (asset == null)
                return "";
            var assetPath = AssetDatabase.GetAssetPath(asset);
            if (string.IsNullOrWhiteSpace(assetPath))
                return "";
            return AssetDatabase.AssetPathToGUID(assetPath);
        }

        public static AssetKey GetKey(this Object asset)
        {
            if (asset == null)
                return AssetKey.Empty;
            if (AssetDatabase.TryGetGUIDAndLocalFileIdentifier(asset, out var guid, out long localId))
                return new AssetKey(guid, localId);
            var assetPath = AssetDatabase.GetAssetPath(asset);
            if (string.IsNullOrWhiteSpace(assetPath))
                return AssetKey.Empty;
            return new AssetKey(AssetDatabase.AssetPathToGUID(assetPath), 0);
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

        public static string GetRelPathFromAssetPath(string subfolder, string assetPath)
        {
            var result = assetPath;
            if (result.StartsWith("Assets/", StringComparison.InvariantCultureIgnoreCase))
                result = result.Substring("Assets/".Length);
            if (!string.IsNullOrWhiteSpace(subfolder))
            {
                if (subfolder.EndsWith("/"))
                    result = subfolder + result;
                else
                    result = subfolder + "/" + result;
            }

            return result;
        }

        public static string GetRelPathFromAsset(string subfolder, Object asset)
        {
            if (asset == null)
                return null;
            var path = AssetDatabase.GetAssetPath(asset);
            return GetRelPathFromAssetPath(subfolder, path);
        }

        public static string GetRelPathFromAsset(string subfolder, Scene asset)
        {
            var path = asset.path;
            return GetRelPathFromAssetPath(subfolder, path);
        }

        public static DateTime GetLastWriteTimeUtc(Object asset)
        {
            if (asset == null)
                return DateTime.MinValue;
            var relPath = GetRelPathFromAsset(null, asset);
            return GetLastWriteTimeUtcFromRelPath(relPath);
        }

        public static DateTime GetLastWriteTimeUtc(params Object[] assets)
        {
            if (assets == null)
                return DateTime.MinValue;
            return MaxDateTime(assets.Select(_ => GetLastWriteTimeUtc(_)).ToArray());
        }

        public static string SafeFileName(string name)
        {
            if (string.IsNullOrEmpty(name)) return name;
            foreach (var invalidFileNameChar in Path.GetInvalidFileNameChars())
                name = name.Replace(invalidFileNameChar, '_');

            return name;
        }

        public static DateTime GetLastWriteTimeUtc(string assetPath)
        {
            var relPath = GetRelPathFromAssetPath("", assetPath);
            return GetLastWriteTimeUtcFromRelPath(relPath);
        }

        public static DateTime MaxDateTime(params DateTime[] dateTimes)
        {
            var max = DateTime.MinValue;
            foreach (var dateTime in dateTimes)
                if (dateTime > max)
                    max = dateTime;

            return max;
        }

        public static Object[] LoadAllAssetsAtPath(string assetPath)
        {
            return typeof(SceneAsset).Equals(AssetDatabase.GetMainAssetTypeAtPath(assetPath))
                ? new[] {AssetDatabase.LoadMainAssetAtPath(assetPath)}
                : AssetDatabase.LoadAllAssetsAtPath(assetPath);
        }

        public static TextureOptions GetTextureOptions(Texture texture)
        {
            var assetPath = AssetDatabase.GetAssetPath(texture);
            var tImporter = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (tImporter != null)
            {
                var options = new TextureOptions();
                var type = tImporter.textureType;
                options.sRGBTexture = type == TextureImporterType.Default ? tImporter.sRGBTexture : false;
                options.filterMode = tImporter.filterMode;
                options.wrapMode = tImporter.wrapMode;
                options.mipmapEnabled = tImporter.mipmapEnabled;
                options.textureImporterFormat = tImporter.GetAutomaticFormat("Standalone");
                return options;
            }

            // Default texture options.
            return new TextureOptions()
            {
                filterMode = FilterMode.Trilinear,
                mipmapEnabled = true,
                sRGBTexture = true,
                textureImporterFormat = null,
                wrapMode = TextureWrapMode.Repeat
            };
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

        public static string Combine(params string[] segments)
        {
            var path = new StringBuilder();
            var separator = "";
            foreach (var segment in segments)
            {
                if (string.IsNullOrWhiteSpace(segment))
                {
                    continue;
                }

                path.Append(separator);
                separator = "/";

                path.Append(segment);

                if (segment.EndsWith("/"))
                    separator = "";
            }
            return path.ToString();
        }
    }
}