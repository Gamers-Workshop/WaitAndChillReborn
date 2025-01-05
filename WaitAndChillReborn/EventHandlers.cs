namespace WaitAndChillReborn
{
    using System.Collections.Generic;
    using Configs;
    using CustomPlayerEffects;
    using Exiled.API.Enums;
    using Exiled.API.Extensions;
    using Exiled.API.Features.Pickups;
    using Exiled.API.Features.Roles;
    using Exiled.Events.EventArgs.Interfaces;
    using Exiled.Events.EventArgs.Player;
    using GameCore;
    using InventorySystem.Items.Pickups;
    using MEC;
    using UnityEngine;
    using static API.API;
    using ItemEvent = Exiled.Events.Handlers.Item;
    using PlayerEvent = Exiled.Events.Handlers.Player;
    using ServerEvent = Exiled.Events.Handlers.Server;
    using MapEvent = Exiled.Events.Handlers.Map;
    using Server = Exiled.API.Features.Server;
    using Exiled.Events.EventArgs.Item;
    using System.Linq;
    using Exiled.API.Features;
    using Exiled.Events.EventArgs.Map;

    internal static class EventHandlers
    {
        internal static void RegisterEvents()
        {
            MapEvent.Generated += OnWaitingForPlayers;
            ItemEvent.ChangingAmmo += OnChangingAmmo;

            PlayerEvent.Verified += OnVerified;
            PlayerEvent.Destroying += OnDestroying;
            PlayerEvent.Spawned += OnSpawned;
            PlayerEvent.Dying += OnDying;
            PlayerEvent.Died += OnDied;

            MapEvent.PlacingBulletHole += OnDeniableEvent;
            // MapEvent.PlacingBlood += OnDeniableEvent;
            PlayerEvent.SpawningRagdoll += OnDeniableEvent;
            PlayerEvent.IntercomSpeaking += OnDeniableEvent;
            PlayerEvent.InteractingDoor += OnDeniableEvent;
            PlayerEvent.InteractingElevator += OnDeniableEvent;
            PlayerEvent.InteractingLocker += OnDeniableEvent;
            PlayerEvent.DamagingWindow += OnDeniableEvent;
            MapEvent.ChangingIntoGrenade += OnDeniableEvent;

            ServerEvent.RoundStarted += OnRoundStarted;
        }

        internal static void UnRegisterEvents()
        {
            MapEvent.Generated -= OnWaitingForPlayers;

            ItemEvent.ChangingAmmo -= OnChangingAmmo;
            PlayerEvent.Verified -= OnVerified;
            PlayerEvent.Destroying -= OnDestroying;
            PlayerEvent.Spawned -= OnSpawned;
            PlayerEvent.Dying -= OnDying;
            PlayerEvent.Died -= OnDied;

            MapEvent.PlacingBulletHole -= OnDeniableEvent;
            // MapEvent.PlacingBlood -= OnDeniableEvent;
            PlayerEvent.SpawningRagdoll -= OnDeniableEvent;
            PlayerEvent.IntercomSpeaking -= OnDeniableEvent;
            PlayerEvent.DroppingItem -= OnDeniableEvent;
            PlayerEvent.DroppingAmmo -= OnDeniableEvent;
            PlayerEvent.InteractingDoor -= OnDeniableEvent;
            PlayerEvent.InteractingElevator -= OnDeniableEvent;
            PlayerEvent.InteractingLocker -= OnDeniableEvent;
            PlayerEvent.DamagingWindow -= OnDeniableEvent;
            MapEvent.ChangingIntoGrenade -= OnDeniableEvent;

            ServerEvent.RoundStarted -= OnRoundStarted;
        } 
        private static void OnDestropAdded(PickupAddedEventArgs pickup)
        {
            PickupsToDestroy.Add(pickup.Pickup);
        }
        private static void OnPickupDestroyed(PickupDestroyedEventArgs pickup)
        {
            PickupsToDestroy.Remove(pickup.Pickup);
        }

        private static void OnWaitingForPlayers()
        {
            if (!WaitAndChillReborn.Singleton.Config.DisplayWaitingForPlayersScreen)
                GameObject.Find("StartRound").transform.localScale = Vector3.zero;

            if (LobbyTimer.IsRunning)
                Timing.KillCoroutines(LobbyTimer);

            if (Server.FriendlyFire)
                FriendlyFireConfig.PauseDetector = true;

            if (WaitAndChillReborn.Singleton.Config.DisplayWaitMessage)
                LobbyTimer = Timing.RunCoroutine(Methods.LobbyTimer());

            Scp173Role.TurnedPlayers.Clear();
            Scp096Role.TurnedPlayers.Clear();

            Methods.SetupAvailablePositions();

            LockedPickups.Clear();
            PickupsToDestroy.Clear();
            foreach (Pickup pickup in Pickup.List)
            {
                try
                {
                    if (!pickup.IsLocked)
                    {
                        PickupSyncInfo info = pickup.Base.NetworkInfo;
                        info.Locked = true;
                        pickup.Base.NetworkInfo = info;

                        pickup.Base.GetComponent<Rigidbody>().isKinematic = true;
                        LockedPickups.Add(pickup);
                    }
                }
                catch (System.Exception)
                {
                    // ignored
                }
            }

            MapEvent.PickupAdded += OnDestropAdded;
            MapEvent.PickupDestroyed += OnPickupDestroyed;
        }
        private static void OnChangingAmmo(ChangingAmmoEventArgs ev)
        {
            if (!Round.IsLobby)
                return;
            if (ev.NewAmmo < ev.OldAmmo)
                ev.IsAllowed = false;
        }
        private static void OnVerified(VerifiedEventArgs ev)
        {
            if (!Round.IsLobby)
                return;

            if (RoundStart.singleton.NetworkTimer > 1 || RoundStart.singleton.NetworkTimer == -2)
            {
                ev.Player.Role.Set(Config.RolesToChoose[Random.Range(0, Config.RolesToChoose.Count)]);

                if (Config.TurnedPlayers)
                {
                    Scp096Role.TurnedPlayers.Add(ev.Player);
                    Scp173Role.TurnedPlayers.Add(ev.Player);
                }
            }
        }
        private static void OnDestroying(DestroyingEventArgs ev)
        {
            if (!Round.IsLobby)
                return;

            foreach (Pickup pickup in PickupsToDestroy.ToList())
                if (pickup.PreviousOwner == ev.Player)
                    pickup.Destroy();
        }
        private static void OnSpawned(SpawnedEventArgs ev)
        {
            if (!Round.IsLobby)
                return;

            if (RoundStart.singleton.NetworkTimer <= 1 && RoundStart.singleton.NetworkTimer != -2)
                return;

            ev.Player.Position = Config.MultipleRooms ? LobbyAvailableSpawnPoints[Random.Range(0, LobbyAvailableSpawnPoints.Count)] : LobbyChoosedSpawnPoint;

            Exiled.CustomItems.API.Extensions.ResetInventory(ev.Player, Config.Inventory);

            foreach (KeyValuePair<AmmoType, ushort> ammo in Config.Ammo)
                ev.Player.Ammo[ammo.Key.GetItemType()] = ammo.Value;

            foreach (KeyValuePair<EffectType, byte> effect in Config.LobbyEffects)
            {
                if (!ev.Player.TryGetEffect(effect.Key, out StatusEffectBase? effectBase))
                    continue;

                effectBase.ServerSetState(effect.Value);
            }
        }

        private static void OnDeniableEvent(IDeniableEvent ev)
        {
            if (Round.IsLobby)
                ev.IsAllowed = false;
        }

        private static void OnDying(IPlayerEvent ev)
        {
            if (Round.IsLobby)
                ev.Player.ClearInventory();
        }

        private static void OnDied(DiedEventArgs ev)
        {
            if (!Round.IsLobby || (RoundStart.singleton.NetworkTimer <= 1 && RoundStart.singleton.NetworkTimer != -2))
                return;

            ev.Player.Role.Set(Config.RolesToChoose[Random.Range(0, Config.RolesToChoose.Count)]);

            ev.Player.Position = Config.MultipleRooms ? LobbyAvailableSpawnPoints[Random.Range(0, LobbyAvailableSpawnPoints.Count)] : LobbyChoosedSpawnPoint;

            foreach (KeyValuePair<EffectType, byte> effect in Config.LobbyEffects)
            {
                ev.Player.EnableEffect(effect.Key);
                ev.Player.ChangeEffectIntensity(effect.Key, effect.Value);
            }

            Exiled.CustomItems.API.Extensions.ResetInventory(ev.Player, Config.Inventory);

            foreach (KeyValuePair<AmmoType, ushort> ammo in Config.Ammo)
                ev.Player.Ammo[ammo.Key.GetItemType()] = ammo.Value;
        }

        private static void OnRoundStarted()
        {
            foreach (Pickup pickup in Pickup.List.ToList())
            {
                if (PickupsToDestroy.Contains(pickup))
                {
                    pickup.Destroy();
                    continue;
                }
                if (LockedPickups.Contains(pickup))
                    pickup.Base.GetComponent<Rigidbody>().isKinematic = false;
            }

            if (Config.TurnedPlayers)
            {
                Scp096Role.TurnedPlayers.Clear();
                Scp173Role.TurnedPlayers.Clear();
            }

            if (Server.FriendlyFire)
                FriendlyFireConfig.PauseDetector = false;

            if (LobbyTimer.IsRunning)
                Timing.KillCoroutines(LobbyTimer);

            foreach (Pickup pickup in LockedPickups)
            {
                try
                {
                    PickupSyncInfo info = pickup.Base.NetworkInfo;
                    info.Locked = false;
                    pickup.Base.NetworkInfo = info;

                    pickup.Base.GetComponent<Rigidbody>().isKinematic = false;
                }
                catch (System.Exception)
                {
                    // ignored
                }
            }

            LockedPickups.Clear();
            PickupsToDestroy.Clear();
            MapEvent.PickupAdded -= OnDestropAdded;
            MapEvent.PickupDestroyed -= OnPickupDestroyed;
        }

        private static readonly HashSet<Pickup> LockedPickups = new();
        private static readonly HashSet<Pickup> PickupsToDestroy = new();
        private static readonly LobbyConfig Config = WaitAndChillReborn.Singleton.Config.LobbyConfig;
    }
}