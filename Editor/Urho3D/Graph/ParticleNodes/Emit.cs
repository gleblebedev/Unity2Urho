namespace UnityToCustomEngineExporter.Editor.Urho3D.Graph.ParticleNodes
{
    public partial class Emit : GraphNode
    {
        public Emit() : base("Emit")
        {
            base.In.Add(Count);
        }

        public Emit(GraphNode count): this()
        {
            Count.Connect(count);
        }

        public Emit(GraphOutPin count): this()
        {
            Count.TargetPin = count;
        }

        public GraphInPin Count { get; } = new GraphInPin("count", VariantType.Float);
    }
}