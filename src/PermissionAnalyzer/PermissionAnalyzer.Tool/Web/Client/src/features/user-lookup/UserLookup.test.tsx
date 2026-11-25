import { fireEvent, render, screen, waitFor, within } from '@testing-library/react';
import { describe, expect, it, vi } from 'vitest';
import { App } from '../../app/App';
import { PermissionApi } from '../../shared/api/PermissionApi';
import type { Catalog, DataverseUser, Report, UserSelection } from '../../shared/api/types';

const url = 'https://example.crm.dynamics.com/';
const alice: DataverseUser = { id: '11111111-1111-1111-1111-111111111111', name: 'Alice Example', email: 'alice@example.com', upn: 'alice@example.com', disabled: false };
const bob: DataverseUser = { ...alice, id: '22222222-2222-2222-2222-222222222222', name: 'Bob Example', email: 'bob@example.com', upn: 'bob@example.com' };
function selection(user: DataverseUser, isCaller: boolean): UserSelection {
  return { selectionId: isCaller ? 'B'.repeat(32) : 'A'.repeat(32), user, environmentUrl: url, readAt: '2026-11-25T12:00:00Z', roles: ['Reader', 'Writer'], isCaller,
    assignments: [{ roleName: 'Reader', rootRoleId: 'reader-id', teamName: null, teamId: null }, { roleName: 'Writer', rootRoleId: 'writer-id', teamName: 'Operations', teamId: 'team-id' }] };
}
const catalog: Catalog = { requirementsOnly: false, incomplete: false, defaults: { roles: ['Reader'], callerRoles: [], roleOrigin: '--role' }, tables: ['account'], methods: [], entryPoints: [],
  userLookup: { available: true, environmentUrl: url, credentialType: 'browser' }, columnCatalog: [],
  roles: [{ name: 'Reader', roleId: 'reader-id', globalPrivilegesKnown: true, grants: [] }, { name: 'Writer', roleId: 'writer-id', globalPrivilegesKnown: true, grants: [] }] };
const report: Report = { analysis: { incomplete: false, truncatedSections: [] }, entryPoints: [], unreached: [] };
function mockApi(model = catalog) {
  return { catalog: vi.fn().mockResolvedValue(model), query: vi.fn().mockResolvedValue(report),
    usersConnection: vi.fn().mockResolvedValue({ connected: false, environmentUrl: null }),
    connectUsers: vi.fn().mockResolvedValue({ connected: true, environmentUrl: url }),
    disconnectUsers: vi.fn().mockResolvedValue({ connected: false, environmentUrl: null }),
    searchUsers: vi.fn().mockResolvedValue([alice, bob]),
    resolveUser: vi.fn(async (id: string, isCaller: boolean) => selection(id === alice.id ? alice : bob, isCaller)),
  };
}
async function connect() {
  const toggle = await screen.findByRole('button', { name: 'Connect to Dataverse' });
  await waitFor(() => expect(toggle).toBeEnabled()); fireEvent.click(toggle);
  fireEvent.click(screen.getByRole('button', { name: 'Connect' }));
  await screen.findByText('Connected for user lookup');
}
async function chooseCaller() {
  fireEvent.click(screen.getByRole('button', { name: 'Look up calling user' }));
  const field = screen.getByLabelText('Search calling user by name or email');
  fireEvent.change(field, { target: { value: 'Example' } });
  fireEvent.keyDown(field, { key: 'Enter' });
  const results = await screen.findByRole('list', { name: 'Calling user search results' });
  fireEvent.click(within(results).getByRole('button', { name: /Alice Example/ }));
  await screen.findByText('Calling user: Alice Example. Live assignments; local privilege definitions.');
}

