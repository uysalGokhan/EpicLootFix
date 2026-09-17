# EpicLootFix

Unofficial third-party stopgap patch for [EpicLoot](https://thunderstore.io/c/valheim/p/RandyKnapp/EpicLoot/)
(by RandyKnapp), for a bug introduced by Valheim patch **1.0.14** (2026-09-17).

## What this fixes

Since 1.0.14, EpicLoot's Enchanting Table UI (Augment and Enchant panels) can
get permanently stuck "processing" after you click Augment or Enchant - the
panel never unlocks, and in some cases can't even be cancelled/closed.

## Root cause

1.0.14 reworked stick input pre-processing and gamepad keybinding handling.
Since then, `ZInput.IsGamepadActive()` spuriously returns `true` even with no
real gamepad/joystick attached (confirmed via Windows device enumeration on
the machine this was diagnosed on - the only matching HID device was an
unrelated Logitech keyboard collection, not a controller).

Four methods in EpicLoot's own code - `AugmentUI.Update`, `EnchantUI.Update`,
`AugmentChoiceDialog.Update`, and `CraftSuccessDialog.Update` - each gate a
block of gamepad-only navigation behind `if (... ZInput.IsGamepadActive())`
and, inside that block, call `ZInput.GetJoyRightStickY()`. That exact
overload no longer exists post-1.0.14:

```
MissingMethodException: Method not found: single .ZInput.GetJoyRightStickY(bool)
```

Since `IsGamepadActive()` now (incorrectly) returns `true` every frame, that
call throws every frame. The exception unwinds the whole `Update()` method,
so none of those four methods ever reach the code *after* the gamepad block -
the code that closes dialogs, unlocks the UI, and finalizes the augment/
enchant. Result: the panel is stuck "processing" forever.

## The fix

Rather than reimplementing each of those four methods' post-gamepad-block
logic (fragile, easy to get subtly wrong), this patches the root of what
actually reaches EpicLoot's code: a Harmony prefix on `ZInput.IsGamepadActive()`
that forces the result to `false`, but **only for calls originating from
EpicLoot's own assembly** (checked via the immediate caller's declaring
type's assembly name). Every one of the four methods then takes the exact
same code path they already use correctly for keyboard/mouse players - no
EpicLoot behavior is reimplemented, this just supplies the correct answer to
the one question their code is currently getting wrong post-1.0.14. Real
gamepad detection for vanilla Valheim and every other mod is untouched.

**Trade-off:** this disables EpicLoot's own gamepad stick-navigation inside
the Enchanting Table UI specifically (which was working before 1.0.14) as
the cost of not being permanently stuck. Keyboard/mouse use of the same UI
is unaffected either way.

## Status

This is a stopgap. Once EpicLoot ships a build compiled against the current
`ZInput`, or Iron Gate fixes whatever this `IsGamepadActive()` regression is,
this patch becomes a no-op for EpicLoot's gamepad support and should be
removed.

## Requirements

- [EpicLoot](https://thunderstore.io/c/valheim/p/RandyKnapp/EpicLoot/) must
  be installed and enabled (hard dependency - this plugin won't load
  without it).

## Installing

Manual install only (not published on Thunderstore):

1. Build `EpicLootFix.dll` (see below), or grab it from a release.
2. Drop it into `BepInEx/plugins/EpicLootFix/EpicLootFix.dll` in your
   Valheim profile.
3. Launch the game. You should see in `BepInEx/LogOutput.log`:
   ```
   [Info :EpicLootFix] Gamepad-detection scope guard applied to
   ZInput.IsGamepadActive (forces false only for EpicLoot's own calls).
   ```

## Building

Requires the .NET SDK (net472 target via
`Microsoft.NETFramework.ReferenceAssemblies`).

The `.csproj` locates BepInEx/Harmony/UnityEngine/game reference DLLs via
the `ValheimProfileBepInEx` and `ValheimManaged` MSBuild properties at the
top of `EpicLootFix.csproj` - edit those paths if your Valheim/profile
install lives somewhere else. Note `ZInput` lives in `assembly_utils.dll`
(one of the split assemblies Valheim 1.0 replaced the old monolithic
`Assembly-CSharp.dll` with), not `assembly_valheim.dll`.

```
dotnet build -c Release
```

Output: `bin/Release/EpicLootFix.dll`.
