import { mkdirSync, writeFileSync, readFileSync, readdirSync } from 'node:fs';
import { resolve } from 'node:path';
import { fileURLToPath } from 'node:url';
import { git } from './github.mjs';
import { products, snapshotName, validateSnapshot, hash } from './model.mjs';

export function materialize(snapshot) {
  validateSnapshot(snapshot);
  if (typeof snapshot.generatedChangelog !== 'string' || !snapshot.generatedChangelog.trim()) throw new Error('Generated release changelog is required.');
  mkdirSync('.release/packages', { recursive: true });
  writeFileSync('.release/notes.md', snapshot.notes + '\n');
  writeFileSync(products[snapshot.product].changelog, snapshot.generatedChangelog);
  const escape = text => text.replace(/[&<>"']/g, c => ({ '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;', "'": '&apos;' })[c]);
  const noteProperty = snapshot.product === 'core' ? 'CoreReleaseNotesPath' : 'MetadataReleaseNotesPath';
  const versionProperty = products[snapshot.product].versionProperty;
  // Recreated on each runner: preparation paths on Linux must not leak into Windows MSBuild.
  writeFileSync('.release/version.props', `<Project><PropertyGroup><${versionProperty}>${escape(snapshot.version)}</${versionProperty}><${noteProperty}>${escape(resolve('.release/notes.md'))}</${noteProperty}></PropertyGroup></Project>\n`);
}

function recordManifest() {
  const path = '.release/' + snapshotName;
  const snapshot = validateSnapshot(JSON.parse(readFileSync(path, 'utf8')), process.env.GITHUB_REPOSITORY);
  const content = JSON.parse(readFileSync('.release/package-content.json', 'utf8'));
  const names = readdirSync('.release/packages').sort();
  const stem = `${products[snapshot.product].packageId}.${snapshot.version}`;
  if (JSON.stringify(names) !== JSON.stringify([`${stem}.nupkg`, `${stem}.snupkg`])) throw new Error('Only the selected package and symbol package may be released.');
  const manifest = names.map(name => ({ name, sha512: hash(readFileSync(`.release/packages/${name}`)), contentHash: content[name] }));
  if (manifest.some(p => !/^[a-f0-9]{128}$/.test(p.contentHash ?? ''))) throw new Error('Missing canonical package content hashes.');
  if (snapshot.packages.length && JSON.stringify(manifest) !== JSON.stringify(snapshot.packages)) throw new Error('Frozen package bytes/content changed. Refusing to republish a different artifact.');
  snapshot.packages = manifest;
  validateSnapshot(snapshot);
  writeFileSync(path, JSON.stringify(snapshot, null, 2) + '\n');
}

if (process.argv[1] && fileURLToPath(import.meta.url) === resolve(process.argv[1])) {
  if (process.argv[2] === 'manifest') recordManifest();
  else if (process.argv[2] === 'materialize') {
    const snapshot = validateSnapshot(JSON.parse(readFileSync('.release/' + snapshotName, 'utf8')), process.env.GITHUB_REPOSITORY);
    if (git('rev-parse', 'HEAD') !== snapshot.sourceSha) throw new Error('Prepared snapshot is not for this source commit.');
    materialize(snapshot);
  } else throw new Error('Expected materialize or manifest.');
}
