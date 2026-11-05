import { execFile } from 'node:child_process';

// No shell interpolation: Markdown/JSON goes over stdin and tokens stay in env.
export function runGh(args, { input, env } = {}) {
  return new Promise((resolve, reject) => {
    const child = execFile('gh', args, { env, encoding: null, timeout: 60000, maxBuffer: 128 * 1024 * 1024 }, (error, stdout, stderr) => {
      if (error) {
        error.stdout = stdout;
        error.stderr = stderr?.toString('utf8') ?? '';
        reject(error);
      } else resolve(stdout);
    });
    child.stdin.on('error', () => {}); // The CLI's exit error is authoritative after an early exit.
    child.stdin.end(input ?? '');
  });
}

export function ghEnvironment(token, host = new URL(process.env.GITHUB_SERVER_URL ?? 'https://github.com').host, extra = {}) {
  const env = { ...process.env, ...extra, GH_HOST: host, GH_PROMPT_DISABLED: '1', GH_NO_UPDATE_NOTIFIER: '1', NO_COLOR: '1' };
  if (token) { env.GH_TOKEN = token; env.GH_ENTERPRISE_TOKEN = token; }
  return env;
}
