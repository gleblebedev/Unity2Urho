using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using Object = UnityEngine.Object;

namespace UnityToCustomEngineExporter.Editor.Urho3D
{
    public class AnimationExporter : AbstractBinaryExpoerter
    {
        private readonly Urho3DEngine _engine;

        public AnimationExporter(Urho3DEngine engine)
        {
            _engine = engine;
        }

        internal interface ISampler : IDisposable
        {
            void Sample(float time);
        }

        private static Avatar GetAvatar(AnimationClip clip)
        {
            var clipPath = AssetDatabase.GetAssetPath(clip);
            var importer = AssetImporter.GetAtPath(clipPath) as ModelImporter;
            return importer?.sourceAvatar;
        }

        public void ExportAnimation(AnimationClip clip, PrefabContext prefabContext)
        {
            if (!_engine.Options.ExportAnimations)
                return;

            var aniFilePath = EvaluateAnimationName(clip, prefabContext);

            using (var file = _engine.TryCreate(clip.GetKey(), aniFilePath,
                ExportUtils.GetLastWriteTimeUtc(clip)))
            {
                if (file != null)
                {
                    UrhoAnimationFile tracks;
                    
                    // Legacy animation
                    if (clip.legacy)
                        tracks = WriteTracksAsIs(clip);
                    else if (clip.isHumanMotion)
                        tracks = WriteHumanoidAnimation(clip);
                    else
                        tracks = WriteGenericAnimation(clip);
                    
                    if (_engine.Options.EliminateRootMotion && IsLoopedAnimation(tracks))
                    {
                        var rootTrack = tracks.Tracks.FirstOrDefault(_ => _.BoneName == tracks.RootBone);
                        if (rootTrack?.Positions != null)
                        {
                            var lastKey = rootTrack.Keyframes.Count - 1;
                            var startPos = rootTrack.Positions[0];
                            var posDelta = rootTrack.Positions[lastKey] - startPos;
                            if (posDelta.sqrMagnitude > 1e-6f)
                            {
                                var startTime = rootTrack.Keyframes[0];
                                var dt = rootTrack.Keyframes[lastKey] - startTime;
                                var vel = posDelta * (1.0f/dt);
                                tracks.LinearVelocity = vel;
                                for (var index = 0; index < rootTrack.Positions.Count; index++)
                                {
                                    rootTrack.Positions[index] = rootTrack.Positions[index] - vel*(rootTrack.Keyframes[index]-startTime);
                                }
                            }
                        }
                    }
                    
                    using (var writer = new BinaryWriter(file))
                    {
                        writer.Write(new byte[] {0x55, 0x41, 0x4e, 0x49});
                        WriteStringSZ(writer, _engine.DecorateName(ExportUtils.GetName(_engine.NameCollisionResolver, clip)));
                        writer.Write(clip.length);

                        writer.Write(tracks.Tracks.Count);
                        foreach (var track in tracks.Tracks)
                        {
                            WriteStringSZ(writer, track.BoneName);
                            byte trackMask = 0;
                            if (track.Positions != null)
                                trackMask |= 1;
                            if (track.Rotations != null)
                                trackMask |= 2;
                            if (track.Scales != null)
                                trackMask |= 4;
                            writer.Write(trackMask);
                            writer.Write(track.Keyframes.Count);
                            for (var index = 0; index < track.Keyframes.Count; index++)
                            {
                                writer.Write(track.Keyframes[index]);

                                if (track.Positions != null)
                                {
                                    Write(writer, track.Positions[index]);
                                }

                                if (track.Rotations != null)
                                {
                                    Write(writer, track.Rotations[index]);
                                }

                                if (track.Scales != null)
                                {
                                    Write(writer, track.Scales[index]);
                                }
                            }
                        }
                    }

                    ExportMetadata(ExportUtils.ReplaceExtension(aniFilePath, ".xml"), clip, prefabContext, tracks);
                }
            }
        }

        private bool IsLoopedAnimation(UrhoAnimationFile tracks)
        {
            foreach (var track in tracks.Tracks)
            {
                if (track.BoneName != tracks.RootBone && track.Keyframes.Count > 1)
                {
                    if (track.Positions != null)
                    {
                        var start = track.Positions[0];
                        var end = track.Positions[track.Positions.Count-1];
                        if ((end - start).sqrMagnitude > 1e-6f)
                        {
                            return false;
                        }
                    }
                    if (track.Rotations != null)
                    {
                        var start = track.Rotations[0];
                        var end = track.Rotations[track.Rotations.Count - 1];
                        var startV = new Vector4(start.x, start.y, start.z, start.w);
                        var endV = new Vector4(end.x, end.y, end.z, end.w);

                        if ((endV - startV).sqrMagnitude > 1e-4f)
                        {
                            return false;
                        }
                    }
                    if (track.Scales != null)
                    {
                        var start = track.Scales[0];
                        var end = track.Scales[track.Scales.Count - 1];
                        if ((end - start).sqrMagnitude > 1e-6f)
                        {
                            return false;
                        }
                    }
                }
            }

            return true;
        }

