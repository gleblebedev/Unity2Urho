using System;
using UnityEngine;

namespace UnityToCustomEngineExporter.Editor.Urho3D.MaterialExporters
{
    [CustomUrho3DExporter(typeof(Material))]
    public class WaterMaterialExporter : AbstractMaterialExporter, IUrho3DMaterialExporter
    {
        public WaterMaterialExporter(Urho3DEngine engine) : base(engine)
        {
        }

        public override int ExporterPriority => 10;

        public override bool CanExportMaterial(Material material)
        {
            if (!Engine.Options.RBFX)
                return false;

            return material.shader.name == "Urho3D/Water";
        }

        public override void ExportMaterial(Material material, PrefabContext prefabContext)
        {
            var urhoPath = EvaluateMaterialName(material, prefabContext);
            using (var writer =
                Engine.TryCreateXml(material.GetKey(), urhoPath, ExportUtils.GetLastWriteTimeUtc(material)))
            {
                if (writer == null)
                    return;
                writer.WriteStartElement("material");
                writer.WriteWhitespace(Environment.NewLine);
                WriteTechnique(writer, "Techniques/LitWater.xml");
                WriteTexture(material.GetTexture("_BumpMap"), writer, "normal", prefabContext);
                writer.WriteParameter("NoiseSpeed", new Vector2(material.GetFloat("_NoiseSpeedX"), material.GetFloat("_NoiseSpeedY")));
                writer.WriteParameter("NoiseTiling", material.GetFloat("_NoiseTiling"));
                writer.WriteParameter("NoiseStrength", material.GetFloat("_NoiseStrength"));
                writer.WriteParameter("FresnelPower", material.GetFloat("_FresnelPower"));
                writer.WriteParameter("FadeOffsetScale", new Vector2(0, 5.0f));
                Color c = material.GetColor("_Color");
                writer.WriteParameter("MatDiffColor", c);

                writer.WriteEndElement();
            }
        }
    }
}