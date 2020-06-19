using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Xml;
using UnityEditor;
using UnityEditor.Experimental.Rendering;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;

namespace Urho3DExporter
{
    public class BaseNodeExporter
    {
        private readonly AssetCollection _assets;
        private readonly bool _skipDisabled;

        protected int _id;

        public BaseNodeExporter(AssetCollection assets, bool skipDisabled)
        {
            _assets = assets;
            _skipDisabled = skipDisabled;
        }

        public static string Format(Color pos)
        {
            return string.Format(CultureInfo.InvariantCulture, "{0} {1} {2} {3}", pos.r, pos.g, pos.b, pos.a);
        }

        public static string FormatRGB(Color pos)
        {
            return string.Format(CultureInfo.InvariantCulture, "{0} {1} {2}", pos.r, pos.g, pos.b);
        }

        public static string Format(float pos)
        {
            return string.Format(CultureInfo.InvariantCulture, "{0}", pos);
        }

        public static string Format(Vector4 pos)
        {
            return string.Format(CultureInfo.InvariantCulture, "{0} {1} {2} {3}", pos.x, pos.y, pos.z, pos.w);
        }

        private static (float min, float max, Vector2 size) WriteHeightMap(AssetContext asset, string heightmapFileName,
            Terrain terrain)
        {
            var w = terrain.terrainData.heightmapResolution;
            var h = terrain.terrainData.heightmapResolution;
            var max = float.MinValue;
            var min = float.MaxValue;
            var heights = terrain.terrainData.GetHeights(0, 0, w, h);
            foreach (var height in heights)
            {
                if (height > max) max = height;
                if (height < min) min = height;
            }

            if (max < min)
            {
                max = 1;
                min = 0;
            }
            else if (max == min)
            {
                max = min + 0.1f;
            }

            using (var imageFile = asset.DestinationFolder.Create(heightmapFileName, DateTime.MaxValue))
            {
                if (imageFile != null)
                    using (var binaryWriter = new BinaryWriter(imageFile))
                    {
                        WriteTgaHeader(binaryWriter, 32, w, h);
                        for (var y = h - 1; y >= 0; --y)
                        for (var x = 0; x < w; ++x)
                        {
                            var height = (heights[h - y - 1, x] - min) / (max - min) * 255.0f;
                            var msb = (byte) height;
                            var lsb = (byte) ((height - msb) * 255.0f);
                            binaryWriter.Write((byte) 0); //B - none
                            binaryWriter.Write(lsb); //G - LSB
                            binaryWriter.Write(msb); //R - MSB
                            binaryWriter.Write((byte) 255); //A - none
                        }
                    }
            }

            return (min, max, new Vector2(w, h));
        }

        private static void WriteTgaHeader(BinaryWriter binaryWriter, int bitsPerPixel, int w, int h)
        {
            binaryWriter.Write((byte) 0);
            binaryWriter.Write((byte) 0);
            binaryWriter.Write((byte) (bitsPerPixel == 8 ? 3 : 2));
            binaryWriter.Write((short) 0);
            binaryWriter.Write((short) 0);
            binaryWriter.Write((byte) 0);
            binaryWriter.Write((short) 0);
            binaryWriter.Write((short) 0);
            binaryWriter.Write((short) w);
            binaryWriter.Write((short) h);
            binaryWriter.Write((byte) bitsPerPixel);
            binaryWriter.Write((byte) 0);
        }

        protected void WriteAttribute(XmlWriter writer, string prefix, string name, float pos)
        {
            WriteAttribute(writer, prefix, name, string.Format(CultureInfo.InvariantCulture, "{0}", pos));
        }

        protected void WriteAttribute(XmlWriter writer, string prefix, string name, Vector3 pos)
        {
            WriteAttribute(writer, prefix, name,
                string.Format(CultureInfo.InvariantCulture, "{0} {1} {2}", pos.x, pos.y, pos.z));
        }

        protected void WriteAttribute(XmlWriter writer, string prefix, string name, Vector4 pos)
        {
            WriteAttribute(writer, prefix, name, Format(pos));
        }

