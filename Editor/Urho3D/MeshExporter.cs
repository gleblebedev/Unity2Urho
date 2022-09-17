using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using Assets.Unity2Urho.Editor.Urho3D.ProBuilder;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;
using Math = System.Math;
using Object = UnityEngine.Object;

namespace UnityToCustomEngineExporter.Editor.Urho3D
{
    public class MeshExporter : AbstractBinaryExpoerter
    {
        private const int MASK_POSITION = 0x1;
        private const int MASK_NORMAL = 0x2;
        private const int MASK_COLOR = 0x4;
        private const int MASK_TANGENT = 0x80;


        public const uint Magic2 = 0x32444d55;

        private readonly Urho3DEngine _engine;

        private readonly HashSet<Mesh> _meshes = new HashSet<Mesh>();

        private readonly Dictionary<Object, string> _dynamicMeshNames = new Dictionary<Object, string>();
        private HashSet<Material> _materials = new HashSet<Material>();

        public MeshExporter(Urho3DEngine engine)
        {
            _engine = engine;
        }

        public void ExportProBuilderMesh(ProBuilderMeshAdapter proBuilderMesh, PrefabContext prefabContext)
        {
            if (!_engine.Options.ExportMeshes)
                return;
            ExportProBuilderMeshModel(proBuilderMesh, prefabContext);
        }

        public void ExportMesh(Mesh mesh, PrefabContext prefabContext)
        {
            if (!_engine.Options.ExportMeshes)
                return;
            var mdlFilePath = EvaluateMeshName(mesh, prefabContext);
            var assetKey = mesh.GetKey();
            var lastWriteDateTimeUtc = ExportUtils.GetLastWriteTimeUtc(mesh);
            ExportMeshModel(() => new MeshSource(mesh), mdlFilePath, assetKey, lastWriteDateTimeUtc);
        }

        public void ExportLODGroup(LODGroup mesh, PrefabContext prefabContext)
        {
            if (!_engine.Options.ExportMeshes)
                return;

            var mdlFilePath = EvaluateLODGroupName(mesh, prefabContext);
            var assetKey = mesh.GetKey();
            var lastWriteDateTimeUtc = ExportUtils.GetLastWriteTimeUtc(mesh);
            ExportMeshModel(() => new LODGroupSource(mesh), mdlFilePath, assetKey, lastWriteDateTimeUtc);
        }


        public string ExportMesh(NavMeshTriangulation mesh, PrefabContext prefabContext)
        {
            var mdlFilePath = EvaluateMeshName(mesh, prefabContext);
            if (_engine.Options.ExportMeshes)
                ExportMeshModel(() => new NavMeshSource(mesh), mdlFilePath,
                    SceneManager.GetActiveScene().GetRootGameObjects().FirstOrDefault().GetKey(), DateTime.MaxValue);

            return mdlFilePath;
        }

        public void ExportMesh(GameObject go, PrefabContext prefabContext)
        {
            if (!_engine.Options.ExportMeshes)
                return;
            var proBuilderMesh = ProBuilderMeshAdapter.Get(go);
            var skinnedMeshRenderer = go.GetComponent<SkinnedMeshRenderer>();
            var meshFilter = go.GetComponent<MeshFilter>();

            if (proBuilderMesh != null)
            {
                ExportProBuilderMeshModel(proBuilderMesh, prefabContext);
            }
            else
            {
                Mesh mesh = null;
                if (skinnedMeshRenderer != null)
                    mesh = skinnedMeshRenderer.sharedMesh;
                else if (meshFilter != null) mesh = meshFilter.sharedMesh;
                ExportMeshModel(() => new MeshSource(mesh, skinnedMeshRenderer), EvaluateMeshName(mesh, prefabContext),
                    mesh.GetKey(),
                    ExportUtils.GetLastWriteTimeUtc(mesh));
            }


            for (var i = 0; i < go.transform.childCount; ++i)
                ExportMesh(go.transform.GetChild(i).gameObject, prefabContext);
        }

        public void ExportMeshModel(Func<IMeshSource> meshSource, string mdlFilePath, AssetKey key,
            DateTime lastWriteDateTimeUtc)
        {
            if (!_engine.Options.ExportMeshes)
                return;
            using (var file = _engine.TryCreate(key, mdlFilePath, lastWriteDateTimeUtc))
            {
                if (file != null)
                    using (var writer = new BinaryWriter(file))
                    {
                        var mesh = meshSource();
                        WriteMesh(writer, mesh);
                    }
            }
        }

