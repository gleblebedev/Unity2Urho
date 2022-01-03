namespace UnityToCustomEngineExporter.Editor.Urho3D.Graph.ParticleNodes
{
    public partial class EffectTime : GraphNode
    {
        public EffectTime() : base("EffectTime")
        {
            base.Out.Add(Out);
        }

        public GraphOutPin Out { get; } = new GraphOutPin("out", VariantType.Float);
    }
}