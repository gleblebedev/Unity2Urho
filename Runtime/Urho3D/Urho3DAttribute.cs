using System.Globalization;
using UnityEngine;

namespace UnityToCustomEngineExporter.Urho3D
{
    public struct Urho3DAttribute
    {
        public string Name { get; set; }
        public string Value { get; set; }

        public Urho3DAttribute(string name, string value)
        {
            Name = name;
            Value = value;
        }

        public Urho3DAttribute(string name, float value)
        {
            Name = name;
            Value = value.ToString(CultureInfo.InvariantCulture);
        }
        public Urho3DAttribute(string name, Vector3 value)
        {
            Name = name;
            Value = string.Format(CultureInfo.InvariantCulture, "{0} {1} {2}", value.x, value.y, value.z);
        }

        public Urho3DAttribute(string name, Vector4 value)
        {
            Name = name;
            Value = string.Format(CultureInfo.InvariantCulture, "{0} {1} {2} {3}", value.x, value.y, value.z, value.w);
        }

        public Urho3DAttribute(string name, Color value)
        {
            Name = name;
            Value = string.Format(CultureInfo.InvariantCulture, "{0} {1} {2} {3}", value.r, value.g, value.b, value.a);
        }

        public Urho3DAttribute(string name, Color32 value32)
        {
            Name = name;
            Color value = value32;
            Value = string.Format(CultureInfo.InvariantCulture, "{0} {1} {2} {3}", value.r, value.g, value.b, value.a);
        }
    }
}