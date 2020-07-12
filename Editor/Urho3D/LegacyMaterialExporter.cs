using System;
using UnityEditor;
using UnityEngine;

namespace UnityToCustomEngineExporter.Editor.Urho3D
{
    [CustomUrho3DExporter(typeof(Material))]
    public class LegacyMaterialExporter : AbstractMaterialExporter, IUrho3DMaterialExporter
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

        public LegacyMaterialExporter(Urho3DEngine engine) : base(engine)
        {
        }

        public override int ExporterPriority => int.MinValue;

        public override bool CanExportMaterial(Material material)
        {
            return true;
        }

        public override void ExportMaterial(Material material, PrefabContext prefabContext)
        {
            var urhoPath = EvaluateMaterialName(material);
            using (var writer =
                Engine.TryCreateXml(material.GetKey(), urhoPath, ExportUtils.GetLastWriteTimeUtc(material)))
            {
                if (writer == null)
                    return;
                var arguments = SetupLegacy(material);
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
                if (arguments.Diffuse != null) WriteTexture(arguments.Diffuse, writer, "diffuse", prefabContext);
                if (arguments.Specular != null) WriteTexture(arguments.Specular, writer, "specular", prefabContext);
                if (arguments.Bump != null) WriteTexture(arguments.Bump, writer, "normal", prefabContext);
                if (arguments.Emission != null) WriteTexture(arguments.Bump, writer, "emissive", prefabContext);
                writer.WriteParameter("MatDiffColor", arguments.DiffColor);
                if (arguments.HasEmission)
                    writer.WriteParameter("MatEmissiveColor", BaseNodeExporter.FormatRGB(arguments.EmissiveColor));
                WriteCommonParameters(writer, arguments);

                writer.WriteEndElement();
            }
        }

        private LegacyShaderArguments SetupLegacy(Material material)
        {
            var arguments = new LegacyShaderArguments();
            SetupFlags(material, arguments);
            var shader = material.shader;
            for (var i = 0; i < ShaderUtil.GetPropertyCount(shader); i++)
            {
                var propertyName = ShaderUtil.GetPropertyName(shader, i);
                var propertyType = ShaderUtil.GetPropertyType(shader, i);
                switch (propertyType)
                {
                    case ShaderUtil.ShaderPropertyType.Color:
                    {
                        var color = material.GetColor(propertyName);
                        switch (propertyName)
                        {
                            case "_MainColor":
                            case "_Color":
                                arguments.DiffColor = color;
                                break;
                            case "_EmissionColor":
                                arguments.EmissiveColor = color;
                                break;
                            case "_SpecColor":
                                arguments.SpecColor = color;
                                break;
                        }

                        break;
                    }
                    case ShaderUtil.ShaderPropertyType.Float:
                    {
                        var value = material.GetFloat(propertyName);
                        switch (propertyName)
                        {
                            case "BumpScale":
                                arguments.BumpScale = value;
                                break;
                            case "_DetailNormalMapScale": break;
                            case "_DstBlend": break;
                            case "_GlossyReflections": break;
                            case "_Mode": break;
                            case "_SmoothnessTextureChannel": break;
                            case "_SpecularHighlights": break;
                            case "_SrcBlend": break;
                            case "_UVSec": break;
                            case "_ZWrite": break;
                            case "_Alpha_1":
                                arguments.DiffColor = new Color(arguments.DiffColor.r, arguments.DiffColor.g,
                                    arguments.DiffColor.b, value);
                                break;
                        }

                        break;
                    }
                    case ShaderUtil.ShaderPropertyType.Range:
                    {
                        var value = material.GetFloat(propertyName);
                        switch (propertyName)
                        {
                            case "_Cutoff":
                                arguments.Cutoff = value;
                                break;
                            case "_GlossMapScale": break;
                            case "_Glossiness": break;
                            case "_OcclusionStrength":
                                arguments.OcclusionStrength = value;
                                break;
                            case "_Parallax": break;
                        }

                        break;
                    }
                    case ShaderUtil.ShaderPropertyType.TexEnv:
                    {
                        var texture = material.GetTexture(propertyName);
                        switch (propertyName)
                        {
                            case "_Normal":
                            case "_NormalMapRefraction":
                            case "_BumpMap":
                                arguments.Bump = texture;
                                break;
                            case "_DetailMask":
                                arguments.Detail = texture;
                                break;
                            case "_DetailNormalMap":
                                arguments.DetailNormal = texture;
                                break;
                            case "_Emission":
                            case "_EmissionMap":
                                arguments.Emission = texture;
                                break;
                            case "_Diffuse":
                            case "_Texture":
                            case "_MainTexture":
                            case "_MainTex":
                                arguments.Diffuse = texture;
                                break;
                            case "_OcclusionMap":
                                arguments.Occlusion = texture;
                                break;
                            case "_ParallaxMap":
                                arguments.Parallax = texture;
                                break;
                            case "_SpecGlossMap":
                            case "_SpecularRGBGlossA":
                                arguments.Specular = texture;
                                break;
                        }

                        break;
                    }
                }
            }

            return arguments;
        }
    }
}