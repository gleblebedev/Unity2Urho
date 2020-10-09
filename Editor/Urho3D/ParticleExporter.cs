using System;
using System.Collections.Generic;
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
                ExportTimeToLive(particleSystem, writer);
                ExportEmissionRate(particleSystem, writer);
                ExportVelocity(particleSystem, writer);
                ExportNumParticles(particleSystem, writer);
                ExportShape(particleSystem, writer);
                ExportSize(particleSystem, writer);

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

        private void ExportSize(ParticleSystem particleSystem, XmlWriter writer)
        {
            writer.WriteStartElement("particlesize");
            GetCurveRange(particleSystem.main.startSpeed, out var min, out var max);
            writer.WriteAttribute("min", new Vector2(min, min));
            writer.WriteAttribute("max", new Vector2(max, max));
            writer.WriteEndElement();
            writer.WriteWhitespace(Environment.NewLine);
        }


        private void ExportShape(ParticleSystem particleSystem, XmlWriter writer)
        {
            if (!particleSystem.shape.enabled)
            {
                writer.WriteStartElement("direction");
                writer.WriteAttribute("min", new Vector3(0,0,1));
                writer.WriteAttribute("max", new Vector3(0, 0, 1));
                writer.WriteEndElement();
                writer.WriteWhitespace(Environment.NewLine);
                return;
            }

            /*
             Ring
            writer.WriteStartElement("emittertype");
            writer.WriteAttributeString("value", "Cylinder");
            writer.WriteEndElement();
            writer.WriteWhitespace(Environment.NewLine);
            writer.WriteStartElement("emittersize");
            writer.WriteAttribute("value", new Vector3(0,0,0));
            writer.WriteEndElement();
            writer.WriteWhitespace(Environment.NewLine);
            */
            switch (particleSystem.shape.shapeType)
            {
                case ParticleSystemShapeType.Sphere:
                    writer.WriteStartElement("emittertype");
                    writer.WriteAttributeString("value", "SphereVolume");
                    writer.WriteEndElement();
                    writer.WriteWhitespace(Environment.NewLine);
                    
                    writer.WriteStartElement("emittersize");
                    writer.WriteAttribute("value", new Vector3(particleSystem.shape.radius, particleSystem.shape.radius, particleSystem.shape.radius));
                    writer.WriteEndElement();
                    writer.WriteWhitespace(Environment.NewLine);
                    break;
                case ParticleSystemShapeType.SphereShell:
                    writer.WriteStartElement("emittertype");
                    writer.WriteAttributeString("value", "Sphere");
                    writer.WriteEndElement();
                    writer.WriteWhitespace(Environment.NewLine);

                    writer.WriteStartElement("emittersize");
                    writer.WriteAttribute("value", new Vector3(particleSystem.shape.radius, particleSystem.shape.radius, particleSystem.shape.radius));
                    writer.WriteEndElement();
                    writer.WriteWhitespace(Environment.NewLine);
                    break;
                case ParticleSystemShapeType.Hemisphere:
                    break;
                case ParticleSystemShapeType.HemisphereShell:
                    break;
                case ParticleSystemShapeType.Cone:
                    break;
                case ParticleSystemShapeType.Box:
                    writer.WriteStartElement("emittertype");
                    writer.WriteAttributeString("value", "Box");
                    writer.WriteEndElement();
                    writer.WriteWhitespace(Environment.NewLine);
                    break;
                case ParticleSystemShapeType.Mesh:
                    break;
                case ParticleSystemShapeType.ConeShell:
                    break;
                case ParticleSystemShapeType.ConeVolume:
                    break;
                case ParticleSystemShapeType.ConeVolumeShell:
                    break;
                case ParticleSystemShapeType.Circle:
                    break;
                case ParticleSystemShapeType.CircleEdge:
                    break;
                case ParticleSystemShapeType.SingleSidedEdge:
                    break;
                case ParticleSystemShapeType.MeshRenderer:
                    break;
                case ParticleSystemShapeType.SkinnedMeshRenderer:
                    break;
                case ParticleSystemShapeType.BoxShell:
                    break;
                case ParticleSystemShapeType.BoxEdge:
                    break;
                case ParticleSystemShapeType.Donut:
                    break;
                case ParticleSystemShapeType.Rectangle:
                    break;
                case ParticleSystemShapeType.Sprite:
                    break;
                case ParticleSystemShapeType.SpriteRenderer:
                    break;
                default:
                    break;
            }
        }


        private IEnumerable<Vector4> GetParticleUVs(ParticleSystem.TextureSheetAnimationModule sheetAnimation)
        {
            if (!sheetAnimation.enabled)
                yield break;

            var numTilesX = sheetAnimation.numTilesX;
            var numTilesY = sheetAnimation.numTilesY;
            var width = 1.0f / numTilesX;
            var height = 1.0f / numTilesY;
            
            for (;;)
            {
                for (int y = 0; y < numTilesY; ++y)
                for (int x = 0; x < numTilesX; ++x)
                {
                    var uv = new Vector4(x * width, y * height, (x + 1) * width, (y + 1) * height);
                    yield return uv;
                }
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

            var lifetime = GetAverageValue(particleSystem.main.startLifetime);

            var sheetAnimation = particleSystem.textureSheetAnimation;
            if (sheetAnimation.enabled)
            {
                var totalTiles = Math.Max(1, sheetAnimation.numTilesX * sheetAnimation.numTilesY);
                float dt = Math.Max(1e-2f, (float)lifetime / (float)sheetAnimation.cycleCount / (float)totalTiles);
                float time = 0.0f;
                foreach (var particleUV in GetParticleUVs(sheetAnimation))
                {
                    writer.WriteStartElement("texanim");
                    writer.WriteAttribute("uv", particleUV);
                    writer.WriteAttribute("time", (float)time);
                    writer.WriteEndElement();
                    writer.WriteWhitespace(Environment.NewLine);
                    time += dt;
                    if (time > lifetime)
                        break;
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

        private double GetAverageValue(ParticleSystem.MinMaxCurve mainStartLifetime)
        {
            switch (mainStartLifetime.mode)
            {
                case ParticleSystemCurveMode.Constant:
                    return mainStartLifetime.constant;
                case ParticleSystemCurveMode.TwoConstants:
                    return (mainStartLifetime.constantMin + mainStartLifetime.constantMax) * 0.5f;
                default:
                    return mainStartLifetime.constant;
            }
        }
        private void ExportVelocity(ParticleSystem particleSystem, XmlWriter writer)
        {
            writer.WriteStartElement("velocity");
            WriteMinMax(writer, particleSystem.main.startSpeed);
            writer.WriteEndElement();
            writer.WriteWhitespace(Environment.NewLine);
        }
        private void ExportTimeToLive(ParticleSystem particleSystem, XmlWriter writer)
        {
            writer.WriteStartElement("timetolive");
            var curve = particleSystem.main.startLifetime;
            WriteMinMax(writer, curve);
            writer.WriteEndElement();
            writer.WriteWhitespace(Environment.NewLine);
        }

        private void WriteMinMax(XmlWriter writer, ParticleSystem.MinMaxCurve curve)
        {
            GetCurveRange(curve, out var min, out var max);
            writer.WriteAttribute("min", min);
            writer.WriteAttribute("max", max);
        }

        private void GetCurveRange(ParticleSystem.MinMaxCurve curve, out float min, out float max)
        {
            min = max = 10;
            switch (curve.mode)
            {
                case ParticleSystemCurveMode.Constant:
                    min = max = curve.constant;
                    break;
                case ParticleSystemCurveMode.TwoConstants:
                    min = curve.constantMin;
                    max = curve.constantMax;
                    break;
                case ParticleSystemCurveMode.Curve:
                    min = GetMinValue(curve.curve);
                    max = GetMaxValue(curve.curve);
                    break;
                case ParticleSystemCurveMode.TwoCurves:
                    min = GetMinValue(curve.curveMin);
                    max = GetMaxValue(curve.curveMax);
                    break;
            }
        }

        private float GetMinValue(AnimationCurve curveCurve)
        {
            var min = float.MaxValue;
            foreach (var frame in curveCurve.keys)
            {
                var v = frame.value;
                if (v < min)
                    min = v;
            }

            return min;
        }

        private float GetMaxValue(AnimationCurve curveCurve)
        {
            var max = float.MinValue;
            foreach (var frame in curveCurve.keys)
            {
                var v = frame.value;
                if (v > max)
                    max = v;
            }

            return max;
        }

        private void ExportNumParticles(ParticleSystem particleSystem, XmlWriter writer)
        {
            writer.WriteStartElement("numparticles");
            writer.WriteAttribute("value", particleSystem.main.maxParticles);
            writer.WriteEndElement();
            writer.WriteWhitespace(Environment.NewLine);
        }

        private void ExportEmissionRate(ParticleSystem particleSystem, XmlWriter writer)
        {
            var rateOverTime = particleSystem.emission.rateOverTime;
            writer.WriteStartElement("emissionrate");
            WriteMinMax(writer, rateOverTime);
            writer.WriteEndElement();
            writer.WriteWhitespace(Environment.NewLine);
        }
    }
}