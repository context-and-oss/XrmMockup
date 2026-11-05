import { readFileSync, writeFileSync } from 'node:fs';
import { GitHub, git, fetchHistory, ancestor, assertPublicationCurrent, output, summary, readState } from './github.mjs';
import { products, snapshotName, releaseMarker, validateSnapshot, validateState, notesFor, hash } from './model.mjs';

const phase = process.argv[2];
if (!['draft', 'probe', 'record', 'complete', 'notify'].includes(phase)) throw new Error('Unknown publication phase.');
const api = new GitHub();
fetchHistory();
const snapshotBytes = readFileSync('.release/' + snapshotName);
const snapshot = validateSnapshot(JSON.parse(snapshotBytes), api.repository);
if (process.env.RELEASE_TAG && process.env.RELEASE_TAG !== snapshot.tag) throw new Error('Artifact tag does not match the workflow-selected product release.');
if (!ancestor(snapshot.sourceSha, 'origin/master')) throw new Error('Release source must be on current master ancestry.');
if (git('rev-parse', 'HEAD') !== snapshot.sourceSha) throw new Error('Artifact source differs from checked-out release source.');
const ledger = validateState(readState());
if (snapshot.baselineSha !== ledger.baselineSha || snapshot.entryIds.some(n => !ledger.entries.some(e => e.number === n && e.products.includes(snapshot.product))) ||
    notesFor(ledger.entries.filter(e => snapshot.entryIds.includes(e.number)), snapshot.product) !== snapshot.notes) throw new Error('Artifact notes differ from the tagged source ledger.');
if (!snapshot.packages.length || snapshot.packages.some(p => !/^[a-f0-9]{128}$/.test(p.contentHash ?? ''))) throw new Error('Validated package manifest is required.');
if (git('rev-parse', '--verify', `refs/tags/${snapshot.tag}^{commit}`) !== snapshot.sourceSha) throw new Error('Tag does not match frozen release source.');
for (const pkg of snapshot.packages) {
  if (hash(readFileSync(`.release/packages/${pkg.name}`)) !== pkg.sha512) throw new Error(`Package artifact changed: ${pkg.name}`);
}
let release = await api.releaseByTag(snapshot.tag);
if (release && !release.body?.includes(releaseMarker)) throw new Error('Refusing to alter an unmanaged GitHub release.');
await assertPublicationCurrent(api, ledger, snapshot);
async function upload(name, bytes) {
  const assets = await api.list(`/releases/${release.id}/assets`);
  const existing = assets.find(a => a.name === name);
  if (existing) {
    if (hash(await api.asset(existing)) !== hash(bytes)) throw new Error(`Existing release asset differs: ${name}. Investigate the incomplete/colliding release; assets are never clobbered.`);
    return;
  }
  await api.uploadAsset(snapshot.tag, name, bytes);
}
async function receipt() {
  if (!release) return null;
  const asset = (await api.list(`/releases/${release.id}/assets`)).find(a => a.name === 'nuget-publication.json');
  if (!asset) return null;
  const saved = JSON.parse((await api.asset(asset)).toString('utf8'));
  if (saved.tag !== snapshot.tag || saved.snapshotHash !== hash(snapshotBytes)) throw new Error('NuGet publication receipt does not match frozen assets.');
  return saved;
}
if (phase === 'draft') {
  if (!release) {
    release = await api.createRelease({ tag: snapshot.tag,
      title: `${snapshot.product === 'core' ? 'XrmMockup365' : 'MetadataGenerator'} ${snapshot.version}`,
      notes: `${snapshot.notes}\n\n${releaseMarker}`, prerelease: snapshot.version.includes('-') });
  }
  for (const pkg of snapshot.packages) await upload(pkg.name, readFileSync(`.release/packages/${pkg.name}`));
  // Snapshot last: its presence means all frozen package assets were uploaded.
  await upload(snapshotName, snapshotBytes);
  summary(`Frozen assets attached to ${snapshot.tag}${release.draft ? ' (draft)' : ' (already published)'}.`);
} else if (phase === 'probe') {
  if (!release) throw new Error('Draft release must exist before publication.');
  const saved = await receipt();
  const id = products[snapshot.product].packageId.toLowerCase(), version = snapshot.version.toLowerCase();
  const url = `https://api.nuget.org/v3-flatcontainer/${id}/${version}/${id}.${version}.nupkg`;
  const response = await fetch(url, { signal: AbortSignal.timeout(60000) });
  if (response.status !== 404 && !response.ok) throw new Error(`NuGet package lookup failed: ${response.status}.`);
  const exists = response.ok;
  if (exists) writeFileSync('.release/existing.nupkg', Buffer.from(await response.arrayBuffer()));
  if (saved && !exists) throw new Error('Recorded NuGet publication is not yet visible. Retry after indexing rather than republishing.');
  if (!release.draft && !saved) throw new Error('Published GitHub release is missing a publication receipt.');
  output('package_exists', exists); output('publication_complete', Boolean(saved));
} else if (phase === 'record') {
  if (!release || !release.draft) {
    if (!await receipt()) throw new Error('Expected draft or verified prior publication.');
  } else {
    await upload('nuget-publication.json', Buffer.from(JSON.stringify({ tag: snapshot.tag, snapshotHash: hash(snapshotBytes) }, null, 2) + '\n'));
  }
} else if (phase === 'complete') {
  if (!await receipt()) throw new Error('Both package and symbol publication must succeed before finalizing GitHub release.');
  if (release.draft) {
    await api.publishRelease(snapshot.tag, snapshot.product === 'core' && !snapshot.version.includes('-'));
  }
  summary(`Published ${snapshot.tag}. Tracked history finalization follows separately.`);
} else {
  await api.dispatch('update-changelogs.yml', 'master');
  summary('Requested tracked changelog/release-history reconciliation on master.');
}
