/** Presentation labels only. Keep the API and exported analysis report unchanged. */
const argumentLabels: Record<string, string> = {
  '--role': 'application roles', '--caller-role': 'impersonated caller roles',
  '--no-roles': 'requirements-only mode', '--metadata': 'local metadata',
  '--user': 'application user', '--caller-user': 'impersonated caller',
  '--config': 'session settings', '--snapshot': 'saved analysis', '--explore': 'explorer',
  '--entry': 'method', '--operation': 'table operation', '--attributes': 'changed columns',
  '--strict': 'complete coverage check', '--fail-on-missing': 'missing privilege check',
};
export function uiMessage(text: string): string {
  return text.replace(/--[a-z]+(?:-[a-z]+)*/g, argument => argumentLabels[argument] || 'session option')
    .replace(/XrmMockup:Permissions:Principals/g, 'application role mappings')
    .replace(/XrmMockup:Permissions:Users/g, 'user role mappings');
}
export function roleOriginLabel(origin?: string | null): string {
  if (!origin || origin === 'CLI' || origin === '--role' || origin === '--caller-role') return 'Session roles';
  if (origin.startsWith('Principals:')) return `Application role mapping · ${origin.slice(11).trim()}`;
  if (origin.startsWith('Users:')) return `User role mapping · ${origin.slice(6).trim()}`;
  return uiMessage(origin);
}
export const identityLabel = (identity: string) => ({ ApplicationUser: 'Application user', ImpersonatedCaller: 'Impersonated caller' })[identity] || identity;
export const basisLabel = (basis: string) => ({ direct: 'Direct', 'execute-privilege': 'Execute privilege', 'all-implementations': 'All implementations',
  'all-lookup-targets': 'All lookup targets', 'filter-unknown': 'Unknown changed columns' })[basis] || basis;
export const coverageLabel = (status: string) => ({ granted: 'Granted', missing: 'Missing', suppressed: 'Suppressed', 'not-checked': 'Not checked',
  system: 'System', unverifiable: 'Unverifiable', 'not-governed': 'Not governed' })[status] || status;
export const depthLabel = (depth: string) => ({ User: 'User', BusinessUnit: 'Business unit', ParentChildBusinessUnit: 'Parent and child business units', Organization: 'Organization' })[depth] || depth;
export function sectionTitle(id: string): string {
  return id.replace(/^(operation|plugin|function|custom-api|method):/, '').replace(/\(([^)]+)\)$/, ' ($1)');
}
export const sectionKindLabel = (kind?: string) => ({ PluginStep: 'Plugin step', CustomApi: 'Custom API', Workflow: 'Workflow', CodeActivity: 'Code activity' })[kind || ''] || kind || 'Unreached method';
