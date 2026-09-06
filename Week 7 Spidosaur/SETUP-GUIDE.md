# Week 7 - Squishy Spidosaur

Build guide for the Week 7 workshop challenge (Movement & Object Interaction: a squishy, stretchable object that can be manipulated in virtual reality). Follow in order; every step maps to a lettered activity from the task sheet.

Scripts: `BoneSpring.cs`, `BoneMarker.cs`, `SpidosaurRig.cs`.

The physics is the week 7 lecture's cloth example applied to a skeleton instead of a grid: `Spring` -> `BoneSpring`, `Node` -> `BoneMarker`, `Cloth` -> `SpidosaurRig`.

---

## (a) Project setup - already done

This project is a copy of `VR Template`. Open `Assets/Scenes/BaseScene.unity`. It already has the XR Origin (XR Rig), XR Device Simulator, XR Interaction Manager, teleportation area and lighting. Unity **6000.0.79f1**, URP, OpenXR + XR Interaction Toolkit 3.0.11.

First open will take a few minutes while Unity rebuilds `Library/`.

## (b) Teleportation and grabbing - already done

The XR Rig prefab includes near-far interactors on both controllers, so anything with an `XR Grab Interactable` is grabbable at range. The ground plane already has a `Teleportation Area`. Nothing to add - the marker prefab built in step (d) picks this up for free.

---

## (c) Import the rigged model

`Assets/Models/spidosaur.blend` is already in the project (copied across from the Assessment 3 support materials).

> Unity imports `.blend` files by calling Blender in the background, so **Blender has to be installed** on the machine. If the model shows as a broken import, open the `.blend` in Blender and export it as FBX into `Assets/Models` instead.

1. Select `spidosaur.blend` in the Project window. In the Inspector:
   - **Model** tab -> **Scale Factor** `1`, **Import BlendShapes** off (not used here).
   - **Rig** tab -> **Animation Type**: `Generic`. Leave **Root node** as `None`.
     - Generic, not Humanoid. Humanoid retargets the skeleton onto Unity's own avatar and hides the real bones, and this task needs the actual imported bones.
   - **Apply**.
2. Drag `spidosaur` from the Project window into the Hierarchy. Rename it **Spidosaur**.
   - Transform Position `0, 0, 2`, Rotation `0, 0, 0`, Scale `1, 1, 1`.
   - Position it so it sits in front of where the rig spawns and is within arm's reach - the whole point is direct manipulation.
3. Expand it in the Hierarchy. There should be a mesh child with a **Skinned Mesh Renderer**, and an armature root (`SpidosaurBones`) with the bone hierarchy under it.
4. **Check the bones deform the mesh before writing any code.** Select a bone a few levels down a leg, move it in the Scene view with the move tool, and watch the skin stretch with it. If it does not move the mesh, the model imported without its skin weights and nothing after this will work.

> **Finding the bones:** `Assets/Editor/RigDumper.cs` (reused from my Assessment 3 work) writes the whole hierarchy to a text file. Select the armature root in the Hierarchy, then **Tools > Dump Rig Hierarchy**. It writes `RigDump.txt` next to the `Assets` folder and flags every junction and tip. This rig has **89 bones, 10 junctions and 23 tips** - the tips are all `_end` bones, which is what the rig builder has to filter out.

---

## (d) Marker template prefab

The grabbable handle that gets paired with a bone.

1. `GameObject > 3D Object > Sphere`, rename to **Marker Template**. Leave the scale at `1` - `SpidosaurRig` sets every marker to its **Marker Scale** field, so all 75 stay the same size and one field changes all of them.
2. **Material**: right-click in `Assets/Materials` -> `Create > Material`, name it `MarkerMaterial`. Base Map colour red. Drag it onto the sphere.
3. Add Component -> **XR Grab Interactable**. This auto-adds a Rigidbody.
   - Rigidbody -> tick **Is Kinematic**, untick **Use Gravity**.
   - XR Grab Interactable -> **Movement Type**: `Kinematic`.

   > **Throw On Detach** is switched off by `BoneMarker` in `Awake`, so you do not have to find it in the Inspector. XRI cannot throw a kinematic Rigidbody and logs a warning on every release if it is left on, and a thrown marker is not wanted here anyway - letting go should hand it straight back to the springs.

   > Kinematic on both, because `BoneMarker` writes `transform.position` itself. A non-kinematic Rigidbody would have the physics engine fighting the script for the same transform every frame - the "Is Kinematic in the XR space" slide: hands and anything driven from tracking data are moved by us, not simulated.

