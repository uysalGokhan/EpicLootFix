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

            Log.LogInfo("[EpicLootFix] Gamepad-detection scope guard applied to ZInput.IsGamepadActive " +
                        "(forces false only for EpicLoot's own calls).");
        }
    }
}
