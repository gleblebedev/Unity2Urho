using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.ProBuilder;
using Object = UnityEngine.Object;

namespace UnityToCustomEngineExporter.Editor
{
    public class ProBuilderMeshSource : AbstractMeshSource, IMeshSource
    {
        private readonly ProBuilderMesh _mesh;
        private readonly Color32[] _colors;
        private readonly List<Geometry> _geometries;

        public ProBuilderMeshSource(Object mesh)
        {
            _mesh = (ProBuilderMesh)mesh;
            var subMeshCount = _mesh.faces.Select(_ => _.submeshIndex).Max() + 1;
            if (_mesh.colors != null)
            {
                _colors = new Color32[_mesh.colors.Count];
                for (var index = 0; index < _mesh.colors.Count; index++) _colors[index] = _mesh.colors[index];
            }

            _geometries = new List<Geometry>(subMeshCount);
            for (var subMeshIndex = 0; subMeshIndex < subMeshCount; ++subMeshIndex) _geometries.Add(new Geometry());

            foreach (var face in _mesh.faces)
            {
                var indices = _geometries[face.submeshIndex];
                indices.AddRange(face.indexes);
            }
        }


        public override IList<Vector3> Vertices => _mesh.positions ?? Array.Empty<Vector3>();

        public override IList<Vector3> Normals => _mesh.normals ?? Array.Empty<Vector3>();
        public override IList<Color32> Colors => _colors ?? Array.Empty<Color32>();
        public override IList<Vector4> Tangents => _mesh.tangents ?? Array.Empty<Vector4>();
        public override IList<Vector2> TexCoords0 => _mesh.textures ?? Array.Empty<Vector2>();
        public override int SubMeshCount => _geometries.Count;

        public override IMeshGeometry GetGeomtery(int subMeshIndex)
        {
            return _geometries[subMeshIndex];
        }

        private class Geometry : IMeshGeometry
        {
            private readonly List<int> _indices = new List<int>();

            public int NumLods => 1;

            public MeshTopology Topology => MeshTopology.Triangles;

            public Bounds? Bounds => null;

            public void AddRange(IEnumerable<int> faceIndexes)
            {
                _indices.AddRange(faceIndexes);
            }

            public IList<int> GetIndices(int lod)
            {
                return _indices;
            }

            public float GetLodDistance(int lod)
            {
                return 0;
            }
        }
    }
}