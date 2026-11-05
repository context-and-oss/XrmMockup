import test from 'node:test';
import assert from 'node:assert/strict';
import { GitHub, validatePulls } from './github.mjs';

test('GitHub pagination does not lose records beyond the first page', async () => {
  const urls = [];
  const api = new GitHub({ repository: 'owner/repo', token: 'test', fetcher: async url => {
    urls.push(url); return new Response(JSON.stringify(new URL(url).searchParams.get('page') === '1' ? Array.from({ length: 100 }, (_, i) => ({ id: i })) : [{ id: 100 }]));
  } });
  assert.equal((await api.list('/releases')).length, 101);
  assert.equal(urls.length, 2); assert.ok(urls[1].includes('page=2'));
});
test('GitHub API refuses to send credentials to an untrusted host', async () => {
  const api = new GitHub({ repository: 'owner/repo', token: 'secret', fetcher: () => { throw new Error('must not fetch'); } });
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
    repo: async path => path.startsWith('/compare/') ? { commits: [{ sha: 'a' }, { sha: 'b' }] } : prs.find(p => path === `/pulls/${p.number}`),
    list: async path => path.includes('/a/') ? [prs[0]] : [prs[0], prs[1]],
  };
  assert.deepEqual(await validatePulls(api, { merge_group: { base_sha: 'base', head_sha: 'head' } }), prs);
});
test('unknown merge group fails closed', async () => {
  await assert.rejects(validatePulls({ repo: async () => ({ commits: [] }) }, { merge_group: { base_sha: 'base', head_sha: 'head' } }), /Cannot identify/);
});
test('API errors are reported and optional 404 returns null', async () => {
  const api = new GitHub({ repository: 'owner/repo', token: 'test', fetcher: async () => new Response('not found', { status: 404 }) });
  assert.equal(await api.repo('/missing', { allow404: true }), null);
  await assert.rejects(api.repo('/missing'), /404/);
});
test('release lookup finds a draft beyond the first page without the published-only tag endpoint', async () => {
  const urls = [], draft = { id: 101, tag_name: 'core-v1.20.3', draft: true };
  const api = new GitHub({ repository: 'owner/repo', token: 'test', fetcher: async url => {
    urls.push(url);
    assert.equal(new URL(url).pathname, '/repos/owner/repo/releases');
    return new Response(JSON.stringify(new URL(url).searchParams.get('page') === '1'
      ? Array.from({ length: 100 }, (_, i) => ({ id: i, tag_name: `old-${i}`, draft: false })) : [draft]));
  } });
  assert.deepEqual(await api.releaseByTag(draft.tag_name), draft);
  assert.equal(urls.length, 2);
  assert.equal(new URL(urls[1]).searchParams.get('page'), '2');
});
test('release lookup refuses ambiguous duplicate tags instead of picking an arbitrary draft', async () => {
  const api = new GitHub({ repository: 'owner/repo', token: 'test', fetcher: async () => new Response(JSON.stringify([
    { id: 1, tag_name: 'core-v1.20.3', draft: true }, { id: 2, tag_name: 'core-v1.20.3', draft: true },
  ])) });
  await assert.rejects(api.releaseByTag('core-v1.20.3'), /Multiple releases\/drafts/);
});
