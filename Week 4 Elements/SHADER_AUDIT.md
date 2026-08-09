# Shader Graph Audit — Week 4 Elements

Audited against `INSTRUCTIONS.md` sections 8 and 9. Every `.shadergraph` was parsed and its
node graph traced edge by edge, not eyeballed.

> **Caveat:** this reflects what is **saved to disk**. Unity holds unsaved edits in memory,
> so save the project (Ctrl+S, plus **Save Asset** in any open Shader Graph window) before
> trusting a "that's already done" reaction to anything below.

---

## Summary

| Graph | Wiring vs spec | Verdict |
|---|---|---|
| `SG_Wave` | all 21 wires correct | 1 property bug |
| `SG_Voronoi` | all 9 wires correct | clean |
| `SG_Frost` | all 7 wires correct | clean |
| `SG_Glow` | all 11 wires correct, Remap set to `(-1,1)→(0.5,1)` | clean |
| `SG_Arcs` | 11 of 12 wires correct | 1 wire missing |

The graph construction is solid. Both real defects are one-field fixes. The larger gap is
downstream: three elements never got a material assigned at all.

---

## Issue 1 — `SG_Wave` emission property has the wrong Reference (silent failure)

**What's wrong**

The blackboard property `_EmissionColour` has its **Reference** field set to `_EmissionColour`
(British spelling). `INSTRUCTIONS.md` §8 step 25 requires exactly `_EmissionColor`.

`ElementPulse.cs` line 69:

```csharp
sphereMaterial.SetColor("_EmissionColor", emissionColour * brightness * emissionStrength);
```

`SetColor` on a name the shader doesn't expose does nothing and throws no error. `ElementPulse`
sits on `Element_Flame` and `EnergyElement`, so **Flame's pulsing emission is currently dead**.

**Corroborating evidence:** `Mat_Flame.mat` has a serialised `_EmissionColor` entry holding
HDR `(34.3, 8.55, 0.0)` — a bright orange — but the shader has no such property, so Unity
ignores it. Meanwhile `_Colour` on that material is **black** and the graph's `_EmissionColour`
default is also black, which means the Flame *sphere* is rendering as a black ball. What you
see in the Game view is the particle system, not the sphere.

**Fix**

1. Open `SG_Wave`, click `_EmissionColour` in the Blackboard.
2. Graph Inspector → **Node Settings** → **Reference** → change to `_EmissionColor`.
3. **Save Asset**.

Renaming the reference makes it line up with the orphaned value already sitting in
`Mat_Flame.mat`, so the orange comes back on its own. Verify: press Play, Flame should
breathe. If it stays black, set `_Colour` on `Mat_Flame` to `#FF6A1E` per the §8 table.

This is worth a sentence in your video — it's a genuine "the compiler can't help you"
failure mode, distinct from the NaN/`Absolute`/`Sign` point.

---

## Issue 2 — `SG_Arcs` Color node is never connected

**What's wrong**

The **Color** node exists and is correctly set to black `(0,0,0)`, but its `Out` port goes
nowhere. `Fragment ▸ Base Color` is unconnected, so URP falls back to the block's default
(white). §9.3 step 12 requires `Color → Base Color`.

Effect: Storm renders as a **pale sphere with purple arcs on it** rather than purple arcs on
near-black. §9.5 expects "thin purple arcs crawling over black". The arcs themselves work —
Time, scroll, noise and Step are all wired correctly.

**Fix**

Open `SG_Arcs`, drag from the Color node's `Out(3)` to `Fragment ▸ Base Color(3)`. **Save Asset**.

---

## Issue 3 — Ember, Spark and Steam have no material

`Element_Ember`, `Element_Spark` and `Element_Steam` carry **no material override** on their
sphere renderer. They inherit whatever `Elementbase` has.

The materials exist and are unused:

