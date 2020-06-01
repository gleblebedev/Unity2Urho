using System;
using System.IO;
using System.Runtime.CompilerServices;
using Assets.Urho3DExporter.Scripts.Editor;
using UnityEditor;
using UnityEngine;
using Path = System.IO.Path;

namespace Urho3DExporter
{
    public class TextureExporter : IExporter
    {
        private readonly AssetCollection _assets;
        private readonly TextureMetadataCollection _textureMetadata;

        public TextureExporter(AssetCollection assets, TextureMetadataCollection textureMetadata) : base()
        {
            _assets = assets;
            _textureMetadata = textureMetadata;
        }

        public void ExportAsset(AssetContext asset)
        {
            if (!File.Exists(asset.FullPath))
            {
                Debug.LogError("File " + asset.FullPath + " not found");
                return;
            }

            var texture = AssetDatabase.LoadAssetAtPath<Texture>(asset.AssetPath);
            _assets.AddTexturePath(texture, asset.UrhoAssetName);

            bool fullCopy = false;
            foreach (var reference in _textureMetadata.ResolveReferences(texture))
            {
                switch (reference.Semantic)
                {
                    case TextureSemantic.MetallicGlossiness:
                    {
                        TransformMetallicGlossiness(asset, texture, reference);
                        break;
                    }
                    case TextureSemantic.SpecularGlossiness:
                    {
                        TransformSpecularGlossiness(asset, texture, reference);
                        break;
                    }
                    default:
                    {
                        if (!fullCopy)
                        {
                            asset.DestinationFolder.CopyFile(asset.FullPath, asset.UrhoAssetName);
                            fullCopy = true;
                        }
                        break;
                    }
                }
            }
        }
        private void EnsureReadableTexture(Texture2D texture)
        {
            if (null == texture) return;

            string assetPath = AssetDatabase.GetAssetPath(texture);
            var tImporter = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (tImporter != null)
            {
                tImporter.textureType = TextureImporterType.Advanced;
                if (tImporter.isReadable != true)
                {
                    tImporter.isReadable = true;
                    AssetDatabase.ImportAsset(assetPath);
                    AssetDatabase.Refresh();
                }
            }
        }


        private void TransformMetallicGlossiness(AssetContext asset, Texture texture, TextureReferences reference)
        {
            var metallicGloss = texture as Texture2D;
            var smoothnessSource = reference.SmoothnessSource as Texture2D;
            EnsureReadableTexture(metallicGloss);
            EnsureReadableTexture(smoothnessSource);

            var metallicRoughMapName = GetTextureOutputName(asset.UrhoAssetName, reference);
            using (var fileStream = asset.DestinationFolder.Create(metallicRoughMapName))
            {
                if (fileStream != null)
                {
                    var width = Math.Max(metallicGloss.width, smoothnessSource.width);
                    var height = Math.Max(metallicGloss.height, smoothnessSource.height);
                    var tmpTexture = new Texture2D(width, height, TextureFormat.RGBA32, false);
                    tmpTexture.hideFlags = HideFlags.HideAndDontSave;

                    var metallicColors = metallicGloss.GetPixels32(0);
                    var smoothnessColors = (metallicGloss== smoothnessSource)? metallicColors: smoothnessSource.GetPixels32(0);
                    var pixels = new Color32[width * height];
                    var index = 0;
                    for (int y = 0; y < height; ++y)
                    {
                        for (int x = 0; x < width; ++x)
                        {
                            var r = 1.0f - Get(smoothnessColors, smoothnessSource.width, smoothnessSource.height, x, y, width, height).a;
                            var m = Get(metallicColors, metallicGloss.width, metallicGloss.height, x, y, width, height).r;
                            pixels[index] = new Color(r, m, 0, 1);
                            ++index;
                        }
                    }
                    tmpTexture.SetPixels32(pixels, 0);
                    var bytes = tmpTexture.EncodeToPNG();
                    fileStream.Write(bytes, 0, bytes.Length);
                }
            }
        }
        private void TransformSpecularGlossiness(AssetContext asset, Texture texture, TextureReferences reference)
        {
            var specularGloss = texture as Texture2D;
            var smoothnessSource = reference.SmoothnessSource as Texture2D;
            EnsureReadableTexture(specularGloss);
            EnsureReadableTexture(smoothnessSource);

            var metallicRoughMapName = GetTextureOutputName(asset.UrhoAssetName, reference);
            using (var fileStream = asset.DestinationFolder.Create(metallicRoughMapName))
            {
                if (fileStream != null)
                {
                    var width = Math.Max(specularGloss.width, smoothnessSource.width);
                    var height = Math.Max(specularGloss.height, smoothnessSource.height);
                    var tmpTexture = new Texture2D(width, height, TextureFormat.RGBA32, false);
                    tmpTexture.hideFlags = HideFlags.HideAndDontSave;

                    var metallicColors = specularGloss.GetPixels32(0);
                    var smoothnessColors = (specularGloss == smoothnessSource) ? metallicColors : smoothnessSource.GetPixels32(0);
                    var pixels = new Color32[width * height];
                    var index = 0;
                    for (int y = 0; y < height; ++y)
                    {
                        for (int x = 0; x < width; ++x)
                        {
                            var r = 1.0f - Get(smoothnessColors, smoothnessSource.width, smoothnessSource.height, x, y, width, height).a;
                            var m = Get(metallicColors, specularGloss.width, specularGloss.height, x, y, width, height).r;
                            pixels[index] = new Color(r, m, 0, 1);
                            ++index;
                        }
                    }
                    tmpTexture.SetPixels32(pixels, 0);
                    var bytes = tmpTexture.EncodeToPNG();
                    fileStream.Write(bytes, 0, bytes.Length);
                }
            }
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private Color Get(Color32[] texture, int texWidth, int texHeight, int x, int y, int width, int height)
        {
            var xx = x * texWidth / width;
            var yy = y * texHeight / height;
            return texture[xx+yy*texWidth];
        }
        public static string GetTextureOutputName(string baseAssetName, TextureReferences reference)
        {
            switch (reference.Semantic)
            {
                case TextureSemantic.MetallicGlossiness:
                    return ReplaceExtension(baseAssetName, ".MetallicRoughness.png");
                case TextureSemantic.SpecularGlossiness:
                    return ReplaceExtension(baseAssetName, ".SpecularGlossiness.png");
                default: return baseAssetName;
            }
        }

        private static string ReplaceExtension(string assetUrhoAssetName, string newExt)
        {
            var lastDot = assetUrhoAssetName.LastIndexOf('.');
            var lastSlash = assetUrhoAssetName.LastIndexOf('/');
            if (lastDot > lastSlash)
            {
                return assetUrhoAssetName.Substring(0, lastDot)+ newExt;
            }

            return assetUrhoAssetName+ newExt;
        }
    }
}