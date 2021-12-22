using System;
using System.Collections.Generic;
using System.Globalization;
using System.Xml;
using Assets.Unity2Urho.Editor.Urho3D.Graph.ParticleNodes;
using UnityEditor;
using UnityEngine;

namespace UnityToCustomEngineExporter.Editor.Urho3D.Graph
{
    public class ParticleGraphLayer
    {
        private readonly Urho3DEngine _engine;
        private readonly PrefabContext _prefabContext;
        private readonly Graph m_emit = new Graph();
        private readonly Graph m_init = new Graph();
        private readonly Graph m_update = new Graph();
        private int _capacity;


        public ParticleGraphLayer(Urho3DEngine engine, PrefabContext prefabContext, ParticleSystem particleSystem)
        {
            _engine = engine;
            _prefabContext = prefabContext;
            BuildEmit(particleSystem);
            BuildUpdate(particleSystem);
        }

        private void BuildUpdate(ParticleSystem particleSystem)
        {
            _capacity = particleSystem.main.maxParticles;
            var init = new ParticleGraphBuilder(m_init);
            var update = new ParticleGraphBuilder(m_update);
            VariantType sizeType;
            //if (particleSystem.main.startSize3D)
            //{
            //    var startSize = init.BuildMinMaxCurve(particleSystem.main.startSizeX, particleSystem.main.startSizeY, particleSystem.main.startSizeZ);

            //}
            //else
            {
                var startSize = init.BuildMinMaxCurve(particleSystem.main.startSize, particleSystem.main.startSizeMultiplier);
                init.Build(GraphNodeType.SetAttribute, new GraphInPin("", VariantType.Float, startSize),
                    new GraphOutPin("size", VariantType.Float));
                sizeType = VariantType.Float;
            }

            var updateSize = update.Build(GraphNodeType.GetAttribute,
                new GraphOutPin("size", sizeType));

            if (particleSystem.sizeOverLifetime.enabled)
            {
                var size = particleSystem.sizeOverLifetime;
                if (!size.separateAxes)
                {
                    var sizeScale = update.BuildMinMaxCurve(size.size, size.sizeMultiplier);
                    updateSize = update.Add(new Multiply(updateSize, sizeScale));
                }
            }

            var renderer = particleSystem.GetComponent<Renderer>();
            if (renderer is ParticleSystemRenderer particleSystemRenderer)
            {
                var render = new RenderBillboard();
                if (sizeType == VariantType.Float)
                {
                    render.Size.Connect(update.Add(new MakeVec2(updateSize, updateSize)));
                }
                render.Material = "Material;" + _engine.EvaluateMaterialName(renderer.sharedMaterial, _prefabContext);
                _engine.ScheduleAssetExport(renderer.sharedMaterial, _prefabContext);
                update.Add(render);
            }
        }

        private void BuildEmit(ParticleSystem particleSystem)
        {
            GraphNode lastSum = null;
            var emit = new ParticleGraphBuilder(m_emit);
            if (particleSystem.emission.rateOverTime.mode != ParticleSystemCurveMode.Constant ||
                particleSystem.emission.rateOverTime.constant > 0)
            {
                var rate = emit.BuildMinMaxCurve(particleSystem.emission.rateOverTime, particleSystem.emission.rateOverTimeMultiplier);
                lastSum = emit.Build(GraphNodeType.TimeStepScale, new GraphInPin("x", rate), new GraphOutPin("out"));
            }

            for (int i = 0; i < particleSystem.emission.burstCount; ++i)
            {
                var b = emit.BuildBurst(particleSystem.emission.GetBurst(i));
                if (lastSum != null)
                {
                    lastSum = emit.Add(new Add(lastSum, b));
                }
                else
                {
                    lastSum = b;
                }
            }

            if (lastSum != null)
            {
                emit.Add(new GraphNode(GraphNodeType.Emit, new GraphInPin("count", lastSum)));
            }
        }

        public Graph Emit => m_emit;

        public Graph Init => m_init;
        
        public Graph Update => m_update;

        public void Write(XmlWriter writer)
        {
            writer.WriteWhitespace(Environment.NewLine);
            writer.WriteStartElement("layer");
            writer.WriteAttributeString("type", "ParticleGraphLayer");
            writer.WriteAttributeString("capacity", _capacity.ToString(CultureInfo.InvariantCulture));

            writer.WriteWhitespace(Environment.NewLine);
            writer.WriteStartElement("emit");
            m_emit.Write(writer);
            writer.WriteWhitespace(Environment.NewLine);
            writer.WriteEndElement();

            writer.WriteWhitespace(Environment.NewLine);
            writer.WriteStartElement("init");
            m_init.Write(writer);
            writer.WriteWhitespace(Environment.NewLine);
            writer.WriteEndElement();

            writer.WriteWhitespace(Environment.NewLine);
            writer.WriteStartElement("update");
            m_update.Write(writer);
            writer.WriteWhitespace(Environment.NewLine);
            writer.WriteEndElement();

            writer.WriteWhitespace(Environment.NewLine);
            writer.WriteEndElement();
        }
    }
}