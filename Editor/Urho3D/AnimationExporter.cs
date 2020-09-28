using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using Object = UnityEngine.Object;

namespace UnityToCustomEngineExporter.Editor.Urho3D
{
    public class AnimationExporter: AbstractBinaryExpoerter
    {
        private readonly List<GameObject> _skeletons = new List<GameObject>();

        private readonly Urho3DEngine _engine;

        public AnimationExporter(Urho3DEngine engine)
        {
            _engine = engine;
        }

        private void WriteSkelAnimation(AnimationClip clipAnimation, GameObject root, BinaryWriter writer)
        {
            var trackBones = CloneTree(root).Select(_ => new BoneTrack(_)).ToList();
            var cloneRoot = trackBones[0].gameObject;
            ISampler sampler;
            if (!clipAnimation.isHumanMotion)
                sampler = new LegacySampler(cloneRoot, clipAnimation);
            else
                sampler = new AnimatorSampler(cloneRoot, clipAnimation);
            using (sampler)
            {
                var timeStep = 1.0f / clipAnimation.frameRate;
                var numKeyFrames = 1 + (int)(clipAnimation.length * clipAnimation.frameRate);

                for (var frame = 0; frame < numKeyFrames; ++frame)
                {
                    var t = frame * timeStep;
                    sampler.Sample(t);
                    //clipAnimation.SampleAnimation(cloneRoot, t);
                    //foreach (var trackBone in trackBones)
                    //{
                    //    clipAnimation.SampleAnimation(trackBone.gameObject, t);
                    //}
                    foreach (var trackBone in trackBones) trackBone.Sample(t);
                }
            }

            writer.Write(trackBones.Count);
            foreach (var bone in trackBones)
            {
                bone.Optimize();
            }
            foreach (var bone in trackBones)
            {
                var mask = 0;
                bool hasTranslation = false;
                bool hasRotation = false;
                bool hasScale = false;
                if (bone.translation != null && bone.translation.Count > 0)
                {
                    mask |= 1;
                    hasTranslation = true;
                }

                if (bone.rotation != null && bone.rotation.Count > 0)
                {
                    mask |= 2;
                    hasRotation = true;
                }

                if (bone.scale != null && bone.scale.Count > 0)
                {
                    mask |= 4;
                    hasScale = true;
                }
                if (mask == 0)
                    continue;
                WriteStringSZ(writer, _engine.DecorateName(bone.gameObject.name));
                writer.Write((byte)mask);
                writer.Write(bone.keys.Count);
                for (var frame = 0; frame < bone.keys.Count; ++frame)
                {
                    writer.Write(bone.keys[frame]);
                    if (hasTranslation) Write(writer, bone.translation[frame]);
                    if (hasRotation) Write(writer, bone.rotation[frame]);
                    if (hasScale) Write(writer, bone.scale[frame]);
                }
            }

            //foreach (var bone in trackBones)
            //{
            //    bone.Reset();
            //}
            Object.DestroyImmediate(trackBones[0].gameObject);
        }

        private class BoneTrack
        {
            public readonly GameObject gameObject;
            public readonly List<float> keys = new List<float>();
            public List<Vector3> translation = new List<Vector3>();
            public List<Quaternion> rotation = new List<Quaternion>();
            public List<Vector3> scale = new List<Vector3>();

            public readonly Vector3 originalTranslation;
            public readonly Quaternion originalRotation;
            public readonly Vector3 originalScale;


            public BoneTrack(GameObject gameObject)
            {
                this.gameObject = gameObject;
                originalTranslation = gameObject.transform.localPosition;
                originalRotation = gameObject.transform.localRotation;
                originalScale = gameObject.transform.localScale;
            }

            public override string ToString()
            {
                return gameObject.name ?? base.ToString();
            }

            public void Reset()
            {
                gameObject.transform.localPosition = originalTranslation;
                gameObject.transform.localRotation = originalRotation;
                gameObject.transform.localScale = originalScale;
            }

