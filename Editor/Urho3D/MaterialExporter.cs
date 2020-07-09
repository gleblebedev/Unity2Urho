using System;
using System.Globalization;
using System.IO;
using System.Text;
using System.Xml;
using Assets.Scripts.UnityToCustomEngineExporter.Editor.Urho3D;
using UnityEditor;
using UnityEngine;

namespace UnityToCustomEngineExporter.Editor.Urho3D
{
    public class MaterialExporter
    {
        public static Technique[] Techniques =
        {
            new Technique {Material = new LegacyTechniqueFlags(), Name = "NoTexture.xml"},
            new Technique {Material = new LegacyTechniqueFlags {hasAlpha = true}, Name = "NoTextureAlpha.xml"},
            new Technique {Material = new LegacyTechniqueFlags {hasNormal = true}, Name = "NoTextureNormal.xml"},
            new Technique
            {
                Material = new LegacyTechniqueFlags {hasNormal = true, hasAlpha = true},
                Name = "NoTextureNormalAlpha.xml"
            },
            //new Technique
            //{
            //    Material = new MaterialFlags {hasNormal = true, hasAlpha = true, hasEmissive = true},
            //    Name = "NoTextureNormalEmissiveAlpha.xml"
            //},
            new Technique {Material = new LegacyTechniqueFlags {hasDiffuse = true}, Name = "Diff.xml"},
            new Technique
                {Material = new LegacyTechniqueFlags {hasDiffuse = true, hasAlpha = true}, Name = "DiffAlpha.xml"},
            new Technique
                {Material = new LegacyTechniqueFlags {hasDiffuse = true, hasSpecular = true}, Name = "DiffSpec.xml"},
            new Technique
            {
                Material = new LegacyTechniqueFlags {hasDiffuse = true, hasSpecular = true, hasAlpha = true},
                Name = "DiffSpecAlpha.xml"
            },
            new Technique
                {Material = new LegacyTechniqueFlags {hasDiffuse = true, hasNormal = true}, Name = "DiffNormal.xml"},
            new Technique
            {
                Material = new LegacyTechniqueFlags {hasDiffuse = true, hasNormal = true, hasAlpha = true},
                Name = "DiffNormalAlpha.xml"
            },
            new Technique
            {
                Material = new LegacyTechniqueFlags {hasDiffuse = true, hasEmissive = true},
                Name = "DiffEmissive.xml"
            },
            new Technique
            {
                Material = new LegacyTechniqueFlags {hasDiffuse = true, hasEmissive = true, hasAlpha = true},
                Name = "DiffEmissiveAlpha.xml"
            },
            new Technique
            {
                Material = new LegacyTechniqueFlags {hasDiffuse = true, hasSpecular = true, hasNormal = true},
                Name = "DiffNormalSpec.xml"
            },
            new Technique
            {
                Material = new LegacyTechniqueFlags
                    {hasDiffuse = true, hasSpecular = true, hasNormal = true, hasAlpha = true},
                Name = "DiffNormalSpecAlpha.xml"
            },
            new Technique
            {
                Material = new LegacyTechniqueFlags {hasDiffuse = true, hasEmissive = true, hasNormal = true},
                Name = "DiffNormalEmissive.xml"
            },
            new Technique
            {
                Material = new LegacyTechniqueFlags
                    {hasDiffuse = true, hasEmissive = true, hasNormal = true, hasAlpha = true},
                Name = "DiffNormalEmissiveAlpha.xml"
            },
            new Technique
            {
                Material = new LegacyTechniqueFlags
                {
                    hasDiffuse = true,
                    hasSpecular = true,
                    hasNormal = true,
                    hasEmissive = true
                },
                Name = "DiffNormalSpecEmissive.xml"
            },
            new Technique
            {
                Material = new LegacyTechniqueFlags
                {
                    hasDiffuse = true,
                    hasSpecular = true,
                    hasNormal = true,
                    hasEmissive = true,
                    hasAlpha = true
                },
                Name = "DiffNormalSpecEmissiveAlpha.xml"
            }
        };

        private readonly Urho3DEngine _engine;

