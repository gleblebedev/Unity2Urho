using System;
using UnityEngine;

namespace UnityToCustomEngineExporter.Editor.Urho3D
{
    [CustomUrho3DExporter(typeof(Material))]
    public class DistortionMaterialExporter : AbstractMaterialExporter, IUrho3DMaterialExporter
    {

        public DistortionMaterialExporter(Urho3DEngine engine) : base(engine)
        {
        }

        public override int ExporterPriority { get; }

        public override bool CanExportMaterial(Material material)
        {
            var shaderName = material.shader.name;
            return (shaderName == "Custom/Distortion" || shaderName == "Hovl/Particles/Distortion");
        }

        public override void ExportMaterial(Material material, PrefabContext prefabContext)
        {
            var urhoPath = EvaluateMaterialName(material, prefabContext);
            using (var writer =
                   Engine.TryCreateXml(material.GetKey(), urhoPath, ExportUtils.GetLastWriteTimeUtc(material)))
            {
                if (writer == null)
                    return;

                writer.WriteStartElement("material"); writer.WriteWhitespace(Environment.NewLine);
                WriteTechnique(writer, "Techniques/Refraction.xml");
                writer.WriteElementParameter("renderorder", "value", "0");
                Texture mainTex = null;
                if (material.HasProperty("_DistTex"))
                    mainTex = material.GetTexture("_DistTex");
                if (mainTex == null && material.HasProperty("_BumpMap"))
                    mainTex = material.GetTexture("_BumpMap");
                if (mainTex == null && material.HasProperty("_NormalMap"))
                    mainTex = material.GetTexture("_NormalMap");
                
                if (mainTex != null)
                {
                    WriteTexture(mainTex, writer, "normal", prefabContext);
                }
                writer.WriteParameter("NoiseStrength", 0.01f);
                writer.WriteEndElement();
            }
        }
    }
}