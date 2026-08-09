# Making Ember / Spark / Smoke look like the reference

The gap between your Game view and the example solution is mostly **lighting**, not particles.
Work through these in order — step 1 alone will change more than everything else combined.

---

## 1. Turn Bloom on

`Assets/Settings/SampleSceneProfile.asset` is the profile your **Global Volume** uses, and its
Bloom override is set to `active: 0`.

Without bloom, HDR colours just clamp. `Mat_Storm._ArcColour` is HDR `(5.2, 0, 16.8)` and
`Mat_Glow._GlowColour` is HDR `(0.16, 14.9, 0)` — values well above 1.0 that are being thrown
away every frame. Same for every additive particle.

**Fix:** select **Global Volume** in the Hierarchy → in the Inspector find **Bloom** → tick the
checkbox next to its name. Then set:

| Setting | Value | Why |
|---|---|---|
| Threshold | `0.9` | anything brighter than this blooms; lower = more glow |
| Intensity | `0.6` | start here, push to `1.0` if it's still flat |
| Scatter | `0.7` | how far the glow spreads |

Also check the **Camera** under `XR Origin (XR Rig)` has **Post Processing** ticked, or the
volume does nothing.

`INSTRUCTIONS.md` §7.3 step 2 asks for this.

---

## 2. Darken the scene

The reference solution is a dark navy room. That is not incidental — additive particles work by
*adding* light to what's behind them. A white speck against a bright salmon wall adds almost
nothing visible. The same speck against near-black is a spark.

Your scene currently has:

- Directional Light at **Intensity 2**
- Ambient Source = **Skybox** (Unity's default procedural sky)

**Fix — pick one:**

**Option A — dark room (matches the reference)**

1. **Window → Rendering → Lighting → Environment**
2. Skybox Material → **None**
3. Ambient Source → **Colour**, set to a dark navy like `#0F1A2E`
4. Select the Directional Light → Intensity → `0.6`

Your element spheres will still be lit by the directional light, but everything else falls away
and the emissive elements carry the scene.

**Option B — keep it bright**

Leave the lighting alone and instead push emission harder: raise bloom Intensity to `1.2`, and
raise the HDR **Intensity** slider on `_ArcColour`, `_GlowColour` and `Mat_Ember`'s emission by
2 stops each. This works but you're fighting the environment the whole way.

Option A is less work and looks closer to the example.

---

## 3. Raise the particle counts

Particles alive at any moment ≈ `rateOverTime × startLifetime`. Your current values:

| Element | Rate | Lifetime | Alive at once |
|---|---|---|---|
| Ember | 8 | 1.5 | **~12** |
| Spark | 25 | 0.4 | **~10** |
| Smoke | 12 | 3.0 | ~36 |

Twelve dots reads as "a few dots". Suggested starting values — open each prefab, select the
child Particle System:

### Ember — slow drifting coals

| Module | Setting | From | To |
|---|---|---|---|
| Emission | Rate over Time | 8 | `40` |
| Main | Start Size | 0.02 | `0.015` – `0.035` (use **Random Between Two Constants**) |
| Main | Start Lifetime | 1.5 | `2` |
| Main | Start Speed | 0.15 | `0.1` – `0.3` (random) |
| Main | Gravity Modifier | — | `-0.05` (coals rise) |
| Renderer | Material | — | `Mat_Particle_Additive` |

### Spark — fast, sharp, short-lived

| Module | Setting | From | To |
|---|---|---|---|
| Emission | Rate over Time | 25 | `120` |
| Main | Start Size | 0.01 | `0.006` – `0.014` (random) |
| Main | Start Lifetime | 0.4 | `0.3` – `0.6` (random) |
| Main | Start Speed | 2 | `1.5` – `3` (random) |
| Main | Gravity Modifier | 0.4 | keep — sparks arc and fall |
| Renderer | Material | — | `Mat_Particle_Additive` |

Adding a **Trails** module to Spark (Ratio `1`, Lifetime `0.3`, Width `0.3`) turns dots into
streaks and is the single biggest readability win for this one.

### Smoke — soft, slow, alpha not additive

| Module | Setting | From | To |
|---|---|---|---|
| Emission | Rate over Time | 12 | `25` |
| Main | Start Size | 0.6 | `0.4` – `0.9` (random) |
| Main | Start Lifetime | 3 | `4` |
| Main | Gravity Modifier | -0.08 | keep |
| Renderer | Material | — | `Mat_Particle_Alpha` (**not** additive — smoke blocks light) |
| Size over Lifetime | enabled | — | curve rising 0.5 → 1.5 (smoke expands as it cools) |
| Rotation over Lifetime | Angular Velocity | — | `-30` to `30` (breaks up repetition) |

Smoke is the one element that should **not** be additive. Additive smoke glows, which is wrong.

---

## 4. Get the sphere out of the way

In the reference frame, Sand, Ash and Spark have **no visible sphere** — the particle effect is
the entire element. Yours are opaque balls with a handful of dots orbiting them, so the ball
wins your attention.

For the elements that are "made of" particles rather than "made of" a surface — Spark, Smoke,
Steam, Ash, Sand — do one of:

- **Shrink the sphere:** set the mesh child's Local Scale to about `0.35` of its current value,
  so the particle cloud is visually larger than the ball
- **Hide it entirely:** untick the **Mesh Renderer** on the sphere child (keep the Collider so
  it's still grabbable — this is the reference's approach for Sand and Ash)

Keep the sphere visible for the elements where the *surface* is the point: Water, Mud, Vine,
Flame, Crystal, Ice, Frost, Stone, Storm, Glow. Those are the ones carrying your Shader Graph
marks, so they need to be seen.

---

## 5. Elements with no particle system at all

| Element | Status | What it needs |
|---|---|---|
| `Element_Steam` | no particle overrides, and no material | rising white wisps, alpha material, gravity `-0.2` |
| `Element_Storm` | no particle overrides | optional — `SG_Arcs` may be enough on its own |
| `Element_Sand` | no particle system (SolidElement has none) | `ObjectParticleEmitter` — see below |
| `Element_Ash` | no particle system | `ObjectParticleEmitter` — see below |

### Sand and Ash — activity (h)

The falling yellow stream piling up on the shelf in the reference frame is **object particles**,
not a Particle System. `ObjectParticleEmitter.cs` is written and sitting in `Assets/Scripts/`
**attached to nothing**.

`INSTRUCTIONS.md` §10.3 covers this, and §14 lists it as a required checklist item. Add the
component as a child of `Element_Sand` and `Element_Ash`, give it a small sphere prefab with a
Rigidbody and Collider, and set the emission rate and lifetime in the Inspector.

---

## Order to work in

1. Bloom on → press Play → this alone should transform Ember, Spark, Storm and Glow
2. Darken the environment → press Play again
3. Only then start tuning rates and sizes

Doing it the other way round means tuning particle values against a broken lighting setup, and
you'll end up with numbers that look wrong once bloom comes on.
