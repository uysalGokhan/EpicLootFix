using System.Reflection;
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;

namespace EpicLootFix
{
    [BepInPlugin(PluginGuid, PluginName, PluginVersion)]
    [BepInDependency(EpicLootGuid)]
    public class Plugin : BaseUnityPlugin
    {
        public const string PluginGuid = "com.uysalgokhan.epiclootfix";
        public const string PluginName = "EpicLootFix";
        public const string PluginVersion = "1.0.0";

        // GUID EpicLoot registers itself under (from EpicLoot.cs's own [BepInPlugin] attribute).
        private const string EpicLootGuid = "randyknapp.mods.epicloot";

        internal static ManualLogSource Log;

        private Harmony _harmony;

        private void Awake()
        {
            Log = Logger;
            _harmony = new Harmony(PluginGuid);

            var targetMethod = AccessTools.Method(typeof(ZInput), nameof(ZInput.IsGamepadActive));
            if (targetMethod == null)
            {
                Log.LogError("[EpicLootFix] Could not find ZInput.IsGamepadActive - Valheim may have renamed/removed it. Patch NOT applied.");
                return;
            }

            var prefix = new HarmonyMethod(typeof(GamepadActiveScopeGuard).GetMethod(nameof(GamepadActiveScopeGuard.Prefix)));
            _harmony.Patch(targetMethod, prefix);

            Log.LogInfo("[EpicLootFix] ZInput.IsGamepadActive forced to false machine-wide " +
                        "(no real gamepad detected on this machine - works around the post-1.0.14 " +
                        "GetJoyRightStickY crash in EpicLoot's Enchanting UI).");
        }
    }
}
