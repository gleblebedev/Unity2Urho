using System;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using UnityToCustomEngineExporter.Editor.Urho3D;

namespace UnityToCustomEngineExporter.Editor
{
    public interface IDestinationEngine : IDisposable
    {
        void ExportScene(Scene scene);

        IEnumerable<ProgressBarReport> ExportAssets(string[] assetGUIDs, PrefabContext prefabContext);
    }
}