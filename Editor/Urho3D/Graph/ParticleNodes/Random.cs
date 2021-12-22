using UnityToCustomEngineExporter.Editor.Urho3D.Graph;

namespace Assets.Unity2Urho.Editor.Urho3D.Graph.ParticleNodes
{
    public class Random : GraphNode
    {
        private readonly GraphNodeProperty _min;
        private readonly GraphNodeProperty _max;

        public Random(VariantType type = VariantType.Float) : base(GraphNodeType.Random)
        {
            Result = new GraphOutPin("out", type);
            Properties.Add(_min = new GraphNodeProperty("Min", type) {Value = "0"});
            Properties.Add(_max = new GraphNodeProperty("Max", type) { Value = "1" });
            Out.Add(Result);
        }

        public GraphOutPin Result { get; }
    }
}