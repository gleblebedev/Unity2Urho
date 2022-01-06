using UnityEngine;

namespace UnityToCustomEngineExporter.Editor.Urho3D.Graph.ParticleNodes
{
    public partial class Normalized : GraphNode
    {
        public Normalized() : base("Normalized")
        {
            base.In.Add(X);
            base.Out.Add(Out);
        }

        public Normalized(GraphNode x): this()
        {
            X.Connect(x);
        }

        public Normalized(GraphOutPin x): this()
        {
            X.TargetPin = x;
        }

        public GraphInPin X { get; } = new GraphInPin("x", VariantType.None);

        public GraphOutPin Out { get; } = new GraphOutPin("out", VariantType.None);
    }
}