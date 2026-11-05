import test from 'node:test';
import assert from 'node:assert/strict';
import {
  classifyProducts, classifyCategory, classify, parseTag, parseVersion, compareVersions,
  validateState, makeSnapshot, applyRelease, renderChangelog, pending, notesFor, validateSnapshot, hash,
} from './model.mjs';

const baseline = '1'.repeat(40), source = '2'.repeat(40);
const pr = (labels, title = 'Fix: A useful fix') => ({ labels: labels.map(name => ({ name })), title });
const entry = (number, products, category = 'Fix') => ({ number, products, category, description: `Change ${number}`, mergeSha: source });
const state = () => ({ schemaVersion: 1, baselineSha: baseline, baselineVersions: { core: '1.20.2', metadata: '1.2.0' }, entries: [], releases: [] });
const snapshot = s => makeSnapshot(s, 'core', '1.20.3', source, 'owner/repo', '2026-11-05');

for (const [labels, expected] of [
  [['product:core'], ['core']], [['product:metadata'], ['metadata']],
  [['product:metadata', 'product:core'], ['core', 'metadata']], [['product:none'], []],
]) test(`valid product classification: ${labels}`, () => assert.deepEqual(classifyProducts(pr(labels)), expected));
for (const labels of [[], ['bug'], ['product:other'], ['product:none', 'product:core'], ['product:none', 'product:metadata'], ['product:none', 'product:core', 'product:metadata']]) {
  test(`invalid product classification: ${labels}`, () => assert.throws(() => classifyProducts(pr(labels))));
}
for (const [label, prefix] of [['feature', 'Add'], ['fix', 'Fix'], ['change', 'Change'], ['breaking', 'Breaking'], ['chore', 'Chore']]) {
  test(`category ${label} can be supplied by label or title`, () => {
    assert.deepEqual(classifyCategory(pr([`change:${label}`], 'Useful description')), { category: prefix, description: 'Useful description' });
    assert.deepEqual(classifyCategory(pr([], `${prefix.toUpperCase()}: Useful description`)), { category: prefix, description: 'Useful description' });
    assert.equal(classifyCategory(pr([`change:${label}`], `${prefix}: Useful description`)).category, prefix);
  });
}
for (const [labels, title] of [
  [[], 'Useful description'], [[], 'feature: Useful description'], [[], 'We fix a bug'], [[], 'Fix:'],
  [['change:fix'], ''], [['change:fix'], 'Fix:'], [['change:fix'], 'Fix: \nInjected text'],
  [['change:feature', 'change:fix'], 'Fix: A bug'], [['change:fix'], 'Add: New feature'],
  [[], 'feat(scope): A feature'], [['change:unknown'], 'Useful description'],
]) test(`invalid category: ${labels} / ${JSON.stringify(title)}`, () => assert.throws(() => classifyCategory(pr(labels, title))));
test('maintenance requires a category too', () => {
  assert.deepEqual(classify(pr(['product:none'], 'Chore: Update workflows')), { products: [], category: 'Chore', description: 'Update workflows' });
  assert.throws(() => classify(pr(['product:none'], 'Update workflows')));
});
for (const tag of ['core-v1.20.3', 'metadata-v1.3.0-rc.1', 'core-v0.0.1-beta-1']) test(`valid release tag ${tag}`, () => assert.equal(parseTag(tag).tag, tag));
for (const tag of ['v1.20.3', 'metadata-1.2.0', 'core-v1.20.1-v1.2.0', 'other-v1.0.0', 'core-v01.2.3', 'core-v1.2', 'core-v1.2.3+', 'core-v1.2.3+build', 'core-v1.2.3-01', 'core-v1.2.3-rc..1', 'core-v1.2.3\n', 'core-v2147483648.0.0']) {
  test(`invalid release tag ${JSON.stringify(tag)}`, () => assert.throws(() => parseTag(tag)));
}
test('SemVer comparison orders prereleases and numeric identifiers correctly', () => {
  const ordered = ['1.0.0-alpha', '1.0.0-alpha.1', '1.0.0-alpha.beta', '1.0.0-beta', '1.0.0-beta.2', '1.0.0-beta.11', '1.0.0-rc.1', '1.0.0', '1.0.1'];
  ordered.slice(1).forEach((v, i) => assert.equal(compareVersions(ordered[i], v), -1));
  assert.equal(compareVersions('1.20.3', '1.20.3'), 0);
  assert.equal(compareVersions('1.20.10', '1.20.9'), 1);
  assert.throws(() => parseVersion('1.2.3-01'));
});
test('notes are grouped deterministically and include shared PRs once per product', () => {
  const entries = [entry(1, ['core', 'metadata']), entry(2, [], 'Chore'), entry(3, ['metadata'], 'Add'), entry(4, ['core'], 'Add')];
  assert.equal(notesFor(entries, 'core'), '* Add: Change 4 (#4)\n* Fix: Change 1 (#1)');
  assert.equal(notesFor(entries, 'metadata'), '* Add: Change 3 (#3)\n* Fix: Change 1 (#1)');
});
test('rendering preserves archives/preambles and is idempotent, including CRLF', () => {
  const s = state(); s.entries = [entry(7, ['core'])];
  const original = '# Changelog\r\n\r\nIntroduction.\r\n\r\n### 1.20.2 - old date\r\n* Old note\r\n';
  const rendered = renderChangelog(original, s, 'core');
  assert.match(rendered, /^# Changelog\n\nIntroduction\./);
  assert.match(rendered, /### Unreleased\n\* Fix: Change 7 \(#7\)/);
  assert.match(rendered, /### 1\.20\.2 - old date\n\* Old note\n$/);
  assert.equal(renderChangelog(rendered, s, 'core'), rendered);
  assert.throws(() => renderChangelog('<!-- BEGIN GENERATED RELEASE NOTES -->\n', s, 'core'));
});
test('release finalization preserves newer PRs and the other product pending state', () => {
  const s = state(); s.entries = [entry(1, ['core', 'metadata']), entry(2, ['metadata'])];
  const frozen = snapshot(s);
  s.entries.push(entry(3, ['core']));
  assert.equal(applyRelease(s, frozen), true);
  assert.equal(applyRelease(s, structuredClone(frozen)), false);
  assert.deepEqual(pending(s, 'core').map(e => e.number), [3]);
  assert.deepEqual(pending(s, 'metadata').map(e => e.number), [1, 2]);
  const rendered = renderChangelog('### 1.20.2 - old\n* Archive\n', s, 'core');
  assert.match(rendered, /### Unreleased\n\* Fix: Change 3 \(#3\)/);
  assert.match(rendered, /### 1\.20\.3 - 2026-11-05\n\* Fix: Change 1 \(#1\)/);
  assert.equal(validateState(s), s);
});
test('both products can release the same shared PR independently', () => {
  const s = state(); s.entries = [entry(1, ['core', 'metadata'])];
  applyRelease(s, snapshot(s));
  applyRelease(s, makeSnapshot(s, 'metadata', '1.2.1', source, 'owner/repo', '2026-11-05'));
  assert.deepEqual(pending(s, 'core'), []); assert.deepEqual(pending(s, 'metadata'), []);
});
test('snapshots do not change when more PRs arrive or original labels are edited', () => {
  const s = state(); s.entries = [entry(1, ['core'])];
  const frozen = snapshot(s), serialized = JSON.stringify(frozen);
  s.entries.push(entry(2, ['core']));
  assert.equal(JSON.stringify(frozen), serialized);
  s.entries[0].description = 'Changed after release';
  assert.throws(() => applyRelease(s, frozen), /metadata/);
});
test('duplicate consumption and changed successful release records are rejected', () => {
  const s = state(); s.entries = [entry(1, ['core'])];
  const frozen = snapshot(s); applyRelease(s, frozen);
  assert.throws(() => applyRelease(s, { ...frozen, tag: 'core-v1.20.4', version: '1.20.4' }), /twice/);
  assert.throws(() => applyRelease(s, { ...frozen, date: '2026-11-06' }), /changed/);
});
test('empty or non-increasing product releases are rejected', () => {
  const s = state(); assert.throws(() => snapshot(s), /No unreleased/);
  s.entries = [entry(1, ['core'])];
  assert.throws(() => makeSnapshot(s, 'core', '1.20.2', source, 'owner/repo', '2026-11-05'), /newer/);
  assert.throws(() => makeSnapshot(s, 'core', '1.20.2-rc.1', source, 'owner/repo', '2026-11-05'), /newer/);
});
test('frozen asset manifest allows only the selected package pair and exact hashes', () => {
  const s = state(); s.entries = [entry(1, ['core'])];
  const frozen = snapshot(s), digest = hash(Buffer.from('bytes'));
  frozen.packages = ['nupkg', 'snupkg'].map(ext => ({ name: `XrmMockup365.1.20.3.${ext}`, sha512: digest, contentHash: digest }));
  assert.equal(validateSnapshot(frozen, 'owner/repo'), frozen);
  assert.throws(() => validateSnapshot(frozen, 'other/repo'));
  assert.throws(() => validateSnapshot({ ...frozen, packages: [{ ...frozen.packages[0], name: '../stolen.nupkg' }] }));
  assert.throws(() => validateSnapshot({ ...frozen, packages: [frozen.packages[0]] }));
  assert.throws(() => validateSnapshot({ ...frozen, packages: [{ ...frozen.packages[0], contentHash: 'bad' }, frozen.packages[1]] }));
});
test('ledger rejects duplicate PRs, invalid ownership, and moved baselines', () => {
  const s = state(); s.entries = [entry(1, ['core']), entry(1, ['core'])]; assert.throws(() => validateState(s));
  s.entries = [entry(1, ['other'])]; assert.throws(() => validateState(s));
  s.entries = [entry(1, ['core'])]; const frozen = snapshot(s); frozen.baselineSha = '3'.repeat(40); assert.throws(() => applyRelease(s, frozen));
});
test('PR titles cannot inject generated-block markers or raw HTML into notes', () => {
  const malicious = { ...entry(1, ['core']), description: '<!-- END GENERATED RELEASE NOTES --> <script> & text' };
  const s = state(); s.entries = [malicious];
  const rendered = renderChangelog('### 1.20.2 - old\n', s, 'core');
  assert.match(rendered, /&lt;!-- END GENERATED RELEASE NOTES --&gt;/);
  assert.equal(renderChangelog(rendered, s, 'core'), rendered);
});
