import type { Report, Section, Source } from './model';

function Sources({ sources }: { sources: Source[] }) {
  if (!sources.length) return null;
  return <details className="sources"><summary>{sources.length} source{sources.length === 1 ? '' : 's'}</summary>
    {sources.map((source, index) => <div key={index} className="source">{source.file}:{source.line}:{source.column}
      {source.path.length > 0 && <div>{source.path.join(' → ')}</div>}
    </div>)}
  </details>;
}

function SectionCard({ section, truncated }: { section: Section; truncated: boolean }) {
  const warnings = [
    ...(truncated ? ['Analysis truncated for this section.'] : []),
    ...section.unresolved.map(value => `Unresolved ${value.operation}: ${value.reason} (${value.source.file}:${value.source.line})`),
    ...section.assumptions.map(value => `Assumption: ${value.reason} (${value.source.file}:${value.source.line})`),
    ...(section.triggers?.length ? [`Triggers: ${section.triggers.join(', ')}`] : []),
  ];
  return <article className="section-card">
    <div className="section-head"><h3>{section.id}</h3><p>{section.kind || 'Unreached method'}</p>
      {section.roles?.map((identity, index) => <p key={index}>{identity.runsAs}{identity.userId && ` (${identity.userId})`}: {identity.roles?.join(' + ') || identity.note || 'Not checked'}{identity.origin && ` · ${identity.origin}`}</p>)}
    </div>
    {section.requirements.length > 0 ? <div className="table-wrap" role="region" aria-label={`${section.id} requirements`} tabIndex={0}><table>
      <thead><tr><th>Table / privilege</th><th>Runs as</th><th>Basis</th><th>Coverage</th></tr></thead>
      <tbody>{section.requirements.map((requirement, index) => <tr key={index}>
        <td>{requirement.table || 'Global'} · {requirement.privilege}<Sources sources={requirement.sources} /></td>
        <td>{requirement.runsAs}{requirement.userId && ` (${requirement.userId})`}{requirement.assumed && ' (assumed)'}</td>
        <td>{requirement.basis}</td>
        <td><span className={`badge ${requirement.coverage?.status || ''}`}>{requirement.coverage?.status || 'Not checked'}</span>
          {requirement.coverage?.depth && <div className="hint">{requirement.coverage.depth}</div>}
          {requirement.coverage?.reason && <div className="hint">{requirement.coverage.reason}</div>}
        </td>
      </tr>)}</tbody>
    </table></div> : <p className="diagnostics">No requirements in this view.</p>}
    {warnings.length > 0 && <div className="diagnostics">{warnings.map((warning, index) => <p key={index}>{warning}</p>)}</div>}
  </article>;
}

export function ReportView({ report }: { report: Report }) {
  const counts: [string, number, string][] = report.result ? [
    ['Granted', report.result.granted, 'good'], ['Missing', report.result.missing, 'bad'],
    ['Not checked', report.result.notChecked, 'warn'], ['Unresolved', report.result.unresolved, 'warn'],
    ['Unverifiable', report.result.unverifiable, 'warn'], ['Truncated', report.result.truncated, 'warn'],
    ['Suppressed', report.result.suppressed, ''], ['System', report.result.system, ''], ['Not governed', report.result.notGoverned, ''],
  ] : [];
  const sections = [...report.entryPoints, ...report.unreached];
  return <div aria-live="polite">
    <div className="summary">
      {counts.filter(([label, count]) => count || label === 'Granted' || label === 'Missing').map(([label, count, style]) =>
        <span key={label} className={`pill ${style}`}>{count} {label}</span>)}
      {!report.result && <span className="pill">Requirements only · no role coverage</span>}
      {report.analysis.incomplete && <span className="pill warn">Incomplete analysis — do not treat as a pass</span>}
      {report.analysis.reportFilter && <span className="pill">Filtered rows · counts cover whole analysis</span>}
      {!!report.tablesWithoutRoleMetadata?.length && <span className="pill warn">No role metadata: {report.tablesWithoutRoleMetadata.join(', ')}</span>}
    </div>
    {sections.length ? sections.map(section => <SectionCard key={section.id} section={section} truncated={report.analysis.truncatedSections.includes(section.id)} />)
      : <div className="empty">No matching sections. Try another scope or clear the filters.</div>}
  </div>;
}
