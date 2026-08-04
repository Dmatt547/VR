# Week 3 — Air Traffic Control for Drones

Build guide for the Week 3 workshop challenge (Autonomous Simulation). Follow in order; every step maps to a lettered activity from the task sheet.

Scripts: `FlightMarker.cs`, `AircraftManager.cs`, `Aircraft.cs`, `AircraftSpawner.cs`.

---

## (a) Project setup — already done

This project is a copy of `VR Template`. Open `Assets/Scenes/BaseScene.unity`. It already has the XR Origin (XR Rig), XR Device Simulator, XR Interaction Manager, teleportation area and lighting.

## (b) Teleportation and grabbing — already done

The XR Rig prefab includes near-far interactors on both controllers, so anything with an `XR Grab Interactable` is grabbable at range. The ground plane already has a `Teleportation Area`.

---

## (c) Terrain

1. `GameObject > 3D Object > Terrain`.
2. Terrain component → gear icon (**Terrain Settings**) → **Mesh Resolution**: Terrain Width `3`, Terrain Length `3`, Terrain Height `1`.
3. Centre it on the origin: Transform Position `-1.5, 0, -1.5`. (Terrain's pivot is a corner, not the middle, so half of 3 in X and Z puts the centre at 0.)
4. **Paint Terrain > Raise or Lower Terrain** — pick a small brush, low opacity, and paint a few hills. Keep them well under the flight height or every aircraft will clip the ground constantly.
5. **Paint Terrain > Paint Texture > Edit Terrain Layers > Create Layer** — pick any texture for a base layer, then add a second layer (rock/grass) and paint it on the hills.
6. Set the terrain's **Tag** to `Terrain`. It isn't a built-in tag, so: Inspector → Tag dropdown → **Add Tag…** → `+` → type `Terrain` → then reselect the terrain and assign it.

> `Aircraft.cs` compares against this tag in `OnTriggerEnter`, so the name must match exactly.

## (d) Flight marker prefab

1. `GameObject > 3D Object > Sphere`, rename to **Flight Marker**. Scale `0.08, 0.08, 0.08`.
2. **Transparent material**: right-click in `Assets/Materials` → `Create > Material`, name it `MarkerMaterial`.
   - Surface Type → **Transparent**
   - Base Map colour → white, alpha around `90`/255
   - Drag onto the sphere.
3. Add Component → **XR Grab Interactable**. This auto-adds a Rigidbody.
   - Rigidbody → tick **Is Kinematic**, untick **Use Gravity**. The markers should stay exactly where you place them rather than falling to the ground.
   - XR Grab Interactable → **Movement Type**: `Kinematic`.
4. Add Component → **FlightMarker** (the script). Leave `Marker Renderer` empty — `Awake` grabs the sphere's own renderer.
5. Drag the object from the Hierarchy into `Assets/Prefabs` to make it a prefab, then delete it from the scene.

## (e) Aircraft prefab

Build this before the manager, since the manager needs to reference it.

1. `GameObject > 3D Object > Cube`, rename to **Aircraft**. Scale `0.05, 0.02, 0.12` — long in Z so you can see which way it's pointing (the bonus "faces direction of travel" behaviour rotates it around +Z).
2. Add Component → **Rigidbody** → tick **Is Kinematic**, untick **Use Gravity**.
3. On the Box Collider → tick **Is Trigger**.
4. Add Component → **Aircraft** (the script).
   - Speed `0.5`, Turn Speed `180`, Arrive Distance `0.05`, Loop Route ticked
   - Flash Duration `1`, Collision Colour red, Terrain Tag `Terrain`
5. Drag into `Assets/Prefabs`, delete from the scene.

> **Why kinematic + Is Trigger:** the task sheet's "switch off the collision response but still trigger on a collision". A trigger collider means two aircraft fly straight through each other instead of physically bouncing, but `OnTriggerEnter` still fires so you can report the conflict. Trigger events need at least one Rigidbody in the pair, which is why the aircraft has a kinematic one.

## (e cont.) Aircraft manager prefab

1. `GameObject > Create Empty`, rename to **Aircraft Manager**.
2. Add Component → **Line Renderer** (this draws the sketched path).
   - Width `0.005` (both ends)
   - Materials → Element 0 → assign a simple **Unlit** material, or create one: `Create > Material`, Shader → `Universal Render Pipeline/Unlit`. Without a material the line renders bright magenta.
   - Untick **Use World Space**? — **no, leave it ticked** (the default). The script feeds it world positions.
3. Add Component → **AircraftManager** (the script). Wire the Inspector:

   | Field | Value |
   |---|---|
   | Marker Prefab | `Flight Marker` prefab |
   | Number Of Markers | `5` |
   | Marker Spacing | `0.6` |
   | Route Direction | `0, 0, 1` |
   | Aircraft Prefab | `Aircraft` prefab |
   | Route Colour | anything (the spawner overrides it) |
   | Path Line | drag this object's own Line Renderer in |

4. Drag into `Assets/Prefabs`, then **delete it from the scene** — the spawner creates them.

## (g) Aircraft spawner

1. `GameObject > Create Empty`, rename to **Aircraft Spawner**. Position `0, 0, 0`.
2. Add Component → **AircraftSpawner**.
   - Aircraft Manager Prefab → the `Aircraft Manager` prefab
   - Number Of Routes `4`
   - Area Size `2`, Min Height `0.3`, Max Height `0.9`
   - Route Colours → leave the four defaults

That's it — press Play and you get four routes, each with its own colour, heading and aircraft.

---

## (i) Testing

Press Play, then use the XR Device Simulator:

- **Left Shift** = left controller, **Space** = right controller, **right-mouse drag** = look
- **WASD** move, **Q/E** up and down
- Aim a controller ray at a marker, **left-click and hold** to grab, drag it somewhere else, release

What to check:

- Aircraft follow their markers in order and loop back to the start
- Each aircraft noses into the direction it's travelling
- Dragging a marker reroutes that aircraft — it pauses while you hold the marker, then flies to the new position
- Drag two routes so they cross; when the aircraft meet, both flash red for a second then go back to their route colour
- Fly a route into a hill — the aircraft flashes on the terrain too

---

## How the pieces talk to each other

```
AircraftSpawner
  └─ instantiates N × AircraftManager (sets each one's direction + colour)
        ├─ instantiates M × Flight Marker  (grabbable waypoints, straight line)
        ├─ instantiates 1 × Aircraft       (calls SetRoute(this))
        └─ Update() → redraws the LineRenderer through the markers

Aircraft.Update()
  ├─ asks the route: IsMarkerHeld(i)?  → pause if the user is dragging it
  ├─ asks the route: GetMarkerPosition(i)
  ├─ Vector3.MoveTowards  (speed * Time.deltaTime)
  ├─ Quaternion.LookRotation + RotateTowards  (bonus: face direction of travel)
  └─ within arriveDistance → next marker (wraps to 0 if looping)

Aircraft.OnTriggerEnter(other)
  ├─ other is an Aircraft → FlashCollision()
  └─ other is tagged Terrain → FlashCollision()
        └─ async: set red → await Awaitable.WaitForSecondsAsync → set back
```

---

## Talking points for the walkthrough video

- **Why triggers, not collisions.** An air traffic display should report a conflict, not simulate a crash. Physics response would knock both aircraft off their assigned routes and the route data would no longer mean anything. Kinematic Rigidbody + `Is Trigger` gives detection without response.
- **Why the manager owns the markers and the aircraft.** Making the manager a prefab means one route is one self-contained unit, so the spawner generates N independent routes without any of them needing to know about each other. Adding a fifth aircraft is a number in the Inspector, not more code.
- **Why the aircraft asks the route for positions each frame instead of caching them.** The markers are grabbable, so their positions change at runtime. `GetMarkerPosition(index)` reads the live transform, which is what makes "sketch the path in VR and watch the aircraft reroute" work.
- **Why `Time.deltaTime * speed`.** Speed is metres per second, so flight timing is identical on a 72 Hz headset and a 144 Hz desktop preview. Same pattern as the Week 2 key rotation.
- **Why `Quaternion.RotateTowards` rather than assigning the rotation directly.** Snapping to the new heading at each waypoint looks wrong; capping the turn rate at `turnSpeed` degrees per second makes the aircraft bank around the corner. Also avoids gimbal-lock issues from driving Euler angles by hand.

## Known limitations worth mentioning

- Route collisions are detected between *aircraft*, not between *paths*. Two routes can cross without a conflict ever registering if the aircraft never arrive at the intersection at the same time. A path-level check (comparing line segments up front) would flag the crossing before anything flies.
- The aircraft pauses while a marker is held. That's a deliberate choice to keep it from chasing a moving target, but a real system would recompute the route continuously instead.
