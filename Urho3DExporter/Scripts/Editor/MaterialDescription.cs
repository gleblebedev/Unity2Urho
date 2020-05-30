using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace Assets.Urho3DExporter.Scripts.Editor
{
    public class MaterialDescription
    {
        public MetallicRoughnessShaderArguments MetallicRoughness { get; set; }
        public SpecularGlossinessShaderArguments SpecularGlossiness { get; set; }
        public LegacyShaderArguments Legacy { get; set; }
        public MaterialDescription(Material material)
        {
            HasAlpha = material.renderQueue == (int)RenderQueue.Transparent;
            AlphaTest = material.renderQueue == (int) RenderQueue.AlphaTest;
            HasEmissive = material.IsKeywordEnabled("_EMISSION");

            if (material.shader.name == "Standard (Specular setup)")
            {
                SpecularGlossiness = SetupSpecularGlossinessPBR(material);
            }
            else if (material.shader.name == "Standard")
            {
                MetallicRoughness = SetupMetallicRoughnessPBR(material);
            }
            else
            {
                Legacy = SetupLegacy(material);
            }
        }

        private LegacyShaderArguments SetupLegacy(Material material)
        {
            var arguments = new LegacyShaderArguments();
            var shader = material.shader;
            LogShaderParameters(shader);
            return arguments;
        }

        private static void LogShaderParameters(Shader shader)
        {
            var sb = new StringBuilder();
            sb.AppendFormat("Shader parameters for \"" + shader.name + "\"");
            sb.AppendLine();
            for (var i = 0; i < ShaderUtil.GetPropertyCount(shader); i++)
            {
                var propertyName = ShaderUtil.GetPropertyName(shader, i);
                var propertyType = ShaderUtil.GetPropertyType(shader, i);
                sb.AppendFormat("{0} {1}", propertyType, propertyName);
                sb.AppendLine();
            }

            Debug.Log(sb.ToString());
        }

        private SpecularGlossinessShaderArguments SetupSpecularGlossinessPBR(Material material)
        {
            var arguments = new SpecularGlossinessShaderArguments();
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
                                case "_Color": break;
                                case "_EmissionColor": break;
                                case "_SpecColor": break;
                            }
                            break;
                        }
                    case ShaderUtil.ShaderPropertyType.Float:
                        {
                            switch (propertyName)
                            {
                                case "_BumpScale": break;
                                case "_DetailNormalMapScale": break;
                                case "_DstBlend": break;
                                case "_GlossyReflections": break;
                                case "_Mode": break;
                                case "_SmoothnessTextureChannel": break;
                                case "_SpecularHighlights": break;
                                case "_SrcBlend": break;
                                case "_UVSec": break;
                                case "_ZWrite": break;
                            }
                            break;
                        }
                    case ShaderUtil.ShaderPropertyType.Range:
                        {
                            switch (propertyName)
                            {
                                case "_Cutoff": break;
                                case "_GlossMapScale": break;
                                case "_Glossiness": break;
                                case "_OcclusionStrength": break;
                                case "_Parallax": break;
                            }
                            break;
                        }
                    case ShaderUtil.ShaderPropertyType.TexEnv:
                        {
                            switch (propertyName)
                            {
                                case "_BumpMap": break;
                                case "_DetailAlbedoMap": break;
                                case "_DetailMask": break;
                                case "_DetailNormalMap": break;
                                case "_EmissionMap": break;
                                case "_MainTex": break;
                                case "_OcclusionMap": break;
                                case "_ParallaxMap": break;
                                case "_SpecGlossMap": break;
                            }
                            break;
                        }
                }
            }
            return arguments;
        }

        private MetallicRoughnessShaderArguments SetupMetallicRoughnessPBR(Material material)
        {
            var arguments = new MetallicRoughnessShaderArguments();
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
                            case "_Color": break;
                            case "_EmissionColor": break;
                        }
                        break;
                    }
                    case ShaderUtil.ShaderPropertyType.Float:
                    {
                        switch (propertyName)
                        {
                            case "_BumpScale": break;
                            case "_DetailNormalMapScale": break;
                            case "_DstBlend": break;
                            case "_GlossyReflections": break;
                            case "_Mode": break;
                            case "_SmoothnessTextureChannel": break;
                            case "_SpecularHighlights": break;
                            case "_SrcBlend": break;
                            case "_UVSec": break;
                            case "_ZWrite": break;
                        }
                        break;
                    }
                    case ShaderUtil.ShaderPropertyType.Range:
                    {
                        switch (propertyName)
                        {
                            case "_Cutoff": break;
                            case "_GlossMapScale": break;
                            case "_Glossiness": break;
                            case "_Metallic": break;
                            case "_OcclusionStrength": break;
                            case "_Parallax": break;
                        }
                        break;
                    }
                    case ShaderUtil.ShaderPropertyType.TexEnv:
                    {
                        var texture = material.GetTexture(propertyName);
                        if (texture != null)
                        {
                            switch (propertyName)
                            {
                                case "_BumpMap": arguments.Bump = texture; break;
                                case "_DetailAlbedoMap": arguments.DetailAlbedo = texture; break;
                                case "_DetailMask": arguments.Detail = texture; break;
                                case "_DetailNormalMap": arguments.DetailNormal = texture; break;
                                case "_EmissionMap": arguments.Emission = texture; break;
                                case "_MainTex": arguments.Albedo = texture; break;
                                case "_MetallicGlossMap": arguments.MetallicGloss = texture; break;
                                case "_OcclusionMap": arguments.Occlusion = texture; break;
                                case "_ParallaxMap": arguments.Parallax = texture; break;
                            }
                        }

                        break;
                }
                }
            }
            return arguments;
        }


        public bool AlphaTest { get; set; }

        public bool HasEmissive { get; set; }

        public bool HasAlpha { get; set; }
    }
}
