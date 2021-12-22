using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

namespace UnityToCustomEngineExporter.Editor.Urho3D.Graph
{
    public class ParticleGraphBuilder
    {
        private readonly Graph _graph;

        public ParticleGraphBuilder(Graph mGraph)
        {
            _graph = mGraph;
        }

        private Dictionary<float, GraphNode> _constants = new Dictionary<float, GraphNode>();

        public GraphNode BuildConstant(float constant)
        {
            if (_constants.TryGetValue(constant, out var c))
                return c;
            c = _graph.Add(new GraphNode(GraphNodeType.Constant, new GraphNodeProperty("Value", constant),
                new GraphOutPin("out", VariantType.Float)));
            _constants.Add(constant, c);
            return c;
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

        private GraphNode BuildCurve(AnimationCurve curve, float multiplier, Func<GraphNode> t)
        {
            return Build(GraphNodeType.Curve,
                new GraphNodeProperty("Curve", curve, multiplier),
                new GraphInPin("t", VariantType.Float, t()),
                new GraphOutPin("out", VariantType.Float));
        }


        public GraphNode BuildBurst(ParticleSystem.Burst burst, Func<GraphNode> t, Func<GraphNode> factor)
        {
            var count = BuildMinMaxCurve(burst.count, 1.0f, t, factor);
            return _graph.Add(new GraphNode(GraphNodeType.BurstTimer,
                new GraphNodeProperty("Delay", burst.time),
                new GraphNodeProperty("Interval", burst.repeatInterval),
                new GraphNodeProperty("Cycles", burst.cycleCount),
                new GraphInPin("count", VariantType.Float, count),
                new GraphOutPin("out", VariantType.Float)));
        }

        public GraphNode Build(string name, params IGraphElement[] pins)
        {
            return _graph.Add(new GraphNode(name, pins));
        }

        public GraphNode Add(GraphNode node)
        {
            _graph.Add(node);
            return node;
        }
    }
}