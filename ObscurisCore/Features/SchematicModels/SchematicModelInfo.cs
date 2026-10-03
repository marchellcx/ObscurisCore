using LabExtended.API;
using LabExtended.Utilities.Update;

using Mirror;

using ObscurisCore.Features.SchematicModels.Configs;

using ProjectMER.Features.Objects;

using UnityEngine;

namespace ObscurisCore.Features.SchematicModels;

/// <summary>
/// Represents detailed information about a schematic model, including its position
/// and rotation offsets, as well as the associated player and schematic object.
/// </summary>
public class SchematicModelInfo
{
    /// <summary>
    /// The position offset of the schematic.
    /// </summary>
    public Vector3? PositionOffset { get; }

    /// <summary>
    /// The rotation offset of the schematic.
    /// </summary>
    public Quaternion? RotationOffset { get; }
    
    /// <summary>
    /// The player whose model was replaced.
    /// </summary>
    public ExPlayer Player { get; }
    
    /// <summary>
    /// The schematic that replaced the player's model.
    /// </summary>
    public SchematicObject Schematic { get; }

    /// <summary>
    /// Whether or not to remove the model on role change.
    /// </summary>
    public bool RemoveOnRoleChange { get; set; } = true;
    
    /// <summary>
    /// Whether the schematic position update is paused.
    /// </summary>
    public bool Paused { get; private set; }

    /// <summary>
    /// Represents detailed information about a schematic model, including its position
    /// and rotation offsets, as well as the associated player and schematic object.
    /// </summary>
    public SchematicModelInfo(ExPlayer player, SchematicObject schematic, SchematicModelConfig? config)
    {
        Player = player;
        Schematic = schematic;

        if (config != null)
        {
            if (config.PositionOffset.X != 0f
                || config.PositionOffset.Y != 0f
                || config.PositionOffset.Z != 0f)
                PositionOffset = config.PositionOffset.Vector;

            if (config.RotationOffset.X != 0f
                || config.RotationOffset.Y != 0f
                || config.RotationOffset.Z != 0f
                || config.RotationOffset.W != 0f)
                RotationOffset = config.RotationOffset.Quaternion;
        }
    }

    /// <summary>
    /// Starts the schematic position update and sets the fake scale for the player.
    /// </summary>
    public void Start()
    {
        PlayerUpdateHelper.OnFixedUpdate += Update;

        Player.SetFakeScale(Vector3.zero, false, false, RemoveOnRoleChange);
    }

    /// <summary>
    /// Stops the schematic position update and destroys the schematic object. Optionally, it can also remove the fake scale from the player.
    /// </summary>
    /// <param name="syncScale">Indicates whether to remove the fake scale from the player.</param>
    public void Stop(bool syncScale)
    {
        PlayerUpdateHelper.OnFixedUpdate -= Update;
        
        if (Schematic != null)
            Schematic.Destroy();
        
        if (syncScale)
            Player?.RemoveFakeScale();
    }

    /// <summary>
    /// Resumes the schematic position update and shows the schematic to the player's view.
    /// </summary>
    public void Resume()
    {
        if (Paused)
        {
            Paused = false;
            
            ReSpawn();
            
            Player?.SetFakeScale(Vector3.zero, false, false, RemoveOnRoleChange);
        }
    }

    /// <summary>
    /// Pauses the schematic position update and hides the schematic from the player's view.
    /// </summary>
    public void Pause()
    {
        if (!Paused)
        {
            Paused = true;
            
            UnSpawn();
            
            Player?.RemoveFakeScale();
        }
    }

    private void UnSpawn()
    {
        for (var x = 0; x < Schematic.NetworkIdentities.Count; x++)
        {
            NetworkServer.SendToReady(new ObjectHideMessage() { netId = Schematic.NetworkIdentities[x].netId });
        }
    }

    private void ReSpawn()
    {
        for (var x = 0; x < Schematic.NetworkIdentities.Count; x++)
        {
            NetworkServer.SendToReady(Schematic.NetworkIdentities[x].GetSpawnMessage());
        }
    }

    private void Update()
    {
        if (Player?.ReferenceHub == null || Schematic == null)
        {
            Stop(true);
            return;
        }

        if (Paused)
        {
            return;
        }

        var position = Player.CameraTransform.position;
        var rotation = Player.CameraTransform.rotation;

        if (PositionOffset.HasValue)
        {
            position += PositionOffset.Value;
        }

        if (RotationOffset.HasValue)
        {
            rotation.x += RotationOffset.Value.x;
            rotation.y += RotationOffset.Value.y;
            rotation.z += RotationOffset.Value.z;
            rotation.w += RotationOffset.Value.w;
        }
        
        Schematic.Position = position;
        Schematic.Rotation = rotation;
    }
}