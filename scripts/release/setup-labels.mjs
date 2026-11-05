import { GitHub, summary } from './github.mjs';
import { categories, products } from './model.mjs';
const api = new GitHub();
const labels = [
  ...Object.entries(products).map(([key, p]) => ({ name: p.label, color: '0052cc', description: `Release notes for the ${key} product` })),
  { name: 'product:none', color: 'ededed', description: 'Repository maintenance; no product release notes' },
  ...categories.map(c => ({ name: c.label, color: '5319e7', description: `${c.prefix} release-note category` })),
];
for (const label of labels) {
  const existing = await api.repo(`/labels/${encodeURIComponent(label.name)}`, { allow404: true });
  await api.repo(existing ? `/labels/${encodeURIComponent(label.name)}` : '/labels', { method: existing ? 'PATCH' : 'POST', body: label });
}
summary('Approved product/category labels provisioned. Configure the two required checks and the changelog bot separately.');
