namespace WaitAndChillReborn
{
    using System;
    using Exiled.API.Features;
    using HarmonyLib;
    using Configs;
    using Config = Configs.Config;
    using Exiled.API.Enums;

    public class WaitAndChillReborn : Plugin<Config, Translation>
    {
        public static WaitAndChillReborn Singleton;

        private Harmony _harmony;
        public override PluginPriority Priority { get; } = PluginPriority.Higher;
        public override void OnEnabled()
        {
            Singleton = this;
            
            EventHandlers.RegisterEvents();
            
            _harmony = new Harmony($"michal78900.wacr-{DateTime.Now.Ticks}");
            _harmony.PatchAll();

            base.OnEnabled();
        }

        public override void OnDisabled()
        {
            EventHandlers.UnRegisterEvents();

            Singleton = null;

            base.OnDisabled();
        }

        public override string Name { get; } = "WaitAndChillReborn";
        public override string Author { get; } = "Michal78900";
        public override Version Version { get; } = new(5, 0, 1);
    }
}
