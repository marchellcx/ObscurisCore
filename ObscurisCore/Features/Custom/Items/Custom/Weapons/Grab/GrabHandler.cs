using AdminToys;

using InventorySystem.Items;
using InventorySystem.Items.Pickups;

using LabExtended.API;
using LabExtended.API.Containers;

using LabExtended.Extensions;

using LabExtended.Utilities;
using LabExtended.Utilities.Update;

using MapGeneration;

using Mirror;
using ObscurisCore.Features.Elements.Alerts;
using ObscurisCore.Utilities;
using PlayerStatsSystem;
using ObscurisCore.Extensions;
using UnityEngine;

namespace ObscurisCore.Features.Custom.Items.Custom.Weapons.Grab
{
    /// <summary>
    /// Provides functionality for grabbing, manipulating, and releasing in-game objects such as players, items, toys,
    /// and windows using a grab gun interface. Manages the state and controls for the grab operation, including
    /// position, rotation, and scale adjustments.
    /// </summary>
    public class GrabHandler : IDisposable
    {
        private static PlayerUpdateComponent update = PlayerUpdateComponent.Create();

        private ExPlayer? targetPlayer;
        private AdminToyBase? targetToy;
        private ItemPickupBase? targetPickup;

        private Vector3? originalScale;
        private Vector3? originalPosition;
        private Quaternion? originalRotation;

        /// <summary>
        /// Gets the grab gun singleton.
        /// </summary>
        public GrabGun Gun { get; }

        /// <summary>
        /// Gets the grab settings menu.
        /// </summary>
        public GrabMenu Menu { get; }

        /// <summary>
        /// Gets the target item of the grab gun.
        /// </summary>
        public ItemBase Item { get; }

        /// <summary>
        /// Gets the owner of the grab gun.
        /// </summary>
        public ExPlayer Player { get; }

        /// <summary>
        /// Gets the rotation angle.
        /// </summary>
        public float Angle => Menu?.Angle ?? 0f;

        /// <summary>
        /// Gets the grab distance.
        /// </summary>
        public float Distance => Menu?.Distance ?? 1f;

        /// <summary>
        /// Gets the scale applied to the grabbed object.
        /// </summary>
        public Vector3 Scale => Menu?.Scale ?? Vector3.one;

        /// <summary>
        /// Gets a value indicating whether any object is currently being grabbed.
        /// </summary>
        public bool AnyGrabbed => targetPlayer != null
            || targetToy != null
            || targetPickup != null;

        /// <summary>
        /// Initializes a new instance of the GrabHandler class for the specified player and item.
        /// </summary>
        /// <param name="player">The player for whom the grab handler is being created. Cannot be null.</param>
        /// <param name="item">The item to be associated with the grab handler. Cannot be null.</param>
        public GrabHandler(ExPlayer player, ItemBase item)
        {
            Gun = GrabGun.Singleton;

            Item = item;
            Player = player;

            Menu = player.GetOrAddMenu<GrabMenu>(() => new());

            update.OnLateUpdate += Update;
        }

        /// <summary>
        /// Attempts to grab the specified toy.
        /// </summary>
        /// <param name="toy">The toy to be grabbed. Cannot be null.</param>
        /// <returns>true if the toy was successfully grabbed; otherwise, false.</returns>
        public bool GrabToy(AdminToyBase toy)
        {
            if (toy == null)
                return false;

            originalScale = null;
            originalPosition = null;
            originalRotation = null;

            InitToy(toy);

            Menu.ShowMenu();
            return true;
        }

        /// <summary>
        /// Attempts to grab the specified player.
        /// </summary>
        /// <param name="player">The player to be grabbed. Cannot be null.</param>
        /// <returns>true if the player was successfully grabbed; otherwise, false.</returns>
        public bool GrabPlayer(ExPlayer player)
        {
            if (player?.ReferenceHub == null || !player.Role.IsAlive)
                return false;

            originalScale = null;
            originalPosition = null;
            originalRotation = null;

            InitPlayer(player);

            Menu.ShowMenu();
            return true;
        }

