import styles from './RolePicker.module.css';
import ui from '../../shared/ui/ui.module.css';
import { cx } from '../../shared/ui/classNames';
import { useState } from 'react';

interface Props {
  id: string;
  label: string;
  names: string[];
  selected: string[];
  disabled: boolean;
  onChange: (roles: string[]) => void;
}

// Native checkboxes keep selection usable with a keyboard and on touch devices.
// Filtering the list must never discard a selection that is currently hidden.
export function RolePicker({ id, label, names, selected, disabled, onChange }: Props) {
  const [search, setSearch] = useState('');
  const matches = names.filter(name => name.toLowerCase().includes(search.trim().toLowerCase()));
  function toggle(name: string) {
    onChange(names.filter(value => value === name ? !selected.includes(value) : selected.includes(value)));
  }
  return <fieldset className={styles.rolePicker} disabled={disabled}>
    <legend>{label}</legend>
    <label className={ui.srOnly} htmlFor={`${id}-search`}>Search {label.toLowerCase()}</label>
    <input id={`${id}-search`} type="search" value={search} onChange={event => setSearch(event.target.value)} placeholder="Search roles…" />
    <div className={styles.roleOptions}>
      {matches.map(name => <label className={cx(ui.check, styles.roleOption)} key={name}>
        <input type="checkbox" checked={selected.includes(name)} onChange={() => toggle(name)} />
        <span>{name}</span>
      </label>)}
      {!matches.length && <p className={ui.hint}>{names.length ? 'No roles match. Try another search.' : 'No security roles loaded.'}</p>}
    </div>
    <div className={styles.rolePickerFooter}><span>{selected.length} explicitly selected</span>
      <button type="button" className={ui.textButton} disabled={!selected.length} onClick={() => onChange([])}>Use session defaults</button>
    </div>
  </fieldset>;
}
