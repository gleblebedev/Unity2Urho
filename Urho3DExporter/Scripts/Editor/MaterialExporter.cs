using System;
using System.IO;
using System.Text;
using System.Xml;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace Urho3DExporter
{
    public class MaterialExporter : XmlExporter, IExporter
    {
        private readonly AssetCollection _assets;

        public MaterialExporter(AssetCollection assets) : base()
        {
            _assets = assets;
        }
        public static Technique[] Techniques =
        {
            new Technique {Material = new MaterialFlags(), Name = "NoTexture.xml"},
            new Technique {Material = new MaterialFlags {hasAlpha = true}, Name = "NoTextureAlpha.xml"},
            new Technique {Material = new MaterialFlags {hasNormal = true}, Name = "NoTextureNormal.xml"},
            new Technique
            {
                Material = new MaterialFlags {hasNormal = true, hasAlpha = true},
                Name = "NoTextureNormalAlpha.xml"
            },
            //new Technique
            //{
            //    Material = new MaterialFlags {hasNormal = true, hasAlpha = true, hasEmissive = true},
            //    Name = "NoTextureNormalEmissiveAlpha.xml"
            //},
            new Technique {Material = new MaterialFlags {hasDiffuse = true}, Name = "Diff.xml"},
            new Technique {Material = new MaterialFlags {hasDiffuse = true, hasAlpha = true}, Name = "DiffAlpha.xml"},
            new Technique {Material = new MaterialFlags {hasDiffuse = true, hasSpecular = true}, Name = "DiffSpec.xml"},
            new Technique
            {
                Material = new MaterialFlags {hasDiffuse = true, hasSpecular = true, hasAlpha = true},
                Name = "DiffSpecAlpha.xml"
            },
            new Technique {Material = new MaterialFlags {hasDiffuse = true, hasNormal = true}, Name = "DiffNormal.xml"},
            new Technique
            {
                Material = new MaterialFlags {hasDiffuse = true, hasNormal = true, hasAlpha = true},
                Name = "DiffNormalAlpha.xml"
            },
            new Technique
            {
                Material = new MaterialFlags {hasDiffuse = true, hasEmissive = true},
                Name = "DiffEmissive.xml"
            },
            new Technique
            {
                Material = new MaterialFlags {hasDiffuse = true, hasEmissive = true, hasAlpha = true},
                Name = "DiffEmissiveAlpha.xml"
            },
            new Technique
            {
                Material = new MaterialFlags {hasDiffuse = true, hasSpecular = true, hasNormal = true},
                Name = "DiffNormalSpec.xml"
            },
            new Technique
            {
                Material = new MaterialFlags {hasDiffuse = true, hasSpecular = true, hasNormal = true, hasAlpha = true},
                Name = "DiffNormalSpecAlpha.xml"
            },
            new Technique
            {
                Material = new MaterialFlags {hasDiffuse = true, hasEmissive = true, hasNormal = true},
                Name = "DiffNormalEmissive.xml"
            },
            new Technique
            {
                Material = new MaterialFlags {hasDiffuse = true, hasEmissive = true, hasNormal = true, hasAlpha = true},
                Name = "DiffNormalEmissiveAlpha.xml"
            },
            new Technique
            {
                Material = new MaterialFlags
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
                Material = new MaterialFlags
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
        private void WriteTechnique(XmlWriter writer, string prefix, string name)
        {
            writer.WriteWhitespace(prefix);
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
                writer.WriteStartElement("texture");
                writer.WriteAttributeString("unit", name);
                writer.WriteAttributeString("name", urhoAssetName);
                writer.WriteEndElement();
                writer.WriteWhitespace(Environment.NewLine);
            }
            return true;
        }
        private void ExportStandartMaterial(Material material, XmlWriter writer)
        {
            {
                var matEmissionEnabled = material.IsKeywordEnabled("_EMISSION");
                var matDiffColor = Color.white;
                var matSpecColor = Color.black;
                var matEmissiveColor = Color.white;
                var flags = new MaterialFlags();
                flags.hasAlpha = material.renderQueue == (int)RenderQueue.Transparent;
                flags.hasEmissive = matEmissionEnabled;
                var shader = material.shader;
                for (var i = 0; i < ShaderUtil.GetPropertyCount(shader); i++)
                {
                    var propertyName = ShaderUtil.GetPropertyName(shader, i);
                    var propertyType = ShaderUtil.GetPropertyType(shader, i);
                    if (propertyType == ShaderUtil.ShaderPropertyType.TexEnv)
                    {
                        var texture = material.GetTexture(propertyName);
                        if (texture != null)
                            switch (propertyName)
                            {
                                case "_Diffuse":
                                case "_Texture":
                                case "_MainTex":
                                    flags.hasDiffuse = WriteTexture(texture, writer, "diffuse");
                                    break;
                                case "_SpecGlossMap":
                                    flags.hasSpecular = WriteTexture(texture, writer, "specular");
                                    break;
                                case "_ParallaxMap":
                                    break;
                                case "_Normal":
                                case "_BumpMap":
                                    flags.hasNormal = WriteTexture(texture, writer, "normal");
                                    break;
                                case "_DetailAlbedoMap":
                                    break;
                                case "_DetailNormalMap":
                                    break;
                                case "_EmissionMap":
                                case "_Emission":
                                    flags.hasEmissive &= WriteTexture(texture, writer, "emissive");
                                    break;
                                case "_MetallicGlossMap":
                                    break;
                                case "_OcclusionMap":
                                    break;
                                case "_Overlay":
                                    break;
                                case "_Mask":
                                    break;
                                case "_DetailMask":
                                    break;
                                default:
                                    Debug.LogWarning(propertyName);
                                    break;
                            }
                    }
                    else if (propertyType == ShaderUtil.ShaderPropertyType.Color)
                    {
                        var color = material.GetColor(propertyName);
                        switch (propertyName)
                        {
                            case "_FresnelColor":
                                break;
                            case "_MainColor":
                            case "_Color":
                                matDiffColor = color;
                                break;
                            case "_EmissionColor":
                                matEmissiveColor = color;
                                break;
                            case "_SpecColor":
                                matSpecColor = color;
                                break;
                            default:
                                Debug.LogWarning(propertyName);
                                break;
                        }
                    }
                    else if (propertyType == ShaderUtil.ShaderPropertyType.Range)
                    {
                        var value = material.GetFloat(propertyName);
                        switch (propertyName)
                        {
                            case "_Cutoff":
                            case "_Glossiness":
                            case "_GlossMapScale":
                            case "_Parallax":
                            case "_OcclusionStrength":
                            case "_Specular":
                            case "_Gloss":
                            case "_FresnelPower":
                            case "_FresnelExp":
                            case "_Alpha_2":
                            case "_RefractionPower":
                            case "_Metallic":
                                break;
                            case "_Alpha_1":
                                matDiffColor.a = value;
                                break;
                            default:
                                Debug.LogWarning(propertyName);
                                break;
                        }
                    }
                    else if (propertyType == ShaderUtil.ShaderPropertyType.Float)
                    {
                        var value = material.GetFloat(propertyName);
                        switch (propertyName)
                        {
                            case "_SmoothnessTextureChannel":
                            case "_SpecularHighlights":
                            case "_GlossyReflections":
                            case "_BumpScale":
                            case "_DetailNormalMapScale":
                            case "_UVSec":
                            case "_Mode":
                            case "_SrcBlend":
                            case "_DstBlend":
                            case "_ZWrite":
                                break;
                            default:
                                Debug.LogWarning(propertyName);
                                break;
                        }
                    }
                    //else
                    //{
                    //    Debug.LogWarning(propertyName+" of unsupported type "+propertyType);
                    //}
                }
                if (!flags.hasDiffuse)
                    WriteParameter(writer, "\t", "MatDiffColor", BaseNodeExporter.Format(matDiffColor));
                if (!flags.hasSpecular)
                    WriteParameter(writer, "\t", "MatSpecColor", BaseNodeExporter.Format(matSpecColor));
                if (matEmissionEnabled)
                    WriteParameter(writer, "\t", "MatEmissiveColor", BaseNodeExporter.Format(matEmissiveColor));

                writer.WriteWhitespace(Environment.NewLine);
                writer.WriteStartElement("technique");
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
                writer.WriteAttributeString("name", "Techniques/" + bestTechnique.Name);
                writer.WriteAttributeString("quality", "0");
                writer.WriteEndElement();
                writer.WriteWhitespace(Environment.NewLine);
            }
        }
        private void WriteParameter(XmlWriter writer, string prefix, string name, string vaue)
        {
            writer.WriteWhitespace(prefix);
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

            if (File.Exists(asset.UrhoFileName))
                return;

            using (XmlTextWriter writer = CreateXmlFile(asset))
            {
                writer.WriteStartDocument();
                writer.WriteStartElement("material");
                writer.WriteWhitespace(Environment.NewLine);
                if (material != null)
                {
                    if (material.shader.name == "Standard")
                    {
                        ExportStandartMaterial(material, writer);
                    }
                    else
                    {
                        Debug.Log("Unknown shader " + material.shader.name);
                        ExportStandartMaterial(material, writer);
                    }
                }

                writer.WriteEndElement();
                writer.WriteEndDocument();
            }
        }

    }
}