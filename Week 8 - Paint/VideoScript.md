# Week 8 Video Scripts

Part A goes over the gameplay footage, Part B over the code. Bracketed lines are notes, don't read them.

---

# PART A — Gameplay voiceover

Around a minute. Skip anything your footage doesn't show.

## Opening

Hi, I'm Daniel, and this is my Week 8 challenge, which is a multi-participant paint by numbers experience. The idea is that a few people are in the same virtual space together, each of them picks a colour, and whatever they paint turns up for everyone else as well. It's built in Unity 6 using the XR Interaction Toolkit, and for the networking I'm using Photon Fusion 2 in shared mode.

## Connection and avatars

So what's happening here is two clients joining the same session. One of them is a standalone build and the other is the Unity editor. When each one joins it spawns its own avatar and takes ownership of it, and after that the avatar just follows the headset around, which is why when I move my head in this window you can see the capsule move over in the other one.

## The picture

And this is the picture we're painting. It's three regions drawn as a set of concentric rings, and rather than importing it as an image I'm generating it in code when the object spawns. You can see in the console it's reporting how many colours it ended up with.

## Brush and palette

The brush is grabbable, and it snaps into a fixed pose in your hand so you're not holding it at whatever angle you happened to grab it at. To pick a colour you just dip it into one of the pots, and then touching the picture paints that region.

I did try this as buttons first, where you hover over a swatch and press select. It worked, but it felt clumsy, because you've already got something in that hand. Dipping a brush into a pot is just a more natural thing to do in VR, so I went with that instead.

## The two-client moment

Now this is the bit that really matters. I'm painting over here in this window.

*[Leave a gap and let it play.]*

And you can see it's changed in the other one too. So both players are genuinely looking at the same picture, rather than each of them having their own local copy that happens to look similar.

## Close

So that's the experience. Two players, a picture they share, a palette to pick from, and every change showing up for everybody. Let me take you through the code and how it actually works.

---

# PART B — Code walkthrough

Around two minutes. Scroll slowly while you talk.

## JoinPlayer

*[Open JoinPlayer.cs]*

This is the script that gets players into the world, and it's tiny, but there's a catch in it.

Fusion calls this PlayerJoined method on every machine whenever anybody joins, not just on the machine that joined. So if I spawned an avatar here without checking anything, every client would end up creating an avatar for every player, and you'd get players times players avatars instead of one each. That's why there's the check that the player who joined is actually the local player, so each machine only ever spawns its own.

The other half of it is this inputAuthority argument on the spawn call, which is basically telling Fusion that this avatar belongs to this machine. If you forget it the avatar still spawns and everything looks fine, but it has no owner, so nobody can actually drive it.

## MoveAvatar

*[Open MoveAvatar.cs]*

This one's on the avatar prefab, and it's where that authority actually does some work.

The thing to remember is that every client is running this same script on every avatar in the scene, including the ones standing in for other people. So the first thing it does is ask whether it has input authority over this particular object, and if it doesn't, it just stops there. Without that check, every client would be dragging all of the avatars around with its own headset.

Once it knows it's the owner, it copies the headset transform every frame, and the Network Transform component takes care of getting that out to everyone else. It also turns off its own renderers, because our avatar is sitting right on top of our head and otherwise we'd spend the whole time looking at the inside of it. That's only happening locally though, so everyone else still sees us fine.

There's one thing I want to be upfront about here. The movement script in the lecture drives the avatar with the thumbstick, so the avatar is the thing you're steering. The workshop task asks for the opposite, for the avatar to take its position from the XR Rig. Both of those write to the same transform, so you can only really have one, and I went with the task. The authority check is unchanged though, because the reason you need it is the same either way.

## PaintCanvas

*[Open PaintCanvas.cs. This is the one to slow down on.]*

This is the heart of it, and the way the picture is stored is the decision I'd most want to talk about.

The obvious way to do this would be to hold a texture and repaint pixels whenever somebody paints. I didn't do that. What I've got instead is two arrays. One of them holds a region number for every pixel, and that gets built once and then never changes. The other one holds whatever colour is currently in each region, and that's the only thing painting ever touches.

So painting a region comes down to changing one entry in a very small array. But the real reason I did it this way is the networking. When I need to tell everyone else what just happened, all I'm sending is a region number and a colour. An int and a Color, not an image. And because every client already has an identical region map, they all rebuild exactly the same texture from exactly the same data, so there's no way for two people's pictures to slowly drift apart.

I should be clear about where that idea came from. The texture code itself, the SetPixel loop and the Apply call, that's straight out of slides 50 and 51. But the slides don't cover storing a picture you can actually paint into, so I worked that part out through some back and forth with AI. What I asked for was the reasoning and the trade-offs rather than code, I gave it the constraint that I didn't want to be sending image data, and then I checked what came back against the unit material to make sure the pieces I was using were things we'd actually been taught.

*[Scroll to PaintAt and the RPC.]*

PaintAt is what the brush calls into. It takes the texture coordinate, works out which region that lands in, and if that region is already the colour you're painting, it just backs out, because there's no point sending a message that doesn't change anything.

And then this is the replication. I went with an RPC rather than a networked variable, and the reasoning is that a networked variable is really for state that's changing all the time, something like a position where you want it syncing every tick. A colour change isn't like that at all. It happens once, and then nothing happens again until somebody paints, so sending it as a one off event is much cheaper.

The last detail is that it targets all clients, and that includes me, the person who painted it. I could have repainted my own copy directly and only sent the change to everyone else, but then you've got two bits of code doing the same job, and two chances for them to end up disagreeing. This way every machine, mine included, repaints through the exact same function.

## PaintBrush and ColourSwatch

*[Open PaintBrush.cs]*

The brush is casting a short ray out of its tip and just asking what it hit. If it hit a swatch it takes that colour, and if it hit the picture it paints with whatever it's carrying. One mechanism covering both, which is why the palette doesn't need any interaction system of its own.

The reason it's a ray and not a collision is this line here. A raycast hit hands you the texture coordinate of the point you hit, for free, and that's exactly what the canvas needs to work out which region you touched. It's also why the picture has a mesh collider on it rather than a box collider, because a box collider won't give you texture coordinates at all.

*[Open ColourSwatch.cs]*

And the swatches are barely anything. Each one just holds a colour and tints itself to match when it starts, so you can read the palette without needing any labels on it.

## Close

So that's the whole thing. Fusion handles the session and the avatars, the picture is stored as region data so that painting costs almost nothing to send, and a single raycast off the brush tip covers both picking a colour up and putting it down.

If I were taking this further, the picture is honestly the easy part to change. The region map all comes out of one method, so swapping the rings for a proper imported image would be a change in one place, and none of the networking would have to move at all. Thanks for watching.

---

## Notes

- Clear the console before recording so only the lines you actually mention are showing.
- PaintCanvas is the section that earns the data structures criterion. Say the two arrays out loud even though they're on screen.
- If you fluff a line, keep rolling and say it again. Trimming is easier than re-recording.
