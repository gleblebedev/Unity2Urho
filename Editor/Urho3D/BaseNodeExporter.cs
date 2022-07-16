using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Xml;
using Assets.Unity2Urho.Editor.Urho3D.ProBuilder;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityToCustomEngineExporter.Urho3D;
using Math = System.Math;
using Object = UnityEngine.Object;

namespace UnityToCustomEngineExporter.Editor.Urho3D
{
    public class BaseNodeExporter
    {
        protected Urho3DEngine _engine;
        protected int _id;
        protected EditorTaskScheduler BackgroundEditorTasks = new EditorTaskScheduler();

        public BaseNodeExporter(Urho3DEngine engine)
        {
            _engine = engine;
        }

        public static string Format(Color pos)
        {
            return string.Format(CultureInfo.InvariantCulture, "{0} {1} {2} {3}", pos.r, pos.g, pos.b, pos.a);
        }

        public static string FormatRGB(Color pos)
        {
            return string.Format(CultureInfo.InvariantCulture, "{0} {1} {2}", pos.r, pos.g, pos.b);
        }

        public static string Format(float pos)
        {
            return string.Format(CultureInfo.InvariantCulture, "{0}", pos);
        }

        public static string Format(Vector4 pos)
        {
            return string.Format(CultureInfo.InvariantCulture, "{0} {1} {2} {3}", pos.x, pos.y, pos.z, pos.w);
        }
        
        public static string Format(Vector3 pos)
        {
            return string.Format(CultureInfo.InvariantCulture, "{0} {1} {2}", pos.x, pos.y, pos.z);
        }
        
        private static void WriteVariant(XmlWriter writer, string subSubPrefix, string type, string valueStr)
        {
            writer.WriteWhitespace(subSubPrefix);
            writer.WriteStartElement("variant");
            writer.WriteAttributeString("type", type);
            writer.WriteAttributeString("value", valueStr);
            writer.WriteEndElement();
            writer.WriteWhitespace(Environment.NewLine);
        }


        protected void WriteAttribute(XmlWriter writer, string prefix, string name, float pos)
        {
            WriteAttribute(writer, prefix, name, string.Format(CultureInfo.InvariantCulture, "{0}", pos));
        }
        
        protected void WriteAttribute(XmlWriter writer, string prefix, string name, Vector2 pos)
        {
            WriteAttribute(writer, prefix, name,
                string.Format(CultureInfo.InvariantCulture, "{0} {1}", pos.x, pos.y));
        }
        
        protected void WriteAttribute(XmlWriter writer, string prefix, string name, Vector3 pos)
        {
            WriteAttribute(writer, prefix, name,
                string.Format(CultureInfo.InvariantCulture, "{0} {1} {2}", pos.x, pos.y, pos.z));
        }

        protected void WriteAttribute(XmlWriter writer, string prefix, string name, Vector4 pos)
        {
            WriteAttribute(writer, prefix, name, Format(pos));
        }

        protected void WriteAttribute(XmlWriter writer, string prefix, string name, Quaternion pos)
        {
            WriteAttribute(writer, prefix, name,
                string.Format(CultureInfo.InvariantCulture, "{0} {1} {2} {3}", pos.w, pos.x, pos.y, pos.z));
        }

        protected void WriteAttribute(XmlWriter writer, string prefix, string name, Color pos)
        {
            WriteAttribute(writer, prefix, name, Format(pos));
        }

        protected void EndElement(XmlWriter writer, string prefix)
        {
            writer.WriteWhitespace(prefix);
            writer.WriteEndElement();
            writer.WriteWhitespace("\n");
        }

        protected void StartComponent(XmlWriter writer, string prefix, string type, bool isEnabled)
        {
            writer.WriteWhitespace(prefix);
            writer.WriteStartElement("component");
            writer.WriteAttributeString("type", type);
            writer.WriteAttributeString("id", (++_id).ToString());
            writer.WriteWhitespace("\n");
            if (!isEnabled) WriteAttribute(writer, prefix + "\t", "Is Enabled", isEnabled);
        }

        protected void WriteAttribute(XmlWriter writer, string prefix, string name, string vaue)
        {
            writer.WriteWhitespace(prefix);
            writer.WriteStartElement("attribute");
            writer.WriteAttributeString("name", name);
            writer.WriteAttributeString("value", vaue);
            writer.WriteEndElement();
            writer.WriteWhitespace("\n");
        }

