'use strict';

const test = require('node:test');
const assert = require('node:assert/strict');
const { createBridgeServer } = require('../server/bridge-server');
const { ScentDeviceSimulator } = require('../tools/scent-device-simulator');

async function withBridge(options, fn) {
  const sim = new ScentDeviceSimulator();
  const simPort = await sim.listen(0);
  const bridge = createBridgeServer(Object.assign({ deviceHost: '127.0.0.1', devicePort: simPort }, options));
  await new Promise((resolve) => bridge.listen(0, resolve));
  const bridgeUrl = `http://127.0.0.1:${bridge.address().port}`;
  try {
    await fn({ sim, bridgeUrl });
  } finally {
    await new Promise((resolve) => bridge.close(resolve));
    await sim.close();
  }
}

test('GET /api/health reports the configured device address', async () => {
  await withBridge({}, async ({ bridgeUrl }) => {
    const res = await fetch(`${bridgeUrl}/api/health`);
    assert.equal(res.status, 200);
    const body = await res.json();
    assert.equal(body.ok, true);
    assert.equal(body.deviceHost, '127.0.0.1');
    assert.equal(body.authRequired, false);
  });
});

test('GET /api/ports returns the scent catalog', async () => {
  await withBridge({}, async ({ bridgeUrl }) => {
    const res = await fetch(`${bridgeUrl}/api/ports`);
    const ports = await res.json();
    assert.equal(ports.length, 10);
    assert.deepEqual(ports[0], { port: 1, name: 'Blood' });
  });
});

test('GET /api/scenarios returns the scenario catalog', async () => {
  await withBridge({}, async ({ bridgeUrl }) => {
    const res = await fetch(`${bridgeUrl}/api/scenarios`);
    const scenarios = await res.json();
    assert.ok(scenarios.find((s) => s.id === 'volcanic-eruption'));
  });
});

test('POST /api/activate forwards a real UDP command to the device', async () => {
  await withBridge({}, async ({ sim, bridgeUrl }) => {
    const received = new Promise((resolve) => sim.once('scent', resolve));
    const res = await fetch(`${bridgeUrl}/api/activate`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ port: 7, intensity: 150, fanMs: 3000 })
    });
    assert.equal(res.status, 200);
    const body = await res.json();
    assert.equal(body.sent, 'OUT,07,0150,1,03000,1000');

    const event = await received;
    assert.equal(event.port, 7);
    assert.equal(event.name, 'Gasoline');
  });
});

test('POST /api/activate rejects an out-of-range port without touching the socket', async () => {
  await withBridge({}, async ({ bridgeUrl }) => {
    const res = await fetch(`${bridgeUrl}/api/activate`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ port: 99 })
    });
    assert.equal(res.status, 400);
  });
});

test('POST /api/scenario fires every port in order, staggered', async () => {
  await withBridge({}, async ({ sim, bridgeUrl }) => {
    const seen = [];
    const done = new Promise((resolve) => {
      sim.on('scent', (event) => {
        seen.push(event.port);
        if (seen.length === 2) resolve();
      });
    });
    const res = await fetch(`${bridgeUrl}/api/scenario`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ id: 'active-shooter', staggerMs: 10 })
    });
    assert.equal(res.status, 200);
    await done;
    assert.deepEqual(seen, [3, 1]);
  });
});

test('unknown scenario id is rejected', async () => {
  await withBridge({}, async ({ bridgeUrl }) => {
    const res = await fetch(`${bridgeUrl}/api/scenario`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ id: 'not-a-real-scenario' })
    });
    assert.equal(res.status, 404);
  });
});

test('when BRIDGE_TOKEN is set, activate requires a matching x-bridge-token header', async () => {
  await withBridge({ token: 'secret' }, async ({ sim, bridgeUrl }) => {
    const noToken = await fetch(`${bridgeUrl}/api/activate`, {
      method: 'POST', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify({ port: 1 })
    });
    assert.equal(noToken.status, 401);

    const wrongToken = await fetch(`${bridgeUrl}/api/activate`, {
      method: 'POST', headers: { 'Content-Type': 'application/json', 'x-bridge-token': 'nope' }, body: JSON.stringify({ port: 1 })
    });
    assert.equal(wrongToken.status, 401);

    const received = new Promise((resolve) => sim.once('scent', resolve));
    const rightToken = await fetch(`${bridgeUrl}/api/activate`, {
      method: 'POST', headers: { 'Content-Type': 'application/json', 'x-bridge-token': 'secret' }, body: JSON.stringify({ port: 1 })
    });
    assert.equal(rightToken.status, 200);
    await received;
  });
});

test('GET routes stay open even when a token is required', async () => {
  await withBridge({ token: 'secret' }, async ({ bridgeUrl }) => {
    const res = await fetch(`${bridgeUrl}/api/health`);
    assert.equal(res.status, 200);
    const body = await res.json();
    assert.equal(body.authRequired, true);
  });
});
