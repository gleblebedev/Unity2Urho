using System;
using System.Collections.Generic;
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


        public ParticleGraphLayer(Urho3DEngine engine, PrefabContext prefabContext, ParticleSystem particleSystem)
        {
            _engine = engine;
            _prefabContext = prefabContext;
            BuildEmit(particleSystem);
            BuildUpdate(particleSystem);
        }

        private void BuildUpdate(ParticleSystem particleSystem)
        {
            var update = new ParticleGraphBuilder(m_update);

            if (particleSystem.sizeOverLifetime.enabled)
            {
                var size = particleSystem.sizeOverLifetime;
                if (!size.separateAxes)
                {
                    update.BuildMinMaxCurve(size.size);
                }
            }

            var renderer = particleSystem.GetComponent<Renderer>();
            if (renderer is ParticleSystemRenderer particleSystemRenderer)
            {
                var render = new RenderBillboard();
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
                var rate = emit.BuildMinMaxCurve(particleSystem.emission.rateOverTime);
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
        }
    }
}