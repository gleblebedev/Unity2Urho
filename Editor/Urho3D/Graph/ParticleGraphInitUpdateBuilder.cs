using System;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using UnityToCustomEngineExporter.Editor.Urho3D.Graph.ParticleNodes;
using UnityEngine;
using Random = UnityToCustomEngineExporter.Editor.Urho3D.Graph.ParticleNodes.Random;

namespace UnityToCustomEngineExporter.Editor.Urho3D.Graph
{
    public class ParticleGraphInitUpdateBuilder
    {
        private readonly Urho3DEngine _engine;
        private readonly PrefabContext _prefabContext;
        private readonly ParticleGraphBuilder _init;
        private readonly ParticleGraphBuilder _update;

        private GraphNode _initNormalizedDuration;

        private GraphNode _updateTime;

        private GraphNode _updateLifeTime;

        private GraphNode _initRandom;
        private ParticleSystem _particleSystem;
        private GraphNode _updateNormTime;
        private GraphNode _updateRandom;
        private GraphNode _initTime;
        private GraphNode _initLifeTime;
        private GraphNode _updatePos;
        private GraphNode _updateVel;

        public ParticleGraphInitUpdateBuilder(Urho3DEngine engine, PrefabContext prefabContext, Graph init,
            Graph update)
        {
            _engine = engine;
            _prefabContext = prefabContext;
            _init = new ParticleGraphBuilder(init);
            _update = new ParticleGraphBuilder(update);
        }
        
