using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace UnityToCustomEngineExporter.Editor.Urho3D
{
    public class AnimationControllerExporter
    {
        private readonly Urho3DEngine _engine;

        public AnimationControllerExporter(Urho3DEngine engine)
        {
            _engine = engine;
        }

        public string EvaluateAnimationControllerName(AnimatorController clip, PrefabContext prefabContext)
        {
            if (clip == null)
                return null;
            var relPath = ExportUtils.GetRelPathFromAsset(_engine.Options.Subfolder, clip);
            return ExportUtils.ReplaceExtension(relPath,".json");
        }

        public void ExportAnimationController(AnimatorController animationController, PrefabContext prefabContext)
        {
            using (var fileStream = _engine.TryCreate(animationController.GetKey(),
                EvaluateAnimationControllerName(animationController, prefabContext), ExportUtils.GetLastWriteTimeUtc(animationController)))
            {
                if (fileStream == null)
                    return;
                using (var streamWriter = new StreamWriter(fileStream, new UTF8Encoding(false)))
                {
                    var conrollerData = new ControllerJson(animationController, _engine, prefabContext);
                    streamWriter.Write(EditorJsonUtility.ToJson(conrollerData, true));
                }
            }
        }
        [Serializable]
        public class LayerJson
        {
            public LayerJson(AnimatorControllerLayer layer, Urho3DEngine engine, PrefabContext prefabContext)
            {
                stateMachine = new StateMachineJson(layer.stateMachine, engine, prefabContext);
            }

            [SerializeField] public StateMachineJson stateMachine;
        }
        [Serializable]
        public class ChildMotionJson
        {
            [SerializeField] public string animationClip;
            [SerializeField] public bool hasBlendTree;
            [SerializeField] public BlendTreeJson blendTree;
            [SerializeField] public float cycleOffset;

            public ChildMotionJson(ChildMotion childMotion, Urho3DEngine engine, PrefabContext prefabContext)
            {
                this.cycleOffset = childMotion.cycleOffset;
                var motion = childMotion.motion;
                if (motion is AnimationClip animationClip)
                {
                    this.animationClip = engine.EvaluateAnimationName(animationClip, prefabContext);
                    engine.ScheduleAssetExport(animationClip, prefabContext);
                }
                else if (motion is BlendTree blendTree)
                {
                    this.hasBlendTree = true;
                    this.blendTree = new BlendTreeJson(blendTree, engine, prefabContext);
                }
            }
        }
        [Serializable]
        public class BlendTreeJson
        {
            [SerializeField] public string name;
            [SerializeField] public ChildMotionJson[] children;
            [SerializeField] public string blendParameter;
            [SerializeField] public string blendParameterY;
            [SerializeField] public BlendTreeType blendType;
            [SerializeField] public float maxThreshold;
            [SerializeField] public float minThreshold;
            [SerializeField] public bool useAutomaticThresholds;
            [SerializeField] public float apparentSpeed;
            [SerializeField] public float averageAngularSpeed;
            [SerializeField] public float averageDuration;
            [SerializeField] public Vector3 averageSpeed;
            [SerializeField] public bool isHumanMotion;
            [SerializeField] public bool isLooping;
            [SerializeField] public bool legacy;

            public BlendTreeJson(BlendTree blendTree, Urho3DEngine engine, PrefabContext prefabContext)
            {
                this.name = blendTree.name;
                this.blendParameter = blendTree.blendParameter;
                this.blendParameterY = blendTree.blendParameterY;
                this.blendType = blendTree.blendType;
                this.maxThreshold = blendTree.maxThreshold;
                this.minThreshold = blendTree.minThreshold;
                this.useAutomaticThresholds = blendTree.useAutomaticThresholds;
                this.apparentSpeed = blendTree.apparentSpeed;
                this.averageAngularSpeed = blendTree.averageAngularSpeed;
                this.averageDuration = blendTree.averageDuration;
                this.averageSpeed = blendTree.averageSpeed;
                this.isHumanMotion = blendTree.isHumanMotion;
                this.isLooping = blendTree.isLooping;
                this.legacy = blendTree.legacy;
                this.children = blendTree.children.Select(_ => new ChildMotionJson(_, engine, prefabContext)).ToArray();
            }
        }
        [Serializable]
        public class StateJson
        {
            public StateJson(AnimatorState state, Urho3DEngine engine, PrefabContext prefabContext)
            {
                this.name = state.name;
                this.speed = state.speed;
                this.cycleOffset = state.cycleOffset;
                var motion = state.motion;
                if (motion is AnimationClip animationClip)
                {
                    this.animationClip = engine.EvaluateAnimationName(animationClip, prefabContext);
                    engine.ScheduleAssetExport(animationClip, prefabContext);
                }
                else if (motion is BlendTree blendTree)
                {
                    this.hasBlendTree = true;
                    this.blendTree = new BlendTreeJson(blendTree, engine, prefabContext);
                }

                transitions = state.transitions.Select(_ => new TransitionJson(_, engine, prefabContext)).ToArray();
            }

            [SerializeField] public string name;
            [SerializeField] public float speed;
            [SerializeField] public string animationClip;
            [SerializeField] public bool hasBlendTree;
            [SerializeField] public BlendTreeJson blendTree;
            [SerializeField] public TransitionJson[] transitions;
            [SerializeField] public float cycleOffset;
        }
        [Serializable]
        public class ConditionJson
        {
            [SerializeField] public AnimatorConditionMode mode;
            [SerializeField] public string parameter;
            [SerializeField] public float threshold;

            public ConditionJson(AnimatorCondition animatorCondition, Urho3DEngine engine, PrefabContext prefabContext)
            {
                this.mode = animatorCondition.mode;
                this.parameter = animatorCondition.parameter;
                this.threshold = animatorCondition.threshold;
            }
        }
        [Serializable]
        public class TransitionJson
        {
            [SerializeField] public string destinationState;
            [SerializeField] public float duration;
            [SerializeField] public  bool hasFixedDuration;
            [SerializeField] public  bool canTransitionToSelf;
            [SerializeField] public  float exitTime;
            [SerializeField] public  bool hasExitTime;
            [SerializeField] public  float offset;
            [SerializeField] public  bool orderedInterruption;
            [SerializeField] public  bool isExit;
            [SerializeField] public  bool mute;
            [SerializeField] public  bool solo;
            [SerializeField] public ConditionJson[] conditions;

            public TransitionJson(AnimatorStateTransition transition, Urho3DEngine engine, PrefabContext prefabContext)
            {
                this.destinationState = transition.destinationState.name;
                this.duration = transition.duration;
                this.hasFixedDuration = transition.hasFixedDuration;
                this.canTransitionToSelf = transition.canTransitionToSelf;
                this.exitTime = transition.exitTime;
                this.hasExitTime = transition.hasExitTime;
                this.offset = transition.offset;
                this.orderedInterruption = transition.orderedInterruption;
                this.conditions = transition.conditions.Select(_=>new ConditionJson(_, engine, prefabContext)).ToArray();
                this.isExit = transition.isExit;
                this.mute = transition.mute;
                this.solo = transition.solo;
            }
        }
        [Serializable]
        public class StateMachineJson
        {
            [SerializeField] public StateJson[] states;

            public StateMachineJson(AnimatorStateMachine stateMachine, Urho3DEngine engine, PrefabContext prefabContext)
            {
                states = stateMachine.states.Select(_ => new StateJson(_.state, engine, prefabContext)).ToArray();
            }
        }
        [Serializable]
        public class ControllerJson
        {
            public ControllerJson(AnimatorController animationController, Urho3DEngine engine, PrefabContext prefabContext)
            {
                this.name = animationController.name;
                layers = animationController.layers.Select(_ => new LayerJson(_, engine, prefabContext)).ToArray();
            }

            [SerializeField] public string name;
            [SerializeField] public LayerJson[] layers;
        }
    }
}