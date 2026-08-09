# Week 4 — Media Challenge: Element Spheres

Build a set of 16 spheres, each representing an element, with visual, audio and haptic
representation. Stage 2 (once the core set works) adds the element-combining game.

**Unity 6000.0.79f1 · URP · OpenXR · XR Interaction Toolkit 3.0.11**

> The task sheet lists 16 elements, not 14: Flame, Steam, Storm, Frost, Mud, Vine, Sand,
> Ember, Spark, Smoke, Stone, Ash, Crystal, Glow, Water, Ice.

This week's lecture is about **shaders** — the vertex/fragment split, the periodic function
`y = (A sin(f x + p))^n`, Perlin noise, and Shader Graph. Sections 7 and 8 build directly
on that, and section 6 of the scripts uses the same formula in C#.

---

## 0. What is already done for you

Copied from `VR Template`, so activities (a) and (b) from the task sheet are done:

- XR project settings, OpenXR loader, XR Plug-in Management — configured
- `Assets/Scenes/BaseScene.unity` has **XR Origin (XR Rig)** with near-far interactors, poke,
  teleport and locomotion; **XR Interaction Manager**; **XR Device Simulator**;
  **Teleportation Area** ground plane; directional light; URP global volume

Empty and waiting for you:

```
Assets/Materials/    one .mat per element
Assets/Prefabs/      ElementBase + family variants + the 16 element variants
Assets/Shaders/      Shader Graph assets
Assets/Audio/        loops and one-shots
Assets/Textures/     downloaded colour + normal maps
Assets/Scripts/      already contains the 4 scripts below
```

### 0.1 Open the project

1. Unity Hub → **Add** → **Add project from disk** → select `Week 4 Elements`.
2. Open with **6000.0.79f1**. First open rebuilds `Library/` and takes a few minutes.
3. Open `Assets/Scenes/BaseScene.unity`.
4. Press **Play**. Hold **Left Shift** (left controller) or **Space** (right) and move the
   mouse to drive a controller; **left mouse** = trigger/select, **G** = grip.
5. **File → Save As** → `Assets/Scenes/Week4_Elements.unity`. Work in the copy so `BaseScene`
   stays clean.

### 0.2 Scripts already written

Same style as weeks 1–3: plain MonoBehaviours, `[SerializeField]` fields set in the
Inspector, event handlers wired in `OnEnable`/`OnDisable`.

| Script | What it does | Goes on |
|---|---|---|
| `ElementSphere.cs` | Ambient loop, grab sound, controller haptics, buoyancy | Every element prefab |
| `ElementPulse.cs` | Animates scale/emission using `(A sin(f t + p))^n` from the lecture | Flame, Ember, Spark, Storm, Glow |
| `ObjectParticleEmitter.cs` | Spawns real GameObject particles with physics (activity h) | Child of Sand and Ash |
| `Billboard.cs` | Turns shelf labels to face the headset | Each label |

All 16 spheres use the same `ElementSphere` component and differ only by the values typed
into the Inspector. That is the shared functionality activity (c) is asking for.

---

## 1. Design the hierarchy first (activity c)

Do this before building anything — it is what stops you making 16 near-identical prefabs
by hand, and it is the part the marker looks at.

**Three levels of prefab, using prefab variants:**

```
ElementBase                 (prefab - grabbable sphere, rigidbody, audio, ElementSphere)
├── SolidElement            (variant - textured, heavy, no glow)
│   Stone, Sand, Mud, Ash, Vine
├── FluidElement            (variant - transparent, vertex displaced surface)
│   Water, Ice, Frost, Crystal
├── GasElement              (variant - soft, buoyant, particle system)
│   Steam, Smoke
└── EnergyElement           (variant - emissive, particle system, point light, ElementPulse)
    Flame, Ember, Spark, Storm, Glow
```

**Why split it this way:** the grouping is by *rendering technique*, not by theme.
Everything in `FluidElement` needs a transparent material and a vertex shader; everything
in `EnergyElement` needs emission plus a particle system. So each family prefab can carry
the components all of its children need, and editing the family updates every variant under
it. Grouping by theme (fire / water / earth) would put Ash next to Flame, and those two
share nothing technically.

Write two or three sentences of that reasoning into your submission — activity (c) wants a
justified hierarchy, not just a folder structure.

### Which technique each element uses

| Element | Family | Colour | Technique (activity) |
|---|---|---|---|
| Stone | Solid | `#7A7A73` | colour + normal texture (d) |
| Sand | Solid | `#D9C08A` | texture (d), object particles (h) |
| Mud | Solid | `#4E3B26` | texture (d) + vertex bubbling (e) |
| Ash | Solid | `#6E6A63` | texture (d), object particles (h) |
| Vine | Solid | `#3E8E41` | texture (d) + slow vertex sway (e) |
| Water | Fluid | `#2E7BC4` | transparent + vertex ripple (e) |
| Ice | Fluid | `#A8E4F0` | transparent + Voronoi crack pattern (f) |
| Frost | Fluid | `#DCF4FF` | Perlin noise pattern (f) |
| Crystal | Fluid | `#B08CFF` | Voronoi facet pattern (f) |
| Steam | Gas | `#DDE7EC` | particle system (g) + buoyancy |
| Smoke | Gas | `#3A3A3A` | particle system (g) + buoyancy |
| Flame | Energy | `#FF6A1E` | particle system (g) + vertex flicker (e) |
| Ember | Energy | `#A32000` | particle system (g), slow emission pulse |
| Spark | Energy | `#FFD34D` | particle system (g), sharp fast pulse |
| Storm | Energy | `#6B5BD6` | sine arc pattern (f) + particle system (g) |
| Glow | Energy | `#7CFFB2` | pulsing fresnel (f) |

That covers (d), (e), (f), (g) and (h) — you need at least one element per technique for
full marks.

---

## 2. Build the room

1. **GameObject → 3D Object → Cube**, name it `Shelf_Row1`. Scale `(4, 0.05, 0.4)`,
   position `(0, 1.0, 2)`.
2. Duplicate twice: `Shelf_Row2` at y `1.4`, `Shelf_Row3` at y `1.8`. Four rows of four
   works for 16, or three rows of five/six.
3. Create an empty GameObject called `Elements` at the origin to parent all the spheres to,
   so the Hierarchy doesn't turn into a wall of 16 objects.
4. Leave the Teleportation Area alone — you can already walk to the shelf.

---

## 3. Build the `ElementBase` prefab

1. **GameObject → 3D Object → Sphere**, name it `ElementBase`, scale `(0.2, 0.2, 0.2)`.
2. Add these components in order:
   - **Rigidbody** — Mass `1`, Use Gravity ✔, Interpolate = **Interpolate**,
     Collision Detection = **Continuous Dynamic** (stops a thrown sphere tunnelling through
     the shelf)
   - **XR Grab Interactable** — Movement Type = **Velocity Tracking**, Throw On Detach ✔,
     Smooth Position ✔, Smooth Rotation ✔
   - **Audio Source** — Play On Awake ✘ (the script starts it)
   - **Element Sphere** (script)
3. The Sphere Collider comes with the primitive — leave it.
4. Add an empty child called `ParticleAnchor` at local `(0,0,0)`. Particle systems and the
   object particle emitter go on this, so they stay separate from the sphere's renderer.
5. Add a child **TextMeshPro - Text (3D)** named `Label`, **Pos Y `1.4`**, scale `0.1`,
   font size `12`, centre aligned. Add the **Billboard** component to it.
   (Unity prompts you to import **TMP Essentials** the first time — accept.)

   Pos Y is `1.4`, not `0.28`, because child positions are multiplied by the parent's
   scale. The sphere is scaled to `0.2`, so `1.4 × 0.2 = 0.28 m` above the centre — clear
   of the sphere's `0.1 m` radius. Put `0.28` in there and the label sits *inside* the
   sphere.
