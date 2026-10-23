// Copyright (c) Spring Collab 2020 contributors. Licensed under the MIT License.

using System.Collections;
using System.Collections.Generic;
using System.Numerics;
using Celeste.Core.Display;
using Celeste.Core.Scheduling;
using Celeste.Core.Mathematics.Easing;
using Celeste.Graphics.Animation;
using Celeste.Graphics.Content;
using Celeste.Core.Mathematics;
using Celeste.Graphics.Content.Atlas;
using Celeste.Game.Execution.Graphics;
using Celeste.Game.Execution.Levels;
using Celeste.Game.Content.Maps.Levels;
using Celeste.Game.Content.Maps.Entities;
using Celeste.Scene.Components.Oscillation;
using Celeste.Scene.Tracking.Attributes;
using Celeste.Core.Reflection.Runtime;
using Celeste.Everest.Modules.Entities.Metadata;
using Celeste.Formats.Map.Entities.Structure;
using Celeste.Formats.Map.Entities.Attributes;
using Celeste.Simulation;
using Celeste.Simulation.Gameplay.Platforms.Foundation;

namespace Celeste.Mods.SafeRespawnCrumble.Entities.Platforms
{
    [SceneTracked]
    [RuntimeTypeAlias("Celeste.Mod.SafeRespawnCrumble.SafeRespawnCrumble")]
    [CustomEntity("SafeRespawnCrumble/SafeRespawnCrumble")]
    public sealed class SafeRespawnCrumble : SourceSolidEntityState
    {
        private readonly ISourceEngineContext engine;

        public SafeRespawnCrumble(OriginalLevelSceneRoot level, EntityData data, Vector2 offset)
            : base(data.Position + offset, data.Width, 8f, true,
                level.SolidWorldFactory.Create(data.Position + offset, false))
        {
            engine = level.EngineContext;
            EnableAssistModeChecks = false;
            Collidable = false;
            hasOutline = data.Bool("hasOutline", true);
        }

        public static SafeRespawnCrumble Load(OriginalLevelSceneRoot level, OriginalLevelRoomData room, Vector2 offset,
            OriginalLevelEntityData data)
            => new SafeRespawnCrumble(level, data, offset);

        public override void Added(object scene)
        {
            base.Added(scene);

            // Set up the outline and block tiles and first-initialize the fader coroutines
            AtlasEntry outlineTexture =
                ((ISourceGameGraphicsEnvironment)engine).GameGraphics.GameAtlas.Entries["objects/crumbleBlock/outline"];
            AtlasEntry tileTexture =
                ((ISourceGameGraphicsEnvironment)engine).GameGraphics.GameAtlas.Entries[
                    "objects/SafeRespawnCrumble/tile"];

            tiles = new List<SpriteImage>();
            outlineTiles = new List<SpriteImage>();

            if (Width <= 8f)
            {
                SpriteImage toDraw = new SpriteImage(outlineTexture.GetSubtexture(24, 0, 8, 8));
                toDraw.Color = PackedColor.White;
                outlineTiles.Add(toDraw);
            }
            else
            {
                // Select left, middle, or right
                for (int tile = 0; (float)tile < base.Width; tile += 8)
                {
                    int tileTex;
                    if (tile == 0)
                        tileTex = 0;
                    else if (tile > 0 && (float)tile < base.Width - 8f)
                        tileTex = 1;
                    else
                        tileTex = 2;

                    SpriteImage toDraw = new SpriteImage(outlineTexture.GetSubtexture(tileTex * 8, 0, 8, 8));
                    toDraw.Position = new Vector2((float)tile, 0f);
                    outlineTiles.Add(toDraw);
                    if (hasOutline)
                    {
                        OwnedComponents.Add(toDraw);
                    }
                }
            }

            // Add pink-block tiles
            for (int tile = 0; (float)tile < base.Width; tile += 8)
            {
                SpriteImage toDraw = new SpriteImage(tileTexture);
                toDraw.CenterOrigin();
                toDraw.Position = new Vector2((float)tile, 0f) + new Vector2(toDraw.Width / 2f, toDraw.Height / 2f);
                toDraw.Color = PackedColor.White * 0f;
                tiles.Add(toDraw);
                OwnedComponents.Add(toDraw);
            }

            OwnedComponents.Add(outlineFader = new SimulationCoroutine(engine, false));
            OwnedComponents.Add(tileFader = new SimulationCoroutine(engine, false));

            OwnedComponents.Add(new SimulationCoroutine(engine, Sequence(), true));

            // Let's try adding a wiggler.
            wiggler = ((ISourceWigglerPoolScope)engine).WigglerPool.Create(
                0.5f,
                4f,
                delegate(float v)
                {
                    foreach (SpriteImage tile in tiles)
                    {
                        tile.Scale = Vector2.One * (1f + v * 0.35f);
                    }
                },
                false,
                false
            );
            OwnedComponents.Add(wiggler);
        }

