using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace UnityToCustomEngineExporter.Editor.Urho3D
{
    public class TextureNameBuilder
    {
        public Urho3DEngine Engine { get; }

        private string _basePath;

        private HashSet<Texture> _visitedTextures = new HashSet<Texture>();
        private StringBuilder _name = new StringBuilder();

        public TextureNameBuilder(Urho3DEngine engine)
        {
            Engine = engine;
        }

        public TextureNameBuilder WithTexture(Texture texture)
        {
            if (texture == null)
                return this;

            if (!_visitedTextures.Add(texture))
                return this;
            
            var name = Engine.EvaluateTextrueName(texture);
            if (_basePath == null)
            {
                _basePath = Path.GetDirectoryName(name).FixAssetSeparator();
            }

            return Append(Path.GetFileNameWithoutExtension(name));
        }

        public TextureNameBuilder WithFloat(float value, float defaultValue = 0.0f, float eps = 1e-3f)
        {
            if (Math.Abs(value-defaultValue) <= eps)
                return this;

            return Append(value.ToString("{0:0.000}", CultureInfo.InvariantCulture));
        }

        public TextureNameBuilder Append(string str)
        {
            if (!string.IsNullOrWhiteSpace(str))
            {
                if (_name.Length > 0)
                    _name.Append(".");
                _name.Append(str);
            }
            return this;
        }

        public string Build()
        {
            var sb = new StringBuilder();
            if (!string.IsNullOrWhiteSpace(_basePath))
            {
                sb.Append(_basePath);
                sb.Append("/");
            }
            sb.Append(_name);
            return sb.ToString();
        }

        public override string ToString()
        {
            return Build();
        }
    }

    public abstract class AbstractMaterialExporter
    {
        protected AbstractMaterialExporter(Urho3DEngine engine)
        {
            if (engine == null)
                throw new ArgumentNullException(nameof(engine));
            Engine = engine;
        }

        public abstract int ExporterPriority { get; }
        public Urho3DEngine Engine { get; }

        public void FixNormalScale(UrhoPBRMaterial urhoMaterial, ShaderArguments arguments)
        {
            if (Engine.Options.RBFX)
            {
                urhoMaterial.NormalScale = arguments.BumpScale;
                arguments.BumpScale = 1.0f;
            }
        }

        public static void WriteMaterial(XmlWriter writer, UrhoPBRMaterial urhoMaterial, PrefabContext prefabContext)
        {
            writer.WriteStartElement("material");
            writer.WriteWhitespace(Environment.NewLine);

            WriteTechnique(writer, urhoMaterial.Technique);

            WriteTexture(urhoMaterial.BaseColorTexture, writer, "diffuse", prefabContext);
            WriteTexture(urhoMaterial.NormalTexture, writer, "normal", prefabContext);
            WriteTexture(urhoMaterial.MetallicRoughnessTexture, writer, "specular", prefabContext);
            if (!string.IsNullOrWhiteSpace(urhoMaterial.EmissiveTexture))
                WriteTexture(urhoMaterial.EmissiveTexture, writer, "emissive", prefabContext);
            else
                WriteTexture(urhoMaterial.AOTexture, writer, "emissive", prefabContext);

            for (var index = 0; index < urhoMaterial.TextureUnits.Count; index++)
                if (!string.IsNullOrWhiteSpace(urhoMaterial.TextureUnits[index]))
                    WriteTexture(urhoMaterial.TextureUnits[index], writer, index, prefabContext);

            writer.WriteParameter("MatEmissiveColor", urhoMaterial.EmissiveColor);
            writer.WriteParameter("MatDiffColor", urhoMaterial.BaseColor);
            writer.WriteParameter("MatEnvMapColor", urhoMaterial.MatEnvMapColor);
            writer.WriteParameter("MatSpecColor", urhoMaterial.MatSpecColor);
            writer.WriteParameter("Roughness", urhoMaterial.Roughness);
            writer.WriteParameter("Metallic", urhoMaterial.Metallic);
            writer.WriteParameter("UOffset", urhoMaterial.UOffset);
            writer.WriteParameter("VOffset", urhoMaterial.VOffset);
            writer.WriteElementParameter("cull", "value", urhoMaterial.Cull.ToString());
            writer.WriteElementParameter("shadowcull", "value", urhoMaterial.ShadowCull.ToString());

            foreach (var extraParameter in urhoMaterial.ExtraParameters)
            {
                var val = extraParameter.Value;
                if (val is float floatValue)
                    writer.WriteParameter(extraParameter.Key, floatValue);
                else if (val is Vector2 vec2Value)
                    writer.WriteParameter(extraParameter.Key, vec2Value);
                else if (val is Vector3 vec3Value)
                    writer.WriteParameter(extraParameter.Key, vec3Value);
                else if (val is Vector4 vec4Value)
                    writer.WriteParameter(extraParameter.Key, vec4Value);
                else if (val is Quaternion quatValue)
                    writer.WriteParameter(extraParameter.Key, quatValue);
                else if (val is Color colorValue)
                    writer.WriteParameter(extraParameter.Key, colorValue);
                else if (val is Color32 color32Value)
                    writer.WriteParameter(extraParameter.Key, color32Value);
                else if (val is string strValue) writer.WriteParameter(extraParameter.Key, strValue);
            }

            if (urhoMaterial.PixelShaderDefines.Count != 0 || urhoMaterial.VertexShaderDefines.Count != 0)
            {
                WriteShader(writer, urhoMaterial.PixelShaderDefines, urhoMaterial.VertexShaderDefines);
            }

            writer.WriteEndElement();
        }

        protected static void WriteTechnique(XmlWriter writer, string name)
        {
            writer.WriteWhitespace("\t");
            writer.WriteStartElement("technique");
            writer.WriteAttributeString("name", name);
            writer.WriteEndElement();
            writer.WriteWhitespace(Environment.NewLine);
        }
        protected static void WriteShader(XmlWriter writer, IEnumerable<string> psdefines, IEnumerable<string> vsdefines)
        {
            writer.WriteWhitespace("\t");
            writer.WriteStartElement("shader");
            if (psdefines != null && psdefines.Any())
                writer.WriteAttributeString("psdefines", string.Join(" ", psdefines));
            if (vsdefines != null && vsdefines.Any())
                writer.WriteAttributeString("vsdefines", string.Join(" ", vsdefines));
            writer.WriteEndElement();
            writer.WriteWhitespace(Environment.NewLine);
        }

        protected static bool WriteTexture(string urhoAssetName, XmlWriter writer, string name,
            PrefabContext prefabContext)
        {
            if (string.IsNullOrWhiteSpace(urhoAssetName))
                return false;
            {
                writer.WriteWhitespace("\t");
                writer.WriteStartElement("texture");
                writer.WriteAttributeString("unit", name);
                writer.WriteAttributeString("name", urhoAssetName);
                writer.WriteEndElement();
                writer.WriteWhitespace(Environment.NewLine);
            }
            return true;
        }

        protected static bool WriteTexture(string urhoAssetName, XmlWriter writer, int unit,
            PrefabContext prefabContext)
        {
            return WriteTexture(urhoAssetName, writer, unit.ToString(CultureInfo.InvariantCulture), prefabContext);
        }

        public abstract bool CanExportMaterial(Material material);

        public abstract void ExportMaterial(Material material, PrefabContext prefabContext);

        public virtual string EvaluateMaterialName(Material material, PrefabContext context)
        {
            if (material == null)
                return null;
            var assetPath = AssetDatabase.GetAssetPath(material);
            if (string.IsNullOrWhiteSpace(assetPath))
                assetPath = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(material);
            if (string.IsNullOrWhiteSpace(assetPath))
                assetPath = ExportUtils.Combine(context.TempFolder, material.name + ".mat");
            if (assetPath.EndsWith(".mat", StringComparison.InvariantCultureIgnoreCase))
                return ExportUtils.ReplaceExtension(
                    ExportUtils.GetRelPathFromAssetPath(Engine.Options.Subfolder, assetPath),
                    ".xml");
            var newExt = "/" + ExportUtils.SafeFileName(Engine.DecorateName(ExportUtils.GetName(Engine.NameCollisionResolver, material))) + ".xml";
            return ExportUtils.ReplaceExtension(
                ExportUtils.GetRelPathFromAssetPath(Engine.Options.Subfolder, assetPath),
                newExt);
        }

        protected virtual void SetupFlags(Material material, ShaderArguments arguments)
        {
            arguments.Shader = material.shader.name;
            arguments.Transparent = material.renderQueue == (int) RenderQueue.Transparent;
            arguments.AlphaTest = material.renderQueue == (int) RenderQueue.AlphaTest;
            arguments.HasEmission = material.IsKeywordEnabled("_EMISSION");
            if (material.HasProperty("_MainTex"))
            {
                arguments.MainTextureOffset = material.mainTextureOffset;
                arguments.MainTextureScale = material.mainTextureScale;
            }
        }

        protected Texture GetTexture(Material material, string propertyName)
        {
            if (!material.HasProperty(propertyName)) return null;

            return material.GetTexture(propertyName);
        }

        protected float GetFloat(Material material, string propertyName, float defaultValue)
        {
            if (!material.HasProperty(propertyName)) return defaultValue;

            return material.GetFloat(propertyName);
        }

        protected string GetScaledNormalTextureName(Texture bump, float bumpScale)
        {
            var normalTexture = Engine.EvaluateTextrueName(bump);
            if (bumpScale < 0.999f)
                normalTexture = ExportUtils.ReplaceExtension(normalTexture,
                    string.Format(CultureInfo.InvariantCulture, "{0:0.000}.dds", bumpScale));

            return normalTexture;
        }


        protected string FormatRGB(Color32 color)
        {
            return string.Format("{0:x2}{1:x2}{2:x2}", color.r, color.g, color.b);
        }

        protected bool WriteTexture(Texture texture, XmlWriter writer, string name, PrefabContext prefabContext)
        {
            Engine.ScheduleAssetExport(texture, prefabContext);
            var urhoAssetName = Engine.EvaluateTextrueName(texture);
            return WriteTexture(urhoAssetName, writer, name, prefabContext);
        }

        protected void WriteCommonParameters(XmlWriter writer, ShaderArguments arguments)
        {
            writer.WriteParameter("UOffset", new Vector4(arguments.MainTextureScale.x, 0, 0,
                arguments.MainTextureOffset.x));
            writer.WriteParameter("VOffset", new Vector4(0, arguments.MainTextureScale.y, 0,
                arguments.MainTextureOffset.y));
            var psargs = new HashSet<string>();
            if (Engine.Options.PackedNormal)
                psargs.Add("PACKEDNORMAL");
            if (arguments.AlphaTest)
                psargs.Add("ALPHAMASK");
            if (psargs.Any())
                WriteShader(writer, psargs, null);
        }

        protected string BuildAOTextureName(Texture occlusion, float occlusionStrength)
        {
            if (occlusion == null)
                return null;
            if (occlusionStrength <= 0)
                return null;
            var baseName = Engine.EvaluateTextrueName(occlusion);
            if (occlusionStrength >= 0.999f)
                return ExportUtils.ReplaceExtension(baseName, ".AO.dds");
            return ExportUtils.ReplaceExtension(baseName,
                string.Format(CultureInfo.InvariantCulture, ".AO.{0:0.000}.dds", occlusionStrength));
        }
    }
}