        /// <summary>
        /// Attempts to grab the specified pickup.
        /// </summary>
        /// <param name="pickup">The pickup to be grabbed. Cannot be null.</param>
        /// <returns>true if the pickup was successfully grabbed; otherwise, false.</returns>
        public bool GrabPickup(ItemPickupBase pickup)
        {
            if (pickup == null)
                return false;

            originalScale = null;
            originalPosition = null;
            originalRotation = null;

            InitPickup(pickup);

            Menu.ShowMenu();
            return true;
        }

        /// <summary>
        /// Attempts to launch the target player or pickup with the specified speed, duration, and gravity settings.
        /// </summary>
        /// <remarks>This method will attempt to launch a player if one is targeted and available;
        /// otherwise, it will attempt to launch a pickup. If neither is available, the method returns false and no
        /// action is taken.</remarks>
        /// <param name="speed">The initial speed to apply to the target when launching.</param>
        /// <param name="duration">The duration, in seconds, for which the launch effect is applied.</param>
        /// <param name="gravity">The gravity value to use during the launch. Higher values result in stronger downward force.</param>
        /// <returns>true if the target player or pickup was successfully launched; otherwise, false.</returns>
        public bool Launch(float speed, float duration, float gravity)
        {
            if (targetPlayer?.ReferenceHub != null)
            {
                LaunchPlayer(speed, duration, gravity);

                targetToy = null;
                targetPlayer = null;
                targetPickup = null;

                originalScale = null;
                originalPosition = null;
                originalRotation = null;
                return true;
            }

            if (targetPickup != null)
            {
                LaunchPickup(speed, duration, gravity);

                targetToy = null;
                targetPlayer = null;
                targetPickup = null;

                originalScale = null;
                originalPosition = null;
                originalRotation = null;

                return true;
            }

            return false;
        }

        /// <summary>
        /// Releases any currently held toy, player, pickup, or window, optionally returning them to their original
        /// positions.
        /// </summary>
        /// <remarks>After calling this method, all held references are cleared and the associated menu is
        /// hidden. This method has no effect if no object is currently held.</remarks>
        /// <param name="toOriginalPosition">true to return the released object to its original position; otherwise, false to leave it at its current
        /// location.</param>
        /// <returns>true if an object was released; otherwise, false.</returns>
        public bool Release(bool toOriginalPosition)
        {
            if (targetToy != null)
                ReleaseToy(toOriginalPosition);

            if (targetPlayer?.ReferenceHub != null)
                ReleasePlayer(toOriginalPosition);

            if (targetPickup != null)
                ReleasePickup(toOriginalPosition);

            targetToy = null;
            targetPlayer = null;
            targetPickup = null;

            originalScale = null;
            originalPosition = null;
            originalRotation = null;

            Menu.HideMenu();
            return true;
        }

        /// <summary>
        /// Releases all resources used by the current instance.
        /// </summary>
        public void Dispose()
        {
            Release(true);

            update.OnLateUpdate -= Update;
        }

        private void Update()
        {
            if (Player?.ReferenceHub == null 
                || !Player.Role.IsAlive 
                || Item == null
                || Menu == null)
            {
                Dispose();
                return;
            }

            if (targetToy != null)
            {
                UpdateToy();
            }
            else if (targetPlayer?.ReferenceHub != null)
            {
                if (!targetPlayer.Role.IsAlive) 
                {
                    Release(true);
                    return;
                }

                UpdatePlayer();
            }
            else if (targetPickup != null)
            {
                UpdatePickup();
            }
        }

        #region Init
        private void InitToy(AdminToyBase adminToyBase)
        {
            originalScale = adminToyBase.Scale;
            originalPosition = adminToyBase.Position;
            originalRotation = adminToyBase.Rotation;

            adminToyBase.NetworkIsStatic = false;

            if (Scale != Vector3.one)
            {
                adminToyBase.NetworkScale = Scale;
            }

            targetToy = adminToyBase;

            Player.SendAlert(AlertType.Info, 5f, "Grab Gun", $"<b>Grabbed: <color=red>Admin Toy</color> {adminToyBase.CommandName}</b>");
        }

