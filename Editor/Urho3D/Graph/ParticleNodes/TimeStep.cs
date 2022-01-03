namespace UnityToCustomEngineExporter.Editor.Urho3D.Graph.ParticleNodes
{
    public partial class TimeStep : GraphNode
    {
        public TimeStep() : base("TimeStep")
        {
            base.Out.Add(Out);
        }

        public GraphOutPin Out { get; } = new GraphOutPin("out", VariantType.Float);
    }
}