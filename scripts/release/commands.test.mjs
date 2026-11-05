import test from 'node:test';
import assert from 'node:assert/strict';
import { execFile, execFileSync } from 'node:child_process';
import { promisify } from 'node:util';
import { mkdtempSync, mkdirSync, writeFileSync, readFileSync, rmSync } from 'node:fs';
import { tmpdir } from 'node:os';
import { join, resolve } from 'node:path';
import { fileURLToPath } from 'node:url';
import { createServer } from 'node:http';
import { products, renderChangelog, releaseSection, changelogBranch } from './model.mjs';
const execute = promisify(execFile), scriptDir = fileURLToPath(new URL('.', import.meta.url));
const git = (cwd, ...args) => execFileSync('git', args, { cwd, encoding: 'utf8', stdio: 'pipe' }).trim();
const write = (cwd, path, data) => { mkdirSync(resolve(cwd, path, '..'), { recursive: true }); writeFileSync(join(cwd, path), data); };
async function fixture(t) {
  const root = mkdtempSync(join(tmpdir(), 'xrmmockup-release-')), work = join(root, 'work'), remote = join(root, 'origin.git');
  mkdirSync(work); git(work, 'init', '-b', 'master'); git(work, 'config', 'user.name', 'maintainer'); git(work, 'config', 'user.email', 'maintainer@example.test');
  write(work, '.gitignore', '.release/\n');
  const releases = ['core', 'metadata'].map(product => ({ id: product === 'core' ? 1 : 2, tag_name: `${product}-v${product === 'core' ? '1.20.2' : '1.2.0'}`, draft: false,
    body: releaseSection(product === 'core' ? '1.20.2' : '1.2.0', '2026-11-05', '* Existing history'), published_at: '2026-11-05T00:00:00Z' }));
  for (const [product, config] of Object.entries(products)) {
    const version = product === 'core' ? '1.20.2' : '1.2.0';
    write(work, config.changelog, renderChangelog(`### ${version} - 2026-11-05\n* Existing history\n`, [], product));
  }
  git(work, 'add', '.'); git(work, 'commit', '-m', 'Baseline');
  for (const release of releases) git(work, 'tag', release.tag_name);
  git(root, 'init', '--bare', remote); git(work, 'remote', 'add', 'origin', remote); git(work, 'push', '-u', 'origin', 'master', '--tags');
  const prs = [], botPrs = [], requests = [], assets = [];
  const ref = name => { try { return git(remote, 'rev-parse', '--verify', `refs/heads/${name}`); } catch { return null; } };
  const server = createServer(async (req, res) => {
    const url = new URL(req.url, 'http://localhost'), path = url.pathname.replace('/repos/owner/repo', '');
    const chunks = []; for await (const chunk of req) chunks.push(chunk);
    const bytes = Buffer.concat(chunks), body = bytes.length && req.headers['content-type']?.includes('json') ? JSON.parse(bytes) : undefined;
    requests.push({ method: req.method, path, body });
    const json = value => { res.setHeader('content-type', 'application/json'); res.end(JSON.stringify(value)); };
    const fail = () => { res.statusCode = 404; res.end('Not found'); };
    const sha = /^\/commits\/([^/]+)\/pulls$/.exec(path)?.[1];
    if (sha) return json([...prs, ...botPrs].filter(pr => pr.merge_commit_sha === sha));
    if (path === '/releases' && req.method === 'GET') return json(releases);
    if (path === '/releases' && req.method === 'POST') { const release = { id: releases.length + 1, published_at: '2026-11-05T12:00:00Z', ...body }; releases.push(release); return json(release); }
    const upload = /^\/upload\/(\d+)$/.exec(path)?.[1];
    if (upload) { assets.push({ releaseId: Number(upload), name: url.searchParams.get('name'), bytes }); return json({}); }
    if (path === `/git/ref/heads/${changelogBranch}`) return ref(changelogBranch) ? json({ object: { sha: ref(changelogBranch) } }) : fail();
    if (path === '/pulls' && req.method === 'GET') return json(botPrs.filter(pr => pr.state === 'open'));
    if (path === '/pulls' && req.method === 'POST') {
      const pr = { number: 1001 + botPrs.length, state: 'open', merged_at: null, ...body, user: { login: 'github-actions[bot]' }, labels: [],
        head: { ref: body.head, sha: ref(body.head), repo: { full_name: 'owner/repo' } }, base: { ref: body.base, repo: { full_name: 'owner/repo' } } };
      botPrs.push(pr); return json(pr);
    }
    const number = /^\/pulls\/(\d+)$/.exec(path)?.[1], pr = [...prs, ...botPrs].find(pr => pr.number === Number(number));
    if (number && req.method === 'GET') return pr ? json(pr) : fail();
    if (number && req.method === 'PATCH') { Object.assign(pr, body); pr.head.sha = ref(changelogBranch); return json(pr); }
    const issue = /^\/issues\/(\d+)\/labels$/.exec(path)?.[1];
    if (issue) { const pr = botPrs.find(pr => pr.number === Number(issue)); pr.labels = body.labels.map(name => ({ name })); return json(pr.labels); }
    if (path === '/check-runs') return json({ id: 1 });
    if (path.endsWith('/dispatches')) { res.statusCode = 204; return res.end(); }
    return fail();
  });
  await new Promise(resolve => server.listen(0, '127.0.0.1', resolve));
  t.after(async () => { await new Promise(resolve => server.close(resolve)); rmSync(root, { recursive: true, force: true }); });
  const env = { ...process.env, GITHUB_REPOSITORY: 'owner/repo', GH_TOKEN: 'test', TEST_GH_API_URL: `http://127.0.0.1:${server.address().port}`,
    NODE_OPTIONS: `--import=${new URL('./test-utils/inject-gh.mjs', import.meta.url).href}`, GITHUB_OUTPUT: join(root, 'outputs'), GITHUB_STEP_SUMMARY: join(root, 'summary') };
  const run = (script, args = [], extra = {}) => execute(process.execPath, [join(scriptDir, script), ...args], { cwd: work, env: { ...env, ...extra } });
  function feature(labels, title) {
    git(work, 'fetch', 'origin', 'master'); git(work, 'checkout', '--force', '-B', 'master', 'origin/master');
    const number = prs.length + 1; write(work, `feature-${number}.txt`, title); git(work, 'add', '.'); git(work, 'commit', '-m', title);
    const sha = git(work, 'rev-parse', 'HEAD'); const pr = { number, title, merged: true, merged_at: '2026-11-05T01:00:00Z', merge_commit_sha: sha,
      labels: labels.map(name => ({ name })), head: { sha }, base: { ref: 'master' } };
    prs.push(pr); git(work, 'push', 'origin', 'master'); return pr;
  }
  function mergeNotes(strategy = 'merge') {
    const pr = botPrs.find(pr => pr.state === 'open'); if (!pr) return;
    git(work, 'fetch', 'origin', 'master', `+refs/heads/${changelogBranch}:refs/remotes/origin/${changelogBranch}`);
    git(work, 'checkout', '--force', '-B', 'master', 'origin/master');
    if (strategy === 'squash') { git(work, 'merge', '--squash', `origin/${changelogBranch}`); git(work, 'commit', '-m', pr.title); }
    else if (strategy === 'rebase') git(work, 'cherry-pick', `origin/${changelogBranch}`);
    else git(work, 'merge', '--no-ff', `origin/${changelogBranch}`, '-m', pr.title);
    pr.state = 'closed'; pr.merged = true; pr.merged_at = '2026-11-05T02:00:00Z'; pr.merge_commit_sha = git(work, 'rev-parse', 'HEAD');
    git(work, 'push', 'origin', 'master');
  }
  return { root, work, remote, releases, prs, botPrs, assets, requests, run, feature, mergeNotes, env };
}

