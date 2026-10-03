using LabApi.Features.Extensions;
using LabApi.Features.Wrappers;

using LabExtended.API;
using LabExtended.API.Custom.Voice.Profiles;

using LabExtended.Core;

using NiveraAPI.IO.Configs;

using ObscurisCore.Features.Custom.Abilities.ScpVoice.Proximity;

using PlayerRoles;

using SecretLabNAudio.Core;
using SecretLabNAudio.Core.Pools;
using SecretLabNAudio.Core.SendEngines;

using VoiceChat;
using VoiceChat.Networking;

namespace ObscurisCore.Features.Custom.Abilities.ScpVoice;

public class ScpVoiceProfile : VoiceProfile
{
    /// <summary>
    /// New speaker settings.
    /// </summary>
    public static SpeakerSettings NewSpeakerSettings =>
        new()
        {

            IsSpatial = SpatialAudio,
                
            Volume = SpeakerVolume,
                
            MinDistance = SpeakerMinDistance,
            MaxDistance = SpeakerMaxDistance
        };

    /// <summary>
    /// Volume of the proximity SCP voice.
    /// </summary>
    [Config("scp-voice", "volume", "Volume multiplier of the proximity SCP voice packets.")]
    public static float Volume { get; set; } = 4f;

    /// <summary>
    /// Volume of the proximity speaker.   
    /// </summary>
    [Config("scp-voice", "speaker-volume", "Volume of the proximity speaker.")]
    public static float SpeakerVolume { get; set; } = 1f;

    /// <summary>
    /// Minimum and maximum distances for the proximity speaker.  
    /// </summary>
    [Config("scp-voice", "min-speaker-distance", "Minimum and maximum distances for the proximity speaker.")]
    public static float SpeakerMinDistance { get; set; } = 5f;
    
    /// <summary>
    /// Minimum and maximum distances for the proximity speaker. 
    /// </summary>
    [Config("scp-voice", "max-speaker-distance", "Minimum and maximum distances for the proximity speaker.")]
    public static float SpeakerMaxDistance { get; set; } = 20f;
    
    /// <summary>
    /// Whether to use spatial audio for the proximity SCP voice. 
    /// </summary>
    [Config("scp-voice", "spatial-audio", "Whether to use spatial audio for the proximity SCP voice.")]
    public static bool SpatialAudio { get; set; } = true;
    
    private volatile Action<ProximityPacket> onProcesed;
    private volatile Func<ProximityPacket> packetFactory;

    /// <summary>
    /// Constructor for the ScpVoiceProfile class.
    /// </summary>
    public ScpVoiceProfile()
    {
        onProcesed = OnProcessed;
        packetFactory = PacketFactory;
    }
    
    /// <summary>
    /// The audio player used for the proximity SCP voice.
    /// </summary>
    public AudioPlayer Audio { get; private set; }
    
    /// <summary>
    /// The SCP Voice ability instance.
    /// </summary>
    public ScpVoiceAbility Ability { get; internal set; }

    /// <summary>
    /// Indicates whether the voice messages should be sent to SCP players based on the current SCP voice ability mode.
    /// </summary>
    public bool SendToScp => Ability != null && Ability.Mode is ScpVoiceStatus.Scp or ScpVoiceStatus.Mixed;

    /// <summary>
    /// Indicates whether the voice messages should be sent to players in proximity based on the current SCP voice ability mode.
    /// </summary>
    public bool SendToProximity => Ability != null && Ability.Mode is ScpVoiceStatus.Proximity or ScpVoiceStatus.Mixed;

    /// <summary>
    /// Starts the proximity SCP voice profile.
    /// </summary>
    public override void Start()
    {
        base.Start();

        Audio = AudioPlayerPool.Rent(NewSpeakerSettings, Player.Transform);
        Audio.SendEngine = new FilteredSendEngine(SendMessageFilter);
    }

    /// <summary>
    /// Stops the proximity SCP voice profile.
    /// </summary>
    public override void Stop()
    {
        base.Stop();

        if (Audio != null)
        {
            AudioPlayerPool.Return(Audio);
            
            Audio = null!;
        }
    }

    /// <summary>
    /// Processes an incoming voice message and determines how the message should be handled based on the current SCP voice ability mode and game state.
    /// </summary>
    /// <param name="message">The voice message to be processed, passed by reference.</param>
    /// <returns>A value indicating the result of processing the voice message, which determines whether the message should be skipped, processed, or ignored.</returns>
    public override VoiceProfileResult ReceiveFrom(ref VoiceMessage message)
    {
        if (Ability == null)
        {
            ApiLog.Warn($"Received voice message from {Player.ToLogString()} but ScpVoiceAbility is null!");
            return VoiceProfileResult.None;
        }
        
        if (message.Channel is VoiceChatChannel.Mimicry 
            || Round.IsRoundEnded)
            return VoiceProfileResult.None;

        if (SendToProximity)
            Player.Voice.Thread.ProcessCustom(message.Data, message.DataLength, ProximityProcessor.Instance, onProcesed, packetFactory);
        
        return SendToScp
            ? VoiceProfileResult.None 
            : VoiceProfileResult.SkipAndDontSend;
    }

    /// <summary>
    /// Processes and sends the voice message to the specified player, adjusting the behavior based on the SCP voice ability settings and game state.
    /// </summary>
    /// <param name="message">The voice message to be sent, passed by reference.</param>
    /// <param name="player">The recipient player to whom the voice message is sent.</param>
    /// <returns>A value indicating the result of handling the voice message, which determines whether the message should be processed, skipped, or ignored.</returns>
    public override VoiceProfileResult SendTo(ref VoiceMessage message, ExPlayer player)
        => VoiceProfileResult.None;

    /// <summary>
    /// Determines whether the SCP voice profile should be enabled based on the player's role change to a new role type.
    /// </summary>
    /// <param name="newRoleType">The new role type to which the player's role has changed.</param>
    /// <returns>True if the SCP voice profile should be enabled for the new role type; otherwise, false.</returns>
    public override bool EnabledOnRoleChange(RoleTypeId newRoleType)
    {
        if (Ability != null)
        {
            Ability.Mode = ScpVoiceStatus.Scp;
            return newRoleType.IsScp();
        }

        return false;
    }

    private bool SendMessageFilter(Player ply)
    {
        if (ply is not ExPlayer player)
            return false;

        if (player.ReferenceHub == null)
            return false;

        if (player == Player && !player.Toggles.CanHearSelf)
            return false;

        if (Ability == null)
            return false;

        return !player.IsSCP || !SendToScp;
    }

    private void OnProcessed(ProximityPacket packet)
    {
        if (Audio != null && Audio.SendEngine != null)
            Audio.SendEngine.Broadcast(new AudioMessage(Audio.Id, packet.Data, packet.Length));
    }

    private ProximityPacket PacketFactory()
    {
        var packet = new ProximityPacket()
        {
            Volume = Volume
        };

        return packet;
    }
}