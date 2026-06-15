import { beforeEach, describe, expect, it, vi } from 'vitest';
import { downloadCsv, type CsvColumn } from '@/shared/lib/exportCsv';

interface Row {
  name: string;
  amount: number;
  notes: string | null;
}

const columns: CsvColumn<Row>[] = [
  { header: 'Name', value: (r) => r.name },
  { header: 'Amount', value: (r) => r.amount },
  { header: 'Notes', value: (r) => r.notes },
];

describe('downloadCsv', () => {
  beforeEach(() => {
    vi.restoreAllMocks();
  });

  it('returns 0 and does not call createObjectURL when rows is empty', () => {
    const spy = vi.spyOn(URL, 'createObjectURL').mockReturnValue('blob:test');
    const count = downloadCsv({ filename: 'empty', columns, rows: [] });
    expect(count).toBe(0);
    expect(spy).not.toHaveBeenCalled();
  });

  it('returns row count when rows are present', () => {
    vi.spyOn(URL, 'createObjectURL').mockReturnValue('blob:test');
    vi.spyOn(URL, 'revokeObjectURL').mockImplementation(() => undefined);
    const count = downloadCsv({
      filename: 'customers',
      columns,
      rows: [
        { name: 'Acme', amount: 1500, notes: null },
        { name: 'Yıldız', amount: 50, notes: 'Has, comma' },
      ],
    });
    expect(count).toBe(2);
  });

  it('escapes cell values containing commas, newlines or quotes', () => {
    let captured: BlobPart[] | null = null;
    const originalBlob = global.Blob;
    class MockBlob {
      parts: BlobPart[];
      type: string;
      constructor(parts: BlobPart[], options?: BlobPropertyBag) {
        this.parts = parts;
        this.type = options?.type ?? '';
        captured = parts;
      }
    }
    (global as unknown as { Blob: typeof Blob }).Blob = MockBlob as unknown as typeof Blob;
    vi.spyOn(URL, 'createObjectURL').mockReturnValue('blob:test');
    vi.spyOn(URL, 'revokeObjectURL').mockImplementation(() => undefined);
    try {
      downloadCsv({
        filename: 'edge',
        columns,
        rows: [{ name: 'Quote "X"', amount: 1, notes: 'a,b\nc' }],
      });
    } finally {
      (global as unknown as { Blob: typeof Blob }).Blob = originalBlob;
    }
    expect(captured).not.toBeNull();
    const csv = (captured as unknown as string[]).slice(1).join('');
    expect(csv).toContain('"Quote ""X"""');
    expect(csv).toContain('"a,b\nc"');
  });

  it('sanitizes filename to safe characters', () => {
    let createdHref: string | null = null;
    const original = document.createElement.bind(document);
    vi.spyOn(document, 'createElement').mockImplementation((tag: string) => {
      const el = original(tag);
      if (tag === 'a') {
        Object.defineProperty(el, 'download', {
          set(v: string) {
            createdHref = v;
          },
          configurable: true,
        });
      }
      return el;
    });
    vi.spyOn(URL, 'createObjectURL').mockReturnValue('blob:test');
    vi.spyOn(URL, 'revokeObjectURL').mockImplementation(() => undefined);

    downloadCsv({
      filename: 'müşteri/raporu',
      columns,
      rows: [{ name: 'x', amount: 1, notes: null }],
    });
    expect(createdHref).not.toBeNull();
    expect(createdHref).toMatch(/^m[_]+teri_raporu_\d{4}-\d{2}-\d{2}\.csv$/);
  });

  it('falls back to "export" filename when all chars are illegal', () => {
    let createdHref: string | null = null;
    const original = document.createElement.bind(document);
    vi.spyOn(document, 'createElement').mockImplementation((tag: string) => {
      const el = original(tag);
      if (tag === 'a') {
        Object.defineProperty(el, 'download', {
          set(v: string) {
            createdHref = v;
          },
          configurable: true,
        });
      }
      return el;
    });
    vi.spyOn(URL, 'createObjectURL').mockReturnValue('blob:test');
    vi.spyOn(URL, 'revokeObjectURL').mockImplementation(() => undefined);

    downloadCsv({
      filename: '!!!!',
      columns,
      rows: [{ name: 'x', amount: 1, notes: null }],
    });
    expect(createdHref).toMatch(/^_+_\d{4}-\d{2}-\d{2}\.csv$/);
  });
});
