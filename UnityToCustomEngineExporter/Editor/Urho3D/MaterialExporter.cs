using System;
using System.Xml;
using UnityEditor;
using UnityEngine;

namespace Assets.Scripts.UnityToCustomEngineExporter.Editor.Urho3D
{
    public class MaterialExporter
    {
        private readonly Urho3DEngine _engine;

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

        public MaterialExporter(Urho3DEngine engine)
        {
            _engine = engine;
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
                return ExportUtils.ReplaceExtension(ExportUtils.GetRelPathFromAssetPath(assetPath), ".xml");
            string newExt = "/"+ ExportUtils.SafeFileName(material.name)+".xml";
            return ExportUtils.ReplaceExtension(ExportUtils.GetRelPathFromAssetPath(assetPath), newExt);
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

        public void ExportMaterial(Material material)
        {
            var path = EvaluateMaterialName(material);
            var mat = new MaterialDescription(material);
            if (mat.SpecularGlossiness != null)
                ExportSpecularGlossiness(path, mat.SpecularGlossiness);
            else if (mat.MetallicRoughness != null)
                ExportMetallicRoughness(path, mat.MetallicRoughness);
            else if (mat.Skybox != null)
                ExportSkybox(path, mat.Skybox);
            else
                ExportLegacy(path, mat.Legacy ?? new LegacyShaderArguments());
        }

        private void ExportSkybox(string urhoPath, SkyboxShaderArguments arguments)
        {
            using (var writer = _engine.TryCreateXml(urhoPath, DateTime.MaxValue))
            {
                if (writer == null)
                    return;
                writer.WriteStartDocument();
                writer.WriteWhitespace(Environment.NewLine);
                writer.WriteStartElement("material");
                writer.WriteWhitespace(Environment.NewLine);
                WriteTechnique(writer, "Techniques/DiffSkyboxHDRScale.xml");
                //WriteTechnique(writer, "Techniques/DiffSkybox.xml");
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
                    }
                    if (!string.IsNullOrWhiteSpace(name))
                    {
                        WriteTexture(name, writer, "diffuse");
                    }
                }
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
                }

