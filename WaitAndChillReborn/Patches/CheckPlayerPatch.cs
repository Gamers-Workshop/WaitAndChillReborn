namespace WaitAndChillReborn.Patches
{
    using HarmonyLib;
    using PlayerRoles;
    using PlayerRoles.RoleAssign;

    [HarmonyPatch(typeof(RoleAssigner), nameof(RoleAssigner.CheckPlayer))]
    public static class CheckPlayerPatch
    {
        private static bool Prefix(ReferenceHub hub, ref bool __result)
        {
            if (WaitAndChillReborn.Singleton.Config.LobbyConfig.RolesToChoose.Contains(hub.roleManager.CurrentRole.RoleTypeId))
            {
                __result = true;
                return false;
            }
            return true;
        }
    }
}