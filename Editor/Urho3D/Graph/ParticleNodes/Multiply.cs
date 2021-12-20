using UnityToCustomEngineExporter.Editor.Urho3D.Graph;

namespace Assets.Unity2Urho.Editor.Urho3D.Graph.ParticleNodes
{

    public class Multiply : BinaryOperator
    {
        public Multiply() : base("Multiply")
        {
        }

        public Multiply(GraphNode x, GraphNode y) : this()
        {
            X.Connect(x);
            Y.Connect(y);
        }
    }
}