using System;
using System.Reflection;
using Celeste.Core.Scheduling;
using Celeste.Everest.Modules.Runtime;
using Celeste.Game.Execution.Levels;
using Celeste.Game.Execution.LevelLoader;
using Celeste.Mods.SafeRespawnCrumble.Entities.Detonators;
using MonoMod.RuntimeDetour;

namespace Celeste.Mods.SafeRespawnCrumble.Modules
{
    public sealed partial class SafeRespawnCrumbleModule : SourceEverestModule
    {
        private IDisposable loadingHook;

        public SafeRespawnCrumbleModule(ISourceEngineContext engine) : base(engine)
        {
        }

        public override void Load()
        {
            LoadCrumbleHooks();
            try
            {
                loadingHook = new Hook(
                    typeof(OriginalLevelLoaderScene).GetMethod("LoadingThread",
                        BindingFlags.Instance | BindingFlags.NonPublic),
                    (Action<Action<OriginalLevelLoaderScene>, OriginalLevelLoaderScene>)LoadingThread);
            }
            catch
            {
                UnloadCrumbleHooks();
                throw;
            }
        }

        private void LoadingThread(Action<OriginalLevelLoaderScene> original, OriginalLevelLoaderScene loader)
        {
            if (ReferenceEquals(loader.EngineContext, SourceEngine))
                loader.Level.Scheduler.QueueSourceEntity(new CrystalBombDetonatorRenderer(SourceEngine));
            original(loader);
        }

        public override void Unload()
        {
            UnloadCrumbleHooks();
            loadingHook?.Dispose();
            loadingHook = null;
        }
    }
}
