using System.Collections.Generic;

namespace Assets.Scripts.UnityToCustomEngineExporter.Editor
{
    public struct SplitResult<T>
    {
        public List<T> Selected;
        public List<T> Rejected;
    }
}