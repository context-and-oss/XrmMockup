import { readFileSync, writeFileSync } from 'node:fs';
import { GitHub, git, discoverPrs, reconcileReleases, readState, writeState, summary } from './github.mjs';
import { products, generatedPaths, renderChangelog, validateState } from './model.mjs';

const api = new GitHub();
// All writers share workflow concurrency; retry against latest master on push races.
for (let attempt = 0; attempt < 3; attempt++) {
  git('fetch', 'origin', 'master', '--tags');
  git('checkout', '-B', 'release-changelog-update', 'origin/master');
  const sourceSha = git('rev-parse', 'HEAD');
  const state = validateState(readState());
  await discoverPrs(api, state, sourceSha);
  await reconcileReleases(api, state, sourceSha);
  validateState(state);
  for (const [product, config] of Object.entries(products)) {
    writeFileSync(config.changelog, renderChangelog(readFileSync(config.changelog, 'utf8'), state, product));
  }
  writeState(state);
  const changed = git('diff', '--name-only', 'HEAD').split('\n').filter(Boolean);
  if (!changed.length) { summary('Changelogs and release history are already current.'); break; }
  if (changed.some(path => !generatedPaths.includes(path))) throw new Error('Refusing to commit an unexpected generated path.');
  git('config', 'user.name', 'release-changelog-bot');
  git('config', 'user.email', 'release-changelog-bot@users.noreply.github.com');
  git('add', '--', ...generatedPaths);
  git('commit', '-m', 'Chore: Reconcile generated product changelogs');
  try {
    git('push', 'origin', 'HEAD:refs/heads/master');
    summary(`Updated tracked changelogs from ${sourceSha}.`);
    break;
  } catch (error) {
    if (attempt === 2) throw error;
    console.log('Master advanced or push failed; retrying reconciliation against current master.');
  }
}
