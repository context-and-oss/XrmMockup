import test from 'node:test';
import assert from 'node:assert/strict';
import { execFileSync, spawnSync } from 'node:child_process';
import { mkdtempSync, mkdirSync, writeFileSync, readFileSync, rmSync, copyFileSync } from 'node:fs';
import { tmpdir } from 'node:os';
import { join } from 'node:path';
import { fileURLToPath } from 'node:url';

const probe = spawnSync('pwsh', ['-NoProfile', '-Command', '$PSVersionTable.PSVersion.ToString()']);
const validator = fileURLToPath(new URL('validate-packages.ps1', import.meta.url));
test('PowerShell package validation checks IDs, symbols, core assets, and signed retry content', { skip: probe.error?.code === 'ENOENT' }, t => {
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
  const run = (path, ...args) => execFileSync('pwsh', ['-NoProfile', '-File', path, ...args], { cwd: root, encoding: 'utf8' });
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
  run(fixture, '-Id', 'Wrong.Package'); assert.throws(() => run(validator), /ID\/version mismatch/);
  run(fixture, '-MissingLegacy'); assert.throws(() => run(validator), /Missing legacy generator/);
});
