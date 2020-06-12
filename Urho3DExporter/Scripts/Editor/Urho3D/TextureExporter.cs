using System;
using System.IO;
using System.Runtime.CompilerServices;
using Assets.Urho3DExporter.Scripts.Editor;
using UnityEditor;
using UnityEngine;

namespace Urho3DExporter
{
    public class TextureExporter : IExporter
    {
        private readonly AssetCollection _assets;
        private readonly TextureMetadataCollection _textureMetadata;

        public TextureExporter(AssetCollection assets, TextureMetadataCollection textureMetadata)
        {
            _assets = assets;
            _textureMetadata = textureMetadata;
        }

        public static float GetLuminance(Color32 rgb)
        {
            var r = rgb.r / 255.0f;
            var g = rgb.g / 255.0f;
            var b = rgb.b / 255.0f;
            return 0.2126f * r + 0.7152f * g + 0.0722f * b;
        }

        public static string GetTextureOutputName(string baseAssetName, TextureReferences reference)
        {
            switch (reference.Semantic)
            {
                case TextureSemantic.PBRMetallicGlossiness:
                    return ReplaceExtension(baseAssetName, ".MetallicRoughness.png");
                case TextureSemantic.PBRSpecularGlossiness:
                    return ReplaceExtension(baseAssetName, ".MetallicRoughness.png");
                case TextureSemantic.PBRDiffuse:
                    return ReplaceExtension(baseAssetName, ".BaseColor.png");
                default: return baseAssetName;
            }
        }

        private static string ReplaceExtension(string assetUrhoAssetName, string newExt)
        {
            var lastDot = assetUrhoAssetName.LastIndexOf('.');
            var lastSlash = assetUrhoAssetName.LastIndexOf('/');
            if (lastDot > lastSlash) return assetUrhoAssetName.Substring(0, lastDot) + newExt;

            return assetUrhoAssetName + newExt;
        }