        private void InitPlayer(ExPlayer player)
        {
            originalScale = player.Scale;

            originalPosition = player.Position;
            originalRotation = player.Rotation;

            if (Scale != Vector3.one)
                player.Scale = Scale;

            player.Gravity = Vector3.zero;
            player.IsGodModeEnabled = true;

            targetPlayer = player;

            Player.SendAlert(AlertType.Info, 5f, "Grab Gun", $"<b>Grabbed: <color=red>Player</color> {player.Nickname}</b>");
        }

        private void InitPickup(ItemPickupBase itemPickupBase)
        {
            originalScale = itemPickupBase.transform.localScale;

            originalPosition = itemPickupBase.Position;
            originalRotation = itemPickupBase.Rotation;

            if (Scale != Vector3.one)
            {
                NetworkServer.UnSpawn(itemPickupBase.gameObject);

                itemPickupBase.transform.localScale = Scale;

                NetworkServer.Spawn(itemPickupBase.gameObject);
            }

            itemPickupBase.LockPickup();
            itemPickupBase.FreezePickup();

            targetPickup = itemPickupBase;

            Player.SendAlert(AlertType.Info, 5f, "Grab Gun", $"<b>Grabbed: <color=red>Item</color> {itemPickupBase.Info.ItemId}</b>");
        }
        #endregion

        #region Update
        private void UpdateToy()
        {
            targetToy.NetworkPosition = GetGrabPosition();
            targetToy.NetworkRotation = GetGrabRotation();
        }

        private void UpdatePlayer()
        {
            targetPlayer.Position.Position = GetGrabPosition();
            targetPlayer.Rotation.Rotation = GetGrabRotation();
        }

        private void UpdatePickup()
        {
            targetPickup.Position = GetGrabPosition();
            targetPickup.Rotation = GetGrabRotation();
        }
        #endregion

        #region Release
        private void ReleaseToy(bool releaseToOriginal)
        {
            if (originalScale != null)
            {
                targetToy.NetworkScale = originalScale.Value;
            }

            if (releaseToOriginal && originalPosition != null && originalRotation != null)
            {
                targetToy.NetworkPosition = originalPosition.Value;
                targetToy.NetworkRotation = originalRotation.Value;
            }
        }

        private void ReleasePlayer(bool releaseToOriginal)
        {
            if (targetPlayer?.ReferenceHub != null)
            {
                targetPlayer.Gravity = PositionContainer.DefaultGravity;

                if (originalScale != null)
                {
                    targetPlayer.Scale = originalScale.Value;
                }

                if (releaseToOriginal && originalPosition != null && originalRotation != null && targetPlayer.Role.IsAlive)
                {
                    targetPlayer.Position.Position = originalPosition.Value;
                    targetPlayer.Rotation.Rotation = originalRotation.Value;
                }

                var target = targetPlayer;

                TimingUtils.AfterFrames((() =>
                {
                    if (target?.ReferenceHub != null)
                    {
                        target.IsGodModeEnabled = false;
                    }
                }), 1);
            }
        }

        private void ReleasePickup(bool releaseToOriginal)
        {
            if (originalScale != null)
            {
                NetworkServer.UnSpawn(targetPickup.gameObject);

                targetPickup.transform.localScale = originalScale.Value;

                NetworkServer.Spawn(targetPickup.gameObject);
            }

            if (releaseToOriginal && originalPosition != null && originalRotation != null)
            {
                targetPickup.Position = originalPosition.Value;
                targetPickup.Rotation = originalRotation.Value;
            }

            targetPickup.UnlockPickup();
            targetPickup.UnfreezePickup();
        }
        #endregion

