import { useId } from 'react';
import { useGLAccountList } from '../hooks/useGLAccountQueries';

interface Props {
  value: string;
  onChange: (code: string) => void;
  postableOnly?: boolean;
  className?: string;
  id?: string;
  placeholder?: string;
}

/**
 * Searchable GL account selector backed by a native datalist: the user can type
 * a code or pick "code — name". Stores the account code. Reused by the GL
 * posting-map config and journal-entry forms so account selection is consistent.
 */
export const AccountPicker = ({
  value,
  onChange,
  postableOnly = true,
  className,
  id,
  placeholder,
}: Props) => {
  const listId = useId();
  const query = useGLAccountList({
    isPostable: postableOnly ? true : undefined,
    isActive: true,
  });
  const accounts = query.data?.data ?? [];

  return (
    <>
      <input
        id={id}
        list={listId}
        value={value}
        onChange={(e) => onChange(e.target.value)}
        placeholder={placeholder}
        className={
          className ??
          'w-full rounded border border-slate-200 bg-white px-2 py-1 font-mono text-xs dark:border-slate-700 dark:bg-slate-900 dark:text-slate-100'
        }
      />
      <datalist id={listId}>
        {accounts.map((a) => (
          <option key={a.id} value={a.code}>
            {a.code} — {a.name}
          </option>
        ))}
      </datalist>
    </>
  );
};
