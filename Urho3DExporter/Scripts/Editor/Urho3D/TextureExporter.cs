using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Assets.Scripts.UnityToCustomEngineExporter.Editor.Urho3D
{
    public class TextureExporter : IDisposable
    {
        private readonly Urho3DEngine _engine;
        private readonly Dictionary<TempTextureKey, List<Texture2D>> _tmpTexturePool = new Dictionary<TempTextureKey, List<Texture2D>>();

        public TextureExporter(Urho3DEngine engine)
        {
            _engine = engine;
        }

        public static string GetTextureOutputName(string baseAssetName, TextureReference reference)
        {
            switch (reference.Semantic)
            {
                case TextureSemantic.PBRMetallicGlossiness:
                    return ExportUtils.ReplaceExtension(baseAssetName, ".MetallicRoughness.png");
                case TextureSemantic.PBRSpecularGlossiness:
                    return ExportUtils.ReplaceExtension(baseAssetName, ".MetallicRoughness.png");
                case TextureSemantic.PBRDiffuse:
                    return ExportUtils.ReplaceExtension(baseAssetName, ".BaseColor.png");
                default: return baseAssetName;
            }
        }

        private TempTexture CreateTargetTexture(Texture2D a, Texture2D b)
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

            return BorrowTempTexture(new TempTextureKey(width, height, TextureFormat.RGBA32, false));
        }

        struct TempTextureKey
        {
            public int width;
            public int height;
            public TextureFormat textureFormat;
            public bool mips;

            public TempTextureKey(int width, int height, TextureFormat textureFormat, bool mips)
            {
                this.width = width;
                this.height = height;
                this.textureFormat = textureFormat;
                this.mips = mips;
            }
        }

        private class TempTexture: IDisposable
        {
            private readonly Texture2D _texture;
            private readonly List<Texture2D> _collection;

            public TempTexture(Texture2D texture, List<Texture2D> collection)
            {
                _texture = texture;
                _collection = collection;
            }

            public Texture2D Texture => _texture;

            public void Dispose()
            {
                _collection.Add(_texture);
            }
        }

        private TempTexture BorrowTempTexture(TempTextureKey key)
        {
            if (!_tmpTexturePool.TryGetValue(key, out var list))
            {
                list = new List<Texture2D>();
                _tmpTexturePool.Add(key, list);
            }

            if (list.Count == 0)
            {
                var tmpTexture = new Texture2D(key.width, key.height, key.textureFormat, key.mips);
                tmpTexture.hideFlags = HideFlags.HideAndDontSave;
                return new TempTexture(tmpTexture, list);
            }

            var index = list.Count - 1;
            var t = list[index];
            list.RemoveAt(index);
            return new TempTexture(t, list);
        }

        public string ResolveTextureName(Texture texture)
        {
            return AssetInfo.GetRelPathFromAsset(texture);
        }

        public void ExportTexture(Texture texture, TextureReference textureReference)
        {
                switch (textureReference.Semantic)
                {
                    case TextureSemantic.PBRMetallicGlossiness:
                        {
                            TransformMetallicGlossiness(texture, (PBRMetallicGlossinessTextureReference)textureReference);
                            break;
                        }
                    case TextureSemantic.PBRSpecularGlossiness:
                        {
                            TransformSpecularGlossiness(texture, (PBRSpecularGlossinessTextureReference)textureReference);
                            break;
                        }
                    case TextureSemantic.PBRDiffuse:
                        {
                            TransformDiffuse(texture, (PBRDiffuseTextureReference)textureReference);
                            break;
                        }
                    default:
                        {
                            CopyTexture(texture);
                            break;
                        }
                }
        }
        public void ExportAsset(AssetContext asset)
        {
            if (!File.Exists(asset.FullPath))
            {
                Debug.LogError("File " + asset.FullPath + " not found");
                return;
            }

            //var texture = AssetDatabase.LoadAssetAtPath<Texture>(asset.AssetPath);

            //foreach (var reference in _textureMetadata.ResolveReferences(texture))
            //    switch (reference.Semantic)
            //    {
            //        case TextureSemantic.PBRMetallicGlossiness:
            //        {
            //            TransformMetallicGlossiness(asset, texture, (PBRMetallicGlossinessTextureReference)reference);
            //            break;
            //        }
            //        case TextureSemantic.PBRSpecularGlossiness:
            //        {
            //            TransformSpecularGlossiness(asset, texture, (PBRSpecularGlossinessTextureReference)reference);
            //            break;
            //        }
            //        case TextureSemantic.PBRDiffuse:
            //        {
            //            TransformDiffuse(asset, texture, (PBRDiffuseTextureReference)reference);
            //            break;
            //        }
            //        default:
            //        {
            //            CopyTexture(asset, texture);
            //            break;
            //        }
            //    }
        }

        private string ReplaceExtensionWithPngIfNeeded(string assetUrhoAssetName)
        {
            var dotIndex = assetUrhoAssetName.LastIndexOf('.');
            var slashIndex = assetUrhoAssetName.LastIndexOf('/');
            if (dotIndex <= slashIndex)
            {
                return assetUrhoAssetName + ".png";
            }

            var ext = assetUrhoAssetName.Substring(dotIndex).ToLower();
            switch (ext)
            {
                case ".tif":
                    return assetUrhoAssetName.Substring(0, dotIndex) + ".png";
            }

            return assetUrhoAssetName;
        }

        private void CopyTexture(Texture texture)
        {
            var relPath = AssetInfo.GetRelPathFromAsset(texture);
            var newName = EvaluateTextrueName(texture);
            if (relPath != newName)
            {
                CopyTextureAsPng(texture);
            }
            else
            {
                _engine.TryCopyFile( AssetDatabase.GetAssetPath(texture), newName);
            }
        }

        private void CopyTextureAsPng(Texture texture)
        {
            var diffuse = texture as Texture2D;
            EnsureReadableTexture(diffuse);
        
            var metallicRoughMapName = ExportUtils.ReplaceExtension(EvaluateTextrueName(texture), ".png");
            using (var fileStream = _engine.TryCreate(metallicRoughMapName, ExportUtils.GetLastWriteTimeUtc(texture)))
            {
                if (fileStream != null)
                {
                    if (texture is Texture2D texture2d)
                    {
                        using (var tmpTextureRef = CreateTargetTexture(texture2d, null))
                        {
                            var tmpTexture = tmpTextureRef.Texture;
                            var colors = texture2d.GetPixels(0);
                            tmpTexture.SetPixels(colors);
                            var bytes = tmpTexture.EncodeToPNG();
                            fileStream.Write(bytes, 0, bytes.Length);
                        }
                    }
                }
            }
        }

        public static void EnsureReadableTexture(Texture2D texture)
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

       
        private void TransformDiffuse(Texture texture, PBRDiffuseTextureReference reference)
        {
            var diffuse = texture as Texture2D;
            EnsureReadableTexture(diffuse);
            var specularGlossiness = reference.Specular as Texture2D;
            EnsureReadableTexture(specularGlossiness);

            var metallicRoughMapName = GetTextureOutputName(EvaluateTextrueName(texture), reference);
            using (var fileStream = _engine.TryCreate(metallicRoughMapName, ExportUtils.GetLastWriteTimeUtc(texture)))
            {
                if (fileStream != null)
                {
                    using (var tmpTextureRef = CreateTargetTexture(diffuse, specularGlossiness))
                    {
                        var tmpTexture = tmpTextureRef.Texture;
                        var specularColors = new PixelSource(specularGlossiness, tmpTexture.width, tmpTexture.height,
                            Color.black);
                        var diffuseColors = new PixelSource(diffuse, tmpTexture.width, tmpTexture.height, Color.white);
                        var smoothnessSource = (reference.Smoothness == reference.Specular)
                                ? specularColors
                                : diffuseColors;
                        var pixels = new Color32[tmpTexture.width * tmpTexture.height];
                        var index = 0;
                        for (var y = 0; y < tmpTexture.height; ++y)
                        {
                            for (var x = 0; x < tmpTexture.width; ++x)
                            {
                                var diffuseColor = diffuseColors.GetAt(x, y);
                                var mr = PBRUtils.ConvertToMetallicRoughness(new PBRUtils.SpecularGlossiness
                                {
                                    diffuse = diffuseColor,
                                    specular = specularColors.GetAt(x, y),
                                    glossiness = smoothnessSource.GetAt(x, y).a * reference.SmoothnessScale,
                                    opacity = diffuseColor.a
                                });
                                pixels[index] = new Color(mr.baseColor.r, mr.baseColor.g, mr.baseColor.b, mr.opacity);
                                ++index;
                            }
                        }

                        tmpTexture.SetPixels32(pixels, 0);
                        var bytes = tmpTexture.EncodeToPNG();
                        fileStream.Write(bytes, 0, bytes.Length);
                    }
                }
            }
        }

        private void TransformMetallicGlossiness(Texture texture, PBRMetallicGlossinessTextureReference reference)
        {
            var metallicGloss = texture as Texture2D;
            EnsureReadableTexture(metallicGloss);
            var smoothnessSource = reference.SmoothnessSource as Texture2D;
            EnsureReadableTexture(smoothnessSource);

            var metallicRoughMapName = GetTextureOutputName(EvaluateTextrueName(texture), reference);
            using (var fileStream = _engine.TryCreate(metallicRoughMapName, ExportUtils.GetLastWriteTimeUtc(texture)))
            {
                if (fileStream != null)
                {
                    using (var tmpTextureRef = CreateTargetTexture(metallicGloss, reference.SmoothnessSource as Texture2D))
                    {
                        var tmpTexture = tmpTextureRef.Texture;
                        var metallicColors = new PixelSource(metallicGloss, tmpTexture.width, tmpTexture.height,
                            new Color(0, 0, 0, 0));
                        var smoothnessColors = metallicGloss == smoothnessSource
                            ? metallicColors
                            : new PixelSource(smoothnessSource, tmpTexture.width, tmpTexture.height,
                                new Color(0, 0, 0, 0));
                        var pixels = new Color32[tmpTexture.width * tmpTexture.height];
                        var index = 0;
                        for (var y = 0; y < tmpTexture.height; ++y)
                        for (var x = 0; x < tmpTexture.width; ++x)
                        {
                            var r = 1.0f - smoothnessColors.GetAt(x, y).a * reference.SmoothnessScale;
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
        }

        private void TransformSpecularGlossiness(Texture texture, PBRSpecularGlossinessTextureReference reference)
        {
            var specularGloss = texture as Texture2D;
            EnsureReadableTexture(specularGloss);
            var diffuse = reference.SmoothnessSource as Texture2D;
            EnsureReadableTexture(diffuse);
            var smoothnessSource =
                (reference.SmoothnessSource == reference.Specular)
                    ? specularGloss
                    : diffuse;

            var metallicRoughMapName = GetTextureOutputName(EvaluateTextrueName(texture), reference);
            using (var fileStream = _engine.TryCreate(metallicRoughMapName, ExportUtils.GetLastWriteTimeUtc(texture)))
            {
                if (fileStream != null)
                {
                    using (var tmpTextureRef = CreateTargetTexture(specularGloss, diffuse))
                    {
                        var tmpTexture = tmpTextureRef.Texture;
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
                                glossiness = smoothnessColors.GetAt(x, y).a * reference.SmoothnessScale,
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

        public void Dispose()
        {
            foreach (var tmpTexture in _tmpTexturePool)
            {
                foreach (var texture2D in tmpTexture.Value)
                {
                    Object.DestroyImmediate(texture2D);
                }
            }
            _tmpTexturePool.Clear();
        }

        public string EvaluateTextrueName(Texture texture)
        {
            if (texture == null)
                return null;
            var assetPath = AssetDatabase.GetAssetPath(texture);
            if (string.IsNullOrWhiteSpace(assetPath))
                return null;

            var newExt = Path.GetExtension(assetPath);
            if (texture is Cubemap)
            {
                newExt = ".xml";
            }
            else
            {
                switch (newExt)
                {
                    case ".tif":
                        newExt = ".png";
                        break;
                }
            }
            return AssetContext.ReplaceExt(AssetContext.GetRelPathFromAssetPath(assetPath), newExt);
        }

     
    }
}