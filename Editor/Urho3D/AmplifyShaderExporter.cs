using System;
using System.IO;
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
        }
        static readonly string _prefix = "/*ASEBEGIN";
        static readonly string _suffix = "ASEEND*/";

        public string EvaluateName(Shader shader, PrefabContext prefabContext)
        {
            return "Techniques/"+shader.name.Replace('/', '_') +".xml";
        }
    }
}