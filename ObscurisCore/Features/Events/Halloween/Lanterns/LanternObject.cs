using LabExtended.API;
using LabExtended.API.Toys;

using LabExtended.Core.Configs.Objects;

using LabExtended.Extensions;
using LabExtended.Utilities;

using NiveraAPI.IO.Configs;

using ObscurisCore.Extensions;

using ObscurisCore.Features.Rays.Enums;
using ObscurisCore.Features.Audio.Clips;
using ObscurisCore.Features.Rays.Objects;
using ObscurisCore.Features.Elements.Alerts;
using ObscurisCore.Features.Events.Halloween.Lanterns.Enums;
using ObscurisCore.Features.Events.Halloween.Lanterns.Objects;

using ProjectMER.Features.Objects;

using UnityEngine;

using Utils;

namespace ObscurisCore.Features.Events.Halloween.Lanterns;

/// <summary>
/// Represents a lantern object that inherits from the RayObject base class.
/// The LanternObject can interact with player actions, such as shooting.
/// It maintains a record of players that have interacted with it to prevent duplicate rewards.
/// </summary>
public class LanternObject : RayObject
{
    /// <summary>
    /// A static variable that holds the message displayed to a player upon interacting with a lantern.
    /// The message uses the placeholders %PreviousPoints%, %CurrentPoints%, and %PointsAdded% to dynamically
    /// show the player's previous points total, current points, and the points added from the interaction.
    /// </summary>
    [Config("halloween", "lantern-hit-message", "The message displayed when a player interacts with a lantern (Variables: %PreviousPoints%, %CurrentPoints%, %PointsAdded%).")]
    public static string HitMessage = "Získal jsi dalších <color=red>%PointsAdded%</color> bodů. Celkový počet bodů: <color=red>%CurrentPoints%</color>";

    /// <summary>
    /// A static variable that determines whether the lantern spawns with an active light.
    /// </summary>
    [Config("halloween", "lantern-light", "Whether the lantern should spawn with a light.")]
    public static bool LanternLight = true;

    /// <summary>
    /// A static variable that determines whether the lantern should exhibit a floating behavior.
    /// This configuration is typically used to enable a dynamic, thematic appearance for Halloween events.
    /// </summary>
    [Config("halloween", "lantern-float", "Whether the lantern should float.")]
    public static bool LanternFloat = true;
    
    /// <summary>
    /// A static variable that determines whether the lantern should have rotational movement enabled.
    /// Typically used to configure the dynamic behavior of lanterns during the Halloween event.
    /// </summary>
    [Config("halloween", "lantern-rotation", "Whether the lantern should rotate.")]
    public static bool LanternRotation = true;

    /// <summary>
    /// A static variable that defines the speed at which the lantern floats in the game.
    /// The value controls how quickly the lantern oscillates during its floating animation.
    /// </summary>
    [Config("halloween", "lantern-float-speed", "The speed at which the lantern should float.")]
    public static float LanternFloatSpeed = 1f;

    /// <summary>
    /// A static variable that determines the adjustment made to the Y axis for the lantern's floating movement.
    /// This value specifies the position around which the lantern will oscillate when floating.
    /// </summary>
    [Config("halloween", "lantern-float-axis", "Adjustment for the Y axis.")]
    public static float LanternFloatAxis = 1f;

    /// <summary>
    /// A static variable that defines the amplitude of the floating motion of a lantern.
    /// This value determines how far the lantern moves vertically during its float animation.
    /// </summary>
    [Config("halloween", "lantern-float-amplitude", "The amplitude of the lantern's float movement.")]
    public static float LanternFloatAmplitude = 1f;
    
    /// <summary>
    /// A static variable that determines the rotational angle of the lantern when the rotation feature is enabled.
    /// This value specifies how far the lantern rotates, typically in degrees, to create a dynamic or decorative effect.
    /// </summary>
    [Config("halloween", "lantern-rotation-angle", "The angle at which the lantern should rotate.")]
    public static float LanternRotationAngle = 10f;

