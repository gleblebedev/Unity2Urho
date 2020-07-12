using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace UnityToCustomEngineExporter.Editor
{
    public class QuaternionAnimationCurveAdapter : AnimationCurveAdapterBase, IAnimationCurveAdapter<Quaternion>
    {
        private readonly string _xTrackName;
        private readonly string _yTrackName;
        private readonly string _zTrackName;
        private readonly string _wTrackName;
        private AnimationCurve _xTrack;
        private AnimationCurve _yTrack;
        private AnimationCurve _zTrack;
        private AnimationCurve _wTrack;

        public QuaternionAnimationCurveAdapter(string propertyName)
        {
            _xTrackName = propertyName + ".x";
            _yTrackName = propertyName + ".y";
            _zTrackName = propertyName + ".z";
            _wTrackName = propertyName + ".w";
        }

        public bool HasTracks => _xTrack != null && _yTrack != null && _zTrack != null && _wTrack != null;

        public bool HasProperty(string propertyName)
        {
            return propertyName == _xTrackName
                   || propertyName == _yTrackName
                   || propertyName == _zTrackName
                   || propertyName == _wTrackName;
        }

        public void PickTracks(AnimationClip clip, IEnumerable<EditorCurveBinding> bindings)
        {
            _xTrack = null;
            _yTrack = null;
            _zTrack = null;
            _wTrack = null;
            foreach (var binding in bindings)
                if (binding.propertyName == _xTrackName)
                    _xTrack = GetEditorCurve(clip, binding);
                else if (binding.propertyName == _yTrackName)
                    _yTrack = GetEditorCurve(clip, binding);
                else if (binding.propertyName == _zTrackName)
                    _zTrack = GetEditorCurve(clip, binding);
                else if (binding.propertyName == _wTrackName)
                    _wTrack = GetEditorCurve(clip, binding);
        }

        public Quaternion Evaluate(float t)
        {
            var rot = Quaternion.identity;
            if (_wTrack != null) rot.w = _wTrack.Evaluate(t);
            if (_xTrack != null) rot.x = _xTrack.Evaluate(t);
            if (_yTrack != null) rot.y = _yTrack.Evaluate(t);
            if (_zTrack != null) rot.z = _zTrack.Evaluate(t);
            return rot.normalized;
        }
    }
}