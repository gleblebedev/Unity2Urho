using UnityEngine;

namespace UnityToCustomEngineExporter.Editor.Urho3D
{
    [CustomUrho3DExporter(typeof(Material))]
    public class HDRPDecalMaterialExporter : HDRPMaterialExporter
    {
        public HDRPDecalMaterialExporter(Urho3DEngine engine) : base(engine)
        {
        }

        public override int ExporterPriority => 10;

        public override bool CanExportMaterial(Material material)
        {
            switch (material.shader.name)
            {
                case "HDRP/Decal":
                    return true;
            }
            return false;
        }

        protected override UrhoPBRMaterial FromMetallicGlossiness(Material mat, MetallicGlossinessShaderArguments arguments)
        {
            var res = base.FromMetallicGlossiness(mat, arguments);
            res.Technique = "Techniques/DeferredDecal.xml";
            return res;
        }

        protected override MetallicGlossinessShaderArguments CreateShaderArguments(Material material)
        {
            var args = base.CreateShaderArguments(material);
            args.ExtraParameters[DecalMaskKey] = Vector4.one;
            return args;
        }

        private static readonly string DecalMaskKey = "DecalMask";

        protected override void ParseFloatOrRange(string propertyName, float value, MetallicGlossinessShaderArguments args)
        {
            switch (propertyName)
            {
                case "_AffectAlbedo":
                {
                    var mask = (Vector4)args.ExtraParameters[DecalMaskKey];
                    mask.x = value;
                    mask.y = value;
                    args.ExtraParameters[DecalMaskKey] = mask;
                    break;
                }
                case "_AffectNormal":
                {
                    var mask = (Vector4)args.ExtraParameters[DecalMaskKey];
                    mask.w = value;
                    args.ExtraParameters[DecalMaskKey] = mask;
                    break;
                }
                case "_AffectMetal":
                {
                    var mask = (Vector4)args.ExtraParameters[DecalMaskKey];
                    mask.z = value;
                    args.ExtraParameters[DecalMaskKey] = mask;
                    break;
                }
            }

            base.ParseFloatOrRange(propertyName, value, args);
        }
    }
}