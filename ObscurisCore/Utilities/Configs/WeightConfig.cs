using System.ComponentModel;

using LabExtended.API;

using ObscurisCore.Extensions;

namespace ObscurisCore.Utilities.Configs;

/// <summary>
/// Represents a configuration used to calculate weights for players, incorporating base weights
/// and additional multipliers specific to individual users.
/// </summary>
public class WeightConfig
{
    /// <summary>
    /// Gets or sets a collection of user-specific weight multipliers.
    /// </summary>
    [Description("Configures a list of user-specific weight multipliers.")]
    public Dictionary<string, float> Multipliers { get; set; } = new();

    /// <summary>
    /// Gets or sets the base weight value used for calculations.
    /// </summary>
    [Description("Configures the base weight.")]
    public float BaseWeight { get; set; } = 0f;

    /// <summary>
    /// Calculates the effective weight for a given player based on configurable multipliers
    /// and the base weight defined within the configuration.
    /// </summary>
    /// <param name="player">The player for whom the weight is being calculated. Cannot be null.</param>
    /// <returns>The calculated weight after applying the multipliers and base weight. Returns the base weight
    /// if the player or multipliers are not valid.</returns>
    public float GetWeight(ExPlayer player)
        => player.GetFloatWeight(Multipliers, BaseWeight);
}