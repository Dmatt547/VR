# Week 3 — Demo Video Script

Target: 2–3 minutes. Screen directions in *italics*, spoken lines in plain text.

Record the Game view for the first half, then switch to the code editor with all four scripts open in tabs. Walk them in the order things actually happen at Play: spawner → manager → marker → aircraft.

---

## 0:00 – 0:20 · What the challenge is

*Game view, Play mode running, looking across the terrain at the aircraft.*

> This is the Week 3 challenge — an air traffic control system for drones. The user sketches each aircraft's flight path by moving waypoints around in VR, and collisions between aircraft get highlighted.
>
> Four routes are running here. Each has its own colour, its own set of waypoints, and one aircraft flying them in order.

## 0:20 – 0:55 · XR interaction

*Aim a controller ray at a marker. Grab it, drag it well off the line, release. Watch the aircraft change course.*

> Every marker is an XR Grab Interactable, so I can pick one up with either controller. It turns yellow while I'm holding it.
>
> Watch the aircraft — as soon as I drop this marker somewhere new, it flies to the new position. The route isn't baked in at startup; the aircraft asks its manager where the marker is on every frame, so the path is live.
>
> One deliberate detail — while I'm actually holding a marker, the aircraft heading for it stops and waits, rather than chasing my hand around.

## 0:55 – 1:15 · Collisions

*Drag two routes so they cross. Wait for the aircraft to meet. Then drag one route low into a hill.*

> Now I'll pull two routes across each other so the aircraft meet in the middle.
>
> There — both flash red for a second, then return to their route colour. Same response if I fly one into the terrain.

## 1:15 – 2:25 · The code

*Switch to the code editor.*

> Four scripts, each owning one job. I'll go in the order things happen when you press Play.

**AircraftSpawner.cs**

> The spawner sits at the top. Its `Start` loops four times, instantiating an aircraft manager prefab each pass. `GetRandomPosition` scatters them inside a box around the spawner at a random height, and `GetRandomDirection` rotates `Vector3.forward` around the Y axis to give each route a random heading — I did it as a rotation rather than random X and Z so the vector stays flat and the routes stay level.
>
> Then it pushes the heading and a colour into the manager. The spawner knows nothing about markers or aircraft — adding a fifth route is just a number in the Inspector.

**AircraftManager.cs**

> Each manager owns one route. `BuildRoute` works out the total line length from the spacing, steps back half of it so the line is centred on the manager rather than trailing off one side, then instantiates the markers along it and keeps the `FlightMarker` components in a list.
>
> `SpawnAircraft` then creates the aircraft at the first marker and calls `SetRoute`, handing it a reference back.
>
> These two accessors are the important part. The aircraft asks for positions *by index* instead of holding references to markers. That's what keeps the manager in control of its own route, and it's why the markers can move at runtime without anything breaking.

**FlightMarker.cs**

> The marker is deliberately thin. It subscribes to the grab interactable's `selectEntered` and `selectExited` events rather than polling the controller — same pattern I used for the bead gun in Week 2 — and unsubscribes in `OnDisable` so it doesn't leave listeners behind.
>
> All it really exposes is `IsHeld`, with a private setter so nothing else can lie about whether the user has hold of it. It also reads its idle colour off the material in `Awake`, so the material controls the look and the script only overrides it while held.

**Aircraft.cs**

> This is the movement. `MoveTowards` steps toward the target marker at speed times delta time, so it flies at the same rate regardless of frame rate. For the bonus challenge, `LookRotation` builds the rotation pointing down the travel vector and `RotateTowards` eases into it, so it turns rather than snapping. When it gets within the arrive distance, the modulo wraps the index back to the first marker.
>
> The collision response is here. The aircraft has a kinematic Rigidbody with Is Trigger ticked — that's the task sheet's "switch off the collision response but still trigger on it". I want an air traffic system to *report* a conflict, not simulate a crash; if physics knocked the aircraft off course, the route data would stop meaning anything. The flash is async — set red, wait a second, set it back — with a flag so overlapping hits don't cut it short.

## 2:25 – 2:45 · Honest limitation

*Back to the Game view.*

> One thing I'd change with more time: this detects collisions between *aircraft*, not between *paths*. Two routes can cross without ever registering a conflict if the aircraft don't reach the intersection at the same moment. A proper system would compare the path segments up front and flag the crossing before anything flew through it.

---

## Checklist before recording

- [ ] Four routes visibly different colours
- [ ] At least one clean grab-and-reroute
- [ ] At least one aircraft-to-aircraft flash captured
- [ ] At least one terrain flash captured
- [ ] Console clear of errors (the haptics warning is fine)
- [ ] Editor font large enough to read at video resolution
