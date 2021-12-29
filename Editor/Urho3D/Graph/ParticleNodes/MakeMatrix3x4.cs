namespace UnityToCustomEngineExporter.Editor.Urho3D.Graph.ParticleNodes
{
    public partial class MakeMatrix3x4 : GraphNode
    {
        public MakeMatrix3x4() : base("MakeMatrix3x4")
        {
            base.In.Add(Translation);
            base.In.Add(Rotation);
            base.In.Add(Scale);
            base.Out.Add(Out);
        }

        public MakeMatrix3x4(GraphNode translation, GraphNode rotation, GraphNode scale) : this()
        {
            Translation.Connect(translation);
            Rotation.Connect(rotation);
            Scale.Connect(scale);
        }

        public MakeMatrix3x4(GraphOutPin translation, GraphOutPin rotation, GraphOutPin scale) : this()
        {
            Translation.TargetPin = translation;
            Rotation.TargetPin = rotation;
            Scale.TargetPin = scale;
        }

        public GraphInPin Translation { get; } = new GraphInPin("translation", VariantType.Vector3);

        public GraphInPin Rotation { get; } = new GraphInPin("rotation", VariantType.Quaternion);

        public GraphInPin Scale { get; } = new GraphInPin("scale", VariantType.Vector3);

        public GraphOutPin Out { get; } = new GraphOutPin("out", VariantType.Matrix3x4);
    }
}