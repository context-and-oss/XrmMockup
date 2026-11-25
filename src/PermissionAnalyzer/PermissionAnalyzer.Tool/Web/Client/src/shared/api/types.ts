export type Scope = 'all' | 'operation' | 'plugin' | 'custom-api' | 'function' | 'method';
export type Identity = 'Caller' | 'User' | 'ApplicationUser' | 'ImpersonatedCaller' | 'System';
export type CoverageStatus = 'granted' | 'missing' | 'not-checked' | 'not-governed' | 'suppressed' | 'system' | 'unverifiable';
export interface Grant { table: string | null; privilege: string; depth: string }
export interface Role { name: string; roleId: string | null; globalPrivilegesKnown: boolean; grants: Grant[] }
export interface ColumnDefinition { logicalName: string; displayName: string | null }
export interface TableColumnCatalog { table: string; displayName: string | null; columns: ColumnDefinition[] }
export type CredentialType = 'browser' | 'devicecode' | 'azcli';
export interface UserLookupOptions { available: boolean; environmentUrl: string; credentialType: CredentialType }
export interface UserConnection { connected: boolean; environmentUrl: string | null }
export interface DataverseUser { id: string; name: string; email: string | null; upn: string | null; disabled: boolean }
export interface UserSelection {
  selectionId: string; user: DataverseUser; environmentUrl: string; readAt: string; roles: string[]; isCaller: boolean;
  assignments: { roleName: string; rootRoleId: string; teamName: string | null; teamId: string | null }[];
}
export interface UserLookupApi {
  usersConnection: () => Promise<UserConnection>;
  connectUsers: (environmentUrl: string, credentialType: CredentialType) => Promise<UserConnection>;
  disconnectUsers: () => Promise<UserConnection>;
  searchUsers: (search: string) => Promise<DataverseUser[]>;
  resolveUser: (userId: string, isCaller: boolean) => Promise<UserSelection>;
}
export interface Catalog {
  userLookup?: UserLookupOptions | null;
  columnCatalog?: TableColumnCatalog[];
  requirementsOnly: boolean;
  incomplete: boolean;
  defaults: { roles: string[]; callerRoles: string[]; roleOrigin?: string | null; callerRoleOrigin?: string | null };
  roles: Role[];
  tables: string[];
  entryPoints: { id: string; kind: string; type: string }[];
  methods: string[];
}
export interface Query {
  userSelection?: string;
  callerUserSelection?: string;
  scope: Scope;
  target?: string | null;
  message?: string | null;
  attributes?: string[];
  roles?: string[];
  callerRoles?: string[];
  tables?: string[];
  privileges?: string[];
  noRoles?: boolean;
}
export interface Source { file: string; line: number; column: number; path: string[] }
export interface Requirement {
  table?: string;
  privilege: string;
  runsAs: Identity;
  userId?: string;
  assumed?: boolean;
  basis: string;
  sources: Source[];
  coverage?: { status: CoverageStatus; depth?: string; reason?: string };
}
export interface Section {
  id: string;
  kind?: string;
  requirements: Requirement[];
  unresolved: { operation: string; reason: string; source: Source }[];
  assumptions: { reason: string; source: Source }[];
  triggers?: string[];
  roles?: { runsAs: Identity; userId?: string; roles?: string[]; origin?: string; note?: string }[];
}
export interface Report {
  analysis: { incomplete: boolean; truncatedSections: string[]; reportFilter?: string };
  entryPoints: Section[];
  unreached: Section[];
  tablesWithoutRoleMetadata?: string[];
  result?: { granted: number; missing: number; notChecked: number; unresolved: number; unverifiable: number;
    truncated: number; suppressed: number; system: number; notGoverned: number };
}
