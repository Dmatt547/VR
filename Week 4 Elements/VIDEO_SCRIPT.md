# Week 4 — Element Spheres: Video Script

**Runtime:** about 4:12 · Only the code section changed — everything before 2:08 is unchanged,
so your existing footage still matches.

**If you need it back near 3 minutes,** cut the `Billboard` block (~25s) and the
subscribe/unsubscribe sentence in `ElementSphere` (~10s). Billboard is the least
mark-bearing of the four — it isn't tied to any of the five techniques.

Read the quoted lines out loud. The bold lines tell you what to show — don't read those.

---

## 0:00 – 0:18 · Opening

**Show:** Game view, standing in front of the three shelves. Pan slowly across all 16.

> This is my Week 4 project — sixteen element spheres, built in Unity 6 with URP and the XR
> Interaction Toolkit. You can pick all of them up, they all give haptic feedback, and each
> one uses a different rendering technique depending on what it's meant to be made of. I'll
> go through the prefabs, the shaders, the particles, then the code.

---

## 0:18 – 0:42 · Prefabs

**Show:** `Assets/Prefabs/` → click `Elementbase`, then the four family prefabs, then open
the `Elements` folder.

> I set the prefabs up like a class hierarchy. `Elementbase` is the parent. It has the mesh,
> the collider, the grab component, the audio source and the label. Under that are four
> family prefabs — Solid, Fluid, Gas and Energy. Energy adds a particle system and a light,
> because all five Energy elements need one.
>
> The sixteen elements are variants of those families. So I set grabbing up once, on the
> parent, and all sixteen inherited it.

---

## 0:42 – 1:34 · Shaders

**Show:** Open `SG_Wave`. Point at Sine, then Absolute, then Power, then the Sign wire.

> For the moving surfaces I used the formula from the lecture — a sine wave, with amplitude,
> frequency, phase, and a power to sharpen it.
>
> There's a catch on the GPU. Raising a negative number to a power gives you NaN, because the
> GPU works `pow` out using logarithms, and you can't take the log of a negative number. A
> sine is negative half the time, so my sphere kept tearing apart. The fix was to take the
> absolute value, raise that to the power, then multiply the sign back on. That's a real
> difference between doing this in C# and doing it on the GPU.

**Show:** `Mat_Flame` and `Mat_Vine` in the Inspector, side by side.

> `SG_Wave` drives Flame and Vine. Flame is frequency twelve, sharpness five. Vine is nought
> point eight and one. Same nodes, four numbers apart — one looks like fire, the other looks
> like a plant swaying.

**Show:** Open `Mud_Shader`. Click a Position node so the space dropdown reading **Object**
is visible. Then Crystal, Ice, Storm and Glow in the Game view.

> `Mud_Shader` is the same idea, plus a texture. These Position nodes have to be set to
> Object space. I had them on World, so the pattern stuck to the room instead of the ball,
> and the surface twisted every time I picked it up.
>
> For surface patterns I used Voronoi on Crystal and Ice, moving noise with a hard cut-off
> for Storm's arcs, and a Fresnel for Glow's rim.

---

## 1:34 – 2:08 · Particles

**Show:** Play mode. Close on Spark, then Ember, then Sand's grains landing on the shelf.

> Spark fires three hundred particles a second. Each one lasts a fifth of a second, and
> gravity is positive, so they arc and drop. Ember runs at forty a second, half a second
> each, and gravity is negative, so the flakes float up. Same settings panel, opposite
> numbers.
>
> Sand works differently. I wrote an emitter that spawns actual GameObjects with rigidbodies,
> so the grains hit the shelf and pile up. Every grain is a physics object, so it's
> expensive. That's why I used it on one element and not all sixteen.

---

## 2:08 – 4:00 · Code

**Show:** The `Scripts` folder, so all four files are visible at once.

> Now the code. There are four scripts. Most of this project is Unity's own systems — the
> Interaction Toolkit does the grabbing, Shader Graph does the visuals, the particle systems
> are all Inspector settings. So I only wrote code where those couldn't reach.

---

**Show:** `ElementSphere.cs`. Scroll past `OnEnable` / `OnDisable`, then `SendHaptics`, then
`FixedUpdate`.

> `ElementSphere` is on all sixteen prefabs. It listens for the grab event — it subscribes in
> OnEnable and unsubscribes in OnDisable, so nothing is left hanging when an object gets
> disabled.
>
> When you grab a sphere it sends a haptic pulse. It has to search upward from the interactor
> in the event arguments to find the haptic player, because that lives on the controller, not
> on the sphere.
>
> It also handles buoyancy. Unity's 2D physics has a gravity scale per object, but 3D physics
> doesn't — so I add an upward force every physics step, in FixedUpdate rather than Update.
> Steam is nought point eight, so it falls at a fifth of normal speed. Stone is zero, so it
> drops normally.

---

**Show:** `ElementPulse.cs` — `Awake`, then `CalculateWave`. Then Ember and Spark's Inspector
values side by side.

