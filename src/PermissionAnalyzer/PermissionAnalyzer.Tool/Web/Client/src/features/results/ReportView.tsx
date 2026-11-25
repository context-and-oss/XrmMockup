import styles from './ReportView.module.css';
import ui from '../../shared/ui/ui.module.css';
import { cx } from '../../shared/ui/classNames';
import { useState } from 'react';
import type { Report, Requirement, Source } from '../../shared/api/types';

type Column = 'table' | 'privilege' | 'identity' | 'basis' | 'coverage';
type Category = 'granted' | 'missing' | 'not-checked' | 'unresolved' | 'unverifiable' | 'truncated' | 'suppressed' | 'system' | 'not-governed';
type Filters = Record<Column, string>;
const columns: [Column, string][] = [['table', 'Table'], ['privilege', 'Privilege'], ['identity', 'Runs as'], ['basis', 'Basis'], ['coverage', 'Coverage']];
const emptyFilters: Filters = { table: '', privilege: '', identity: '', basis: '', coverage: '' };
const status = (requirement: Requirement) => requirement.coverage?.status || 'not-checked';
function value(requirement: Requirement, column: Column) {
  switch (column) {
    case 'table': return requirement.table || 'Global';
    case 'privilege': return requirement.privilege;
    case 'identity': return `${requirement.runsAs}${requirement.userId ? ` (${requirement.userId})` : ''}${requirement.assumed ? ' (assumed)' : ''}`;
    case 'basis': return requirement.basis;
    case 'coverage': return [status(requirement).replaceAll('-', ' '), requirement.coverage?.depth, requirement.coverage?.reason].filter(Boolean).join(' · ');
  }
}
function Sources({ sources }: { sources: Source[] }) {
  if (!sources.length) return null;
  return <details className={styles.sources}><summary>{sources.length} source{sources.length === 1 ? '' : 's'}</summary>
    {sources.map((source, index) => <div key={index} className={styles.source}>{source.file}:{source.line}:{source.column}
      {source.path.length > 0 && <div>{source.path.join(' → ')}</div>}
    </div>)}
  </details>;
}

