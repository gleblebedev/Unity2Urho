using NUnit.Framework;
using Assert = UnityEngine.Assertions.Assert;

namespace UnityToCustomEngineExporter.Editor.Tests
{
    [TestFixture]
    public class ExportUtilsTests
    {
        [Test]
        public void Combine_EmptyStrings_ResultEmpty()
        {
            var result = ExportUtils.Combine("", "", "");
            Assert.AreEqual("", result);
        }

        [Test]
        public void Combine_TwoStrings_ResultSeparated()
        {
            var result = ExportUtils.Combine("A", "B");
            Assert.AreEqual("A/B", result);
        }

        [Test]
        public void Combine_TwoStringsWithSeparators_ResultSeparated()
        {
            var result = ExportUtils.Combine("A/", "B/");
            Assert.AreEqual("A/B/", result);
        }

        [Test]
        public void Combine_TreeStringsWithSeparators_ResultSeparated()
        {
            var result = ExportUtils.Combine("A/", "B/", "C/");
            Assert.AreEqual("A/B/C/", result);
        }
    }
}