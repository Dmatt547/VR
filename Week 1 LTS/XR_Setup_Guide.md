# Adding VR/XR Support — Setup Guide

Your project already has XR Management and OpenXR installed (just not the interaction toolkit yet). Follow these steps in the Unity Editor.

## 1. Install XR Interaction Toolkit

1. **Window > Package Manager**
2. Top-left dropdown → **Unity Registry**
3. Search **"XR Interaction Toolkit"** → Install (latest version, compatible with Unity 6)
4. In the package's details panel, open the **Samples** tab → import **Starter Assets**
   - This brings in the default input actions asset, an `XR Origin (VR)` prefab, and the **XR Device Simulator** (lets you test grab/movement with mouse + keyboard, no headset needed)

## 2. Enable the OpenXR loader

1. **Edit > Project Settings > XR Plug-in Management**
2. Under the **PC/Standalone** tab, tick **OpenXR**
3. Go to the **OpenXR** settings page below it → under Interaction Profiles, add **Oculus Touch Controller Profile** (or whichever controller matches what you use in class)

## 3. Add the XR rig to the scene

1. Open `Assets/Scenes/SampleScene.unity`
2. Delete or disable the existing `Main Camera` (the XR rig brings its own)
3. **GameObject > XR > XR Origin (VR)** — adds the camera rig + both controllers
4. If not already present, add an **XR Interaction Manager** to the scene (usually auto-added with the XR Origin)
5. Drag in the **XR Device Simulator** prefab (from the Starter Assets sample) so you can test in Play Mode without a headset

## 4. Make items grabbable

1. Select `Assets/Prefabs/Item.prefab`
2. **Add Component > XR Grab Interactable**
   - It already has a `Rigidbody` with gravity on, so it already falls/collides like a normal physics object
   - Set **Movement Type** to `Velocity Tracking` — this keeps the Rigidbody fully physics-driven even while held (not kinematic), so it can still be blocked by walls, knocked around, and thrown with real momentum. Leave **Throw On Detach** ticked so letting go gives it realistic velocity.
3. **Add Component > Item Xr Grab Handler** (the script just added) — this is what tells `ConveyorBelt` to release the item when you grab it in VR

## 5. Test without a headset

1. Enter Play Mode
2. Use the **XR Device Simulator** (keyboard/mouse) to move a controller over an item and simulate a grab (check the on-screen prompts it shows — usually a key like `G` or mouse click while hovering)
3. Confirm the item stops moving with the belt while held, and rejoins the belt if dropped back on

## What to say in the video

Since you can't demo on a real headset at home, just show: the `Item` prefab with `XR Grab Interactable` + `ItemXRGrabHandler` attached, the `XR Origin (VR)` in the scene hierarchy, and briefly explain the code — that grabbing an item in VR removes it from the belt's tracked list so physics doesn't fight the player's hand.
