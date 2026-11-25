import { describe, expect, it, vi, afterEach } from 'vitest';
import { fireEvent, render, screen, waitFor, within } from '@testing-library/react';
import { App } from './App';
import { PermissionApi } from './model';
import type { Catalog, Report } from './model';

const catalog: Catalog = {
  requirementsOnly: false, incomplete: false,
  defaults: { roles: ['Reader'], callerRoles: [], roleOrigin: 'Dataverse' },
  roles: [
    { name: 'Reader', roleId: null, globalPrivilegesKnown: false, grants: [{ table: 'account', privilege: 'Read', depth: 'User' }] },
    { name: '<script>alert(1)</script>', roleId: null, globalPrivilegesKnown: true, grants: [{ table: null, privilege: 'prvCustom', depth: 'Organization' }] },
  ],
  columnCatalog: [{ table: 'account', displayName: 'Account', columns: [{ logicalName: 'name', displayName: 'Account name' }, { logicalName: 'ownerid', displayName: 'Owner' }] }, { table: 'contact', displayName: 'Contact', columns: [{ logicalName: 'fullname', displayName: 'Full name' }] }],
  tables: ['account', 'contact'], methods: ['App.Run'],
  entryPoints: [{ id: 'custom-api:ctx_Run', kind: 'CustomApi', type: 'P' }],
};
const report: Report = {
  analysis: { incomplete: true, truncatedSections: ['operation:Update account'], reportFilter: 'table:account' },
  entryPoints: [{ id: 'operation:Update account', kind: 'Operation',
    requirements: [{ table: 'account', privilege: 'Write', runsAs: 'Caller', basis: 'direct',
      sources: [{ file: '<img src=x onerror=alert(1)>', line: 4, column: 5, path: ['App.Run', 'Save'] }],
      coverage: { status: 'missing', reason: 'No role grants this privilege.' } }],
    unresolved: [{ operation: 'Execute', reason: 'Unknown request', source: { file: 'Code.cs', line: 3, column: 1, path: [] } }],
    assumptions: [],
  }], unreached: [],
  result: { granted: 0, missing: 1, notChecked: 0, unresolved: 1, unverifiable: 0, truncated: 1, suppressed: 0, system: 0, notGoverned: 0 },
};

function mockApi(model = catalog) {
  return { catalog: vi.fn().mockResolvedValue(model), query: vi.fn().mockResolvedValue(report) };
}
afterEach(() => vi.unstubAllGlobals());

