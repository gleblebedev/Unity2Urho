using System;
using UnityEditor;
using UnityEngine;

namespace UnityToCustomEngineExporter.Editor.Urho3D
{
    public class SkyboxMaterialExporter : AbstractMaterialExporter, IUrho3DMaterialExporter
    {
        public SkyboxMaterialExporter(Urho3DEngine engine) : base(engine)
        {
        }

        public override int ExporterPriority => 0;

        public override bool CanExportMaterial(Material material)
        {
            return material.shader.name.StartsWith("Skybox/");
        }

        public override void ExportMaterial(Material material, PrefabContext prefabContext)
        {
            var assetGuid = material.GetKey();
            var urhoPath = EvaluateMaterialName(material);
            var arguments = SetupSkybox(material);
            using (var writer = Engine.TryCreateXml(assetGuid, urhoPath, ExportUtils.GetLastWriteTimeUtc(material)))
            {
                if (writer == null)
                    return;
                //writer.WriteStartDocument();
                //writer.WriteWhitespace(Environment.NewLine);
                writer.WriteStartElement("material");
                writer.WriteWhitespace(Environment.NewLine);
                var technique = "Techniques/DiffSkyboxHDRScale.xml";

                var anyFace = arguments.BackTex ?? arguments.DownTex ?? arguments.FrontTex ??
                              arguments.LeftTex ?? arguments.RightTex ?? arguments.UpTex;
                if (arguments.Skybox != null)
                {
                    Engine.ScheduleAssetExport(arguments.Skybox, prefabContext);
                    string name;
                    if (arguments.Skybox is Cubemap cubemap)
                    {
                        name = Engine.EvaluateCubemapName(cubemap);
                    }
                    else
                    {
                        name = Engine.EvaluateTextrueName(arguments.Skybox);
                        technique = "Techniques/DiffSkydome.xml";
                    }

                    if (!string.IsNullOrWhiteSpace(name)) WriteTexture(name, writer, "diffuse", prefabContext);
                }
                else if (anyFace != null)
                {
                    var cubemapName = ExportUtils.ReplaceExtension(urhoPath, ".Cubemap.xml");
                    using (var cubemapWriter = Engine.TryCreateXml(assetGuid, cubemapName, DateTime.MaxValue))
                    {
                        if (cubemapWriter != null)
                        {
                            cubemapWriter.WriteStartElement("cubemap");
                            foreach (var tex in new[]
                            {
                                arguments.RightTex, arguments.LeftTex, arguments.UpTex, arguments.DownTex,
                                arguments.FrontTex, arguments.BackTex
                            })
                            {
                                cubemapWriter.WriteWhitespace(Environment.NewLine);
                                cubemapWriter.WriteStartElement("face");
                                cubemapWriter.WriteAttributeString("name", Engine.EvaluateTextrueName(tex ?? anyFace));
                                cubemapWriter.WriteEndElement();
                                Engine.ScheduleTexture(tex);
                            }

                            cubemapWriter.WriteWhitespace(Environment.NewLine);
                            cubemapWriter.WriteStartElement("address");
                            cubemapWriter.WriteAttributeString("coord", "uv");
                            cubemapWriter.WriteAttributeString("mode", "clamp");
                            cubemapWriter.WriteEndElement();
                            cubemapWriter.WriteWhitespace(Environment.NewLine);
                            cubemapWriter.WriteStartElement("mipmap");
                            cubemapWriter.WriteAttributeString("enable", "true");
                            cubemapWriter.WriteEndElement();
                            cubemapWriter.WriteWhitespace(Environment.NewLine);
                            cubemapWriter.WriteStartElement("filter");
                            cubemapWriter.WriteAttributeString("mode", "trilinear");
                            cubemapWriter.WriteEndElement();
                            cubemapWriter.WriteWhitespace(Environment.NewLine);
                            cubemapWriter.WriteEndElement();
                        }
                    }

                    WriteTexture(cubemapName, writer, "diffuse", prefabContext);
                }
                else
                {
                    WriteTexture("Resources/unity_builtin_extra/Default-Skybox-Map.xml", writer, "diffuse", prefabContext);
                }

                WriteTechnique(writer, technique);

                {
                    writer.WriteWhitespace("\t");
                    writer.WriteStartElement("cull");
                    writer.WriteAttributeString("value", "none");
                    writer.WriteEndElement();
                    writer.WriteWhitespace(Environment.NewLine);

                    writer.WriteWhitespace("\t");
                    writer.WriteStartElement("shadowcull");
                    writer.WriteAttributeString("value", "ccw");
                    writer.WriteEndElement();
                    writer.WriteWhitespace(Environment.NewLine);

                    writer.WriteWhitespace("\t");
                    writer.WriteStartElement("shader");
                    writer.WriteAttributeString("vsdefines", "IGNORENODETRANSFORM");
                    writer.WriteEndElement();
                    writer.WriteWhitespace(Environment.NewLine);
                }

                writer.WriteEndElement();
            }
        }

        public SkyboxShaderArguments SetupSkybox(Material material)
        {
            var setupProceduralSkybox = new SkyboxShaderArguments();
            var shader = material.shader;
            for (var i = 0; i < ShaderUtil.GetPropertyCount(shader); i++)
            {
                var propertyName = ShaderUtil.GetPropertyName(shader, i);
                var propertyType = ShaderUtil.GetPropertyType(shader, i);
                if (propertyType == ShaderUtil.ShaderPropertyType.TexEnv)
                {
                    var texture = material.GetTexture(propertyName);
                    switch (propertyName)
                    {
                        case "_Tex":
                            setupProceduralSkybox.Skybox = texture;
                            break;
                        case "_MainTex":
                            setupProceduralSkybox.Skybox = texture;
                            break;
                        case "_FrontTex":
                            setupProceduralSkybox.FrontTex = texture;
                            break;
                        case "_BackTex":
                            setupProceduralSkybox.BackTex = texture;
                            break;
                        case "_LeftTex":
                            setupProceduralSkybox.LeftTex = texture;
                            break;
                        case "_RightTex":
                            setupProceduralSkybox.RightTex = texture;
                            break;
                        case "_UpTex":
                            setupProceduralSkybox.UpTex = texture;
                            break;
                        case "_DownTex":
                            setupProceduralSkybox.DownTex = texture;
                            break;
                    }
                }
            }

            return setupProceduralSkybox;
        }
    }
}