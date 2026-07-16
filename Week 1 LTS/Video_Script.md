# 2-Minute Video Script — Conveyor Belt, Item Spawner & VR Grab

**[0:00–0:12] Intro**
"Hey, this is my Week 1 solution for the VR conveyor belt scene. I've got three pieces of logic: `ItemSpawner`, `ConveyorBelt`, and VR grab support so items can be picked up with a headset controller. Let me walk through each."

**[0:12–0:35] ItemSpawner.cs**
"ItemSpawner has three public fields — the item prefab, how many to spawn, and the interval between spawns. On Start, it runs an async loop: instantiate the prefab, then await spawnInterval seconds using Unity's Awaitable API before spawning the next one. That's async/await instead of a coroutine, staggering spawns without blocking the main thread."

**[0:35–1:05] ConveyorBelt.cs**
"ConveyorBelt tracks every Rigidbody touching it in a list. OnCollisionEnter adds an item, OnCollisionExit removes it. In FixedUpdate, I calculate a belt velocity from the direction and speed fields, then overwrite each tracked Rigidbody's x and z velocity with that — but keep its existing y velocity, so gravity and bounce still behave naturally while it's pushed along."

**[1:05–1:40] VR Grab Support**
"To make items grabbable in VR, I added an `XRGrabInteractable` component with Velocity Tracking, so the item stays fully physics-driven even while held — it can still collide with things and gets thrown with real momentum. The tricky part: while held, the item's still touching the belt, so the belt would keep shoving it. My `ItemXRGrabHandler` script listens for the grab event and tells the belt to drop that item from its tracked list immediately, so physics doesn't fight the player's hand. Release it back on the belt, and `OnCollisionEnter` picks it up again automatically. This runs through OpenXR, so it works with any XR headset — including the Quest 3 controllers we've been using in class."

**[1:40–2:00] Wrap-up**
"So: the spawner drip-feeds items, the belt pushes them along, and grabbing one in VR cleanly hands control back to the player. I don't have a headset at home to demo live, but the code and prefab setup are all in place and ready to test. That's my solution — thanks for watching."

---
*Timing is approximate — read at a natural pace, ~130 wpm. Total ≈ 300 words / 2 minutes.*
