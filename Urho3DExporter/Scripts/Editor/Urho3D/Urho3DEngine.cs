using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Xml;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace Assets.Scripts.UnityToCustomEngineExporter.Editor.Urho3D
{
    public class Urho3DEngine : AbstractDestinationEngine, IDestinationEngine
    {
        private readonly string _dataFolder;
        private readonly bool _exportUpdatedOnly;
        Dictionary<Object, string> _assetPaths = new Dictionary<Object, string>();
        private HashSet<string> _createdFiles = new HashSet<string>();
        private TextureExporter _textureExporter;
        private CubemapExporter _cubemapExporter;
        private MeshExporter _meshExporter;
        private MaterialExporter _materialExporter;
        private SceneExporter _sceneExporter;
        private PrefabExporter _prefabExporter;
        private TerrainExporter _terrainExporter;

        public Urho3DEngine(string dataFolder, CancellationToken cancellationToken, bool exportUpdatedOnly, bool exportSceneAsPrefab, bool skipDisabled)
            : base(cancellationToken)
        {
            _dataFolder = dataFolder;
            _exportUpdatedOnly = exportUpdatedOnly;
            _textureExporter = new TextureExporter(this);
            _cubemapExporter = new CubemapExporter(this);
            _meshExporter = new MeshExporter(this);
            _materialExporter = new MaterialExporter(this);
            _sceneExporter = new SceneExporter(this, exportSceneAsPrefab, skipDisabled);
            _prefabExporter = new PrefabExporter(this, skipDisabled);
            _terrainExporter = new TerrainExporter(this);
        }

        public string GetAssetId(Object asset)
        {
            if (_assetPaths.TryGetValue(asset, out var path))
            {
                return path;
            }
            string fileSystemPath = AssetDatabase.GetAssetPath(asset);

            var relPath = fileSystemPath;

            var assetsPrefix = "Assets/";
            if (relPath.StartsWith(assetsPrefix, StringComparison.InvariantCultureIgnoreCase))
                relPath = relPath.Substring(assetsPrefix.Length);

            var assets = AssetDatabase.LoadAllAssetsAtPath(fileSystemPath);
            foreach (var subAsset in assets)
            {
                var subAssetPath = ResolveAssetPath(relPath, subAsset, assets.Length != 1);
                _assetPaths[subAsset] = subAssetPath;
                if (asset == subAsset)
                {
                    path = subAssetPath;
                }
            }

            return path;
        }

        private string ResolveAssetPath(string relPath, Object asset, bool inPrefab)
        {
            return null;
            //if (asset is Texture2D texture2D)
            //{
            //    return _textureExporter.ResolveAssetPath(relPath, texture2D, inPrefab);
            //}
            //if (asset is Cubemap cubemap)
            //{
            //    return _cubemapExporter.ResolveAssetPath(relPath, cubemap, inPrefab);
            //}
            //if (asset is Mesh mesh)
            //{
            //    return _meshExporter.ResolveAssetPath(relPath, mesh, inPrefab);
            //}
            //if (asset is Material material)
            //{
            //    return _meshExporter.ResolveAssetPath(relPath, material, inPrefab);
            //}
            //if (asset is GameObject gameObject)
            //{
            //    if (relPath.EndsWith(".unity", StringComparison.InvariantCultureIgnoreCase))
            //    {
            //        return _sceneExporter.ResolveAssetPath(relPath, gameObject, inPrefab);
            //    }
            //    else
            //    {
            //        return _prefabExporter.ResolveAssetPath(relPath, gameObject, inPrefab);
            //    }
            //}

            return BuildAssetPath(relPath, asset.name, inPrefab, Path.GetExtension(relPath));
        }

        public static string BuildAssetPath(string relPath, string name, bool inPrefab, string newExtension)
        {
            if (inPrefab)
            {
                if (name != null)
                {
                    foreach (var invalidFileNameChar in Path.GetInvalidFileNameChars())
                    {
                        name = name.Replace(invalidFileNameChar, '_');
                    }
                }

                return ExportUtils.ReplaceExtension(relPath, "/" + name + newExtension);
            }

            return ExportUtils.ReplaceExtension(relPath, newExtension);
        }



        public void Dispose()
        {

        }

        public string GetTargetFilePath(string relativePath)
        {
            return Path.Combine(_dataFolder, relativePath.FixDirectorySeparator()).FixDirectorySeparator();
        }

        public void TryCopyFile(string assetPath, string destinationFilePath)
        {
            if (destinationFilePath == null)
                return;

            var sourceFilePath = Path.Combine(Application.dataPath, ExportUtils.GetRelPathFromAssetPath(assetPath));
            if (!File.Exists(sourceFilePath))
                return;
            var targetPath = GetTargetFilePath(destinationFilePath);

            //Skip file if it already exported
            if (!_createdFiles.Add(targetPath))
            {
                return;
            }

            //Skip file if it is already up to date
            if (_exportUpdatedOnly)
            {
                if (File.Exists(targetPath))
                {
                    var sourceLastWriteTimeUtc = File.GetLastWriteTimeUtc(sourceFilePath);
                    var lastWriteTimeUtc = File.GetLastWriteTimeUtc(targetPath);
                    if (sourceLastWriteTimeUtc <= lastWriteTimeUtc)
                        return;
                }
            }

            var directoryName = Path.GetDirectoryName(targetPath);
            if (directoryName != null) Directory.CreateDirectory(directoryName);

            File.Copy(sourceFilePath, targetPath, true);
        }
        public FileStream TryCreate(string relativePath, DateTime sourceFileTimestampUTC)
        {
            if (relativePath == null)
            {
                return null;
            }

            var targetPath = GetTargetFilePath(relativePath);

            //Skip file if it already exported
            if (!_createdFiles.Add(targetPath))
            {
                return null;
            }

            //Skip file if it is already up to date
            if (_exportUpdatedOnly)
            {
                if (File.Exists(targetPath))
                {
                    var lastWriteTimeUtc = File.GetLastWriteTimeUtc(targetPath);
                    if (sourceFileTimestampUTC <= lastWriteTimeUtc)
                        return null;
                }
            }

            var directoryName = Path.GetDirectoryName(targetPath);
            if (directoryName != null) Directory.CreateDirectory(directoryName);

            return File.Open(targetPath, FileMode.Create, FileAccess.Write, FileShare.Read);
        }

        public XmlWriter TryCreateXml(string relativePath, DateTime sourceFileTimestampUTC)
        {
            var fileStream = TryCreate(relativePath, sourceFileTimestampUTC);
            if (fileStream == null)
                return null;
            return new XmlTextWriter(fileStream, new UTF8Encoding(false));
        }

        protected override void ExportAssetBlock(string assetPath, Type mainType, Object[] assets)
        {
            if (mainType == typeof(GameObject))
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
                _meshExporter.ExportMesh(prefab);
                _prefabExporter.ExportPrefab(_prefabExporter.EvaluatePrefabName(assetPath), prefab);
            }
            else
            {
                foreach (var asset in assets)
                {
                    if (asset is Mesh mesh)
                    {
                        EditorTaskScheduler.Default.ScheduleForegroundTask(() => _meshExporter.ExportMeshModel(mesh, null), mesh.name + " from " + assetPath);
                    }
                }
            }

            foreach (var asset in assets)
            {
                if (asset is Mesh mesh)
                {
                    //We already processed all meshes.
                }
                else if (asset is GameObject gameObject)
                {
                    //We already processed prefab.
                }
                else if (asset is Material material)
                {
                    EditorTaskScheduler.Default.ScheduleForegroundTask(() => _materialExporter.ExportMaterial(material), material.name + " from " + assetPath);
                }
                else if (asset is TerrainData terrainData)
                {
                    EditorTaskScheduler.Default.ScheduleForegroundTask(() => _terrainExporter.ExportTerrain(terrainData), terrainData.name + " from " + assetPath);
                }
                else if (asset is Texture2D texture2d)
                {
                    EditorTaskScheduler.Default.ScheduleForegroundTask(() => _textureExporter.ExportTexture(texture2d, new TextureReference(TextureSemantic.Other)), texture2d.name + " from " + assetPath);
                }
                else if (asset is Cubemap cubemap)
                {
                    EditorTaskScheduler.Default.ScheduleForegroundTask(() => _cubemapExporter.Cubemap(cubemap), cubemap.name + " from " + assetPath);
                }
                else
                {
                    Debug.LogWarning("UnknownAssetType " + asset.GetType().Name);
                }
            }
        }

        public void ExportScene(Scene scene)
        {
            _sceneExporter.ExportScene(scene);
        }

        public void ScheduleTexture(Texture texture, TextureReference textureReference = null)
        {
            if (texture == null)
            {
                return;
            }
            EditorTaskScheduler.Default.ScheduleForegroundTask(() => _textureExporter.ExportTexture(texture, textureReference), texture.name + " from " + AssetDatabase.GetAssetPath(texture));
        }

        public string EvaluateCubemapName(Cubemap cubemap)
        {
            return _cubemapExporter.EvaluateCubemapName(cubemap);
        }

        public string EvaluateTextrueName(Texture texture)
        {
            if (texture is Cubemap cubemap)
            {
                return EvaluateCubemapName(cubemap);
            }

            return EvaluateTextrueName(texture, new TextureReference(TextureSemantic.Other));
        }
        public string EvaluateTextrueName(Texture texture, TextureReference textureReference)
        {
            return _textureExporter.EvaluateTextrueName(texture, textureReference);
        }
        public string EvaluateMaterialName(Material skyboxMaterial)
        {
            return _materialExporter.EvaluateMaterialName(skyboxMaterial);
        }

        public string EvaluateMeshName(Mesh sharedMesh)
        {
            return _meshExporter.EvaluateMeshName(sharedMesh);
        }

        public string EvaluateTerrainHeightMap(TerrainData terrainData)
        {
            return _terrainExporter.EvaluateHeightMap(terrainData);
        }

        public string EvaluateTerrainMaterial(TerrainData terrainData)
        {
            return _terrainExporter.EvaluateMaterial(terrainData);
        }
    }
}