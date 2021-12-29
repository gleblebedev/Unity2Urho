namespace UnityToCustomEngineExporter.Editor.Urho3D.Graph.ParticleNodes
{
    public partial class RenderMesh : GraphNode
    {
        private readonly GraphNodeProperty<ResourceRef> _model = new GraphNodeProperty<ResourceRef>("Model");

        private readonly GraphNodeProperty<ResourceRefList> _material = new GraphNodeProperty<ResourceRefList>("Material");

        public RenderMesh() : base("RenderMesh")
        {
            base.In.Add(Transform);
            base.Properties.Add(_model);
            base.Properties.Add(_material);
        }

        public RenderMesh(GraphNode transform) : this()
        {
            Transform.Connect(transform);
        }

        public RenderMesh(GraphOutPin transform) : this()
        {
            Transform.TargetPin = transform;
        }

        public ResourceRef Model
        {
            get => _model.Value;
            set => _model.Value = value;
        }

        public ResourceRefList Material
        {
            get => _material.Value;
            set => _material.Value = value;
        }

        public GraphInPin Transform { get; } = new GraphInPin("transform", VariantType.Matrix3x4);
    }
}