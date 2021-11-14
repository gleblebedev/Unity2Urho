using System.Collections.Generic;
using UnityEngine;

namespace UnityToCustomEngineExporter.Editor
{
    public interface IMeshSource
    {
        int BonesCount { get; }
        IList<Vector3> Vertices { get; }
        IList<Vector3> Normals { get; }
        IList<Color32> Colors { get; }
        IList<Vector4> Tangents { get; }
        BoneWeight[] BoneWeights { get; }
        IList<Vector2> TexCoords0 { get; }
        IList<Vector2> TexCoords1 { get; }
        IList<Vector2> TexCoords2 { get; }
        IList<Vector2> TexCoords3 { get; }
        int SubMeshCount { get; }
        IList<IMorphTarget> MorphTargets { get; }
        Transform GetBoneTransform(int index);
        int? GetBoneParent(int index);
        Matrix4x4 GetBoneBindPose(int index);
        IMeshGeometry GetGeomtery(int subMeshIndex);
    }

    public interface IMorphTarget
    {
        string Name { get; }
        IList<Vector3> Vertices { get; }
        IList<Vector3> Normals { get;  }
        IList<Vector3> Tangents { get;  }
    }

    public interface IMeshGeometry
    {
        int NumLods { get; }
        Bounds? Bounds { get; }
        MeshTopology Topology { get; }
        IList<int> GetIndices(int lod);
        float GetLodDistance(int lod);
    }
}