        public MaterialExporter(Urho3DEngine engine)
        {
            _engine = engine;
        }

        private static void WriteAlphaTest(XmlWriter writer)
        {
            writer.WriteWhitespace("\t");
            writer.WriteStartElement("shader");
            writer.WriteAttributeString("psdefines", "ALPHAMASK");
            writer.WriteEndElement();
            writer.WriteWhitespace("\n");
        }

        public UrhoPBRMaterial FromSpecularGlossiness(SpecularGlossinessShaderArguments arguments)
        {
            var material = new UrhoPBRMaterial();
            material.NormalTexture = _engine.EvaluateTextrueName(arguments.Bump);
            material.EmissiveTexture = _engine.EvaluateTextrueName(arguments.Emission);
            material.AOTexture = BuildAOTextureName(arguments.Occlusion, arguments.OcclusionStrength);
            var diffuseTextrueName = _engine.EvaluateTextrueName(arguments.Diffuse);
            var specularTexture = _engine.EvaluateTextrueName(arguments.PBRSpecular.Texture);
            string smoothnessTexture;
            if (arguments.Smoothness.Texture == arguments.Diffuse)
                smoothnessTexture = diffuseTextrueName;
            else
                smoothnessTexture = specularTexture;

            if (string.IsNullOrWhiteSpace(specularTexture) && string.IsNullOrWhiteSpace(diffuseTextrueName))
            {
                var pbrValues = PBRUtils.ConvertToMetallicRoughnessSRGB(new PBRUtils.SpecularGlossiness
                {
                    diffuse = arguments.DiffuseColor,
                    specular = arguments.PBRSpecular.Color,
                    opacity = arguments.DiffuseColor.a,
                    glossiness = arguments.Glossiness
                }).linear();
                material.BaseColor = pbrValues.baseColor;
                material.Metallic = pbrValues.metallic;
                material.Roughness = pbrValues.roughness;
            }
            else
            {
                {
                    var baseColorTextureNameBuilder = new StringBuilder();
                    if (!string.IsNullOrWhiteSpace(diffuseTextrueName))
                        baseColorTextureNameBuilder.Append(
                            Path.GetDirectoryName(diffuseTextrueName).FixAssetSeparator());
                    else
                        baseColorTextureNameBuilder.Append(Path.GetDirectoryName(specularTexture).FixAssetSeparator());
                    if (baseColorTextureNameBuilder.Length > 0) baseColorTextureNameBuilder.Append('/');
                    if (!string.IsNullOrWhiteSpace(diffuseTextrueName))
                        baseColorTextureNameBuilder.Append(Path.GetFileNameWithoutExtension(diffuseTextrueName));
                    else
                        baseColorTextureNameBuilder.Append(FormatRGB(arguments.DiffuseColor.linear));
                    baseColorTextureNameBuilder.Append('.');
                    if (!string.IsNullOrWhiteSpace(specularTexture))
                        baseColorTextureNameBuilder.Append(Path.GetFileNameWithoutExtension(specularTexture));
                    else
                        baseColorTextureNameBuilder.Append(FormatRGB(arguments.PBRSpecular.Color.linear));

                    baseColorTextureNameBuilder.Append(".BaseColor.png");
                    material.BaseColorTexture = baseColorTextureNameBuilder.ToString();
                }
                {
                    var metallicTextureNameBuilder = new StringBuilder();
                    if (!string.IsNullOrWhiteSpace(specularTexture))
                        metallicTextureNameBuilder.Append(Path.GetDirectoryName(specularTexture).FixAssetSeparator());
                    else
                        metallicTextureNameBuilder.Append(Path.GetDirectoryName(diffuseTextrueName)
                            .FixAssetSeparator());
                    if (metallicTextureNameBuilder.Length > 0) metallicTextureNameBuilder.Append('/');
                    if (!string.IsNullOrWhiteSpace(specularTexture))
                        metallicTextureNameBuilder.Append(Path.GetFileNameWithoutExtension(specularTexture));
                    else
                        metallicTextureNameBuilder.Append(FormatRGB(arguments.PBRSpecular.Color.linear));

                    metallicTextureNameBuilder.Append('.');
                    if (!string.IsNullOrWhiteSpace(diffuseTextrueName))
                        metallicTextureNameBuilder.Append(Path.GetFileNameWithoutExtension(diffuseTextrueName));
                    else
                        metallicTextureNameBuilder.Append(FormatRGB(arguments.DiffuseColor.linear));

                    if (!string.IsNullOrWhiteSpace(smoothnessTexture))
                    {
                        if (arguments.GlossinessTextureScale < 0.999f)
                        {
                            metallicTextureNameBuilder.Append('.');
                            metallicTextureNameBuilder.AppendFormat(CultureInfo.InvariantCulture, "{0:0.000}",
                                arguments.GlossinessTextureScale);
                        }
                    }
                    else
                    {
                        if (arguments.Glossiness > 0)
                        {
                            metallicTextureNameBuilder.Append('.');
                            metallicTextureNameBuilder.AppendFormat(CultureInfo.InvariantCulture, "{0:0.000}",
                                arguments.Glossiness);
                        }
                    }

                    metallicTextureNameBuilder.Append(".MetallicRoughness.png");
                    material.MetallicRoughnessTexture = metallicTextureNameBuilder.ToString();
                }

                if (arguments.Diffuse != null)
                    material.BaseColor = arguments.DiffuseColor.linear;
                else
                    material.BaseColor = Color.white;
            }

            material.AlphaBlend = arguments.Transparent;
            material.AlphaTest = arguments.AlphaTest;
            if (arguments.Emission != null)
                material.EmissiveColor = Color.white;
            else
                material.EmissiveColor = arguments.EmissiveColor.linear;
            material.MatSpecColor = new Color(1, 1, 1, 0);
            material.UOffset = new Vector4(arguments.MainTextureScale.x, 0, 0, arguments.MainTextureOffset.x);
            material.VOffset = new Vector4(0, arguments.MainTextureScale.y, 0, arguments.MainTextureOffset.y);
            material.EvaluateTechnique();
            return material;
        }

