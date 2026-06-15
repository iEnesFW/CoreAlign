# 09. i18n Workflow

Linter, locale-parity gating, and the process for adding a new locale across the
three SPAs (`src/` admin, `apps/customer-portal/`, `apps/b2b/`).

## Locale Files

| App             | Path                                                | Required Locales |
| --------------- | --------------------------------------------------- | ---------------- |
| Admin           | `src/app/i18n/locales/{en,tr}.json`                 | en, tr           |
| Customer Portal | `apps/customer-portal/src/app/locales/{en,tr}.json` | en, tr           |
| B2B Dealer      | `apps/b2b/src/app/locales/{en,tr}.json`             | en, tr           |

Optional / gated locales (admin only today): `ar`, `de`, `ru`.

## Linter (`npm run lint:i18n`)

Script: `scripts/i18n-lint.mjs`. It walks each SPA, extracts every `t('...')`
key invocation (string and template-literal forms), and compares against the
union of keys in en + tr.

### Reported Categories

1. **missing-in-en** — key present in tr but not en (hard fail).
2. **missing-in-tr** — key present in en but not tr (hard fail).
3. **used-but-undefined** — key appears in source but not in any locale
   (warning by default, hard fail under `--strict`).
4. **defined-but-unused** — key present in en but no `t('...')` reference in
   source. Informational only — never fails. The first 20 stale keys per app
   are listed; the remainder is summarized as `+N more`. Prefix matches (e.g.
   `t('Page.Section')` used as a namespace via `useTranslation('Page')`) are
   treated as live.

### Exit Codes

| Mode            | EN/TR parity broken | used-but-undefined | defined-but-unused |
| --------------- | ------------------- | ------------------ | ------------------ |
| Default         | exit 1              | warn (exit 0)      | info (exit 0)      |
| `--strict` (CI) | exit 1              | exit 1             | info (exit 0)      |

`npm run lint` chains `lint:i18n` before `eslint`, so any parity break blocks
the lint pipeline. Enable strict mode in CI with
`node scripts/i18n-lint.mjs --strict`.

## Locale Parity Gating (Customer Portal)

Gated languages stay hidden in the language switcher until they reach **80% key
parity** with en. See `apps/customer-portal/src/shared/lib/languageGating.ts`
and `apps/customer-portal/src/widgets/Topbar.tsx`.

The switcher calls `visibleLanguages(loadGatedLocales())` at render time, which
evaluates parity against the bundled en locale. `loadGatedLocales()` uses
`import.meta.glob` to auto-discover any `ar.json` / `de.json` / `ru.json` that
appears in `apps/customer-portal/src/app/locales/`. To reveal a new locale,
drop the JSON file at the matching path and rebuild — no Topbar edit needed.

## Adding a New Locale (Worked Example)

Assume adding `de` to the customer portal.

1. **Translate** `apps/customer-portal/src/app/locales/en.json` into
   `apps/customer-portal/src/app/locales/de.json` — keep the structure
   identical.
2. **Register** the locale in `apps/customer-portal/src/app/i18n.ts`:

   ```ts
   import de from './locales/de.json';
   // ...
   resources: { en: { translation: en }, tr: { translation: tr }, de: { translation: de } },
   supportedLngs: ['en', 'tr', 'de'],
   ```

3. **Auto-discovery**: `loadGatedLocales()` picks up `de.json` automatically via
   `import.meta.glob`. No code change is required in `languageGating.ts` or the
   Topbar to expose `de` in the switcher.
4. **Run the linter** to confirm parity is above 80%:

   ```bash
   npm run lint:i18n
   ```

5. **Re-run** the full lint pipeline:

   ```bash
   npm run lint
   ```

## Adding a Key

1. Define the key in **both** `en.json` and `tr.json` for the affected SPA
   (admin / customer / b2b). Optional locales can lag behind, but parity must
   eventually reach the 80% threshold to be revealed in the switcher.
2. Use a hierarchical dotted path: `PageName.SectionName.Label`.
3. Reference the key via `t('PageName.SectionName.Label')` — never hard-code
   user-facing strings.
4. Run `npm run lint:i18n` to confirm no parity break.