        protected void WriteAttribute(XmlWriter writer, string prefix, string name, Quaternion pos)
        {
            WriteAttribute(writer, prefix, name,
                string.Format(CultureInfo.InvariantCulture, "{0} {1} {2} {3}", pos.w, pos.x, pos.y, pos.z));
        }

        protected void WriteAttribute(XmlWriter writer, string prefix, string name, Color pos)
        {
            WriteAttribute(writer, prefix, name, Format(pos));
        }

        protected void EndElement(XmlWriter writer, string prefix)
        {
            writer.WriteWhitespace(prefix);
            writer.WriteEndElement();
            writer.WriteWhitespace("\n");
        }

        protected void StartCompoent(XmlWriter writer, string prefix, string type)
        {
            writer.WriteWhitespace(prefix);
            writer.WriteStartElement("component");
            writer.WriteAttributeString("type", type);
            writer.WriteAttributeString("id", (++_id).ToString());
            writer.WriteWhitespace("\n");
        }

        protected void WriteAttribute(XmlWriter writer, string prefix, string name, string vaue)
        {
            writer.WriteWhitespace(prefix);
            writer.WriteStartElement("attribute");
            writer.WriteAttributeString("name", name);
            writer.WriteAttributeString("value", vaue);
            writer.WriteEndElement();
            writer.WriteWhitespace("\n");
        }


        //private void WriteMaterialAttribute(string subSubPrefix, Material[] meshRendererMaterials)
        //{
        //    var material = new StringBuilder();
        //    material.Append("Material");
        //    for (var i = 0; i < meshRendererMaterials.Length; ++i)
        //    {
        //        var meshRendererMaterial = meshRendererMaterials[i];
        //        var relPath = GetRelAssetPath(meshRendererMaterial);

        //        var outputMaterialName = "Materials/" + relPath + ".xml";

        //        material.Append(";");
        //        material.Append(outputMaterialName);