4. Add Component -> **BoneMarker** (the script).
   - Mass `1`
   - Damping `3`
5. Drag the object from the Hierarchy into `Assets/Prefabs` to make it a prefab, then **delete it from the scene**. `SpidosaurRig` creates them.

## (e) Spring template prefab

The visible spring between two markers.

1. `GameObject > Create Empty`, rename to **Spring Template**. Position `0, 0, 0`.
2. Add Component -> **Line Renderer**.
   - **Positions** -> Size `2` (leave both at zero, the script overwrites them every frame)
   - **Width** - leave it. `SpidosaurRig` overrides it with its **Spring Line Width** field. The Line Renderer default is `0.1`, which on a creature about a metre across draws each of the 130-odd springs as a wide ribbon and buries the model.
   - **Materials > Element 0** -> create `Assets/Materials/SpringMaterial`, any colour, and assign it. Without a material the line renders bright magenta.
   - **Use World Space** ticked.
3. Add Component -> **BoneSpring** (the script). Leave End 1, End 2 and Stiffness alone - `SpidosaurRig` fills them in.
4. Drag into `Assets/Prefabs`, delete from the scene.

## (d + e) Rig builder

One component covers both activities: it pairs every usable bone with a grabbable marker and drives the bone from it (d), and it creates the springs that pull each bone back to its rest length (e).

1. `GameObject > Create Empty`, rename to **Spidosaur Rig**. Position `0, 0, 0`, Scale `1, 1, 1`.

   > Leave it at the origin with scale 1. Markers are parented to it, and a scaled parent would scale every marker and every spring rest length with it.

2. Add Component -> **SpidosaurRig** (the script). Wire the Inspector:

   | Field | Value |
   |---|---|
   | Root Bone | the armature root (`SpidosaurBones`) from under **Spidosaur** in the Hierarchy |
   | Marker Template | `Marker Template` prefab |
   | Spring Template | `Spring Template` prefab |
   | Marker Scale | `0.03` |
   | Aim Bones At Children | ticked |
   | Stiffness | `30` |
   | Spring Line Width | `0.004` |
   | Show Springs | ticked while building, untick for the demo video |
   | Add Bracing Springs | **unticked** |
   | Bracing Stiffness | `10` |
   | Minimum Bone Length | `0.02` |

3. Press Play once and read the Console. It logs:

   ```
   Spidosaur rig built: N markers, M springs, K bones skipped as shorter than 0.02m.
   ```

   At `0.02` this rig reports **75 markers, 133 springs, 14 bones skipped** - 74 parent springs, plus 59 bracing springs if bracing is on.

   - **Around 14 skipped** is right. The rig has 23 `_end` tips, but only the genuinely short ones fall under the threshold; the rest are real bone lengths and keep their markers.
   - **Far more skipped** means the model imported small and real bones are being filtered out too. Drop **Minimum Bone Length** (try `0.005`) until only the short ones go.
   - **0 skipped** means the filter is too low and the zero-length bones are still getting springs. Those springs have a rest length of 0 and no direction to pull along, and their markers fly off - raise it.

---

## (f) Testing

Press Play, then use the XR Device Simulator:

- **Left Shift** = left controller, **Space** = right controller, **right-mouse drag** = look
- **WASD** move, **Q/E** up and down
- Aim a controller ray at a marker, **left-click and hold** to grab, drag it, release

What to check:

- Every bone that matters has a red marker sitting on it, joined to its neighbours by lines
- Dragging a marker drags that part of the model with it, and the skin stretches between it and the markers either side - not just the marker moving on its own
- Letting go springs it back to roughly where it started, overshooting slightly and settling
- Dragging a marker near the body drags the whole limb below it along
- The creature stays where it was placed - the base marker is pinned, so pulling on a leg cannot walk the model across the scene

