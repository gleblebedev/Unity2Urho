using UnityEngine;

namespace UnityToCustomEngineExporter.Editor.Urho3D
{
    [CustomUrho3DExporter(typeof(Material))]
    public class HDRPMaterialExporter : StandardMaterialExporter, IUrho3DMaterialExporter
    {
        public HDRPMaterialExporter(Urho3DEngine engine) : base(engine)
        {
        }

        public override int ExporterPriority => 10;

        public override bool CanExportMaterial(Material material)
        {
            return (material.shader.name == "HDRP/Lit");
        }

        protected override void ParseColor(string propertyName, Color color, MetallicGlossinessShaderArguments arguments)
        {
            switch (propertyName)
            {
                case "_BaseColor":
                    arguments.BaseColorColor = color;
                    break;
                case "_EmissiveColor":
                    arguments.EmissiveColor = color;
                    break;
                case "_EmissiveColorLDR":
                    //arguments.EmissiveColorLDR = color;
                    break;
                case "_EmissionColor":
                    //arguments.EmissionColor = color;
                    break;
                case "_SpecularColor":
                    //arguments.SpecularColor = color;
                    break;
                case "_UVDetailsMappingMask":
                    //arguments.UVDetailsMappingMask = color;
                    break;
                case "_UVMappingMaskEmissive":
                    //arguments.UVMappingMaskEmissive = color;
                    break;
                case "_TransmittanceColor":
                    //arguments.TransmittanceColor = color;
                    break;
                default:
                    Debug.Log("Color " + propertyName + " = " + color);
                    break;
            }
        }

        protected override void ParseTexture(string propertyName, Texture texture, MetallicGlossinessShaderArguments arguments)
        {
            if (texture == null)
                return;
            if (texture == null)
                return;
            switch (propertyName)
            {
                case "_BaseColorMap":
                    arguments.BaseColor = texture;
                    break;
                case "_MaskMap":
                    arguments.MetallicGloss = texture;
                    break;
                case "_NormalMap":
                    arguments.Bump = texture;
                    break;
                case "_EmissiveColorMap":
                    arguments.Emission = texture;
                    break;

                //case "_DetailAlbedoMap":
                //    arguments.DetailBaseColor = texture;
                //    break;
                //case "_DetailMask":
                //    arguments.Detail = texture;
                //    break;
                //case "_DetailNormalMap":
                //    arguments.DetailNormal = texture;
                //    break;
                //case "_OcclusionMap":
                //    arguments.Occlusion = texture;
                //    break;
                //case "_ParallaxMap":
                //    arguments.Parallax = texture;
                //    break;
            }
        }

        protected override void ParseFloatOrRange(string propertyName, float value, MetallicGlossinessShaderArguments arguments)
        {
            switch (propertyName)
            {
                case "_NormalScale":
                    arguments.BumpScale = value;
                    break;
                case "_Cutoff":
                    arguments.Cutoff = value;
                    break;
                case "_SmoothnessRemapMin":
                    arguments.SmoothnessRemapMin = value;
                    break;
                case "_SmoothnessRemapMax":
                    arguments.SmoothnessRemapMax = value;
                    break;
                case "_Smoothness":
                    arguments.Glossiness = value;
                    break;
                case "_Metallic":
                    arguments.Metallic = 0;
                    arguments.MetallicScale = value;
                    break;
                case "_AORemapMax":
                    arguments.OcclusionStrength = value;
                    break;
            }
        }
    }
}