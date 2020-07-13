using System.Globalization;
using System.IO;
using System.Text;
using Assets.Scripts.UnityToCustomEngineExporter.Editor.Urho3D;
using UnityEditor;
using UnityEngine;

namespace UnityToCustomEngineExporter.Editor.Urho3D
{
    [CustomUrho3DExporter(typeof(Material))]
    public class StandardMaterialExporter : AbstractMaterialExporter, IUrho3DMaterialExporter
    {
        public StandardMaterialExporter(Urho3DEngine engine) : base(engine)
        {
        }

        public override int ExporterPriority => 0;

        public override bool CanExportMaterial(Material material)
        {
            return material.shader.name == "Standard"
                   || material.shader.name == "ProBuilder/Standard Vertex Color"
                   || material.shader.name == "UnityChan/Skin"
                   || material.shader.name.StartsWith("Urho3D/");
        }

        public override void ExportMaterial(Material material, PrefabContext prefabContext)
        {
            var urhoPath = EvaluateMaterialName(material);
            using (var writer =
                Engine.TryCreateXml(material.GetKey(), urhoPath, ExportUtils.GetLastWriteTimeUtc(material)))
            {
                if (writer == null)
                    return;

                var arguments = CreateShaderArguments(material);
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
                            ParseColor(propertyName, material.GetColor(propertyName), arguments);
                            break;
                        }
                        case ShaderUtil.ShaderPropertyType.Float:
                        {
                            ParseFloatOrRange(propertyName, material.GetFloat(propertyName), arguments);
                            break;
                        }
                        case ShaderUtil.ShaderPropertyType.Range:
                        {
                            ParseFloatOrRange(propertyName, material.GetFloat(propertyName), arguments);
                            break;
                        }
                        case ShaderUtil.ShaderPropertyType.TexEnv:
                        {
                            ParseTexture(propertyName, material.GetTexture(propertyName), arguments);
                            break;
                        }
                    }
                }

                var urhoMaterial = FromMetallicGlossiness(material, arguments);
                var shaderName = arguments.Shader;
                WriteMaterial(writer, shaderName, urhoMaterial, prefabContext);

                Engine.ScheduleTexture(arguments.BaseColor, new TextureReference(TextureSemantic.PBRBaseColor));
                //Engine.ScheduleTexture(arguments.Bump, new TextureReference(TextureSemantic.Bump));
                Engine.ScheduleTexture(arguments.DetailBaseColor, new TextureReference(TextureSemantic.Detail));

                Engine.SchedulePBRTextures(arguments, urhoMaterial);

                Engine.ScheduleTexture(arguments.Emission, new TextureReference(TextureSemantic.Emission));
                //Engine.ScheduleTexture(arguments.Occlusion, new TextureReference(TextureSemantic.Occlusion));
            }
        }

        protected virtual UrhoPBRMaterial FromMetallicGlossiness(Material mat, MetallicGlossinessShaderArguments arguments)
        {
            var material = new UrhoPBRMaterial();

            material.NormalTexture = GetScaledNormalTextureName(arguments.Bump, arguments.BumpScale, material);
            material.EmissiveTexture = Engine.EvaluateTextrueName(arguments.Emission);
            material.AOTexture = BuildAOTextureName(arguments.Occlusion, arguments.OcclusionStrength);
            material.BaseColorTexture = Engine.EvaluateTextrueName(arguments.BaseColor);
            var metalicGlossinesTexture = Engine.EvaluateTextrueName(arguments.MetallicGloss);
            var smoothnessTexture = Engine.EvaluateTextrueName(arguments.Smoothness);
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
            if (arguments.AlphaTest) material.PixelShaderDefines.Add("ALPHAMASK");
            material.EmissiveColor = arguments.EmissiveColor.linear;
            material.MatSpecColor = new Color(1, 1, 1, 0);
            material.UOffset = new Vector4(arguments.MainTextureScale.x, 0, 0, arguments.MainTextureOffset.x);
            material.VOffset = new Vector4(0, arguments.MainTextureScale.y, 0, arguments.MainTextureOffset.y);
            material.EvaluateTechnique();
            return material;
        }

        protected virtual void ParseTexture(string propertyName, Texture texture,
            MetallicGlossinessShaderArguments arguments)
        {
            if (texture != null)
                switch (propertyName)
                {
                    case "_BumpMap":
                        arguments.Bump = texture;
                        break;
                    case "_DetailAlbedoMap":
                        arguments.DetailBaseColor = texture;
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
                        arguments.BaseColor = texture;
                        break;
                    case "_MetallicGlossMap":
                        arguments.MetallicGloss = texture;
                        break;
                    case "_OcclusionMap":
                        arguments.Occlusion = texture;
                        break;
                    case "_ParallaxMap":
                        arguments.Parallax = texture;
                        break;
                }
        }

        protected virtual void ParseFloatOrRange(string propertyName, float value,
            MetallicGlossinessShaderArguments arguments)
        {
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
                case "_Cutoff":
                    arguments.Cutoff = value;
                    break;
                case "_GlossMapScale":
                    arguments.GlossinessTextureScale = value;
                    break;
                case "_Glossiness":
                    arguments.Glossiness = value;
                    break;
                case "_Metallic":
                    arguments.Metallic = value;
                    break;
                case "_OcclusionStrength":
                    arguments.OcclusionStrength = value;
                    break;
                case "_Parallax": break;
            }
        }

        protected virtual void ParseColor(string propertyName, Color color, MetallicGlossinessShaderArguments arguments)
        {
            switch (propertyName)
            {
                case "_Color":
                    arguments.BaseColorColor = color;
                    break;
                case "_EmissionColor":
                    arguments.EmissiveColor = color;
                    break;
            }
        }

        protected virtual MetallicGlossinessShaderArguments CreateShaderArguments(Material material)
        {
            return new MetallicGlossinessShaderArguments();
        }
    }
}