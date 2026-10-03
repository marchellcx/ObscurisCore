using LabExtended.Extensions;

using NAudio.Wave;
using SecretLabNAudio.Core;
using SecretLabNAudio.Core.Extensions;

namespace ObscurisCore.Features.Audio.Playback
{
    /// <summary>
    /// Represents a handle for managing audio playback, providing control over playback state, volume, and associated
    /// resources.
    /// </summary>
    /// <remarks>A <see cref="PlaybackHandle"/> is used to interact with an audio clip being played by an <see
    /// cref="AudioPlayer"/>.  It provides methods to control playback (e.g., <see cref="Resume"/>, <see
    /// cref="Pause"/>), adjust volume, and access  metadata such as the clip name and file path. The handle also tracks
    /// the playback state (e.g., <see cref="IsPlaying"/>,  <see cref="IsPaused"/>) and validity (<see
    /// cref="IsValid"/>).  The handle becomes invalid when the associated <see cref="AudioPlayer"/> is destroyed, at
    /// which point the  <see cref="OnDestroyed"/> delegate is invoked.</remarks>
    public struct PlaybackHandle
    {
        /// <summary>
        /// Whether or not the audio is being played.
        /// </summary>
        public bool IsPlaying;

        /// <summary>
        /// Whether or not the audio is paused.
        /// </summary>
        public bool IsPaused;

        /// <summary>
        /// Whether or not the handle is valid.
        /// </summary>
        public bool IsValid;

        /// <summary>
        /// The name of the clip's audio file.
        /// </summary>
        public readonly string Clip;

        /// <summary>
        /// The audio player playing the clip.
        /// </summary>
        public readonly AudioPlayer Player;

        /// <summary>
        /// The loaded audio wave stream.
        /// </summary>
        public readonly WaveStream Stream;

        /// <summary>
        /// The loaded audio provider.
        /// </summary>
        public readonly IWaveProvider Provider;

        /// <summary>
        /// The delegate invoked when the audio player is destroyed.
        /// </summary>
        public readonly Action? OnDestroyed;

        public PlaybackHandle(string clip, AudioPlayer player, WaveStream stream, IWaveProvider provider, Action? onDestroyed = null)
        {
            Clip = clip;
            Stream = stream;
            Provider = provider;
            OnDestroyed = onDestroyed;

            Player = player;
            Player.Destroyed += Destroyed;

            IsValid = true;
            IsPaused = false;
            IsPlaying = true;
        }

        /// <summary>
        /// Resumes playback if it is currently paused.
        /// </summary>
        /// <remarks>This method transitions the playback state from paused to playing.  If the playback
        /// is not paused, calling this method has no effect.</remarks>
        public void Resume()
        {
            if (!IsPaused)
                return;

            Player.Pause(false);

            IsPaused = false;
            IsPlaying = true;
        }

        /// <summary>
        /// Pauses playback if it is currently active.
        /// </summary>
        /// <remarks>This method transitions the playback state to paused. If playback is not active, the
        /// method does nothing.</remarks>
        public void Pause()
        {
            if (!IsPlaying)
                return;

            Player.Pause(true);

            IsPlaying = false;
            IsPaused = true;
        }

        /// <summary>
        /// Sets the playback volume for the player.
        /// </summary>
        /// <remarks>The exact behavior of the volume adjustment depends on the implementation of the
        /// player.  Ensure the value is within the supported range to avoid unexpected results.</remarks>
        /// <param name="volume">The desired volume level, specified as a floating-point value.  Valid values typically range from 0.0
        /// (muted) to 1.0 (maximum volume).</param>
        public void SetVolume(float volume)
        {
            Player.WithVolume(volume);
        }

        /// <summary>
        /// Releases the resources used by the underlying stream.
        /// </summary>
        public void Destroy()
        {
            try
            {
                Stream.Dispose();
            }
            catch
            {

            }
        }

        private void Destroyed()
        {
            OnDestroyed?.InvokeSafe();

            Player.Destroyed -= Destroyed;

            IsPlaying = false;
            IsPaused = false;
            IsValid = false;
        }
    }
}