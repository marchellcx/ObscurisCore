using LabExtended.Utilities;

using NAudio.Wave;

using SecretLabNAudio.Core;
using SecretLabNAudio.Core.Pools;
using SecretLabNAudio.Core.Extensions;
using SecretLabNAudio.Core.SendEngines;

using UnityEngine;

using LabApi.Features.Wrappers;
using ObscurisCore.Features.Audio.Engines;
using ObscurisCore.Features.Audio.Playback;

namespace ObscurisCore.Features.Audio.Clips
{
    /// <summary>
    /// Manages the playback of audio or video clips, providing functionality to play, retrieve, and manage clip
    /// definitions and playback states.
    /// </summary>
    /// <remarks>The <see cref="ClipManager{T}"/> class is designed to handle playback operations for clips,
    /// including managing playback state, enforcing cooldowns, and retrieving clip definitions. It supports playing
    /// specific clips or selecting random clips based on predefined configurations.</remarks>
    /// <typeparam name="T">The type used to identify clips, such as a string or an enumeration.</typeparam>
    public class ClipManager<T>
    {
        private WaveStream? stream;
        private IWaveProvider? provider;

        /// <summary>
        /// Gets called when the playback of an audio clip ends.
        /// </summary>
        public event Action? ClipEnded;

        /// <summary>
        /// Gets called when the playback of an audio clip starts.
        /// </summary>
        public event Action? ClipStarted;

        /// <summary>
        /// Gets the current playback handle, which represents the active playback session, if any.
        /// </summary>
        public AudioPlayer Player { get; private set; }

        /// <summary>
        /// Gets the clip currently being played.
        /// </summary>
        public ClipDefinition? CurrentClip { get; private set; }

        /// <summary>
        /// Gets or sets the configuration settings for the clip operation.
        /// </summary>
        public ClipConfig<T> Config { get; set; }

        /// <summary>
        /// Gets a dictionary that maps keys of type <typeparamref name="T"/> to their associated clip times.
        /// </summary>
        public Dictionary<T, float> ClipTimes { get; } = new();

        /// <summary>
        /// Sets the volume level for the current audio playback.
        /// </summary>
        public float Volume
        {
            get => field;
            set
            {
                if (Player != null)
                {
                    Player.WithVolume(value);

                    field = value;
                }
            }
        }

        /// <summary>
        /// Gets a value indicating whether or not any clip is playing.
        /// </summary>
        public bool IsPlaying => stream != null;

        /// <summary>
        /// Gets or sets a value indicating whether the current operation is paused.
        /// </summary>
        /// <remarks>Setting this property to <see langword="true"/> pauses the current operation, if one
        /// is active.  Setting it to <see langword="false"/> resumes the operation. If no operation is active, the
        /// property  will return <see langword="false"/>.</remarks>
        public bool IsPaused
        {
            get => Player.IsPaused;
            set => Player.IsPaused = value;
        }

