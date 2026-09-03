'use strict';

const test = require('node:test');
const assert = require('node:assert/strict');
const dgram = require('dgram');
const { ScentDeviceSimulator } = require('../tools/scent-device-simulator');
const { encodeCommand } = require('../tools/protocol');

function sendUdp(message, port, host) {
  return new Promise((resolve, reject) => {
    const socket = dgram.createSocket('udp4');
    socket.send(message, port, host, (error) => {
      socket.close();
      error ? reject(error) : resolve();
    });
  });
}

test('a real UDP activation command reaches the simulator and decodes correctly', async () => {
  const sim = new ScentDeviceSimulator();
  const boundPort = await sim.listen(0);

  const received = new Promise((resolve) => sim.once('scent', resolve));
  const message = encodeCommand({ port: 7, intensity: 150, fanMs: 3000 });
  await sendUdp(message, boundPort, '127.0.0.1');

  const event = await received;
  assert.equal(event.port, 7);
  assert.equal(event.intensity, 150);
  assert.equal(event.fanMs, 3000);
  assert.equal(event.name, 'Gasoline');

  await sim.close();
});

test('the simulator flags malformed packets instead of crashing', async () => {
  const sim = new ScentDeviceSimulator();
  const boundPort = await sim.listen(0);

  const invalid = new Promise((resolve) => sim.once('invalid', resolve));
  await sendUdp('NOT,A,VALID,PACKET', boundPort, '127.0.0.1');

  const event = await invalid;
  assert.match(event.raw, /NOT,A,VALID,PACKET/);

  await sim.close();
});

test('every port 1-10 activates and reports its own scent name', async () => {
  const sim = new ScentDeviceSimulator();
  const boundPort = await sim.listen(0);

  for (let port = 1; port <= 10; port++) {
    const received = new Promise((resolve) => sim.once('scent', resolve));
    await sendUdp(encodeCommand({ port, intensity: 100, fanMs: 1000 }), boundPort, '127.0.0.1');
    const event = await received;
    assert.equal(event.port, port);
    assert.ok(event.name && event.name !== 'Unknown scent', `port ${port} should map to a known scent`);
  }

  await sim.close();
});
