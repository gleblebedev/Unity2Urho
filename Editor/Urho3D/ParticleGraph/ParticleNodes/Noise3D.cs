using UnityEngine;
using UnityToCustomEngineExporter.Editor.Urho3D.Graph;

namespace UnityToCustomEngineExporter.Editor.Urho3D.ParticleGraph.ParticleNodes
{
    public partial class Noise3D : GraphNode
    {
        public Noise3D() : base("Noise3D")
        {
            base.In.Add(X);
            base.Out.Add(Out);
        }

        public Noise3D(GraphNode x): this()
        {
            X.Connect(x);
        }

        public Noise3D(GraphOutPin x): this()
        {
            X.TargetPin = x;
        }

        public GraphInPin X { get; } = new GraphInPin("x", VariantType.Vector3);

        public GraphOutPin Out { get; } = new GraphOutPin("out", VariantType.Float);
    }
}