        private IEnumerator Sequence()
        {
            for (;;)
            {
                // Wait until activated
                Collidable = false;
                while (!activated)
                    yield return null;

                // Fade out the outline, fade in the tiles. Oh, and make ourselves collidable.
                if (isRespawn)
                    wiggler.Start(1.5f, 1.5f);
                else
                    wiggler.Start(0.5f, 4f);
                tileFader.Replace(TileFade(1f, tiles, !isRespawn));
                outlineFader.Replace(TileFade(0f, outlineTiles, !isRespawn));
                Collidable = true;

                // Wait until player is found
                while (GetPlayerOnTop() == null)
                    yield return null;

                // Wait until player leaves
                while (CheckToStayEnabled())
                    yield return null;

                // Fade out tiles, fade in outline.
                tileFader.Replace(TileFade(0f, tiles, true));
                outlineFader.Replace(TileFade(1f, outlineTiles, true));

                // Do nothing until next activation
                activated = false;
                isRespawn = false;
            }
        }

        private bool CheckToStayEnabled()
        {
            RoomPlayerSession p = GetPlayerOnTop();
            if (p != null)
            {
                if (p.Data.StartedDashing)
                {
                    p.Data.Motion.JumpGraceTimer = 0f;
                    return false;
                }

                if (p.Data.Motion.Ducking)
                    return false;
                return true;
            }

            return false;
        }

        // Fade the passed tiles in or out.
        private IEnumerator TileFade(float to, List<SpriteImage> targetTiles, bool fast = false)
        {
            float from = 1f - to;
            for (float t = 0f; t < 1f; t += engine.DeltaTime * (fast ? 5f : 1.5f))
            {
                foreach (SpriteImage img in targetTiles)
                    img.Color = PackedColor.White *
                                (from + (to - from) * ((ISourceEasingEnvironment)engine).EasingArithmetic.CubeInOut(t));
                yield return null;
            }

            yield break;
        }

        internal static void Activate(RoomPlayerSession player, bool respawn)
        {
            SafeRespawnCrumble target = null;
            float distance = float.MaxValue;
            foreach (var entity in ((Celeste.Scene.Tracking.ISourceScene)player.Scene).SourceScene.Entities
                     .GetTrackedEntities(typeof(SafeRespawnCrumble)))
            {
                float next =
                    ((ISourceVectorEnvironment)player.EngineContext).VectorArithmetic.DistanceSquared(
                        ((SafeRespawnCrumble)entity).Position, player.Position);
                if (next < distance)
                {
                    target = (SafeRespawnCrumble)entity;
                    distance = next;
                }
            }

            if (target == null) return;
            if (target.Left < player.X && target.Right > player.X && target.Top - 12f <= player.Y &&
                target.Bottom > player.Y)
            {
                target.activated = true;
                target.isRespawn = respawn;
            }
        }

        private List<SpriteImage> tiles;
        private List<SpriteImage> outlineTiles;
        private SimulationCoroutine tileFader;
        private SimulationCoroutine outlineFader;
        private WigglerComponent wiggler;
        private bool activated;
        private bool isRespawn;
        private readonly bool hasOutline;
    }
}
