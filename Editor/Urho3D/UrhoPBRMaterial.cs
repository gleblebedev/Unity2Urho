using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace UnityToCustomEngineExporter.Editor.Urho3D
{
    public class UrhoPBRMaterial
    {
        private IList<string> _textureUnits = new List<string>();

        public string Technique { get; set; }

        public IList<string> TextureUnits
        {
            get => _textureUnits;
            set => _textureUnits = value ?? new List<string>();
        }

        public string BaseColorTexture { get; set; }

        public string MetallicRoughnessTexture { get; set; }

        public string NormalTexture { get; set; }

        public string EmissiveTexture { get; set; }

        public string AOTexture { get; set; }

        /// <summary>
        ///     MatDiffColor
        /// </summary>
        public Color BaseColor { get; set; } = Color.white;

        /// <summary>
        ///     MatEnvMapColor
        /// </summary>
        public Color MatEnvMapColor { get; set; } = Color.white;

        /// <summary>
        ///     MatEnvMapColor
        /// </summary>
        public Color MatSpecColor { get; set; } = Color.white;

        public float Roughness { get; set; }

        public float Metallic { get; set; }

        public Vector4 UOffset { get; set; } = new Vector4(1, 0, 0, 0);

        public Vector4 VOffset { get; set; } = new Vector4(0, 1, 0, 0);

        public bool AlphaBlend { get; set; }

        public Urho3DCulling Cull { get; set; } = Urho3DCulling.ccw;
        public Urho3DCulling ShadowCull { get; set; } = Urho3DCulling.ccw;

        public Color EmissiveColor { get; set; } = Color.black;

        public Dictionary<string, object> ExtraParameters { get; } = new Dictionary<string, object>();

        public HashSet<string> PixelShaderDefines { get; } = new HashSet<string>();

        public HashSet<string> VertexShaderDefines { get; } = new HashSet<string>();

        public void EvaluateTechnique()
        {
            var name = new StringBuilder("Techniques/PBR/PBR");
            var hasTexture = false;
            if (!string.IsNullOrWhiteSpace(MetallicRoughnessTexture))
            {
                name.Append("MetallicRough");
                hasTexture = true;
            }

            if (!string.IsNullOrWhiteSpace(BaseColorTexture))
            {
                name.Append("Diff");
                hasTexture = true;
            }

            if (!string.IsNullOrWhiteSpace(NormalTexture))
            {
                name.Append("Normal");
                hasTexture = true;
            }

            if (!string.IsNullOrWhiteSpace(MetallicRoughnessTexture))
            {
                name.Append("Spec");
                hasTexture = true;
            }

            if (!string.IsNullOrWhiteSpace(EmissiveTexture))
            {
                name.Append("Emissive");
                hasTexture = true;
            }

            if (string.IsNullOrWhiteSpace(EmissiveTexture) && !string.IsNullOrWhiteSpace(AOTexture))
            {
                name.Append("AO");
                hasTexture = true;
            }

            if (!hasTexture) name.Append("NoTexture");
            if (AlphaBlend) name.Append("Alpha");

            name.Append(".xml");

            Technique = name.ToString();
        }
    }
}