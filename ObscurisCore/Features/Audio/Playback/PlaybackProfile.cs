using LabExtended.API;
using LabExtended.API.Custom.Voice.Profiles;

using LabExtended.Core;

using VoiceChat;
using VoiceChat.Networking;

using SecretLabNAudio.Core;
using SecretLabNAudio.Core.Pools;

namespace ObscurisCore.Features.Audio.Playback;

/// <summary>
/// Represents a voice profile with the ability to resend voice messages through speakers.
/// </summary>
public abstract class PlaybackProfile : VoiceProfile
{
    /// <summary>
    /// Gets the player's audio player.
    /// </summary>
    public AudioPlayer? Audio { get; private set; }

    /// <summary>
    /// Gets the speaker settings for the audio player.
    /// </summary>
    public virtual SpeakerSettings SpeakerSettings { get; } = SpeakerSettings.Default;

    /// <inheritdoc/>
    public override void Enable()
    {
        base.Enable();

        Audio = AudioPlayerPool.Rent(SpeakerSettings, Player.CameraTransform);
    }

    /// <inheritdoc/>
    public override void Disable()
    {
        base.Disable();

        if (Audio != null)
        {
            AudioPlayerPool.Return(Audio);

            Audio = null!;
        }
    }

    /// <summary>
    /// Resends the specified voice message to all players, optionally allowing customization of the target voice
    /// channel and inclusion of the original speaker.
    /// </summary>
    /// <remarks>The method iterates over all players and sends the provided voice message to each,
    /// using the specified or original channel. The message's channel is temporarily changed for each recipient if
    /// a channel selector is provided, and restored after sending. The Speaker property of the message is set to
    /// the current player if it is not already set. This method does not return a value and does not indicate which
    /// players received the message.</remarks>
    /// <param name="message">A reference to the voice message to resend. The message's Speaker will be set to the current player if not
    /// already set.</param>
    /// <param name="sendToSpeaker">true to also send the message to the original speaker; otherwise, false.</param>
    /// <param name="channelSelector">An optional function that selects the voice chat channel for each recipient. If null, the message is sent
    /// using its original channel.</param>
    public void ResendViaVoice(ref VoiceMessage message, bool sendToSpeaker = false,
        Func<ExPlayer, VoiceChatChannel>? channelSelector = null)
    {
        if (message.Speaker == null || message.Speaker != Player.ReferenceHub)
            message.Speaker = Player.ReferenceHub;

        for (var x = 0; x < ExPlayer.Players.Count; x++)
        {
            var player = ExPlayer.Players[x];

            if (player?.ReferenceHub == null)
                continue;

            if (!sendToSpeaker && player.NetworkId == Player.NetworkId)
                continue;

            var originalChannel = message.Channel;
            
            var playerChannel = channelSelector != null
                ? channelSelector(player)
                : message.Channel;

            if (playerChannel != originalChannel)
                message.Channel = playerChannel;

            player.Send(message);

            message.Channel = originalChannel;
        }
    }

    /// <summary>
    /// Resends the specified voice message to all players' speakers who meet the given criteria.
    /// </summary>
    /// <remarks>Use this method to broadcast a voice message to multiple players' speakers,
    /// optionally filtering recipients with a predicate. The method does not guarantee delivery to any specific
    /// player if they do not meet the criteria.</remarks>
    /// <param name="message">The voice message to be resent. The message data may be modified during transmission.</param>
    /// <param name="sendToSpeaker">true to also send the message to the current player's own speaker; otherwise, false. The default is false.</param>
    /// <param name="receivePredicate">An optional predicate used to determine which players should receive the message. If null, all eligible
    /// players receive the message.</param>
    /// <exception cref="Exception">Thrown if there are no valid speakers available for message delivery.</exception>
    public void ResendViaSpeakers(ref VoiceMessage message, bool sendToSpeaker = false,
        Predicate<ExPlayer>? receivePredicate = null)
    {
        if (Audio?.Speaker?.Base == null)
        {
            ApiLog.Error("PlaybackProfile", $"Profile does not have any valid speakers to resend audio through!");
            return;
        }

        for (var x = 0; x < ExPlayer.Players.Count; x++)
        {
            var player = ExPlayer.Players[x];

            if (player?.ReferenceHub == null)
                continue;

            if (!sendToSpeaker && player.NetworkId == Player.NetworkId)
                continue;

            if (receivePredicate != null && !receivePredicate(player))
                continue;

            player.Send(new AudioMessage(Audio.Speaker.ControllerId, message.Data, message.DataLength));
        }
    }
}