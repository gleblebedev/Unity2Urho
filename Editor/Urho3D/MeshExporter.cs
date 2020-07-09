using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.ProBuilder;
using UnityEngine.SceneManagement;
using Math = System.Math;
using Object = UnityEngine.Object;

namespace UnityToCustomEngineExporter.Editor.Urho3D
{
    public class MeshExporter
    {
        public const uint Magic2 = 0x32444d55;

        

        private static readonly string[] animationProperties =
        {
            "m_LocalPosition.x",
            "m_LocalPosition.y",
            "m_LocalPosition.z",
            "m_LocalRotation.w",
            "m_LocalRotation.x",
            "m_LocalRotation.y",
            "m_LocalRotation.z",
            "m_LocalScale.x",
            "m_LocalScale.y",
            "m_LocalScale.z"
        };

        private readonly Urho3DEngine _engine;

        private readonly List<GameObject> _skeletons = new List<GameObject>();

        private readonly HashSet<Mesh> _meshes = new HashSet<Mesh>();
        private HashSet<Material> _materials = new HashSet<Material>();

        private readonly Dictionary<Object, string> _dynamicMeshNames = new Dictionary<Object, string>();

        public MeshExporter(Urho3DEngine engine)
        {
            _engine = engine;
        }

        internal interface ISampler : IDisposable
        {
            void Sample(float time);
        }

        public void ExportAnimation(AnimationClip clipAnimation)
        {
            var name = GetSafeFileName(clipAnimation.name);

            //_assetCollection.AddAnimationPath(clipAnimation, fileName);

            var aniFilePath = EvaluateAnimationName(clipAnimation);
            using (var file = _engine.TryCreate(clipAnimation.GetKey(), aniFilePath,
                ExportUtils.GetLastWriteTimeUtc(clipAnimation)))
            {
                if (file == null)
                    return;
                using (var writer = new BinaryWriter(file))
                {
                    writer.Write(new byte[] {0x55, 0x41, 0x4e, 0x49});
                    WriteStringSZ(writer, clipAnimation.name);
                    writer.Write(clipAnimation.length);

                    if (clipAnimation.legacy)
                    {
                        WriteTracksAsIs(clipAnimation, writer);
                    }
                    else
                    {
                        var allBindings = AnimationUtility.GetCurveBindings(clipAnimation);
                        var rootBones =
                            new HashSet<string>(allBindings.Select(_ => GetRootBoneName(_)).Where(_ => _ != null));
                        if (rootBones.Count != 1)
                        {
                            Debug.LogWarning(aniFilePath + ": Multiple root bones found (" +
                                             string.Join(", ", rootBones.ToArray()) +
                                             "), falling back to curve export");
                            WriteTracksAsIs(clipAnimation, writer);
                        }
                        else
                        {
                            var rootBoneName = rootBones.First();
                            var rootGOs = _skeletons
                                .Select(_ =>
                                    _.name == rootBoneName ? _.transform : _.transform.Find(rootBoneName))
                                .Where(_ => _ != null).ToList();
                            if (rootGOs.Count == 1)
                            {
                                WriteSkelAnimation(clipAnimation, rootGOs.First().gameObject, writer);
                            }
                            else
                            {
                                Debug.LogWarning(aniFilePath +
                                                 ": Multiple game objects found that match root bone name, falling back to curve export");
                                WriteTracksAsIs(clipAnimation, writer);
                            }
                        }
                    }
                }
            }
        }

