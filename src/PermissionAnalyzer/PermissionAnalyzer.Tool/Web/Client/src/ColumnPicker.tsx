import { useEffect, useRef, useState } from 'react';
import type { TableColumnCatalog } from './model';

interface Props {
  table: string;
  catalog?: TableColumnCatalog;
  selected: string[];
  disabled: boolean;
  onChange: (columns: string[]) => void;
}

export function ColumnPicker({ table, catalog, selected, disabled, onChange }: Props) {
  const [open, setOpen] = useState(false);
  const [search, setSearch] = useState('');
  const root = useRef<HTMLDivElement>(null);
  const searchInput = useRef<HTMLInputElement>(null);
  const trigger = useRef<HTMLButtonElement>(null);
  const columns = catalog?.columns ?? [];
  const filter = search.trim().toLowerCase();
  const matches = columns.filter(column => `${column.displayName || ''} ${column.logicalName}`.toLowerCase().includes(filter));
  const manual = search.trim().toLowerCase();
  const validManual = /^[a-z_][a-z0-9_]*$/.test(manual) && manual.length <= 1024;
  const canAdd = validManual && !columns.some(column => column.logicalName.toLowerCase() === manual) && !selected.includes(manual);
  const limit = selected.length >= 100;
  useEffect(() => { setOpen(false); setSearch(''); }, [table]);
  useEffect(() => { if (disabled) setOpen(false); }, [disabled]);
  useEffect(() => {
    if (!open) return;
    searchInput.current?.focus();
    function outside(event: PointerEvent) {
      if (!root.current?.contains(event.target as Node)) setOpen(false);
    }
    document.addEventListener('pointerdown', outside);
    return () => document.removeEventListener('pointerdown', outside);
  }, [open]);
  function toggle(name: string) {
    if (selected.includes(name)) onChange(selected.filter(value => value !== name));
    else if (!limit) onChange([...selected, name]);
  }
  function add() {
    if (!canAdd || limit) return;
    onChange([...selected, manual]); setSearch(''); searchInput.current?.focus();
  }

  return <div className="column-picker" ref={root} onKeyDown={event => {
    if (event.key === 'Escape' && open) { event.preventDefault(); event.stopPropagation(); setOpen(false); trigger.current?.focus(); }
  }} onBlur={event => { if (!event.currentTarget.contains(event.relatedTarget as Node)) setOpen(false); }}>
    <button ref={trigger} type="button" className="column-trigger" disabled={disabled || !table.trim()}
      aria-label="Changed columns (optional)" aria-describedby="attributes-help" aria-expanded={open} aria-controls="column-options"
      onClick={() => setOpen(!open)}>
      <span>{selected.length ? `${selected.length} ${selected.length === 1 ? 'column' : 'columns'} selected` : table.trim() ? 'Choose changed columns…' : 'Choose a table first'}</span>
      <svg viewBox="0 0 20 20" fill="none" stroke="currentColor" strokeWidth="1.5" aria-hidden="true"><path d={open ? 'm6 12 4-4 4 4' : 'm6 8 4 4 4-4'} /></svg>
    </button>
    {selected.length > 0 && <ul className="column-chips" aria-label="Selected changed columns">{selected.map(name => {
      const column = columns.find(c => c.logicalName === name);
      return <li key={name}><span>{column?.displayName || name}<small>{column?.displayName ? name : column ? '' : 'Manual value'}</small></span>
        <button type="button" disabled={disabled} aria-label={`Remove column ${name}`} onClick={() => { onChange(selected.filter(value => value !== name)); (open ? searchInput.current : trigger.current)?.focus(); }}>×</button>
      </li>;
    })}</ul>}
    {open && !disabled && <section id="column-options" className="column-options-panel" aria-label={`Changed columns for ${table}`}>
      <div className="column-panel-heading"><strong>{catalog?.displayName || table}</strong><span>{columns.length} columns in catalog</span></div>
      <label className="sr-only" htmlFor="column-search">Search columns by display or logical name</label>
      <input ref={searchInput} id="column-search" type="search" autoComplete="off" value={search} placeholder="Search columns…"
        onChange={event => setSearch(event.target.value)} onKeyDown={event => { if (event.key === 'Enter') { event.preventDefault(); add(); } }} />
      {!catalog && <p className="hint">No column catalog for this table. Enter a logical name to add it manually.</p>}
      <fieldset className="column-choices"><legend className="sr-only">Available changed columns</legend>
        {matches.slice(0, 100).map(column => <label className="check column-choice" key={column.logicalName}>
          <input type="checkbox" aria-label={column.displayName ? `${column.displayName} (${column.logicalName})` : column.logicalName} checked={selected.includes(column.logicalName)} disabled={limit && !selected.includes(column.logicalName)} onChange={() => toggle(column.logicalName)} />
          <span>{column.displayName || column.logicalName}{column.displayName && <small>{column.logicalName}</small>}</span>
        </label>)}
        {!matches.length && catalog && <p className="hint">{columns.length ? 'No columns match your search.' : 'No columns recorded for this table.'}</p>}
      </fieldset>
      {matches.length > 100 && <p className="hint">Showing 100 of {matches.length} matches. Keep typing to narrow the list.</p>}
      {canAdd && <button type="button" className="column-manual" disabled={limit} onClick={add}>Add “{manual}” manually</button>}
      {search.trim() && !validManual && !matches.length && <p className="hint">For a manual value, enter one logical name using letters, numbers, and underscores (up to 1024 characters).</p>}
      <div className="column-panel-footer"><span role="status">{selected.length ? `${selected.length} selected${limit ? ' · Maximum 100 columns' : ''}` : 'None selected · changed columns are unknown'}</span>
        <div><button type="button" className="text-button" disabled={!selected.length} onClick={() => { onChange([]); searchInput.current?.focus(); }}>Clear</button>
          <button type="button" className="text-button" onClick={() => { setOpen(false); trigger.current?.focus(); }}>Done</button></div>
      </div>
    </section>}
  </div>;
}
