import { useState } from 'react';
import { fireEvent, render, screen, within } from '@testing-library/react';
import { describe, expect, it } from 'vitest';
import { ColumnPicker } from './ColumnPicker';
import type { TableColumnCatalog } from '../../shared/api/types';

const catalog: TableColumnCatalog = { table: 'account', displayName: 'Account', columns: [
  { logicalName: 'name', displayName: 'Account name' }, { logicalName: 'ownerid', displayName: 'Owner' },
  { logicalName: 'ctx_flag', displayName: null },
] };
function Harness({ model = catalog, initial = [], disabled = false }: { model?: TableColumnCatalog; initial?: string[]; disabled?: boolean }) {
  const [selected, setSelected] = useState(initial);
  return <ColumnPicker table="account" catalog={model} selected={selected} disabled={disabled} onChange={setSelected} />;
}
const open = () => fireEvent.click(screen.getByRole('button', { name: 'Changed columns (optional)' }));

describe('Changed column picker', () => {
  it('searches both names and preserves selections hidden by search as removable chips', () => {
    render(<Harness />); open();
    expect(screen.getByLabelText('Search columns by display or logical name')).toHaveFocus();
    fireEvent.click(screen.getByRole('checkbox', { name: 'Account name (name)' }));
    fireEvent.change(screen.getByRole('searchbox'), { target: { value: 'OWNERID' } });
    expect(screen.getByRole('checkbox', { name: 'Owner (ownerid)' })).toBeInTheDocument();
    expect(screen.queryByRole('checkbox', { name: 'Account name (name)' })).not.toBeInTheDocument();
    fireEvent.click(screen.getByRole('checkbox', { name: 'Owner (ownerid)' }));
    expect(screen.getByRole('status')).toHaveTextContent('2 selected');
    expect(within(screen.getByRole('list', { name: 'Selected changed columns' })).getByText('Account name')).toBeInTheDocument();
    fireEvent.click(screen.getByRole('button', { name: 'Remove column name' }));
    expect(screen.getByRole('status')).toHaveTextContent('1 selected');
    fireEvent.change(screen.getByRole('searchbox'), { target: { value: 'Account name' } });
    expect(screen.getByRole('checkbox', { name: 'Account name (name)' })).not.toBeChecked();
  });

  it('treats clicks on the entire label as selection and does not dismiss on a transient blur', () => {
    render(<Harness />); open();
    const checkbox = screen.getByRole('checkbox', { name: 'Account name (name)' });
    const label = checkbox.closest('label')!;
    fireEvent.pointerDown(within(label).getByText('Account name'));
    fireEvent.blur(screen.getByRole('searchbox'), { relatedTarget: null });
    fireEvent.click(within(label).getByText('Account name'));
    expect(checkbox).toBeChecked();
    expect(screen.getByRole('searchbox')).toBeInTheDocument();
    fireEvent.click(within(label).getByText('name'));
    expect(checkbox).not.toBeChecked();
    fireEvent.click(label);
    expect(checkbox).toBeChecked();
    fireEvent.keyDown(checkbox, { key: 'Escape' });
    expect(screen.queryByRole('searchbox')).not.toBeInTheDocument();
  });

  it('allows manual values, prevents duplicates, and clears back to unknown', () => {
    render(<Harness />); open();
    fireEvent.change(screen.getByRole('searchbox'), { target: { value: 'CTX_CUSTOM' } });
    fireEvent.click(screen.getByRole('button', { name: 'Add “ctx_custom” manually' }));
    expect(screen.getByRole('button', { name: 'Remove column ctx_custom' })).toBeInTheDocument();
    fireEvent.change(screen.getByRole('searchbox'), { target: { value: 'ctx_custom' } });
    expect(screen.queryByRole('button', { name: /Add/ })).not.toBeInTheDocument();
    fireEvent.click(screen.getByRole('button', { name: 'Clear' }));
    expect(screen.getByRole('status')).toHaveTextContent('None selected · changed columns are unknown');
    expect(screen.queryByRole('list', { name: 'Selected changed columns' })).not.toBeInTheDocument();
  });

  it('dismisses on Escape and restores focus, and disables editing during a query', () => {
    const view = render(<Harness initial={['name']} />); open();
    fireEvent.keyDown(screen.getByRole('searchbox'), { key: 'Escape' });
    expect(screen.queryByRole('searchbox')).not.toBeInTheDocument();
    expect(screen.getByRole('button', { name: 'Changed columns (optional)' })).toHaveFocus();
    open(); view.rerender(<Harness initial={['name']} disabled />);
    expect(screen.queryByRole('searchbox')).not.toBeInTheDocument();
    expect(screen.getByRole('button', { name: 'Changed columns (optional)' })).toBeDisabled();
    expect(screen.getByRole('button', { name: 'Remove column name' })).toBeDisabled();
  });

  it('bounds the list and selected count without discarding catalog values', () => {
    const columns = Array.from({ length: 140 }, (_, i) => ({ logicalName: `ctx_${i}`, displayName: null }));
    render(<Harness model={{ ...catalog, columns }} initial={columns.slice(0, 100).map(c => c.logicalName)} />); open();
    expect(screen.getByText(/Showing 100 of 140/)).toBeInTheDocument();
    fireEvent.change(screen.getByRole('searchbox'), { target: { value: 'ctx_139' } });
    expect(screen.getByRole('checkbox', { name: 'ctx_139' })).toBeDisabled();
    fireEvent.click(screen.getByRole('button', { name: 'Remove column ctx_0' }));
    expect(screen.getByRole('checkbox', { name: 'ctx_139' })).toBeEnabled();
  });

  it('renders untrusted metadata as text and distinguishes an empty catalog from no catalog', () => {
    const view = render(<Harness model={{ ...catalog, columns: [{ logicalName: 'ctx_test', displayName: '<img src=x onerror=alert(1)>' }] }} />); open();
    expect(screen.getByText('<img src=x onerror=alert(1)>')).toBeInTheDocument();
    expect(document.querySelector('img')).toBeNull();
    view.unmount(); render(<Harness model={{ ...catalog, columns: [] }} />); open();
    expect(screen.getByText('No columns recorded for this table.')).toBeInTheDocument();
  });
});
