import { describe, expect, it } from 'vitest';
import { roleOriginLabel, sectionTitle, uiMessage } from './labels';

describe('Explorer labels', () => {
  it('presents role origins and server guidance without command switches', () => {
    expect(roleOriginLabel('--role')).toBe('Session roles');
    expect(roleOriginLabel('--caller-role')).toBe('Session roles');
    expect(roleOriginLabel('Principals: Contoso.Sync')).toBe('Application role mapping · Contoso.Sync');
    expect(roleOriginLabel('Dataverse')).toBe('Dataverse');
    expect(uiMessage('Pass --metadata and --role, or use --no-roles.')).toBe('Pass local metadata and application roles, or use requirements-only mode.');
  });
  it('shows entry-point titles without wire-format prefixes while preserving distinguishing identifiers', () => {
    expect(sectionTitle('operation:Update paa_pipe(paa_measuredlength)')).toBe('Update paa_pipe (paa_measuredlength)');
    expect(sectionTitle('function:Contoso.Sync/Run')).toBe('Contoso.Sync/Run');
    expect(sectionTitle('custom-api:ctx_Process')).toBe('ctx_Process');
  });
});
