import { execFileSync, spawnSync } from 'node:child_process';
import { appendFileSync } from 'node:fs';
import { runGh, ghEnvironment } from './gh-cli.mjs';
import { classifyProducts, classifyCategory, products, historyBaseline, parseTag, compareVersions } from './model.mjs';

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
  async createRelease({ tag, title, notes, prerelease, packagePath, symbolPath }) {
    const args = ['release', 'create', tag, packagePath, symbolPath, '--repo', this.qualifiedRepo, '--verify-tag', '--title', title, '--notes-file', '-', `--latest=${!prerelease && tag.startsWith('core-v')}`];
    if (prerelease) args.push('--prerelease');
    await this.cli(args, notes);
  }
  async upsertLabel(label) {
    await this.cli(['label', 'create', label.name, '--repo', this.qualifiedRepo, '--color', label.color, '--description', label.description, '--force']);
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
// Release bodies are the published history; unmerged/failed tags are not a boundary.
export function productReleases(releases, product) {
  return releases.filter(release => !release.draft && release.tag_name.startsWith(product + '-v'))
    .sort((a, b) => compareVersions(parseTag(b.tag_name).version, parseTag(a.tag_name).version));
}
export async function collectChanges(api, sourceSha, releases, selectedProduct) {
  const selected = selectedProduct ? [selectedProduct] : Object.keys(products);
  const boundaries = selected.map(product => productReleases(releases, product)[0]?.tag_name ?? historyBaseline);
  const ranges = Object.fromEntries(selected.map((product, index) => {
    const ref = boundaries[index];
    if (!ancestor(ref, sourceSha)) throw new Error('Source predates a successful product release. Select current master.');
    return [product, new Set(git('rev-list', `${ref}..${sourceSha}`).split('\n').filter(Boolean))];
  }));
  const oldest = boundaries.length === 1 ? boundaries[0] : git('merge-base', ...boundaries);
  const entries = new Map();
  for (const sha of git('rev-list', '--reverse', '--first-parent', `${oldest}..${sourceSha}`).split('\n').filter(Boolean)) {
    for (const pr of await api.list(`/commits/${sha}/pulls`)) {
      if (!pr.merged_at || !pr.merge_commit_sha || pr.base?.ref !== 'master' || entries.has(pr.number) || !ancestor(pr.merge_commit_sha, sourceSha)) continue;
      const pendingProducts = classifyProducts(pr).filter(product => ranges[product]?.has(pr.merge_commit_sha));
      if (!pendingProducts.length) continue;
      entries.set(pr.number, { number: pr.number, products: pendingProducts, ...classifyCategory(pr) });
    }
  }
  return [...entries.values()];
}
export async function validatePulls(api, event) {
  const number = event.pull_request?.number ?? event.inputs?.pr_number;
  if (!/^[1-9]\d*$/.test(String(number)) || !Number.isSafeInteger(Number(number))) throw new Error('A valid PR number is required.');
  return [await api.repo(`/pulls/${number}`)];
}