        private static Texture2D CreateTargetTexture(Texture2D a, Texture2D b)
        {
            var width = 1;
            var height = 1;
            if (a != null)
            {
                if (a.width > width) width = a.width;
                if (a.height > height) height = a.height;
            }

            if (b != null)
            {
                if (b.width > width) width = b.width;
                if (b.height > height) height = b.height;
            }

            var tmpTexture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            tmpTexture.hideFlags = HideFlags.HideAndDontSave;
            return tmpTexture;
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

            var fullCopy = false;
            foreach (var reference in _textureMetadata.ResolveReferences(texture))
                switch (reference.Semantic)
                {
                    case TextureSemantic.PBRMetallicGlossiness:
                    {
                        TransformMetallicGlossiness(asset, texture, reference);
                        break;
                    }
                    case TextureSemantic.PBRSpecularGlossiness:
                    {
                        TransformSpecularGlossiness(asset, texture, reference);
                        break;
                    }
                    case TextureSemantic.PBRDiffuse:
                    {
                        TransformDiffuse(asset, texture, reference);
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

        private void EnsureReadableTexture(Texture2D texture)
        {
            if (null == texture) return;

            var assetPath = AssetDatabase.GetAssetPath(texture);
            var tImporter = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (tImporter != null)
            {
                tImporter.textureType = TextureImporterType.Default;
                if (tImporter.isReadable != true)
                {
                    tImporter.isReadable = true;
                    AssetDatabase.ImportAsset(assetPath);
                    AssetDatabase.Refresh();
                }
            }
        }

        private void TransformDiffuse(AssetContext asset, Texture texture, TextureReferences reference)
        {
            var diffuse = texture as Texture2D;
            EnsureReadableTexture(diffuse);
            var specularGlossiness = reference.SmoothnessSource as Texture2D;
            EnsureReadableTexture(specularGlossiness);

            var metallicRoughMapName = GetTextureOutputName(asset.UrhoAssetName, reference);
            using (var fileStream = asset.DestinationFolder.Create(metallicRoughMapName, DateTime.MaxValue))
            {
                if (fileStream != null)
                {
                    var tmpTexture = CreateTargetTexture(diffuse, specularGlossiness);
                    var specularColors = new PixelSource(specularGlossiness, tmpTexture.width, tmpTexture.height,
                        Color.black);
                    var diffuseColors = new PixelSource(diffuse, tmpTexture.width, tmpTexture.height, Color.white);
                    var smoothnessSource =
                        reference.SmoothnessTextureChannel == SmoothnessTextureChannel.MetallicOrSpecularAlpha
                            ? specularColors
                            : diffuseColors;
                    var pixels = new Color32[tmpTexture.width * tmpTexture.height];
                    var index = 0;
                    for (var y = 0; y < tmpTexture.height; ++y)
                    for (var x = 0; x < tmpTexture.width; ++x)
                    {
                        var diffuseColor = diffuseColors.GetAt(x, y);
                        var mr = PBRUtils.ConvertToMetallicRoughness(new PBRUtils.SpecularGlossiness
                        {
                            diffuse = diffuseColor,
                            specular = specularColors.GetAt(x, y),
                            glossiness = smoothnessSource.GetAt(x, y).a,
                            opacity = diffuseColor.a
                        });
                        pixels[index] = new Color(mr.baseColor.r, mr.baseColor.g, mr.baseColor.b, mr.opacity);
                        ++index;
                    }

                    tmpTexture.SetPixels32(pixels, 0);
                    var bytes = tmpTexture.EncodeToPNG();
                    fileStream.Write(bytes, 0, bytes.Length);
                }
            }
        }

        private void TransformMetallicGlossiness(AssetContext asset, Texture texture, TextureReferences reference)
        {
            var metallicGloss = texture as Texture2D;
            EnsureReadableTexture(metallicGloss);
            var smoothnessSource = reference.SmoothnessSource as Texture2D;
            EnsureReadableTexture(smoothnessSource);

            var metallicRoughMapName = GetTextureOutputName(asset.UrhoAssetName, reference);
            using (var fileStream = asset.DestinationFolder.Create(metallicRoughMapName, DateTime.MaxValue))
            {
                if (fileStream != null)
                {
                    var tmpTexture = CreateTargetTexture(metallicGloss, reference.SmoothnessSource as Texture2D);

                    var metallicColors = new PixelSource(metallicGloss, tmpTexture.width, tmpTexture.height,
                        new Color(0, 0, 0, 0));
                    var smoothnessColors = metallicGloss == smoothnessSource
                        ? metallicColors
                        : new PixelSource(smoothnessSource, tmpTexture.width, tmpTexture.height, new Color(0, 0, 0, 0));
                    var pixels = new Color32[tmpTexture.width * tmpTexture.height];
                    var index = 0;
                    for (var y = 0; y < tmpTexture.height; ++y)
                    for (var x = 0; x < tmpTexture.width; ++x)
                    {
                        var r = 1.0f - smoothnessColors.GetAt(x, y).a;
                        var m = metallicColors.GetAt(x, y).r;
                        pixels[index] = new Color(r, m, 0, 1);
                        ++index;
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
            EnsureReadableTexture(specularGloss);
            var diffuse = reference.SmoothnessSource as Texture2D;
            EnsureReadableTexture(diffuse);
            var smoothnessSource =
                reference.SmoothnessTextureChannel == SmoothnessTextureChannel.MetallicOrSpecularAlpha
                    ? specularGloss
                    : diffuse;

            var metallicRoughMapName = GetTextureOutputName(asset.UrhoAssetName, reference);
            using (var fileStream = asset.DestinationFolder.Create(metallicRoughMapName, DateTime.MaxValue))
            {
                if (fileStream != null)
                {
                    var tmpTexture = CreateTargetTexture(specularGloss, diffuse);
                    var specularColors =
                        new PixelSource(specularGloss, tmpTexture.width, tmpTexture.height, Color.black);
                    var diffuseColors = new PixelSource(diffuse, tmpTexture.width, tmpTexture.height, Color.white);
                    var smoothnessColors = specularGloss == smoothnessSource
                        ? specularColors
                        : diffuseColors;
                    var pixels = new Color32[tmpTexture.width * tmpTexture.height];
                    var index = 0;
                    for (var y = 0; y < tmpTexture.height; ++y)
                    for (var x = 0; x < tmpTexture.width; ++x)
                    {
                        var mr = PBRUtils.ConvertToMetallicRoughness(new PBRUtils.SpecularGlossiness
                        {
                            diffuse = diffuseColors.GetAt(x, y),
                            specular = specularColors.GetAt(x, y),
                            glossiness = smoothnessColors.GetAt(x, y).a,
                            opacity = 1.0f
                        });
                        pixels[index] = new Color(mr.roughness, mr.metallic, 0, 1);
                        ++index;
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
            return texture[xx + yy * texWidth];
        }

        private struct PixelSource
        {
            private readonly Color[] _colors;
            private readonly int _texWidth;
            private readonly int _texHeight;
            private readonly int _targetWidth;
            private readonly int _targetHeight;

            public PixelSource(
                Color[] colors,
                int texWidth,
                int texHeight,
                int targetWidth,
                int targetHeight)
            {
                _colors = colors;
                _texWidth = texWidth;
                _texHeight = texHeight;
                _targetWidth = targetWidth;
                _targetHeight = targetHeight;
            }

            public PixelSource(
                Texture2D texture,
                int targetWidth,
                int targetHeight)
            {
                _colors = texture.GetPixels(0);
                _texWidth = texture.width;
                _texHeight = texture.height;
                _targetWidth = targetWidth;
                _targetHeight = targetHeight;
            }

            public PixelSource(
                Texture2D texture,
                int targetWidth,
                int targetHeight,
                Color defaultColor)
            {
                if (texture != null)
                {
                    _colors = texture.GetPixels(0);
                    _texWidth = texture.width;
                    _texHeight = texture.height;
                }
                else
                {
                    _colors = new[] {defaultColor};
                    _texWidth = 1;
                    _texHeight = 1;
                }

                _targetWidth = targetWidth;
                _targetHeight = targetHeight;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public Color GetAt(int x, int y)
            {
                var xx = x * _texWidth / _targetWidth;
                var yy = y * _texHeight / _targetHeight;
                return _colors[xx + yy * _texWidth];
            }
        }
    }
}