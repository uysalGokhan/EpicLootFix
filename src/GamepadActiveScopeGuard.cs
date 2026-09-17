using System;
using System.Diagnostics;
using System.Reflection;

namespace EpicLootFix
{
    /// <summary>
    /// Harmony prefix for ZInput.IsGamepadActive().
    ///
    /// Root cause (Valheim patch 1.0.14, 2026-09-17): the update reworked stick input
    /// pre-processing and gamepad keybinding handling. Since then, ZInput.IsGamepadActive()
    /// spuriously returns true on this machine even with no real gamepad/joystick attached
    /// (confirmed via Windows device enumeration - only an unrelated Logitech keyboard HID
    /// collection, no actual controller). Whatever changed evidently affects more than just
    /// the method EpicLoot happens to call afterward.
    ///
    /// EpicLoot's Enchanting/Augmenting UI (AugmentUI.Update, EnchantUI.Update,
    /// AugmentChoiceDialog.Update, CraftSuccessDialog.Update - all in the EpicLoot assembly)
    /// each gate a block of gamepad-only navigation code behind
    /// "if (... ZInput.IsGamepadActive())" and, inside that block, call
    /// ZInput.GetJoyRightStickY(). That exact overload no longer exists post-1.0.14
    /// ("MissingMethodException: Method not found: single .ZInput.GetJoyRightStickY(bool)"),
    /// and since IsGamepadActive() now (incorrectly) returns true every frame, that call
    /// throws every frame. The exception unwinds the whole Update() method, so none of
    /// those methods ever reach the code after the gamepad block - the code that closes
    /// dialogs, unlocks the UI, and finalizes the augment/enchant. Result: the Enchant/
    /// Augment panel is stuck "processing" forever, un-clickable, un-cancelable.
    ///
    /// Rather than reimplementing each of those four methods' post-gamepad-block logic
    /// (fragile, easy to get subtly wrong), this patches the root cause of what actually
    /// reaches EpicLoot's code: force IsGamepadActive() to return false, but ONLY for
    /// calls originating from EpicLoot's own assembly. Every one of those four methods
    /// then takes the exact same code path they already use correctly for keyboard/mouse
    /// players - no EpicLoot behavior is reimplemented, we just supply the correct answer
    /// to the one question their code is currently getting wrong. Real gamepad detection
    /// for vanilla Valheim and every other mod is untouched.
    ///
    /// This is a stopgap: once EpicLoot ships a build compiled against the current
    /// ZInput, or Iron Gate reverts/fixes whatever IsGamepadActive() regression this is,
    /// this patch becomes a no-op for EpicLoot's own gamepad support (which was working
    /// before 1.0.14) and should be removed.
    /// </summary>
    internal static class GamepadActiveScopeGuard
    {
        private const string EpicLootAssemblyName = "EpicLoot";

        // How many frames up to check. Harmony's patch trampoline can insert one or more
        // of its own frames between this Prefix and the actual EpicLoot call site, so a
        // single fixed frame index isn't reliable - walk a small window instead.
        private const int MaxFramesToCheck = 8;

        public static bool Prefix(ref bool __result)
        {
            try
            {
                var trace = new StackTrace(1, false);
                int depth = Math.Min(trace.FrameCount, MaxFramesToCheck);
                for (int i = 0; i < depth; i++)
                {
                    var declaringType = trace.GetFrame(i)?.GetMethod()?.DeclaringType;
                    if (declaringType != null && declaringType.Assembly.GetName().Name == EpicLootAssemblyName)
                    {
                        __result = false;
                        return false; // we've supplied the answer - skip the (currently buggy-for-us) original
                    }
                }
            }
            catch (Exception ex)
            {
                Plugin.Log?.LogWarning($"[EpicLootFix] Caller-assembly check failed, falling back to original: {ex.Message}");
            }

            return true; // not an EpicLoot call - let the real check run, untouched, for everyone else
        }
    }
}
