import { useTranslation } from 'react-i18next';
import { Link } from 'react-router-dom';
import { ExternalLink } from 'lucide-react';
import { useDocumentChain } from '../hooks/useDocumentChain';
import type {
  ChainEntity,
  ChainNode,
  ChainNodeKind,
  ChainNodeState,
} from '../model/chainNode.types';

interface Props {
  entity: ChainEntity;
  id: string;
}

const dotClass = (state: ChainNodeState) =>
  state === 'done'
    ? 'bg-success-500'
    : state === 'partial'
      ? 'bg-warning-500'
      : 'bg-slate-300 dark:bg-slate-600';

const ringClass = (isCurrent: boolean) =>
  isCurrent ? 'ring-2 ring-primary-400 ring-offset-1 dark:ring-offset-slate-900' : '';

export const DocumentChain = ({ entity, id }: Props) => {
  const { t } = useTranslation();
  const { nodes, isLoading } = useDocumentChain({ entity, id });

  const kindLabel = (kind: ChainNodeKind) =>
    t(`DocumentChain.nodes.${kind}` as const, { defaultValue: kind });

  if (isLoading && nodes.length === 0) {
    return (
      <div className="text-sm text-slate-500">
        {t('common.loading', { defaultValue: 'Yükleniyor…' })}
      </div>
    );
  }

  if (nodes.length === 0) {
    return (
      <div className="rounded border border-slate-200 p-4 text-center text-sm text-slate-500 dark:border-slate-800">
        {t('DocumentChain.empty', { defaultValue: 'Bağlı belge yok' })}
      </div>
    );
  }

  return (
    <ol
      className="space-y-0"
      aria-label={t('DocumentChain.title', { defaultValue: 'Belge Zinciri' })}
    >
      {nodes.map((node, idx) => (
        <li key={`${node.kind}-${node.id || idx}`} className="flex gap-3">
          <div className="flex flex-col items-center">
            <span
              className={`mt-1 h-3 w-3 shrink-0 rounded-full ${dotClass(node.state)} ${ringClass(node.isCurrent)}`}
            />
            {idx < nodes.length - 1 && (
              <span className="my-0.5 w-px flex-1 bg-slate-200 dark:bg-slate-700" />
            )}
          </div>
          <div className="pb-4">
            <div className="text-xs font-semibold uppercase tracking-wider text-slate-500 dark:text-slate-400">
              {kindLabel(node.kind)}
            </div>
            <ChainLabel node={node} />
            <div className="text-[11px] text-slate-500 dark:text-slate-400">{node.statusLabel}</div>
          </div>
        </li>
      ))}
    </ol>
  );
};

const ChainLabel = ({ node }: { node: ChainNode }) => {
  if (!node.to || !node.id) {
    return <div className="text-sm text-slate-800 dark:text-slate-100">{node.label}</div>;
  }
  return (
    <Link
      to={node.to}
      className="inline-flex items-center gap-1 text-sm font-medium text-primary-600 hover:underline dark:text-primary-400"
    >
      {node.label}
      <ExternalLink size={11} />
    </Link>
  );
};