            public void Sample(float t)
            {
                keys.Add(t);
                translation.Add(gameObject.transform.localPosition);
                rotation.Add(gameObject.transform.localRotation);
                scale.Add(gameObject.transform.localScale);
            }

            public void Optimize()
            {
                if (scale != null)
                    if (scale.All(_ => _ == Vector3.one))
                        scale = null;
                if (rotation != null)
                    if (rotation.All(_ => _ == Quaternion.identity))
                        rotation = null;
                if (translation != null)
                    if (translation.All(_ => _ == Vector3.zero))
                        translation = null;
            }
        }

        private void WriteTracksAsIs(AnimationClip clipAnimation, BinaryWriter writer)
        {
            var positionAdapter = new Vector3AnimationCurveAdapter("m_LocalPosition", Vector3.zero);
            var rotationAdapter = new QuaternionAnimationCurveAdapter("m_LocalRotation");
            var eulerAnglesRawAdapter = new EulerAnglesAnimationCurveAdapter("localEulerAnglesRaw");
            var scaleAdapter = new Vector3AnimationCurveAdapter("m_LocalScale", Vector3.one);
            var allAdapters = new IAnimationCurveAdapter[]
                {positionAdapter, rotationAdapter, scaleAdapter, eulerAnglesRawAdapter};
            var allBindings = AnimationUtility.GetCurveBindings(clipAnimation);

            var bindingGroups = allBindings.Where(_ => allAdapters.Any(a => a.HasProperty(_.propertyName)))
                .GroupBy(_ => _.path)
                .OrderBy(_ => _.Key.Length).ToArray();
            var timeStep = 1.0f / clipAnimation.frameRate;
            var numKeyFrames = 1 + (int)(clipAnimation.length * clipAnimation.frameRate);

            var numTracks = (uint)bindingGroups.Length;
            writer.Write(numTracks);
            foreach (var group in bindingGroups)
            {
                foreach (var adapter in allAdapters) adapter.PickTracks(clipAnimation, group);

                var boneName = group.Key;
                boneName = boneName.Substring(boneName.LastIndexOf('/') + 1);
                WriteStringSZ(writer, boneName);

                IAnimationCurveAdapter<Vector3> position = null;
                if (positionAdapter.HasTracks)
                    position = positionAdapter;

                var rotation =
                    new IAnimationCurveAdapter<Quaternion>[] { rotationAdapter, eulerAnglesRawAdapter }.FirstOrDefault(
                        _ => _.HasTracks);

                IAnimationCurveAdapter<Vector3> scale = null;
                if (scaleAdapter.HasTracks)
                    scale = scaleAdapter;

                byte trackMask = 0;
                if (position != null)
                    trackMask |= 1;
                if (rotation != null)
                    trackMask |= 2;
                if (scale != null)
                    trackMask |= 4;
                writer.Write(trackMask);
                writer.Write(numKeyFrames);
                for (var frame = 0; frame < numKeyFrames; ++frame)
                {
                    var t = frame * timeStep;
                    writer.Write(t);

                    if ((trackMask & 1) != 0)
                    {
                        var pos = position.Evaluate(t);
                        Write(writer, pos);
                    }

                    if ((trackMask & 2) != 0)
                    {
                        var rot = rotation.Evaluate(t);
                        Write(writer, rot);
                    }

                    if ((trackMask & 4) != 0)
                    {
                        var scaleV = scale.Evaluate(t);
                        Write(writer, scaleV);
                    }
                }
            }
        }

        internal class LegacySampler : ISampler
        {
            private readonly GameObject _root;
            private readonly AnimationClip _animationClip;
            private Animation _animation;

            public LegacySampler(GameObject root, AnimationClip animationClip)
            {
                _root = root;
                _animationClip = animationClip;
            }

            public void Dispose()
            {
            }

