import { readFileSync } from 'node:fs';
import { basename } from 'node:path';
const base = new URL(process.env.TEST_GH_API_URL ?? '');
if (base.protocol !== 'http:' || !['127.0.0.1', 'localhost'].includes(base.hostname)) throw new Error('CLI double can only access a loopback test API.');
const args = process.argv.slice(2);
const value = flag => args[args.indexOf(flag) + 1];
const values = flag => args.flatMap((arg, i) => arg === flag ? [args[i + 1]] : []);
const input = await new Promise(resolve => {
  const chunks = []; process.stdin.on('data', chunk => chunks.push(chunk)); process.stdin.on('end', () => resolve(Buffer.concat(chunks)));
});
const root = `/repos/${process.env.GITHUB_REPOSITORY}`;
async function request(path, method = 'GET', body, binary = false) {
  const response = await fetch(base.origin + path, { method, headers: body ? { 'content-type': binary ? 'application/octet-stream' : 'application/json' } : {},
    ...(body ? { body: binary ? body : JSON.stringify(body) } : {}) });
  if (!response.ok) throw new Error(`gh: ${await response.text()} (HTTP ${response.status})`);
  return response;
}
async function json(path, method, body) {
  const response = await request(path, method, body);
  return response.status === 204 ? null : response.json();
}
async function release(tag) {
  const releases = await json(`${root}/releases`);
  const found = releases.find(r => r.tag_name === tag);
  if (found) return found;
  // This double's high-level commands need to find drafts on subsequent pages too.
  for (let page = 2; releases.length === 100; page++) {
    const batch = await json(`${root}/releases?page=${page}`);
    const item = batch.find(r => r.tag_name === tag); if (item) return item;
    if (batch.length < 100) break;
  }
  throw new Error('gh: release not found (HTTP 404)');
}
try {
  if (args[0] === 'api') {
    let path = args[1];
    const body = args.includes('--input') ? JSON.parse(input.toString()) : undefined;
    do {
      const response = await request(path, value('--method'), body);
      const bytes = Buffer.from(await response.arrayBuffer());
      if (args.includes('--paginate')) {
        const payload = JSON.parse(bytes), records = value('--jq') === '.commits[] | @json' ? payload.commits : payload;
        for (const item of records) process.stdout.write(JSON.stringify(item) + '\n');
        const next = /<([^>]+)>; rel="next"/.exec(response.headers.get('link') ?? '')?.[1];
        path = next ? new URL(next).pathname + new URL(next).search : null;
      } else { process.stdout.write(bytes); path = null; }
    } while (path);
  } else if (args[0] === 'pr' && args[1] === 'create') {
    const pr = await json(`${root}/pulls`, 'POST', { title: value('--title'), body: input.toString(), head: value('--head'), base: value('--base') });
    await json(`${root}/issues/${pr.number}/labels`, 'PUT', { labels: values('--label') });
    process.stdout.write(`https://github.com/${process.env.GITHUB_REPOSITORY}/pull/${pr.number}\n`);
  } else if (args[0] === 'pr' && args[1] === 'edit') {
    const pr = await json(`${root}/pulls/${args[2]}`);
    await json(`${root}/pulls/${args[2]}`, 'PATCH', { title: value('--title'), body: input.toString() });
    const remove = new Set(values('--remove-label')), labels = pr.labels.map(l => l.name).filter(name => !remove.has(name));
    await json(`${root}/issues/${args[2]}/labels`, 'PUT', { labels: [...new Set([...labels, ...values('--add-label')])] });
  } else if (args[0] === 'pr' && args[1] === 'close') {
    await json(`${root}/pulls/${args[2]}`, 'PATCH', { state: 'closed' });
  } else if (args[0] === 'workflow') {
    await json(`${root}/actions/workflows/${args[2]}/dispatches`, 'POST', { ref: value('--ref'), ...(args.includes('--json') ? { inputs: JSON.parse(input) } : {}) });
  } else if (args[0] === 'release' && args[1] === 'create') {
    await json(`${root}/releases`, 'POST', { tag_name: args[2], name: value('--title'), body: input.toString(), draft: true, prerelease: args.includes('--prerelease') });
  } else if (args[0] === 'release' && args[1] === 'edit') {
    const found = await release(args[2]);
    await json(`${root}/releases/${found.id}`, 'PATCH', { draft: false, make_latest: args.find(arg => arg.startsWith('--latest=')).slice('--latest='.length) });
  } else if (args[0] === 'release' && args[1] === 'upload') {
    const found = await release(args[2]);
    await request(`${root}/upload/${found.id}?name=${encodeURIComponent(basename(args[3]))}`, 'POST', readFileSync(args[3]), true);
  } else if (args[0] === 'label') {
    await json(`${root}/labels`, 'POST', { name: args[2], color: value('--color'), description: value('--description') });
  } else throw new Error(`Unsupported test CLI command: ${args.join(' ')}`);
} catch (error) { process.stderr.write(error.message + '\n'); process.exitCode = 1; }
