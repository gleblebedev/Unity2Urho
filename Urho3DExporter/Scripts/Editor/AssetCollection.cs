using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Urho3DExporter
{
    public class AssetCollection : IEnumerable<AssetContext>
    {
        private readonly string _urhoDataPath;
        private readonly List<AssetContext> _assets;

        public AssetCollection(string urhoDataPath, IEnumerable<AssetContext> assets)
        {
            _urhoDataPath = urhoDataPath.Replace('/', Path.DirectorySeparatorChar);
            if (!_urhoDataPath.EndsWith(Path.DirectorySeparatorChar.ToString()))
                _urhoDataPath += Path.DirectorySeparatorChar;
            _assets = assets.ToList();
            foreach (var assetContext in assets.Where(_=>_.Type == typeof(Material)))
            {
                AddMaterialPath(AssetDatabase.LoadAssetAtPath<Material>(assetContext.AssetPath),
                    assetContext.UrhoAssetName);
            }
        }

        public IEnumerator<AssetContext> GetEnumerator()
        {
            return _assets.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable) _assets).GetEnumerator();
        }

        public void AddMeshPath(Mesh mesh, string fileName)
        {
            if (fileName.StartsWith(_urhoDataPath, StringComparison.InvariantCultureIgnoreCase))
                fileName = fileName.Substring(_urhoDataPath.Length);
            fileName = fileName.Replace(Path.DirectorySeparatorChar, '/');
            TryAdd(_meshPaths, mesh, mesh.name, fileName);
        }

        public bool TryAdd(Dictionary<string, string> values, UnityEngine.Object asset, string name, string fileName)
        {
            var path = AssetDatabase.GetAssetPath(asset);
            var id = path + "#" + name;
            if (values.ContainsKey(id))
            {
                //Debug.LogError("Duplicate asset " + id);
                return false;
            }
            values.Add(id, fileName);
            return true;
        }

        Dictionary<string, string> _meshPaths = new Dictionary<string, string>();

        public bool TryGetMeshPath(Mesh sharedMesh, out string meshPath)
        {
            meshPath = null;
            if (sharedMesh == null)
                return false;
            var path = AssetDatabase.GetAssetPath(sharedMesh);
            var id = path + "#" + sharedMesh.name;
            return _meshPaths.TryGetValue(id, out meshPath);
        }

        public void AddMaterialPath(Material material, string fileName)
        {

            if (fileName.StartsWith(_urhoDataPath, StringComparison.InvariantCultureIgnoreCase))
                fileName = fileName.Substring(_urhoDataPath.Length).Replace(Path.DirectorySeparatorChar, '/');
            TryAdd(_materialPaths, material, material.name, fileName);
        }

        Dictionary<string, string> _materialPaths = new Dictionary<string, string>();

        public bool TryGetMaterialPath(Material sharedMaterial, out string materialPath)
        {
            materialPath = null;
            if (sharedMaterial == null)
                return false;
            var path = AssetDatabase.GetAssetPath(sharedMaterial);
            var id = path + "#" + sharedMaterial.name;
            return _materialPaths.TryGetValue(id, out materialPath);
        }



        public void AddTexturePath(Texture material, string fileName)
        {

            if (fileName.StartsWith(_urhoDataPath, StringComparison.InvariantCultureIgnoreCase))
                fileName = fileName.Substring(_urhoDataPath.Length).Replace(Path.DirectorySeparatorChar, '/');
            TryAdd(_texturePaths, material, material.name, fileName);
        }

        Dictionary<string, string> _texturePaths = new Dictionary<string, string>();

        public bool TryGetTexturePath(Texture sharedTexture, out string materialPath)
        {
            materialPath = null;
            if (sharedTexture == null)
                return false;
            var path = AssetDatabase.GetAssetPath(sharedTexture);
            var id = path + "#" + sharedTexture.name;
            return _texturePaths.TryGetValue(id, out materialPath);
        }
    }
}