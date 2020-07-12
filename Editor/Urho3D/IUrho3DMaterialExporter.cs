using UnityEngine;

namespace UnityToCustomEngineExporter.Editor.Urho3D
{
    public interface IUrho3DMaterialExporter
    {
        int ExporterPriority { get; }

        bool CanExportMaterial(Material material);

        void ExportMaterial(Material material);

        string EvaluateMaterialName(Material material);
    }
}