using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace UnityToCustomEngineExporter.Editor.Urho3D
{
    public class SceneExporter : BaseNodeExporter
    {
        private readonly bool _asPrefab;

        public SceneExporter(Urho3DEngine engine, bool asPrefab, bool skipDisabled) : base(engine, skipDisabled)
        {
            _asPrefab = asPrefab;
        }

        public string ResolveAssetPath(Scene asset)
        {
            var sceneAssetName = ExportUtils.ReplaceExtension(ExportUtils.GetRelPathFromAsset(asset), ".xml");
            var scenesPrefix = "Scenes/";
            if (sceneAssetName.StartsWith(scenesPrefix, StringComparison.InvariantCultureIgnoreCase))
                //Fix scene path
                sceneAssetName = scenesPrefix + sceneAssetName.Substring(scenesPrefix.Length).Replace('/', '_');
            else
                //Fix scene path
                sceneAssetName = scenesPrefix + sceneAssetName.Replace('/', '_');
            return sceneAssetName;
        }

        public void ExportScene(Scene scene)
        {
            var exlusion = new HashSet<Renderer>();

            var sceneAssetName = ResolveAssetPath(scene);
            using (var writer = _engine.TryCreateXml(sceneAssetName, DateTime.MaxValue))
            {
                if (writer == null) return;
                var rootGameObjects = scene.GetRootGameObjects();
                if (_asPrefab)
                {
                    if (rootGameObjects.Length > 1)
                    {
                        writer.WriteStartElement("node");
                        writer.WriteAttributeString("id", (++_id).ToString());
                        writer.WriteWhitespace("\n");
                        foreach (var gameObject in rootGameObjects)
                            WriteObject(writer, "\t", gameObject, exlusion, true);
                        writer.WriteEndElement();
                        writer.WriteWhitespace("\n");
                    }
                    else
                    {
                        foreach (var gameObject in rootGameObjects) WriteObject(writer, "", gameObject, exlusion, true);
                    }
                }
                else
                {
                    using (var sceneElement = Element.Start(writer, "scene"))
                    {
                        WriteAttribute(writer, "\t", "Name", scene.name);
                        StartCompoent(writer, "\t", "Octree");
                        EndElement(writer, "\t");
                        StartCompoent(writer, "\t", "DebugRenderer");
                        EndElement(writer, "\t");
                        foreach (var gameObject in rootGameObjects) WriteObject(writer, "", gameObject, exlusion, true);
                    }
                }
            }
        }
    }
}