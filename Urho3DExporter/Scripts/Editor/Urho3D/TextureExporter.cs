using System.IO;
using UnityEditor;
using UnityEngine;
using Path = System.IO.Path;

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

            if (!File.Exists(asset.FullPath))
            {
                Debug.LogError("File "+asset.FullPath+" not found");
                return;
            }

            asset.DestinationFolder.CopyFile(asset.FullPath, asset.UrhoAssetName);
        }
    }
}