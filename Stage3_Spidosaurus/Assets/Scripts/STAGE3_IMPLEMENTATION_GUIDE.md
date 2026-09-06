# Stage 3 guide

This tells you what each piece has to achieve, what you have to decide, and how to know it
works. It does not give you the code or the click order. Where you need an API, look it up.


---

## What already exists

Project `C:\Code\VR\Stage3_Spidosaurus`, Unity 6000.0.79f1, URP, XRI 3.0.11, OpenXR with the
XR Device Simulator. Scene has the XR rig, the creature and a terrain of box colliders on a
Ground layer.

Measured facts about the supplied rig, from `RigDump.txt`:

| | |
|---|---|
| Bones under `SpidosaurBones` | 89 |
| Top-level appendages | 14 |
| Junctions (2+ children) | 10 |
| Feet | 23 |
| Limbs that branch | 7 of 14 |
| Limbs with a junction below a junction | 2 (`Bone.017`, `Bone.025`) |

The solved chain: hip `Bone.008`, then `Bone.009`, `Bone.010`, junction `Bone.011`, feet
`Bone.012_end` and `Bone.013_end`. Full extension 1.28 m and 1.32 m.

---

## The five pieces and what each is responsible for

Keep these separate. Most of the time lost on this project came from one piece quietly doing
another's job.

**The chain.** Turn the Transform hierarchy into something the solver can walk, and cache what
never changes: bone lengths and bind rotations, measured once. It answers "how far can this foot
reach". It does not move anything.

*You decide:* how to find the bones between root and tips, and how to order them so a parent is
always visited before its child. That ordering is used in both directions, so get it right once.

*Verify:* it reports 8 joints and 2 feet on `Bone.008`.

**The solver.** Given foot goals, produce bone rotations. Backward pass toward the hip, forward
pass back down, a fixed number of times, then write rotations. The junction is placed at the
weighted average of what its children ask for, weighted by how much foot weight sits below each.

*You decide:* what happens when a direction is degenerate, how you clamp a joint, and whether the
solve is allowed to see last frame's result.

*Verify:* planted error under 15 mm standing, and the same pose from the same targets every frame.

**The gait.** Decide which foot is planted, ease the weight so it never steps, plan where each
foot should land, and stop two feet claiming the same ground.

*You decide:* what triggers a step, what the foot is aiming at, and whether a foothold in flight
is allowed to change.

*Verify:* standing still, both feet report planted and nothing moves.

**The body.** Own the creature's height and heading. Nothing else may write them.

*You decide:* where the ground height comes from, and how hard it is smoothed.

*Verify:* the body settles and stays settled with no input.

**The probe.** Measure per-frame movement of body, hip, targets and tips, and report the largest
value over a window. It changes nothing.

*Verify:* when something twitches, one row is obviously larger than the rest.

---

## Questions to answer before you write each piece

Write your answer down. If you cannot answer one, that is the thing to go and read about.

**Chain**
- Why cache bone lengths at bind time instead of measuring them each frame?
- What breaks if a child is processed before its parent?

**Solver**
- Why does dividing by summed weight instead of child count change the behaviour at all?
- What does the solver gain and lose by running a fixed number of passes?
- If a bone cannot reach its target because of a joint limit, where does that error go?

**Gait**
- Why store the resting position in the body's local space rather than world space?
- What is the velocity lead compensating for?
- If a foot is standing on the best spot the planner can offer, how does the trigger know not to
  step again?

**Body**
- Why is a Rigidbody the wrong tool here?
- What is the feedback path if body height is derived from the feet, and what stops it running away?

---

## Three traps, found by measurement on this project

These cost hours. They are also the strongest material in your report.

**A solver with joint limits cannot warm-start from its own output.** Seeding from last frame is
only valid if last frame's pose is the pose the solver asked for. Once a clamp changes the result,
the solve depends on its own output and never settles at a fixed pass count.

**Do not recover a rotation axis from a quaternion near 180 degrees.** Every axis is valid there,
so the answer changes frame to frame. Work from the two direction vectors instead. Measured
effect: 201 mm of tip movement against 6 mm of target movement.

**The step trigger and the step planner must agree.** If the trigger measures drift against an
ideal position the planner will never deliver, the foot lands, still reads as too far, and steps
again forever.

---

## Verification ladder

Do not move down until the current line passes.

| Check | Pass condition |
|---|---|
| Chain builds | 8 joints, 2 feet |
| Rotations write | Rig holds bind pose, no errors, nothing drifts |
| Solver tracks | Drag a target in Play, the foot follows |
| Solver is stable | Probe: tip movement 1 to 3 mm standing still |
| Gait rests | Both feet planted, targets static |
| Gait walks | Feet stay under the body while driving |
| Contact | Planted error under 15 mm, toes on the surface |
| Cost | Solver visible in the Profiler, inside 11.1 ms |

---

## Where to look things up

FABRIK and the shared-joint rule: Aristidou and Lasenby (2011). Joint limits: Aristidou,
Chrysanthou and Lasenby (2016). Who decides which feet are planted: Karim et al. (2013). The
15 mm figure: Guan et al. (2023). Predictive foot placement: Roche and Torres-Cros (2016).

Unity: execution order and `LateUpdate`, `Quaternion.AngleAxis`, `Vector3.Cross` and
`Vector3.Angle`, `Transform.rotation` versus `localRotation`, `TransformPoint` and
`InverseTransformPoint`, `Physics.Raycast`, `Profiler.BeginSample`.

The Week 7 class task is the closest working reference on this same rig. It solves the same
creature with springs rather than IK, and it handles a junction by ignoring all but the first
child. Read `SpidosaurRig.LateUpdate` before writing your rotation write-back.

---

## Evidence to capture as you go

Rig hierarchy with the junction selected. The scene running through the simulator. Probe output
before and after a fix. Startup log with the reach figures. The readout during locomotion. The
Profiler with the solver sample visible.

Record the numbers as you get them: planted error standing and walking, worst case, solver cost,
tip movement per frame. Those are what the report is built on.

---

## What to write up

Not what the code does. Why it is shaped that way, and where else that shape applies.

The junction is a contested resource and you arbitrate by weight. Ground is a claimed region and
you resolve overlaps after claiming. Cost is fixed because a late frame in VR is judder. Each of
those is a pattern, and each generalises past this creature.

Then say what still fails, with numbers. A measured limitation scores better than silence.