        #region Launch
        private void LaunchPlayer(float speed, float duration, float gravity)
        {
            var animator = new LaunchAnimator<ExPlayer>();

            animator.Target = targetPlayer;
            animator.Direction = Player.Rotation.CameraForward;

            animator.Speed = speed;
            animator.Gravity = gravity;
            animator.Duration = duration;

            targetPlayer = null;

            originalScale = null;
            originalPosition = null;
            originalRotation = null;

            Vector3 GetCurrent(ExPlayer player, LaunchAnimator<ExPlayer> launchAnimator)
            {
                if (player?.ReferenceHub == null || !player.Role.IsAlive)
                {
                    launchAnimator.Dispose();
                    return Vector3.zero;
                }

                return player.Position;
            }

            void SetCurrent(ExPlayer player, LaunchAnimator<ExPlayer> launchAnimator, Vector3 value)
            {
                if (player?.ReferenceHub == null || !player.Role.IsAlive)
                {
                    launchAnimator.Dispose();
                    return;
                }

                player.Position.Position = value;
            }

            void Complete(ExPlayer player, LaunchAnimator<ExPlayer> launchAnimator)
            {
                if (player?.ReferenceHub != null)
                {
                    player.Gravity = PositionContainer.DefaultGravity;
                    player.IsGodModeEnabled = false;

                    if (player.Role.IsAlive)
                        player.ReferenceHub.playerStats.KillPlayer(new CustomReasonDamageHandler("Launched by a Grab Gun", -1f));
                }
            }

            animator.GetCurrent = GetCurrent;
            animator.SetCurrent = SetCurrent;

            animator.OnComplete = Complete;
            animator.Start();

            Menu.HideMenu();
        }

        private void LaunchPickup(float speed, float duration, float gravity)
        {
            var animator = new LaunchAnimator<ItemPickupBase>();

            animator.Target = targetPickup;
            animator.Direction = Player.Rotation.CameraForward;

            animator.Speed = speed;
            animator.Gravity = gravity;
            animator.Duration = duration;

            Vector3 GetCurrent(ItemPickupBase pickup, LaunchAnimator<ItemPickupBase> launchAnimator)
            {
                if (pickup == null)
                {
                    launchAnimator.Dispose();
                    return Vector3.zero;
                }

                return pickup.Position;
            }

            void SetCurrent(ItemPickupBase pickup, LaunchAnimator<ItemPickupBase> launchAnimator, Vector3 value)
            {
                if (pickup == null)
                {
                    launchAnimator.Dispose();
                    return;
                }

                pickup.Position = value;
            }

            void Complete(ItemPickupBase pickup, LaunchAnimator<ItemPickupBase> launchAnimator)
            {
                if (pickup.Position.TryGetRoom(out var room) 
                    && room.Zone != FacilityZone.None 
                    && room.Zone != FacilityZone.Other)
                {
                    pickup.UnfreezePickup();
                    pickup.UnlockPickup();

                    return;
                }

                pickup.DestroySelf();
            }

            animator.GetCurrent = GetCurrent;
            animator.SetCurrent = SetCurrent;

            animator.OnComplete = Complete;
            animator.Start();

            Menu.HideMenu();
        }
        #endregion

        private Vector3 GetGrabPosition()
        {
            var cameraPosition = Player.Rotation.CameraPosition;
            var cameraForward = Player.Rotation.CameraForward;

            if (Menu.DistanceSlider.Value > 0f)
                cameraForward *= Menu.DistanceSlider.Value;

            return cameraPosition + cameraForward;
        }

        private Quaternion GetGrabRotation()
        {
            var cameraForward = Player.Rotation.CameraForward;
            var cameraUp = Player.CameraTransform.up;

            var lookAtPlayer = Quaternion.LookRotation(cameraForward, cameraUp);

            if (Menu.RotateSetting.IsAButtonActive)
            {
                if (Angle != 0f)
                {
                    lookAtPlayer.x += Angle + Time.deltaTime;
                }
                else
                {
                    lookAtPlayer.x += Time.deltaTime;
                }
            }
            else if (Angle != 0f)
            {
                lookAtPlayer.x += Angle;
            }

            return lookAtPlayer;
        }

        internal void UpdateScale(Vector3 scale)
        {
            if (targetPlayer?.ReferenceHub != null)
            {
                originalScale ??= targetPlayer.Scale;

                targetPlayer.Scale = scale;
            }
            else if (targetPickup != null)
            {
                originalScale ??= targetPickup.transform.localScale;

                NetworkServer.UnSpawn(targetPickup.gameObject);

                targetPickup.transform.localScale = scale;

                NetworkServer.Spawn(targetPickup.gameObject);
            }
            else if (targetToy != null)
            {
                originalScale ??= targetToy.NetworkScale;

                targetToy.NetworkScale = scale;
            }
        }
    }
}