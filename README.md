# Scent Simulation

An interactive concept page for the Olorama Professional 10-Scents Generator,
plus a real, tested Node.js integration for its UDP control protocol.

## The page

`index.html` is a standalone page — open it in a browser. It previews all 10
cartridges of the "Emergency & Tactical Training" kit and shows the exact
device command each one sends, per the Olorama user guide's "Activation via
API" section. A **Scenarios** tab combines multiple ports for situations
where more than one scent would plausibly show up together — a vehicle
collision (blood + burnt rubber + gasoline), a volcanic eruption (volcano +
fire), and four others — firing them in the staggered sequence the CLI below
actually sends. The page can't fire real commands itself (browsers can't open
raw UDP sockets) — use the tools below for that.

## The tools

The wire protocol from the user guide:

```
OUT,[port 01-10],[intensity 0100-0500],1,[fan time 1000-9000 ms],1000
```

- `tools/protocol.js` — encodes/decodes that command, enforcing the
  documented ranges.
- `tools/scent-catalog.js` — the 10 cartridge names for the currently loaded
  kit, shared by the CLI, the simulator, and the page.
- `tools/scenario-catalog.js` — multi-port training scenarios (which ports
  fire together), mirrored by the page's Scenarios tab.
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

`--host`/`--port` default to `192.168.0.40:8000` — the manual doesn't print
the real UDP port; pull it from the PacketSender export file it links to, or
confirm it with Olorama support. Override the defaults with `OLORAMA_HOST` /
`OLORAMA_PORT`, or per-command with the flags.

Try it against the simulator instead of real hardware:

```
npm run simulate                                                    # terminal 1
node tools/activate-scent.js 2 --host 127.0.0.1 --port 8000 --intensity 150 --fan-ms 3000   # terminal 2
```

## Tests

```
npm test
```

Runs `node --test`: unit tests on the command encoding/decoding, plus
integration tests that send real UDP packets to the simulator and check all
10 ports decode to the right scent, intensity, and fan time.
