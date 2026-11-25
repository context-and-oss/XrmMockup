import styles from './QueryForm.module.css';
import ui from '../../shared/ui/ui.module.css';
import { cx } from '../../shared/ui/classNames';
import { useEffect, useState } from 'react';
import type { FormEvent } from 'react';
import type { Catalog, Query, Scope, UserLookupApi, UserSelection, UserConnection as Connection } from '../../shared/api/types';
import { values } from './queryValues';
import { RolePicker } from './RolePicker';
import { SuggestionPicker } from '../../shared/components/SuggestionPicker/SuggestionPicker';
import { ColumnPicker } from './ColumnPicker';
import { queryDescription, queryContextDescription, scopeLabels } from './queryDescription';
import { UserConnection } from '../user-lookup/UserConnection';
import { UserPicker } from '../user-lookup/UserPicker';
import { roleOriginLabel } from '../../shared/presentation/labels';

interface Props {
  model: Catalog;
  busy: boolean;
  onQuery: (query: Query, description?: string) => Promise<void>;
  userApi?: Partial<UserLookupApi>;
  onDraftChange?: (signature: string) => void;
}

const scopeHints: Record<Scope, string> = {
  all: 'Explore requirements across the entire loaded analysis. No target needed.',
  operation: 'Trace a table operation and the plugins and cascades it can trigger.',
  plugin: 'Choose a plugin type, or enter a specific step to narrow the analysis.',
  'custom-api': 'Trace a custom API and its required execute privilege.',
  function: 'Trace the Dataverse privileges used by an Azure Function.',
  method: 'Trace a method by its fully qualified name, including unreached methods.',
};

