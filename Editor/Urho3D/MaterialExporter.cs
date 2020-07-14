using System.Collections.Generic;
using System.Linq;
using System.Xml;
using UnityEngine;

namespace UnityToCustomEngineExporter.Editor.Urho3D
{
    public class MaterialExporter
    {
        private readonly Urho3DEngine _engine;
        private readonly List<IUrho3DMaterialExporter> _exporters;
        private readonly LegacyMaterialExporter _defaultExporter;
        private SkyboxMaterialExporter _skyboxMaterialExporter;

        public MaterialExporter(Urho3DEngine engine)
        {
            _engine = engine;
            _defaultExporter = new LegacyMaterialExporter(_engine);
            _skyboxMaterialExporter = new SkyboxMaterialExporter(_engine);
            _exporters = new IUrho3DMaterialExporter[]
            {
                _defaultExporter,
                new StandardMaterialExporter(_engine),
                new StandardSpecularMaterialExporter(_engine),
                new WaterMaterialExporter(_engine),
                _skyboxMaterialExporter,
                new VegetationMaterialExporter(_engine)
            }.OrderByDescending(_ => _.ExporterPriority).ToList();
        }

        public string EvaluateMaterialName(Material material)
        {
            if (material == null)
                return null;

            foreach (var materialExporter in _exporters)
                if (materialExporter.CanExportMaterial(material))
                    return materialExporter.EvaluateMaterialName(material);

            return _defaultExporter.EvaluateMaterialName(material);
        }

        public void ExportMaterial(Material material, PrefabContext prefabContext)
        {
            if (material == null)
                return;

            foreach (var materialExporter in _exporters)
                if (materialExporter.CanExportMaterial(material))
                {
                    materialExporter.ExportMaterial(material, prefabContext);
                    return;
                }

            _defaultExporter.ExportMaterial(material, prefabContext);
        }

        public string TryGetSkyboxCubemap(Material skyboxMaterial)
        {
            if (!_skyboxMaterialExporter.CanExportMaterial(skyboxMaterial))
                return null;
            var arguments = _skyboxMaterialExporter.SetupSkybox(skyboxMaterial);
            if (arguments.Skybox != null)
                return _engine.EvaluateTextrueName(arguments.Skybox);
            var anyFace = arguments.BackTex ?? arguments.DownTex ?? arguments.FrontTex ??
                          arguments.LeftTex ?? arguments.RightTex ?? arguments.UpTex;
            if (anyFace != null)
            {
                return ExportUtils.ReplaceExtension(_engine.EvaluateMaterialName(skyboxMaterial), ".Cubemap.xml");
            }

            return null;
        }
    }
}