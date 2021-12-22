using UnityToCustomEngineExporter.Editor.Urho3D.Graph;

namespace Assets.Unity2Urho.Editor.Urho3D.Graph.ParticleNodes
{
    public class MakeVec2 : GraphNode
    {
        public MakeVec2() : base(GraphNodeType.MakeVec2)
        {
            In.Add(X);
            In.Add(Y);
            base.Out.Add(Result);
        }
        public MakeVec2(GraphNode x, GraphNode y) : this()
        {
            X.Connect(x);
            Y.Connect(y);
        }
        public GraphInPin X { get; } = new GraphInPin("x", VariantType.Float);
        public GraphInPin Y { get; } = new GraphInPin("y", VariantType.Float);
        public GraphOutPin Result { get; } = new GraphOutPin("out", VariantType.Vector2);
    }
}