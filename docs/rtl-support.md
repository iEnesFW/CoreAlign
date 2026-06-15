# RTL (Right-to-Left) Support

CoreAlign's admin SPA supports right-to-left rendering for Arabic and any other
RTL locale that ships with sufficient translation parity. This document
explains how the system flips direction at runtime, which surfaces have been
visually validated, and how to add coverage when introducing new layouts.

## Activation

The active locale is owned by `i18next`. On boot and on every
`languageChanged` event, `src/app/i18n/rtl.ts#registerRtlListener` writes the
correct attributes to `<html>`:

| Attribute  | Value                                              |
| ---------- | -------------------------------------------------- |
| `dir`      | `rtl` for locales in `RTL_CODES`, `ltr` otherwise. |
| `lang`     | Two-letter language code derived from i18n.        |
| `data-dir` | Mirrors `dir`; usable as a CSS attribute selector. |

`RTL_CODES` lives in `src/app/i18n/supportedLocales.ts` and currently contains
`ar`, `fa`, `he`, `ur`.

## Tailwind direction-aware classes

Tailwind v4 ships native `rtl:` and `ltr:` direction variants because the
`dir` attribute is set on `<html>`. Use the logical-property utilities
(`ps-*`, `pe-*`, `ms-*`, `me-*`, `start-*`, `end-*`) by default. When a layout
genuinely needs a physical override, prefix with `rtl:` or `ltr:`:

```tsx
<button className="ps-2 pe-3 rtl:flex-row-reverse">…</button>
```

## Visually validated surfaces

| Surface                                    | Notes                                                                                |
| ------------------------------------------ | ------------------------------------------------------------------------------------ |
| Admin Dashboard                            | KPI cards, chart legends and arrow icons mirror correctly.                           |
| Orders list                                | Table column order, status pills and pagination chevrons flip.                       |
| Invoice detail                             | Line-item table, totals block, and "Back" arrow point to the right when in RTL.      |
| Navbar profile menu and `LanguageSwitcher` | Dropdown opens from the trailing edge; selected check mark sits on the leading side. |

## Manual test plan

1. Sign in to the admin SPA.
2. Open the navbar profile menu and pick `العربية`.
3. The page should re-render without a reload; `<html dir>` becomes `rtl` and
   the `lang` becomes `ar`.
4. Visit `/dashboard`, `/orders`, `/invoices/:id`. Confirm:
   - Sidebar swaps to the right edge of the viewport.
   - Chevrons (`<` / `>`) inside pagination and "Back" buttons flip.
   - Table headers read right-to-left.
   - Text inputs are right-aligned and the cursor starts on the right edge.
5. Switch back to `English`. `<html dir>` should immediately return to `ltr`.

## Automated coverage

- `src/app/i18n/__tests__/rtl.test.ts` asserts that the listener writes
  `dir="rtl"` for `ar` and `dir="ltr"` for `en`.
- `src/app/i18n/__tests__/supportedLocales.test.ts` asserts that Arabic is
  resolved with `dir: 'rtl'` and that region-tagged codes (`ar-SA`) normalise
  correctly.

## Adding a new RTL locale

1. Drop the translated JSON into `src/app/i18n/locales/<code>.json`.
2. Add the two-letter code to `RTL_CODES` in `supportedLocales.ts` if the
   language is not already Arabic-family.
3. Run `node scripts/i18n-completeness.mjs`. The locale is hidden from the
   switcher until it reaches **80%** parity with `en.json`.
4. Add the locale to `ALL_LOCALES` with the correct `nativeLabel` and
   `dir: 'rtl'`.

## Known gaps

- Charts that hardcode `direction: 'ltr'` for numeric axes (Recharts) remain
  LTR by design; only chart legends and tooltip arrows mirror.
- PDF rendering (QuestPDF) is not direction-aware yet — invoice/order PDFs
  always render LTR. Tracked as a follow-up.
