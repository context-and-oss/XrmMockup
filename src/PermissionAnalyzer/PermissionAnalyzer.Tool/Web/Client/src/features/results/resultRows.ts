import type { Requirement, Source } from '../../shared/api/types';
import { basisLabel, coverageLabel, depthLabel, identityLabel, uiMessage } from '../../shared/presentation/labels';

export type Column = 'table' | 'privilege' | 'identity' | 'basis' | 'coverage';
export const columns: [Column, string][] = [['table', 'Table'], ['privilege', 'Privilege'], ['identity', 'Runs as'], ['basis', 'Basis'], ['coverage', 'Coverage']];
export const status = (requirement: Requirement) => requirement.coverage?.status || 'not-checked';
export function value(requirement: Requirement, column: Column): string {
  switch (column) {
    case 'table': return requirement.table || 'Global';
    case 'privilege': return requirement.privilege;
    case 'identity': return `${identityLabel(requirement.runsAs)}${requirement.userId ? ` (${requirement.userId})` : ''}${requirement.assumed ? ' (assumed)' : ''}`;
    case 'basis': return basisLabel(requirement.basis);
    case 'coverage': return [coverageLabel(status(requirement)), requirement.coverage?.depth && depthLabel(requirement.coverage.depth), requirement.coverage?.reason && uiMessage(requirement.coverage.reason)].filter(Boolean).join(' · ');
  }
}
// Keep raw aliases searchable for people who already know the model's terms.
export function searchValue(requirement: Requirement, column: Column): string {
  const raw = column === 'identity' ? requirement.runsAs : column === 'basis' ? requirement.basis : column === 'coverage' ? `${status(requirement)} ${requirement.coverage?.depth || ''}` : '';
  return `${value(requirement, column)} ${raw}`;
}
export interface PermissionGroup { key: string; requirement: Requirement; count: number }
export interface TableGroup { table: string; permissions: PermissionGroup[]; count: number }
export interface ResultRow { requirement: Requirement; index: number }
export type Sort = { column: Column; direction: 'ascending' | 'descending' };
const compare = (a: string, b: string) => a.toLowerCase() < b.toLowerCase() ? -1 : a.toLowerCase() > b.toLowerCase() ? 1 : 0;

/** One visual row per table. Only fully equivalent requirements are combined;
 * identity, assumption, basis, coverage depth/reason and source paths stay distinct. */
export function groupRows(rows: ResultRow[], sort?: Sort): TableGroup[] {
  const tables = new Map<string, TableGroup>();
  for (const { requirement } of rows) {
    const table = requirement.table || 'Global';
    let group = tables.get(table);
    if (!group) { group = { table, permissions: [], count: 0 }; tables.set(table, group); }
    group.count++;
    const key = JSON.stringify([requirement.privilege, requirement.runsAs, requirement.userId ?? null, !!requirement.assumed,
      requirement.basis, status(requirement), requirement.coverage?.depth ?? null, requirement.coverage?.reason ?? null]);
    const existing = group.permissions.find(permission => permission.key === key);
    if (existing) {
      existing.count++;
      existing.requirement = { ...existing.requirement, sources: mergeSources(existing.requirement.sources, requirement.sources) };
    } else group.permissions.push({ key, count: 1, requirement: { ...requirement, sources: mergeSources([], requirement.sources) } });
  }
  const groups = [...tables.values()];
  if (sort) {
    const direction = sort.direction === 'ascending' ? 1 : -1;
    for (const group of groups) group.permissions.sort((a, b) => direction * compare(value(a.requirement, sort.column), value(b.requirement, sort.column)));
    // A group is ordered by its leading permission in the selected direction.
    // Array.sort is stable, preserving input order for tied values.
    groups.sort((a, b) => direction * compare(sort.column === 'table' ? a.table : value(a.permissions[0]!.requirement, sort.column),
      sort.column === 'table' ? b.table : value(b.permissions[0]!.requirement, sort.column)));
  }
  return groups;
}
function mergeSources(left: Source[], right: Source[]): Source[] {
  return [...new Map([...left, ...right].map(source => [JSON.stringify([source.file, source.line, source.column, source.path]), source])).values()];
}
