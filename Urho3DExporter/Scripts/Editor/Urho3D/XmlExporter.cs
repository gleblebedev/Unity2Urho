using System.IO;
using System.Text;
using System.Xml;

namespace Urho3DExporter
{
    public class XmlExporter
    {
        protected XmlTextWriter CreateXmlFile(AssetContext asset)
        {
            var file = ExportAssets.CreateFile(asset.UrhoFileName);
            return new XmlTextWriter(file, new UTF8Encoding(false));
        }
    }
}