Things worth trying. **Stiffness**, **Bracing Stiffness** and **Minimum Bone Length** are read once when the rig is built, and **Mass** and **Damping** come off the Marker Template prefab, so change these with the scene stopped and press Play again:

| Change | What happens | Why |
|---|---|---|
| Stiffness `30` -> `120` | snaps back hard, may buzz | large *k*, stiff spring |
| Stiffness `30` -> `5` | limp, sags out of shape | weak spring, barely restores rest length |
| Damping `3` -> `0` | never settles - keeps wobbling, and at higher stiffness blows up | the naive Euler version from the lecture, adding a little energy on every step |
| Damping `3` -> `10` | slides back with no bounce | over-damped |
| Aim Bones At Children off | limbs stop bending - a dragged marker slides its piece of skin sideways and stretches its neighbours instead | bones are only translated, never turned, so no joint ever hinges |
| Add Bracing Springs on | limbs stop folding flat sideways, but pulling one marker drags the joint above it instead of bending the joint you grabbed | a bracing spring reaches past a joint, so it couples two bones that are not neighbours |

> To watch one spring while the scene is running, select any `Spring (...)` object under **Spidosaur Rig** in the Hierarchy - its Stiffness is editable live, and the same for Damping on any `Marker (...)`.

> **If it runs slowly:** every spring draws its own Line Renderer, which is one draw call each - around 120 of them with bracing on. Untick **Add Bracing Springs**, or set the Spring Template's line width to 0, to cut that back. The simulation itself is 66 markers doing a handful of vector operations each and costs almost nothing.

---

## How the pieces talk to each other

```
SpidosaurRig.Start()
  └─ BuildRig()
       ├─ CreateMarker(root bone, anchored: true)
       └─ AddBone(bone, parent, grandparent)                  (depth-first, parent before child)
            ├─ distance(bone, parent.marker) < minimumBoneLength?  → skip, children inherit parent
            ├─ CreateMarker(bone)                             → grabbable handle at the bone
            ├─ CreateSpring(parent.marker, marker, stiffness) → holds the bone's length
            ├─ CreateSpring(grandparent.marker, marker, bracingStiffness)  → optional, resists folding
            └─ parent.childMarker ??= marker                  → the marker the parent bone will face,
                 parent.aimDirection = direction to it in the parent's own space, at bind time

SpidosaurRig.FixedUpdate()                                    (fixed timestep, not frame rate)
  └─ for each marker, parent first: BoneMarker.DoDynamics(Time.fixedDeltaTime)
        ├─ held or anchored?  → position = transform.position, velocity = 0, stop
        ├─ force = Σ spring.GetForce(this)                    F = F1 + F2 + Fn
        ├─ force += -damping * velocity
        ├─ a = force / mass                                   F = m * a
        ├─ velocity += a * dt                                 v_final = v_start + a·Δt
        └─ position += velocity * dt                          x_final = x_start + v·Δt

SpidosaurRig.LateUpdate()                                     (after the interactors have moved held markers)
  └─ for each bone, parent first, each finished before the next starts:
        ├─ bone.position = marker.position                    → the bone goes where its marker is
        ├─ facing  = bone.rotation * aimDirection             → where it points now
        ├─ wanted  = childMarker.position - marker.position   → where the next joint has ended up
        └─ bone.rotation = FromToRotation(facing, wanted) * bone.rotation
              └─ Skinned Mesh Renderer bends and stretches the skin to follow

BoneSpring.GetForce(end)
  ├─ d = (end1 - end2).normalized
  ├─ length = |end1 - end2|
  ├─ force = -stiffness * (length - restLength) * d           F = -k * x
  └─ end == end2 ? -force : force                             maths is written from end1's point of view
```

## Design notes

**Why the bone follows the marker, and not the other way round.** The task is direct manipulation - the hand has to be the thing that decides where a bone goes. Marker positions are the state the simulation owns; bones are written out from them once per frame in `LateUpdate` and never read back. That keeps one authority for each bone's position instead of two systems writing to the same transform.

**Positions, then facing.** Marker positions are the only thing the simulation solves - the springs and the Euler step never touch a rotation. Each bone is then placed on its marker and turned to keep pointing at the next marker down the chain, which is one `Quaternion.FromToRotation` per bone and no solver at all.

