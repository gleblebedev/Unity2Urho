using UnityToCustomEngineExporter.Editor.Urho3D.Graph;

namespace Assets.Unity2Urho.Editor.Urho3D.Graph.ParticleNodes
{

    public class Subtract : BinaryOperator
    {
        public Subtract() : base("Subtract")
        {
        }

        public Subtract(GraphNode x, GraphNode y) : this()
        {
            X.Connect(x);
            Y.Connect(y);
        }
    }
}