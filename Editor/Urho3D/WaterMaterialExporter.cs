using Assets.Scripts.UnityToCustomEngineExporter.Editor.Urho3D;
using UnityEngine;

namespace UnityToCustomEngineExporter.Editor.Urho3D
{
    [CustomUrho3DExporter(typeof(Material))]
    public class WaterMaterialExporter : StandardMaterialExporter, IUrho3DMaterialExporter
    {
        public WaterMaterialExporter(Urho3DEngine engine) : base(engine)
        {
        }

        public override int ExporterPriority => 10;

        public override bool CanExportMaterial(Material material)
        {
            return (material.shader.name == "Urho3D/PBR/PBRWater");
        }

        protected override UrhoPBRMaterial FromMetallicGlossiness(Material mat, MetallicGlossinessShaderArguments arguments)
        {
            var material = base.FromMetallicGlossiness(mat, arguments);
            var _WaterMetallic = mat.GetFloat("_WaterMetallic");
            var _WaterSmoothness = mat.GetFloat("_WaterSmoothness");
            var _FlowSpeed = mat.GetFloat("_FlowSpeed");
            var _TimeScale = mat.GetFloat("_TimeScale");
            var _FresnelPower = mat.GetFloat("_FresnelPower");
            
            material.ExtraParameters.Add("WaterMetallic", _WaterMetallic);
            material.ExtraParameters.Add("WaterRoughness", 1.0f - _WaterSmoothness);
            material.ExtraParameters.Add("WaterFlowSpeed", _FlowSpeed);
            material.ExtraParameters.Add("WaterTimeScale", _TimeScale);
            material.ExtraParameters.Add("WaterFresnelPower", _FresnelPower);
            if (arguments.BaseColor != null)
            {
                material.PixelShaderDefines.Add("DIFFMAP");
            }
            material.Technique = "Techniques/PBR/PBRWater.xml";

            return material;
        }
    }
}