export function QueryForm({ model, busy, onQuery, onDraftChange, userApi }: Props) {
  const [connection, setConnection] = useState<Connection>();
  const [lookupBusy, setLookupBusy] = useState(false);
  const [callingUser, setCallingUser] = useState<UserSelection>();
  const [impersonatedUser, setImpersonatedUser] = useState<UserSelection>();
  const [callingLookup, setCallingLookup] = useState(false);
  const [impersonatedLookup, setImpersonatedLookup] = useState(false);
  const liveApi = userApi?.usersConnection && userApi.connectUsers && userApi.disconnectUsers && userApi.searchUsers && userApi.resolveUser ? userApi as UserLookupApi : undefined;
  const pending = busy || lookupBusy;
  function connectionChanged(next: Connection) {
    setConnection(next); setCallingUser(undefined); setImpersonatedUser(undefined);
    setCallingLookup(false); setImpersonatedLookup(false);
  }
  const [scope, setScope] = useState<Scope>('all');
  const [target, setTarget] = useState('');
  const [message, setMessage] = useState('Update');
  const [attributes, setAttributes] = useState<string[]>([]);
  const [roles, setRoles] = useState<string[]>([]);
  const [callerRoles, setCallerRoles] = useState<string[]>([]);
  const [noRoles, setNoRoles] = useState(model.requirementsOnly);
  const [tables, setTables] = useState('');
  const [privileges, setPrivileges] = useState('');
  const [collapsed, setCollapsed] = useState(false);
  const [rolePickerOpen, setRolePickerOpen] = useState(false);
  const [callerPickerOpen, setCallerPickerOpen] = useState(false);
  const [filtersOpen, setFiltersOpen] = useState(false);
  const roleNames = [...new Set(model.roles.map(role => role.name))];
  const suggestions = scope === 'operation' ? model.tables : scope === 'method' ? model.methods : model.entryPoints
    .filter(entry => scope === 'plugin' ? entry.kind.startsWith('Plugin') : scope === 'function' ? entry.kind === 'Function' : entry.kind === 'CustomApi')
    .map(entry => scope === 'plugin' ? entry.type : entry.id.replace(/^(function|custom-api):/, ''));
  // Only send fields applicable to this scope. Hidden values survive switching scopes
  // but must not filter or change a query for a different kind of entry point.
  const draft: Query = {
    scope, target: scope === 'all' ? null : target.trim() || null,
    message: scope === 'operation' ? message.trim() || null : null,
    attributes: scope === 'operation' ? attributes : [],
    roles: noRoles || callingUser ? [] : roles, callerRoles: noRoles || impersonatedUser ? [] : callerRoles,
    ...(!noRoles && callingUser ? { userSelection: callingUser.selectionId } : {}),
    ...(!noRoles && impersonatedUser ? { callerUserSelection: impersonatedUser.selectionId } : {}),
    noRoles, tables: values(tables), privileges: values(privileges),
  };
  const signature = JSON.stringify(draft);
  useEffect(() => { onDraftChange?.(signature); }, [signature, onDraftChange]);
  const ready = (scope === 'all' || !!target.trim()) && (scope !== 'operation' || !!message.trim());
  const filterCount = values(tables).length + values(privileges).length;
  const effectiveRoles = callingUser ? callingUser.roles : roles.length ? roles : model.defaults.roles;
  const effectiveCallerRoles = impersonatedUser ? impersonatedUser.roles : callerRoles.length ? callerRoles : model.defaults.callerRoles;

  function reset() {
    setCallingUser(undefined); setImpersonatedUser(undefined); setCallingLookup(false); setImpersonatedLookup(false);
    setScope('all'); setTarget(''); setMessage('Update'); setAttributes([]);
    setRoles([]); setCallerRoles([]); setNoRoles(model.requirementsOnly);
    setTables(''); setPrivileges(''); setCollapsed(false);
    setRolePickerOpen(false); setCallerPickerOpen(false); setFiltersOpen(false);
  }
  async function submit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    if (!ready || pending) return;
    await onQuery(draft, queryContextDescription(draft, model, callingUser, impersonatedUser));
  }

  return <form className={cx(ui.surface, styles.queryBuilder)} onSubmit={submit} aria-labelledby="builder-title">
    <div className={styles.builderHeading}>
      <div><p className={ui.eyebrow}>QUERY BUILDER</p><h2 id="builder-title">Build a query</h2><p className={ui.hint}>Define the analysis. Choose the role context. Inspect the results below.</p></div>
      <button type="button" className={ui.textButton} aria-expanded={!collapsed} aria-controls="query-fields" disabled={pending} onClick={() => setCollapsed(!collapsed)}>{collapsed ? 'Edit query' : 'Collapse builder'}</button>
    </div>
    <fieldset className={styles.builderFields} id="query-fields" disabled={busy} hidden={collapsed}>
      <legend className={ui.srOnly}>Query settings</legend>
      {liveApi && model.userLookup && !model.requirementsOnly && <UserConnection api={liveApi} options={model.userLookup} connection={connection} disabled={busy || lookupBusy}
        onChange={connectionChanged} onBusy={setLookupBusy} />}
      <div className={styles.builderGrid}>
        <section className={styles.builderStep} aria-labelledby="scope-title">
          <h3 id="scope-title"><span className={styles.stepNumber} aria-hidden="true">1</span>What do you want to analyze?</h3>
          <div className={styles.queryClause}><span className={styles.clauseWord} aria-hidden="true">Analyze</span><div className={styles.clauseField}><label htmlFor="scope">Scope</label>
            <select id="scope" disabled={pending} value={scope} aria-describedby="scope-help" onChange={event => { setScope(event.target.value as Scope); setTarget(''); setAttributes([]); }}>
              {Object.entries(scopeLabels).map(([value, label]) => <option value={value} key={value}>{label}</option>)}
            </select></div></div>
          <p id="scope-help" className={cx(ui.hint, styles.clauseHint)}>{scopeHints[scope]}</p>
          {scope !== 'all' && <div className={styles.queryClause}><span className={styles.clauseWord} aria-hidden="true">For</span><div className={styles.clauseField}>
            <label htmlFor="target">{scope === 'operation' ? 'Table logical name' : scope === 'plugin' ? 'Plugin type or step' : 'Target name'}</label>
            <SuggestionPicker key={scope} id="target" label={scope === 'operation' ? 'Tables' : 'Targets'} options={suggestions} required disabled={pending} value={target} onChange={value => { if (value.trim().toLowerCase() !== target.trim().toLowerCase()) setAttributes([]); setTarget(value); }}
              placeholder={scope === 'operation' ? 'Choose or enter a table…' : scope === 'plugin' ? 'AccountPostPlugin:Update' : scope === 'method' ? 'Contoso.Importer.Run' : 'Choose or enter a target…'} />
          </div></div>}
          {scope === 'operation' && <>
            <div className={styles.queryClause}><span className={styles.clauseWord} aria-hidden="true">When</span><div className={styles.clauseField}><label htmlFor="message">Message</label>
              <SuggestionPicker id="message" label="Messages" options={['Create', 'Update', 'Delete', 'Retrieve', 'RetrieveMultiple', 'Assign', 'Share', 'Associate', 'Disassociate']} required disabled={pending} value={message} onChange={setMessage} />
            </div></div>
            <div className={cx(styles.queryClause, styles.columnClause)}><span className={styles.clauseWord} aria-hidden="true">With</span><div className={styles.clauseField}><p className={styles.columnFieldLabel}>Changed columns <span className={ui.muted}>(optional)</span></p>
              <ColumnPicker key={target.trim().toLowerCase()} table={target.trim()} catalog={model.columnCatalog?.find(t => t.table.toLowerCase() === target.trim().toLowerCase())} selected={attributes} disabled={pending} onChange={setAttributes} />
            </div></div>
            <p id="attributes-help" className={cx(ui.hint, styles.clauseHint)}>No selection means unknown changed columns, not “no changes”; filtered Update steps may be included. Column suggestions do not refresh the saved analysis.</p>
          </>}
        </section>
        <section className={cx(styles.builderStep, styles.contextStep)} aria-labelledby="context-title">
          <h3 id="context-title"><span className={styles.stepNumber} aria-hidden="true">2</span>Which roles should cover it?</h3>
          <label className={cx(ui.check, styles.coverageToggle)}><input type="checkbox" checked={noRoles} disabled={model.requirementsOnly || pending}
            onChange={event => setNoRoles(event.target.checked)} />Requirements only — skip role coverage</label>
          <p className={ui.hint}>{model.requirementsOnly ? 'This session is fixed to requirements-only analysis.' : 'Combine role grants or inspect requirements without checking coverage.'}</p>
          <div className={cx(styles.roleContext, noRoles && styles.contextDisabled)}>
            <p className={styles.contextLabel}>Application / caller roles</p>
            <p className={styles.effectiveRoles}>{noRoles ? 'Not checked' : effectiveRoles.join(' + ') || 'Session defaults'}</p>
            <p className={ui.hint}>{callingUser ? `Calling user: ${callingUser.user.name}. Live assignments; local privilege definitions.` : !roles.length ? `Defaults: ${model.defaults.roles.join(' + ') || 'Session defaults'} (${roleOriginLabel(model.defaults.roleOrigin)}).` : `${roles.length} explicit roles combined. Session defaults replaced.`}</p>
            {!callingUser && connection?.connected && liveApi && <button type="button" className={ui.textButton} disabled={noRoles || pending} aria-expanded={callingLookup}
              onClick={() => { setCallingLookup(!callingLookup); }}>Look up calling user</button>}
            {callingLookup && connection?.connected && liveApi && <UserPicker api={liveApi} isCaller={false} selected={callingUser} disabled={noRoles || pending}
              onBusy={setLookupBusy} onSelect={selected => { setCallingUser(selected); if (selected) setRoles([]); else setCallingLookup(false); }} />}
            {!callingUser && <details className={styles.pickerDisclosure} open={rolePickerOpen} onToggle={event => setRolePickerOpen(event.currentTarget.open)}><summary>Choose application / caller roles</summary>
              <RolePicker id="roles" label="Application / caller roles" names={roleNames} selected={roles} disabled={noRoles || pending} onChange={setRoles} />
            </details>}
          </div>
          <details className={cx(styles.pickerDisclosure, styles.impersonation)} open={callerPickerOpen} onToggle={event => setCallerPickerOpen(event.currentTarget.open)}><summary>Impersonated caller roles{effectiveCallerRoles.length > 0 && <span className={styles.detailCount}>{effectiveCallerRoles.length}</span>}</summary>
            <p className={ui.hint}>For application code acting on behalf of another user, not additional application roles. {impersonatedUser ? `Using live assignments for ${impersonatedUser.user.name}.` : `Defaults: ${model.defaults.callerRoles.join(' + ') || 'Session defaults'} (${roleOriginLabel(model.defaults.callerRoleOrigin)}).`}</p>
            <p className={ui.hint}>Plugin and workflow registrations with a fixed user keep their recorded identity; this does not replace that user.</p>
            {!impersonatedUser && connection?.connected && liveApi && <button type="button" className={ui.textButton} disabled={noRoles || pending} aria-expanded={impersonatedLookup}
              onClick={() => setImpersonatedLookup(!impersonatedLookup)}>Look up impersonated user</button>}
            {impersonatedLookup && connection?.connected && liveApi && <UserPicker api={liveApi} isCaller={true} selected={impersonatedUser} disabled={noRoles || pending}
              onBusy={setLookupBusy} onSelect={selected => { setImpersonatedUser(selected); if (selected) setCallerRoles([]); else setImpersonatedLookup(false); }} />}
            {!impersonatedUser && <RolePicker id="caller-roles" label="Impersonated caller roles" names={roleNames} selected={callerRoles} disabled={noRoles || pending} onChange={setCallerRoles} />}
          </details>
        </section>
      </div>
      <details className={styles.filterDisclosure} open={filtersOpen} onToggle={event => setFiltersOpen(event.currentTarget.open)}><summary><span className={styles.stepNumber} aria-hidden="true">3</span>Report filters <span className={styles.optionalLabel}>{filterCount ? `${filterCount} values applied` : 'Optional'}</span></summary>
        <div className={styles.filterFields}><div><label htmlFor="tables">Tables</label><SuggestionPicker id="tables" label="Report tables" options={model.tables} commaSeparated disabled={pending} value={tables} onChange={setTables} placeholder="account, contact" /></div>
          <div><label htmlFor="privileges">Privileges</label><SuggestionPicker id="privileges" label="Report privileges" options={['Create', 'Read', 'Write', 'Delete', 'Append', 'AppendTo', 'Assign', 'Share', ...model.roles.flatMap(role => role.grants.map(grant => grant.privilege))]} commaSeparated disabled={pending} value={privileges} onChange={setPrivileges} placeholder="Read, Write, custom privilege…" /></div>
          <button type="button" className={ui.textButton} disabled={pending || !filterCount} onClick={() => { setTables(''); setPrivileges(''); }}>Clear filters</button>
        </div><p className={ui.hint}>Comma-separated. Filters narrow displayed rows, not whole-analysis counts.</p>
      </details>
    </fieldset>
    <div className={styles.builderActions}>
      <div className={styles.queryPreview}><span className={ui.eyebrow}>YOUR QUERY</span><p>{queryDescription(draft)}</p><span>{queryContextDescription(draft, model, callingUser, impersonatedUser)}</span>
        {!ready && <p className={styles.queryValidation}>{!target.trim() ? 'Choose a target to run this query.' : 'Enter a message to run this query.'}</p>}
      </div>
      <div className={styles.builderButtons}><button type="button" className={ui.textButton} disabled={pending} onClick={reset}>Reset query</button>
        <button className={ui.primary} type="submit" disabled={pending || !ready}>{busy ? 'Evaluating…' : 'Run query'}<span aria-hidden="true">→</span></button></div>
    </div>
  </form>;
}
