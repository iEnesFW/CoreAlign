# Performance Budget

CoreAlign ships three SPAs (`src/` admin, `apps/customer-portal`, `apps/b2b`).
Bundle-size discipline keeps Time-to-Interactive predictable on
field-engineer tablets and mid-tier customer hardware. This document defines
the per-chunk budgets that CI enforces and the process for raising them.

## Budgets (gzipped on the wire, asserted uncompressed on disk)

| Chunk pattern                                   | Cap (raw `.js` on disk) | Notes                                                                        |
| ----------------------------------------------- | ----------------------- | ---------------------------------------------------------------------------- |
| `vendor-3d-*.js`                                | 1500 KB                 | Three.js + react-three/fiber + drei. Required for glass-enclosure 3D viewer. |
| `index-*.js` (main entry)                       | 800 KB                  | App shell + router + shared layout.                                          |
| Any other non-vendor chunk                      | 600 KB                  | Route-level lazy chunks.                                                     |
| `vendor-*-*.js` (charts/forms/i18n/query/react) | 600 KB soft cap         | Treated like a regular chunk by the gate.                                    |

Caps are intentionally on the raw size. Brotli ratios vary across deploys —
asserting on raw disk size gives a stable, reproducible CI signal and an
honest "what is this code worth in source" number.

## Local Use

```bash
# Build then check the admin bundle:
npm run build && npm run check:bundle

# Check the B2B portal:
npm --prefix apps/b2b run build && npm run check:bundle:b2b

# Check the customer portal:
npm --prefix apps/customer-portal run build && npm run check:bundle:customer-portal

# Check all three:
npm run build \
  && npm --prefix apps/customer-portal run build \
  && npm --prefix apps/b2b run build \
  && npm run check:bundle:all
```

The script (`scripts/check-bundle-size.mjs`) scans `<dist>/assets/*.js`,
classifies each chunk, compares against the budget, prints a sorted top-15
report and exits 1 if any chunk overruns.

## CI Enforcement

The `bundle-size-gate` job in `.github/workflows/ci.yml` runs after the
three SPA builds and asserts on each `dist/`. A failure blocks the PR.

## Raising a Budget (Per-PR Override)

The script accepts overrides via flags **or** env vars so a single risky PR
can land without a workflow edit:

```bash
# Allow a 2 MB vendor-3d temporarily (CI step):
BUNDLE_VENDOR_3D_MAX_KB=2048 npm run check:bundle
```

| Flag                                  | Env var                                 |
| ------------------------------------- | --------------------------------------- |
| `--vendor-3d-max-kb`                  | `BUNDLE_VENDOR_3D_MAX_KB`               |
| `--main-max-kb`                       | `BUNDLE_MAIN_MAX_KB`                    |
| `--chunk-max-kb`                      | `BUNDLE_CHUNK_MAX_KB`                   |
| `--dist`                              | `BUNDLE_DIST`                           |
| `--allow-chunk <prefix>` (repeatable) | `BUNDLE_ALLOW_CHUNKS` (comma-separated) |

**The override must be removed in a follow-up PR.** Per CLAUDE.md sec 11
(Foresight), bundle creep is technical debt; the gate is the alarm.

## Documented Chunk Exemptions (Tracked Tech Debt)

| Chunk                      | Reason                                                                                                                                                        | Owner / Ticket       | Removal trigger                                                                                                       |
| -------------------------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------- | -------------------- | --------------------------------------------------------------------------------------------------------------------- |
| `AddressRegionFields-*.js` | Eagerly imports the full `country-state-city` dataset (~8 MB). Pre-existing repo state. CI passes `--allow-chunk AddressRegionFields` for the admin SPA only. | sprint11-blockers.md | Split via `React.lazy` + on-demand dataset fetch, OR replace with a lighter province lookup; then remove the CI flag. |
| `GlassProjectDesignerPage-*.js` | The 3D glass designer (three.js r128 + ~157 feature files: scene builders, inspectors, interaction handles) — inherently large. Already ~618 KB; the single-edge-arc + polygon-roof geometry work nudged it to ~627 KB (~8 KB delta). CI passes `--allow-chunk GlassProjectDesignerPage` for the admin SPA. | docs/Cam_Mekan_Modul_Plan.md | Code-split the scene builders + lazy-load the per-body inspectors behind the designer route; then remove the flag. |

## Tightening a Budget

Edit the defaults in `scripts/check-bundle-size.mjs` (the `DEFAULTS` const)
and this document. Re-run the gate on `main` to verify all three SPAs still
pass.

## Drift Investigation Checklist

When the gate fails:

1. Inspect the failing chunk(s): `npm run build -- --mode production` then
   `npx vite-bundle-visualizer` (already shipped) to see the source.
2. Check for accidental eager imports of `recharts`, `three`, or
   `@react-three/*` from a non-3D route — these belong in `vendor-3d` or a
   `React.lazy` boundary.
3. If a vendor was upgraded, compare with the npm bundle phobia / unpkg
   diff. If genuinely larger, raise the cap with justification in the PR
   description.
4. If a feature genuinely grew, split it: `React.lazy(() => import(...))`
   for new routes; `manualChunks` rule for shared vendors.

## Why These Numbers

| Cap               | Rationale                                                                                                                                              |
| ----------------- | ------------------------------------------------------------------------------------------------------------------------------------------------------ |
| 1500 KB vendor-3d | Three.js + drei alone is ~1.1 MB raw; we leave 400 KB head-room for the project. Anything above this hurts cold-start past 1.5 s on a 50 Mbps 4G link. |
| 800 KB main       | App shell must paint in <2 s on the demo dashboard. 800 KB raw = ~250 KB brotli.                                                                       |
| 600 KB chunk      | Route chunks should be smaller than the shell; otherwise their lazy benefit evaporates.                                                                |

## Related

- `scripts/check-bundle-size.mjs`
- `vite.config.ts` (admin) — `chunkSizeWarningLimit` + `manualChunks`
- `.github/workflows/ci.yml` — `bundle-size-gate` job
