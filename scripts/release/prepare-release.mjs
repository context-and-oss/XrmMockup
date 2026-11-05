import { readFileSync, writeFileSync, mkdirSync } from 'node:fs';
import { materialize } from './workspace.mjs';
import { GitHub, git, fetchHistory, ancestor, assertPublicationCurrent, output, summary, readState, discoverPrs, reconcileReleases } from './github.mjs';
import { products, parseTag, parseVersion, validateState, makeSnapshot, applyRelease, renderChangelog, validateSourceSnapshot, snapshotName, releaseMarker } from './model.mjs';

const api = new GitHub();
const manual = process.env.GITHUB_EVENT_NAME === 'workflow_dispatch';
const dryRun = manual && process.env.DRY_RUN !== 'false';
const productInput = process.env.PRODUCT;
const versionInput = process.env.VERSION;
const resolved = manual ? (() => {
  if (!products[productInput]) throw new Error('Choose core or metadata.');
  parseVersion(versionInput);
  return parseTag(`${productInput}-v${versionInput}`);
})() : parseTag(process.env.GITHUB_REF_NAME ?? '');
const { product, version, tag } = resolved;
const sourceSha = git('rev-parse', 'HEAD');
fetchHistory();
if (!ancestor(sourceSha, 'origin/master')) throw new Error('Release source must be on master ancestry.');
if (!dryRun && git('rev-parse', '--verify', `refs/tags/${tag}^{commit}`) !== sourceSha) throw new Error('Publishing requires an existing matching tag at the selected source.');
const state = validateState(readState());
const discovered = structuredClone(state);
await discoverPrs(api, discovered, sourceSha);
if (JSON.stringify(discovered.entries) !== JSON.stringify(state.entries)) throw new Error('Tagged source has unreconciled PRs. Wait for the changelog updater and tag its generated commit.');
for (const [p, config] of Object.entries(products)) {
  const actual = readFileSync(config.changelog, 'utf8').replace(/\r\n/g, '\n');
  if (renderChangelog(actual, state, p) !== actual) throw new Error(`${config.changelog} is not reconciled at the selected source.`);
}
mkdirSync('.release/packages', { recursive: true });
let snapshot, reuse = false;
const existing = await api.releaseByTag(tag);
if (existing) {
  if (!existing.body?.includes(releaseMarker)) throw new Error('An unmanaged GitHub release already exists for this tag.');
  snapshot = await api.snapshot(existing);
  if (snapshot) {
    validateSourceSnapshot(snapshot, state, sourceSha, api.repository);
    if (snapshot.tag !== tag || !snapshot.packages.length || typeof snapshot.generatedChangelog !== 'string') throw new Error('Existing frozen release is incomplete.');
    await assertPublicationCurrent(api, state, snapshot);
    const assets = await api.list(`/releases/${existing.id}/assets`);
    for (const pkg of snapshot.packages) {
      const asset = assets.find(a => a.name === pkg.name);
      if (!asset) throw new Error(`Frozen package asset missing: ${pkg.name}. Do not rebuild published artifacts.`);
      writeFileSync(`.release/packages/${pkg.name}`, await api.asset(asset));
    }
    reuse = true;
  } else if (!existing.draft) throw new Error('Published release has no frozen snapshot.');
}
if (!snapshot) {
  await reconcileReleases(api, state, sourceSha, product);
  snapshot = makeSnapshot(state, product, version, sourceSha, api.repository, existing?.created_at.slice(0, 10) ?? new Date().toISOString().slice(0, 10));
  applyRelease(state, snapshot);
  snapshot.generatedChangelog = renderChangelog(readFileSync(products[product].changelog, 'utf8'), state, product);
}
writeFileSync('.release/' + snapshotName, JSON.stringify(snapshot, null, 2) + '\n');
materialize(snapshot);
for (const [key, value] of Object.entries({ product, version, tag, project: products[product].project, source_sha: sourceSha, dry_run: dryRun, reuse })) output(key, value);
summary(`Prepared ${tag} at ${sourceSha}; ${snapshot.entryIds.length} PR(s), ${dryRun ? 'credential-free preview' : 'publication'}, ${reuse ? 'reusing frozen artifacts' : 'new build'}.`);
