namespace UnityToCustomEngineExporter.Editor.Urho3D.Graph.ParticleNodes
{
    public partial class NormalizedEffectTime : GraphNode
    {
        public NormalizedEffectTime() : base("NormalizedEffectTime")
        {
            base.Out.Add(Out);
        }

        public GraphOutPin Out { get; } = new GraphOutPin("out", VariantType.Float);
    }
}