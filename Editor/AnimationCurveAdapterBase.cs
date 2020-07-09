using UnityEditor;
using UnityEngine;

namespace UnityToCustomEngineExporter.Editor
{
    public abstract class AnimationCurveAdapterBase
    {
        protected static AnimationCurve GetEditorCurve(AnimationClip clip, EditorCurveBinding curveBinding)
        {
            if (curveBinding.propertyName != null)
                return AnimationUtility.GetEditorCurve(clip, curveBinding);
            return null;
        }
    }
}