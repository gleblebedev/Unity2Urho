using UnityEngine;

namespace UnityToCustomEngineExporter.Editor
{
    public class DecalDecalProjector: ReflectionProxy
    {
        public DecalDecalProjector(Component instance): base(instance)
        {
        }
        //public Vector3 decalOffset
        //{
        //    get
        //    {
        //        return GetValue<Vector3>(nameof(decalOffset));
        //    }
        //}
        public Vector3 size
        {
            get
            {
                return GetValue<Vector3>(nameof(size));
            }
        }

        public Vector3 offset
        {
            get
            {
                return GetValue<Vector3>("m_Offset");
            }
        }

        public Material material
        {
            get
            {
                return GetValue<Material>(nameof(material));
            }
        }
    }
}