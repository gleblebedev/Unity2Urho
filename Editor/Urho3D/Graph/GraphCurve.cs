using System;
using System.Collections.Generic;
using System.Globalization;
using System.Xml;
using UnityEngine;

namespace UnityToCustomEngineExporter.Editor.Urho3D.Graph
{
    public class GraphCurve
    {
        public abstract class Keyframe
        {
            private readonly float _time;

            protected Keyframe(float time)
            {
                _time = time;
            }

            public void Write(XmlWriter writer)
            {
                writer.WriteWhitespace(Environment.NewLine);
                writer.WriteWhitespace("\t\t\t\t\t");
                writer.WriteStartElement("keyframe");
                writer.WriteAttributeString("time", string.Format(CultureInfo.InvariantCulture, "{0}", _time));
                WriteValue(writer);
                writer.WriteEndElement();
            }

            protected abstract void WriteValue(XmlWriter writer);
        }
        public class FloatKeyframe: Keyframe
        {
            private readonly float _value;
            private readonly float _inTangent;
            private readonly float _outTangent;

            public FloatKeyframe(UnityEngine.Keyframe keyframe, float scale):base(keyframe.time)
            {
                _value = keyframe.value*scale;
                _inTangent = keyframe.inTangent * scale;
                _outTangent = keyframe.outTangent * scale;
            }

            protected override void WriteValue(XmlWriter writer)
            {
                writer.WriteAttributeString("value", string.Format(CultureInfo.InvariantCulture, "{0}", _value));
            }
        }
        public VariantType Type { get; }
        public List<Keyframe> Keyframes { get; }

        public GraphCurve(AnimationCurve curve, float scale = 1.0f)
        {
            Type = VariantType.Float;
            var keys = curve.keys;
            Keyframes = new List<Keyframe>(keys.Length);
            foreach (var keyframe in keys)
            {
                Keyframes.Add(new FloatKeyframe(keyframe, scale));
            }
        }

        public void Write(XmlWriter writer)
        {
            writer.WriteWhitespace(Environment.NewLine);
            writer.WriteWhitespace("\t\t\t\t\t");
            writer.WriteStartElement("value");
            writer.WriteAttributeString("name", "");
            writer.WriteAttributeString("type", Type.ToString());
            writer.WriteAttributeString("interpolation", "linear");
            writer.WriteAttributeString("splineTension", "0.5");
            {
                writer.WriteStartElement("keyframes");
                foreach (var key in Keyframes)
                {
                    key.Write(writer);
                }
                writer.WriteEndElement();
            }
            writer.WriteWhitespace(Environment.NewLine);
            writer.WriteWhitespace("\t\t\t\t\t");
            writer.WriteEndElement();
            writer.WriteWhitespace(Environment.NewLine);
            writer.WriteWhitespace("\t\t\t\t");
        }
    }
}