        private readonly Dictionary<GameObject, int> _objectIds = new Dictionary<GameObject, int>();
        protected void WriteObject(XmlWriter writer, string prefix, GameObject gameObject, HashSet<Component> excludeList,
            bool parentEnabled, PrefabContext prefabContext, bool skipPos = false)
        {
            if (gameObject == null)
                return;

            var isEnabled = gameObject.activeSelf && parentEnabled;
            if (_engine.Options.SkipDisabled && !isEnabled) return;

            if (_engine.Options.ExportPrefabReferences)
            {
                if (prefabContext.PrefabRoot != gameObject && PrefabUtility.IsAnyPrefabInstanceRoot(gameObject))
                {
                    if (WritePrefabReferenceNode(writer, prefix, gameObject, isEnabled, prefabContext))
                    {
                        return;
                    }
                }
            }
            prefabContext = prefabContext.Retarget(gameObject);
            var localExcludeList = new HashSet<Component>(excludeList);
            var subPrefix = prefix + "\t";
            var subSubPrefix = subPrefix + "\t";

            WriteNodeHeader(writer, prefix, gameObject, isEnabled, prefabContext, skipPos);

            foreach (var component in gameObject.GetComponents<Component>().Where(_=>!excludeList.Contains(_)))
                if (component is IUrho3DComponent customComponent)
                {
                    ExportCustomComponent(writer, subPrefix, customComponent);
                }
                else if (component is Camera camera)
                {
                    ExportCamera(writer, camera, subPrefix, subSubPrefix);
                }
                else if (component is Light light)
                {
                    ExportLight(writer, light, subPrefix, subSubPrefix);
                }
                else if (component is AudioSource audioSource)
                {
                    ExportAudioSource(writer, audioSource, subPrefix, prefabContext);
                }
                else if (component is ParticleSystem particleSystem)
                {
                    ExportParticleSystem(writer, particleSystem, subPrefix, prefabContext, isEnabled);
                }
                else if (component is Terrain terrain)
                {
                    ExportTerrain(writer, terrain?.terrainData, gameObject.GetComponent<TerrainCollider>(), subPrefix,
                        terrain.enabled, prefabContext);
                }
                else if (component is Rigidbody rigidbody)
                {
                    StartComponent(writer, subPrefix, "RigidBody", true);
                    var localToWorldMatrix = gameObject.transform.localToWorldMatrix;
                    var pos = new Vector3(localToWorldMatrix.m03, localToWorldMatrix.m13, localToWorldMatrix.m23);
                    //WriteAttribute(writer, subSubPrefix, "Physics Position", pos);
                    WriteAttribute(writer, subSubPrefix, "Mass", rigidbody.mass);
                    EndElement(writer, subPrefix);
                }
                else if (component is MeshCollider meshCollider)
                {
                    StartComponent(writer, subPrefix, "CollisionShape", meshCollider.enabled);
                    WriteCommonCollisionAttributes(writer, subSubPrefix, meshCollider);
                    WriteAttribute(writer, subSubPrefix, "Shape Type", "TriangleMesh");
                    if (meshCollider.sharedMesh != null)
                    {
                        var sharedMesh = meshCollider.sharedMesh;
                        _engine.ScheduleAssetExport(sharedMesh, prefabContext);
                        var meshPath = _engine.EvaluateMeshName(sharedMesh, prefabContext);
                        if (!string.IsNullOrWhiteSpace(meshPath))
                            WriteAttribute(writer, subSubPrefix, "Model", "Model;" + meshPath);
                    }

                    EndElement(writer, subPrefix);
                    WriteStaticRigidBody(writer, gameObject, subPrefix, subSubPrefix, meshCollider.enabled);
                }
                else if (component is BoxCollider boxCollider)
                {
                    StartComponent(writer, subPrefix, "CollisionShape", boxCollider.enabled);
                    WriteCommonCollisionAttributes(writer, subSubPrefix, boxCollider);
                    WriteAttribute(writer, subSubPrefix, "Size", boxCollider.size);
                    WriteAttribute(writer, subSubPrefix, "Offset Position", boxCollider.center);
                    //WriteAttribute(writer, subSubPrefix, "Offset Rotation", new Quaternion(0,0,0, 1));
                    EndElement(writer, subPrefix);
                    WriteStaticRigidBody(writer, gameObject, subPrefix, subSubPrefix, boxCollider.enabled);
                }
                else if (component is TerrainCollider terrainCollider)
                {
                    //Skip terrain collider as the actual terrain is in another node
                }
                else if (component is CharacterJoint characterJoint)
                {
                    var connectedObject = characterJoint.connectedBody?.gameObject;
                    Vector3 axis = characterJoint.axis;
                    Vector2 highLimit;
                    Vector2 lowLimit;
                    Quaternion rotation;
                    Quaternion otherBodyRotation;
                    string constraintType;
                    if (Math.Abs(characterJoint.swing1Limit.limit * 2) >=
                        Math.Abs(characterJoint.lowTwistLimit.limit - characterJoint.highTwistLimit.limit))
                    {
                        constraintType = "Hinge";
                        highLimit = new Vector2(characterJoint.swing1Limit.limit, 0);
                        lowLimit = new Vector2(-characterJoint.swing1Limit.limit, 0);
                        rotation = GetHingeOrPointRotation(characterJoint.swingAxis);
                        otherBodyRotation = GetHingeOrPointRotation(connectedObject.transform.InverseTransformDirection(gameObject.transform.TransformDirection(characterJoint.swingAxis)));
                    }
                    else
                    {
                        constraintType = "Hinge";
                        highLimit = new Vector2(characterJoint.highTwistLimit.limit, 0);
                        lowLimit = new Vector2(-characterJoint.lowTwistLimit.limit, 0);
                        rotation = GetHingeOrPointRotation(characterJoint.axis);
                        otherBodyRotation = GetHingeOrPointRotation(connectedObject.transform.InverseTransformDirection(gameObject.transform.TransformDirection(characterJoint.axis)));
                    }
                    StartComponent(writer, subPrefix, "Constraint", isEnabled);
                    WriteAttribute(writer, subSubPrefix, "Constraint Type", constraintType);
                    WriteAttribute(writer, subSubPrefix, "Position", characterJoint.anchor);
                    WriteAttribute(writer, subSubPrefix, "Rotation", rotation);
                    if (_objectIds.TryGetValue(connectedObject, out var connectedId))
                    {
                        WriteAttribute(writer, subSubPrefix, "Other Body NodeID", connectedId);
                        var connectedAxis = connectedObject.transform.InverseTransformDirection(gameObject.transform.TransformDirection(axis));
                    }
                    WriteAttribute(writer, subSubPrefix, "Other Body Position", characterJoint.connectedAnchor);
                    WriteAttribute(writer, subSubPrefix, "Other Body Rotation", otherBodyRotation);
                    WriteAttribute(writer, subSubPrefix, "Disable Collision", true);
                    WriteAttribute(writer, subSubPrefix, "High Limit", highLimit);
                    WriteAttribute(writer, subSubPrefix, "Low Limit", lowLimit);
                    EndElement(writer, subPrefix);
                }
                else if (component is SphereCollider sphereCollider)
                {
                    StartComponent(writer, subPrefix, "CollisionShape", sphereCollider.enabled);
                    WriteCommonCollisionAttributes(writer, subSubPrefix, sphereCollider);
                    WriteAttribute(writer, subSubPrefix, "Shape Type", "Sphere");
                    WriteAttribute(writer, subSubPrefix, "Offset Position", sphereCollider.center);
                    WriteAttribute(writer, subSubPrefix, "Size", new Vector3(sphereCollider.radius, sphereCollider.radius, sphereCollider.radius));
                    EndElement(writer, subPrefix);
                    WriteStaticRigidBody(writer, gameObject, subPrefix, subSubPrefix, sphereCollider.enabled);
                }
                else if (component is CapsuleCollider capsuleCollider)
                {
                    StartComponent(writer, subPrefix, "CollisionShape", capsuleCollider.enabled);
                    WriteCommonCollisionAttributes(writer, subSubPrefix, capsuleCollider);
                    if (component.name == "Cylinder")
                        WriteAttribute(writer, subSubPrefix, "Shape Type", "Cylinder");
                    else
                        WriteAttribute(writer, subSubPrefix, "Shape Type", "Capsule");
                    var d = capsuleCollider.radius * 2.0f;
                    WriteAttribute(writer, subSubPrefix, "Size", new Vector3(d, capsuleCollider.height, d));
                    WriteAttribute(writer, subSubPrefix, "Offset Position", capsuleCollider.center);
                    switch (capsuleCollider.direction)
                    {
                        case 0:
                            WriteAttribute(writer, subSubPrefix, "Offset Rotation", new Quaternion(0, 0, -0.707107f, 0.707107f));
                            break;
                        case 2:
                            WriteAttribute(writer, subSubPrefix, "Offset Rotation", new Quaternion( 0.707107f, 0, 0, 0.707107f));
                            break;
                    } 
                    EndElement(writer, subPrefix);
                    WriteStaticRigidBody(writer, gameObject, subPrefix, subSubPrefix, capsuleCollider.enabled);
                }
                else if (component is Skybox skybox)
                {
                    var skyboxMaterial = skybox.material;
                    WriteSkyboxComponent(writer, subPrefix, skyboxMaterial, prefabContext, skybox.enabled);
                }
                else if (component is CharacterController characterController)
                {
                    WriteCharacterController(writer, subPrefix, characterController, prefabContext);
                }
                else if (component is Collider collider)
                {
                    StartComponent(writer, subPrefix, "CollisionShape", collider.enabled);
                    WriteCommonCollisionAttributes(writer, subSubPrefix, collider);
                    EndElement(writer, subPrefix);
                    WriteStaticRigidBody(writer, gameObject, subPrefix, subSubPrefix, collider.enabled);
                }
                else if (component is Animation animation)
                {
                    WriteAnimationController(writer, subPrefix, animation, prefabContext);
                }
                // else if (component is Animator animator)
                // {
                //     WriteAnimationController(writer, subPrefix, animator, prefabContext);
                // }
                else if (component is ReflectionProbe reflectionProbe)
                {
                    switch (reflectionProbe.mode)
                    {
                        case ReflectionProbeMode.Baked:
                            ExportZone(writer, subPrefix, reflectionProbe.size, reflectionProbe.bakedTexture as Cubemap,
                                reflectionProbe.enabled, prefabContext);
                            break;
                        case ReflectionProbeMode.Custom:
                            ExportZone(writer, subPrefix, reflectionProbe.size,
                                reflectionProbe.customBakedTexture as Cubemap, reflectionProbe.enabled, prefabContext);
                            break;
                    }
                }

            var meshFilter = gameObject.GetComponent<MeshFilter>();

            var proBuilderMesh = ProBuilderMeshAdapter.Get(gameObject);
            var meshRenderer = gameObject.GetComponent<MeshRenderer>();
            var skinnedMeshRenderer = gameObject.GetComponent<SkinnedMeshRenderer>();
            var lodGroup = gameObject.GetComponent<LODGroup>();

            if (lodGroup != null)
            {
                var canExportLods = CanExportAsLod(lodGroup);
                if (canExportLods)
                {
                    _engine.ScheduleLODGroup(lodGroup, prefabContext);
                    var lods = lodGroup.GetLODs();
                    var renderers = lods.SelectMany(_ => _.renderers.Where(_1 => _1 != null)).ToList();

                    StartComponent(writer, subPrefix, "StaticModel", lodGroup.enabled);
                    var meshPath = _engine.EvaluateMeshName(lodGroup, prefabContext);
                    if (!string.IsNullOrWhiteSpace(meshPath))
                        WriteAttribute(writer, subSubPrefix, "Model", "Model;" + meshPath);

                    var materials = new StringBuilder("Material");
                    var visitedMaterials = new HashSet<Material>();
                    foreach (var renderer in renderers.Where(_ => _ != null))
                        foreach (var material in renderer.sharedMaterials)
                            if (visitedMaterials.Add(material))
                            {
                                _engine.ScheduleAssetExport(material, prefabContext);
                                var path = _engine.EvaluateMaterialName(material, prefabContext);
                                materials.Append(";");
                                materials.Append(path);
                            }

                    WriteAttribute(writer, subSubPrefix, "Material", materials.ToString());

                    var firstRenderer = renderers.FirstOrDefault();
                    if (firstRenderer != null)
                        WriteAttribute(writer, subSubPrefix, "Cast Shadows",
                            firstRenderer.shadowCastingMode != ShadowCastingMode.Off);

                    EndElement(writer, subPrefix);

                    foreach (var lod in lods.Skip(0))
                    foreach (var renderer in lod.renderers.Where(_ => _ != null))
                        localExcludeList.Add(renderer);

                    foreach (var lod in lods.Skip(1))
                    foreach (var collider in lod.renderers.Where(_ => _ != null).Select(_=>_.gameObject.GetComponent<Collider>()).Where(_=>_!=null))
                        localExcludeList.Add(collider);
                }
                else
                {
                    var lods = lodGroup.GetLODs();
                    foreach (var lod in lods.Skip(1))
                    foreach (var renderer in lod.renderers.Where(_ => _ != null))
                        localExcludeList.Add(renderer);

                    foreach (var lod in lods.Skip(1))
                    foreach (var collider in lod.renderers.Where(_ => _ != null).Select(_ => _.gameObject.GetComponent<Collider>()).Where(_ => _ != null))
                        localExcludeList.Add(collider);
                }
            }

            if (meshRenderer != null)
            {
                if (!localExcludeList.Contains(meshRenderer))
                    if (meshFilter != null || proBuilderMesh != null)
                    {
                        var enabled = true;
                        if (meshFilter == null && proBuilderMesh != null)
                            enabled = proBuilderMesh.enabled;
                        else if (meshRenderer != null)
                            enabled = meshRenderer.enabled;
                        StartComponent(writer, subPrefix, "StaticModel", enabled);

                        string meshPath;
                        if (proBuilderMesh != null)
                        {
                            _engine.ScheduleAssetExport(proBuilderMesh.Object, prefabContext);
                            meshPath = _engine.EvaluateMeshName(proBuilderMesh, prefabContext);
                        }
                        else
                        {
                            var sharedMesh = meshFilter.sharedMesh;
                            _engine.ScheduleAssetExport(sharedMesh, prefabContext);
                            meshPath = _engine.EvaluateMeshName(sharedMesh, prefabContext);
                        }

                        if (!string.IsNullOrWhiteSpace(meshPath))
                            WriteAttribute(writer, subSubPrefix, "Model", "Model;" + meshPath);

                        var materials = "Material";
                        foreach (var material in meshRenderer.sharedMaterials)
                        {
                            _engine.ScheduleAssetExport(material, prefabContext);
                            var path = _engine.EvaluateMaterialName(material, prefabContext);
                            materials += ";" + path;
                        }

                        WriteAttribute(writer, subSubPrefix, "Material", materials);

                        WriteAttribute(writer, subSubPrefix, "Cast Shadows",
                            meshRenderer.shadowCastingMode != ShadowCastingMode.Off);

                        EndElement(writer, subPrefix);

                        //WriteAnimationController(writer, subPrefix, animator);
                    }
            }

            else if (skinnedMeshRenderer != null)
            {
                if (!localExcludeList.Contains(skinnedMeshRenderer))
                {
                    StartComponent(writer, subPrefix, "AnimatedModel", skinnedMeshRenderer.enabled);


                    var sharedMesh = skinnedMeshRenderer.sharedMesh;
                    _engine.ScheduleAssetExport(sharedMesh, prefabContext);
                    var meshPath = _engine.EvaluateMeshName(sharedMesh, prefabContext);
                    if (!string.IsNullOrWhiteSpace(meshPath))
                        WriteAttribute(writer, subSubPrefix, "Model", "Model;" + meshPath);

                    var materials = "Material";
                    foreach (var material in skinnedMeshRenderer.sharedMaterials)
                    {
                        _engine.ScheduleAssetExport(material, prefabContext);
                        var path = _engine.EvaluateMaterialName(material, prefabContext);
                        materials += ";" + path;
                    }

                    WriteAttribute(writer, subSubPrefix, "Material", materials);

                    WriteAttribute(writer, subSubPrefix, "Cast Shadows",
                        skinnedMeshRenderer.shadowCastingMode != ShadowCastingMode.Off);

                    //WriteAnimationStates(writer, animator, subPrefix, "Animation States");

                    EndElement(writer, subPrefix);
                }
            }

            foreach (Transform childTransform in gameObject.transform)
                if (childTransform.parent.gameObject == gameObject)
                    WriteObject(writer, subPrefix, childTransform.gameObject, localExcludeList, isEnabled,
                        prefabContext);

            if (!string.IsNullOrEmpty(prefix))
                writer.WriteWhitespace(prefix);
            writer.WriteEndElement();
            writer.WriteWhitespace("\n");
        }

