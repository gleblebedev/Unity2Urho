using System;
using System.IO;
using System.Xml.Serialization;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityToCustomEngineExporter.Editor.Urho3D;

namespace UnityToCustomEngineExporter.Editor
{
    public class ExportTerrain : EditorWindow
    {
        private string _exportFolder = "C:\\Temp\\Terrain\\Data";

        [MenuItem("Tools/Export To Custom Engine/Export Terrain")]
        public static void Init()
        {
            var window = (EditorWindow)GetWindow<ExportTerrain>("Export Terrain");
            window.Show();
        }
        public void OnGUI()
        {
            _exportFolder = EditorGUILayout.TextField("Export Folder", _exportFolder);
            if (GUILayout.Button("Export")) Export();
        }

        private void Export()
        {
            var activeScene = SceneManager.GetActiveScene();
            foreach (var gameObject in activeScene.GetRootGameObjects())
            {
                var terrain = gameObject.GetComponentInChildren<Terrain>();
                if (terrain != null)
                {
                    Export(terrain);
                    return;
                }
            }
        }

        [Serializable]
        public class LayerMetadata
        {
            public Vector2 tileSize;
            public Vector2 tileOffset;
            public Color specular;
            public float metallic;
            public float smoothness;
            public float normalScale;
            public Vector4 diffuseRemapMin;
            public Vector4 diffuseRemapMax;
            public Vector4 maskMapRemapMin;
            public Vector4 maskMapRemapMax;
            public string diffuseTexture;
            public string normalMapTexture;
            public string maskMapTexture;

            public LayerMetadata()
            {

            }
            public LayerMetadata(TerrainLayer layer)
            {
                tileSize = layer.tileSize;
                tileOffset = layer.tileOffset;
                specular = layer.specular;
                metallic = layer.metallic;
                smoothness = layer.smoothness;
                normalScale = layer.normalScale;
                diffuseRemapMin = layer.diffuseRemapMin;
                diffuseRemapMax = layer.diffuseRemapMax;
                maskMapRemapMin = layer.maskMapRemapMin;
                maskMapRemapMax = layer.maskMapRemapMax;
                diffuseTexture = GetTexPath(layer.diffuseTexture);
                normalMapTexture = GetTexPath(layer.normalMapTexture);
                maskMapTexture = GetTexPath(layer.maskMapTexture);
            }

            public static string GetTexPath(Texture2D tex)
            {
                if (tex == null)
                    return null;
                return AssetDatabase.GetAssetPath(tex);
            }
        }
        private void Export(Terrain terrain)
        {
            var data = terrain.terrainData;
            var w = data.alphamapWidth;
            var h = data.alphamapHeight;
            var alphamaps = data.GetAlphamaps(0, 0, w, h);

            Directory.CreateDirectory(_exportFolder);

            using (var fileStream = File.Create(Path.Combine(_exportFolder, "heights.tga")))
            {
                TerrainExporter.SerializeHeightmapAsTga(fileStream, data);
            }

            var layers = data.terrainLayers;
            for (var index = 0; index < layers.Length; index++)
            {
                var layer = layers[index];
                var folder = Path.Combine(_exportFolder, index.ToString());
                Directory.CreateDirectory(folder);
                XmlSerializer serializer = new XmlSerializer(typeof(LayerMetadata));

                using (FileStream stream = new FileStream(Path.Combine(folder, "layer.xml"), FileMode.Create))
                {
                    serializer.Serialize(stream, new LayerMetadata(layer));
                    CopyTexture(layer.diffuseTexture, folder);
                    CopyTexture(layer.normalMapTexture, folder);
                    CopyTexture(layer.maskMapTexture, folder);
                }

                using (FileStream stream = new FileStream(Path.Combine(folder, "layer.tga"), FileMode.Create))
                {
                    using (var binaryWriter = new BinaryWriter(stream))
                    {
                        var bytesPerPixell = 4;
                        TerrainExporter.WriteTgaHeader(binaryWriter, bytesPerPixell * 8, w, h);
                        for (var y = h - 1; y >= 0; --y)
                        for (var x = 0; x < w; ++x)
                        {
                            var v = alphamaps[h - y - 1, x, index];
                            Color c = new Color(v, v, v, 1);
                            Color32 c32 = c;
                            binaryWriter.Write(c32.b);
                            binaryWriter.Write(c32.g);
                            binaryWriter.Write(c32.r);
                            binaryWriter.Write(c32.a);
                        }
                    }
                }
            }
            }

        private static void CopyTexture(Texture2D tex, string folder)
        {
            if (tex != null)
            {
                var path = Path.Combine(Path.GetDirectoryName(Application.dataPath), AssetDatabase.GetAssetPath(tex));
                var destFileName = Path.Combine(folder, Path.GetFileName(path));
                if (!File.Exists(destFileName))
                    File.Copy(path, destFileName);
            }
        }
    }
}