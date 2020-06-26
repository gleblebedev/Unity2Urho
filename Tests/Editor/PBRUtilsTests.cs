using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Assert = UnityEngine.Assertions.Assert;

namespace UnityToCustomEngineExporter.Editor.Tests
{
    [TestFixture]
    public class PBRUtilsTests
    {
        [Test]
        public void ConvertPBRArguments()
        {
            var mr = new PBRUtils.MetallicRoughness();
            mr.baseColor = new Color(0.1f, 0.2f, 0.3f, 0.5f);
            mr.opacity = 0.5f;
            mr.roughness = 0.6f;
            mr.metallic = 0.4f;

            var sp = PBRUtils.ConvertToSpecularGlossiness(mr);

            var mr2 = PBRUtils.ConvertToMetallicRoughness(sp);

            Assert.AreApproximatelyEqual(mr.opacity, mr2.opacity, 1e-6f);
            Assert.AreApproximatelyEqual(mr.metallic, mr2.metallic, 1e-6f);
            Assert.AreApproximatelyEqual(mr.roughness, mr2.roughness, 1e-6f);
            Assert.AreApproximatelyEqual(mr.baseColor.a, mr2.baseColor.a, 1e-6f);
            Assert.AreApproximatelyEqual(mr.baseColor.r, mr2.baseColor.r, 1e-6f);
            Assert.AreApproximatelyEqual(mr.baseColor.g, mr2.baseColor.g, 1e-6f);
            Assert.AreApproximatelyEqual(mr.baseColor.b, mr2.baseColor.b, 1e-6f);
        }
    }
}