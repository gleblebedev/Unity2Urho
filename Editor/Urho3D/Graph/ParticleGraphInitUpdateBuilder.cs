using System;
using System.Linq;
using Assets.Unity2Urho.Editor.Urho3D.Graph.ParticleNodes;
using UnityEngine;
using Random = Assets.Unity2Urho.Editor.Urho3D.Graph.ParticleNodes.Random;

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
                //case ParticleSystemShapeType.ConeVolumeShell:
                //    break;
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

            var velocityOptions = _particleSystem.velocityOverLifetime;
            if (velocityOptions.enabled)
            {
                var linearX = _update.BuildMinMaxCurve(velocityOptions.x, velocityOptions.xMultiplier, GetNormalizedTime,
                    GetUpdateRandom);
                var linearY = _update.BuildMinMaxCurve(velocityOptions.y, velocityOptions.yMultiplier, GetNormalizedTime,
                    GetUpdateRandom);
                var linearZ = _update.BuildMinMaxCurve(velocityOptions.z, velocityOptions.zMultiplier, GetNormalizedTime,
                    GetUpdateRandom);
                _updateVel = _update.Add(new MakeVec3(linearX, linearY, linearZ));
            }

            _updatePos = _update.Add(new Move(_updatePos, _updateVel));
            _updatePos = _update.Add(new SetAttribute("pos", VariantType.Vector3, _updatePos));

            var renderer = particleSystem.GetComponent<Renderer>();
            if (renderer is ParticleSystemRenderer particleSystemRenderer)
            {
                var render = new RenderBillboard();
                render.Color.Connect(BuildColor());
                var size = BuildSize(out var sizeType);
                if (size != null)
                {
                    if (sizeType == VariantType.Float)
                        render.Size.Connect(_update.Add(new MakeVec2(size, size)));
                    else if (sizeType == VariantType.Vector2) render.Size.Connect(size);
                }
                render.Pos.Connect(_updatePos);
                render.Material = _engine.EvaluateMaterialName(particleSystemRenderer.sharedMaterial, _prefabContext);
                if (particleSystem.textureSheetAnimation.enabled)
                {
                    render.Rows = particleSystem.textureSheetAnimation.numTilesY;
                    render.Columns = particleSystem.textureSheetAnimation.numTilesX;
                }
                else if (particleSystemRenderer.sharedMaterial.HasProperty("_TilingXY"))
                {
                    var v = particleSystemRenderer.sharedMaterial.GetVector("_TilingXY");
                    render.Rows = (int)v.x;
                    render.Columns = (int)v.y;
                }
                _engine.ScheduleAssetExport(renderer.sharedMaterial, _prefabContext);
                _update.Add(render);
            }
            else if (renderer is MeshRenderer meshRenderer)
            {
                //var render = new RenderMesh();
            }
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
                _init.Build(GraphNodeType.NormalizedEffectTime, new GraphOutPin("out", VariantType.Float));
            return _initNormalizedDuration;
        }

        private GraphNode _updateColor;
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
            //if (particleSystem.main.startSize3D)
            //{
            //    var startSize = init.BuildMinMaxCurve(particleSystem.main.startSizeX, particleSystem.main.startSizeY, particleSystem.main.startSizeZ);

            //}
            //else
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
            return _updateTime ?? (_updateTime =
                _update.Build(GraphNodeType.ParticleTime, new GraphOutPin("time", VariantType.Float)));
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