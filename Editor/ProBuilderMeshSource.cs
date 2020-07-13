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
        private readonly Color32[] _colors;
        private readonly List<List<int>> _indicesPerSubMesh;

        public ProBuilderMeshSource(ProBuilderMesh mesh)
        {
            _mesh = mesh;
            var subMeshCount = _mesh.faces.Select(_ => _.submeshIndex).Max() + 1;
            if (_mesh.colors != null)
            {
                _colors = new Color32[_mesh.colors.Count];
                for (var index = 0; index < _mesh.colors.Count; index++) _colors[index] = _mesh.colors[index];
            }

            _indicesPerSubMesh = new List<List<int>>(subMeshCount);
            for (var subMeshIndex = 0; subMeshIndex < subMeshCount; ++subMeshIndex)
            {
                _indicesPerSubMesh.Add(new List<int>());
            }

            foreach (var face in _mesh.faces)
            {
                var indices = _indicesPerSubMesh[face.submeshIndex];
                indices.AddRange(face.indexes);
            }
        }


        public override IList<Vector3> Vertices => _mesh.positions ?? Array.Empty<Vector3>();

        public override IList<Vector3> Normals => _mesh.normals ?? Array.Empty<Vector3>();
        public override IList<Color32> Colors => _colors ?? Array.Empty<Color32>();
        public override IList<Vector4> Tangents => _mesh.tangents ?? Array.Empty<Vector4>();
        public override IList<Vector2> TexCoords0 => _mesh.textures ?? Array.Empty<Vector2>();
        public override int SubMeshCount => _indicesPerSubMesh.Count;

        public override IList<int> GetIndices(int subMeshIndex)
        {
            return _indicesPerSubMesh[subMeshIndex];
        }
    }
}