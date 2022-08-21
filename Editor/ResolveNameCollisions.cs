using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace UnityToCustomEngineExporter.Editor
{
    public class ResolveNameCollisions
    {
        struct AssetRef
        {
            public Type MainType;
            public string Path;
            public string Ext;
        }
        [MenuItem("Tools/Export To Custom Engine/Resolve name collisions")]
        public static void Rearrange()
        {
            HashSet<string> allAssetTypes = new HashSet<string>();
            var assetsFolder = "Assets/";
            var existingAssets = AssetDatabase.GetAllAssetPaths().Where(_ => _.StartsWith(assetsFolder)).Select(_ =>
                new AssetRef {Path = _, Ext = Path.GetExtension(_), MainType = AssetDatabase.GetMainAssetTypeAtPath(_)}).ToLookup(_=>GetKeyFromPath(_.Path));

            var problematicAssets = existingAssets.Where(_ => _.Count() > 1).ToList();
            
            var renamedAssets = new Dictionary<string, AssetRef>();
            ResolveCollisionsForType(existingAssets, problematicAssets, typeof(Texture), renamedAssets, null);
            ResolveCollisionsForType(existingAssets, problematicAssets, typeof(Material), renamedAssets, "Mat");

        }

        private static void ResolveCollisionsForType(ILookup<string, AssetRef> existingAssets,
            List<IGrouping<string, AssetRef>> problematicAssets, Type type, Dictionary<string, AssetRef> renamedAssets,
            string suffix)
        {
            foreach (var existingAsset in problematicAssets.SelectMany(_=>_.Select(_1=>new KeyValuePair<string, AssetRef>(_.Key, _1))).Where(_ => type.IsAssignableFrom(_.Value.MainType)))
            {
                ResolveCollision(existingAssets, renamedAssets, suffix ?? existingAsset.Value.Ext.Trim('.'), existingAsset);
            }
        }

        private static void ResolveCollision(ILookup<string, AssetRef> existingAssets, Dictionary<string, AssetRef> renamedAssets, string suffix,
            KeyValuePair<string, AssetRef> existingAsset)
        {
            var assetKey = existingAsset.Key;
            if (renamedAssets.ContainsKey(assetKey))
            {
                var assetPath = existingAsset.Value.Path;
                if (!string.IsNullOrWhiteSpace(suffix))
                {
                    var newName = AddSuffix(assetPath, suffix);
                    var newKey = GetKeyFromPath(newName);
                    if (!existingAssets.Contains(newKey) && !renamedAssets.ContainsKey(newKey))
                    {
                        AssetDatabase.MoveAsset(existingAsset.Value.Path, newName);
                        renamedAssets.Add(newKey, existingAsset.Value);
                        return;
                    }

                    assetPath = newName;
                }

                for (int i = 2;; ++i)
                {
                    var newName = AddSuffix(assetPath, i.ToString(CultureInfo.InvariantCulture));
                    var newKey = GetKeyFromPath(newName);
                    if (!existingAssets.Contains(newKey) && !renamedAssets.ContainsKey(newKey))
                    {
                        AssetDatabase.MoveAsset(existingAsset.Value.Path, newName);
                        renamedAssets.Add(newKey, existingAsset.Value);
                        return;
                    }
                }
            }
            else
            {
                renamedAssets.Add(assetKey, existingAsset.Value);
            }
        }

        private static string GetKeyFromPath(string path)
        {
            return path.Substring(0, path.Length - Path.GetExtension(path).Length).ToLower();
        }

        private static string AddSuffix(string path, string suffix)
        {
            var ext = Path.GetExtension(path);
            return path.Substring(0, path.Length - ext.Length) + suffix + ext;
        }
    }
}