        public string EvaluateAnimationName(AnimationClip clip, PrefabContext prefabContext)
        {
            if (clip == null)
                return null;
            var relPath = ExportUtils.GetRelPathFromAsset(_engine.Options.Subfolder, clip);
            if (Path.GetExtension(relPath).ToLowerInvariant() == ".anim")
                return ExportUtils.ReplaceExtension(relPath, ".ani");
            var folder = ExportUtils.ReplaceExtension(relPath, "");
            if (string.IsNullOrWhiteSpace(folder)) folder = prefabContext.TempFolder;
            return ExportUtils.Combine(folder,
                ExportUtils.SafeFileName(_engine.DecorateName(ExportUtils.GetName(_engine.NameCollisionResolver, clip))) + ".ani");
        }

        private UrhoAnimationFile WriteSkelAnimation(AnimationClip clipAnimation, GameObject root, Dictionary<string,string> avatarBones)
        {
            var animationFile = new UrhoAnimationFile();
            var trackBones = CloneTree(root).Select(_ => new BoneTrack(_, avatarBones.GetOrDefault(_.name))).ToList();
            var cloneRoot = trackBones[0].gameObject;
            ISampler sampler;
            if (!clipAnimation.isHumanMotion)
            {
                sampler = new LegacySampler(cloneRoot, clipAnimation);
                FilterBoneTracks(trackBones, clipAnimation);
            }
            else
            {
                sampler = new AnimatorSampler(cloneRoot, clipAnimation);
            }

            using (sampler)
            {
                var timeStep = 1.0f / clipAnimation.frameRate;
                var numKeyFrames = 1 + (int) (clipAnimation.length * clipAnimation.frameRate);

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

            foreach (var bone in trackBones)
            {
                bone.Optimize();
                // Disable translation for avatar bones.
                if (!string.IsNullOrWhiteSpace(bone.avatarBoneName) && bone.avatarBoneName != "Hips")
                {
                    bone.translation = null;
                }
            }
            foreach (var bone in trackBones)
            {
                var track = new UrhoAnimationTrack() { BoneName = _engine.DecorateName(bone.gameObject.name) };
                animationFile.Tracks.Add(track);
   
                if (bone.translation != null && bone.translation.Count > 0)
                {
                    track.Positions = new List<Vector3>(bone.keys.Count);
                }

                if (bone.rotation != null && bone.rotation.Count > 0)
                {
                    track.Rotations = new List<Quaternion>(bone.keys.Count);
                }

                if (bone.scale != null && bone.scale.Count > 0)
                {
                    track.Scales = new List<Vector3>(bone.keys.Count);
                }

                animationFile.Tracks.Add(track);
                track.Keyframes = new List<float>(bone.keys.Count);
                for (var frame = 0; frame < bone.keys.Count; ++frame)
                {
                    track.Keyframes.Add(bone.keys[frame]);
                    track.Positions?.Add(bone.translation[frame]);
                    track.Rotations?.Add(bone.rotation[frame]);
                    track.Scales?.Add(bone.scale[frame]);
                }
            }

            //foreach (var bone in trackBones)
            //{
            //    bone.Reset();
            //}
            Object.DestroyImmediate(trackBones[0].gameObject);

            return animationFile;
        }

        private void FilterBoneTracks(List<BoneTrack> trackBones, AnimationClip clipAnimation)
        {
            var allBindings = AnimationUtility.GetCurveBindings(clipAnimation);
        }

        private UrhoAnimationFile WriteTracksAsIs(AnimationClip clipAnimation)
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
            var numKeyFrames = 1 + (int) (clipAnimation.length * clipAnimation.frameRate);

            var numTracks = (uint) bindingGroups.Length;
            var animationFile = new UrhoAnimationFile();

            var possibleRootNodes = bindingGroups.Select(_ => (_.Key.IndexOf('/') < 0) ? _.Key : _.Key.Substring(0, _.Key.IndexOf('/')))
                .Distinct().ToList();

            if (possibleRootNodes.Count == 1)
            {
                animationFile.RootBone = possibleRootNodes[0];
            }
                
                
            foreach (var group in bindingGroups)
            {
                foreach (var adapter in allAdapters) adapter.PickTracks(clipAnimation, group);

                var track = new UrhoAnimationTrack();
                animationFile.Tracks.Add(track);
                var boneName = group.Key;
                boneName = boneName.Substring(boneName.LastIndexOf('/') + 1);
                track.BoneName = boneName;

                IAnimationCurveAdapter<Vector3> position = null;
                if (positionAdapter.HasTracks)
                    position = positionAdapter;

                var rotation =
                    new IAnimationCurveAdapter<Quaternion>[] {rotationAdapter, eulerAnglesRawAdapter}.FirstOrDefault(
                        _ => _.HasTracks);

                IAnimationCurveAdapter<Vector3> scale = null;
                if (scaleAdapter.HasTracks)
                    scale = scaleAdapter;

                track.Keyframes = new List<float>(numKeyFrames);
                if (position != null)
                {
                    track.Positions = new List<Vector3>(numKeyFrames);
                }

                if (rotation != null)
                {
                    track.Rotations = new List<Quaternion>(numKeyFrames);
                }

                if (scale != null)
                {
                    track.Scales = new List<Vector3>(numKeyFrames);
                }
                for (var frame = 0; frame < numKeyFrames; ++frame)
                {
                    var t = frame * timeStep;
                    track.Keyframes.Add(t);


                    track.Positions?.Add(position.Evaluate(t));

                    track.Rotations?.Add(rotation.Evaluate(t));

                    track.Scales?.Add(scale.Evaluate(t));
                }
            }

            return animationFile;
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

        private void ExportMetadata(string metadataFileName, AnimationClip clip, PrefabContext prefabContext, UrhoAnimationFile animation)
        {
            using (var file =
                _engine.TryCreateXml(clip.GetKey(), metadataFileName, ExportUtils.GetLastWriteTimeUtc(clip)))
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

                if (animation.LinearVelocity.HasValue)
                {
                    file.WriteWhitespace("\t");
                    file.WriteStartElement("metadata");
                    file.WriteAttributeString("name", "LinearVelocity");
                    file.WriteAttributeString("type", "Vector3");
                    file.WriteAttributeString("value", BaseNodeExporter.Format(animation.LinearVelocity.Value));
                    file.WriteEndElement();
                    file.WriteWhitespace(Environment.NewLine);
                }
                file.WriteEndElement();
                file.WriteWhitespace(Environment.NewLine);
            }
        }

