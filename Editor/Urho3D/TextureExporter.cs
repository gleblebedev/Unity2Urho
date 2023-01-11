using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
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

        public void ExportTexture(Texture texture)
        {
            if (!_engine.Options.ExportTextures) return;
            CopyTexture(texture);
        }

        public string EvaluateTextureName(Texture texture)
        {
            if (texture == null)
                return null;
            var assetPath = AssetDatabase.GetAssetPath(texture);
            if (string.IsNullOrWhiteSpace(assetPath))
                return null;

            var newExt = Path.GetExtension(assetPath);
            if (texture is Cubemap || texture.dimension == TextureDimension.Cube)
                newExt = ".xml";
            else
                switch (newExt.ToLower())
                {
                    case ".exr":
                        newExt = ".exr";
                        break;
                    default:
                        newExt = ".dds";
                        break;
                }

            return ExportUtils.ReplaceExtension(
                ExportUtils.GetRelPathFromAssetPath(_engine.Options.Subfolder, assetPath), newExt);
        }

        public void ExportPBRTextures(MetallicGlossinessShaderArguments arguments, UrhoPBRMaterial urhoMaterial)
        {
            if (!_engine.Options.ExportTextures) return;

            if (!string.IsNullOrWhiteSpace(urhoMaterial.MetallicRoughnessTexture))
                TransformMetallicGlossiness(arguments, urhoMaterial.MetallicRoughnessTexture);
            if (!string.IsNullOrWhiteSpace(urhoMaterial.AOTexture))
                TransformAOTexture(arguments, urhoMaterial.AOTexture);
            if (!string.IsNullOrWhiteSpace(urhoMaterial.NormalTexture))
                TransformNormal(arguments.Bump, arguments.BumpScale, urhoMaterial.NormalTexture);
        }

        public void ExportPBRTextures(SpecularGlossinessShaderArguments arguments, UrhoPBRMaterial urhoMaterial)
        {
            if (!_engine.Options.ExportTextures) return;

            if (!string.IsNullOrWhiteSpace(urhoMaterial.MetallicRoughnessTexture))
                TransformSpecularGlossiness(arguments, urhoMaterial.MetallicRoughnessTexture);
            if (!string.IsNullOrWhiteSpace(urhoMaterial.BaseColorTexture))
                TransformDiffuse(arguments, urhoMaterial.BaseColorTexture);
            if (!string.IsNullOrWhiteSpace(urhoMaterial.AOTexture))
                TransformAOTexture(arguments, urhoMaterial.AOTexture);
            if (!string.IsNullOrWhiteSpace(urhoMaterial.NormalTexture))
                TransformNormal(arguments.Bump, arguments.BumpScale, urhoMaterial.NormalTexture);
        }

        public void ExportPBRTextures(AutodeskInteractiveShaderArguments arguments, UrhoPBRMaterial urhoMaterial)
        {
            if (!_engine.Options.ExportTextures) return;

            if (!string.IsNullOrWhiteSpace(urhoMaterial.MetallicRoughnessTexture))
                TransformAutodeskInteractive(arguments, urhoMaterial.MetallicRoughnessTexture);
            if (!string.IsNullOrWhiteSpace(urhoMaterial.NormalTexture))
                TransformNormal(arguments.Bump, arguments.BumpScale, urhoMaterial.NormalTexture);
        }

        protected void WriteOptions(AssetKey assetGuid, string urhoTextureName, DateTime lastWriteTimeUtc,
            TextureOptions options)
        {
            if (options == null)
                return;
            var xmlFileName = ExportUtils.ReplaceExtension(urhoTextureName, ".xml");
            if (xmlFileName == urhoTextureName)
                return;
            using (var writer = _engine.TryCreateXml(assetGuid, xmlFileName, lastWriteTimeUtc))
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

                    writer.WriteElementParameter("srgb", "enable", options.sRGBTexture ? "true" : "false");
                    writer.WriteElementParameter("mipmap", "enable", options.mipmapEnabled ? "true" : "false");
                    writer.WriteEndElement();
                }
            }
        }

        private void CopyTexture(Texture texture)
        {
            var relPath = ExportUtils.GetRelPathFromAsset(_engine.Options.Subfolder, texture);
            var newName = EvaluateTextureName(texture);
            if (relPath != newName)
            {
                CopyTextureAndSaveAs(texture);
            }
            else
            {
                if (_engine.Options.PackedNormal)
                {
                    var tImporter = AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(texture)) as TextureImporter;
                    var texType = tImporter?.textureType ?? TextureImporterType.Default;
                    if (texType == TextureImporterType.NormalMap)
                    {
                        CopyTextureAndSaveAs(texture);
                        return;
                    }
                }
                _engine.TryCopyFile(AssetDatabase.GetAssetPath(texture), newName);
                WriteOptions(texture.GetKey(), newName, ExportUtils.GetLastWriteTimeUtc(texture),
                    ExportUtils.GetTextureOptions(texture));
            }
        }

        private void CopyTextureAndSaveAs(Texture texture)
        {
            var outputAssetName = EvaluateTextureName(texture);
            var sourceFileTimestampUtc = ExportUtils.GetLastWriteTimeUtc(texture);
            var assetGuid = texture.GetKey();
            if (_engine.IsUpToDate(assetGuid, outputAssetName, sourceFileTimestampUtc)) return;

            var tImporter = AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(texture)) as TextureImporter;
            var texType = tImporter?.textureType ?? TextureImporterType.Default;
            var textureOptions = ExportUtils.GetTextureOptions(texture);
            switch (texType)
            {
                case TextureImporterType.NormalMap:
                    new TextureProcessor().ProcessAndSaveTexture(texture,
                        _engine.Options.PackedNormal
                            ? "Hidden/UnityToCustomEngineExporter/Urho3D/DecodeNormalMapPackedNormal"
                            : "Hidden/UnityToCustomEngineExporter/Urho3D/DecodeNormalMap",
                        _engine.GetTargetFilePath(outputAssetName));
                    break;
                default:
                    new TextureProcessor().ProcessAndSaveTexture(texture,
                        "Hidden/UnityToCustomEngineExporter/Urho3D/Copy", _engine.GetTargetFilePath(outputAssetName),
                        textureOptions.textureImporterFormat != TextureImporterFormat.DXT1, new Dictionary<string, float>
                        {
                            {"_GammaInput",( PlayerSettings.colorSpace == ColorSpace.Linear)?0.0f:1.0f},
                            {"_GammaOutput",1.0f},
                        });
                    WriteOptions(assetGuid, outputAssetName, sourceFileTimestampUtc,
                        textureOptions.WithSRGB(tImporter?.sRGBTexture ?? true));
                    break;
            }
        }

        private void TransformDiffuse(SpecularGlossinessShaderArguments arguments, string baseColorName)
        {
            var sourceFileTimestampUtc = ExportUtils.GetLastWriteTimeUtc(arguments.Diffuse,
                arguments.PBRSpecular.Texture, arguments.Smoothness.Texture);
            var assetGuid =
                (arguments.Diffuse ?? arguments.PBRSpecular.Texture ?? arguments.Smoothness.Texture).GetKey();
            if (_engine.IsUpToDate(assetGuid, baseColorName, sourceFileTimestampUtc)) return;

            var tmpMaterial = new Material(Shader.Find("Hidden/UnityToCustomEngineExporter/Urho3D/ConvertToBaseColor"));
            Texture mainTexture = null;
            Texture specularTexture = null;
            Texture smoothnessTexture = null;
            try
            {
                mainTexture = EnsureTexture(new TextureOrColor(arguments.Diffuse, arguments.DiffuseColor));
                specularTexture = EnsureTexture(arguments.PBRSpecular);
                smoothnessTexture = EnsureTexture(arguments.Smoothness);
                var (width, height) = MaxTexutreSize(mainTexture, specularTexture, smoothnessTexture);
                tmpMaterial.SetTexture("_MainTex", mainTexture);
                tmpMaterial.SetTexture("_SpecGlossMap", specularTexture);
                tmpMaterial.SetFloat("_SmoothnessScale",
                    arguments.GlossinessTextureScale *
                    (arguments.Smoothness.Texture != null ? 1.0f : arguments.Glossiness));
                tmpMaterial.SetTexture("_Smoothness", smoothnessTexture);
                new TextureProcessor().ProcessAndSaveTexture(mainTexture, width, height, tmpMaterial,
                    _engine.GetTargetFilePath(baseColorName));
                WriteOptions(assetGuid, baseColorName, sourceFileTimestampUtc,
                    (ExportUtils.GetTextureOptions(mainTexture) ?? ExportUtils.GetTextureOptions(specularTexture) ??
                        ExportUtils.GetTextureOptions(smoothnessTexture)).WithSRGB(true));
            }
            finally
            {
                Object.DestroyImmediate(tmpMaterial);
                DestroyTmpTexture(arguments.Diffuse, mainTexture);
                DestroyTmpTexture(arguments.PBRSpecular, specularTexture);
                DestroyTmpTexture(arguments.Smoothness, smoothnessTexture);
            }
        }

        private (int, int) MaxTexutreSize(params Texture[] textures)
        {
            var width = 1;
            var height = 1;
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
            var assetGuid = (arguments.MetallicGloss ?? arguments.Smoothness).GetKey();
            if (_engine.IsUpToDate(assetGuid, baseColorName, sourceFileTimestampUtc)) return;

            var shader = Shader.Find("Hidden/UnityToCustomEngineExporter/Urho3D/ConvertToMetallicRoughness");
            var tmpMaterial = new Material(shader);

            Texture mainTexture = null;
            Texture smoothnessTexture = null;
            try
            {
                mainTexture = EnsureTexture(new TextureOrColor(arguments.MetallicGloss,
                    new Color(arguments.Metallic, arguments.Metallic, arguments.Metallic, arguments.Glossiness)));
                smoothnessTexture =
                    EnsureTexture(new TextureOrColor(arguments.Smoothness, new Color(0, 0, 0, arguments.Glossiness)));
                var (width, height) = MaxTexutreSize(mainTexture, smoothnessTexture);
                tmpMaterial.SetTexture("_MainTex", mainTexture);
                tmpMaterial.SetFloat("_MetallicScale", arguments.MetallicScale);
                tmpMaterial.SetFloat("_SmoothnessRemapMin", arguments.SmoothnessRemapMin);
                tmpMaterial.SetFloat("_SmoothnessRemapMax", arguments.SmoothnessRemapMax);
                tmpMaterial.SetTexture("_Smoothness", smoothnessTexture);
                tmpMaterial.SetFloat("_GammaInput", (PlayerSettings.colorSpace == ColorSpace.Linear) ? 0.0f : 1.0f);
                if (_engine.Options.RBFX)
                {
                    tmpMaterial.SetTexture("_Occlusion", arguments.Occlusion);
                    tmpMaterial.SetFloat("_OcclusionStrength", arguments.OcclusionStrength);
                }
                new TextureProcessor().ProcessAndSaveTexture(mainTexture, width, height, tmpMaterial,
                    _engine.GetTargetFilePath(baseColorName));
                WriteOptions(assetGuid, baseColorName, sourceFileTimestampUtc,
                    (ExportUtils.GetTextureOptions(mainTexture) ?? ExportUtils.GetTextureOptions(smoothnessTexture))
                    .WithSRGB(false));
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
            var sourceFileTimestampUtc = ExportUtils.GetLastWriteTimeUtc(arguments.Diffuse,
                arguments.PBRSpecular.Texture, arguments.Smoothness.Texture);
            var assetGuid =
                (arguments.Diffuse ?? arguments.PBRSpecular.Texture ?? arguments.Smoothness.Texture).GetKey();
            if (_engine.IsUpToDate(assetGuid, baseColorName, sourceFileTimestampUtc)) return;

            var tmpMaterial =
                new Material(
                    Shader.Find("Hidden/UnityToCustomEngineExporter/Urho3D/ConvertSpecularToMetallicRoughness"));
            Texture mainTexture = null;
            Texture specularTexture = null;
            Texture smoothnessTexture = null;
            try
            {
                mainTexture = EnsureTexture(new TextureOrColor(arguments.Diffuse, arguments.DiffuseColor));
                specularTexture = EnsureTexture(arguments.PBRSpecular);
                smoothnessTexture = EnsureTexture(arguments.Smoothness);
                var (width, height) = MaxTexutreSize(mainTexture, specularTexture, smoothnessTexture);
                tmpMaterial.SetTexture("_MainTex", mainTexture);
                tmpMaterial.SetTexture("_SpecGlossMap", specularTexture);
                if (_engine.Options.RBFX)
                {
                    tmpMaterial.SetTexture("_Occlusion", arguments.Occlusion);
                    tmpMaterial.SetFloat("_OcclusionStrength", arguments.OcclusionStrength);
                }
                tmpMaterial.SetFloat("_SmoothnessScale",
                    arguments.GlossinessTextureScale *
                    (arguments.Smoothness.Texture != null ? 1.0f : arguments.Glossiness));
                tmpMaterial.SetTexture("_Smoothness", smoothnessTexture);
                new TextureProcessor().ProcessAndSaveTexture(mainTexture, width, height, tmpMaterial,
                    _engine.GetTargetFilePath(baseColorName));
                WriteOptions(assetGuid, baseColorName, sourceFileTimestampUtc,
                    (ExportUtils.GetTextureOptions(mainTexture) ?? ExportUtils.GetTextureOptions(specularTexture) ??
                        ExportUtils.GetTextureOptions(smoothnessTexture)).WithSRGB(false));
            }
            finally
            {
                Object.DestroyImmediate(tmpMaterial);
                DestroyTmpTexture(arguments.Diffuse, mainTexture);
                DestroyTmpTexture(arguments.PBRSpecular, specularTexture);
                DestroyTmpTexture(arguments.Smoothness, smoothnessTexture);
            }
        }

        private void TransformAutodeskInteractive(AutodeskInteractiveShaderArguments arguments, string baseColorName)
        {
            var sourceFileTimestampUtc = ExportUtils.GetLastWriteTimeUtc(arguments.Diffuse,
                arguments.RoughnessMap, arguments.MetallicMap, arguments.Occlusion);
            var assetGuid =
                (arguments.Diffuse ?? arguments.RoughnessMap ?? arguments.MetallicMap ?? arguments.Occlusion).GetKey();
            if (_engine.IsUpToDate(assetGuid, baseColorName, sourceFileTimestampUtc)) return;

            var tmpMaterial =
                new Material(
                    Shader.Find("Hidden/UnityToCustomEngineExporter/Urho3D/CombineMetallicRoughnessOcclusion"));
            Texture metallicMap = null;
            Texture roughnessMap = null;
            Texture occlusionMap = null;
            try
            {
                roughnessMap = arguments.RoughnessMap;
                metallicMap = arguments.MetallicMap;
                occlusionMap = arguments.Occlusion;
                var (width, height) = MaxTexutreSize(occlusionMap, metallicMap, roughnessMap);
                tmpMaterial.SetTexture("_RoughnessMap", roughnessMap);
                tmpMaterial.SetFloat("_RoughnessScale", (arguments.RoughnessMap != null) ? 1.0f : arguments.Glossiness);
                tmpMaterial.SetFloat("_RoughnessOffset", 0.0f);
                tmpMaterial.SetVector("_RoughnessMask", new Vector4(1,1,1,0));
                tmpMaterial.SetTexture("_OcclusionMap", arguments.Occlusion);
                tmpMaterial.SetFloat("_OcclusionScale", arguments.OcclusionStrength);
                tmpMaterial.SetFloat("_OcclusionOffset", 0.0f);
                tmpMaterial.SetVector("_OcclusionMask", new Vector4(1, 1, 1, 0));
                tmpMaterial.SetTexture("_MetallicMap", metallicMap);
                tmpMaterial.SetFloat("_MetallicScale", arguments.OcclusionStrength);
                tmpMaterial.SetFloat("_MetallicOffset", 0.0f);
                tmpMaterial.SetVector("_MetallicMask", new Vector4(1, 1, 1, 0));

                new TextureProcessor().ProcessAndSaveTexture(metallicMap ?? roughnessMap ?? occlusionMap, width, height, tmpMaterial,
                    _engine.GetTargetFilePath(baseColorName));
                WriteOptions(assetGuid, baseColorName, sourceFileTimestampUtc,
                    (ExportUtils.GetTextureOptions(roughnessMap) ??
                        ExportUtils.GetTextureOptions(metallicMap) ??
                        ExportUtils.GetTextureOptions(occlusionMap)).WithSRGB(false));
            }
            finally
            {
                Object.DestroyImmediate(tmpMaterial);
                DestroyTmpTexture(arguments.RoughnessMap, roughnessMap);
                DestroyTmpTexture(arguments.MetallicMap, metallicMap);
                DestroyTmpTexture(arguments.Occlusion, occlusionMap);
            }
        }
        private void TransformNormal(Texture bump, float bumpScale, string baseColorName)
        {
            if (bump == null)
                return;

            if (bumpScale >= 0.999f)
            {
                CopyTexture(bump);
                return;
            }

            var sourceFileTimestampUtc = ExportUtils.GetLastWriteTimeUtc(bump);
            var assetGuid = bump.GetKey();
            if (_engine.IsUpToDate(assetGuid, baseColorName, sourceFileTimestampUtc)) return;

            Material tmpMaterial;
            if (this._engine.Options.PackedNormal)
            {
                tmpMaterial = new Material(Shader.Find("Hidden/UnityToCustomEngineExporter/Urho3D/DecodeNormalMapPackedNormal"));
            }
            else
            {
                tmpMaterial = new Material(Shader.Find("Hidden/UnityToCustomEngineExporter/Urho3D/DecodeNormalMap"));
            }
            try
            {
                var mainTexture = bump;
                tmpMaterial.SetTexture("_MainTex", mainTexture);
                tmpMaterial.SetFloat("_BumpScale", bumpScale);
                new TextureProcessor().ProcessAndSaveTexture(mainTexture, tmpMaterial,
                    _engine.GetTargetFilePath(baseColorName));
                WriteOptions(assetGuid, baseColorName, sourceFileTimestampUtc,
                    ExportUtils.GetTextureOptions(mainTexture).WithSRGB(false));
            }
            finally
            {
                Object.DestroyImmediate(tmpMaterial);
            }
        }

        private void TransformAOTexture(ShaderArguments arguments, string baseColorName)
        {
            if (arguments.Occlusion == null)
                return;
            var sourceFileTimestampUtc = ExportUtils.GetLastWriteTimeUtc(arguments.Occlusion);
            var assetGuid = arguments.Occlusion.GetKey();
            if (_engine.IsUpToDate(assetGuid, baseColorName, sourceFileTimestampUtc)) return;

            var tmpMaterial =
                new Material(Shader.Find("Hidden/UnityToCustomEngineExporter/Urho3D/PremultiplyOcclusionStrength"));
            try
            {
                var mainTexture = arguments.Occlusion;
                tmpMaterial.SetTexture("_MainTex", mainTexture);
                tmpMaterial.SetFloat("_OcclusionStrength", arguments.OcclusionStrength);
                new TextureProcessor().ProcessAndSaveTexture(mainTexture, tmpMaterial,
                    _engine.GetTargetFilePath(baseColorName));
                WriteOptions(assetGuid, baseColorName, sourceFileTimestampUtc,
                    ExportUtils.GetTextureOptions(mainTexture).WithSRGB(true));
            }
            finally
            {
                Object.DestroyImmediate(tmpMaterial);
            }
        }
    }
}