        private bool WritePrefabReferenceNode(XmlWriter writer, string prefix, GameObject gameObject, bool isEnabled, PrefabContext prefabContext)
        {
            if (!_engine.Options.ExportPrefabReferences)
            {
                return false;
            }
            
            var originalSource = PrefabUtility.GetCorrespondingObjectFromOriginalSource(gameObject);
            if (originalSource == null)
            {
                return false;
            }

            var name = _engine.EvaluatePrefabName(originalSource);
            if (string.IsNullOrWhiteSpace(name))
                return false;

            var subPrefix = prefix + "\t";
            var subSubPrefix = subPrefix + "\t";

            WriteNodeHeader(writer, prefix, gameObject, isEnabled, prefabContext);

            StartComponent(writer, subPrefix, "PrefabReference", true);
            WriteAttribute(writer, subSubPrefix, "Prefab", "XMLFile;"+name);
            EndElement(writer, subPrefix);

            if (!string.IsNullOrEmpty(prefix))
                writer.WriteWhitespace(prefix);
            writer.WriteEndElement();
            writer.WriteWhitespace("\n");

            _engine.ScheduleAssetExport(originalSource, new PrefabContext(_engine, originalSource, name, prefabContext.DefaultFolder));

            return true;
        }

        private void WriteNodeHeader(XmlWriter writer, string prefix, GameObject gameObject, bool isEnabled, PrefabContext prefabContext, bool skipPos = false)

