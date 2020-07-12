using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace UnityToCustomEngineExporter.Editor
{
    public class Vector3AnimationCurveAdapter : AnimationCurveAdapterBase, IAnimationCurveAdapter<Vector3>
    {
        private readonly Vector3 _defaultValue;
        private readonly string _xTrackName;
        private readonly string _yTrackName;
        private readonly string _zTrackName;
        private AnimationCurve _xTrack;
        private AnimationCurve _yTrack;
        private AnimationCurve _zTrack;

        public Vector3AnimationCurveAdapter(string propertyName, Vector3 defaultValue)
        {
            _defaultValue = defaultValue;
            _xTrackName = propertyName + ".x";
            _yTrackName = propertyName + ".y";
            _zTrackName = propertyName + ".z";
        }

        public bool HasTracks => _xTrack != null && _yTrack != null && _zTrack != null;

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
                if (binding.propertyName == _xTrackName)
                    _xTrack = GetEditorCurve(clip, binding);
                else if (binding.propertyName == _yTrackName)
                    _yTrack = GetEditorCurve(clip, binding);
                else if (binding.propertyName == _zTrackName)
                    _zTrack = GetEditorCurve(clip, binding);
        }

        public Vector3 Evaluate(float t)
        {
            var pos = _defaultValue;
            if (_xTrack != null) pos.x = _xTrack.Evaluate(t);
            if (_yTrack != null) pos.y = _yTrack.Evaluate(t);
            if (_zTrack != null) pos.z = _zTrack.Evaluate(t);
            return pos;
        }
    }
}