            public void Sample(float time)
            {
                _animationClip.SampleAnimation(_root, time);
                //AnimationState state = _animation[_animationClip.name];
                //if (state != null)
                //{
                //    state.enabled = true;
                //    state.time = time;
                //    state.weight = 1;
                //}
                //_animation.Sample();
            }
        }

        internal class AnimatorSampler : ISampler
        {
            private readonly GameObject _root;
            private readonly AnimationClip _animationClip;
            private readonly Animator _animator;
            private readonly AnimatorController _controller;
            private readonly string _controllerPath;
            private readonly float _length;

            public AnimatorSampler(GameObject root, AnimationClip animationClip)
            {
                _root = root;
                _animationClip = animationClip;
                _length = _animationClip.length;
                if (_length < 1e-6f) _length = 1e-6f;
                _animator = _root.AddComponent<Animator>();

                _controllerPath = Path.Combine("Assets", "UnityToCustomEngineExporter.TempController.controller");
                _controller =
                    AnimatorController.CreateAnimatorControllerAtPathWithClip(_controllerPath, _animationClip);
                var layers = _controller.layers;
                layers[0].iKPass = true;
                //layers[0].stateMachine.
                _controller.layers = layers;
                _animator.avatar = GetAvatar(animationClip);
                _animator.applyRootMotion = true;
                _animator.runtimeAnimatorController = _controller;
            }

            public void Dispose()
            {
                AssetDatabase.DeleteAsset(_controllerPath);
            }

            public void Sample(float time)
            {
                var aniStateInfo = _animator.GetCurrentAnimatorStateInfo(0);
                _animator.Play(aniStateInfo.shortNameHash, 0, time / _length);
                _animator.Update(0f);
            }
        }
        private string GetRootBoneName(EditorCurveBinding editorCurveBinding)
        {
            var path = editorCurveBinding.path;
            if (string.IsNullOrEmpty(path))
                return path;
            var slash = path.IndexOf('/');
            if (slash < 0)
                return path;
            return _engine.DecorateName(path.Substring(0, slash));
        }
        internal interface ISampler : IDisposable
        {
            void Sample(float time);
        }

        public void ExportAnimation(AnimationClip clip, PrefabContext prefabContext)
        {
            if (!_engine.Options.ExportAnimations)
                return;

            var aniFilePath = EvaluateAnimationName(clip, prefabContext);

            ExportMetadata(ExportUtils.ReplaceExtension(aniFilePath, ".xml"), clip, prefabContext);

            using (var file = _engine.TryCreate(clip.GetKey(), aniFilePath,
                ExportUtils.GetLastWriteTimeUtc(clip)))
            {
                if (file == null)
                    return;
                using (var writer = new BinaryWriter(file))
                {
                    writer.Write(new byte[] { 0x55, 0x41, 0x4e, 0x49 });
                    WriteStringSZ(writer, _engine.DecorateName(ExportUtils.GetName(clip)));
                    writer.Write(clip.length);

                    // Legacy animation
                    if (clip.legacy)
                    {
                        WriteTracksAsIs(clip, writer);
                    }
                    else if (clip.isHumanMotion)
                    {
                        WriteHumanoidAnimation(clip, writer);
                    }
                    else
                    {
                        WriteGenericAnimation(clip, writer);
                    }
                }
            }

        }

        private void ExportMetadata(string metadataFileName, AnimationClip clip, PrefabContext prefabContext)
        {
            using (var file = _engine.TryCreateXml(clip.GetKey(), metadataFileName, ExportUtils.GetLastWriteTimeUtc(clip)))
            {
                if (file == null)
                    return;

                file.WriteStartElement("animation");
                file.WriteWhitespace(Environment.NewLine);
                foreach (var clipEvent in clip.events)
                {
                    file.WriteWhitespace("\t");
                    file.WriteStartElement("trigger");
                    file.WriteAttributeString("time", BaseNodeExporter.Format(clipEvent.time));
                    file.WriteAttributeString("type", "String");
                    file.WriteAttributeString("value", clipEvent.functionName);
                    file.WriteEndElement();
                    file.WriteWhitespace(Environment.NewLine);
                }
                file.WriteEndElement();
                file.WriteWhitespace(Environment.NewLine);
            }
        }

