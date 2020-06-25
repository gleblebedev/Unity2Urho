using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace UnityToCustomEngineExporter.Editor.Urho3D
{
    public class TextureExporter : IDisposable
    {
        private readonly Urho3DEngine _engine;
        private readonly Dictionary<TempTextureKey, List<Texture2D>> _tmpTexturePool = new Dictionary<TempTextureKey, List<Texture2D>>();

        public TextureExporter(Urho3DEngine engine)
        {
            _engine = engine;
        }

        private static string GetTextureOutputName(string baseAssetName, TextureReference reference)
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


        public void ExportTexture(Texture texture, TextureReference textureReference)
        {
            if (textureReference == null)
            {
                CopyTexture(texture);
                return;
            }
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

        private void CopyTexture(Texture texture)
        {
            var relPath = ExportUtils.GetRelPathFromAsset(texture);
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
            var outputAssetName = EvaluateTextrueName(texture);
            if (_engine.IsUpToDate(outputAssetName, ExportUtils.GetLastWriteTimeUtc(texture)))
            {
                return;
            }

            var tImporter = AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(texture)) as TextureImporter;
            var texType = tImporter?.textureType ?? TextureImporterType.Default;
            switch (texType)
            {
                case TextureImporterType.NormalMap:
                    new TextureProcessor().ProcessAndSaveTexture(texture, "Hidden/UnityToCustomEngineExporter/Urho3D/DecodeNormalMap", _engine.GetTargetFilePath(outputAssetName));
                    break;
                default:
                    new TextureProcessor().ProcessAndSaveTexture(texture, "Hidden/UnityToCustomEngineExporter/Urho3D/Copy", _engine.GetTargetFilePath(outputAssetName));
                    break;
            }


        }

        public static void EnsureReadableTexture(Texture2D texture)
        {
            if (null == texture) return;

            var assetPath = AssetDatabase.GetAssetPath(texture);
            var tImporter = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (tImporter != null)
            {
                //tImporter.textureType = TextureImporterType.Default;
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
            var baseColorName = GetTextureOutputName(EvaluateTextrueName(texture), reference);
            if (_engine.IsUpToDate(baseColorName, reference.GetLastWriteTimeUtc(texture)))
            {
                return;
            }

            var tmpMaterial = new Material(Shader.Find("Hidden/UnityToCustomEngineExporter/Urho3D/ConvertToBaseColor"));
            tmpMaterial.SetTexture("_MainTex", texture);
            tmpMaterial.SetTexture("_SpecGlossMap", reference.Specular);
            tmpMaterial.SetFloat("_SmoothnessScale", reference.SmoothnessScale);
            tmpMaterial.SetTexture("_Smoothness", reference.Smoothness);
            try
            {
                new TextureProcessor().ProcessAndSaveTexture(texture, tmpMaterial, _engine.GetTargetFilePath(baseColorName));
            }
            finally
            {
                GameObject.DestroyImmediate(tmpMaterial);
            }
        }

        private void TransformMetallicGlossiness(Texture texture, PBRMetallicGlossinessTextureReference reference)
        {
            var baseColorName = GetTextureOutputName(EvaluateTextrueName(texture), reference);
            if (_engine.IsUpToDate(baseColorName, reference.GetLastWriteTimeUtc(texture)))
            {
                return;
            }

            var tmpMaterial = new Material(Shader.Find("Hidden/UnityToCustomEngineExporter/Urho3D/ConvertToMetallicRoughness"));
            tmpMaterial.SetTexture("_MainTex", texture);
            tmpMaterial.SetFloat("_SmoothnessScale", reference.SmoothnessScale);
            tmpMaterial.SetTexture("_Smoothness", reference.Smoothness);
            try
            {
                new TextureProcessor().ProcessAndSaveTexture(texture, tmpMaterial, _engine.GetTargetFilePath(baseColorName));
            }
            finally
            {
                GameObject.DestroyImmediate(tmpMaterial);
            }
        }

        private void TransformSpecularGlossiness(Texture texture, PBRSpecularGlossinessTextureReference reference)
        {
            var baseColorName = GetTextureOutputName(EvaluateTextrueName(texture), reference);
            if (_engine.IsUpToDate(baseColorName, reference.GetLastWriteTimeUtc(texture)))
            {
                return;
            }

            var tmpMaterial = new Material(Shader.Find("Hidden/UnityToCustomEngineExporter/Urho3D/ConvertSpecularToMetallicRoughness"));
            tmpMaterial.SetTexture("_SpecGlossMap", texture);
            tmpMaterial.SetTexture("_MainTex", reference.Diffuse);
            tmpMaterial.SetFloat("_SmoothnessScale", reference.SmoothnessScale);
            tmpMaterial.SetTexture("_Smoothness", reference.Smoothness);
            try
            {
                new TextureProcessor().ProcessAndSaveTexture(reference.Diffuse, tmpMaterial, _engine.GetTargetFilePath(baseColorName));
            }
            finally
            {
                GameObject.DestroyImmediate(tmpMaterial);
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

        public string EvaluateTextrueName(Texture texture, TextureReference reference)
        {
            var baseName = EvaluateTextrueName(texture);
            return GetTextureOutputName(baseName, reference);
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
            return ExportUtils.ReplaceExtension(ExportUtils.GetRelPathFromAssetPath(assetPath), newExt);
        }

     
    }
}