        public void Build(ParticleSystem particleSystem)
        {
            _particleSystem = particleSystem;
            var renderer = particleSystem.GetComponent<Renderer>() as ParticleSystemRenderer;

            _initTime = _init.Add(new SetAttribute("time", VariantType.Float, _init.BuildConstant(0.0f)));
            _initLifeTime = _init.Add(new SetAttribute("lifetime", VariantType.Float, _init.BuildMinMaxCurve(particleSystem.main.startLifetime, GetInitNormalizedDuration, GetInitRandom)));
            var radToDeg = 57.295779513f;
            if (_particleSystem.main.startRotation3D)
            {
                var x = _init.BuildMinMaxCurve(_particleSystem.main.startRotationX, GetInitNormalizedDuration, GetInitRandom, 57.295779513f);
                var y = _init.BuildMinMaxCurve(_particleSystem.main.startRotationY, GetInitNormalizedDuration, GetInitRandom, 57.295779513f);
                var z = _init.BuildMinMaxCurve(_particleSystem.main.startRotationZ, GetInitNormalizedDuration, GetInitRandom, 57.295779513f);

                var q = _init.Add(Make.Make_pitch_yaw_roll_out(x, y, z));
                _rotationType = VariantType.Quaternion;
                _initRotation = _init.Add(new SetAttribute("rotation", _rotationType, q));
            }
            else
            {
                _rotationType = VariantType.Float;
                if (renderer?.renderMode == ParticleSystemRenderMode.Stretch)
                {
                    _initRotation = _init.Add(new SetAttribute("rotation", _rotationType, _init.BuildConstant(90.0f)));
                }
                else
                {
                    _initRotation = _init.Add(new SetAttribute("rotation", _rotationType, _init.BuildMinMaxCurve(_particleSystem.main.startRotation, GetInitNormalizedDuration, GetInitRandom, radToDeg)));
                }
            }
            var startVelocity = BuildShape(particleSystem);


            _update.Build("Expire", new GraphInPin("time", GetTime()), new GraphInPin("lifetime", GetLifeTime()));
            _updatePos = _update.Add(new GetAttribute("pos", VariantType.Vector3));
            GraphNode getVel;
            _updateVel = _update.Add(new GetAttribute("vel", VariantType.Vector3));
            getVel = _updateVel;

            var velocityOverLifetime = _particleSystem.velocityOverLifetime;
            if (velocityOverLifetime.enabled)
            {
                var isConstantVelocityOverLifetime = velocityOverLifetime.x.mode == ParticleSystemCurveMode.Constant ||
                                                     velocityOverLifetime.x.mode ==
                                                     ParticleSystemCurveMode.TwoConstants;
                if (isConstantVelocityOverLifetime)
                {
                    var additionalVelocity = BuildVelocityOverLifetime(_init, velocityOverLifetime);
                    _init.Add(new SetAttribute("vel", VariantType.Vector3, _init.Add(new Add(startVelocity, additionalVelocity))));
                }
                else
                {
                    _init.Add(new SetAttribute("vel", VariantType.Vector3, startVelocity));
                    _init.Add(new SetAttribute("velOverLifetime", VariantType.Vector3, _init.BuildConstant(Vector3.zero)));
                    var prevValue = _update.Add(new GetAttribute("velOverLifetime", VariantType.Vector3));
                    var additionalVelocity = BuildVelocityOverLifetime(_update, velocityOverLifetime);
                    var diff = _update.Add(new Subtract(additionalVelocity, prevValue));
                    _updateVel = _update.Add(new Add(_updateVel, diff));
                    _update.Add(new SetAttribute("velOverLifetime", VariantType.Vector3, additionalVelocity));
                }
            }
            else
            {
                _init.Add(new SetAttribute("vel", VariantType.Vector3, startVelocity));
            }

            var limitVelocityOverLifetime = _particleSystem.limitVelocityOverLifetime;
            if (limitVelocityOverLifetime.enabled)
            {
                _updateVel = _update.Add(new LimitVelocity(_updateVel, _update.BuildMinMaxCurve(limitVelocityOverLifetime.limit, GetNormalizedTime, GetUpdateRandom))
                {
                    Dampen = limitVelocityOverLifetime.dampen
                });
            }
            var forceOverLifetime = _particleSystem.forceOverLifetime;
            if (forceOverLifetime.enabled)
            {
                var x = _update.BuildMinMaxCurve(forceOverLifetime.x, GetNormalizedTime,
                    GetUpdateRandom);
                var y = _update.BuildMinMaxCurve(forceOverLifetime.y, GetNormalizedTime,
                    GetUpdateRandom);
                var z = _update.BuildMinMaxCurve(forceOverLifetime.z, GetNormalizedTime,
                    GetUpdateRandom);
                var force = _update.Add(Make.Make_x_y_z_out(x, y, z));
                _updateVel = _update.Add(new ApplyForce(_updateVel, force));
            }
            
            if (_particleSystem.collision.enabled)
            {
                var bounce = _update.Add(new Bounce(_updatePos, _updateVel));
                _updatePos = bounce;
                _updatePos = _update.Add(new SetAttribute("pos", VariantType.Vector3, bounce.NewPosition));
                _updateVel = _update.Add(new SetAttribute("vel", VariantType.Vector3, bounce.NewVelocity));
            }
            else
            {
                if (getVel != _updateVel)
                {
                    _updateVel = _update.Add(new SetAttribute("vel", VariantType.Vector3, _updateVel));
                }
                _updatePos = _update.Add(new Move(_updatePos, _updateVel));
                _updatePos = _update.Add(new SetAttribute("pos", VariantType.Vector3, _updatePos));
            }

            if (renderer != null)
            {
                switch (renderer.renderMode)
                {
                    case ParticleSystemRenderMode.Billboard:
                        AddRenderBillboard(renderer);
                        break;
                    case ParticleSystemRenderMode.Stretch:
                        AddRenderBillboard(renderer);
                        break;
                    case ParticleSystemRenderMode.HorizontalBillboard:
                        AddRenderBillboard(renderer);
                        break;
                    case ParticleSystemRenderMode.VerticalBillboard:
                        AddRenderBillboard(renderer);
                        break;
                    case ParticleSystemRenderMode.Mesh:
                        AddRenderMesh(renderer);
                        break;
                }
            }
        }

