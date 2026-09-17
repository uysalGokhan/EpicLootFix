using HarmonyLib;

namespace EpicLootFix
{
    /// <summary>
    /// Harmony prefix for ZInput.IsGamepadActive().
    ///
    /// Root cause (Valheim patch 1.0.14, 2026-09-17): the update reworked stick input
    /// pre-processing and gamepad keybinding handling. Since then, ZInput.IsGamepadActive()
    /// spuriously returns true on this machine even with no real gamepad/joystick attached
    /// (confirmed via Windows device enumeration - the only matching HID device was an
    /// unrelated Logitech keyboard collection, no actual controller present).
    ///
    /// EpicLoot's Enchanting/Augmenting UI (AugmentUI.Update, EnchantUI.Update,
    /// AugmentChoiceDialog.Update, CraftSuccessDialog.Update) each gate a block of
    /// gamepad-only navigation behind "if (... ZInput.IsGamepadActive())" and, inside
    /// that block, call ZInput.GetJoyRightStickY(bool) - an overload that no longer
    /// exists post-1.0.14 (the current signature is parameterless: GetJoyRightStickY()).
    /// Confirmed via reflection against the live assembly_utils.dll. Since
    /// IsGamepadActive() now (incorrectly) returns true every frame, that call throws
    /// every frame, unwinding the whole Update() method before it ever reaches the code
    /// that unlocks the UI and finalizes the augment/enchant - the panel gets stuck
    /// "processing" forever.
    ///
    /// First attempt scoped this fix to only affect calls originating from EpicLoot's
    /// own assembly, detected via walking the managed call stack from inside this
    /// prefix. That didn't hold up in testing - the exception kept firing at roughly
    /// the same rate with the scoped version installed, meaning the stack walk wasn't
    /// reliably reaching a frame whose declaring type's assembly is EpicLoot (Harmony's
    /// patch trampoline apparently doesn't preserve a walkable managed frame here the
    /// way a normal call does).
    ///
    /// Given we've confirmed there's no real gamepad on this machine, the simpler and
    /// actually-effective fix is unconditional: IsGamepadActive() should be returning
    /// false already, for everyone, all the time, on this machine - that's not an
    /// EpicLoot-specific hack, it's what the vanilla check is supposed to produce here.
    /// Forcing it removes gamepad detection machine-wide (not just for EpicLoot) for as
    /// long as this patch is active, which has no practical cost given there's nothing
    /// to detect - and it closes every current and future call site that depends on
    /// this check being correct, not just the four we found.
    ///
    /// This is a stopgap: once EpicLoot ships a build compiled against the current
    /// ZInput, or Iron Gate fixes whatever this IsGamepadActive() regression is, this
    /// patch should be removed (or re-scoped, if a real gamepad is ever plugged in on
    /// this machine and its detection needs to work again).
    /// </summary>
    internal static class GamepadActiveScopeGuard
    {
        public static bool Prefix(ref bool __result)
        {
            __result = false;
            return false;
        }
    }
}
