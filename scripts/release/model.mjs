// Pure release policy and rendering. Shared by validation, reconciliation and release.
import { createHash } from 'node:crypto';

export const products = {
  core: { label: 'product:core', packageId: 'XrmMockup365', project: 'src/XrmMockup365/XrmMockup365.csproj', changelog: 'RELEASE_NOTES.md', versionProperty: 'CoreReleaseVersion' },
  metadata: { label: 'product:metadata', packageId: 'XrmMockup.MetadataGenerator', project: 'src/MetadataGen/MetadataGenerator.Tool/MetadataGenerator.Tool.csproj', changelog: 'src/MetadataGen/MetadataGenerator.Tool/CHANGELOG.md', versionProperty: 'MetadataReleaseVersion' },
};
export const categories = [
  { label: 'change:feature', prefix: 'Add' },
  { label: 'change:fix', prefix: 'Fix' },
  { label: 'change:change', prefix: 'Change' },
  { label: 'change:breaking', prefix: 'Breaking' },
  { label: 'change:chore', prefix: 'Chore' },
];
export const statePath = '.github/release-state.json';
export const generatedPaths = [statePath, ...Object.values(products).map(p => p.changelog)];
export const snapshotName = 'release-snapshot.json';
const begin = '<!-- BEGIN GENERATED RELEASE NOTES -->';
const end = '<!-- END GENERATED RELEASE NOTES -->';
export const releaseMarker = '<!-- xrmmockup-product-release:v1 -->';

