# Feature 1: Tool (Energy Scanner) — Implementation Plan

Home Energy Debugger, SIT283 Assessment 2. Covers rubric criterion 3 (Tool) and is a
prerequisite for criterion 2 (Sequence of States) and criterion 4 (Media).

## Why this pattern

Reviewed the weekly workshop projects in `C:\Code\VR` to keep this project's code
consistent with what the unit has already taught, so implementations don't look like
they came from a different course.

The closest match is `Week 5 Scalpel/Assets/Scripts/ScalpelTool.cs` and its target-side
counterpart `CuttableObject.cs`:

- `ScalpelTool.cs`: hand-held tool with `[RequireComponent(typeof(XRGrabInteractable))]`,
  caches the interactable in `Awake`, subscribes to `selectEntered` / `selectExited` /
  `activated` in `OnEnable` (unsubscribes in `OnDisable`), and raycasts forward from a
  tip Transform against a `LayerMask` to find a target.
- `CuttableObject.cs`: lives on the target object. The tool finds the target and calls
  a public method on it (`TryCut`); the target owns the reaction to being interacted
  with, not the tool.
- `Week 1 LTS/ItemXRGrabHandler.cs` confirms the same house style for simpler grab
  logic: cache in `Awake`, subscribe in `OnEnable`, unsubscribe in `OnDisable`.

Namespace note: this project is on XRI 3.0.11, so grab/interactable types come from
`UnityEngine.XR.Interaction.Toolkit.Interactables`, a separate namespace from the base
`UnityEngine.XR.Interaction.Toolkit` — matches what these weekly scripts already import,
but differs from a lot of older XRI tutorials online.

The energy scanner reuses this exact split: a tool-side script (`EnergyScannerTool`)
that raycasts and delegates, and a target-side script (`ScannableAppliance`) that owns
its own reading and, later, its own state machine.

## Steps

1. **Add an "Appliances" physics layer.**
   Edit > Project Settings > Tags and Layers. Mirrors Scalpel's `cuttableLayers` field —
   the scanner should only ever hit appliances, not walls or the floor.

2. **Place a placeholder Fridge object** in the Kitchen room.
   A primitive cube is fine for now — don't wait on a modeled fridge, Scalpel's own
   test objects were primitives too. Give it a Box Collider and set its layer to
   `Appliances`.

3. **Write `ScannableAppliance.cs`** in `Assets/Scripts/Appliances`.
   Target-side component, same role as `CuttableObject`. For this feature it only
   needs:
   - An appliance name (string).
   - A simulated power-draw value (float, serialized so it can be tuned per appliance).
   - A public `Scan()` method that returns the reading.
   - A comment marking where the state machine hooks in for the next feature.

4. **Build a placeholder scanner tool object.**
   A primitive (cylinder works) with a Rigidbody and an `XR Grab Interactable`
   component, plus a child empty Transform named `ScanTip` at the front, facing
   forward — equivalent to Scalpel's `bladeTip`.

5. **Write `EnergyScannerTool.cs`** in `Assets/Scripts/Interaction`.
   Structured like `ScalpelTool.cs`:
   - `[RequireComponent(typeof(XRGrabInteractable))]`
   - Cache the interactable in `Awake`.
   - Subscribe to `selectEntered` / `selectExited` / `activated` in `OnEnable`,
     unsubscribe in `OnDisable`.
   - `FindTarget()`: raycast from `scanTip.forward`, out to `scanRange`, filtered by
     the `Appliances` layer mask.
   - On `activated` (trigger press), call `target.Scan()` on whatever the raycast hits.

6. **Wire it up in the Inspector.**
   Drag `ScanTip` into the script's field. Set Scan Range (~5m). Set Scannable Layers
   to `Appliances` only.

7. **Test with the XR Device Simulator.**
   Grab the scanner, point it at the fridge, pull the trigger. Confirm the Console logs
   a reading — same verification approach Scalpel uses (`logCuts` bool) before any
   visuals exist.

8. **Capture evidence.**
   Screenshot it working in Play mode, and write the 2–3 sentences for report section
   2.3 (Tool) while it's fresh. The "Research conducted" line can honestly reference
   that this mirrors the raycast-and-delegate pattern from the Week 5 Scalpel exercise.

## Files this creates

- `Assets/Scripts/Appliances/ScannableAppliance.cs`
- `Assets/Scripts/Interaction/EnergyScannerTool.cs`

## Next feature this unlocks

Criterion 2 (Sequence of States): `ScannableAppliance.Scan()` becomes the trigger that
advances a state machine — Idle → Scanning → Leak Identified → Upgrade Applied →
Result — living inside `ScannableAppliance` itself, same way `CuttableObject` owns the
cut logic that `ScalpelTool` triggers.
