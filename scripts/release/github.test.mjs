import test from 'node:test';
import assert from 'node:assert/strict';
import { readFileSync, existsSync } from 'node:fs';
import { GitHub, validatePulls } from './github.mjs';
const bytes = value => Buffer.from(JSON.stringify(value));

test('gh handles pagination and emits JSON records without a hand-rolled page loop', async () => {
  const calls = [];
  const api = new GitHub({ repository: 'owner/repo', token: 'test', runner: async args => {
    calls.push(args); return Buffer.from(Array.from({ length: 101 }, (_, id) => JSON.stringify({ id })).join('\n') + '\n');
  } });
  assert.equal((await api.list('/releases')).length, 101);
  assert.equal(calls.length, 1); assert.ok(calls[0].includes('--paginate'));
  assert.equal(calls[0][calls[0].indexOf('--jq') + 1], '.[] | @json');
});
test('the gh adapter refuses an absolute URL before any credential-bearing command', async () => {
  const api = new GitHub({ repository: 'owner/repo', token: 'secret', runner: () => { throw new Error('must not invoke gh'); } });
  await assert.rejects(api.request('https://attacker.example/assets'), /Untrusted/);
});
test('PR checks fetch current metadata rather than stale event labels', async () => {
  const current = { number: 5, title: 'Fix: Current', labels: [{ name: 'product:core' }], head: { sha: 'a'.repeat(40) } };
  const api = { repo: async path => { assert.equal(path, '/pulls/5'); return current; } };
  assert.deepEqual(await validatePulls(api, { pull_request: { number: 5, labels: [] } }), [current]);
});
test('merge-group checks validate each associated unmerged master PR', async () => {
  const prs = [{ number: 1, base: { ref: 'master' }, merged_at: null }, { number: 2, base: { ref: 'master' }, merged_at: null }];
  const api = {
    repo: async path => prs.find(p => path === `/pulls/${p.number}`),
    list: async (path, selector) => {
      if (path.startsWith('/compare/')) { assert.equal(selector, '.commits[] | @json'); return [{ sha: 'a' }, { sha: 'b' }]; }
      return path.includes('/a/') ? [prs[0]] : [prs[0], prs[1]];
    },
  };
  assert.deepEqual(await validatePulls(api, { merge_group: { base_sha: 'base', head_sha: 'head' } }), prs);
});
test('unknown merge group fails closed', async () => {
  await assert.rejects(validatePulls({ list: async () => [] }, { merge_group: { base_sha: 'base', head_sha: 'head' } }), /Cannot identify/);
});
test('optional 404 is handled but CLI permissions and command errors still fail', async () => {
  const failure = status => Object.assign(new Error(`gh failed: ${status}`), { stderr: `gh: Not Found (HTTP ${status})\n` });
  const api = new GitHub({ repository: 'owner/repo', token: 'test', runner: async () => { throw failure(404); } });
  assert.equal(await api.repo('/missing', { allow404: true }), null);
  await assert.rejects(api.repo('/missing'), /404/);
  api.runner = async () => { throw failure(403); };
  await assert.rejects(api.repo('/missing', { allow404: true }), /403/);
  api.runner = async () => { throw Object.assign(new Error('403 permission failure'), { stderr: 'gh: message mentioning (HTTP 404) but denied (HTTP 403)\n' }); };
  await assert.rejects(api.repo('/missing', { allow404: true }), /403/);
});
test('draft lookup retains authenticated release-list semantics through gh api', async () => {
  const draft = { id: 101, tag_name: 'core-v1.20.3', draft: true };
  const api = new GitHub({ repository: 'owner/repo', token: 'test', runner: async args => {
    assert.match(args[1], /^\/repos\/owner\/repo\/releases\?/);
    assert.ok(args.includes('--paginate'));
    return Buffer.from([...Array.from({ length: 100 }, (_, id) => ({ id, tag_name: `old-${id}` })), draft].map(value => JSON.stringify(value)).join('\n'));
  } });
  assert.deepEqual(await api.releaseByTag(draft.tag_name), draft);
});
test('release lookup refuses ambiguous duplicate tags instead of picking an arbitrary draft', async () => {
  const api = new GitHub({ repository: 'owner/repo', token: 'test', runner: async () => Buffer.from([
    { id: 1, tag_name: 'core-v1.20.3', draft: true }, { id: 2, tag_name: 'core-v1.20.3', draft: true },
  ].map(value => JSON.stringify(value)).join('\n')) });
  await assert.rejects(api.releaseByTag('core-v1.20.3'), /Multiple releases\/drafts/);
});
test('JSON goes through stdin, authentication stays in env, and 204/binary responses are preserved', async () => {
  const calls = [];
  const api = new GitHub({ repository: 'owner/repo', token: 'sensitive-token', host: 'ghe.example', runner: async (args, options) => {
    calls.push({ args, options }); return Buffer.alloc(0);
  } });
  const body = { text: 'Quotes " and newlines\n remain data', number: 12 };
  assert.equal(await api.repo('/check-runs', { method: 'POST', body }), null);
  assert.deepEqual(JSON.parse(calls[0].options.input), body);
  assert.ok(!calls[0].args.includes('sensitive-token'));
  assert.equal(calls[0].options.env.GH_ENTERPRISE_TOKEN, 'sensitive-token');
  assert.equal(calls[0].options.env.GH_HOST, 'ghe.example');
  const binary = Buffer.from([0, 255, 128, 10]); api.runner = async args => { assert.ok(args.includes('Accept:application/octet-stream')); return binary; };
  assert.deepEqual(await api.asset({ id: 1 }), binary);
});
test('PR writes and workflow dispatch use native CLI commands and keep bodies out of argv', async () => {
  const calls = [], pr = { number: 12 };
  const api = new GitHub({ repository: 'owner/repo', token: 'test', runner: async (args, options) => {
    calls.push({ args, options });
    return args[0] === 'pr' ? Buffer.from('https://github.com/owner/repo/pull/12\n') : bytes(pr);
  } });
  const body = 'Multiline\nreview description';
  assert.deepEqual(await api.createPull({ title: 'Chore: Update notes', body, head: 'automation/update-changelogs', base: 'master' }), pr);
  assert.deepEqual(calls[0].args.slice(0, 2), ['pr', 'create']); assert.equal(calls[0].options.input, body);
  await api.editPull({ number: 12, labels: [{ name: 'product:core' }, { name: 'reviewed' }] }, 'Chore: Update notes', body);
  const edit = calls.find(c => c.args[1] === 'edit').args;
  assert.ok(edit.includes('--remove-label')); assert.ok(edit.includes('product:core')); assert.ok(!edit.includes('reviewed'));
  await api.dispatch('ci.yml', 'automation/update-changelogs');
  assert.deepEqual(calls.at(-1).args.slice(0, 3), ['workflow', 'run', 'ci.yml']);
});
test('asset upload uses a temporary file and never enables clobber', async () => {
  let path;
  const api = new GitHub({ repository: 'owner/repo', token: 'test', runner: async args => {
    assert.deepEqual(args.slice(0, 3), ['release', 'upload', 'core-v1.20.3']);
    path = args[3]; assert.deepEqual(readFileSync(path), Buffer.from([0, 255]));
    assert.ok(!args.includes('--clobber')); return Buffer.alloc(0);
  } });
  await api.uploadAsset('core-v1.20.3', 'release-snapshot.json', Buffer.from([0, 255]));
  assert.equal(existsSync(path), false);
  await assert.rejects(api.uploadAsset('core-v1.20.3', '../bad', Buffer.from('x')), /Invalid/);
});
test('release creation verifies the existing tag and publishes with an explicit latest policy', async () => {
  const calls = [];
  const api = new GitHub({ repository: 'owner/repo', token: 'test', runner: async (args, options) => {
    calls.push({ args, options }); return args[0] === 'api' ? Buffer.from(JSON.stringify({ tag_name: 'core-v1.20.3', draft: true }) + '\n') : Buffer.alloc(0);
  } });
  await api.createRelease({ tag: 'core-v1.20.3', title: 'Core 1.20.3', notes: 'Frozen notes', prerelease: false });
  assert.ok(calls[0].args.includes('--verify-tag')); assert.ok(calls[0].args.includes('--draft')); assert.equal(calls[0].options.input, 'Frozen notes');
  await api.publishRelease('core-v1.20.3', false); assert.ok(calls.at(-1).args.includes('--latest=false'));
});
