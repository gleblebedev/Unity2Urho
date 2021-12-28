namespace UnityToCustomEngineExporter.Editor.Urho3D.Graph.ParticleNodes
{
    public partial class Add : GraphNode
    {
        public Add() : base("Add")
        {
            In.Add(X);
            In.Add(Y);
            base.Out.Add(Out);
        }

        public Add(GraphNode x, GraphNode y) : this()
        {
            X.Connect(x);
            Y.Connect(y);
        }

        public Add(GraphOutPin x, GraphOutPin y) : this()
        {
            X.TargetPin = x;
            Y.TargetPin = y;
        }

        public GraphInPin X { get; } = new GraphInPin("x");
        public GraphInPin Y { get; } = new GraphInPin("y");
        public GraphOutPin Out { get; } = new GraphOutPin("out");
    }
}