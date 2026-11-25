import type { Catalog, Query, Scope } from '../../shared/api/types';

export const scopeLabels: Record<Scope, string> = {
  all: 'All entry points & unreached methods', operation: 'Table operation + cascades',
  plugin: 'Plugin / plugin step', 'custom-api': 'Custom API', function: 'Azure Function', method: 'Method',
};
export function queryDescription(query: Query): string {
  if (query.scope === 'all') return 'All entry points & unreached methods';
  if (query.scope === 'operation') return `${query.message || 'Choose a message'} ${query.target || '…'} + cascades`;
  return `${scopeLabels[query.scope]} · ${query.target || 'Choose a target'}`;
}

export function queryContextDescription(query: Query, model: Catalog): string {
  const roles = query.roles?.length ? query.roles : model.defaults.roles;
  const callerRoles = query.callerRoles?.length ? query.callerRoles : model.defaults.callerRoles;
  return [
    query.noRoles ? 'Requirements only · no role coverage' : `Coverage: ${roles.join(' + ') || 'session defaults'}`,
    !query.noRoles && callerRoles.length ? `Impersonated caller: ${callerRoles.join(' + ')}` : '',
    query.scope === 'operation' && query.attributes?.length ? `Changed columns: ${query.attributes.join(', ')}` : '',
    query.tables?.length ? `Report tables: ${query.tables.join(', ')}` : '',
    query.privileges?.length ? `Report privileges: ${query.privileges.join(', ')}` : '',
  ].filter(Boolean).join(' · ');
}