test('several merges produce one combined notes PR, and its own merge produces no follow-up', async t => {
  const f = await fixture(t); f.feature(['product:core'], 'Fix: Core repair'); await f.run('update-changelogs.mjs');
  f.feature(['product:metadata'], 'Add: Metadata feature'); f.feature(['product:core', 'product:metadata'], 'Change: Shared');
  const master = git(f.remote, 'rev-parse', 'master'); await f.run('update-changelogs.mjs');
  assert.equal(f.botPrs.length, 1); assert.equal(git(f.remote, 'rev-parse', 'master'), master);
  assert.deepEqual(f.botPrs[0].labels.map(label => label.name), ['product:none', 'change:chore']);
  assert.match(git(f.remote, 'show', `${changelogBranch}:${products.core.changelog}`), /Core repair/);
  assert.match(git(f.remote, 'show', `${changelogBranch}:${products.metadata.changelog}`), /Metadata feature/);
  f.mergeNotes(); const merged = git(f.remote, 'rev-parse', 'master'); await f.run('update-changelogs.mjs');
  assert.equal(f.botPrs.length, 1); assert.equal(git(f.remote, 'rev-parse', 'master'), merged);
});
for (const strategy of ['squash', 'rebase']) test(`notes PR supports ${strategy} merging`, async t => {
  const f = await fixture(t); f.feature(['product:core'], 'Fix: Repair'); await f.run('update-changelogs.mjs'); f.mergeNotes(strategy);
  await f.run('update-changelogs.mjs'); assert.equal(f.botPrs.length, 1);
});
for (const product of ['core', 'metadata']) test(`${product} preview builds the selected version without publishing`, async t => {
  const f = await fixture(t); f.feature([products[product].label], 'Fix: Product change'); await f.run('update-changelogs.mjs'); f.mergeNotes();
  f.requests.length = 0;
  await f.run('prepare-release.mjs', [], { GITHUB_EVENT_NAME: 'workflow_dispatch', PRODUCT: product, VERSION: product === 'core' ? '1.20.3' : '1.2.1', DRY_RUN: 'true' });
  const release = JSON.parse(readFileSync(join(f.work, '.release/release.json')));
  assert.equal(release.product, product); assert.match(readFileSync(join(f.work, '.release/version.props'), 'utf8'), new RegExp(products[product].versionProperty));
  assert.ok(f.requests.every(request => request.method === 'GET'));
});
test('unmerged source and pending notes are rejected before release', async t => {
  const f = await fixture(t); f.feature(['product:core'], 'Fix: Repair');
  const preview = { GITHUB_EVENT_NAME: 'workflow_dispatch', PRODUCT: 'core', VERSION: '1.20.3', DRY_RUN: 'true' };
  await assert.rejects(f.run('prepare-release.mjs', [], preview), /Merge the changelog PR/);
  git(f.work, 'checkout', '-b', 'unmerged'); write(f.work, 'unmerged.txt', 'change'); git(f.work, 'add', '.'); git(f.work, 'commit', '-m', 'Unmerged');
  await assert.rejects(f.run('prepare-release.mjs', [], preview), /master ancestry/);
});
for (const product of ['core', 'metadata']) test(`${product} tag creates one product release and advances only its notes`, async t => {
  const f = await fixture(t); f.feature(['product:core', 'product:metadata'], 'Fix: Shared repair'); await f.run('update-changelogs.mjs'); f.mergeNotes();
  const tag = `${product}-v${product === 'core' ? '1.20.3' : '1.2.1'}`;
  git(f.work, 'tag', tag); git(f.work, 'push', 'origin', '--tags');
  await f.run('prepare-release.mjs', [], { GITHUB_EVENT_NAME: 'push', GITHUB_REF_NAME: tag });
  const release = JSON.parse(readFileSync(join(f.work, '.release/release.json')));
  const filenames = ['nupkg', 'snupkg'].map(extension => `${products[product].packageId}.${release.version}.${extension}`);
  for (const filename of filenames) write(f.work, `.release/packages/${filename}`, 'package bytes');
  await f.run('publish-release.mjs', [], { RELEASE_TAG: tag });
  assert.deepEqual(f.assets.map(asset => asset.name).sort(), filenames);
  await f.run('update-changelogs.mjs');
  const published = git(f.remote, 'show', `${changelogBranch}:${products[product].changelog}`);
  const other = product === 'core' ? 'metadata' : 'core';
  assert.match(published, new RegExp(`### ${release.version.replaceAll('.', '\\.')}`));
  assert.match(git(f.remote, 'show', `${changelogBranch}:${products[other].changelog}`), /### Unreleased\n\* Fix: Shared repair/);
});
test('PR metadata checks fail invalid labels against the actual current head', async t => {
  const f = await fixture(t), pr = f.feature([], 'Fix: Missing product'), event = join(f.root, 'event.json');
  writeFileSync(event, JSON.stringify({ pull_request: { number: pr.number } }));
  await assert.rejects(f.run('check-pr.mjs', ['product'], { GITHUB_EVENT_PATH: event }));
  const check = f.requests.find(request => request.path === '/check-runs').body;
  assert.equal(check.conclusion, 'failure'); assert.equal(check.head_sha, pr.head.sha);
});

for (const product of ['core', 'metadata']) test(`${product} preparation ignores a later release boundary for the other product`, async t => {
  const f = await fixture(t);
  f.feature(['product:core', 'product:metadata'], 'Fix: Shared reviewed change');
  await f.run('update-changelogs.mjs'); f.mergeNotes();
  const earlierSource = git(f.work, 'rev-parse', 'HEAD');
  const other = product === 'core' ? 'metadata' : 'core';
  f.feature([products[other].label], 'Add: Later other-product change');
  await f.run('update-changelogs.mjs'); f.mergeNotes();
  const laterSource = git(f.work, 'rev-parse', 'HEAD');
  const otherVersion = other === 'core' ? '1.20.3' : '1.2.1', otherTag = `${other}-v${otherVersion}`;
  git(f.work, 'tag', otherTag); git(f.work, 'push', 'origin', '--tags');
  f.releases.push({ id: 3, tag_name: otherTag, draft: false, body: releaseSection(otherVersion, '2026-11-05', '* Published other-product changes'), published_at: '2026-11-05T12:00:00Z' });
  git(f.work, 'checkout', '--force', '--detach', earlierSource);
  const version = product === 'core' ? '1.20.3' : '1.2.1';
  const preview = { GITHUB_EVENT_NAME: 'workflow_dispatch', PRODUCT: product, VERSION: version, DRY_RUN: 'true' };
  f.requests.length = 0;
  await f.run('prepare-release.mjs', [], preview);
  const release = JSON.parse(readFileSync(join(f.work, '.release/release.json'), 'utf8'));
  assert.equal(release.product, product); assert.equal(release.sourceSha, earlierSource);
  assert.match(readFileSync(join(f.work, '.release/notes.md'), 'utf8'), /Shared reviewed change/);
  assert.ok(!readFileSync(join(f.work, '.release/notes.md'), 'utf8').includes('Later other-product change'));
  assert.ok(f.requests.every(request => request.method === 'GET'));

  // Preparation must still enforce its own product's successful-release boundary.
  const ownTag = `${product}-v${version}`; git(f.work, 'tag', ownTag, laterSource); git(f.work, 'push', 'origin', '--tags');
  f.releases.push({ id: 4, tag_name: ownTag, draft: false, body: releaseSection(version, '2026-11-05', '* Own release'), published_at: '2026-11-05T13:00:00Z' });
  await assert.rejects(f.run('prepare-release.mjs', [], { ...preview, VERSION: product === 'core' ? '1.20.4' : '1.2.2' }), /Source predates a successful product release/);
});

test('released-only PRs with edited invalid titles do not block pending notes, but shared pending PRs still validate', async t => {
  const f = await fixture(t), released = f.feature(['product:core'], 'Fix: Released core repair');
  await f.run('update-changelogs.mjs'); f.mergeNotes();
  const tag = 'core-v1.20.3'; git(f.work, 'tag', tag); git(f.work, 'push', 'origin', '--tags');
  f.releases.push({ id: 3, tag_name: tag, draft: false, body: releaseSection('1.20.3', '2026-11-05', '* Fix: Released core repair (#1)'), published_at: '2026-11-05T12:00:00Z' });
  released.title = 'Edited title without a category';
  f.feature(['product:metadata'], 'Add: Pending metadata capability');
  await f.run('update-changelogs.mjs'); f.mergeNotes();
  const preview = { GITHUB_EVENT_NAME: 'workflow_dispatch', PRODUCT: 'metadata', VERSION: '1.2.1', DRY_RUN: 'true' };
  await f.run('prepare-release.mjs', [], preview);
  assert.match(readFileSync(join(f.work, '.release/notes.md'), 'utf8'), /Pending metadata capability/);
  assert.ok(!readFileSync(join(f.work, '.release/notes.md'), 'utf8').includes('Edited title'));
  git(f.work, 'checkout', '--', products.metadata.changelog); // Preparation changes only the temporary build workspace.
  released.labels.push({ name: 'product:metadata' });
  await assert.rejects(f.run('update-changelogs.mjs'), /category label|start the title/);
  await assert.rejects(f.run('prepare-release.mjs', [], preview), /category label|start the title/);
});

test('a differently cased prerelease cannot pass progression as a new NuGet version', async t => {
  const f = await fixture(t); f.feature(['product:core'], 'Fix: Candidate repair'); await f.run('update-changelogs.mjs'); f.mergeNotes();
  const tag = 'core-v1.20.3-RC.1'; git(f.work, 'tag', tag); git(f.work, 'push', 'origin', '--tags');
  f.releases.push({ id: 3, tag_name: tag, draft: false, body: releaseSection('1.20.3-RC.1', '2026-11-05', '* Fix: Candidate repair'), published_at: '2026-11-05T12:00:00Z' });
  f.requests.length = 0;
  await assert.rejects(f.run('prepare-release.mjs', [], { GITHUB_EVENT_NAME: 'workflow_dispatch', PRODUCT: 'core', VERSION: '1.20.3-rc.1', DRY_RUN: 'true' }), /newer than the last successful/);
  assert.ok(f.requests.every(request => request.method === 'GET'));
});

test('core packaged changelog keeps dollar-sign sequences exactly as the saved release notes', async t => {
  const f = await fixture(t); f.feature(['product:core'], "Fix: Preserve $$, $&, $` and $' in templates");
  await f.run('update-changelogs.mjs'); f.mergeNotes();
  await f.run('prepare-release.mjs', [], { GITHUB_EVENT_NAME: 'workflow_dispatch', PRODUCT: 'core', VERSION: '1.20.3', DRY_RUN: 'true' });
  const notes = readFileSync(join(f.work, '.release/notes.md'), 'utf8').trim();
  const changelog = readFileSync(join(f.work, products.core.changelog), 'utf8');
  assert.match(notes, /Preserve \$\$/); assert.ok(notes.includes("$` and $'"));
  assert.ok(changelog.includes(notes), 'The exact versioned notes must be embedded in the packaged changelog');
});
