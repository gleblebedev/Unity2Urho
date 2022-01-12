using System;
using System.Globalization;
using System.Linq;
using System.Xml;

namespace UnityToCustomEngineExporter.Editor.Urho3D.Graph
{
    public class GraphInPin: GraphPin
    {
        public GraphInPin(string name, VariantType type = VariantType.None) : base(name)
        {
            Type = type;
        }

        public GraphInPin(string name, GraphOutPin pin) : base(name)
        {
            TargetPin = pin;
        }

        public GraphInPin(string name, VariantType type, GraphNode target) : base(name)
        {
            Type = type;
            Connect(target);
        }
        public GraphInPin(string name, VariantType type, GraphOutPin target) : base(name)
        {
            Type = type;
            TargetPin = target;
        }
        public GraphInPin(string name, GraphNode target) : base(name)
        {
            Connect(target);
        }

        public GraphOutPin TargetPin { get; set; }
        public VariantType Type { get; set; }

        public string Value { get; set; }
        public override void WriteAttributes(XmlWriter writer)
        {
            if (Type != VariantType.None)
            {
                writer.WriteAttributeString("type", Type.ToString());
            }
            if (TargetPin != null)
            {
                if (TargetPin.Node == null)
                {

                }
                writer.WriteAttributeString("node", TargetPin.Node.Id.ToString(CultureInfo.InvariantCulture));
                writer.WriteAttributeString("pin", TargetPin.Name);
            }
            else if (!string.IsNullOrWhiteSpace(Value))
            {
                writer.WriteAttributeString("value", Value);
            }
        }

        public void Connect(GraphNode target)
        {
            TargetPin = target.Out.FirstOrDefault();
            if (TargetPin == null)
            {
                throw new ArgumentException($"No output pins in {target.Name}");
            }
        }

        public void Connect(GraphOutPin target)
        {
            TargetPin = target;
        }
    }
}