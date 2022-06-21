using UnityEngine;
using UnityEngine.Rendering;

namespace UnityToCustomEngineExporter.Editor.Urho3D
{
    [CustomUrho3DExporter(typeof(Material))]
    public class TreeCreatorMaterialExporter : StandardMaterialExporter, IUrho3DMaterialExporter
    {
        public TreeCreatorMaterialExporter(Urho3DEngine engine) : base(engine)
        {
        }

        public override int ExporterPriority => 10;

        public override bool CanExportMaterial(Material material)
        {
            return material.shader.name == "Hidden/Nature/Tree Creator Leaves Optimized";
        }

        protected override UrhoPBRMaterial FromMetallicGlossiness(Material mat,
            MetallicGlossinessShaderArguments arguments)
        {
            arguments.AlphaTest = true;

            var material = base.FromMetallicGlossiness(mat, arguments);

            return material;
        }
    }
}