describe('React security explorer', () => {
  it('builds operation queries and preserves startup live defaults until explicitly changed', async () => {
    const api = mockApi(); render(<App api={api} />);
    await screen.findByText('Build a query');
    expect(screen.getByText(/Defaults: Reader \(Dataverse\)/)).toBeInTheDocument();
    fireEvent.change(screen.getByLabelText('Scope'), { target: { value: 'operation' } });
    fireEvent.change(screen.getByLabelText('Table logical name'), { target: { value: 'account' } });
    fireEvent.click(screen.getByRole('button', { name: 'Changed columns (optional)' }));
    fireEvent.click(screen.getByRole('checkbox', { name: 'Account name (name)' }));
    fireEvent.click(screen.getByRole('checkbox', { name: 'Owner (ownerid)' }));
    fireEvent.click(screen.getByRole('button', { name: 'Done' }));
    fireEvent.click(screen.getByRole('button', { name: 'Run query' }));
    await waitFor(() => expect(api.query).toHaveBeenCalledWith(expect.objectContaining({
      scope: 'operation', target: 'account', message: 'Update', attributes: ['name', 'ownerid'], roles: [], callerRoles: [],
    })));
    expect(await screen.findByText('1 Missing')).toBeInTheDocument();
    expect(screen.getByText('Incomplete analysis — do not treat as a pass')).toBeInTheDocument();
    expect(screen.getByText('Filtered rows · counts cover whole analysis')).toBeInTheDocument();
    expect(screen.getByText(/Unresolved Execute: Unknown request/)).toBeInTheDocument();
    fireEvent.click(screen.getByText('1 source'));
    expect(screen.getByText('App.Run → Save')).toBeInTheDocument();
    expect(document.querySelector('img')).toBeNull();
    expect(screen.getByRole('button', { name: 'Download JSON' })).toBeEnabled();
  });

  it('combines explicit roles separately from impersonated roles', async () => {
    const api = mockApi(); render(<App api={api} />); await screen.findByText('Build a query');
    fireEvent.click(screen.getByText('Choose application / caller roles'));
    const roles = screen.getByRole('group', { name: 'Application / caller roles' });
    fireEvent.click(within(roles).getByRole('checkbox', { name: 'Reader' }));
    fireEvent.click(within(roles).getByRole('checkbox', { name: '<script>alert(1)</script>' }));
    fireEvent.click(screen.getByText('Impersonated caller roles', { selector: 'summary' }));
    const caller = screen.getByRole('group', { name: 'Impersonated caller roles' });
    fireEvent.click(within(caller).getByRole('checkbox', { name: 'Reader' }));
    fireEvent.click(screen.getByRole('button', { name: 'Run query' }));
    await waitFor(() => expect(api.query).toHaveBeenCalledWith(expect.objectContaining({ roles: ['Reader', '<script>alert(1)</script>'], callerRoles: ['Reader'] })));
  });

  it('shows errors without stale reports and can retry', async () => {
    const api = mockApi(); render(<App api={api} />); await screen.findByText('Build a query');
    fireEvent.click(screen.getByRole('button', { name: 'Run query' })); await screen.findByText('1 Missing');
    api.query.mockRejectedValueOnce(new Error('Unknown security role.'));
    fireEvent.click(screen.getByRole('button', { name: 'Run query' }));
    expect(await screen.findByRole('alert')).toHaveTextContent('Unknown security role.');
    expect(screen.queryByText('1 Missing')).not.toBeInTheDocument();
    expect(screen.getByRole('button', { name: 'Download JSON' })).toBeDisabled();
    fireEvent.click(screen.getByRole('button', { name: 'Run query' })); await screen.findByText('1 Missing');
  });

  it('searches global catalog grants and renders untrusted metadata as text', async () => {
    render(<App api={mockApi()} />); await screen.findByText('Build a query');
    fireEvent.click(screen.getByRole('button', { name: 'Role catalog' }));
    fireEvent.change(screen.getByLabelText('Filter role, table, or privilege'), { target: { value: 'prvCustom' } });
    expect(screen.getByText('prvCustom')).toBeInTheDocument();
    expect(screen.getByText('Global')).toBeInTheDocument();
    expect(screen.getByText(/<script>alert\(1\)<\/script> · 1 grants/)).toBeInTheDocument();
    expect(document.querySelector('script')).toBeNull();
    fireEvent.change(screen.getByLabelText('Filter role, table, or privilege'), { target: { value: 'nothing' } });
    expect(screen.getByText('No grants match your search.')).toBeInTheDocument();
  });

  it('fixes requirements-only sessions and exposes custom API suggestions', async () => {
    const api = mockApi({ ...catalog, requirementsOnly: true }); render(<App api={api} />); await screen.findByText('Build a query');
    expect(screen.getByRole('checkbox', { name: /Requirements only/ })).toBeChecked(); expect(screen.getByRole('checkbox', { name: /Requirements only/ })).toBeDisabled();
    fireEvent.click(screen.getByText('Choose application / caller roles'));
    expect(screen.getByLabelText('Search application / caller roles')).toBeDisabled();
    fireEvent.change(screen.getByLabelText('Scope'), { target: { value: 'custom-api' } });
    fireEvent.focus(screen.getByLabelText('Target name'));
    expect(screen.getByRole('option', { name: 'ctx_Run' })).toBeInTheDocument();
    fireEvent.change(screen.getByLabelText('Target name'), { target: { value: 'ctx_Run' } });
    fireEvent.click(screen.getByRole('button', { name: 'Run query' }));
    await waitFor(() => expect(api.query).toHaveBeenCalledWith(expect.objectContaining({ scope: 'custom-api', target: 'ctx_Run', noRoles: true, roles: [] })));
    fireEvent.click(screen.getByRole('button', { name: 'Reset query' }));
    expect(screen.getByRole('checkbox', { name: /Requirements only/ })).toBeChecked();
    expect(screen.getByRole('checkbox', { name: /Requirements only/ })).toBeDisabled();
  });

  it('reports startup failures and empty role catalogs', async () => {
    const api = mockApi(); api.catalog.mockRejectedValueOnce(new Error('Server stopped'));
    const first = render(<App api={api} />);
    expect(await screen.findByRole('alert')).toHaveTextContent('Could not load model: Server stopped'); first.unmount();
    render(<App api={mockApi({ ...catalog, roles: [] })} />); await screen.findByText('Build a query');
    fireEvent.click(screen.getByRole('button', { name: 'Role catalog' }));
    expect(screen.getByText(/No security roles loaded\. Restart/)).toBeInTheDocument();
  });

  it('offers a skip link and communicates the selected explorer view', async () => {
    render(<App api={mockApi()} />);
    await screen.findByText('Build a query');
    expect(screen.getByRole('link', { name: 'Skip to security explorer' })).toHaveAttribute('href', '#explorer');
    expect(document.querySelector('#explorer')).toHaveAttribute('tabindex', '-1');
    expect(screen.getByRole('button', { name: 'Query permissions' })).toHaveAttribute('aria-pressed', 'true');
    fireEvent.change(screen.getByLabelText('Scope'), { target: { value: 'operation' } });
    fireEvent.change(screen.getByLabelText('Table logical name'), { target: { value: 'account' } });
    fireEvent.click(screen.getByRole('button', { name: 'Role catalog' }));
    expect(screen.getByRole('button', { name: 'Role catalog' })).toHaveAttribute('aria-pressed', 'true');
    fireEvent.click(screen.getByRole('button', { name: 'Query permissions' }));
    expect(screen.getByLabelText('Table logical name')).toHaveValue('account');
  });

  it('announces a pending query and disables repeat submissions and downloads', async () => {
    let resolve!: (value: Report) => void;
    const api = { catalog: vi.fn().mockResolvedValue(catalog), query: vi.fn(() => new Promise<Report>(done => { resolve = done; })) };
    render(<App api={api} />);
    await screen.findByText('Build a query');
    fireEvent.click(screen.getByRole('button', { name: 'Run query' }));
    expect(screen.getByRole('status')).toHaveTextContent('Evaluating query…');
    expect(screen.getByRole('button', { name: 'Evaluating…' })).toBeDisabled();
    expect(screen.getByRole('button', { name: 'Download JSON' })).toBeDisabled();
    resolve(report);
    await screen.findByText('1 Missing');
    expect(screen.getByRole('button', { name: 'Run query' })).toBeEnabled();
    expect(screen.getByRole('region', { name: 'operation:Update account requirements' })).toHaveAttribute('tabindex', '0');
  });

  it('preserves hidden role selections while searching and restores session defaults explicitly', async () => {
    const api = mockApi(); render(<App api={api} />); await screen.findByText('Build a query');
    fireEvent.click(screen.getByText('Choose application / caller roles'));
    const roles = screen.getByRole('group', { name: 'Application / caller roles' });
    fireEvent.click(within(roles).getByRole('checkbox', { name: 'Reader' }));
    fireEvent.change(screen.getByLabelText('Search application / caller roles'), { target: { value: 'script' } });
    expect(within(roles).queryByRole('checkbox', { name: 'Reader' })).not.toBeInTheDocument();
    fireEvent.click(within(roles).getByRole('checkbox', { name: '<script>alert(1)</script>' }));
    fireEvent.click(screen.getByRole('button', { name: 'Run query' }));
    await waitFor(() => expect(api.query).toHaveBeenLastCalledWith(expect.objectContaining({ roles: ['Reader', '<script>alert(1)</script>'] })));
    fireEvent.click(within(roles).getByRole('button', { name: 'Use session defaults' }));
    fireEvent.click(screen.getByRole('button', { name: 'Run query' }));
    await waitFor(() => expect(api.query).toHaveBeenLastCalledWith(expect.objectContaining({ roles: [] })));
    expect(screen.getByText(/Defaults: Reader \(Dataverse\)/)).toBeInTheDocument();
  });

  it('keeps a compact query editable and labels results from a previous draft', async () => {
    const api = mockApi(); render(<App api={api} />); await screen.findByText('Build a query');
    fireEvent.click(screen.getByRole('button', { name: 'Run query' })); await screen.findByText('1 Missing');
    fireEvent.click(screen.getByRole('button', { name: 'Collapse builder' }));
    expect(screen.queryByRole('combobox', { name: 'Scope' })).not.toBeInTheDocument();
    expect(screen.getByRole('button', { name: 'Run query' })).toBeEnabled();
    fireEvent.click(screen.getByRole('button', { name: 'Edit query' }));
    fireEvent.change(screen.getByLabelText('Scope'), { target: { value: 'operation' } });
    expect(screen.getByRole('button', { name: 'Run query' })).toBeDisabled();
    expect(screen.getByText('Choose a target to run this query.')).toBeInTheDocument();
    expect(screen.getByRole('status')).toHaveTextContent('These results and the JSON download are from the last run');
    fireEvent.change(screen.getByLabelText('Table logical name'), { target: { value: 'account' } });
    fireEvent.click(screen.getByRole('button', { name: 'Run query' }));
    await waitFor(() => expect(screen.queryByRole('status')).not.toBeInTheDocument());
    expect(screen.getByText('Last run: Update account + cascades')).toBeInTheDocument();
  });

  it('clears table-specific columns across scopes without sending hidden operation constraints', async () => {
    const api = mockApi(); render(<App api={api} />); await screen.findByText('Build a query');
    fireEvent.change(screen.getByLabelText('Scope'), { target: { value: 'operation' } });
    fireEvent.change(screen.getByLabelText('Table logical name'), { target: { value: 'account' } });
    fireEvent.click(screen.getByRole('button', { name: 'Changed columns (optional)' }));
    fireEvent.click(screen.getByRole('checkbox', { name: 'Account name (name)' }));
    fireEvent.click(screen.getByRole('button', { name: 'Done' }));
    fireEvent.change(screen.getByLabelText('Scope'), { target: { value: 'method' } });
    fireEvent.change(screen.getByLabelText('Target name'), { target: { value: 'App.Run' } });
    fireEvent.click(screen.getByRole('button', { name: 'Run query' }));
    await waitFor(() => expect(api.query).toHaveBeenLastCalledWith(expect.objectContaining({ scope: 'method', target: 'App.Run', message: null, attributes: [] })));
    fireEvent.change(screen.getByLabelText('Scope'), { target: { value: 'operation' } });
    expect(screen.queryByRole('button', { name: 'Remove column name' })).not.toBeInTheDocument();
  });

  it('clears optional filters and resets to the session starting point', async () => {
    const api = mockApi(); render(<App api={api} />); await screen.findByText('Build a query');
    fireEvent.click(screen.getByText('Report filters'));
    fireEvent.change(screen.getByLabelText('Tables', { selector: 'input' }), { target: { value: 'account, contact' } });
    fireEvent.change(screen.getByLabelText('Privileges'), { target: { value: 'Read' } });
    expect(screen.getByText('3 values applied')).toBeInTheDocument();
    fireEvent.click(screen.getByRole('button', { name: 'Run query' }));
    await waitFor(() => expect(api.query).toHaveBeenLastCalledWith(expect.objectContaining({ tables: ['account', 'contact'], privileges: ['Read'] })));
    fireEvent.click(screen.getByRole('button', { name: 'Clear filters' }));
    expect(screen.getByLabelText('Tables', { selector: 'input' })).toHaveValue('');
    fireEvent.click(screen.getByRole('button', { name: 'Reset query' }));
    expect(screen.getByLabelText('Scope')).toHaveValue('all');
    expect(screen.getByRole('checkbox', { name: /Requirements only/ })).not.toBeChecked();
    fireEvent.click(screen.getByRole('button', { name: 'Run query' }));
    await waitFor(() => expect(api.query).toHaveBeenLastCalledWith(expect.objectContaining({ scope: 'all', roles: [], callerRoles: [], noRoles: false, tables: [], privileges: [] })));
  });

  it('skips both identities in requirements-only mode without discarding explicit selections', async () => {
    const api = mockApi(); render(<App api={api} />); await screen.findByText('Build a query');
    fireEvent.click(screen.getByText('Choose application / caller roles'));
    const roles = screen.getByRole('group', { name: 'Application / caller roles' });
    fireEvent.click(within(roles).getByRole('checkbox', { name: 'Reader' }));
    fireEvent.click(screen.getByText('Impersonated caller roles', { selector: 'summary' }));
    const caller = screen.getByRole('group', { name: 'Impersonated caller roles' });
    fireEvent.click(within(caller).getByRole('checkbox', { name: 'Reader' }));
    fireEvent.click(screen.getByRole('checkbox', { name: /Requirements only/ }));
    expect(within(roles).getByRole('checkbox', { name: 'Reader' })).toBeDisabled();
    expect(within(caller).getByRole('checkbox', { name: 'Reader' })).toBeDisabled();
    fireEvent.click(screen.getByRole('button', { name: 'Run query' }));
    await waitFor(() => expect(api.query).toHaveBeenLastCalledWith(expect.objectContaining({ noRoles: true, roles: [], callerRoles: [] })));
    fireEvent.click(screen.getByRole('checkbox', { name: /Requirements only/ }));
    expect(within(roles).getByRole('checkbox', { name: 'Reader' })).toBeChecked();
    expect(within(caller).getByRole('checkbox', { name: 'Reader' })).toBeChecked();
    fireEvent.click(screen.getByRole('button', { name: 'Run query' }));
    await waitFor(() => expect(api.query).toHaveBeenLastCalledWith(expect.objectContaining({ noRoles: false, roles: ['Reader'], callerRoles: ['Reader'] })));
  });

  it('clears changed columns on table changes and keeps an empty selection as unknown', async () => {
    const api = mockApi(); render(<App api={api} />); await screen.findByText('Build a query');
    fireEvent.change(screen.getByLabelText('Scope'), { target: { value: 'operation' } });
    expect(screen.getByRole('button', { name: 'Changed columns (optional)' })).toBeDisabled();
    fireEvent.change(screen.getByLabelText('Table logical name'), { target: { value: 'account' } });
    fireEvent.click(screen.getByRole('button', { name: 'Changed columns (optional)' }));
    fireEvent.click(screen.getByRole('checkbox', { name: 'Account name (name)' }));
    fireEvent.click(screen.getByRole('button', { name: 'Done' }));
    fireEvent.change(screen.getByLabelText('Table logical name'), { target: { value: 'contact' } });
    expect(screen.queryByRole('button', { name: 'Remove column name' })).not.toBeInTheDocument();
    fireEvent.click(screen.getByRole('button', { name: 'Changed columns (optional)' }));
    expect(screen.getByRole('checkbox', { name: 'Full name (fullname)' })).not.toBeChecked();
    expect(screen.queryByRole('checkbox', { name: 'Account name (name)' })).not.toBeInTheDocument();
    fireEvent.click(screen.getByRole('button', { name: 'Done' }));
    fireEvent.click(screen.getByRole('button', { name: 'Run query' }));
    await waitFor(() => expect(api.query).toHaveBeenLastCalledWith(expect.objectContaining({ target: 'contact', attributes: [] })));
  });

  it('supports old catalogs with manual columns without submitting on Enter', async () => {
    const api = mockApi({ ...catalog, columnCatalog: undefined }); render(<App api={api} />); await screen.findByText('Build a query');
    fireEvent.change(screen.getByLabelText('Scope'), { target: { value: 'operation' } });
    fireEvent.change(screen.getByLabelText('Table logical name'), { target: { value: 'account' } });
    fireEvent.click(screen.getByRole('button', { name: 'Changed columns (optional)' }));
    expect(screen.getByText(/No column catalog for this table/)).toBeInTheDocument();
    const search = screen.getByLabelText('Search columns by display or logical name');
    fireEvent.change(search, { target: { value: 'ctx_custom' } });
    fireEvent.keyDown(search, { key: 'Enter' });
    expect(api.query).not.toHaveBeenCalled();
    expect(screen.getByRole('button', { name: 'Remove column ctx_custom' })).toBeInTheDocument();
    fireEvent.click(screen.getByRole('button', { name: 'Done' }));
    fireEvent.click(screen.getByRole('button', { name: 'Run query' }));
    await waitFor(() => expect(api.query).toHaveBeenLastCalledWith(expect.objectContaining({ attributes: ['ctx_custom'] })));
  });

  it('sends the session token through the typed API and surfaces server errors', async () => {
    const fetchMock = vi.fn().mockResolvedValueOnce(new Response(JSON.stringify({ token: 'session-secret' })))
      .mockResolvedValueOnce(new Response(JSON.stringify(catalog)))
      .mockResolvedValueOnce(new Response(JSON.stringify({ error: 'No entry point matches.' }), { status: 400 }));
    vi.stubGlobal('fetch', fetchMock);
    const api = new PermissionApi(); expect(await api.catalog()).toEqual(catalog);
    expect(fetchMock.mock.calls[1]?.[1].headers['X-XrmMockup-Token']).toBe('session-secret');
    await expect(api.query({ scope: 'method', target: 'Missing' })).rejects.toThrow('No entry point matches.');
    expect(fetchMock.mock.calls[2]?.[1].method).toBe('POST');
  });
});