        public UrhoPBRMaterial FromMetallicGlossiness(MetallicGlossinessShaderArguments arguments)
        {
            var material = new UrhoPBRMaterial();
            material.NormalTexture = _engine.EvaluateTextrueName(arguments.Bump);
            material.EmissiveTexture = _engine.EvaluateTextrueName(arguments.Emission);
            material.AOTexture = BuildAOTextureName(arguments.Occlusion, arguments.OcclusionStrength);
            material.BaseColorTexture = _engine.EvaluateTextrueName(arguments.BaseColor);
            var metalicGlossinesTexture = _engine.EvaluateTextrueName(arguments.MetallicGloss);
            var smoothnessTexture = _engine.EvaluateTextrueName(arguments.Smoothness);
            var linearMetallic = new Color(arguments.Metallic, 0, 0, 1).linear.r;
            if (string.IsNullOrWhiteSpace(metalicGlossinesTexture) && string.IsNullOrWhiteSpace(smoothnessTexture))
            {
                material.Metallic = linearMetallic;
                material.Roughness = 1.0f - arguments.Glossiness;
            }
            else
            {
                var texNameBuilder = new StringBuilder();
                if (!string.IsNullOrWhiteSpace(metalicGlossinesTexture))
                    texNameBuilder.Append(Path.GetDirectoryName(metalicGlossinesTexture).FixAssetSeparator());
                else
                    texNameBuilder.Append(Path.GetDirectoryName(smoothnessTexture).FixAssetSeparator());
                if (texNameBuilder.Length > 0) texNameBuilder.Append('/');

                if (!string.IsNullOrWhiteSpace(metalicGlossinesTexture))
                    texNameBuilder.Append(Path.GetFileNameWithoutExtension(metalicGlossinesTexture));
                else
                    texNameBuilder.AppendFormat(CultureInfo.InvariantCulture, "{0:0.00}", linearMetallic);

                if (smoothnessTexture != metalicGlossinesTexture)
                {
                    texNameBuilder.Append('.');
                    texNameBuilder.Append(Path.GetFileNameWithoutExtension(smoothnessTexture));
                }

                if (arguments.GlossinessTextureScale < 0.999f)
                {
                    texNameBuilder.Append('.');
                    texNameBuilder.AppendFormat(CultureInfo.InvariantCulture, "{0:0.000}",
                        arguments.GlossinessTextureScale);
                }

                texNameBuilder.Append(".MetallicRoughness.png");
                material.MetallicRoughnessTexture = texNameBuilder.ToString();
            }

            material.BaseColor = arguments.BaseColorColor.linear;
            material.AlphaBlend = arguments.Transparent;
            material.AlphaTest = arguments.AlphaTest;
            material.EmissiveColor = arguments.EmissiveColor.linear;
            material.MatSpecColor = new Color(1, 1, 1, 0);
            material.UOffset = new Vector4(arguments.MainTextureScale.x, 0, 0, arguments.MainTextureOffset.x);
            material.VOffset = new Vector4(0, arguments.MainTextureScale.y, 0, arguments.MainTextureOffset.y);
            material.EvaluateTechnique();
            return material;
        }
        //private void ExportStandartMaterial(Material mat, XmlWriter xmlStream)
        //{
        //    var _MainTex = mat.GetTexture("_MainTex");
        //    var _BumpMap = mat.GetTexture("_BumpMap");
        //    var _DetailNormalMap = mat.GetTexture("_DetailNormalMap");
        //    var _ParallaxMap = mat.GetTexture("_ParallaxMap");
        //    var _OcclusionMap = mat.GetTexture("_OcclusionMap");
        //    var _EmissionMap = mat.GetTexture("_EmissionMap");
        //    var _DetailMask = mat.GetTexture("_DetailMask");
        //    var _DetailAlbedoMap = mat.GetTexture("_DetailAlbedoMap");
        //    var _MetallicGlossMap = mat.GetTexture("_MetallicGlossMap");