        //        var materialFileName = Path.Combine(_assetsFolder, outputMaterialName);
        //        if (!File.Exists(materialFileName))
        //            CreateMaterial(materialFileName, meshRendererMaterial);
        //    }
        //    WriteAttribute(subSubPrefix, "Material", material.ToString());
        //}
        protected void WriteObject(XmlWriter writer, string prefix, GameObject obj, HashSet<Renderer> excludeList,
            AssetContext asset, bool parentEnabled)
        {
            var isEnabled = obj.activeSelf && parentEnabled;
            if (_skipDisabled && !isEnabled)
            {
                return;
            }

            var localExcludeList = new HashSet<Renderer>(excludeList);
            if (!string.IsNullOrEmpty(prefix))
                writer.WriteWhitespace(prefix);
            writer.WriteStartElement("node");
            writer.WriteAttributeString("id", (++_id).ToString());
            writer.WriteWhitespace("\n");

            var subPrefix = prefix + "\t";
            var subSubPrefix = subPrefix + "\t";

            WriteAttribute(writer, subPrefix, "Is Enabled", isEnabled);
            WriteAttribute(writer, subPrefix, "Name", obj.name);
            WriteAttribute(writer, subPrefix, "Tags", obj.tag);
            WriteAttribute(writer, subPrefix, "Position", obj.transform.localPosition);
            WriteAttribute(writer, subPrefix, "Rotation", obj.transform.localRotation);
            WriteAttribute(writer, subPrefix, "Scale", obj.transform.localScale);

            foreach (var component in obj.GetComponents<Component>())
            {
                if (component is IComponentToExport customComponent)
                {
                    ExportCustomComponent(writer, subPrefix, customComponent);
                }
                else if (component is Camera camera)
                {
                    if (camera != null)
                    {
                        StartCompoent(writer, subPrefix, "Camera");

                        WriteAttribute(writer, subSubPrefix, "Near Clip", camera.nearClipPlane);
                        WriteAttribute(writer, subSubPrefix, "Far Clip", camera.farClipPlane);

                        EndElement(writer, subPrefix);
                    }
                }
                else if (component is Light light)
                {
                    if (light != null && light.type != LightType.Area)
                    {
                        StartCompoent(writer, subPrefix, "Light");
                        if (light.type == LightType.Directional)
                        {
                            WriteAttribute(writer, subSubPrefix, "Light Type", "Directional");
                            WriteAttribute(writer, subSubPrefix, "CSM Splits", "2 16 128 0");
                        }
                        else if (light.type == LightType.Spot)
                            WriteAttribute(writer, subSubPrefix, "Light Type", "Spot");
                        else if (light.type == LightType.Point)
                            WriteAttribute(writer, subSubPrefix, "Range", light.range);
                        WriteAttribute(writer, subSubPrefix, "Color", light.color);
                        WriteAttribute(writer, subSubPrefix, "Brightness Multiplier", light.intensity * 1000);
                        WriteAttribute(writer, subSubPrefix, "Use Physical Values", "true");
                        WriteAttribute(writer, subSubPrefix, "Cast Shadows", light.shadows != LightShadows.None);

                        EndElement(writer, subPrefix);
                    }
                }
                else if (component is Terrain terrain)
                {
                    ExportTerrain(writer, obj, asset, terrain, subPrefix);
                }
                else if (component is Rigidbody rigidbody)
                {
                    StartCompoent(writer, subPrefix, "RigidBody");
                    var localToWorldMatrix = obj.transform.localToWorldMatrix;
                    var pos = new Vector3(localToWorldMatrix.m03, localToWorldMatrix.m13, localToWorldMatrix.m23);
                    WriteAttribute(writer, subSubPrefix, "Physics Position", pos);
                    WriteAttribute(writer, subSubPrefix, "Mass", rigidbody.mass);
                    EndElement(writer, subPrefix);
                }
                else if (component is MeshCollider meshCollider)
                {
                    StartCompoent(writer, subPrefix, "CollisionShape");
                    WriteAttribute(writer, subSubPrefix, "Shape Type", "TriangleMesh");
                    if (meshCollider.sharedMesh != null)
                    {
                        var sharedMesh = meshCollider.sharedMesh;
                        string meshPath;
                        if (_assets.TryGetMeshPath(sharedMesh, out meshPath))
                        {
                            WriteAttribute(writer, subSubPrefix, "Model", "Model;" + meshPath);
                        }
                    }
                    EndElement(writer, subPrefix);
                    if (obj.GetComponent<Rigidbody>() == null)
                    {
                        StartCompoent(writer, subPrefix, "RigidBody");
                        var localToWorldMatrix = obj.transform.localToWorldMatrix;
                        var pos = new Vector3(localToWorldMatrix.m03, localToWorldMatrix.m13, localToWorldMatrix.m23);
                        WriteAttribute(writer, subSubPrefix, "Physics Position", pos);
                        EndElement(writer, subPrefix);
                    }
                }
                else if (component is BoxCollider boxCollider)
                {
                    StartCompoent(writer, subPrefix, "CollisionShape");
                    WriteAttribute(writer, subSubPrefix, "Size", "1 1 1");
                    EndElement(writer, subPrefix);
                }
                else if (component is SphereCollider sphereCollider)
                {
                    StartCompoent(writer, subPrefix, "CollisionShape");
                    WriteAttribute(writer, subSubPrefix, "Shape Type", "Sphere");
                    EndElement(writer, subPrefix);
                }
                else if (component is CapsuleCollider capsuleCollider)
                {
                    StartCompoent(writer, subPrefix, "CollisionShape");
                    if (component.name == "Cylinder")
                        WriteAttribute(writer, subSubPrefix, "Shape Type", "Cylinder");
                    else
                        WriteAttribute(writer, subSubPrefix, "Shape Type", "Capsule");
                    var d = capsuleCollider.radius * 2.0f;
                    WriteAttribute(writer, subSubPrefix, "Size", new Vector3(d, capsuleCollider.height, d));
                    EndElement(writer, subPrefix);
                }
                else if (component is Skybox skybox)
                {
                    StartCompoent(writer, subPrefix, "Skybox");
                    {
                        // Export cube
                        GameObject gameObject = GameObject.CreatePrimitive(UnityEngine.PrimitiveType.Cube);
                        Mesh mesh = gameObject.GetComponent<MeshFilter>().sharedMesh;
                        GameObject.DestroyImmediate(gameObject);
                        var sharedMeshName = "UnityBuiltIn/Cube.mdl";
                        new MeshExporter(null).ExportMeshModel(asset.DestinationFolder, sharedMeshName, mesh, null,
                            DateTime.MinValue);
                    }
                    WriteAttribute(writer, subSubPrefix, "Model", "Model;UnityBuiltIn/Cube.mdl");
                    
                    var materials = "Material;" + MaterialExporter.GetUrhoPath(skybox.material);
                    WriteAttribute(writer, subSubPrefix, "Material", materials);
                    EndElement(writer, subPrefix);
                }
                else if (component is Collider collider)
                {
                    StartCompoent(writer, subPrefix, "CollisionShape");
                    EndElement(writer, subPrefix);
                }
                else if (component is ReflectionProbe reflectionProbe)
                {
                    switch (reflectionProbe.mode)
                    {
                        case ReflectionProbeMode.Baked:
                            ExportZone(writer, subPrefix, reflectionProbe, reflectionProbe.bakedTexture as Cubemap);
                            break;
                        case ReflectionProbeMode.Custom:
                            ExportZone(writer, subPrefix, reflectionProbe, reflectionProbe.customBakedTexture as Cubemap);
                            break;
                    }

                }
            }

            var meshFilter = obj.GetComponent<MeshFilter>();
            var meshRenderer = obj.GetComponent<MeshRenderer>();
            var skinnedMeshRenderer = obj.GetComponent<SkinnedMeshRenderer>();
            var lodGroup = obj.GetComponent<LODGroup>();

            if (lodGroup != null)
            {
                var lods = lodGroup.GetLODs();
                foreach (var lod in lods.Skip(1))
                {
                    foreach (var renderer in lod.renderers)
                    {
                        localExcludeList.Add(renderer);
                    }
                }
            }

            if (meshRenderer != null && !localExcludeList.Contains(meshRenderer))
                if (meshFilter != null)
                {
                    StartCompoent(writer, subPrefix, "StaticModel");

                    var sharedMesh = meshFilter.sharedMesh;
                    string meshPath;
                    if (_assets.TryGetMeshPath(sharedMesh, out meshPath))
                        WriteAttribute(writer, subSubPrefix, "Model", "Model;" + meshPath);

                    var materials = "Material";
                    foreach (var material in meshRenderer.sharedMaterials)
                    {
                        string path;
                        _assets.TryGetMaterialPath(material, out path);
                        materials += ";" + path;
                    }

                    WriteAttribute(writer, subSubPrefix, "Material", materials);

                    WriteAttribute(writer, subSubPrefix, "Cast Shadows",
                        meshRenderer.shadowCastingMode != ShadowCastingMode.Off);

                    EndElement(writer, subPrefix);
                }

            if (skinnedMeshRenderer != null && !localExcludeList.Contains(skinnedMeshRenderer))
            {
                StartCompoent(writer, subPrefix, "AnimatedModel");

                var sharedMesh = skinnedMeshRenderer.sharedMesh;
                string meshPath;
                if (_assets.TryGetMeshPath(sharedMesh, out meshPath))
                    WriteAttribute(writer, subSubPrefix, "Model", "Model;" + meshPath);

                var materials = "Material";
                foreach (var material in skinnedMeshRenderer.sharedMaterials)
                {
                    string path;
                    _assets.TryGetMaterialPath(material, out path);
                    materials += ";" + path;
                }

                WriteAttribute(writer, subSubPrefix, "Material", materials);

                WriteAttribute(writer, subSubPrefix, "Cast Shadows",
                    skinnedMeshRenderer.shadowCastingMode != ShadowCastingMode.Off);

                EndElement(writer, subPrefix);
            }

            foreach (Transform childTransform in obj.transform)
                if (childTransform.parent.gameObject == obj)
                    WriteObject(writer, subPrefix, childTransform.gameObject, localExcludeList, asset, isEnabled);

            if (!string.IsNullOrEmpty(prefix))
                writer.WriteWhitespace(prefix);
            writer.WriteEndElement();
            writer.WriteWhitespace("\n");
        }

