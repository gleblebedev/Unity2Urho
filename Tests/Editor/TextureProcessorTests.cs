using System.IO;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace UnityToCustomEngineExporter.Editor.Tests
{
    [TestFixture]
    public class TextureProcessorTests
    {
        [Test]
        public void Copy_SRGB_WithProfile()
        {
            var tp = new TextureProcessor();
            tp.ProcessTexture(LoadTexture("a9d943a416fe4ae489e32436d1c7f9ae"), "Hidden/UnityToCustomEngineExporter/Urho3D/Copy",
                texture =>
                {
                    var pixels = texture.GetPixels32(0);
                    Assert.AreEqual(new Color32(66, 128, 192, 255), pixels[0]);
                });
        }

        [Test]
        public void Copy_SRGB_WithNoProfile()
        {
            var tp = new TextureProcessor();
            tp.ProcessTexture(LoadTexture("6462cded15de8e34f81c10f215529d00"), "Hidden/UnityToCustomEngineExporter/Urho3D/Copy",
                texture =>
                {
                    var pixels = texture.GetPixels32(0);
                    Assert.AreEqual(new Color32(63, 128, 192, 255), pixels[0]);
                });
        }

        [Test]
        public void Copy_Linear_WithNoProfile()
        {
            var tp = new TextureProcessor();
            tp.ProcessTexture(LoadTexture("6caa426259ccb414abd1378d13ca59e3"), "Hidden/UnityToCustomEngineExporter/Urho3D/Copy",
                texture =>
                {
                    var pixels = texture.GetPixels32(0);
                    Assert.AreEqual(new Color32(63, 128, 192, 255), pixels[0]);
                });
        }

        [Test]
        public void Copy_Normal()
        {
            var tp = new TextureProcessor();
            tp.ProcessTexture(LoadTexture("dcffc39e6a9542d4281f38d8f8d5a12e"), "Hidden/UnityToCustomEngineExporter/Urho3D/Copy",
                texture =>
                {
                    var pixels = texture.GetPixels32(0);
                    Assert.AreEqual(new Color32(255, 128, 129, 64), pixels[0]);
                });
        }

        [Test]
        public void DecodeNormalMap()
        {
            var tp = new TextureProcessor();
            tp.ProcessTexture(LoadTexture("dcffc39e6a9542d4281f38d8f8d5a12e"), "Hidden/UnityToCustomEngineExporter/Urho3D/DecodeNormalMap",
                texture =>
                {
                    var pixels = texture.GetPixels32(0);
                    Assert.AreEqual(new Color32(64, 128, 238, 255), pixels[0]);
                });
        }

        [Test]
        public void DecodeNormalMapPackedNormal()
        {
            var tp = new TextureProcessor();
            tp.ProcessTexture(LoadTexture("dcffc39e6a9542d4281f38d8f8d5a12e"), "Hidden/UnityToCustomEngineExporter/Urho3D/DecodeNormalMapPackedNormal",
                texture =>
                {
                    var pixels = texture.GetPixels32(0);
                    Assert.AreEqual(new Color32(128, 128, 128, 64), pixels[0]);
                });
        }
        public Texture2D LoadTexture(string guid)
        {
            var guidToAssetPath = AssetDatabase.GUIDToAssetPath(guid);
            if (string.IsNullOrEmpty(guidToAssetPath))
                throw new FileNotFoundException($"Asset {guid} not found");
            return AssetDatabase.LoadAssetAtPath<Texture2D>(guidToAssetPath);
        }
    }
}