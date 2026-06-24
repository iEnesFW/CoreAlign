import { useMemo, useState } from 'react';
import { useTranslation } from 'react-i18next';
import type { TFunction } from 'i18next';
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
import { PageHeader } from '@/shared/ui/PageHeader/PageHeader';
import { ListPageTemplate } from '@/shared/ui/PageTemplate/PageTemplate';
import { Button } from '@/shared/ui/Button/Button';
import { Input } from '@/shared/ui/Input/Input';
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
  Asset: 'bg-success-100 text-success-700 dark:bg-success-500/20 dark:text-success-300',
  Liability: 'bg-danger-100 text-danger-700 dark:bg-danger-500/20 dark:text-danger-300',
  Equity: 'bg-violet-100 text-violet-700 dark:bg-violet-500/20 dark:text-violet-300',
  Revenue: 'bg-info-100 text-info-700 dark:bg-info-500/20 dark:text-info-300',
  Expense: 'bg-warning-100 text-warning-800 dark:bg-warning-500/20 dark:text-warning-300',
  CostOfGoodsSold: 'bg-warning-100 text-warning-700 dark:bg-warning-500/20 dark:text-warning-300',
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
  t,
}: {
  node: TreeNode;
  depth: number;
  expanded: Set<string>;
  onToggle: (id: string) => void;
  onAddChild: (parent: GLAccount) => void;
  onEdit: (account: GLAccount) => void;
  onToggleActive: (account: GLAccount) => void;
  onDelete: (account: GLAccount) => void;
  t: TFunction;
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
          aria-label={
            isOpen
              ? t('accounting.coa.collapse', { defaultValue: 'Collapse' })
              : t('accounting.coa.expand', { defaultValue: 'Expand' })
          }
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
          {node.account.normalSide === 'Debit'
            ? t('accounting.coa.debitShort', { defaultValue: 'Dr' })
            : t('accounting.coa.creditShort', { defaultValue: 'Cr' })}
        </span>
        <span className="w-10 text-center text-[10px] text-slate-500">{node.account.currency}</span>
        {!node.account.isActive && (
          <span className="rounded bg-slate-200 px-1.5 py-0.5 text-[10px] font-semibold text-slate-600 dark:bg-slate-700 dark:text-slate-300">
            {t('accounting.coa.inactiveBadge', { defaultValue: 'Pasif' })}
          </span>
        )}
        {node.account.isPostable && (
          <span className="rounded bg-primary-100 px-1.5 py-0.5 text-[10px] font-semibold text-primary-700 dark:bg-primary-500/20 dark:text-primary-300">
            {t('accounting.coa.postableBadge', { defaultValue: 'Postable' })}
          </span>
        )}
        <div className="ml-2 flex items-center gap-0.5 opacity-0 group-hover:opacity-100 [div:hover>&]:opacity-100">
          <button
            type="button"
            onClick={() => onAddChild(node.account)}
            className="rounded p-1 text-slate-400 hover:bg-slate-100 hover:text-slate-700 dark:hover:bg-slate-800"
            title={t('accounting.coa.addChild', { defaultValue: 'Alt hesap ekle' })}
          >
            <Plus size={12} />
          </button>
          <button
            type="button"
            onClick={() => onEdit(node.account)}
            className="rounded p-1 text-slate-400 hover:bg-slate-100 hover:text-slate-700 dark:hover:bg-slate-800"
            title={t('accounting.coa.edit', { defaultValue: 'Düzenle' })}
          >
            <BookOpen size={12} />
          </button>
          <button
            type="button"
            onClick={() => onToggleActive(node.account)}
            className="rounded p-1 text-slate-400 hover:bg-slate-100 hover:text-slate-700 dark:hover:bg-slate-800"
            title={
              node.account.isActive
                ? t('accounting.coa.deactivate', { defaultValue: 'Pasifle' })
                : t('accounting.coa.activate', { defaultValue: 'Aktifle' })
            }
          >
            {node.account.isActive ? <PowerOff size={12} /> : <Power size={12} />}
          </button>
          <button
            type="button"
            onClick={() => onDelete(node.account)}
            className="rounded p-1 text-slate-400 hover:bg-danger-50 hover:text-danger-700 dark:hover:bg-danger-500/10"
            title={t('accounting.coa.delete', { defaultValue: 'Sil' })}
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
            t={t}
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

  const accounts = useMemo(() => tree.data?.data ?? [], [tree.data]);
  const roots = useMemo(() => buildTree(accounts), [accounts]);
  const filtered = useMemo(
    () => (search.trim() ? filterTree(roots, search.trim()) : roots),
    [roots, search],
  );

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
      title: t('accounting.coa.seedTitle', { defaultValue: 'TDHP Yükle' }),
      message: t('accounting.coa.seedConfirm', {
        defaultValue: 'Tek Düzen Hesap Planı (TDHP) eklenecek. Devam edilsin mi?',
      }),
      confirmLabel: t('accounting.coa.seedConfirmLabel', { defaultValue: 'Yükle' }),
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
      toast.success(
        account.isActive
          ? t('accounting.coa.deactivated', { defaultValue: 'Hesap pasifleştirildi.' })
          : t('accounting.coa.activated', { defaultValue: 'Hesap aktifleştirildi.' }),
      );
    } catch (err) {
      toastApiError(err);
    }
  };

  const remove = async (account: GLAccount) => {
    const ok = await confirm({
      title: t('accounting.coa.deleteTitle', { defaultValue: 'Hesabı Sil' }),
      message: t('accounting.coa.deleteConfirm', {
        defaultValue: '{{code}} - {{name}} silinsin mi?',
        code: account.code,
        name: account.name,
      }),
      confirmLabel: t('accounting.coa.deleteConfirmLabel', { defaultValue: 'Sil' }),
      tone: 'danger',
    });
    if (!ok) return;
    try {
      await deleteMutation.mutateAsync(account.id);
      toast.success(t('accounting.coa.deleted', { defaultValue: 'Hesap silindi.' }));
    } catch (err) {
      toastApiError(err);
    }
  };

  return (
    <ListPageTemplate
      header={
        <PageHeader
          icon={<BookOpen size={20} />}
          title={t('accounting.coa.title', { defaultValue: 'Hesap Planı' })}
          subtitle={t('accounting.coa.subtitle', {
            defaultValue:
              'Tek Düzen Hesap Planı (TDHP) hiyerarşik görünüm. Yevmiye fişleri sadece "Postable" alt hesaplara post edilebilir.',
          })}
          actions={
            <Button size="sm" onClick={() => setModalState({ mode: 'create' })}>
              <Plus size={14} />
              {t('accounting.coa.create', { defaultValue: 'Yeni Hesap' })}
            </Button>
          }
        />
      }
      toolbar={
        <div className="flex flex-wrap items-center gap-2">
          <Input
            type="search"
            value={search}
            onChange={(e) => setSearch(e.target.value)}
            placeholder={t('accounting.coa.searchPlaceholder', {
              defaultValue: 'Kod veya isim ile ara…',
            })}
            className="w-full sm:w-72"
          />
          {accounts.length === 0 && (
            <Button
              variant="outline"
              size="sm"
              onClick={seedTurkish}
              isLoading={seedMutation.isPending}
            >
              <Database size={14} />
              {t('accounting.coa.seedTurkish', { defaultValue: 'TDHP Yükle' })}
            </Button>
          )}
        </div>
      }
    >
      <div className="overflow-hidden rounded-lg border border-slate-200 bg-white dark:border-slate-800 dark:bg-slate-900">
        {tree.isPending ? (
          <div className="p-8 text-center text-sm text-slate-500">
            {t('accounting.coa.loading', { defaultValue: 'Yükleniyor…' })}
          </div>
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
                t={t}
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
    </ListPageTemplate>
  );
};

export default ChartOfAccountsPage;
