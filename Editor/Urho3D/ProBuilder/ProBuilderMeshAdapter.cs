using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Assets.Unity2Urho.Editor.Urho3D.ProBuilder
{
    public class ProBuilderMeshAdapter
    {
        private readonly Behaviour _component;
        public IList<Vector3> positions;
        public IList<FaceAdapter> faces;
        public IList<Color> colors;
        public IList<Vector3> normals;
        public IList<Vector4> tangents;
        public IList<Vector2> textures;
        public bool enabled;

        public ProBuilderMeshAdapter(Behaviour component)
        {
            _component = component;
            var type = component.GetType();
            this.positions = type.GetProperty("positions")?.GetValue(_component) as IList<Vector3>;
            this.colors = type.GetProperty("colors")?.GetValue(_component) as IList<Color>;
            this.normals = type.GetProperty("normals")?.GetValue(_component) as IList<Vector3>;
            this.tangents = type.GetProperty("tangents")?.GetValue(_component) as IList<Vector4>;
            this.textures = type.GetProperty("textures")?.GetValue(_component) as IList<Vector2>;
            var facesSource = (type.GetProperty("faces")?.GetValue(_component) as IList);
            this.enabled = component.enabled;
            this.faces = new List<FaceAdapter>(facesSource.Count);
            foreach (var face in facesSource)
            {
                this.faces.Add(new FaceAdapter(face));
            }
        }

        public UnityEngine.Object Object => _component;

        public static ProBuilderMeshAdapter Get(GameObject gameObject)
        {
            var component = gameObject.GetComponent("ProBuilderMesh") as Behaviour;
            if (component == null)
                return null;
            return new ProBuilderMeshAdapter(component);
        }
    }

    public class FaceAdapter
    {
        private readonly object _face;
        public int submeshIndex;
        public IList<int> indexes;

        public FaceAdapter(object face)
        {
            _face = face;
            this.submeshIndex = (int)_face.GetType().GetProperty("submeshIndex").GetValue(_face);
            this.indexes = _face.GetType().GetProperty("indexes")?.GetValue(_face) as IList<int>;
        }
    }
}
