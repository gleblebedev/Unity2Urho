using System.Globalization;
using System.IO;
using System.Text;
using Assets.Scripts.UnityToCustomEngineExporter.Editor.Urho3D;
using UnityEditor;
using UnityEngine;

namespace UnityToCustomEngineExporter.Editor.Urho3D
{
    [CustomUrho3DExporter(typeof(Material))]
    public class StandardSpecularMaterialExporter : AbstractMaterialExporter, IUrho3DMaterialExporter
    {
        public StandardSpecularMaterialExporter(Urho3DEngine engine) : base(engine)
        {
        }

        public override int ExporterPriority => 0;


        public UrhoPBRMaterial FromSpecularGlossiness(SpecularGlossinessShaderArguments arguments)
        {
            var material = new UrhoPBRMaterial();
            material.NormalTexture = GetScaledNormalTextureName(arguments.Bump, arguments.BumpScale, material);
            material.EmissiveTexture = Engine.EvaluateTextrueName(arguments.Emission);
            material.AOTexture = BuildAOTextureName(arguments.Occlusion, arguments.OcclusionStrength);
            var diffuseTextrueName = Engine.EvaluateTextrueName(arguments.Diffuse);
            var specularTexture = Engine.EvaluateTextrueName(arguments.PBRSpecular.Texture);
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
            if (arguments.AlphaTest) material.PixelShaderDefines.Add("ALPHAMASK");
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

        public override bool CanExportMaterial(Material material)
        {
            return material.shader.name == "Standard (Specular setup)";
        }

        public override void ExportMaterial(Material material, PrefabContext prefabContext)
        {
            var urhoPath = EvaluateMaterialName(material);
            using (var writer =
                Engine.TryCreateXml(material.GetKey(), urhoPath, ExportUtils.GetLastWriteTimeUtc(material)))
            {
                if (writer == null)
                    return;

                var arguments = SetupSpecularGlossinessPBR(material);
                var urhoMaterial = FromSpecularGlossiness(arguments);
                var shaderName = arguments.Shader;
                WriteMaterial(writer, shaderName, urhoMaterial, prefabContext);

                //Engine.ScheduleTexture(arguments.Bump, new TextureReference(TextureSemantic.Bump));

                Engine.SchedulePBRTextures(arguments, urhoMaterial);

                Engine.ScheduleTexture(arguments.Emission, new TextureReference(TextureSemantic.Emission));
                //Engine.ScheduleTexture(arguments.Occlusion, new TextureReference(TextureSemantic.Occlusion));
            }
        }

        private SpecularGlossinessShaderArguments SetupSpecularGlossinessPBR(Material material)
        {
            var arguments = new SpecularGlossinessShaderArguments();
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
                            case "_Color":
                                arguments.DiffuseColor = color;
                                break;
                            case "_EmissionColor":
                                arguments.EmissiveColor = color;
                                break;
                            case "_SpecColor":
                                arguments.PBRSpecular = new TextureOrColor(arguments.PBRSpecular.Texture, color);
                                break;
                        }

                        break;
                    }
                    case ShaderUtil.ShaderPropertyType.Float:
                    {
                        var value = material.GetFloat(propertyName);
                        switch (propertyName)
                        {
                            case "_BumpScale":
                                arguments.BumpScale = value;
                                break;
                            case "_DetailNormalMapScale": break;
                            case "_DstBlend": break;
                            case "_GlossyReflections": break;
                            case "_Mode": break;
                            case "_SmoothnessTextureChannel":
                                arguments.SmoothnessTextureChannel = (SmoothnessTextureChannel) value;
                                break;
                            case "_SpecularHighlights": break;
                            case "_SrcBlend": break;
                            case "_UVSec": break;
                            case "_ZWrite": break;
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
                            case "_GlossMapScale":
                                arguments.GlossinessTextureScale = value;
                                break;
                            case "_Glossiness":
                                arguments.Glossiness = value;
                                break;
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
                            case "_BumpMap":
                                arguments.Bump = texture;
                                break;
                            case "_DetailAlbedoMap":
                                arguments.DetailDiffuse = texture;
                                break;
                            case "_DetailMask":
                                arguments.Detail = texture;
                                break;
                            case "_DetailNormalMap":
                                arguments.DetailNormal = texture;
                                break;
                            case "_EmissionMap":
                                arguments.Emission = texture;
                                break;
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
                                arguments.PBRSpecular = new TextureOrColor(texture, arguments.PBRSpecular.Color);
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