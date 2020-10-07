using System;
using System.Collections.Generic;
using UnityEngine;

namespace UnityToCustomEngineExporter.Editor
{
    public class LODGroupSource : AbstractMeshSource, IMeshSource
    {
        public LODGroupSource(LODGroup lodGroup)
        {
            var materialToSubmesh = new Dictionary<Material, int>();
            foreach (var lod in lodGroup.GetLODs())
            foreach (var lodRenderer in lod.renderers)
            {
                var subIndices = new int[lodRenderer.materials.Length];
                for (var index = 0; index < lodRenderer.materials.Length; index++)
                    if (materialToSubmesh.TryGetValue(lodRenderer.materials[index], out var matIndex))
                    {
                        subIndices[index] = matIndex;
                    }
                    else
                    {
                        matIndex = materialToSubmesh.Count;
                        subIndices[index] = matIndex;
                        materialToSubmesh.Add(lodRenderer.materials[index], matIndex);
                    }

                if (lodRenderer is SkinnedMeshRenderer skinnedMeshRenderer)
                    throw new NotImplementedException();
                if (lodRenderer is MeshRenderer renderer)
                    foreach (var subIndex in subIndices)
                    {
                    }
            }
        }

        public override IList<Vector3> Vertices { get; }

        public override int SubMeshCount { get; }

        public override IMeshGeometry GetGeomtery(int subMeshIndex)
        {
            throw new NotImplementedException();
        }

        private class Geometry : IMeshGeometry
        {
            private readonly IList<int>[] _lods;

            public Geometry(IList<int>[] lods)
            {
                _lods = lods;
            }

            public int NumLods => _lods.Length;

            public IList<int> GetIndices(int lod)
            {
                return _lods[lod];
            }
        }
    }
}