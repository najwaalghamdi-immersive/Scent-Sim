'use strict';

const dgram = require('dgram');
const { EventEmitter } = require('events');
const { decodeCommand } = require('./protocol');
const SCENT_NAMES = require('./scent-catalog');

// A local stand-in for the real Olorama generator: it listens on UDP and
// decodes the same "OUT,..." command the manual documents, so the sender
// in activate-scent.js can be tested without physical hardware. It does not
// otherwise emulate the device's firmware (e.g. it does not send back any
// acknowledgement — the manual documents the real protocol as fire-and-forget).
class ScentDeviceSimulator extends EventEmitter {
  constructor() {
    super();
    this.socket = dgram.createSocket('udp4');
    this.socket.on('message', (msg, rinfo) => this._handleMessage(msg, rinfo));
  }

  _handleMessage(msg, rinfo) {
    let command;
    try {
      command = decodeCommand(msg);
    } catch (error) {
      this.emit('invalid', { error, raw: msg.toString(), rinfo });
      return;
    }
    const name = SCENT_NAMES[command.port] || 'Unknown scent';
    this.emit('scent', { ...command, name, rinfo });
  }

  listen(port = 0) {
    return new Promise((resolve, reject) => {
      this.socket.once('error', reject);
      this.socket.bind(port, () => {
        this.socket.removeListener('error', reject);
        resolve(this.socket.address().port);
      });
    });
  }

  close() {
    return new Promise((resolve) => this.socket.close(resolve));
  }
}

module.exports = { ScentDeviceSimulator };

if (require.main === module) {
  const requestedPort = Number(process.argv[2] || process.env.OLORAMA_PORT || 5010);
  const sim = new ScentDeviceSimulator();

  sim.on('scent', ({ port, name, intensity, fanMs, raw, rinfo }) => {
    console.log(
      `[scent-sim] ${raw}  ->  port ${String(port).padStart(2, '0')} "${name}" ` +
      `@ intensity ${intensity} for ${fanMs}ms  (from ${rinfo.address}:${rinfo.port})`
    );
  });
  sim.on('invalid', ({ raw, error, rinfo }) => {
    console.warn(`[scent-sim] ignored malformed packet from ${rinfo.address}:${rinfo.port}: "${raw}" (${error.message})`);
  });

  sim.listen(requestedPort).then((boundPort) => {
    console.log(`[scent-sim] listening on udp://0.0.0.0:${boundPort}`);
    console.log('[scent-sim] this is a local test double for the real generator, not the real device.');
    console.log('[scent-sim] Ctrl+C to stop.');
  }).catch((error) => {
    console.error('[scent-sim] failed to start:', error.message);
    process.exitCode = 1;
  });
}
