// Test-only preload: redirect gh subprocesses to a loopback API-backed CLI double.
// Production code has no test transport switch and always executes the installed gh.
import childProcess from 'node:child_process';
import { syncBuiltinESMExports } from 'node:module';
import { fileURLToPath } from 'node:url';
const original = childProcess.execFile;
const mock = fileURLToPath(new URL('./mock-gh.mjs', import.meta.url));
childProcess.execFile = (command, args, ...options) => command === 'gh'
  ? original(process.execPath, [mock, ...args], ...options)
  : original(command, args, ...options);
syncBuiltinESMExports();

const originalFetch = globalThis.fetch;
globalThis.fetch = (url, options) => String(url).startsWith('https://api.nuget.org/')
  ? originalFetch(process.env.TEST_GH_API_URL + '/nuget/' + String(url).split('/').at(-1), options)
  : originalFetch(url, options);
