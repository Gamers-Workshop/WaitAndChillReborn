namespace WaitAndChillReborn.Patches
{
    using Exiled.API.Features;
    using HarmonyLib;
    using Interactables.Interobjects;
    using Interactables.Interobjects.DoorUtils;

    [HarmonyPatch(typeof(BreakableDoor), nameof(BreakableDoor.ServerDamage))]
    internal static class DoorDamagePatch
    {
        private static bool Prefix(BreakableDoor __instance, float hp, DoorDamageType type) =>
            !(Round.IsLobby && type is not DoorDamageType.ServerCommand);
    }
}
