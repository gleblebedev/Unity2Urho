using UnityEngine;

using UnityToCustomEngineExporter.Editor.Urho3D.Graph;

namespace UnityToCustomEngineExporter.Editor.Urho3D.ParticleGraph.ParticleNodes
{
    public partial class RenderMesh : GraphNode
    {
        private readonly GraphNodeProperty<ResourceRef> _model = new GraphNodeProperty<ResourceRef>("Model");

        private readonly GraphNodeProperty<ResourceRefList> _material = new GraphNodeProperty<ResourceRefList>("Material");

        private readonly GraphNodeProperty<bool> _isWorldspace = new GraphNodeProperty<bool>("Is Worldspace");

        public RenderMesh() : base("RenderMesh")
        {
            base.In.Add(Transform);
            base.Properties.Add(_model);
            base.Properties.Add(_material);
            base.Properties.Add(_isWorldspace);
        }

        public RenderMesh(GraphNode transform): this()
        {
            Transform.Connect(transform);
        }

        public RenderMesh(GraphOutPin transform): this()
        {
            Transform.TargetPin = transform;
        }

        public ResourceRef Model {
            get => _model.Value;
            set => _model.Value = value;
        }

        public ResourceRefList Material {
            get => _material.Value;
            set => _material.Value = value;
        }

        public bool IsWorldspace {
            get => _isWorldspace.Value;
            set => _isWorldspace.Value = value;
        }

        public GraphInPin Transform { get; } = new GraphInPin("transform", VariantType.Matrix3x4);
    }
}