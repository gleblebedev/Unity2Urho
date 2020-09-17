using UnityEngine;

namespace UnityToCustomEngineExporter.Editor.Urho3D.MaterialExporters
{
    [CustomUrho3DExporter(typeof(Material))]
    public class NatureManufactureWaterMaterialExporter : AbstractMaterialExporter, IUrho3DMaterialExporter
    {
        public NatureManufactureWaterMaterialExporter(Urho3DEngine engine) : base(engine)
        {
        }

        public override int ExporterPriority => 10;

        public override bool CanExportMaterial(Material material)
        {
            return material.shader.name.StartsWith("NatureManufacture Shaders/Water/");
        }

        public override void ExportMaterial(Material material, PrefabContext prefabContext)
        {
            var urhoPath = EvaluateMaterialName(material);
            using (var writer =
                Engine.TryCreateXml(material.GetKey(), urhoPath, ExportUtils.GetLastWriteTimeUtc(material)))
            {
                if (writer == null)
                    return;

                var urhoMaterial = new UrhoPBRMaterial();
                urhoMaterial.Technique = "Techniques/PBR/PBRWater.xml";
                var metallicGlossinessShaderArguments = new MetallicGlossinessShaderArguments();

                var _SlowWaterNormal = GetTexture(material,"_SlowWaterNormal");
                var _SlowNormalScale = GetFloat(material, "_SlowNormalScale", 1);

                metallicGlossinessShaderArguments.Bump = _SlowWaterNormal;
                metallicGlossinessShaderArguments.BumpScale = _SlowNormalScale;
                urhoMaterial.NormalTexture = GetScaledNormalTextureName(_SlowWaterNormal, _SlowNormalScale);

                urhoMaterial.ExtraParameters.Add("WaterMetallic", 1);
                urhoMaterial.ExtraParameters.Add("WaterRoughness", 1.0f - 1);
                urhoMaterial.ExtraParameters.Add("WaterFlowSpeed", 0.2);
                urhoMaterial.ExtraParameters.Add("WaterTimeScale", 1);
                urhoMaterial.ExtraParameters.Add("WaterFresnelPower", 4);

                Engine.SchedulePBRTextures(metallicGlossinessShaderArguments, urhoMaterial);

                WriteMaterial(writer, urhoMaterial, prefabContext);
            }
        }
    }
}