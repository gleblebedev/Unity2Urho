using System.Globalization;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace UnityToCustomEngineExporter.Editor.Urho3D
{
    [CustomUrho3DExporter(typeof(Material))]
    public class AutodeskInteractiveExporter : AbstractMaterialExporter, IUrho3DMaterialExporter
    {
        public AutodeskInteractiveExporter(Urho3DEngine engine) : base(engine)
        {
        }

        public override int ExporterPriority => 0;

        public override bool CanExportMaterial(Material material)
        {
            return material.shader.name == "Autodesk Interactive";
        }

        public override void ExportMaterial(Material material, PrefabContext prefabContext)
        {
            var urhoPath = EvaluateMaterialName(material, prefabContext);
            using (var writer =
                   Engine.TryCreateXml(material.GetKey(), urhoPath, ExportUtils.GetLastWriteTimeUtc(material)))
            {
                if (writer == null)
                    return;

                var arguments = SetupAutodeskInteractivePBR(material);
                var urhoMaterial = FromAutodeskInteractive(material, arguments);

                WriteMaterial(writer, urhoMaterial, prefabContext);

                //Engine.ScheduleTexture(arguments.Bump, new TextureReference(TextureSemantic.Bump));

                Engine.SchedulePBRTextures(arguments, urhoMaterial);

                Engine.ScheduleTexture(arguments.Emission);
                //Engine.ScheduleTexture(arguments.Occlusion, new TextureReference(TextureSemantic.Occlusion));
            }
        }

        protected virtual UrhoPBRMaterial FromAutodeskInteractive(Material mat,
            AutodeskInteractiveShaderArguments arguments)
        {
            var material = new UrhoPBRMaterial();
            FixNormalScale(material, arguments);
            material.NormalTexture = GetScaledNormalTextureName(arguments.Bump, arguments.BumpScale);
            if (arguments.Bump != null && Engine.Options.PackedNormal) material.PixelShaderDefines.Add("PACKEDNORMAL");
            material.EmissiveTexture = Engine.EvaluateTextrueName(arguments.Emission);
            if (!Engine.Options.RBFX)
            {
                material.AOTexture = BuildAOTextureName(arguments.Occlusion, arguments.OcclusionStrength);
            }

            material.BaseColorTexture = Engine.EvaluateTextrueName(arguments.Diffuse);
            var metalicGlossinesTexture = Engine.EvaluateTextrueName(arguments.MetallicMap ?? arguments.RoughnessMap);
            var linearMetallic = Engine.FixMaterialColorSpace(new Color(arguments.Metallic, 0, 0, 1)).r;
            if (string.IsNullOrWhiteSpace(metalicGlossinesTexture))
            {
                material.Metallic = linearMetallic;
                material.Roughness = 1.0f - arguments.Glossiness;
            }
            else
            {
                if (Engine.Options.RBFX)
                {
                    material.Metallic = 1.0f;
                    material.Roughness = 1.0f;
                }

                var texNameBuilder = new TextureNameBuilder(Engine)
                    .WithTexture(arguments.MetallicMap)
                    .WithTexture(arguments.RoughnessMap)
                    .WithTexture(arguments.Occlusion)
                    .Append("MetallicRoughness.dds");

                material.MetallicRoughnessTexture = texNameBuilder.ToString();
            }

            material.BaseColor = Engine.FixMaterialColorSpace(arguments.Color);
            material.AlphaBlend = arguments.Transparent;
            material.Cull = Urho3DCulling.ccw;
            material.ShadowCull = Urho3DCulling.ccw;
            if (arguments.AlphaTest) material.PixelShaderDefines.Add("ALPHAMASK");
            if (arguments.HasEmission) material.EmissiveColor = Engine.FixMaterialColorSpace(arguments.EmissiveColor);
            material.MatSpecColor = new Color(1, 1, 1, 0);
            material.UOffset = new Vector4(arguments.MainTextureScale.x, 0, 0, arguments.MainTextureOffset.x);
            material.VOffset = new Vector4(0, arguments.MainTextureScale.y, 0, arguments.MainTextureOffset.y);
            material.EvaluateTechnique();
            foreach (var argumentsExtraParameter in arguments.ExtraParameters)
            {
                material.ExtraParameters[argumentsExtraParameter.Key] = argumentsExtraParameter.Value;
            }
            return material;
        }

        private AutodeskInteractiveShaderArguments SetupAutodeskInteractivePBR(Material material)
        {
            var arguments = new AutodeskInteractiveShaderArguments();
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
                                    arguments.Color = color;
                                    break;
                                case "_EmissionColor":
                                    arguments.EmissiveColor = color;
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

                            break;
                        }
                    case ShaderUtil.ShaderPropertyType.TexEnv:
                        {
                            var texture = material.GetTexture(propertyName);
                            switch (propertyName)
                            {
                                case "_MainTex":
                                    arguments.Diffuse = texture;
                                    break;
                                case "_SpecGlossMap":
                                    arguments.RoughnessMap = texture;
                                    break;
                                case "_MetallicGlossMap":
                                    arguments.MetallicMap = texture;
                                    break;
                                case "_BumpMap":
                                    arguments.Bump = texture;
                                    break;
                                case "_ParallaxMap":
                                    arguments.Parallax = texture;
                                    break;
                                case "_OcclusionMap":
                                    arguments.Occlusion = texture;
                                    break;
                                case "_EmissionMap":
                                    arguments.Emission = texture;
                                    break;
                                case "_DetailMask":
                                    arguments.Detail = texture;
                                    break;
                                case "_DetailAlbedoMap":
                                    break;
                                case "_DetailNormalMap":
                                    arguments.DetailNormal = texture;
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