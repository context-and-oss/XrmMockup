import { useEffect, useState } from 'react';
import type { Catalog, Query, Report } from './model';
import { PermissionApi } from './model';
import { QueryForm } from './QueryForm';
import { ReportView } from './ReportView';
import { RoleCatalog } from './RoleCatalog';

const defaultApi = new PermissionApi();
interface Props { api?: Pick<PermissionApi, 'catalog' | 'query'> }
const errorMessage = (error: unknown) => error instanceof Error ? error.message : 'Unexpected error.';

export function App({ api = defaultApi }: Props) {
  const [model, setModel] = useState<Catalog>();
  const [error, setError] = useState('');
  const [report, setReport] = useState<Report>();
  const [failed, setFailed] = useState(false);
  const [busy, setBusy] = useState(false);
  const [view, setView] = useState<'query' | 'roles'>('query');

  useEffect(() => {
    let active = true;
    api.catalog().then(catalog => { if (active) setModel(catalog); }).catch(error => {
      if (active) setError(`Could not load model: ${errorMessage(error)}. Check that the CLI is still running.`);
    });
    return () => { active = false; };
  }, [api]);

  async function query(query: Query) {
    setBusy(true); setError(''); setReport(undefined); setFailed(false);
    try { setReport(await api.query(query)); }
    catch (error) { setError(errorMessage(error)); setFailed(true); }
    finally { setBusy(false); }
  }

  function download() {
    if (!report) return;
    const url = URL.createObjectURL(new Blob([JSON.stringify(report, null, 2) + '\n'], { type: 'application/json' }));
    const link = document.createElement('a'); link.href = url; link.download = 'permission-query.json'; link.click();
    setTimeout(() => URL.revokeObjectURL(url), 1000);
  }

  return <>
    <a className="skip-link" href="#explorer">Skip to security explorer</a>
    <header><div className="brand"><span className="product-name">XrmMockup</span><span className="product-divider" aria-hidden="true" /><span className="product-description">Permission Analyzer</span></div><span className="local"><span className="status-dot" aria-hidden="true" />Local session</span></header>
    <main id="explorer" tabIndex={-1}>
      <div className="heading"><div><p className="eyebrow">SECURITY EXPLORER</p><h1>Understand your security model.</h1><p className="subtitle">Explore role grants. Trace the privileges your code needs.</p></div>
        <div className="stats" aria-live="polite">{model ? [[model.roles.length, 'Security roles'], [model.tables.length, 'Tables'], [model.entryPoints.length, 'Entry points']].map(([value, label]) =>
          <div key={label} className="stat"><strong>{value}</strong><span>{label}</span></div>) : error ? 'Model unavailable' : 'Loading model…'}</div>
      </div>
      <aside className="notice"><svg className="notice-icon" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.5" aria-hidden="true"><path d="M12 3 4 6v6c0 4 4 7 8 9 4-2 8-5 8-9V6l-8-3Z" /><path d="M12 8v5m0 3v.5" /></svg><p><strong>Static analysis, not an access check.</strong> A granted privilege is not proof of record-level access. Business units, ownership, sharing, and runtime behavior can affect access. <span>No changes are made to Dataverse.</span></p></aside>
      {model?.incomplete && <p className="error" role="status">The loaded analysis is incomplete. Query results cannot prove complete privilege coverage.</p>}
      {error && <p className="error" role="alert">{error}</p>}
      <nav aria-label="Explorer views">{(['query', 'roles'] as const).map(tab => <button key={tab} className={`tab ${view === tab ? 'active' : ''}`} aria-pressed={view === tab} onClick={() => setView(tab)}>
        {tab === 'query' ? 'Query permissions' : 'Role catalog'}</button>)}</nav>
      {model && <>
        <section className="workspace" hidden={view !== 'query'}>
          <QueryForm model={model} busy={busy} onQuery={query} />
          <div className="results"><div className="results-heading"><div><p className="eyebrow">ANALYSIS</p><h2>Query results</h2></div><button className="secondary" disabled={!report || busy} onClick={download}>Download JSON</button></div>
            {busy ? <div className="empty" role="status">Evaluating query…</div> : report ? <ReportView report={report} /> : failed ? <div className="empty">The query failed. Adjust the query and try again.</div> :
              <div className="empty"><svg className="empty-icon" viewBox="0 0 48 48" fill="none" stroke="currentColor" strokeWidth="1.5" aria-hidden="true"><rect x="8" y="6" width="26" height="34" rx="5" /><path d="M15 15h12m-12 7h9m-9 7h6" /><circle cx="33" cy="32" r="7" /><path d="m38 37 5 5" /></svg><h3>Your model, explained.</h3><p>Choose an operation or entry point, select roles, and run a query.</p><span className="empty-caption">Requirements · Role coverage · Source traces</span></div>}
          </div>
        </section>
        {view === 'roles' && <RoleCatalog model={model} />}
      </>}
      <footer>Read-only · Source and analysis inputs are frozen for this session. Restart the CLI to refresh the model. Ctrl+C stops the server.</footer>
    </main>
  </>;
}
