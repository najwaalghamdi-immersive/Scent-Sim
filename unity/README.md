# Unity integration

`OloramaUDPSender.cs` sends the same `OUT,...` UDP command as the rest of
this repo (`tools/protocol.js`), straight from a Unity scene — replacing
the sample from Olorama's own Unity integration guide, which had a bug that
would have silently broken every send (see below).

## Setup

Scents are data (`ScentMaker` assets), decoupled from where they're
triggered from (`ScentTrigger`, or your own code calling `Activate()`
directly) and from the network config (`OloramaUDPSender`, one per scene).

1. Copy `OloramaUDPSender.cs`, `ScentMaker.cs`, and `ScentTrigger.cs` into
   your Unity project's `Assets/Scripts/` (or wherever your scripts live).
2. Add `OloramaUDPSender` to exactly one GameObject in the scene (e.g. an
   empty "ScentController"), and set **Target IP** / **Target Port** in the
   Inspector. `ScentMaker` finds it automatically via
   `OloramaUDPSender.Instance` — no need to wire a reference per trigger.
3. Create a scent asset: **Assets → Create → Scriptable Objects →
   ScentMaker**. Name it after the scent (e.g. `Blood.asset`) and set its
   **Port** / **Intensity** / **Fan Ms** in the Inspector. Make one per
   scent you use — they're reusable across as many triggers as you want.
4. On any trigger volume (a GameObject with a Collider set to **Is
   Trigger**), add `ScentTrigger`, drag the scent asset from step 3 into
   its **Scent** field, and set the tag it should respond to.
5. Either the trigger volume or the object entering it (the player) needs a
   non-kinematic `Rigidbody`, or `OnTriggerEnter` never fires — a common
   silent failure that has nothing to do with the networking code.

Need one collision to fire a different scent than another? Just assign a
different `ScentMaker` asset to each `ScentTrigger` — no code changes.

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