        {
            var id = StartNode(writer, prefix);
            _objectIds[gameObject] = id;
            var subPrefix = prefix + "\t";
            

            WriteAttribute(writer, subPrefix, "Is Enabled", isEnabled);
            if (!string.IsNullOrWhiteSpace(gameObject.name))
                WriteAttribute(writer, subPrefix, "Name", _engine.DecorateName(gameObject.name));
            if (!string.IsNullOrWhiteSpace(gameObject.tag))
            {
                writer.WriteWhitespace(subPrefix);
                writer.WriteStartElement("attribute");
                writer.WriteAttributeString("name", "Tags");
                writer.WriteStartElement("string");
                writer.WriteAttributeString("value", gameObject.tag);
                writer.WriteEndElement();
                writer.WriteEndElement();
                writer.WriteWhitespace(Environment.NewLine);
            }

            //WriteAttribute(writer, subPrefix, "Tags", obj.tag);
            if (!skipPos)
            {
                if (gameObject.transform.localPosition != Vector3.zero)
                    WriteAttribute(writer, subPrefix, "Position", gameObject.transform.localPosition);
                if (gameObject.transform.localRotation != Quaternion.identity)
                    WriteAttribute(writer, subPrefix, "Rotation", gameObject.transform.localRotation);
                if (gameObject.transform.localScale != Vector3.one)
                    WriteAttribute(writer, subPrefix, "Scale", gameObject.transform.localScale);
            }
        }

        private Quaternion GetRotationFromConstraintAxis(UrhoConstraint urhoConstraint, Vector3 axis)
        {
            switch (urhoConstraint)
            {
                case UrhoConstraint.Point:
                case UrhoConstraint.Hinge:
                    return GetHingeOrPointRotation(axis);
                default:
                    return GetSliderOrConeRotation(axis);
            }
        }

        private Quaternion GetHingeOrPointRotation(Vector3 axis)
        {
            return Quaternion.FromToRotation(new Vector3(0, 0, 1), axis);
        }

        private Quaternion GetSliderOrConeRotation(Vector3 axis)
        {
            return Quaternion.FromToRotation(new Vector3(1, 0, 0), axis);
        }

