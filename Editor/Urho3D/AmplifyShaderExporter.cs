using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace UnityToCustomEngineExporter.Editor.Urho3D
{
    [CustomUrho3DExporter(typeof(Shader))]
    public class AmplifyShaderExporter
    {
        private readonly Urho3DEngine _engine;

        public AmplifyShaderExporter(Urho3DEngine engine)
        {
            _engine = engine;
        }

        public static string ReadShaderText(Shader shader)
        {
            var shaderPath = AssetDatabase.GetAssetPath(shader);
            if (string.IsNullOrWhiteSpace(shaderPath))
                return null;
            var path = Path.Combine(Application.dataPath, "..", shaderPath);
            if (!File.Exists(path))
                return null;
            return File.ReadAllText(path);
        }
        public void ExportShader(Shader shader, PrefabContext prefabContext)
        {
            string src = ReadShaderText(shader);
            if (string.IsNullOrWhiteSpace(src))
            {
                return;
            }

            var startIndex = src.IndexOf(_prefix);
            if (startIndex < 0)
                return;
            startIndex += _prefix.Length;
            var endIndex = src.IndexOf(_suffix, startIndex);
            if (endIndex < 0)
                return;

            var lines = src.Substring(startIndex, endIndex - startIndex).Trim().Split(new[]{ '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

            ParseGraph(lines);
        }

        private bool ParseGraph(IEnumerable<string> lines)
        {
            var versionLine = lines.FirstOrDefault();
            _version = 0;
            foreach (var line in lines)
            {
                if (line.StartsWith(_versionPrefix))
                {
                    _version = int.Parse(versionLine.Substring(_versionPrefix.Length), CultureInfo.InvariantCulture);
                }
                else
                {
                    var e = new SequenceReader(line).GetEnumerator();
                    if (e.MoveNext())
                    {
                        var type = e.Current;
                        switch (type)
                        {
                            case "Node":
                                ParseNode(e);
                                break;
                            case "WireConnection":
                                ParseWireConnection(e);
                                break;
                        }
                    }
                }
            }
            return true;
        }

        private bool ParseWireConnection(IEnumerator<string> parameters)
        {
            if (!parameters.MoveNext()) return false;
            var inputNode = parameters.Current.AsInt();
            if (!parameters.MoveNext()) return false;
            var inputPin = parameters.Current.AsInt();
            if (!parameters.MoveNext()) return false;
            var outputNode = parameters.Current.AsInt();
            if (!parameters.MoveNext()) return false;
            var outputPin = parameters.Current.AsInt();
            return true;
        }

        private bool ParseNode(IEnumerator<string> parameters)
        {
            if (!parameters.MoveNext()) return false;
            var nodeName = parameters.Current;
            if (!parameters.MoveNext()) return false;
            var nodeId = parameters.Current.AsInt();
            if (!parameters.MoveNext()) return false;
            var coords = parameters.Current;
            if (_version > 22)
            {
                if (!parameters.MoveNext()) return false;
                var precision = parameters.Current;
            }

            if (_version > 5004)
            {
                if (!parameters.MoveNext()) return false;
                var show = parameters.Current;
            }

            return true;
        }

        static readonly string _versionPrefix = "Version=";
        static readonly string _prefix = "/*ASEBEGIN";
        static readonly string _suffix = "ASEEND*/";
        private int _version;

        public string EvaluateName(Shader shader, PrefabContext prefabContext)
        {
            return "Techniques/"+shader.name.Replace('/', '_') +".xml";
        }
    }
}