        //    WriteTechnique(xmlStream, "\t", "Techniques/Diff.xml");
        //    if (_MainTex != null)
        //    {
        //        string t;
        //        if (_assets.TryGetTexturePath(_MainTex, out t))
        //        {
        //            WriteTexture(xmlStream, "\t", "diffuse", t);
        //        }
        //    }
        //}

        public string EvaluateMaterialName(Material material)
        {
            if (material == null)
                return null;
            var assetPath = AssetDatabase.GetAssetPath(material);
            if (string.IsNullOrWhiteSpace(assetPath))
                return null;
            if (assetPath.EndsWith(".mat", StringComparison.InvariantCultureIgnoreCase))
                return ExportUtils.ReplaceExtension(ExportUtils.GetRelPathFromAssetPath(_engine.Subfolder, assetPath),
                    ".xml");
            var newExt = "/" + ExportUtils.SafeFileName(material.name) + ".xml";
            return ExportUtils.ReplaceExtension(ExportUtils.GetRelPathFromAssetPath(_engine.Subfolder, assetPath),
                newExt);
        }

        public void ExportMaterial(Material material)
        {
            var path = EvaluateMaterialName(material);
            var mat = new MaterialDescription(material);
            var assetGuid = material.GetKey();
            if (mat.SpecularGlossiness != null)
                ExportSpecularGlossiness(assetGuid, path, mat.SpecularGlossiness);
            else if (mat.MetallicGlossiness != null)
                ExportMetallicRoughness(assetGuid, path, mat.MetallicGlossiness);
            else if (mat.Skybox != null)
                ExportSkybox(assetGuid, path, mat.Skybox);
            else
                ExportLegacy(assetGuid, path, mat.Legacy ?? new LegacyShaderArguments());
        }

        private string FormatRGB(Color32 color)
        {
            return string.Format("{0:x2}{1:x2}{2:x2}", color.r, color.g, color.b);
        }

