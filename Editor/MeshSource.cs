using System;
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

            MorphTargets = new List<IMorphTarget>(mesh.blendShapeCount);
            if (mesh.blendShapeCount > 0)
            {
                Debug.LogWarning(mesh.name+" has blendshapes");
            }
            for (int i=0; i<mesh.blendShapeCount; ++i)
            {
                var numFrames = mesh.GetBlendShapeFrameCount(i);
                if (numFrames == 1)
                {
                    MorphTargets.Add(new MorphTarget(mesh.GetBlendShapeName(i), mesh, i, 0));
                }
                else
                {
                    for (int frame = 0; frame < numFrames; ++frame)
                    {
                        MorphTargets.Add(new MorphTarget(mesh.GetBlendShapeName(i) + "_" + frame, mesh, i, frame));
                    }
                }
            }
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
        public override IList<IMorphTarget> MorphTargets { get; }

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
                if (skinBone == null)
                    return null;
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

        public override IMeshGeometry GetGeomtery(int subMeshIndex)
        {
            return new Geometry(_mesh, subMeshIndex);
        }
        public class MorphTarget : IMorphTarget
        {
            public MorphTarget(string name, Mesh mesh, int targetIndex, int frameIndex)
            {
                Name = name;
                var numVerts = mesh.vertices.Length;
                var vertices = new Vector3[numVerts];
                var normals = new Vector3[numVerts];
                var tangents = new Vector3[numVerts];
                mesh.GetBlendShapeFrameVertices(targetIndex, frameIndex, vertices, normals, tangents);
                Vertices = vertices;
                Normals = normals;
                Tangents = tangents;
            }

            public string Name { get; set; }
            public IList<Vector3> Vertices { get; }
            public IList<Vector3> Normals { get; set; }
            public IList<Vector3> Tangents { get; set; }
        }

        private class Geometry : IMeshGeometry
        {
            private readonly Mesh _mesh;
            private readonly int _submesh;

            public Geometry(Mesh mesh, int submesh)
            {
                _mesh = mesh;
                _submesh = submesh;
                Bounds = _mesh.GetSubMesh(_submesh).bounds;
            }

            public Bounds? Bounds { get; }

            public int NumLods => 1;

            public MeshTopology Topology => _mesh.GetSubMesh(_submesh).topology;

            public IList<int> GetIndices(int lod)
            {
                if (lod == 0)
                    return _mesh.GetIndices(_submesh);
                return Array.Empty<int>();
            }

            public float GetLodDistance(int lod)
            {
                return 0;
            }
        }
    }
}