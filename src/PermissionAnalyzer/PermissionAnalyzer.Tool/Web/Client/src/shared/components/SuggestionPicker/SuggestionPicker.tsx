import styles from './SuggestionPicker.module.css';
import { cx } from '../../ui/classNames';
import { useEffect, useLayoutEffect, useRef, useState } from 'react';
import type { KeyboardEvent } from 'react';
import { createPortal } from 'react-dom';

interface Props {
  id: string;
  label: string;
  value: string;
  options: string[];
  placeholder?: string;
  required?: boolean;
  disabled?: boolean;
  commaSeparated?: boolean;
  onChange: (value: string) => void;
}

// An editable combobox: suggestions help discover loaded metadata, but never
// restrict valid identifiers (for example plugin type:message:stage suffixes).
export function SuggestionPicker({ id, label, value, options, placeholder, required, disabled, commaSeparated = false, onChange }: Props) {
  const [open, setOpen] = useState(false);
  const [browsing, setBrowsing] = useState(true);
  const [active, setActive] = useState(-1);
  const [position, setPosition] = useState({ left: 0, top: 0, width: 0, maxHeight: 280 });
  const root = useRef<HTMLDivElement>(null);
  const input = useRef<HTMLInputElement>(null);
  const popup = useRef<HTMLDivElement>(null);
  const listId = `${id}-suggestions`;
  const tokens = commaSeparated ? value.split(',') : [value];
  const typed = tokens.at(-1)?.trim() || '';
  const preceding = commaSeparated ? tokens.slice(0, -1).map(token => token.trim().toLowerCase()) : [];
  const filter = browsing ? '' : typed.toLowerCase();
  const matches = [...new Set(options)].filter(option => option.toLowerCase().includes(filter) && !preceding.includes(option.toLowerCase()));
  // Bound large metadata lists without hiding the fact that more matches exist.
  const visible = matches.slice(0, 80);
  const matchSignature = JSON.stringify(visible);
  const expanded = open && !disabled;

  useEffect(() => { setActive(-1); }, [value, matchSignature]);
  useEffect(() => { if (disabled) setOpen(false); }, [disabled]);
  useLayoutEffect(() => {
    if (!expanded) return;
    function place() {
      const rect = root.current?.getBoundingClientRect();
      if (!rect) return;
      const margin = 8;
      const viewport = window.visualViewport;
      const viewLeft = viewport?.offsetLeft ?? 0;
      const viewTop = viewport?.offsetTop ?? 0;
      const viewWidth = viewport?.width ?? window.innerWidth;
      const viewHeight = viewport?.height ?? window.innerHeight;
      const width = Math.min(rect.width, viewWidth - margin * 2);
      const below = viewTop + viewHeight - rect.bottom - margin * 2;
      const above = rect.top - viewTop - margin * 2;
      const height = Math.min(280, 88 + visible.length * 44);
      const upwards = below < Math.min(height, 180) && above > below;
      const maxHeight = Math.max(0, Math.min(height, upwards ? above : below));
      setPosition({
        left: Math.max(viewLeft + margin, Math.min(rect.left, viewLeft + viewWidth - width - margin)),
        top: upwards ? rect.top - maxHeight - margin : rect.bottom + margin,
        width, maxHeight,
      });
    }
    place();
    window.addEventListener('resize', place);
    window.addEventListener('scroll', place, true);
    window.visualViewport?.addEventListener('resize', place);
    window.visualViewport?.addEventListener('scroll', place);
    function outside(event: PointerEvent) {
      const target = event.target as Node;
      if (!root.current?.contains(target) && !popup.current?.contains(target)) setOpen(false);
    }
    document.addEventListener('pointerdown', outside);
    return () => {
      window.removeEventListener('resize', place);
      window.removeEventListener('scroll', place, true);
      window.visualViewport?.removeEventListener('resize', place);
      window.visualViewport?.removeEventListener('scroll', place);
      document.removeEventListener('pointerdown', outside);
    };
  }, [expanded, visible.length]);
  useEffect(() => {
    if (active >= 0 && expanded) popup.current?.querySelector(`#${id}-option-${active}`)?.scrollIntoView?.({ block: 'nearest' });
  }, [active, expanded, id]);

  function choose(option: string) {
    onChange(commaSeparated ? [...tokens.slice(0, -1).map(token => token.trim()).filter(Boolean), option].join(', ') : option); setActive(-1); setOpen(false);
  }
  function keyDown(event: KeyboardEvent<HTMLInputElement>) {
    if (event.key === 'ArrowDown' || event.key === 'ArrowUp') {
      event.preventDefault(); setOpen(true);
      setActive(index => !visible.length ? -1 : event.key === 'ArrowDown' ?
        Math.min(index + 1, visible.length - 1) : index < 0 ? visible.length - 1 : Math.max(0, index - 1));
    } else if (event.key === 'Escape' && expanded) {
      event.preventDefault(); event.stopPropagation(); setOpen(false); setActive(-1);
    } else if (event.key === 'Enter' && expanded && !event.nativeEvent.isComposing) {
      // Enter accepts a highlighted suggestion, or keeps the typed identifier.
      // Neither action should accidentally execute the query.
      event.preventDefault();
      if (active >= 0 && visible[active]) choose(visible[active]);
      else setOpen(false);
    } else if (event.key === 'Tab') {
      setOpen(false); setActive(-1);
    }
  }

  return <div className={styles.suggestionPicker} ref={root}>
    <input ref={input} id={id} role="combobox" type="text" value={value} required={required} disabled={disabled}
      placeholder={placeholder} autoComplete="off" spellCheck={false}
      aria-autocomplete="list" aria-expanded={expanded} aria-controls={expanded ? listId : undefined}
      aria-activedescendant={expanded && active >= 0 && visible[active] ? `${id}-option-${active}` : undefined}
      onFocus={() => { setOpen(true); setBrowsing(true); setActive(-1); }} onClick={() => setOpen(true)} onBlur={() => { setOpen(false); setActive(-1); }}
      onChange={event => { onChange(event.target.value); setBrowsing(false); setOpen(true); setActive(-1); }} onKeyDown={keyDown} />
    <button type="button" className={styles.suggestionTrigger} aria-label={`${expanded ? 'Hide' : 'Show'} ${label.toLowerCase()} suggestions`}
      disabled={disabled} tabIndex={-1} aria-expanded={expanded} aria-controls={expanded ? listId : undefined}
      onPointerDown={event => event.preventDefault()} onClick={() => { input.current?.focus(); setBrowsing(true); setOpen(!expanded); setActive(-1); }}>
      <svg viewBox="0 0 20 20" fill="none" stroke="currentColor" strokeWidth="1.5" aria-hidden="true"><path d={expanded ? 'm6 12 4-4 4 4' : 'm6 8 4 4 4-4'} /></svg>
    </button>
    {expanded && createPortal(<div ref={popup} className={styles.suggestionPopup} style={position}>
      <div className={styles.suggestionCaption}>{label}<span aria-live="polite" aria-atomic="true">{matches.length} {matches.length === 1 ? 'match' : 'matches'}</span></div>
      <div id={listId} role="listbox" aria-label={`${label} suggestions`} className={styles.suggestionList}>
        {visible.map((option, index) => <div key={option} id={`${id}-option-${index}`} role="option"
          aria-selected={typed === option} className={cx(styles.suggestionOption, active === index && styles.isActive)}
          onPointerDown={event => event.preventDefault()} onClick={() => choose(option)}>
          <span>{option}</span>{typed === option && <svg viewBox="0 0 20 20" fill="none" stroke="currentColor" strokeWidth="1.5" aria-hidden="true"><path d="m4 10 4 4 8-8" /></svg>}
        </div>)}
      </div>
      {!visible.length && <p className={styles.suggestionEmpty}>{options.length ? 'No matching suggestions.' : 'No suggestions in the loaded model.'} You can still use the value you enter.</p>}
      <div className={styles.suggestionFooter}>{matches.length > visible.length ? 'Showing the first 80 matches. Keep typing to narrow the list.' : commaSeparated ? 'Select a value, then type a comma to add another.' : 'Type to search, or enter your own value.'}</div>
    </div>, document.body)}
  </div>;
}
