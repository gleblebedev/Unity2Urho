using System.Collections.Generic;

namespace UnityToCustomEngineExporter
{
    public interface IComponentToExport
    {
        string GetExportType();

        IEnumerable<KeyValuePair<string, string>> GetAttributesToExport();
    }
}