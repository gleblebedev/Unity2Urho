using UnityToCustomEngineExporter.Editor.Urho3D.Graph;

namespace UnityToCustomEngineExporter.Editor.Urho3D.ParticleGraph.ParticleNodes
{
    public partial class BurstTimer : GraphNode
    {
        private readonly GraphNodeProperty<float> _delay = new GraphNodeProperty<float>("Delay");

        private readonly GraphNodeProperty<float> _interval = new GraphNodeProperty<float>("Interval");

        private readonly GraphNodeProperty<int> _cycles = new GraphNodeProperty<int>("Cycles");

        public BurstTimer() : base("BurstTimer")
        {
            base.In.Add(Count);
            base.Out.Add(Out);
            base.Properties.Add(_delay);
            base.Properties.Add(_interval);
            base.Properties.Add(_cycles);
        }

        public BurstTimer(GraphNode count): this()
        {
            Count.Connect(count);
        }

        public BurstTimer(GraphOutPin count): this()
        {
            Count.TargetPin = count;
        }

        public float Delay {
            get => _delay.Value;
            set => _delay.Value = value;
        }

        public float Interval {
            get => _interval.Value;
            set => _interval.Value = value;
        }

        public int Cycles {
            get => _cycles.Value;
            set => _cycles.Value = value;
        }

        public GraphInPin Count { get; } = new GraphInPin("count", VariantType.Float);

        public GraphOutPin Out { get; } = new GraphOutPin("out", VariantType.Float);
    }
}