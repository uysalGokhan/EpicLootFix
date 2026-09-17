using System;
using HarmonyLib;

namespace EpicLootFix
{
    /// <summary>
    /// Harmony prefix for ZInput.IsGamepadActive(). See README for the full diagnosis.
    /// Currently instrumented with a call counter while we track down why forcing this
    /// to false unconditionally hasn't stopped the GetJoyRightStickY crash in testing.
    /// </summary>
    internal static class GamepadActiveScopeGuard
    {
        private static long _callCount;

        public static bool Prefix(ref bool __result)
        {
            long count = System.Threading.Interlocked.Increment(ref _callCount);
            if (count <= 5 || count % 200 == 0)
            {
                Plugin.Log?.LogInfo($"[EpicLootFix] IsGamepadActive prefix hit #{count}, forcing false.");
            }

            __result = false;
            return false;
        }
    }
}