6. Drag `ElementBase` into `Assets/Prefabs/`, then delete it from the scene.

**Test it now.** Drag the prefab back into the scene, press Play, grab it with the
simulator. Fix any problems here, before you make 16 variants of it.

---

## 4. Family variants

For each of `SolidElement`, `FluidElement`, `GasElement`, `EnergyElement`:

1. Right-click `ElementBase` in the Project window → **Create → Prefab Variant**, rename it.
2. Set what the whole family shares:

| Family | Settings |
|---|---|
| SolidElement | Rigidbody Linear Damping `0.5`, Angular Damping `1` |
| FluidElement | Rigidbody Linear Damping `1.5` — fluids feel damped |
| GasElement | Linear Damping `3`, so they drift rather than fly; particle system on `ParticleAnchor` — **do the shared setup in section 10.1 now, while you're in here** |
| EnergyElement | Particle system on `ParticleAnchor` — **section 10.1 now**; **Light** (Point, Range `1`, Intensity `2`); **Element Pulse** script |

Solid and Fluid do **not** get a particle system. Their surfaces come from textures and
shaders instead.

---

## 5. The 16 element variants

For each element:

1. Right-click the correct family prefab → **Create → Prefab Variant**.
2. Rename it `Element_Flame`, `Element_Stone`, and so on.
3. Set the `Label` child's text to the element name.
4. Assign the material (section 6).
5. Fill in the `ElementSphere` values from the table below.
6. Tune the particle system (section 10.2) if this element has one.

### Where each value goes

Double-click the element prefab to open it in Prefab Mode, select the root object, and
scroll the Inspector:

| Value in the tables | Component | Field |
|---|---|---|
| Element Name | Element Sphere → **Element** | Element Name |
| Grab Amplitude | Element Sphere → **Haptics** | Grab Amplitude |
| Grab Duration | Element Sphere → **Haptics** | Grab Duration |
| Buoyancy | Element Sphere → **Physics** | Buoyancy |
| Mass | **Rigidbody** | Mass |

Element Sphere sits near the bottom of the Inspector, under XR Grab Interactable and Audio
Source. Its fields are grouped under bold **Element / Audio / Haptics / Physics** headers —
those come from the `[Header]` attributes in the script.

Mass is the one value not on the script, because the Rigidbody owns it. `ElementSphere`
reads it when applying buoyancy rather than setting it.

### Which family each element comes from

Work down this table one family at a time. Every element is a variant of the family prefab
in its section heading — nothing is made from `ElementBase` directly.

**From `SolidElement`** — heavy, textured, no glow, no particle system

| Element | Element Name | Mass | Buoyancy | Grab Amplitude | Grab Duration |
|---|---|---|---|---|---|
| Stone | Stone | 3.0 | 0 | 0.90 | 0.20 |
| Sand | Sand | 2.0 | 0 | 0.45 | 0.15 |
| Mud | Mud | 2.5 | 0 | 0.70 | 0.30 |
| Ash | Ash | 0.4 | 0.1 | 0.20 | 0.15 |
| Vine | Vine | 1.0 | 0 | 0.40 | 0.18 |

**From `FluidElement`** — transparent, shader-driven surface, no particle system

| Element | Element Name | Mass | Buoyancy | Grab Amplitude | Grab Duration |
|---|---|---|---|---|---|
| Water | Water | 1.0 | 0 | 0.25 | 0.15 |
| Ice | Ice | 1.2 | 0 | 0.55 | 0.06 |
| Frost | Frost | 0.8 | 0 | 0.30 | 0.10 |
| Crystal | Crystal | 1.5 | 0 | 0.65 | 0.05 |

**From `GasElement`** — light, buoyant, particle system

| Element | Element Name | Mass | Buoyancy | Grab Amplitude | Grab Duration |
|---|---|---|---|---|---|
| Steam | Steam | 0.1 | 0.8 | 0.12 | 0.20 |
| Smoke | Smoke | 0.1 | 0.6 | 0.10 | 0.25 |

**From `EnergyElement`** — emissive, particle system, point light, `ElementPulse`

| Element | Element Name | Mass | Buoyancy | Grab Amplitude | Grab Duration |
|---|---|---|---|---|---|
| Flame | Flame | 0.2 | 0.3 | 0.35 | 0.10 |
| Ember | Ember | 0.6 | 0 | 0.30 | 0.20 |
| Spark | Spark | 0.1 | 0.1 | 0.20 | 0.05 |
| Storm | Storm | 0.3 | 0.2 | 0.60 | 0.08 |
| Glow | Glow | 0.2 | 0.1 | 0.15 | 0.30 |

Mass goes on the Rigidbody, the rest on the `ElementSphere` component.

**If you're unsure where something belongs, ask what it needs to render**, not what it is
thematically. Ash is a fire word but it is a dull grey powder with a texture and no glow,
so it is Solid. Crystal sounds solid but it is transparent with a procedural pattern, so it
is Fluid. That question — technique, not theme — is the argument you make for activity (c).

The haptics are doing design work here, not just buzzing: amplitude encodes weight (Stone
`0.9`, Spark `0.2`) and duration encodes hardness (Ice is a short sharp `0.06` knock, Mud
is a long soft `0.30` squelch). Say that in your submission — it is the difference between
"I added haptics" and "I chose these haptics".

Then drag all 16 into the scene under `Elements` and space them along the shelves. Placing
them by hand takes about five minutes; don't write an editor script for it.

---

## 6. `ElementPulse` — the lecture formula in C#

Add **Element Pulse** to Flame, Ember, Spark, Storm and Glow (it is already on the
`EnergyElement` family prefab if you added it there).

It computes `(A sin(f t + p))^n` every frame and uses the result to drive scale and
emission brightness. The four parameters are exactly the ones from the lecture:

| Field | Lecture symbol | Effect |
|---|---|---|
| Amplitude | A | how far it swells |
| Frequency | f | how fast it repeats |
| Phase | p | where in the cycle it starts |
| Sharpness | n | `n = 1` smooth, high even `n` gives narrow sharp flashes |

Settings to use:

| Element | Amplitude | Frequency | Phase | Sharpness | Pulse Scale | Pulse Emission |
|---|---|---|---|---|---|---|
| Flame | 1 | 6 | 0 | 3 | ✔ 0.04 | ✔ orange, 3 |
| Ember | 1 | 0.8 | 0 | 1 | ✘ | ✔ deep red, 1.5 |
| Spark | 1 | 12 | 0 | 8 | ✘ | ✔ yellow, 5 |
| Storm | 1 | 9 | 0 | 6 | ✔ 0.02 | ✔ purple, 4 |
| Glow | 1 | 1.5 | 0 | 1 | ✔ 0.06 | ✔ green, 4 |

Set **Phase** to a different value on each of the five (say `0`, `1.2`, `2.4`, `3.6`, `4.8`)
so they don't all pulse in sync. That is exactly what the lecture means by phase shifting
the start of the cycle without reshaping the wave.

**Pulse Emission needs a `_EmissionColor` property on the material.** URP Lit materials have
one, so Ember and Spark work straight away. Shader Graph materials only have it if you
named a property's **Reference** exactly `_EmissionColor` — which step 25 of section 8 does
for `SG_Wave`, so Flame works too. Storm and Glow use `SG_Arcs` and `SG_Glow`, which have
their own emission built into the graph, so untick Pulse Emission on those two and use
Pulse Scale only.

There is no error when it fails. The script calls `SetColor` on a property that isn't
there and nothing happens, which is why it is worth knowing about rather than debugging
blind.

Compare Ember (`n = 1`, slow) with Spark (`n = 8`, fast): same formula, and the sharpness
term is what turns a smooth smoulder into a hard strobe. Worth mentioning in the video.