function Results({ report }: { report: Report }) {
  const [selected, setSelected] = useState<Category[]>([]);
  const [filters, setFilters] = useState<Filters>(emptyFilters);
  const [filterOpen, setFilterOpen] = useState(false);
  const [sort, setSort] = useState<{ column: Column; direction: 'ascending' | 'descending' }>();
  const sections = [...report.entryPoints, ...report.unreached];
  const hasFilters = selected.length > 0 || Object.values(filters).some(value => value.trim());
  const matchesColumns = (requirement: Requirement) => columns.every(([column]) =>
    value(requirement, column).toLowerCase().includes(filters[column].trim().toLowerCase()));
  const views = sections.map(section => {
    const truncated = report.analysis.truncatedSections.includes(section.id);
    const rows = section.requirements.map((requirement, index) => ({ requirement, index }))
      .filter(({ requirement }) => (!selected.length || selected.includes(status(requirement)) || truncated && selected.includes('truncated')) && matchesColumns(requirement));
    if (sort) rows.sort((a, b) => {
      const left = value(a.requirement, sort.column).toLowerCase(), right = value(b.requirement, sort.column).toLowerCase();
      const comparison = left < right ? -1 : left > right ? 1 : 0;
      return comparison * (sort.direction === 'ascending' ? 1 : -1) || a.index - b.index;
    });
    const diagnosticMatch = !selected.length || selected.includes('unresolved') && section.unresolved.length > 0 || selected.includes('truncated') && truncated;
    // Preserve incomplete-analysis evidence even if column filters hide every requirement.
    const showDiagnostics = diagnosticMatch && (truncated || section.unresolved.length > 0 || section.assumptions.length > 0);
    return { section, rows, truncated, visible: !hasFilters || rows.length > 0 || showDiagnostics };
  });
  const counts: [string, number, string, Category][] = report.result ? [
    ['Granted', report.result.granted, 'good', 'granted'], ['Missing', report.result.missing, 'bad', 'missing'],
    ['Not checked', report.result.notChecked, 'warn', 'not-checked'], ['Unresolved', report.result.unresolved, 'warn', 'unresolved'],
    ['Unverifiable', report.result.unverifiable, 'warn', 'unverifiable'], ['Truncated', report.result.truncated, 'warn', 'truncated'],
    ['Suppressed', report.result.suppressed, '', 'suppressed'], ['System', report.result.system, '', 'system'], ['Not governed', report.result.notGoverned, '', 'not-governed'],
  ] : [];
  const shownRows = views.reduce((count, view) => count + view.rows.length, 0);
  const totalRows = sections.reduce((count, section) => count + section.requirements.length, 0);
  function toggle(category: Category) {
    setSelected(current => current.includes(category) ? current.filter(value => value !== category) : [...current, category]);
  }
  function sortColumn(column: Column) {
    setSort(current => current?.column === column ? current.direction === 'ascending' ? { column, direction: 'descending' } : undefined : { column, direction: 'ascending' });
  }
  function clear() { setSelected([]); setFilters(emptyFilters); }

  return <div>
    <div className={styles.summary} role="group" aria-label="Filter results by finding">
      {counts.filter(([label, count]) => count || label === 'Granted' || label === 'Missing').map(([label, count, style, category]) =>
        <button type="button" key={label} className={cx(styles.pill, styles.resultFilter, styles[style])} aria-pressed={selected.includes(category)} onClick={() => toggle(category)}>
          <span aria-hidden="true" className={styles.filterCheck}>{selected.includes(category) ? '✓' : '+'}</span>{count} {label}</button>)}
      {!report.result && <span className={styles.pill}>Requirements only · no role coverage</span>}
      {report.analysis.incomplete && <span className={cx(styles.pill, styles.warn)}>Incomplete analysis — do not treat as a pass</span>}
      {report.analysis.reportFilter && <span className={styles.pill}>Filtered rows · counts cover whole analysis</span>}
      {!!report.tablesWithoutRoleMetadata?.length && <span className={cx(styles.pill, styles.warn)}>No role metadata: {report.tablesWithoutRoleMetadata.join(', ')}</span>}
    </div>
    <div className={styles.resultToolbar}>
      <div><p className={styles.resultCount} role="status" aria-label="Result row count">Showing {shownRows} of {totalRows} loaded requirement rows{selected.length > 0 && ' · Finding filters active'}</p>
        <p className={ui.hint}>Toggle badges to combine findings. Column filters combine with AND. Counts and JSON stay unchanged; diagnostics remain in matching sections.</p></div>
      <div className={styles.resultToolbarActions}><button className={ui.secondary} type="button" aria-expanded={filterOpen} aria-controls="result-column-filters" onClick={() => setFilterOpen(!filterOpen)}>{filterOpen ? 'Hide column filters' : 'Filter columns'}</button>
        {hasFilters && <button className={ui.textButton} type="button" onClick={clear}>Clear result filters</button>}</div>
    </div>
    {filterOpen && <fieldset id="result-column-filters" className={styles.resultColumnFilters}><legend className={ui.srOnly}>Result column filters</legend>
      {columns.map(([column, label]) => <div key={column}><label htmlFor={`result-filter-${column}`}>{label}</label>
        <input id={`result-filter-${column}`} type="search" value={filters[column]} onChange={event => setFilters({ ...filters, [column]: event.target.value })}
          placeholder={column === 'coverage' ? 'Status, depth, or reason…' : `Filter ${label.toLowerCase()}…`} /></div>)}
    </fieldset>}
    {!views.some(view => view.visible) && <div className={cx(ui.surface, ui.empty)}><h3>No matching results</h3><p>Try another finding or clear the column filters. The analysis totals above have not changed.</p></div>}
    {views.filter(view => view.visible).map(({ section, rows, truncated }) => {
      const warnings = [
        ...(truncated ? ['Analysis truncated for this section.'] : []),
        ...section.unresolved.map(value => `Unresolved ${value.operation}: ${value.reason} (${value.source.file}:${value.source.line})`),
        ...section.assumptions.map(value => `Assumption: ${value.reason} (${value.source.file}:${value.source.line})`),
        ...(section.triggers?.length ? [`Triggers: ${section.triggers.join(', ')}`] : []),
      ];
      return <article className={cx(ui.surface, styles.sectionCard)} key={section.id}>
        <div className={styles.sectionHead}><h3>{section.id}</h3><p>{section.kind || 'Unreached method'}</p>
          {section.roles?.map((identity, index) => <p key={index}>{identity.runsAs}{identity.userId && ` (${identity.userId})`}: {identity.roles?.join(' + ') || identity.note || 'Not checked'}{identity.origin && ` · ${identity.origin}`}</p>)}
          <p>{rows.length} of {section.requirements.length} requirement rows</p>
        </div>
        <div className={ui.tableWrap} role="region" aria-label={`${section.id} requirements`} tabIndex={0}><table className={ui.table}>
          <thead><tr>{columns.map(([column, label]) => <th key={column} scope="col" aria-sort={sort?.column === column ? sort.direction : 'none'}>
            <button className={styles.tableSort} type="button" onClick={() => sortColumn(column)} aria-label={`Sort by ${label.toLowerCase()}`}>
              {label}<span aria-hidden="true">{sort?.column === column ? sort.direction === 'ascending' ? '↑' : '↓' : '↕'}</span></button>
          </th>)}</tr></thead>
          <tbody>{rows.map(({ requirement, index }) => <tr key={index}>
            <td>{requirement.table || 'Global'}<Sources sources={requirement.sources} /></td>
            <td>{requirement.privilege}</td><td>{value(requirement, 'identity')}</td><td>{requirement.basis}</td>
            <td><span className={cx(styles.badge, styles[status(requirement)])}>{requirement.coverage?.status || 'Not checked'}</span>
              {requirement.coverage?.depth && <div className={ui.hint}>{requirement.coverage.depth}</div>}
              {requirement.coverage?.reason && <div className={ui.hint}>{requirement.coverage.reason}</div>}
            </td>
          </tr>)}
          {!rows.length && <tr><td colSpan={columns.length} className={styles.noResultRows}>{section.requirements.length ? 'No requirement rows match these filters. Section diagnostics are shown below.' : 'No requirements in this view.'}</td></tr>}
          </tbody>
        </table></div>
        {warnings.length > 0 && <div className={styles.diagnostics}>{warnings.map((warning, index) => <p key={index}>{warning}</p>)}</div>}
      </article>;
    })}
  </div>;
}

// A new report resets display-only controls; edits to the query do not reset the last report view.
export function ReportView({ report }: { report: Report }) {
  const [current, setCurrent] = useState(report);
  const [revision, setRevision] = useState(0);
  if (current !== report) { setCurrent(report); setRevision(revision + 1); }
  return <Results key={revision} report={report} />;
}
