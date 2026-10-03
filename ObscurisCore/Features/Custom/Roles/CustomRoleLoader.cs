using LabExtended.API;
using LabExtended.API.Custom.Roles;

using LabExtended.Events;
using LabExtended.Utilities;
using LabExtended.Attributes;
using LabExtended.Extensions;

using MEC;

using NorthwoodLib.Pools;

namespace ObscurisCore.Features.Custom.Roles;

/// <summary>
/// Provides functionality for registering and managing custom role spawning logic based on specified conditions and
/// optional player predicates.
/// </summary>
public static class CustomRoleLoader
{
    private static void OnRoundStarted()
    {
        Timing.CallDelayed(0.5f, () =>
        {
            var players = ListPool<ExPlayer>.Shared.Rent(ExPlayer.Players);

            if (players.Count > 0)
            {
                foreach (var kvp in CustomRole.RegisteredObjects)
                {
                    if (kvp.Value is not SpawnableCustomRole spawnableCustomRole)
                        continue;

                    spawnableCustomRole.SpawnRoleOnRoundStart(players);
                }
            }
            
            ListPool<ExPlayer>.Shared.Return(players);
        });
    }

    private static void Initialize()
    {
        ExRoundEvents.Started += OnRoundStarted;
        
        var dir = FileUtils.CreatePath(ObscurisPlugin.RootDirectory, "roles");
        
        foreach (var type in typeof(CustomRoleLoader).Assembly.GetTypes())
        {
            if (type.IsAbstract || type.IsInterface)
                continue;
            
            if (string.IsNullOrEmpty(type.Namespace) || !type.Namespace.StartsWith("ObscurisCore.Features.Roles"))
                continue;
            
            if (type.HasAttribute<LoaderIgnoreAttribute>())
                continue;
            
            if (!typeof(CustomRole).IsAssignableFrom(type))
                continue;

            var name = type.Name;
            var path = Path.Combine(dir, name + ".yml");

            if (FileUtils.TryLoadYamlFile<CustomRole>(path, type, out var role))
            {
                role.Register();
            }
            else
            {
                if (Activator.CreateInstance(type) is CustomRole customRole)
                {
                    customRole.Register();

                    FileUtils.TrySaveYamlFile(path, customRole);
                }
            }
        }
    }
}