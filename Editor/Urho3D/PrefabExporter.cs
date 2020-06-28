using System.Collections.Generic;
using UnityEngine;

namespace UnityToCustomEngineExporter.Editor.Urho3D
{
    public class PrefabExporter : BaseNodeExporter
    {
        public PrefabExporter(Urho3DEngine engine, bool skipDisabled) : base(engine, skipDisabled)
        {
        }

        public void ExportPrefab(string assetPath, GameObject gameObject)
        {
            using (var writer = _engine.TryCreateXml(EvaluatePrefabName(assetPath),
                ExportUtils.GetLastWriteTimeUtc(assetPath)))
            {
                if (writer == null)
                    return;
                WriteObject(writer, "", gameObject, new HashSet<Renderer>(), true);
            }
        }

        public string EvaluatePrefabName(string assetPath)
        {
            return ExportUtils.ReplaceExtension(ExportUtils.GetRelPathFromAssetPath(assetPath), ".xml");
        }
    }
}