'use strict';

// Wire format from the Olorama Professional 10-Scents Generator user guide,
// section 3.2 "Activation via API":
//
//   OUT,[port 01-10],[intensity 0100-0500],1,[fan time 1000-9000 ms],1000
//
// The 3rd and 6th fields are documented as internal and must not be changed.

const PORT_COUNT = 10;
const INTENSITY_MIN = 100;
const INTENSITY_MAX = 500;
const FAN_MS_MIN = 1000;
const FAN_MS_MAX = 9000;

const FIXED_FIELD_3 = '1';
const FIXED_FIELD_6 = '1000';

function assertInRange(value, min, max, label) {
  if (typeof value !== 'number' || !Number.isFinite(value) || value < min || value > max) {
    throw new RangeError(`${label} must be a number between ${min} and ${max}, got ${value}`);
  }
}

function encodeCommand({ port, intensity = INTENSITY_MIN, fanMs = 2000 }) {
  assertInRange(port, 1, PORT_COUNT, 'port');
  assertInRange(intensity, INTENSITY_MIN, INTENSITY_MAX, 'intensity');
  assertInRange(fanMs, FAN_MS_MIN, FAN_MS_MAX, 'fanMs');

  return [
    'OUT',
    String(port).padStart(2, '0'),
    String(intensity).padStart(4, '0'),
    FIXED_FIELD_3,
    String(fanMs).padStart(5, '0'),
    FIXED_FIELD_6
  ].join(',');
}

function decodeCommand(message) {
  const raw = String(message).trim();
  const parts = raw.split(',');
  if (parts.length !== 6 || parts[0] !== 'OUT') {
    throw new Error(`not an Olorama OUT command: "${raw}"`);
  }
  const [, portStr, intensityStr, , fanStr] = parts;
  const port = Number(portStr);
  const intensity = Number(intensityStr);
  const fanMs = Number(fanStr);
  if (!Number.isInteger(port) || !Number.isInteger(intensity) || !Number.isInteger(fanMs)) {
    throw new Error(`could not parse numeric fields: "${raw}"`);
  }
  return { port, intensity, fanMs, raw };
}

module.exports = {
  encodeCommand,
  decodeCommand,
  PORT_COUNT,
  INTENSITY_MIN,
  INTENSITY_MAX,
  FAN_MS_MIN,
  FAN_MS_MAX
};
