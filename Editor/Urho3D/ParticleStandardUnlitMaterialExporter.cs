﻿using System;
using UnityEditor;
using UnityEditor.Build.Content;
using UnityEngine;

namespace UnityToCustomEngineExporter.Editor.Urho3D
{
    [CustomUrho3DExporter(typeof(Material))]
    public class ParticleStandardUnlitMaterialExporter : AbstractMaterialExporter, IUrho3DMaterialExporter
    {
        public ParticleStandardUnlitMaterialExporter(Urho3DEngine engine) : base(engine)
        {
        }

        public override int ExporterPriority { get; }

        public override bool CanExportMaterial(Material material)
        {
            return material.shader.name == "Particles/Standard Unlit";
        }

        public override void ExportMaterial(Material material, PrefabContext prefabContext)
        {

            var urhoPath = EvaluateMaterialName(material, prefabContext);
            using (var writer = Engine.TryCreateXml(material.GetKey(), urhoPath, ExportUtils.GetLastWriteTimeUtc(material)))
            {
                if (writer == null)
                    return;
                writer.WriteStartElement("material"); writer.WriteWhitespace(Environment.NewLine);
                var mainTex = material.GetTexture("_MainTex");
                if (mainTex != null)
                {
                    WriteTexture(mainTex, writer, "diffuse", prefabContext);
                }
                writer.WriteParameter("MatDiffColor", material.GetColor("_Color"));
                WriteTechnique(writer, "Techniques/DiffAddAlpha.xml");
                WriteTexture(mainTex, writer, "diffuse", prefabContext);
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