import { readFileSync } from 'node:fs';
import { git } from './github.mjs';
import { validateSnapshot } from './model.mjs';
import { materialize } from './workspace.mjs';
const snapshot = validateSnapshot(JSON.parse(readFileSync('.release/release-snapshot.json', 'utf8')), process.env.GITHUB_REPOSITORY);
if (git('rev-parse', 'HEAD') !== snapshot.sourceSha) throw new Error('Prepared snapshot is not for this source commit.');
materialize(snapshot);
