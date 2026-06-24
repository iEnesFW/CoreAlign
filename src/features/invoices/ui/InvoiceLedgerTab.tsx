import { useMemo } from 'react';
import { useCreditNotesForInvoice } from '@/features/invoices/hooks/useInvoiceQueries';
import type { Invoice } from '@/features/invoices/model/invoice.types';
import { buildGlEntries, computeDunningLevel } from './invoiceLedger/ledgerModel';
import { DunningCard } from './invoiceLedger/DunningCard';
import { GlPostingCard } from './invoiceLedger/GlPostingCard';
import { LinkedDocumentsCard } from './invoiceLedger/LinkedDocuments';
import { RemindersCard } from './invoiceLedger/Reminders';

interface Props {
  invoice: Invoice;
  locale: string;
}

export const InvoiceLedgerTab = ({ invoice, locale }: Props) => {
  const glEntries = useMemo(() => buildGlEntries(invoice), [invoice]);
  const creditNotesQuery = useCreditNotesForInvoice(invoice.id);
  const creditNotes = creditNotesQuery.data?.data ?? [];
  const totalDebit = glEntries.reduce((s, e) => s + e.debit, 0);
  const totalCredit = glEntries.reduce((s, e) => s + e.credit, 0);
  const dunningLevel = computeDunningLevel(invoice);

  return (
    <div className="space-y-3">
      <DunningCard invoice={invoice} dunningLevel={dunningLevel} locale={locale} />
      <GlPostingCard
        invoice={invoice}
        entries={glEntries}
        totalDebit={totalDebit}
        totalCredit={totalCredit}
        locale={locale}
      />
      <LinkedDocumentsCard invoice={invoice} creditNotes={creditNotes} locale={locale} />
      <RemindersCard invoice={invoice} dunningLevel={dunningLevel} locale={locale} />
    </div>
  );
};
