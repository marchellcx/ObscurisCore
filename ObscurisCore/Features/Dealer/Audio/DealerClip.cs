namespace ObscurisCore.Features.Dealer.Audio;

/// <summary>
/// Describes the type of audio clip to play.
/// </summary>
public enum DealerClip
{
    /// <summary>
    /// A new trade has started.
    /// </summary>
    TradeStart,

    /// <summary>
    /// A trade has ended without any purchased items.
    /// </summary>
    TradeEndedNoPurchase,

    /// <summary>
    /// A trade has ended with a purchased item.
    /// </summary>
    TradeEndedWithPurchase,

    /// <summary>
    /// The trading player has died.
    /// </summary>
    PlayerDied,

    /// <summary>
    /// A player is close to the dealer.
    /// </summary>
    PlayerClose,

    /// <summary>
    /// The trader's inventory is empty.
    /// </summary>
    EmptyInventory,

    /// <summary>
    /// A purchase was attempted but the player could not afford it.
    /// </summary>
    PurchaseFailed,

    /// <summary>
    /// A purchase was succesfull.
    /// </summary>
    PurchaseSuccessful,
}
