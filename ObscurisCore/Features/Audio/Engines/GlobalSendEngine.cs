using LabApi.Features.Wrappers;

using LabExtended.API;

using Mirror;
using SecretLabNAudio.Core.SendEngines;

using VoiceChat.Networking;

using SpeakerToy = AdminToys.SpeakerToy;

namespace ObscurisCore.Features.Audio.Engines
{
    /// <summary>
    /// Represents a send engine that uses a single speaker toy and fakes it's position for each player.
    /// </summary>
    public class GlobalSendEngine : SendEngine, IDisposable
    {
        /// <summary>
        /// Gets the RPC hash for the RpcChangeParent method of the SpeakerToy.
        /// </summary>
        public static int RpcHash { get; } = MirrorMethods.TryGetRpcHash(typeof(SpeakerToy), nameof(SpeakerToy.RpcChangeParent), out var hash)
            ? hash
            : 0;

        /// <summary>
        /// Gets the list of players that have received the fake parent RPC.
        /// </summary>
        public HashSet<string> SyncedPlayers { get; } = new();

        /// <summary>
        /// Gets the target speaker toy.
        /// </summary>
        public SpeakerToy Speaker { get; }

        /// <summary>
        /// Initializes a new instance of the GlobalSendEngine class using the specified speaker.
        /// </summary>
        /// <param name="speaker">The SpeakerToy instance to be used for sending audio data. Cannot be null.</param>
        public GlobalSendEngine(SpeakerToy speaker)
            => Speaker = speaker;

        /// <inheritdoc/>
        protected override bool Broadcast(Player player, AudioMessage message)
        {
            if (SyncedPlayers.Add(player.UserId))
                player.ReferenceHub.connectionToClient.Send(Speaker.GetRpcMessage(RpcHash, writer => writer.WriteUInt(player.NetworkId)));

            return base.Broadcast(player, message);
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            foreach (var player in ExPlayer.Players)
            {
                if (player?.ReferenceHub == null)
                    continue;

                if (SyncedPlayers.Remove(player.UserId))
                    player.ReferenceHub.connectionToClient.Send(Speaker.GetRpcMessage(RpcHash, writer => writer.WriteUInt(0)));
            }

            SyncedPlayers.Clear();
        }
    }
}