---

## 7. Materials (activity d)

One material per element in `Assets/Materials/`. They come from three different places, so
here is the full list up front — tick them off as you go.

| Material | Element | Made from | Section |
|---|---|---|---|
| Mat_Stone | Stone | URP Lit + downloaded texture | 7.1 |
| Mat_Sand | Sand | URP Lit + downloaded texture | 7.1 |
| Mat_Ash | Ash | URP Lit + texture, grey tint | 7.1 |
| Mat_Steam | Steam | URP Lit, transparent | 7.2 |
| Mat_Smoke | Smoke | URP Lit, transparent | 7.2 |
| Mat_Ember | Ember | URP Lit, emissive | 7.3 |
| Mat_Spark | Spark | URP Lit, emissive | 7.3 |
| Mat_Water | Water | `SG_Wave` | 8 |
| Mat_Mud | Mud | `SG_Wave` | 8 |
| Mat_Flame | Flame | `SG_Wave` | 8 |
| Mat_Vine | Vine | `SG_Wave` | 8 |
| Mat_Crystal | Crystal | `SG_Voronoi` | 9.1 |
| Mat_Ice | Ice | `SG_Voronoi` | 9.1 |
| Mat_Frost | Frost | `SG_Frost` | 9.2 |
| Mat_Storm | Storm | `SG_Arcs` | 9.3 |
| Mat_Glow | Glow | `SG_Glow` | 9.4 |

Mud and Vine appear twice by design: they get the `SG_Wave` displacement *and* you can plug
their downloaded texture into the graph later if you want both. Keep it simple first — get
all 16 looking distinct, then improve.

Sections 7.1–7.3 below cover the seven plain URP Lit ones. The other nine come from the
Shader Graphs in sections 8 and 9.

### 7.1 Textured elements — Stone, Sand, Mud, Ash, Vine

These five have well-known surface structure, so you use photographed textures rather than
a shader. You need **five textures downloaded, five materials made**. Same process each
time — the full walkthrough is below, then just repeat it.

#### Step 1 — Download

On **ambientCG.com**, use the search box and grab one asset for each element:

| Element | Search for | Pick something that looks like |
|---|---|---|
| Stone | `rock` or `gravel` | grey, coarse, non-uniform |
| Sand | `sand` | fine, pale yellow, rippled |
| Mud | `mud` or `ground` | dark brown, wet, lumpy |
| Ash | `ground`, `soil`, `dirt` | fine, powdery (there is no "ash" asset — see note) |
| Vine | `leaves` or `bark` | green, organic |

For each one click **1K-JPG .zip**. That's the ~10 MB download, not the 123 MB one — 4K is
wasted on a sphere this size and will hurt your frame rate.

**When a material doesn't exist — tint one that does.** There is no "ash" texture on
ambientCG. Rather than hunt for a perfect match, take the closest structure and recolour it
in the material: the **Base Map colour swatch** next to the texture slot multiplies over
the texture, so a brown soil tinted `#8A8880` reads as ash straight away.

Better still, reuse your Stone texture for Ash with a grey tint, lower Tiling and the same
low Smoothness. That is worth saying out loud in your submission — you reused one texture
across two elements instead of shipping a near-duplicate 1K asset, because at this sphere
size the tint does more visual work than the source photograph does. The texture supplies
structure; the material supplies colour.

#### Step 2 — Unzip and copy in

Each zip contains 5–6 files. Using Gravel043 as the example:

| File in the zip | What it is | Do you need it? |
|---|---|---|
| `Gravel043_1K-JPG_Color.jpg` | the actual colours | **Yes** |
| `Gravel043_1K-JPG_NormalGL.jpg` | surface bumps, OpenGL convention | **Yes** |
| `Gravel043_1K-JPG_NormalDX.jpg` | same, DirectX convention | No — Unity uses GL |
| `Gravel043_1K-JPG_Roughness.jpg` | per-pixel shininess | No — Smoothness slider instead |
| `Gravel043_1K-JPG_AmbientOcclusion.jpg` | contact shadows | No |
| `Gravel043_1K-JPG_Displacement.jpg` | height, for tessellation | No |

Copy just **Color** and **NormalGL** into `Assets/Textures/`. You are deliberately using
two of six maps: URP Lit can take the others, but on a 20–50 cm sphere seen from a metre
away the difference is invisible and each extra map is another texture fetch per pixel, ×2
for stereo rendering. That trade-off is worth a sentence in your submission.

**NormalGL vs NormalDX** catches people out. Unity uses the OpenGL convention, so pick GL.
Use DX and the bumps light up inverted — dents look like lumps.

#### Step 3 — Tell Unity the normal map is a normal map

1. Click the `..._NormalGL` texture in the Project window.
2. In the Inspector, **Texture Type** dropdown → **Normal map**.
3. Click **Apply** at the bottom.

Skip this and Unity treats it as an ordinary colour image, so your surface lights up
blue-purple and flat. Only do this to the Normal file, never the Color one.

#### Step 4 — Make the material

1. `Assets/Materials/` → right-click → **Create → Material** → name it `Mat_Stone`.
2. Shader stays on **Universal Render Pipeline/Lit** (the default).
3. **Base Map** — click the small square to its left, choose the `..._Color` texture.
4. **Normal Map** — tick its checkbox, then assign the `..._NormalGL` texture.
5. **Smoothness** slider — see the table below.
6. **Tiling** (under Surface Inputs, near the bottom) — set X and Y to `2`. Without this
   the texture is stretched once around the whole sphere and looks smeared.
7. Drag `Mat_Stone` onto the sphere in the `Element_Stone` prefab.

#### Step 5 — Repeat for the other four

| Material | Smoothness | Why |
|---|---|---|
| Mat_Stone | 0.15 | dry rock, barely reflective |
| Mat_Sand | 0.05 | grains scatter light, almost no highlight |
| Mat_Mud | 0.45 | wet, so it catches a visible sheen |
| Mat_Ash | 0.15 | fine powder, matte |
| Mat_Vine | 0.30 | waxy leaf surface |

Smoothness is where these five stop looking like the same rock in different colours, so
don't leave it at the default `0.5` for all of them.

**Credit your sources.** ambientCG is CC0, but list which assets you used (e.g.
"Gravel043, Sand003 — ambientCG.com, CC0") in your submission.

### 7.2 Transparent elements — Water, Ice, Steam, Smoke, Crystal

No downloads needed. Create a material per element, leave the shader on
**Universal Render Pipeline/Lit**, then:

1. **Surface Type** (top of the Inspector, under Surface Options) → **Transparent**.
2. **Blending Mode** → **Alpha**.
3. **Base Map** — leave the texture slot empty, but click the **colour swatch** beside it
   and set the colour from the section 1 table. In the colour picker, drag the **A** (alpha)
   slider down to about `140` out of 255. That is what makes it see-through.
4. **Smoothness** → `0.9` for Water, Ice and Crystal; `0.1` for Steam and Smoke.

Water, Ice and Crystal get overwritten later by the Shader Graph materials in section 8 and
9 — these plain ones are just so the spheres look right in the meantime.

### 7.3 Emissive elements — Ember and Spark

Only these two are made here. Flame comes from `SG_Wave` (section 8), Storm from `SG_Arcs`
and Glow from `SG_Glow` (section 9) — but they all follow the same emission idea, and the
bloom setup at the bottom of this section applies to all five.

| Material | Base Map colour | Emission colour | Intensity | Smoothness |
|---|---|---|---|---|
| Mat_Ember | `#A32000` | `#FF4400` | 1.5 | 0.2 |
| Mat_Spark | `#FFD34D` | `#FFE680` | 5 | 0.6 |

