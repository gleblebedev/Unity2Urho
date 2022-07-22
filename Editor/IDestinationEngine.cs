using System;
using System.Collections.Generic;
using UnityEditor.Experimental.SceneManagement;
using UnityEngine.SceneManagement;
using UnityToCustomEngineExporter.Editor.Urho3D;

namespace UnityToCustomEngineExporter.Editor
{
    public interface IDestinationEngine : IDisposable
    {
        void ExportScene(Scene scene);
        void ExportPrefab(PrefabStage prefabStage);

        IEnumerable<ProgressBarReport> ExportAssets(string[] assetGUIDs, PrefabContext prefabContext);

    }
}