> `ElementPulse` is on Flame and the Energy family. In Awake it takes its own copy of the
> material, so pulsing one sphere doesn't pulse every sphere sharing that material.
>
> Then every frame it runs the lecture formula — amplitude, times sine of frequency times
> time plus phase, raised to a power. The wave comes out between minus one and one, so before
> using it as brightness I remap it into zero to one.
>
> Ember uses a power of one — a smooth, slow glow. Spark uses eight, which flattens the
> bottom of the wave and leaves sharp spikes, so it flashes. Same equation, one number apart.
> And that same formula is in the project twice — here on the CPU, and again in `SG_Glow` on
> the GPU, running per pixel.

---

**Show:** `ObjectParticleEmitter.cs`, then cut to Sand's grains landing on the shelf.

> `ObjectParticleEmitter` is on Sand. Unity's particle system can't spawn objects with
> rigidbodies, so this instantiates real GameObjects on a loop — spawn one, await the spawn
> interval, repeat.
>
> Each grain gets a random rotation, a random size, and a force in a randomly tilted cone, so
> they spray rather than fall in a straight line. Then it destroys each one after four
> seconds, because otherwise they'd build up forever and the frame rate would drop.

---

**Show:** `Billboard.cs`, then a label in the Game view as you walk past it.

> `Billboard` is on every label. World space text is only readable when it's facing you, so
> this turns each label towards the headset. It runs in LateUpdate rather than Update, so it
> happens after the rig has finished moving that frame. And it zeroes out the vertical
> difference, so the text stays upright instead of tilting when you look down at the bottom
> shelf.

---

**Show:** Back to `ElementSphere.cs`, scrolled to `SendHaptics()`.

> One thing I'd change. My haptics code duplicates a component the Interaction Toolkit
> already ships, called Simple Haptic Feedback. It does the same job from the Inspector. I'd
> delete mine and use that.

---

## 4:00 – 4:12 · Close

**Show:** Pull back to the full shelf view. Pick up an element and put it down.

> So that's textures, vertex shaders, fragment shaders, particle systems and object particles
> — one element showing each, with the shared behaviour up in the prefab parents. Thanks for
> watching.

---

## Things to know before you record

**There is no audio in the project.** `Assets/Audio/` is empty, and `ambientLoop` and
`grabClip` are unassigned on all sixteen prefabs. `PlayAmbientLoop()` returns immediately when
the clip is null, and `OnGrabbed` skips `PlayOneShot`. So don't claim sound on camera — the
script above only mentions haptics now. §14 asks for "a visual plus at least one of
audio/haptic", so haptics alone satisfies it.

**Buoyancy slows the fall, it doesn't make things float.** The script does:

```csharp
sphereRigidbody.AddForce(-Physics.gravity * buoyancy * sphereRigidbody.mass);
```

Default `ForceMode.Force`, so upward acceleration is `9.81 × buoyancy` against `9.81` of
gravity. At Steam's `0.8` the net is still `1.96 m/s²` **downward**. It falls at a fifth of
normal speed. Nothing floats unless buoyancy is above `1`.

If you want Steam to genuinely rise, set its buoyancy to `1.15` and change the line to "Steam
is one point one five, so it drifts upward." One number, and the claim becomes true.

Also worth knowing: mass cancels out. The force is multiplied by mass, then acceleration
divides by mass again — so Stone's mass of `3` and Steam's `0.1` make no difference to how
buoyancy behaves. Don't say mass affects it.

**`SG_Frost` isn't being used.** `Element_Frost` is on `Frost.mat`, a plain URP Lit material
with a texture — not `Mat_Frost`. The script never mentions Frost, so you're fine as written.
Just don't improvise about it on camera.

**Unused materials in the folder.** `Mat_Mud`, `Mat_Smoke`, `Mat_Water`, `Mat_Frost` and
`Ice.mat` are all sitting there unassigned. Delete them if you're going to scroll that folder
on camera, otherwise ignore it.

---

## Numbers in the script, checked against your files

| What you say | Where it's from |
|---|---|
| Energy adds a particle system and a light | `EnergyElement.prefab` |
| Flame: frequency 12, sharpness 5 | `Mat_Flame.mat` |
| Vine: frequency 0.8, sharpness 1 | `Mat_Vine.mat` |
| Absolute, Sign, Power and Sine all in the graph | `SG_Wave.shadergraph` |
| Position nodes set to Object space | `Mud_Shader.shadergraph` |
| Voronoi on Crystal and Ice | `Mat_Crystal.mat`, `Mat_Ice.mat` |
| Storm uses noise with a threshold | `SG_Arcs.shadergraph` |
| Glow uses Fresnel | `SG_Glow.shadergraph` |
| Spark: 300/sec, 0.2s life, gravity +0.2 | `Element_Spark.prefab` |
| Ember: 40/sec, 0.5s life, gravity −0.05 | `Element_Ember.prefab` |
| Sand emitter spawns rigidbody grains | `ObjectParticleEmitter.cs`, `Element_Sand.prefab` |
| Steam buoyancy 0.8, Stone 0 | `Element_Steam.prefab`, `Element_Stone.prefab` |
| Spark power 8, Ember power 1 | `Element_Spark.prefab`, `ElementPulse.cs` |
| Buoyancy applied in FixedUpdate | `ElementSphere.cs` |
| Haptic player found via event args | `ElementSphere.SendHaptics()` |
| Steam falls at 1/5 speed, not floating | maths on `AddForce` line, buoyancy `0.8` |
