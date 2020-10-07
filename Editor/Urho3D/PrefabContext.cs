using UnityEditor;
using UnityEngine;

namespace UnityToCustomEngineExporter.Editor.Urho3D
{
    public class PrefabContext
    {
        private readonly Urho3DEngine _engine;
        private string _prefabFolder;
        private readonly string _defaultFolder;
        private readonly GameObject _prefabRoot;

        public PrefabContext(Urho3DEngine engine, GameObject prefabRoot, string path, string defaultFolder = null)
        {
            _engine = engine;
            _prefabRoot = prefabRoot;
            TempFolder = path;
            _defaultFolder = defaultFolder ?? TempFolder;
        }

        public string TempFolder
        {
            get => _prefabFolder;
            private set
            {
                _prefabFolder = (value ?? "").FixAssetSeparator().Trim('/');
                if (!string.IsNullOrWhiteSpace(_prefabFolder))
                    _prefabFolder += "/";
            }
        }

        public PrefabContext Retarget(GameObject gameObject)
        {
            var root = PrefabUtility.GetNearestPrefabInstanceRoot(gameObject);
            if (root == _prefabRoot) return this;
            if (root == null) return new PrefabContext(_engine, null, _defaultFolder);
            var assetPath = AssetDatabase.GetAssetPath(root);
            if (string.IsNullOrWhiteSpace(assetPath))
                assetPath = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(gameObject);
            var relPathFromAssetPath = ExportUtils.GetRelPathFromAssetPath(_engine.Options.Subfolder,
                ExportUtils.ReplaceExtension(assetPath, ""));
            return new PrefabContext(_engine, root, relPathFromAssetPath, _defaultFolder);
        }
    }
}