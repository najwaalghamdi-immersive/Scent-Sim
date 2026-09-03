'use strict';

const test = require('node:test');
const assert = require('node:assert/strict');
const { execFile } = require('node:child_process');
const path = require('node:path');
const { promisify } = require('node:util');
const { ScentDeviceSimulator } = require('../tools/scent-device-simulator');
const SCENARIOS = require('../tools/scenario-catalog');

const execFileAsync = promisify(execFile);
const CLI = path.join(__dirname, '..', 'tools', 'activate-scent.js');

test('every scenario references only valid ports 1-10', () => {
  for (const scenario of SCENARIOS) {
    assert.ok(scenario.ports.length >= 2, `${scenario.id} should combine at least 2 ports`);
    for (const port of scenario.ports) {
      assert.ok(port >= 1 && port <= 10, `${scenario.id} references an out-of-range port: ${port}`);
    }
  }
});

test('firing a scenario sends one command per port, in order, over real UDP', async () => {
  const sim = new ScentDeviceSimulator();
  const boundPort = await sim.listen(0);
  const scenario = SCENARIOS.find((s) => s.id === 'volcanic-eruption');

  const seen = [];
  const allReceived = new Promise((resolve) => {
    sim.on('scent', (event) => {
      seen.push(event.port);
      if (seen.length === scenario.ports.length) resolve();
    });
  });

  await execFileAsync(process.execPath, [
    CLI, '--scenario', scenario.id,
    '--host', '127.0.0.1', '--port', String(boundPort),
    '--stagger-ms', '10'
  ]);

  await allReceived;
  assert.deepEqual(seen, scenario.ports);

  await sim.close();
});
