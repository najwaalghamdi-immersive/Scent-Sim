#!/usr/bin/env node
'use strict';

// A tiny local HTTP-to-UDP bridge. The page (index.html) can't open a raw
// UDP socket — no browser can — so it makes an ordinary same-machine HTTP
// request here instead, and this process sends the real UDP command the
// browser sandbox won't allow.
//
// Run it: node server/bridge-server.js
// Then, on the page, turn on "Send to real device via Bridge" and point it
// at this server's URL (default http://localhost:8787).
//
// Security: every request this server accepts turns into a real UDP packet
// aimed at physical hardware (a fan + scent cartridges). It binds to
// 127.0.0.1 only by default — set BRIDGE_HOST=0.0.0.0 to expose it on your
// LAN (e.g. so a tablet running the page can reach it) — and set
// BRIDGE_TOKEN to require every activation request to carry a matching
// `x-bridge-token` header. With neither set, any page open in a browser on
// this machine can trigger the device; that's the trade-off of a local
// bridge with no auth, so don't set BRIDGE_HOST=0.0.0.0 without also
// setting BRIDGE_TOKEN.

const http = require('http');
const dgram = require('dgram');
const { encodeCommand } = require('../tools/protocol');
const SCENT_NAMES = require('../tools/scent-catalog');
const SCENARIOS = require('../tools/scenario-catalog');

const sleep = (ms) => new Promise((resolve) => setTimeout(resolve, ms));

function readJsonBody(req) {
  return new Promise((resolve, reject) => {
    let data = '';
    req.on('data', (chunk) => {
      data += chunk;
      if (data.length > 1e5) req.destroy(new Error('body too large'));
    });
    req.on('end', () => {
      if (!data) { resolve({}); return; }
      try { resolve(JSON.parse(data)); }
      catch (error) { reject(error); }
    });
    req.on('error', reject);
  });
}

function sendJson(res, status, body) {
  res.writeHead(status, { 'Content-Type': 'application/json' });
  res.end(JSON.stringify(body));
}

function withCors(req, res) {
  const origin = req.headers.origin;
  // file:// pages send Origin: null; reflect any real origin, else allow all.
  res.setHeader('Access-Control-Allow-Origin', origin && origin !== 'null' ? origin : '*');
  res.setHeader('Access-Control-Allow-Methods', 'GET, POST, OPTIONS');
  res.setHeader('Access-Control-Allow-Headers', 'Content-Type, x-bridge-token');
}

/**
 * Builds the bridge's http.Server without starting it, so tests can bind it
 * to an ephemeral port and point it at a simulator instead of a real device.
 */
function createBridgeServer(options) {
  options = options || {};
  const deviceHost = options.deviceHost || process.env.OLORAMA_HOST || '192.168.0.40';
  const devicePort = Number(options.devicePort || process.env.OLORAMA_PORT || 5010);
  const token = options.token || process.env.BRIDGE_TOKEN || '';

  const socket = dgram.createSocket('udp4');

  function sendUdp(port, intensity, fanMs) {
    return new Promise((resolve, reject) => {
      let message;
      try {
        message = encodeCommand({ port, intensity, fanMs });
      } catch (error) {
        reject(error);
        return;
      }
      socket.send(message, devicePort, deviceHost, (error) => {
        if (error) reject(error);
        else resolve(message);
      });
    });
  }

  function authorized(req) {
    return !token || req.headers['x-bridge-token'] === token;
  }

  const server = http.createServer(async (req, res) => {
    withCors(req, res);

    if (req.method === 'OPTIONS') {
      res.writeHead(204);
      res.end();
      return;
    }

    let url;
    try {
      url = new URL(req.url, `http://${req.headers.host || 'localhost'}`);
    } catch {
      sendJson(res, 400, { ok: false, error: 'invalid request URL' });
      return;
    }

    if (req.method === 'GET' && url.pathname === '/api/health') {
      sendJson(res, 200, { ok: true, deviceHost, devicePort, authRequired: !!token });
      return;
    }

    if (req.method === 'GET' && url.pathname === '/api/ports') {
      sendJson(res, 200, Object.entries(SCENT_NAMES).map(([port, name]) => ({ port: Number(port), name })));
      return;
    }

    if (req.method === 'GET' && url.pathname === '/api/scenarios') {
      sendJson(res, 200, SCENARIOS);
      return;
    }

    if (!authorized(req)) {
      sendJson(res, 401, { ok: false, error: 'unauthorized: missing or wrong x-bridge-token header' });
      return;
    }

    if (req.method === 'POST' && url.pathname === '/api/activate') {
      let body;
      try { body = await readJsonBody(req); }
      catch { sendJson(res, 400, { ok: false, error: 'invalid JSON body' }); return; }

      try {
        const message = await sendUdp(body.port, body.intensity ?? 100, body.fanMs ?? 2000);
        sendJson(res, 200, { ok: true, sent: message });
      } catch (error) {
        sendJson(res, 400, { ok: false, error: error.message });
      }
      return;
    }

    if (req.method === 'POST' && url.pathname === '/api/scenario') {
      let body;
      try { body = await readJsonBody(req); }
      catch { sendJson(res, 400, { ok: false, error: 'invalid JSON body' }); return; }

      const scenario = SCENARIOS.find((s) => s.id === body.id);
      if (!scenario) { sendJson(res, 404, { ok: false, error: `unknown scenario "${body.id}"` }); return; }

      try {
        const sent = [];
        for (let i = 0; i < scenario.ports.length; i++) {
          const message = await sendUdp(scenario.ports[i], body.intensity ?? 100, body.fanMs ?? 2000);
          sent.push(message);
          if (i < scenario.ports.length - 1) await sleep(body.staggerMs ?? 300);
        }
        sendJson(res, 200, { ok: true, sent });
      } catch (error) {
        sendJson(res, 400, { ok: false, error: error.message });
      }
      return;
    }

    sendJson(res, 404, { ok: false, error: 'not found' });
  });

  server.on('close', () => socket.close());
  return server;
}

module.exports = { createBridgeServer };

if (require.main === module) {
  const host = process.env.BRIDGE_HOST || '127.0.0.1';
  const port = Number(process.env.BRIDGE_PORT || 8787);
  const server = createBridgeServer({});

  server.listen(port, host, () => {
    console.log(`[bridge] listening on http://${host}:${port}`);
    console.log(`[bridge] forwarding to Olorama device at ${process.env.OLORAMA_HOST || '192.168.0.40'}:${process.env.OLORAMA_PORT || 5010}`);
    if (!process.env.BRIDGE_TOKEN) {
      console.log('[bridge] WARNING: no BRIDGE_TOKEN set — any page open in a browser on this machine can trigger the device.');
    }
    if (host === '0.0.0.0' && !process.env.BRIDGE_TOKEN) {
      console.log('[bridge] WARNING: bound to 0.0.0.0 with no token — anyone on your network can trigger the device.');
    }
  });
}
