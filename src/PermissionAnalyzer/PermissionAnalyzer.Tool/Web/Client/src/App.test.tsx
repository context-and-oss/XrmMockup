import { describe, expect, it, vi, afterEach } from 'vitest';
import { fireEvent, render, screen, waitFor } from '@testing-library/react';
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
    fireEvent.change(screen.getByLabelText(/Changed columns/), { target: { value: 'name, ownerid' } });
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
    const roles = screen.getByLabelText('Application / caller roles') as HTMLSelectElement;
    roles.options[0]!.selected = true; roles.options[1]!.selected = true; fireEvent.change(roles);
    const caller = screen.getByLabelText('Impersonated caller roles') as HTMLSelectElement;
    caller.options[0]!.selected = true; fireEvent.change(caller);
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
    expect(screen.getByRole('checkbox')).toBeChecked(); expect(screen.getByRole('checkbox')).toBeDisabled();
    expect(screen.getByLabelText('Application / caller roles')).toBeDisabled();
    fireEvent.change(screen.getByLabelText('Scope'), { target: { value: 'custom-api' } });
    expect(document.querySelector('#targets option')?.getAttribute('value')).toBe('ctx_Run');
    fireEvent.change(screen.getByLabelText('Target name'), { target: { value: 'ctx_Run' } });
    fireEvent.click(screen.getByRole('button', { name: 'Run query' }));
    await waitFor(() => expect(api.query).toHaveBeenCalledWith(expect.objectContaining({ scope: 'custom-api', target: 'ctx_Run', noRoles: true, roles: [] })));
  });

  it('reports startup failures and empty role catalogs', async () => {
    const api = mockApi(); api.catalog.mockRejectedValueOnce(new Error('Server stopped'));
    const first = render(<App api={api} />);
    expect(await screen.findByRole('alert')).toHaveTextContent('Could not load model: Server stopped'); first.unmount();
    render(<App api={mockApi({ ...catalog, roles: [] })} />); await screen.findByText('Build a query');
    fireEvent.click(screen.getByRole('button', { name: 'Role catalog' }));
    expect(screen.getByText(/No security roles loaded/)).toBeInTheDocument();
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
