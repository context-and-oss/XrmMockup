import test from 'node:test';
import assert from 'node:assert/strict';
import { readFileSync, existsSync } from 'node:fs';
import { fileURLToPath } from 'node:url';
const root = fileURLToPath(new URL('../../', import.meta.url));
const read = path => readFileSync(root + path, 'utf8');
test('privileged PR validation uses only trusted metadata scripts and publishes explicit head checks', () => {
  for (const name of ['pr-product-labels', 'pr-change-category']) {
    const yaml = read(`.github/workflows/${name}.yml`);
    assert.match(yaml, /pull_request_target:/); assert.match(yaml, /checks: write/);
    assert.match(yaml, /ref: \$\{\{ github\.event\.repository\.default_branch \}\}/);
    assert.match(yaml, /labeled, unlabeled, edited/); assert.match(yaml, /merge_group:/);
    assert.ok(!yaml.includes('head.ref')); assert.ok(!yaml.includes('dotnet'));
  }
  const script = read('scripts/release/check-pr.mjs');
  assert.match(script, /head_sha: headSha/);
});
test('tag release separates metadata preparation, read-only builds, and dry-run-gated publication', () => {
  const yaml = read('.github/workflows/release.yml');
  assert.match(yaml, /'core-v\*'/); assert.match(yaml, /'metadata-v\*'/);
  assert.ok(!yaml.includes('types: [released]'));
  const build = yaml.slice(yaml.indexOf('  build:\n'), yaml.indexOf('  publish:\n'));
  assert.ok(!build.includes('contents: write')); assert.ok(!build.includes('id-token: write'));
  const publish = yaml.slice(yaml.indexOf('  publish:\n'));
  assert.match(publish, /if: needs\.build\.outputs\.dry_run == 'false'/);
  assert.ok(publish.indexOf('publish-release.mjs complete') < publish.indexOf('publish-release.mjs notify'));
  assert.ok(!yaml.includes('--skip-duplicate'));
});
test('writer is serialized, uses the repository token for PRs, and generated source is not ignored', () => {
  const yaml = read('.github/workflows/update-changelogs.yml');
  assert.match(yaml, /group: release-changelog-writer/); assert.match(yaml, /cancel-in-progress: false/);
  assert.match(yaml, /pull-requests: write/); assert.match(yaml, /actions: write/);
  assert.match(yaml, /GH_TOKEN: \$\{\{ github\.token \}\}/);
  assert.ok(!yaml.includes('RELEASE_APP')); assert.ok(!yaml.includes('RELEASE_BOT_TOKEN'));
  assert.ok(!yaml.includes('create-github-app-token'));
  assert.match(read('.gitignore'), /!\/scripts\/release\//);
});
test('CI/local pack no longer derives versions from changelog headings', () => {
  assert.ok(!read('.github/workflows/ci.yml').includes('Set-VersionFromChangelog'));
  assert.ok(!read('scripts/Pack-Local.ps1').includes('Set-VersionFromChangelog'));
  assert.ok(!existsSync(root + 'scripts/Set-VersionFromChangelog.ps1'));
  assert.match(read('src/XrmMockup365/XrmMockup365.csproj'), /CoreReleaseVersion/);
  assert.match(read('src/MetadataGen/Directory.Build.props'), /MetadataReleaseVersion/);
});
test('release automation workflows consistently pin Node 24', () => {
  for (const name of ['ci', 'release', 'pr-product-labels', 'pr-change-category', 'setup-release-labels', 'update-changelogs']) {
    const pins = [...read(`.github/workflows/${name}.yml`).matchAll(/node-version:\s*['"]?(\d+)/g)].map(match => match[1]);
    assert.ok(pins.length > 0, `${name} must explicitly configure Node`);
    assert.ok(pins.every(version => version === '24'), `${name} must use Node 24 throughout`);
  }
});
test('privileged preparation and publication run default-branch helpers over isolated source data', () => {
  const yaml = read('.github/workflows/release.yml');
  const prepare = yaml.slice(yaml.indexOf('  prepare:\n'), yaml.indexOf('  build:\n'));
  assert.match(prepare, /permissions:\n      contents: write\n      pull-requests: read/);
  for (const section of [prepare, yaml.slice(yaml.indexOf('  publish:\n'))]) {
    assert.match(section, /ref: \$\{\{ github\.event\.repository\.default_branch \}\}\n          path: release-tools\n          persist-credentials: false/);
    assert.match(section, /path: release-source\n          fetch-depth: 0\n          persist-credentials: false/);
    assert.match(section, /working-directory: release-source/);
    assert.ok(!section.includes('run: node scripts/release/'), 'Do not execute scripts from the selected source with a privileged token');
  }
  assert.match(prepare, /run: node \.\.\/release-tools\/scripts\/release\/prepare-release\.mjs/);
  assert.match(yaml, /run: node \.\.\/release-tools\/scripts\/release\/publish-release\.mjs/);
});
test('changelog PR checks are explicitly dispatchable and the writer never pushes master or merges PRs', () => {
  for (const name of ['pr-product-labels', 'pr-change-category']) {
    const yaml = read(`.github/workflows/${name}.yml`); assert.match(yaml, /workflow_dispatch:/); assert.match(yaml, /pr_number:/);
  }
  assert.match(read('.github/workflows/ci.yml'), /workflow_dispatch:/);
  const script = read('scripts/release/update-changelogs.mjs');
  assert.ok(!script.includes('HEAD:refs/heads/master')); assert.ok(!script.includes('/merge'));
  assert.match(script, /state=open&base=master&head=/); assert.match(script, /force-with-lease/);
  assert.match(read('scripts/release/github.mjs'), /'--label', 'product:none', '--label', 'change:chore'/);
});
test('GitHub transport delegates to gh', () => {
  const client = read('scripts/release/github.mjs');
  assert.ok(!client.includes('fetch(')); assert.ok(!client.includes('authorization:'));
  assert.match(client, /'pr', 'create'/); assert.match(client, /'release', 'upload'/);
  assert.match(client, /'workflow', 'run'/); assert.match(client, /'label', 'create'/);
});
