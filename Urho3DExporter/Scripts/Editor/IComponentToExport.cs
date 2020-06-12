using System.Collections.Generic;

namespace Urho3DExporter
{
    public interface IComponentToExport
    {
        string GetExportType();

        IEnumerable<KeyValuePair<string, string>> GetAttributesToExport();
    }
}