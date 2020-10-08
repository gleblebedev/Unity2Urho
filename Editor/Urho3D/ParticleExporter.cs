using System;
using System.Xml;
using UnityEngine;
using UnityEngine.Video;

namespace UnityToCustomEngineExporter.Editor.Urho3D
{
    public class ParticleExporter
    {
        private Urho3DEngine _engine;

        public ParticleExporter(Urho3DEngine engine)
        {
            _engine = engine;
        }

        public string EvaluateName(ParticleSystem particleSystem, PrefabContext context)
        {
            if (particleSystem == null)
                return null;

            return ExportUtils.Combine(context.TempFolder, particleSystem.name + ".xml");
        }

        public void ExportEffect(ParticleSystem particleSystem, PrefabContext prefabContext)
        {
            if (!_engine.Options.ExportParticles)
                return;

            var key = ExportUtils.GetKey(particleSystem);
            var name = EvaluateName(particleSystem, prefabContext);
            using (var writer = _engine.TryCreateXml(key, name, DateTime.UtcNow))
            {
                if (writer == null)
                    return;
                writer.WriteStartElement("particleeffect");
                writer.WriteWhitespace(Environment.NewLine);

                ExportMaterial(particleSystem, writer, prefabContext);
                ExportEmissionRate(particleSystem, writer);
                ExportNumParticles(particleSystem, writer);
                //particleSystem.sizeOverLifetime
                /*
                    <material name="Materials/Particle.xml" />
                    <numparticles value="10" />
                    <updateinvisible enable="false" />
                    <relative enable="true" />
                    <scaled enable="true" />
                    <sorted enable="false" />
                    <fixedscreensize enable="false" />
                    <animlodbias value="1" />
                    <emittertype value="Sphere" />
                    <emittersize value="0 0 0" />
                    <direction min="-2 -1 -1" max="2 1 1" />
                    <constantforce value="1 0 0" />
                    <dampingforce value="1" />
                    <activetime value="1" />
                    <inactivetime value="1" />
                    <emissionrate min="10" max="10" />
                    <particlesize min="0.11 0.1" max="1 1" />
                    <timetolive min="2" max="1" />
                    <velocity min="2" max="1" />
                    <rotation min="1" max="0" />
                    <rotationspeed min="1" max="0" />
                    <sizedelta add="1" mul="2" />
                    <faceCameraMode value="Rotate XYZ" />
                    <color value="1 1 1 1" />
                 */
                writer.WriteEndElement();
            }
        }

        private void ExportMaterial(ParticleSystem particleSystem, XmlWriter writer, PrefabContext prefabContext)
        {
            var renderer = particleSystem.GetComponent<Renderer>();
            if (renderer == null)
                return;
            var material = renderer.sharedMaterial;
            if (material == null)
                return;

            var name = _engine.EvaluateMaterialName(material, prefabContext);
            _engine.ScheduleAssetExport(material, prefabContext);
            writer.WriteStartElement("material");
            writer.WriteAttributeString("name", name);
            writer.WriteEndElement();
            writer.WriteWhitespace(Environment.NewLine);

            var sheetAnimation = particleSystem.textureSheetAnimation;
            if (sheetAnimation.enabled)
            {
                var numTilesX = sheetAnimation.numTilesX;
                var numTilesY = sheetAnimation.numTilesY;
                var width = 1.0f / numTilesX;
                var height = 1.0f / numTilesY;

                int frame = 0;
                for (int y=0; y<numTilesY; ++y)
                for (int x = 0; x < numTilesX; ++x)
                {
                    writer.WriteStartElement("texanim");
                    writer.WriteAttribute("uv", new Vector4(x * width, y * height, (x + 1) * width, (y + 1) * height));
                    writer.WriteAttribute("time", (float)frame/ (float)sheetAnimation.fps);
                    writer.WriteEndElement();
                    writer.WriteWhitespace(Environment.NewLine);
                }

                //var mode = sheetAnimation.frameOverTime.mode;
                //switch (mode)
                //{
                //    case ParticleSystemCurveMode.Curve:
                //        var curve = sheetAnimation.frameOverTime.curve;
                //        foreach (var keyframe in curve.keys)
                //        {
                //            writer.WriteStartElement("texanim");
                //            var frame = (int)keyframe.value;
                //            var y = (frame / numTilesX) % numTilesY;
                //            var x = (frame - y * numTilesX) % numTilesX;
                //            writer.WriteAttribute("uv", new Vector4(x * width, y * height, (x + 1) * width, (y + 1) * height));
                //            writer.WriteAttribute("time", keyframe.time);
                //            writer.WriteEndElement();
                //            writer.WriteWhitespace(Environment.NewLine);
                //        }
                //        break;
                //}
            }
        }

        private void ExportNumParticles(ParticleSystem particleSystem, XmlWriter writer)
        {
            writer.WriteStartElement("numparticles");
            writer.WriteAttribute("value", particleSystem.main.maxParticles);
            writer.WriteEndElement();
            writer.WriteWhitespace(Environment.NewLine);
        }

        private static void ExportEmissionRate(ParticleSystem particleSystem, XmlWriter writer)
        {
            var rateOverTime = particleSystem.emission.rateOverTime;
            switch (rateOverTime.mode)
            {
                case ParticleSystemCurveMode.Constant:
                default:
                    writer.WriteStartElement("emissionrate");
                    writer.WriteAttribute("min", rateOverTime.constant);
                    writer.WriteAttribute("max", rateOverTime.constant);
                    writer.WriteEndElement();
                    writer.WriteWhitespace(Environment.NewLine);
                    break;
            }
        }
    }
}