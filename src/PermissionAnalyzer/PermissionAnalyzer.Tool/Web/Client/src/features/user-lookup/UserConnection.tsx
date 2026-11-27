import { useEffect, useState } from 'react';
import styles from './UserConnection.module.css';
import ui from '../../shared/ui/ui.module.css';
import type { CredentialType, UserConnection as Connection, UserLookupApi, UserLookupOptions } from '../../shared/api/types';
import { uiMessage } from '../../shared/presentation/labels';

export function UserConnection({ api, options, connection, disabled, onChange, onBusy }: {
  api: UserLookupApi; options: UserLookupOptions; connection?: Connection; disabled: boolean;
  onChange: (connection: Connection) => void; onBusy: (busy: boolean) => void;
}) {
  const [loading, setLoading] = useState(true);
  const [expanded, setExpanded] = useState(false);
  const [url, setUrl] = useState(options.environmentUrl);
  const [credential, setCredential] = useState<CredentialType>(options.credentialType || 'browser');
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState('');
  // Local status only: recover an existing connection after a page reload without
  // making a Dataverse call or starting authentication automatically.
  useEffect(() => {
    let active = true;
    api.usersConnection().then(connection => { if (active) onChange(connection); })
      .catch(error => { if (active) setError(uiMessage(error instanceof Error ? error.message : 'Could not read the connection status.')); })
      .finally(() => { if (active) setLoading(false); });
    return () => { active = false; };
  }, [api]);
  async function connect() {
    setBusy(true); onBusy(true); setError('');
    try { onChange(await api.connectUsers(url.trim(), credential)); setExpanded(false); }
    catch (error) { setError(uiMessage(error instanceof Error ? error.message : 'Could not connect. Check your local sign-in and try again.')); }
    finally { setBusy(false); onBusy(false); }
  }
  async function disconnect() {
    setBusy(true); onBusy(true); setError('');
    try { onChange(await api.disconnectUsers()); }
    catch (error) { setError(uiMessage(error instanceof Error ? error.message : 'Could not disconnect. Try again.')); }
    finally { setBusy(false); onBusy(false); }
  }
  return <section className={styles.connection} aria-label="Dataverse user lookup">
    <div className={styles.heading}><div><h3>{connection?.connected ? 'Connected for user lookup' : 'Optional · Look up Dataverse users'}</h3>
      {connection?.connected ? <div className={styles.connectionDetails}>
        <span>{connection.environmentUrl}</span>
        {connection.connectedUser && <span className={styles.identity} aria-label="Connected Dataverse user">
          <strong>{connection.connectedUser.name}</strong>
          <span>{connection.connectedUser.upn || connection.connectedUser.email || connection.connectedUser.id}</span>
        </span>}
      </div> : <p>Use live user assignments with the security-role definitions loaded in this session.</p>}</div>
      {connection?.connected ? <button type="button" className={ui.textButton} disabled={loading || disabled || busy} onClick={disconnect}>{busy ? 'Disconnecting…' : 'Disconnect'}</button> :
        <button type="button" className={ui.textButton} aria-expanded={expanded} aria-controls="user-connection-settings" disabled={loading || disabled || busy || !options.available} onClick={() => setExpanded(!expanded)}>{expanded ? 'Hide connection' : 'Connect to Dataverse'}</button>}
    </div>
    {!options.available && <p className={ui.hint}>User lookup requires a local role catalog. Restart with role metadata or a saved analysis that contains roles.</p>}
    {expanded && !connection?.connected && <div id="user-connection-settings" className={styles.settings}>
      <div><label htmlFor="dataverse-url">Environment URL</label><input id="dataverse-url" type="url" value={url} placeholder="https://your-org.crm.dynamics.com" disabled={busy || disabled} onChange={event => setUrl(event.target.value)} /></div>
      <div><label htmlFor="dataverse-sign-in">Sign-in method</label><select id="dataverse-sign-in" value={credential} disabled={busy || disabled} onChange={event => setCredential(event.target.value as CredentialType)}>
        <option value="browser">Browser sign-in</option><option value="devicecode">Device code · local terminal</option><option value="azcli">Existing Azure CLI sign-in</option></select></div>
      <button type="button" className={ui.secondary} disabled={busy || disabled || !url.trim()} onClick={connect}>{busy ? 'Connecting…' : 'Connect'}</button>
      <p className={ui.hint}>Sign-in runs in the local analyzer, not this page. Browser sign-in opens a local browser; device-code instructions appear in its terminal. No connection is made until you select Connect.</p>
    </div>}
    {busy && <p role="status" className={ui.hint}>{connection?.connected ? 'Disconnecting and clearing live user selections…' : 'Complete sign-in in the local browser or analyzer terminal. Authentication may take a moment.'}</p>}
    {error && <p role="alert" className={ui.error}>{error}</p>}
  </section>;
}
