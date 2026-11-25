import { fireEvent, render, screen, within } from '@testing-library/react';
import { describe, expect, it } from 'vitest';
import { ReportView } from './ReportView';
import type { Report, Requirement, Section } from './model';

const requirement = (table: string | undefined, privilege: string, coverage: Requirement['coverage'], extra: Partial<Requirement> = {}): Requirement =>
  ({ table, privilege, coverage, runsAs: 'Caller', basis: 'direct', sources: [], ...extra });
const section: Section = {
  id: 'function:Sync', kind: 'Function', requirements: [
    requirement('contact', 'Write', { status: 'missing', reason: 'No role grants Write' }),
    requirement('account', 'Read', { status: 'granted', depth: 'Organization' }),
    requirement('account', 'Write', { status: 'missing' }, { runsAs: 'User', userId: 'user-123', assumed: true, basis: 'filter-unknown', sources: [{ file: 'Code.cs', line: 1, column: 1, path: ['Sync', 'Save'] }] }),
    requirement(undefined, 'prvCustom', { status: 'unverifiable' }),
    requirement('lead', 'Delete', { status: 'system' }, { runsAs: 'System' }),
    requirement('team', 'Read', undefined),
  ], unresolved: [], assumptions: [],
};
const diagnostics: Section = { id: 'plugin:Unknown', kind: 'Plugin', requirements: [], unresolved: [{ operation: 'Execute', reason: 'Unknown request', source: { file: 'Plugin.cs', line: 2, column: 1, path: [] } }], assumptions: [] };
const report: Report = {
  analysis: { incomplete: true, truncatedSections: ['plugin:Unknown'], reportFilter: 'table:account' },
  entryPoints: [section, diagnostics], unreached: [],
  result: { granted: 1, missing: 2, unresolved: 1, notChecked: 1, unverifiable: 1, truncated: 1, suppressed: 0, system: 1, notGoverned: 0 },
};
const table = () => within(screen.getByRole('region', { name: 'function:Sync requirements' })).getByRole('table');
const rows = () => within(table()).getAllByRole('row').slice(1);
const filterColumns = () => fireEvent.click(screen.getByRole('button', { name: 'Filter columns' }));

