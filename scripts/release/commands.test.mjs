import test from 'node:test';
import assert from 'node:assert/strict';
import { execFile, execFileSync } from 'node:child_process';
import { promisify } from 'node:util';
import { mkdtempSync, mkdirSync, writeFileSync, readFileSync, rmSync, existsSync } from 'node:fs';
import { tmpdir } from 'node:os';
import { join, resolve } from 'node:path';
import { fileURLToPath, pathToFileURL } from 'node:url';
import { createServer } from 'node:http';
import { products, renderChangelog, makeSnapshot, applyRelease, hash, releaseMarker } from './model.mjs';

const execute = promisify(execFile), scriptDir = fileURLToPath(new URL('.', import.meta.url));
const g = (cwd, ...args) => execFileSync('git', args, { cwd, encoding: 'utf8', stdio: ['ignore', 'pipe', 'pipe'] }).trim();
const write = (cwd, path, data) => { mkdirSync(resolve(cwd, path, '..'), { recursive: true }); writeFileSync(join(cwd, path), typeof data === 'string' ? data : JSON.stringify(data, null, 2) + '\n'); };
async function fixture(t) {
  const root = mkdtempSync(join(tmpdir(), 'xrmmockup-release-')), work = join(root, 'work'), remote = join(root, 'origin.git');
  mkdirSync(work); g(work, 'init', '-b', 'master');
  g(work, 'config', 'user.name', 'maintainer'); g(work, 'config', 'user.email', 'maintainer@example.test');
  write(work, '.gitignore', '.release/\n');
  for (const config of Object.values(products)) write(work, config.changelog, '### 1.0.0 - archive\n* Historical release\n');
  g(work, 'add', '.'); g(work, 'commit', '-m', 'Baseline');
  const baselineSha = g(work, 'rev-parse', 'HEAD');
  const state = { schemaVersion: 1, baselineSha, baselineVersions: { core: '1.20.2', metadata: '1.2.0' }, entries: [], releases: [] };
  write(work, '.github/release-state.json', state);
  for (const [p, config] of Object.entries(products)) write(work, config.changelog, renderChangelog(readFileSync(join(work, config.changelog), 'utf8'), state, p));
  g(work, 'config', 'user.name', 'release-changelog-bot'); g(work, 'config', 'user.email', 'release-changelog-bot@users.noreply.github.com');
  g(work, 'add', '.'); g(work, 'commit', '-m', 'Chore: Initialize ledger');
  g(work, 'config', 'user.name', 'maintainer'); g(work, 'config', 'user.email', 'maintainer@example.test');
  g(root, 'init', '--bare', remote); g(work, 'remote', 'add', 'origin', remote); g(work, 'push', '-u', 'origin', 'master');
  const pullRequests = [], requests = [], releases = [], assets = new Map();
  const server = createServer(async (req, res) => {
    try {
      const url = new URL(req.url, 'http://localhost'), path = url.pathname.replace('/repos/owner/repo', '');
      const chunks = []; for await (const chunk of req) chunks.push(chunk);
      const bytes = Buffer.concat(chunks), body = bytes.length && req.headers['content-type']?.includes('application/json') ? JSON.parse(bytes) : undefined;
      requests.push({ method: req.method, path, body });
      const json = value => { res.setHeader('content-type', 'application/json'); res.end(JSON.stringify(value)); };
      const sha = /^\/commits\/([^/]+)\/pulls$/.exec(path)?.[1];
      if (sha) return json(pullRequests.filter(pr => pr.merge_commit_sha === sha));
      const number = /^\/pulls\/(\d+)$/.exec(path)?.[1];
      if (number) return json(pullRequests.find(pr => pr.number === Number(number)));
      if (path === '/check-runs') return json({ id: 1 });
      if (path === '/releases' && req.method === 'GET') { const start = (Number(url.searchParams.get('page') ?? 1) - 1) * 100; return json(releases.slice(start, start + 100)); }
      if (path === '/releases' && req.method === 'POST') {
        const release = { id: releases.length + 1, created_at: '2026-11-05T10:00:00Z', ...body, upload_url: `${base}/repos/owner/repo/upload/${releases.length + 1}{?name,label}` };
        releases.push(release); return json(release);
      }
      if (path.startsWith('/releases/tags/')) { const release = releases.find(r => !r.draft && r.tag_name === decodeURIComponent(path.slice('/releases/tags/'.length))); if (release) return json(release); res.statusCode = 404; return res.end('Not found'); }
      const assetList = /^\/releases\/(\d+)\/assets$/.exec(path);
      if (assetList) return json([...assets.values()].filter(a => a.releaseId === Number(assetList[1])).map(({ bytes, ...a }) => a));
      const assetId = /^\/releases\/assets\/(\d+)$/.exec(path)?.[1];
      if (assetId) { const asset = assets.get(Number(assetId)); res.end(asset.bytes); return; }
      const uploadId = /^\/upload\/(\d+)$/.exec(path)?.[1];
      if (uploadId) {
        const id = assets.size + 1, name = url.searchParams.get('name'); assets.set(id, { id, name, bytes, releaseId: Number(uploadId) }); return json({ id, name });
      }
      const releaseId = /^\/releases\/(\d+)$/.exec(path)?.[1];
      if (releaseId && req.method === 'PATCH') { Object.assign(releases.find(r => r.id === Number(releaseId)), body); return json(releases.find(r => r.id === Number(releaseId))); }
      if (path === '/actions/workflows/update-changelogs.yml/dispatches') { res.statusCode = 204; return res.end(); }
      res.statusCode = 500; res.end(`Unhandled test request ${req.method} ${path}`);
    } catch (error) { res.statusCode = 500; res.end(error.stack); }
  });
  await new Promise(resolve => server.listen(0, '127.0.0.1', resolve));
  const base = `http://127.0.0.1:${server.address().port}`;
  t.after(async () => { await new Promise(resolve => server.close(resolve)); rmSync(root, { recursive: true, force: true }); });
  const env = { ...process.env, GITHUB_REPOSITORY: 'owner/repo', GH_TOKEN: 'test', GITHUB_API_URL: base, GITHUB_OUTPUT: join(root, 'outputs'), GITHUB_STEP_SUMMARY: join(root, 'summary') };
  function feature(labels = ['product:core'], title = 'Fix: A feature fix') {
    g(work, 'fetch', 'origin', 'master'); g(work, 'checkout', '-B', 'master', 'origin/master');
    g(work, 'config', 'user.name', 'maintainer'); g(work, 'config', 'user.email', 'maintainer@example.test');
    const number = pullRequests.length + 1; write(work, `feature-${number}.txt`, title);
    g(work, 'add', '.'); g(work, 'commit', '-m', title); const sha = g(work, 'rev-parse', 'HEAD');
    const pr = { number, title, labels: labels.map(name => ({ name })), merged: true, merged_at: '2026-11-05T09:00:00Z', merge_commit_sha: sha, base: { ref: 'master' }, head: { sha } };
    pullRequests.push(pr); g(work, 'push', 'origin', 'master'); return pr;
  }
  async function run(script, args = [], overrides = {}) { return execute(process.execPath, [join(scriptDir, script), ...args], { cwd: work, env: { ...env, ...overrides }, maxBuffer: 5 * 1024 * 1024 }); }
  const readState = () => JSON.parse(readFileSync(join(work, '.github/release-state.json'), 'utf8'));
  return { root, work, remote, state, feature, run, readState, pullRequests, requests, releases, assets, env };
}

