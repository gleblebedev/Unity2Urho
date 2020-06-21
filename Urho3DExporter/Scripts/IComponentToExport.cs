using System.Collections.Generic;

namespace Assets.Scripts.UnityToCustomEngineExporter.Editor
{
    public interface IComponentToExport
    {
        string GetExportType();

        IEnumerable<KeyValuePair<string, string>> GetAttributesToExport();
    }
}