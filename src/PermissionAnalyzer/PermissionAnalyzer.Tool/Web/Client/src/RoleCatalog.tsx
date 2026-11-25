import { useState } from 'react';
import type { Catalog, Grant, Role } from './model';

function RoleCard({ role, grants, expanded }: { role: Role; grants: Grant[]; expanded: boolean }) {
  const [open, setOpen] = useState(expanded);
  // Mount grant tables only when opened, rather than thousands of invisible rows for large catalogs.
  return <details className="catalog-card" open={open} onToggle={event => setOpen(event.currentTarget.open)}>
    <summary>{role.name} · {grants.length} grants{role.roleId && ` · ${role.roleId}`}</summary>
    {open && <>
      {!role.globalPrivilegesKnown && <p className="hint">Global privileges are unknown in this metadata (not evidence of missing grants).</p>}
      <div className="table-wrap" role="region" aria-label={`${role.name} grants`} tabIndex={0}><table><thead><tr><th>Table</th><th>Privilege</th><th>Depth</th></tr></thead>
        <tbody>{grants.map((grant, index) => <tr key={index}><td>{grant.table || 'Global'}</td><td>{grant.privilege}</td><td>{grant.depth}</td></tr>)}</tbody>
      </table></div>
      {!grants.length && <p className="hint">No recorded grants.</p>}
    </>}
  </details>;
}

export function RoleCatalog({ model }: { model: Catalog }) {
  const [search, setSearch] = useState('');
  const filter = search.trim().toLowerCase();
  const matches = model.roles.map(role => ({ role, grants: role.grants.filter(grant => role.name.toLowerCase().includes(filter)
    || `${grant.table || 'global'} ${grant.privilege} ${grant.depth}`.toLowerCase().includes(filter)) }))
    .filter(({ role, grants }) => role.name.toLowerCase().includes(filter) || grants.length);
  return <section>
    <div className="catalog-toolbar"><div><h2>Role privilege catalog</h2><p className="muted">Individual role grants at their recorded depth. Query permissions to evaluate combined roles.</p></div>
      <label htmlFor="catalog-search">Filter role, table, or privilege<input id="catalog-search" type="search" value={search} onChange={event => setSearch(event.target.value)} placeholder="Search grants…" /></label>
    </div>
    {matches.map(({ role, grants }, index) => <RoleCard key={`${index}:${role.roleId || role.name}:${filter}`} role={role} grants={grants} expanded={Boolean(filter)} />)}
    {!matches.length && <div className="empty">{model.roles.length ? 'No grants match your search.' : 'No security roles loaded. Restart with --metadata containing SecurityRoles, or a snapshot with embedded roles.'}</div>}
  </section>;
}
