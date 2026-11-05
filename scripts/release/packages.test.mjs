import test from 'node:test';
import assert from 'node:assert/strict';
import { execFileSync, spawnSync } from 'node:child_process';
import { mkdtempSync, mkdirSync, writeFileSync, readFileSync, rmSync, copyFileSync } from 'node:fs';
import { tmpdir } from 'node:os';
import { join } from 'node:path';
import { fileURLToPath } from 'node:url';

// Explicit stdio prevents execFileSync from echoing expected failures into CI logs.
// Failed commands still throw with status/stdout/stderr for assertions and diagnostics.
const runCaptured = (command, args, options = {}) => execFileSync(command, args, {
  ...options, encoding: 'utf8', stdio: ['ignore', 'pipe', 'pipe'],
});

// These regression tests do not require PowerShell, so capture/fail-fast behavior is
// exercised locally as well as on the hosted runners that run the package fixtures.
test('expected child failures are captured without printing raw exceptions', () => {
  const child = spawnSync(process.execPath, ['--input-type=module', '-e', `
    import assert from 'node:assert/strict';
    import { execFileSync } from 'node:child_process';
    const runCaptured = ${runCaptured.toString()};
    assert.throws(() => runCaptured(process.execPath, ['-e',
      'process.stderr.write("expected validation error"); process.exit(1);']), error => {
      assert.equal(error.status, 1);
      assert.match(error.stderr, /expected validation error/);
      return true;
    });
  `], { encoding: 'utf8' });
  assert.equal(child.status, 0, child.stderr);
  assert.equal(child.stderr, '', 'Expected negative tests must not leak exception logs');
});

test('unexpected captured child failures still fail the invoking process', () => {
  const child = spawnSync(process.execPath, ['--input-type=module', '-e', `
    import { execFileSync } from 'node:child_process';
    const runCaptured = ${runCaptured.toString()};
    runCaptured(process.execPath, ['-e',
      'process.stderr.write("unexpected validation error"); process.exit(7);']);
  `], { encoding: 'utf8' });
  assert.notEqual(child.status, 0);
  assert.match(child.stderr, /unexpected validation error/);
});

const probe = spawnSync('pwsh', ['-NoProfile', '-Command', '$PSVersionTable.PSVersion.ToString()']);
const validator = fileURLToPath(new URL('validate-packages.ps1', import.meta.url));
test('PowerShell package validation checks IDs, symbols, core assets, and signed retry content', { skip: probe.error?.code === 'ENOENT' }, async t => {
  assert.equal(probe.status, 0, probe.stderr?.toString());
  const root = mkdtempSync(join(tmpdir(), 'xrmmockup-packages-'));
  t.after(() => rmSync(root, { recursive: true, force: true }));
  mkdirSync(join(root, '.release/packages'), { recursive: true });
  const fixture = join(root, 'fixture.ps1');
  writeFileSync(fixture, `
param([string]$Id = 'XrmMockup365', [switch]$MissingLegacy, [switch]$Signed)
$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.IO.Compression.FileSystem
foreach ($extension in @('nupkg', 'snupkg')) {
    $path = Join-Path $PWD ".release/packages/XrmMockup365.1.20.3.$extension"
    if (Test-Path $path) { Remove-Item $path }
    $archive = [IO.Compression.ZipFile]::Open($path, [IO.Compression.ZipArchiveMode]::Create)
    try {
        $files = [ordered]@{ 'XrmMockup365.nuspec' = "<package><metadata><id>$Id</id><version>1.20.3</version></metadata></package>" }
        if ($extension -eq 'nupkg' -and -not $MissingLegacy) {
            foreach ($framework in @('net462', 'net10.0')) {
                $files["content/$framework/Metadata/MetadataGenerator365.exe"] = 'legacy bytes'
                $files["content/$framework/Metadata/MetadataGenerator365.exe.config"] = 'config'
                $files["content/$framework/Metadata/TypeDeclarations.cs"] = 'context'
            }
        }
        if ($Signed -and $extension -eq 'nupkg') { $files['.signature.p7s'] = 'repository signature' }
        foreach ($name in $files.Keys) {
            $writer = [IO.StreamWriter]::new($archive.CreateEntry($name).Open())
            try { $writer.Write($files[$name]) } finally { $writer.Dispose() }
        }
    } finally { $archive.Dispose() }
}
`);
  const run = (path, ...args) => runCaptured('pwsh', ['-NoProfile', '-File', path, ...args], { cwd: root });
  const snapshot = { product: 'core', version: '1.20.3', packages: [] };
  const snapshotPath = join(root, '.release/release-snapshot.json');
  writeFileSync(snapshotPath, JSON.stringify(snapshot));
  run(fixture); run(validator);
  const content = JSON.parse(readFileSync(join(root, '.release/package-content.json'), 'utf8').replace(/^\uFEFF/, ''));
  assert.equal(Object.keys(content).length, 2);
  snapshot.packages = Object.entries(content).map(([name, contentHash]) => ({ name, contentHash }));
  writeFileSync(snapshotPath, JSON.stringify(snapshot));
  run(fixture, '-Signed');
  copyFileSync(join(root, '.release/packages/XrmMockup365.1.20.3.nupkg'), join(root, '.release/existing.nupkg'));
  run(validator, '-Existing'); // Repository signature is excluded, but real content must agree.
  await t.test('rejects a wrong package ID with exit code 1 (expected failure)', () => {
    run(fixture, '-Id', 'Wrong.Package');
    assert.throws(() => run(validator), error => {
      assert.equal(error.status, 1, 'Wrong package metadata must fail the validator');
      assert.match(error.stderr, /Package ID\/version mismatch/);
      return true;
    });
  });
  await t.test('rejects missing legacy assets with exit code 1 (expected failure)', () => {
    run(fixture, '-MissingLegacy');
    assert.throws(() => run(validator), error => {
      assert.equal(error.status, 1, 'Missing legacy assets must fail the validator');
      assert.match(error.stderr, /Missing legacy generator/);
      return true;
    });
  });
});
