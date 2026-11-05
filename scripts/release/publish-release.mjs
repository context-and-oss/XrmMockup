// Called only after the workflow's single NuGet push succeeds. Failures are manual recovery.
import { readFileSync } from 'node:fs';
import { GitHub, git, fetchHistory, ancestor, summary } from './github.mjs';
import { products, validateRelease } from './model.mjs';

const api = new GitHub(), release = validateRelease(JSON.parse(readFileSync('.release/release.json', 'utf8')));
if (release.tag !== process.env.RELEASE_TAG) throw new Error('Artifact does not match the workflow-selected release.');
fetchHistory();
if (!ancestor(release.sourceSha, 'origin/master') || git('rev-parse', `refs/tags/${release.tag}^{commit}`) !== release.sourceSha) throw new Error('Release source/tag changed; recover using the saved artifacts.');
await api.createRelease({ tag: release.tag, title: `${release.product === 'core' ? 'XrmMockup365' : 'MetadataGenerator'} ${release.version}`,
  notes: readFileSync('.release/notes.md', 'utf8'), prerelease: release.prerelease,
  packagePath: `.release/packages/${products[release.product].packageId}.${release.version}.nupkg`,
  symbolPath: `.release/packages/${products[release.product].packageId}.${release.version}.snupkg` });
await api.dispatch('update-changelogs.yml', 'master');
summary(`Published GitHub release ${release.tag}; requested its changelog PR update.`);
