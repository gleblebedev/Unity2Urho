using System;
using System.IO;
using System.Runtime.CompilerServices;
using Assets.Scripts.UnityToCustomEngineExporter.Editor.Urho3D;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace UnityToCustomEngineExporter.Editor.Urho3D
{
    public class TextureExporter
    {
        private readonly Urho3DEngine _engine;

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

        private static void DestroyTmpTexture(TextureOrColor reference, Texture specularTexture)
        {
            if (specularTexture != null && specularTexture != reference.Texture)
                Object.DestroyImmediate(specularTexture);
        }
        private static void DestroyTmpTexture(Texture reference, Texture specularTexture)
        {
            if (specularTexture != null && specularTexture != reference)
                Object.DestroyImmediate(specularTexture);
        }
        private static Texture EnsureTexture(TextureOrColor textureOrColor)
        {
            var specularTexture = textureOrColor.Texture;
            if (specularTexture == null)
            {
                var tmpSpecularTexture = new Texture2D(1, 1, TextureFormat.ARGB32, false);
                tmpSpecularTexture.SetPixels(new[] {textureOrColor.Color});
                tmpSpecularTexture.Apply();
                return tmpSpecularTexture;
            }

            return specularTexture;
        }

        public void WriteOptions(string urhoTextureName, DateTime lastWriteTimeUtc, TextureOptions options)
        {
            if (options == null)
                return;
            var xmlFileName =ExportUtils.ReplaceExtension(urhoTextureName, ".xml");
            if (xmlFileName == urhoTextureName)
                return;
            using (var writer = _engine.TryCreateXml(xmlFileName, lastWriteTimeUtc))
            {
                if (writer != null)
                {
                    writer.WriteStartElement("texture");
                    writer.WriteWhitespace(Environment.NewLine);
                    switch (options.filterMode)
                    {
                        case FilterMode.Point:
                            writer.WriteElementParameter("filter", "mode", "nearest");
                            break;
                        case FilterMode.Bilinear:
                            writer.WriteElementParameter("filter", "mode", "bilinear");
                            break;
                        case FilterMode.Trilinear:
                            writer.WriteElementParameter("filter", "mode", "trilinear");
                            break;
                        default:
                            writer.WriteElementParameter("filter", "mode", "default");
                            break;
                    }

                    switch (options.wrapMode)
                    {
                        case TextureWrapMode.Repeat:
                            writer.WriteElementParameter("address", "mode", "wrap");
                            break;
                        case TextureWrapMode.Clamp:
                            writer.WriteElementParameter("address", "mode", "clamp");
                            break;
                        case TextureWrapMode.Mirror:
                            writer.WriteElementParameter("address", "mode", "mirror");
                            break;
                    }
                    writer.WriteElementParameter("srgb", "enable", options.sRGBTexture? "true": "false");
                    writer.WriteElementParameter("mipmap", "enable", options.mipmapEnabled ? "true" : "false");
                    writer.WriteEndElement();
                }
            }
        }

        public void ExportTexture(Texture texture, TextureReference textureReference)
        {
            CopyTexture(texture);
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
                newExt = ".xml";
            else
                switch (newExt)
                {
                    case ".tif":
                        newExt = ".png";
                        break;
                }

            return ExportUtils.ReplaceExtension(ExportUtils.GetRelPathFromAssetPath(assetPath), newExt);
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
                _engine.TryCopyFile(AssetDatabase.GetAssetPath(texture), newName);
                WriteOptions(newName, ExportUtils.GetLastWriteTimeUtc(texture), ExportUtils.GetTextureOptions(texture));
            }
        }

        private void CopyTextureAsPng(Texture texture)
        {
            var outputAssetName = EvaluateTextrueName(texture);
            var sourceFileTimestampUtc = ExportUtils.GetLastWriteTimeUtc(texture);
            if (_engine.IsUpToDate(outputAssetName, sourceFileTimestampUtc)) return;

            var tImporter = AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(texture)) as TextureImporter;
            var texType = tImporter?.textureType ?? TextureImporterType.Default;
            switch (texType)
            {
                case TextureImporterType.NormalMap:
                    new TextureProcessor().ProcessAndSaveTexture(texture,
                        "Hidden/UnityToCustomEngineExporter/Urho3D/DecodeNormalMap",
                        _engine.GetTargetFilePath(outputAssetName));
                    break;
                default:
                    new TextureProcessor().ProcessAndSaveTexture(texture,
                        "Hidden/UnityToCustomEngineExporter/Urho3D/Copy", _engine.GetTargetFilePath(outputAssetName));
                    WriteOptions(outputAssetName, sourceFileTimestampUtc, ExportUtils.GetTextureOptions(texture).WithSRGB(true));
                    break;
            }
        }

        private void TransformDiffuse(SpecularGlossinessShaderArguments arguments, string baseColorName)
        {
            var sourceFileTimestampUtc = ExportUtils.GetLastWriteTimeUtc(arguments.Diffuse, arguments.PBRSpecular.Texture, arguments.Smoothness.Texture);
            if (_engine.IsUpToDate(baseColorName, sourceFileTimestampUtc)) return;

            var tmpMaterial = new Material(Shader.Find("Hidden/UnityToCustomEngineExporter/Urho3D/ConvertToBaseColor"));
            Texture mainTexture = null;
            Texture specularTexture = null;
            Texture smoothnessTexture = null;
            try
            {
                mainTexture = EnsureTexture(new TextureOrColor(arguments.Diffuse, arguments.DiffuseColor));
                specularTexture = EnsureTexture(arguments.PBRSpecular);
                smoothnessTexture = EnsureTexture(arguments.Smoothness);
                (var width, var height) = MaxTexutreSize(mainTexture, specularTexture, smoothnessTexture);
                tmpMaterial.SetTexture("_MainTex", mainTexture);
                tmpMaterial.SetTexture("_SpecGlossMap", specularTexture);
                tmpMaterial.SetFloat("_SmoothnessScale", arguments.GlossinessTextureScale * (arguments.Smoothness.Texture != null ? 1.0f : arguments.Glossiness));
                tmpMaterial.SetTexture("_Smoothness", smoothnessTexture);
                new TextureProcessor().ProcessAndSaveTexture(mainTexture, width, height, tmpMaterial, _engine.GetTargetFilePath(baseColorName));
                WriteOptions(baseColorName, sourceFileTimestampUtc, (ExportUtils.GetTextureOptions(mainTexture) ?? ExportUtils.GetTextureOptions(specularTexture) ?? ExportUtils.GetTextureOptions(smoothnessTexture)).WithSRGB(true));
            }
            finally
            {
                Object.DestroyImmediate(tmpMaterial);
                DestroyTmpTexture(arguments.Diffuse, mainTexture);
                DestroyTmpTexture(arguments.PBRSpecular, specularTexture);
                DestroyTmpTexture(arguments.Smoothness, smoothnessTexture);
            }
        }

        private (int,int) MaxTexutreSize(params Texture[] textures)
        {
            int width = 1;
            int height = 1;
            foreach (var texture in textures)
            {
                if (texture.width > width)
                    width = texture.width;
                if (texture.height > height)
                    height = texture.height;
            }

            return (width, height);
        }

        private void TransformMetallicGlossiness(MetallicGlossinessShaderArguments arguments, string baseColorName)
        {
            var sourceFileTimestampUtc = ExportUtils.GetLastWriteTimeUtc(arguments.MetallicGloss, arguments.Smoothness);
            if (_engine.IsUpToDate(baseColorName, sourceFileTimestampUtc)) return;

            var tmpMaterial = new Material(Shader.Find("Hidden/UnityToCustomEngineExporter/Urho3D/ConvertToMetallicRoughness"));

            Texture mainTexture = null;
            Texture smoothnessTexture = null;
            try
            {
                mainTexture = EnsureTexture(new TextureOrColor(arguments.MetallicGloss, new Color(arguments.Metallic, arguments.Metallic, arguments.Metallic, arguments.Glossiness)));
                smoothnessTexture = EnsureTexture(new TextureOrColor(arguments.Smoothness, new Color(0,0,0, arguments.Glossiness)));
                (var width, var height) = MaxTexutreSize(mainTexture, smoothnessTexture);
                tmpMaterial.SetTexture("_MainTex", mainTexture);
                tmpMaterial.SetFloat("_SmoothnessScale", arguments.GlossinessTextureScale);
                tmpMaterial.SetTexture("_Smoothness", smoothnessTexture);
                new TextureProcessor().ProcessAndSaveTexture(mainTexture, width, height, tmpMaterial, _engine.GetTargetFilePath(baseColorName));
                WriteOptions(baseColorName, sourceFileTimestampUtc, (ExportUtils.GetTextureOptions(mainTexture) ?? ExportUtils.GetTextureOptions(smoothnessTexture)).WithSRGB(false));
            }
            finally
            {
                Object.DestroyImmediate(tmpMaterial);
                DestroyTmpTexture(arguments.MetallicGloss, mainTexture);
                DestroyTmpTexture(arguments.Smoothness, smoothnessTexture);
            }
        }

        private void TransformSpecularGlossiness(SpecularGlossinessShaderArguments arguments, string baseColorName)
        {
            var sourceFileTimestampUtc = ExportUtils.GetLastWriteTimeUtc(arguments.Diffuse, arguments.PBRSpecular.Texture, arguments.Smoothness.Texture);
            if (_engine.IsUpToDate(baseColorName, sourceFileTimestampUtc)) return;

            var tmpMaterial = new Material(Shader.Find("Hidden/UnityToCustomEngineExporter/Urho3D/ConvertSpecularToMetallicRoughness"));
            Texture mainTexture = null;
            Texture specularTexture = null;
            Texture smoothnessTexture = null;
            try
            {
                mainTexture = EnsureTexture(new TextureOrColor(arguments.Diffuse, arguments.DiffuseColor));
                specularTexture = EnsureTexture(arguments.PBRSpecular);
                smoothnessTexture = EnsureTexture(arguments.Smoothness);
                (var width, var height) = MaxTexutreSize(mainTexture, specularTexture, smoothnessTexture);
                tmpMaterial.SetTexture("_MainTex", mainTexture);
                tmpMaterial.SetTexture("_SpecGlossMap", specularTexture);
                tmpMaterial.SetFloat("_SmoothnessScale", arguments.GlossinessTextureScale * (arguments.Smoothness.Texture != null?1.0f:arguments.Glossiness));
                tmpMaterial.SetTexture("_Smoothness", smoothnessTexture);
                new TextureProcessor().ProcessAndSaveTexture(mainTexture, width, height, tmpMaterial, _engine.GetTargetFilePath(baseColorName));
                WriteOptions(baseColorName, sourceFileTimestampUtc, (ExportUtils.GetTextureOptions(mainTexture) ?? ExportUtils.GetTextureOptions(specularTexture) ?? ExportUtils.GetTextureOptions(smoothnessTexture)).WithSRGB(false));
            }
            finally
            {
                Object.DestroyImmediate(tmpMaterial);
                DestroyTmpTexture(arguments.Diffuse, mainTexture);
                DestroyTmpTexture(arguments.PBRSpecular, specularTexture);
                DestroyTmpTexture(arguments.Smoothness, smoothnessTexture);
            }
        }

        public void ExportPBRTextures(MetallicGlossinessShaderArguments arguments, UrhoPBRMaterial urhoMaterial)
        {
            if (!string.IsNullOrWhiteSpace(urhoMaterial.MetallicRoughnessTexture))
            {
                TransformMetallicGlossiness(arguments, urhoMaterial.MetallicRoughnessTexture);
            }
        }
        public void ExportPBRTextures(SpecularGlossinessShaderArguments arguments, UrhoPBRMaterial urhoMaterial)
        {
            if (!string.IsNullOrWhiteSpace(urhoMaterial.MetallicRoughnessTexture))
            {
                TransformSpecularGlossiness(arguments, urhoMaterial.MetallicRoughnessTexture);
            }
            if (!string.IsNullOrWhiteSpace(urhoMaterial.BaseColorTexture))
            {
                TransformDiffuse(arguments, urhoMaterial.BaseColorTexture);
            }
        }
    }
}