test('merge reconciler writes both product files, excludes maintenance, and is retry-safe', async t => {
  const f = await fixture(t); f.feature(['product:core', 'product:metadata']); f.feature(['product:none'], 'Chore: Update infrastructure');
  await f.run('update-changelogs.mjs');
  const committed = g(f.work, 'rev-parse', 'HEAD'), state = f.readState();
  assert.equal(state.entries.length, 2);
  for (const config of Object.values(products)) {
    const log = readFileSync(join(f.work, config.changelog), 'utf8'); assert.match(log, /Fix: A feature fix \(#1\)/); assert.ok(!log.includes('Update infrastructure'));
  }
  assert.equal(g(f.remote, 'rev-parse', 'master'), committed);
  await f.run('update-changelogs.mjs'); assert.equal(g(f.work, 'rev-parse', 'HEAD'), committed);
  f.pullRequests[0].title = 'Breaking: Edited after reconciliation';
  await f.run('update-changelogs.mjs'); assert.equal(f.readState().entries[0].category, 'Fix');
});
test('merge reconciler rejects invalid classification without committing', async t => {
  const f = await fixture(t); f.feature([], 'Fix: Missing product'); const before = g(f.remote, 'rev-parse', 'master');
  await assert.rejects(f.run('update-changelogs.mjs'), /Add product:core/);
  assert.equal(g(f.remote, 'rev-parse', 'master'), before);
});
test('human direct commits cannot silently bypass PR classification', async t => {
  const f = await fixture(t); write(f.work, 'unclassified.txt', 'change'); g(f.work, 'add', '.'); g(f.work, 'commit', '-m', 'Direct change'); g(f.work, 'push', 'origin', 'master');
  await assert.rejects(f.run('update-changelogs.mjs'), /no classified merged master PR/);
});
test('release freshness gate rejects a tag that races reconciliation', async t => {
  const f = await fixture(t); f.feature(); g(f.work, 'tag', 'core-v1.20.3'); g(f.work, 'push', 'origin', '--tags');
  await assert.rejects(f.run('prepare-release.mjs', [], { GITHUB_EVENT_NAME: 'push', GITHUB_REF_NAME: 'core-v1.20.3' }), /unreconciled PRs/);
  assert.ok(!f.requests.some(r => r.method !== 'GET'));
});
test('manual dry run resolves scoped versions/notes with no API writes or tag requirement', async t => {
  const f = await fixture(t); f.feature(['product:metadata']); await f.run('update-changelogs.mjs'); f.requests.length = 0;
  await f.run('prepare-release.mjs', [], { GITHUB_EVENT_NAME: 'workflow_dispatch', PRODUCT: 'metadata', VERSION: '1.2.1', DRY_RUN: 'true' });
  assert.ok(!f.requests.some(r => r.method !== 'GET'));
  const props = readFileSync(join(f.work, '.release/version.props'), 'utf8');
  assert.match(props, /<MetadataReleaseVersion>1\.2\.1<\/MetadataReleaseVersion>/); assert.ok(!props.includes('CoreReleaseVersion'));
  const snap = JSON.parse(readFileSync(join(f.work, '.release/release-snapshot.json'), 'utf8')); assert.deepEqual(snap.entryIds, [1]);
  assert.equal(f.readState().releases.length, 0);
});
test('manual publication requires the existing matching tag', async t => {
  const f = await fixture(t); f.feature(); await f.run('update-changelogs.mjs');
  await assert.rejects(f.run('prepare-release.mjs', [], { GITHUB_EVENT_NAME: 'workflow_dispatch', PRODUCT: 'core', VERSION: '1.20.3', DRY_RUN: 'false' }));
});
test('PR check publishes failure/success on the current PR head rather than base commit', async t => {
  const f = await fixture(t), pr = f.feature(); const event = join(f.root, 'event.json');
  writeFileSync(event, JSON.stringify({ pull_request: { number: pr.number, head: { sha: 'stale' } } }));
  await f.run('check-pr.mjs', ['product'], { GITHUB_EVENT_PATH: event });
  let check = f.requests.filter(r => r.path === '/check-runs').at(-1).body;
  assert.equal(check.head_sha, pr.head.sha); assert.equal(check.conclusion, 'success'); assert.equal(check.name, 'PR product labels');
  pr.labels = [];
  await assert.rejects(f.run('check-pr.mjs', ['product'], { GITHUB_EVENT_PATH: event }));
  check = f.requests.filter(r => r.path === '/check-runs').at(-1).body;
  assert.equal(check.head_sha, pr.head.sha); assert.equal(check.conclusion, 'failure');
});
test('successful release finalization preserves later core and pending metadata changes', async t => {
  const f = await fixture(t); f.feature(['product:core', 'product:metadata']); await f.run('update-changelogs.mjs');
  const sourceSha = g(f.work, 'rev-parse', 'HEAD'), s = f.readState(), snap = makeSnapshot(s, 'core', '1.20.3', sourceSha, 'owner/repo', '2026-11-05');
  snap.packages = ['nupkg', 'snupkg'].map(ext => ({ name: `XrmMockup365.1.20.3.${ext}`, sha512: hash(Buffer.from(ext)), contentHash: hash(Buffer.from(ext)) }));
  g(f.work, 'tag', snap.tag); g(f.work, 'push', 'origin', '--tags');
  f.releases.push({ id: 1, tag_name: snap.tag, draft: false, body: releaseMarker });
  f.assets.set(1, { id: 1, name: 'release-snapshot.json', releaseId: 1, bytes: Buffer.from(JSON.stringify(snap)) });
  // Successful snapshots require a publication receipt, not just a manually published draft.
  f.assets.set(2, { id: 2, name: 'nuget-publication.json', releaseId: 1, bytes: Buffer.from(JSON.stringify({ tag: snap.tag, snapshotHash: hash(Buffer.from(JSON.stringify(snap))) })) });
  f.feature(['product:core'], 'Add: Newer feature');
  await f.run('update-changelogs.mjs');
  const core = readFileSync(join(f.work, products.core.changelog), 'utf8'), metadata = readFileSync(join(f.work, products.metadata.changelog), 'utf8');
  assert.match(core, /### Unreleased\n\* Add: Newer feature/); assert.match(core, /### 1\.20\.3 - 2026-11-05\n\* Fix: A feature fix/);
  assert.match(metadata, /### Unreleased\n\* Fix: A feature fix/); assert.equal(f.readState().releases.length, 1);
  const commit = g(f.work, 'rev-parse', 'HEAD'); await f.run('update-changelogs.mjs'); assert.equal(g(f.work, 'rev-parse', 'HEAD'), commit);
});
test('manifest refuses extra or changed artifacts', async t => {
  const f = await fixture(t); f.feature(); await f.run('update-changelogs.mjs');
  const snap = makeSnapshot(f.readState(), 'core', '1.20.3', g(f.work, 'rev-parse', 'HEAD'), 'owner/repo', '2026-11-05');
  write(f.work, '.release/release-snapshot.json', snap);
  const hashes = {};
  for (const ext of ['nupkg', 'snupkg']) { const name = `XrmMockup365.1.20.3.${ext}`; write(f.work, `.release/packages/${name}`, ext); hashes[name] = hash(Buffer.from(ext)); }
  write(f.work, '.release/package-content.json', hashes); await f.run('package-manifest.mjs');
  write(f.work, '.release/packages/XrmMockup365.1.20.3.nupkg', 'changed');
  await assert.rejects(f.run('package-manifest.mjs'), /Frozen package bytes/);
  write(f.work, '.release/packages/unrelated.nupkg', 'other'); await assert.rejects(f.run('package-manifest.mjs'), /Only the selected/);
});
test('draft assets cannot be clobbered and GitHub finalization requires a NuGet receipt', async t => {
  const f = await fixture(t); f.feature(); await f.run('update-changelogs.mjs');
  const snap = makeSnapshot(f.readState(), 'core', '1.20.3', g(f.work, 'rev-parse', 'HEAD'), 'owner/repo', '2026-11-05');
  g(f.work, 'tag', snap.tag); g(f.work, 'push', 'origin', '--tags');
  snap.packages = ['nupkg', 'snupkg'].map(ext => { const name = `XrmMockup365.1.20.3.${ext}`; write(f.work, `.release/packages/${name}`, ext); return { name, sha512: hash(Buffer.from(ext)), contentHash: hash(Buffer.from(ext)) }; });
  write(f.work, '.release/release-snapshot.json', snap);
  await f.run('publish-release.mjs', ['draft']);
  assert.equal(f.releases[0].draft, true); assert.equal(f.assets.size, 3);
  await f.run('publish-release.mjs', ['draft']); assert.equal(f.assets.size, 3);
  await assert.rejects(f.run('publish-release.mjs', ['complete']), /publication must succeed/);
  await f.run('publish-release.mjs', ['record']); await f.run('publish-release.mjs', ['complete']);
  assert.equal(f.releases[0].draft, false);
  await f.run('publish-release.mjs', ['notify']); assert.ok(f.requests.some(r => r.path.endsWith('/dispatches')));
  f.assets.get(1).bytes = Buffer.from('different asset');
  await assert.rejects(f.run('publish-release.mjs', ['draft']), /Existing release asset differs/);
});
test('retry preparation recovers the original draft bytes rather than rebuilding mutable artifacts', async t => {
  const f = await fixture(t); f.feature(); await f.run('update-changelogs.mjs');
  const sourceSha = g(f.work, 'rev-parse', 'HEAD');
  g(f.work, 'tag', 'core-v1.20.3'); g(f.work, 'push', 'origin', '--tags');
  const env = { GITHUB_EVENT_NAME: 'push', GITHUB_REF_NAME: 'core-v1.20.3' };
  await f.run('prepare-release.mjs', [], env);
  const snapPath = join(f.work, '.release/release-snapshot.json');
  const snap = JSON.parse(readFileSync(snapPath, 'utf8'));
  snap.packages = ['nupkg', 'snupkg'].map(ext => {
    const name = `XrmMockup365.1.20.3.${ext}`; write(f.work, `.release/packages/${name}`, ext);
    return { name, sha512: hash(Buffer.from(ext)), contentHash: hash(Buffer.from(ext)) };
  });
  writeFileSync(snapPath, JSON.stringify(snap, null, 2) + '\n');
  await f.run('publish-release.mjs', ['draft']);
  write(f.work, '.release/packages/XrmMockup365.1.20.3.nupkg', 'local mutated build');
  // Restore the source-tree changelog, as a fresh checkout would before the retry.
  g(f.work, 'checkout', '--', products.core.changelog);
  f.requests.length = 0;
  await f.run('prepare-release.mjs', [], env);
  assert.equal(readFileSync(join(f.work, '.release/packages/XrmMockup365.1.20.3.nupkg'), 'utf8'), 'nupkg');
  assert.equal(JSON.parse(readFileSync(snapPath, 'utf8')).sourceSha, sourceSha);
  assert.match(readFileSync(f.env.GITHUB_OUTPUT, 'utf8'), /reuse=true/);
  assert.ok(!f.requests.some(r => r.method !== 'GET'));
});

async function frozenDraft(f, version = '1.20.3', product = 'core') {
  const sourceSha = g(f.work, 'rev-parse', 'HEAD'), tag = `${product}-v${version}`;
  g(f.work, 'tag', tag); g(f.work, 'push', 'origin', '--tags');
  await f.run('prepare-release.mjs', [], { GITHUB_EVENT_NAME: 'push', GITHUB_REF_NAME: tag });
  const snapshot = JSON.parse(readFileSync(join(f.work, '.release/release-snapshot.json'), 'utf8'));
  snapshot.packages = ['nupkg', 'snupkg'].map(ext => {
    const name = `${products[product].packageId}.${version}.${ext}`;
    write(f.work, `.release/packages/${name}`, ext);
    return { name, sha512: hash(Buffer.from(ext)), contentHash: hash(Buffer.from(ext)) };
  });
  write(f.work, '.release/release-snapshot.json', snapshot);
  await f.run('publish-release.mjs', ['draft']);
  g(f.work, 'checkout', '--', ...Object.values(products).map(p => p.changelog));
  return snapshot;
}
function successfulRelease(f, snapshot) {
  const id = f.releases.length + 1;
  f.releases.push({ id, tag_name: snapshot.tag, draft: false, body: releaseMarker });
  const bytes = Buffer.from(JSON.stringify(snapshot, null, 2) + '\n');
  let assetId = f.assets.size + 1;
  f.assets.set(assetId, { id: assetId, name: 'release-snapshot.json', releaseId: id, bytes });
  assetId++;
  f.assets.set(assetId, { id: assetId, name: 'nuget-publication.json', releaseId: id, bytes: Buffer.from(JSON.stringify({ tag: snapshot.tag, snapshotHash: hash(bytes) })) });
  g(f.work, 'tag', snapshot.tag, snapshot.sourceSha); g(f.work, 'push', 'origin', '--tags');
}
function restoreFrozenSource(f, snapshot) {
  g(f.work, 'checkout', '--force', '--detach', snapshot.sourceSha);
  write(f.work, '.release/release-snapshot.json', snapshot);
  for (const pkg of snapshot.packages) write(f.work, `.release/packages/${pkg.name}`, pkg.name.endsWith('.snupkg') ? 'snupkg' : 'nupkg');
}

test('trusted preparation rejects an unmerged source without executing its replacement script', async t => {
  const f = await fixture(t); f.feature(); await f.run('update-changelogs.mjs');
  g(f.work, 'checkout', '-b', 'unmerged');
  write(f.work, 'scripts/release/prepare-release.mjs', "import fs from 'node:fs'; fs.writeFileSync('untrusted-executed', 'bad');\n");
  g(f.work, 'add', '.'); g(f.work, 'commit', '-m', 'Replace privileged preparation'); g(f.work, 'push', 'origin', 'HEAD:refs/heads/unmerged');
  f.requests.length = 0;
  await assert.rejects(f.run('prepare-release.mjs', [], { GITHUB_EVENT_NAME: 'workflow_dispatch', PRODUCT: 'core', VERSION: '1.20.3', DRY_RUN: 'true' }), /master ancestry/);
  assert.equal(existsSync(join(f.work, 'untrusted-executed')), false);
  assert.equal(f.requests.length, 0, 'Reject unmerged sources before any API or artifact operations');
});

test('checkpoint reconciliation makes no PR API requests on self-pushes or unchanged previews', async t => {
  const f = await fixture(t), first = f.feature(); await f.run('update-changelogs.mjs');
  assert.equal(f.readState().checkpointSha, first.merge_commit_sha);
  const checkpointCommit = g(f.work, 'rev-parse', 'HEAD');
  f.requests.length = 0; await f.run('update-changelogs.mjs');
  assert.equal(g(f.work, 'rev-parse', 'HEAD'), checkpointCommit);
  assert.ok(!f.requests.some(r => r.path.startsWith('/commits/') || r.path.startsWith('/pulls/')));
  const next = f.feature(['product:metadata'], 'Add: Metadata addition');
  f.requests.length = 0; await f.run('update-changelogs.mjs');
  assert.deepEqual(f.requests.filter(r => r.path.startsWith('/commits/')).map(r => r.path), [`/commits/${next.merge_commit_sha}/pulls`]);
  assert.equal(f.readState().checkpointSha, next.merge_commit_sha);
  f.requests.length = 0;
  await f.run('prepare-release.mjs', [], { GITHUB_EVENT_NAME: 'workflow_dispatch', PRODUCT: 'core', VERSION: '1.20.3', DRY_RUN: 'true' });
  assert.ok(!f.requests.some(r => r.path.startsWith('/commits/') || r.path.startsWith('/pulls/')));
});

test('checkpoint optimization still detects a new unclassified direct commit', async t => {
  const f = await fixture(t); f.feature(); await f.run('update-changelogs.mjs');
  g(f.work, 'config', 'user.name', 'maintainer'); g(f.work, 'config', 'user.email', 'maintainer@example.test');
  write(f.work, 'direct.txt', 'unclassified'); g(f.work, 'add', '.'); g(f.work, 'commit', '-m', 'Direct update');
  const directSha = g(f.work, 'rev-parse', 'HEAD'); g(f.work, 'push', 'origin', 'HEAD:master');
  f.requests.length = 0;
  await assert.rejects(f.run('update-changelogs.mjs'), /no classified merged master PR/);
  assert.deepEqual(f.requests.filter(r => r.path.startsWith('/commits/')).map(r => r.path), [`/commits/${directSha}/pulls`]);
});

test('a manually advanced checkpoint cannot hide an earlier unclassified direct commit', async t => {
  const f = await fixture(t); f.feature(); await f.run('update-changelogs.mjs');
  g(f.work, 'config', 'user.name', 'maintainer'); g(f.work, 'config', 'user.email', 'maintainer@example.test');
  write(f.work, 'hidden-direct.txt', 'must not be skipped'); g(f.work, 'add', '.'); g(f.work, 'commit', '-m', 'Unclassified direct commit');
  const directSha = g(f.work, 'rev-parse', 'HEAD'); g(f.work, 'push', 'origin', 'HEAD:master');
  const forged = f.readState(); forged.checkpointSha = directSha; write(f.work, '.github/release-state.json', forged);
  f.feature(['product:none'], 'Chore: Manually edit the checkpoint'); f.requests.length = 0;
  await assert.rejects(f.run('update-changelogs.mjs'), /no classified merged master PR/);
  assert.ok(f.requests.some(r => r.path === `/commits/${directSha}/pulls`));
});

test('a checkpoint outside source ancestry fails before PR API access', async t => {
  const f = await fixture(t); f.feature(); await f.run('update-changelogs.mjs');
  const master = g(f.work, 'rev-parse', 'HEAD'); g(f.work, 'checkout', '-b', 'checkpoint-side');
  write(f.work, 'side.txt', 'side'); g(f.work, 'add', '.'); g(f.work, 'commit', '-m', 'Side commit');
  const side = g(f.work, 'rev-parse', 'HEAD'); g(f.work, 'checkout', '--detach', master);
  const invalid = f.readState(); invalid.checkpointSha = side; write(f.work, '.github/release-state.json', invalid);
  f.requests.length = 0;
  await assert.rejects(f.run('prepare-release.mjs', [], { GITHUB_EVENT_NAME: 'workflow_dispatch', PRODUCT: 'core', VERSION: '1.20.3', DRY_RUN: 'true' }), /checkpoint.*ancestry/);
  assert.equal(f.requests.length, 0);
});

test('draft-only paginated lookup works across separate draft, probe, and preparation invocations', async t => {
  const f = await fixture(t); f.feature(); await f.run('update-changelogs.mjs');
  for (let i = 0; i < 100; i++) f.releases.push({ id: i + 1, tag_name: `unrelated-${i}`, draft: true });
  const frozen = await frozenDraft(f);
  const mock = join(f.root, 'mock-nuget.mjs');
  writeFileSync(mock, `const original = globalThis.fetch; globalThis.fetch = (url, options) => String(url).startsWith('https://api.nuget.org/') ? Promise.resolve(new Response(null, { status: 404 })) : original(url, options);`);
  await f.run('publish-release.mjs', ['probe'], { NODE_OPTIONS: `--import=${pathToFileURL(mock).href}` });
  assert.match(readFileSync(f.env.GITHUB_OUTPUT, 'utf8'), /publication_complete=false/);
  await f.run('prepare-release.mjs', [], { GITHUB_EVENT_NAME: 'push', GITHUB_REF_NAME: frozen.tag });
  assert.match(readFileSync(f.env.GITHUB_OUTPUT, 'utf8'), /reuse=true/);
  assert.ok(!f.requests.some(r => r.path.startsWith('/releases/tags/')));
  assert.equal(f.releases.filter(r => r.tag_name === frozen.tag).length, 1);
});

for (const scenario of ['newer version', 'already consumed PRs', 'newer source']) {
  test(`obsolete drafts are rejected in preparation and publish-only retries: ${scenario}`, async t => {
    const f = await fixture(t); f.feature(); await f.run('update-changelogs.mjs');
    const frozen = await frozenDraft(f, scenario === 'newer version' ? '1.20.3' : '1.20.5');
    if (scenario !== 'already consumed PRs') {
      f.feature(['product:core'], 'Add: Later core work'); await f.run('update-changelogs.mjs');
    }
    const newer = makeSnapshot(f.readState(), 'core', '1.20.4', g(f.work, 'rev-parse', 'HEAD'), 'owner/repo', '2026-11-05');
    newer.packages = ['nupkg', 'snupkg'].map(ext => ({ name: `XrmMockup365.1.20.4.${ext}`, sha512: hash(Buffer.from(ext)), contentHash: hash(Buffer.from(ext)) }));
    successfulRelease(f, newer); restoreFrozenSource(f, frozen); f.requests.length = 0;
    await assert.rejects(f.run('prepare-release.mjs', [], { GITHUB_EVENT_NAME: 'push', GITHUB_REF_NAME: frozen.tag }), /Obsolete draft/);
    for (const phase of ['draft', 'probe', 'record', 'complete', 'notify']) {
      await assert.rejects(f.run('publish-release.mjs', [phase]), /Obsolete draft/);
    }
    assert.ok(f.requests.every(r => r.method === 'GET'), 'Reject obsolete snapshots before upload, publication or dispatch side effects');
  });
}

test('completed release recovery is still permitted after a newer same-product release', async t => {
  const f = await fixture(t); f.feature(); await f.run('update-changelogs.mjs'); const completed = await frozenDraft(f);
  await f.run('publish-release.mjs', ['record']); await f.run('publish-release.mjs', ['complete']);
  f.feature(['product:core'], 'Add: Next release work'); await f.run('update-changelogs.mjs');
  const newer = makeSnapshot(f.readState(), 'core', '1.20.4', g(f.work, 'rev-parse', 'HEAD'), 'owner/repo', '2026-11-05');
  newer.packages = ['nupkg', 'snupkg'].map(ext => ({ name: `XrmMockup365.1.20.4.${ext}`, sha512: hash(Buffer.from(ext)), contentHash: hash(Buffer.from(ext)) }));
  successfulRelease(f, newer); restoreFrozenSource(f, completed); f.requests.length = 0;
  await f.run('prepare-release.mjs', [], { GITHUB_EVENT_NAME: 'push', GITHUB_REF_NAME: completed.tag });
  await f.run('publish-release.mjs', ['complete']); await f.run('publish-release.mjs', ['notify']);
  assert.equal(f.releases.find(r => r.tag_name === completed.tag).draft, false);
  assert.ok(!f.requests.some(r => r.method === 'PATCH'));
});

test('a later metadata release does not invalidate an independent core draft', async t => {
  const f = await fixture(t); f.feature(['product:core', 'product:metadata']); await f.run('update-changelogs.mjs');
  const frozen = await frozenDraft(f); f.feature(['product:metadata'], 'Add: Metadata-only work'); await f.run('update-changelogs.mjs');
  const metadata = makeSnapshot(f.readState(), 'metadata', '1.2.1', g(f.work, 'rev-parse', 'HEAD'), 'owner/repo', '2026-11-05');
  metadata.packages = ['nupkg', 'snupkg'].map(ext => ({ name: `XrmMockup.MetadataGenerator.1.2.1.${ext}`, sha512: hash(Buffer.from(ext)), contentHash: hash(Buffer.from(ext)) }));
  successfulRelease(f, metadata); restoreFrozenSource(f, frozen);
  await f.run('prepare-release.mjs', [], { GITHUB_EVENT_NAME: 'push', GITHUB_REF_NAME: frozen.tag });
  await f.run('publish-release.mjs', ['draft']);
});
test('a writer-looking author does not cause a human-committed ledger PR to be skipped', async t => {
  const f = await fixture(t); f.feature(); await f.run('update-changelogs.mjs');
  g(f.work, 'config', 'user.name', 'maintainer'); g(f.work, 'config', 'user.email', 'maintainer@example.test');
  const edited = f.readState(); edited.manualAudit = true; write(f.work, '.github/release-state.json', edited);
  g(f.work, 'add', '.'); g(f.work, 'commit', '--author=release-changelog-bot <release-changelog-bot@users.noreply.github.com>', '-m', 'Chore: Review ledger metadata');
  const sha = g(f.work, 'rev-parse', 'HEAD'), number = f.pullRequests.length + 1;
  f.pullRequests.push({ number, title: 'Chore: Review ledger metadata', labels: [{ name: 'product:none' }], merged: true, merged_at: '2026-11-05T11:00:00Z', merge_commit_sha: sha, base: { ref: 'master' }, head: { sha } });
  g(f.work, 'push', 'origin', 'HEAD:master'); f.requests.length = 0;
  await f.run('update-changelogs.mjs');
  assert.ok(f.requests.some(r => r.path === `/commits/${sha}/pulls`));
  assert.ok(f.readState().entries.some(entry => entry.number === number));
});

test('completed-release recovery refuses a changed frozen snapshot before any side effects', async t => {
  const f = await fixture(t); f.feature(); await f.run('update-changelogs.mjs'); const frozen = await frozenDraft(f);
  await f.run('publish-release.mjs', ['record']); await f.run('publish-release.mjs', ['complete']);
  write(f.work, '.release/release-snapshot.json', { ...frozen, date: '2026-11-06' });
  f.requests.length = 0;
  await assert.rejects(f.run('publish-release.mjs', ['complete']), /Completed release differs/);
  assert.ok(f.requests.every(r => r.method === 'GET'));
});
test('a reachable checkpoint on a merged side branch is not accepted as a first-parent boundary', async t => {
  const f = await fixture(t); f.feature(); await f.run('update-changelogs.mjs');
  g(f.work, 'config', 'user.name', 'maintainer'); g(f.work, 'config', 'user.email', 'maintainer@example.test');
  g(f.work, 'checkout', '-b', 'side-boundary'); write(f.work, 'side-history.txt', 'side commit');
  g(f.work, 'add', '.'); g(f.work, 'commit', '-m', 'Side history'); const side = g(f.work, 'rev-parse', 'HEAD');
  g(f.work, 'checkout', '-B', 'master', 'origin/master'); g(f.work, 'merge', '--no-ff', 'side-boundary', '-m', 'Merge side history');
  g(f.work, 'push', 'origin', 'master');
  const edited = f.readState(); edited.checkpointSha = side; write(f.work, '.github/release-state.json', edited);
  f.feature(['product:none'], 'Chore: Review side boundary'); f.requests.length = 0;
  await assert.rejects(f.run('prepare-release.mjs', [], { GITHUB_EVENT_NAME: 'workflow_dispatch', PRODUCT: 'core', VERSION: '1.20.3', DRY_RUN: 'true' }), /checkpoint.*first-parent ancestry/);
  assert.equal(f.requests.length, 0);
});