        private GraphNode BuildVelocityOverLifetime(ParticleGraphBuilder graph, ParticleSystem.VelocityOverLifetimeModule velocityOverLifetime)
        {
            Func<GraphNode> rnd;
            if (graph == _update)
                rnd = GetUpdateRandom;
            else 
                rnd =GetInitRandom;
            Func<GraphNode> t;
            if (graph == _update) 
                t = GetNormalizedTime;
            else
                t = GetInitNormalizedDuration;
            var linearX = graph.BuildMinMaxCurve(velocityOverLifetime.x, t,
                rnd);
            var linearY = graph.BuildMinMaxCurve(velocityOverLifetime.y, t,
                rnd);
            var linearZ = graph.BuildMinMaxCurve(velocityOverLifetime.z, t,
                rnd);
            return graph.Add(Make.Make_x_y_z_out(linearX, linearY, linearZ));
        }

        private GraphNode BuildShape(ParticleSystem particleSystem)
        {
            if (particleSystem.shape.enabled)
            {
                switch (particleSystem.shape.shapeType)
                {
                    case ParticleSystemShapeType.Sphere:
                        return BuildSphere(EmitFrom.Volume);
                    case ParticleSystemShapeType.SphereShell:
                        return BuildSphere(EmitFrom.Surface);
                    case ParticleSystemShapeType.Hemisphere:
                        return BuildHemisphere(EmitFrom.Volume);
                    case ParticleSystemShapeType.HemisphereShell:
                        return BuildHemisphere(EmitFrom.Surface);
                    case ParticleSystemShapeType.Cone:
                        return BuildCone(EmitFrom.Base);
                    //case ParticleSystemShapeType.Box:
                    //    break;
                    //case ParticleSystemShapeType.Mesh:
                    //    break;
                    case ParticleSystemShapeType.ConeShell:
                        return BuildCone(EmitFrom.Surface);
                    case ParticleSystemShapeType.ConeVolume:
                        return BuildCone(EmitFrom.Volume);
                    case ParticleSystemShapeType.ConeVolumeShell:
                        return BuildCone(EmitFrom.Surface);
                    //case ParticleSystemShapeType.Circle:
                    //    break;
                    //case ParticleSystemShapeType.CircleEdge:
                    //    break;
                    //case ParticleSystemShapeType.SingleSidedEdge:
                    //    break;
                    //case ParticleSystemShapeType.MeshRenderer:
                    //    break;
                    //case ParticleSystemShapeType.SkinnedMeshRenderer:
                    //    break;
                    //case ParticleSystemShapeType.BoxShell:
                    //    break;
                    //case ParticleSystemShapeType.BoxEdge:
                    //    break;
                    //case ParticleSystemShapeType.Donut:
                    //    break;
                    //case ParticleSystemShapeType.Rectangle:
                    //    break;
                    //case ParticleSystemShapeType.Sprite:
                    //    break;
                    //case ParticleSystemShapeType.SpriteRenderer:
                    //    break;
                }
            }
            _init.Add(new SetAttribute("pos", VariantType.Vector3, _init.BuildConstant(Vector3.zero)));
            return _init.BuildConstant(Vector3.zero);
        }

        private void AddRenderMesh(ParticleSystemRenderer particleSystemRenderer)
        {
            var size = BuildSize(out var sizeType, 1.0f);
            GraphNode scale = null;
            if (size != null)
            {
                if (sizeType == VariantType.Float)
                {
                    scale = _update.Add(Make.Make_x_y_z_out(size, size, size));
                }
                else if (sizeType == VariantType.Vector3)
                {
                    scale = size;
                }
            }

            if (scale == null)
            {
                scale = _update.BuildConstant(Vector3.one);
            }

            GraphNode rot;
            if (_rotationType == VariantType.Quaternion)
                rot = _update.Add(new GetAttribute("rotation", VariantType.Quaternion));
            else
                rot = _update.BuildConstant(Quaternion.identity);
            var transform = _update.Add(Make.Make_translation_rotation_scale_out(_updatePos, rot, scale));
            var render = _update.Add(new RenderMesh(transform) { IsWorldspace = false });
            render.Model = new ResourceRef("Model", _engine.EvaluateMeshName(particleSystemRenderer.mesh, _prefabContext));
            _engine.ScheduleAssetExport(particleSystemRenderer.mesh, _prefabContext);
            var materials = new ResourceRefList("Material");
            foreach (var sharedMaterial in particleSystemRenderer.sharedMaterials)
            {
                materials.Path.Add(_engine.EvaluateMaterialName(sharedMaterial, _prefabContext));
                _engine.ScheduleAssetExport(sharedMaterial, _prefabContext);
            }

            render.Material = materials;
        }

