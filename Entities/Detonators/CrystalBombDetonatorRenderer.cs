using System.Numerics;
using Celeste.Core.Display;
using Celeste.Core.Geometry;
using Celeste.Core.Mathematics;
using Celeste.Core.Reflection.Runtime;
using Celeste.Core.Rendering;
using Celeste.Core.Scheduling;
using Celeste.Graphics.Geometry.TwoD.Camera;
using Celeste.Scene.Tracking;
using Celeste.Scene.Tracking.Attributes;
using Celeste.Simulation.Gameplay.Enemies.Seeker;

namespace Celeste.Mods.SafeRespawnCrumble.Entities.Detonators
{
    [SceneTracked(false)]
    [RuntimeTypeAlias("Celeste.Mod.SafeRespawnCrumble.CrystalBombDetonatorRenderer")]
    public sealed class CrystalBombDetonatorRenderer : SeekerBarrierRendererState
    {
        private readonly ISourceEngineContext engine;

        public CrystalBombDetonatorRenderer(ISourceEngineContext engine)
            : base(engine, (ISourceRenderEnvironment)engine, PackedColor.Purple, .45f, .55f)
        {
            this.engine = engine;
        }

        protected override Vector2 BloomSegmentEnd(Vector2 at, Vector2 perpendicular, float wave)
            => Vector2Math.Round(at + perpendicular * wave);

        public override void Update()
        {
            if (Dirty) RebuildEdges();
            var scene = ((ISourceScene)Scene).SourceScene;
            var camera = ((ISourceCameraScene)Scene).Camera;
            var view = new IntRect((int)camera.Left - 4, (int)camera.Top - 4,
                (int)(camera.Right - camera.Left) + 8, (int)(camera.Bottom - camera.Top) + 8);
            for (int i = 0; i < Edges.Count; i++)
            {
                var edge = Edges[i];
                if (edge.Visible && scene.Clock.OnInterval(.25f, i * .01f, engine) && !edge.InView(ref view))
                    edge.Visible = false;
                else if (scene.Clock.OnInterval(.05f, i * .01f, engine) && edge.InView(ref view))
                    edge.Visible = true;
                if (edge.Visible && (scene.Clock.OnInterval(.05f, i * .01f, engine) || edge.Wave == null))
                    edge.UpdateWave(scene.Clock.TimeActive * 3f);
            }
        }
    }
}
