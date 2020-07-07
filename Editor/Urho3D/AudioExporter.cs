using UnityEditor;
using UnityEngine;

namespace UnityToCustomEngineExporter.Editor.Urho3D
{
    public class AudioExporter
    {
        private readonly Urho3DEngine _engine;

        public AudioExporter(Urho3DEngine engine)
        {
            _engine = engine;
        }

        public void ExportClip(AudioClip audioClip)
        {
            var relPath = ExportUtils.GetRelPathFromAsset(_engine.Subfolder, audioClip);
            var newName = EvaluateAudioClipName(audioClip);
            _engine.TryCopyFile(AssetDatabase.GetAssetPath(audioClip), newName);
        }

        public string EvaluateAudioClipName(AudioClip audioClip)
        {
            if (audioClip == null)
                return null;
            var assetPath = AssetDatabase.GetAssetPath(audioClip);
            if (string.IsNullOrWhiteSpace(assetPath))
                return null;
            return ExportUtils.GetRelPathFromAssetPath(_engine.Subfolder, assetPath);
        }
    }
}