using UnityEngine;

using UnityToCustomEngineExporter.Editor.Urho3D.Graph;

namespace UnityToCustomEngineExporter.Editor.Urho3D.ParticleGraph.ParticleNodes
{
    public partial class Length : GraphNode
    {
        public Length() : base("Length")
        {
            base.In.Add(X);
            base.Out.Add(Out);
        }

        public Length(GraphNode x) : this()
        {
            X.Connect(x);
        }

        public Length(GraphOutPin x) : this()
        {
            X.TargetPin = x;
        }

        public GraphInPin X { get; } = new GraphInPin("x", VariantType.None);

        public GraphOutPin Out { get; } = new GraphOutPin("out", VariantType.None);
    }
}