using UnityEngine;

namespace UnityToCustomEngineExporter.Editor.Urho3D
{
    [CustomUrho3DExporter(typeof(Material))]
    public class SyntyStudiosBloodExporter : StandardMaterialExporter, IUrho3DMaterialExporter
    {
        public SyntyStudiosBloodExporter(Urho3DEngine engine) : base(engine)
        {
        }

        protected override void ParseTexture(string propertyName, Texture texture, MetallicGlossinessShaderArguments arguments)
        {
            switch (propertyName)
            {
                case "_Texture":
                    arguments.BaseColor = texture;
                    return;
                case "_Emissive":
                    arguments.Emission = texture;
                    return;
            }

            base.ParseTexture(propertyName, texture, arguments);
        }

        public override bool CanExportMaterial(Material material)
        {
            return material.shader.name == "SyntyStudios/Blood";
        }
    }
}