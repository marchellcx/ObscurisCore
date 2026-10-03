using System.ComponentModel;

using LabExtended.API;

namespace ObscurisCore.Features.Coin;

/// <summary>
/// Represents an action that can be invoked by flipping a coin.
/// </summary>
public class CoinAction
{
    /// <summary>
    /// Gets or sets the weight of the action, representing its relative importance or influence
    /// within the context of the coin action system.
    /// </summary>
    [Description("Sets the weight of the action.")]
    public float Weight { get; set; } = 0f;

    /// <summary>
    /// Gets or sets the multipliers associated with the action, where each key corresponds to
    /// a specific aspect or condition and the value represents its respective multiplier factor.
    /// </summary>
    [Description("Sets the multipliers of the action.")]
    public Dictionary<string, float> Multipliers { get; set; } = new();
    
    /// <summary>
    /// Gets a value indicating whether the action can be combined with other actions.
    /// </summary>
    public virtual bool CanBeCombined => true;

    /// <summary>
    /// Determines whether the action should be forced for the specified player.
    /// </summary>
    /// <param name="player">The player for whom the force condition is being evaluated.</param>
    /// <returns>
    /// A boolean value indicating whether the action should be forcibly executed for the specified player.
    /// </returns>
    public virtual bool ShouldForce(ExPlayer player) => false;

    /// <summary>
    /// Determines whether the coin action is available to the specified player.
    /// </summary>
    /// <param name="player">The player for whom the availability of the action is being checked.</param>
    /// <returns>
    /// A boolean value indicating whether the coin action is available for the specified player.
    /// </returns>
    public virtual bool IsAvailable(ExPlayer player) => true;

    /// <summary>
    /// Executes the coin action for the specified player.
    /// </summary>
    /// <param name="player">The player for whom the coin action is being executed.</param>
    public virtual void Execute(ExPlayer player)
    {
        
    }

    /// <summary>
    /// Enables the coin action, making it available for use.
    /// </summary>
    public virtual void Enable()
    {
        
    }
}