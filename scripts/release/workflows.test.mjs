// These are security contracts of the deployment boundary, not formatting checks.
// Release behavior, CLI arguments and runtime versions are tested elsewhere or
// validated by the workflow schema, rather than mirrored as source-text assertions.
import test from 'node:test';
import assert from 'node:assert/strict';
import { readFileSync } from 'node:fs';
import { fileURLToPath } from 'node:url';
const root = fileURLToPath(new URL('../../', import.meta.url));
const read = path => readFileSync(root + path, 'utf8');

test('privileged PR checks inspect metadata with trusted default-branch code', () => {
  for (const name of ['pr-product-labels', 'pr-change-category']) {
    const workflow = read(`.github/workflows/${name}.yml`);
    assert.match(workflow, /ref: \$\{\{ github\.event\.repository\.default_branch \}\}/);
    assert.ok(!workflow.includes('head.ref'));
    assert.ok(!workflow.includes('dotnet'));
  }
});

test('publication is dry-run gated and executes trusted helpers, not selected source code', () => {
  const workflow = read('.github/workflows/release.yml');
  const build = workflow.slice(workflow.indexOf('  build:\n'), workflow.indexOf('  publish:\n'));
  assert.ok(!build.includes('contents: write') && !build.includes('id-token: write'));
  const publish = workflow.slice(workflow.indexOf('  publish:\n'));
  assert.match(publish, /if: needs\.build\.outputs\.dry_run == 'false'/);
  for (const section of [workflow.slice(workflow.indexOf('  prepare:\n'), workflow.indexOf('  build:\n')), publish]) {
    assert.match(section, /ref: \$\{\{ github\.event\.repository\.default_branch \}\}/);
    assert.match(section, /run: node \.\.\/release-tools\/scripts\/release\//);
    assert.ok(!section.includes('run: node scripts/release/'));
  }
});
