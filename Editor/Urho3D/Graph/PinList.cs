using System;
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
        public new T Add(T pin)
        {
            base.Add(pin);
            pin.Node = _node;
            return pin;
        }

        public T this[string name]
        {
            get
            {
                foreach (var pin in this)
                {
                    if (pin.Name == name)
                        return pin;
                }

                throw new KeyNotFoundException(name + " pin not found");
            }
        }
    }
}