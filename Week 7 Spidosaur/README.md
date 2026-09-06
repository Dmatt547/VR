# Week 7 - Squishy Spidosaur

Week 7 workshop challenge: a squishy, stretchable object that can be manipulated in virtual reality.

Copy of `VR Template` (Unity **6000.0.79f1**, URP, OpenXR + XR Interaction Toolkit 3.0.11). Open `Assets/Scenes/BaseScene.unity`.

The rigged spidosaur is driven by a mass-spring system built from the week 7 lecture's cloth example: one grabbable marker per bone, springs holding each bone at its imported length, and explicit Euler integration on the fixed timestep. Pull a marker and the skin stretches; let go and the springs pull it back.

**Build steps, Inspector values and testing: `SETUP-GUIDE.md`.**

| Script | Lecture equivalent | Job |
|---|---|---|
| `Assets/Scripts/BoneSpring.cs` | `Spring` | F = -k * (length - restLength) between two markers |
| `Assets/Scripts/BoneMarker.cs` | `Node` | grabbable handle; sums its spring forces and integrates a = F/m, v += a·dt, p += v·dt |
| `Assets/Scripts/SpidosaurRig.cs` | `Cloth` | walks the armature, builds the markers and springs, drives the bones |

`Assets/Editor/RigDumper.cs` (**Tools > Dump Rig Hierarchy**) writes the bone hierarchy to `RigDump.txt` - reused from my Assessment 3 work.

> `Assets/Models/spidosaur.blend` imports through Blender, so Blender has to be installed. If the import is broken, export the model to FBX from Blender instead.
