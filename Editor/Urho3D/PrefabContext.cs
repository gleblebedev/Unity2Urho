namespace UnityToCustomEngineExporter.Editor.Urho3D
{
    public class PrefabContext
    {
        private string _tempFolder;

        public string TempFolder
        {
            get => _tempFolder;
            set
            {
                _tempFolder = (value ?? "").FixAssetSeparator().Trim('/');
                if (!string.IsNullOrWhiteSpace(_tempFolder))
                    _tempFolder += "/";
            }
        }

    }
}