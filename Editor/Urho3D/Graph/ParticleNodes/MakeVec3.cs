using UnityToCustomEngineExporter.Editor.Urho3D.Graph;

namespace Assets.Unity2Urho.Editor.Urho3D.Graph.ParticleNodes
{
    public class MakeVec3 : GraphNode
    {
        public MakeVec3() : base(GraphNodeType.MakeVec3)
        {
            In.Add(X);
            In.Add(Y);
            In.Add(Z);
            base.Out.Add(Result);
        }
        public MakeVec3(GraphNode x, GraphNode y, GraphNode z) : this()
        {
            X.Connect(x);
            Y.Connect(y);
            Z.Connect(z);
        }
        public GraphInPin X { get; } = new GraphInPin("x", VariantType.Float);
        public GraphInPin Y { get; } = new GraphInPin("y", VariantType.Float);
        public GraphInPin Z { get; } = new GraphInPin("z", VariantType.Float);
        public GraphOutPin Result { get; } = new GraphOutPin("out", VariantType.Vector3);
    }
}