        private UrhoAnimationFile WriteHumanoidAnimation(AnimationClip clip)
        {
            var avatar = GetAvatar(clip);
            if (avatar != null)
                return WriteHumanoidAnimation(clip, avatar);
            Debug.Log("Failed to export " + clip.name + ". Avatar not found. Try to change it to Generic or Legacy setup.");
            return new UrhoAnimationFile();
        }

        private UrhoAnimationFile WriteHumanoidAnimation(AnimationClip clip, Avatar avatar)
        {
            var avatarPath = AssetDatabase.GetAssetPath(avatar);
            var prefabRoot = AssetDatabase.LoadAssetAtPath(avatarPath, typeof(GameObject)) as GameObject;
            if (prefabRoot == null)
                return new UrhoAnimationFile();

            var avatarBones = avatar.humanDescription.human.ToDictionary(_ => _.boneName, _ => _.humanName);

            return WriteSkelAnimation(clip, prefabRoot, avatarBones);
        }

        private UrhoAnimationFile WriteGenericAnimation(AnimationClip clip)
        {
            var allBindings = AnimationUtility.GetCurveBindings(clip);
            var rootBones = new HashSet<string>(allBindings.Select(_ => GetRootBoneName(_)).Where(_ => _ != null));
            if (rootBones.Count != 1)
            {
                Debug.LogWarning(clip.name + ": Multiple root bones found (" +
                                 string.Join(", ", rootBones.ToArray()) +
                                 "), falling back to curve export");
                return WriteTracksAsIs(clip);
            }
            else
            {
                var rootBoneName = rootBones.First();

                var avatarPath = AssetDatabase.GetAssetPath(clip);
                var skeleton = AssetDatabase.LoadAssetAtPath<GameObject>(avatarPath);
                Transform rootBoneGO = null;
                if (skeleton != null)
                {
                    rootBoneGO = skeleton.name == rootBoneName
                        ? skeleton.transform
                        : skeleton.transform.Find(rootBoneName);
                }
                rootBoneGO = null;
                if (rootBoneGO != null)
                {
                    return WriteSkelAnimation(clip, rootBoneGO.gameObject, new Dictionary<string, string>());
                }
                else
                {
                    Debug.LogWarning(clip.name + ": Multiple game objects found that match root bone name, falling back to curve export");
                    return WriteTracksAsIs(clip);
                }
            }
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
                _controller = AnimatorController.CreateAnimatorControllerAtPathWithClip(_controllerPath, _animationClip);
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

        private class BoneTrack
        {
            public readonly GameObject gameObject;
            public readonly List<float> keys = new List<float>();

            public readonly Vector3 originalTranslation;
            public readonly Quaternion originalRotation;
            public readonly Vector3 originalScale;
            public List<Vector3> translation = new List<Vector3>();
            public List<Quaternion> rotation = new List<Quaternion>();
            public List<Vector3> scale = new List<Vector3>();
            public readonly string avatarBoneName;


            public BoneTrack(GameObject gameObject, string avatarBoneName = null)
            {
                this.gameObject = gameObject;
                this.avatarBoneName = avatarBoneName;
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
    }
}