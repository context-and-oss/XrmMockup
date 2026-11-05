import { readFileSync } from 'node:fs';
import { GitHub, validatePulls, summary } from './github.mjs';
import { classifyProducts, classifyCategory } from './model.mjs';

const mode = process.argv[2];
const checks = { product: ['PR product labels', classifyProducts], category: ['PR change category', classifyCategory] };
if (!checks[mode]) throw new Error('Expected product or category mode.');
const [name, validate] = checks[mode];
const api = new GitHub();
const event = JSON.parse(readFileSync(process.env.GITHUB_EVENT_PATH, 'utf8'));
let failure, prs = [];
try {
  prs = await validatePulls(api, event);
  for (const pr of prs) validate(pr);
} catch (error) { failure = error.message; }
const headSha = event.merge_group?.head_sha ?? prs[0]?.head.sha ?? event.pull_request?.head.sha;
if (!headSha) throw new Error('Cannot determine check head SHA.');
const text = failure ?? `Validated ${prs.map(pr => `#${pr.number}`).join(', ')} against the approved release classification rules.`;
await api.repo('/check-runs', { method: 'POST', body: {
  name, head_sha: headSha, status: 'completed', conclusion: failure ? 'failure' : 'success',
  completed_at: new Date().toISOString(), external_id: `${mode}-${process.env.GITHUB_RUN_ID}-${process.env.GITHUB_RUN_ATTEMPT}`,
  output: { title: failure ? 'PR classification required' : 'Valid PR classification', summary: text },
} });
summary(`${name}: ${text}`);
if (failure) process.exitCode = 1;