        private void AddRenderBillboard(ParticleSystemRenderer particleSystemRenderer)
        {
            var render = new RenderBillboard();
            render.Color.Connect(BuildColor());
            var size = BuildSize(out var sizeType, 0.5f);
            render.Size.Connect(BuildBillboardSize(particleSystemRenderer, size, sizeType, render, out var height));

            render.Material = new ResourceRef("Material",  _engine.EvaluateMaterialName(particleSystemRenderer.sharedMaterial, _prefabContext));
            if (_particleSystem.textureSheetAnimation.enabled)
            {
                render.Rows = _particleSystem.textureSheetAnimation.numTilesY;
                render.Columns = _particleSystem.textureSheetAnimation.numTilesX;
            }
            else if (particleSystemRenderer.sharedMaterial.HasProperty("_TilingXY"))
            {
                var v = particleSystemRenderer.sharedMaterial.GetVector("_TilingXY");
                render.Rows = (int)v.x;
                render.Columns = (int)v.y;
            }

            render.Frame.Connect(_update.Add(new Multiply(GetNormalizedTime(),
                _update.BuildConstant<float>(render.Rows * render.Columns))));

            if (_rotationType == VariantType.Float)
                render.Rotation.Connect(_update.Add(new GetAttribute("rotation", VariantType.Float)));
            else
                render.Rotation.Connect(_update.BuildConstant(0.0f));

            switch (particleSystemRenderer.renderMode)
            {
                case ParticleSystemRenderMode.Stretch:
                    render.FaceCameraMode = FaceCameraMode.Direction;
                    var dir = _update.Add(new Normalized(_updateVel));
                    render.Pos.Connect(_update.Add(new Add(_updatePos,  _update.Add(new Multiply(dir, _update.Add(new Negate(height)))))));
                    render.Direction.Connect(dir);
                    break;
                default:
                    render.Pos.Connect(_updatePos);
                    switch (particleSystemRenderer.alignment)
                    {
                        default:
                            render.FaceCameraMode = FaceCameraMode.RotateXYZ;
                            render.Direction.Connect(_update.BuildConstant(new Vector3(0, 1, 0)));
                            break;
                    }
                    break;
            }


            _engine.ScheduleAssetExport(particleSystemRenderer.sharedMaterial, _prefabContext);
            _update.Add(render);
        }

        private GraphNode BuildBillboardSize(ParticleSystemRenderer particleSystemRenderer, GraphNode size, VariantType sizeType,
            RenderBillboard render, out GraphOutPin height)
        {
            height = null;
            if (size == null)
            {
                return null;
            }

            if (particleSystemRenderer.renderMode == ParticleSystemRenderMode.Stretch)
            {
                if (sizeType == VariantType.Float)
                {
                    height = StretchSize(particleSystemRenderer, size.Out.FirstOrDefault());
                    return _update.Add(Make.Make_x_y_out(height, size.Out.FirstOrDefault()));
                }
                else if (sizeType == VariantType.Vector3)
                {
                    // swap x and y axises to let urho to match unity
                    var b = _update.Add(new BreakVector3(size));
                    height = StretchSize(particleSystemRenderer, b.X);
                    return _update.Add(Make.Make_x_y_out(height, b.Y));
                }
                else if (sizeType == VariantType.Vector2)
                {
                    return size;
                }
            }
            else
            {
                if (sizeType == VariantType.Float)
                {
                    height = StretchSize(particleSystemRenderer, size.Out.FirstOrDefault());
                    return _update.Add(Make.Make_x_y_out(size.Out.FirstOrDefault(), height));
                }
                else if (sizeType == VariantType.Vector3)
                {
                    var b = _update.Add(new BreakVector3(size));
                    height = StretchSize(particleSystemRenderer, b.Y);
                    return _update.Add(Make.Make_x_y_out(b.X, height));
                }
                else if (sizeType == VariantType.Vector2)
                {
                    return size;
                }
            }

            return null;
        }

