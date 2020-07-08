using System.Collections.Generic;
using UnityEngine;

namespace UnityToCustomEngineExporter.Editor.Urho3D
{
    public class PrefabExporter : BaseNodeExporter
    {
        public PrefabExporter(Urho3DEngine engine, bool skipDisabled) : base(engine, skipDisabled)
        {
        }

        public void ExportPrefab(AssetKey assetGuid, string assetPath, GameObject gameObject)
        {
            using (var writer = _engine.TryCreateXml(assetGuid,EvaluatePrefabName(assetPath),
                ExportUtils.GetLastWriteTimeUtc(assetPath)))
            {
                if (writer == null)
                    return;
                WriteObject(writer, "", gameObject, new HashSet<Renderer>(), true);
            }
        }

        public string EvaluatePrefabName(string assetPath)
        {
            return ExportUtils.ReplaceExtension(ExportUtils.GetRelPathFromAssetPath(_engine.Subfolder, assetPath), ".xml");
        }
    }
}