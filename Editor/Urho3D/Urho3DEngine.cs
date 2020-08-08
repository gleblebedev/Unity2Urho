using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Xml;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.ProBuilder;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace UnityToCustomEngineExporter.Editor.Urho3D
{
    public class Urho3DEngine : AbstractDestinationEngine, IDestinationEngine
    {
        private readonly string _dataFolder;
        private readonly bool _exportUpdatedOnly;
        private readonly Dictionary<string, AssetKey> _createdFiles = new Dictionary<string, AssetKey>();
        private readonly TextureExporter _textureExporter;
        private readonly CubemapExporter _cubemapExporter;
        private readonly MeshExporter _meshExporter;
        private readonly AnimationExporter _animationExporter;
        private readonly AnimationControllerExporter _animationControllerExporter;
        private readonly MaterialExporter _materialExporter;
        private readonly AudioExporter _audioExporter;
        private readonly SceneExporter _sceneExporter;
        private readonly PrefabExporter _prefabExporter;
        private readonly TerrainExporter _terrainExporter;
        private Dictionary<Object, string> _assetPaths = new Dictionary<Object, string>();
        private string _tempFolder;
        private Urho3DExportOptions _options;

        public Urho3DEngine(string dataFolder, CancellationToken cancellationToken,
            Urho3DExportOptions options)
            : base(cancellationToken)
        {
            _dataFolder = dataFolder;

            _options = options;


            _audioExporter = new AudioExporter(this);
            _textureExporter = new TextureExporter(this);
            _cubemapExporter = new CubemapExporter(this);
            _meshExporter = new MeshExporter(this);
            _materialExporter = new MaterialExporter(this);
            _sceneExporter = new SceneExporter(this);
            _prefabExporter = new PrefabExporter(this);
            _terrainExporter = new TerrainExporter(this);
            _animationExporter = new AnimationExporter(this);
            _animationControllerExporter = new AnimationControllerExporter(this);
            CopyFolder(options.Subfolder, "bcc1b6196266be34e88c40110ba206ce");
            if (_options.ExportShadersAndTechniques)
                CopyFolder("", "a20749a09ce562043815b33e8eec4077");
            _createdFiles.Clear();
        }

        public Urho3DExportOptions Options => _options;


        public void CopyFolder(string subfolder, string guid)
        {
            var assetsPath = AssetDatabase.GUIDToAssetPath(guid);
            var rootPath = Path.GetDirectoryName(Application.dataPath) + Path.DirectorySeparatorChar;
            var sourceFolderPath = Path.Combine(rootPath, assetsPath) + Path.DirectorySeparatorChar;
            foreach (var file in Directory.GetFiles(sourceFolderPath, "*", SearchOption.AllDirectories))
            {
                if (string.Equals(Path.GetExtension(file), ".Meta", StringComparison.InvariantCultureIgnoreCase))
                    continue;
                var target = file.Substring(sourceFolderPath.Length).FixAssetSeparator();
                if (!string.IsNullOrWhiteSpace(target))
                {
                    if (!string.IsNullOrWhiteSpace(subfolder)) target = subfolder + target;

                    var assetPath = file.Substring(rootPath.Length);
                    var sourceFilePath = Path.Combine(Path.GetDirectoryName(Application.dataPath), assetPath);
                    if (File.Exists(sourceFilePath))
                    {
                        var targetPath = GetTargetFilePath(target);
                        if (_exportUpdatedOnly)
                            if (File.Exists(targetPath))
                            {
                                var sourceLastWriteTimeUtc = File.GetLastWriteTimeUtc(sourceFilePath);
                                var lastWriteTimeUtc = File.GetLastWriteTimeUtc(targetPath);
                                if (sourceLastWriteTimeUtc <= lastWriteTimeUtc) continue;
                            }

                        var directoryName = Path.GetDirectoryName(targetPath);
                        if (directoryName != null) Directory.CreateDirectory(directoryName);

                        File.Copy(sourceFilePath, targetPath, true);
                    }
                }
            }
        }

        public string GetTargetFilePath(string relativePath)
        {
            return Path.Combine(_dataFolder, relativePath.FixDirectorySeparator()).FixDirectorySeparator();
        }

        public void TryWriteFile(AssetKey assetGuid, string destinationFilePath, byte[] bytes,
            DateTime sourceLastWriteTimeUtc)
        {
            if (destinationFilePath == null)
                return;

            var targetPath = GetTargetFilePath(destinationFilePath);

            //Skip file if it already exported
            if (!CheckForFileUniqueness(targetPath, assetGuid)) return;

            //Skip file if it is already up to date
            if (_exportUpdatedOnly)
                if (File.Exists(targetPath))
                {
                    var lastWriteTimeUtc = File.GetLastWriteTimeUtc(targetPath);
                    if (sourceLastWriteTimeUtc <= lastWriteTimeUtc)
                        return;
                }

            var directoryName = Path.GetDirectoryName(targetPath);
            if (directoryName != null) Directory.CreateDirectory(directoryName);

            File.WriteAllBytes(targetPath, bytes);
        }

        public void TryCopyFile(string assetPath, string destinationFilePath)
        {
            if (destinationFilePath == null)
                return;

            var sourceFilePath = Path.Combine(Path.GetDirectoryName(Application.dataPath), assetPath);
            if (!File.Exists(sourceFilePath))
                return;
            var targetPath = GetTargetFilePath(destinationFilePath);

            //Skip file if it already exported
            if (!CheckForFileUniqueness(targetPath, new AssetKey(AssetDatabase.AssetPathToGUID(assetPath), 0))) return;

            //Skip file if it is already up to date
            if (_exportUpdatedOnly)
                if (File.Exists(targetPath))
                {
                    var sourceLastWriteTimeUtc = File.GetLastWriteTimeUtc(sourceFilePath);
                    var lastWriteTimeUtc = File.GetLastWriteTimeUtc(targetPath);
                    if (sourceLastWriteTimeUtc <= lastWriteTimeUtc)
                        return;
                }

            var directoryName = Path.GetDirectoryName(targetPath);
            if (directoryName != null) Directory.CreateDirectory(directoryName);

            File.Copy(sourceFilePath, targetPath, true);
        }

        public bool IsUpToDate(AssetKey assetGuid, string relativePath, DateTime sourceFileTimestampUTC)
        {
            if (relativePath == null) return true;
            var targetPath = GetTargetFilePath(relativePath);

            //Skip file if it already exported
            if (_createdFiles.TryGetValue(targetPath, out var existingAsset))
            {
                if (existingAsset != assetGuid)
                {
                    Debug.LogError("Asset file name collision: " + AssetDatabase.GUIDToAssetPath(assetGuid.Guid) +
                                   " and " + AssetDatabase.GUIDToAssetPath(existingAsset.Guid) +
                                   " attempt to write into same file " + targetPath);
                    return true;
                }

                return true;
            }

            //Skip file if it is already up to date
            if (_exportUpdatedOnly)
                if (File.Exists(targetPath))
                {
                    var lastWriteTimeUtc = File.GetLastWriteTimeUtc(targetPath);
                    if (sourceFileTimestampUTC <= lastWriteTimeUtc)
                        return true;
                }

            return false;
        }

        public FileStream TryCreate(AssetKey assetGuid, string relativePath, DateTime sourceFileTimestampUTC)
        {
            if (IsUpToDate(assetGuid, relativePath, sourceFileTimestampUTC)) return null;
            var targetPath = GetTargetFilePath(relativePath);

            //Skip file if it already exported
            if (!CheckForFileUniqueness(targetPath, assetGuid)) return null;

            var directoryName = Path.GetDirectoryName(targetPath);
            if (directoryName != null) Directory.CreateDirectory(directoryName);

            return File.Open(targetPath, FileMode.Create, FileAccess.Write, FileShare.Read);
        }

        public XmlWriter TryCreateXml(AssetKey assetGuid, string relativePath, DateTime sourceFileTimestampUTC)
        {
            var fileStream = TryCreate(assetGuid, relativePath, sourceFileTimestampUTC);
            if (fileStream == null)
                return null;
            return new XmlTextWriter(fileStream, new UTF8Encoding(false));
        }

        public void ScheduleTexture(Texture texture, TextureReference textureReference = null)
        {
            if (texture == null) return;
            EditorTaskScheduler.Default.ScheduleForegroundTask(
                () => _textureExporter.ExportTexture(texture, textureReference),
                texture.name + " from " + AssetDatabase.GetAssetPath(texture));
        }

        public string EvaluateCubemapName(Cubemap cubemap)
        {
            return _cubemapExporter.EvaluateCubemapName(cubemap);
        }

        public string EvaluateTextrueName(Texture texture)
        {
            if (texture == null)
                return null;

            if (texture is Cubemap cubemap) return EvaluateCubemapName(cubemap);

            return EvaluateTextrueName(texture, new TextureReference(TextureSemantic.Other));
        }

        public string EvaluateTextrueName(Texture texture, TextureReference textureReference)
        {
            if (texture == null)
                return null;
            return _textureExporter.EvaluateTextureName(texture, textureReference);
        }

        public string EvaluateMaterialName(Material skyboxMaterial)
        {
            return _materialExporter.EvaluateMaterialName(skyboxMaterial);
        }

        public string EvaluateMeshName(Mesh sharedMesh, PrefabContext prefabContext)
        {
            return _meshExporter.EvaluateMeshName(sharedMesh, prefabContext);
        }

        public string EvaluateMeshName(ProBuilderMesh sharedMesh, PrefabContext prefabContext)
        {
            return _meshExporter.EvaluateMeshName(sharedMesh, prefabContext);
        }

        public string EvaluateTerrainHeightMap(TerrainData terrainData)
        {
            return _terrainExporter.EvaluateHeightMap(terrainData);
        }

        public string EvaluateTerrainMaterial(TerrainData terrainData)
        {
            return _terrainExporter.EvaluateMaterial(terrainData);
        }

        public void SchedulePBRTextures(MetallicGlossinessShaderArguments arguments, UrhoPBRMaterial urhoMaterial)
        {
            EditorTaskScheduler.Default.ScheduleForegroundTask(
                () => _textureExporter.ExportPBRTextures(arguments, urhoMaterial),
                urhoMaterial.MetallicRoughnessTexture);
        }

        public void SchedulePBRTextures(SpecularGlossinessShaderArguments arguments, UrhoPBRMaterial urhoMaterial)
        {
            EditorTaskScheduler.Default.ScheduleForegroundTask(
                () => _textureExporter.ExportPBRTextures(arguments, urhoMaterial),
                urhoMaterial.MetallicRoughnessTexture);
        }

        public string EvaluateAudioClipName(AudioClip audioClip)
        {
            return _audioExporter.EvaluateAudioClipName(audioClip);
        }

        public void ExportNavMesh(PrefabContext prefabContext)
        {
            _meshExporter.ExportMesh(NavMesh.CalculateTriangulation(), prefabContext);
        }

        public string EvaluateAnimationName(AnimationClip clip, PrefabContext prefabContext)
        {
            if (clip == null)
                return null;
            return _animationExporter.EvaluateAnimationName(clip, prefabContext);
        }

        public void ExportScene(Scene scene)
        {
            _sceneExporter.ExportScene(scene);
        }

        public void Dispose()
        {
        }

        protected override void ExportAssetBlock(string assetPath, Type mainType, Object[] assets, PrefabContext prefabContext)
        {
            if (mainType == typeof(GameObject))
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
                _meshExporter.ExportMesh(prefab, prefabContext);
                _prefabExporter.ExportPrefab(new AssetKey(AssetDatabase.AssetPathToGUID(assetPath), 0),
                    _prefabExporter.EvaluatePrefabName(assetPath), prefab);
            }
            else
            {
                foreach (var asset in assets)
                    if (asset is Mesh mesh)
                        EditorTaskScheduler.Default.ScheduleForegroundTask(
                            () => _meshExporter.ExportMeshModel(new MeshSource(mesh), EvaluateMeshName(mesh, prefabContext),
                                mesh.GetKey(), ExportUtils.GetLastWriteTimeUtc(mesh)),
                            mesh.name + " from " + assetPath);
            }

            foreach (var asset in assets)
                if (asset is Mesh mesh)
                {
                    //We already processed all meshes.
                }
                else if (asset is GameObject gameObject)
                {
                    //We already processed prefab.
                }
                else if (asset is Transform transform)
                {
                    //Skip
                }
                else if (asset is MeshRenderer meshRenderer)
                {
                    //Skip
                }
                else if (asset is MeshFilter meshFilter)
                {
                    //Skip
                }
                else if (asset is MeshCollider meshCollider)
                {
                    //Skip
                }
                else if (asset is ProBuilderMesh proBuilderMesh)
                {
                    //Skip
                }
                else if (asset is LODGroup lodGroup)
                {
                    //Skip
                }
                else if (asset is SkinnedMeshRenderer skinnedMeshRenderer)
                {
                    //Skip
                }
                else if (asset is Animation animation)
                {
                    //Skip
                }
                else if (asset is AudioClip audioClip)
                {
                    EditorTaskScheduler.Default.ScheduleForegroundTask(() => _audioExporter.ExportClip(audioClip),
                        audioClip.name + " from " + assetPath);
                }
                else if (asset is Material material)
                {
                    EditorTaskScheduler.Default.ScheduleForegroundTask(() => _materialExporter.ExportMaterial(material, prefabContext),
                        material.name + " from " + assetPath);
                }
                else if (asset is TerrainData terrainData)
                {
                    EditorTaskScheduler.Default.ScheduleForegroundTask(
                        () => _terrainExporter.ExportTerrain(terrainData, prefabContext), terrainData.name + " from " + assetPath);
                }
                else if (asset is Texture2D texture2d)
                {
                    EditorTaskScheduler.Default.ScheduleForegroundTask(
                        () => _textureExporter.ExportTexture(texture2d, new TextureReference(TextureSemantic.Other)),
                        texture2d.name + " from " + assetPath);
                }
                else if (asset is Cubemap cubemap)
                {
                    EditorTaskScheduler.Default.ScheduleForegroundTask(() => _cubemapExporter.Cubemap(cubemap),
                        cubemap.name + " from " + assetPath);
                }
                else if (asset is AnimationClip animationClip)
                {
                    EditorTaskScheduler.Default.ScheduleForegroundTask(
                        () => _animationExporter.ExportAnimation(animationClip, prefabContext), animationClip.name + " from " + assetPath);
                }
                else if (asset is AnimatorController animationController)
                {
                    EditorTaskScheduler.Default.ScheduleForegroundTask(
                        () => _animationControllerExporter.ExportAnimationController(animationController, prefabContext), animationController.name + " from " + assetPath);
                }
        }

        protected override IEnumerable<ProgressBarReport> ExportDynamicAsset(Object asset, PrefabContext prefabContext)
        {
            if (asset is ProBuilderMesh proBuilderMesh)
            {
                _meshExporter.ExportMesh(proBuilderMesh, prefabContext);
                yield break;
            }
            if (asset is Mesh mesh)
            {
                _meshExporter.ExportMesh(mesh, prefabContext);
                yield break;
            }
            if (asset is LODGroup lodGroup)
            {
                _meshExporter.ExportLODGroup(lodGroup, prefabContext);
                yield break;
            }
        }

        private bool CheckForFileUniqueness(string targetPath, AssetKey assetGuid)
        {
            if (!_createdFiles.TryGetValue(targetPath, out var existingAsset))
            {
                _createdFiles.Add(targetPath, assetGuid);
                return true;
            }

            if (existingAsset != assetGuid)
            {
                Debug.LogError("Asset file name collision: " + AssetDatabase.GUIDToAssetPath(assetGuid.Guid) + " and " +
                               AssetDatabase.GUIDToAssetPath(existingAsset.Guid) + " attempt to write into same file " +
                               targetPath);
                return false;
            }

            return false;
        }

        public string TryGetSkyboxCubemap(Material skyboxMaterial)
        {
            return _materialExporter.TryGetSkyboxCubemap(skyboxMaterial);
        }

        public string ScheduleLODGroup(LODGroup lodGroup, PrefabContext prefabContext)
        {
            if (lodGroup == null)
                return null;
            var name = _meshExporter.EvaluateLODGroupName(lodGroup, prefabContext);
            _meshExporter.ExportLODGroup(lodGroup, prefabContext);
            return name;
        }

        public string DecorateName(string materialName)
        {
            if (string.IsNullOrEmpty(materialName))
                return materialName;
            if (!Options.ASCIIOnly)
                return materialName;

            return Uri.EscapeDataString(materialName);
        }
    }
}