        private GraphOutPin StretchSize(ParticleSystemRenderer particleSystemRenderer, GraphOutPin y)
        {
            if (particleSystemRenderer.renderMode != ParticleSystemRenderMode.Stretch)
                return y;

            var result = y;
            if (Math.Abs(particleSystemRenderer.lengthScale - 1.0f) > 1e-6f)
                result = _update.Add(new Multiply(y, _update.BuildConstant(particleSystemRenderer.lengthScale).Out.FirstOrDefault())).Out;

            if (particleSystemRenderer.velocityScale > 1e-6f)
            {
                // ToDo: (optimize) make a single node that implements this math
                var updateVelLength = new Length(_updateVel);
                // 10 is an empirically obtained but accurate multiplier
                // we took 2 particle systems with different settings - one is with velocityScale and second one is with fixed size
                // found that 10 works for differnt values
                var updateVelLengthMultVelScale = new Multiply(updateVelLength, _update.BuildConstant(particleSystemRenderer.velocityScale * 10.0f));
                var totalUpdateVelLengthMultVelScale = new Add(updateVelLengthMultVelScale, _update.BuildConstant(1.0f));
                result = _update.Add(new Multiply(result, totalUpdateVelLengthMultVelScale.Out)).Out;
                _update.Add(updateVelLength);
                _update.Add(updateVelLengthMultVelScale);
                _update.Add(totalUpdateVelLengthMultVelScale);
            }
            return result;
        }

        private GraphNode BuildCone(EmitFrom emitFrom)
        {
            var shape = _particleSystem.shape;
            var cone = new Cone
            {
                Radius = shape.radius,
                RadiusThickness = shape.radiusThickness,
                Angle = shape.angle,
                Rotation = Quaternion.Euler(shape.rotation),
                Translation = shape.position,
                Scale = shape.scale,
                From = emitFrom,
                Length = shape.length
            };
            _init.Add(cone);
            _init.Add(new SetAttribute("pos", VariantType.Vector3, cone.Position));
            var speed = _init.BuildMinMaxCurve(_particleSystem.main.startSpeed,
                GetInitNormalizedDuration, GetInitRandom);
            var vel = _init.Add(new Multiply(speed.Out.FirstOrDefault(), cone.Velocity));
            return vel;
        }
        private GraphNode BuildHemisphere(EmitFrom emitFrom)
        {
            var shape = _particleSystem.shape;
            var cone = new Hemisphere()
            {
                Radius = shape.radius,
                RadiusThickness = shape.radiusThickness,
                Rotation = Quaternion.Euler(shape.rotation),
                Translation = shape.position,
                Scale = shape.scale,
                From = emitFrom,
            };
            _init.Add(cone);
            _init.Add(new SetAttribute("pos", VariantType.Vector3, cone.Position));
            var speed = _init.BuildMinMaxCurve(_particleSystem.main.startSpeed,
                GetInitNormalizedDuration, GetInitRandom);
            return _init.Add(new Multiply(speed.Out.FirstOrDefault(), cone.Velocity));
        }
        private GraphNode BuildSphere(EmitFrom emitFrom)
        {
            var shape = _particleSystem.shape;
            var cone = new Sphere()
            {
                Radius = shape.radius,
                RadiusThickness = shape.radiusThickness,
                Rotation = Quaternion.Euler(shape.rotation),
                Translation = shape.position,
                Scale = shape.scale,
                From = emitFrom,
            };
            _init.Add(cone);
            _init.Add(new SetAttribute("pos", VariantType.Vector3, cone.Position));
            var speed = _init.BuildMinMaxCurve(_particleSystem.main.startSpeed,
                GetInitNormalizedDuration, GetInitRandom);
            return _init.Add(new Multiply(speed.Out.FirstOrDefault(), cone.Velocity));
        }
        private GraphNode GetInitNormalizedDuration()
        {
            if (_initNormalizedDuration != null) return _initNormalizedDuration;

            _initNormalizedDuration =
                _init.Add(new NormalizedEffectTime());
            return _initNormalizedDuration;
        }

        private GraphNode _updateColor;
        private SetAttribute _initRotation;
        private VariantType _rotationType;

