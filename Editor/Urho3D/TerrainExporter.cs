using System;
using System.Globalization;
using System.IO;
using System.Linq;
using UnityEngine;

namespace UnityToCustomEngineExporter.Editor.Urho3D
{
    public class TerrainExporter
    {
        private readonly Urho3DEngine _engine;

        public TerrainExporter(Urho3DEngine engine)
        {
            _engine = engine;
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

        public string EvaluateHeightMap(TerrainData terrainData)
        {
            return ExportUtils.ReplaceExtension(ExportUtils.GetRelPathFromAsset(_engine.Options.Subfolder, terrainData),
                ".Heightmap.tga");
        }

        public string EvaluateWeightsMap(TerrainData terrainData)
        {
            return ExportUtils.ReplaceExtension(ExportUtils.GetRelPathFromAsset(_engine.Options.Subfolder, terrainData),
                ".Weights.tga");
        }

        public string EvaluateMaterial(TerrainData terrainData)
        {
            return ExportUtils.ReplaceExtension(ExportUtils.GetRelPathFromAsset(_engine.Options.Subfolder, terrainData),
                ".Material.xml");
        }

        public void ExportTerrain(TerrainData terrainData)
        {
            WriteTerrainMaterial(terrainData);
            WriteHeightMap(terrainData);
            WriteTerrainWeightsTexture(terrainData);
            ExportDetails(terrainData);

            //var folderAndName = tempFolder + "/" + Path.GetInvalidFileNameChars().Aggregate(obj.name, (_1, _2) => _1.Replace(_2, '_'));
            //var heightmapFileName = "Textures/Terrains/" + folderAndName + ".tga";
            //var materialFileName = "Materials/Terrains/" + folderAndName + ".xml";
        }

        private void ExportDetails(TerrainData terrainData)
        {
            return;

            for (var detailIndex = 0; detailIndex < terrainData.detailPrototypes.Length; detailIndex++)
            {
                var detailPrototype = terrainData.detailPrototypes[detailIndex];
                //detailPrototype.renderMode == DetailRenderMode.GrassBillboard

                //The Terrain system uses detail layer density maps. Each map is essentially a grayscale image where each pixel value denotes the number
                //of detail objects that will be procedurally placed terrain area. That corresponds to the pixel. Since several different detail types
                //may be used, the map is arranged into "layers" - the array indices of the layers are determined by the order of the detail types
                //defined in the Terrain inspector (ie, when the Paint Details tool is selected).
                var map = terrainData.GetDetailLayer(0, 0, terrainData.detailWidth, terrainData.detailHeight, detailIndex);
                for (var y = 0; y < terrainData.detailHeight; y++)
                {
                    for (var x = 0; x < terrainData.detailWidth; x++)
                    {
                        //The return value of each element [z,x] element is an int from 0-16, which // represent the number of details placed at that location. detailLayer[z,x]
                        //So, if you want to set the number of flowers at this location to 8, just set it to 8. It would be the same as painting flowers there with the strength setting set to .5 (8 = 1/2 of 16). 
                        var value = map[x, y];
                    }
                }
            }
        }

        private void WriteTerrainMaterial(TerrainData terrain)
        {
            using (var writer =
                _engine.TryCreateXml(terrain.GetKey(), EvaluateMaterial(terrain),
                    ExportUtils.GetLastWriteTimeUtc(terrain)))
            {
                if (writer == null)
                    return;

                var layers = terrain.terrainLayers;
                if (layers.Length > 3) layers = layers.Take(3).ToArray();

                writer.WriteStartElement("material");
                writer.WriteWhitespace(Environment.NewLine);
                {
                    writer.WriteStartElement("technique");
                    writer.WriteAttributeString("name", "Techniques/PBR/PBRTerrainBlend.xml");
                    writer.WriteEndElement();
                    writer.WriteWhitespace(Environment.NewLine);
                }
                {
                    writer.WriteStartElement("texture");
                    writer.WriteAttributeString("unit", "0");
                    writer.WriteAttributeString("name", EvaluateWeightsMap(terrain));
                    writer.WriteEndElement();
                    writer.WriteWhitespace(Environment.NewLine);
                }
                Vector2 detailTiling = new Vector2(1,1);
                for (var layerIndex = 0; layerIndex < layers.Length; ++layerIndex)
                {
                    var layer = layers[layerIndex];
                    detailTiling = new Vector2(terrain.size.x/layer.tileSize.x, terrain.size.z / layer.tileSize.y);

                    writer.WriteStartElement("texture");
                    writer.WriteAttributeString("unit", (layerIndex + 1).ToString(CultureInfo.InvariantCulture));
                    if (layer.diffuseTexture != null)
                    {
                        _engine.ScheduleTexture(layer.diffuseTexture);
                        var urhoAssetName = _engine.EvaluateTextrueName(layer.diffuseTexture);
                        if (!string.IsNullOrWhiteSpace(urhoAssetName))
                            writer.WriteAttributeString("name", urhoAssetName);
                    }

                    writer.WriteEndElement();
                    writer.WriteWhitespace(Environment.NewLine);
                }

                writer.WriteParameter("MatSpecColor", "0.0 0.0 0.0 16");
                writer.WriteParameter("DetailTiling", detailTiling);
                writer.WriteParameter("Roughness", "1");
                writer.WriteParameter("Metallic", "0");
                writer.WriteEndElement();
            }
        }

        private void WriteHeightMap(TerrainData terrain)
        {
            var w = terrain.heightmapResolution;
            var h = terrain.heightmapResolution;
            var max = float.MinValue;
            var min = float.MaxValue;
            var heights = terrain.GetHeights(0, 0, w, h);
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

            using (var imageFile = _engine.TryCreate(terrain.GetKey(), EvaluateHeightMap(terrain), DateTime.MaxValue))
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
        }

        private void WriteTerrainWeightsTexture(TerrainData terrain)
        {
            var layers = terrain.terrainLayers;
            var w = terrain.alphamapWidth;
            var h = terrain.alphamapHeight;
            var alphamaps = terrain.GetAlphamaps(0, 0, w, h);
            var numAlphamaps = alphamaps.GetLength(2);

            //Urho3D doesn't support more than 3 textures
            if (numAlphamaps > 3) numAlphamaps = 3;

            using (var imageFile = _engine.TryCreate(terrain.GetKey(), EvaluateWeightsMap(terrain), DateTime.MaxValue))
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
    }
}