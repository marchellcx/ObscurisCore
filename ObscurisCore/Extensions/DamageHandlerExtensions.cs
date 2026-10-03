using InventorySystem.Items.Scp1509;

using LabExtended.API;

using PlayerRoles.PlayableScps.Scp1507;
using PlayerRoles.PlayableScps.Scp3114;
using PlayerRoles.PlayableScps.Scp939;

using PlayerStatsSystem;

namespace ObscurisCore.Extensions;

/// <summary>
/// Provides extension methods for handling and translating damage-related operations
/// in the game's damage handling system.
/// </summary>
public static class DamageHandlerExtensions
{
    /// <summary>
    /// Translates a given death reason based on the type of the damage handler and the context of the attacker.
    /// </summary>
    /// <param name="damageHandler">The damage handler instance that provides the details of the death.</param>
    /// <param name="attackerText">Optional text representing the attacker responsible for the death, such as a player name.</param>
    /// <returns>A string representing the translated death reason with contextual information.</returns>
    public static string TranslateDeathReason(this DamageHandlerBase damageHandler, string? attackerText)
    {
        if (damageHandler == null)
            return "(null)";

        if (damageHandler is AttackerDamageHandler attackerDamageHandler)
        {
            if (attackerText == null)
            {
                if (ExPlayer.TryGet(attackerDamageHandler.Attacker.Hub, out var attacker))
                    attackerText = $"<color=yellow>{attacker.Nickname}</color>";
                else
                    attackerText = "(null)";
            }

            if (attackerDamageHandler is JailbirdDamageHandler)
                return $"odpálen <color=yellow>Jailbirdem</color> hráče {attackerText}";
            
            if (attackerDamageHandler is FirearmDamageHandler firearmDamageHandler)
                return $"zastřelen zbraní <color=yellow>{firearmDamageHandler.Firearm.ItemTypeId}</color> hráče {attackerText}";

            if (attackerDamageHandler is GrayCandyDamageHandler)
                return "zabit <color=yellow>ocelovou tyčí</color>";

            if (attackerDamageHandler is MicroHidDamageHandler)
                return $"usmažen <color=red>pomocí Micro-HID</color> hráče {attackerText}";

            if (attackerDamageHandler is RecontainmentDamageHandler)
                return $"recontained hráčem {attackerText}";

            if (attackerDamageHandler is Scp018DamageHandler)
                return $"odpálen pomocí <color=red>SCP-018</color> hráče {attackerText}";

            if (attackerDamageHandler is Scp049DamageHandler)
                return $"zabit hráčem <color=red>SCP-049</color> {attackerText}";

            if (attackerDamageHandler is Scp096DamageHandler)
                return $"zabit hráčem <color=red>SCP-096</color> {attackerText}";
            
            if (attackerDamageHandler is Scp1507DamageHandler)
                return $"zabit hráčem <color=red>SCP-1507</color> {attackerText}";
            
            if (attackerDamageHandler is Scp1509DamageHandler)
                return $"zabit hráčem <color=red>SCP-1509</color> {attackerText}";
            
            if (attackerDamageHandler is Scp3114DamageHandler)
                return $"zabit hráčem <color=red>SCP-3114</color> {attackerText}";

            if (attackerDamageHandler is Scp939DamageHandler)
                return $"zabit hráčem <color=red>SCP-939</color> {attackerText}";

            if (attackerDamageHandler is SnowballDamageHandler)
                return $"zabit <color=yellow>sněhovou koulí</color> hráče {attackerText}";
            
            if (attackerDamageHandler is DisruptorDamageHandler)
            {
                if (attackerText != null)
                    return $"<color=yellow>disintegrován</color> hráčem {attackerText}";

                return "<color=yellow>disintegrován</color>";
            }

            if (attackerDamageHandler is ExplosionDamageHandler explosionDamageHandler)
            {
                return explosionDamageHandler.ExplosionType switch
                {
                    ExplosionType.Cola => "odpálen <color=red>sklenkou SCP-207</color>",
                    ExplosionType.Disruptor => $"<color=yellow>disintegrován</color> hráčem {attackerText}",
                    ExplosionType.Grenade => $"odpálen <color=yellow>HE granátem</color> hráče {attackerText}",
                    ExplosionType.Jailbird => $"odpálen <color=red>Jailbirdem</color> hráče {attackerText}",
                    ExplosionType.SCP018 => $"odpálen <color=red>SCP-018</color> granátem hráče {attackerText}",
                    ExplosionType.PinkCandy => $"odpálen <color=red>růžovým bonbónem</color> hráče {attackerText}",
                    
                    _ => $"odpálen <color=yellow>neznámým předmětem</color> hráče {attackerText}"
                };
            }
        }

        if (damageHandler is CustomReasonDamageHandler customReasonDamageHandler)
            return customReasonDamageHandler._deathReason;

        if (damageHandler is CustomReasonFirearmDamageHandler customReasonFirearmDamageHandler)
            return customReasonFirearmDamageHandler.DeathScreenText;

        if (damageHandler is WarheadDamageHandler)
            return "zabit <color=yellow>explozí Alpha Warhead</color>";

        if (damageHandler is UniversalDamageHandler universalDamageHandler)
        {
            if (DeathTranslations.TranslationsById.TryGetValue(universalDamageHandler.TranslationId,
                    out var translation))
            {
                if (translation.Id == DeathTranslations.Asphyxiated.Id)
                    return attackerText != null ? $"<color=yellow>uškrcen</color> hráčem {attackerText}" : "<&uškrcen";

                if (translation.Id == DeathTranslations.Bleeding.Id)
                    return "zabit <color=red>vykrvácením</color>";

                if (translation.Id == DeathTranslations.BulletWounds.Id)
                    return attackerText != null
                        ? $"zabit <color=yellow>pomocí zbraně</color> hráče {attackerText}"
                        : "zabit <color=red>vykrvácením</color>";

                if (translation.Id == DeathTranslations.CardiacArrest.Id)
                    return "zabit <color=yellow>zástavou srdce</color>";

                if (translation.Id == DeathTranslations.Crushed.Id)
                    return "zabit <color=yellow>těžkou bránou</color>";

                if (translation.Id == DeathTranslations.Decontamination.Id)
                    return "zabit <color=yellow>dekontaminačními plyny</color>";

                if (translation.Id == DeathTranslations.Explosion.Id)
                    return "zabit <color=yellow>explozí/color>";

                if (translation.Id == DeathTranslations.Falldown.Id)
                    return "zabit <color=yellow>pádem z výšky</color>";

                if (translation.Id == DeathTranslations.FriendlyFireDetector.Id)
                    return "zabit z důvodu vysokého počtu friendly fire hitů";

                if (translation.Id == DeathTranslations.Hypothermia.Id)
                    return "zabit <color=blue>umrznutím</color>";

                if (translation.Id == DeathTranslations.PocketDecay.Id)
                    return "zabit <color=red>Pocket Dimenzí</color>";

                if (translation.Id == DeathTranslations.Poisoned.Id)
                    return "zabit <color=yellow>jedem</color>";

                if (translation.Id == DeathTranslations.SeveredHands.Id)
                    return "zabit <color=yellow>vykrvácením po ztrátě rukou</color>";

                if (translation.Id == DeathTranslations.Tesla.Id)
                    return "zabit <color=red>elektrickým šokem Tesla brány</color>";
                
                if (translation.Id == DeathTranslations.UsedAs106Bait.Id)
                    return "zabit <color=red>jako návnada pro SCP-106</color>";

                if (translation.Id == DeathTranslations.Zombie.Id)
                    return "zabit <color=red>instancí SCP-049-2</color>";
            }
        }
        
        return "zabit <color=red>neznámým způsobem</color>";
    }
}