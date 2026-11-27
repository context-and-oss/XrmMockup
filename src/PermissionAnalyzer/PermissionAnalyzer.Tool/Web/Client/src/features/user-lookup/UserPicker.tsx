import { useEffect, useRef, useState } from 'react';
import styles from './UserPicker.module.css';
import ui from '../../shared/ui/ui.module.css';
import type { DataverseUser, UserLookupApi, UserSelection } from '../../shared/api/types';
import { uiMessage } from '../../shared/presentation/labels';

export function UserPicker({ api, isCaller, selected, disabled, onSelect, onBusy }: {
  api: UserLookupApi; isCaller: boolean; selected?: UserSelection; disabled: boolean;
  onSelect: (selected?: UserSelection) => void; onBusy: (busy: boolean) => void;
}) {
  const [search, setSearch] = useState('');
  const [matches, setMatches] = useState<DataverseUser[]>();
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState('');
  const active = useRef(true);
  const sequence = useRef(0);
  const id = isCaller ? 'impersonated-user-search' : 'calling-user-search';
  const label = isCaller ? 'Impersonated user' : 'Calling user';
  useEffect(() => { active.current = true; return () => { active.current = false; }; }, []);
  async function lookup() {
    const current = ++sequence.current;
    setBusy(true); onBusy(true); setError(''); setMatches(undefined);
    try {
      const matches = await api.searchUsers(search.trim());
      if (active.current && current === sequence.current) setMatches(matches);
    } catch (error) { if (active.current) setError(uiMessage(error instanceof Error ? error.message : 'Search failed. Try again.')); }
    finally { if (active.current) setBusy(false); onBusy(false); }
  }
  async function choose(user: DataverseUser) {
    setBusy(true); onBusy(true); setError('');
    try { const selection = await api.resolveUser(user.id, isCaller); if (active.current) { onSelect(selection); setMatches(undefined); } }
    catch (error) { if (active.current) setError(uiMessage(error instanceof Error ? error.message : 'Could not read this user’s roles. Try again.')); }
    finally { if (active.current) setBusy(false); onBusy(false); }
  }
  return <section className={styles.picker} aria-label={`${label} lookup`}>
    {selected ? <div className={styles.selection}>
      <strong>{selected.user.name}</strong><p>{selected.user.upn || selected.user.email || selected.user.id}</p>
      <p>{selected.roles.join(' + ')}</p><p className={ui.hint}>Assignments read {new Date(selected.readAt).toLocaleString()} · {selected.environmentUrl}</p>
      <details><summary>Direct and team assignments</summary><ul>{selected.assignments.map((assignment, index) => <li key={index}>{assignment.roleName} · {assignment.teamId ? `Team: ${assignment.teamName || assignment.teamId}` : 'Direct assignment'}</li>)}</ul></details>
      <button type="button" className={ui.textButton} disabled={disabled || busy} onClick={() => { onSelect(undefined); setSearch(''); setMatches(undefined); }}>Use role selection instead</button>
      <button type="button" className={ui.textButton} disabled={disabled || busy} onClick={() => choose(selected.user)}>Refresh assignments</button>
    </div> : <>
      <label htmlFor={id}>Search {label.toLowerCase()} by name or email</label>
      <div className={styles.search}><input id={id} type="search" value={search} autoComplete="off" disabled={disabled || busy}
        placeholder="Start of name, email, or sign-in name…" onChange={event => { setSearch(event.target.value); setMatches(undefined); }}
        onKeyDown={event => { if (event.key === 'Enter') { event.preventDefault(); if (!disabled && !busy && search.trim().length >= 2) void lookup(); } }} />
        <button type="button" className={ui.secondary} disabled={disabled || busy || search.trim().length < 2 || search.trim().length > 200} onClick={lookup}>Search users</button></div>
      {matches && <ul className={styles.matches} aria-label={`${label} search results`}>{matches.map(user => <li key={user.id}>
        <button type="button" disabled={disabled || busy || user.disabled} onClick={() => choose(user)}>
          <strong>{user.name}{user.disabled && ' · Disabled'}</strong><span>{user.upn || user.email || user.id}</span></button></li>)}</ul>}
      {matches && !matches.length && <p className={ui.hint} role="status">No users found. Try a different name or email.</p>}
      {matches?.length === 50 && <p className={ui.hint}>Showing the first 50 users. Refine your search to find another user.</p>}
    </>}
    {busy && <p className={ui.hint} role="status">{matches ? 'Reading direct and team role assignments…' : 'Looking up user information…'}</p>}
    {error && <p className={ui.error} role="alert">{error}</p>}
  </section>;
}