        private string BuildAOTextureName(Texture occlusion, float occlusionStrength)
        {
            if (occlusion == null)
                return null;
            if (occlusionStrength <= 0)
                return null;
            var baseName = _engine.EvaluateTextrueName(occlusion);
            if (occlusionStrength >= 0.999f)
                return ExportUtils.ReplaceExtension(baseName, ".AO.png");
            return ExportUtils.ReplaceExtension(baseName,
                string.Format(CultureInfo.InvariantCulture, ".AO.{0:0.000}.png", occlusionStrength));
        }

        private void WriteTechnique(XmlWriter writer, string name)
        {
            writer.WriteWhitespace("\t");
            writer.WriteStartElement("technique");
            writer.WriteAttributeString("name", name);
            writer.WriteEndElement();
            writer.WriteWhitespace("\n");
        }

        private bool WriteTexture(Texture texture, XmlWriter writer, string name)
        {
            _engine.ScheduleAssetExport(texture);
            var urhoAssetName = _engine.EvaluateTextrueName(texture);
            return WriteTexture(urhoAssetName, writer, name);
        }

        private bool WriteTexture(string urhoAssetName, XmlWriter writer, string name)
        {
            if (string.IsNullOrWhiteSpace(urhoAssetName))
                return false;
            {
                writer.WriteWhitespace("\t");
                writer.WriteStartElement("texture");
                writer.WriteAttributeString("unit", name);
                writer.WriteAttributeString("name", urhoAssetName);
                writer.WriteEndElement();
                writer.WriteWhitespace(Environment.NewLine);
            }
            return true;
        }

        private void ExportSkybox(AssetKey assetGuid, string urhoPath, SkyboxShaderArguments arguments)
        {
            using (var writer = _engine.TryCreateXml(assetGuid, urhoPath, DateTime.MaxValue))
            {
                if (writer == null)
                    return;
                //writer.WriteStartDocument();
                //writer.WriteWhitespace(Environment.NewLine);
                writer.WriteStartElement("material");
                writer.WriteWhitespace(Environment.NewLine);
                var technique = "Techniques/DiffSkyboxHDRScale.xml";

                var anyFace = arguments.BackTex ?? arguments.DownTex ?? arguments.FrontTex ??
                              arguments.LeftTex ?? arguments.RightTex ?? arguments.UpTex;
                if (arguments.Skybox != null)
                {
                    _engine.ScheduleAssetExport(arguments.Skybox);
                    string name;
                    if (arguments.Skybox is Cubemap cubemap)
                    {
                        name = _engine.EvaluateCubemapName(cubemap);
                    }
                    else
                    {
                        name = _engine.EvaluateTextrueName(arguments.Skybox);
                        technique = "Techniques/DiffSkydome.xml";
                    }

                    if (!string.IsNullOrWhiteSpace(name)) WriteTexture(name, writer, "diffuse");
                }
                else if (anyFace != null)
                {
                    var cubemapName = ExportUtils.ReplaceExtension(urhoPath, ".Cubemap.xml");
                    using (var cubemapWriter = _engine.TryCreateXml(assetGuid, cubemapName, DateTime.MaxValue))
                    {
                        if (cubemapWriter != null)
                        {
                            cubemapWriter.WriteStartElement("cubemap");
                            foreach (var tex in new[]
                            {
                                arguments.RightTex, arguments.LeftTex, arguments.UpTex, arguments.DownTex,
                                arguments.FrontTex, arguments.BackTex
                            })
                            {
                                cubemapWriter.WriteWhitespace(Environment.NewLine);
                                cubemapWriter.WriteStartElement("face");
                                cubemapWriter.WriteAttributeString("name", _engine.EvaluateTextrueName(tex ?? anyFace));
                                cubemapWriter.WriteEndElement();
                                _engine.ScheduleTexture(tex, new TextureReference(TextureSemantic.Other));
                            }

                            cubemapWriter.WriteWhitespace(Environment.NewLine);
                            cubemapWriter.WriteStartElement("address");
                            cubemapWriter.WriteAttributeString("coord", "uv");
                            cubemapWriter.WriteAttributeString("mode", "clamp");
                            cubemapWriter.WriteEndElement();
                            cubemapWriter.WriteWhitespace(Environment.NewLine);
                            cubemapWriter.WriteStartElement("mipmap");
                            cubemapWriter.WriteAttributeString("enable", "true");
                            cubemapWriter.WriteEndElement();
                            cubemapWriter.WriteWhitespace(Environment.NewLine);
                            cubemapWriter.WriteStartElement("filter");
                            cubemapWriter.WriteAttributeString("mode", "trilinear");
                            cubemapWriter.WriteEndElement();
                            cubemapWriter.WriteWhitespace(Environment.NewLine);
                            cubemapWriter.WriteEndElement();
                        }
                    }

                    WriteTexture(cubemapName, writer, "diffuse");
                }
                else
                {
                    WriteTexture("Resources/unity_builtin_extra/Default-Skybox-Map.xml", writer, "diffuse");
                }

                WriteTechnique(writer, technique);

                {
                    writer.WriteWhitespace("\t");
                    writer.WriteStartElement("cull");
                    writer.WriteAttributeString("value", "none");
                    writer.WriteEndElement();
                    writer.WriteWhitespace(Environment.NewLine);

                    writer.WriteWhitespace("\t");
                    writer.WriteStartElement("shadowcull");
                    writer.WriteAttributeString("value", "ccw");
                    writer.WriteEndElement();
                    writer.WriteWhitespace(Environment.NewLine);

                    writer.WriteWhitespace("\t");
                    writer.WriteStartElement("shader");
                    writer.WriteAttributeString("vsdefines", "IGNORENODETRANSFORM");
                    writer.WriteEndElement();
                    writer.WriteWhitespace(Environment.NewLine);
                }

                writer.WriteEndElement();
            }
        }