function labelsOf(pr) {
  return new Set((pr.labels ?? []).map(label => typeof label === 'string' ? label : label.name));
}
export function classifyProducts(pr) {
  const labels = labelsOf(pr);
  const selected = Object.keys(products).filter(p => labels.has(products[p].label));
  if (labels.has('product:none')) {
    if (selected.length) throw new Error('product:none must not be combined with product:core or product:metadata.');
    return [];
  }
  if (!selected.length) throw new Error('Add product:core, product:metadata, both, or product:none.');
  return selected;
}
export function classifyCategory(pr) {
  const labels = labelsOf(pr);
  const selected = categories.filter(c => labels.has(c.label));
  if (selected.length > 1) throw new Error('Use exactly one change category label.');
  const title = String(pr.title ?? '').trim();
  if (!title || /[\r\n]/.test(title)) throw new Error('Use a non-empty, single-line descriptive PR title.');
  const match = /^(Add|Fix|Change|Breaking|Chore):\s*(.*)$/i.exec(title);
  if (match && !match[2].trim()) throw new Error('Add descriptive text after the category prefix.');
  const inferred = match && categories.find(c => c.prefix.toLowerCase() === match[1].toLowerCase());
  if (selected[0] && inferred && selected[0] !== inferred) throw new Error('The category label conflicts with the PR title prefix.');
  const category = selected[0] ?? inferred;
  if (!category) throw new Error('Add a change category label or start the title with Add:, Fix:, Change:, Breaking:, or Chore:.');
  return { category: category.prefix, description: match ? match[2].trim() : title };
}
export function classify(pr) {
  return { products: classifyProducts(pr), ...classifyCategory(pr) };
}
export function parseVersion(version) {
  // NuGet version identity is kept unambiguous: no build metadata or numeric leading zeros.
  const match = /^(0|[1-9]\d*)\.(0|[1-9]\d*)\.(0|[1-9]\d*)(?:-([0-9A-Za-z-]+(?:\.[0-9A-Za-z-]+)*))?$/.exec(version);
  if (!match || (match[4] ?? '').split('.').some(x => /^\d+$/.test(x) && x.length > 1 && x[0] === '0')) {
    throw new Error(`Invalid release version: ${version}. Use major.minor.patch[-prerelease] without leading zeros or build metadata.`);
  }
  // .NET/NuGet version components must fit Int32.
  if (match.slice(1, 4).some(x => BigInt(x) > 2147483647n)) throw new Error('Version components exceed NuGet limits.');
  return { numbers: match.slice(1, 4).map(BigInt), prerelease: match[4]?.split('.') ?? [] };
}
export function compareVersions(a, b) {
  const x = parseVersion(a), y = parseVersion(b);
  for (let i = 0; i < 3; i++) if (x.numbers[i] !== y.numbers[i]) return x.numbers[i] > y.numbers[i] ? 1 : -1;
  if (!x.prerelease.length || !y.prerelease.length) return x.prerelease.length === y.prerelease.length ? 0 : x.prerelease.length ? -1 : 1;
  for (let i = 0; i < Math.max(x.prerelease.length, y.prerelease.length); i++) {
    const l = x.prerelease[i], r = y.prerelease[i];
    if (l === r) continue;
    if (l === undefined || r === undefined) return l === undefined ? -1 : 1;
    const ln = /^\d+$/.test(l), rn = /^\d+$/.test(r);
    if (ln && rn) return BigInt(l) > BigInt(r) ? 1 : -1;
    if (ln !== rn) return ln ? -1 : 1;
    return l > r ? 1 : -1;
  }
  return 0;
}
export function parseTag(tag) {
  const match = /^(core|metadata)-v(.+)$/.exec(tag);
  if (!match) throw new Error('Release tags must be core-v<VERSION> or metadata-v<VERSION>.');
  if (/-v\d+\.\d+\.\d+(?:$|-)/.test(match[2])) throw new Error('Combined legacy version tags are not allowed.');
  parseVersion(match[2]);
  return { product: match[1], version: match[2], tag, prerelease: match[2].includes('-') };
}
export function validateState(state) {
  if (state.schemaVersion !== 1 || !/^[a-f0-9]{40}$/.test(state.baselineSha ?? '')) throw new Error('Invalid release ledger/baseline.');
  if (state.checkpointSha !== undefined && (typeof state.checkpointSha !== 'string' || !/^[a-f0-9]{40}$/.test(state.checkpointSha))) throw new Error('Invalid reconciliation checkpoint.');
  if (!Array.isArray(state.entries) || !Array.isArray(state.releases)) throw new Error('Invalid release ledger arrays.');
  for (const product of Object.keys(products)) parseVersion(state.baselineVersions?.[product] ?? '');
  const ids = new Set();
  for (const entry of state.entries) {
    if (!Number.isSafeInteger(entry.number) || entry.number < 1 || ids.has(entry.number)) throw new Error('Invalid/duplicate PR in ledger.');
    ids.add(entry.number);
    if (!/^[a-f0-9]{40}$/.test(entry.mergeSha) || !Array.isArray(entry.products) || new Set(entry.products).size !== entry.products.length || entry.products.some(p => !products[p])) throw new Error('Invalid ledger PR ownership/source.');
    if (!categories.some(c => c.prefix === entry.category) || typeof entry.description !== 'string' || !entry.description.trim() || /[\r\n]/.test(entry.description)) throw new Error('Invalid ledger PR category/description.');
  }
  const tags = new Set(), releasedEntries = new Set();
  for (const release of state.releases) {
    if (tags.has(release.tag)) throw new Error('Duplicate release record.');
    tags.add(release.tag);
    validateSnapshot(release);
    if (release.baselineSha !== state.baselineSha) throw new Error('Release baseline mismatch.');
    for (const id of release.entryIds) {
      if (!state.entries.some(e => e.number === id && e.products.includes(release.product)) || releasedEntries.has(`${release.product}:${id}`)) throw new Error('Invalid or duplicate released PR ownership.');
      releasedEntries.add(`${release.product}:${id}`);
    }
    if (notesFor(state.entries.filter(e => release.entryIds.includes(e.number)), release.product) !== release.notes) throw new Error('Release notes do not match ledger metadata.');
  }
  return state;
}
export function consumed(state, product) {
  return new Set(state.releases.filter(r => r.product === product).flatMap(r => r.entryIds));
}
export function pending(state, product) {
  const done = consumed(state, product);
  return state.entries.filter(e => e.products.includes(product) && !done.has(e.number));
}
export function notesFor(entries, product) {
  return categories.flatMap(c => entries.filter(e => e.products.includes(product) && e.category === c.prefix)
    .map(e => `* ${c.prefix}: ${e.description.replace(/&/g, '&amp;').replace(/</g, '&lt;').replace(/>/g, '&gt;')} (#${e.number})`)).join('\n');
}
export function renderChangelog(original, state, product) {
  const normalized = original.replace(/\r\n/g, '\n');
  const markerStart = normalized.indexOf(begin), markerEnd = normalized.indexOf(end);
  if ((markerStart < 0) !== (markerEnd < 0) || (markerStart >= 0 && markerEnd < markerStart)) throw new Error('Damaged generated changelog markers.');
  const archive = markerStart < 0 ? normalized : normalized.slice(0, markerStart) + normalized.slice(markerEnd + end.length).replace(/^\n+/, '');
  const heading = archive.search(/^### /m);
  const preamble = heading < 0 ? archive.trimEnd() : archive.slice(0, heading).trimEnd();
  const history = heading < 0 ? '' : archive.slice(heading).trimEnd();
  const releases = state.releases.filter(r => r.product === product).sort((a, b) => compareVersions(b.version, a.version));
  const sections = [begin, '### Unreleased', notesFor(pending(state, product), product) || '* No unreleased changes.',
    ...releases.flatMap(r => ['', `### ${r.version} - ${r.date}`, r.notes]), end];
  return [preamble, sections.join('\n'), history].filter(Boolean).join('\n\n') + '\n';
}
export function makeSnapshot(state, product, version, sourceSha, repository, date) {
  parseVersion(version);
  if (!products[product]) throw new Error('Unknown product.');
  const previous = state.releases.filter(r => r.product === product).sort((a, b) => compareVersions(b.version, a.version))[0];
  if (compareVersions(version, previous?.version ?? state.baselineVersions[product]) <= 0) throw new Error('Release version must be newer than the previous successful product release/baseline.');
  const entries = pending(state, product);
  if (!entries.length) throw new Error(`No unreleased ${product} PRs at the selected source.`);
  return {
    schemaVersion: 1, repository, baselineSha: state.baselineSha, sourceSha, product, version,
    tag: `${product}-v${version}`, date, entryIds: entries.map(e => e.number), notes: notesFor(entries, product),
    previousTag: previous?.tag ?? null, packages: [],
  };
}
export function validateSnapshot(snapshot, repository) {
  const parsed = parseTag(snapshot.tag ?? '');
  if (snapshot.schemaVersion !== 1 || snapshot.product !== parsed.product || snapshot.version !== parsed.version ||
      !/^[a-f0-9]{40}$/.test(snapshot.sourceSha ?? '') || !/^[a-f0-9]{40}$/.test(snapshot.baselineSha ?? '') ||
      !/^\d{4}-\d{2}-\d{2}$/.test(snapshot.date ?? '') || !Array.isArray(snapshot.entryIds) || !snapshot.entryIds.length ||
      new Set(snapshot.entryIds).size !== snapshot.entryIds.length || snapshot.entryIds.some(n => !Number.isSafeInteger(n) || n < 1) ||
      typeof snapshot.notes !== 'string' || !snapshot.notes.trim() || !Array.isArray(snapshot.packages) ||
      (repository && snapshot.repository !== repository)) throw new Error('Invalid release snapshot.');
  const filenames = new Set();
  for (const pkg of snapshot.packages) {
    const expected = `${products[snapshot.product].packageId}.${snapshot.version}`;
    if (![`${expected}.nupkg`, `${expected}.snupkg`].includes(pkg.name) || filenames.has(pkg.name) || (!/^[a-f0-9]{128}$/.test(pkg.sha512 ?? '') || !/^[a-f0-9]{128}$/.test(pkg.contentHash ?? ''))) throw new Error('Invalid release package manifest.');
    filenames.add(pkg.name);
  }
  if (snapshot.packages.length && (!snapshot.packages.some(p => p.name.endsWith('.nupkg')) || !snapshot.packages.some(p => p.name.endsWith('.snupkg')))) throw new Error('Release manifest must contain the package and symbol package.');
  return snapshot;
}
export function applyRelease(state, snapshot) {
  validateSnapshot(snapshot);
  if (snapshot.baselineSha !== state.baselineSha) throw new Error('Release baseline mismatch.');
  const existing = state.releases.find(r => r.tag === snapshot.tag);
  if (existing) {
    if (JSON.stringify(existing) !== JSON.stringify(snapshot)) throw new Error('A recorded release snapshot changed.');
    return false;
  }
  const entries = snapshot.entryIds.map(n => state.entries.find(e => e.number === n));
  if (entries.some(e => !e || !e.products.includes(snapshot.product)) || notesFor(entries, snapshot.product) !== snapshot.notes) throw new Error('Released PR metadata does not match the ledger.');
  const already = consumed(state, snapshot.product);
  if (snapshot.entryIds.some(n => already.has(n))) throw new Error('A PR was released twice for the same product.');
  state.releases.push(snapshot);
  return true;
}
export function hash(buffer) { return createHash('sha512').update(buffer).digest('hex'); }
