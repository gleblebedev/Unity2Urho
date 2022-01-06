using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using UnityToCustomEngineExporter.Editor.Urho3D.Graph.ParticleNodes;

namespace UnityToCustomEngineExporter.Editor.Urho3D.Graph
{
    public class ParticleGraphBuilder
    {
        private readonly Graph _graph;

        abstract class AbstractConstCollection
        {
            protected readonly ParticleGraphBuilder _builder;

            public AbstractConstCollection(ParticleGraphBuilder builder)
            {
                _builder = builder;
            }

            public abstract GraphNode Get(object constant);
        }
        class ConstCollection<T>: AbstractConstCollection
        {
            private readonly Dictionary<T, GraphNode> _constants = new Dictionary<T, GraphNode>();
            public ConstCollection(ParticleGraphBuilder builder) : base(builder)
            {
            }

            public override GraphNode Get(object constant)
            {
                var val = (T)constant;

                if (_constants.TryGetValue(val, out var c))
                    return c;
                var property = new GraphNodeProperty<T>("Value", val);
                c = _builder.Add(new GraphNode(GraphNodeType.Constant, property,
                    new GraphOutPin("out", property.Type)));
                _constants.Add(val, c);
                return c;

            }
        }

        public ParticleGraphBuilder(Graph mGraph)
        {
            _graph = mGraph;
        }

        private Dictionary<Type, AbstractConstCollection> _constCollections =
            new Dictionary<Type, AbstractConstCollection>();

        public GraphNode BuildConstant<T>(T constant)
        {
            if (!_constCollections.TryGetValue(typeof(T), out var collection))
            {
                collection = new ConstCollection<T>(this);
                _constCollections.Add(typeof(T), collection);
            }

            return collection.Get(constant);
        }

        public GraphNode BuildMinMaxCurve(ParticleSystem.MinMaxGradient curve, Func<GraphNode> t,
            Func<GraphNode> factor)
        {
            switch (curve.mode)
            {
                case ParticleSystemGradientMode.Color:
                    return BuildConstant(curve.color);
                case ParticleSystemGradientMode.Gradient:
                    return BuildGradient(curve.gradient, t);
                case ParticleSystemGradientMode.TwoColors:
                {
                    return Add(new Lerp(BuildConstant(curve.colorMin), BuildConstant(curve.colorMax), factor()));
                }
                case ParticleSystemGradientMode.TwoGradients:
                    var min = BuildGradient(curve.gradientMin, t);
                    var max = BuildGradient(curve.gradientMax, t);
                    return Add(new Lerp(min, max, factor()));
                case ParticleSystemGradientMode.RandomColor:
                    //TODO: Generate color!
                    return BuildConstant(curve.color);
            }
            throw new ArgumentOutOfRangeException(curve.mode.ToString());
        }

        public GraphNode BuildMinMaxCurve(ParticleSystem.MinMaxCurve curve, Func<GraphNode> t, Func<GraphNode> factor)
        {
            switch (curve.mode)
            {
                case ParticleSystemCurveMode.Constant:
                    return BuildConstant(curve.constant);
                case ParticleSystemCurveMode.Curve:
                {
                    return BuildCurve(curve.curve, curve.curveMultiplier, t);
                }
                case ParticleSystemCurveMode.TwoCurves:
                {
                    var min = BuildCurve(curve.curveMin, curve.curveMultiplier , t);
                    var max = BuildCurve(curve.curveMax, curve.curveMultiplier, t);
                    return Add(new Lerp(min, max, factor()));
                }
                case ParticleSystemCurveMode.TwoConstants:
                {
                    if (curve.constantMin == curve.constantMax)
                        return BuildConstant(curve.constantMin);
                    var f = factor();
                    if (curve.constantMin == 0.0f && Math.Abs(curve.constantMax - 1.0f) < 1e-6f)
                    {
                        return f;
                    }
                    return Add(new Lerp(BuildConstant(curve.constantMin), BuildConstant(curve.constantMax), f));
                }
            }

            throw new ArgumentOutOfRangeException(curve.mode.ToString());
        }
        
        private GraphNode BuildGradient(Gradient curve, Func<GraphNode> t)
        {
            return Build(GraphNodeType.Curve,
                new GraphNodeProperty<GraphCurve>("Curve", new GraphCurve(curve)),
                new GraphInPin("t", VariantType.Float, t()),
                new GraphOutPin("out", VariantType.Color));
        }

        private GraphNode BuildCurve(AnimationCurve curve, float multiplier, Func<GraphNode> t)
        {
            return Build(GraphNodeType.Curve,
                new GraphNodeProperty<GraphCurve>("Curve", new GraphCurve(curve, multiplier)),
                new GraphInPin("t", VariantType.Float, t()),
                new GraphOutPin("out", VariantType.Float));
        }


        public GraphNode BuildBurst(ParticleSystem.Burst burst, Func<GraphNode> t, Func<GraphNode> factor)
        {
            var count = BuildMinMaxCurve(burst.count, t, factor);
            return _graph.Add(
                new BurstTimer(count)
                {
                    Cycles = burst.cycleCount,
                    Delay = burst.time,
                    Interval = burst.repeatInterval
                });
        }

        public GraphNode Build(string name, params IGraphElement[] pins)
        {
            return _graph.Add(new GraphNode(name, pins));
        }

        public T Add<T>(T node) where T: GraphNode
        {
            _graph.Add(node);
            return node;
        }
    }
}