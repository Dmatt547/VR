# Challenge02 – Bead Sculpting VR — Setup Guide

Scripts are in `Assets/Scripts/`: `BeadGun.cs`, `Bead.cs`, `ColorSelector.cs`.

## Your project already has (copied from Week 1)

- XR packages installed: XR Interaction Toolkit 3.0.11, XR Plugin
  Management 4.7.0, OpenXR 1.16.1, Input System — nothing to install.
- An **XR Origin (VR)** rig, **XR Interaction Manager**, and a
  **Teleportation Area** already in `SampleScene`. Steps a and b from the
  workshop sheet are done.
- Leftover Week 1 content you don't need for this challenge: 3x
  `Conveyor Belt` objects, `Roller 1/2/3`, `Spawn Point`, the
  `Belt.prefab` / `Item.prefab` prefabs, and the scripts `ConveyorBelt.cs`,
  `ItemSpawner.cs`, `ItemXRGrabHandler.cs`. There's also an unused
  `Assets/Photon` folder (Fusion multiplayer) — nothing in this project
  actually calls it, so it's just dead weight, safe to ignore or delete
  later if you want a smaller project.

## Recommended: build in a fresh scene, don't gut SampleScene

Keep `SampleScene` as your Week 1 record. Duplicate it instead of editing
in place:

1. In the Project window, select `Assets/Scenes/SampleScene`, press
   Ctrl+D, rename the copy `BeadSculpting`.
2. Open `BeadSculpting.unity`.
3. Delete the Week 1-only objects from the hierarchy: the 3
   `Conveyor Belt` objects (and their Roller children), `Spawn Point`.
   Leave `XR Origin (VR)`, `XR Interaction Manager`, `Teleportation Area`,
   `Directional Light`, `Global Volume`, `Main Camera` (if present) —
   those are reusable.
4. File > Build Settings, make sure `BeadSculpting` is in the build
   scene list if you'll be building to a headset.

## c. Bead gun object

1. Create an empty GameObject named `BeadGun` in the new scene. Group its
   visual parts (barrel, handle) as children — stretched cubes/cylinders
   are fine.
2. Add a **Rigidbody** and a **Collider** wrapping the gun.
3. Tag it `BeadGun` (Add Tag if it doesn't exist).
4. Add **XR Grab Interactable**.
   - **Movement Type** → `Kinematic`.
   - **Attach Transform** → an empty child positioned like a hand grip
     (optional but recommended).
   - **Far attach mode → Near**, so it snaps to your hand.
5. Add the `BeadGun` script.
6. Add an empty child `FirePoint` at the barrel tip, blue (Z) axis facing
   outward. Assign it to the script's **Fire Point** field.

## d. Bead prefab

1. Create a small Sphere, scale ~0.03–0.05.
2. Add a **Rigidbody**.
3. New Material `Assets/Materials/BeadMaterial`, assign to the sphere's
   Renderer (the script recolours a runtime instance of it, original
   asset stays untouched).
4. Add the `Bead` script.
5. Drag into `Assets/Prefabs/` as a new prefab (name it `Bead`, don't
   confuse with the old `Belt`/`Item` prefabs already in that folder),
   then delete from the scene.
6. Assign it to `BeadGun`'s **Bead Prefab** field.

## e–f. Firing (handled by BeadGun.cs)

Listens to `activated` (trigger press) and `selectEntered`/`selectExited`
(grab/release):

- Only fires while held (bonus, done).
- **Fire Rate** field rate-limits shots (bonus, done).

Tune **Fire Force** and **Fire Rate** in the Inspector.

## g. Colour selection object

1. Create a Cube, scale into a small block.
2. **Collider** with **Is Trigger** checked (no Rigidbody needed — the
   gun's Rigidbody satisfies the trigger requirement).
3. Add `ColorSelector`, set **Colour**.
4. Make a few of these as a palette (red, blue, green…).
5. Bonus: tick **Use Directional Colour**, set **Colour Front** /
   **Colour Back** — object's blue (Z) axis decides the side.

Make sure the `BeadGun` collider is tagged `BeadGun`.

## h. Blocking plane

1. Create a Cube, squish flat on Y.
2. **Rigidbody** (uncheck gravity or make kinematic) + **Collider**.
3. Tag it `Blocker`.
4. Add **XR Grab Interactable** so it's movable.

## i. Freeze beads on the blocker

Handled by `Bead.cs`: `OnCollisionEnter` checks the `Blocker` tag,
zeroes velocity, sets `isKinematic = true` so it sticks in place.

## j. Test

Play Mode, grab the gun, pull trigger, touch a colour selector, grab the
blocker into the stream, confirm beads stack up and freeze.

## Gotchas

- Namespace errors: XRI 3.x uses
  `UnityEngine.XR.Interaction.Toolkit.Interactables` for
  `XRGrabInteractable` and its event args — already reflected in the
  `using` statements in these scripts, matching your installed 3.0.11.
- If beads fall through the blocker, check both colliders are
  non-trigger.
- If the trigger doesn't fire, confirm the XR Origin's controllers have
  an **Activate** action bound — check the Input Action asset used by
  the XR Origin (Week 1's rig already has this working, since
  `ItemXRGrabHandler` used the same grab pattern).
