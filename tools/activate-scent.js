#!/usr/bin/env node
'use strict';

// Sends one real UDP activation command to an Olorama Professional
// 10-Scents Generator, per the user guide's "Activation via API" section.
// Run as a standalone script (not in a browser — browsers can't open raw
// UDP sockets).
//
// Usage:
//   node tools/activate-scent.js <port 1-10> [--intensity 100-500] [--fan-ms 1000-9000] [--host IP] [--port UDP_PORT]
//   node tools/activate-scent.js --scenario <id> [--stagger-ms 300] [...same options]
//   node tools/activate-scent.js --list-scenarios
//
// OLORAMA_HOST / OLORAMA_PORT env vars set the defaults for --host / --port.
// 5010 is the default UDP port per Olorama's C++ integration guide
// (https://olorama.com/downloads/c_integration_guide.html, mirrored at
// reference/olorama_c_integration_guide.md) — the user guide PDF itself
// doesn't print it. Override it with --port / $OLORAMA_PORT if your unit's
// PacketSender export or Olorama support gives you a different value.

const dgram = require('dgram');
const { encodeCommand, PORT_COUNT, INTENSITY_MIN, INTENSITY_MAX, FAN_MS_MIN, FAN_MS_MAX } = require('./protocol');
const SCENT_NAMES = require('./scent-catalog');
const SCENARIOS = require('./scenario-catalog');

const DEFAULT_HOST = process.env.OLORAMA_HOST || '192.168.0.40';
const DEFAULT_UDP_PORT = Number(process.env.OLORAMA_PORT || 5010);

function parseArgs(argv) {
  const args = {
    intensity: INTENSITY_MIN,
    fanMs: 2000,
    host: DEFAULT_HOST,
    port: DEFAULT_UDP_PORT,
    staggerMs: 300,
    scenario: null,
    listScenarios: false
  };
  const positional = [];
  for (let i = 0; i < argv.length; i++) {
    const arg = argv[i];
    if (arg === '--host') args.host = argv[++i];
    else if (arg === '--port') args.port = Number(argv[++i]);
    else if (arg === '--intensity') args.intensity = Number(argv[++i]);
    else if (arg === '--fan-ms') args.fanMs = Number(argv[++i]);
    else if (arg === '--scenario') args.scenario = argv[++i];
    else if (arg === '--stagger-ms') args.staggerMs = Number(argv[++i]);
    else if (arg === '--list-scenarios') args.listScenarios = true;
    else positional.push(arg);
  }
  args.scentPort = Number(positional[0]);
  return args;
}

function printUsage() {
  console.log(`Usage: node tools/activate-scent.js <port 1-${PORT_COUNT}> [options]
   or: node tools/activate-scent.js --scenario <id> [options]
   or: node tools/activate-scent.js --list-scenarios

Options:
  --intensity N    ${INTENSITY_MIN}-${INTENSITY_MAX}, default ${INTENSITY_MIN} (manual recommends 100-300)
  --fan-ms N       ${FAN_MS_MIN}-${FAN_MS_MAX}, default 2000
  --host IP        device address, default ${DEFAULT_HOST} (or $OLORAMA_HOST)
  --port N         device UDP port, default ${DEFAULT_UDP_PORT} (or $OLORAMA_PORT)
  --stagger-ms N   delay between ports in a scenario, default 300

Ports:
${Object.entries(SCENT_NAMES).map(([n, name]) => `  ${n.padStart(2, ' ')}  ${name}`).join('\n')}

Scenarios:
${SCENARIOS.map((s) => `  ${s.id.padEnd(18, ' ')}${s.ports.map((p) => SCENT_NAMES[p]).join(' + ')}`).join('\n')}

Examples:
  node tools/activate-scent.js 2 --intensity 150 --fan-ms 3000   # Fire & Explosions
  node tools/activate-scent.js --scenario volcanic-eruption`);
}

function sendCommand(socket, { host, port, scentPort, intensity, fanMs }) {
  return new Promise((resolve, reject) => {
    let message;
    try {
      message = encodeCommand({ port: scentPort, intensity, fanMs });
    } catch (error) {
      reject(error);
      return;
    }
    socket.send(message, port, host, (error) => {
      if (error) reject(error);
      else resolve(message);
    });
  });
}

const sleep = (ms) => new Promise((resolve) => setTimeout(resolve, ms));

async function main() {
  const args = parseArgs(process.argv.slice(2));

  if (args.listScenarios) {
    for (const s of SCENARIOS) {
      console.log(`${s.id}\t${s.name}\t${s.ports.map((p) => SCENT_NAMES[p]).join(' + ')}`);
    }
    return;
  }

  const socket = dgram.createSocket('udp4');

  if (args.scenario) {
    const scenario = SCENARIOS.find((s) => s.id === args.scenario);
    if (!scenario) {
      console.error(`Unknown scenario "${args.scenario}". Run --list-scenarios to see the options.`);
      process.exitCode = 1;
      socket.close();
      return;
    }
    console.log(`Firing scenario "${scenario.name}": ${scenario.ports.map((p) => SCENT_NAMES[p]).join(' + ')}`);
    try {
      for (let i = 0; i < scenario.ports.length; i++) {
        const scentPort = scenario.ports[i];
        const message = await sendCommand(socket, { host: args.host, port: args.port, scentPort, intensity: args.intensity, fanMs: args.fanMs });
        console.log(`  Sent to ${args.host}:${args.port} -> ${message}  ("${SCENT_NAMES[scentPort]}")`);
        if (i < scenario.ports.length - 1) await sleep(args.staggerMs);
      }
    } catch (error) {
      console.error('Send failed:', error.message);
      process.exitCode = 1;
    } finally {
      socket.close();
    }
    return;
  }

  if (!Number.isInteger(args.scentPort) || args.scentPort < 1 || args.scentPort > PORT_COUNT) {
    printUsage();
    process.exitCode = 1;
    socket.close();
    return;
  }

  try {
    const message = await sendCommand(socket, { host: args.host, port: args.port, scentPort: args.scentPort, intensity: args.intensity, fanMs: args.fanMs });
    console.log(`Sent to ${args.host}:${args.port} -> ${message}  ("${SCENT_NAMES[args.scentPort] || 'Unknown scent'}")`);
  } catch (error) {
    console.error(error instanceof RangeError ? 'Invalid command:' : 'Send failed:', error.message);
    process.exitCode = 1;
  } finally {
    socket.close();
  }
}

main();
