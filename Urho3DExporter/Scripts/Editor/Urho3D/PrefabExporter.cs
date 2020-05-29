using System.Collections.Generic;
using System.Linq;
using System.Xml;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace Urho3DExporter
{
    public class PrefabExporter : BaseNodeExporter, IExporter
    {

        public PrefabExporter(AssetCollection assets):base(assets)
        {
        }
        public void ExportAsset(AssetContext asset)
        {
            using (XmlTextWriter writer = asset.CreateXml())
            {
                if (writer == null)
                    return;
                var go = AssetDatabase.LoadAssetAtPath<GameObject>(asset.AssetPath);
                WriteObject(writer, "", go, new HashSet<Renderer>(), asset);
            }
        }
     
    }
}