﻿using UnityToCustomEngineExporter.Editor.Urho3D.ParticleGraph.ParticleNodes;
using UnityToCustomEngineExporter.Editor.Urho3D.Graph;
using UnityEngine;
using Random = UnityToCustomEngineExporter.Editor.Urho3D.ParticleGraph.ParticleNodes.Random;

namespace UnityToCustomEngineExporter.Editor.Urho3D.ParticleGraph
{
    public class ParticleGraphEmitBuilder
    {
        private readonly Urho3DEngine _engine;
        private readonly PrefabContext _prefabContext;
        private readonly ParticleGraphBuilder _emit;
        private GraphNode _random;
        private GraphNode _normalizedEffectTime;

        public ParticleGraphEmitBuilder(Urho3DEngine engine, PrefabContext prefabContext, GraphResource emit)
        {
            _engine = engine;
            _prefabContext = prefabContext;
            _emit = new ParticleGraphBuilder(emit);

        }

        public void Build(ParticleSystem particleSystem)
        {

            GraphNode lastSum = null;
            if (particleSystem.emission.rateOverTime.mode != ParticleSystemCurveMode.Constant ||
                particleSystem.emission.rateOverTime.constant > 0)
            {
                var rate = _emit.BuildMinMaxCurve(particleSystem.emission.rateOverTime, ParticleEffectTime, GetRandom);
                lastSum = _emit.Add(new TimeStepScale(rate));
            }

            for (int i = 0; i < particleSystem.emission.burstCount; ++i)
            {
                var b = _emit.BuildBurst(particleSystem.emission.GetBurst(i), ParticleEffectTime, GetRandom);
                if (lastSum != null)
                {
                    lastSum = _emit.Add(new Add(lastSum, b));
                }
                else
                {
                    lastSum = b;
                }
            }

            if (lastSum != null)
            {
                _emit.Add(new Emit(lastSum));
            }
        }

        private GraphNode GetRandom()
        {
            return _random??(_random = _emit.Add(new Random()));
        }

        private GraphNode ParticleEffectTime()
        {
            return _normalizedEffectTime ?? (_normalizedEffectTime = _emit.Add(new NormalizedEffectTime()));
        }
    }
}