// Deployment contracts: privileged operations must not run unmerged source code,
// and a failed package push must stop before creating the GitHub release.
import test from 'node:test';
import assert from 'node:assert/strict';
import { readFileSync } from 'node:fs';
import { fileURLToPath } from 'node:url';
const root = fileURLToPath(new URL('../../', import.meta.url));
const read = name => readFileSync(root + `.github/workflows/${name}.yml`, 'utf8');
test('metadata validators execute trusted default-branch code', () => {
  for (const name of ['pr-product-labels', 'pr-change-category']) {
    const workflow = read(name); assert.match(workflow, /github\.event\.repository\.default_branch/);
    assert.ok(!workflow.includes('head.ref') && !workflow.includes('dotnet'));
  }
});
test('release uses one NuGet push, gates publishing on dry-run, and keeps write privileges out of builds', () => {
  const workflow = read('release'), build = workflow.slice(workflow.indexOf('  build:'), workflow.indexOf('  publish:'));
  const publish = workflow.slice(workflow.indexOf('  publish:'));
  assert.ok(!build.includes('contents: write') && !build.includes('id-token: write'));
  assert.match(publish, /dry_run == 'false'/);
  assert.match(publish, /github\.event\.repository\.default_branch/);
  assert.equal((publish.match(/dotnet nuget push/g) ?? []).length, 1);
  assert.ok(!publish.includes('--no-symbols') && !publish.includes('--skip-duplicate'));
  assert.match(publish, /if \(\$LASTEXITCODE -ne 0\) \{ exit \$LASTEXITCODE \}/);
  assert.ok(publish.indexOf('dotnet nuget push') < publish.indexOf('publish-release.mjs'));
});
