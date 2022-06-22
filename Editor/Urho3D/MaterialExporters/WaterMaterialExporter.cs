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
                {
                    writer.WriteWhitespace("\t");
                    writer.WriteStartElement("shader");
                    writer.WriteAttributeString("vsdefines", "PBR");
                    writer.WriteAttributeString("psdefines", "PBR");
                    writer.WriteEndElement();
                    writer.WriteWhitespace(Environment.NewLine);
                }
                WriteTexture(material.GetTexture("_BumpMap"), writer, "normal", prefabContext);
                writer.WriteParameter("MatDiffColor", material.GetColor("_Color"));
                var specColor = material.GetColor("_SpecularColor");
                writer.WriteParameter("MatSpecColor", new Vector4(specColor.r, specColor.g, specColor.b, material.GetFloat("_SpecularPower")));
                writer.WriteParameter("NormalScale", material.GetFloat("_BumpScale"));
                writer.WriteParameter("NoiseSpeed", new Vector2(material.GetFloat("_NoiseSpeedX"), material.GetFloat("_NoiseSpeedY")));
                writer.WriteParameter("NoiseStrength", material.GetFloat("_NoiseStrength"));
                writer.WriteParameter("FadeOffsetScale", new Vector2(material.GetFloat("_FadeOffset"), material.GetFloat("_FadeScale")));

                writer.WriteEndElement();
            }
        }
    }
}