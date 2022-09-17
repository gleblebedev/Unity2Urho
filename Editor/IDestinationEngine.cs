using System;
using System.Collections.Generic;
#if UNITY_2021_2_OR_NEWER
using UnityEditor.SceneManagement;
#else
using UnityEditor.Experimental.SceneManagement;
#endif
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