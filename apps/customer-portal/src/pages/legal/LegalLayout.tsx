import { type ReactNode } from 'react';
import { Link } from 'react-router-dom';
import ReactMarkdown from 'react-markdown';
import remarkGfm from 'remark-gfm';
import { useTranslation } from 'react-i18next';

interface LegalLayoutProps {
  title: string;
  contentTr: string;
  contentEn: string;
  variables?: Record<string, string>;
  footer?: ReactNode;
}

const applyVariables = (markdown: string, variables: Record<string, string>) => {
  return Object.entries(variables).reduce((acc, [key, value]) => {
    const pattern = new RegExp(`{{\\s*${key}\\s*}}`, 'g');
    return acc.replace(pattern, value);
  }, markdown);
};

const DEFAULT_VARIABLES = {
  tenantName: 'CoreAlign',
  tenantLegalName: 'CoreAlign Yazılım A.Ş.',
  mersisNumber: '0000000000000000',
  dpoName: 'CoreAlign DPO',
  dpoEmail: 'dpo@corealign.com',
  policyVersion: 'v2026-06-01',
  effectiveDate: '2026-06-01',
};

export const LegalLayout = ({
  title,
  contentTr,
  contentEn,
  variables,
  footer,
}: LegalLayoutProps) => {
  const { i18n } = useTranslation();
  const merged = { ...DEFAULT_VARIABLES, ...(variables ?? {}) };
  const language = i18n.language?.startsWith('tr') ? 'tr' : 'en';
  const source = language === 'tr' ? contentTr : contentEn;
  const rendered = applyVariables(source, merged);

  return (
    <div className="min-h-screen bg-slate-50 px-4 py-8 dark:bg-slate-950 sm:px-6 lg:px-10">
      <div className="mx-auto max-w-3xl space-y-4">
        <nav aria-label="breadcrumb" className="text-xs text-slate-500 dark:text-slate-400">
          <Link to="/" className="hover:underline">
            CoreAlign
          </Link>
          <span aria-hidden> / </span>
          <span>{title}</span>
        </nav>
        <article className="prose prose-slate max-w-none rounded-2xl bg-white p-6 shadow-sm dark:prose-invert dark:bg-slate-900 sm:p-8">
          <ReactMarkdown remarkPlugins={[remarkGfm]}>{rendered}</ReactMarkdown>
        </article>
        {footer}
      </div>
    </div>
  );
};
