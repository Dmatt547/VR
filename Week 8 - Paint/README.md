# VR Template

Clean starting point for weekly VR tasks. Unity **6000.0.79f1**, URP, OpenXR + XR Interaction Toolkit 3.0.11.

## How to use

1. Copy the whole `VR Template` folder → rename to `Week N`.
2. Open it in Unity Hub (**Add project from disk**). First open takes a few minutes while Unity rebuilds `Library/`.
3. Open `Assets/Scenes/BaseScene.unity` and build your week's work in it.

## What's in the base scene

- **XR Origin (XR Rig)** — Starter Assets prefab: head, left/right controllers, near-far interactors, poke, teleport, locomotion
- **XR Device Simulator** — test in the Game view without a headset (hold Left Shift / Space to drive the controllers)
- **XR Interaction Manager**
- **Teleportation Area** — the ground plane
- **Directional Light**, **Global Volume** (URP post-processing)

## What's kept outside the scene

- `Assets/Samples/XR Interaction Toolkit/3.0.11/` — Starter Assets (rig, controller + interactor prefabs, presets, `XRI Default Input Actions`) and the XR Device Simulator
- `Assets/XR/` + `Assets/XRI/` — OpenXR loader, XR Plug-in Management, interaction layer settings
- `Assets/Settings/` — URP pipeline assets (PC + Mobile)
- `Assets/InputSystem_Actions.inputactions` — project-wide input actions
- `Assets/TextMesh Pro/` — TMP essentials
- `ProjectSettings/`, `Packages/manifest.json`

`Assets/Scripts`, `Assets/Prefabs` and `Assets/Materials` are empty, ready for your own work.

## Removed from Week 2/3

Bead sculpting scene and scripts, conveyor belt prefabs and materials, Photon Fusion SDK, the empty VersatileControllerInterface folder, and the URP tutorial Readme.

> Photon Fusion was stripped out. If a later week needs multiplayer or the phone-as-controller interface, re-import the Fusion package into that week's copy — don't add it back here unless every week needs it.
