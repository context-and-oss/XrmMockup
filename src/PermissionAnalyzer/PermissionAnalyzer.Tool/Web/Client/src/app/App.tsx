import styles from './App.module.css';
import ui from '../shared/ui/ui.module.css';
import { cx } from '../shared/ui/classNames';
import { useEffect, useState } from 'react';
import type { Catalog, Query, Report } from '../shared/api/types';
import { PermissionApi } from '../shared/api/PermissionApi';
import { QueryForm } from '../features/query-builder/QueryForm';
import { queryDescription, queryContextDescription } from '../features/query-builder/queryDescription';
import { ReportView } from '../features/results/ReportView';
import { RoleCatalog } from '../features/role-catalog/RoleCatalog';
import { uiMessage } from '../shared/presentation/labels';

const defaultApi = new PermissionApi();
interface Props { api?: Pick<PermissionApi, 'catalog' | 'query'> }
const errorMessage = (error: unknown) => error instanceof Error ? uiMessage(error.message) : 'Unexpected error.';

export function App({ api = defaultApi }: Props) {
  const [model, setModel] = useState<Catalog>();
  const [error, setError] = useState('');
  const [report, setReport] = useState<Report>();
  const [failed, setFailed] = useState(false);
  const [busy, setBusy] = useState(false);
  const [draftSignature, setDraftSignature] = useState('');
  const [lastQuery, setLastQuery] = useState<Query>();
  const stale = !!report && !!lastQuery && draftSignature !== JSON.stringify(lastQuery);
  const [view, setView] = useState<'query' | 'roles'>('query');

  useEffect(() => {
    let active = true;
    api.catalog().then(catalog => { if (active) setModel(catalog); }).catch(error => {
      if (active) setError(`Could not load model: ${errorMessage(error)}. Check that the local analyzer is still running.`);
    });
    return () => { active = false; };
  }, [api]);

  async function query(query: Query) {
    setBusy(true); setError(''); setReport(undefined); setFailed(false); setLastQuery(query);
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
    <a className={styles.skipLink} href="#explorer">Skip to security explorer</a>
    <header className={styles.header}><div className={styles.brand}><span className={styles.productName}>XrmMockup</span><span className={styles.productDivider} aria-hidden="true" /><span className={styles.productDescription}>Permission Analyzer</span></div><span className={styles.local}><span className={styles.statusDot} aria-hidden="true" />Local session</span></header>
    <main className={styles.main} id="explorer" tabIndex={-1}>
      <div className={styles.heading}><div><p className={ui.eyebrow}>SECURITY EXPLORER</p><h1>Understand your security model.</h1><p className={styles.subtitle}>Explore role grants. Trace the privileges your code needs.</p></div>
        <div className={styles.stats} aria-live="polite">{model ? [[model.roles.length, 'Security roles'], [model.tables.length, 'Tables'], [model.entryPoints.length, 'Entry points']].map(([value, label]) =>
          <div key={label} className={styles.stat}><strong>{value}</strong><span>{label}</span></div>) : error ? 'Model unavailable' : 'Loading model…'}</div>
      </div>
      <aside className={styles.notice}><svg className={styles.noticeIcon} viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.5" aria-hidden="true"><path d="M12 3 4 6v6c0 4 4 7 8 9 4-2 8-5 8-9V6l-8-3Z" /><path d="M12 8v5m0 3v.5" /></svg><p><strong>Static analysis, not an access check.</strong> A granted privilege is not proof of record-level access. Business units, ownership, sharing, and runtime behavior can affect access. <span>No changes are made to Dataverse.</span></p></aside>
      {model?.incomplete && <p className={ui.error} role="status">The loaded analysis is incomplete. Query results cannot prove complete privilege coverage.</p>}
      {error && !failed && <p className={ui.error} role="alert">{error}</p>}
      <nav className={styles.nav} aria-label="Explorer views">{(['query', 'roles'] as const).map(tab => <button key={tab} className={cx(styles.tab, view === tab && styles.active)} aria-pressed={view === tab} onClick={() => setView(tab)}>
        {tab === 'query' ? 'Query permissions' : 'Role catalog'}</button>)}</nav>
      {model && <>
        <section className={styles.workspace} hidden={view !== 'query'}>
          <QueryForm model={model} busy={busy} onQuery={query} onDraftChange={setDraftSignature} />
          <section className={styles.results} aria-labelledby="results-title"><div className={styles.resultsHeading}><div><p className={ui.eyebrow}>ANALYSIS</p><h2 id="results-title">Query results</h2></div><button className={ui.secondary} disabled={!report || busy} onClick={download}>Download JSON</button></div>
            {report && lastQuery && <div className={styles.lastRun}><p>Last run: {queryDescription(lastQuery)}</p><p>{queryContextDescription(lastQuery, model)}</p></div>}
            {failed && error && <p className={ui.error} role="alert">{error}</p>}
            {stale && <p className={styles.staleNotice} role="status">Query changed. These results and the JSON download are from the last run. Run the query again to refresh.</p>}
            {busy ? <div className={cx(ui.surface, ui.empty)} role="status">Evaluating query…</div> : report ? <ReportView report={report} /> : failed ? <div className={cx(ui.surface, ui.empty)}>The query failed. Adjust the query and try again.</div> :
              <div className={cx(ui.surface, ui.empty)}><svg className={ui.emptyIcon} viewBox="0 0 48 48" fill="none" stroke="currentColor" strokeWidth="1.5" aria-hidden="true"><rect x="8" y="6" width="26" height="34" rx="5" /><path d="M15 15h12m-12 7h9m-9 7h6" /><circle cx="33" cy="32" r="7" /><path d="m38 37 5 5" /></svg><h3>Your model, explained.</h3><p>Choose an operation or entry point, select roles, and run a query.</p><span className={ui.emptyCaption}>Requirements · Role coverage · Source traces</span></div>}
          </section>
        </section>
        {view === 'roles' && <RoleCatalog model={model} />}
      </>}
      <footer className={styles.footer}>Read-only · Source and analysis inputs are frozen for this session. Restart the local analyzer to refresh the model. Ctrl+C stops the server.</footer>
    </main>
  </>;
}
