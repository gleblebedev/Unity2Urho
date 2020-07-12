using UnityEngine;

namespace UnityToCustomEngineExporter.Editor.Urho3D
{
    public interface IUrho3DMaterialExporter
    {
        int ExporterPriority { get; }

        bool CanExportMaterial(Material material);

        void ExportMaterial(Material material, PrefabContext prefabContext);

        string EvaluateMaterialName(Material material);
    }
}