        private void ExportLegacy(AssetKey assetGuid, string urhoPath, LegacyShaderArguments arguments)
        {
            using (var writer = _engine.TryCreateXml(assetGuid, urhoPath, DateTime.MaxValue))
            {
                if (writer == null)
                    return;
                var flags = new LegacyTechniqueFlags();
                flags.hasAlpha = arguments.Transparent;
                flags.hasDiffuse = arguments.Diffuse != null;
                flags.hasEmissive = arguments.Emission != null;
                flags.hasNormal = arguments.Bump != null;
                flags.hasSpecular = arguments.Specular != null;
                writer.WriteStartElement("material");
                writer.WriteWhitespace(Environment.NewLine);
                {
                    var bestTechnique = Techniques[0];
                    var bestTechniqueDistance = bestTechnique.Material - flags;
                    foreach (var technique in Techniques)
                        if (technique.Material.Fits(flags))
                        {
                            var d = technique.Material - flags;
                            if (d < bestTechniqueDistance)
                            {
                                bestTechnique = technique;
                                bestTechniqueDistance = d;
                            }
                        }

                    WriteTechnique(writer, "Techniques/" + bestTechnique.Name);
                }
                if (arguments.Diffuse != null) WriteTexture(arguments.Diffuse, writer, "diffuse");
                if (arguments.Specular != null) WriteTexture(arguments.Specular, writer, "specular");
                if (arguments.Bump != null) WriteTexture(arguments.Bump, writer, "normal");
                if (arguments.Emission != null) WriteTexture(arguments.Bump, writer, "emissive");
                writer.WriteParameter("MatDiffColor", arguments.DiffColor);
                if (arguments.HasEmission)
                    writer.WriteParameter("MatEmissiveColor", BaseNodeExporter.FormatRGB(arguments.EmissiveColor));
                WriteCommonParameters(writer, arguments);

                writer.WriteEndElement();
            }
        }

