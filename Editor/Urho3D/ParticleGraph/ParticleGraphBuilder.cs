using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using UnityEngine;
using UnityToCustomEngineExporter.Editor.Urho3D.Graph;
using UnityToCustomEngineExporter.Editor.Urho3D.ParticleGraph.ParticleNodes;

namespace UnityToCustomEngineExporter.Editor.Urho3D.ParticleGraph
{
    public class ParticleGraphBuilder
    {
        private readonly GraphResource _graph;

        private readonly Dictionary<Type, AbstractConstCollection> _constCollections = new Dictionary<Type, AbstractConstCollection>();

        public ParticleGraphBuilder(GraphResource mGraph)
        {
            _graph = mGraph;
        }

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

        public GraphNode BuildMinMaxCurve(ParticleSystem.MinMaxCurve curve, Func<GraphNode> t, Func<GraphNode> factor,
            float scale = 1.0f)
        {
            switch (curve.mode)
            {
                case ParticleSystemCurveMode.Constant:
                    var val = curve.constant * scale;
                    //Expression<Func<float>> expression = () => val;
                    return BuildConstant(val);
                case ParticleSystemCurveMode.Curve:
                {
                    return BuildCurve(curve.curve, curve.curveMultiplier * scale, t);
                }
                case ParticleSystemCurveMode.TwoCurves:
                {
                    var min = BuildCurve(curve.curveMin, curve.curveMultiplier * scale, t);
                    var max = BuildCurve(curve.curveMax, curve.curveMultiplier * scale, t);
                    return Add(new Lerp(min, max, factor()));
                }
                case ParticleSystemCurveMode.TwoConstants:
                {
                    if (curve.constantMin == curve.constantMax)
                        return BuildConstant(curve.constantMin * scale);
                    var f = factor();
                    var min = curve.constantMin * scale;
                    var max = curve.constantMax * scale;
                    if (min == 0.0f && Math.Abs(max - 1.0f) < 1e-6f) return f;
                    return Add(new Lerp(BuildConstant(min), BuildConstant(max), f));
                }
            }

            throw new ArgumentOutOfRangeException(curve.mode.ToString());
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

        public T Add<T>(T node) where T : GraphNode
        {
            _graph.Add(node);
            return node;
        }

        public GraphOutPin Visit(Expression expression, params GraphOutPin[] args)
        {
            var visitor = new GraphExpressionVisitor(this, args);
            return visitor.Visit(expression);
        }

        public GraphOutPin Visit<TResult>(Expression<Func<TResult>> expression, params GraphOutPin[] args)
        {
            var visitor = new GraphExpressionVisitor(this, args);
            return visitor.Visit(expression);
        }

        public GraphOutPin Visit<T, TResult>(Expression<Func<T, TResult>> expression, params GraphOutPin[] args)
        {
            var visitor = new GraphExpressionVisitor(this, args);
            return visitor.Visit(expression);
        }

        public GraphOutPin Visit<T1, T2, TResult>(Expression<Func<T1, T2, TResult>> expression,
            params GraphOutPin[] args)
        {
            var visitor = new GraphExpressionVisitor(this, args);
            return visitor.Visit(expression);
        }

        public GraphOutPin Visit<T1, T2, T3, TResult>(Expression<Func<T1, T2, T3, TResult>> expression,
            params GraphOutPin[] args)
        {
            var visitor = new GraphExpressionVisitor(this, args);
            return visitor.Visit(expression);
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

        private abstract class AbstractConstCollection
        {
            protected readonly ParticleGraphBuilder _builder;

            public AbstractConstCollection(ParticleGraphBuilder builder)
            {
                _builder = builder;
            }

            public abstract GraphNode Get(object constant);
        }

        private class ConstCollection<T> : AbstractConstCollection
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
    }
}