# Week 2 Submission Video Script

Target length: 2-3 minutes.

## [0:00-0:20] Intro + quick demo

*(Game view, gun already in scene)*

"Hey, this is my Week 2 solution — the bead sculpting tool. The idea is you've got a bead gun, you grab it, fire coloured beads, and they build up into a sculpture when they hit a moving blocker plane."

*(Grab the gun, fire a couple of beads at the blocker)*

"So here I'm grabbing the gun, pulling the trigger to fire beads, and you can see they stick to the blocker instead of bouncing off — that's what lets you actually sculpt with them."

## [0:20-0:35] Colour demo

*(Touch gun to a colour selector, fire again)*

"If I touch the gun to one of these colour selector objects, the gun changes colour, and any beads I fire after that come out in the new colour too."

## [0:35-1:10] BeadGun.cs — grabbing and firing logic

*(Switch to code editor, BeadGun.cs)*

"So in the code, the gun has an XR Grab Interactable component, and instead of checking for input every frame, I'm subscribing to its events in OnEnable — selectEntered and selectExited just track whether the gun is currently held, and activated fires when the trigger's pulled.

In OnActivated, I check isHeld first, so it can't fire unless it's actually been picked up — that was one of the bonus challenges. Then I check canFire, which is a cooldown flag — I set it false, fire the bead, then await a short delay before setting it back to true, so beads don't spawn on top of each other.

FireBead just instantiates the bead prefab at the fire point, passes it the gun's current colour, and adds a force to its Rigidbody so it flies out of the barrel."

## [1:10-1:35] Bead.cs — freezing on the blocker

*(Switch to Bead.cs)*

"The bead script is pretty simple — when it collides with something tagged Blocker, it zeroes out its velocity and sets isKinematic to true, which basically tells physics to stop moving it. That's what makes it stick exactly where it lands instead of sliding off."

## [1:35-2:00] ColorSelector.cs — touch to change colour

*(Switch to ColorSelector.cs)*

"The colour selector uses OnTriggerEnter instead, since I want the gun to pass through it rather than collide with it. It checks the tag, then uses GetComponentInParent to find the BeadGun script — because the bit that actually touches it is a child collider on the gun, not the root object. Once it's got that reference, it just calls SetColour on the gun."

## [2:00-2:20] Bonus + wrap-up

*(Back to game view, quick blocker-grab demo)*

"I've also got the blocker set to grab and move around, and its Rigidbody is kinematic so it doesn't get knocked around by beads hitting it — it only moves when I actually grab it myself.

That's the whole solution — grab, fire, freeze on contact, and touch-based colour changing, all built around Unity's event functions, colliders, and Rigidbody physics from the last couple of weeks."
