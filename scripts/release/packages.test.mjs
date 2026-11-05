import test from 'node:test';
import assert from 'node:assert/strict';
import { execFileSync, spawnSync } from 'node:child_process';
import { mkdtempSync, mkdirSync, writeFileSync, rmSync } from 'node:fs';
import { tmpdir } from 'node:os';
import { join } from 'node:path';
import { fileURLToPath } from 'node:url';
const probe = spawnSync('pwsh', ['-NoProfile', '-Command', '$PSVersionTable.PSVersion.ToString()']);
test('package validation accepts a matching package/symbol pair and rejects missing or incorrect artifacts', { skip: probe.error?.code === 'ENOENT' }, t => {
  const root = mkdtempSync(join(tmpdir(), 'xrmmockup-package-')); t.after(() => rmSync(root, { recursive: true, force: true }));
  mkdirSync(join(root, '.release/packages'), { recursive: true });
  writeFileSync(join(root, '.release/release.json'), JSON.stringify({ product: 'core', version: '1.20.3', tag: 'core-v1.20.3' }));
  const fixture = join(root, 'fixture.ps1');
  writeFileSync(fixture, `
param([string]$Version = '1.20.3')
Add-Type -AssemblyName System.IO.Compression.FileSystem
foreach ($extension in @('nupkg', 'snupkg')) {
  $path = Join-Path $PWD ".release/packages/XrmMockup365.1.20.3.$extension"
  if (Test-Path $path) { Remove-Item $path }
  $zip = [IO.Compression.ZipFile]::Open($path, [IO.Compression.ZipArchiveMode]::Create)
  try {
    $writer = [IO.StreamWriter]::new($zip.CreateEntry('XrmMockup365.nuspec').Open())
    try { $writer.Write("<package><metadata><id>XrmMockup365</id><version>$Version</version></metadata></package>") } finally { $writer.Dispose() }
  } finally { $zip.Dispose() }
}
`);
  const validator = fileURLToPath(new URL('validate-packages.ps1', import.meta.url));
  const run = (path, ...args) => execFileSync('pwsh', ['-NoProfile', '-File', path, ...args], { cwd: root, env: { ...process.env, RELEASE_TAG: '' }, encoding: 'utf8', stdio: 'pipe' });
  run(fixture); run(validator);
  run(fixture, '-Version', '1.20.4'); assert.throws(() => run(validator), /ID\/version mismatch/);
  run(fixture); rmSync(join(root, '.release/packages/XrmMockup365.1.20.3.snupkg'));
  assert.throws(() => run(validator), /matching symbols/);
  run(fixture); writeFileSync(join(root, '.release/packages/another.nupkg'), 'unexpected');
  assert.throws(() => run(validator), /only the selected product/);
});