        private void ExportZone(XmlWriter writer, string subPrefix, ReflectionProbe reflectionProbe, Cubemap cubemap)
        {
            if (cubemap == null)
            {
                return;
            }

            var assetPath = AssetDatabase.GetAssetPath(cubemap);
            if (string.IsNullOrWhiteSpace(assetPath))
                return;

            var texName = AssetContext.ReplaceExt(AssetContext.GetRelPathFromAssetPath(assetPath),".xml");

            StartCompoent(writer, subPrefix, "Zone");

            var subSubPrefix = subPrefix + "\t";
            WriteAttribute(writer, subSubPrefix, "Bounding Box Min", -(reflectionProbe.size * 0.5f));
            WriteAttribute(writer, subSubPrefix, "Bounding Box Max", (reflectionProbe.size * 0.5f));

            WriteAttribute(writer, subSubPrefix, "Zone Texture", "TextureCube;" + texName);
            EndElement(writer, subPrefix);
        }

        private void ExportCustomComponent(XmlWriter writer, string subPrefix, IComponentToExport customComponent)
        {
            if (customComponent == null) return;

            var subSubPrefix = subPrefix + "\t";
            StartCompoent(writer, subPrefix, customComponent.GetExportType());
            foreach (var keyValuePair in customComponent.GetAttributesToExport())
            {
                WriteAttribute(writer, subSubPrefix, keyValuePair.Key, keyValuePair.Value);
            }
            EndElement(writer, subPrefix);
        }

