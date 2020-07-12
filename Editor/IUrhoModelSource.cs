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
        Transform GetBoneTransform(int index);
        int? GetBoneParent(int index);
        Matrix4x4 GetBoneBindPose(int index);
        IList<int> GetIndices(int subMeshIndex);
    }
}