        protected void WriteSkyboxComponent(XmlWriter writer, string subPrefix, Material skyboxMaterial,
            PrefabContext prefabContext, bool enabled)
        {
            var subSubPrefix = subPrefix + "\t";
            StartComponent(writer, subPrefix, "Skybox", enabled);
            if (skyboxMaterial.shader.name == "Skybox/Panoramic")
            {
                // Export sphere
                var gameObject = GameObject.CreatePrimitive(UnityEngine.PrimitiveType.Sphere);
                var mesh = gameObject.GetComponent<MeshFilter>().sharedMesh;
                Object.DestroyImmediate(gameObject);
                _engine.ScheduleAssetExport(mesh, prefabContext);
                WriteAttribute(writer, subSubPrefix, "Model", "Model;" + _engine.EvaluateMeshName(mesh, prefabContext));
            }
            else
            {
                // Export cube
                var gameObject = GameObject.CreatePrimitive(UnityEngine.PrimitiveType.Cube);
                var mesh = gameObject.GetComponent<MeshFilter>().sharedMesh;
                Object.DestroyImmediate(gameObject);
                _engine.ScheduleAssetExport(mesh, prefabContext);
                WriteAttribute(writer, subSubPrefix, "Model", "Model;" + _engine.EvaluateMeshName(mesh, prefabContext));
            }

            _engine.ScheduleAssetExport(skyboxMaterial, prefabContext);
            var materials = "Material;" + _engine.EvaluateMaterialName(skyboxMaterial, prefabContext);
            WriteAttribute(writer, subSubPrefix, "Material", materials);
            EndElement(writer, subPrefix);
        }

        protected void ExportZone(XmlWriter writer, string subPrefix, Vector3 size, string cubemap,
            PrefabContext prefabContext, bool enabled)
        {
            StartComponent(writer, subPrefix, "Zone", enabled);

            var subSubPrefix = subPrefix + "\t";
            WriteAttribute(writer, subSubPrefix, "Ambient Color", _engine.FixMaterialColorSpace(RenderSettings.ambientLight));
            WriteAttribute(writer, subSubPrefix, "Override Mode", false);
            if (RenderSettings.fog)
            {
                WriteAttribute(writer, subSubPrefix, "Fog Color", _engine.FixMaterialColorSpace(RenderSettings.fogColor));
                WriteAttribute(writer, subSubPrefix, "Fog Start", RenderSettings.fogStartDistance);
                WriteAttribute(writer, subSubPrefix, "Fog End", RenderSettings.fogEndDistance);
                //switch (RenderSettings.fogMode)
                //{
                //    case FogMode.Linear:
                //        break;
                //    case FogMode.Exponential:
                //        break;
                //    case FogMode.ExponentialSquared:
                //        break;
                //    default:
                //        throw new ArgumentOutOfRangeException();
                //}
            }


            WriteAttribute(writer, subSubPrefix, "Bounding Box Min", -(size * 0.5f));
            WriteAttribute(writer, subSubPrefix, "Bounding Box Max", size * 0.5f);

            var volume = size.x * size.y * size.z;
            if (volume != 0)
            {
                var priority = int.MaxValue / (volume * 2);
                WriteAttribute(writer, subSubPrefix, "Priority", (int) priority);
            }

            WriteAttribute(writer, subSubPrefix, "Zone Texture", "TextureCube;" + cubemap);
            EndElement(writer, subPrefix);
        }

        protected void ExportZone(XmlWriter writer, string subPrefix, Vector3 size, Cubemap cubemap, bool enabled,
            PrefabContext prefabContext)
        {
            if (cubemap == null) return;

            var assetPath = AssetDatabase.GetAssetPath(cubemap);
            if (string.IsNullOrWhiteSpace(assetPath))
                return;

            _engine.ScheduleAssetExport(cubemap, prefabContext);
            var texName = _engine.EvaluateCubemapName(cubemap);

            ExportZone(writer, subPrefix, size, texName, prefabContext, enabled);
        }

        protected int StartNode(XmlWriter writer, string prefix)
        {
            if (!string.IsNullOrEmpty(prefix))
                writer.WriteWhitespace(prefix);
            writer.WriteStartElement("node");
            var id = ++_id;
            writer.WriteAttributeString("id", id.ToString(CultureInfo.InvariantCulture));
            writer.WriteWhitespace(Environment.NewLine);
            return id;
        }

        private bool CanExportAsLod(LODGroup lodGroup)
        {
            if (!_engine.Options.ExportLODs)
                return false;
            if (lodGroup.GetLODs().SelectMany(_ => _.renderers.Where(_1 => _1 != null)).Any(_ => _ is SkinnedMeshRenderer))
                return false;
            return true;
        }

        private void WriteCharacterController(XmlWriter writer, string prefix, CharacterController characterController,
            PrefabContext prefabContext)
        {
            var subPrefix = prefix + "\t";

            StartComponent(writer, prefix, "CollisionShape", characterController.enabled);
            WriteCommonCollisionAttributes(writer, subPrefix, characterController);
            WriteAttribute(writer, subPrefix, "Shape Type", "Capsule");
            var d = characterController.radius * 2.0f;
            WriteAttribute(writer, subPrefix, "Size", new Vector3(d, characterController.height, d));
            WriteAttribute(writer, subPrefix, "Offset Position", characterController.center);
            EndElement(writer, prefix);

            StartComponent(writer, prefix, "RigidBody", characterController.enabled);
            var localToWorldMatrix = characterController.gameObject.transform.localToWorldMatrix;
            var pos = new Vector3(localToWorldMatrix.m03, localToWorldMatrix.m13, localToWorldMatrix.m23);
            //WriteAttribute(writer, subPrefix, "Physics Position", pos);
            WriteAttribute(writer, subPrefix, "Angular Factor", Vector3.zero);
            WriteAttribute(writer, subPrefix, "Collision Event Mode", "Always");
            WriteAttribute(writer, subPrefix, "Is Kinematic", "true");
            WriteAttribute(writer, subPrefix, "Is Trigger", "true");
            EndElement(writer, prefix);

            StartComponent(writer, prefix, "KinematicCharacterController", characterController.enabled);
            WriteAttribute(writer, subPrefix, "Max Slope", characterController.slopeLimit);
            WriteAttribute(writer, subPrefix, "Step Height", characterController.stepOffset);
            EndElement(writer, prefix);
        }

