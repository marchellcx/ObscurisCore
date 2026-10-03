using LabApi.Loader.Features.Paths;

using LabExtended.Core;
using LabExtended.Extensions;

using SecretLabNAudio.Core;
using SecretLabNAudio.Core.Pools;
using SecretLabNAudio.Core.Extensions;
using SecretLabNAudio.Core.FileReading;

using UnityEngine;

using NAudio.Wave;
using NorthwoodLib.Pools;

namespace ObscurisCore.Features.Audio.Playback
{
    /// <summary>
    /// Audio-related utilities.
    /// </summary>
    public static class PlaybackUtils
    {
        /// <summary>
        /// Gets a list of all loaded audio clips in memory.
        /// </summary>
        public static Dictionary<string, KeyValuePair<byte[], string>> LoadedClips { get; } = new();

        /// <summary>
        /// Extracts a list of unique file names from the loaded audio clips, preserving only the last part of each file path.
        /// </summary>
        /// <remarks>
        /// This method processes all keys in the <see cref="LoadedClips"/> dictionary. If a key contains a path with
        /// a forward slash ('/'), it splits the path and retains only the last segment (i.e., the file name). If no
        /// slash is found, the entire key is treated as a unique file name. Duplicates are filtered out, and only unique
        /// file names are returned.
        /// </remarks>
        /// <returns>
        /// An array of unique file names extracted from the keys of the <see cref="LoadedClips"/> dictionary.
        /// </returns>
        public static string[] UniqueFiles()
        {
            var list = HashSetPool<string>.Shared.Rent();

            foreach (var p in LoadedClips)
            {
                if (p.Key.TrySplit('/', true, null, out var parts))
                {
                    list.Add(parts.Last().Trim());
                }
                else
                {
                    list.Add(p.Key.Trim());
                }
            }

            var array = list.ToArray();
            
            HashSetPool<string>.Shared.Return(list);
            return array;
        }
        
        /// <summary>
        /// Reloads all audio files from the audio directory into memory and clears previously loaded clips.
        /// </summary>
        /// <remarks>
        /// This method scans the Secret Lab's "audio" directory and its subdirectories for audio files.
        /// If the directory does not exist, it will be created. All files found, along with their extensions,
        /// are loaded into the <see cref="LoadedClips"/> dictionary. Both file names with and without extensions
        /// are stored as keys. Subdirectory organization is preserved in the keys by including the folder name
        /// in the path where applicable. If the file names are duplicates (including across directories), only
        /// unique keys are stored. A log message is generated indicating the number of unique files loaded.
        /// </remarks>
        /// <exception cref="IOException">Thrown if an error occurs while reading files or directories.</exception>
        /// <exception cref="UnauthorizedAccessException">Thrown if the application does not have permission to access the file system.</exception>
        public static void ReloadFiles()
        {
            LoadedClips.Clear();
            
            var path = Path.Combine(PathManager.SecretLab.FullName, "audio");

            if (!Directory.Exists(path))
            {
                ApiLog.Warn("PlaybackUtils", "Audio directory does not exist, creating ..");

                Directory.CreateDirectory(path);
            }

            var uniqueCount = 0;

            foreach (var file in Directory.GetFiles(path))
            {
                var data = File.ReadAllBytes(file);
                var extension = Path.GetExtension(file);

                var nameExtension = Path.GetFileName(file);
                var nameNoExtension = Path.GetFileNameWithoutExtension(file);

                if (!LoadedClips.ContainsKey(nameExtension)) LoadedClips.Add(nameExtension, new(data, extension));
                if (!LoadedClips.ContainsKey(nameNoExtension)) LoadedClips.Add(nameNoExtension, new(data, extension));

                uniqueCount++;
            }

            foreach (var directory in Directory.GetDirectories(path))
            {
                var name = Path.GetFileName(directory);

                foreach (var file in Directory.GetFiles(directory))
                {
                    var data = File.ReadAllBytes(file);
                    var extension = Path.GetExtension(file);

                    var nameExtensionWithDirectory = $"{name}/{Path.GetFileName(file)}";
                    var nameExtensionWithoutDirectory = Path.GetFileName(file);

                    var nameNoExtensionWithDirectory = Path.GetFileNameWithoutExtension(file);
                    var nameNoExtensionWithoutDirectory = $"{name}/{Path.GetFileNameWithoutExtension(file)}";

                    if (!LoadedClips.ContainsKey(nameExtensionWithDirectory)) LoadedClips.Add(nameExtensionWithDirectory, new(data, extension));
                    if (!LoadedClips.ContainsKey(nameExtensionWithoutDirectory)) LoadedClips.Add(nameExtensionWithoutDirectory, new(data, extension));

                    if (!LoadedClips.ContainsKey(nameNoExtensionWithDirectory)) LoadedClips.Add(nameNoExtensionWithDirectory, new(data, extension));
                    if (!LoadedClips.ContainsKey(nameNoExtensionWithoutDirectory)) LoadedClips.Add(nameNoExtensionWithoutDirectory, new(data, extension));

                    uniqueCount++;
                }
            }

            ApiLog.Info("PlaybackUtils", $"Loaded &a{uniqueCount}&r audio clips from &3{path}&r");
        }
        
