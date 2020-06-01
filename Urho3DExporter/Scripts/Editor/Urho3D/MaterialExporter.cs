using System;
using System.Xml;
using Assets.Urho3DExporter.Scripts.Editor;
using UnityEditor;
using UnityEngine;

namespace Urho3DExporter
{
    public class MaterialExporter : IExporter
    {
        private readonly AssetCollection _assets;

        public MaterialExporter(AssetCollection assets, TextureMetadataCollection textureMetadataCollection) : base()
        {
            _assets = assets;
        }
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
            new Technique {Material = new LegacyTechniqueFlags {hasDiffuse = true, hasAlpha = true}, Name = "DiffAlpha.xml"},
            new Technique {Material = new LegacyTechniqueFlags {hasDiffuse = true, hasSpecular = true}, Name = "DiffSpec.xml"},
            new Technique
            {
                Material = new LegacyTechniqueFlags {hasDiffuse = true, hasSpecular = true, hasAlpha = true},
                Name = "DiffSpecAlpha.xml"
            },
            new Technique {Material = new LegacyTechniqueFlags {hasDiffuse = true, hasNormal = true}, Name = "DiffNormal.xml"},
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
                Material = new LegacyTechniqueFlags {hasDiffuse = true, hasSpecular = true, hasNormal = true, hasAlpha = true},
                Name = "DiffNormalSpecAlpha.xml"
            },
            new Technique
            {
                Material = new LegacyTechniqueFlags {hasDiffuse = true, hasEmissive = true, hasNormal = true},
                Name = "DiffNormalEmissive.xml"
            },
            new Technique
            {
                Material = new LegacyTechniqueFlags {hasDiffuse = true, hasEmissive = true, hasNormal = true, hasAlpha = true},
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
            string urhoAssetName;
            if (!_assets.TryGetTexturePath(texture, out urhoAssetName))
                return false;
            {
                WriteTexture(urhoAssetName, writer, name);
            }
            return true;
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
        private void ExportMaterial(AssetContext asset, Material material)
        {
            var mat = new MaterialDescription(material);
            if (mat.SpecularGlossiness != null)
            {
                ExportSpecularGlossiness(asset, mat.SpecularGlossiness);
            }
            else if (mat.MetallicRoughness != null)
            {
                ExportMetallicRoughness(asset, mat.MetallicRoughness);
            }
            else
            {
                ExportLegacy(asset, mat.Legacy ?? new LegacyShaderArguments());
            }
        }

        private void ExportLegacy(AssetContext asset, LegacyShaderArguments arguments)
        {
            using (XmlTextWriter writer = asset.DestinationFolder.CreateXml(asset.UrhoAssetName))
            {
                if (writer == null)
                    return;
                writer.WriteStartDocument(); writer.WriteWhitespace(Environment.NewLine);

                var flags = new LegacyTechniqueFlags();
                flags.hasAlpha = arguments.Transparent;
                flags.hasDiffuse = arguments.Diffuse != null;
                flags.hasEmissive = arguments.Emission != null;
                flags.hasNormal = arguments.Bump != null;
                flags.hasSpecular = arguments.Specular != null;
                writer.WriteStartElement("material"); writer.WriteWhitespace(Environment.NewLine);
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
                    WriteTechnique(writer, "Techniques/"+bestTechnique.Name);
                }
                if (arguments.Diffuse != null) WriteTexture(arguments.Diffuse, writer, "diffuse");
                if (arguments.Specular != null) WriteTexture(arguments.Specular, writer, "specular");
                if (arguments.Bump != null) WriteTexture(arguments.Bump, writer, "normal");
                if (arguments.Emission != null) WriteTexture(arguments.Bump, writer, "emissive");
                writer.WriteEndElement();
                writer.WriteEndDocument();
            }
        }

