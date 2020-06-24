using System;
using UnityEngine;

namespace Assets.Scripts.UnityToCustomEngineExporter.Editor
{
    public class PBRMetallicGlossinessTextureReference : TextureReference, IEquatable<PBRMetallicGlossinessTextureReference>
    {
        public bool Equals(PBRMetallicGlossinessTextureReference other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return base.Equals(other) && SmoothnessScale.Equals(other.SmoothnessScale) && Equals(Smoothness, other.Smoothness);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((PBRMetallicGlossinessTextureReference) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hashCode = base.GetHashCode();
                hashCode = (hashCode * 397) ^ SmoothnessScale.GetHashCode();
                hashCode = (hashCode * 397) ^ (Smoothness != null ? Smoothness.GetHashCode() : 0);
                return hashCode;
            }
        }

        public static bool operator ==(PBRMetallicGlossinessTextureReference left, PBRMetallicGlossinessTextureReference right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(PBRMetallicGlossinessTextureReference left, PBRMetallicGlossinessTextureReference right)
        {
            return !Equals(left, right);
        }

        public PBRMetallicGlossinessTextureReference(float smoothnessScale, Texture smoothness) :base(TextureSemantic.PBRMetallicGlossiness)
        {
            SmoothnessScale = smoothnessScale;
            Smoothness = smoothness;
        }

        public float SmoothnessScale = 1.0f;
        public Texture Smoothness;

        public DateTime GetLastWriteTimeUtc(Texture metallicGloss)
        {
            return ExportUtils.MaxDateTime(ExportUtils.GetLastWriteTimeUtc(metallicGloss), ExportUtils.GetLastWriteTimeUtc(Smoothness));
        }
    }
}