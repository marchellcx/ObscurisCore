using LabExtended.API;

using ObscurisCore.Features.Audio.Playback;

using UnityEngine;

namespace ObscurisCore.Features.Dealer.Audio;

/// <summary>
/// Manages audio of a spawned dealer instance.
/// </summary>
public class DealerAudio
{
    private ExPlayer? lastClosePlayer;

    private Dictionary<DealerClip, float> lastClipPlayTimes = new();

    private bool isPlaying;

    /// <summary>
    /// Gets the dealer instance this audio manager belongs to.
    /// </summary>
    public DealerInstance Dealer { get; }

    public DealerAudio(DealerInstance dealer)
    {
        if (dealer is null || dealer.IsDestroyed)
            throw new ArgumentNullException(nameof(dealer));

        Dealer = dealer;
    }

    /// <summary>
    /// Gets called when the parent dealer instance is being initialized.
    /// </summary>
    public void Initialize()
    {

    }

    /// <summary>
    /// Gets called when the parent dealer instance is being destroyed.
    /// </summary>
    public void Destroy()
    {
        lastClipPlayTimes.Clear();
    }

    /// <summary>
    /// Gets a random clip file name for a specific clip type.
    /// </summary>
    /// <param name="type">The clip type.</param>
    /// <returns>The name of the clip's audio file (or empty if no clips were loaded).</returns>
    public string GetRandomClip(DealerClip type)
    {
        if (!DealerManager.ClipNames.TryGetValue(type, out var names) || names.Count == 0)
            return string.Empty;

        return names.RandomItem();
    }

    /// <summary>
    /// Plays a random clip of the specified type.
    /// </summary>
    public void PlayRandomClip(DealerClip type)
    {
        if (isPlaying)
            return;

        if (DealerManager.ClipCooldown.TryGetValue(type, out var clipCooldown)
            && lastClipPlayTimes.TryGetValue(type, out var clipTime)
            && (Time.realtimeSinceStartup - clipTime) < clipCooldown)
            return;

        var clipPath = GetRandomClip(type);

        if (string.IsNullOrWhiteSpace(clipPath))
            return;

        isPlaying = PlaybackUtils.PlayAt(clipPath, Dealer.Player.Position, null, false, () =>
        {
            lastClipPlayTimes[type] = Time.realtimeSinceStartup;

            isPlaying = false;
        }).HasValue;
    }

    /// <summary>
    /// Gets called when a player gets detected as the closest player to the dealer.
    /// </summary>
    public void OnClosestPlayerDetected(ExPlayer closestPlayer, float distance)
    {
        if (lastClosePlayer != null && lastClosePlayer == closestPlayer)
            return;

        if (distance > DealerManager.MaxAudioDistance)
            return;

        lastClosePlayer = closestPlayer;

        PlayRandomClip(DealerClip.PlayerClose);
    }

    /// <summary>
    /// Gets called before an item is purchased.
    /// </summary>
    public void OnPurchasingItem(bool canAfford)
    {
        if (!canAfford)
        {
            PlayRandomClip(DealerClip.PurchaseFailed);
        }
    }

    /// <summary>
    /// Gets called after an item purchase is finished.
    /// </summary>
    public void OnPurchasedItem()
    {
        PlayRandomClip(DealerClip.PurchaseSuccessful);
    }

    /// <summary>
    /// Gets called before the player dies.
    /// </summary>
    public void OnPlayerDied()
    {
        PlayRandomClip(DealerClip.PlayerDied);
    }

    /// <summary>
    /// Gets called when a trade start fails due to an empty inventory.
    /// </summary>
    public void OnTradeFailedEmptyInventory()
    {
        PlayRandomClip(DealerClip.EmptyInventory);
    }

    /// <summary>
    /// Gets called after a trade is started.
    /// </summary>
    public void OnTradeStarted()
    {
        PlayRandomClip(DealerClip.TradeStart);
    }

    /// <summary>
    /// Gets called after a trade is finished.
    /// </summary>
    public void OnTradeFinished(bool anyPurchased)
    {
        PlayRandomClip(anyPurchased
            ? DealerClip.TradeEndedWithPurchase
            : DealerClip.TradeEndedNoPurchase);
    }
}