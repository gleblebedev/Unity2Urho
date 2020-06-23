using System;
using UnityEngine;

namespace Assets.Scripts.UnityToCustomEngineExporter.Editor
{
    public class PBRSpecularGlossinessTextureReference : TextureReference, IEquatable<PBRSpecularGlossinessTextureReference>
    {
        public bool Equals(PBRSpecularGlossinessTextureReference other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return base.Equals(other) && SmoothnessScale.Equals(other.SmoothnessScale) && Equals(Diffuse, other.Diffuse) && Equals(SmoothnessSource, other.SmoothnessSource);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((PBRSpecularGlossinessTextureReference) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hashCode = base.GetHashCode();
                hashCode = (hashCode * 397) ^ SmoothnessScale.GetHashCode();
                hashCode = (hashCode * 397) ^ (Diffuse != null ? Diffuse.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (SmoothnessSource != null ? SmoothnessSource.GetHashCode() : 0);
                return hashCode;
            }
        }

        public static bool operator ==(PBRSpecularGlossinessTextureReference left, PBRSpecularGlossinessTextureReference right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(PBRSpecularGlossinessTextureReference left, PBRSpecularGlossinessTextureReference right)
        {
            return !Equals(left, right);
        }

        public PBRSpecularGlossinessTextureReference(float smoothnessScale, Texture smoothnessSource, Texture diffuse) : base(TextureSemantic.PBRSpecularGlossiness)
        {
            SmoothnessScale = smoothnessScale;
            SmoothnessSource = smoothnessSource;
            Diffuse = diffuse;
        }

        public float SmoothnessScale = 1.0f;
        public Texture SmoothnessSource;
        public Texture Diffuse;
    }
}