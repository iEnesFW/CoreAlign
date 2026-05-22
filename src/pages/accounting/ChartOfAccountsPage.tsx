import { useMemo, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { toast } from 'sonner';
import {
  BookOpen,
  ChevronDown,
  ChevronRight,
  Database,
  Plus,
  Power,
  PowerOff,
  Trash2,
} from 'lucide-react';
import { toastApiError } from '@/shared/lib/mutationToast';
import { useConfirm } from '@/shared/ui/ConfirmDialog/useConfirm';
import {
  useDeleteGLAccount,
  useGLAccountTree,
  useSeedTurkishChart,
  useSetGLAccountActive,
} from '@/features/accounting/hooks/useGLAccountQueries';
import type { AccountType, GLAccount } from '@/features/accounting/model/glAccount.types';
import { GLAccountFormModal } from '@/features/accounting/ui/GLAccountFormModal';

interface TreeNode {
  account: GLAccount;
  children: TreeNode[];
}

const TYPE_STYLES: Record<AccountType, string> = {
  Asset: 'bg-emerald-100 text-emerald-700 dark:bg-emerald-500/20 dark:text-emerald-300',
  Liability: 'bg-rose-100 text-rose-700 dark:bg-rose-500/20 dark:text-rose-300',
  Equity: 'bg-violet-100 text-violet-700 dark:bg-violet-500/20 dark:text-violet-300',
  Revenue: 'bg-sky-100 text-sky-700 dark:bg-sky-500/20 dark:text-sky-300',
  Expense: 'bg-amber-100 text-amber-800 dark:bg-amber-500/20 dark:text-amber-300',
  CostOfGoodsSold: 'bg-orange-100 text-orange-700 dark:bg-orange-500/20 dark:text-orange-300',
  Memorandum: 'bg-slate-200 text-slate-700 dark:bg-slate-700/40 dark:text-slate-300',
};

const buildTree = (accounts: GLAccount[]): TreeNode[] => {
  const byId = new Map<string, TreeNode>();
  for (const a of accounts) byId.set(a.id, { account: a, children: [] });

  const roots: TreeNode[] = [];
  for (const node of byId.values()) {
    if (node.account.parentId && byId.has(node.account.parentId)) {
      byId.get(node.account.parentId)!.children.push(node);
    } else {
      roots.push(node);
    }
  }
  // Children come back code-ordered from the server, but ensure roots are too.
  roots.sort((a, b) => a.account.code.localeCompare(b.account.code));
  return roots;
};

const filterTree = (nodes: TreeNode[], term: string): TreeNode[] => {
  const lower = term.toLowerCase();
  const visit = (node: TreeNode): TreeNode | null => {
    const children = node.children.map(visit).filter((c): c is TreeNode => c !== null);
    const selfMatches =
      node.account.code.toLowerCase().includes(lower) ||
      node.account.name.toLowerCase().includes(lower);
    if (selfMatches || children.length > 0) {
      return { account: node.account, children };
    }
    return null;
  };
  return nodes.map(visit).filter((c): c is TreeNode => c !== null);
};

const TreeRow = ({
  node,
  depth,
  expanded,
  onToggle,
  onAddChild,
  onEdit,
  onToggleActive,
  onDelete,
}: {
  node: TreeNode;
  depth: number;
  expanded: Set<string>;
  onToggle: (id: string) => void;
  onAddChild: (parent: GLAccount) => void;
  onEdit: (account: GLAccount) => void;
  onToggleActive: (account: GLAccount) => void;
  onDelete: (account: GLAccount) => void;
}) => {
  const isOpen = expanded.has(node.account.id);
  const hasChildren = node.children.length > 0;
  return (
    <>
      <div
        className="flex items-center gap-2 border-b border-slate-100 px-3 py-1.5 hover:bg-slate-50 dark:border-slate-800 dark:hover:bg-slate-800/30"
        style={{ paddingLeft: `${12 + depth * 18}px` }}
      >
        <button
          type="button"
          onClick={() => hasChildren && onToggle(node.account.id)}
          className="flex h-5 w-5 items-center justify-center text-slate-400 disabled:opacity-30"
          disabled={!hasChildren}
          aria-label={isOpen ? 'Collapse' : 'Expand'}
        >
          {hasChildren ? (
            isOpen ? (
              <ChevronDown size={14} />
            ) : (
              <ChevronRight size={14} />
            )
          ) : (
            <span className="text-[10px]">•</span>
          )}
        </button>
        <span className="w-24 font-mono text-xs font-semibold text-slate-700 dark:text-slate-200">
          {node.account.code}
        </span>
        <span className="flex-1 text-sm text-slate-900 dark:text-slate-100">
          {node.account.name}
        </span>
        <span
          className={`rounded px-1.5 py-0.5 text-[10px] font-semibold ${TYPE_STYLES[node.account.type]}`}
        >
          {node.account.type}
        </span>
        <span className="w-12 text-center text-[10px] uppercase text-slate-500">
          {node.account.normalSide === 'Debit' ? 'Dr' : 'Cr'}
        </span>
        <span className="w-10 text-center text-[10px] text-slate-500">{node.account.currency}</span>
        {!node.account.isActive && (
          <span className="rounded bg-slate-200 px-1.5 py-0.5 text-[10px] font-semibold text-slate-600 dark:bg-slate-700 dark:text-slate-300">
            Pasif
          </span>
        )}
        {node.account.isPostable && (
          <span className="rounded bg-indigo-100 px-1.5 py-0.5 text-[10px] font-semibold text-indigo-700 dark:bg-indigo-500/20 dark:text-indigo-300">
            Postable
          </span>
        )}
        <div className="ml-2 flex items-center gap-0.5 opacity-0 group-hover:opacity-100 [div:hover>&]:opacity-100">
          <button
            type="button"
            onClick={() => onAddChild(node.account)}
            className="rounded p-1 text-slate-400 hover:bg-slate-100 hover:text-slate-700 dark:hover:bg-slate-800"
            title="Alt hesap ekle"
          >
            <Plus size={12} />
          </button>
          <button
            type="button"
            onClick={() => onEdit(node.account)}
            className="rounded p-1 text-slate-400 hover:bg-slate-100 hover:text-slate-700 dark:hover:bg-slate-800"
            title="Düzenle"
          >
            <BookOpen size={12} />
          </button>
          <button
            type="button"
            onClick={() => onToggleActive(node.account)}
            className="rounded p-1 text-slate-400 hover:bg-slate-100 hover:text-slate-700 dark:hover:bg-slate-800"
            title={node.account.isActive ? 'Pasifle' : 'Aktifle'}
          >
            {node.account.isActive ? <PowerOff size={12} /> : <Power size={12} />}
          </button>
          <button
            type="button"
            onClick={() => onDelete(node.account)}
            className="rounded p-1 text-slate-400 hover:bg-rose-50 hover:text-rose-700 dark:hover:bg-rose-500/10"
            title="Sil"
          >
            <Trash2 size={12} />
          </button>
        </div>
      </div>
      {isOpen &&
        node.children.map((child) => (
          <TreeRow
            key={child.account.id}
            node={child}
            depth={depth + 1}
            expanded={expanded}
            onToggle={onToggle}
            onAddChild={onAddChild}
            onEdit={onEdit}
            onToggleActive={onToggleActive}
            onDelete={onDelete}
          />
        ))}
    </>
  );
};

export const ChartOfAccountsPage = () => {
  const { t } = useTranslation();
  const confirm = useConfirm();
  const tree = useGLAccountTree();
  const seedMutation = useSeedTurkishChart();
  const setActiveMutation = useSetGLAccountActive();
  const deleteMutation = useDeleteGLAccount();

  const [search, setSearch] = useState('');
  const [expanded, setExpanded] = useState<Set<string>>(new Set());
  const [modalState, setModalState] = useState<
    | { mode: 'closed' }
    | { mode: 'create'; parent?: GLAccount }
    | { mode: 'edit'; account: GLAccount }
  >({ mode: 'closed' });

  // `tree.data?.data ?? []` would produce a new array identity on every render
  // and force buildTree to re-run; memoize the fallback so the empty array is
  // stable when the query is still loading.
  const accounts = useMemo(() => tree.data?.data ?? [], [tree.data]);
  const roots = useMemo(() => buildTree(accounts), [accounts]);
  const filtered = useMemo(
    () => (search.trim() ? filterTree(roots, search.trim()) : roots),
    [roots, search],
  );

  // When the user types in search, auto-expand all matched paths so results
  // are visible without manual clicking.
  const expandedForView = useMemo(() => {
    if (!search.trim()) return expanded;
    const all = new Set<string>();
    const walk = (n: TreeNode) => {
      all.add(n.account.id);
      n.children.forEach(walk);
    };
    filtered.forEach(walk);
    return all;
  }, [filtered, expanded, search]);

  const toggle = (id: string) =>
    setExpanded((prev) => {
      const next = new Set(prev);
      if (next.has(id)) next.delete(id);
      else next.add(id);
      return next;
    });

  const seedTurkish = async () => {
    const ok = await confirm({
      title: 'TDHP Yükle',
      message: t('accounting.coa.seedConfirm', {
        defaultValue: 'Tek Düzen Hesap Planı (TDHP) eklenecek. Devam edilsin mi?',
      }),
      confirmLabel: 'Yükle',
    });
    if (!ok) return;
    try {
      const result = await seedMutation.mutateAsync();
      toast.success(
        t('accounting.coa.seedDone', {
          defaultValue: '{{count}} hesap eklendi.',
          count: result.data ?? 0,
        }),
      );
    } catch (err) {
      toastApiError(err);
    }
  };

  const toggleActive = async (account: GLAccount) => {
    try {
      await setActiveMutation.mutateAsync({ id: account.id, isActive: !account.isActive });
      toast.success(account.isActive ? 'Hesap pasifleştirildi.' : 'Hesap aktifleştirildi.');
    } catch (err) {
      toastApiError(err);
    }
  };

  const remove = async (account: GLAccount) => {
    const ok = await confirm({
      title: 'Hesabı Sil',
      message: t('accounting.coa.deleteConfirm', {
        defaultValue: '{{code}} - {{name}} silinsin mi?',
        code: account.code,
        name: account.name,
      }),
      confirmLabel: 'Sil',
      tone: 'danger',
    });
    if (!ok) return;
    try {
      await deleteMutation.mutateAsync(account.id);
      toast.success('Hesap silindi.');
    } catch (err) {
      toastApiError(err);
    }
  };

  return (
    <div className="space-y-4 p-4">
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-xl font-bold text-slate-900 dark:text-slate-100">
            {t('accounting.coa.title', { defaultValue: 'Hesap Planı' })}
          </h1>
          <p className="mt-0.5 text-sm text-slate-500 dark:text-slate-400">
            {t('accounting.coa.subtitle', {
              defaultValue:
                'Tek Düzen Hesap Planı (TDHP) hiyerarşik görünüm. Yevmiye fişleri sadece "Postable" alt hesaplara post edilebilir.',
            })}
          </p>
        </div>
        <div className="flex items-center gap-2">
          <input
            type="search"
            value={search}
            onChange={(e) => setSearch(e.target.value)}
            placeholder={t('accounting.coa.searchPlaceholder', {
              defaultValue: 'Kod veya isim ile ara…',
            })}
            className="w-64 rounded border border-slate-200 bg-white px-3 py-1.5 text-sm dark:border-slate-700 dark:bg-slate-900"
          />
          {accounts.length === 0 && (
            <button
              type="button"
              onClick={seedTurkish}
              disabled={seedMutation.isPending}
              className="inline-flex items-center gap-1.5 rounded border border-indigo-200 bg-indigo-50 px-2.5 py-1.5 text-xs font-semibold text-indigo-700 hover:bg-indigo-100 disabled:opacity-50 dark:border-indigo-500/30 dark:bg-indigo-500/10 dark:text-indigo-300"
            >
              <Database size={12} />
              {t('accounting.coa.seedTurkish', { defaultValue: 'TDHP Yükle' })}
            </button>
          )}
          <button
            type="button"
            onClick={() => setModalState({ mode: 'create' })}
            className="inline-flex items-center gap-1.5 rounded bg-indigo-600 px-3 py-1.5 text-xs font-semibold text-white hover:bg-indigo-700"
          >
            <Plus size={12} />
            {t('accounting.coa.create', { defaultValue: 'Yeni Hesap' })}
          </button>
        </div>
      </div>

      <div className="overflow-hidden rounded-lg border border-slate-200 bg-white dark:border-slate-800 dark:bg-slate-900">
        {tree.isPending ? (
          <div className="p-8 text-center text-sm text-slate-500">Yükleniyor…</div>
        ) : filtered.length === 0 ? (
          <div className="p-8 text-center text-sm text-slate-500">
            {accounts.length === 0
              ? t('accounting.coa.empty', {
                  defaultValue: 'Henüz hesap eklenmedi. TDHP yükle ile başlayabilirsiniz.',
                })
              : t('accounting.coa.noResults', { defaultValue: 'Eşleşen hesap bulunamadı.' })}
          </div>
        ) : (
          <div className="max-h-[70vh] overflow-y-auto">
            {filtered.map((root) => (
              <TreeRow
                key={root.account.id}
                node={root}
                depth={0}
                expanded={expandedForView}
                onToggle={toggle}
                onAddChild={(parent) => setModalState({ mode: 'create', parent })}
                onEdit={(account) => setModalState({ mode: 'edit', account })}
                onToggleActive={toggleActive}
                onDelete={remove}
              />
            ))}
          </div>
        )}
      </div>

      {modalState.mode !== 'closed' && (
        <GLAccountFormModal
          mode={modalState.mode}
          account={modalState.mode === 'edit' ? modalState.account : undefined}
          parent={modalState.mode === 'create' ? modalState.parent : undefined}
          onClose={() => setModalState({ mode: 'closed' })}
        />
      )}
    </div>
  );
};

export default ChartOfAccountsPage;
