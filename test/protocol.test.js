'use strict';

const test = require('node:test');
const assert = require('node:assert/strict');
const { encodeCommand, decodeCommand } = require('../tools/protocol');

test('encodeCommand pads fields per the Olorama UDP spec', () => {
  assert.equal(encodeCommand({ port: 2, intensity: 150, fanMs: 3000 }), 'OUT,02,0150,1,03000,1000');
  assert.equal(encodeCommand({ port: 10, intensity: 100, fanMs: 1000 }), 'OUT,10,0100,1,01000,1000');
});

test('encodeCommand defaults to minimum intensity and a 2s fan run', () => {
  assert.equal(encodeCommand({ port: 5 }), 'OUT,05,0100,1,02000,1000');
});

test('encodeCommand rejects values outside the manual\'s documented ranges', () => {
  assert.throws(() => encodeCommand({ port: 0, intensity: 100, fanMs: 1000 }), RangeError);
  assert.throws(() => encodeCommand({ port: 11, intensity: 100, fanMs: 1000 }), RangeError);
  assert.throws(() => encodeCommand({ port: 1, intensity: 99, fanMs: 1000 }), RangeError);
  assert.throws(() => encodeCommand({ port: 1, intensity: 501, fanMs: 1000 }), RangeError);
  assert.throws(() => encodeCommand({ port: 1, intensity: 100, fanMs: 999 }), RangeError);
  assert.throws(() => encodeCommand({ port: 1, intensity: 100, fanMs: 9001 }), RangeError);
});

test('decodeCommand parses what encodeCommand produced', () => {
  const message = encodeCommand({ port: 7, intensity: 300, fanMs: 5000 });
  assert.deepEqual(decodeCommand(message), { port: 7, intensity: 300, fanMs: 5000, raw: message });
});

test('decodeCommand rejects anything that is not an OUT command', () => {
  assert.throws(() => decodeCommand('HELLO,02,0100,1,02000,1000'), /not an Olorama OUT command/);
  assert.throws(() => decodeCommand('OUT,02,0100,1,02000'), /not an Olorama OUT command/);
});
