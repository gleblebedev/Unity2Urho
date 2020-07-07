using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace UnityToCustomEngineExporter.Editor
{
    public class FixTexureImportOptions : EditorWindow
    {
        //[MenuItem("Assets/Export/Fix texture import settings (sRGB, etc)")]
        public static void FixTextureSettings()
        {
            var window = (FixTexureImportOptions)GetWindow(typeof(FixTexureImportOptions));
            window.Show();
            EditorTaskScheduler.Default.ScheduleForegroundTask(()=>window.FetchAllMaterials());
        }

        private HashSet<Material> _visitedMaterials = new HashSet<Material>();
        private HashSet<Texture> _visitedTextures = new HashSet<Texture>();
        private int _assetCount;
        private int _assetIndex;

        private IEnumerable<ProgressBarReport> FetchAllMaterials()
        {
            try
            {
                var assets = AssetDatabase.FindAssets("");
                _assetCount = assets.Length;
                for (_assetIndex = 0; _assetIndex < assets.Length; _assetIndex++)
                {
                    var asset = assets[_assetIndex];
                    var path = AssetDatabase.GUIDToAssetPath(asset);
                    yield return path;
                    Object[] assetsAtPath = null;
                    assetsAtPath = ExportUtils.LoadAllAssetsAtPath(path);

                    foreach (var material in assetsAtPath.OfType<Material>())
                    {
                        if (_visitedMaterials.Add(material))
                        {
                            var materialDescription = new MaterialDescription(material);
                            if (materialDescription.MetallicGlossiness != null)
                            {
                                DropSRGBFlag(materialDescription.MetallicGlossiness.MetallicGloss);
                                EnsureNormalMap(materialDescription.MetallicGlossiness.Bump);
                            }

                            if (materialDescription.SpecularGlossiness != null)
                            {
                                DropSRGBFlag(materialDescription.SpecularGlossiness.PBRSpecular.Texture);
                                EnsureNormalMap(materialDescription.SpecularGlossiness.Bump);
                            }

                            if (materialDescription.Legacy != null)
                            {
                                EnsureNormalMap(materialDescription.Legacy.Bump);
                            }
                        }
                    }
                }
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }

            this.Close();
        }

        private void EnsureNormalMap(Texture texture)
        {
            UpdateImporter(texture, (importer) =>
            {
                if (importer.textureType != TextureImporterType.NormalMap)
                {
                    Debug.Log("Reimport texture " + AssetDatabase.GetAssetPath(texture) + "");
                    importer.textureType = TextureImporterType.NormalMap;
                    importer.sRGBTexture = false;
                    importer.SaveAndReimport();
                }
            });
        }

        private void UpdateImporter(Texture texture, Action<TextureImporter> updateAction)
        {
            if (!_visitedTextures.Contains(texture))
                return;
            var assetPath = AssetDatabase.GetAssetPath(texture);
            if (string.IsNullOrWhiteSpace(assetPath))
                return;
            var importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (importer == null)
            {
                Debug.LogWarning("Texture at " + assetPath + " doesn't have importer");
                return;
            }

            updateAction(importer);
        }

        private void DropSRGBFlag(Texture texture)
        {
            UpdateImporter(texture, (importer) =>
            {
                if (importer.sRGBTexture != false)
                {
                    Debug.Log("Reimport texture " + AssetDatabase.GetAssetPath(texture) + "");
                    importer.sRGBTexture = false;
                    importer.SaveAndReimport();
                }
            });
        }

        public void OnGUI()
        {
            EditorUtility.DisplayProgressBar("Hold on...", EditorTaskScheduler.Default.CurrentReport.Message, _assetIndex/(float)_assetCount);
        }
    }
}