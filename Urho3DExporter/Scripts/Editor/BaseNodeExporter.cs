using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Xml;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace Urho3DExporter
{
    public class BaseNodeExporter: XmlExporter
    {
        private readonly AssetCollection _assets;

        private int _id;

        public class Element:IDisposable
        {
            private XmlWriter _writer;

            public Element(XmlWriter writer)
            {
                this._writer = writer;
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

        public BaseNodeExporter(AssetCollection assets)
        {
            _assets = assets;
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

        public static string Format(Color pos)
        {
            return string.Format(CultureInfo.InvariantCulture, "{0} {1} {2} {3}", pos.r, pos.g, pos.b, pos.a);
        }

        public static string Format(Vector4 pos)
        {
            return string.Format(CultureInfo.InvariantCulture, "{0} {1} {2} {3}", pos.x, pos.y, pos.z, pos.w);
        }

        private void WriteAttribute(XmlWriter writer, string prefix, string name, bool flag)
        {
            WriteAttribute(writer, prefix, name, flag ? "true" : "false");
        }

        private void WriteAttribute(XmlWriter writer, string prefix, string name, int flag)
        {
            WriteAttribute(writer, prefix, name, flag.ToString(CultureInfo.InvariantCulture));
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
        protected void WriteObject(XmlWriter writer, string prefix, GameObject obj, HashSet<Renderer> excludeList)
        {
            var localExcludeList = new HashSet<Renderer>(excludeList);
            if (!string.IsNullOrEmpty(prefix))
                writer.WriteWhitespace(prefix);
            writer.WriteStartElement("node");
            writer.WriteAttributeString("id", (++_id).ToString());
            writer.WriteWhitespace("\n");

            var subPrefix = prefix + "\t";
            var subSubPrefix = subPrefix + "\t";

            WriteAttribute(writer, subPrefix, "Is Enabled", obj.activeSelf);
            WriteAttribute(writer, subPrefix, "Name", obj.name);
            WriteAttribute(writer, subPrefix, "Tags", obj.tag);
            WriteAttribute(writer, subPrefix, "Position", obj.transform.localPosition);
            WriteAttribute(writer, subPrefix, "Rotation", obj.transform.localRotation);
            WriteAttribute(writer, subPrefix, "Scale", obj.transform.localScale);

            var meshFilter = obj.GetComponent<MeshFilter>();
            var meshRenderer = obj.GetComponent<MeshRenderer>();
            var skinnedMeshRenderer = obj.GetComponent<SkinnedMeshRenderer>();
            var lodGroup = obj.GetComponent<LODGroup>();
            var meshCollider = obj.GetComponent<MeshCollider>();
            var terrain = obj.GetComponent<Terrain>();
            var light = obj.GetComponent<Light>();
            var camera = obj.GetComponent<Camera>();
            var reflectionProbe = obj.GetComponent<ReflectionProbe>();

            //if (reflectionProbe != null)
            //{
            //    StartCompoent(subPrefix, "Zone");

            //    WriteAttribute(subSubPrefix, "Bounding Box Min", -(reflectionProbe.size * 0.5f));
            //    WriteAttribute(subSubPrefix, "Bounding Box Max", (reflectionProbe.size * 0.5f));
            //    var cubemap = reflectionProbe.bakedTexture as Cubemap;
            //    if (cubemap != null)
            //    {
            //        var name = SaveCubemap(cubemap);
            //        WriteAttribute(subSubPrefix, "Zone Texture", "TextureCube;" + name);
            //    }
            //    EndElement(subPrefix);
            //}
            if (camera != null)
            {
                StartCompoent(writer, subPrefix, "Camera");

                WriteAttribute(writer, subSubPrefix, "Near Clip", camera.nearClipPlane);
                WriteAttribute(writer, subSubPrefix, "Far Clip", camera.farClipPlane);

                EndElement(writer, subPrefix);
            }

            if (light != null && light.type != LightType.Area)
            {
                StartCompoent(writer, subPrefix, "Light");
                if (light.type == LightType.Directional)
                    WriteAttribute(writer, subSubPrefix, "Light Type", "Directional");
                else if (light.type == LightType.Spot)
                    WriteAttribute(writer, subSubPrefix, "Light Type", "Spot");
                else if (light.type == LightType.Point)
                    WriteAttribute(writer, subSubPrefix, "Range", light.range);
                WriteAttribute(writer, subSubPrefix, "Color", light.color);
                WriteAttribute(writer, subSubPrefix, "Brightness Multiplier", light.intensity);
                WriteAttribute(writer, subSubPrefix, "Cast Shadows", light.shadows != LightShadows.None);

                EndElement(writer, subPrefix);
            }

            //if (terrain != null)
            //{
            //    var terrainSize = terrain.terrainData.size;
            //    writer.WriteWhitespace(subPrefix);
            //    writer.WriteStartElement("node");
            //    writer.WriteAttributeString("id", (++_id).ToString());
            //    writer.WriteWhitespace("\n");

            //    var w = terrain.terrainData.heightmapWidth;
            //    var h = terrain.terrainData.heightmapHeight;
            //    var max = float.MinValue;
            //    var min = float.MaxValue;
            //    var heights = terrain.terrainData.GetHeights(0, 0, w, h);
            //    foreach (var height in heights)
            //    {
            //        if (height > max) max = height;
            //        if (height < min) min = height;
            //    }

            //    if (max < min)
            //    {
            //        max = 1;
            //        min = 0;
            //    }
            //    else if (max == min)
            //    {
            //        max = min + 0.1f;
            //    }

            //    WriteAttribute(writer, subPrefix, "Position", new Vector3(terrainSize.x * 0.5f, -min, terrainSize.z * 0.5f));

            //    StartCompoent(writer, subPrefix, "Terrain");
            //    //var folderAndName = _fileNameWithoutExtension + "/" +
            //    //                    Path.GetInvalidFileNameChars().Aggregate(obj.name, (_1, _2) => _1.Replace(_2, '_'));
            //    //var heightmapFileName = "Textures/Terrains/" + folderAndName + ".tga";
            //    //var materialFileName = "Materials/Terrains/" + folderAndName + ".xml";
            //    //WriteAttribute(subSubPrefix, "Height Map", "Image;" + heightmapFileName);

            //    //WriteAttribute(subSubPrefix, "Material", "Material;" + materialFileName);
            //    //WriteTerrainMaterial(terrain, Path.Combine(_assetsFolder, materialFileName));
            //    //WriteAttribute(subSubPrefix, "Vertex Spacing", new Vector3(terrainSize.x / w, (max - min), terrainSize.z / h));
            //    //Directory.CreateDirectory(Path.GetDirectoryName(Path.Combine(_assetsFolder, heightmapFileName)));
            //    //using (var imageFile = File.Open(Path.Combine(_assetsFolder, heightmapFileName), FileMode.Create, FileAccess.Write,
            //    //    FileShare.Read))
            //    //{
            //    //    using (var binaryWriter = new BinaryWriter(imageFile))
            //    //    {
            //    //        binaryWriter.Write((byte)0);
            //    //        binaryWriter.Write((byte)0);
            //    //        binaryWriter.Write((byte)3);
            //    //        binaryWriter.Write((short)0);
            //    //        binaryWriter.Write((short)0);
            //    //        binaryWriter.Write((byte)0);
            //    //        binaryWriter.Write((short)0);
            //    //        binaryWriter.Write((short)0);
            //    //        binaryWriter.Write((short)w);
            //    //        binaryWriter.Write((short)h);
            //    //        binaryWriter.Write((byte)8);
            //    //        binaryWriter.Write((byte)0);
            //    //        for (int y = h - 1; y >= 0; --y)
            //    //        {
            //    //            for (int x = 0; x < w; ++x)
            //    //            {
            //    //                var height = (heights[w - x - 1, y] - min) / (max - min) * 255.0f;
            //    //                binaryWriter.Write((byte)height);
            //    //            }
            //    //        }
            //    //    }
            //    //}
            //    EndElement(writer, subPrefix);
            //    EndElement(writer, subPrefix);
            //}

            if (lodGroup != null)
            {
                var lods = lodGroup.GetLODs();
                foreach (var lod in lods.Skip(1))
                    foreach (var renderer in lod.renderers)
                        localExcludeList.Add(renderer);
                //lods[0].renderers
            }

            if (meshRenderer != null && !localExcludeList.Contains(meshRenderer))
            {
                if (meshFilter != null)
                {
                    StartCompoent(writer, subPrefix, "StaticModel");

                    var sharedMesh = meshFilter.sharedMesh;
                    string meshPath;
                    if (_assets.TryGetMeshPath(sharedMesh, out meshPath))
                    {
                        WriteAttribute(writer, subSubPrefix, "Model", "Model;" + meshPath);
                    }

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
            }

            if (skinnedMeshRenderer != null && !localExcludeList.Contains(skinnedMeshRenderer))
            {
                StartCompoent(writer, subPrefix, "AnimatedModel");

                var sharedMesh = skinnedMeshRenderer.sharedMesh;
                string meshPath;
                if (_assets.TryGetMeshPath(sharedMesh, out meshPath))
                {
                    WriteAttribute(writer, subSubPrefix, "Model", "Model;" + meshPath);
                }

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

            if (meshCollider != null)
            {
            }

            foreach (Transform childTransform in obj.transform)
                if (childTransform.parent.gameObject == obj)
                    WriteObject(writer, subPrefix, childTransform.gameObject, localExcludeList);

            if (!string.IsNullOrEmpty(prefix))
                writer.WriteWhitespace(prefix);
            writer.WriteEndElement();
            writer.WriteWhitespace("\n");
        }

    }
}