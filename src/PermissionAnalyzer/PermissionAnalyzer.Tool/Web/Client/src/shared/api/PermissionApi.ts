import type { Catalog, Query, Report, UserConnection, DataverseUser, UserSelection, CredentialType } from './types';

export class PermissionApi {
  private token = '';

  async catalog(): Promise<Catalog> {
    this.token = (await this.request<{ token: string }>('/api/session')).token;
    return this.request<Catalog>('/api/model');
  }

  query(query: Query): Promise<Report> { return this.request<Report>('/api/query', query); }

  usersConnection(): Promise<UserConnection> { return this.request('/api/users/status'); }
  connectUsers(environmentUrl: string, credentialType: CredentialType): Promise<UserConnection> {
    return this.request('/api/users/connect', { environmentUrl, credentialType });
  }
  disconnectUsers(): Promise<UserConnection> { return this.request('/api/users/disconnect', {}); }
  searchUsers(search: string): Promise<DataverseUser[]> { return this.request('/api/users/search', { search }); }
  resolveUser(userId: string, isCaller: boolean): Promise<UserSelection> { return this.request('/api/users/resolve', { userId, isCaller }); }

  private async request<T>(path: string, query?: object): Promise<T> {
    const response = await fetch(path, {
      method: query ? 'POST' : 'GET',
      headers: { 'X-XrmMockup-Token': this.token, ...(query ? { 'Content-Type': 'application/json' } : {}) },
      ...(query ? { body: JSON.stringify(query) } : {}),
    });
    const body = await response.json() as T & { error?: string };
    if (!response.ok) throw new Error(body.error || `Request failed (${response.status})`);
    return body;
  }
}

