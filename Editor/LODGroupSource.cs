using System;
using System.Collections.Generic;
using UnityEngine;

namespace UnityToCustomEngineExporter.Editor
{
    public class LODGroupSource: AbstractMeshSource, IMeshSource
    {
        public LODGroupSource(LODGroup lodGroup)
        {

        }

        public override IList<Vector3> Vertices { get; }
        
        public override int SubMeshCount { get; }

        public override IList<int> GetIndices(int subMeshIndex)
        {
            return Array.Empty<int>();
        }
    }
}