        public string EvaluateMeshName(Mesh mesh, PrefabContext prefabContext)
        {
            if (mesh == null)
                return null;
            var folder =
                ExportUtils.ReplaceExtension(ExportUtils.GetRelPathFromAsset(_engine.Options.Subfolder, mesh), "");
            if (string.IsNullOrWhiteSpace(folder)) folder = prefabContext.TempFolder;
            return ExportUtils.Combine(folder,
                ExportUtils.SafeFileName(_engine.DecorateName(ExportUtils.GetName(mesh))) + ".mdl");
        }

        public string EvaluateLODGroupName(LODGroup lodGroup, PrefabContext prefabContext)
        {
            if (lodGroup == null)
                return null;
            if (!string.IsNullOrWhiteSpace(lodGroup.gameObject.name))
                return ExportUtils.Combine(prefabContext.TempFolder, lodGroup.gameObject.name + ".mdl");
            var lods = lodGroup.GetLODs();
            if (lods.Length == 0)
                return null;
            var firstMesh = lods[0].renderers.FirstOrDefault(_ => _ != null);
            if (firstMesh == null)
                return null;
            var folder =
                ExportUtils.ReplaceExtension(ExportUtils.GetRelPathFromAsset(_engine.Options.Subfolder, firstMesh), "");
            if (string.IsNullOrWhiteSpace(folder)) folder = prefabContext.TempFolder;
            return ExportUtils.Combine(folder,
                ExportUtils.SafeFileName(_engine.DecorateName(ExportUtils.GetName(firstMesh))) + ".With" +
                lods.Length.ToString(CultureInfo.InvariantCulture) + "Lods.mdl");
        }

        public string EvaluateMeshName(Object mesh, PrefabContext prefabContext)
        {
            if (mesh == null)
                return null;
            if (_dynamicMeshNames.TryGetValue(mesh, out var name))
                return name;

            var assetUrhoAssetName = ExportUtils.GetRelPathFromAsset(_engine.Options.Subfolder, mesh);
            if (string.IsNullOrWhiteSpace(assetUrhoAssetName))
            {
                name = ExportUtils.Combine(prefabContext.TempFolder,
                    ExportUtils.SafeFileName(_engine.DecorateName(ExportUtils.GetName(mesh))) + "." +
                    _dynamicMeshNames.Count + ".mdl");
                _dynamicMeshNames.Add(mesh, name);
                return name;
            }

            return ExportUtils.ReplaceExtension(assetUrhoAssetName, "") + "/" +
                   ExportUtils.SafeFileName(_engine.DecorateName(mesh.name)) + ".mdl";
        }

        public string EvaluateMeshName(NavMeshTriangulation mesh, PrefabContext prefabContext)
        {
            return ExportUtils.Combine(prefabContext.TempFolder, "NavMesh.mdl");
        }

