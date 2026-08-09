# Week 4 — Element Spheres

Media challenge: 16 element spheres with visual, audio and haptic representation.
Copied from `VR Template`. Unity **6000.0.79f1**, URP, OpenXR + XR Interaction Toolkit 3.0.11.

**Start here: [`INSTRUCTIONS.md`](INSTRUCTIONS.md)** — the full step-by-step build guide.

## Quick start

1. Unity Hub → **Add project from disk** → this folder. First open rebuilds `Library/`.
2. Open `Assets/Scenes/BaseScene.unity`, **Save As** → `Week4_Elements.unity`, work in the copy.
3. Follow `INSTRUCTIONS.md` from section 1.

## Elements

Flame, Steam, Storm, Frost, Mud, Vine, Sand, Ember, Spark, Smoke, Stone, Ash, Crystal, Glow, Water, Ice

## Scripts

`Assets/Scripts/`

| Script | Purpose |
|---|---|
| `ElementSphere.cs` | Ambient loop, grab sound, controller haptics, buoyancy |
| `ElementPulse.cs` | Animates scale/emission with `(A sin(f t + p))^n` from the week 4 lecture |
| `ObjectParticleEmitter.cs` | GameObject particles with real physics (task activity h) |
| `Billboard.cs` | Keeps shelf labels facing the headset |

All 16 spheres share `ElementSphere` and differ only by their Inspector values.

## Inherited from the template

XR Origin rig, XR Device Simulator, teleportation area, URP settings, OpenXR loader,
XRI Starter Assets. Activities (a) and (b) of the task sheet are already done.