Ember is deliberately dim and Spark is bright. That is the difference between a dying coal
and a live electrical spark, and it pairs with the `_Sharpness` values on `ElementPulse` —
Ember `n = 1` for a slow smoulder, Spark `n = 8` for a hard strobe. Same idea expressed
twice, in the material and in the script.

Create a material per element on **Universal Render Pipeline/Lit**, then:

1. Set **Base Map**'s colour swatch to the element colour from section 1.
2. Scroll to **Emission** and tick its checkbox.
3. Click the **Emission colour swatch**. In the picker, set the hex, then set the
   **Intensity** field to between `1.5` and `5` (Ember low, Spark high). Intensity is only
   there because the swatch is HDR — below about `1.5` it will not bloom at all.

Then turn bloom on, or none of the above shows up:

1. Select **Global Volume** in the Hierarchy → click its Profile → **Add Override →
   Post-processing → Bloom**. Threshold `0.9`, Intensity `1.0`, **Scatter `0.8`**.
2. Select **Main Camera** inside the XR Origin → in the Camera component, tick
   **Post Processing** under Rendering.

Step 2 is the one everyone misses. Emission without it just looks like flat bright colour.

**Scatter is the setting that matters most.** High scatter spreads the bloom into a wide
soft halo, which is what makes an emissive sphere read as a ball of fire rather than a ball
with a bright rim. Low scatter keeps it tight to the object.

**If the whole scene turns pink and hazy**, the threshold is too low — surfaces that are
merely bright, like white walls and the controllers, start blooming as well as the emissive
ones. Raise Threshold until only the elements glow. Judge this with the room lights
sensible: five point lights at high range in a small white room will wash everything out on
their own, and it is easy to blame bloom for it.

---

## 8. Shader Graph — vertex displacement (activity e)

Dynamic surfaces: **Water** (ripple), **Mud** (bubbling), **Flame** (flicker),
**Vine** (sway). One graph, reused with different parameters.

Vertex shaders run once per vertex and control *where* geometry sits, so this is the right
half of the graph for a moving surface.

1. `Assets/Shaders/` → right-click → **Create → Shader Graph → URP → Lit Shader Graph**.
   Name it `SG_Wave`.
2. Open it. In the Blackboard (`+` top left) add these exposed properties — the same four
   parameters as the lecture, so you can tune each material without reopening the graph:
   - `_Amplitude` (Float, `0.02`)
   - `_Frequency` (Float, `2`)
   - `_Phase` (Float, `0`)
   - `_Sharpness` (Float, `1`)
   - `_Colour` (Color)
   - `_Smoothness` (Float, `0.5`)
#### How to work in Shader Graph

Before the node list, three mechanics you need:

- **Adding a node:** right-click empty canvas → **Create Node**, then type the name. Hitting
  **Space** on the canvas does the same thing. Node names are exact — searching "position"
  brings up **Position**, **Object** (which has a Position *output* but is a different node),
  and others. Read the name in the list, don't just take the first hit.
- **Using a blackboard property:** drag it from the Blackboard panel onto the canvas. It
  appears as a small node with one output.
- **Connecting:** drag from a small circle on the right of one node to a circle on the left
  of another. To disconnect, drag the wire off the input and drop it on empty canvas.
- **Deleting:** click a node, press Delete.

Turn on the **Main Preview** (bottom right) and right-click it → **Sphere**, so you can see
the effect as you build. It only updates after you hit **Save Asset**.

#### 3. Build the wave in the Vertex half

Create these eleven nodes. Positions on the canvas don't matter, only the wiring.

The **#** column is just a label so you can tell the four identical Multiply nodes apart —
the wiring steps below refer back to it. It is not the order you have to create them in.

**Tip:** double-click a node's title bar to rename it. Renaming your four Multiplies to
"× frequency", "× sign", "× amplitude" and "× normal" makes the rest of this far easier to
follow, and makes the graph readable when you screen-record it for the video.

| # | Node to create | Setting | What it does |
|---|---|---|---|
| 1 | **Position** | Space → **Object** | where this vertex currently is |
| 2 | **Split** | — | pulls the Y channel out of Position |
| 3 | **Time** | — | makes the wave animate |
| 4 | **Add** | — | height + time |
| 5 | **Multiply** | — | × `_Frequency` |
| 6 | **Add** | second Add | + `_Phase` |
| 7 | **Sine** | — | the wave itself |
| 8 | **Absolute** | — | strips the sign before Power |
| 9 | **Power** | — | ^ `_Sharpness` |
| 10 | **Sign** | — | remembers the sign we stripped |
| 11 | **Multiply** | second Multiply | × sign, putting it back |
| 12 | **Multiply** | third Multiply | × `_Amplitude` |
| 13 | **Normal Vector** | Space → **Object** | direction to push this vertex |
| 14 | **Multiply** | fourth Multiply | × normal, giving the offset |
| 15 | **Add** | third Add | original position + offset |

Now wire them, in this order. `→` means drag from the first port to the second.

**Port widths below are what you'll see *after* connecting, not before.** Multiply, Add and
the other maths nodes have *dynamic* ports: they display `(1)` until you plug a wider value
in, then resize themselves. So when a step says **Multiply #14** `A(3)` and the node in
front of you shows `A(1)`, that is correct — connect the Vector3 and the label updates
itself. Only the ports on the Vertex and Fragment blocks have fixed widths.

**Part A — get `x`, the input to the wave**

1. **Position** `Out(3)` → **Split** `In(4)`
2. **Split** `G(1)` → **Add #4** `A(1)`

   `G` is the Y channel. Using height as the wave input means the top and bottom of the
   sphere are at different points in the cycle, which is what makes it look like a wave
   travelling across the surface rather than the whole ball throbbing at once.
3. **Time** `Time(1)` → **Add #4** `B(1)`

   Adding time is what animates it. Without this the wave is frozen in place.

**Part B — apply frequency and phase**

4. **Add #4** `Out(1)` → **Multiply #5** `A(1)`
5. Drag `_Frequency` from the Blackboard onto the canvas → its output → **Multiply #5** `B(1)`
6. **Multiply #5** `Out(1)` → **Add #6** `A(1)`
7. Drag `_Phase` onto the canvas → its output → **Add #6** `B(1)`

   That gives you `f x + p` — frequency compresses the cycle, phase slides where it starts.

**Part C — the sine, and the sharpness fix**