    /// <summary>
    /// A configurable static variable that determines the intensity level of the lantern's light.
    /// This value dictates how bright the lantern appears when illuminated.
    /// </summary>
    [Config("halloween", "lantern-light-intensity", "The intensity of the lantern's light.")]
    public static byte LanternLightIntensity = 100;

    /// <summary>
    /// A static variable that defines the range of light emitted by a lantern object in the game.
    /// </summary>
    [Config("halloween", "lantern-light-range", "The range of the lantern's light.")]
    public static float LanternLightRange = 100f;

    /// <summary>
    /// A static variable that sets the inner spot angle of the lantern's light.
    /// This value determines the angle of the inner cone of the spotlight emitted by the lantern,
    /// influencing how focused or wide the central beam of the light appears.
    /// </summary>
    [Config("halloween", "lantern-light-inner-spot-angle", "The inner spot angle of the lantern's light.")]
    public static float LanternLightInnerSpotAngle = 0f;

    /// <summary>
    /// A configurable static variable that determines the strength of shadows
    /// cast by the lantern's light. The value typically ranges from 0 (no shadow)
    /// to 1 (fully opaque shadow).
    /// </summary>
    [Config("halloween", "lantern-light-shadow-strength", "The shadow strength of the lantern's light.")]
    public static float LanternLightShadowStrength = 0f;

    /// <summary>
    /// A static variable that determines the shadow type of the lantern's light.
    /// </summary>
    [Config("halloween", "lantern-light-shadow-type", "The shadow type of the lantern's light.")]
    public static LightShadows LanternLightShadowType = LightShadows.Hard;

    /// <summary>
    /// A static variable that defines the color of the light emitted by a lantern.
    /// </summary>
    [Config("halloween", "lantern-color", "The color of the lantern's light.")]
    public static YamlColor LanternLightColor = new(Color.yellow);

    /// <summary>
    /// A static variable that defines the audio clips used by the lantern during the Halloween event.
    /// </summary>
    [Config("halloween", "lantern-audio-clips", "The audio clips associated with the lantern in the Halloween event.")]
    public static ClipConfig<LanternClip> LanternAudioClips = new()
    {
        Cooldowns = new()
        {
            { LanternClip.Shot, 2f },
            { LanternClip.Ambience, 0f }
        },
        
        Clips = new()
        {
            { LanternClip.Shot, new() { new() } },
            { LanternClip.Ambience, new() { new() } }
        }
    };

    /// <summary>
    /// A static variable that determines the likelihood (in percentage) of a lantern exploding
    /// during the Halloween event. This value is used to control the randomized behavior of lanterns
    /// in the game, adding an element of unpredictability to player interactions.
    /// </summary>
    [Config("halloween", "lantern-explosion-chance", "The chance of the lantern exploding in the Halloween event.")]
    public static float LanternExplosionChance = 20f;
    
    private float initialY;
    
    /// <summary>
    /// Represents a dynamic light component attached to the lantern object.
    /// </summary>
    public LightToy? LightObject;
    
    /// <summary>
    /// Represents a schematic object associated with a lantern in the game.
    /// </summary>
    public SchematicObject? SchematicObject;

    /// <summary>
    /// A variable that manages the audio clips associated with the lantern object.
    /// It utilizes the ClipManager and supports various sound effects defined in the LanternClip enum,
    /// such as ambient sound and sound effects triggered during interactions like shooting.
    /// </summary>
    public ClipManager<LanternClip> AudioClips;
    
    /// <summary>
    /// A collection of player identifiers representing players who have interacted
    /// with the LanternObject and received points. This acts as a record to prevent
    /// duplicate point awards to the same player for subsequent interactions.
    /// </summary>
    public HashSet<string> ServedPlayers = new();

    /// <summary>
    /// Gets called when the object gets added.
    /// </summary>
    public override void Start()
    {
        base.Start();

        AudioClips = new(LanternAudioClips, SchematicObject.Position);
        AudioClips.PlayRandomClip(LanternClip.Ambience);

        if (LanternLight)
        {
            LightObject = new(SchematicObject.Position, SchematicObject.Rotation);
            LightObject.Color = LanternLightColor.Color.FixPrimitiveColor();
            
            LightObject.Range = LanternLightRange;
            LightObject.Intensity = LanternLightIntensity;
            LightObject.ShadowType = LanternLightShadowType;
            LightObject.InnerSpotAngle = LanternLightInnerSpotAngle;
            LightObject.ShadowStrength = LanternLightShadowStrength;

            LightObject.Base.syncInterval = 0f;
        }
        
        initialY = SchematicObject.Position.y + LanternFloatAxis;
    }

