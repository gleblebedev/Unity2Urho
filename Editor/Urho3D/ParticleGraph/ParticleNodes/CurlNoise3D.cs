using UnityEngine;
using UnityToCustomEngineExporter.Editor.Urho3D.Graph;

namespace UnityToCustomEngineExporter.Editor.Urho3D.ParticleGraph.ParticleNodes
{
    public partial class CurlNoise3D : GraphNode
    {
        public CurlNoise3D() : base("CurlNoise3D")
        {
            base.In.Add(X);
            base.Out.Add(Out);
        }

        public CurlNoise3D(GraphNode x): this()
        {
            X.Connect(x);
        }

        public CurlNoise3D(GraphOutPin x): this()
        {
            X.TargetPin = x;
        }

        public GraphInPin X { get; } = new GraphInPin("x", VariantType.Vector3);

        public GraphOutPin Out { get; } = new GraphOutPin("out", VariantType.Vector3);
    }
}