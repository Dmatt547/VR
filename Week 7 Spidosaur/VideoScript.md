# Week 7 - Demo Video Script

Target: 3-4 minutes. Screen directions in *italics*, spoken lines in plain text.

Around 740 spoken words, which lands near four minutes once you allow for the demo pauses. Read at a normal pace and let the on-screen actions breathe.

---

## 0:00 - 0:20 · What it is

*Game view, Play mode running, looking at the spidosaur with the markers and springs visible.*

> This is the Week 7 challenge, a squishy object you can pull out of shape in VR. Every red ball sits on one of the spidosaur's bones, and the lines between them are springs. Each one measured its own length when the scene loaded, and that's the length it wants back.

## 0:20 - 0:50 · Pulling it about

*Grab a marker halfway down a leg with the controller ray. Drag it well out, hold a second, release.*

> I'll grab a marker halfway down a leg and pull. The bone under my hand goes exactly where I put it, and the springs either side are stretched now, pulling on their neighbours, which pull on theirs. The limb comes with me, softly, a bit behind my hand.
>
> Let go and it springs back. It overshoots and settles, because that's a real spring with damping, not a lerp back to a saved pose.

## 0:50 - 2:15 · How it works

*Switch to the code editor with SpidosaurRig.cs open.*

> The first problem is that you can't grab a bone. It's a transform buried inside the model, nothing a controller can hit. So the whole thing is a swap. Put a grabbable object on top of every bone, and have the bone copy wherever that object ends up.
>
> This script does that on startup. It walks down the skeleton, drops a marker on each bone, and runs a spring between that marker and the one above it. Every spring measures itself right then and remembers it, and that's the only memory of the creature's shape there is. No saved pose, just a hundred and thirty numbers saying how far apart things ought to be.
>
> Bone length caught me out. Blender puts a tiny bone at the tip of each limb, and a few sit almost on top of their parent. Two points in the same place give a spring no direction to pull along, so the marker gets flung across the map. Anything under a minimum length doesn't get one.

*Open BoneSpring.cs.*

> The spring is barely anything. It knows its two ends and its rest length, and when either end asks what force it's under, it compares the current gap to that and pushes back in proportion.
>
> The task sheet suggests Unity's SpringJoint and I went against it, which is the biggest call I made. It connects two Rigidbodies, so I'd be putting a Rigidbody and collider on all 75 bones and handing them to the physics engine. But the skinned mesh and the XR interactors want those same transforms, so three systems end up fighting over one. Writing the force myself is forty lines, and one thing owns each bone.

*Open BoneMarker.cs.*

> The markers are what actually move. Each one adds up the forces from its springs and turns that into motion the usual way. Force over mass gives acceleration, acceleration changes velocity, velocity changes position.
>
> The bit worth pointing out is what it does while I'm holding it. It stops simulating and just reads where my hand is. Pushing it there with a force would be more honest physically, but it would trail and rubber band, and in VR that reads as broken rather than soft.

*Back in SpidosaurRig, scroll to FixedUpdate and LateUpdate.*

> Then there's the order, which matters more than it looks. The springs run on the fixed physics clock, not per frame, because the time step is baked into that maths. And the bones move dead last, after XR has finished dragging whatever I'm holding, or they'd sit a frame behind my hand.

## 2:15 - 2:55 · Two things I can show

*Stop Play. Untick Aim Bones At Children. Play, drag the same leg marker.*

> Worth showing because I got it wrong first time. My first version only moved the bones, never rotated them. It stretches, but nothing hinges. Watch the leg, the skin slides sideways and the joint I'm holding never bends. Turning each bone to face the next marker down the chain puts the deformation where my hand is, and it's still stretchy, because the bone's length is whatever the springs allow.

*Stop Play. Set Damping to 0 on the Marker Template. Play, pull a leg and release.*

> And this is why there's damping. The lecture version has no drag term, and the integration quietly adds energy every step, so at zero it never settles. It wobbles harder and harder until it comes apart.

## 2:55 - 3:20 · Honest limitation

*Back to the Game view, everything back to normal.*

> One thing it doesn't solve. The springs only hold distances, so a limb can still corkscrew around its own axis, and where the skeleton branches only the first limb steers the bone above it. Fixing that needs angular constraints, which is where my Assessment 3 work goes.
>
> But for something you grab and pull about, distance springs and a facing pass get you most of the way for very little code.