        private void ExportMetallicRoughness(AssetContext asset, MetallicRoughnessShaderArguments arguments)
        {
            using (XmlTextWriter writer = asset.DestinationFolder.CreateXml(asset.UrhoAssetName))
            {
                if (writer == null)
                    return;
                writer.WriteStartDocument(); writer.WriteWhitespace(Environment.NewLine);
                writer.WriteStartElement("material"); writer.WriteWhitespace(Environment.NewLine);

                if (arguments.Albedo != null)
                {   // Albedo
                    if (arguments.MetallicGloss != null)
                    {   // Albedo, MetallicGloss
                        if (arguments.Bump != null)
                        {   // Albedo, MetallicGloss, Normal
                            if (arguments.Emission)
                            {   // Albedo, MetallicGloss, Normal, Emission
                                if (arguments.Transparent)
                                {
                                    WriteTechnique(writer, "Techniques/PBR/PBRMetallicRoughDiffNormalSpecEmissiveAlpha.xml");
                                }
                                else
                                {
                                    WriteTechnique(writer, "Techniques/PBR/PBRMetallicRoughDiffNormalSpecEmissive.xml");
                                }
                                WriteTexture(arguments.Emission, writer, "emissive");
                            }
                            else
                            {   // Albedo, MetallicGloss, Normal, No Emission
                                if (arguments.Transparent)
                                {
                                    WriteTechnique(writer, "Techniques/PBR/PBRMetallicRoughDiffNormalSpecAlpha.xml");
                                }
                                else
                                {
                                    WriteTechnique(writer, "Techniques/PBR/PBRMetallicRoughDiffNormalSpec.xml");
                                }
                            }
                            WriteTexture(arguments.Bump, writer, "normal");
                        }
                        else
                        {   // Albedo, MetallicGloss, No Normal
                            if (arguments.Transparent)
                            {
                                WriteTechnique(writer, "Techniques/PBR/PBRMetallicRoughDiffSpecAlpha.xml");
                            }
                            else
                            {
                                WriteTechnique(writer, "Techniques/PBR/PBRMetallicRoughDiffSpec.xml");
                            }
                        }

                        if (_assets.TryGetTexturePath(arguments.MetallicGloss, out var baseAssetName))
                        {
                            var textureReferences = new TextureReferences(TextureSemantic.MetallicGlossiness, 1.0f, (arguments.SmoothnessTextureChannel == SmoothnessTextureChannel.AlbedoAlpha) ? arguments.Albedo : arguments.MetallicGloss, arguments.SmoothnessTextureChannel);
                            var textureOutputName = TextureExporter.GetTextureOutputName(baseAssetName, textureReferences);
                            WriteTexture(textureOutputName, writer, "specular");
                        }
                    }
                    else
                    {   // Albedo, No MetallicGloss
                        if (arguments.Bump != null)
                        {   // Albedo, No MetallicGloss, Normal
                            if (arguments.Emission != null)
                            {   // Albedo, No MetallicGloss, Normal, Emission
                                if (arguments.Transparent)
                                {
                                    WriteTechnique(writer, "Techniques/PBR/PBRDiffNormalEmissiveAlpha.xml");
                                }
                                else
                                {
                                    WriteTechnique(writer, "Techniques/PBR/PBRDiffNormalEmissive.xml");
                                }
                                WriteTexture(arguments.Emission, writer, "emissive");
                            }
                            else
                            {   // Albedo, No MetallicGloss, Normal, No Emission
                                if (arguments.Transparent)
                                {
                                    WriteTechnique(writer, "Techniques/PBR/PBRDiffNormalAlpha.xml");
                                }
                                else
                                {
                                    WriteTechnique(writer, "Techniques/PBR/PBRDiffNormal.xml");
                                }
                            }
                            WriteTexture(arguments.Bump, writer, "normal");
                        }
                        else
                        {   // Albedo, No MetallicGloss, No Normal
                            if (arguments.Transparent)
                            {
                                WriteTechnique(writer, "Techniques/PBR/PBRDiffAlpha.xml");
                            }
                            else
                            {
                                WriteTechnique(writer, "Techniques/PBR/PBRDiff.xml");
                            }
                        }
                    }
                    WriteTexture(arguments.Albedo, writer, "diffuse");
                }
                else
                {   // No albedo
                    if (arguments.Transparent)
                    {
                        WriteTechnique(writer, "Techniques/PBR/PBRNoTextureAlpha.xml");
                    }
                    else
                    {
                        WriteTechnique(writer, "Techniques/PBR/PBRNoTexture.xml");
                    }
                }
                WriteParameter(writer, "MatDiffColor", BaseNodeExporter.Format(arguments.AlbedoColor));
                WriteParameter(writer, "MatEmissiveColor", BaseNodeExporter.FormatRGB(arguments.EmissiveColor));
                WriteParameter(writer, "MatEnvMapColor", BaseNodeExporter.FormatRGB(Color.white));
                WriteParameter(writer, "MatSpecColor", BaseNodeExporter.Format(Vector4.zero));
                if (arguments.MetallicGloss != null)
                {
                    WriteParameter(writer, "Roughness", BaseNodeExporter.Format(0));
                    WriteParameter(writer, "Metallic", BaseNodeExporter.Format(0));
                }
                else
                {
                    WriteParameter(writer, "Roughness", BaseNodeExporter.Format(1.0f-arguments.Glossiness));
                    WriteParameter(writer, "Metallic", BaseNodeExporter.Format(arguments.Metallic));
                }
                writer.WriteEndElement();
                writer.WriteEndDocument();
            }
        }