        private void WriteCommonCollisionAttributes(XmlWriter writer, string subSubPrefix, Collider collider)
        {
            WriteAttribute(writer, subSubPrefix, "Collision Margin", collider.contactOffset);
        }

        private void ExportCamera(XmlWriter writer, Camera camera, string subPrefix, string subSubPrefix)
        {
            if (!_engine.Options.ExportCameras)
                return;

            if (camera == null) return;

            StartComponent(writer, subPrefix, "Camera", camera.enabled);

            WriteAttribute(writer, subSubPrefix, "Near Clip", camera.nearClipPlane);
            WriteAttribute(writer, subSubPrefix, "Far Clip", camera.farClipPlane);
            WriteAttribute(writer, subSubPrefix, "FOV", camera.fieldOfView);
            WriteAttribute(writer, subSubPrefix, "Aspect Ratio", camera.aspect);
            WriteAttribute(writer, subSubPrefix, "Orthographic", camera.orthographic);
            WriteAttribute(writer, subSubPrefix, "Orthographic Size", camera.orthographicSize);
            //WriteAttribute(writer, subSubPrefix, "Auto Aspect Ratio", true);
            //WriteAttribute(writer, subSubPrefix, "View Mask", camera.cullingMask);


            //WriteAttribute(writer, subSubPrefix, "Fill Mode", camera.?); - Wireframe, ...
            //WriteAttribute(writer, subSubPrefix, "Zoom", camera.zoom);
            //WriteAttribute(writer, subSubPrefix, "LOD Bias", camera.lodBias);
            //WriteAttribute(writer, subSubPrefix, "View Override Flags", camera.??);
            //WriteAttribute(writer, subSubPrefix, "Projection Offset", camera. ???);
            //WriteAttribute(writer, subSubPrefix, "Reflection Plane", camera. ???);
            //WriteAttribute(writer, subSubPrefix, "Clip Plane", camera. ???);
            //WriteAttribute(writer, subSubPrefix, "Use Reflection", camera. ???);
            //WriteAttribute(writer, subSubPrefix, "Use Clipping", camera. ???);

            EndElement(writer, subPrefix);
        }

        private void WriteAnimationController(XmlWriter writer, string prefix, Animator animator,
            PrefabContext prefabContext)
        {
            if (animator == null)
                return;
            StartComponent(writer, prefix, "AnimationController", animator.enabled);
            var subPrefix = prefix + "\t";

            WriteAnimationStates(writer, animator, subPrefix, "Node Animation States", prefabContext);
            EndElement(writer, prefix);
        }

        private void WriteAnimationController(XmlWriter writer, string prefix, Animation animation,
            PrefabContext prefabContext)
        {
            if (animation == null)
                return;
            StartComponent(writer, prefix, "AnimationController", animation.enabled);
            var subPrefix = prefix + "\t";

            WriteAnimationStates(writer, animation, subPrefix, "Node Animation States", prefabContext);
            EndElement(writer, prefix);
        }

        private void WriteAnimationStates(XmlWriter writer, Animator animator, string subPrefix, string statesAttr,
            PrefabContext prefabContext)
        {
            if (animator == null)
                return;
            writer.WriteWhitespace(subPrefix);
            writer.WriteStartElement("attribute");
            writer.WriteAttributeString("name", statesAttr);
            writer.WriteWhitespace(Environment.NewLine);
            var subSubPrefix = subPrefix + "\t";
            var controller = animator.runtimeAnimatorController;
            if (controller == null)
            {
                WriteVariant(writer, subSubPrefix, 0);
            }
            else
            {
                WriteVariant(writer, subSubPrefix, controller.animationClips.Length);
                foreach (var clip in controller.animationClips)
                {
                    WriteVariant(writer, subSubPrefix, "ResourceRef",
                        "Animation;" + _engine.EvaluateAnimationName(clip, prefabContext));
                    _engine.ScheduleAssetExport(clip, prefabContext);
                    var startBone = "";
                    var isLooped = true;
                    var weight = 0.0f;
                    var time = 0.0f;
                    var layer = 0;
                    WriteVariant(writer, subSubPrefix, startBone);
                    WriteVariant(writer, subSubPrefix, isLooped);
                    WriteVariant(writer, subSubPrefix, weight);
                    WriteVariant(writer, subSubPrefix, time);
                    WriteVariant(writer, subSubPrefix, layer);
                }
            }

            writer.WriteWhitespace(subPrefix);
            writer.WriteEndElement();
            writer.WriteWhitespace(Environment.NewLine);
        }

        private void WriteAnimationStates(XmlWriter writer, Animation animation, string subPrefix, string statesAttr,
            PrefabContext prefabContext)
        {
            if (animation == null)
                return;
            writer.WriteWhitespace(subPrefix);
            writer.WriteStartElement("attribute");
            writer.WriteAttributeString("name", statesAttr);
            writer.WriteWhitespace(Environment.NewLine);
            var subSubPrefix = subPrefix + "\t";

            WriteVariant(writer, subSubPrefix, animation.GetClipCount());
            if (animation.GetClipCount() > 0)
                foreach (var animationItem in animation)
                    if (animationItem is AnimationState animationState)
                    {
                        var clip = animationState.clip;
                        WriteVariant(writer, subSubPrefix, "ResourceRef",
                            "Animation;" + _engine.EvaluateAnimationName(clip, prefabContext));
                        _engine.ScheduleAssetExport(clip, prefabContext);
                        var startBone = "";
                        var isLooped = clip.wrapMode == WrapMode.Loop;
                        var weight = animation.playAutomatically && clip == animation.clip ? 1.0f : 0.0f;
                        var time = 0.0f;
                        var layer = animationState.layer;
                        WriteVariant(writer, subSubPrefix, startBone);
                        WriteVariant(writer, subSubPrefix, isLooped);
                        WriteVariant(writer, subSubPrefix, weight);
                        WriteVariant(writer, subSubPrefix, time);
                        WriteVariant(writer, subSubPrefix, layer);
                    }
                    else
                    {
                        Debug.LogWarning(animationItem.GetType().FullName);
                    }

            writer.WriteWhitespace(subPrefix);
            writer.WriteEndElement();
            writer.WriteWhitespace(Environment.NewLine);
        }

        private void WriteVariant(XmlWriter writer, string subSubPrefix, int value)
        {
            WriteVariant(writer, subSubPrefix, "Int", value.ToString(CultureInfo.InvariantCulture));
        }