        public void ExportMesh(ProBuilderMesh proBuilderMesh)
        {
            ExportProBuilderMeshModel(proBuilderMesh);
        }
        public string ExportMesh(NavMeshTriangulation mesh)
        {
            var mdlFilePath = EvaluateMeshName(mesh);
            ExportMeshModel(new NavMeshSource(mesh), mdlFilePath, SceneManager.GetActiveScene().GetRootGameObjects().FirstOrDefault().GetKey(), DateTime.MaxValue);
            return mdlFilePath;
        }
        public void ExportMesh(GameObject go)
        {
            var proBuilderMesh = go.GetComponent<ProBuilderMesh>();
            var skinnedMeshRenderer = go.GetComponent<SkinnedMeshRenderer>();
            var meshFilter = go.GetComponent<MeshFilter>();

            //Debug.Log("Game object: "+go.name+", components: "+string.Join(", ", go.GetComponents<Component>().Select(_=>_.GetType().Name).ToArray()));

            if (proBuilderMesh != null)
            {
                ExportProBuilderMeshModel(proBuilderMesh);
            }
            else
            {
                Mesh mesh = null;
                if (skinnedMeshRenderer != null)
                    mesh = skinnedMeshRenderer.sharedMesh;
                else if (meshFilter != null) mesh = meshFilter.sharedMesh;
                var meshSource = new MeshSource(mesh, skinnedMeshRenderer);
                ExportMeshModel(meshSource, EvaluateMeshName(mesh), mesh.GetKey(), ExportUtils.GetLastWriteTimeUtc(mesh));
            }


            for (var i = 0; i < go.transform.childCount; ++i) ExportMesh(go.transform.GetChild(i).gameObject);
        }

        private void ExportProBuilderMeshModel(ProBuilderMesh proBuilderMesh)
        {
            ExportMeshModel(new ProBuilderMeshSource(proBuilderMesh), EvaluateMeshName(proBuilderMesh), proBuilderMesh.GetKey(), ExportUtils.GetLastWriteTimeUtc(proBuilderMesh));
        }

        public void ExportMeshModel(IMeshSource mesh, string mdlFilePath, AssetKey key, DateTime lastWriteDateTimeUtc)
        {
            using (var file = _engine.TryCreate(key, mdlFilePath, lastWriteDateTimeUtc))
            {
                if (file != null)
                    using (var writer = new BinaryWriter(file))
                    {
                        WriteMesh(writer, mesh);
                    }
            }
        }

        public string EvaluateAnimationName(AnimationClip clip)
        {
            if (clip == null)
                return null;
            return ExportUtils.ReplaceExtension(ExportUtils.GetRelPathFromAsset(_engine.Subfolder, clip), "") + "/" +
                   ExportUtils.SafeFileName(clip.name) + ".ani";
        }

        public string EvaluateMeshName(Mesh mesh)
        {
            if (mesh == null)
                return null;
            return ExportUtils.ReplaceExtension(ExportUtils.GetRelPathFromAsset(_engine.Subfolder, mesh), "") + "/" +
                   ExportUtils.SafeFileName(mesh.name) + ".mdl";
        }

        public string EvaluateMeshName(ProBuilderMesh mesh)
        {
            if (mesh == null)
                return null;
            if (_dynamicMeshNames.TryGetValue(mesh, out var name))
                return name;

            var assetUrhoAssetName = ExportUtils.GetRelPathFromAsset(_engine.Subfolder, mesh);
            if (string.IsNullOrWhiteSpace(assetUrhoAssetName))
            {
                name = _engine.TempFolder + ExportUtils.SafeFileName(mesh.name) + "." + _dynamicMeshNames.Count + ".mdl";
                _dynamicMeshNames.Add(mesh, name);
                return name;
            }

            return ExportUtils.ReplaceExtension(assetUrhoAssetName, "") + "/" +
                   ExportUtils.SafeFileName(mesh.name) + ".mdl";
        }

