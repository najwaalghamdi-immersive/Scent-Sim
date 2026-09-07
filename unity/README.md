# Unity integration

`OloramaUDPSender.cs` sends the same `OUT,...` UDP command as the rest of
this repo (`tools/protocol.js`), straight from a Unity scene — replacing
the sample from Olorama's own Unity integration guide, which had a bug that
would have silently broken every send (see below).

## Setup

Scents are data (`ScentMaker` assets), decoupled from where they're
triggered from and from the network config (`OloramaUDPSender`, one per
scene). Three independent ways to fire a scent are provided — collision,
direct selection, and a gun — and all three end up calling the same
`ScentMaker.Activate()`, so add whichever ones your MR experience needs.

1. Copy `OloramaUDPSender.cs`, `ScentMaker.cs`, `ScentSelection.cs`,
   `ScentMenu.cs`, `ScentTrigger.cs`, and `ScentGun.cs` into your Unity
   project's `Assets/Scripts/` (or wherever your scripts live).
2. Add `OloramaUDPSender` to exactly one GameObject in the scene (e.g. an
   empty "ScentController"), and set **Target IP** / **Target Port** in the
   Inspector. Add `ScentSelection` to the same (or another) GameObject if
   you want a "currently chosen scent" shared across triggers/guns — see
   below.
3. Create a scent asset per scent: **Assets → Create → Scriptable Objects →
   ScentMaker**. Name it after the scent (e.g. `Blood.asset`) and set its
   **Port** / **Intensity** / **Fan Ms** in the Inspector. They're
   reusable across as many triggers/menus/guns as you want.

### Trigger 1 — collision

Add `ScentTrigger` to a trigger volume (a Collider with **Is Trigger**
checked), drag a scent asset into its **Scent** field, and set the tag it
should respond to (defaults to `"Player"`). Either the trigger volume or
the object entering it needs a non-kinematic `Rigidbody`, or
`OnTriggerEnter` never fires — a common silent failure that has nothing to
do with the networking code.

Leave **Scent** empty instead of assigning one, and the trigger fires
whatever the player currently has selected (see Trigger 2) rather than a
fixed scent.

### Trigger 2 — chosen from a menu

Add `ScentMenu` to your MR menu/UI GameObject and assign an array of scent
assets to **Scents**. Wire each menu option's UI Button `OnClick` (or an XR
interactable's Select event) to `ScentMenu.Choose(index)` — index 0 for the
first scent, 1 for the second, etc. Choosing an entry calls
`ScentSelection.Choose()`, which fires it immediately (toggle this off with
`ScentSelection.activateOnChoose` if you only want selection to *arm* a
scent, not fire it on the spot) and becomes the scent that any
empty-**Scent** `ScentTrigger` or `ScentGun` will use next.

### Trigger 3 — a gun

Add `ScentGun` to a gun-like GameObject. Either assign a **Fixed Scent**
(a gun permanently loaded with one cartridge) or leave it empty to fire
whatever's currently selected via `ScentSelection`. It raycasts forward
from its **Muzzle** (or its own transform) up to **Range** and activates
the scent when it hits something — optionally restrict hits to a
**Required Hit Tag** so shooting empty scenery doesn't burn a cartridge.
Call the public `Fire()` method from an XR controller's Activate/Select
event or an Input Action; `useLegacyFireButton` is on by default so it
also works with a plain mouse/keyboard click (`Fire1`) for quick testing
outside VR.

## The bug this fixes

The original sample built its message with **spaces after the commas**:

```csharp
$"OUT, {scentID:00}, {intensity:0000}, ..."   // OUT, 01, 0200, 1, 04000, 1000
```

Every test in this repo that actually got a real device to respond (the
Node CLI, PacketSender) used the format with **no spaces**:

```
OUT,01,0200,1,04000,1000
```

A simple embedded parser can silently ignore the malformed version, which
looks indistinguishable from a networking problem — worth ruling out before
chasing IP addresses if a device that responds to the CLI still won't
respond to Unity. `OloramaUDPSender.BuildMessage()` is a pure function
(no socket) specifically so this can be unit-tested and never regress.

It also stopped exposing the command's third field (the manual's own
"internal — do not modify" fan-fixed field) as a public `fanOn` bool.
Sending `0` there is untested and undocumented — it's now always `1`.

## Target IP

Defaults to `169.254.255.255` — the link-local broadcast address that
worked during this project's own hardware bring-up, for a unit that hadn't
picked up a normal DHCP lease. If your device later gets a real IP (check
via the Olorama app, or `arp -a` after it's on the network), point
`targetIP` at that address directly instead of the broadcast.

## Tests

`Tests/OloramaProtocolTests.cs` mirrors `test/protocol.test.js` — same
cases, so the Node and Unity implementations are checked against each
other, not just against themselves. It needs Unity's Test Framework
package (`com.unity.test-framework`, included by default in recent Unity
versions) and must live in an Editor-only assembly to run: either move it
under an `Editor/` folder, or add an `.asmdef` next to it with
`"includePlatforms": ["Editor"]`. Run it from **Window → General → Test
Runner → EditMode → Run All**.

These tests haven't been run inside an actual Unity Editor as part of this
change (no Unity install in this environment) — they're written to the
same cases already verified in `test/protocol.test.js`, but treat them as
unverified until you've run them once yourself.

## Pushing Unity project changes to this repo

Use a local git client (`git add`/`commit`/`push` from a checkout on your
machine), not GitHub's "Upload files" web UI. The web uploader ignores
`.gitignore`, so it will happily commit Unity's regenerated `Library/`,
`Logs/`, and `UserSettings/` folders — gigabytes of rebuildable cache that
bloats the repo for everyone who clones it. This already happened once and
had to be cleaned up with a history rewrite; the root `.gitignore` only
protects you if the tool you're using actually reads it.