                writer.WriteEndElement();
                writer.WriteEndDocument();
            }
        }

        private void ExportLegacy(string urhoPath, LegacyShaderArguments arguments)
        {
            using (var writer = _engine.TryCreateXml(urhoPath, DateTime.MaxValue))
            {
                if (writer == null)
                    return;
                writer.WriteStartDocument();
                writer.WriteWhitespace(Environment.NewLine);

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
                WriteParameter(writer, "MatDiffColor", BaseNodeExporter.Format(arguments.DiffColor));
                if (arguments.HasEmission)
                    WriteParameter(writer, "MatEmissiveColor", BaseNodeExporter.FormatRGB(arguments.EmissiveColor));
                WriteCommonParameters(writer, arguments);

                writer.WriteEndElement();
                writer.WriteEndDocument();
            }
        }

        private void ExportMetallicRoughness(string urhoPath, MetallicRoughnessShaderArguments arguments)
        {
            using (var writer = _engine.TryCreateXml(urhoPath, DateTime.MaxValue))
            {
                if (writer == null)
                    return;
                writer.WriteStartDocument();
                writer.WriteWhitespace(Environment.NewLine);
                writer.WriteStartElement("material");
                writer.WriteWhitespace(Environment.NewLine);

                if (arguments.BaseColor != null)
                {
                    // Albedo
                    if (arguments.MetallicGloss != null)
                    {
                        // Albedo, MetallicGloss
                        if (arguments.Bump != null)
                        {
                            // Albedo, MetallicGloss, Normal
                            if (arguments.Emission)
                            {
                                // Albedo, MetallicGloss, Normal, Emission
                                if (arguments.Transparent)
                                    WriteTechnique(writer, "Techniques/PBR/PBRMetallicRoughDiffNormalSpecEmissiveAlpha.xml");
                                else
                                    WriteTechnique(writer, "Techniques/PBR/PBRMetallicRoughDiffNormalSpecEmissive.xml");
                                WriteTexture(arguments.Emission, writer, "emissive");
                            }
                            else
                            {
                                // Albedo, MetallicGloss, Normal, No Emission
                                if (arguments.Transparent)
                                    WriteTechnique(writer, "Techniques/PBR/PBRMetallicRoughDiffNormalSpecAlpha.xml");
                                else
                                    WriteTechnique(writer, "Techniques/PBR/PBRMetallicRoughDiffNormalSpec.xml");
                            }

                            WriteTexture(arguments.Bump, writer, "normal");
                        }
                        else
                        {
                            // Albedo, MetallicGloss, No Normal
                            if (arguments.Transparent)
                                WriteTechnique(writer, "Techniques/PBR/PBRMetallicRoughDiffSpecAlpha.xml");
                            else
                                WriteTechnique(writer, "Techniques/PBR/PBRMetallicRoughDiffSpec.xml");
                        }

                        {
                            var textureReferences = new PBRMetallicGlossinessTextureReference(arguments.GlossinessTextureScale, arguments.Smoothness);
                            var textureOutputName = _engine.EvaluateTextrueName(arguments.MetallicGloss, textureReferences);
                            _engine.ScheduleTexture(arguments.MetallicGloss, textureReferences);
                            WriteTexture(textureOutputName, writer, "specular");
                        }
                    }
                    else
                    {
                        // Albedo, No MetallicGloss
                        if (arguments.Bump != null)
                        {
                            // Albedo, No MetallicGloss, Normal
                            if (arguments.Emission != null)
                            {
                                // Albedo, No MetallicGloss, Normal, Emission
                                if (arguments.Transparent)
                                    WriteTechnique(writer, "Techniques/PBR/PBRDiffNormalEmissiveAlpha.xml");
                                else
                                    WriteTechnique(writer, "Techniques/PBR/PBRDiffNormalEmissive.xml");
                                WriteTexture(arguments.Emission, writer, "emissive");
                            }
                            else
                            {
                                // Albedo, No MetallicGloss, Normal, No Emission
                                if (arguments.Transparent)
                                    WriteTechnique(writer, "Techniques/PBR/PBRDiffNormalAlpha.xml");
                                else
                                    WriteTechnique(writer, "Techniques/PBR/PBRDiffNormal.xml");
                            }

                            WriteTexture(arguments.Bump, writer, "normal");
                        }
                        else
                        {
                            // Albedo, No MetallicGloss, No Normal
                            if (arguments.Transparent)
                                WriteTechnique(writer, "Techniques/PBR/PBRDiffAlpha.xml");
                            else
                                WriteTechnique(writer, "Techniques/PBR/PBRDiff.xml");
                        }
                    }

                    WriteTexture(arguments.BaseColor, writer, "diffuse");
                }
                else
                {
                    // No albedo
                    if (arguments.Transparent)
                        WriteTechnique(writer, "Techniques/PBR/PBRNoTextureAlpha.xml");
                    else
                        WriteTechnique(writer, "Techniques/PBR/PBRNoTexture.xml");
                }

                WriteParameter(writer, "MatDiffColor", BaseNodeExporter.Format(arguments.BaseColorColor));
                if (arguments.HasEmission)
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
                    WriteParameter(writer, "Roughness", BaseNodeExporter.Format(1.0f - arguments.Glossiness));
                    WriteParameter(writer, "Metallic", BaseNodeExporter.Format(arguments.Metallic));
                }
                WriteCommonParameters(writer, arguments);

                writer.WriteEndElement();
                writer.WriteEndDocument();
            }
        }

        private static void WriteAlphaTest(XmlWriter writer)
        {
            writer.WriteWhitespace("\t");
            writer.WriteStartElement("shader");
            writer.WriteAttributeString("psdefines", "ALPHAMASK");
            writer.WriteEndElement();
            writer.WriteWhitespace("\n");
        }


        private void ExportSpecularGlossiness(string urhoPath, SpecularGlossinessShaderArguments arguments)
        {
            using (var writer = _engine.TryCreateXml(urhoPath, DateTime.MaxValue))
            {
                if (writer == null)
                    return;
                writer.WriteStartDocument();
                writer.WriteWhitespace(Environment.NewLine);
                writer.WriteStartElement("material");
                writer.WriteWhitespace(Environment.NewLine);

                if (arguments.Diffuse != null)
                {
                    // Albedo
                    if (arguments.PBRSpecular != null)
                    {
                        // Albedo, MetallicGloss
                        if (arguments.Bump != null)
                        {
                            // Albedo, MetallicGloss, Normal
                            if (arguments.Emission)
                            {
                                // Albedo, MetallicGloss, Normal, Emission
                                if (arguments.Transparent)
                                    WriteTechnique(writer,
                                        "Techniques/PBR/PBRMetallicRoughDiffNormalSpecEmissiveAlpha.xml");
                                else
                                    WriteTechnique(writer, "Techniques/PBR/PBRMetallicRoughDiffNormalSpecEmissive.xml");
                                WriteTexture(arguments.Emission, writer, "emissive");
                            }
                            else
                            {
                                // Albedo, MetallicGloss, Normal, No Emission
                                if (arguments.Transparent)
                                    WriteTechnique(writer, "Techniques/PBR/PBRMetallicRoughDiffNormalSpecAlpha.xml");
                                else
                                    WriteTechnique(writer, "Techniques/PBR/PBRMetallicRoughDiffNormalSpec.xml");
                            }

                            WriteTexture(arguments.Bump, writer, "normal");
                        }
                        else
                        {
                            // Albedo, MetallicGloss, No Normal
                            if (arguments.Transparent)
                                WriteTechnique(writer, "Techniques/PBR/PBRMetallicRoughDiffSpecAlpha.xml");
                            else
                                WriteTechnique(writer, "Techniques/PBR/PBRMetallicRoughDiffSpec.xml");
                        }

                        {
                            var textureReferences = new PBRSpecularGlossinessTextureReference(arguments.GlossinessTextureScale, arguments.Smoothness, arguments.Diffuse);
                            var textureOutputName = _engine.EvaluateTextrueName(arguments.PBRSpecular, textureReferences);
                            _engine.ScheduleTexture(arguments.PBRSpecular, textureReferences);
                            WriteTexture(textureOutputName, writer, "specular");
                        }
                    }
                    else
                    {
                        // Albedo, No MetallicGloss
                        if (arguments.Bump != null)
                        {
                            // Albedo, No MetallicGloss, Normal
                            if (arguments.Emission != null)
                            {
                                // Albedo, No MetallicGloss, Normal, Emission
                                if (arguments.Transparent)
                                    WriteTechnique(writer, "Techniques/PBR/PBRDiffNormalEmissiveAlpha.xml");
                                else
                                    WriteTechnique(writer, "Techniques/PBR/PBRDiffNormalEmissive.xml");
                                WriteTexture(arguments.Emission, writer, "emissive");
                            }
                            else
                            {
                                // Albedo, No MetallicGloss, Normal, No Emission
                                if (arguments.Transparent)
                                    WriteTechnique(writer, "Techniques/PBR/PBRDiffNormalAlpha.xml");
                                else
                                    WriteTechnique(writer, "Techniques/PBR/PBRDiffNormal.xml");
                            }

                            WriteTexture(arguments.Bump, writer, "normal");
                        }
                        else
                        {
                            // Albedo, No MetallicGloss, No Normal
                            if (arguments.Transparent)
                                WriteTechnique(writer, "Techniques/PBR/PBRDiffAlpha.xml");
                            else
                                WriteTechnique(writer, "Techniques/PBR/PBRDiff.xml");
                        }
                    }

                    {
                        var textureReferences = new PBRDiffuseTextureReference(arguments.PBRSpecular, arguments.Smoothness, arguments.GlossinessTextureScale);
                        var textureOutputName = _engine.EvaluateTextrueName(arguments.Diffuse, textureReferences);
                        _engine.ScheduleTexture(arguments.Diffuse, textureReferences);
                        WriteTexture(textureOutputName, writer, "diffuse");
                    }
                }
                else
                {
                    // No albedo
                    if (arguments.Transparent)
                        WriteTechnique(writer, "Techniques/PBR/PBRNoTextureAlpha.xml");
                    else
                        WriteTechnique(writer, "Techniques/PBR/PBRNoTexture.xml");
                }

                WriteParameter(writer, "MatDiffColor", BaseNodeExporter.Format(arguments.DiffuseColor));
                if (arguments.HasEmission)
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
                    WriteParameter(writer, "Roughness", BaseNodeExporter.Format(1.0f-arguments.Glossiness));
                    WriteParameter(writer, "Metallic", BaseNodeExporter.Format(0.0f));
                }

                WriteCommonParameters(writer, arguments);
                writer.WriteEndElement();
                writer.WriteEndDocument();
            }
        }

        private void WriteCommonParameters(XmlWriter writer, ShaderArguments arguments)
        {
            WriteParameter(writer, "UOffset", BaseNodeExporter.Format(new Vector4(arguments.MainTextureScale.x,0,0, arguments.MainTextureOffset.x)));
            WriteParameter(writer, "VOffset", BaseNodeExporter.Format(new Vector4(0, arguments.MainTextureScale.y, 0, arguments.MainTextureOffset.y)));
            if (arguments.AlphaTest)
            {
                WriteAlphaTest(writer);
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
    }
}