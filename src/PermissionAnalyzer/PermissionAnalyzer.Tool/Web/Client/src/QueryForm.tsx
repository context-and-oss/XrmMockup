import { useState } from 'react';
import type { FormEvent } from 'react';
import type { Catalog, Query, Scope } from './model';
import { values } from './model';

interface Props { model: Catalog; busy: boolean; onQuery: (query: Query) => Promise<void> }

export function QueryForm({ model, busy, onQuery }: Props) {
  const [scope, setScope] = useState<Scope>('all');
  const [target, setTarget] = useState('');
  const [message, setMessage] = useState('Update');
  const [attributes, setAttributes] = useState('');
  const [roles, setRoles] = useState<string[]>([]);
  const [callerRoles, setCallerRoles] = useState<string[]>([]);
  const [noRoles, setNoRoles] = useState(model.requirementsOnly);
  const [tables, setTables] = useState('');
  const [privileges, setPrivileges] = useState('');
  const roleNames = [...new Set(model.roles.map(role => role.name))];
  const suggestions = scope === 'operation' ? model.tables : scope === 'method' ? model.methods : model.entryPoints
    .filter(entry => scope === 'plugin' ? entry.kind.startsWith('Plugin') : scope === 'function' ? entry.kind === 'Function' : entry.kind === 'CustomApi')
    .map(entry => scope === 'plugin' ? entry.type : entry.id.replace(/^(function|custom-api):/, ''));

  async function submit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    await onQuery({ scope, target: target.trim() || null, message: message.trim() || null,
      attributes: values(attributes), roles: noRoles ? [] : roles, callerRoles: noRoles ? [] : callerRoles,
      noRoles, tables: values(tables), privileges: values(privileges) });
  }

  return <form className="panel controls" onSubmit={submit}>
    <p className="eyebrow">QUERY SETUP</p><h2>Build a query</h2><p className="muted small">Evaluate against the loaded model.</p>
    <label htmlFor="scope">Scope</label>
    <select id="scope" value={scope} onChange={event => { setScope(event.target.value as Scope); setTarget(''); }}>
      <option value="all">All entry points &amp; unreached methods</option>
      <option value="operation">Table operation + cascades</option>
      <option value="plugin">Plugin / plugin step</option>
      <option value="custom-api">Custom API</option>
      <option value="function">Azure Function</option>
      <option value="method">Method</option>
    </select>
    {scope !== 'all' && <>
      <label htmlFor="target">{scope === 'operation' ? 'Table logical name' : scope === 'plugin' ? 'Plugin type[:message[:stage]]' : 'Target name'}</label>
      <input id="target" list="targets" required value={target} onChange={event => setTarget(event.target.value)}
        placeholder={scope === 'operation' ? 'account' : scope === 'plugin' ? 'AccountPostPlugin:Update' : scope === 'method' ? 'Contoso.Importer.Run' : 'Name'} />
      <datalist id="targets">{[...new Set(suggestions)].map(value => <option key={value} value={value} />)}</datalist>
    </>}
    {scope === 'operation' && <>
      <label htmlFor="message">Message</label><input id="message" list="messages" required value={message} onChange={event => setMessage(event.target.value)} />
      <datalist id="messages">{['Create', 'Update', 'Delete', 'Retrieve', 'RetrieveMultiple', 'Assign', 'Share', 'Associate', 'Disassociate'].map(value => <option key={value} value={value} />)}</datalist>
      <label htmlFor="attributes">Changed columns <span className="muted">(optional)</span></label>
      <input id="attributes" value={attributes} onChange={event => setAttributes(event.target.value)} placeholder="name, ownerid" />
      <p className="hint">Leave empty for unknown columns; filtered Update steps may be included.</p>
    </>}
    <label htmlFor="roles">Application / caller roles</label>
    <select id="roles" multiple size={4} disabled={noRoles} value={roles} aria-describedby="roles-help"
      onChange={event => setRoles([...event.target.selectedOptions].map(option => option.value))}>
      {roleNames.map(name => <option key={name} value={name}>{name}</option>)}
    </select>
    <p id="roles-help" className="hint">Combine roles with Ctrl / Cmd. No selection uses CLI/configuration defaults.
      {model.defaults.roles.length > 0 && <> Defaults: {model.defaults.roles.join(' + ')} ({model.defaults.roleOrigin || 'CLI'}).</>}
    </p>
    <label htmlFor="caller-roles">Impersonated caller roles</label>
    <select id="caller-roles" multiple size={3} disabled={noRoles} value={callerRoles}
      onChange={event => setCallerRoles([...event.target.selectedOptions].map(option => option.value))}>
      {roleNames.map(name => <option key={name} value={name}>{name}</option>)}
    </select>
    {model.defaults.callerRoles.length > 0 && <p className="hint">Defaults: {model.defaults.callerRoles.join(' + ')} ({model.defaults.callerRoleOrigin || 'CLI'}).</p>}
    <label className="check"><input type="checkbox" checked={noRoles} disabled={model.requirementsOnly}
      onChange={event => setNoRoles(event.target.checked)} />Requirements only — skip role coverage</label>
    <details><summary>Report filters</summary>
      <label htmlFor="tables">Tables</label><input id="tables" list="table-names" value={tables} onChange={event => setTables(event.target.value)} placeholder="account, contact" />
      <datalist id="table-names">{model.tables.map(table => <option key={table} value={table} />)}</datalist>
      <label htmlFor="privileges">Privileges</label><input id="privileges" value={privileges} onChange={event => setPrivileges(event.target.value)} placeholder="Read, Write, prvCustomPrivilege" />
      <p className="hint">Comma-separated. Filters narrow displayed rows, not whole-analysis counts.</p>
    </details>
    <button className="primary" type="submit" disabled={busy}>{busy ? 'Evaluating…' : 'Run query'}<span aria-hidden="true">→</span></button>
  </form>;
}
