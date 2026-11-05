import { mkdirSync, writeFileSync } from 'node:fs';
import { resolve } from 'node:path';
import { products, validateSnapshot } from './model.mjs';

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
