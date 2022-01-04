using System;
using System.Globalization;
using System.Xml;
using UnityToCustomEngineExporter.Editor.Urho3D.Graph.ParticleNodes;
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
                writer.WriteAttributeString("value", ToString(val));
            }

            public string ToString(object val)
            {
                return string.Format(CultureInfo.InvariantCulture, "{0}", val);
            }
        }
        
        public class ResourceRefFormatter : IValueFormatter
        {
            public VariantType Type => VariantType.ResourceRef;

            public void WriteValue(XmlWriter writer, object val)
            {
                writer.WriteAttributeString("value", ToString(val));
            }

            public string ToString(object val)
            {
                var Value = (ResourceRef)val;
                return string.Format(CultureInfo.InvariantCulture, "{0};{1}", Value.Type, Value.Path);
            }
        }
        public class ResourceRefListFormatter : IValueFormatter
        {
            public VariantType Type => VariantType.ResourceRefList;

            public void WriteValue(XmlWriter writer, object val)
            {
                writer.WriteAttributeString("value", ToString(val));
            }

            public string ToString(object val)
            {
                var Value = (ResourceRefList)val;
                return string.Format(CultureInfo.InvariantCulture, "{0};{1}", Value.Type, string.Join(";", Value.Path));
            }
        }
        public class EmitFromFormatter : IValueFormatter
        {
            public VariantType Type => VariantType.Int;

            public void WriteValue(XmlWriter writer, object val)
            {
                writer.WriteAttributeString("value", ToString(val));
            }

            public string ToString(object val)
            {
                var Value = (int)(EmitFrom)val;
                return string.Format(CultureInfo.InvariantCulture, "{0}", Value);
            }
        }
        public class FaceCameraModeFormatter : IValueFormatter
        {
            public VariantType Type => VariantType.Int;

            public void WriteValue(XmlWriter writer, object val)
            {
                writer.WriteAttributeString("value", ToString(val));
            }

            public string ToString(object val)
            {
                var Value = (int)(FaceCameraMode)val;
                return string.Format(CultureInfo.InvariantCulture, "{0}", Value);
                //switch ((FaceCameraMode)val)
                //{
                //    case FaceCameraMode.None:
                //        return "None";
                //    case FaceCameraMode.RotateXYZ:
                //        return "Rotate XYZ";
                //    case FaceCameraMode.RotateY:
                //        return "Rotate Y";
                //    case FaceCameraMode.LookAtXYZ:
                //        return "LookAt XYZ";
                //    case FaceCameraMode.LookAtY:
                //        return "LookAt Y";
                //    case FaceCameraMode.LookAtMixed:
                //        return "LookAt Mixed";
                //    case FaceCameraMode.Direction:
                //        return "Direction";
                //    default:
                //        return string.Format(CultureInfo.InvariantCulture, "{0}", (FaceCameraMode)val);
                //}
            }
        }
        public class Vec2Formatter : IValueFormatter
        {
            public VariantType Type => VariantType.Vector2;

            public void WriteValue(XmlWriter writer, object val)
            {
                writer.WriteAttributeString("value", ToString(val));
            }

            public string ToString(object val)
            {
                var Value = (Vector2)val;
                return string.Format(CultureInfo.InvariantCulture, "{0} {1}", Value.x, Value.y);
            }
        }
        public class Vec3Formatter : IValueFormatter
        {
            public VariantType Type => VariantType.Vector3;

            public void WriteValue(XmlWriter writer, object val)
            {
                writer.WriteAttributeString("value", ToString(val));
            }

            public string ToString(object val)
            {
                var Value = (Vector3)val;
                return string.Format(CultureInfo.InvariantCulture, "{0} {1} {2}", Value.x, Value.y, Value.z);
            }
        }
        public class Vec4Formatter : IValueFormatter
        {
            public VariantType Type => VariantType.Vector4;

            public void WriteValue(XmlWriter writer, object val)
            {
                writer.WriteAttributeString("value", ToString(val));
            }

            public string ToString(object val)
            {
                var Value = (Vector4)val;
                return string.Format(CultureInfo.InvariantCulture, "{0} {1} {2} {3}", Value.x, Value.y, Value.z, Value.w);
            }
        }
        public class QuaternionFormatter : IValueFormatter
        {
            public VariantType Type => VariantType.Quaternion;

            public void WriteValue(XmlWriter writer, object val)
            {
                writer.WriteAttributeString("value", ToString(val));
            }

            public string ToString(object val)
            {
                var Value = (Quaternion)val;
                return string.Format(CultureInfo.InvariantCulture, "{0} {1} {2} {3}", Value.w, Value.x, Value.y,
                    Value.z);
            }
        }
        public class ColorFormatter : IValueFormatter
        {
            public VariantType Type => VariantType.Color;

            public void WriteValue(XmlWriter writer, object val)
            {
                writer.WriteAttributeString("value", ToString(val));
            }

            public string ToString(object val)
            {
                var Value = (Color)val;
                return string.Format(CultureInfo.InvariantCulture, "{0} {1} {2} {3}", Value.r, Value.g, Value.b, Value.a);
            }
        }
        public class Color32Formatter : IValueFormatter
        {
            public VariantType Type => VariantType.Color;

            public void WriteValue(XmlWriter writer, object val)
            {
                writer.WriteAttributeString("value", ToString(val));
            }

            public string ToString(object val)
            {
                Color Value = ((Color32)val);
                return string.Format(CultureInfo.InvariantCulture, "{0} {1} {2} {3}", Value.r, Value.g, Value.b,
                    Value.a);
            }
        }
        public class CurveFormatter : IValueFormatter
        {
            public VariantType Type => VariantType.VariantCurve;

            public void WriteValue(XmlWriter writer, object val)
            {
                ((GraphCurve)val).Write(writer);
            }

            public string ToString(object val)
            {
                throw new NotImplementedException();
            }
        }

        protected static bool BuildFormatters()
        {
            ValueFormatter<float>.Default = new SimpleFormatter(VariantType.Float);
            ValueFormatter<double>.Default = new SimpleFormatter(VariantType.Double);
            ValueFormatter<int>.Default = new SimpleFormatter(VariantType.Int);
            ValueFormatter<uint>.Default = new SimpleFormatter(VariantType.Int);
            ValueFormatter<bool>.Default = new SimpleFormatter(VariantType.Bool);
            ValueFormatter<Vector2>.Default = new Vec2Formatter();
            ValueFormatter<Vector3>.Default = new Vec3Formatter();
            ValueFormatter<Vector4>.Default = new Vec4Formatter();
            ValueFormatter<Quaternion>.Default = new QuaternionFormatter();
            ValueFormatter<Color>.Default = new ColorFormatter();
            ValueFormatter<Color32>.Default = new Color32Formatter();
            ValueFormatter<ResourceRef>.Default = new ResourceRefFormatter();
            ValueFormatter<ResourceRefList>.Default = new ResourceRefListFormatter();
            ValueFormatter<GraphCurve>.Default = new CurveFormatter();
            ValueFormatter<EmitFrom>.Default = new EmitFromFormatter();
            ValueFormatter<FaceCameraMode>.Default = new FaceCameraModeFormatter();
            return true;
        }
    }
}