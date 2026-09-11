# Home Energy Debugger — Project Memory / Handoff

SIT283 Assessment 2 (VR Development Challenge), Deakin University. Daniel Mattioli.
Due end of Week 10. This file exists because the assistant's Linux sandbox (used for
editing the Word report's internals and rendering previews) became wedged mid-session
and stopped responding after 5 retry attempts. This is a full handoff so a fresh
session can resume immediately without re-deriving context.

## Where things physically live
- Unity project: `C:\Code\VR\Home Energy Debugger`
- Report + screenshots: `C:\Users\danie\OneDrive\Documents\UNI\YEAR 4\SIT283 - Development for Virtual and Augmented Reality\Assessment 2 - Home Energy Debugger`
  - The ONLY document that should exist in that folder is `SIT283_Assessment2_Report.docx` (single master report, per Daniel's explicit instruction — do not create additional report files).
  - `Screenshots` subfolder holds all evidence screenshots.
- Weekly workshop reference code (read-only, for pattern/citation matching): `C:\Code\VR\Week 1 LTS` through `Week 7 Spidosaur`.
- Weekly lecture slides (read-only, for citation-accuracy checking): `C:\Users\danie\OneDrive\Documents\UNI\YEAR 4\SIT283 - Development for Virtual and Augmented Reality\Weekly Slides` (PDFs, Weeks 2–8).

## Report style rules (must keep following)
- No em dashes. Flowy, plain wording.
- Harvard-style citations. Only cite something as external if it is genuinely NOT
  taught in Deakin's own weekly workshops or lecture slides — internal course
  content gets cited as Deakin University lecture/workshop material, not left
  uncited and not cited as if it were external research. This has already been
  corrected once (see References list below) after Daniel specifically asked for
  the Weekly Slides to be checked before citing anything as external.
- Section 4 (Use of Generative AI) is STILL a placeholder — not yet filled in.
  Daniel's real Assessment 3 report pastes full unedited prompt/response
  transcripts into this section; the same style should be used here eventually.

## Rubric criteria status (7 required features)

1. **Movement** — done earlier (XR Origin, continuous move/turn, grab). Not
   separately documented in this handoff since it predates the recent work.
2. **Sequence of States** — DONE. Report section 2.2 written, Figure 2 embedded.
3. **Tool** — DONE. Report section 2.3 written, Figure 3 embedded.
4. **Media** — DONE. Report section 2.4 written, Figures 4 and 5 embedded
   (Figure 4 = scanning/yellow pulse, Figure 5 = Leak Identified/red).
5. **Configuration Interface** — DONE (code + report). Report section 2.5
   written, Figures 6 and 7 embedded (Figure 6 = lever retracted/red fridge,
   Figure 7 = lever pulled/green fridge).
6. **Autonomous Simulation** — DONE. Report section 2.6 written, Figures 8 and 9
   embedded (Figure 8 = slow/before, Figure 9 = fast/after).
7. **Start Screen** — CODE AND SCENE DONE, REPORT SECTION 2.7 NOT YET WRITTEN.
   Screenshot for Figure 10 not yet taken.

## Immediate next task
Take the Start Screen screenshot (Figure 10) and write report section **2.7 Start
Screen**. Also fix section **2.5**, whose code excerpt is now out of date (see
Known corrections below).

## Report structure reference (current figure numbering as of last successful edit)
- Figure 2 = Sequence of States (state machine Console output)
- Figure 3 = Tool (scanner grabbed, reading logged)
- Figure 4 = Media, scanning state (yellow pulse)
- Figure 5 = Media, Leak Identified state (red)
- Figure 6 = Configuration Interface, lever retracted (red)
- Figure 7 = Configuration Interface, lever pulled (green)
- Figure 8 = Autonomous Simulation, slow/before (INSERTED)
- Figure 9 = Autonomous Simulation, fast/after (INSERTED)
- Figure 10 = Start Screen (placeholder, bump already applied)

## References list (current state, Harvard style, letters a–g used so far)
All under "Deakin University (2026x)" except one Unity Technologies citation.
Order in the document (not alphabetical — inserted in the order each feature
needed a new source):
- (2026a) Week 5 lecture slides, Frame-Rate Independent Movement
- (2026b) Week 5 workshop materials, ScalpelTool.cs and CuttableObject.cs
- Unity Technologies (2024) XR Interaction Toolkit 3.0 manual (external — genuinely
  not covered in weekly slides, verified by search)
- (2026c) Week 6 lecture slides, State Machines
- (2026d) Week 6 workshop materials, Fish.cs
- (2026e) Week 4 lecture slides, Periodic Functions
- (2026f) Week 4 workshop materials, ElementPulse.cs and ElementSphere.cs
- (2026g) Week 3 lecture slides, Transforms

Autonomous Simulation's research paragraph added no new citation letter, as
planned — it reuses (2026e)/(2026f) (Week 4 Periodic Functions / ElementPulse.cs),
since `PowerFlowSimulator.cs` is the same sine-wave technique applied to position
instead of emission colour. Next new source for the Start Screen would be (2026h).

## Start Screen implementation (built this session)
Diegetic wall-mounted "Household Energy Inspection Terminal" world-space canvas
called `StartScreenPanel`, sitting flat on the kitchen wall the player spawns
facing. Dark slate panel (#141A21) with cyan accent (#3DBBD9), thin Image-based
divider lines, TextMeshPro throughout.

Two states, switched by `SessionManager`:
- `BriefingRoot` — background Image (must be the FIRST child so it draws behind
  the text), title, subtitle, YOUR JOB column, CONTROLS column, author footer
  (Daniel Mattioli, S223377562, SIT283, Deakin, T2 2026), and a START INSPECTION
  button.
- `ActiveRoot` — a single wide RETURN TO BRIEFING button shown during the session.

Gating approach: interactors stay enabled so the ray can press the buttons.
What is gated instead is the energy scanner tool (in `sessionObjects`) and the
`Locomotion` object under XR Origin. No scanner means nothing can be scanned and
the lever can do nothing, which makes it a sufficient single gate.

`SessionManager.cs` (Assets/Scripts/UI/) fields as wired: Briefing Root,
Active Bar Root, Session Objects [Scanner], Locomotion Root [Locomotion],
Appliances [Fridge, Stove, Toaster, Kettle], Levers [InsulationLever],
Pose Reset Objects [Scanner transform]. Buttons hooked via OnClick to
`StartSession()` and `ReturnToBriefing()`.

## Solved bug: XR ray retracting, UI unclickable (IMPORTANT)
Symptom: interactor lines randomly short or long, near-grab worked but the ray
would not interact with the world-space UI at all.

ROOT CAUSE: the `EventSystem` had TWO input modules on it at once, both enabled —
`XR UI Input Module` (the correct one) and `Input System UI Input Module`. An
EventSystem only uses one; having both meant the XR ray never registered against
the canvas, so the interactor found no target and the visual retracted.

FIX: removed `Input System UI Input Module`, kept `XR UI Input Module`. Also set
the world-space Canvas's `Event Camera` to `Main Camera`, which cleared Unity's
"World Space Canvas with no specified Event Camera" warning.

Note for the report: this is a genuinely good "Evidence it works / testing"
anecdote for section 2.7 if a debugging narrative is wanted.

## Known corrections still outstanding in the report
- Section 2.5 (Configuration Interface) describes `ConfigLever` sliding along
  local **Z** and shows a single-target code excerpt. The script now slides along
  local **X** and drives FOUR appliances (targetAppliance through
  targetAppliance4). The excerpt and surrounding prose both need updating.
- `ConfigLever` has since gained null guards (`ApplyUpgradeTo`) and a
  `ResetLever()` method; `ScannableAppliance` has gained an `Idle` case in
  `HandleStateChangedEvent` and a `ResetAppliance()` method. Both were added for
  the Start Screen reset.

## Outstanding scene concern (not currently blocking)
`XR Origin (XR Rig)` is scaled 2,2,2. This scales every interactor distance,
near-cast region, grab offset and locomotion speed under the rig. It was
suspected in the ray bug but was not the cause. Left as is because it works, but
worth remembering if further interaction oddities appear.

## Current C# script states (all confirmed working as of last test)

### `Assets/Scripts/Appliances/ScannableAppliance.cs`
State machine: Idle -> Scanning -> LeakIdentified -> UpgradeApplied -> Result.
Has status light (emission colour via Renderer.material, pulsing sine wave while
Scanning) and audio (single shared AudioClip "Button Pop.wav" from XRI Starter
Assets, pitch-shifted per state via AudioSource.PlayOneShot). Has a public
`State` getter (`public ApplianceState State => state;`) added specifically so
`PowerFlowSimulator` can read it without being able to change it. `ApplyUpgrade()`
is called by `ConfigLever`, guarded to only work if state == LeakIdentified.

### `Assets/Scripts/Interaction/EnergyScannerTool.cs`
Raycast-and-delegate tool, mirrors Week 5 Scalpel pattern. Unchanged for a while,
confirmed working.

### `Assets/Scripts/Interaction/ConfigLever.cs`
NOT a dial — this replaced an earlier twisting-dial approach (`ConfigDial.cs`,
now deleted from the project) after `RotationAxisLockGrabTransformer` proved
unreliable (it locks rotation in world-space Euler angles, which broke once the
dial's resting rotation wasn't axis-aligned, and separately Unity discarded a
Play-Mode-only Inspector edit that was never saved in Edit mode — two compounding
issues). The lever instead just slides along its own local Z axis: XR Grab
Interactable has Track Position on, Track Rotation OFF, no grab transformer
components at all. Every frame, `Update()` clamps `transform.localPosition` back
onto a straight line (X and Y locked to their starting values, only Z varies
between 0 and `maxSlideDistance`), calling `targetAppliance.ApplyUpgrade()` once
travelled crosses `upgradeThreshold`. Current working values: **maxSlideDistance
= 0.15**, **upgradeThreshold = 0.7**. Guard flag `upgradeSent` stops repeat calls.

### `Assets/Scripts/Environment/PowerFlowSimulator.cs`
New for Autonomous Simulation. Sits on `PowerFlowIndicator`. Every frame, moves
the object's local Z position via `Mathf.Sin(Time.time * speed) * amplitude` —
same wave formula as `ElementPulse.cs` (Week 4 Elements), just driving position
instead of emission colour. Reads `targetAppliance.State`; if it equals
`UpgradeApplied`, uses `upgradedSpeed` instead of `normalSpeed`. Fields:
`targetAppliance` (set to Fridge), `amplitude`, `normalSpeed`, `upgradedSpeed`.
Confirmed working — screenshots taken show clearly different trail lengths
between slow (before upgrade) and fast (after upgrade).

## Scene object notes
Daniel reorganised part of the scene into a Hierarchy group called
"Inspection panel" containing `PowerFlowIndicator`, `StatusLight`, and
`PowerFlowTube` (the see-through pipe, a Cube or Cylinder with a transparent
URP/Lit material, Surface Type Transparent, Render Face Both, so the sphere is
visible sliding inside it). `InsulationLever` sits outside that group at the
top level of the Hierarchy. This is just a Hierarchy reorganisation for
inspection convenience, not a functional change — scripts and references are
unaffected.

`PowerFlowIndicator` has a Trail Renderer attached (added for visual polish,
not functionally required by the rubric): material `M_PowerFlowTrail`
(URP/Unlit, light blue, transparent surface type), width curve tapering from a
small peak (~0.02–0.03) down to 0, Time ~0.5s, alpha gradient fading to 0 at
the tail end.

## Outstanding known issue class (for awareness, not currently blocking)
Any Inspector edits made WHILE in Play Mode are discarded the moment Play Mode
stops — this bit us once already with the Rotation Axis Lock Grab Transformer
setup on the old dial. Any future component wiring must be done in Edit mode
and saved (Ctrl+S) before testing in Play mode, not the other way around.

## Task list state (as tracked in the assistant's TaskList tool)
1. [completed] Build grabbable Tool (energy scanner)
2. [completed] Build state machine on one appliance (fridge)
3. [completed] Add Media feedback (visual + audio)
4. [completed] Build Configuration Interface (dial — ended up as a lever)
5. [completed] Build Autonomous Simulation — code, screenshots and report section 2.6 all done
6. [completed] Build Start Screen — scene and scripts done, report section 2.7 pending
7. [pending] Capture evidence screenshots per feature
8. [pending] Fill in report sections as features complete
9. [pending] Package project zip and final submission (Assets/Packages/ProjectSettings only, no cache folders)

## What to do when resuming in a new session
1. Take the Start Screen screenshot from the Game view and embed it as Figure 10.
2. Write report section 2.7 Start Screen, matching sections 2.2-2.6 exactly
   (bold label runs, italic body, Consolas 9pt paragraphs shaded F2F2F2 for code,
   italic 9pt captions, images at 6 inches wide).
3. Fix section 2.5's out-of-date lever description and code excerpt.
4. Fill in Section 3 (Operational Instructions), 3.1 install/setup and 3.2 how to
   test each of the seven features.
5. Fill in Section 4 (Use of Generative AI) with real transcript excerpts.
6. Package the submission zip (Assets, Packages, ProjectSettings only) and do a
   final pass over the Rubric self-check table.

## Session log
- 2026-09-10: Wrote section 2.6 Autonomous Simulation (all four labelled
  paragraphs, a shaded Consolas code excerpt from `PowerFlowSimulator.cs` plus a
  discussion paragraph), embedded Figures 8 and 9 from the two PowerFlowIndicator
  screenshots, and bumped the Start Screen placeholder from Figure 9 to Figure 10.
  Report saved back over `SIT283_Assessment2_Report.docx` in place.
- 2026-09-10 (later): Built the Start Screen feature end to end — SessionManager.cs,
  the wall terminal canvas with both panel states, reset methods on
  ScannableAppliance and ConfigLever, and null guards on the lever's four targets.
  Diagnosed and fixed the duplicate EventSystem input module that was breaking all
  ray-based UI interaction. Full loop confirmed working.
