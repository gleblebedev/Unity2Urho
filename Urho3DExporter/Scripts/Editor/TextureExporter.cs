using System.IO;
using UnityEditor;
using UnityEngine;

namespace Urho3DExporter
{
    public class TextureExporter : IExporter
    {
        private readonly AssetCollection _assets;

        public TextureExporter(AssetCollection assets) : base()
        {
            _assets = assets;
        }

        public void ExportAsset(AssetContext asset)
        {
            var texture = AssetDatabase.LoadAssetAtPath<Texture>(asset.AssetPath);
            _assets.AddTexturePath(texture, asset.UrhoAssetName);

            if (asset.UrhoFileName == null)
                return;
            if (File.Exists(asset.UrhoFileName))
                return;
            if (!File.Exists(asset.FullPath))
            {
                Debug.LogError("File "+asset.FullPath+" not found");
                return;
            }

            Directory.CreateDirectory(Path.GetDirectoryName(asset.UrhoFileName));
            File.Copy(asset.FullPath, asset.UrhoFileName);
        }
    }
}