using System;
using UnityEngine;

namespace UnityToCustomEngineExporter.Editor
{
    public class PBRMetallicGlossinessTextureReference : TextureReference,
        IEquatable<PBRMetallicGlossinessTextureReference>
    {
        public float SmoothnessScale = 1.0f;
        public Texture Smoothness;

        public PBRMetallicGlossinessTextureReference(float smoothnessScale, Texture smoothness) : base(TextureSemantic
            .PBRMetallicGlossiness)
        {
            SmoothnessScale = smoothnessScale;
            Smoothness = smoothness;
        }

        public static bool operator ==(PBRMetallicGlossinessTextureReference left,
            PBRMetallicGlossinessTextureReference right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(PBRMetallicGlossinessTextureReference left,
            PBRMetallicGlossinessTextureReference right)
        {
            return !Equals(left, right);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != GetType()) return false;
            return Equals((PBRMetallicGlossinessTextureReference) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = base.GetHashCode();
                hashCode = (hashCode * 397) ^ SmoothnessScale.GetHashCode();
                hashCode = (hashCode * 397) ^ (Smoothness != null ? Smoothness.GetHashCode() : 0);
                return hashCode;
            }
        }

        public DateTime GetLastWriteTimeUtc(Texture metallicGloss)
        {
            return ExportUtils.MaxDateTime(ExportUtils.GetLastWriteTimeUtc(metallicGloss),
                ExportUtils.GetLastWriteTimeUtc(Smoothness));
        }

        public bool Equals(PBRMetallicGlossinessTextureReference other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return base.Equals(other) && SmoothnessScale.Equals(other.SmoothnessScale) &&
                   Equals(Smoothness, other.Smoothness);
        }
    }
}