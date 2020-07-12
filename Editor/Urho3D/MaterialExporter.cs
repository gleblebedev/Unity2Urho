using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace UnityToCustomEngineExporter.Editor.Urho3D
{
    public class MaterialExporter
    {
        private readonly Urho3DEngine _engine;
        private readonly List<IUrho3DMaterialExporter> _exporters;
        private readonly LegacyMaterialExporter _defaultExporter;

        public MaterialExporter(Urho3DEngine engine)
        {
            _engine = engine;
            _defaultExporter = new LegacyMaterialExporter(_engine);
            _exporters = new IUrho3DMaterialExporter[]
            {
                _defaultExporter,
                new StandardMaterialExporter(_engine),
                new StandardSpecularMaterialExporter(_engine),
                new WaterMaterialExporter(_engine),
                new SkyboxMaterialExporter(_engine),
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
    }
}