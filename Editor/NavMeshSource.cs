using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.AI;

namespace UnityToCustomEngineExporter.Editor
{
    public class NavMeshSource : AbstractMeshSource, IMeshSource
    {
        private readonly NavMeshTriangulation _mesh;

        public NavMeshSource(NavMeshTriangulation mesh)
        {
            _mesh = mesh;
            Normals = _mesh.vertices.Select(_ => new Vector3(0, 1, 0)).ToList();
            TexCoords0 = _mesh.vertices.Select(_ => new Vector2(_.x, _.z)).ToList();
        }


        public override IList<Vector3> Vertices => _mesh.vertices;

        public override IList<Vector3> Normals { get; }

        public override IList<Vector2> TexCoords0 { get; }

        public override int SubMeshCount => 1;

        public override IList<int> GetIndices(int subMeshIndex)
        {
            return _mesh.indices;
        }
    }
}