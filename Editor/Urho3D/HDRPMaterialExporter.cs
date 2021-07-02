using System;
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
            switch (material.shader.name)
            {
                case "HDRP/Lit":
                case "HDRP/LayeredLit":
                    return true;
            }
            return false;
        }

        protected override void ParseColor(string propertyName, Color color,
            MetallicGlossinessShaderArguments arguments)
        {
            switch (propertyName)
            {
                case "_BaseColor":
                case "_BaseColor0":
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
                    //Debug.Log("Color " + propertyName + " = " + color);
                    break;
            }
        }

        protected override void ParseTexture(string propertyName, Texture texture,
            MetallicGlossinessShaderArguments arguments)
        {
            if (texture == null)
                return;
            if (texture == null)
                return;
            switch (propertyName)
            {
                case "_BaseColorMap":
                case "_BaseColorMap0":
                    arguments.BaseColor = texture;
                    break;
                case "_MaskMap":
                case "_MaskMap0":
                    arguments.MetallicGloss = texture;
                    break;
                case "_NormalMap":
                case "_NormalMap0":
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

        protected override void ParseFloatOrRange(string propertyName, float value,
            MetallicGlossinessShaderArguments arguments)
        {
            switch (propertyName)
            {
                case "_NormalScale":
                case "_NormalScale0":
                    arguments.BumpScale = value;
                    break;
                case "_Cutoff":
                    arguments.Cutoff = value;
                    break;
                case "_SmoothnessRemapMin":
                case "_SmoothnessRemapMin0":
                    arguments.SmoothnessRemapMin = value;
                    break;
                case "_SmoothnessRemapMax":
                case "_SmoothnessRemapMax0":
                    arguments.SmoothnessRemapMax = value;
                    break;
                case "_Smoothness":
                case "_Smoothness0":
                    arguments.Glossiness = value;
                    break;
                case "_Metallic":
                case "_Metallic0":
                    arguments.Metallic = 0;
                    arguments.MetallicScale = value;
                    break;
                case "_AORemapMax":
                case "_AORemapMax0":
                    arguments.OcclusionStrength = value;
                    break;
                case "_SurfaceType":
                    arguments.Transparent = (int) value == 1;
                    break;
            }
        }
    }
}