        private void WriteVariant(XmlWriter writer, string subSubPrefix, float value)
        {
            WriteVariant(writer, subSubPrefix, "Float", value.ToString(CultureInfo.InvariantCulture));
        }

        protected void WriteVariant(XmlWriter writer, string subSubPrefix, bool value)
        {
            WriteVariant(writer, subSubPrefix, "Bool", value ? "true" : "false");
        }

        private void WriteVariant(XmlWriter writer, string subSubPrefix, string value)
        {
            WriteVariant(writer, subSubPrefix, "String", value);
        }

        private void ExportLight(XmlWriter writer, Light light, string subPrefix, string subSubPrefix)
        {
            if (!_engine.Options.ExportLights)
                return;
            if (light != null && light.type != LightType.Area)
            {
                StartComponent(writer, subPrefix, "Light", light.enabled);
                if (light.type == LightType.Directional)
                {
                    WriteAttribute(writer, subSubPrefix, "Light Type", "Directional");
                    var shadowCascades = QualitySettings.shadowCascades;
                    if (shadowCascades > 0)
                    {
                        if (shadowCascades > 4)
                            shadowCascades = 4;
                        var cascadeMask = new[]
                        {
                            new Vector4(0, 0, 0, 0),
                            new Vector4(1, 0, 0, 0),
                            new Vector4(1, 1, 0, 0),
                            new Vector4(1, 1, 1, 0)
                        };
                        var shadowCascade4Split = QualitySettings.shadowCascade4Split * QualitySettings.shadowDistance;
                        var csmSplits = Vector4.Scale(
                            new Vector4(shadowCascade4Split.x, shadowCascade4Split.y, shadowCascade4Split.z,
                                QualitySettings.shadowDistance),
                            cascadeMask[shadowCascades - 1]);
                        switch (shadowCascades)
                        {
                            case 1:
                                csmSplits.x = QualitySettings.shadowDistance;
                                break;
                            case 2:
                                csmSplits.y = QualitySettings.shadowDistance;
                                break;
                            case 3:
                                csmSplits.z = QualitySettings.shadowDistance;
                                break;
                            case 4:
                                csmSplits.w = QualitySettings.shadowDistance;
                                break;
                        }

                        WriteAttribute(writer, subSubPrefix, "CSM Splits", csmSplits);
                    }
                }
                else if (light.type == LightType.Spot)
                {
                    WriteAttribute(writer, subSubPrefix, "Light Type", "Spot");
                    WriteAttribute(writer, subSubPrefix, "Spot FOV", light.spotAngle);
                    WriteAttribute(writer, subSubPrefix, "Range", light.range);
                }
                else if (light.type == LightType.Point)
                {
                    WriteAttribute(writer, subSubPrefix, "Range", light.range);
                }

                WriteAttribute(writer, subSubPrefix, "Color", _engine.FixMaterialColorSpace(light.color));
                var lightIntensity = light.intensity;
                if (!_engine.Options.RBFX)
                {
                    lightIntensity *= Mathf.PI * 2.0f;
                }

                if (_engine.Options.UsePhysicalValues)
                {
                    WriteAttribute(writer, subSubPrefix, "Brightness Multiplier", lightIntensity * 156.25f);
                    WriteAttribute(writer, subSubPrefix, "Use Physical Values", "true");
                }
                else
                {
                    WriteAttribute(writer, subSubPrefix, "Brightness Multiplier", lightIntensity);
                    WriteAttribute(writer, subSubPrefix, "Use Physical Values", "false");
                }

                WriteAttribute(writer, subSubPrefix, "Depth Constant Bias", 0.0001f);
                WriteAttribute(writer, subSubPrefix, "Cast Shadows", light.shadows != LightShadows.None);
                if (light.cookie != null)
                {
                    _engine.ScheduleTexture(light.cookie);
                    WriteAttribute(writer, subSubPrefix, "Light Shape Texture",
                        "Texture2D;" + _engine.EvaluateTextrueName(light.cookie));
                }

                EndElement(writer, subPrefix);
            }
        }

        private void ExportParticleSystem(XmlWriter writer, ParticleSystem particleSystem, string subPrefix,
            PrefabContext prefabContext, bool enabled)
        {
            var subSubPrefix = subPrefix + "\t";
            var name = _engine.ScheduleParticleEffect(particleSystem, prefabContext);
            if (_engine.Options.RBFX)
            {
                StartComponent(writer, subPrefix, "ParticleGraphEmitter", true);
                WriteAttribute(writer, subSubPrefix, "Effect", "ParticleGraphEffect;" + name);
            }
            else
            {
                StartComponent(writer, subPrefix, "ParticleEmitter", true);
                WriteAttribute(writer, subSubPrefix, "Effect", "ParticleEffect;" + name);
            }
            WriteAttribute(writer, subSubPrefix, "Animation LOD Bias", 0);
            EndElement(writer, subPrefix);
        }

        private void ExportAudioSource(XmlWriter writer, AudioSource audioSource, string subPrefix,
            PrefabContext prefabContext)
        {
            var subSubPrefix = subPrefix + "\t";
            StartComponent(writer, subPrefix, "SoundSource3D", audioSource.enabled);
            if (audioSource.clip != null)
            {
                var name = _engine.EvaluateAudioClipName(audioSource.clip);
                _engine.ScheduleAssetExport(audioSource.clip, prefabContext);
                WriteAttribute(writer, subSubPrefix, "Sound", "Sound;" + name);
                WriteAttribute(writer, subSubPrefix, "Frequency", audioSource.clip.frequency);
                WriteAttribute(writer, subSubPrefix, "Is Playing", audioSource.playOnAwake);
                WriteAttribute(writer, subSubPrefix, "Play Position", 0);
            }

            EndElement(writer, subPrefix);
        }

        private void WriteStaticRigidBody(XmlWriter writer, GameObject obj, string subPrefix, string subSubPrefix,
            bool meshColliderEnabled)
        {
            if (obj.GetComponent<Rigidbody>() == null)
            {
                StartComponent(writer, subPrefix, "RigidBody", meshColliderEnabled);
                // When saving prefab we don't need world position
                var localToWorldMatrix = obj.transform.localToWorldMatrix;
                var pos = new Vector3(localToWorldMatrix.m03, localToWorldMatrix.m13, localToWorldMatrix.m23);
                //WriteAttribute(writer, subSubPrefix, "Physics Position", pos);
                EndElement(writer, subPrefix);
            }
        }

