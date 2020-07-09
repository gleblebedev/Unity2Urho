using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.ProBuilder;

namespace UnityToCustomEngineExporter.Editor
{
    public class ProBuilderMeshSource : AbstractMeshSource, IMeshSource
    {
        private readonly ProBuilderMesh _mesh;
        private Color32[] _colors;
        private List<List<int>> _indicesPerSubMesh;

        public ProBuilderMeshSource(ProBuilderMesh mesh)
        {
            _mesh = mesh;
            var subMeshCount = _mesh.faces.Select(_ => _.submeshIndex).Max() + 1;
            if (_mesh.colors != null)
            {
                _colors = new Color32[_mesh.colors.Count];
                for (var index = 0; index < _mesh.colors.Count; index++)
                {
                    _colors[index] = _mesh.colors[index];
                }
            }
            _indicesPerSubMesh = new List<List<int>>();
            for (var subMeshIndex = 0; subMeshIndex < subMeshCount; ++subMeshIndex)
            {
                var indices = new List<int>();
                foreach (var face in _mesh.faces.Where(_ => _.submeshIndex == subMeshIndex))
                    for (var tIndex = 2; tIndex < face.indexes.Count; ++tIndex)
                    {
                        indices.Add(face.indexes[0]);
                        indices.Add(face.indexes[tIndex - 1]);
                        indices.Add(face.indexes[tIndex]);
                    }
                _indicesPerSubMesh.Add(indices);
            }
        }


        public override IList<Vector3> Vertices { get => _mesh.positions ?? Array.Empty<Vector3>(); }

        public override IList<Vector3> Normals { get => _mesh.normals ?? Array.Empty<Vector3>(); }
        public override IList<Color32> Colors { get => _colors ?? Array.Empty<Color32>(); }
        public override IList<Vector4> Tangents { get => _mesh.tangents ?? Array.Empty<Vector4>(); }
        public override IList<Vector2> TexCoords0 { get => _mesh.textures ?? Array.Empty<Vector2>(); }
        public override int SubMeshCount => _indicesPerSubMesh.Count;
        public override IList<int> GetIndices(int subMeshIndex)
        {
            return _indicesPerSubMesh[subMeshIndex];
        }
    }
}