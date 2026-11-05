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
test('a merge group validates its queue members, not an unqueued stacked PR sharing commits', async () => {
  const queued = { number: 1, base: { ref: 'master' }, head: { sha: 'pr-a' }, labels: [{ name: 'product:core' }], title: 'Fix: Valid queued work' };
  const calls = [];
  const api = {
    list: async path => { assert.ok(path.startsWith('/compare/')); return [{ sha: 'shared-a' }, { sha: 'group-a' }]; },
    mergeQueueEntries: async () => [{ headCommit: { oid: 'group-a' }, pullRequest: { number: 1, headRefOid: 'pr-a' } }],
    repo: async path => { calls.push(path); assert.equal(path, '/pulls/1'); return queued; },
  };
  assert.deepEqual(await validatePulls(api, { merge_group: { base_sha: 'base', head_sha: 'group-a' } }), [queued]);
  assert.deepEqual(calls, ['/pulls/1']); // PR B shares shared-a but is not in the queue at all.
});
test('multi-PR groups include all contained queue build heads, not later queued entries or their base', async () => {
  const prs = [1, 2, 3].map(number => ({ number, base: { ref: 'master' }, head: { sha: `pr-${number}` } }));
  const api = {
    list: async () => [{ sha: 'group-1' }, { sha: 'group-2' }],
    mergeQueueEntries: async () => prs.map(pr => ({ headCommit: { oid: `group-${pr.number}` }, pullRequest: { number: pr.number, headRefOid: pr.head.sha } })),
    repo: async path => prs.find(pr => path === `/pulls/${pr.number}`),
  };
  assert.deepEqual(await validatePulls(api, { merge_group: { base_sha: 'base', head_sha: 'group-2' } }), prs.slice(0, 2));
  api.list = async () => [{ sha: 'group-2' }];
  assert.deepEqual(await validatePulls(api, { merge_group: { base_sha: 'group-1', head_sha: 'group-2' } }), [prs[1]]);
});
test('stale merge-group membership or a changed PR head fails instead of guessing members', async () => {
  const api = { list: async () => [{ sha: 'group-a' }], mergeQueueEntries: async () => [] };
  await assert.rejects(validatePulls(api, { merge_group: { base_sha: 'base', head_sha: 'group-a' } }), /Cannot identify/);
  api.mergeQueueEntries = async () => [{ headCommit: { oid: 'group-a' }, pullRequest: { number: 1, headRefOid: 'old-head' } }];
  api.repo = async () => ({ number: 1, base: { ref: 'master' }, head: { sha: 'new-head' } });
  await assert.rejects(validatePulls(api, { merge_group: { base_sha: 'base', head_sha: 'group-a' } }), /changed while resolving/);
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
