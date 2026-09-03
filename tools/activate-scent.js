#!/usr/bin/env node
'use strict';

// Sends one real UDP activation command to an Olorama Professional
// 10-Scents Generator, per the user guide's "Activation via API" section.
// Run as a standalone script (not in a browser — browsers can't open raw
// UDP sockets).
//
// Usage:
//   node tools/activate-scent.js <port 1-10> [--intensity 100-500] [--fan-ms 1000-9000] [--host IP] [--port UDP_PORT]
//
// OLORAMA_HOST / OLORAMA_PORT env vars set the defaults for --host / --port.
// The UDP port isn't printed in the manual — confirm it from your device's
// PacketSender export (see the user guide, section 3.2) or Olorama support.

const dgram = require('dgram');
const { encodeCommand, PORT_COUNT, INTENSITY_MIN, INTENSITY_MAX, FAN_MS_MIN, FAN_MS_MAX } = require('./protocol');
const SCENT_NAMES = require('./scent-catalog');

const DEFAULT_HOST = process.env.OLORAMA_HOST || '192.168.0.40';
const DEFAULT_UDP_PORT = Number(process.env.OLORAMA_PORT || 8000);

function parseArgs(argv) {
  const args = { intensity: INTENSITY_MIN, fanMs: 2000, host: DEFAULT_HOST, port: DEFAULT_UDP_PORT };
  const positional = [];
  for (let i = 0; i < argv.length; i++) {
    const arg = argv[i];
    if (arg === '--host') args.host = argv[++i];
    else if (arg === '--port') args.port = Number(argv[++i]);
    else if (arg === '--intensity') args.intensity = Number(argv[++i]);
    else if (arg === '--fan-ms') args.fanMs = Number(argv[++i]);
    else positional.push(arg);
  }
  args.scentPort = Number(positional[0]);
  return args;
}

function printUsage() {
  console.log(`Usage: node tools/activate-scent.js <port 1-${PORT_COUNT}> [options]

Options:
  --intensity N   ${INTENSITY_MIN}-${INTENSITY_MAX}, default ${INTENSITY_MIN} (manual recommends 100-300)
  --fan-ms N      ${FAN_MS_MIN}-${FAN_MS_MAX}, default 2000
  --host IP       device address, default ${DEFAULT_HOST} (or $OLORAMA_HOST)
  --port N        device UDP port, default ${DEFAULT_UDP_PORT} (or $OLORAMA_PORT)

Ports:
${Object.entries(SCENT_NAMES).map(([n, name]) => `  ${n.padStart(2, ' ')}  ${name}`).join('\n')}

Example:
  node tools/activate-scent.js 2 --intensity 150 --fan-ms 3000   # Fire & Explosions`);
}

function main() {
  const args = parseArgs(process.argv.slice(2));
  if (!Number.isInteger(args.scentPort) || args.scentPort < 1 || args.scentPort > PORT_COUNT) {
    printUsage();
    process.exitCode = 1;
    return;
  }

  let message;
  try {
    message = encodeCommand({ port: args.scentPort, intensity: args.intensity, fanMs: args.fanMs });
  } catch (error) {
    console.error('Invalid command:', error.message);
    process.exitCode = 1;
    return;
  }

  const socket = dgram.createSocket('udp4');
  socket.send(message, args.port, args.host, (error) => {
    if (error) {
      console.error('Send failed:', error.message);
      process.exitCode = 1;
    } else {
      const name = SCENT_NAMES[args.scentPort] || 'Unknown scent';
      console.log(`Sent to ${args.host}:${args.port} -> ${message}  ("${name}")`);
    }
    socket.close();
  });
}

main();
