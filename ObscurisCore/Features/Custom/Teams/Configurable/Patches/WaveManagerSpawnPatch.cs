using Christmas.Scp2536.Gifts;

using HarmonyLib;

using LabExtended.Core;

using Respawning;
using Respawning.Waves;
using Respawning.Waves.Generic;

namespace ObscurisCore.Features.Custom.Teams.Configurable.Patches;

public static class WaveManagerSpawnPatch
{
    [HarmonyPatch(typeof(WaveManager), nameof(WaveManager.Spawn))]
    private static bool Prefix(SpawnableWaveBase wave)
    {
        if (wave is IMiniWave)
        {
            ApiLog.Debug("WaveManagerSpawnPatch", $"Ignoring mini-wave: &3{wave.GetType().Name}&r");
            return true;
        }
        
        ApiLog.Debug("WaveManagerSpawnPatch", $"Attempting to replace wave: &3{wave.GetType().Name}&r");

        if (!ConfigurableTeam.TryPickPlayers(out var players))
        {
            ApiLog.Debug("WaveManagerSpawnPatch", $"Failed to pick players for wave: &3{wave.GetType().Name}&r");
            return true;
        }

        ApiLog.Debug("WaveManagerSpawnPatch", $"Attempting to pick team for wave: &3{wave.GetType().Name}&r");
        
        if (!ConfigurableTeam.TryPickTeam(players, wave, out var configurableTeam))
        {
            ApiLog.Debug("WaveManagerSpawnPatch", $"Failed to pick team for wave: &3{wave.GetType().Name}&r");
            return true;
        }
        
        ApiLog.Debug("WaveManagerSpawnPatch", $"Attempting to spawn wave: &3{wave.GetType().Name}&r");

        if (ConfigurableTeam.TrySpawnWave(players, configurableTeam, wave))
        {
            ApiLog.Debug("WaveManagerSpawnPatch", "Spawned replacement wave!");
            
            WaveManager.State = WaveQueueState.WaveSpawned;
            WaveManager._nextWave = null;
            
            var hubs = players.Select(p => p.ReferenceHub).ToList();
            
            wave.OnWaveSpawned();
            
            // OnWaveSpawned event start
            BuddyInABox._waveSpawnedAlready = true;

            if (configurableTeam.TokenCost > 0 && configurableTeam.DeductTokens && wave is ILimitedWave limitedWave)
            {
                ApiLog.Debug("WaveManagerSpawnPatch", $"Removing &3{configurableTeam.TokenCost}&r token(s) from &3{limitedWave.GetType().Name}&r");
                
                limitedWave.RespawnTokens -= configurableTeam.TokenCost;
                
                WaveUpdateMessage.ServerSendUpdate(wave, UpdateMessageFlags.Tokens);
            }

            WaveManager.Waves.ForEach(w => w.OnAnyWaveSpawned(wave, hubs));
            // OnWaveSpawned event end

            WaveManager.State = WaveQueueState.Idle;
            
            ApiLog.Debug("WaveManagerSpawnPatch", $"Finished replacing wave: &3{wave.GetType().Name}&r");
            return false;
        }
        
        ApiLog.Debug("WaveManagerSpawnPatch", $"Failed to replace wave: &3{wave.GetType().Name}&r");
        return true;
    }
}