        private void ExportProBuilderMeshModel(ProBuilderMeshAdapter proBuilderMesh, PrefabContext prefabContext)
        {
            ExportMeshModel(() => new ProBuilderMeshSource(proBuilderMesh),
                EvaluateMeshName(proBuilderMesh.Object, prefabContext),
                proBuilderMesh.Object.GetKey(), ExportUtils.GetLastWriteTimeUtc(proBuilderMesh.Object));
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
                    bone.parent = parentIndex.Value;
                else
                    bone.parent = index;

                bone.name = string.IsNullOrWhiteSpace(unityBone?.name)? "bone" + index : unityBone.name;
                //if (bone.parent != 0)
                //{
                if (unityBone != null)
                {
                    bone.actualPos = unityBone.localPosition;
                    bone.actualRot = unityBone.localRotation;
                    bone.actualScale = unityBone.localScale;
                }
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


        private void WriteMesh(BinaryWriter writer, IMeshSource mesh)
        {
            float minX, minY, minZ;
            float maxX, maxY, maxZ;
            maxX = maxY = maxZ = float.MinValue;
            minX = minY = minZ = float.MaxValue;

            var indexBuffer = new List<int>(1024);
            writer.Write(Magic2);
            var positions = mesh.Vertices;
            {
                // Vertex buffer
                uint numVertexBuffers = 1;
                writer.Write(numVertexBuffers);
                for (var vbIndex = 0; vbIndex < numVertexBuffers; ++vbIndex)
                {
                    var elements = WriteVertexBufferHeader(writer, mesh, positions);
                    
                    var morphableVertexRangeStartIndex = 0;
                    var morphableVertexCount = 0;
                    if (mesh.MorphTargets.Count > 0)
                    {
                        morphableVertexCount = positions.Count;
                    }
                    writer.Write(morphableVertexRangeStartIndex);
                    writer.Write(morphableVertexCount);
                    for (var index = 0; index < positions.Count; ++index)
                    for (var i = 0; i < elements.Count; ++i)
                        elements[i].Write(writer, index);
                    for (var subMeshIndex = 0; subMeshIndex < mesh.SubMeshCount; ++subMeshIndex)
                    {
                        var meshGeometry = mesh.GetGeomtery(subMeshIndex);
                        for (var lodIndex = 0; lodIndex < meshGeometry.NumLods; lodIndex++)
                        {
                            var indices = meshGeometry.GetIndices(lodIndex);
                            indexBuffer.AddRange(indices);
                            foreach (var index in indices)
                            {
                                if (index > positions.Count)
                                    throw new InvalidOperationException();
                            }
                        }
                    }

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
                }
            }

            {
                // Index buffer
                uint numIndexBuffers = 1;
                writer.Write(numIndexBuffers);
                writer.Write(indexBuffer.Count);
                if (!indexBuffer.Any(_ => _ > 65535))
                {
                    writer.Write(2);
                    for (var i = 0; i < indexBuffer.Count; ++i)
                        writer.Write((ushort) indexBuffer[i]);
                }
                else
                {
                    writer.Write(4);
                    for (var i = 0; i < indexBuffer.Count; ++i)
                        writer.Write((uint) indexBuffer[i]);
                }
            }

            writer.Write(mesh.SubMeshCount);
            var indexOffset = 0;
            for (var subMeshIndex = 0; subMeshIndex < mesh.SubMeshCount; ++subMeshIndex)
            {
                var numberOfBoneMappingEntries = 0;
                writer.Write(numberOfBoneMappingEntries);

                var geomtery = mesh.GetGeomtery(subMeshIndex);

                var numberOfLODLevels = geomtery.NumLods;
                writer.Write(numberOfLODLevels);
                for (var lodIndex = 0; lodIndex < numberOfLODLevels; lodIndex++)
                {
                    var lodIndices = geomtery.GetIndices(lodIndex);
                    writer.Write((float)geomtery.GetLodDistance(lodIndex));
                    switch (geomtery.Topology)
                    {
                        case MeshTopology.Lines:
                            writer.Write((uint)PrimitiveType.LINE_LIST);
                            break;
                        case MeshTopology.LineStrip:
                            writer.Write((uint)PrimitiveType.LINE_STRIP);
                            break;
                        case MeshTopology.Points:
                            writer.Write((uint)PrimitiveType.POINT_LIST);
                            break;
                        default:
                            writer.Write((uint)PrimitiveType.TRIANGLE_LIST);
                            break;
                    }
                    writer.Write((uint)0); //vb
                    writer.Write((uint)0); //ib
                    writer.Write((uint)indexOffset); //Draw range: index start
                    var indicesPerLod = lodIndices.Count;
                    writer.Write((uint)indicesPerLod); //Draw range: index count
                    indexOffset += indicesPerLod;
                }
            }
            if (indexOffset != indexBuffer.Count)
                throw new InvalidOperationException();

            var morphTargets = mesh.MorphTargets;
            writer.Write(morphTargets.Count);
            foreach (var morphTarget in morphTargets)
            {
                WriteStringSZ(writer, morphTarget.Name); // Name of morph

                writer.Write(1); // Number of affected vertex buffers
                
                writer.Write(0); // Vertex buffer index, starting from 0

                var mask = 0;
                if (morphTarget.Vertices.Any(_ => _ != Vector3.zero))
                    mask |= MASK_POSITION;
                if (morphTarget.Normals.Any(_ => _ != Vector3.zero))
                    mask |= MASK_NORMAL;
                if (morphTarget.Tangents.Any(_ => _ != Vector3.zero))
                    mask |= MASK_TANGENT;
                writer.Write(mask); // Vertex element mask for morph data. Only positions, normals & tangents are supported.

                var indices = new List<int>();
                
                for (var index = 0; index < morphTarget.Vertices.Count; index++)
                {
                    if (morphTarget.Vertices[index] != Vector3.zero)
                    {
                        indices.Add(index);
                        continue;
                    }
                    if (morphTarget.Normals[index] != Vector3.zero)
                    {
                        indices.Add(index);
                        continue;
                    }
                    if (morphTarget.Tangents[index] != Vector3.zero)
                    {
                        indices.Add(index);
                        continue;
                    }
                }

                writer.Write(indices.Count); // Vertex count
                foreach (var index in indices)
                {
                    writer.Write(index);
                    if (0 != (mask & MASK_POSITION))
                    {
                        Write(writer, morphTarget.Vertices[index]);
                    }
                    if (0 != (mask & MASK_NORMAL))
                    {
                        Write(writer, morphTarget.Normals[index]);
                    }
                    if (0 != (mask & MASK_TANGENT))
                    {
                        Write(writer, morphTarget.Tangents[index]);
                    }
                }
            }

            var bones = BuildBones(mesh);
            var numOfBones = bones.Length;
            writer.Write(numOfBones);
            var boneIndex = 0;
            foreach (var bone in bones)
            {
                WriteStringSZ(writer, _engine.DecorateName(bone.name));
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

                using (var e = GetBoneVertices(mesh.BoneWeights, boneIndex).GetEnumerator())
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

            if (minX > maxX)
            {
                writer.Write(-0.1f);
                writer.Write(-0.1f);
                writer.Write(-0.1f);
                writer.Write(0.1f);
                writer.Write(0.1f);
                writer.Write(0.1f);
            }
            else
            {
                writer.Write(minX);
                writer.Write(minY);
                writer.Write(minZ);
                writer.Write(maxX);
                writer.Write(maxY);
                writer.Write(maxZ);
            }
        }

        private List<MeshStreamWriter> WriteVertexBufferHeader(BinaryWriter writer, IMeshSource mesh, IList<Vector3> positions)
        {
            var normals = mesh.Normals;
            var colors = _engine.Options.ExportVertexColor?mesh.Colors:Array.Empty<Color32>();
            var tangents = mesh.Tangents;
            var boneWeights = mesh.BoneWeights;
            var uvs = mesh.TexCoords0;
            var uvs2 = mesh.TexCoords1;
            var uvs3 = mesh.TexCoords2;
            var uvs4 = mesh.TexCoords3;

            writer.Write(positions.Count);
            var elements = new List<MeshStreamWriter>();

            //if (positions.Count > 0) We should always have positions!
            elements.Add(new MeshVector3Stream(positions, VertexElementSemantic.SEM_POSITION));

            if (normals.Count > 0)
            {
                //var n = new Vector3[normals.Count];
                //for (var index = 0; index < normals.Count; index++)
                //{
                //    n[index] = normals[index].normalized;
                //}
                //elements.Add(new MeshVector3Stream(n, VertexElementSemantic.SEM_NORMAL));
                elements.Add(new MeshVector3Stream(normals, VertexElementSemantic.SEM_NORMAL));
            }

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

            if (colors.Count > 0) elements.Add(new MeshColor32Stream(colors, VertexElementSemantic.SEM_COLOR));
            if (tangents.Count > 0)
                elements.Add(new MeshVector4Stream(FlipW(tangents), VertexElementSemantic.SEM_TANGENT));
            if (uvs.Count > 0)
                elements.Add(new MeshUVStream(uvs, VertexElementSemantic.SEM_TEXCOORD));
            if (uvs2.Count > 0)
                elements.Add(new MeshUVStream(uvs2, VertexElementSemantic.SEM_TEXCOORD, 1));
            if (uvs3.Count > 0)
                elements.Add(new MeshUVStream(uvs3, VertexElementSemantic.SEM_TEXCOORD, 2));
            if (uvs4.Count > 0)
                elements.Add(new MeshUVStream(uvs4, VertexElementSemantic.SEM_TEXCOORD, 3));
            writer.Write(elements.Count);
            for (var i = 0; i < elements.Count; ++i)
                writer.Write(elements[i].Element);
            return elements;
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


        public class Urho3DBone
        {
            public string name;
            public int parent;
            public Vector3 actualPos = Vector3.zero;
            public Quaternion actualRot = Quaternion.identity;
            public Vector3 actualScale = Vector3.one;
            public Matrix4x4 binding = Matrix4x4.identity;

            public override string ToString()
            {
                return name + " (Parent: "+parent+")";
            }
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
                Element = (int) VertexElementType.TYPE_UBYTE4_NORM | ((int) sem << 8) | (index << 16);
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
    }
}