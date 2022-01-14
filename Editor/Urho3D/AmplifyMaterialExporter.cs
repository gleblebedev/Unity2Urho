using UnityEngine;

namespace UnityToCustomEngineExporter.Editor.Urho3D
{
    [CustomUrho3DExporter(typeof(Material))]
    public class AmplifyMaterialExporter : ParticleStandardUnlitMaterialExporter
    {
        public AmplifyMaterialExporter(Urho3DEngine engine) : base(engine)
        {
        }
        public override int ExporterPriority => 10;

        public override bool CanExportMaterial(Material material)
        {
            string src = AmplifyShaderExporter.ReadShaderText(material.shader);
            if (string.IsNullOrWhiteSpace(src))
            {
                return false;
            }
            return false;
            return src.Contains("/*ASEBEGIN");
        }

        public override void ExportMaterial(Material material, PrefabContext prefabContext)
        {
            Engine.ScheduleAmplifyShader(material.shader, prefabContext);
            base.ExportMaterial(material, prefabContext);
        }
    }
}