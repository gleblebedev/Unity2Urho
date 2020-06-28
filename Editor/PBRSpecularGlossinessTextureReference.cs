using System;
using UnityEngine;

namespace UnityToCustomEngineExporter.Editor
{
    public class PBRSpecularGlossinessTextureReference : TextureReference,
        IEquatable<PBRSpecularGlossinessTextureReference>
    {
        public float SmoothnessScale = 1.0f;
        public TextureOrColor Smoothness;
        public Texture Diffuse;

        public PBRSpecularGlossinessTextureReference(float smoothnessScale, TextureOrColor smoothness, Texture diffuse)
            : base(TextureSemantic.PBRSpecularGlossiness)
        {
            SmoothnessScale = smoothnessScale;
            Smoothness = smoothness;
            Diffuse = diffuse;
        }

        public static bool operator ==(PBRSpecularGlossinessTextureReference left,
            PBRSpecularGlossinessTextureReference right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(PBRSpecularGlossinessTextureReference left,
            PBRSpecularGlossinessTextureReference right)
        {
            return !Equals(left, right);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != GetType()) return false;
            return Equals((PBRSpecularGlossinessTextureReference) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = base.GetHashCode();
                hashCode = (hashCode * 397) ^ SmoothnessScale.GetHashCode();
                hashCode = (hashCode * 397) ^ (Diffuse != null ? Diffuse.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ Smoothness.GetHashCode();
                return hashCode;
            }
        }

        public DateTime GetLastWriteTimeUtc(Texture texture)
        {
            return ExportUtils.MaxDateTime(ExportUtils.GetLastWriteTimeUtc(texture),
                ExportUtils.GetLastWriteTimeUtc(Smoothness.Texture), ExportUtils.GetLastWriteTimeUtc(Diffuse));
        }

        public bool Equals(PBRSpecularGlossinessTextureReference other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return base.Equals(other) && SmoothnessScale.Equals(other.SmoothnessScale) &&
                   Equals(Diffuse, other.Diffuse) && Equals(Smoothness, other.Smoothness);
        }
    }
}