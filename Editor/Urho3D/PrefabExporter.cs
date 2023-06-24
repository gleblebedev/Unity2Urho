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
            Reset();
            var relativePath = EvaluatePrefabName(assetPath);
            using (var writer =
                _engine.TryCreateXml(assetGuid, relativePath, ExportUtils.GetLastWriteTimeUtc(assetPath)))
            {
                if (writer == null)
                    return;
                var prefabContext =
                    new PrefabContext(_engine, gameObject, ExportUtils.ReplaceExtension(relativePath, ""));

                if (_engine.Options.RBFX)
                {
                    using (var sceneElement = Element.Start(writer, "scene"))
                    {
                        var prefix = "\t";

                        StartComponent(writer, prefix, "Octree", true);
                        EndElement(writer, prefix);

                        WriteObject(writer, prefix, gameObject, new HashSet<Component>(), true, prefabContext, true);
                    }
                }
                else
                {
                    WriteObject(writer, "", gameObject, new HashSet<Component>(), true, prefabContext, true);
                }
            }
        }

        public string EvaluatePrefabName(string assetPath)
        {
            if (string.IsNullOrWhiteSpace(assetPath))
                return null;
            return ExportUtils.ReplaceExtension(
                ExportUtils.GetRelPathFromAssetPath(_engine.Options.Subfolder, assetPath),
                _engine.Options.RBFX ? ".prefab" : ".xml");
        }
    }
}