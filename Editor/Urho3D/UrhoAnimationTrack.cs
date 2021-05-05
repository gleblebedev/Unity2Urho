using System.Collections.Generic;
using UnityEngine;

namespace UnityToCustomEngineExporter.Editor.Urho3D
{
    public class UrhoAnimationTrack
    {
        public string BoneName { get; set; }
        public IList<float> Keyframes { get; set; }
        public IList<Vector3> Positions { get; set; }
        public IList<Quaternion> Rotations { get; set; }
        public IList<Vector3> Scales { get; set; }
    }

    public class UrhoAnimationFile
    {
        public List<UrhoAnimationTrack> Tracks { get; private set; } = new List<UrhoAnimationTrack>();
        public string RootBone { get; set; }
        public Vector3? LinearVelocity { get; set; }
    }
}