        /// <summary>
        /// Gets or sets the send engine used for audio playback.
        /// </summary>
        public SendEngine? SendEngine
        {
            get => Player.SendEngine;
            set => Player.SendEngine = value;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ClipManager{T}"/> class with the specified configuration.
        /// </summary>
        /// <param name="config">The configuration settings for the clip manager. Cannot be <see langword="null"/>.</param>
        /// <param name="parent">The parent transform for the audio player. Cannot be <see langword="null"/>.</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="config"/> is <see langword="null"/>.</exception>
        public ClipManager(ClipConfig<T> config, Transform parent)
        {
            if (config is null)
                throw new ArgumentNullException(nameof(config));

            if (parent is null)
                throw new ArgumentNullException(nameof(parent));

            Config = config;

            Player = AudioPlayer.Create(AudioPlayerPool.NextAvailableId, SpeakerSettings.Default, parent);
            Player.NoSamplesRead += OnEnd;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ClipManager{T}"/> class with the specified configuration.
        /// </summary>
        /// <param name="config">The configuration settings for the clip manager. Cannot be <see langword="null"/>.</param>
        /// <param name="position">The position in the 3D space where the audio player should be created.</param>       
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="config"/> is <see langword="null"/>.</exception>
        public ClipManager(ClipConfig<T> config, Vector3 position)
        {
            if (config is null)
                throw new ArgumentNullException(nameof(config));

            Config = config;

            Player = AudioPlayer.Create(AudioPlayerPool.NextAvailableId, SpeakerSettings.Default, null, position);
            Player.NoSamplesRead += OnEnd;
        }

        /// <summary>
        /// Attempts to retrieve a <see cref="ClipDefinition"/> associated with the specified clip.
        /// </summary>
        /// <remarks>If multiple configurations are associated with the specified clip, one is selected at
        /// random based on its weight.</remarks>
        /// <param name="clip">The clip for which to retrieve the associated configuration.</param>
        /// <param name="config">When this method returns, contains the <see cref="ClipDefinition"/> associated with the specified clip, if
        /// the operation succeeds; otherwise, <see langword="null"/>.</param>
        /// <returns><see langword="true"/> if a <see cref="ClipDefinition"/> is successfully retrieved; otherwise, <see
        /// langword="false"/>.</returns>
        public bool TryGetClip(T clip, out ClipDefinition config)
        {
            config = null!;

            if (!Config.Clips.TryGetValue(clip, out var list))
                return false;

            if (list.Count == 0)
                return false;

            if (list.Count == 1)
            {
                config = list[0];
                return true;
            }

            config = list.GetRandomWeighted(x => x.Weight);
            return config != null;
        }

        /// <summary>
        /// Plays the specified clip at the given position using the provided clip definition.
        /// </summary>
        /// <remarks>If the clip starts playing, the method updates the current playback state, including
        /// the active clip and its handle. When playback completes, the state is reset, and the playback time for the
        /// clip is recorded.</remarks>
        /// <param name="clip">The clip to be played. This is used to track playback state and timing.</param>
        /// <param name="definition">The definition of the clip, including its name and playback settings. Cannot be <see langword="null"/>.</param>
        /// <returns><see langword="true"/> if the clip starts playing successfully; otherwise, <see langword="false"/> if a clip
        /// is already playing.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="definition"/> is <see langword="null"/>.</exception>
        public bool PlayClip(T clip, ClipDefinition definition)
        {
            if (definition is null)
                throw new ArgumentNullException(nameof(definition));

            if (Player != null)
            {
                Stop();

                if (!PlaybackUtils.TryLoadClip(definition.Name, definition.Loop, out stream, out provider))
                    return false;

                Player.WithMasterAmplification(definition.Amplification);
                Player.WithProvider(provider);

                CurrentClip = definition;

                ClipTimes[clip] = Time.realtimeSinceStartup;
                ClipStarted?.Invoke();

                return true;
            }

            return false;
        }
        
        /// <summary>
        /// Plays the specified clip at the given position using the provided clip definition.
        /// </summary>
        /// <remarks>If the clip starts playing, the method updates the current playback state, including
        /// the active clip and its handle. When playback completes, the state is reset, and the playback time for the
        /// clip is recorded.</remarks>
        /// <param name="clip">The clip to be played. This is used to track playback state and timing.</param>
        /// <param name="definition">The definition of the clip, including its name and playback settings. Cannot be <see langword="null"/>.</param>
        /// <param name="position">The position in the 3D space where the clip should be played.</param>      
        /// <returns><see langword="true"/> if the clip starts playing successfully; otherwise, <see langword="false"/> if a clip
        /// is already playing.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="definition"/> is <see langword="null"/>.</exception>
        public bool PlayClip(T clip, ClipDefinition definition, Vector3 position)
        {
            if (definition is null)
                throw new ArgumentNullException(nameof(definition));

            if (Player != null)
            {
                Stop();

                if (!PlaybackUtils.TryLoadClip(definition.Name, definition.Loop, out stream, out provider))
                    return false;
                
                if (Player.Speaker != null)
                    Player.Speaker.Position = position;

                Player.WithMasterAmplification(definition.Amplification);
                Player.WithProvider(provider);

                CurrentClip = definition;

                ClipTimes[clip] = Time.realtimeSinceStartup;
                ClipStarted?.Invoke();

                return true;
            }

            return false;
        }

        /// <summary>
        /// Attempts to play a random clip at the specified position.
        /// </summary>
        /// <remarks>The method checks if the clip is eligible to be played based on cooldown restrictions
        /// and whether the clip is available. If the clip is currently playing or does not meet the cooldown
        /// requirements, the method returns <see langword="false"/>.</remarks>
        /// <param name="clip">The identifier of the clip to play.</param>
        /// <returns><see langword="true"/> if the clip was successfully played; otherwise, <see langword="false"/>.</returns>
        public bool PlayRandomClip(T clip)
        {
            if (Config.Cooldowns.TryGetValue(clip, out var cooldown)
                && cooldown > 0f
                && ClipTimes.TryGetValue(clip, out var lastPlayedTime)
                && (Time.realtimeSinceStartup - lastPlayedTime) < cooldown)
                return false;

            if (!TryGetClip(clip, out var clipDef))
                return false;

            return PlayClip(clip, clipDef);
        }
        
        /// <summary>
        /// Attempts to play a random clip at the specified position.
        /// </summary>
        /// <remarks>The method checks if the clip is eligible to be played based on cooldown restrictions
        /// and whether the clip is available. If the clip is currently playing or does not meet the cooldown
        /// requirements, the method returns <see langword="false"/>.</remarks>
        /// <param name="clip">The identifier of the clip to play.</param>
        /// <param name="position">The position in the 3D space where the clip should be played.</param>     
        /// <returns><see langword="true"/> if the clip was successfully played; otherwise, <see langword="false"/>.</returns>
        public bool PlayRandomClip(T clip, Vector3 position)
        {
            if (Config.Cooldowns.TryGetValue(clip, out var cooldown)
                && cooldown > 0f
                && ClipTimes.TryGetValue(clip, out var lastPlayedTime)
                && (Time.realtimeSinceStartup - lastPlayedTime) < cooldown)
                return false;

            if (!TryGetClip(clip, out var clipDef))
                return false;

            return PlayClip(clip, clipDef, position);
        }

        /// <summary>
        /// Sets a proximity or filtered send engine for the specified target player.
        /// </summary>
        public void SetPersonal(Player target, bool sendToOthers = true)
        {
            if (target?.ReferenceHub == null)
                throw new ArgumentNullException(nameof(target));

            if (sendToOthers)
            {
                if (SendEngine == SendEngine.DefaultEngine)
                    return;

                if (SendEngine is IDisposable disposable)
                    disposable.Dispose();

                SendEngine = SendEngine.DefaultEngine;
            }
            else
            {
                if (SendEngine is SpecificPlayerSendEngine specificPlayerSendEngine
                    && specificPlayerSendEngine.Target == target)
                    return;

                if (SendEngine is IDisposable disposable)
                    disposable.Dispose();

                SendEngine = new SpecificPlayerSendEngine(target);
            }
        }

        /// <summary>
        /// Sets a global send engine.
        /// </summary>
        public void SetGlobal()
        {
            if (SendEngine is IDisposable disposable)
                disposable.Dispose();

            SendEngine = new GlobalSendEngine(Player.Speaker.Base);
        }
        
        /// <summary>
        /// Stops the current playback and releases associated resources.
        /// </summary>
        public void Stop()
        {
            if (stream != null)
            {
                if (CurrentClip != null)
                    ClipEnded?.Invoke();

                CurrentClip = null;

                try
                {
                    stream?.Dispose();
                    stream = null;
                }
                catch
                {

                }

                try
                {
                    if (provider is IDisposable disposable)
                        disposable.Dispose();
                }
                catch
                {

                }

                provider = null;

                if (Player != null)
                {
                    if (SendEngine != SendEngine.DefaultEngine)
                    {
                        if (SendEngine is IDisposable disposable)
                            disposable.Dispose();

                        SendEngine = SendEngine.DefaultEngine;
                    }
                }
            }
        }

        /// <summary>
        /// Releases resources and resets the state of the object.
        /// </summary>
        public void Destroy()
        {
            Stop();

            ClipTimes.Clear();

            if (Player != null)
            {
                Player.NoSamplesRead -= OnEnd;
                Player.Destroy();
            }

            Player = null!;
        }

        private void OnEnd()
        {
            if (CurrentClip != null)
                ClipEnded?.Invoke();

            CurrentClip = null;

            try
            {
                stream?.Dispose();
                stream = null;
            }
            catch
            {

            }

            try
            {
                if (provider is IDisposable disposable)
                    disposable.Dispose();
            }
            catch
            {

            }

            provider = null;

            if (Player != null)
            {
                if (SendEngine != SendEngine.DefaultEngine)
                {
                    if (SendEngine is IDisposable disposable)
                        disposable.Dispose();

                    SendEngine = SendEngine.DefaultEngine;
                }
            }
        }
    }
}