| Prefab | Material sitting unassigned | Where |
|---|---|---|
| `Element_Ember` | `Mat_Ember.mat` (URP Lit, emissive — §7.3) | `Assets/Materials/` |
| `Element_Spark` | `Mat_Spark.mat` (URP Lit, emissive — §7.3) | `Assets/Materials/` |
| `Element_Steam` | `Steam.mat` | `Assets/Materials/` |

**Fix:** open each prefab, drag the material onto the sphere's Mesh Renderer.

Ember and Spark are the two elements `ElementPulse` is meant to differentiate (`n = 1` vs
`n = 8`). With no material assigned there is nothing for the emission pulse to drive, so this
also costs you the §14 checklist item about explaining that difference in the video.

---

## Issue 4 — Smoke is on the URP Lit material, not the Shader Graph one

`Element_Smoke` is assigned `Smoke.mat` (plain URP Lit). `Mat_Smoke.mat` exists, is built on
`SG_Wave`, and is unassigned.

`Mat_Smoke` also has `_Colour` set to `#598FEA` — water blue. It was clearly duplicated from
`Mat_Water` and never recoloured.

**Fix:** decide which one Smoke should use. If you want the vertex displacement, set
`Mat_Smoke`'s `_Colour` to a grey (around `#6B6B6B`) and assign it. If not, delete
`Mat_Smoke.mat` so it isn't sitting there looking like an oversight.

---

## Issue 5 — Duplicate legacy materials

`Assets/Materials/` still holds plain URP Lit `Water`, `Mud`, `Flame`, `Vine`, `Ice`,
`Crystal` and `Smoke` materials that the Shader Graph versions in `Assets/Shaders/` replaced.
All are unassigned except `Smoke`.

Not a bug, but seven near-duplicates named almost identically to the live ones is how you end
up assigning the wrong material at 11pm. Delete them, or move them to
`Assets/Materials/_Deprecated/`.

Also worth tidying: the SG materials live in `Assets/Shaders/` alongside the graphs, while the
URP Lit ones live in `Assets/Materials/`. `INSTRUCTIONS.md` §0 puts all `.mat` files in
`Assets/Materials/`.

---

## Verified correct — no action needed

**`SG_Wave` vertex chain**, traced end to end:

```
Position ─┬→ Split.G ─→ Add(+Time) ─→ Multiply(×_Frequency) ─→ Add(+_Phase) ─→ Sine
          │                                                                     │
          │                          ┌──────────────────────────────────────────┤
          │                          ↓                                          ↓
          │                      Absolute ─→ Power(^_Sharpness) ─→ Multiply ←─ Sign
          │                                                          │
          │                              Multiply(×_Amplitude) ←─────┘
          │                                        ↓
          │             NormalVector ─→ Multiply ←─┘
          │                                ↓
          └──────────────→ Add ←───────────┘
                            ↓
                   Vertex ▸ Position
```

The `Absolute`/`Sign` pair around `Power` is present and correctly placed — that's the GPU
NaN fix, and the single best thing in these graphs to talk about on camera.

**Material parameter values** match the §8 and §9 tables. Only deviation is
`Mat_Water._Amplitude = 0.02` against a specified `0.015` — a tuning choice, not an error.

**`SG_Voronoi`, `SG_Frost`, `SG_Glow`** are wired exactly to spec, including the `Remap`
`(-1,1) → (0.5,1)` that stops Glow strobing.

---

## Outside the shader scope, but flagged

- **`ObjectParticleEmitter.cs` is not attached to anything.** Activity (h) — object particles
  on Sand and Ash — is not done. It's a §14 checklist item.
- **You're working in `BaseScene.unity`.** §0.1 step 5 asks you to `Save As`
  `Week4_Elements.unity` and leave `BaseScene` clean.

---

## Fix order

1. `SG_Wave` Reference → `_EmissionColor` (unblocks Flame)
2. `SG_Arcs` Color → Base Color (unblocks Storm)
3. Assign `Mat_Ember`, `Mat_Spark`, `Steam` to their prefabs
4. Resolve Smoke — recolour `Mat_Smoke` and assign, or delete it
5. Clear out the duplicate materials
6. Play, and check each element against the §9.5 table