        private void WriteAttribute(XmlWriter writer, string prefix, string name, bool flag)
        {
            WriteAttribute(writer, prefix, name, flag ? "true" : "false");
        }

        private void WriteAttribute(XmlWriter writer, string prefix, string name, int flag)
        {
            WriteAttribute(writer, prefix, name, flag.ToString(CultureInfo.InvariantCulture));
        }

        private void ExportTerrain(XmlWriter writer, GameObject obj, AssetContext asset, Terrain terrain, string subPrefix)
        {
            if (terrain == null) return;

            var subSubPrefix = subPrefix + "\t";


            var terrainSize = terrain.terrainData.size;
            writer.WriteWhitespace(subPrefix);
            writer.WriteStartElement("node");
            writer.WriteAttributeString("id", (++_id).ToString());
            writer.WriteWhitespace("\n");

            var folderAndName = asset.RelPath + "/" +
                                Path.GetInvalidFileNameChars().Aggregate(obj.name, (_1, _2) => _1.Replace(_2, '_'));
            var heightmapFileName = "Textures/Terrains/" + folderAndName + ".tga";
            var materialFileName = "Materials/Terrains/" + folderAndName + ".xml";

            var (min, max, size) = WriteHeightMap(asset, heightmapFileName, terrain);

            WriteAttribute(writer, subPrefix, "Position",
                new Vector3(terrainSize.x * 0.5f, -min, terrainSize.z * 0.5f));

            StartCompoent(writer, subPrefix, "Terrain");

            WriteAttribute(writer, subSubPrefix, "Height Map", "Image;" + heightmapFileName);
            WriteAttribute(writer, subSubPrefix, "Material", "Material;" + materialFileName);
            WriteTerrainMaterial(terrain, asset, materialFileName,
                "Textures/Terrains/" + folderAndName + ".Weights.tga");
            WriteAttribute(writer, subSubPrefix, "Vertex Spacing",
                new Vector3(terrainSize.x / size.x, 2.0f * (max - min), terrainSize.z / size.y));
            EndElement(writer, subPrefix);
            EndElement(writer, subPrefix);
        }

