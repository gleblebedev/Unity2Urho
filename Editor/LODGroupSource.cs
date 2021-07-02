using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace UnityToCustomEngineExporter.Editor
{
    public class LODGroupSource : AbstractMeshSource, IMeshSource
    {
        private readonly List<Vector3> _vertices = new List<Vector3>();
        private readonly List<Vector3> _normals = new List<Vector3>();

        private readonly List<Geometry> _geometries = new List<Geometry>();
        private readonly List<Lod> _lods = new List<Lod>();
        private readonly List<Vector2> _texCoords0 = new List<Vector2>();
        private readonly List<Vector2> _texCoords1 = new List<Vector2>();
        private readonly List<Vector2> _texCoords2 = new List<Vector2>();
        private readonly List<Vector2> _texCoords3 = new List<Vector2>();
        private readonly List<Vector4> _tangents = new List<Vector4>();
        private readonly List<Color32> _colors = new List<Color32>();

        public LODGroupSource(LODGroup lodGroup)
        {
            var materialToSubmesh = new Dictionary<MaterialTopology, int>();
            var rendererToSubmeshMappings = new Dictionary<MeshInstanceKey, MeshMapping>();
            var worldToLocalMatrix = lodGroup.transform.worldToLocalMatrix;

            var lods = lodGroup.GetLODs();
            _lods = lods.Select(_ => new Lod(_)).ToList();
            for (var lodIndex = 0; lodIndex < lods.Length; lodIndex++)
            {
                var lod = lods[lodIndex];
                foreach (var renderer in lod.renderers.Where(_=>_!=null))
                {
                    var mesh = renderer.GetComponent<MeshFilter>()?.sharedMesh;
                    for (var submeshIndex = 0; submeshIndex < mesh.subMeshCount; submeshIndex++)
                    {
                        var material = (renderer.sharedMaterials.Length > submeshIndex)? renderer.sharedMaterials[submeshIndex]: null;
                        var topology = mesh.GetTopology(submeshIndex);
                        var key = new MaterialTopology() {material = material, topology = topology};
                        if (!materialToSubmesh.TryGetValue(key, out var matIndex))
                        {
                            matIndex = materialToSubmesh.Count;
                            materialToSubmesh.Add(key, matIndex);
                            _geometries.Add(new Geometry(material, _lods, topology));
                        }
                    }
                }
            }

            for (var lodIndex = 0; lodIndex < lods.Length; lodIndex++)
            {
                var lod = lods[lodIndex];
                foreach (var renderer in lod.renderers.Where(_ => _ != null))
                {
                    var transform = renderer.localToWorldMatrix * worldToLocalMatrix;
                    var mesh = renderer.GetComponent<MeshFilter>()?.sharedMesh;
                    if (mesh != null)
                    {
                        var key = new MeshInstanceKey {mesh = mesh, transform = transform};
                        if (!rendererToSubmeshMappings.TryGetValue(key, out var meshMapping))
                        {
                            meshMapping = BuildMapping(renderer, mesh, transform);
                            rendererToSubmeshMappings[key] = meshMapping;
                        }

                        for (var subIndex = 0; subIndex < mesh.subMeshCount; ++subIndex)
                        {
                            var material = (renderer.sharedMaterials.Length > subIndex) ? renderer.sharedMaterials[subIndex] : null;
                            var topology = mesh.GetTopology(subIndex);
                            var submeshKey = new MaterialTopology() { material = material, topology = topology };
                            var geometry = _geometries[materialToSubmesh[submeshKey]];
                            geometry.AddReference(lodIndex,
                                new SubmeshReference {Mesh = meshMapping, Submesh = subIndex});
                        }
                    }
                }
            }

            if (_vertices.Count > 0)
            {
                var min = _vertices[0];
                var max = _vertices[0];
                foreach (var originalPosition in _vertices)
                {
                    var p = originalPosition;
                    if (p.x < min.x) min.x = p.x;
                    if (p.y < min.y) min.y = p.y;
                    if (p.z < min.z) min.z = p.z;
                    if (p.x > max.x) max.x = p.x;
                    if (p.y > max.y) max.y = p.y;
                    if (p.z > max.z) max.z = p.z;
                }

                var diag = max - min;
                var unitySize = Math.Max(Math.Max(diag.x, diag.y), diag.z);
                var urhoSize = Vector3.Dot(diag, new Vector3(1.0f / 3.0f, 1.0f / 3.0f, 1.0f / 3.0f));
                foreach (var lod in _lods) lod.SetSizeFactor(unitySize/(urhoSize+1e-6f));
            }
        }

        public override IList<Vector3> Vertices => _vertices;
        public override IList<Vector3> Normals => _normals;
        public override IList<Vector2> TexCoords0 => _texCoords0;
        public override IList<Vector2> TexCoords1 => _texCoords1;
        public override IList<Vector2> TexCoords2 => _texCoords2;
        public override IList<Vector2> TexCoords3 => _texCoords3;
        public override IList<Vector4> Tangents => _tangents;
        public override IList<Color32> Colors => _colors;

        public override int SubMeshCount => _geometries.Count;

        public override IMeshGeometry GetGeomtery(int subMeshIndex)
        {
            return _geometries[subMeshIndex];
        }

        private MeshMapping BuildMapping(Renderer renderer, Mesh mesh, Matrix4x4 transform)
        {
            var buildMapping = new MeshMapping(renderer, mesh, transform);
            var startIndex = Vertices.Count;

            var meshSource = buildMapping.Source;
            var expectedTargetCount = _vertices.Count;
            AddData(_vertices, expectedTargetCount, TransformPos(meshSource.Vertices, transform),
                meshSource.Vertices.Count, new Vector3(0, 0, 0));
            AddData(_normals, expectedTargetCount, TransformNorm(meshSource.Normals, transform),
                meshSource.Vertices.Count, new Vector3(0, 1, 0));
            AddData(_tangents, expectedTargetCount, TransformTangent(meshSource.Tangents, transform),
                meshSource.Vertices.Count, new Vector4(1, 0, 0, 1));
            AddData(_colors, expectedTargetCount, meshSource.Colors, meshSource.Vertices.Count,
                new Color32(255, 255, 255, 255));
            AddData(_texCoords0, expectedTargetCount, meshSource.TexCoords0, meshSource.Vertices.Count,
                new Vector2(0, 0));
            AddData(_texCoords1, expectedTargetCount, meshSource.TexCoords1, meshSource.Vertices.Count,
                new Vector2(0, 0));
            AddData(_texCoords2, expectedTargetCount, meshSource.TexCoords2, meshSource.Vertices.Count,
                new Vector2(0, 0));
            AddData(_texCoords3, expectedTargetCount, meshSource.TexCoords3, meshSource.Vertices.Count,
                new Vector2(0, 0));

            for (var i = 0; i < mesh.subMeshCount; ++i)
                buildMapping.AddSubmeshIndices(startIndex, meshSource.GetGeomtery(i).GetIndices(0));

            return buildMapping;
        }

        private IList<Vector3> TransformPos(IList<Vector3> meshSourceVertices, Matrix4x4 transform)
        {
            var res = new Vector3[meshSourceVertices.Count];
            for (var index = 0; index < meshSourceVertices.Count; index++)
            {
                var pos = meshSourceVertices[index];
                res[index] = transform.MultiplyPoint(pos);
            }

            return res;
        }

        private IList<Vector3> TransformNorm(IList<Vector3> meshSourceVertices, Matrix4x4 transform)
        {
            var res = new Vector3[meshSourceVertices.Count];
            for (var index = 0; index < meshSourceVertices.Count; index++)
            {
                var pos = meshSourceVertices[index];
                res[index] = transform.MultiplyVector(pos);
            }

            return res;
        }

        private IList<Vector4> TransformTangent(IList<Vector4> meshSourceVertices, Matrix4x4 transform)
        {
            var res = new Vector4[meshSourceVertices.Count];
            for (var index = 0; index < meshSourceVertices.Count; index++)
            {
                var pos = meshSourceVertices[index];
                var t = transform.MultiplyVector(new Vector3(pos.x, pos.y, pos.z));
                res[index] = new Vector4(t.x, t.y, t.z, pos.w);
            }

            return res;
        }

        private void AddData<T>(List<T> targetContainer, int expectedTargetCount, IList<T> sourceContainer,
            int expectedCount, T defaultValue)
        {
            if (targetContainer.Count == 0 && sourceContainer.Count == 0)
                return;
            while (targetContainer.Count < expectedTargetCount) targetContainer.Add(defaultValue);

            if (sourceContainer.Count == 0)
                for (var i = 0; i < expectedCount; i++)
                    targetContainer.Add(defaultValue);
            else
                targetContainer.AddRange(sourceContainer);
        }
        private struct MaterialTopology
        {
            public Material material;
            public MeshTopology topology;
        }

        private struct MeshInstanceKey
        {
            public Matrix4x4 transform;
            public Mesh mesh;
        }

        private class MeshMapping
        {
            public readonly List<List<int>> Indices = new List<List<int>>();

            public MeshMapping(Renderer renderer, Mesh mesh, Matrix4x4 transform)
            {
                Transform = transform;
                Source = new MeshSource(mesh, renderer as SkinnedMeshRenderer);
            }

            public MeshSource Source { get; }

            public Matrix4x4 Transform { get; }

            public void AddSubmeshIndices(int startIndex, IEnumerable<int> getIndices)
            {
                Indices.Add(getIndices.Select(_ => _ + startIndex).ToList());
            }
        }

        private class SubmeshReference
        {
            public MeshMapping Mesh;
            public int Submesh;
        }

        private class Geometry : IMeshGeometry
        {
            private readonly Material _material;
            private readonly List<Lod> _lods;
            private readonly List<List<int>> _refs;

            public Geometry(Material material, List<Lod> lods, MeshTopology topology)
            {
                _material = material;
                _lods = lods;
                _refs = lods.Select(_ => new List<int>()).ToList();
                Topology = topology;
            }

            public int NumLods
            {
                get
                {
                    if (_lods.Count == 0)
                        return 0;
                    if (_lods[_lods.Count - 1].ScreenRelativeTransitionHeight < 1e-6f)
                        return _lods.Count;
                    return _lods.Count + 1;
                }
            }

            public MeshTopology Topology {get; }

            public void AddReference(int lodIndex, SubmeshReference reference)
            {
                _refs[lodIndex].AddRange(reference.Mesh.Indices[reference.Submesh]);
            }

            public IList<int> GetIndices(int lod)
            {
                if (lod >= _refs.Count)
                    return Array.Empty<int>();
                return _refs[lod];
            }

            public float GetLodDistance(int lod)
            {
                if (lod == 0)
                    return 0;
                return _lods[lod - 1].GetDistance();
            }
        }

        private class Lod
        {
            private readonly LOD _lod;
            private float _sizeFactor = 1;

            public Lod(LOD lod)
            {
                _lod = lod;
            }

            public float ScreenRelativeTransitionHeight => _lod.screenRelativeTransitionHeight;

            public float GetDistance()
            {
                return _sizeFactor * 1.0f / Math.Max(_lod.screenRelativeTransitionHeight, 1e-6f);
            }

            public void SetSizeFactor(float size)
            {
                _sizeFactor = Math.Max(1e-6f, size);
            }
        }
    }
}