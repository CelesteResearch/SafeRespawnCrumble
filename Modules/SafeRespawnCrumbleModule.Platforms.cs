using Celeste.Mods.SafeRespawnCrumble.Gameplay.Platforms;

namespace Celeste.Mods.SafeRespawnCrumble.Modules
{
    public sealed partial class SafeRespawnCrumbleModule
    {
        private SafeCrumbleActivationHooks crumbleHooks;
        private void LoadCrumbleHooks() => crumbleHooks = new SafeCrumbleActivationHooks(SourceEngine);

        private void UnloadCrumbleHooks()
        {
            crumbleHooks?.Dispose();
            crumbleHooks = null;
        }
    }
}