        private void WriteHumanoidAnimation(AnimationClip clip, BinaryWriter writer)
        {
            Avatar avatar = GetAvatar(clip);
            if (avatar != null)
            {
                if (WriteHumanoidAnimation(clip, avatar, writer))
                    return;
            }
            Debug.Log("Failed to export "+clip.name+". Try to change it to Generic or Legacy setup.");
            var numTracks = (uint)0;
            writer.Write(numTracks);
         }

        private bool WriteHumanoidAnimation(AnimationClip clip, Avatar avatar, BinaryWriter writer)
        {
            var avatarPath = AssetDatabase.GetAssetPath(avatar);
            var prefabRoot = AssetDatabase.LoadAssetAtPath(avatarPath, typeof(GameObject)) as GameObject;
            if (prefabRoot == null)
                return false;

            WriteSkelAnimation(clip, prefabRoot, writer);
            return true;
        }

        private static Avatar GetAvatar(AnimationClip clip)
        {
            var clipPath = AssetDatabase.GetAssetPath(clip);
            var importer = AssetImporter.GetAtPath(clipPath) as ModelImporter;
            return importer?.sourceAvatar;
        }

        private void WriteGenericAnimation(AnimationClip clip, BinaryWriter writer)
        {
            var allBindings = AnimationUtility.GetCurveBindings(clip);
            var rootBones =
                new HashSet<string>(allBindings.Select(_ => GetRootBoneName(_)).Where(_ => _ != null));
            if (rootBones.Count != 1)
            {
                Debug.LogWarning(clip.name + ": Multiple root bones found (" +
                                 string.Join(", ", rootBones.ToArray()) +
                                 "), falling back to curve export");
                WriteTracksAsIs(clip, writer);
            }
            else
            {
                var rootBoneName = rootBones.First();
                var rootGOs = _skeletons
                    .Select(_ => _.name == rootBoneName ? _.transform : _.transform.Find(rootBoneName))
                    .Where(_ => _ != null).ToList();
                if (rootGOs.Count == 1)
                {
                    WriteSkelAnimation(clip, rootGOs.First().gameObject, writer);
                }
                else
                {
                    Debug.LogWarning(clip.name +
                                     ": Multiple game objects found that match root bone name, falling back to curve export");
                    WriteTracksAsIs(clip, writer);
                }
            }
        }

        public string EvaluateAnimationName(AnimationClip clip, PrefabContext prefabContext)
        {
            if (clip == null)
                return null;
            var relPath = ExportUtils.GetRelPathFromAsset(_engine.Options.Subfolder, clip);
            if (Path.GetExtension(relPath).ToLowerInvariant() == ".anim")
                return ExportUtils.ReplaceExtension(relPath, ".ani");
            var folder = ExportUtils.ReplaceExtension(relPath, "");
            if (string.IsNullOrWhiteSpace(folder))
            {
                folder = prefabContext.TempFolder;
            }
            return ExportUtils.Combine(folder, ExportUtils.SafeFileName(_engine.DecorateName(ExportUtils.GetName(clip))) + ".ani");
        }
        private IEnumerable<GameObject> CloneTree(GameObject go)
        {
            if (go == null)
                yield break;
            var clone = new GameObject();
            clone.name = go.name;
            clone.transform.localPosition = go.transform.localPosition;
            clone.transform.localScale = go.transform.localScale;
            clone.transform.localRotation = go.transform.localRotation;
            yield return clone;
            for (var i = 0; i < go.transform.childCount; ++i)
                foreach (var gameObject in CloneTree(go.transform.GetChild(i).gameObject))
                {
                    if (gameObject.transform.parent == null) gameObject.transform.SetParent(clone.transform, false);

                    yield return gameObject;
                }
        }
    }
}