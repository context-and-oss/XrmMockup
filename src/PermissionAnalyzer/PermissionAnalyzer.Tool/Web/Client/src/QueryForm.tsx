import { useEffect, useState } from 'react';
import type { FormEvent } from 'react';
import type { Catalog, Query, Scope } from './model';
import { values } from './model';
import { RolePicker } from './RolePicker';
import { SuggestionPicker } from './SuggestionPicker';
import { ColumnPicker } from './ColumnPicker';

interface Props {
  model: Catalog;
  busy: boolean;
  onQuery: (query: Query) => Promise<void>;
  onDraftChange?: (signature: string) => void;
}

const scopeLabels: Record<Scope, string> = {
  all: 'All entry points & unreached methods', operation: 'Table operation + cascades',
  plugin: 'Plugin / plugin step', 'custom-api': 'Custom API', function: 'Azure Function', method: 'Method',
};
const scopeHints: Record<Scope, string> = {
  all: 'Explore requirements across the entire loaded analysis. No target needed.',
  operation: 'Trace a table operation and the plugins and cascades it can trigger.',
  plugin: 'Choose a plugin type. Optionally append :message:stage to narrow the step.',
  'custom-api': 'Trace a custom API and its required execute privilege.',
  function: 'Trace the Dataverse privileges used by an Azure Function.',
  method: 'Trace a method by its fully qualified name, including unreached methods.',
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

export function QueryForm({ model, busy, onQuery, onDraftChange }: Props) {
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
    roles: noRoles ? [] : roles, callerRoles: noRoles ? [] : callerRoles,
    noRoles, tables: values(tables), privileges: values(privileges),
  };
  const signature = JSON.stringify(draft);
  useEffect(() => { onDraftChange?.(signature); }, [signature, onDraftChange]);
  const ready = (scope === 'all' || !!target.trim()) && (scope !== 'operation' || !!message.trim());
  const filterCount = values(tables).length + values(privileges).length;
  const effectiveRoles = roles.length ? roles : model.defaults.roles;
  const effectiveCallerRoles = callerRoles.length ? callerRoles : model.defaults.callerRoles;

  function reset() {
    setScope('all'); setTarget(''); setMessage('Update'); setAttributes([]);
    setRoles([]); setCallerRoles([]); setNoRoles(model.requirementsOnly);
    setTables(''); setPrivileges(''); setCollapsed(false);
    setRolePickerOpen(false); setCallerPickerOpen(false); setFiltersOpen(false);
  }
  async function submit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    if (!ready || busy) return;
    await onQuery(draft);
  }

  return <form className={`panel query-builder ${collapsed ? 'is-collapsed' : ''}`} onSubmit={submit} aria-labelledby="builder-title">
    <div className="builder-heading">
      <div><p className="eyebrow">QUERY BUILDER</p><h2 id="builder-title">Build a query</h2><p className="hint">Define the analysis. Choose the role context. Inspect the results below.</p></div>
      <button type="button" className="text-button" aria-expanded={!collapsed} aria-controls="query-fields" disabled={busy} onClick={() => setCollapsed(!collapsed)}>{collapsed ? 'Edit query' : 'Collapse builder'}</button>
    </div>
    <fieldset className="builder-fields" id="query-fields" disabled={busy} hidden={collapsed}>
      <legend className="sr-only">Query settings</legend>
      <div className="builder-grid">
        <section className="builder-step" aria-labelledby="scope-title">
          <h3 id="scope-title"><span className="step-number" aria-hidden="true">1</span>What do you want to analyze?</h3>
          <div className="query-clause"><span className="clause-word" aria-hidden="true">Analyze</span><div className="clause-field"><label htmlFor="scope">Scope</label>
            <select id="scope" value={scope} aria-describedby="scope-help" onChange={event => { setScope(event.target.value as Scope); setTarget(''); setAttributes([]); }}>
              {Object.entries(scopeLabels).map(([value, label]) => <option value={value} key={value}>{label}</option>)}
            </select></div></div>
          <p id="scope-help" className="hint clause-hint">{scopeHints[scope]}</p>
          {scope !== 'all' && <div className="query-clause"><span className="clause-word" aria-hidden="true">For</span><div className="clause-field">
            <label htmlFor="target">{scope === 'operation' ? 'Table logical name' : scope === 'plugin' ? 'Plugin type[:message[:stage]]' : 'Target name'}</label>
            <SuggestionPicker key={scope} id="target" label={scope === 'operation' ? 'Tables' : 'Targets'} options={suggestions} required disabled={busy} value={target} onChange={value => { if (value.trim().toLowerCase() !== target.trim().toLowerCase()) setAttributes([]); setTarget(value); }}
              placeholder={scope === 'operation' ? 'Choose or enter a table…' : scope === 'plugin' ? 'AccountPostPlugin:Update' : scope === 'method' ? 'Contoso.Importer.Run' : 'Choose or enter a target…'} />
          </div></div>}
          {scope === 'operation' && <>
            <div className="query-clause"><span className="clause-word" aria-hidden="true">When</span><div className="clause-field"><label htmlFor="message">Message</label>
              <SuggestionPicker id="message" label="Messages" options={['Create', 'Update', 'Delete', 'Retrieve', 'RetrieveMultiple', 'Assign', 'Share', 'Associate', 'Disassociate']} required disabled={busy} value={message} onChange={setMessage} />
            </div></div>
            <div className="query-clause"><span className="clause-word" aria-hidden="true">With</span><div className="clause-field"><p className="column-field-label">Changed columns <span className="muted">(optional)</span></p>
              <ColumnPicker key={target.trim().toLowerCase()} table={target.trim()} catalog={model.columnCatalog?.find(t => t.table.toLowerCase() === target.trim().toLowerCase())} selected={attributes} disabled={busy} onChange={setAttributes} />
            </div></div>
            <p id="attributes-help" className="hint clause-hint">No selection means unknown changed columns, not “no changes”; filtered Update steps may be included. Column suggestions do not refresh the saved analysis.</p>
          </>}
        </section>
        <section className="builder-step context-step" aria-labelledby="context-title">
          <h3 id="context-title"><span className="step-number" aria-hidden="true">2</span>Which roles should cover it?</h3>
          <label className="check coverage-toggle"><input type="checkbox" checked={noRoles} disabled={model.requirementsOnly}
            onChange={event => setNoRoles(event.target.checked)} />Requirements only — skip role coverage</label>
          <p className="hint">{model.requirementsOnly ? 'This session is fixed to requirements-only analysis.' : 'Combine role grants or inspect requirements without checking coverage.'}</p>
          <div className={`role-context ${noRoles ? 'context-disabled' : ''}`}>
            <p className="context-label">Application / caller roles</p>
            <p className="effective-roles">{noRoles ? 'Not checked' : effectiveRoles.join(' + ') || 'CLI / configuration defaults'}</p>
            <p className="hint">{!roles.length ? `Defaults: ${model.defaults.roles.join(' + ') || 'CLI / configuration defaults'} (${model.defaults.roleOrigin || 'CLI'}).` : `${roles.length} explicit roles combined. Session defaults replaced.`}</p>
            <details className="picker-disclosure" open={rolePickerOpen} onToggle={event => setRolePickerOpen(event.currentTarget.open)}><summary>Choose application / caller roles</summary>
              <RolePicker id="roles" label="Application / caller roles" names={roleNames} selected={roles} disabled={noRoles} onChange={setRoles} />
            </details>
          </div>
          <details className="picker-disclosure impersonation" open={callerPickerOpen} onToggle={event => setCallerPickerOpen(event.currentTarget.open)}><summary>Impersonated caller roles{effectiveCallerRoles.length > 0 && <span className="detail-count">{effectiveCallerRoles.length}</span>}</summary>
            <p className="hint">A separate caller identity, not additional application roles. Defaults: {model.defaults.callerRoles.join(' + ') || 'CLI / configuration defaults'} ({model.defaults.callerRoleOrigin || 'CLI'}).</p>
            <RolePicker id="caller-roles" label="Impersonated caller roles" names={roleNames} selected={callerRoles} disabled={noRoles} onChange={setCallerRoles} />
          </details>
        </section>
      </div>
      <details className="filter-disclosure" open={filtersOpen} onToggle={event => setFiltersOpen(event.currentTarget.open)}><summary><span className="step-number" aria-hidden="true">3</span>Report filters <span className="optional-label">{filterCount ? `${filterCount} values applied` : 'Optional'}</span></summary>
        <div className="filter-fields"><div><label htmlFor="tables">Tables</label><input id="tables" value={tables} onChange={event => setTables(event.target.value)} placeholder="account, contact" /></div>
          <div><label htmlFor="privileges">Privileges</label><input id="privileges" value={privileges} onChange={event => setPrivileges(event.target.value)} placeholder="Read, Write, prvCustomPrivilege" /></div>
          <button type="button" className="text-button" disabled={!filterCount} onClick={() => { setTables(''); setPrivileges(''); }}>Clear filters</button>
        </div><p className="hint">Comma-separated. Filters narrow displayed rows, not whole-analysis counts.</p>
      </details>
    </fieldset>
    <div className="builder-actions">
      <div className="query-preview"><span className="eyebrow">YOUR QUERY</span><p>{queryDescription(draft)}</p><span>{queryContextDescription(draft, model)}</span>
        {!ready && <p className="query-validation">{!target.trim() ? 'Choose a target to run this query.' : 'Enter a message to run this query.'}</p>}
      </div>
      <div className="builder-buttons"><button type="button" className="text-button" disabled={busy} onClick={reset}>Reset query</button>
        <button className="primary" type="submit" disabled={busy || !ready}>{busy ? 'Evaluating…' : 'Run query'}<span aria-hidden="true">→</span></button></div>
    </div>
  </form>;
}
