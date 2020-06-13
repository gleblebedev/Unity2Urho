using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Urho3DExporter
{
    public class SceneExporter : BaseNodeExporter, IExporter
    {
        private readonly bool _asPrefab;

        public SceneExporter(AssetCollection assets, bool asPrefab, bool skipDisabled) : base(assets, skipDisabled)
        {
            _asPrefab = asPrefab;
        }

        public void ExportAsset(AssetContext asset, Scene scene)
        {
            var exlusion = new HashSet<Renderer>();
            var scenesPrefix = "Scenes/";
            var sceneAssetName = asset.UrhoAssetName;
            if (sceneAssetName.StartsWith(scenesPrefix, StringComparison.InvariantCultureIgnoreCase))
            {
                //Fix scene path
                sceneAssetName = scenesPrefix + sceneAssetName.Substring(scenesPrefix.Length).Replace('/', '_');
            }
            else
            {
                //Fix scene path
                sceneAssetName = scenesPrefix + sceneAssetName.Replace('/', '_');
            }
            using (var writer = asset.DestinationFolder.CreateXml(sceneAssetName, DateTime.MaxValue))
            {
                if (writer == null)
                    return;
                var rootGameObjects = scene.GetRootGameObjects();
                if (_asPrefab)
                {
                    if (rootGameObjects.Length > 1)
                    {
                        writer.WriteStartElement("node");
                        writer.WriteAttributeString("id", (++_id).ToString());
                        writer.WriteWhitespace("\n");
                        foreach (var gameObject in rootGameObjects)
                        {
                            WriteObject(writer, "\t", gameObject, exlusion, asset, true);
                        }
                        writer.WriteEndElement();
                        writer.WriteWhitespace("\n");
                    }
                    else
                    {
                        foreach (var gameObject in rootGameObjects)
                        {
                            WriteObject(writer, "", gameObject, exlusion, asset, true);
                        }
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
                        foreach (var gameObject in rootGameObjects)
                        {
                            WriteObject(writer, "", gameObject, exlusion, asset, true);
                        }
                    }
                }
            }
        }

        public void ExportAsset(AssetContext asset)
        {
            var go = AssetDatabase.LoadAssetAtPath<SceneAsset>(asset.AssetPath);

            var exclusionSet = new HashSet<Renderer>();
            using (var writer = asset.CreateXml())
            {
                if (writer == null)
                    return;
                using (var scene = Element.Start(writer, "scene"))
                {
                }
            }
        }
    }
}