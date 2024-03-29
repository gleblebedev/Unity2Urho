﻿using UnityToCustomEngineExporter.Editor.Urho3D.Graph;

namespace UnityToCustomEngineExporter.Editor.Urho3D.ParticleGraph.ParticleNodes
{
    public class Random : GraphNode
    {
        private readonly GraphNodeProperty _min;
        private readonly GraphNodeProperty _max;

        public Random(VariantType type = VariantType.Float) : base(GraphNodeType.Random)
        {
            Result = new GraphOutPin("out", type);
            Properties.Add(_min = GraphNodeProperty.Make("Min", 0.0f));
            Properties.Add(_max = GraphNodeProperty.Make("Max", 1.0f));
            Out.Add(Result);
        }

        public GraphOutPin Result { get; }
    }
}