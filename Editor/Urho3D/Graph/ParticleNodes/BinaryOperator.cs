namespace UnityToCustomEngineExporter.Editor.Urho3D.Graph.ParticleNodes
{
    public class BinaryOperator : GraphNode
    {
        public BinaryOperator(string name) : base(name)
        {
            In.Add(X);
            In.Add(Y);
            Out.Add(Result);
        }

        public GraphInPin X { get; } = new("x");
        public GraphInPin Y { get; } = new("y");
        public GraphOutPin Result { get; } = new("out");
    }
}