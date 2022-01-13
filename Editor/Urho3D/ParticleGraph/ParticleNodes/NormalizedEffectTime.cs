using UnityToCustomEngineExporter.Editor.Urho3D.Graph;

namespace UnityToCustomEngineExporter.Editor.Urho3D.ParticleGraph.ParticleNodes
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