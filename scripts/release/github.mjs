import { execFileSync, spawnSync } from 'node:child_process';
import { readFileSync, writeFileSync, appendFileSync } from 'node:fs';
import { isDeepStrictEqual } from 'node:util';
import { hash, classify, products, generatedPaths, statePath, snapshotName, releaseMarker, parseTag, validateSnapshot, applyRelease, compareVersions, makeSnapshot } from './model.mjs';

export function git(...args) {
  return execFileSync('git', args, { encoding: 'utf8', maxBuffer: 20 * 1024 * 1024 }).replace(/\r\n/g, '\n').trim();
}
// Do not persist a privileged token in the requested source checkout. Authenticate
// history refreshes only in this trusted subprocess's environment (not its argv).
export function fetchHistory() {
  const env = { ...process.env };
  const token = env.GH_TOKEN ?? env.GITHUB_TOKEN;
  if (token) {
    const count = Number(env.GIT_CONFIG_COUNT ?? 0);
    if (!Number.isSafeInteger(count) || count < 0) throw new Error('Invalid Git configuration environment.');
    env.GIT_CONFIG_COUNT = String(count + 1);
    env[`GIT_CONFIG_KEY_${count}`] = `http.${env.GITHUB_SERVER_URL ?? 'https://github.com'}/.extraheader`;
    env[`GIT_CONFIG_VALUE_${count}`] = `AUTHORIZATION: basic ${Buffer.from('x-access-token:' + token).toString('base64')}`;
  }
  return execFileSync('git', ['fetch', 'origin', 'master', '--tags'], { env, encoding: 'utf8', maxBuffer: 20 * 1024 * 1024, stdio: ['ignore', 'pipe', 'pipe'] }).trim();
}
export function ancestor(a, b) {
  const result = spawnSync('git', ['merge-base', '--is-ancestor', a, b]);
  if (result.status > 1 || result.error) throw new Error(`Unable to verify git ancestry: ${a} -> ${b}`);
  return result.status === 0;
}
export class GitHub {
  constructor({ repository = process.env.GITHUB_REPOSITORY, token = process.env.GH_TOKEN ?? process.env.GITHUB_TOKEN, fetcher = fetch } = {}) {
    if (!/^[\w.-]+\/[\w.-]+$/.test(repository ?? '')) throw new Error('GITHUB_REPOSITORY is required.');
    if (!token) throw new Error('A GitHub API token is required.');
    this.repository = repository; this.token = token; this.fetcher = fetcher;
    this.base = process.env.GITHUB_API_URL ?? 'https://api.github.com';
  }
  async request(path, { method = 'GET', body, raw = false, allow404 = false, headers = {} } = {}) {
    const url = /^https?:\/\//.test(path) ? path : `${this.base}${path}`;
    // Only send credentials to the GitHub API / upload service, not arbitrary asset URLs.
    if (![new URL(this.base).hostname, 'uploads.github.com'].includes(new URL(url).hostname)) throw new Error('Untrusted GitHub API host.');
    const response = await this.fetcher(url, {
      method, signal: AbortSignal.timeout(60000), headers: { authorization: `Bearer ${this.token}`, accept: raw ? 'application/octet-stream' : 'application/vnd.github+json',
        'X-GitHub-Api-Version': '2022-11-28', 'user-agent': 'xrmmockup-release', ...(body && !Buffer.isBuffer(body) ? { 'content-type': 'application/json' } : {}), ...headers },
      ...(body ? { body: Buffer.isBuffer(body) ? body : JSON.stringify(body) } : {}),
    });
    if (allow404 && response.status === 404) return null;
    if (!response.ok) throw new Error(`GitHub ${method} ${new URL(url).pathname}: ${response.status} ${(await response.text()).slice(0, 500)}`);
    if (response.status === 204) return null;
    return raw ? Buffer.from(await response.arrayBuffer()) : response.json();
  }
  repo(path, options) { return this.request(`/repos/${this.repository}${path}`, options); }
  async list(path) {
    const all = [];
    for (let page = 1; ; page++) {
      const batch = await this.repo(`${path}${path.includes('?') ? '&' : '?'}per_page=100&page=${page}`);
      all.push(...batch);
      if (batch.length < 100) return all;
    }
  }
  // The tag endpoint is for published releases. Authenticated listing includes drafts.
  async releaseByTag(tag) {
    const matches = (await this.list('/releases')).filter(release => release.tag_name === tag);
    if (matches.length > 1) throw new Error(`Multiple releases/drafts use tag ${tag}; resolve the ambiguity before retrying.`);
    return matches[0] ?? null;
  }
  async asset(asset) { return this.repo(`/releases/assets/${asset.id}`, { raw: true }); }
  async snapshot(release, requireReceipt = false) {
    const assets = await this.list(`/releases/${release.id}/assets`);
    const asset = assets.find(a => a.name === snapshotName);
    if (!asset) return null;
    const bytes = await this.asset(asset);
    const snapshot = validateSnapshot(JSON.parse(bytes.toString('utf8')), this.repository);
    if (requireReceipt) {
      const receiptAsset = assets.find(a => a.name === 'nuget-publication.json');
      if (!receiptAsset) throw new Error(`Release ${release.tag_name} has no verified publication receipt.`);
      const receipt = JSON.parse((await this.asset(receiptAsset)).toString('utf8'));
      if (receipt.tag !== snapshot.tag || receipt.snapshotHash !== hash(bytes)) throw new Error('Release publication receipt/snapshot mismatch.');
    }
    return snapshot;
  }
}
export function output(name, value) {
  if (/[\r\n]/.test(String(value))) throw new Error('Invalid workflow output.');
  if (process.env.GITHUB_OUTPUT) appendFileSync(process.env.GITHUB_OUTPUT, `${name}=${value}\n`);
}
export function summary(text) {
  if (process.env.GITHUB_STEP_SUMMARY) appendFileSync(process.env.GITHUB_STEP_SUMMARY, `${text}\n`);
  else console.log(text);
}
function firstParent(sha) {
  const parents = git('rev-list', '--parents', '-n', '1', sha).split(' ');
  return parents.length === 2 ? parents[1] : null;
}
function generatedCommit(sha) {
  const identity = 'release-changelog-bot <release-changelog-bot@users.noreply.github.com>';
  // PR rebases/squashes can preserve a forged author. The writer must also be
  // the committer; normal protected-branch bypass remains limited to its identity.
  if (!firstParent(sha) || git('show', '-s', '--format=%an <%ae>%n%cn <%ce>', sha).split('\n').some(value => value !== identity)) return false;
  const paths = git('diff-tree', '--no-commit-id', '--name-only', '-r', sha).split('\n').filter(Boolean);
  return paths.length > 0 && paths.every(path => generatedPaths.includes(path));
}
export async function discoverPrs(api, state, sourceSha) {
  if (!ancestor(state.baselineSha, sourceSha)) throw new Error('Source is not descended from the release collection baseline.');
  const checkpoint = state.checkpointSha ?? state.baselineSha;
  if (!ancestor(state.baselineSha, checkpoint) || !ancestor(checkpoint, sourceSha) ||
      !git('rev-list', '--first-parent', sourceSha).split('\n').includes(checkpoint)) throw new Error('Reconciliation checkpoint is not on the selected source first-parent ancestry.');
  // A reviewed/manual ledger edit invalidates the skip boundary. Rescan rather than
  // trusting an arbitrarily advanced checkpoint, and let the writer establish it again.
  const ledgerCommit = git('log', '-1', '--format=%H', sourceSha, '--', statePath);
  const trustedCheckpoint = checkpoint === state.baselineSha || (ledgerCommit && generatedCommit(ledgerCommit) && ancestor(checkpoint, firstParent(ledgerCommit)));
  const from = trustedCheckpoint ? checkpoint : state.baselineSha;
  const known = new Set(state.entries.map(entry => entry.number));
  const commits = git('rev-list', '--reverse', '--first-parent', `${from}..${sourceSha}`).split('\n').filter(Boolean);
  let substantiveHistory = false;
  for (const sha of commits) {
    // The writer's own pushes must not make API requests or create an endless
    // checkpoint-only commit loop. Verify their identity, parent and path allowlist locally.
    if (generatedCommit(sha)) continue;
    const associated = await api.list(`/commits/${sha}/pulls`);
    const eligible = associated.filter(pr => pr.base?.ref === 'master' && pr.merged_at && pr.merge_commit_sha &&
      ancestor(pr.merge_commit_sha, sourceSha) && !ancestor(pr.merge_commit_sha, state.baselineSha));
    if (!eligible.length) throw new Error(`Commit ${sha} has no classified merged master PR. Use PRs or record an explicitly reviewed emergency correction.`);
    substantiveHistory = true;
    for (const candidate of eligible.sort((a, b) => a.number - b.number)) {
      if (known.has(candidate.number)) continue;
      const pr = await api.repo(`/pulls/${candidate.number}`);
      if (!pr.merged || pr.base.ref !== 'master') throw new Error(`PR #${pr.number} is not merged into master.`);
      const classification = classify(pr);
      state.entries.push({ number: pr.number, mergeSha: pr.merge_commit_sha, ...classification });
      known.add(pr.number);
    }
  }
  // Old ledgers migrate on their next successful write. Bot-only self-runs keep
  // the previous boundary, so they remain a true no-op rather than generating another push.
  if (substantiveHistory || !state.checkpointSha) state.checkpointSha = sourceSha;
  return state;
}
export async function successfulSnapshots(api, baselineSha, sourceSha, releaseProduct) {
  const snapshots = [];
  for (const release of await api.list('/releases')) {
    if (release.draft || !release.body?.includes(releaseMarker)) continue;
    let tag;
    try { tag = parseTag(release.tag_name); } catch { continue; }
    const snapshot = await api.snapshot(release, true);
    if (!snapshot || !snapshot.packages.length || snapshot.tag !== tag.tag || snapshot.baselineSha !== baselineSha) throw new Error(`Published release ${release.tag_name} has no valid frozen snapshot.`);
    const tagSha = git('rev-parse', `${tag.tag}^{commit}`);
    if (tagSha !== snapshot.sourceSha) throw new Error(`Published tag ${tag.tag} moved since publication.`);
    if (sourceSha && releaseProduct === snapshot.product && !ancestor(snapshot.sourceSha, sourceSha)) throw new Error('New release source is older than or diverges from a successful product release.');
    if (!sourceSha || ancestor(snapshot.sourceSha, sourceSha)) snapshots.push(snapshot);
  }
  return snapshots.sort((a, b) => a.date.localeCompare(b.date) || a.tag.localeCompare(b.tag));
}
export async function reconcileReleases(api, state, sourceSha, releaseProduct) {
  for (const snapshot of await successfulSnapshots(api, state.baselineSha, sourceSha, releaseProduct)) applyRelease(state, snapshot);
  return state;
}
// Run in both preparation and every publish invocation. A job-only retry must
// not trust the historical tagged ledger as the current publication boundary.
export async function assertPublicationCurrent(api, state, snapshot) {
  const successful = (await successfulSnapshots(api, state.baselineSha)).filter(release => release.product === snapshot.product);
  const completed = successful.find(release => release.tag === snapshot.tag);
  if (completed) {
    if (!isDeepStrictEqual(completed, snapshot)) throw new Error('Completed release differs from the frozen retry snapshot.');
    return true; // Recovery of immutable completed releases is allowed even after newer ones.
  }
  const effective = structuredClone(state);
  for (const release of successful) {
    if (compareVersions(snapshot.version, release.version) <= 0) throw new Error('Obsolete draft: version must be newer than every successful same-product release.');
    if (!ancestor(release.sourceSha, snapshot.sourceSha)) throw new Error('Obsolete draft: source is older than or diverges from a successful same-product release.');
    if (snapshot.entryIds.some(id => release.entryIds.includes(id))) throw new Error('Obsolete draft: PRs were already consumed by a successful same-product release.');
    applyRelease(effective, release);
  }
  const expected = makeSnapshot(effective, snapshot.product, snapshot.version, snapshot.sourceSha, snapshot.repository, snapshot.date);
  if (!isDeepStrictEqual(expected.entryIds, snapshot.entryIds) || expected.notes !== snapshot.notes) throw new Error('Obsolete draft: frozen PR set no longer matches the current unreleased product changes.');
  return false;
}
export function readState() { return JSON.parse(readFileSync(statePath, 'utf8')); }
export function writeState(state) { writeFileSync(statePath, JSON.stringify(state, null, 2) + '\n'); }
export async function validatePulls(api, event) {
  if (event.pull_request) return [await api.repo(`/pulls/${event.pull_request.number}`)];
  if (!event.merge_group) throw new Error('Expected a pull_request_target or merge_group event.');
  const group = event.merge_group, found = new Map();
  for (let page = 1; ; page++) {
    const comparison = await api.repo(`/compare/${group.base_sha}...${group.head_sha}?per_page=100&page=${page}`);
    for (const commit of comparison.commits) {
      for (const pr of await api.list(`/commits/${commit.sha}/pulls`)) {
        if (pr.base?.ref === 'master' && !pr.merged_at) found.set(pr.number, pr);
      }
    }
    if (comparison.commits.length < 100) break;
  }
  if (!found.size) throw new Error('Cannot identify the PRs in this merge group; refusing to bypass classification.');
  return Promise.all([...found.keys()].map(n => api.repo(`/pulls/${n}`)));
}
