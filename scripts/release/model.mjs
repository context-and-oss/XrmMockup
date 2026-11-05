// Pure release policy and rendering. Shared by validation, reconciliation and release.

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
export const changelogBranch = 'automation/update-changelogs';
export const changelogPrMarker = '<!-- xrmmockup-changelog-pr:v1 -->';
export function isChangelogPr(pr) {
  return pr.user?.login === 'github-actions[bot]' && pr.head?.ref === changelogBranch &&
    typeof pr.base?.repo?.full_name === 'string' && pr.head?.repo?.full_name === pr.base.repo.full_name;
}

// Start PR-based notes after the existing manually maintained release history.
export const historyBaseline = '46f2af04b6d5dc4797f6069c31610c7101de3b47';
export const baselineVersions = { core: '1.20.2', metadata: '1.2.0' };
export const generatedPaths = Object.values(products).map(p => p.changelog);
const begin = '<!-- BEGIN GENERATED RELEASE NOTES -->';
const end = '<!-- END GENERATED RELEASE NOTES -->';

function labelsOf(pr) {
  return new Set((pr.labels ?? []).map(label => typeof label === 'string' ? label : label.name));
}
export function classifyProducts(pr) {
  const labels = labelsOf(pr);
  const selected = Object.keys(products).filter(p => labels.has(products[p].label));
  if (isChangelogPr(pr) && (!labels.has('product:none') || selected.length)) throw new Error('The changelog automation PR must use product:none exclusively.');
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
  if (isChangelogPr(pr) && category?.prefix !== 'Chore') throw new Error('The changelog automation PR must use the Chore category.');
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
  return { numbers: match.slice(1, 4).map(BigInt), prerelease: match[4]?.toLowerCase().split('.') ?? [] };
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
export function notesFor(entries, product) {
  return categories.flatMap(category => entries.filter(entry => entry.products.includes(product) && entry.category === category.prefix)
    .map(entry => `* ${category.prefix}: ${entry.description.replace(/&/g, '&amp;').replace(/</g, '&lt;').replace(/>/g, '&gt;')} (#${entry.number})`)).join('\n');
}
export function releaseSection(version, date, notes) { return `### ${version} - ${date}\n${notes}`; }
export function renderChangelog(original, entries, product, releases = []) {
  let archive = original.replace(/\r\n/g, '\n');
  const start = archive.indexOf(begin), finish = archive.indexOf(end);
  if ((start < 0) !== (finish < 0) || (start >= 0 && finish < start)) throw new Error('Damaged changelog markers.');
  if (start >= 0) {
    // Retain versioned history from the previous renderer during migration.
    const block = archive.slice(start + begin.length, finish), versions = block.search(/^### v?\d/m);
    archive = archive.slice(0, start) + (versions < 0 ? '' : block.slice(versions).trim() + '\n\n') + archive.slice(finish + end.length).replace(/^\n+/, '');
  }
  const firstVersion = archive.search(/^### v?\d/m);
  const preamble = (firstVersion < 0 ? archive : archive.slice(0, firstVersion)).trimEnd();
  const history = firstVersion < 0 ? '' : archive.slice(firstVersion).trimEnd();
  const recorded = new Set([...history.matchAll(/^### v?(\d+\.\d+\.\d+(?:-[\w.-]+)?)(?=\s|$)/gm)].map(match => match[1]));
  const published = releases.filter(release => !recorded.has(parseTag(release.tag_name).version)).map(release => {
    const version = parseTag(release.tag_name).version, body = (release.body ?? '').trim();
    return body.startsWith(`### ${version} -`) ? body : releaseSection(version, release.published_at.slice(0, 10), body);
  });
  const pending = [begin, '### Unreleased', notesFor(entries, product) || '* No unreleased changes.', end].join('\n');
  return [preamble, pending, ...published, history].filter(Boolean).join('\n\n') + '\n';
}
export function pendingNotes(changelog) {
  const match = /### Unreleased\s*\n([\s\S]*?)<!-- END GENERATED RELEASE NOTES -->/.exec(changelog.replace(/\r\n/g, '\n'));
  if (!match) throw new Error('Generated Unreleased notes are missing. Merge the changelog PR before tagging.');
  return match[1].trim();
}
export function validateRelease(release) {
  const tag = parseTag(release.tag);
  if (release.product !== tag.product || release.version !== tag.version || !/^[a-f0-9]{40}$/.test(release.sourceSha)) throw new Error('Invalid release build metadata.');
  return { ...tag, sourceSha: release.sourceSha };
}
