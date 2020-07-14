using System.Collections.Generic;
using UnityEngine;

namespace UnityToCustomEngineExporter.Editor
{
    public class MeshSource : AbstractMeshSource, IMeshSource
    {
        private readonly Mesh _mesh;
        private readonly SkinnedMeshRenderer _skin;

        public MeshSource(Mesh mesh, SkinnedMeshRenderer skin = null)
        {
            _mesh = mesh;
            _skin = skin;
        }

        public override int BonesCount
        {
            get
            {
                if (_skin == null)
                    return 0;
                return _skin.bones.Length;
            }
        }

        public override IList<Vector3> Vertices => _mesh.vertices;

        public override IList<Vector3> Normals => _mesh.normals;
        public override IList<Color32> Colors => _mesh.colors32;
        public override IList<Vector4> Tangents => _mesh.tangents;
        public override BoneWeight[] BoneWeights => _mesh.boneWeights;
        public override IList<Vector2> TexCoords0 => _mesh.uv;
        public override IList<Vector2> TexCoords1 => _mesh.uv2;
        public override IList<Vector2> TexCoords2 => _mesh.uv3;
        public override IList<Vector2> TexCoords3 => _mesh.uv4;

        public override int SubMeshCount => _mesh.subMeshCount;

        public override Transform GetBoneTransform(int index)
        {
            return _skin != null ? _skin.bones[index] : null;
        }

        public override int? GetBoneParent(int index)
        {
            if (_skin != null)
            {
                var skinBone = _skin.bones[index];
                var boneParent = skinBone.parent;
                if (boneParent == null)
                    return null;
                for (var i = 0; i < _skin.bones.Length; ++i)
                    if (_skin.bones[i] == boneParent)
                        return i;
                return null;
            }

            return null;
        }

        public override Matrix4x4 GetBoneBindPose(int index)
        {
            if (_skin == null)
                return base.GetBoneBindPose(index);
            return _skin.sharedMesh.bindposes[index];
        }

        public override IList<int> GetIndices(int subMeshIndex)
        {
            return _mesh.GetIndices(subMeshIndex);
        }
    }
}