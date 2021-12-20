using UnityToCustomEngineExporter.Editor.Urho3D.Graph;

namespace Assets.Unity2Urho.Editor.Urho3D.Graph.ParticleNodes
{

    public class Divide : BinaryOperator
    {
        public Divide() : base("Divide")
        {
        }

        public Divide(GraphNode x, GraphNode y) : this()
        {
            X.Connect(x);
            Y.Connect(y);
        }
    }
}