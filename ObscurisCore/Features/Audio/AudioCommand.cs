using System.Net;

using LabApi.Loader.Features.Paths;

using LabExtended.API;

using LabExtended.Commands;
using LabExtended.Commands.Attributes;
using LabExtended.Commands.Interfaces;

using LabExtended.Core;
using LabExtended.Utilities;

using UnityEngine;

using SecretLabNAudio.Core;
using ObscurisCore.Features.Audio.Clips;
using LabExtended.Commands.Utilities;
using ObscurisCore.Features.Audio.Playback;
using ObscurisCore.Features.Elements.Alerts;

namespace ObscurisCore.Features.Audio
{
    /// <summary>
    /// Audio playback commands.
    /// </summary>
    [Command("audio", "Base command for audio utilities")]
    public class AudioCommand : CommandBase, IServerSideCommand
    {
        [CommandOverload("list", "Lists all audio files.", "audio.list")]
        private void List()
        {
            PlaybackUtils.ReloadFiles();

            var files = PlaybackUtils.UniqueFiles();

            if (files.Length < 1)
            {
                Fail($"No audio files are loaded.");
                return;
            }
            
            Ok(x =>
            {
                x.AppendLine();

                foreach (var name in files)
                    x.AppendLine($"- {name}");
            });
        }

        [CommandOverload("download", "Downloads an audio file.", "audio.download")]
        private void Download(
            [CommandParameter("Url", "The URL to download the audio file from.")] string url,
            [CommandParameter("Name", "Name of the file to be saved as - must include file type (mp3).")] string name)
        {
            var extension = Path.GetExtension(name);

            if (string.IsNullOrWhiteSpace(extension))
            {
                Fail($"The 'Name' argument must contain a valid audio file extension (like mp3).");
                return;
            }
            
            Ok($"Attempting to download audio file ({name}) from: {url}");
            
            var path = Path.Combine(PathManager.SecretLab.FullName, "audio");

            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);

            path = Path.Combine(path, "downloads");

            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);

            path = Path.Combine(path, name);

            var player = Context.Sender;
            
            Task.Run(async () =>
            {
                using (var client = new WebClient())
                    await client.DownloadFileTaskAsync(url, path);
            }).ContinueWithOnMain(task =>
            {
                if (task.Exception != null)
                {
                    ApiLog.Error("Audio", task.Exception);
                    
                    Write($"Audio file download failed:\n{task.Exception}");
                    
                    player.SendAlert(AlertType.Warn, 10f, "Audio System", 
                        $"<b>Stahování audio souboru <color=yellow>{name}</color> bylo neúspěšné <i>(více info v konzoli)</i>!</b>");
                }
                
                PlaybackUtils.ReloadFiles();
                
                Write($"Downloaded audio file '{name}'");
                
                player.SendAlert(AlertType.Info, 10f, "Audio System", 
                    $"<b>Dokončeno stahování souboru <color=yellow>{name}</color>!</b>");
            });
        }
        
        [CommandOverload("position", "Starts playing an audio clip at a specified position.", "audio.position")]
        private void StartAt(
            [CommandParameter("Position", "The position to play the audio at.")] Vector3 position, 
            [CommandParameter("Clip", "The name of the audio clip file.")] string clip, 
            [CommandParameter("Loop", "Whether or not the clip shoud loop.")] bool loop = false)
        {
            if (PlaybackUtils.PlayAt(clip, position, SpeakerSettings.Default, loop).HasValue)
            {
                Ok($"Started playing clip '{clip}' at {position.ToPreciseString()}");
            }
            else
            {
                Fail($"Could not start playing clip '{clip}' at {position.ToPreciseString()}");
            }
        }

        [CommandOverload("player", "Starts playing an audio clip at a specified player.", "audio.player")]
        private void StartPlayer(
            [CommandParameter("Target", "The player to start playing the clip for.")] ExPlayer player,
            [CommandParameter("Clip", "The name of the audio clip file.")] string clip,
            [CommandParameter("Volume", "The volume of the speaker toy.")] float volume = 1f,
            [CommandParameter("Amplification", "Master audio amplification (increases volume).")] float amplification = 1f,
            [CommandParameter("IsPersonal", "Whether or not the audio should be heard by the targeted player only.")] bool isPersonal = false)
        {
            player ??= Sender;
            
            if (player.PlayClip(clip, volume, amplification, isPersonal))
            {
                Ok($"Started playing clip '{clip}' for player {player.ToCommandString()}.");
            }
            else
            {
                Fail($"Failed to play clip '{clip}' for player {player.ToCommandString()}. Make sure the clip exists and is a valid audio file.");
            }
        }

        [CommandOverload("stop", "Stops audio playback for the specified players.", "audio.stop")]
        private void Stop(
            [CommandParameter("Players", "The list of players to stop audio playback for.")] List<ExPlayer> players)
        {
            this.ForEachExecute(players, player =>
            {
                player.StopClip(out var clip);
                
                if (clip != null)
                {
                    return $"&3Stopped&r &2{clip}&r";
                }
                else
                {
                    return $"&1No audio to stop&r";
                }
            });
        }
    }
}