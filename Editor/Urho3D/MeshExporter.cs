using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using UnityEngine.ProBuilder;
using Math = System.Math;
using Object = UnityEngine.Object;
//using UnityEngine.ProBuilder;

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
            using (var file = _engine.TryCreate(clipAnimation.GetKey(), aniFilePath, ExportUtils.GetLastWriteTimeUtc(clipAnimation)))
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
                ExportMeshModel(mesh, skinnedMeshRenderer);
            }


            for (var i = 0; i < go.transform.childCount; ++i) ExportMesh(go.transform.GetChild(i).gameObject);
        }

        private void ExportProBuilderMeshModel(ProBuilderMesh mesh)
        {
            var mdlFilePath = EvaluateMeshName(mesh);
            using (var file = _engine.TryCreate(mesh.gameObject.GetKey(), mdlFilePath, ExportUtils.GetLastWriteTimeUtc(mesh)))
            {
                if (file != null)
                {
                    using (var writer = new BinaryWriter(file))
                    {
                        WriteProBuilderMesh(writer, mesh);
                    }
                }
            }
        }

        public void ExportMeshModel(Mesh mesh, SkinnedMeshRenderer skinnedMeshRenderer)
        {
            var mdlFilePath = EvaluateMeshName(mesh);
            using (var file = _engine.TryCreate(mesh.GetKey(), mdlFilePath, ExportUtils.GetLastWriteTimeUtc(mesh)))
            {
                if (file != null)
                    using (var writer = new BinaryWriter(file))
                    {
                        WriteMesh(writer, mesh, BuildBones(skinnedMeshRenderer));
                    }
            }
        }

        public string EvaluateAnimationName(AnimationClip mesh)
        {
            if (mesh == null)
                return null;
            return ExportUtils.ReplaceExtension(ExportUtils.GetRelPathFromAsset(_engine.Subfolder, mesh), "") + "/" +
                   ExportUtils.SafeFileName(mesh.name) + ".ani";
        }

        public string EvaluateMeshName(Mesh mesh)
        {
            if (mesh == null)
                return null;
            return ExportUtils.ReplaceExtension(ExportUtils.GetRelPathFromAsset(_engine.Subfolder, mesh), "") + "/" +
                   ExportUtils.SafeFileName(mesh.name) + ".mdl";
        }

        private Dictionary<Object, string> _dynamicMeshNames = new Dictionary<Object, string>();

        public string EvaluateMeshName(ProBuilderMesh mesh)
        {
            if (mesh == null)
                return null;
            if (_dynamicMeshNames.TryGetValue(mesh, out var name))
                return name;

            var assetUrhoAssetName = ExportUtils.GetRelPathFromAsset(_engine.Subfolder, mesh);
            if (string.IsNullOrWhiteSpace(assetUrhoAssetName))
            {
                name = _engine.TempFolder + ExportUtils.SafeFileName(mesh.name)+"."+ _dynamicMeshNames .Count+ ".mdl";
                _dynamicMeshNames.Add(mesh, name);
                return name;
            }
            return ExportUtils.ReplaceExtension(assetUrhoAssetName, "") + "/" +
                   ExportUtils.SafeFileName(mesh.name) + ".mdl";
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
            var allBindings = AnimationUtility.GetCurveBindings(clipAnimation);
            var propertiesToKeep = new HashSet<string>(animationProperties);

            var bindingGroups = allBindings.Where(_ => propertiesToKeep.Contains(_.propertyName)).GroupBy(_ => _.path)
                .OrderBy(_ => _.Key.Length).ToArray();
            var timeStep = 1.0f / clipAnimation.frameRate;
            var numKeyFrames = 1 + (int) (clipAnimation.length * clipAnimation.frameRate);

            var numTracks = (uint) bindingGroups.Length;
            writer.Write(numTracks);
            foreach (var group in bindingGroups)
            {
                var boneName = group.Key;
                boneName = boneName.Substring(boneName.LastIndexOf('/') + 1);
                WriteStringSZ(writer, boneName);

                var curves = new AnimationCurve[animationProperties.Length];
                for (var index = 0; index < animationProperties.Length; index++)
                {
                    var curveBinding = group.FirstOrDefault(_ => _.propertyName == animationProperties[index]);
                    if (curveBinding.propertyName != null)
                        curves[index] = AnimationUtility.GetEditorCurve(clipAnimation, curveBinding);
                }

                byte trackMask = 0;
                if (curves[0] != null || curves[1] != null || curves[2] != null)
                    trackMask |= 1;
                if (curves[3] != null || curves[4] != null || curves[5] != null || curves[6] != null)
                    trackMask |= 2;
                if (curves[7] != null || curves[8] != null || curves[9] != null)
                    trackMask |= 4;
                writer.Write(trackMask);
                writer.Write(numKeyFrames);
                for (var frame = 0; frame < numKeyFrames; ++frame)
                {
                    var t = frame * timeStep;
                    writer.Write(t);

                    if ((trackMask & 1) != 0)
                    {
                        var pos = Vector3.zero;
                        if (curves[0] != null) pos.x = curves[0].Evaluate(t);
                        if (curves[1] != null) pos.y = curves[1].Evaluate(t);
                        if (curves[2] != null) pos.z = curves[2].Evaluate(t);
                        Write(writer, pos);
                    }

                    if ((trackMask & 2) != 0)
                    {
                        var rot = Quaternion.identity;
                        if (curves[3] != null) rot.w = curves[3].Evaluate(t);
                        if (curves[4] != null) rot.x = curves[4].Evaluate(t);
                        if (curves[5] != null) rot.y = curves[5].Evaluate(t);
                        if (curves[6] != null) rot.z = curves[6].Evaluate(t);
                        rot.Normalize();
                        Write(writer, rot);
                    }

                    if ((trackMask & 4) != 0)
                    {
                        var scale = Vector3.one;
                        if (curves[7] != null) scale.x = curves[7].Evaluate(t);
                        if (curves[8] != null) scale.y = curves[8].Evaluate(t);
                        if (curves[9] != null) scale.z = curves[9].Evaluate(t);
                        Write(writer, scale);
                    }
                }
            }
        }

        private string GetRootBoneName(EditorCurveBinding editorCurveBinding)
        {
            var path = editorCurveBinding.path;
            if (string.IsNullOrEmpty(path))
                return null;
            var slash = path.IndexOf('/');
            if (slash < 0)
                return path;
            return path.Substring(0, slash);
        }

        private Urho3DBone[] BuildBones(SkinnedMeshRenderer skinnedMeshRenderer)
        {
            if (skinnedMeshRenderer == null || skinnedMeshRenderer.bones.Length == 0)
                return new Urho3DBone[0];
            var unityBones = skinnedMeshRenderer.bones;
            var bones = new Urho3DBone[unityBones.Length];
            for (var index = 0; index < bones.Length; index++)
            {
                var bone = new Urho3DBone();
                var unityBone = unityBones[index];
                for (var pIndex = 0; pIndex < bones.Length; ++pIndex)
                    if (unityBones[pIndex] == unityBone.parent)
                    {
                        bone.parent = pIndex;
                        break;
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

                bone.binding = skinnedMeshRenderer.sharedMesh.bindposes[index];
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

        private void WriteProBuilderMesh(BinaryWriter writer, ProBuilderMesh _mesh)
        {
            writer.Write(Magic2);
            writer.Write(1);
            for (var vbIndex = 0; vbIndex < 1 /*_mesh.vertexBufferCount*/; ++vbIndex)
            {
                var positions = _mesh.positions;
                var normals = _mesh.GetNormals();
                var colors = _mesh.GetColors();
                var tangents = _mesh.GetTangents();
                var uvs = _mesh.textures;

                writer.Write(positions.Count);
                var elements = new List<MeshStreamWriter>();
                if (positions.Count > 0)
                    elements.Add(new MeshVector3Stream(positions, VertexElementSemantic.SEM_POSITION));
                if (normals.Length > 0)
                    elements.Add(new MeshVector3Stream(normals, VertexElementSemantic.SEM_NORMAL));

                //if (colors.Length > 0)
                //{
                //    elements.Add(new MeshColorStream(colors, VertexElementSemantic.SEM_COLOR));
                //}
                if (tangents.Length > 0)
                    elements.Add(new MeshVector4Stream(FlipW(tangents), VertexElementSemantic.SEM_TANGENT));
                if (uvs.Count > 0)
                    elements.Add(new MeshUVStream(uvs, VertexElementSemantic.SEM_TEXCOORD));
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
                var indicesPerSubMesh = new List<List<int>>();
                var totalIndices = 0;
                var subMeshCount = _mesh.faces.Select(_ => _.submeshIndex).Max() + 1;
                for (var subMeshIndex = 0; subMeshIndex < subMeshCount; ++subMeshIndex)
                {
                    var indices = new List<int>();
                    foreach (var face in _mesh.faces.Where(_ => _.submeshIndex == subMeshIndex))
                    {
                        for (var tIndex = 2; tIndex < face.indexes.Count; ++tIndex)
                        {
                            indices.Add(face.indexes[0]);
                            indices.Add(face.indexes[tIndex - 1]);
                            indices.Add(face.indexes[tIndex]);
                        }
                    }

                    indicesPerSubMesh.Add(indices);
                    totalIndices += indices.Count;
                }

                writer.Write(1);
                writer.Write(totalIndices);
                if (positions.Count < 65536)
                {
                    writer.Write(2);
                    for (var subMeshIndex = 0; subMeshIndex < subMeshCount; ++subMeshIndex)
                        for (var i = 0; i < indicesPerSubMesh[subMeshIndex].Count; ++i)
                            writer.Write((ushort)indicesPerSubMesh[subMeshIndex][i]);
                }
                else
                {
                    writer.Write(4);
                    for (var subMeshIndex = 0; subMeshIndex < subMeshCount; ++subMeshIndex)
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
                    writer.Write((int)PrimitiveType.TRIANGLE_LIST);
                    writer.Write(0);
                    writer.Write(0);
                    writer.Write(totalIndices);
                    writer.Write(indicesPerSubMesh[subMeshIndex].Count);
                    totalIndices += indicesPerSubMesh[subMeshIndex].Count;
                }

                var numMorphTargets = 0;
                writer.Write(numMorphTargets);

                var numOfBones = 0;
                writer.Write(numOfBones);
                var boneIndex = 0;

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

        private void WriteMesh(BinaryWriter writer, Mesh _mesh, Urho3DBone[] bones)
        {
            writer.Write(Magic2);
            writer.Write(1);
            for (var vbIndex = 0; vbIndex < 1 /*_mesh.vertexBufferCount*/; ++vbIndex)
            {
                var positions = _mesh.vertices;
                var normals = _mesh.normals;
                var colors = _mesh.colors;
                var tangents = _mesh.tangents;
                var boneWeights = _mesh.boneWeights;
                var uvs = _mesh.uv;
                var uvs2 = _mesh.uv2;
                var uvs3 = _mesh.uv3;
                var uvs4 = _mesh.uv4;

                writer.Write(positions.Length);
                var elements = new List<MeshStreamWriter>();
                if (positions.Length > 0)
                    elements.Add(new MeshVector3Stream(positions, VertexElementSemantic.SEM_POSITION));
                if (normals.Length > 0)
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

                //if (colors.Length > 0)
                //{
                //    elements.Add(new MeshColorStream(colors, VertexElementSemantic.SEM_COLOR));
                //}
                if (tangents.Length > 0)
                    elements.Add(new MeshVector4Stream(FlipW(tangents), VertexElementSemantic.SEM_TANGENT));
                if (uvs.Length > 0)
                    elements.Add(new MeshUVStream(uvs, VertexElementSemantic.SEM_TEXCOORD));
                if (uvs2.Length > 0)
                    elements.Add(new MeshUVStream(uvs2, VertexElementSemantic.SEM_TEXCOORD, 1));
                if (uvs3.Length > 0)
                    elements.Add(new MeshUVStream(uvs2, VertexElementSemantic.SEM_TEXCOORD, 2));
                if (uvs4.Length > 0)
                    elements.Add(new MeshUVStream(uvs2, VertexElementSemantic.SEM_TEXCOORD, 3));
                writer.Write(elements.Count);
                for (var i = 0; i < elements.Count; ++i)
                    writer.Write(elements[i].Element);
                var morphableVertexRangeStartIndex = 0;
                var morphableVertexCount = 0;
                writer.Write(morphableVertexRangeStartIndex);
                writer.Write(morphableVertexCount);
                for (var index = 0; index < positions.Length; ++index)
                for (var i = 0; i < elements.Count; ++i)
                    elements[i].Write(writer, index);
                var indicesPerSubMesh = new List<int[]>();
                var totalIndices = 0;
                for (var subMeshIndex = 0; subMeshIndex < _mesh.subMeshCount; ++subMeshIndex)
                {
                    var indices = _mesh.GetIndices(subMeshIndex);
                    indicesPerSubMesh.Add(indices);
                    totalIndices += indices.Length;
                }

                writer.Write(1);
                writer.Write(totalIndices);
                if (positions.Length < 65536)
                {
                    writer.Write(2);
                    for (var subMeshIndex = 0; subMeshIndex < _mesh.subMeshCount; ++subMeshIndex)
                    for (var i = 0; i < indicesPerSubMesh[subMeshIndex].Length; ++i)
                        writer.Write((ushort) indicesPerSubMesh[subMeshIndex][i]);
                }
                else
                {
                    writer.Write(4);
                    for (var subMeshIndex = 0; subMeshIndex < _mesh.subMeshCount; ++subMeshIndex)
                    for (var i = 0; i < indicesPerSubMesh[subMeshIndex].Length; ++i)
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
                    writer.Write(indicesPerSubMesh[subMeshIndex].Length);
                    totalIndices += indicesPerSubMesh[subMeshIndex].Length;
                }

                var numMorphTargets = 0;
                writer.Write(numMorphTargets);

                var numOfBones = bones.Length;
                writer.Write(numOfBones);
                var boneIndex = 0;
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
                for (var i = 0; i < positions.Length; ++i)
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