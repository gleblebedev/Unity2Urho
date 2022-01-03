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

            _initTime = _init.Add(new SetAttribute("time", VariantType.Float, _init.BuildConstant(0.0f)));
            _initLifeTime = _init.Add(new SetAttribute("lifetime", VariantType.Float, _init.BuildMinMaxCurve(particleSystem.main.startLifetime, particleSystem.main.startLifetimeMultiplier, GetInitNormalizedDuration, GetInitRandom)));
            _initRotation = _init.Add(new SetAttribute("rotation", VariantType.Float, _init.BuildMinMaxCurve(_particleSystem.main.startRotation, _particleSystem.main.startRotationMultiplier, GetInitNormalizedDuration, GetInitRandom)));
            switch (particleSystem.shape.shapeType)
            {
                case ParticleSystemShapeType.Sphere:
                    BuildSphere(EmitFrom.Volume);
                    break;
                case ParticleSystemShapeType.SphereShell:
                    BuildSphere(EmitFrom.Surface);
                    break;
                case ParticleSystemShapeType.Hemisphere:
                    BuildHemisphere(EmitFrom.Volume);
                    break;
                case ParticleSystemShapeType.HemisphereShell:
                    BuildHemisphere(EmitFrom.Surface);
                    break;
                case ParticleSystemShapeType.Cone:
                    BuildCone(EmitFrom.Base);
                    break;
                //case ParticleSystemShapeType.Box:
                //    break;
                //case ParticleSystemShapeType.Mesh:
                //    break;
                case ParticleSystemShapeType.ConeShell:
                    BuildCone(EmitFrom.Surface);
                    break;
                case ParticleSystemShapeType.ConeVolume:
                    BuildCone(EmitFrom.Volume);
                    break;
                case ParticleSystemShapeType.ConeVolumeShell:
                    BuildCone(EmitFrom.Surface);
                    break;
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
                default:
                {
                    _init.Add(new SetAttribute("pos", VariantType.Vector3, _init.BuildConstant(Vector3.zero)));
                    _init.Add(new SetAttribute("vel", VariantType.Vector3, _init.BuildConstant(Vector3.zero)));
                    break;
                }
            }

            _update.Build("Expire", new GraphInPin("time", GetTime()), new GraphInPin("lifetime", GetLifeTime()));
            _updatePos = _update.Add(new GetAttribute("pos", VariantType.Vector3));
            _updateVel = _update.Add(new GetAttribute("vel", VariantType.Vector3));
            var getVel = _updateVel;

            var velocityOverLifetime = _particleSystem.velocityOverLifetime;
            if (velocityOverLifetime.enabled)
            {
                var linearX = _update.BuildMinMaxCurve(velocityOverLifetime.x, velocityOverLifetime.xMultiplier, GetNormalizedTime,
                    GetUpdateRandom);
                var linearY = _update.BuildMinMaxCurve(velocityOverLifetime.y, velocityOverLifetime.yMultiplier, GetNormalizedTime,
                    GetUpdateRandom);
                var linearZ = _update.BuildMinMaxCurve(velocityOverLifetime.z, velocityOverLifetime.zMultiplier, GetNormalizedTime,
                    GetUpdateRandom);
                _updateVel = _update.Add(Make.Make_x_y_z_out(linearX, linearY, linearZ));
            }

            var limitVelocityOverLifetime = _particleSystem.limitVelocityOverLifetime;
            if (limitVelocityOverLifetime.enabled)
            {
                _updateVel = _update.Add(new LimitVelocity(_updateVel, _update.BuildMinMaxCurve(limitVelocityOverLifetime.limit, limitVelocityOverLifetime.limitMultiplier, GetNormalizedTime, GetUpdateRandom))
                {
                    Dampen = limitVelocityOverLifetime.dampen
                });
            }
            var forceOverLifetime = _particleSystem.forceOverLifetime;
            if (forceOverLifetime.enabled)
            {
                var x = _update.BuildMinMaxCurve(forceOverLifetime.x, forceOverLifetime.xMultiplier, GetNormalizedTime,
                    GetUpdateRandom);
                var y = _update.BuildMinMaxCurve(forceOverLifetime.y, forceOverLifetime.yMultiplier, GetNormalizedTime,
                    GetUpdateRandom);
                var z = _update.BuildMinMaxCurve(forceOverLifetime.z, forceOverLifetime.zMultiplier, GetNormalizedTime,
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


            var renderer = particleSystem.GetComponent<Renderer>();
            if (renderer is ParticleSystemRenderer particleSystemRenderer)
            {
                switch (particleSystemRenderer.renderMode)
                {
                    case ParticleSystemRenderMode.Billboard:
                        AddRenderBillboard(particleSystemRenderer);
                        break;
                    case ParticleSystemRenderMode.Stretch:
                        AddRenderBillboard(particleSystemRenderer);
                        break;
                    case ParticleSystemRenderMode.HorizontalBillboard:
                        AddRenderBillboard(particleSystemRenderer);
                        break;
                    case ParticleSystemRenderMode.VerticalBillboard:
                        AddRenderBillboard(particleSystemRenderer);
                        break;
                    case ParticleSystemRenderMode.Mesh:
                        AddRenderMesh(particleSystemRenderer);
                        break;
                }
            }
        }

        private void AddRenderMesh(ParticleSystemRenderer particleSystemRenderer)
        {
            var size = BuildSize(out var sizeType);
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
            var transform = _update.Add(Make.Make_translation_rotation_scale_out(_updatePos, _update.BuildConstant(Quaternion.identity), scale));
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
            var size = BuildSize(out var sizeType);
            if (size != null)
            {
                if (sizeType == VariantType.Float)
                {
                    render.Size.Connect(_update.Add(Make.Make_x_y_out(size, size)));
                }
                else if (sizeType == VariantType.Vector3)
                {
                    var b = _update.Add(new BreakVector3(size));
                    render.Size.Connect(_update.Add(Make.Make_x_y_out(b.X, b.Y)));
                }
                else if (sizeType == VariantType.Vector2)
                {
                    render.Size.Connect(size);
                }
            }

            render.Pos.Connect(_updatePos);
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
            render.Rotation.Connect(_update.Add(new GetAttribute("rotation", VariantType.Float)));
            _engine.ScheduleAssetExport(particleSystemRenderer.sharedMaterial, _prefabContext);
            _update.Add(render);
        }

        private void BuildCone(EmitFrom emitFrom)
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
            var speed = _init.BuildMinMaxCurve(_particleSystem.main.startSpeed, _particleSystem.main.startSpeedMultiplier,
                GetInitNormalizedDuration, GetInitRandom);
            var vel = _init.Add(new Multiply(speed.Out.FirstOrDefault(), cone.Velocity));
            _init.Add(new SetAttribute("vel", VariantType.Vector3, vel));
        }
        private void BuildHemisphere(EmitFrom emitFrom)
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
            var speed = _init.BuildMinMaxCurve(_particleSystem.main.startSpeed, _particleSystem.main.startSpeedMultiplier,
                GetInitNormalizedDuration, GetInitRandom);
            var vel = _init.Add(new Multiply(speed.Out.FirstOrDefault(), cone.Velocity));
            _init.Add(new SetAttribute("vel", VariantType.Vector3, vel));
        }
        private void BuildSphere(EmitFrom emitFrom)
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
            var speed = _init.BuildMinMaxCurve(_particleSystem.main.startSpeed, _particleSystem.main.startSpeedMultiplier,
                GetInitNormalizedDuration, GetInitRandom);
            var vel = _init.Add(new Multiply(speed.Out.FirstOrDefault(), cone.Velocity));
            _init.Add(new SetAttribute("vel", VariantType.Vector3, vel));
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
        private GraphNode BuildSize(out VariantType sizeType)
        {
            if (_particleSystem.main.startSize3D)
            {
                var startSizeX = _init.BuildMinMaxCurve(_particleSystem.main.startSizeX,
                    _particleSystem.main.startSizeXMultiplier, GetInitNormalizedDuration, GetInitRandom);
                var startSizeY = _init.BuildMinMaxCurve(_particleSystem.main.startSizeY,
                    _particleSystem.main.startSizeYMultiplier, GetInitNormalizedDuration, GetInitRandom);
                var startSizeZ = _init.BuildMinMaxCurve(_particleSystem.main.startSizeZ,
                    _particleSystem.main.startSizeZMultiplier, GetInitNormalizedDuration, GetInitRandom);
                var startSize = _init.Add(Make.Make_x_y_z_out(startSizeX, startSizeY, startSizeZ));
                _init.Build(GraphNodeType.SetAttribute, new GraphInPin("", VariantType.Vector3, startSize),
                    new GraphOutPin("size", VariantType.Vector3));
                sizeType = VariantType.Vector3;
            }
            else
            {
                var startSize = _init.BuildMinMaxCurve(_particleSystem.main.startSize,
                    _particleSystem.main.startSizeMultiplier, GetInitNormalizedDuration, GetInitRandom);
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
                    var sizeScale = _update.BuildMinMaxCurve(size.size, size.sizeMultiplier, GetNormalizedTime,
                        GetUpdateRandom);
                    updateSize = _update.Add(new Multiply(updateSize, sizeScale));
                }
            }

            return updateSize;
        }

        private GraphNode GetUpdateRandom()
        {
            GetInitRandom();
            return _updateRandom ?? (_updateRandom =
                _update.Build(GraphNodeType.GetAttribute, new GraphOutPin("rnd", VariantType.Float)));
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