        private GraphNode BuildColor()
        {
            if (_updateColor != null)
                return _updateColor;

            _init.Add(new SetAttribute("color", VariantType.Color, _init.BuildMinMaxCurve(_particleSystem.main.startColor, GetInitNormalizedDuration, GetInitRandom) ));

            _updateColor = _update.Add(new GetAttribute("color", VariantType.Color));

            if (_particleSystem.colorOverLifetime.enabled)
            {
                var modulation = _update.BuildMinMaxCurve(_particleSystem.colorOverLifetime.color, GetNormalizedTime, GetUpdateRandom);
                _updateColor = _update.Add(new Multiply(_updateColor, modulation));
            }
            return _updateColor;
        }

        private GraphNode BuildSize(out VariantType sizeType, float scale)
        {
            if (_particleSystem.main.startSize3D)
            {
                var startSizeX = _init.BuildMinMaxCurve(_particleSystem.main.startSizeX, GetInitNormalizedDuration, GetInitRandom, scale);
                var startSizeY = _init.BuildMinMaxCurve(_particleSystem.main.startSizeY, GetInitNormalizedDuration, GetInitRandom, scale);
                var startSizeZ = _init.BuildMinMaxCurve(_particleSystem.main.startSizeZ, GetInitNormalizedDuration, GetInitRandom, scale);
                var startSize = _init.Add(Make.Make_x_y_z_out(startSizeX, startSizeY, startSizeZ));
                _init.Build(GraphNodeType.SetAttribute, new GraphInPin("", VariantType.Vector3, startSize),
                    new GraphOutPin("size", VariantType.Vector3));
                sizeType = VariantType.Vector3;
            }
            else
            {
                var startSize = _init.BuildMinMaxCurve(_particleSystem.main.startSize, GetInitNormalizedDuration, GetInitRandom, scale);
                _init.Build(GraphNodeType.SetAttribute, new GraphInPin("", VariantType.Float, startSize),
                    new GraphOutPin("size", VariantType.Float));
                sizeType = VariantType.Float;
            }

            var updateSize = _update.Build(GraphNodeType.GetAttribute,
                new GraphOutPin("size", sizeType));

            if (_particleSystem.sizeOverLifetime.enabled)
            {
                var size = _particleSystem.sizeOverLifetime;
                //if (!size.separateAxes)
                {
                    var sizeScale = _update.BuildMinMaxCurve(size.size, GetNormalizedTime,
                        GetUpdateRandom);
                    updateSize = _update.Add(new Multiply(updateSize, sizeScale));
                }
            }

            return updateSize;
        }

        private GraphNode GetUpdateRandom()
        {
            GetInitRandom();
            if (_updateRandom != null)
                return _updateRandom;
            _updateRandom = _update.Build(GraphNodeType.GetAttribute, new GraphOutPin("rnd", VariantType.Float));
            return _updateRandom;
        }

        private GraphNode GetNormalizedTime()
        {
            if (_updateNormTime != null)
                return _updateNormTime;
            var t = GetTime();
            var lt = GetLifeTime();
            _updateNormTime = _update.Add(new Divide(t, lt));
            return _updateNormTime;
        }

        private GraphNode GetTime()
        {
            if (_updateTime != null)
                return _updateTime;
            GraphNode getTime = _update.Add(new GetAttribute("time", VariantType.Float));
            getTime = _update.Add(new Add(getTime, _update.Add(new TimeStep())));
            _updateTime = _update.Add(new SetAttribute("time", VariantType.Float, getTime));
            return _updateTime;
        }

        private GraphNode GetLifeTime()
        {
            return _updateLifeTime ?? (_updateLifeTime =
                _update.Build(GraphNodeType.GetAttribute, new GraphOutPin("lifetime", VariantType.Float)));
        }


        private GraphNode GetInitRandom()
        {
            if (_initRandom != null)
                return _initRandom;
            var rnd = _init.Add(new Random());
            _initRandom = _init.Build(GraphNodeType.SetAttribute, new GraphInPin("", VariantType.Float, rnd),
                new GraphOutPin("rnd", VariantType.Float));
            return _initRandom;
        }
    }
}