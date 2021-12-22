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
        private readonly Graph _emit = new Graph();
        private readonly Graph _init = new Graph();
        private readonly Graph _update = new Graph();
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
            new ParticleGraphInitUpdateBuilder(_engine, _prefabContext, _init, _update).Build(particleSystem);
        }

        private void BuildEmit(ParticleSystem particleSystem)
        {
            new ParticleGraphEmitBuilder(_engine, _prefabContext, _emit).Build(particleSystem);

        }

        public Graph Emit => _emit;

        public Graph Init => _init;
        
        public Graph Update => _update;

        public void Write(XmlWriter writer)
        {
            writer.WriteWhitespace(Environment.NewLine);
            writer.WriteStartElement("layer");
            writer.WriteAttributeString("type", "ParticleGraphLayer");
            writer.WriteAttributeString("capacity", _capacity.ToString(CultureInfo.InvariantCulture));

            writer.WriteWhitespace(Environment.NewLine);
            writer.WriteStartElement("emit");
            _emit.Write(writer);
            writer.WriteWhitespace(Environment.NewLine);
            writer.WriteEndElement();

            writer.WriteWhitespace(Environment.NewLine);
            writer.WriteStartElement("init");
            _init.Write(writer);
            writer.WriteWhitespace(Environment.NewLine);
            writer.WriteEndElement();

            writer.WriteWhitespace(Environment.NewLine);
            writer.WriteStartElement("update");
            _update.Write(writer);
            writer.WriteWhitespace(Environment.NewLine);
            writer.WriteEndElement();

            writer.WriteWhitespace(Environment.NewLine);
            writer.WriteEndElement();
        }
    }
}