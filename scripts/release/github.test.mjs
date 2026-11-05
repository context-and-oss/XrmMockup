import test from 'node:test';
import assert from 'node:assert/strict';
import { GitHub, validatePulls } from './github.mjs';

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
      if (path.startsWith('/compare/')) { return [{ sha: 'a' }, { sha: 'b' }]; }
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