        private void WriteTerrainMaterial(Terrain terrain, AssetContext asset, string materialPath,
            string weightsTextureName)
        {
            using (var writer = asset.DestinationFolder.CreateXml(materialPath, DateTime.MaxValue))
            {
                if (writer == null)
                    return;

                var layers = terrain.terrainData.terrainLayers;
                if (layers.Length > 3) layers = layers.Take(3).ToArray();

                writer.WriteStartDocument();
                writer.WriteStartElement("material");
                writer.WriteWhitespace(Environment.NewLine);
                {
                    writer.WriteStartElement("technique");
                    writer.WriteAttributeString("name", "Techniques/TerrainBlend.xml");
                    writer.WriteEndElement();
                    writer.WriteWhitespace(Environment.NewLine);
                }
                {
                    writer.WriteStartElement("texture");
                    writer.WriteAttributeString("unit", "0");
                    writer.WriteAttributeString("name", weightsTextureName);
                    writer.WriteEndElement();
                    writer.WriteWhitespace(Environment.NewLine);

                    WriteTerrainWeightsTexture(asset, weightsTextureName, terrain);
                }
                for (var layerIndex = 0; layerIndex < layers.Length; ++layerIndex)
                {
                    var layer = layers[layerIndex];

                    writer.WriteStartElement("texture");
                    writer.WriteAttributeString("unit", (layerIndex + 1).ToString(CultureInfo.InvariantCulture));
                    if (layer.diffuseTexture != null)
                    {
                        string urhoAssetName;
                        if (_assets.TryGetTexturePath(layer.diffuseTexture, out urhoAssetName))
                            writer.WriteAttributeString("name", urhoAssetName);
                    }

                    writer.WriteEndElement();
                    writer.WriteWhitespace(Environment.NewLine);
                }

                {
                    writer.WriteStartElement("parameter");
                    writer.WriteAttributeString("name", "MatSpecColor");
                    writer.WriteAttributeString("value", "0.5 0.5 0.5 16");
                    writer.WriteEndElement();
                    writer.WriteWhitespace(Environment.NewLine);
                }
                {
                    writer.WriteStartElement("parameter");
                    writer.WriteAttributeString("name", "DetailTiling");
                    writer.WriteAttributeString("value", "32 32");
                    writer.WriteEndElement();
                    writer.WriteWhitespace(Environment.NewLine);
                }
                writer.WriteEndElement();
                writer.WriteEndDocument();
            }
        }

        private void WriteTerrainWeightsTexture(AssetContext asset, string weightsTextureName, Terrain terrain)
        {
            var layers = terrain.terrainData.terrainLayers;
            var w = terrain.terrainData.alphamapWidth;
            var h = terrain.terrainData.alphamapHeight;
            var alphamaps = terrain.terrainData.GetAlphamaps(0, 0, w, h);
            var numAlphamaps = alphamaps.GetLength(2);

            //Urho3D doesn't support more than 3 textures
            if (numAlphamaps > 3) numAlphamaps = 3;

            using (var imageFile = asset.DestinationFolder.Create(weightsTextureName, DateTime.MaxValue))
            {
                if (imageFile == null)
                    return;
                using (var binaryWriter = new BinaryWriter(imageFile))
                {
                    var weights = new byte[4];
                    weights[3] = 255;
                    WriteTgaHeader(binaryWriter, weights.Length * 8, w, h);
                    for (var y = h - 1; y >= 0; --y)
                    for (var x = 0; x < w; ++x)
                    {
                        var sum = 0;
                        for (var i = 0; i < weights.Length; ++i)
                            if (numAlphamaps > i && layers.Length > i)
                            {
                                var weight = (byte) (alphamaps[h - y - 1, x, i] * 255.0f);
                                sum += weight;
                                weights[i] = weight;
                            }

                        if (sum == 0)
                            weights[0] = 255;

                        binaryWriter.Write(weights[2]); //B
                        binaryWriter.Write(weights[1]); //G
                        binaryWriter.Write(weights[0]); //R
                        binaryWriter.Write(weights[3]); //A
                    }
                }
            }
        }

        public class Element : IDisposable
        {
            private readonly XmlWriter _writer;

            public Element(XmlWriter writer)
            {
                _writer = writer;
            }

            public static IDisposable Start(XmlWriter writer, string localName)
            {
                writer.WriteStartElement(localName);
                return new Element(writer);
            }

            public void Dispose()
            {
                _writer.WriteEndElement();
            }
        }
    }
}