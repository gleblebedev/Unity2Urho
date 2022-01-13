using UnityToCustomEngineExporter.Editor.Urho3D.Graph;

namespace UnityToCustomEngineExporter.Editor.Urho3D.ParticleGraph.ParticleNodes
{
    public partial class Make : GraphNode
    {
        public Make() : base("Make")
        {
        }

        public static Make Make_x_y_out(GraphNode x, GraphNode y)
        {
            var res = new Make();
            res.In.Add(new GraphInPin("x", VariantType.Float)).Connect(x);
            res.In.Add(new GraphInPin("y", VariantType.Float)).Connect(y);
            res.Out.Add(new GraphOutPin("out", VariantType.Vector2));
            return res;
        }

        public static Make Make_x_y_out(GraphOutPin x, GraphOutPin y)
        {
            var res = new Make();
            res.In.Add(new GraphInPin("x", VariantType.Float)).TargetPin = x;
            res.In.Add(new GraphInPin("y", VariantType.Float)).TargetPin = y;
            res.Out.Add(new GraphOutPin("out", VariantType.Vector2));
            return res;
        }

        public static Make Make_x_y_z_out(GraphNode x, GraphNode y, GraphNode z)
        {
            var res = new Make();
            res.In.Add(new GraphInPin("x", VariantType.Float)).Connect(x);
            res.In.Add(new GraphInPin("y", VariantType.Float)).Connect(y);
            res.In.Add(new GraphInPin("z", VariantType.Float)).Connect(z);
            res.Out.Add(new GraphOutPin("out", VariantType.Vector3));
            return res;
        }

        public static Make Make_x_y_z_out(GraphOutPin x, GraphOutPin y, GraphOutPin z)
        {
            var res = new Make();
            res.In.Add(new GraphInPin("x", VariantType.Float)).TargetPin = x;
            res.In.Add(new GraphInPin("y", VariantType.Float)).TargetPin = y;
            res.In.Add(new GraphInPin("z", VariantType.Float)).TargetPin = z;
            res.Out.Add(new GraphOutPin("out", VariantType.Vector3));
            return res;
        }

        public static Make Make_translation_rotation_scale_out(GraphNode translation, GraphNode rotation, GraphNode scale)
        {
            var res = new Make();
            res.In.Add(new GraphInPin("translation", VariantType.Vector3)).Connect(translation);
            res.In.Add(new GraphInPin("rotation", VariantType.Quaternion)).Connect(rotation);
            res.In.Add(new GraphInPin("scale", VariantType.Vector3)).Connect(scale);
            res.Out.Add(new GraphOutPin("out", VariantType.Matrix3x4));
            return res;
        }

        public static Make Make_translation_rotation_scale_out(GraphOutPin translation, GraphOutPin rotation, GraphOutPin scale)
        {
            var res = new Make();
            res.In.Add(new GraphInPin("translation", VariantType.Vector3)).TargetPin = translation;
            res.In.Add(new GraphInPin("rotation", VariantType.Quaternion)).TargetPin = rotation;
            res.In.Add(new GraphInPin("scale", VariantType.Vector3)).TargetPin = scale;
            res.Out.Add(new GraphOutPin("out", VariantType.Matrix3x4));
            return res;
        }

        public static Make Make_pitch_yaw_roll_out(GraphNode pitch, GraphNode yaw, GraphNode roll)
        {
            var res = new Make();
            res.In.Add(new GraphInPin("pitch", VariantType.Float)).Connect(pitch);
            res.In.Add(new GraphInPin("yaw", VariantType.Float)).Connect(yaw);
            res.In.Add(new GraphInPin("roll", VariantType.Float)).Connect(roll);
            res.Out.Add(new GraphOutPin("out", VariantType.Quaternion));
            return res;
        }

        public static Make Make_pitch_yaw_roll_out(GraphOutPin pitch, GraphOutPin yaw, GraphOutPin roll)
        {
            var res = new Make();
            res.In.Add(new GraphInPin("pitch", VariantType.Float)).TargetPin = pitch;
            res.In.Add(new GraphInPin("yaw", VariantType.Float)).TargetPin = yaw;
            res.In.Add(new GraphInPin("roll", VariantType.Float)).TargetPin = roll;
            res.Out.Add(new GraphOutPin("out", VariantType.Quaternion));
            return res;
        }

    }
}