# Scent Simulation

An interactive concept page for the Olorama Professional 10-Scents Generator,
plus a real, tested Node.js integration for its UDP control protocol.

## The page

`index.html` is a standalone page — open it in a browser.

**Load Your Cartridges** is the actual inventory model: two cascading
dropdowns per physical port (library, then scent within it) across the real
catalog of 61 cartridges in 4 libraries — Nature Smells, Food Smells,
Ambiance & Fragrance, and Action Scene Smells (the original 10 tactical
scents). Each library has its own icon and color, used consistently for the
port grid, the configurator tags, and the atmosphere picker — icons
represent the library, not the individual scent, so a new scent in an
existing library needs no new artwork.

The port grid, the **Scenarios** tab, and the **Scent Atmosphere** preview
all follow whatever you've loaded. Scenarios need 2-3 specific scents
present somewhere in your 10 ports (e.g. Vehicle Collision needs Blood +
Burnt Rubber + Gasoline); the grid sorts ones you can fire right now first
and shows exactly what to load to unlock the rest. Firing one resolves each
needed scent to its current port and fires them in a staggered sequence.

The page can't open a raw UDP socket itself — no browser can — but turning
on **Send to real device via Bridge** in the console makes it send real
commands through the bridge server below instead of only previewing them.

**Branding** is pulled from `Digital_Saudi_Template_En.pptx` (the "Digital
Saudi" theme in `ppt/theme/theme1.xml`, plus its logo asset), not guessed:
navy `#271D6A` (their dominant text color, used 377× in the deck) as `--ink`,
their accent1 sky-blue `#15AAFD` (used 57×, clearly their primary
interactive color) as `--brand`, and their accent3 red `#B40000` as
`--danger`. `--brand` and the Food library color are darkened from the raw
theme hex (`#15AAFD`→`#027CC1`, `#FFA92B`→`#AB6600`) because the originals
are under 2:1 contrast against white — too light to use as text or under
white icons/labels; the undarkened tones only appear in the brand mark's
gradient, extracted from the template's own logo. Action, Food, and
Ambiance & Fragrance library colors are the brand's red/orange/purple;
Nature stays a dedicated green since the palette has no brand green to draw
on. The logo itself (`assets/digital-saudi-logo.png`, also inlined as a data
URI in the page) is cropped from the template's own asset, not redrawn.

## The tools

The wire protocol from the user guide:

```
OUT,[port 01-10],[intensity 0100-0500],1,[fan time 1000-9000 ms],1000
```

- `tools/protocol.js` — encodes/decodes that command, enforcing the
  documented ranges.
- `tools/scent-catalog.js` — the 10 Action Scene cartridge names, shared by
  the CLI and the simulator. The page's fuller library/scent picker (61
  scents across 4 libraries) is a planning view independent of this — the
  CLI and bridge only ever know "fire physical port N."
- `tools/scenario-catalog.js` — the CLI's 6 built-in tactical scenarios
  (which ports fire together). The page has 12, evaluated dynamically
  against whatever you've loaded rather than fixed port numbers.
- `tools/activate-scent.js` — CLI that sends one real UDP command, or a whole
  scenario as a staggered sequence of commands, to a generator.
- `tools/scent-device-simulator.js` — a local UDP listener that decodes the
  same command, for developing and testing against without physical
  hardware. It is not a firmware emulation — it just proves a given payload
  is well-formed and lets you see what the device would receive.

```
node tools/activate-scent.js <port 1-10> [--intensity 100-500] [--fan-ms 1000-9000] [--host IP] [--port UDP_PORT]
node tools/activate-scent.js --scenario <id> [--stagger-ms 300] [...same options]
node tools/activate-scent.js --list-scenarios
```

`--host`/`--port` default to `192.168.0.40:5010`. The user guide PDF doesn't
print the UDP port, but Olorama's C++ integration guide does (mirrored at
`reference/olorama_c_integration_guide.md`) — 5010 is its documented default.
If your unit reports a different one (PacketSender export, or Olorama
support), override it with `OLORAMA_HOST`/`OLORAMA_PORT` or the flags.

Try it against the simulator instead of real hardware:

```
npm run simulate                                                    # terminal 1
node tools/activate-scent.js 2 --host 127.0.0.1 --port 5010 --intensity 150 --fan-ms 3000   # terminal 2
```

## The bridge (real activation from the page)

Browsers can't send UDP, so `index.html`'s toggle instead talks to a small
local HTTP server that can — it turns each request into the exact same
`OUT,...` command as the CLI, over the same `tools/protocol.js`.

```
npm run bridge                              # listens on http://127.0.0.1:8787
```

Then on the page, check **Send to real device via Bridge** in the console
and confirm the URL matches (default `http://localhost:8787`) — every port
click or scenario fire now also goes out over real UDP, in addition to the
on-page preview.

Endpoints: `GET /api/health`, `GET /api/ports`, `GET /api/scenarios`,
`POST /api/activate` (`{port, intensity?, fanMs?}`), `POST /api/scenario`
(`{id, intensity?, fanMs?, staggerMs?}`).

**Security:** every request this server accepts turns into a real UDP packet
aimed at physical hardware. It binds to `127.0.0.1` only by default — set
`BRIDGE_HOST=0.0.0.0` only if you need it reachable from elsewhere on your
LAN (e.g. a tablet running the page), and set `BRIDGE_TOKEN=<secret>` to
require every activation request to carry a matching `x-bridge-token`
header. With neither set, any page open in a browser on the same machine
could trigger the device while the bridge is running — that's fine on a
laptop only you use, but set a token before binding it to your network.

`OLORAMA_HOST`/`OLORAMA_PORT` (same as the CLI) set which device the bridge
forwards to; `BRIDGE_PORT` changes which port the bridge itself listens on.

## Tests

```
npm test
```

Runs `node --test`: unit tests on the command encoding/decoding, integration
tests that send real UDP packets to the simulator and check all 10 ports and
every scenario decode correctly, and tests that drive the bridge server over
real HTTP (including its token-auth path) end-to-end through to the
simulator.