        private void ExportMetallicRoughness(AssetKey assetGuid, string urhoPath,
            MetallicGlossinessShaderArguments arguments)
        {
            using (var writer = _engine.TryCreateXml(assetGuid, urhoPath, DateTime.MaxValue))
            {
                if (writer == null)
                    return;

                var urhoMaterial = FromMetallicGlossiness(arguments);
                var shaderName = arguments.Shader;
                ExportMaterial(writer, shaderName, urhoMaterial);

                _engine.ScheduleTexture(arguments.BaseColor, new TextureReference(TextureSemantic.PBRBaseColor));
                _engine.ScheduleTexture(arguments.Bump, new TextureReference(TextureSemantic.Bump));
                _engine.ScheduleTexture(arguments.DetailBaseColor, new TextureReference(TextureSemantic.Detail));

                _engine.SchedulePBRTextures(arguments, urhoMaterial);

                _engine.ScheduleTexture(arguments.Emission, new TextureReference(TextureSemantic.Emission));
                //_engine.ScheduleTexture(arguments.Occlusion, new TextureReference(TextureSemantic.Occlusion));
            }
        }

        private void ExportMaterial(XmlWriter writer, string shaderName, UrhoPBRMaterial urhoMaterial)
        {
            writer.WriteStartElement("material");
            writer.WriteWhitespace(Environment.NewLine);

            if (shaderName == "Urho3D/PBR/PBRVegetation")
            {
                var technique = "Techniques/PBR/PBRVegetationDiff.xml";
                WriteTechnique(writer, technique);
                writer.WriteParameter("WindHeightFactor", 0.1f);
                writer.WriteParameter("WindHeightPivot", 0.01f);
                writer.WriteParameter("WindPeriod", 0.5f);
                writer.WriteParameter("WindWorldSpacing", new Vector2(0.5f, 0.5f));
            }
            else
            {
                WriteTechnique(writer, urhoMaterial.Technique);
            }

            WriteTexture(urhoMaterial.BaseColorTexture, writer, "diffuse");
            WriteTexture(urhoMaterial.NormalTexture, writer, "normal");
            WriteTexture(urhoMaterial.MetallicRoughnessTexture, writer, "specular");
            if (!string.IsNullOrWhiteSpace(urhoMaterial.EmissiveTexture))
                WriteTexture(urhoMaterial.EmissiveTexture, writer, "emissive");
            else
                WriteTexture(urhoMaterial.AOTexture, writer, "emissive");

            writer.WriteParameter("MatEmissiveColor", urhoMaterial.EmissiveColor);
            writer.WriteParameter("MatDiffColor", urhoMaterial.BaseColor);
            writer.WriteParameter("MatEnvMapColor", urhoMaterial.MatEnvMapColor);
            writer.WriteParameter("MatSpecColor", urhoMaterial.MatSpecColor);
            writer.WriteParameter("Roughness", urhoMaterial.Roughness);
            writer.WriteParameter("Metallic", urhoMaterial.Metallic);
            writer.WriteParameter("UOffset", urhoMaterial.UOffset);
            writer.WriteParameter("VOffset", urhoMaterial.VOffset);
            if (urhoMaterial.AlphaTest) WriteAlphaTest(writer);

            writer.WriteEndElement();
        }


        private void ExportSpecularGlossiness(AssetKey assetGuid, string urhoPath,
            SpecularGlossinessShaderArguments arguments)
        {
            using (var writer = _engine.TryCreateXml(assetGuid, urhoPath, DateTime.MaxValue))
            {
                if (writer == null)
                    return;

                var urhoMaterial = FromSpecularGlossiness(arguments);
                var shaderName = arguments.Shader;
                ExportMaterial(writer, shaderName, urhoMaterial);

                _engine.ScheduleTexture(arguments.Bump, new TextureReference(TextureSemantic.Bump));

                _engine.SchedulePBRTextures(arguments, urhoMaterial);

                _engine.ScheduleTexture(arguments.Emission, new TextureReference(TextureSemantic.Emission));
                //_engine.ScheduleTexture(arguments.Occlusion, new TextureReference(TextureSemantic.Occlusion));
            }
        }

        private void WriteCommonParameters(XmlWriter writer, ShaderArguments arguments)
        {
            writer.WriteParameter("UOffset", new Vector4(arguments.MainTextureScale.x, 0, 0,
                arguments.MainTextureOffset.x));
            writer.WriteParameter("VOffset", new Vector4(0, arguments.MainTextureScale.y, 0,
                arguments.MainTextureOffset.y));
            if (arguments.AlphaTest) WriteAlphaTest(writer);
        }
    }
}