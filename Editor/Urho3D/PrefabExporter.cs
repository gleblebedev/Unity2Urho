using System.Collections.Generic;
using UnityEngine;

namespace UnityToCustomEngineExporter.Editor.Urho3D
{
    public class PrefabExporter : BaseNodeExporter
    {
        public PrefabExporter(Urho3DEngine engine) : base(engine)
        {
        }

        public void ExportPrefab(AssetKey assetGuid, string assetPath, GameObject gameObject)
        {
            var relativePath = EvaluatePrefabName(assetPath);
            using (var writer = _engine.TryCreateXml(assetGuid, relativePath, ExportUtils.GetLastWriteTimeUtc(assetPath)))
            {
                if (writer == null)
                    return;
                var prefabContext = new PrefabContext()
                {
                    TempFolder = ExportUtils.ReplaceExtension(relativePath, "")
                };

                WriteObject(writer, "", gameObject, new HashSet<Renderer>(), true, prefabContext);
            }
        }

        public string EvaluatePrefabName(string assetPath)
        {
            return ExportUtils.ReplaceExtension(ExportUtils.GetRelPathFromAssetPath(_engine.Options.Subfolder, assetPath),
                ".xml");
        }
    }
}