using System.IO;

namespace UnityToCustomEngineExporter.Editor
{
    internal static class ExtensionMethods
    {
        internal static string FixDirectorySeparator(this string path)
        {
            if (path == null)
                return null;
            if (Path.DirectorySeparatorChar == '/')
                return path.Replace('\\', Path.DirectorySeparatorChar);
            return path.Replace('/', Path.DirectorySeparatorChar);
        }

        internal static string FixAssetSeparator(this string path)
        {
            if (path == null)
                return null;
            if (Path.DirectorySeparatorChar == '/')
                return path.Replace('\\', '/');
            return path.Replace(Path.DirectorySeparatorChar, '/');
        }
    }
}