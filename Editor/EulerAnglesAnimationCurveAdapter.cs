using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace UnityToCustomEngineExporter.Editor
{
    public class EulerAnglesAnimationCurveAdapter : AnimationCurveAdapterBase, IAnimationCurveAdapter<Quaternion>
    {
        private readonly string _xTrackName;
        private readonly string _yTrackName;
        private readonly string _zTrackName;
        private AnimationCurve _xTrack;
        private AnimationCurve _yTrack;
        private AnimationCurve _zTrack;

        public EulerAnglesAnimationCurveAdapter(string propertyName)
        {
            _xTrackName = propertyName + ".x";
            _yTrackName = propertyName + ".y";
            _zTrackName = propertyName + ".z";
        }

        public Quaternion Evaluate(float t)
        {
            var rot = Vector3.zero;
            if (_xTrack != null) rot.x = _xTrack.Evaluate(t);
            if (_yTrack != null) rot.y = _yTrack.Evaluate(t);
            if (_zTrack != null) rot.z = _zTrack.Evaluate(t);
            return Quaternion.Euler(rot);
        }

        public bool HasProperty(string propertyName)
        {
            return propertyName == _xTrackName
                   || propertyName == _yTrackName
                   || propertyName == _zTrackName;
        }

        public void PickTracks(AnimationClip clip, IEnumerable<EditorCurveBinding> bindings)
        {
            _xTrack = null;
            _yTrack = null;
            _zTrack = null;
            foreach (var binding in bindings)
            {
                if (binding.propertyName == _xTrackName)
                    _xTrack = GetEditorCurve(clip, binding);
                else if (binding.propertyName == _yTrackName)
                    _yTrack = GetEditorCurve(clip, binding);
                else if (binding.propertyName == _zTrackName)
                    _zTrack = GetEditorCurve(clip, binding);
            }
        }

        public bool HasTracks => _xTrack != null && _yTrack != null && _zTrack != null;
    }
}