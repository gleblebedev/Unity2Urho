using UnityEditor;
using UnityEngine;

namespace UnityToCustomEngineExporter.Editor.Urho3D
{
    public class PrefabContext
    {
        private readonly Urho3DEngine _engine;
        private readonly string _defaultFolder;
        private readonly GameObject _prefabRoot;
        private string _prefabFolder;

        public PrefabContext(Urho3DEngine engine, GameObject prefabRoot, string path, string defaultFolder = null)
        {
            _engine = engine;
            _prefabRoot = prefabRoot;
            TempFolder = path;
            _defaultFolder = defaultFolder ?? TempFolder;
        }

        public GameObject PrefabRoot
        {
            get
            {
                return _prefabRoot;
            }
        }

        public string DefaultFolder => _defaultFolder;

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

        public PrefabContext RetargetToRoot(GameObject root)
        {
            if (root == null) return new PrefabContext(_engine, null, _defaultFolder);


            var assetPath = AssetDatabase.GetAssetPath(root);
            if (string.IsNullOrWhiteSpace(assetPath))
                assetPath = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(root);
            var relPathFromAssetPath = ExportUtils.GetRelPathFromAssetPath(_engine.Options.Subfolder,
                ExportUtils.ReplaceExtension(assetPath, ""));
            return new PrefabContext(_engine, root, relPathFromAssetPath, _defaultFolder);
        }

        public PrefabContext Retarget(GameObject gameObject)
        {
            if (gameObject == _prefabRoot) return this;
            if (gameObject == null) return this;
            var root = PrefabUtility.GetNearestPrefabInstanceRoot(gameObject);
            if (root == _prefabRoot) return this;
            return RetargetToRoot(root);
        }
    }
}