        private void ExportSpecularGlossiness(AssetContext asset, SpecularGlossinessShaderArguments arguments)
        {
            using (XmlTextWriter writer = asset.DestinationFolder.CreateXml(asset.UrhoAssetName))
            {
                if (writer == null)
                    return;
                writer.WriteStartDocument(); writer.WriteWhitespace(Environment.NewLine);
                writer.WriteStartElement("material"); writer.WriteWhitespace(Environment.NewLine);

                if (arguments.Albedo != null)
                {   // Albedo
                    if (arguments.PBRSpecular != null)
                    {   // Albedo, MetallicGloss
                        if (arguments.Bump != null)
                        {   // Albedo, MetallicGloss, Normal
                            if (arguments.Emission)
                            {   // Albedo, MetallicGloss, Normal, Emission
                                if (arguments.Transparent)
                                {
                                    WriteTechnique(writer, "Techniques/PBR/PBRMetallicRoughDiffNormalSpecEmissiveAlpha.xml");
                                }
                                else
                                {
                                    WriteTechnique(writer, "Techniques/PBR/PBRMetallicRoughDiffNormalSpecEmissive.xml");
                                }
                                WriteTexture(arguments.Emission, writer, "emissive");
                            }
                            else
                            {   // Albedo, MetallicGloss, Normal, No Emission
                                if (arguments.Transparent)
                                {
                                    WriteTechnique(writer, "Techniques/PBR/PBRMetallicRoughDiffNormalSpecAlpha.xml");
                                }
                                else
                                {
                                    WriteTechnique(writer, "Techniques/PBR/PBRMetallicRoughDiffNormalSpec.xml");
                                }
                            }
                            WriteTexture(arguments.Bump, writer, "normal");
                        }
                        else
                        {   // Albedo, MetallicGloss, No Normal
                            if (arguments.Transparent)
                            {
                                WriteTechnique(writer, "Techniques/PBR/PBRMetallicRoughDiffSpecAlpha.xml");
                            }
                            else
                            {
                                WriteTechnique(writer, "Techniques/PBR/PBRMetallicRoughDiffSpec.xml");
                            }
                        }

                        if (_assets.TryGetTexturePath(arguments.PBRSpecular, out var baseAssetName))
                        {
                            var textureReferences = new TextureReferences(TextureSemantic.SpecularGlossiness, 1.0f, (arguments.SmoothnessTextureChannel == SmoothnessTextureChannel.AlbedoAlpha) ? arguments.Albedo : arguments.PBRSpecular, arguments.SmoothnessTextureChannel);
                            var textureOutputName = TextureExporter.GetTextureOutputName(baseAssetName, textureReferences);
                            WriteTexture(textureOutputName, writer, "specular");
                        }
                    }
                    else
                    {   // Albedo, No MetallicGloss
                        if (arguments.Bump != null)
                        {   // Albedo, No MetallicGloss, Normal
                            if (arguments.Emission != null)
                            {   // Albedo, No MetallicGloss, Normal, Emission
                                if (arguments.Transparent)
                                {
                                    WriteTechnique(writer, "Techniques/PBR/PBRDiffNormalEmissiveAlpha.xml");
                                }
                                else
                                {
                                    WriteTechnique(writer, "Techniques/PBR/PBRDiffNormalEmissive.xml");
                                }
                                WriteTexture(arguments.Emission, writer, "emissive");
                            }
                            else
                            {   // Albedo, No MetallicGloss, Normal, No Emission
                                if (arguments.Transparent)
                                {
                                    WriteTechnique(writer, "Techniques/PBR/PBRDiffNormalAlpha.xml");
                                }
                                else
                                {
                                    WriteTechnique(writer, "Techniques/PBR/PBRDiffNormal.xml");
                                }
                            }
                            WriteTexture(arguments.Bump, writer, "normal");
                        }
                        else
                        {   // Albedo, No MetallicGloss, No Normal
                            if (arguments.Transparent)
                            {
                                WriteTechnique(writer, "Techniques/PBR/PBRDiffAlpha.xml");
                            }
                            else
                            {
                                WriteTechnique(writer, "Techniques/PBR/PBRDiff.xml");
                            }
                        }
                    }
                    WriteTexture(arguments.Albedo, writer, "diffuse");
                }
                else
                {   // No albedo
                    if (arguments.Transparent)
                    {
                        WriteTechnique(writer, "Techniques/PBR/PBRNoTextureAlpha.xml");
                    }
                    else
                    {
                        WriteTechnique(writer, "Techniques/PBR/PBRNoTexture.xml");
                    }
                }
                WriteParameter(writer, "MatDiffColor", BaseNodeExporter.Format(arguments.DiffuseColor));
                WriteParameter(writer, "MatEmissiveColor", BaseNodeExporter.FormatRGB(arguments.EmissiveColor));
                WriteParameter(writer, "MatEnvMapColor", BaseNodeExporter.FormatRGB(Color.white));
                WriteParameter(writer, "MatSpecColor", BaseNodeExporter.Format(Vector4.zero));
                if (arguments.PBRSpecular != null)
                {
                    WriteParameter(writer, "Roughness", BaseNodeExporter.Format(0));
                    WriteParameter(writer, "Metallic", BaseNodeExporter.Format(0));
                }
                else
                {
                    ////TODO: Evaluate correct parameters:
                    WriteParameter(writer, "Roughness", BaseNodeExporter.Format(0.0f)); 
                    WriteParameter(writer, "Metallic", BaseNodeExporter.Format(0.0f));
                }
                writer.WriteEndElement();
                writer.WriteEndDocument();
            }
        }


        private void WriteParameter(XmlWriter writer, string name, string vaue)
        {
            writer.WriteWhitespace("\t");
            writer.WriteStartElement("parameter");
            writer.WriteAttributeString("name", name);
            writer.WriteAttributeString("value", vaue);
            writer.WriteEndElement();
            writer.WriteWhitespace("\n");
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

        public void ExportAsset(AssetContext asset)
        {
            var material = AssetDatabase.LoadAssetAtPath<Material>(asset.AssetPath);
            _assets.AddMaterialPath(material, asset.UrhoAssetName);

            ExportMaterial(asset, material);
        }

    }
}