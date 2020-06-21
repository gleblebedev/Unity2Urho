using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Assets.Scripts.UnityToCustomEngineExporter.Editor.Urho3D
{
    public class CubemapExporter
    {
        private readonly Urho3DEngine _engine;

        public CubemapExporter(Urho3DEngine engine)
        {
            _engine = engine;
        }
        public void ExportAsset(AssetContext asset)
        {
            if (!File.Exists(asset.FullPath))
            {
                Debug.LogError("File " + asset.FullPath + " not found");
                return;
            }
            var texture = AssetDatabase.LoadAssetAtPath<Cubemap>(asset.AssetPath);
            if (!EnsureReadableTexture(texture))
                return;

            using (var writer =
                asset.DestinationFolder.CreateXml(asset.UrhoAssetName, File.GetLastWriteTimeUtc(asset.FullPath)))
            {
                if (writer != null)
                {
                    var ddsName = asset.UrhoAssetName.Replace(".xml", ".dds");
                    DDS.SaveAsRgbaDds(texture, asset.DestinationFolder.GetTargetFilePath(ddsName));
                    writer.WriteStartDocument();
                    writer.WriteWhitespace(Environment.NewLine);
                    writer.WriteStartElement("cubemap");
                    writer.WriteStartElement("image");
                    writer.WriteAttributeString("name", Path.GetFileName(ddsName));
                    writer.WriteEndElement();
                    writer.WriteEndElement();
                    writer.WriteEndDocument();
                }
            }

        }

        public static bool EnsureReadableTexture(Cubemap texture)
        {
            if (null == texture) return false;

            var assetPath = AssetDatabase.GetAssetPath(texture);
            var tImporter = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (tImporter != null)
            {
                tImporter.textureType = TextureImporterType.Default;
                if (tImporter.isReadable != true)
                {
                    tImporter.isReadable = true;
                    AssetDatabase.ImportAsset(assetPath);
                    AssetDatabase.Refresh();
                }
                return true;
            }

            return false;
        }

        public string EvaluateCubemapName(Cubemap cubemap)
        {
            return AssetContext.ReplaceExt(AssetInfo.GetRelPathFromAsset(cubemap), ".dds");
        }
    }
}