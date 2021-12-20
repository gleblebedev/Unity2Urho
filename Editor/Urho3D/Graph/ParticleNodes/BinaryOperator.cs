using UnityToCustomEngineExporter.Editor.Urho3D.Graph;

namespace Assets.Unity2Urho.Editor.Urho3D.Graph.ParticleNodes
{

    public class BinaryOperator : GraphNode
    {
        public BinaryOperator(string name) : base(name)
        {
            In.Add(X);
            In.Add(Y);
            base.Out.Add(Result);
        }

        public GraphInPin X => new GraphInPin("x");
        public GraphInPin Y => new GraphInPin("y");
        public GraphOutPin Result => new GraphOutPin("out");
    }
}