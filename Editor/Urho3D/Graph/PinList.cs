using System.Collections.Generic;

namespace UnityToCustomEngineExporter.Editor.Urho3D.Graph
{
    public class PinList<T> : List<T> where T : GraphPin
    {
        private readonly GraphNode _node;

        public PinList(GraphNode node)
        {
            _node = node;
        }
        public new void Add(T pin)
        {
            base.Add(pin);
            pin.Node = _node;
        }
    }
}