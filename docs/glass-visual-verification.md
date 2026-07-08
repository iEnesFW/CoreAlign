# Glass 3D Designer — Visual & Numerical Verification Harness

A backend/auth-free way to render and inspect the glass 3D designer against **deterministic fixture
scenes**, so the designer can be verified without a running backend, a seeded project, or logging in.

This is the concrete implementation of the three-tier verification approach (see
`docs/3d_cad_ai_testing_framework.md` for the original proposal and the design critique):

| Tier                                              | What                                                                                           | Status                                    |
| ------------------------------------------------- | ---------------------------------------------------------------------------------------------- | ----------------------------------------- |
| **T1 — Pure geometry (vitest)**                   | `arcGeometry` / `panelOutline` / `curvedExtrude` / `planCollision` tests                       | ✅ existing                               |
| **T2 — Live scene data (`window.__CAD_SCENE__`)** | exact mm geometry from the store (radius/sweep/arcLength/shape) for numerical invariant checks | ✅ **works, autonomous**                  |
| **T3 — Visual screenshot**                        | eyeball the render (framing, frame ring, orientation)                                          | ⚠️ **real browser only** (see limitation) |

## Pieces

- **`/dev/glass-fixture` route** — registered only under `import.meta.env.DEV` (never in production),
  outside auth. Injects a named fixture scene into the designer store and renders the real
  `CanvasPanel` with mock catalogs + low quality. Query params:
  - `?scene=<key>` — a fixture from `src/features/glass-enclosure/dev/fixtures.ts`
    (`arc-holefill-triangle`, `straight-run`, …). Add more there.
- **`SceneDataExporter`** (`src/features/glass-enclosure/dev/`) — exposes `window.__CAD_SCENE__()`
  (the exact store scene + derived arc math). Dev/E2E-gated. This is a STORE export (mm-precise),
  deliberately not a lossy `THREE.Box3` readback.
- **`scripts/glass-fixture-verify.mjs`** — loads a fixture headless and prints `__CAD_SCENE__()`.

## Usage

```bash
npm run dev                 # Vite on :5273 (no backend needed for /dev/glass-fixture)
# open in a NORMAL browser to SEE the render:
#   http://localhost:5273/dev/glass-fixture?scene=arc-holefill-triangle
# or read exact geometry autonomously:
npm run e2e:install         # once, for Playwright chromium
node scripts/glass-fixture-verify.mjs arc-holefill-triangle
```

## Known limitation — automated screenshot

Capturing the WebGL canvas through **Playwright automation** is unreliable for this app: the React
Three Fiber renderer emits `THREE.WebGLRenderer: Context Lost.` under Playwright's browser
(reproduced across headless, headed, real-GPU, SwiftShader, and low quality — so it is not a
render-load or GPU issue but an automation/`preserveDrawingBuffer` incompatibility). The captured
PNG is therefore usually blank.

The route renders **correctly in a normal browser** (the app itself works). So the visual workflow is:
open `/dev/glass-fixture?scene=…` in a real browser and eyeball / screenshot it; use
`__CAD_SCENE__()` (via the script) for the autonomous numerical checks.
