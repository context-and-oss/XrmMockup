import { readFileSync, writeFileSync } from 'node:fs';
import { GitHub, git, collectChanges, productReleases, summary } from './github.mjs';
import { products, generatedPaths, changelogBranch, changelogPrMarker, isChangelogPr, renderChangelog } from './model.mjs';

const api = new GitHub();
git('fetch', 'origin', 'master', '--tags');
git('checkout', '-B', 'release-changelog-update', 'origin/master');
const open = await api.list(`/pulls?state=open&base=master&head=${encodeURIComponent(api.repository.split('/')[0] + ':' + changelogBranch)}`);
if (open.length > 1 || (open[0] && !isChangelogPr(open[0]))) throw new Error('Reserved changelog branch has an unexpected PR.');
let pr = open[0];
const ref = await api.repo(`/git/ref/heads/${changelogBranch}`, { allow404: true });
if (ref) {
  git('fetch', 'origin', `+refs/heads/${changelogBranch}:refs/remotes/origin/${changelogBranch}`);
  const paths = git('diff', '--name-only', `origin/master...origin/${changelogBranch}`).split('\n').filter(Boolean);
  if (paths.some(path => !generatedPaths.includes(path))) throw new Error('Changelog branch contains unrelated changes; refusing to overwrite it.');
}
const releases = await api.list('/releases'), entries = await collectChanges(api, git('rev-parse', 'HEAD'), releases);
for (const [product, config] of Object.entries(products)) {
  writeFileSync(config.changelog, renderChangelog(readFileSync(config.changelog, 'utf8'), entries, product, productReleases(releases, product)));
}
if (!git('diff', '--name-only', 'HEAD')) {
  if (pr) await api.closePull(pr.number);
  summary('Changelogs are current; no PR needed.');
} else {
  git('add', '--', ...generatedPaths);
  if (!ref || git('diff', '--cached', '--name-only', ref.object.sha)) {
    git('config', 'user.name', 'github-actions[bot]');
    git('config', 'user.email', '41898282+github-actions[bot]@users.noreply.github.com');
    git('commit', '-m', 'Chore: Update generated product changelogs', '-m', changelogPrMarker);
    git('push', `--force-with-lease=refs/heads/${changelogBranch}:${ref?.object.sha ?? ''}`, 'origin', `HEAD:refs/heads/${changelogBranch}`);
  }
  const title = 'Chore: Update generated product changelogs';
  const body = `${changelogPrMarker}\n\nGenerated from merged PRs and published releases. Review and merge before tagging. Product:none excludes this PR from release notes. Further master merges update this same PR.`;
  if (pr) await api.editPull(pr, title, body);
  else pr = await api.createPull({ title, body, head: changelogBranch, base: 'master' });
  for (const workflow of ['pr-product-labels.yml', 'pr-change-category.yml']) await api.dispatch(workflow, 'master', { pr_number: String(pr.number) });
  await api.dispatch('ci.yml', changelogBranch);
  summary(`Updated changelog PR #${pr.number}; master was not pushed.`);
}
