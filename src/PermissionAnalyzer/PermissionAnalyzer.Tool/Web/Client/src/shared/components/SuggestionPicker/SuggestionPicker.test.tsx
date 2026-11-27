import { useState } from 'react';
import { describe, expect, it, vi } from 'vitest';
import { fireEvent, render, screen } from '@testing-library/react';
import { SuggestionPicker } from './SuggestionPicker';

function Harness({ options = ['account', 'contact', 'businessunit'], initial = '', disabled = false, commaSeparated = false }: {
  options?: string[]; initial?: string; disabled?: boolean; commaSeparated?: boolean;
}) {
  const [value, setValue] = useState(initial);
  return <form onSubmit={event => event.preventDefault()}>
    <label htmlFor="test-picker">Table logical name</label>
    <SuggestionPicker id="test-picker" label="Tables" options={options} value={value} onChange={setValue} disabled={disabled} commaSeparated={commaSeparated} />
    <button type="button">Outside</button>
  </form>;
}

describe('Editable suggestion picker', () => {
  it('browses all suggestions on focus and filters case-insensitively while typing', () => {
    render(<Harness initial="account" />);
    const input = screen.getByRole('combobox', { name: 'Table logical name' });
    fireEvent.focus(input);
    expect(input).toHaveAttribute('aria-expanded', 'true');
    expect(screen.getAllByRole('option')).toHaveLength(3);
    expect(screen.getByRole('option', { name: 'account' })).toHaveAttribute('aria-selected', 'true');
    fireEvent.change(input, { target: { value: 'CO' } });
    expect(screen.getAllByRole('option')).toHaveLength(2);
    expect(screen.queryByRole('option', { name: 'businessunit' })).not.toBeInTheDocument();
    expect(input).not.toHaveAttribute('list');
    expect(document.querySelector('datalist')).toBeNull();
  });

  it('keeps focus on the input and selects using arrow keys and Enter without submitting', () => {
    render(<Harness />);
    const input = screen.getByRole('combobox');
    const submit = vi.fn(); input.closest('form')!.addEventListener('submit', submit);
    fireEvent.focus(input);
    fireEvent.keyDown(input, { key: 'ArrowDown' });
    expect(input).toHaveAttribute('aria-activedescendant', 'test-picker-option-0');
    fireEvent.keyDown(input, { key: 'ArrowDown' });
    expect(input).toHaveAttribute('aria-activedescendant', 'test-picker-option-1');
    fireEvent.keyDown(input, { key: 'Enter' });
    expect(input).toHaveValue('contact');
    expect(input).toHaveAttribute('aria-expanded', 'false');
    expect(submit).not.toHaveBeenCalled();
    fireEvent.keyDown(input, { key: 'ArrowUp' });
    expect(input).toHaveAttribute('aria-activedescendant', 'test-picker-option-2');
    fireEvent.keyDown(input, { key: 'Escape' });
    expect(input).toHaveValue('contact');
    expect(screen.queryByRole('listbox')).not.toBeInTheDocument();
  });

  it('accepts a pointer selection and dismisses on blur or outside interaction', () => {
    render(<Harness />);
    const input = screen.getByRole('combobox');
    fireEvent.focus(input);
    fireEvent.click(screen.getByRole('option', { name: 'businessunit' }));
    expect(input).toHaveValue('businessunit');
    expect(input).toHaveAttribute('aria-expanded', 'false');
    fireEvent.click(screen.getByRole('button', { name: 'Show tables suggestions' }));
    expect(screen.getByRole('listbox')).toBeInTheDocument();
    fireEvent.pointerDown(screen.getByRole('button', { name: 'Outside' }));
    expect(screen.queryByRole('listbox')).not.toBeInTheDocument();
    fireEvent.focus(input); fireEvent.blur(input);
    expect(screen.queryByRole('listbox')).not.toBeInTheDocument();
  });

  it('allows custom values and does not change typed text on Tab or Escape', () => {
    render(<Harness />);
    const input = screen.getByRole('combobox');
    fireEvent.change(input, { target: { value: 'Contoso.Plugin:Update:40' } });
    expect(screen.getByText(/No matching suggestions/)).toBeInTheDocument();
    fireEvent.keyDown(input, { key: 'Enter' });
    expect(input).toHaveValue('Contoso.Plugin:Update:40');
    expect(input).toHaveAttribute('aria-expanded', 'false');
    fireEvent.focus(input); fireEvent.keyDown(input, { key: 'ArrowDown' });
    fireEvent.keyDown(input, { key: 'Tab' });
    expect(input).toHaveValue('Contoso.Plugin:Update:40');
    expect(input).toHaveAttribute('aria-expanded', 'false');
  });

  it('shows useful empty and bounded-large-list states and renders metadata as text', () => {
    const first = render(<Harness options={[]} />);
    fireEvent.focus(screen.getByRole('combobox'));
    expect(screen.getByText(/No suggestions in the loaded model/)).toBeInTheDocument();
    first.unmount();
    render(<Harness options={['<img src=x onerror=alert(1)>', ...Array.from({ length: 100 }, (_, i) => `table${i}`)]} />);
    fireEvent.focus(screen.getByRole('combobox'));
    expect(screen.getAllByRole('option')).toHaveLength(80);
    expect(screen.getByText(/Showing the first 80 matches/)).toBeInTheDocument();
    expect(document.querySelector('img')).toBeNull();
    fireEvent.change(screen.getByRole('combobox'), { target: { value: 'table99' } });
    expect(screen.getByRole('option', { name: 'table99' })).toBeInTheDocument();
  });

  it('completes the final comma-separated value without dropping previous selections', () => {
    render(<Harness initial="account, con" commaSeparated />);
    const input = screen.getByRole('combobox');
    fireEvent.focus(input);
    expect(screen.queryByRole('option', { name: 'account' })).not.toBeInTheDocument();
    fireEvent.click(screen.getByRole('option', { name: 'contact' }));
    expect(input).toHaveValue('account, contact');
    fireEvent.change(input, { target: { value: 'account, contact, business' } });
    fireEvent.keyDown(input, { key: 'ArrowDown' }); fireEvent.keyDown(input, { key: 'Enter' });
    expect(input).toHaveValue('account, contact, businessunit');
  });

  it('closes an open popup and disables both input and trigger when busy', () => {
    const view = render(<Harness />);
    fireEvent.focus(screen.getByRole('combobox'));
    expect(screen.getByRole('listbox')).toBeInTheDocument();
    view.rerender(<Harness disabled />);
    expect(screen.getByRole('combobox')).toBeDisabled();
    expect(screen.getByRole('button', { name: 'Show tables suggestions' })).toBeDisabled();
    expect(screen.queryByRole('listbox')).not.toBeInTheDocument();
  });
});
