import styles from './PermissionTable.module.css';
import ui from '../../shared/ui/ui.module.css';
import { cx } from '../../shared/ui/classNames';
import { basisLabel, coverageLabel, depthLabel, sectionTitle, uiMessage } from '../../shared/presentation/labels';
import type { Source } from '../../shared/api/types';
import { columns, status, value } from './resultRows';
import type { Column, Sort, TableGroup } from './resultRows';

function Sources({ sources }: { sources: Source[] }) {
  if (!sources.length) return null;
  return <details className={styles.sources}><summary>{sources.length} source{sources.length === 1 ? '' : 's'}</summary>
    {sources.map((source, index) => <div key={index} className={styles.source}>{source.file}:{source.line}:{source.column}
      {source.path.length > 0 && <div>{source.path.join(' → ')}</div>}
    </div>)}
  </details>;
}

export function PermissionTable({ id, groups, hasRequirements, sort, onSort }: {
  id: string; groups: TableGroup[]; hasRequirements: boolean; sort?: Sort; onSort: (column: Column) => void;
}) {
  return <div className={ui.tableWrap} role="region" aria-label={`${sectionTitle(id)} requirements`} tabIndex={0}>
    <table className={cx(ui.table, styles.table)}>
      <colgroup><col className={styles.tableColumn} />{columns.slice(1).map(([column]) => <col key={column} />)}</colgroup>
      <thead><tr>{columns.map(([column, label]) => <th key={column} scope="col" aria-sort={sort?.column === column ? sort.direction : 'none'}>
        <button className={styles.tableSort} type="button" onClick={() => onSort(column)} aria-label={`Sort by ${label.toLowerCase()}`}>
          {label}<span aria-hidden="true">{sort?.column === column ? sort.direction === 'ascending' ? '↑' : '↓' : '↕'}</span>
        </button>
      </th>)}</tr></thead>
      <tbody>{groups.map(group => <tr key={group.table}>
        <th scope="row" className={styles.tableName}>{group.table}<small>{group.permissions.length} {group.permissions.length === 1 ? 'permission' : 'permissions'}</small></th>
        <td colSpan={4} className={styles.permissionCell}>
          <ul className={styles.permissions} aria-label={`${group.table} permissions`}>
            {group.permissions.map(({ key, requirement, count }) => <li className={styles.permission} key={key}>
              <div className={styles.privilege}><span className={ui.srOnly}>Privilege: </span>{requirement.privilege}
                {count > 1 && <small>{count} equivalent requirements</small>}</div>
              <div><span className={ui.srOnly}>Runs as: </span>{value(requirement, 'identity')}</div>
              <div><span className={ui.srOnly}>Basis: </span>{basisLabel(requirement.basis)}</div>
              <div><span className={ui.srOnly}>Coverage: </span><span className={cx(styles.badge, styles[status(requirement)])}>{coverageLabel(status(requirement))}</span>
                {requirement.coverage?.depth && <div className={ui.hint}>{depthLabel(requirement.coverage.depth)}</div>}
                {requirement.coverage?.reason && <div className={ui.hint}>{uiMessage(requirement.coverage.reason)}</div>}
              </div>
              {requirement.sources.length > 0 && <div className={styles.permissionSources}><Sources sources={requirement.sources} /></div>}
            </li>)}
          </ul>
        </td>
      </tr>)}
      {!groups.length && <tr><td colSpan={columns.length} className={styles.noResultRows}>{hasRequirements ? 'No permissions match these filters. Section diagnostics are shown below.' : 'No requirements in this view.'}</td></tr>}
      </tbody>
    </table>
  </div>;
}
