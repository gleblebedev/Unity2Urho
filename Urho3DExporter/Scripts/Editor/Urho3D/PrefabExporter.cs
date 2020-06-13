using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Urho3DExporter
{
    public class PrefabExporter : BaseNodeExporter, IExporter
    {
        public PrefabExporter(AssetCollection assets) : base(assets)
        {
        }

        public void ExportAsset(AssetContext asset)
        {
            using (var writer = asset.CreateXml())
            {
                if (writer == null)
                    return;
                var go = AssetDatabase.LoadAssetAtPath<GameObject>(asset.AssetPath);
                WriteObject(writer, "", go, new HashSet<Renderer>(), asset, false);
            }
        }
    }
}