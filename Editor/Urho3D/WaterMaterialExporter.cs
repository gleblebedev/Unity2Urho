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
            //var _WindHeightFactor = mat.GetFloat("_WindHeightFactor");
            //var _WindHeightPivot = mat.GetFloat("_WindHeightPivot");
            //var _WindStemAxisX = mat.GetFloat("_WindStemAxisX");
            //var _WindStemAxisY = mat.GetFloat("_WindStemAxisY");
            //var _WindStemAxisZ = mat.GetFloat("_WindStemAxisZ");
            //var _WindPeriod = mat.GetFloat("_WindPeriod");
            //var _WindWorldSpacingX = mat.GetFloat("_WindWorldSpacingX");
            //var _WindWorldSpacingY = mat.GetFloat("_WindWorldSpacingY");
            //material.ExtraParameters.Add("WindHeightFactor", _WindHeightFactor);
            //material.ExtraParameters.Add("WindHeightPivot", _WindHeightPivot);
            //material.ExtraParameters.Add("WindStemAxis", new Vector3(_WindStemAxisX, _WindStemAxisY, _WindStemAxisZ));
            //material.ExtraParameters.Add("WindPeriod", _WindPeriod);
            //material.ExtraParameters.Add("WindWorldSpacingX", new Vector2(_WindWorldSpacingX, _WindWorldSpacingY));
            material.Technique = "Techniques/PBR/PBRWater.xml";

            return material;
        }
    }
}