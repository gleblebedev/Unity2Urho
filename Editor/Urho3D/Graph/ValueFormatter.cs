using System;
using System.Globalization;
using System.Xml;
using Assets.Unity2Urho.Editor.Urho3D.Graph.ParticleNodes;
using UnityEngine;

namespace UnityToCustomEngineExporter.Editor.Urho3D.Graph
{
    public class ValueFormatter<T>: ValueFormatter
    {
        private static IValueFormatter _default;

        public static IValueFormatter Default
        {
            get
            {
                var v = _buildFormatters.Value;
                if (_default == null)
                    _default = new SimpleFormatter(VariantType.String);
                return _default;
            }
            set => _default = value;
        }
    }

    public class ValueFormatter
    {
        protected static Lazy<bool> _buildFormatters = new Lazy<bool>(BuildFormatters);
        public class SimpleFormatter : IValueFormatter
        {
            private readonly VariantType _type;

            public SimpleFormatter(VariantType type)
            {
                _type = type;
            }

            public VariantType Type => _type;

            public void WriteValue(XmlWriter writer, object val)
            {
                writer.WriteAttributeString("value", string.Format(CultureInfo.InvariantCulture, "{0}", val));
            }
        }
        
        public class ResourceRefFormatter : IValueFormatter
        {
            public VariantType Type => VariantType.ResourceRef;

            public void WriteValue(XmlWriter writer, object val)
            {
                var Value = (ResourceRef)val;
                writer.WriteAttributeString("value", string.Format(CultureInfo.InvariantCulture, "{0};{1}", Value.Type, Value.Path));
            }
        }
       
        public class Vec3Formatter : IValueFormatter
        {
            public VariantType Type => VariantType.Vector3;

            public void WriteValue(XmlWriter writer, object val)
            {
                var Value = (Vector3)val;
                writer.WriteAttributeString("value", string.Format(CultureInfo.InvariantCulture, "{0} {1} {2}", Value.x, Value.y, Value.z));
            }
        }
        public class QuaternionFormatter : IValueFormatter
        {
            public VariantType Type => VariantType.Quaternion;

            public void WriteValue(XmlWriter writer, object val)
            {
                var Value = (Quaternion)val;
                writer.WriteAttributeString("value", string.Format(CultureInfo.InvariantCulture, "{0} {1} {2} {3}", Value.w, Value.x, Value.y, Value.z));
            }
        }
        public class ColorFormatter : IValueFormatter
        {
            public VariantType Type => VariantType.Color;

            public void WriteValue(XmlWriter writer, object val)
            {
                var Value = (Color)val;
                writer.WriteAttributeString("value", string.Format(CultureInfo.InvariantCulture, "{0} {1} {2} {3}", Value.r, Value.g, Value.b, Value.a));
            }
        }
        public class Color32Formatter : IValueFormatter
        {
            public VariantType Type => VariantType.Color;

            public void WriteValue(XmlWriter writer, object val)
            {
                Color Value = ((Color32)val);
                writer.WriteAttributeString("value", string.Format(CultureInfo.InvariantCulture, "{0} {1} {2} {3}", Value.r, Value.g, Value.b, Value.a));
            }
        }
        public class CurveFormatter : IValueFormatter
        {
            public VariantType Type => VariantType.VariantCurve;

            public void WriteValue(XmlWriter writer, object val)
            {
                ((GraphCurve)val).Write(writer);
            }
        }

        protected static bool BuildFormatters()
        {
            ValueFormatter<float>.Default = new SimpleFormatter(VariantType.Float);
            ValueFormatter<double>.Default = new SimpleFormatter(VariantType.Double);
            ValueFormatter<int>.Default = new SimpleFormatter(VariantType.Int);
            ValueFormatter<uint>.Default = new SimpleFormatter(VariantType.Int);
            ValueFormatter<bool>.Default = new SimpleFormatter(VariantType.Bool);
            ValueFormatter<Vector3>.Default = new Vec3Formatter();
            ValueFormatter<Quaternion>.Default = new QuaternionFormatter();
            ValueFormatter<Color>.Default = new ColorFormatter();
            ValueFormatter<Color32>.Default = new Color32Formatter();
            ValueFormatter<ResourceRef>.Default = new ResourceRefFormatter();
            ValueFormatter<GraphCurve>.Default = new CurveFormatter();
            return true;
        }
    }
}