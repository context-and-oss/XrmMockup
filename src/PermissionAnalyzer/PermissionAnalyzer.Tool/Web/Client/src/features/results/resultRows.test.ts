import { describe, expect, it } from 'vitest';
import type { Requirement, Source } from '../../shared/api/types';
import { groupRows } from './resultRows';

const source = (file: string, path = ['Execute', 'Save']): Source => ({ file, line: 12, column: 1, path });
const requirement = (extra: Partial<Requirement> = {}): Requirement => ({ table: 'account', privilege: 'Read', runsAs: 'Caller', basis: 'direct',
  coverage: { status: 'granted', depth: 'Organization' }, sources: [source('One.cs')], ...extra });
const rows = (values: Requirement[]) => values.map((requirement, index) => ({ requirement, index }));

describe('Table permission grouping', () => {
  it('combines only equivalent requirements and deduplicates sources without mutating the report', () => {
    const facts = [requirement(), requirement({ sources: [source('One.cs'), source('Two.cs'), source('One.cs', ['Execute', 'Retrieve'])] }), requirement({ privilege: 'Write' })];
    const original = JSON.stringify(facts);
    const groups = groupRows(rows(facts));
    expect(groups).toHaveLength(1); expect(groups[0]!.count).toBe(3);
    expect(groups[0]!.permissions).toHaveLength(2);
    expect(groups[0]!.permissions[0]!.count).toBe(2);
    expect(groups[0]!.permissions[0]!.requirement.sources).toHaveLength(3);
    expect(JSON.stringify(facts)).toBe(original);
  });

  it('never combines different identities, assumptions, bases, depths, reasons, or coverage states', () => {
    const facts = [requirement(), requirement({ runsAs: 'System' }), requirement({ assumed: true }), requirement({ userId: 'other-user' }),
      requirement({ basis: 'filter-unknown' }), requirement({ coverage: { status: 'missing' } }),
      requirement({ coverage: { status: 'granted', depth: 'User' } }), requirement({ coverage: { status: 'granted', depth: 'Organization', reason: 'Different evidence' } })];
    expect(groupRows(rows(facts))[0]!.permissions).toHaveLength(facts.length);
  });

  it('keeps global privileges and tables separate and sorts groups and their permissions stably', () => {
    const facts = [requirement({ table: 'contact', privilege: 'Write' }), requirement({ privilege: 'Write' }), requirement({ privilege: 'Read' }), requirement({ table: undefined, privilege: 'prvCustom' })];
    expect(groupRows(rows(facts)).map(group => group.table)).toEqual(['contact', 'account', 'Global']);
    expect(groupRows(rows(facts), { column: 'table', direction: 'ascending' }).map(group => group.table)).toEqual(['account', 'contact', 'Global']);
    expect(groupRows(rows(facts), { column: 'privilege', direction: 'ascending' }).find(group => group.table === 'account')!.permissions.map(permission => permission.requirement.privilege)).toEqual(['Read', 'Write']);
    expect(groupRows(rows(facts))[1]!.permissions.map(permission => permission.requirement.privilege)).toEqual(['Write', 'Read']);
  });
});
