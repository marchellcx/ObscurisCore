using System.Text;
using LabExtended.API;
using LabExtended.Commands;
using LabExtended.Commands.Attributes;
using LabExtended.Commands.Interfaces;
using PRTS.Client.Profiles;
using PRTS.Client.Profiles.Objects;

namespace ObscurisCore.Features.Events.Halloween.Lanterns.Commands;

/// <summary>
/// Provides command implementations related to the Halloween lantern event.
/// </summary>
[Command("lanterns", "Příkazy k Halloween eventu 2026.")]
public class LanternCommands : CommandBase, IServerSideCommand
{
    [CommandOverload("toppoints", "Zobrazí tabulku TOP hráčů podle počtu získaných bodů.")]
    private void TopPoints(
        [CommandParameter("Počet", "Počet hráčů v TOP seznamu.")] int playerCount)
    {
        var sender = Sender;
        
        void Callback(Dictionary<ProfileInfo, int>? dict)
        {
            if (dict == null)
            {
                sender.SendRemoteAdminMessage("Seznam top hráčů je prázdný.", success: false, tag: "PRTS");
            }
            else
            {
                var sb = new StringBuilder();
                var index = 0;

                foreach (var kvp in dict)
                    sb.AppendLine($"{index + 1} -> [&3{kvp.Key.UserId}&r] {kvp.Key.GetNickname()}: &6{kvp.Value}&r");
                
                sender.SendRemoteAdminMessage(sb.ToString(), success: true, tag: "PRTS");
            }
        }

        if (!LanternEvent.GetTopPlayers(playerCount, Callback))
        {
            Fail("Nepodařilo se odeslat požadavek serveru.");
        }
        else
        {
            Ok("Požadavek serveru odeslán.");
        }
    }
    
    [CommandOverload("getpoints", "Získá počet bodů hráče.", null)]
    private void GetPoints(
        [CommandParameter("Hráč", "Hráč jemuž chcete resetovat body.")] 
        [CommandParameter(ParserType = typeof(ExPlayer), ParserProperty = "UserId")]
        string userId)
    {
        if (ProfileModule.Singleton == null)
        {
            Fail("PRTS není připojeno k serveru.");
            return;
        }

        var sender = Sender;
        
        ProfileModule.Singleton.CallCmdGetCustomData(userId, LanternEvent.PointsDataName, reply =>
        {
            if (!reply.ProfileFound)
            {
                sender.SendRemoteAdminMessage($"Nebyl nalezen profil hráče &3{userId}&r", success: false, tag: "PRTS");
                return;
            }
            
            if (!reply.KeyFound || !int.TryParse(reply.DataValue, out var points))
            {
                sender.SendRemoteAdminMessage($"Nebyly nalezeny body hráče &3{userId}&r", success: false, tag: "PRTS");
                return;
            }
            
            sender.SendRemoteAdminMessage($"Hráč &3{userId}&r má &3{points}&r bodů.", tag: "PRTS");
        });
        
        Ok("Požadavek odeslán serveru.");
    }
    
    [CommandOverload("addpoints", "Přidá hráči body.", null)]
    private void AddPoints(
        [CommandParameter("Hráč", "Hráč jemuž chcete resetovat body.")] 
        [CommandParameter(ParserType = typeof(ExPlayer), ParserProperty = "UserId")]
        string userId,
        [CommandParameter("Body", "Počet bodů, které chcete přidat hráči.")] int points)
    {
        if (ProfileModule.Singleton == null)
        {
            Fail("PRTS není připojeno k serveru.");
            return;
        }
        
        var sender = Sender;
        
        ProfileModule.Singleton.CallCmdGetCustomData(userId, LanternEvent.PointsDataName, reply =>
        {
            if (!reply.ProfileFound)
            {
                sender.SendRemoteAdminMessage($"Nebyl nalezen profil hráče &3{userId}&r", success: false, tag: "PRTS");
                return;
            }

            if (reply.KeyFound && int.TryParse(reply.DataValue, out var curPoints))
            {
                sender.SendRemoteAdminMessage($"Odesílám požadavek na přidání bodů hráči &3{userId}&r", success: true, tag: "PRTS");
                
                ProfileModule.Singleton.CallCmdSetCustomData(userId, LanternEvent.PointsDataName, (curPoints + points).ToString(), result =>
                {
                    if (result)
                    {
                        sender.SendRemoteAdminMessage($"Body hráče &3{userId}&r úspěšně přidány, nový počet: &6{curPoints + points}&r.", tag: "PRTS");
                    }
                    else
                    {
                        sender.SendRemoteAdminMessage($"Nepodařilo se přidat body hráče &3{userId}&r.", success: false, tag: "PRTS");
                    }
                });
            }
            else
            {
                sender.SendRemoteAdminMessage($"Odesílám požadavek na nastavení bodů hráči &3{userId}&r", success: true, tag: "PRTS");
                
                ProfileModule.Singleton.CallCmdSetCustomData(userId, LanternEvent.PointsDataName, points.ToString(), result =>
                {
                    if (result)
                    {
                        sender.SendRemoteAdminMessage($"Body hráče &3{userId}&r úspěšně nastaveny na &6{points}&r.", tag: "PRTS");
                    }
                    else
                    {
                        sender.SendRemoteAdminMessage($"Nepodařilo se nastavit body hráče &3{userId}&r.", success: false, tag: "PRTS");
                    }
                });
            }
        });
        
        Ok("Odesílám požadavek serveru ..");
    }
    
    [CommandOverload("resetpoints", "Resetuje nasbírané body hráče.", null)]
    private void ResetPoints(
        [CommandParameter("Hráč", "Hráč jemuž chcete resetovat body.")] 
        [CommandParameter(ParserType = typeof(ExPlayer), ParserProperty = "UserId")]
        string userId)
    {
        if (ProfileModule.Singleton == null)
        {
            Fail("PRTS není připojeno k serveru.");
            return;
        }

        var sender = Sender;
        
        ProfileModule.Singleton.CallCmdSetCustomData(userId, LanternEvent.PointsDataName, "0", result =>
        {
            if (result)
            {
                sender.SendRemoteAdminMessage($"Body hráče &3{userId}&r úspěšně resetovány.", tag: "PRTS");
            }
            else
            {
                sender.SendRemoteAdminMessage($"Nepodařilo se resetovat body hráče &3{userId}&r.", success: false, tag: "PRTS");
            }
        });
        
        Ok($"Odeslán požadavek na reset XP hráče &3{userId}&r ..");
    }
}