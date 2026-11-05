import { execFileSync, spawnSync } from 'node:child_process';
import { readFileSync, writeFileSync, appendFileSync, mkdtempSync, rmSync } from 'node:fs';
import { tmpdir } from 'node:os';
import { basename, join } from 'node:path';
import { runGh, ghEnvironment } from './gh-cli.mjs';
import { isDeepStrictEqual } from 'node:util';
import { hash, classify, products, generatedPaths, statePath, snapshotName, releaseMarker, parseTag, validateSnapshot, applyRelease, compareVersions, makeSnapshot, isChangelogPr } from './model.mjs';

export function git(...args) {
  return execFileSync('git', args, { encoding: 'utf8', maxBuffer: 20 * 1024 * 1024 }).replace(/\r\n/g, '\n').trim();
}
// Let gh handle Git credentials without persisting a write token in the source checkout.
export function fetchHistory() {
  const host = new URL(process.env.GITHUB_SERVER_URL ?? 'https://github.com').host;
  const env = ghEnvironment(process.env.GH_TOKEN ?? process.env.GITHUB_TOKEN, host);
  const count = Number(env.GIT_CONFIG_COUNT ?? 0);
  if (!Number.isSafeInteger(count) || count < 0) throw new Error('Invalid Git configuration environment.');
  env.GIT_CONFIG_COUNT = String(count + 2);
  for (let i = 0; i < 2; i++) env[`GIT_CONFIG_KEY_${count + i}`] = `credential.https://${host}.helper`;
  env[`GIT_CONFIG_VALUE_${count}`] = '';
  env[`GIT_CONFIG_VALUE_${count + 1}`] = '!gh auth git-credential';
  return execFileSync('git', ['fetch', 'origin', 'master', '--tags'], { env, encoding: 'utf8', maxBuffer: 20 * 1024 * 1024, stdio: ['ignore', 'pipe', 'pipe'] }).trim();
}
export function ancestor(a, b) {
  const result = spawnSync('git', ['merge-base', '--is-ancestor', a, b]);
  if (result.status > 1 || result.error) throw new Error(`Unable to verify git ancestry: ${a} -> ${b}`);
  return result.status === 0;
}
export class GitHub {
  constructor({ repository = process.env.GITHUB_REPOSITORY, token = process.env.GH_TOKEN ?? process.env.GITHUB_TOKEN,
    runner = runGh, host = new URL(process.env.GITHUB_SERVER_URL ?? 'https://github.com').host, environment = {} } = {}) {
    if (!/^[\w.-]+\/[\w.-]+$/.test(repository ?? '')) throw new Error('GITHUB_REPOSITORY is required.');
    this.repository = repository; this.host = host; this.runner = runner;
    this.environment = ghEnvironment(token, host, environment);
    this.qualifiedRepo = `${host}/${repository}`;
  }
  async cli(args, input) { return this.runner(args, { input, env: this.environment }); }
  async request(path, { method = 'GET', body, raw = false, allow404 = false, headers = {} } = {}) {
    if (!/^\/(?!\/)[^\s]+$/.test(path) || path.includes('://')) throw new Error('Untrusted GitHub API path: only relative endpoints are allowed.');
    if (Buffer.isBuffer(body)) throw new Error('Use gh release upload for binary assets.');
    const args = ['api', path, '--hostname', this.host, '--method', method, '--header', 'X-GitHub-Api-Version:2022-11-28'];
    if (raw) args.push('--header', 'Accept:application/octet-stream');
    for (const [name, value] of Object.entries(headers)) args.push('--header', `${name}:${value}`);
    if (body !== undefined) args.push('--input', '-');
    let bytes;
    try { bytes = await this.cli(args, body === undefined ? undefined : JSON.stringify(body)); }
    catch (error) {
      if (allow404 && (String(error.stderr ?? '').trim().match(/\(HTTP (\d{3})\)$/)?.[1] === '404' || /^HTTP 404:/.test(String(error.stderr ?? '').trim()))) return null;
      throw error;
    }
    if (raw) return Buffer.from(bytes);
    const text = bytes.toString('utf8').trim();
    return text ? JSON.parse(text) : null; // gh returns no body for a 204 response.
  }
  repo(path, options) { return this.request(`/repos/${this.repository}${path}`, options); }
  async list(path, selector = '.[] | @json') {
    const endpoint = `/repos/${this.repository}${path}${path.includes('?') ? '&' : '?'}per_page=100`;
    // gh follows pagination links. One compact JSON record per line also works
    // with older gh versions that do not yet provide --slurp.
    const bytes = await this.cli(['api', endpoint, '--hostname', this.host, '--method', 'GET', '--paginate', '--jq', selector, '--header', 'X-GitHub-Api-Version:2022-11-28']);
    return bytes.toString('utf8').split(/\r?\n/).filter(Boolean).map(line => JSON.parse(line));
  }
  async mergeQueueEntries(branch) {
    const [owner, name] = this.repository.split('/');
    const query = 'query($owner:String!,$name:String!,$branch:String!,$endCursor:String){repository(owner:$owner,name:$name){mergeQueue(branch:$branch){entries(first:100,after:$endCursor){nodes{headCommit{oid} pullRequest{number headRefOid}} pageInfo{hasNextPage endCursor}}}}}';
    const bytes = await this.cli(['api', 'graphql', '--hostname', this.host, '--paginate',
      '--raw-field', `query=${query}`, '--raw-field', `owner=${owner}`, '--raw-field', `name=${name}`, '--raw-field', `branch=${branch}`,
      '--jq', '.data.repository.mergeQueue.entries.nodes[] | @json']);
    return bytes.toString('utf8').split(/\r?\n/).filter(Boolean).map(line => JSON.parse(line));
  }
  async releaseByTag(tag) {
    const matches = (await this.list('/releases')).filter(release => release.tag_name === tag);
    if (matches.length > 1) throw new Error(`Multiple releases/drafts use tag ${tag}; resolve the ambiguity before retrying.`);
    return matches[0] ?? null;
  }
  async createPull({ title, body, head, base }) {
    const bytes = await this.cli(['pr', 'create', '--repo', this.qualifiedRepo, '--head', head, '--base', base,
      '--title', title, '--body-file', '-', '--label', 'product:none', '--label', 'change:chore'], body);
    const url = new URL(bytes.toString('utf8').trim().split(/\r?\n/).at(-1));
    const match = /^\/([^/]+)\/([^/]+)\/pull\/(\d+)$/.exec(url.pathname);
    if (url.host !== this.host || !match || `${match[1]}/${match[2]}` !== this.repository) throw new Error('gh pr create returned an unexpected repository/PR URL.');
    return this.repo(`/pulls/${match[3]}`);
  }
  async editPull(pr, title, body) {
    const args = ['pr', 'edit', String(pr.number), '--repo', this.qualifiedRepo, '--title', title, '--body-file', '-'];
    const unwanted = (pr.labels ?? []).map(label => label.name).filter(name => /^(product|change):/.test(name) && !['product:none', 'change:chore'].includes(name));
    for (const name of unwanted) args.push('--remove-label', name);
    args.push('--add-label', 'product:none', '--add-label', 'change:chore');
    await this.cli(args, body);
  }
  async closePull(number) { await this.cli(['pr', 'close', String(number), '--repo', this.qualifiedRepo]); }
  async dispatch(workflow, ref, inputs) {
    const args = ['workflow', 'run', workflow, '--repo', this.qualifiedRepo, '--ref', ref];
    if (inputs) args.push('--json');
    await this.cli(args, inputs ? JSON.stringify(inputs) : undefined);
  }
  async createRelease({ tag, title, notes, prerelease }) {
    const args = ['release', 'create', tag, '--repo', this.qualifiedRepo, '--verify-tag', '--draft', '--title', title, '--notes-file', '-'];
    if (prerelease) args.push('--prerelease');
    await this.cli(args, notes);
    return this.releaseByTag(tag);
  }
  async publishRelease(tag, latest) {
    await this.cli(['release', 'edit', tag, '--repo', this.qualifiedRepo, '--draft=false', `--latest=${latest}`]);
  }
  async uploadAsset(tag, name, bytes) {
    if (basename(name) !== name || /[\/\\#]/.test(name) || name.startsWith('-')) throw new Error('Invalid release asset filename.');
    const directory = mkdtempSync(join(tmpdir(), 'xrmmockup-gh-upload-'));
    try {
      const path = join(directory, name); writeFileSync(path, bytes);
      // No --clobber: the caller verifies matching existing bytes or fails first.
      await this.cli(['release', 'upload', tag, path, '--repo', this.qualifiedRepo]);
    } finally { rmSync(directory, { recursive: true, force: true }); }
  }
  async upsertLabel(label) {
    await this.cli(['label', 'create', label.name, '--repo', this.qualifiedRepo, '--color', label.color, '--description', label.description, '--force']);
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
export function generatedCommit(sha) {
  const identity = 'release-changelog-bot <release-changelog-bot@users.noreply.github.com>';
  // Verify owned branch commits and historical direct-writer commits. A PR
  // merge can rewrite these identities and is verified through GitHub metadata.
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
  const associations = new Map();
  const prsFor = async sha => {
    if (!associations.has(sha)) associations.set(sha, await api.list(`/commits/${sha}/pulls`));
    return associations.get(sha);
  };
  const generatedPulls = new Map();
  const generatedPrFor = async sha => {
    if (generatedPulls.has(sha)) return generatedPulls.get(sha);
    const candidates = (await prsFor(sha)).filter(pr => pr.merge_commit_sha === sha && pr.merged_at && isChangelogPr(pr));
    let result = null;
    for (const candidate of candidates) {
      const pr = await api.repo(`/pulls/${candidate.number}`);
      if (!pr.merged || pr.base.ref !== 'master' || pr.base.repo.full_name !== api.repository || !isChangelogPr(pr)) continue;
      classify(pr); // product:none and Chore remain mandatory, never a blanket bypass.
      const parent = git('rev-list', '--parents', '-n', '1', sha).split(' ')[1];
      const paths = parent ? git('diff', '--name-only', parent, sha).split('\n').filter(Boolean) : [];
      if (!paths.length || paths.some(path => !generatedPaths.includes(path))) throw new Error('Changelog PR contains changes outside its generated-file allowlist.');
      result = pr; break;
    }
    generatedPulls.set(sha, result); return result;
  };
  // A PR merge can rewrite author/committer metadata. Verify its actual GitHub PR
  // identity and changed paths instead of treating it as a direct bot commit.
  const ledgerCommit = git('log', '--first-parent', '-1', '--format=%H', sourceSha, '--', statePath);
  const parent = ledgerCommit && git('rev-list', '--parents', '-n', '1', ledgerCommit).split(' ')[1];
  const trustedCheckpoint = checkpoint === state.baselineSha || (parent && ancestor(checkpoint, parent) &&
    (generatedCommit(ledgerCommit) || await generatedPrFor(ledgerCommit)));
  const from = trustedCheckpoint ? checkpoint : state.baselineSha;
  const reviewed = new Map();
  const approveCorrection = async sha => {
    const correction = state.corrections?.[sha];
    if (!correction) throw new Error(`Commit ${sha} has no classified merged master PR. Record a reviewed SHA-keyed emergency correction.`);
    const pr = await api.repo(`/pulls/${correction.reviewPr}`);
    if (!pr.merged || pr.base?.ref !== 'master' || pr.base.repo?.full_name !== api.repository || isChangelogPr(pr) ||
        !/^[a-f0-9]{40}$/.test(pr.merge_commit_sha ?? '') || pr.merge_commit_sha === sha ||
        !ancestor(sha, pr.merge_commit_sha) || !ancestor(pr.merge_commit_sha, sourceSha)) throw new Error('Emergency correction must be reviewed in a later merged master PR in this repository.');
    classify(pr); // Classification and release-note text come from the corrective PR.
    const atReview = JSON.parse(git('show', `${pr.merge_commit_sha}:${statePath}`));
    const parent = git('rev-list', '--parents', '-n', '1', pr.merge_commit_sha).split(' ')[1];
    const before = parent && git('ls-tree', '--name-only', parent, statePath)
      ? JSON.parse(git('show', `${parent}:${statePath}`)).corrections?.[sha] : undefined;
    if (!isDeepStrictEqual(atReview.corrections?.[sha], correction) || isDeepStrictEqual(before, correction)) throw new Error('Emergency correction was not introduced or updated by its referenced review PR.');
    reviewed.set(pr.merge_commit_sha, pr);
  };
  const known = new Set(state.entries.map(entry => entry.number));
  const commits = git('rev-list', '--reverse', '--first-parent', `${from}..${sourceSha}`).split('\n').filter(Boolean);
  let substantiveHistory = false;
  for (const sha of commits) {
    // Historical direct bot commits can be verified locally. Current changelog
    // PR merges are verified once per scan and excluded without a follow-up row.
    if (generatedCommit(sha)) continue;
    const associated = await prsFor(sha);
    if (await generatedPrFor(sha)) continue; // Do not add a ledger row/checkpoint update for the notes PR itself.
    const eligible = associated.filter(pr => pr.base?.ref === 'master' && pr.merged_at && pr.merge_commit_sha &&
      ancestor(pr.merge_commit_sha, sourceSha) && !ancestor(pr.merge_commit_sha, state.baselineSha));
    if (!eligible.length && reviewed.has(sha)) eligible.push(reviewed.get(sha));
    if (!eligible.length) { await approveCorrection(sha); substantiveHistory = true; continue; }
    substantiveHistory = true;
    for (const candidate of eligible.sort((a, b) => a.number - b.number)) {
      if (known.has(candidate.number)) continue;
      const pr = reviewed.get(sha) ?? await api.repo(`/pulls/${candidate.number}`);
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
  if (event.inputs?.pr_number !== undefined) {
    if (!/^[1-9]\d*$/.test(event.inputs.pr_number) || !Number.isSafeInteger(Number(event.inputs.pr_number))) throw new Error('A valid PR number is required.');
    return [await api.repo(`/pulls/${event.inputs.pr_number}`)];
  }
  if (event.pull_request) return [await api.repo(`/pulls/${event.pull_request.number}`)];
  if (!event.merge_group) throw new Error('Expected a pull_request_target or merge_group event.');
  const group = event.merge_group;
  if (group.base_ref && group.base_ref !== 'refs/heads/master') throw new Error('Only master merge groups are supported.');
  const commits = new Set((await api.list(`/compare/${group.base_sha}...${group.head_sha}`, '.commits[] | @json')).map(commit => commit.sha));
  const entries = await api.mergeQueueEntries('master');
  // Queue build heads, not arbitrary PRs sharing a commit, identify this group.
  if (!entries.some(entry => entry?.headCommit?.oid === group.head_sha) || !commits.has(group.head_sha)) throw new Error('Cannot identify the current merge group; its queue membership may have changed.');
  const members = entries.filter(entry => entry && commits.has(entry.headCommit?.oid));
  if (!members.length || members.some(entry => !entry.pullRequest)) throw new Error('Cannot identify PRs in the merge group.');
  return Promise.all(members.map(async entry => {
    const pr = await api.repo(`/pulls/${entry.pullRequest.number}`);
    if (pr.merged || pr.merged_at || pr.base?.ref !== 'master' || pr.head.sha !== entry.pullRequest.headRefOid) throw new Error('Merge group PR changed while resolving queue membership; retry the current group.');
    return pr;
  }));
}
