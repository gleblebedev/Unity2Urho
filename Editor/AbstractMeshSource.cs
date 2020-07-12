using System;
using System.Collections.Generic;
using UnityEngine;

namespace UnityToCustomEngineExporter.Editor
{
    public abstract class AbstractMeshSource : IMeshSource
    {
        public abstract IList<Vector3> Vertices { get; }
        public abstract int SubMeshCount { get; }

        public virtual int BonesCount => 0;

        public virtual IList<Vector3> Normals => Array.Empty<Vector3>();
        public virtual IList<Color32> Colors => Array.Empty<Color32>();
        public virtual IList<Vector4> Tangents => Array.Empty<Vector4>();
        public virtual BoneWeight[] BoneWeights => Array.Empty<BoneWeight>();
        public virtual IList<Vector2> TexCoords0 => Array.Empty<Vector2>();
        public virtual IList<Vector2> TexCoords1 => Array.Empty<Vector2>();
        public virtual IList<Vector2> TexCoords2 => Array.Empty<Vector2>();
        public virtual IList<Vector2> TexCoords3 => Array.Empty<Vector2>();

        public abstract IList<int> GetIndices(int subMeshIndex);

        public virtual Transform GetBoneTransform(int index)
        {
            return null;
        }

        public virtual int? GetBoneParent(int index)
        {
            return null;
        }

        public virtual Matrix4x4 GetBoneBindPose(int index)
        {
            return Matrix4x4.identity;
        }
    }
}