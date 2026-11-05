import { execFileSync, spawnSync } from 'node:child_process';
import { readFileSync, writeFileSync, appendFileSync } from 'node:fs';
import { hash, classify, products, generatedPaths, statePath, snapshotName, releaseMarker, parseTag, validateSnapshot, applyRelease } from './model.mjs';

export function git(...args) {
  return execFileSync('git', args, { encoding: 'utf8', maxBuffer: 20 * 1024 * 1024 }).trim();
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
export async function discoverPrs(api, state, sourceSha) {
  if (!ancestor(state.baselineSha, sourceSha)) throw new Error('Source is not descended from the release collection baseline.');
  const known = new Set(state.entries.map(e => e.number));
  const commits = git('rev-list', '--reverse', '--first-parent', `${state.baselineSha}..${sourceSha}`).split('\n').filter(Boolean);
  for (const sha of commits) {
    const associated = await api.list(`/commits/${sha}/pulls`);
    const eligible = associated.filter(pr => pr.base?.ref === 'master' && pr.merged_at && pr.merge_commit_sha &&
      ancestor(pr.merge_commit_sha, sourceSha) && !ancestor(pr.merge_commit_sha, state.baselineSha));
    if (!eligible.length) {
      const paths = git('diff-tree', '--no-commit-id', '--name-only', '-r', sha).split('\n').filter(Boolean);
      const identity = git('show', '-s', '--format=%an <%ae>', sha);
      if (!paths.length || !paths.every(p => generatedPaths.includes(p)) || identity !== 'release-changelog-bot <release-changelog-bot@users.noreply.github.com>') {
        throw new Error(`Commit ${sha} has no classified merged master PR. Use PRs or record an explicitly reviewed emergency correction.`);
      }
      continue;
    }
    for (const candidate of eligible.sort((a, b) => a.number - b.number)) {
      if (known.has(candidate.number)) continue;
      const pr = await api.repo(`/pulls/${candidate.number}`);
      if (!pr.merged || pr.base.ref !== 'master') throw new Error(`PR #${pr.number} is not merged into master.`);
      const classification = classify(pr);
      state.entries.push({ number: pr.number, mergeSha: pr.merge_commit_sha, ...classification });
      known.add(pr.number);
    }
  }
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
    if (releaseProduct === snapshot.product && !ancestor(snapshot.sourceSha, sourceSha)) throw new Error('New release source is older than or diverges from a successful product release.');
    if (ancestor(snapshot.sourceSha, sourceSha)) snapshots.push(snapshot);
  }
  return snapshots.sort((a, b) => a.date.localeCompare(b.date) || a.tag.localeCompare(b.tag));
}
export async function reconcileReleases(api, state, sourceSha, releaseProduct) {
  for (const snapshot of await successfulSnapshots(api, state.baselineSha, sourceSha, releaseProduct)) applyRelease(state, snapshot);
  return state;
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
