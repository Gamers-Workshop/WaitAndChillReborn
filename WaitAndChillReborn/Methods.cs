﻿namespace WaitAndChillReborn
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Linq.Expressions;
    using System.Text;
    using Configs;
    using Exiled.API.Enums;
    using Exiled.API.Extensions;
    using Exiled.API.Features;
    using Exiled.API.Features.Doors;
    using Interactables.Interobjects.DoorUtils;
    using MapGeneration.Distributors;
    using MEC;
    using PlayerRoles;
    using PlayerRoles.FirstPersonControl;
    using PlayerRoles.RoleAssign;
    using UnityEngine;
    using static API.API;
    
    internal static class Methods
    {
        internal static IEnumerator<float> LobbyTimer()
        {
            while (!Round.IsStarted)
            {
                StringBuilder stringBuilder = NorthwoodLib.Pools.StringBuilderPool.Shared.Rent();

                if (WaitAndChillReborn.Singleton.Config.HintVertPos != 0 && WaitAndChillReborn.Singleton.Config.HintVertPos < 0)
                    for (int i = WaitAndChillReborn.Singleton.Config.HintVertPos; i < 0; i++)
                        stringBuilder.Append("\n");

                stringBuilder.Append(Translation.TopMessage);
                stringBuilder.Append($"\n{Translation.BottomMessage}");

                short networkTimer = GameCore.RoundStart.singleton.NetworkTimer;
                int numberOfPlayer = Player.List.Count(x => RoleAssigner.CheckPlayer(x.ReferenceHub));
                switch (networkTimer)
                {
                    case -2: 
                        stringBuilder.Replace("{seconds}", Translation.ServerIsPaused); 
                        break;
                    case -1 or 0:
                        stringBuilder.Replace("{seconds}", Translation.RoundIsBeingStarted); 
                        break;
                    case 1: 
                        stringBuilder.Replace("{seconds}", $"{networkTimer} {Translation.OneSecondRemain}"); 
                        break;
                    default: 
                        stringBuilder.Replace("{seconds}", $"{networkTimer} {Translation.XSecondsRemains}"); 
                        break;
                }

                stringBuilder.Replace("{players}", numberOfPlayer is <= 1 ? $"{numberOfPlayer} {Translation.OnePlayerConnected}" : $"{numberOfPlayer} {Translation.XPlayersConnected}");

                if (WaitAndChillReborn.Singleton.Config.HintVertPos != 0 && WaitAndChillReborn.Singleton.Config.HintVertPos > 0)
                    for (int i = 0; i < WaitAndChillReborn.Singleton.Config.HintVertPos; i++)
                        stringBuilder.Append("\n");

                string text = NorthwoodLib.Pools.StringBuilderPool.Shared.ToStringReturn(stringBuilder);
                
                foreach (Player player in Player.List)
                {
                    if (WaitAndChillReborn.Singleton.Config.UseHints)
                        player.SessionVariables["hint_bottom"] = new Tuple<string, float>(text, 2f);
                    else
                        player.Broadcast(1, text);
                }

                yield return Timing.WaitForSeconds(1f);
            }
        }

        internal static void SetupAvailablePositions()
        {
            LobbyAvailableSpawnPoints.Clear();

            for (int i = 0; i < Config.LobbyRoom.Count; i++)
                Config.LobbyRoom[i] = Config.LobbyRoom[i].ToUpper();

            if (Config.LobbyRoom.Contains("TOWER1")) LobbyAvailableSpawnPoints.Add(new Vector3(39.150f, 1014.112f, -31.818f));
            if (Config.LobbyRoom.Contains("TOWER2")) LobbyAvailableSpawnPoints.Add(new Vector3(162.125f, 1019.440f, -13f));
            if (Config.LobbyRoom.Contains("TOWER3")) LobbyAvailableSpawnPoints.Add(new Vector3(108.3f, 1048.048f, -14.075f));
            if (Config.LobbyRoom.Contains("TOWER4")) LobbyAvailableSpawnPoints.Add(new Vector3(-15.105f, 1014.461f, -31.797f));
            if (Config.LobbyRoom.Contains("TOWER5")) LobbyAvailableSpawnPoints.Add(new Vector3(44.137f, 1013.065f, -50.931f));
            if (Config.LobbyRoom.Contains("NUKE_SURFACE")) LobbyAvailableSpawnPoints.Add(new Vector3(29.69f, 991.86f, -26.7f));

            if (Config.LobbyRoom.Contains("WC"))
                foreach (Transform transform in ItemSpawnpoint.RandomInstances.First(x => x.name == "Random Keycard")._positionVariants)
                    LobbyAvailableSpawnPoints.Add(transform.position + Vector3.up);

            if (Config.LobbyRoom.Contains("GR18"))
                LobbyAvailableSpawnPoints.Add(ItemSpawnpoint.RandomInstances.First(x => x.name == "COM-15" && x.TriggerDoorName == "GR18")._positionVariants.First().position + Vector3.up);

            Dictionary<RoomType, string> roomToString = new()
            {
                { RoomType.EzShelter, "SHELTER" },
                { RoomType.EzGateA, "GATE_A" },
                { RoomType.EzGateB, "GATE_B" },
            };

            foreach (Room room in Room.List)
            {
                if (roomToString.ContainsKey(room.Type) && Config.LobbyRoom.Contains(roomToString[room.Type]))
                {
                    Vector3 roomPos = room.transform.position;
                    LobbyAvailableSpawnPoints.Add(new Vector3(roomPos.x, roomPos.y + 2f, roomPos.z));
                }
            }

            if (Config.LobbyRoom.Contains("INTERCOM"))
            {
                Transform transform = Intercom.IntercomDisplay.transform;
                LobbyAvailableSpawnPoints.Add(transform.position + transform.forward * 3f);
            }

            if (Config.LobbyRoom.Contains("079"))
            {
                Door Scp079Second = Door.Get(DoorType.Scp079Second);
                Vector3 secondDoorPos = Scp079Second.Transform.position + Vector3.up;
                secondDoorPos += Scp079Second.Transform.forward * 2;
                LobbyAvailableSpawnPoints.Add(secondDoorPos);
            }

            if (Config.LobbyRoom.Contains("096"))
                LobbyAvailableSpawnPoints.Add(ItemSpawnpoint.AutospawnInstances.First(x => x.AutospawnItem == ItemType.KeycardMTFOperative).transform.position + Vector3.up);

            Dictionary<string, RoleTypeId> stringToRole = new()
            {
                { "049", RoleTypeId.Scp049 },
                { "106", RoleTypeId.Scp106 },
                { "173", RoleTypeId.Scp173 },
                { "939", RoleTypeId.Scp939 },
            };

            foreach (KeyValuePair<string, RoleTypeId> role in stringToRole)
                if (Config.LobbyRoom.Contains(role.Key))
                    LobbyAvailableSpawnPoints.Add(role.Value.GetRandomSpawnLocation().Position);

            foreach (Vector3 position in Config.StaticLobbyPositions)
            {
                if (position == -Vector3.one)
                    continue;

                LobbyAvailableSpawnPoints.Add(position);
            }

            LobbyChoosedSpawnPoint = LobbyAvailableSpawnPoints[UnityEngine.Random.Range(0, LobbyAvailableSpawnPoints.Count)];
        }

        private static readonly Translation Translation = WaitAndChillReborn.Singleton.Translation;
        private static readonly LobbyConfig Config = WaitAndChillReborn.Singleton.Config.LobbyConfig;
    }
}

