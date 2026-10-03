using LabExtended.API;

namespace ObscurisCore.Features.Looting.Configs;

/// <summary>
/// Represents a function that can be used to loot a player.
/// </summary>
public delegate void LootFunction(ExPlayer target, string[] arguments);