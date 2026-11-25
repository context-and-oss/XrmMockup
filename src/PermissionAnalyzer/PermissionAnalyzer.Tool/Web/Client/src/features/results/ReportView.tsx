import styles from './ReportView.module.css';
import ui from '../../shared/ui/ui.module.css';
import { cx } from '../../shared/ui/classNames';
import { useState } from 'react';
import { SuggestionPicker } from '../../shared/components/SuggestionPicker/SuggestionPicker';
import { roleOriginLabel, sectionTitle, sectionKindLabel, identityLabel, uiMessage, coverageLabel, depthLabel } from '../../shared/presentation/labels';
import { PermissionTable } from './PermissionTable';
import { columns, status, value, searchValue, groupRows } from './resultRows';
import type { Column, Sort } from './resultRows';
import type { Report, Requirement } from '../../shared/api/types';

type Category = 'granted' | 'missing' | 'not-checked' | 'unresolved' | 'unverifiable' | 'truncated' | 'suppressed' | 'system' | 'not-governed';
type Filters = Record<Column, string>;
const emptyFilters: Filters = { table: '', privilege: '', identity: '', basis: '', coverage: '' };

function Results({ report }: { report: Report }) {
  const [selected, setSelected] = useState<Category[]>([]);
  const [filters, setFilters] = useState<Filters>(emptyFilters);
  const [filterOpen, setFilterOpen] = useState(false);
  const [sort, setSort] = useState<Sort>();
  const sections = [...report.entryPoints, ...report.unreached];
  const hasFilters = selected.length > 0 || Object.values(filters).some(value => value.trim());
  const matchesColumns = (requirement: Requirement) => columns.every(([column]) =>
    searchValue(requirement, column).toLowerCase().includes(filters[column].trim().toLowerCase()));
  const views = sections.map(section => {
    const truncated = report.analysis.truncatedSections.includes(section.id);
    const rows = section.requirements.map((requirement, index) => ({ requirement, index }))
      .filter(({ requirement }) => (!selected.length || selected.includes(status(requirement)) || truncated && selected.includes('truncated')) && matchesColumns(requirement));
    const diagnosticMatch = !selected.length || selected.includes('unresolved') && section.unresolved.length > 0 || selected.includes('truncated') && truncated;
    // Preserve incomplete-analysis evidence even if column filters hide every requirement.
    const showDiagnostics = diagnosticMatch && (truncated || section.unresolved.length > 0 || section.assumptions.length > 0);
    return { section, rows, groups: groupRows(rows, sort), truncated, visible: !hasFilters || rows.length > 0 || showDiagnostics };
  });
  const counts: [string, number, string, Category][] = report.result ? [
    ['Granted', report.result.granted, 'good', 'granted'], ['Missing', report.result.missing, 'bad', 'missing'],
    ['Not checked', report.result.notChecked, 'warn', 'not-checked'], ['Unresolved', report.result.unresolved, 'warn', 'unresolved'],
    ['Unverifiable', report.result.unverifiable, 'warn', 'unverifiable'], ['Truncated', report.result.truncated, 'warn', 'truncated'],
    ['Suppressed', report.result.suppressed, '', 'suppressed'], ['System', report.result.system, '', 'system'], ['Not governed', report.result.notGoverned, '', 'not-governed'],
  ] : [];
  const shownRows = views.reduce((count, view) => count + view.rows.length, 0);
  const groupCount = views.reduce((count, view) => count + view.groups.length, 0);
  const filterOptions = Object.fromEntries(columns.map(([column]) => [column, [...new Set(sections.flatMap(section => section.requirements.flatMap(requirement => column === 'coverage' ? [coverageLabel(status(requirement)), ...(requirement.coverage?.depth ? [depthLabel(requirement.coverage.depth)] : []), ...(requirement.coverage?.reason ? [uiMessage(requirement.coverage.reason)] : [])] : [value(requirement, column)])))]])) as Record<Column, string[]>;
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
      <div><p className={styles.resultCount} role="status" aria-label="Result row count">Showing {shownRows} of {totalRows} loaded requirements across {groupCount} table groups{selected.length > 0 && ' · Finding filters active'}</p>
        <p className={ui.hint}>Toggle badges to combine findings. Column filters combine with AND. Counts and JSON stay unchanged; diagnostics remain in matching sections.</p></div>
      <div className={styles.resultToolbarActions}><button className={ui.secondary} type="button" aria-expanded={filterOpen} aria-controls="result-column-filters" onClick={() => setFilterOpen(!filterOpen)}>{filterOpen ? 'Hide column filters' : 'Filter columns'}</button>
        {hasFilters && <button className={ui.textButton} type="button" onClick={clear}>Clear result filters</button>}</div>
    </div>
    {filterOpen && <fieldset id="result-column-filters" className={styles.resultColumnFilters}><legend className={ui.srOnly}>Result column filters</legend>
      {columns.map(([column, label]) => <div key={column}><label htmlFor={`result-filter-${column}`}>{label}</label>
        <SuggestionPicker id={`result-filter-${column}`} label={label} options={filterOptions[column]} value={filters[column]} onChange={value => setFilters(current => ({ ...current, [column]: value }))}
          placeholder={column === 'coverage' ? 'Status, depth, or reason…' : `Filter ${label.toLowerCase()}…`} /></div>)}
    </fieldset>}
    {!views.some(view => view.visible) && <div className={cx(ui.surface, ui.empty)}><h3>No matching results</h3><p>Try another finding or clear the column filters. The analysis totals above have not changed.</p></div>}
    {views.filter(view => view.visible).map(({ section, rows, groups, truncated }) => {
      const warnings = [
        ...(truncated ? ['Analysis truncated for this section.'] : []),
        ...section.unresolved.map(value => `Unresolved ${value.operation}: ${value.reason} (${value.source.file}:${value.source.line})`),
        ...section.assumptions.map(value => `Assumption: ${value.reason} (${value.source.file}:${value.source.line})`),
        ...(section.triggers?.length ? [`Triggers: ${section.triggers.join(', ')}`] : []),
      ];
      return <article className={cx(ui.surface, styles.sectionCard)} key={section.id}>
        <div className={styles.sectionHead}><h3>{sectionTitle(section.id)}</h3><p>{sectionKindLabel(section.kind)}</p>
          {section.roles?.map((identity, index) => <p key={index}>{identityLabel(identity.runsAs)}{identity.userId && ` (${identity.userId})`}: {identity.roles?.join(' + ') || (identity.note && uiMessage(identity.note)) || 'Not checked'}{identity.origin && ` · ${roleOriginLabel(identity.origin)}`}</p>)}
          <p>{rows.length} of {section.requirements.length} requirements · {groups.length} table groups</p>
        </div>
        <PermissionTable id={section.id} groups={groups} hasRequirements={section.requirements.length > 0} sort={sort} onSort={sortColumn} />
        {warnings.length > 0 && <div className={styles.diagnostics}>{warnings.map((warning, index) => <p key={index}>{uiMessage(warning)}</p>)}</div>}
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
