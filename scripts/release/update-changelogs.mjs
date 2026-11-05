import { readFileSync, writeFileSync } from 'node:fs';
import { GitHub, git, ancestor, generatedCommit, discoverPrs, reconcileReleases, readState, writeState, summary } from './github.mjs';
import { products, generatedPaths, statePath, changelogBranch, changelogPrMarker, isChangelogPr, renderChangelog, validateState } from './model.mjs';

const api = new GitHub();
const remoteBranch = `refs/heads/${changelogBranch}`;
const localBranch = `refs/remotes/origin/${changelogBranch}`;
const title = 'Chore: Update generated product changelogs';
const body = `${changelogPrMarker}\n\nGenerated from merged PRs and successful product releases.\n\nReview and merge this PR before tagging a release. It changes only the two changelogs and the release ledger. Do not edit its generated branch directly; correct pending notes through a reviewed source/ledger change.\n\nThis PR is tagged product:none and is excluded from release notes and the changelog ledger.`;

// A single writer owns this branch. Leases protect against concurrent edits; master
// is never pushed, force-updated, approved, or merged by this workflow.
for (let attempt = 0; attempt < 3; attempt++) {
  git('fetch', 'origin', 'master', '--tags');
  git('checkout', '-B', 'release-changelog-update', 'origin/master');
  const sourceSha = git('rev-parse', 'HEAD');
  const open = await api.list(`/pulls?state=open&base=master&head=${encodeURIComponent(api.repository.split('/')[0] + ':' + changelogBranch)}`);
  if (open.length > 1) throw new Error('Multiple open changelog PRs; resolve ambiguity before updating.');
  let pr = open[0];
  if (pr && (!isChangelogPr(pr) || pr.base.repo.full_name !== api.repository)) throw new Error('Refusing to overwrite a non-automation PR on the reserved changelog branch.');
  const ref = await api.repo(`/git/ref/heads/${changelogBranch}`, { allow404: true });
  const previousSha = ref?.object.sha;
  const state = validateState(readState());
  if (previousSha) {
    if (!/^[a-f0-9]{40}$/.test(previousSha)) throw new Error('Invalid remote branch SHA.');
    git('fetch', 'origin', `+${remoteBranch}:${localBranch}`);
    if (git('rev-parse', localBranch) !== previousSha) {
      if (attempt === 2) throw new Error('Changelog branch kept advancing; retry reconciliation.');
      console.log('Branch advanced; retrying.'); continue;
    }
    const base = git('rev-parse', `${previousSha}^1`);
    const paths = git('diff', '--name-only', `origin/master...${previousSha}`).split('\n').filter(Boolean);
    if (!ancestor(base, 'origin/master') || !generatedCommit(previousSha) ||
        !git('show', '-s', '--format=%B', previousSha).includes(changelogPrMarker) || paths.some(path => !generatedPaths.includes(path))) {
      throw new Error('Reserved changelog branch contains unowned or unexpected changes; refusing to overwrite it.');
    }
    // Preserve the first processed metadata while the bot PR awaits review. Main
    // wins for already merged/corrected rows; the branch is not a skip checkpoint.
    const cached = validateState(JSON.parse(git('show', `${previousSha}:${statePath}`)));
    if (cached.baselineSha !== state.baselineSha) throw new Error('Cached changelog branch uses a different migration baseline.');
    for (const entry of cached.entries) {
      if (!ancestor(entry.mergeSha, 'origin/master')) throw new Error('Cached PR metadata is not on master ancestry.');
      if (!state.entries.some(existing => existing.number === entry.number)) state.entries.push(entry);
    }
  }
  await discoverPrs(api, state, sourceSha);
  await reconcileReleases(api, state, sourceSha);
  validateState(state);
  for (const [product, config] of Object.entries(products)) writeFileSync(config.changelog, renderChangelog(readFileSync(config.changelog, 'utf8'), state, product));
  writeState(state);
  const changed = git('diff', '--name-only', 'HEAD').split('\n').filter(Boolean);
  if (changed.some(path => !generatedPaths.includes(path))) throw new Error('Refusing to commit an unexpected generated path.');
  if (!changed.length) {
    if (pr) await api.closePull(pr.number);
    summary('Master changelogs are current; no changelog PR needed.');
    break;
  }
  git('add', '--', ...generatedPaths);
  const branchChanged = !previousSha || git('diff', '--cached', '--name-only', previousSha).length > 0;
  if (branchChanged) {
    git('config', 'user.name', 'release-changelog-bot');
    git('config', 'user.email', 'release-changelog-bot@users.noreply.github.com');
    git('commit', '-m', title, '-m', changelogPrMarker);
    try {
      git('push', `--force-with-lease=${remoteBranch}:${previousSha ?? ''}`, 'origin', `HEAD:${remoteBranch}`);
    } catch (error) {
      if (attempt === 2) throw error;
      console.log('Changelog branch push failed; retrying against current master/branch.');
      continue;
    }
  }
  if (!pr) pr = await api.createPull({ title, body, head: changelogBranch, base: 'master' });
  else await api.editPull(pr, title, body);
  // GITHUB_TOKEN-created events are not a reliable CI trigger. Explicit dispatch
  // runs the real classifiers and read-only build checks without an App or PAT.
  for (const workflow of ['pr-product-labels.yml', 'pr-change-category.yml']) {
    await api.dispatch(workflow, 'master', { pr_number: String(pr.number) });
  }
  await api.dispatch('ci.yml', changelogBranch);
  summary(`Updated changelog PR #${pr.number}. Merge it after required checks/review; master was not pushed.`);
  break;
}
