# Unity integration

`OloramaUDPSender.cs` sends the same `OUT,...` UDP command as the rest of
this repo (`tools/protocol.js`), straight from a Unity scene — replacing
the sample from Olorama's own Unity integration guide, which had a bug that
would have silently broken every send (see below).

## Setup

1. Copy `OloramaUDPSender.cs` and `GameManager.cs` into your Unity
   project's `Assets/Scripts/` (or wherever your scripts live).
2. Add `OloramaUDPSender` to a GameObject (e.g. an empty "ScentController").
3. Set **Target IP** and **Target Port** in the Inspector.
4. On any trigger volume (a GameObject with a Collider set to **Is
   Trigger**), add `GameManager`, drag the ScentController object into its
   **Scent Sender** field, and set the port/intensity/fan time you want.
5. Either the trigger volume or the object entering it (the player) needs a
   non-kinematic `Rigidbody`, or `OnTriggerEnter` never fires — a common
   silent failure that has nothing to do with the networking code.

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
