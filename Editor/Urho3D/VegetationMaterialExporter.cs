using UnityToCustomEngineExporter.Editor.Urho3D;
using UnityEngine;
using UnityEngine.Rendering;

namespace UnityToCustomEngineExporter.Editor.Urho3D
{
    [CustomUrho3DExporter(typeof(Material))]
    public class VegetationMaterialExporter : StandardMaterialExporter, IUrho3DMaterialExporter
    {
        public VegetationMaterialExporter(Urho3DEngine engine) : base(engine)
        {
        }

        public override int ExporterPriority => 10;

        public override bool CanExportMaterial(Material material)
        {
            return (material.shader.name == "Urho3D/PBR/PBRVegetation");
        }

        protected override UrhoPBRMaterial FromMetallicGlossiness(Material mat, MetallicGlossinessShaderArguments arguments)
        {
            var material = base.FromMetallicGlossiness(mat, arguments);
            var _WindHeightFactor = mat.GetFloat("_WindHeightFactor");
            var _WindHeightPivot = mat.GetFloat("_WindHeightPivot");
            var _WindStemAxisX = mat.GetFloat("_WindStemAxisX");
            var _WindStemAxisY = mat.GetFloat("_WindStemAxisY");
            var _WindStemAxisZ = mat.GetFloat("_WindStemAxisZ");
            var _WindPeriod = mat.GetFloat("_WindPeriod");
            var _WindWorldSpacingX = mat.GetFloat("_WindWorldSpacingX");
            var _WindWorldSpacingY = mat.GetFloat("_WindWorldSpacingY");
            material.ExtraParameters.Add("WindHeightFactor", _WindHeightFactor);
            material.ExtraParameters.Add("WindHeightPivot", _WindHeightPivot);
            var windStemAxis = new Vector3(_WindStemAxisX, _WindStemAxisY, _WindStemAxisZ);
            material.ExtraParameters.Add("WindStemAxis", windStemAxis);
            material.ExtraParameters.Add("WindPeriod", _WindPeriod);
            material.ExtraParameters.Add("WindWorldSpacingX", new Vector2(_WindWorldSpacingX, _WindWorldSpacingY));

            CullMode cull = (CullMode)mat.GetFloat("_Cull");
            switch (cull)
            {
                case CullMode.Off:
                    material.Cull = Urho3DCulling.none;
                    material.ShadowCull = Urho3DCulling.none;
                    break;
                case CullMode.Front:
                    material.Cull = Urho3DCulling.cw;
                    material.ShadowCull = Urho3DCulling.cw;
                    break;
                default:
                    material.Cull = Urho3DCulling.ccw;
                    material.ShadowCull = Urho3DCulling.ccw;
                    break;
            }

            if (windStemAxis != Vector3.up)
            {
                material.VertexShaderDefines.Add("WINDSTEMAXIS");
            }
            material.Technique = "Techniques/PBR/PBRVegetationDiff.xml";

            return material;
        }
    }
}