    /// <summary>
    /// Gets called when the parent object gets destroyed.
    /// </summary>
    public override void Destroy()
    {
        base.Destroy();

        LightObject?.Delete();
        LightObject = null;
        
        SchematicObject?.Destroy();
        SchematicObject = null;
        
        AudioClips?.Destroy();
        AudioClips = null!;
        
        ServedPlayers.Clear();
    }

    /// <summary>
    /// Handles the event triggered when the lantern object is hit.
    /// </summary>
    /// <param name="player">The player who caused the hit.</param>
    /// <param name="source">The source of the hit, such as a weapon or SCP entity.</param>
    /// <param name="damage">The amount of damage dealt to the object.</param>
    public override void OnHit(ExPlayer player, ObjectHitSource source, float damage)
    {
        base.OnHit(player, source, damage);
        
        if (!ServedPlayers.Add(player.UserId))
            return;
        
        LanternEvent.AddPoints(player, LanternSpawner.LanternPoints, points =>
        {
            if (points.HasValue && !string.IsNullOrWhiteSpace(HitMessage))
            {
                LanternEvent.UpdateCollected(player, points.Value, LanternSpawner.LanternPoints);
                
                AudioClips?.PlayRandomClip(LanternClip.Shot);

                player.SendHitMarker();
                player.SendFormattedAlert(HitMessage
                    .Replace("%PreviousPoints%", (points.Value - LanternSpawner.LanternPoints).ToString())
                    .Replace("%CurrentPoints%", points.Value.ToString())
                    .Replace("%PointsAdded%", LanternSpawner.LanternPoints.ToString()), true, AlertType.Info, 5f, "Jack-O-Lantern");
                
                LightObject?.Base.DestroyFor(player.Connection);
                AudioClips?.Player?.Speaker?.Base.DestroyFor(player.Connection);
                SchematicObject?.AdminToyBases.ForEach(toy => toy.DestroyFor(player.Connection));

                if (FlyingLantern.LanternFlyChance > 0f && (FlyingLantern.LanternFlyChance >= 100f || WeightUtils.GetBool(FlyingLantern.LanternFlyChance)))
                    FlyingLantern.SpawnNew(player, SchematicObject.Position, SchematicObject.Rotation);
                else if (FollowingLantern.LanternFollowChance > 0f && (FollowingLantern.LanternFollowChance >= 100f || WeightUtils.GetBool(FollowingLantern.LanternFollowChance)))
                    FollowingLantern.SpawnNew(player, SchematicObject.Position, SchematicObject.Rotation);
                else if (LanternExplosionChance > 0f && (LanternExplosionChance >= 100f || WeightUtils.GetBool(LanternExplosionChance)))
                    ExplosionUtils.ServerSpawnEffect(SchematicObject!.Position, WeightUtils.GetBool(50f) ? ItemType.GrenadeHE : ItemType.GrenadeFlash);
            }
        });
    }

    /// <summary>
    /// Gets called once per frame at the end of all Update calls when the parent object updates.
    /// </summary>
    public override void LateUpdate()
    {
        base.LateUpdate();

        if (SchematicObject != null)
        {
            if (LanternRotation)
            {
                SchematicObject.Rotation = SchematicObject.Rotation.Rotate(LanternRotationAngle);

                if (LightObject != null)
                    LightObject.Rotation = SchematicObject.Rotation;
            }

            if (LanternFloat)
            {
                SchematicObject.Position = SchematicObject.Position.Float(initialY, LanternFloatSpeed, LanternFloatAmplitude);
                
                if (LightObject != null)
                    LightObject.Position = SchematicObject.Position;
            }
        }

        if (AudioClips != null && !AudioClips.IsPlaying)
        {
            AudioClips.PlayRandomClip(LanternClip.Ambience);
        }
    }
}