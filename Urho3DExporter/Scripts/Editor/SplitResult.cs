using System.Collections.Generic;

namespace Urho3DExporter
{
    public struct SplitResult<T>
    {
        public List<T> Selected;
        public List<T> Rejected;
    }
}