Translating the bones on their own is genuinely stretchy, but nothing ever hinges: dragging the marker on a knee slides that section of skin sideways and stretches what is either side of it, so the leg deforms everywhere except the joint being held. Adding the facing step puts the deformation where the hand is. The stretch is still there - the bone's length is whatever the springs currently allow, not its rest length - so the model still squashes and pulls out of shape, it just bends at its joints while it does it. `Aim Bones At Children` turns it off if the pure translation version is wanted for comparison.

At the ten junction bones the facing follows the first branch only. The other limbs off that junction are still pulled along by their own springs; they just do not get a say in which way the bone above them points.

**Why the short bones are dropped rather than fixed.** A spring's whole job is `length - restLength` along the direction between its ends. With a rest length of zero there is no meaningful direction, and `normalized` on a near-zero vector is unstable, so the marker gets thrown. The `_end` bones carry no skin weights either, so nothing is lost by leaving them parented to the bone above and carried along with it.

**Why the physics is in `FixedUpdate`.** Explicit Euler's error depends on the step size, so running it on the frame time would make the springs behave differently at 30fps and 120fps, and a single long frame could make the whole rig explode. `FixedUpdate` gives the same fixed `Δt` every step regardless of frame rate.

**Why not Unity's SpringJoint component.** The task sheet suggests a spring joint per bone. `SpringJoint` is a Rigidbody-to-Rigidbody constraint, so taking that route means giving all 66 bones a Rigidbody and a collider and handing the bone transforms over to the physics engine - which then fights the Skinned Mesh Renderer and the interactors for control of the same transforms, and needs every bone-to-bone collision disabled by hand. Writing the spring force out of the lecture instead keeps a single authority per bone, costs about forty lines, and makes the stiffness, damping and rest length readable and tunable rather than buried in a joint's solver settings.

**Known limitation.** The bracing springs stop a joint folding flat but do not stop the limb rotating around the axis through its two braced ends - a leg can still corkscrew. A proper fix would be an angular constraint at each joint, or solving rotations rather than positions, which is where the FABRIK work in my Assessment 3 goes.

---

## Troubleshooting

**Wide blue ribbons over the whole model.** The Line Renderer's default width is `0.1`, which is 10cm per spring across a creature about a metre wide. `SpidosaurRig` now sets the width itself from **Spring Line Width** (`0.004`), so the prefab's own setting is ignored - if it still looks heavy, drop that field, or untick **Show Springs** to hide them completely.

**The model deforms somewhere other than the marker being held.** Two causes, in order of likelihood:

1. **Add Bracing Springs** is on. A bracing spring runs from a bone straight to its grandparent, skipping the joint between them, so pulling a marker hauls on a bone two links up rather than bending the joint under your hand. Untick it.
2. **Aim Bones At Children** is off. Without it the bones are only translated, never turned, so no joint hinges at all - the skin just slides and stretches around the marker.

**Console: "Cannot throw a kinematic Rigidbody..."** Handled - `BoneMarker` switches **Throw On Detach** off in `Awake`. If it still appears, the Marker Template prefab is out of date; reimport the scripts and press Play again.

**Markers sit off the bones.** `SpidosaurRig` must be at position `0, 0, 0` with scale `1, 1, 1`. Markers are parented to it, and a moved or scaled parent offsets every one of them.

**A marker shoots off on the first frame.** A spring got a rest length of zero, so it has no direction to pull along. Raise **Minimum Bone Length** until the Console reports those bones as skipped.

---

## Attribution

- `spidosaur.blend` - unit support material for Assessment 3.
- `Assets/Editor/RigDumper.cs` - my own editor utility, written for Assessment 3 Stage 3 and reused here to find the bones.
- `BoneSpring`, `BoneMarker` and `SpidosaurRig` are built on the `Spring` / `Node` / `Cloth` example from the Week 7 lecture (slides 22-37). The spring force, the force summation and the Euler integration steps are the lecture's; the bone-hierarchy walk, the short-bone filter, the grab handling, the damping term and the bracing springs are mine.
