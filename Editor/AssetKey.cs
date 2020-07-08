using System;

namespace UnityToCustomEngineExporter.Editor
{
    public struct AssetKey : IEquatable<AssetKey>
    {
        public bool Equals(AssetKey other)
        {
            return Guid == other.Guid && LocalId == other.LocalId;
        }

        public override bool Equals(object obj)
        {
            return obj is AssetKey other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return ((Guid != null ? Guid.GetHashCode() : 0) * 397) ^ LocalId.GetHashCode();
            }
        }

        public static bool operator ==(AssetKey left, AssetKey right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(AssetKey left, AssetKey right)
        {
            return !left.Equals(right);
        }

        public static readonly AssetKey Empty = new AssetKey();
        public string Guid;
        public long LocalId;

        public AssetKey(string guid, long localId)
        {
            Guid = guid;
            LocalId = localId;
        }
    }
}