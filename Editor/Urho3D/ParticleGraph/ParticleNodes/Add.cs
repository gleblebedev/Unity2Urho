using UnityToCustomEngineExporter.Editor.Urho3D.Graph;

namespace UnityToCustomEngineExporter.Editor.Urho3D.ParticleGraph.ParticleNodes
{
    public partial class Add : GraphNode
    {
        public Add() : base("Add")
        {
            base.In.Add(X);
            base.In.Add(Y);
            base.Out.Add(Out);
        }

        public Add(GraphNode x, GraphNode y): this()
        {
            X.Connect(x);
            Y.Connect(y);
        }

        public Add(GraphOutPin x, GraphOutPin y): this()
        {
            X.TargetPin = x;
            Y.TargetPin = y;
        }

        public GraphInPin X { get; } = new GraphInPin("x", VariantType.None);

        public GraphInPin Y { get; } = new GraphInPin("y", VariantType.None);

        public new GraphOutPin Out { get; } = new GraphOutPin("out", VariantType.None);
    }
}