        private (float min, float max, Vector2 size) GetTerrainSize(TerrainData terrain)
        {
            var w = terrain.heightmapResolution;
            var h = terrain.heightmapResolution;
            var max = float.MinValue;
            var min = float.MaxValue;
            var heights = terrain.GetHeights(0, 0, w, h);
            foreach (var height in heights)
            {
                if (height > max) max = height;
                if (height < min) min = height;
            }

            return (min, max, new Vector2(w, h));
        }

        private void ExportCustomComponent(XmlWriter writer, string subPrefix, IUrho3DComponent customComponent)
        {
            if (customComponent == null) return;

            var subSubPrefix = subPrefix + "\t";
            StartComponent(writer, subPrefix, customComponent.GetUrho3DComponentName(),
                customComponent.IsUrho3DComponentEnabled);
            foreach (var keyValuePair in customComponent.GetUrho3DComponentAttributes())
                WriteAttribute(writer, subSubPrefix, keyValuePair.Name, keyValuePair.Value);
            EndElement(writer, subPrefix);
        }

        protected void WriteAttribute(XmlWriter writer, string prefix, string name, bool flag)
        {
            WriteAttribute(writer, prefix, name, flag ? "true" : "false");
        }

        private void WriteAttribute(XmlWriter writer, string prefix, string name, int flag)
        {
            WriteAttribute(writer, prefix, name, flag.ToString(CultureInfo.InvariantCulture));
        }

        private void ExportTerrain(XmlWriter writer, TerrainData terrainData, TerrainCollider terrainCollider,
            string subPrefix, bool enabled, PrefabContext prefabContext)
        {
            if (terrainData == null) return;

            var subSubPrefix = subPrefix + "\t";

            var terrainSize = terrainData.size;
            StartNode(writer, subPrefix);

            _engine.ScheduleAssetExport(terrainData, prefabContext);

            var (min, max, size) = GetTerrainSize(terrainData);

            var offset = new Vector3(terrainSize.x * 0.5f, terrainSize.y * min, terrainSize.z * 0.5f);
            WriteAttribute(writer, subPrefix, "Position", offset);
            StartComponent(writer, subPrefix, "Terrain", enabled);

            WriteAttribute(writer, subSubPrefix, "Height Map",
                "Image;" + _engine.EvaluateTerrainHeightMap(terrainData));
            WriteAttribute(writer, subSubPrefix, "Material",
                "Material;" + _engine.EvaluateTerrainMaterial(terrainData));
            //WriteTerrainMaterial(terrainData, materialFileName, "Textures/Terrains/" + folderAndName + ".Weights.tga");
            var vertexSpacing = new Vector3(terrainSize.x / size.x, terrainSize.y * (max - min) / 255.0f,
                terrainSize.z / size.y);
            WriteAttribute(writer, subSubPrefix, "Vertex Spacing",
                vertexSpacing);
            EndElement(writer, subPrefix);
            if (terrainCollider != null)
            {
                StartComponent(writer, subPrefix, "CollisionShape", enabled);
                WriteCommonCollisionAttributes(writer, subSubPrefix, terrainCollider);
                WriteAttribute(writer, subSubPrefix, "Shape Type", "Terrain");
                EndElement(writer, subPrefix);
                StartComponent(writer, subPrefix, "RigidBody", enabled);
                var localToWorldMatrix = terrainCollider.transform.localToWorldMatrix;
                var pos = localToWorldMatrix.MultiplyPoint(offset);
                //WriteAttribute(writer, subPrefix, "Physics Position", pos);
                EndElement(writer, subPrefix);
            }

            if (terrainData.detailPrototypes.Length > 0)
            {
                StartNode(writer, subPrefix);
                var detailOffset = subPrefix + "\t";
                StartComponent(writer, detailOffset, "StaticModel", enabled);
                EndElement(writer, detailOffset);
                EndElement(writer, subPrefix);
            }

            EndElement(writer, subPrefix);

            var numTrees = Math.Min(terrainData.treeInstances.Length, terrainData.treeInstanceCount);
            //numTrees = Math.Min(numTrees, 20);
            for (var index = 0; index < numTrees; index++)
            {
                var treeInstance = terrainData.treeInstances[index];
                ExportTree(writer, subPrefix, treeInstance, terrainData, enabled, prefabContext);
            }
        }

        private void ExportTree(XmlWriter writer, string subPrefix, TreeInstance treeInstance,
            TerrainData terrainData, bool enabled, PrefabContext prefabContext)
        {
            var treePrototype = terrainData.treePrototypes[treeInstance.prototypeIndex];
            StartNode(writer, subPrefix);
            var position = Vector3.Scale(terrainData.size, treeInstance.position);
            WriteAttribute(writer, subPrefix, "Position", position);
            var scale = new Vector3(treeInstance.widthScale, treeInstance.heightScale, treeInstance.widthScale);
            WriteAttribute(writer, subPrefix, "Scale", scale);
            var rotation = Quaternion.AngleAxis(treeInstance.rotation, Vector3.up);
            WriteAttribute(writer, subPrefix, "Rotation", rotation);

            var subSubPrefix = subPrefix + "\t";
            if (_engine.Options.ExportPrefabReferences)
            {
                var name = _engine.EvaluatePrefabName(treePrototype.prefab);

                StartComponent(writer, subPrefix, "PrefabReference", true);
                WriteAttribute(writer, subSubPrefix, "Prefab", "XMLFile;" + name);
                EndElement(writer, subPrefix);
            }
            else
            {
                WriteObject(writer, subSubPrefix, treePrototype.prefab, new HashSet<Component>(), enabled,
                    prefabContext.RetargetToRoot(treePrototype.prefab), true);
            }


            EndElement(writer, subPrefix);
        }

        public class Element : IDisposable
        {
            private readonly XmlWriter _writer;

            public Element(XmlWriter writer)
            {
                _writer = writer;
            }

            public static IDisposable Start(XmlWriter writer, string localName)
            {
                writer.WriteStartElement(localName);
                return new Element(writer);
            }

            public void Dispose()
            {
                _writer.WriteEndElement();
            }
        }
    }
}