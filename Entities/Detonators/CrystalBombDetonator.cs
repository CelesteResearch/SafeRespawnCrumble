using System;
using System.Collections.Generic;
using System.Numerics;
using System.Reflection;
using Celeste.Core.Display;
using Celeste.Core.Reflection.Runtime;
using Celeste.Core.Rendering;
using Celeste.Core.Scheduling;
using Celeste.Everest.Modules.Entities.Metadata;
using Celeste.Formats.Map.Entities.Structure;
using Celeste.Game.Content.Maps.Entities;
using Celeste.Game.Content.Maps.Levels;
using Celeste.Game.Execution.Levels;
using Celeste.Graphics.Display;
using Celeste.Graphics.Rendering;
using Celeste.Physics.Collision;
using Celeste.Scene.Entities;
using Celeste.Scene.Tracking;
using Celeste.Scene.Tracking.Attributes;
using Celeste.Simulation.Gameplay.Actors.Foundation;
using Celeste.Simulation.Gameplay.Enemies.Seeker;
using Celeste.Simulation.Gameplay.Platforms.Foundation;

namespace Celeste.Mods.SafeRespawnCrumble.Entities.Detonators
{
    [SceneTracked(false)]
    [RuntimeTypeAlias("Celeste.Mod.SafeRespawnCrumble.CrystalBombDetonator")]
    [CustomEntity("CrystalBombDetonator/CrystalBombDetonator = Load")]
    public sealed class CrystalBombDetonator : SeekerBarrierState
    {
        private static MethodInfo bombExplosionMethod;
        private readonly List<SourceActorEntityState> overlappingActors = new();
        private readonly string rendererTrackerKey;
        private readonly string adjacentTrackerKey;
        protected override string RendererTrackerKey => rendererTrackerKey;
        protected override string AdjacentTrackerKey => adjacentTrackerKey;

        public static CrystalBombDetonator Load(OriginalLevelSceneRoot level, OriginalLevelRoomData room,
            Vector2 offset, OriginalLevelEntityData data) => new(data, offset, level.SolidWorldFactory);

        public CrystalBombDetonator(EntityData data, Vector2 offset, ISourceSolidWorldFactory worlds)
            : this(data.Position + offset, data.Width, data.Height, worlds.Create(data.Position + offset, false))
        {
        }

        public CrystalBombDetonator(Vector2 position, float width, float height, ISourceSolidWorld world)
            : base(position, width, height, world)
        {
            var catalog = ((ISourceSceneCatalogEnvironment)world.EngineContext).SceneCatalog;
            rendererTrackerKey = catalog.Find(typeof(CrystalBombDetonatorRenderer)).Name;
            adjacentTrackerKey = catalog.Find(typeof(CrystalBombDetonator)).Name;
        }

        public void OnTriggerDetonation() => OnReflectSeeker();

        public override void Update()
        {
            base.Update();
            CheckForBombs();
        }

        private void CheckForBombs()
        {
            // Original Entity.CollideAll<Actor> snapshots the active entity list, not a tracked Actor bucket.
            var entities = ((ISourceScene)Scene).SourceScene.Entities.Entities;
            overlappingActors.Clear();
            for (int i = 0; i < entities.Count; i++)
                if (entities[i] is SourceActorEntityState actor && SourceEntityCollision.Check(this, actor))
                    overlappingActors.Add(actor);
            foreach (var actor in overlappingActors)
                if (actor.GetType().ToString().Contains("CrystalBomb"))
                {
                    bombExplosionMethod ??=
                        actor.GetType().GetMethod("Explode", BindingFlags.Instance | BindingFlags.NonPublic);
                    bombExplosionMethod.Invoke(actor, null);
                    if (!Flashing) OnTriggerDetonation();
                }
        }

        public override void Render(ISourceRenderContext context)
        {
            var color = PackedColor.Yellow * .6f;
            foreach (var particle in ParticlePositions)
            {
                var image = new AtlasTextureDraw(
                    ((Celeste.Graphics.Rendering.Drawing.ISourceDrawScope)context).Draw.Pixel,
                    Position + particle, Vector2.Zero, color, Vector2.One);
                ((ISourceRenderCommandSink<AtlasTextureDraw>)context).Draw(in image, this);
            }

            if (Flashing)
                SourcePrimitiveDrawing.Rect(context, this, Collider.AbsoluteLeft, Collider.AbsoluteTop,
                    Collider.Width, Collider.Height, PackedColor.Purple * Flash * .5f);
        }
    }
}
