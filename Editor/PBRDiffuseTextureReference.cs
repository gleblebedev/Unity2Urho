using System;
using UnityEngine;

namespace UnityToCustomEngineExporter.Editor
{
    public class PBRDiffuseTextureReference : TextureReference, IEquatable<PBRDiffuseTextureReference>
    {
        public bool Equals(PBRDiffuseTextureReference other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return base.Equals(other) && Equals(Specular, other.Specular) && Equals(Smoothness, other.Smoothness) && SmoothnessScale.Equals(other.SmoothnessScale);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hashCode = base.GetHashCode();
                hashCode = (hashCode * 397) ^ (Specular != null ? Specular.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (Smoothness != null ? Smoothness.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ SmoothnessScale.GetHashCode();
                return hashCode;
            }
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((PBRDiffuseTextureReference) obj);
        }


        public static bool operator ==(PBRDiffuseTextureReference left, PBRDiffuseTextureReference right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(PBRDiffuseTextureReference left, PBRDiffuseTextureReference right)
        {
            return !Equals(left, right);
        }

        public PBRDiffuseTextureReference(TextureOrColor specular, TextureOrColor smoothness, float smoothnessScale) : base(TextureSemantic.PBRDiffuse)
        {
            Specular = specular;
            Smoothness = smoothness;
            SmoothnessScale = smoothnessScale;
        }
        public TextureOrColor Specular;
        public TextureOrColor Smoothness;
        public float SmoothnessScale;

        public DateTime GetLastWriteTimeUtc(Texture diffuse)
        {
            return ExportUtils.MaxDateTime(ExportUtils.GetLastWriteTimeUtc(diffuse), ExportUtils.GetLastWriteTimeUtc(Specular.Texture), ExportUtils.GetLastWriteTimeUtc(Smoothness.Texture));
        }
    }
}