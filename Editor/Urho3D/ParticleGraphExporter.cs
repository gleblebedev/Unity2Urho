using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityToCustomEngineExporter.Editor.Urho3D.Graph;

namespace UnityToCustomEngineExporter.Editor.Urho3D
{
    public class ParticleGraphExporter
    {
        private Urho3DEngine _engine;

        public ParticleGraphExporter(Urho3DEngine engine)
        {
            _engine = engine;
        }

        public string EvaluateName(ParticleSystem particleSystem, PrefabContext context)
        {
            if (particleSystem == null)
                return null;

            return ExportUtils.Combine(context.TempFolder, particleSystem.name + ".xml");
        }

        public void ExportEffect(IEnumerable<ParticleSystem> particleSystems, PrefabContext prefabContext)
        {
            if (!_engine.Options.ExportParticles)
                return;

            var effect = new ParticleGraphEffect();
            foreach (var particleSystem in particleSystems)
            {
                effect.Layers.Add(new ParticleGraphLayer(_engine, prefabContext, particleSystem));
            }

            var key = ExportUtils.GetKey(particleSystems.First());
            var name = EvaluateName(particleSystems.First(), prefabContext);
            using (var writer = _engine.TryCreateXml(key, name, DateTime.UtcNow))
            {
                if (writer == null)
                    return;
                effect.Write(writer);
            }
        }

        public void ExportEffect(ParticleSystem particleSystems, PrefabContext prefabContext)
        {
            ExportEffect(new[]{ particleSystems }, prefabContext);
        }
    }
}