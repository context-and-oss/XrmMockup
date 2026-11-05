import { readFileSync, writeFileSync } from 'node:fs';
import { GitHub, git, fetchHistory, ancestor, assertPublicationCurrent, output, summary, readState } from './github.mjs';
import { products, snapshotName, releaseMarker, validateSourceSnapshot, validateState, hash } from './model.mjs';

const stage = process.argv[2];
if (!['stage', 'finalize'].includes(stage)) throw new Error('Expected stage or finalize.');
const api = new GitHub();
fetchHistory();
const snapshotBytes = readFileSync('.release/' + snapshotName);
const ledger = validateState(readState());
const snapshot = validateSourceSnapshot(JSON.parse(snapshotBytes), ledger, git('rev-parse', 'HEAD'), api.repository);
if (process.env.RELEASE_TAG && process.env.RELEASE_TAG !== snapshot.tag) throw new Error('Artifact tag does not match the selected release.');
if (!ancestor(snapshot.sourceSha, 'origin/master') || git('rev-parse', `refs/tags/${snapshot.tag}^{commit}`) !== snapshot.sourceSha) throw new Error('Release source/tag is not on master ancestry.');
if (!snapshot.packages.length) throw new Error('Validated package manifest is required.');
for (const pkg of snapshot.packages) {
  if (hash(readFileSync(`.release/packages/${pkg.name}`)) !== pkg.sha512) throw new Error(`Package artifact changed: ${pkg.name}`);
}
let release = await api.releaseByTag(snapshot.tag);
if (release && !release.body?.includes(releaseMarker)) throw new Error('Refusing to alter an unmanaged GitHub release.');
await assertPublicationCurrent(api, ledger, snapshot);

if (!release) {
  if (stage !== 'stage') throw new Error('Stage the release before finalizing it.');
  release = await api.createRelease({ tag: snapshot.tag, title: `${snapshot.product === 'core' ? 'XrmMockup365' : 'MetadataGenerator'} ${snapshot.version}`,
    notes: `${snapshot.notes}\n\n${releaseMarker}`, prerelease: snapshot.version.includes('-') });
}
const assets = await api.list(`/releases/${release.id}/assets`);
async function upload(name, bytes) {
  const existing = assets.find(asset => asset.name === name);
  if (existing) {
    if (hash(await api.asset(existing)) !== hash(bytes)) throw new Error(`Existing release asset differs: ${name}. Assets are never clobbered.`);
  } else await api.uploadAsset(snapshot.tag, name, bytes);
}
const receiptAsset = assets.find(asset => asset.name === 'nuget-publication.json');
if (receiptAsset) {
  const receipt = JSON.parse((await api.asset(receiptAsset)).toString('utf8'));
  if (receipt.tag !== snapshot.tag || receipt.snapshotHash !== hash(snapshotBytes)) throw new Error('Publication receipt does not match frozen assets.');
}
if (stage === 'stage') {
  for (const pkg of snapshot.packages) await upload(pkg.name, readFileSync(`.release/packages/${pkg.name}`));
  await upload(snapshotName, snapshotBytes); // A complete snapshot implies all packages were uploaded.
  const id = products[snapshot.product].packageId.toLowerCase(), version = snapshot.version.toLowerCase();
  const response = await fetch(`https://api.nuget.org/v3-flatcontainer/${id}/${version}/${id}.${version}.nupkg`, { signal: AbortSignal.timeout(60000) });
  if (response.status !== 404 && !response.ok) throw new Error(`NuGet package lookup failed: ${response.status}.`);
  if (response.ok) writeFileSync('.release/existing.nupkg', Buffer.from(await response.arrayBuffer()));
  if (receiptAsset && !response.ok) throw new Error('Recorded NuGet publication is not yet visible; retry after indexing.');
  if (!release.draft && !receiptAsset) throw new Error('Published release is missing its publication receipt.');
  output('package_exists', response.ok); output('publication_complete', Boolean(receiptAsset));
  summary(`Staged frozen artifacts for ${snapshot.tag}.`);
} else {
  if (!receiptAsset) {
    if (process.env.NUGET_PUBLISHED !== 'true') throw new Error('Package and symbol publication must succeed before finalizing the release.');
    await upload('nuget-publication.json', Buffer.from(JSON.stringify({ tag: snapshot.tag, snapshotHash: hash(snapshotBytes) }, null, 2) + '\n'));
  }
  if (release.draft) await api.publishRelease(snapshot.tag, snapshot.product === 'core' && !snapshot.version.includes('-'));
  await api.dispatch('update-changelogs.yml', 'master');
  summary(`Published ${snapshot.tag}; requested its changelog PR history update.`);
}
