using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
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
                ValueFormatter<float>.Default.WriteValue(writer, _value);
            }
        }
        public class ColorKeyframe : Keyframe
        {
            private readonly Color32 _value;

            public ColorKeyframe(float time, Color32 color) : base(time)
            {
                _value = color;
            }

            protected override void WriteValue(XmlWriter writer)
            {
                ValueFormatter<Color32>.Default.WriteValue(writer, _value);
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
        public GraphCurve(Gradient curve)
        {
            Type = VariantType.Color;

            var t = -1.0f;
            int colorIndex = 0;
            int alphaIndex = 0;
            var colorKeys = curve.colorKeys;
            var alphaKeys = curve.alphaKeys;
            Keyframes = new List<Keyframe>(colorKeys.Length + alphaKeys.Length);
            for (;;)
            {
                var nextTime = float.MaxValue;
                if (colorIndex < colorKeys.Length && colorKeys[colorIndex].time < nextTime)
                    nextTime = colorKeys[colorIndex].time;
                if (alphaIndex < alphaKeys.Length && alphaKeys[alphaIndex].time < nextTime)
                    nextTime = alphaKeys[alphaIndex].time;
                if (nextTime == float.MaxValue)
                    break;
                Keyframes.Add(new ColorKeyframe(nextTime, curve.Evaluate(nextTime)));
                if (colorIndex < colorKeys.Length && colorKeys[colorIndex].time <= nextTime)
                    ++colorIndex;
                if (alphaIndex < alphaKeys.Length && alphaKeys[alphaIndex].time <= nextTime)
                    ++alphaIndex;
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