8. **Add #6** `Out(1)` → **Sine** `In(1)`
9. **Sine** `Out(1)` → **Absolute** `In(1)`
10. **Absolute** `Out(1)` → **Power** `A(1)`
11. Drag `_Sharpness` onto the canvas → its output → **Power** `B(1)`
12. **Sine** `Out(1)` → **Sign** `In(1)`  *(a second wire out of Sine — that's allowed)*
13. **Power** `Out(1)` → **Multiply #11** `A(1)`
14. **Sign** `Out(1)` → **Multiply #11** `B(1)`

    **Why Absolute and Sign are here.** The lecture writes the formula as
    `(A sin(f x + p))^n`, and in C# that works fine. But on the GPU, `pow()` is undefined
    for a negative base — it computes `exp(n · log(x))`, and the log of a negative number is
    NaN. A sine is negative half the time, so a plain Power node gives you NaN across half
    the sphere and the mesh tears apart. The fix is to raise the *magnitude* to the power,
    then multiply the sign back on: `sign(s) · |s|^n`. Identical result, no NaN. This is a
    good thing to point out in your video — it is a real difference between doing the maths
    on the CPU and on the GPU.

**Part D — apply amplitude and displace along the normal**

15. **Multiply #11** `Out(1)` → **Multiply #12** `A(1)`
16. Drag `_Amplitude` onto the canvas → its output → **Multiply #12** `B(1)`
17. **Normal Vector** `Out(3)` → **Multiply #14** `A(3)`
18. **Multiply #12** `Out(1)` → **Multiply #14** `B(1)`

    Multiplying a Vector3 by a float scales the whole vector. Displacing along each vertex's
    own normal makes the sphere swell and dent. Displacing along a fixed axis instead would
    shear it sideways.
19. **Position** `Out(3)` → **Add #15** `A(3)`  *(second wire out of Position)*
20. **Multiply #14** `Out(3)` → **Add #15** `B(3)`
21. **Add #15** `Out(3)` → **Vertex ▸ Position(3)**

#### 4. Fragment half

22. Drag `_Colour` onto the canvas → its output → **Fragment ▸ Base Color(3)**
23. Drag `_Smoothness` onto the canvas → its output → **Fragment ▸ Smoothness(1)**

Add one more property so this graph can drive emissive elements too:

24. Blackboard `+` → **Color**, name it `_EmissionColour`
25. In Node Settings: tick **HDR**, set **Default** to black, and set the **Reference**
    field to exactly `_EmissionColor` — the American spelling, no `u`
26. Drag it onto the canvas → its output → **Fragment ▸ Emission(3)**

**Why the Reference must be `_EmissionColor`.** That is the property name `ElementPulse.cs`
writes to with `SetColor`. Match it and the script's Pulse Emission works on Shader Graph
materials as well as URP Lit ones. Spell it any other way and the script silently does
nothing — no error, it just has no effect.

Defaulting to black means Water, Mud and Vine are unaffected. Only Flame turns it up.

#### 5. Save and check

Click **Save Asset** (top left of the Shader Graph window). The Main Preview sphere should
now be visibly rippling.

**If nothing moves:** check the Time node is actually connected — it is the most commonly
forgotten wire. **If the sphere goes black or disappears:** you have wired Power directly to
Sine and skipped the Absolute/Sign pair. **If it looks identical to a plain sphere:**
`_Amplitude` is probably still `0`.

#### 6. Create the four materials

| Material | _Colour | Amplitude | Frequency | Phase | Sharpness | Note |
|---|---|---|---|---|---|---|
| Mat_Water | `#2E7BC4` | 0.015 | 3 | 0 | 1 | smooth ripple |
| Mat_Mud | `#4E3B26` | 0.03 | 1 | 0 | 2 | slow fat bubbles |
| Mat_Flame | `#FF6A1E` | 0.025 | 12 | 0 | 5 | fast sharp flicker |
| Mat_Vine | `#3E8E41` | 0.010 | 0.8 | 0 | 1 | gentle sway |

To make one: right-click `SG_Wave` in the Project window → **Create → Material**. It comes
out already using the graph, with all six properties exposed in its Inspector. Set the
values from the table, then drag the material onto the sphere in the matching prefab.

**Every new material starts with the graph's default values**, so all four will be the same
colour until you set `_Colour` on each one individually. The graph default is only a
starting point — overriding it per material is the entire reason the property is exposed
rather than hard-coded into the graph.

Compare Mat_Vine (frequency `0.8`, sharpness `1`) with Mat_Flame (frequency `12`,
sharpness `5`). Same shader, same nodes — only four numbers differ, and one reads as a
plant swaying while the other reads as fire. That is the argument for exposing parameters
rather than hard-coding them, and it is worth saying in the video.

**Two things that will catch you out:**

- Displacing vertices does **not** recalculate normals, so at high amplitude the lighting
  goes wrong. Keep amplitude under about `0.04` on a 0.2-scale sphere.
- Unity's default sphere has around 500 vertices. That is enough here, but if the
  displacement looks faceted, that is why.

---

## 9. Shader Graph — procedural patterns (activity f)

Fragment shaders run once per pixel and decide *how* the surface is coloured, so complex
surface patterns belong here rather than in the vertex half.

Four graphs here. They're all shorter than `SG_Wave` and follow the same three mechanics:
right-click → **Create Node** to add, drag Blackboard properties onto the canvas to use
them, drag between ports to wire. Port widths shown are what you'll see *after* connecting.

Do them in order — 9.1 teaches the pattern and the rest are variations on it.

---

### 9.1 `SG_Voronoi` — Crystal and Ice

Voronoi divides the surface into randomised cells, which is what a faceted crystal or
cracked ice looks like.

**Create the graph:** `Assets/Shaders/` → right-click → **Create → Shader Graph → URP →
Lit Shader Graph**, name it `SG_Voronoi`.

**Blackboard properties** (`+` in the Blackboard panel):

| Property | Type | Default |
|---|---|---|
| `_ColourA` | Color | dark purple |
| `_ColourB` | Color | light purple |
| `_CellDensity` | Float | `8` |
| `_EdgeSharpness` | Float | `12` |

**Nodes to create:**

| # | Node | What it does |
|---|---|---|
| 1 | **Voronoi** | generates the cell pattern |
| 2 | **Power** | sharpens soft cells into hard facet edges |
| 3 | **Lerp** | blends between the two colours using the pattern |
| 4 | **One Minus** | inverts the pattern for the smoothness output |

**Wiring:**

1. Drag `_CellDensity` onto the canvas → its output → **Voronoi** `Cell Density(1)`
2. **Voronoi** `Out(1)` → **Power** `A(1)`
3. Drag `_EdgeSharpness` onto the canvas → its output → **Power** `B(1)`
4. **Power** `Out(1)` → **Lerp** `T(1)`
5. Drag `_ColourA` onto the canvas → its output → **Lerp** `A(4)`
6. Drag `_ColourB` onto the canvas → its output → **Lerp** `B(4)`
7. **Lerp** `Out(4)` → **Fragment ▸ Base Color(3)`
8. **Voronoi** `Out(1)` → **One Minus** `In(1)`  *(second wire out of Voronoi)*
9. **One Minus** `Out(1)` → **Fragment ▸ Smoothness(1)`
10. **Save Asset**

**What Power is doing here.** It's the same sharpness idea as `_Sharpness` in section 8:
raising a 0–1 value to a power pushes the middle towards zero and leaves only the top end,
so soft blobby cells become hard-edged facets. No Absolute/Sign needed this time, because
Voronoi only ever outputs 0–1 — there is no negative base to break `pow()`.

**Step 9 makes the edges rougher than the faces.** Real crystal catches light on flat faces
and scatters it along fractures, so inverting the pattern into Smoothness is doing physical
work, not just decoration.

**Two materials from this graph:**

| Material | `_ColourA` | `_ColourB` | Cell Density | Edge Sharpness | Surface Type |
|---|---|---|---|---|---|
| Mat_Crystal | `#5B3B8C` | `#C0A0FF` | 6 | 20 | Opaque |
| Mat_Ice | `#4A8CA8` | `#A8E4F0` | 14 | 6 | Transparent |

To set Surface Type on a Shader Graph material, open the graph → **Graph Inspector →
Graph Settings → Surface Type**. It's a graph-wide setting, not per material — so if you
want Ice transparent and Crystal opaque, either duplicate the graph or leave both opaque.
Leaving both opaque is fine and is one fewer thing to break.

---

### 9.2 `SG_Frost` — Frost

Perlin noise, straight out of the lecture. Same shape as 9.1 with two nodes swapped.

**Create:** `SG_Frost`, Lit Shader Graph.

**Blackboard:**

| Property | Type | Default |
|---|---|---|
| `_ColourA` | Color | `#8CB8CC` |
| `_ColourB` | Color | `#FFFFFF` |
| `_Scale` | Float | `25` |
| `_Threshold` | Float | `0.55` |

**Nodes:** **Simple Noise**, **Step**, **Lerp**.

**Wiring:**

1. Drag `_Scale` → **Simple Noise** `Scale(1)`
2. **Simple Noise** `Out(1)` → **Step** `In(1)`
3. Drag `_Threshold` → **Step** `Edge(1)`
4. **Step** `Out(1)` → **Lerp** `T(1)`
5. Drag `_ColourA` → **Lerp** `A(4)`
6. Drag `_ColourB` → **Lerp** `B(4)`
7. **Lerp** `Out(4)` → **Fragment ▸ Base Color(3)`
8. **Save Asset**

**Simple Noise is Perlin noise** — the same function from lecture slides 34–35. `Scale` is
the frequency parameter: higher means tighter and more jagged, lower means broad and
rolling. Try `5` and then `60` to see it.

**Step is what makes it read as frost.** It outputs 0 below the threshold and 1 above, so
smooth grey noise snaps into hard white crystals against the background colour. Take Step
out and you get soft grey mush. Drag `_Threshold` from `0.3` to `0.8` on the material and
watch the frost coverage thin out — that one number controls how frozen it looks.

Create one material, `Mat_Frost`, and drop it on `Element_Frost`.

---

### 9.3 `SG_Arcs` — Storm

Scrolling noise plus a hard threshold gives you flickering electrical bands.

**Create:** `SG_Arcs`, Lit Shader Graph.

**Blackboard:**

| Property | Type | Default |
|---|---|---|
| `_ArcColour` | Color (**tick HDR** in Node Settings) | purple, Intensity `4` |
| `_Scale` | Float | `30` |
| `_Threshold` | Float | `0.72` |
| `_ScrollSpeed` | Float | `6` |

**Nodes:** **Time**, **Multiply**, **Combine**, **Tiling And Offset**, **Simple Noise**,
**Step**, **Multiply** (a second one), **Color** (set to near-black).

**Wiring:**

1. **Time** `Time(1)` → **Multiply #1** `A(1)`
2. Drag `_ScrollSpeed` → **Multiply #1** `B(1)`
3. **Multiply #1** `Out(1)` → **Combine** `R(1)`

   Leave `G`, `B` and `A` at 0. You are building a Vector2 offset that moves along one axis
   only, so the arcs travel in a single direction rather than drifting diagonally.
4. **Combine** `RG(2)` → **Tiling And Offset** `Offset(2)`
5. **Tiling And Offset** `Out(2)` → **Simple Noise** `UV(2)`
6. Drag `_Scale` → **Simple Noise** `Scale(1)`
7. **Simple Noise** `Out(1)` → **Step** `In(1)`
8. Drag `_Threshold` → **Step** `Edge(1)`
9. **Step** `Out(1)` → **Multiply #2** `A(1)`
10. Drag `_ArcColour` → **Multiply #2** `B(4)`
11. **Multiply #2** `Out(4)` → **Fragment ▸ Emission(3)`
12. **Color** node (near-black) → **Fragment ▸ Base Color(3)`
13. **Save Asset**

**Why the threshold is so high.** At `0.72` only the brightest ~28% of the noise survives,
which leaves thin scattered bands rather than a solid glow. Lower it to `0.4` and Storm
turns into a purple ball. That single value is the difference between "lightning" and "lit".

Emission needs bloom to actually glow — section 7.3, step 2 on the camera.

Create `Mat_Storm` and drop it on `Element_Storm`.

---

### 9.4 `SG_Glow` — Glow

Fresnel makes the rim of a sphere brighter than its centre, which is what a glowing orb of
gas looks like. Then the same sine from the lecture makes it breathe.

**Create:** `SG_Glow`, Lit Shader Graph.

**Blackboard:**

| Property | Type | Default |
|---|---|---|
| `_GlowColour` | Color (**tick HDR**) | green, Intensity `4` |
| `_FresnelPower` | Float | `2` |
| `_PulseSpeed` | Float | `1.5` |

**Nodes:** **Fresnel Effect**, **Time**, **Multiply** ×3, **Sine**, **Remap**, **Color**
(near-black).

**Wiring:**

1. Drag `_FresnelPower` → **Fresnel Effect** `Power(1)`
2. **Time** `Time(1)` → **Multiply #1** `A(1)`
3. Drag `_PulseSpeed` → **Multiply #1** `B(1)`
4. **Multiply #1** `Out(1)` → **Sine** `In(1)`
5. **Sine** `Out(1)` → **Remap** `In(1)`
6. On the **Remap** node set **In Min Max** to `(-1, 1)` and **Out Min Max** to `(0.5, 1)`
7. **Fresnel Effect** `Out(1)` → **Multiply #2** `A(1)`
8. **Remap** `Out(1)` → **Multiply #2** `B(1)`
9. **Multiply #2** `Out(1)` → **Multiply #3** `A(1)`
10. Drag `_GlowColour` → **Multiply #3** `B(4)`
11. **Multiply #3** `Out(4)` → **Fragment ▸ Emission(3)`
12. **Color** node (near-black) → **Fragment ▸ Base Color(3)`
13. **Save Asset**

**Why Remap is there.** A sine swings from −1 to 1. Feed that straight into brightness and
the glow goes fully dark for half of every cycle and tries to go negative. Remapping to
`0.5–1` means it pulses between half brightness and full — a breathing glow rather than a
strobe. Change Out Min Max to `(0, 1)` if you want it to fade out completely.

This is the same `A sin(f t + p)` from the lecture and from `ElementPulse.cs`, done on the
GPU instead of the CPU. Worth pointing at in your video: identical maths, two places, and
the GPU version runs per-pixel while the C# one runs once per frame per object.

Create `Mat_Glow` and drop it on `Element_Glow`.

---

### 9.5 Check your work

| Element | Should look like | If it doesn't |
|---|---|---|
| Crystal | hard-edged purple facets | Edge Sharpness too low, try `20` |
| Ice | fine pale blue cracks | Cell Density too low, try `14` |
| Frost | white crystals on pale blue | Step not wired — you'll see grey mush |
| Storm | thin purple arcs crawling over black | no movement means Time isn't connected |
| Glow | green rim that breathes | no glow means bloom is off (section 7.3) |

Save each graph, make a material from it, assign it to the matching element.

---

## 10. Particle systems (activity g)

Only six elements get a particle system: **Flame, Ember, Spark, Storm** (on
`EnergyElement`) and **Steam, Smoke** (on `GasElement`). Solid and Fluid elements do not —
they get their look from textures and shaders instead.

### 10.0 How to read the Particle System inspector

This is the bit that trips everyone up the first time. When you add a Particle System, the
Inspector shows a **stack of modules**, not a flat list of settings:

- The **top block** has no name of its own — it just shows the object's name as its header.
  This is the **Main module**. Duration, Looping, Start Lifetime, Start Speed, Start Size,
  Start Color, Gravity Modifier, Simulation Space, Play On Awake and Max Particles all live
  here.
- **Below it** is a list of module names, each with a **tick box** on the left: Emission,
  Shape, Velocity over Lifetime, Color over Lifetime, Size over Lifetime, Noise, Renderer,
  and so on.
  - Clicking the **tick box** turns that module on or off.
  - Clicking the **name** expands it so you can see its settings.
  - A module that is ticked but collapsed is still running.
- Emission, Shape and Renderer are ticked by default. Everything else starts off.

So when the table below says "Rate `40`", that means: expand **Emission**, set
**Rate over Time** to `40`. Here is where every value in this section lives:

| Setting | Module | Field name |
|---|---|---|
| Lifetime | Main | Start Lifetime |
| Start Speed | Main | Start Speed |
| Start Size | Main | Start Size |
| Gravity Mod | Main | Gravity Modifier |
| Simulation Space | Main | Simulation Space |
| Max Particles | Main | Max Particles |
| Rate | Emission | Rate over Time |
| Shape / Radius | Shape | Shape, Radius |
| Material | Renderer | Material |
| Colour over Lifetime | Color over Lifetime | the gradient bar |
| Size over Lifetime | Size over Lifetime | the curve |

### 10.1 Shared setup

Do this once on `GasElement` and once on `EnergyElement`, so all their children inherit it.

1. Open the family prefab (double-click it in the Project window).
2. Select the `ParticleAnchor` child → **Add Component** → **Particle System**.
3. In the **Main module** (the top block):
   - **Simulation Space** = **World** — so particles trail behind when you carry the sphere.
     Left on **Local** the whole plume teleports with your hand, which looks wrong. This is
     the single most important setting here
   - **Max Particles** = `100` — the default of 1000 is far too many with 16 spheres on
     screen at once
   - **Start Speed** = `0.5` and **Start Lifetime** = `1` for now; the per-element table
     overrides these
   - **Start Size** = `0.05` — the default of `1` gives you particles five times bigger than
     the sphere itself
4. **Shape** module → Shape = **Sphere**, Radius = `0.1`.
5. **Renderer** module → **Material**. You have to *create* this material — nothing in the
   picker list will work, and Unity's default particle material is built for the old
   pipeline, which is why unassigned particles show up as magenta squares.
   1. In `Assets/Materials/`, right-click → **Create → Material**, name it
      `Mat_Particle_Additive`.
   2. Select it. In the **Shader** dropdown at the top, choose
      **Universal Render Pipeline → Particles → Unlit**.
   3. **Surface Type** = **Transparent**, **Blending Mode** = **Additive**. Additive is
      what makes overlapping particles read as density rather than as flat overlapping
      quads.
   4. **Base Map** → click the small circle to its left → choose **ParticleSoftDot**
      (in `Assets/Textures/`). Without a texture every particle is a hard-edged square,
      which no element should look like.
   5. Back on `ParticleAnchor`, drag `Mat_Particle_Additive` into the Renderer module's
      Material slot.
6. Save and exit Prefab Mode.

`ParticleSoftDot.png` is a 128×128 white radial gradient that fades to zero alpha at the
edge. One greyscale texture serves all six elements because the particle system tints it —
the colour comes from Start Color and the Color over Lifetime gradient, not from the
texture. Mention that in your submission: it is one texture and one material shared across
every particle effect, rather than six separate assets.

If the preview looks like a firehose, it is because the Unity defaults are Start Speed `5`,
Start Lifetime `5` and Max Particles `1000`. Fix those four Main-module values first and
everything else becomes easier to judge.

### 10.2 Per element

Set these on each element variant, not on the family prefab.

These are sized for a sphere at scale **0.5**. If your `ElementBase` is a different scale,
multiply the sizes and the shape radius to match.

| | Rate | Lifetime | Start Speed | Start Size | Gravity Mod | Colour over Lifetime |
|---|---|---|---|---|---|---|
| Flame | 30 | 0.4 | 0 | 0.3 | −0.2 | red → yellow → white → transparent |
| Ember | 8 | 1.5 | 0 | 0.08 | −0.05 | dark red → orange → fade |
| Spark | 25 | 0.4 | 2.0 | 0.04 | 0.4 | white → yellow → fade fast |
| Smoke | 12 | 3.0 | 0 | 0.2 → 0.5 | −0.08 | dark grey, alpha 0.4 → 0 |
| Steam | 18 | 2.0 | 0 | 0.15 → 0.4 | −0.12 | white, alpha 0.5 → 0 |
| Storm | 30 | 0.3 | 1.2 | 0.05 | 0 | purple → white → fade |

**Start Speed `0` on Flame, Ember, Smoke and Steam.** With a Sphere shape, any speed above
zero launches each particle outward in a random direction, so you get specks scattered
evenly around the ball instead of a plume rising off it. At `0` the only force acting is
gravity, so they all move together. Spark is the exception — it *should* spray outward,
which is why it keeps a high speed and positive gravity.

**Fewer, bigger, shorter-lived beats many, small, long-lived.** Additive particles only read
as a body of fire when they overlap. Lots of small ones drifting a long way read as embers,
not flame — and with seven systems running at once in World space they fill the room with
what looks like snow.

Negative Gravity Modifier makes particles rise — that is what separates Flame/Smoke/Steam
from Spark, which is thrown out and pulled back down.

**Where the two-value entries go.** `0.06 → 0.01` means it starts at `0.06` and shrinks to
`0.01`. Set **Start Size** to the first number in the Main module, then tick **Size over
Lifetime** and drag its curve down to the right. Single values just go in Start Size, and
you leave Size over Lifetime off.

**Setting a Colour over Lifetime gradient:**

1. Tick **Color over Lifetime**, then click the module name to expand it.
2. Click the **coloured bar** — a Gradient Editor window opens.
3. The markers **below** the bar are colour keys, the ones **above** are alpha keys.
4. Click anywhere under the bar to add a colour key, then click it and pick a colour. For
   Flame you want four: red at 0%, orange/yellow at 35%, white at 70%, dark at 100%.
5. Click above the bar at 100% and drag alpha to `0` so the particle fades out instead of
   vanishing.

This is the setting that most decides whether an element reads correctly — the task sheet
calls it out specifically for flame (red → yellow → white → black/transparent).

**Two more modules worth ticking:**

- **Limit Velocity over Lifetime** — Damping `0.3` on Smoke and Steam, so they slow as they rise
- **Noise** — Strength `0.15`, Frequency `1.5` on Flame, Smoke and Steam, for turbulence

### 10.3 Object particles (activity h)

For **Sand** and **Ash**, which should physically fall and pile up:

1. Make a tiny cube prefab `Particle_Grain`, scale `0.01`, with a **Rigidbody** (mass
   `0.001`) and a **Box Collider**. Save it to `Assets/Prefabs/`.
2. On the Sand sphere's `ParticleAnchor`, add **Object Particle Emitter**:
   - Particle Prefab `Particle_Grain`, Spawn Interval `0.15`, Particle Lifetime `4`
   - Emit Direction `(0, -1, 0)`, Emit Speed `0.3`, Spread Angle `35`
   - Min/Max Scale `0.008` / `0.02`
3. Ash: Spawn Interval `0.25`, Lifetime `6`, Emit Speed `0.15`, Spread `60`.

Keep the spawn interval above `0.1`. Every grain is a rigidbody, and physics bodies are far
more expensive than normal particles — that trade-off is the point of activity (h), and is
worth a sentence in your submission.

---

## 11. Audio

Source CC0 loops from **freesound.org** (filter Licence = Creative Commons 0).

| Element | Ambient loop | Grab one-shot |
|---|---|---|
| Flame / Ember | fire crackle | whoosh |
| Spark / Storm | electric buzz | zap |
| Water | water lapping | splash |
| Ice / Frost / Crystal | — | glass chime / crack |
| Steam | steam hiss | hiss burst |
| Smoke | — | soft puff |
| Stone / Sand / Mud / Ash | — | thud / grit / squelch |
| Vine | — | rustle |
| Glow | low hum | soft chime |

1. Drop the files into `Assets/Audio/`.
2. Assign to **Ambient Loop** and **Grab Clip** on each sphere's `ElementSphere` component.
   The script handles looping, volume and 3D spatialisation — nothing else to wire up.

Only give ambient loops to Flame, Ember, Spark, Storm, Water, Steam and Glow. Sixteen
simultaneous loops is just noise; the rest get grab sounds only.

---

## 12. Test

1. Press Play. Simulator: **Left Shift** = left controller, **Space** = right, **Tab** =
   head, **left mouse** = trigger, **G** = grip.
2. Check:
   - [ ] Every sphere is grabbable and the physics reads right (Stone drops fast, Steam drifts up)
   - [ ] Labels face you from anywhere in the room
   - [ ] Emissive elements bloom — if not, Post Processing is off on the camera
   - [ ] Particles trail when you carry a sphere (Simulation Space = World)
   - [ ] Ambient audio fades with distance
   - [ ] No console errors, no pink materials (pink = shader isn't URP compatible)
3. **Window → Analysis → Profiler** — anything above 11 ms per frame is a problem for a
   90 Hz headset. Usual causes: too many object particles, or 4K textures.

Haptics do not fire in the XR Device Simulator. Test on a headset, or say in your
submission that the values are set but untested on hardware.

---

## 13. Stage 2 — the combining game

Plan only. Come back once sections 1–12 work.

1. New script `ElementCombiner` on `ElementBase`, with a serialized list of recipes
   (element name A, element name B, result prefab).
2. `OnCollisionEnter` → read the other object's `ElementSphere.ElementName` → look for a
   matching recipe → destroy both, spawn the result at the midpoint, play a burst particle
   effect and a strong haptic.
3. Only combine while at least one sphere is held, otherwise spheres resting on the shelf
   fuse on their own.
4. Recipes, all producing elements from the required list:
   Flame + Water → Steam · Water + Frost → Ice · Flame + Stone → Ember ·
   Ember + Ash → Smoke · Water + Sand → Mud · Storm + Sand → Crystal ·
   Flame + Vine → Ash · Spark + Smoke → Storm · Ice + Flame → Water · Glow + Stone → Crystal
5. Add a respawner so consumed elements come back on the shelf.

---

## Appendix A — Assembling one element end to end

Sections 1–12 build the ingredients. This appendix is the assembly line: do one element
completely, confirm it works, then repeat. That is far less painful than setting one field
across all 16 prefabs and discovering at the end that something upstream was wrong.

Start with **Flame**. It is the most complex element you have — shader, particles, pulse,
light, audio and haptics all at once — so every problem the other fifteen can produce shows
up here first. It is also in the Energy family, so half of the fixes propagate to Ember,
Spark, Storm and Glow automatically.

### A.1 — Fix the family prefab first

Anything you fix here lands on every child, so it is always worth doing before touching an
individual element. Double-click `EnergyElement` in the Project window to open Prefab Mode.

1. Click each object in the Hierarchy in turn — root, `ParticleAnchor`, `Label` — and check
   where the Particle System actually is. If there is more than one, remove the one that is
   **not** on `ParticleAnchor`: right-click the component header → **Remove Component**, and
   remove its **Particle System Renderer** as well.

   A Particle System on the root is a real problem, not just untidy: the root already has
   the sphere's Mesh Renderer, and a GameObject cannot have two renderers behaving sensibly.
2. Select `ParticleAnchor` → **Add Component → Light**. Type **Point**, Range `1`,
   Intensity `2`, Color a warm orange. This is what casts light onto the shelf.
3. On `ParticleAnchor`'s Particle System, expand the **Renderer** module and confirm
   **Material** is `Mat_Particle_Additive`. An empty slot here is what produces magenta
   squares instead of particles.
4. Select the **root** and confirm **Element Pulse** is present.
5. Save, exit Prefab Mode.

Repeat the equivalent checks on `GasElement`, `SolidElement` and `FluidElement`. Solid and
Fluid should have **no** particle system at all.

### A.2 — Worked example: `Element_Flame`

Open `Element_Flame` in Prefab Mode. Select the **root** object.

| # | Where | Set |
|---|---|---|
| 1 | Rigidbody | Mass `0.2` |
| 2 | Element Sphere → Element | Element Name `Flame` |
| 3 | Element Sphere → Haptics | Grab Amplitude `0.35`, Grab Duration `0.10` |
| 4 | Element Sphere → Physics | Buoyancy `0.3` |
| 5 | Element Sphere → Audio | Grab Clip assigned |
| 6 | Mesh Renderer → Materials | Element 0 = `Mat_Flame` |
| 7 | Element Pulse | Amplitude `1`, Frequency `6`, Phase `0`, Sharpness `3` |
| 8 | Element Pulse | Pulse Scale ✔ `0.04`, **Pulse Emission ✘** (see section 6) |

Then select the `Label` child:

| # | Where | Set |
|---|---|---|
| 9 | TextMeshPro | Text = `Flame` |

Then select `ParticleAnchor`:

| # | Where | Set |
|---|---|---|
| 10 | Particle System → Main | Start Lifetime `0.6`, Start Speed `0.5`, Start Size `0.06` |
| 11 | Particle System → Main | Gravity Modifier `-0.15`, Simulation Space **World**, Max Particles `100`, Prewarm **off** |
| 12 | Emission | Rate over Time `40` |
| 13 | Shape | Sphere, Radius `0.1` |
| 14 | Color over Lifetime | tick, gradient red → yellow → white, alpha `0` at 100% |
| 15 | Size over Lifetime | tick, curve descending to near zero |

Save and exit Prefab Mode.

### A.3 — Verify before moving on

Press **Play** and look for four things:

| Should happen | If it doesn't |
|---|---|
| Sphere pulses gently in size | Element Pulse missing, or Scale Amount is `0` |
| Flame plume rises off it | check Rate over Time, and Gravity Modifier is negative |
| Warm light on the shelf below | Point light missing from `ParticleAnchor` (A.1 step 2) |
| Sound and grab work in the simulator | Grab Clip empty, or no Audio Listener on Main Camera |

Magenta particles mean the Renderer module has no material. Particles that vanish the
instant you pick the sphere up mean Simulation Space is on Local, not World.

Only move to the next element once all four are true.

### A.4 — The repeatable checklist

For every remaining element, walk this list. Most take two or three minutes.

1. Rigidbody **Mass** — section 5 table
2. Element Sphere: **Element Name**, **Grab Amplitude**, **Grab Duration**, **Buoyancy** — section 5 table
3. Element Sphere: **Ambient Loop** and **Grab Clip** — section 11 table
4. Mesh Renderer: **Material** — section 7 table
5. `Label` child: **Text** = element name
6. If Energy: **Element Pulse** values — section 6 table
7. If it has a particle system: the six values and the gradient — section 10.2
8. If Sand or Ash: **Object Particle Emitter** on `ParticleAnchor` — section 10.3
9. Press Play, check it, then move on

### A.5 — What each element needs

Use this to know when an element is finished.

| Element | Material | Particles | Pulse | Object particles | Ambient audio |
|---|---|---|---|---|---|
| Flame | Mat_Flame | ✔ | scale only | — | ✔ |
| Ember | Mat_Ember | ✔ | scale + emission | — | ✔ |
| Spark | Mat_Spark | ✔ | scale + emission | — | ✔ |
| Storm | Mat_Storm | ✔ | scale only | — | ✔ |
| Glow | Mat_Glow | ✔ | scale only | — | ✔ |
| Steam | Steam | ✔ | — | — | ✔ |
| Smoke | Smoke | ✔ | — | — | — |
| Water | Mat_Water | — | — | — | ✔ |
| Ice | Mat_Ice | — | — | — | — |
| Frost | Mat_Frost | — | — | — | — |
| Crystal | Mat_Crystal | — | — | — | — |
| Stone | Mat_Stone | — | — | — | — |
| Sand | Sand | — | — | ✔ | — |
| Mud | Mat_Mud | — | — | — | — |
| Ash | Ash | — | — | ✔ | — |
| Vine | Mat_Vine | — | — | — | — |

Every element gets a material, a label, a grab sound, haptics, a mass and a buoyancy value.
The columns above are only the extras on top of that.

---

## 14. Submission checklist

- [ ] All 16 elements, each with a visual plus at least one of audio/haptic
- [ ] Hierarchy justified in writing (activity c)
- [ ] At least one element per technique: texture (d), vertex shader (e), fragment shader (f),
      particle system (g), object particles (h)
- [ ] Texture and audio sources credited
- [ ] No console errors
- [ ] Video: show it running in the Game view **and** walk through the code. The unit
      wants your logic and reasoning, not a line-by-line reading of the syntax — so explain
      *why* Spark uses `n = 8` and Ember uses `n = 1`, not what `Mathf.Pow` does
