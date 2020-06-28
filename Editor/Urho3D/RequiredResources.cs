using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace UnityToCustomEngineExporter.Editor.Urho3D
{
    public static class RequiredResources
    {
        public static void Copy(Urho3DEngine engine)
        {
            var assetsPath = AssetDatabase.GUIDToAssetPath("bcc1b6196266be34e88c40110ba206ce");
            var rootPath = Path.GetDirectoryName(Application.dataPath) + Path.DirectorySeparatorChar;
            var sourceFolderPath = Path.Combine(rootPath, assetsPath) + Path.DirectorySeparatorChar;
            foreach (var file in Directory.GetFiles(sourceFolderPath, "*", SearchOption.AllDirectories))
            {
                if (string.Equals(Path.GetExtension(file), ".Meta", StringComparison.InvariantCultureIgnoreCase))
                    continue;
                var target = file.Substring(sourceFolderPath.Length).FixAssetSeparator();
                engine.TryCopyFile(file.Substring(rootPath.Length), target);
            }
        }
    }
}