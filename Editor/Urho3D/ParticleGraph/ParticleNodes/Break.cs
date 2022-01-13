using UnityEngine;

using UnityToCustomEngineExporter.Editor.Urho3D.Graph;

namespace UnityToCustomEngineExporter.Editor.Urho3D.ParticleGraph.ParticleNodes
{
    public partial class Break : GraphNode
    {
        public Break() : base("Break")
        {
        }

        public static Break Break_vec_x_y_z(GraphNode vec)
        {
            var res = new Break();
            res.In.Add(new GraphInPin("vec", VariantType.Vector3)).Connect(vec);
            res.Out.Add(new GraphOutPin("x", VariantType.Float));
            res.Out.Add(new GraphOutPin("y", VariantType.Float));
            res.Out.Add(new GraphOutPin("z", VariantType.Float));
            return res;
        }

        public static Break Break_vec_x_y_z(GraphOutPin vec)
        {
            var res = new Break();
            res.In.Add(new GraphInPin("vec", VariantType.Vector3)).TargetPin = vec;
            res.Out.Add(new GraphOutPin("x", VariantType.Float));
            res.Out.Add(new GraphOutPin("y", VariantType.Float));
            res.Out.Add(new GraphOutPin("z", VariantType.Float));
            return res;
        }

        public static Break Break_vec_x_y(GraphNode vec)
        {
            var res = new Break();
            res.In.Add(new GraphInPin("vec", VariantType.Vector2)).Connect(vec);
            res.Out.Add(new GraphOutPin("x", VariantType.Float));
            res.Out.Add(new GraphOutPin("y", VariantType.Float));
            return res;
        }

        public static Break Break_vec_x_y(GraphOutPin vec)
        {
            var res = new Break();
            res.In.Add(new GraphInPin("vec", VariantType.Vector2)).TargetPin = vec;
            res.Out.Add(new GraphOutPin("x", VariantType.Float));
            res.Out.Add(new GraphOutPin("y", VariantType.Float));
            return res;
        }

        public static Break Break_q_x_y_z_w(GraphNode q)
        {
            var res = new Break();
            res.In.Add(new GraphInPin("q", VariantType.Quaternion)).Connect(q);
            res.Out.Add(new GraphOutPin("x", VariantType.Float));
            res.Out.Add(new GraphOutPin("y", VariantType.Float));
            res.Out.Add(new GraphOutPin("z", VariantType.Float));
            res.Out.Add(new GraphOutPin("w", VariantType.Float));
            return res;
        }

        public static Break Break_q_x_y_z_w(GraphOutPin q)
        {
            var res = new Break();
            res.In.Add(new GraphInPin("q", VariantType.Quaternion)).TargetPin = q;
            res.Out.Add(new GraphOutPin("x", VariantType.Float));
            res.Out.Add(new GraphOutPin("y", VariantType.Float));
            res.Out.Add(new GraphOutPin("z", VariantType.Float));
            res.Out.Add(new GraphOutPin("w", VariantType.Float));
            return res;
        }

        public static Break Break_q_axis_angle(GraphNode q)
        {
            var res = new Break();
            res.In.Add(new GraphInPin("q", VariantType.Quaternion)).Connect(q);
            res.Out.Add(new GraphOutPin("axis", VariantType.Vector3));
            res.Out.Add(new GraphOutPin("angle", VariantType.Float));
            return res;
        }

        public static Break Break_q_axis_angle(GraphOutPin q)
        {
            var res = new Break();
            res.In.Add(new GraphInPin("q", VariantType.Quaternion)).TargetPin = q;
            res.Out.Add(new GraphOutPin("axis", VariantType.Vector3));
            res.Out.Add(new GraphOutPin("angle", VariantType.Float));
            return res;
        }
    }
}