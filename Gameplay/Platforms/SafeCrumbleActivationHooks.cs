using System;
using System.Collections;
using System.Reflection;
using Celeste.Core.Scheduling;
using SafePlatform = Celeste.Mods.SafeRespawnCrumble.Entities.Platforms.SafeRespawnCrumble;
using Celeste.Simulation;
using Celeste.Simulation.Gameplay.Player.Respawn;
using MonoMod.RuntimeDetour;

namespace Celeste.Mods.SafeRespawnCrumble.Gameplay.Platforms
{
    public sealed class SafeCrumbleActivationHooks : IDisposable
    {
        private readonly ISourceEngineContext engine;
        private IDisposable cassetteHook;
        private IDisposable respawnHook;

        public SafeCrumbleActivationHooks(ISourceEngineContext engine)
        {
            this.engine = engine;
            try
            {
                cassetteHook = new Hook(
                    typeof(RoomPlayerSession).GetMethod("CassetteFlyCoroutine",
                        BindingFlags.Instance | BindingFlags.NonPublic),
                    (Func<Func<RoomPlayerSession, IEnumerator>, RoomPlayerSession, IEnumerator>)CassetteFly);
                respawnHook = new Hook(
                    typeof(PlayerRespawnSequence).GetMethod("Begin", BindingFlags.Instance | BindingFlags.NonPublic),
                    (Action<Action<PlayerRespawnSequence>, PlayerRespawnSequence>)RespawnBegin);
            }
            catch
            {
                Dispose();
                throw;
            }
        }

        private IEnumerator CassetteFly(Func<RoomPlayerSession, IEnumerator> orig, RoomPlayerSession player)
        {
            IEnumerator routine = orig(player);
            while (routine.MoveNext()) yield return routine.Current;
            if (ReferenceEquals(player.EngineContext, engine)) SafePlatform.Activate(player, false);
        }

        private void RespawnBegin(Action<PlayerRespawnSequence> orig, PlayerRespawnSequence sequence)
        {
            orig(sequence);
            if (ReferenceEquals(sequence.Player.EngineContext, engine))
                SafePlatform.Activate(sequence.Player, true);
        }

        public void Dispose()
        {
            respawnHook?.Dispose();
            respawnHook = null;
            cassetteHook?.Dispose();
            cassetteHook = null;
        }
    }
}