describe('Interactive report results', () => {
  it('combines legend selections as OR, toggles off, and leaves report counts untouched', () => {
    const original = JSON.stringify(report); render(<ReportView report={report} />);
    fireEvent.click(screen.getByRole('button', { name: '2 Missing' }));
    expect(rows()).toHaveLength(2);
    expect(screen.getByRole('button', { name: '2 Missing' })).toHaveAttribute('aria-pressed', 'true');
    fireEvent.click(screen.getByRole('button', { name: '1 Granted' }));
    expect(rows()).toHaveLength(3);
    fireEvent.click(screen.getByRole('button', { name: '2 Missing' }));
    expect(rows()).toHaveLength(1);
    expect(within(rows()[0]!).getByText('Read')).toBeInTheDocument();
    fireEvent.click(screen.getByRole('button', { name: '1 Granted' }));
    expect(rows()).toHaveLength(6);
    expect(JSON.stringify(report)).toBe(original);
    expect(screen.getByText('Incomplete analysis — do not treat as a pass')).toBeInTheDocument();
    expect(screen.getByText('Filtered rows · counts cover whole analysis')).toBeInTheDocument();
  });

  it('selects unresolved and truncated sections without presenting a clean empty analysis', () => {
    render(<ReportView report={report} />);
    fireEvent.click(screen.getByRole('button', { name: '1 Unresolved' }));
    expect(screen.queryByRole('region', { name: 'function:Sync requirements' })).not.toBeInTheDocument();
    expect(screen.getByText(/Unresolved Execute: Unknown request/)).toBeInTheDocument();
    expect(screen.getByText('Analysis truncated for this section.')).toBeInTheDocument();
    expect(screen.getByRole('status', { name: 'Result row count' })).toHaveTextContent('Showing 0 of 6');
    fireEvent.click(screen.getByRole('button', { name: '1 Truncated' }));
    fireEvent.click(screen.getByRole('button', { name: '1 Unresolved' }));
    expect(screen.getByText('plugin:Unknown')).toBeInTheDocument();
  });

  it('filters every column case-insensitively with AND, including identity details and coverage depth/reason', () => {
    render(<ReportView report={report} />); filterColumns();
    fireEvent.change(screen.getByLabelText('Table'), { target: { value: 'ACCOUNT' } });
    expect(rows()).toHaveLength(2);
    fireEvent.change(screen.getByLabelText('Privilege'), { target: { value: 'Write' } });
    expect(rows()).toHaveLength(1);
    fireEvent.change(screen.getByLabelText('Runs as'), { target: { value: 'USER-123' } });
    fireEvent.change(screen.getByLabelText('Basis'), { target: { value: 'filter-unknown' } });
    fireEvent.change(screen.getByLabelText('Coverage'), { target: { value: 'MISSING' } });
    expect(rows()).toHaveLength(1);
    fireEvent.click(screen.getByText('1 source')); expect(screen.getByText('Sync → Save')).toBeInTheDocument();
    fireEvent.click(screen.getByRole('button', { name: 'Clear result filters' }));
    fireEvent.change(screen.getByLabelText('Coverage'), { target: { value: 'organization' } });
    expect(rows()).toHaveLength(1);
    fireEvent.change(screen.getByLabelText('Coverage'), { target: { value: 'No role grants' } });
    expect(rows()).toHaveLength(1);
    expect(within(rows()[0]!).getByText('contact')).toBeInTheDocument();
    expect(screen.getByText(/Unresolved Execute/)).toBeInTheDocument();
  });

  it('sorts ascending, descending, then original order with deterministic ties and aria-sort', () => {
    render(<ReportView report={report} />);
    const button = within(table()).getByRole('button', { name: 'Sort by table' });
    fireEvent.click(button);
    expect(button.closest('th')).toHaveAttribute('aria-sort', 'ascending');
    expect(rows().map(row => row.querySelector('td')!.firstChild!.textContent)).toEqual(['account', 'account', 'contact', 'Global', 'lead', 'team']);
    fireEvent.click(button);
    expect(button.closest('th')).toHaveAttribute('aria-sort', 'descending');
    expect(rows()[0]!.querySelector('td')).toHaveTextContent('team');
    fireEvent.click(button);
    expect(button.closest('th')).toHaveAttribute('aria-sort', 'none');
    expect(rows()[0]!.querySelector('td')).toHaveTextContent('contact');
    for (const label of ['privilege', 'runs as', 'basis', 'coverage']) {
      fireEvent.click(within(table()).getByRole('button', { name: `Sort by ${label}` }));
      expect(table().querySelectorAll('th[aria-sort=ascending]')).toHaveLength(1);
    }
  });

  it('supports global and no-coverage rows, empty filters, hidden filter controls, and new report reset', () => {
    const view = render(<ReportView report={report} />); filterColumns();
    fireEvent.change(screen.getByLabelText('Table'), { target: { value: 'global' } });
    expect(rows()).toHaveLength(1); expect(within(rows()[0]!).getByText('prvCustom')).toBeInTheDocument();
    fireEvent.click(screen.getByRole('button', { name: 'Hide column filters' }));
    expect(rows()).toHaveLength(1);
    fireEvent.click(screen.getByRole('button', { name: 'Clear result filters' }));
    fireEvent.click(screen.getByRole('button', { name: '1 Not checked' }));
    expect(rows()).toHaveLength(1); expect(within(rows()[0]!).getByText('team')).toBeInTheDocument();
    view.rerender(<ReportView report={{ ...report }} />);
    expect(rows()).toHaveLength(6);
    expect(screen.getByRole('button', { name: '1 Not checked' })).toHaveAttribute('aria-pressed', 'false');
    fireEvent.click(screen.getByRole('button', { name: '1 Granted' })); filterColumns();
    fireEvent.change(screen.getByLabelText('Table'), { target: { value: 'nothing' } });
    expect(screen.getByText('No matching results')).toBeInTheDocument();
    expect(screen.getByText('Incomplete analysis — do not treat as a pass')).toBeInTheDocument();
    fireEvent.click(screen.getByRole('button', { name: 'Clear result filters' })); expect(rows()).toHaveLength(6);
  });

  it('can filter a zero-count badge without inventing matches and keeps suppression statuses distinct', () => {
    const zero = { ...report, result: { ...report.result!, granted: 0 }, entryPoints: [{ ...section, requirements: [
      requirement('account', 'Write', { status: 'suppressed' }),
      requirement('account', 'Read', { status: 'not-governed' }),
    ] }] };
    zero.result.suppressed = 1; zero.result.notGoverned = 1;
    render(<ReportView report={zero} />);
    fireEvent.click(screen.getByRole('button', { name: '0 Granted' }));
    expect(screen.getByText('No matching results')).toBeInTheDocument();
    fireEvent.click(screen.getByRole('button', { name: 'Clear result filters' }));
    fireEvent.click(screen.getByRole('button', { name: '1 Suppressed' }));
    expect(rows()).toHaveLength(1); expect(within(rows()[0]!).getByText('suppressed')).toBeInTheDocument();
    fireEvent.click(screen.getByRole('button', { name: '1 Not governed' }));
    expect(rows()).toHaveLength(2);
  });

  it('supports requirements-only reports without inventing role coverage', () => {
    render(<ReportView report={{ ...report, result: undefined, entryPoints: [{ ...section, requirements: section.requirements.map(r => ({ ...r, coverage: undefined })) }] }} />);
    expect(screen.getByText('Requirements only · no role coverage')).toBeInTheDocument();
    expect(screen.queryByRole('button', { name: /Granted/ })).not.toBeInTheDocument();
    filterColumns(); fireEvent.change(screen.getByLabelText('Coverage'), { target: { value: 'not checked' } });
    expect(rows()).toHaveLength(6);
  });
});