describe('Connected user lookup', () => {
  it('connects only on explicit action and selects calling and impersonated identities separately', async () => {
    const api = mockApi(); render(<App api={api} />); await screen.findByText('Build a query');
    expect(api.connectUsers).not.toHaveBeenCalled(); expect(api.searchUsers).not.toHaveBeenCalled();
    await connect(); expect(api.connectUsers).toHaveBeenCalledWith(url, 'browser');
    await chooseCaller(); expect(api.resolveUser).toHaveBeenCalledWith(alice.id, false);
    fireEvent.click(screen.getByText('Impersonated caller roles', { selector: 'summary' }));
    fireEvent.click(screen.getByRole('button', { name: 'Look up impersonated user' }));
    fireEvent.change(screen.getByLabelText('Search impersonated user by name or email'), { target: { value: 'Bob' } });
    const impersonated = screen.getByRole('region', { name: 'Impersonated user lookup' });
    fireEvent.click(within(impersonated).getByRole('button', { name: 'Search users' }));
    const matches = await screen.findByRole('list', { name: 'Impersonated user search results' });
    fireEvent.click(within(matches).getByRole('button', { name: /Bob Example/ }));
    await waitFor(() => expect(api.resolveUser).toHaveBeenCalledWith(bob.id, true));
    await within(impersonated).findByText('Bob Example');
    fireEvent.click(screen.getByRole('button', { name: 'Run query' }));
    await waitFor(() => expect(api.query).toHaveBeenCalledWith(expect.objectContaining({ userSelection: 'A'.repeat(32), callerUserSelection: 'B'.repeat(32), roles: [], callerRoles: [] })));
    expect(screen.getByText(/Last run:/)).toBeInTheDocument();
    expect(screen.getAllByText(/Calling user: Alice Example · Reader/).length).toBeGreaterThan(0);
    fireEvent.click(within(impersonated).getByText('Direct and team assignments'));
    expect(within(impersonated).getByText('Reader · Direct assignment')).toBeInTheDocument();
    expect(within(impersonated).getByText('Writer · Team: Operations')).toBeInTheDocument();
  });

  it('reports authentication and search failures, supports retries, and preserves selected roles on refresh failure', async () => {
    const api = mockApi(); api.connectUsers.mockRejectedValueOnce(new Error('Sign-in failed. Try again.'));
    render(<App api={api} />);
    const button = await screen.findByRole('button', { name: 'Connect to Dataverse' }); await waitFor(() => expect(button).toBeEnabled());
    fireEvent.click(button); fireEvent.click(screen.getByRole('button', { name: 'Connect' }));
    expect(await screen.findByRole('alert')).toHaveTextContent('Sign-in failed');
    expect(screen.getByLabelText('Environment URL')).toHaveValue(url);
    fireEvent.click(screen.getByRole('button', { name: 'Connect' })); await screen.findByText('Connected for user lookup');
    api.searchUsers.mockRejectedValueOnce(new Error('Could not read users. Try again.'));
    fireEvent.click(screen.getByRole('button', { name: 'Look up calling user' }));
    fireEvent.change(screen.getByLabelText('Search calling user by name or email'), { target: { value: 'Example' } });
    fireEvent.click(screen.getByRole('button', { name: 'Search users' }));
    expect(await screen.findByRole('alert')).toHaveTextContent('Could not read users');
    fireEvent.click(screen.getByRole('button', { name: 'Search users' }));
    const matches = await screen.findByRole('list', { name: 'Calling user search results' });
    api.resolveUser.mockRejectedValueOnce(new Error('A user role is missing from local metadata.'));
    fireEvent.click(within(matches).getByRole('button', { name: /Alice Example/ }));
    expect(await screen.findByRole('alert')).toHaveTextContent('missing from local metadata');
    fireEvent.click(within(matches).getByRole('button', { name: /Alice Example/ }));
    await screen.findByText('Calling user: Alice Example. Live assignments; local privilege definitions.');
    api.resolveUser.mockRejectedValueOnce(new Error('Refresh failed.'));
    fireEvent.click(screen.getByRole('button', { name: 'Refresh assignments' }));
    expect(await screen.findByRole('alert')).toHaveTextContent('Refresh failed');
    fireEvent.click(screen.getByRole('button', { name: 'Run query' }));
    await waitFor(() => expect(api.query).toHaveBeenLastCalledWith(expect.objectContaining({ userSelection: 'A'.repeat(32) })));
  });

  it('disconnects and clears live selections while preserving the frozen report and offline role workflow', async () => {
    const api = mockApi(); render(<App api={api} />); await connect(); await chooseCaller();
    fireEvent.click(screen.getByRole('button', { name: 'Run query' })); await screen.findByText('Last run: All entry points & unreached methods');
    fireEvent.click(screen.getByRole('button', { name: 'Disconnect' }));
    await screen.findByRole('button', { name: 'Connect to Dataverse' });
    expect(screen.queryByText('Calling user: Alice Example. Live assignments; local privilege definitions.')).not.toBeInTheDocument();
    expect(await screen.findByText(/Query changed. These results/)).toBeInTheDocument();
    fireEvent.click(screen.getByRole('button', { name: 'Run query' }));
    await waitFor(() => expect(api.query).toHaveBeenLastCalledWith(expect.not.objectContaining({ userSelection: expect.anything() })));
    expect(api.disconnectUsers).toHaveBeenCalledOnce();
  });

  it('skips live identities for requirements-only queries, and restores them when coverage resumes', async () => {
    const api = mockApi(); render(<App api={api} />); await connect(); await chooseCaller();
    fireEvent.click(screen.getByRole('checkbox', { name: /Requirements only/ }));
    expect(screen.getByRole('button', { name: 'Refresh assignments' })).toBeDisabled();
    fireEvent.click(screen.getByRole('button', { name: 'Run query' }));
    await waitFor(() => expect(api.query).toHaveBeenLastCalledWith(expect.objectContaining({ noRoles: true, roles: [], callerRoles: [] })));
    expect(api.query.mock.calls.at(-1)?.[0]).not.toHaveProperty('userSelection');
    fireEvent.click(screen.getByRole('checkbox', { name: /Requirements only/ }));
    fireEvent.click(screen.getByRole('button', { name: 'Run query' }));
    await waitFor(() => expect(api.query).toHaveBeenLastCalledWith(expect.objectContaining({ userSelection: 'A'.repeat(32) })));
    fireEvent.click(screen.getByRole('button', { name: 'Use role selection instead' }));
    expect(screen.queryByRole('region', { name: 'Calling user lookup' })).not.toBeInTheDocument();
  });

  it('restores connection status after a reload without reconnecting, and disallows lookup in fixed requirements-only sessions', async () => {
    const api = mockApi(); api.usersConnection.mockResolvedValue({ connected: true, environmentUrl: url });
    const view = render(<App api={api} />); await screen.findByText('Connected for user lookup'); expect(api.connectUsers).not.toHaveBeenCalled();
    view.unmount(); const fixed = mockApi({ ...catalog, requirementsOnly: true }); render(<App api={fixed} />); await screen.findByText('Build a query');
    expect(screen.queryByRole('button', { name: 'Connect to Dataverse' })).not.toBeInTheDocument(); expect(fixed.usersConnection).not.toHaveBeenCalled();
  });

  it('disables disabled-user choices and shows empty search feedback', async () => {
    const api = mockApi(); api.searchUsers.mockResolvedValueOnce([{ ...alice, disabled: true }]).mockResolvedValueOnce([]);
    render(<App api={api} />); await connect(); fireEvent.click(screen.getByRole('button', { name: 'Look up calling user' }));
    fireEvent.change(screen.getByLabelText('Search calling user by name or email'), { target: { value: 'Alice' } });
    fireEvent.click(screen.getByRole('button', { name: 'Search users' }));
    expect(await screen.findByRole('button', { name: /Alice Example · Disabled/ })).toBeDisabled();
    fireEvent.click(screen.getByRole('button', { name: 'Search users' }));
    await screen.findByText('No users found. Try a different name or email.'); expect(api.resolveUser).not.toHaveBeenCalled();
  });

  it('keeps the session token on connection and lookup requests without sending credentials', async () => {
    const requests: { url: string; init: RequestInit }[] = [];
    const original = globalThis.fetch;
    globalThis.fetch = vi.fn(async (url, init) => {
      requests.push({ url: String(url), init: init! });
      const data = String(url).endsWith('/session') ? { token: 'local-session' } : String(url).endsWith('/model') ? catalog : [];
      return new Response(JSON.stringify(data));
    });
    try {
      const api = new PermissionApi(); await api.catalog(); await api.connectUsers(url, 'devicecode'); await api.searchUsers('Alice'); await api.resolveUser(alice.id, true); await api.disconnectUsers();
      for (const request of requests.slice(2)) expect(request.init.headers).toMatchObject({ 'X-XrmMockup-Token': 'local-session' });
      expect(requests[2]!.init.body).toBe(JSON.stringify({ environmentUrl: url, credentialType: 'devicecode' }));
      expect(requests[4]!.init.body).toBe(JSON.stringify({ userId: alice.id, isCaller: true }));
    } finally { globalThis.fetch = original; }
  });
});