        /// <summary>
        /// Attempts to load an audio clip by its name and create an audio stream and provider.
        /// </summary>
        /// <remarks>This method searches for the audio clip in the "Audio" directory within the
        /// application's secret lab path. If the directory does not exist, it will be created, and the method will
        /// return <see langword="false"/>. If the specified clip does not exist or cannot be loaded, the method logs a
        /// warning and returns <see langword="false"/>.</remarks>
        /// <param name="clipName">The name of the audio clip to load. Cannot be null, empty, or whitespace.</param>
        /// <param name="loopClip">A value indicating whether the audio clip should loop. If <see langword="true"/>, the returned provider will
        /// loop the clip; otherwise, it will play the clip once.</param>
        /// <param name="stream">When this method returns, contains the <see cref="WaveStream"/> representing the audio clip, or <see
        /// langword="null"/> if the method fails.</param>
        /// <param name="provider">When this method returns, contains the <see cref="IWaveProvider"/> for playing the audio clip, or <see
        /// langword="null"/> if the method fails.</param>
        /// <returns><see langword="true"/> if the audio clip was successfully loaded; otherwise, <see langword="false"/>.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="clipName"/> is <see langword="null"/>, empty, or consists only of whitespace.</exception>
        public static bool TryLoadClip(string clipName, bool loopClip, out WaveStream? stream, out IWaveProvider? provider)
        {
            provider = null;
            stream = null!;

            if (string.IsNullOrWhiteSpace(clipName))
                throw new ArgumentNullException(nameof(clipName));

            if (LoadedClips.TryGetValue(clipName, out var clipData))
            {
                var fileStream = new MemoryStream(clipData.Key);

                if (!TryCreateAudioReader.Stream(fileStream, clipData.Value, true, out stream))
                {
                    ApiLog.Warn("PlaybackUtils", $"Clip &3{clipName}&r could not be loaded from memory!");
                    return false;
                }

                provider = loopClip
                    ? stream.Loop()
                    : stream;

                return true;
            }

            var path = Path.Combine(PathManager.SecretLab.FullName, "audio");
            var clipPath = Path.Combine(path, clipName);

            if (!File.Exists(clipPath))
            {
                ApiLog.Warn("PlaybackUtils", $"Clip &3{clipName}&r does not exist!");
                return false;
            }

            if (!TryCreateAudioReader.Stream(clipPath, out stream))
            {
                ApiLog.Warn("PlaybackUtils", $"Clip &3{clipName}&r could not be loaded!");
                return false;
            }

            provider = loopClip
                ? stream.Loop()
                : stream;

            return true;
        }

        /// <summary>
        /// Attempts to play an audio clip at the specified position using the provided speaker settings.
        /// </summary>
        /// <remarks>If the audio directory or the specified clip does not exist, or if the clip cannot be
        /// loaded, the method logs a warning and returns false. The method does not throw for missing files or
        /// directories.</remarks>
        /// <param name="clipName">The name of the audio clip file to play. Cannot be null, empty, or whitespace.</param>
        /// <param name="position">The world position at which the audio clip will be played.</param>
        /// <param name="settings">Optional speaker settings to use for playback. If null, default settings are applied.</param>
        /// <returns>true if the audio clip was successfully played; otherwise, false.</returns>
        /// <exception cref="ArgumentNullException">Thrown if clipName is null, empty, or consists only of whitespace.</exception>
        public static PlaybackHandle? PlayParented(string clipName, Transform transform, SpeakerSettings? settings = null, bool loop = false, Action? destroyCallback = null)
        {
            if (string.IsNullOrWhiteSpace(clipName))
                throw new ArgumentNullException(nameof(clipName));

            if (!TryLoadClip(clipName, loop, out var stream, out var provider)
                || provider == null || stream == null)
                return null;

            var player = AudioPlayerPool.Rent(settings ?? SpeakerSettings.Default, transform)
                .WithProvider(provider)
                .PoolOnEnd()
                .DisposeOnDestroy(stream);

            return new(clipName, player, stream, provider, destroyCallback);
        }

        /// <summary>
        /// Attempts to play an audio clip at the specified position using the provided speaker settings.
        /// </summary>
        /// <remarks>If the audio directory or the specified clip does not exist, or if the clip cannot be
        /// loaded, the method logs a warning and returns false. The method does not throw for missing files or
        /// directories.</remarks>
        /// <param name="clipName">The name of the audio clip file to play. Cannot be null, empty, or whitespace.</param>
        /// <param name="position">The world position at which the audio clip will be played.</param>
        /// <param name="settings">Optional speaker settings to use for playback. If null, default settings are applied.</param>
        /// <returns>true if the audio clip was successfully played; otherwise, false.</returns>
        /// <exception cref="ArgumentNullException">Thrown if clipName is null, empty, or consists only of whitespace.</exception>
        public static PlaybackHandle? PlayAt(string clipName, Vector3 position, SpeakerSettings? settings = null, bool loop = false, Action? destroyCallback = null)
        {
            if (string.IsNullOrWhiteSpace(clipName))
                throw new ArgumentNullException(nameof(clipName));

            if (!TryLoadClip(clipName, loop, out var stream, out var provider)
                || provider == null || stream == null)
                return null;

            var player = AudioPlayerPool.Rent(settings ?? SpeakerSettings.Default, null, position)
                .WithProvider(provider)
                .PoolOnEnd()
                .DisposeOnDestroy(stream);

            return new(clipName, player, stream, provider, destroyCallback);
        }

        internal static void Initialize()
        {
            ReloadFiles();
        }
    }
}