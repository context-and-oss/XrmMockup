import type { Catalog, Query, Report } from './types';

export class PermissionApi {
  private token = '';

  async catalog(): Promise<Catalog> {
    this.token = (await this.request<{ token: string }>('/api/session')).token;
    return this.request<Catalog>('/api/model');
  }

  query(query: Query): Promise<Report> { return this.request<Report>('/api/query', query); }

  private async request<T>(path: string, query?: Query): Promise<T> {
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

