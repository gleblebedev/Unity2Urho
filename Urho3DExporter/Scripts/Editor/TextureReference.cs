using System;
using UnityEngine;

namespace Assets.Urho3DExporter.Scripts.Editor
{
    public class PBRSpecularGlossinessTextureReference : TextureReference, IEquatable<PBRSpecularGlossinessTextureReference>
    {
        public bool Equals(PBRSpecularGlossinessTextureReference other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return base.Equals(other) && SmoothnessScale.Equals(other.SmoothnessScale) && Equals(Specular, other.Specular) && Equals(SmoothnessSource, other.SmoothnessSource);
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
                hashCode = (hashCode * 397) ^ (Specular != null ? Specular.GetHashCode() : 0);
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

        public PBRSpecularGlossinessTextureReference(float smoothnessScale, Texture smoothnessSource, Texture specular) : base(TextureSemantic.PBRSpecularGlossiness)
        {
            SmoothnessScale = smoothnessScale;
            SmoothnessSource = smoothnessSource;
            Specular = specular;
        }

        public float SmoothnessScale = 1.0f;
        public Texture Specular;
        public Texture SmoothnessSource;
    }

    public class TextureScaleReference: TextureReference, IEquatable<TextureScaleReference>
    {
        public bool Equals(TextureScaleReference other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return base.Equals(other) && _scale.Equals(other._scale);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((TextureScaleReference) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (base.GetHashCode() * 397) ^ _scale.GetHashCode();
            }
        }

        public static bool operator ==(TextureScaleReference left, TextureScaleReference right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(TextureScaleReference left, TextureScaleReference right)
        {
            return !Equals(left, right);
        }

        private readonly float _scale;

        public TextureScaleReference(TextureSemantic semantic, float scale):base(semantic)
        {
            _scale = scale;
        }
    }

    public class TextureReference : IEquatable<TextureReference>
    {
        public bool Equals(TextureReference other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return Semantic == other.Semantic;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((TextureReference) obj);
        }

        public override int GetHashCode()
        {
            return (int) Semantic;
        }

        public static bool operator ==(TextureReference left, TextureReference right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(TextureReference left, TextureReference right)
        {
            return !Equals(left, right);
        }

        public TextureSemantic Semantic;

        public TextureReference(TextureSemantic semantic)
        {
            Semantic = semantic;
        }
    }
}