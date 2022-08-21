using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace UnityToCustomEngineExporter.Editor
{
    public class RearrangeAssets
    {
        [MenuItem("Tools/Export To Custom Engine/Rearrange Assets")]
        public static void Rearrange()
        {
            HashSet<string> allAssetTypes = new HashSet<string>();
            var assetsFolder = "Assets/";
            foreach (var assetPath in AssetDatabase.GetAllAssetPaths().Where(_=>_.StartsWith(assetsFolder)))
            {
                var mainAssetType = AssetDatabase.GetMainAssetTypeAtPath(assetPath);

                if (typeof(Texture).IsAssignableFrom(mainAssetType))
                {
                    MoveIfNecessary(assetsFolder, "Textures/", assetPath);
                }
                else if (typeof(Material).IsAssignableFrom(mainAssetType))
                {
                    MoveIfNecessary(assetsFolder, "Materials/", assetPath);
                }
                else if (typeof(GameObject).IsAssignableFrom(mainAssetType))
                {
                    MoveIfNecessary(assetsFolder, "Models/", assetPath);
                }
                else if (typeof(DefaultAsset).IsAssignableFrom(mainAssetType))
                {
                }
                else if (typeof(MonoScript).IsAssignableFrom(mainAssetType))
                {
                }
                else if (typeof(TextAsset).IsAssignableFrom(mainAssetType))
                {
                }
                else if (typeof(Shader).IsAssignableFrom(mainAssetType))
                {
                }
                else if (typeof(SceneAsset).IsAssignableFrom(mainAssetType))
                {
                }
                else if (typeof(LightingDataAsset).IsAssignableFrom(mainAssetType))
                {
                }
                else
                {
                    if (allAssetTypes.Add(mainAssetType.Name))
                    {
                        Debug.Log(mainAssetType.Name);
                    }
                }
            }
        }

        private static void MoveIfNecessary(string assetsFolder, string subfolder, string assetPath)
        {
            var texturePath = assetsFolder + subfolder;
            var dataPath = Application.dataPath;
            if (!assetPath.StartsWith(texturePath))
            {
                var subPath = assetPath.Substring(assetsFolder.Length);
                var newPath = texturePath + subPath;
                var folderName = Path.GetDirectoryName(Path.Combine(dataPath, subfolder, subPath));
                Directory.CreateDirectory(folderName);
                var errMessage = AssetDatabase.MoveAsset(assetPath, newPath);
                if (!string.IsNullOrWhiteSpace(errMessage))
                    Debug.Log(errMessage);
            }
        }
    }
}