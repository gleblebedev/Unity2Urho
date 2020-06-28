using System;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

namespace UnityToCustomEngineExporter.Editor
{
    public interface IDestinationEngine : IDisposable
    {
        void ExportScene(Scene scene);

        IEnumerable<ProgressBarReport> ExportAssets(string[] assetGUIDs);
    }
}