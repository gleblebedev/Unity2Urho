using System;
using UnityEngine;

namespace UnityToCustomEngineExporter.Editor
{
    public struct TextureOrColor : IEquatable<TextureOrColor>
    {
        public bool Equals(TextureOrColor other)
        {
            return Equals(Texture, other.Texture) && Color.Equals(other.Color);
        }

        public override bool Equals(object obj)
        {
            return obj is TextureOrColor other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return ((Texture != null ? Texture.GetHashCode() : 0) * 397) ^ Color.GetHashCode();
            }
        }

        public static bool operator ==(TextureOrColor left, TextureOrColor right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(TextureOrColor left, TextureOrColor right)
        {
            return !left.Equals(right);
        }

        public Texture Texture;
        public Color Color;

        public TextureOrColor(Texture texture, Color color)
        {
            Texture = texture;
            Color = color;
        }
    }
}