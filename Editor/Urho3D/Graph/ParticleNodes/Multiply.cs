using UnityToCustomEngineExporter.Editor.Urho3D.Graph;

namespace UnityToCustomEngineExporter.Editor.Urho3D.Graph.ParticleNodes
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
        public Multiply(GraphOutPin x, GraphOutPin y) : this()
        {
            X.TargetPin = x;
            Y.TargetPin = y;
        }
    }
}