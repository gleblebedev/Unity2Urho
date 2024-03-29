﻿using System;
using System.Globalization;
using System.Xml;
using UnityEngine;
using UnityToCustomEngineExporter.Editor.Urho3D.Graph;

namespace UnityToCustomEngineExporter.Editor.Urho3D.ParticleGraph
{
    public class ParticleGraphLayer
    {
        private readonly Urho3DEngine _engine;
        private readonly PrefabContext _prefabContext;
        private readonly GraphResource _emit = new GraphResource();
        private readonly GraphResource _init = new GraphResource();
        private readonly GraphResource _update = new GraphResource();
        private int _capacity;
        private float _timeScale;
        private float _duration;
        private bool _loop;

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
            _timeScale = particleSystem.main.simulationSpeed;
            _duration = particleSystem.main.duration;
            _loop = particleSystem.main.loop;
            new ParticleGraphInitUpdateBuilder(_engine, _prefabContext, _init, _update).Build(particleSystem);
        }

        private void BuildEmit(ParticleSystem particleSystem)
        {
            new ParticleGraphEmitBuilder(_engine, _prefabContext, _emit).Build(particleSystem);

        }

        public GraphResource Emit => _emit;

        public GraphResource Init => _init;
        
        public GraphResource Update => _update;

        public void Write(XmlWriter writer)
        {
            writer.WriteWhitespace(Environment.NewLine);
            writer.WriteStartElement("layer");
            writer.WriteAttributeString("type", "ParticleGraphLayer");
            writer.WriteAttributeString("capacity", _capacity.ToString(CultureInfo.InvariantCulture));
            writer.WriteAttributeString("timeScale", _timeScale.ToString(CultureInfo.InvariantCulture));
            writer.WriteAttributeString("duration", _duration.ToString(CultureInfo.InvariantCulture));
            writer.WriteAttributeString("loop", _loop.ToString(CultureInfo.InvariantCulture));

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