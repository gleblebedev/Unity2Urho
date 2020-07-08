using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace UnityToCustomEngineExporter.Editor
{
    public class MaterialDescription
    {
        public MaterialDescription(Material material)
        {
            if (material.shader.name == "Standard (Specular setup)")
                SpecularGlossiness = SetupSpecularGlossinessPBR(material);
            else if (material.shader.name == "Standard")
                MetallicGlossiness = SetupMetallicRoughnessPBR(material);
            else if (material.shader.name == "ProBuilder/Standard Vertex Color")
                MetallicGlossiness = SetupMetallicRoughnessPBR(material);
            else if (material.shader.name == "UnityChan/Skin")
                MetallicGlossiness = SetupMetallicRoughnessPBR(material);
            else if (material.shader.name.StartsWith("Skybox/"))
                Skybox = SetupSkybox(material);
            else if (material.shader.name.StartsWith("Urho3D/"))
                MetallicGlossiness = SetupMetallicRoughnessPBR(material);
            else
                Legacy = SetupLegacy(material);
        }

        public MetallicGlossinessShaderArguments MetallicGlossiness { get; set; }
        public SpecularGlossinessShaderArguments SpecularGlossiness { get; set; }
        public SkyboxShaderArguments Skybox { get; set; }
        public LegacyShaderArguments Legacy { get; set; }


        private SkyboxShaderArguments SetupSkybox(Material material)
        {
            var setupProceduralSkybox = new SkyboxShaderArguments();
            var shader = material.shader;
            for (var i = 0; i < ShaderUtil.GetPropertyCount(shader); i++)
            {
                var propertyName = ShaderUtil.GetPropertyName(shader, i);
                var propertyType = ShaderUtil.GetPropertyType(shader, i);
                if (propertyType == ShaderUtil.ShaderPropertyType.TexEnv)
                {
                    var texture = material.GetTexture(propertyName);
                    switch (propertyName)
                    {
                        case "_Tex": setupProceduralSkybox.Skybox = texture; break;
                        case "_MainTex": setupProceduralSkybox.Skybox = texture; break;
                        case "_FrontTex": setupProceduralSkybox.FrontTex = texture; break;
                        case "_BackTex": setupProceduralSkybox.BackTex = texture; break;
                        case "_LeftTex": setupProceduralSkybox.LeftTex = texture; break;
                        case "_RightTex": setupProceduralSkybox.RightTex = texture; break;
                        case "_UpTex": setupProceduralSkybox.UpTex = texture; break;
                        case "_DownTex": setupProceduralSkybox.DownTex = texture; break;
                    }
                }
            }

            return setupProceduralSkybox;
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

        private void SetupFlags(Material material, ShaderArguments arguments)
        {
            arguments.Shader = material.shader.name;
            arguments.Transparent = material.renderQueue == (int) RenderQueue.Transparent;
            arguments.AlphaTest = material.renderQueue == (int) RenderQueue.AlphaTest;
            arguments.HasEmission = material.IsKeywordEnabled("_EMISSION");
            if (material.HasProperty("_MainTex"))
            {
                arguments.MainTextureOffset = material.mainTextureOffset;
                arguments.MainTextureScale = material.mainTextureScale;
            }
        }

        private MetallicGlossinessShaderArguments SetupMetallicRoughnessPBR(Material material)
        {
            var arguments = new MetallicGlossinessShaderArguments();
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
                                arguments.BaseColorColor = color;
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

                        break;
                    }
                }
            }

            return arguments;
        }
    }
}