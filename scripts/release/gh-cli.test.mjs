import test from 'node:test';
import assert from 'node:assert/strict';
import { createServer } from 'node:https';
import { createServer as createProxy } from 'node:http';
import { connect } from 'node:net';
import { execFileSync, spawnSync } from 'node:child_process';
import { mkdtempSync, readFileSync, rmSync } from 'node:fs';
import { tmpdir } from 'node:os';
import { join } from 'node:path';
import { GitHub } from './github.mjs';
import { runGh } from './gh-cli.mjs';
const gh = spawnSync('gh', ['--version']);
const openssl = spawnSync('openssl', ['version']);
// Go's SSL_CERT_FILE test trust override is Unix-specific. Windows uses its OS
// certificate store; the CLI command contract is still covered by injected tests.
test('installed gh performs authenticated API pagination against a local TLS server', {
  skip: process.platform === 'win32' || gh.error?.code === 'ENOENT' || openssl.error?.code === 'ENOENT',
}, async t => {
  assert.equal(gh.status, 0); assert.equal(openssl.status, 0);
  const directory = mkdtempSync(join(tmpdir(), 'xrmmockup-gh-tls-'));
  const key = join(directory, 'key.pem'), cert = join(directory, 'cert.pem');
  execFileSync('openssl', ['req', '-x509', '-newkey', 'rsa:2048', '-nodes', '-keyout', key, '-out', cert, '-days', '1',
    '-subj', '/CN=github.example', '-addext', 'subjectAltName=DNS:github.example', '-addext', 'basicConstraints=critical,CA:TRUE'], { stdio: 'pipe' });
  const requests = [];
  const server = createServer({ key: readFileSync(key), cert: readFileSync(cert) }, async (req, res) => {
    requests.push(req); const url = new URL(req.url, `https://${req.headers.host}`);
    if (url.pathname.endsWith('/check-runs')) {
      const chunks = []; for await (const chunk of req) chunks.push(chunk);
      res.setHeader('content-type', 'application/json'); return res.end(Buffer.concat(chunks));
    }
    if (url.pathname.endsWith('/releases/assets/1')) { res.setHeader('content-type', 'application/octet-stream'); return res.end(Buffer.from([0, 255, 128, 10])); }
    if (url.pathname.endsWith('/empty')) { res.statusCode = 204; return res.end(); }
    if (url.pathname.endsWith('/missing')) { res.statusCode = 404; res.setHeader('content-type', 'application/json'); return res.end(JSON.stringify({ message: 'Not Found' })); }
    res.setHeader('content-type', 'application/json');
    if (url.searchParams.get('page') === '2') res.end(JSON.stringify([{ id: 101, draft: true, tag_name: 'core-v1.20.3' }]));
    else {
      res.setHeader('link', `<https://${req.headers.host}${url.pathname}?per_page=100&page=2>; rel="next"`);
      res.end(JSON.stringify(Array.from({ length: 100 }, (_, id) => ({ id, tag_name: `old-${id}` }))));
    }
  });
  await new Promise(resolve => server.listen(0, '127.0.0.1', resolve));
  // gh accepts a hostname, not a host:port. A local CONNECT proxy sends the
  // fake enterprise host's TLS traffic to this test server without external DNS.
  const proxy = createProxy();
  proxy.on('connect', (req, socket, head) => {
    if (req.url !== 'github.example:443') return socket.destroy();
    const upstream = connect(server.address().port, '127.0.0.1', () => {
      socket.write('HTTP/1.1 200 Connection Established\r\n\r\n');
      if (head.length) upstream.write(head);
      socket.pipe(upstream); upstream.pipe(socket);
    });
    socket.on('error', () => upstream.destroy()); upstream.on('error', () => socket.destroy());
  });
  await new Promise(resolve => proxy.listen(0, '127.0.0.1', resolve));
  t.after(async () => {
    await Promise.all([new Promise(resolve => proxy.close(resolve)), new Promise(resolve => server.close(resolve))]);
    rmSync(directory, { recursive: true, force: true });
  });
  const api = new GitHub({ repository: 'owner/repo', token: 'fixture-token', host: 'github.example', environment: {
    SSL_CERT_FILE: cert, HTTPS_PROXY: `http://127.0.0.1:${proxy.address().port}`, NO_PROXY: '', no_proxy: '',
  } });
  const result = await api.releaseByTag('core-v1.20.3');
  assert.equal(result.id, 101); assert.equal(result.draft, true);
  assert.equal(requests.length, 2);
  assert.ok(requests.every(req => req.headers.authorization?.includes('fixture-token')));
  assert.ok(requests.every(req => req.url.includes('/repos/owner/repo/releases')));
  const body = { text: 'Multiline\nJSON with "quotes" stays data', value: 12 };
  assert.deepEqual(await api.repo('/check-runs', { method: 'POST', body }), body);
  assert.deepEqual(await api.asset({ id: 1 }), Buffer.from([0, 255, 128, 10]));
  assert.equal(await api.repo('/empty'), null);
  assert.equal(await api.repo('/missing', { allow404: true }), null);
});

test('an actual CLI operation fails with ENOENT when gh is unavailable', async () => {
  const env = Object.fromEntries(Object.entries(process.env).filter(([key]) => key.toUpperCase() !== 'PATH'));
  env.PATH = '';
  await assert.rejects(runGh(['api', '/repos/owner/repo/releases'], { env }), { code: 'ENOENT' });
});
