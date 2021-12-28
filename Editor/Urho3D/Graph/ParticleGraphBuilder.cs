using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

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
                    var f = factor();
                    return Build(GraphNodeType.Lerp,
                        new GraphInPin("x", VariantType.Color) { Value = ValueFormatter<Color>.Default.ToString(curve.colorMin) },
                        new GraphInPin("y", VariantType.Color) { Value = ValueFormatter<Color>.Default.ToString(curve.colorMax) },
                        new GraphInPin("t", VariantType.Float, f),
                        new GraphOutPin("out", VariantType.Color));
                    }
                case ParticleSystemGradientMode.TwoGradients:
                    var min = BuildGradient(curve.gradientMin, t);
                    var max = BuildGradient(curve.gradientMax, t);
                    return Build(GraphNodeType.Lerp,
                        new GraphInPin("x", VariantType.Color, min),
                        new GraphInPin("y", VariantType.Color, max),
                        new GraphInPin("t", VariantType.Float, factor()),
                        new GraphOutPin("out", VariantType.Color));
                case ParticleSystemGradientMode.RandomColor:
                    //TODO: Generate color!
                    return BuildConstant(curve.color);
            }
            throw new ArgumentOutOfRangeException(curve.mode.ToString());
        }

        public GraphNode BuildMinMaxCurve(ParticleSystem.MinMaxCurve curve, float multiplier, Func<GraphNode> t, Func<GraphNode> factor)
        {
            switch (curve.mode)
            {
                case ParticleSystemCurveMode.Constant:
                    return BuildConstant(curve.constant);
                case ParticleSystemCurveMode.Curve:
                {
                    return BuildCurve(curve.curve, curve.curveMultiplier * multiplier, t);
                }
                case ParticleSystemCurveMode.TwoCurves:
                {
                    var min = BuildCurve(curve.curveMin, curve.curveMultiplier * multiplier, t);
                    var max = BuildCurve(curve.curveMax, curve.curveMultiplier * multiplier, t);
                    return Build(GraphNodeType.Lerp,
                        new GraphInPin("x", VariantType.Float, min),
                        new GraphInPin("y", VariantType.Float, max),
                        new GraphInPin("t", VariantType.Float, factor()),
                        new GraphOutPin("out", VariantType.Float));
                }
                case ParticleSystemCurveMode.TwoConstants:
                {
                    var f = factor();
                    if (curve.constantMin == 0.0f && Math.Abs(curve.constantMax - 1.0f) < 1e-6f)
                    {
                        return f;
                    }
                    return Build(GraphNodeType.Lerp,
                        new GraphInPin("x", VariantType.Float) {Value = curve.constantMin.ToString(CultureInfo.InvariantCulture) },
                        new GraphInPin("y", VariantType.Float) { Value = curve.constantMax.ToString(CultureInfo.InvariantCulture) },
                        new GraphInPin("t", VariantType.Float, f),
                        new GraphOutPin("out", VariantType.Float));
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
            var count = BuildMinMaxCurve(burst.count, 1.0f, t, factor);
            return _graph.Add(new GraphNode(GraphNodeType.BurstTimer,
                GraphNodeProperty.Make("Delay", burst.time),
                GraphNodeProperty.Make("Interval", burst.repeatInterval),
                GraphNodeProperty.Make("Cycles", burst.cycleCount),
                new GraphInPin("count", VariantType.Float, count),
                new GraphOutPin("out", VariantType.Float)));
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