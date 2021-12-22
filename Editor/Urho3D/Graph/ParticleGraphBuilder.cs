using System;
using System.Collections.Generic;
using UnityEngine;

namespace UnityToCustomEngineExporter.Editor.Urho3D.Graph
{
    public class ParticleGraphBuilder
    {
        private readonly Graph m_graph;

        public ParticleGraphBuilder(Graph mGraph)
        {
            m_graph = mGraph;
        }

        private Dictionary<float, GraphNode> m_constants = new Dictionary<float, GraphNode>();
        public GraphNode BuildConstant(float constant)
        {
            if (m_constants.TryGetValue(constant, out var c))
                return c;
            c = m_graph.Add(new GraphNode(GraphNodeType.Constant, new GraphNodeProperty("Value", constant),
                new GraphOutPin("out", VariantType.Float)));
            m_constants.Add(constant, c);
            return c;
        }

        public GraphNode BuildMinMaxCurve(ParticleSystem.MinMaxCurve curve, float multiplier)
        {
            switch (curve.mode)
            {
                case ParticleSystemCurveMode.Constant:
                    return BuildConstant(curve.constant);
                case ParticleSystemCurveMode.Curve:
                    return Build(GraphNodeType.Curve,
                        new GraphNodeProperty("Curve", curve.curve, curve.curveMultiplier* multiplier),
                        new GraphInPin("t", VariantType.Float, BuildConstant(0.0f)),
                        new GraphOutPin("out", VariantType.Float));
                case ParticleSystemCurveMode.TwoCurves:
                    return Build(GraphNodeType.LerpCurves,
                        new GraphNodeProperty("CurveMin", curve.curveMin, curve.curveMultiplier * multiplier),
                        new GraphNodeProperty("CurveMax", curve.curveMax, curve.curveMultiplier * multiplier),
                        new GraphInPin("factor", VariantType.Float, BuildConstant(0.0f)),
                        new GraphInPin("t", VariantType.Float, BuildConstant(0.0f)),
                        new GraphOutPin("out", VariantType.Float));
                case ParticleSystemCurveMode.TwoConstants:
                    return Build(GraphNodeType.Random, 
                        new GraphNodeProperty("Min", curve.constantMin),
                        new GraphNodeProperty("Max", curve.constantMax),
                        new GraphOutPin("out", VariantType.Float));
            }

            throw new ArgumentOutOfRangeException(curve.mode.ToString());
        }


        public GraphNode BuildBurst(ParticleSystem.Burst burst)
        {
            var count = BuildMinMaxCurve(burst.count, 1.0f);
            return m_graph.Add(new GraphNode(GraphNodeType.BurstTimer,
                new GraphNodeProperty("Delay", burst.time),
                new GraphNodeProperty("Interval", burst.repeatInterval),
                new GraphNodeProperty("Cycles", burst.cycleCount),
                new GraphInPin("count", VariantType.Float, count),
                new GraphOutPin("out", VariantType.Float)));
        }

        public GraphNode Build(string name, params IGraphElement[] pins)
        {
            return m_graph.Add(new GraphNode(name, pins));
        }

        public GraphNode Add(GraphNode node)
        {
            m_graph.Add(node);
            return node;
        }
    }
}