        public string EvaluateMeshName(NavMeshTriangulation mesh)
        {
            return _engine.TempFolder + "NavMesh.mdl";
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

        private void WriteSkelAnimation(AnimationClip clipAnimation, GameObject root, BinaryWriter writer)
        {
            var trackBones = CloneTree(root).Select(_ => new BoneTrack(_)).ToList();
            var cloneRoot = trackBones[0].gameObject;
            ISampler sampler;
            if (clipAnimation.legacy)
                sampler = new LegacySampler(cloneRoot, clipAnimation);
            else
                sampler = new AnimatorSampler(cloneRoot, clipAnimation);
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

            writer.Write(trackBones.Count);
            foreach (var bone in trackBones)
            {
                WriteStringSZ(writer, bone.gameObject.name);
                writer.Write((byte) 7);
                writer.Write(bone.translation.Count);
                for (var frame = 0; frame < bone.translation.Count; ++frame)
                {
                    writer.Write(bone.keys[frame]);
                    Write(writer, bone.translation[frame]);
                    Write(writer, bone.rotation[frame]);
                    Write(writer, bone.scale[frame]);
                }
            }

            //foreach (var bone in trackBones)
            //{
            //    bone.Reset();
            //}
            Object.DestroyImmediate(trackBones[0].gameObject);
        }

        private void WriteTracksAsIs(AnimationClip clipAnimation, BinaryWriter writer)
        {
            var positionAdapter = new Vector3AnimationCurveAdapter("m_LocalPosition", Vector3.zero);
            var rotationAdapter = new QuaternionAnimationCurveAdapter("m_LocalRotation");
            var eulerAnglesRawAdapter = new EulerAnglesAnimationCurveAdapter("localEulerAnglesRaw");
            var scaleAdapter = new Vector3AnimationCurveAdapter("m_LocalScale", Vector3.one);
            var allAdapters = new IAnimationCurveAdapter[] {positionAdapter, rotationAdapter ,scaleAdapter, eulerAnglesRawAdapter };
            var allBindings = AnimationUtility.GetCurveBindings(clipAnimation);

            var bindingGroups = allBindings.Where(_ => allAdapters.Any(a=>a.HasProperty(_.propertyName))).GroupBy(_ => _.path)
                .OrderBy(_ => _.Key.Length).ToArray();
            var timeStep = 1.0f / clipAnimation.frameRate;
            var numKeyFrames = 1 + (int) (clipAnimation.length * clipAnimation.frameRate);

            var numTracks = (uint) bindingGroups.Length;
            writer.Write(numTracks);
            foreach (var group in bindingGroups)
            {
                foreach (var adapter in allAdapters)
                {
                    adapter.PickTracks(clipAnimation, @group);
                }

                var boneName = group.Key;
                boneName = boneName.Substring(boneName.LastIndexOf('/') + 1);
                WriteStringSZ(writer, boneName);

                IAnimationCurveAdapter<Vector3> position = null;
                if (positionAdapter.HasTracks)
                    position = positionAdapter;

                IAnimationCurveAdapter<Quaternion> rotation = new IAnimationCurveAdapter<Quaternion>[]{ rotationAdapter, eulerAnglesRawAdapter }.FirstOrDefault(_=>_.HasTracks);

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

        private string GetRootBoneName(EditorCurveBinding editorCurveBinding)
        {
            var path = editorCurveBinding.path;
            if (string.IsNullOrEmpty(path))
                return path;
            var slash = path.IndexOf('/');
            if (slash < 0)
                return path;
            return path.Substring(0, slash);
        }

        private Urho3DBone[] BuildBones(IMeshSource skinnedMeshRenderer)
        {
            if (skinnedMeshRenderer == null || skinnedMeshRenderer.BonesCount == 0)
                return new Urho3DBone[0];
            //var unityBones = skinnedMeshRenderer.bones;
            var bones = new Urho3DBone[skinnedMeshRenderer.BonesCount];
            for (var index = 0; index < bones.Length; index++)
            {
                var bone = new Urho3DBone();
                var unityBone = skinnedMeshRenderer.GetBoneTransform(index);
                var parentIndex = skinnedMeshRenderer.GetBoneParent(index);
                if (parentIndex.HasValue)
                {
                    bone.parent = parentIndex.Value;
                }

                bone.name = unityBone.name ?? "bone" + index;
                //if (bone.parent != 0)
                //{
                bone.actualPos = unityBone.localPosition;
                bone.actualRot = unityBone.localRotation;
                bone.actualScale = unityBone.localScale;
                //}
                //else
                //{
                //    bone.actualPos = unityBone.position;
                //    bone.actualRot = unityBone.rotation;
                //    bone.actualScale = unityBone.lossyScale;
                //}

                bone.binding = skinnedMeshRenderer.GetBoneBindPose(index);
                bones[index] = bone;
            }

            return bones;
        }

        private string GetSafeFileName(string name)
        {
            if (name == null)
                return "";
            foreach (var c in Path.GetInvalidFileNameChars()) name = name.Replace(c, '_');

            return name;
        }

        private void WriteMesh(BinaryWriter writer, IMeshSource mesh)
        {
            writer.Write(Magic2);
            writer.Write(1);
            for (var vbIndex = 0; vbIndex < 1 /*_mesh.vertexBufferCount*/; ++vbIndex)
            {
                var positions = mesh.Vertices;
                var normals = mesh.Normals;
                var colors = mesh.Colors;
                var tangents = mesh.Tangents;
                var boneWeights = mesh.BoneWeights;
                var uvs = mesh.TexCoords0;
                var uvs2 = mesh.TexCoords1;
                var uvs3 = mesh.TexCoords2;
                var uvs4 = mesh.TexCoords3;

                writer.Write(positions.Count);
                var elements = new List<MeshStreamWriter>();
                if (positions.Count > 0)
                    elements.Add(new MeshVector3Stream(positions, VertexElementSemantic.SEM_POSITION));
                if (normals.Count > 0)
                    elements.Add(new MeshVector3Stream(normals, VertexElementSemantic.SEM_NORMAL));
                if (boneWeights.Length > 0)
                {
                    var indices = new Vector4[boneWeights.Length];
                    var weights = new Vector4[boneWeights.Length];
                    for (var i = 0; i < boneWeights.Length; ++i)
                    {
                        indices[i] = new Vector4(boneWeights[i].boneIndex0, boneWeights[i].boneIndex1,
                            boneWeights[i].boneIndex2, boneWeights[i].boneIndex3);
                        weights[i] = new Vector4(boneWeights[i].weight0, boneWeights[i].weight1,
                            boneWeights[i].weight2, boneWeights[i].weight3);
                    }

                    elements.Add(new MeshVector4Stream(weights, VertexElementSemantic.SEM_BLENDWEIGHTS));
                    elements.Add(new MeshUByte4Stream(indices, VertexElementSemantic.SEM_BLENDINDICES));
                }

                if (colors.Count > 0)
                {
                    elements.Add(new MeshColor32Stream(colors, VertexElementSemantic.SEM_COLOR));
                }
                if (tangents.Count > 0)
                    elements.Add(new MeshVector4Stream(FlipW(tangents), VertexElementSemantic.SEM_TANGENT));
                if (uvs.Count > 0)
                    elements.Add(new MeshUVStream(uvs, VertexElementSemantic.SEM_TEXCOORD));
                if (uvs2.Count > 0)
                    elements.Add(new MeshUVStream(uvs2, VertexElementSemantic.SEM_TEXCOORD, 1));
                if (uvs3.Count > 0)
                    elements.Add(new MeshUVStream(uvs2, VertexElementSemantic.SEM_TEXCOORD, 2));
                if (uvs4.Count > 0)
                    elements.Add(new MeshUVStream(uvs2, VertexElementSemantic.SEM_TEXCOORD, 3));
                writer.Write(elements.Count);
                for (var i = 0; i < elements.Count; ++i)
                    writer.Write(elements[i].Element);
                var morphableVertexRangeStartIndex = 0;
                var morphableVertexCount = 0;
                writer.Write(morphableVertexRangeStartIndex);
                writer.Write(morphableVertexCount);
                for (var index = 0; index < positions.Count; ++index)
                for (var i = 0; i < elements.Count; ++i)
                    elements[i].Write(writer, index);
                var indicesPerSubMesh = new List<IList<int>>();
                var totalIndices = 0;
                for (var subMeshIndex = 0; subMeshIndex < mesh.SubMeshCount; ++subMeshIndex)
                {
                    var indices = mesh.GetIndices(subMeshIndex);
                    indicesPerSubMesh.Add(indices);
                    totalIndices += indices.Count;
                }

                writer.Write(1);
                writer.Write(totalIndices);
                if (positions.Count < 65536)
                {
                    writer.Write(2);
                    for (var subMeshIndex = 0; subMeshIndex < mesh.SubMeshCount; ++subMeshIndex)
                    for (var i = 0; i < indicesPerSubMesh[subMeshIndex].Count; ++i)
                        writer.Write((ushort) indicesPerSubMesh[subMeshIndex][i]);
                }
                else
                {
                    writer.Write(4);
                    for (var subMeshIndex = 0; subMeshIndex < mesh.SubMeshCount; ++subMeshIndex)
                    for (var i = 0; i < indicesPerSubMesh[subMeshIndex].Count; ++i)
                        writer.Write(indicesPerSubMesh[subMeshIndex][i]);
                }

                writer.Write(indicesPerSubMesh.Count);
                totalIndices = 0;
                for (var subMeshIndex = 0; subMeshIndex < indicesPerSubMesh.Count; ++subMeshIndex)
                {
                    var numberOfBoneMappingEntries = 0;
                    writer.Write(numberOfBoneMappingEntries);
                    var numberOfLODLevels = 1;
                    writer.Write(numberOfLODLevels);
                    writer.Write(0.0f);
                    writer.Write((int) PrimitiveType.TRIANGLE_LIST);
                    writer.Write(0);
                    writer.Write(0);
                    writer.Write(totalIndices);
                    writer.Write(indicesPerSubMesh[subMeshIndex].Count);
                    totalIndices += indicesPerSubMesh[subMeshIndex].Count;
                }

                var numMorphTargets = 0;
                writer.Write(numMorphTargets);

                var bones = BuildBones(mesh);
                var numOfBones = bones.Length;
                writer.Write(numOfBones);
                int boneIndex = 0;
                foreach (var bone in bones)
                {

                    WriteStringSZ(writer, bone.name);
                    writer.Write(bone.parent); //Parent
                    Write(writer, bone.actualPos);
                    Write(writer, bone.actualRot);
                    Write(writer, bone.actualScale);

                    var d = new[]
                    {
                        bone.binding.m00, bone.binding.m01, bone.binding.m02, bone.binding.m03,
                        bone.binding.m10, bone.binding.m11, bone.binding.m12, bone.binding.m13,
                        bone.binding.m20, bone.binding.m21, bone.binding.m22, bone.binding.m23
                    };
                    foreach (var v in d) writer.Write(v);

                    using (var e = GetBoneVertices(boneWeights, boneIndex).GetEnumerator())
                    {
                        if (!e.MoveNext())
                        {
                            writer.Write((byte) 3);
                            //R
                            writer.Write(0.1f);
                            //BBox
                            Write(writer, new Vector3(-0.1f, -0.1f, -0.1f));
                            Write(writer, new Vector3(0.1f, 0.1f, 0.1f));
                        }
                        else
                        {
                            var binding = bone.binding;
                            //binding = binding.inverse;
                            var center = binding.MultiplyPoint(positions[e.Current]);
                            var min = center;
                            var max = center;

                            while (e.MoveNext())
                            {
                                var originalPosition = positions[e.Current];
                                var p = binding.MultiplyPoint(originalPosition);
                                if (p.x < min.x) min.x = p.x;
                                if (p.y < min.y) min.y = p.y;
                                if (p.z < min.z) min.z = p.z;
                                if (p.x > max.x) max.x = p.x;
                                if (p.y > max.y) max.y = p.y;
                                if (p.z > max.z) max.z = p.z;
                            }

                            writer.Write((byte) 3);
                            //R
                            writer.Write(Math.Max(max.magnitude, min.magnitude));
                            //BBox
                            Write(writer, min);
                            Write(writer, max);
                        }
                    }


                    ++boneIndex;
                }

                float minX, minY, minZ;
                float maxX, maxY, maxZ;
                maxX = maxY = maxZ = float.MinValue;
                minX = minY = minZ = float.MaxValue;
                for (var i = 0; i < positions.Count; ++i)
                {
                    if (minX > positions[i].x)
                        minX = positions[i].x;
                    if (minY > positions[i].y)
                        minY = positions[i].y;
                    if (minZ > positions[i].z)
                        minZ = positions[i].z;
                    if (maxX < positions[i].x)
                        maxX = positions[i].x;
                    if (maxY < positions[i].y)
                        maxY = positions[i].y;
                    if (maxZ < positions[i].z)
                        maxZ = positions[i].z;
                }

                writer.Write(minX);
                writer.Write(minY);
                writer.Write(minZ);
                writer.Write(maxX);
                writer.Write(maxY);
                writer.Write(maxZ);
            }
        }

        private Vector4[] FlipW(IList<Vector4> tangents)
        {
            var res = new Vector4[tangents.Count];
            for (var index = 0; index < tangents.Count; index++)
            {
                var tangent = tangents[index];
                res[index] = new Vector4(tangent.x, tangent.y, tangent.z, -tangent.w);
            }

            return res;
        }

        private IEnumerable<int> GetBoneVertices(BoneWeight[] boneWeights, int boneIndex, float threshold = 0.01f)
        {
            if (boneWeights == null)
                yield break;
            for (var index = 0; index < boneWeights.Length; index++)
            {
                var boneWeight = boneWeights[index];
                var useVertex = boneWeight.boneIndex0 == boneIndex && boneWeight.weight0 >= threshold
                                || boneWeight.boneIndex1 == boneIndex && boneWeight.weight1 >= threshold
                                || boneWeight.boneIndex2 == boneIndex && boneWeight.weight2 >= threshold
                                || boneWeight.boneIndex3 == boneIndex && boneWeight.weight3 >= threshold;
                if (useVertex) yield return index;
            }
        }

        private void Write(BinaryWriter writer, Quaternion v)
        {
            writer.Write(v.w);
            writer.Write(v.x);
            writer.Write(v.y);
            writer.Write(v.z);
        }

        private void Write(BinaryWriter writer, Vector3 v)
        {
            writer.Write(v.x);
            writer.Write(v.y);
            writer.Write(v.z);
        }

        private void WriteStringSZ(BinaryWriter writer, string boneName)
        {
            var a = new UTF8Encoding(false).GetBytes(boneName + '\0');
            writer.Write(a);
        }

        public class Urho3DBone
        {
            public string name;
            public int parent;
            public Vector3 actualPos = Vector3.zero;
            public Quaternion actualRot = Quaternion.identity;
            public Vector3 actualScale = Vector3.one;
            public Matrix4x4 binding = Matrix4x4.identity;
        }

        internal abstract class MeshStreamWriter
        {
            public int Element;
            public abstract void Write(BinaryWriter writer, int index);
        }

        internal class MeshVector3Stream : MeshStreamWriter
        {
            private readonly IList<Vector3> positions;

            public MeshVector3Stream(IList<Vector3> positions, VertexElementSemantic sem, int index = 0)
            {
                this.positions = positions;
                Element = (int) VertexElementType.TYPE_VECTOR3 | ((int) sem << 8) | (index << 16);
            }

            public override void Write(BinaryWriter writer, int index)
            {
                writer.Write(positions[index].x);
                writer.Write(positions[index].y);
                writer.Write(positions[index].z);
            }
        }

        internal class MeshUVStream : MeshStreamWriter
        {
            private readonly IList<Vector2> positions;

            public MeshUVStream(IList<Vector2> positions, VertexElementSemantic sem, int index = 0)
            {
                this.positions = positions;
                Element = (int) VertexElementType.TYPE_VECTOR2 | ((int) sem << 8) | (index << 16);
            }

            public override void Write(BinaryWriter writer, int index)
            {
                writer.Write(positions[index].x);
                writer.Write(1.0f - positions[index].y);
            }
        }

        internal class MeshVector2Stream : MeshStreamWriter
        {
            private readonly Vector2[] positions;

            public MeshVector2Stream(Vector2[] positions, VertexElementSemantic sem, int index = 0)
            {
                this.positions = positions;
                Element = (int) VertexElementType.TYPE_VECTOR2 | ((int) sem << 8) | (index << 16);
            }

            public override void Write(BinaryWriter writer, int index)
            {
                writer.Write(positions[index].x);
                writer.Write(positions[index].y);
            }
        }

        internal class MeshVector4Stream : MeshStreamWriter
        {
            private readonly Vector4[] positions;

            public MeshVector4Stream(Vector4[] positions, VertexElementSemantic sem, int index = 0)
            {
                this.positions = positions;
                Element = (int) VertexElementType.TYPE_VECTOR4 | ((int) sem << 8) | (index << 16);
            }

            public override void Write(BinaryWriter writer, int index)
            {
                writer.Write(positions[index].x);
                writer.Write(positions[index].y);
                writer.Write(positions[index].z);
                writer.Write(positions[index].w);
            }
        }
        internal class MeshColor32Stream : MeshStreamWriter
        {
            private readonly IList<Color32> colors;

            public MeshColor32Stream(IList<Color32> colors, VertexElementSemantic sem, int index = 0)
            {
                this.colors = colors;
                Element = (int)VertexElementType.TYPE_UBYTE4_NORM | ((int)sem << 8) | (index << 16);
            }

            public override void Write(BinaryWriter writer, int index)
            {
                writer.Write(colors[index].r);
                writer.Write(colors[index].g);
                writer.Write(colors[index].b);
                writer.Write(colors[index].a);
            }
        }
        internal class MeshUByte4Stream : MeshStreamWriter
        {
            private readonly Vector4[] positions;

            public MeshUByte4Stream(Vector4[] positions, VertexElementSemantic sem, int index = 0)
            {
                this.positions = positions;
                Element = (int) VertexElementType.TYPE_UBYTE4 | ((int) sem << 8) | (index << 16);
            }

            public override void Write(BinaryWriter writer, int index)
            {
                writer.Write((byte) positions[index].x);
                writer.Write((byte) positions[index].y);
                writer.Write((byte) positions[index].z);
                writer.Write((byte) positions[index].w);
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
                //_animation = _root.AddComponent<Animation>();
                //_animation.clip = animationClip;
                //_animation.AddClip(animationClip, animationClip.name);
                //_animation.Play(animationClip.name);
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

                _controllerPath = Path.Combine(Path.Combine("Assets", "UnityToCustomEngineExporter"),
                    "TempController.controller");
                _controller =
                    AnimatorController.CreateAnimatorControllerAtPathWithClip(_controllerPath, _animationClip);
                var layers = _controller.layers;
                layers[0].iKPass = true;
                //layers[0].stateMachine.
                _controller.layers = layers;
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
            public readonly List<Vector3> translation = new List<Vector3>();
            public readonly List<Quaternion> rotation = new List<Quaternion>();
            public readonly List<Vector3> scale = new List<Vector3>();

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
        }

        //public string EvaluateMeshName(ProBuilderMesh mesh)
        //{
        //    if (mesh == null)
        //        return null;
        //    return ExportUtils.ReplaceExtension(ExportUtils.GetRelPathFromAsset(mesh.gameObject), "") + "/" + ExportUtils.SafeFileName(mesh.name) + ".mdl";
        //}
    }
}