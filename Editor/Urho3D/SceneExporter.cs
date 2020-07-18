using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace UnityToCustomEngineExporter.Editor.Urho3D
{
    public class SceneExporter : BaseNodeExporter
    {
        public SceneExporter(Urho3DEngine engine) : base(engine)
        {
        }

        public string ResolveAssetPath(Scene asset)
        {
            var sceneAssetName =
                ExportUtils.ReplaceExtension(ExportUtils.GetRelPathFromAsset(_engine.Options.Subfolder, asset), ".xml");
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
            var prefabContext = new PrefabContext()
            {
                TempFolder = ExportUtils.ReplaceExtension(sceneAssetName, "")
            };
            using (var writer = _engine.TryCreateXml(AssetKey.Empty, sceneAssetName, DateTime.MaxValue))
            {
                if (writer == null) return;
                var rootGameObjects = scene.GetRootGameObjects();
                if (_engine.Options.ExportSceneAsPrefab)
                {
                    if (rootGameObjects.Length > 1)
                    {
                        StartNode(writer, "");
                        foreach (var gameObject in rootGameObjects)
                            WriteObject(writer, "\t", gameObject, exlusion, true, prefabContext);
                        writer.WriteEndElement();
                        writer.WriteWhitespace("\n");
                    }
                    else
                    {
                        foreach (var gameObject in rootGameObjects)
                            WriteObject(writer, "", gameObject, exlusion, true, prefabContext);
                    }
                }
                else
                {
                    using (var sceneElement = Element.Start(writer, "scene"))
                    {
                        WriteAttribute(writer, "\t", "Name", scene.name);
                        StartComponent(writer, "\t", "Octree", true);
                        EndElement(writer, "\t");
                        StartComponent(writer, "\t", "DebugRenderer", true);
                        EndElement(writer, "\t");

                        var skybox = scene.GetRootGameObjects().Select(_ => _.GetComponentInChildren<Skybox>(true))
                            .Where(_ => _ != null).FirstOrDefault();
                        var skyboxMaterial = skybox?.material ?? RenderSettings.skybox;
                        if (skybox == null)
                        {
                            WriteSkyboxComponent(writer, "\t", RenderSettings.skybox, prefabContext, true);
                        }
                        if (skyboxMaterial != null)
                        {
                            var skyboxCubemap = _engine.TryGetSkyboxCubemap(skyboxMaterial);
                            if (!string.IsNullOrWhiteSpace(skyboxCubemap))
                            {
                                ExportZone(writer, "\t", new Vector3(2000, 2000, 2000), skyboxCubemap, prefabContext, true);
                            }
                        }

                        foreach (var gameObject in rootGameObjects)
                            WriteObject(writer, "", gameObject, exlusion, true, prefabContext);
                    }
                }
            }

            _engine.ExportNavMesh(prefabContext);
        }
    }
}