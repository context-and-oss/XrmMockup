import test from 'node:test';
import assert from 'node:assert/strict';
import { GitHub, validatePulls, productReleases } from './github.mjs';
test('metadata checks read current PR labels/title', async () => {
  const current = { number: 12, title: 'Fix: Latest title', labels: ['product:core'] };
  const api = { repo: async () => current };
  assert.deepEqual(await validatePulls(api, { pull_request: { number: 12, labels: [] } }), [current]);
  assert.deepEqual(await validatePulls(api, { inputs: { pr_number: '12' } }), [current]);
});
test('failed commands propagate; optional absent references are not authorization bypasses', async () => {
  const api = new GitHub({ repository: 'owner/repo', runner: async () => { throw Object.assign(new Error('Not found'), { stderr: 'gh: Not Found (HTTP 404)' }); } });
  assert.equal(await api.repo('/git/ref/heads/missing', { allow404: true }), null);
  api.runner = async () => { throw Object.assign(new Error('Denied'), { stderr: 'gh: Denied (HTTP 403)' }); };
  await assert.rejects(api.repo('/git/ref/heads/missing', { allow404: true }), /Denied/);
  await assert.rejects(api.request('https://untrusted.example'), /Untrusted/);
});
test('only successful same-product releases define the next changelog range', () => {
  const releases = [{ tag_name: 'core-v1.20.3', draft: false }, { tag_name: 'core-v1.20.4', draft: true }, { tag_name: 'metadata-v1.2.1', draft: false }];
  assert.deepEqual(productReleases(releases, 'core').map(release => release.tag_name), ['core-v1.20.3']);
});
