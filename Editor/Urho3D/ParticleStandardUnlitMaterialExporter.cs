using System;
using System.Collections.Generic;
using UnityEngine;

namespace UnityToCustomEngineExporter.Editor.Urho3D
{
    [CustomUrho3DExporter(typeof(Material))]
    public class ParticleStandardUnlitMaterialExporter : AbstractMaterialExporter, IUrho3DMaterialExporter
    {
        enum Mode
        {
            Opaque,
            Cutout,
            Fade,
            Transparent,
            Additive,
            Subtractive,
            Modulate,
        }

        enum ColorMode
        {
            Multiply,
            Additive,
            Subtractive,
            Overlay,
            Color,
            Difference,
        }

        public ParticleStandardUnlitMaterialExporter(Urho3DEngine engine) : base(engine)
        {
            _techniqueByShader["Particles/Standard Unlit"] = "Techniques/DiffAddAlpha.xml";
            _techniqueByShader["Legacy Shaders/Particles/Alpha Blended Premultiply"] = "Techniques/DiffUnlitParticleAlpha.xml";
            _techniqueByShader["Hovl/Particles/Blend_CenterGlow"] = "Techniques/UnlitTransparent.xml";
            _techniqueByShader["Hovl/Particles/Add_CenterGlow"] = "Techniques/DiffAdd.xml";
        }

        public override int ExporterPriority { get; }

        private Dictionary<string, string> _techniqueByShader = new Dictionary<string, string>();

        public override bool CanExportMaterial(Material material)
        {
            var shaderName = material.shader.name;
            return _techniqueByShader.ContainsKey(shaderName);
        }

        public override void ExportMaterial(Material material, PrefabContext prefabContext)
        {
            var urhoPath = EvaluateMaterialName(material, prefabContext);
            using (var writer = Engine.TryCreateXml(material.GetKey(), urhoPath, ExportUtils.GetLastWriteTimeUtc(material)))
            {
                if (writer == null)
                    return;
                writer.WriteStartElement("material"); writer.WriteWhitespace(Environment.NewLine);

                if (material.shader.name == "Particles/Standard Unlit")
                {
                    var mode = (Mode)(int)material.GetFloat("_Mode");
                    var colorMode = (ColorMode)(int)material.GetFloat("_ColorMode");
                    var blendOp = material.GetFloat("_BlendOp");
                    var srcBlend = material.GetFloat("_SrcBlend");
                    var dstBlend = material.GetFloat("_DstBlend");
                    var zWrite = material.GetFloat("_ZWrite");
                    var twoSided = material.GetFloat("_Cull") < 0.5f;
                    var soft = material.GetFloat("_SoftParticlesEnabled") > 0.5f;

                    //Debug.Log($"emissionColor: {emissionColor}, emissionEnabled: {emissionEnabled}");
                    if (mode == Mode.Opaque || mode == Mode.Cutout)
                    {
                        if (soft)
                        {
                            WriteTechnique(writer, "Techniques/DiffLitParticleAlpha.xml");
                        }
                        else
                        {
                            WriteTechnique(writer, "Techniques/DiffLitParticleAlphaSoft.xml");
                        }
                    }
                    if (mode == Mode.Additive)
                    {
                        if (soft)
                        {
                            WriteTechnique(writer, "Techniques/DiffUnlitParticleAdd.xml");
                        }
                        else
                        {
                            WriteTechnique(writer, "Techniques/DiffUnlitParticleAddSoft.xml");
                        }
                        writer.WriteElementParameter("renderorder", "value", "129");
                    }
                    else
                    {
                        if (soft)
                        {
                            WriteTechnique(writer, "Techniques/DiffUnlitParticleAlpha.xml");
                        }
                        else
                        {
                            WriteTechnique(writer, "Techniques/DiffUnlitParticleAlphaSoft.xml");
                        }
                    }

                    if (soft)
                    {
                        writer.WriteElementParameter("SoftParticleFadeScale", "value", "1");
                    }
                    if (twoSided)
                    {
                        writer.WriteElementParameter("cull", "value", Urho3DCulling.none.ToString());
                        writer.WriteElementParameter("shadowcull", "value", Urho3DCulling.none.ToString());
                    }
                    else
                    {
                        writer.WriteElementParameter("cull", "value", Urho3DCulling.ccw.ToString());
                        writer.WriteElementParameter("shadowcull", "value", Urho3DCulling.ccw.ToString());
                    }


                }
                else if (_techniqueByShader.TryGetValue(material.shader.name, out var technique))
                {
                    WriteTechnique(writer, technique);
                }
                else
                {
                    WriteTechnique(writer, "Techniques/DiffAddAlpha.xml");
                }

                if (material.HasProperty("_MainTex"))
                {
                    var mainTex = material.GetTexture("_MainTex");
                    if (mainTex != null)
                    {
                        WriteTexture(mainTex, writer, "diffuse", prefabContext);
                    }
                }

                if (material.HasProperty("_Color"))
                {
                    writer.WriteParameter("MatDiffColor", material.GetColor("_Color"));
                }

                if (material.HasProperty("_EmissionEnabled") && material.GetFloat("_EmissionEnabled") > 0.5f)
                {
                    if (material.HasProperty("_EmissionColor"))
                        writer.WriteParameter("MatEmissiveColor", material.GetColor("_EmissionColor"));
                }
     
                writer.WriteEndElement();
            }
            //TexEnv _MainTex

                //Color _Color

                //Range _Cutoff

                //Float _BumpScale

                //TexEnv _BumpMap

                //Color _EmissionColor

                //TexEnv _EmissionMap

                //Float _DistortionStrength

                //Range _DistortionBlend

                //Float _SoftParticlesNearFadeDistance

                //Float _SoftParticlesFarFadeDistance

                //Float _CameraNearFadeDistance

                //Float _CameraFarFadeDistance

                //Float _Mode

                //Float _ColorMode

                //Float _FlipbookMode

                //Float _LightingEnabled

                //Float _DistortionEnabled

                //Float _EmissionEnabled

                //Float _BlendOp

                //Float _SrcBlend

                //Float _DstBlend

                //Float _ZWrite

                //Float _Cull

                //Float _SoftParticlesEnabled

                //Float _CameraFadingEnabled

                //Vector _SoftParticleFadeParams

                //Vector _CameraFadeParams

                //Vector _ColorAddSubDiff

                //Float _DistortionStrengthScaled

                //var shader = material.shader;
                //for (var i = 0; i < ShaderUtil.GetPropertyCount(shader); i++)
                //{
                //    var propertyName = ShaderUtil.GetPropertyName(shader, i);
                //    var propertyType = ShaderUtil.GetPropertyType(shader, i);
                //    System.Diagnostics.Debug.WriteLine($"{propertyType} {propertyName}");
                //}
            }
        }
}