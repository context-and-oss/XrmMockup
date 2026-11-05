import test from 'node:test';
import assert from 'node:assert/strict';
import { classify, parseTag, compareVersions, notesFor, renderChangelog, pendingNotes, releaseSection } from './model.mjs';
const pr = (labels, title) => ({ labels: labels.map(name => ({ name })), title });

test('product labels and category labels/title prefixes classify PRs', () => {
  assert.deepEqual(classify(pr(['product:core'], 'Fix: Core repair')), { products: ['core'], category: 'Fix', description: 'Core repair' });
  assert.deepEqual(classify(pr(['product:metadata', 'change:feature'], 'Metadata capability')).products, ['metadata']);
  assert.deepEqual(classify(pr(['product:core', 'product:metadata'], 'Change: Shared behavior')).products, ['core', 'metadata']);
  assert.deepEqual(classify(pr(['product:none'], 'Chore: Repository work')).products, []);
  for (const invalid of [pr([], 'Fix: Missing product'), pr(['product:none', 'product:core'], 'Fix: Conflicting product'),
    pr(['product:core'], 'Missing category'), pr(['product:core', 'change:fix'], 'Add: Conflict'),
    pr(['product:core', 'change:fix', 'change:feature'], 'Fix: Multiple'), pr(['product:none'], 'Chore:')]) assert.throws(() => classify(invalid));
});
test('the automation PR is maintenance, not a product release entry', () => {
  const bot = { ...pr(['product:none', 'change:chore'], 'Chore: Update changelogs'), user: { login: 'github-actions[bot]' },
    head: { ref: 'automation/update-changelogs', repo: { full_name: 'owner/repo' } }, base: { repo: { full_name: 'owner/repo' } } };
  assert.deepEqual(classify(bot).products, []);
  assert.throws(() => classify({ ...bot, labels: ['product:core'] }));
});
test('tags select an independent product/version and reject unsupported forms', () => {
  assert.equal(parseTag('core-v1.20.3').product, 'core');
  assert.equal(parseTag('metadata-v1.3.0-rc.1').prerelease, true);
  for (const invalid of ['v1.20.3', 'other-v1.0.0', 'core-v01.2.3', 'core-v1.2', 'core-v1.2.3+build', 'core-v1.2.3-01', 'core-v1.20.1-v1.2.0']) assert.throws(() => parseTag(invalid));
  assert.ok(compareVersions('1.20.3', '1.20.3-rc.1') > 0);
  assert.ok(compareVersions('1.3.0-rc.10', '1.3.0-rc.2') > 0);
});
test('notes include the correct products once, with stable categories and safe text', () => {
  const entries = [ { number: 1, ...classify(pr(['product:core'], 'Fix: Core <repair>')) },
    { number: 2, ...classify(pr(['product:metadata'], 'Add: Tool feature')) },
    { number: 3, ...classify(pr(['product:core', 'product:metadata'], 'Change: Shared')) },
    { number: 4, ...classify(pr(['product:none'], 'Chore: CI')) } ];
  assert.equal(notesFor(entries, 'core'), '* Fix: Core &lt;repair&gt; (#1)\n* Change: Shared (#3)');
  assert.equal(notesFor(entries, 'metadata'), '* Add: Tool feature (#2)\n* Change: Shared (#3)');
});
test('rendering keeps published history, adds new releases once and replaces pending notes', () => {
  const original = '# Changelog\n\n### 1.20.2 - old\n* Historical note\n';
  const entries = [{ number: 1, ...classify(pr(['product:core'], 'Fix: New note')) }];
  const published = [{ tag_name: 'core-v1.20.3', body: releaseSection('1.20.3', '2026-11-05', '* Fix: Released (#2)'), published_at: '2026-11-05T00:00:00Z' }];
  const rendered = renderChangelog(original, entries, 'core', published);
  assert.equal(pendingNotes(rendered), '* Fix: New note (#1)');
  assert.match(rendered, /Historical note/); assert.match(rendered, /Released \(#2\)/);
  assert.equal(renderChangelog(rendered, entries, 'core', published), rendered);
  const cleared = renderChangelog(rendered, [], 'core', published);
  assert.equal(pendingNotes(cleared), '* No unreleased changes.');
});
