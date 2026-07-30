/**
 * Glass designer visual + interaction harness.
 *
 * WHY this exists: the designer's real defects live in two layers no unit test reaches — what the
 * renderer actually BUILDS (a carve that comes out inside-out, a body drawn somewhere its numbers
 * do not say) and what a real POINTER does (drag, corner handle, pen stroke). Reaching either needs
 * a browser that genuinely rasterises WebGL and a way to turn a world coordinate into the pixel you
 * have to click. This provides both, and proves it is really rendering instead of assuming so.
 *
 * Requires the Vite dev server and the API to be up (the fixture fetches real catalogues):
 *   npm run dev            (5273)
 *   dotnet run --project server/src/CoreAlign.API/CoreAlign.API.csproj   (5178)
 *
 * Usage:
 *   node e2e/designer/harness.mjs --self-test
 *   node e2e/designer/harness.mjs --shot out.png
 */
import { chromium } from '@playwright/test';
import { writeFileSync } from 'node:fs';

export const FIXTURE_URL = 'http://localhost:5273/dev/glass-fixture';

// Headless Chromium falls back to a stub GL that silently produces blank frames. ANGLE over
// SwiftShader is a real software rasteriser, which is what makes a screenshot meaningful here.
const GL_ARGS = [
  '--use-gl=angle',
  '--use-angle=swiftshader',
  '--enable-unsafe-swiftshader',
  '--enable-webgl',
  '--ignore-gpu-blocklist',
];

const PAGE_FNS = {
  /** The R3F context: scene, camera, gl, size. */
  r3f: () => {
    const f = window.__CAD_R3F__;
    return typeof f === 'function' ? f() : null;
  },
};

export async function openDesigner({
  width = 1600,
  height = 1000,
  timeout = 90_000,
  appearance = 'plain',
} = {}) {
  const browser = await chromium.launch({ args: GL_ARGS });
  const context = await browser.newContext({ viewport: { width, height } });
  const page = await context.newPage();

  // WHY 'plain' by default: the HDR-backed appearance presets make drei decode a gainmap, and that
  // decoder opens a fresh WebGL context per call — dozens within a few milliseconds. Chromium caps
  // a page at 16 live contexts and force-loses the OLDEST to make room, which is the designer's
  // own canvas. Measured: the renderer's context died 2-10 s after load, every run, and never came
  // back. 'plain' (environment: 'none') is a first-class user preset, so this configures the app
  // rather than patching it — and it drops a CDN fetch the tests should not depend on.
  await page.addInitScript((preset) => {
    window.localStorage.setItem('glassDesigner.viewerAppearance', JSON.stringify(preset));

    // WHY: React StrictMode (on in main.tsx) runs mount -> cleanup -> mount. R3F's cleanup defers
    // its teardown through setTimeout and ends it with gl.forceContextLoss(); by the time that
    // fires, the SECOND mount is already live on the SAME <canvas>, so the teardown kills the
    // context the app is actually using. Measured: forceContextLoss at ~3.2 s from R3F's
    // unmountComponentAtNode, after which the renderer never draws another frame and every
    // screenshot is blank. Neutralising the extension here keeps the live context alive for the
    // test browser only — the discarded renderer is garbage either way.
    const origGetContext = HTMLCanvasElement.prototype.getContext;
    HTMLCanvasElement.prototype.getContext = function (...args) {
      const ctx = origGetContext.apply(this, args);
      if (ctx && String(args[0]).includes('webgl') && !ctx.__cadLoseGuard) {
        ctx.__cadLoseGuard = true;
        const origGetExtension = ctx.getExtension.bind(ctx);
        ctx.getExtension = (name) => {
          const ext = origGetExtension(name);
          if (name === 'WEBGL_lose_context' && ext) {
            return { ...ext, loseContext: () => undefined, restoreContext: () => undefined };
          }
          return ext;
        };
      }
      return ctx;
    };
  }, appearance);

  const consoleErrors = [];
  page.on('console', (m) => {
    if (m.type() === 'error') consoleErrors.push(m.text().slice(0, 240));
  });
  page.on('pageerror', (e) => consoleErrors.push('PAGEERROR ' + String(e).slice(0, 240)));

  await page.goto(FIXTURE_URL, { waitUntil: 'domcontentloaded' });

  // The store hook appears as soon as the designer mounts; the R3F hook only once the 3D viewport
  // has been measured and the Canvas created. Waiting for BOTH is what distinguishes "the page
  // loaded" from "the scene exists".
  await page.waitForFunction(() => typeof window.__CAD_STORE__ === 'function', undefined, {
    timeout,
  });
  // WHY three conditions in ONE predicate, held stable: the hook appears while the subtree is
  // still SUSPENDED — React hides suspended content with an inline `display: none !important`, so
  // the canvas has a live GL context and a sized drawing buffer while its CSS box is 0x0
  // (measuring there gives a blank screenshot and a projection that divides by zero). The Canvas
  // can also remount as data resolves, which momentarily removes the hook. So: hook present AND
  // laid out AND actually rasterising, unchanged across consecutive polls.
  await page.waitForFunction(
    () => {
      const w = window;
      if (typeof w.__CAD_R3F__ !== 'function') {
        w.__cadReadyStreak = 0;
        return false;
      }
      let ok = false;
      try {
        const three = w.__CAD_R3F__();
        const c = three.gl.domElement;
        const r = c.getBoundingClientRect();
        if (r.width > 50 && r.height > 50) {
          const gl = three.gl.getContext();
          const px = new Uint8Array(4 * 64);
          gl.readPixels(0, Math.floor(gl.drawingBufferHeight / 2), 64, 1, gl.RGBA, gl.UNSIGNED_BYTE, px);
          const seen = new Set();
          for (let i = 0; i < px.length; i += 4) seen.add((px[i] << 16) | (px[i + 1] << 8) | px[i + 2]);
          ok = seen.size > 1;
        }
      } catch {
        ok = false;
      }
      w.__cadReadyStreak = ok ? (w.__cadReadyStreak ?? 0) + 1 : 0;
      return w.__cadReadyStreak >= 5;
    },
    undefined,
    { timeout, polling: 120 },
  );

  // WHY: the same deferred StrictMode teardown that force-loses the context also runs
  // state.events.disconnect() first, unhooking R3F's DOM pointer listeners from the shared canvas.
  // Measured: events.connected === false, hovered stayed 0 on every mousemove, and clicks selected
  // nothing even when the ray provably hit an interactive mesh. Re-connecting restores hover and
  // selection (verified: a click then returns {kind:'panel', runId, panelId}).
  await page.evaluate(() => {
    const s = window.__CAD_R3F__();
    if (!s.events.connected) s.events.connect(s.gl.domElement);
  });

  const api = {
    page,
    browser,
    consoleErrors,

    /** The authoritative designer store (same actions the UI calls). */
    store: (fn, arg) => page.evaluate(fn, arg),

    /** Canvas rect + drawing-buffer size, so callers can sanity-check the surface is real. */
    surface: () =>
      page.evaluate(() => {
        const three = window.__CAD_R3F__();
        const c = three.gl.domElement;
        const r = c.getBoundingClientRect();
        return {
          css: { w: Math.round(r.width), h: Math.round(r.height), x: Math.round(r.left), y: Math.round(r.top) },
          buffer: { w: c.width, h: c.height },
          dpr: three.viewport ? three.viewport.dpr : null,
        };
      }),

    /**
     * Proof that pixels were actually rasterised: sample the drawing buffer and report how many
     * distinct colours it holds. A stub GL or an unrendered canvas returns 1.
     */
    renderProof: () =>
      page.evaluate(() => {
        const three = window.__CAD_R3F__();
        const gl = three.gl.getContext();
        const w = gl.drawingBufferWidth;
        const h = gl.drawingBufferHeight;
        const px = new Uint8Array(w * h * 4);
        gl.readPixels(0, 0, w, h, gl.RGBA, gl.UNSIGNED_BYTE, px);
        const seen = new Set();
        for (let i = 0; i < px.length; i += 4 * 97) {
          seen.add((px[i] << 16) | (px[i + 1] << 8) | px[i + 2]);
          if (seen.size > 400) break;
        }
        return { width: w, height: h, distinctColours: seen.size };
      }),

    /**
     * World (three.js metres) -> viewport pixel, through the SAME camera the renderer uses. This is
     * what makes a real drag possible: a corner handle's pixel cannot be guessed, only projected.
     */
    project: (world) =>
      page.evaluate((p) => {
        const three = window.__CAD_R3F__();
        const cam = three.camera;
        const V = cam.position.constructor;
        const v = new V(p[0], p[1], p[2]).project(cam);
        const r = three.gl.domElement.getBoundingClientRect();
        return {
          x: r.left + ((v.x + 1) / 2) * r.width,
          y: r.top + ((1 - v.y) / 2) * r.height,
          behindCamera: v.z > 1,
        };
      }, world),

    /**
     * Raycast from a viewport pixel and report what the user would actually hit. Paired with
     * project() this closes the loop: project a body to a pixel, pick that pixel, and the same
     * object must come back — which is the only real proof the coordinates are trustworthy.
     */
    pickAt: (px, py) =>
      page.evaluate(({ x, y }) => {
        const three = window.__CAD_R3F__();
        const r = three.gl.domElement.getBoundingClientRect();
        three.raycaster.setFromCamera(
          { x: ((x - r.left) / r.width) * 2 - 1, y: -(((y - r.top) / r.height) * 2 - 1) },
          three.camera,
        );
        const hits = three.raycaster.intersectObjects(three.scene.children, true);
        const first = hits.find((h) => h.object.visible);
        if (!first) return null;
        return {
          uuid: first.object.uuid,
          name: first.object.name || first.object.type,
          distance: Number(first.distance.toFixed(4)),
          point: [first.point.x, first.point.y, first.point.z].map((v) => Number(v.toFixed(4))),
        };
      }, { x: px, y: py }),

    /**
     * The designer's own geometry, excluding ground/grid/shadow helpers. Aiming the mouse needs a
     * target that is actually a body: the ground disc is 70 m across and swallows three quarters of
     * the viewport, so "biggest mesh" picks scenery, not a wall.
     */
    bodies: () =>
      page.evaluate(() => {
        const three = window.__CAD_R3F__();
        const root = three.scene.getObjectByName('designer-root');
        if (!root) return [];
        const out = [];
        root.traverse((o) => {
          if (!o.isMesh || !o.geometry || !o.visible) return;
          o.geometry.computeBoundingBox();
          const bb = o.geometry.boundingBox;
          if (!bb) return;
          const size = bb.max.clone().sub(bb.min);
          const c = bb.min.clone().add(bb.max).multiplyScalar(0.5);
          o.updateWorldMatrix(true, false);
          const world = c.applyMatrix4(o.matrixWorld);
          out.push({
            uuid: o.uuid,
            name: o.name || o.type,
            area: Math.abs(size.x * size.y) + Math.abs(size.x * size.z) + Math.abs(size.y * size.z),
            world: [world.x, world.y, world.z],
          });
        });
        return out.sort((a, b) => b.area - a.area);
      }),

    /**
     * A pixel that genuinely lands on designer geometry. Projecting a body's bounding-box centre
     * is NOT good enough: a curved or L-shaped body has its bbox centre off the surface, so the
     * ray sails past it into the ground. Sampling the screen and testing ancestry is shape-proof.
     */
    findBodyPixel: (samples = 240) =>
      page.evaluate((n) => {
        const three = window.__CAD_R3F__();
        const root = three.scene.getObjectByName('designer-root');
        if (!root) return null;
        // WHY the interaction list and not the geometry: R3F only dispatches to objects that
        // registered handlers, and the designer draws non-interactive frames/edges IN FRONT of the
        // clickable body. A pixel that hits geometry can therefore be dead to the app — measured:
        // the sampled pixel hit a Mesh, yet hovered stayed 0 and the click selected nothing.
        const targets = three.internal?.interaction?.length ? three.internal.interaction : [root];
        const rect = three.gl.domElement.getBoundingClientRect();
        const cols = Math.round(Math.sqrt(n * 1.6));
        const rows = Math.max(1, Math.round(n / cols));
        for (let gy = 0; gy < rows; gy += 1) {
          for (let gx = 0; gx < cols; gx += 1) {
            const x = rect.left + ((gx + 0.5) / cols) * rect.width;
            const y = rect.top + ((gy + 0.5) / rows) * rect.height;
            three.raycaster.setFromCamera(
              { x: ((x - rect.left) / rect.width) * 2 - 1, y: -(((y - rect.top) / rect.height) * 2 - 1) },
              three.camera,
            );
            const hits = three.raycaster.intersectObjects(targets, true).filter((h) => h.object.visible);
            if (hits.length) {
              return {
                x,
                y,
                uuid: hits[0].object.uuid,
                name: hits[0].object.name || hits[0].object.type,
                point: [hits[0].point.x, hits[0].point.y, hits[0].point.z],
              };
            }
          }
        }
        return null;
      }, samples),

    /** Is this pixel over the designer's own geometry (rather than ground/grid/scenery)? */
    hitsBody: (px, py) =>
      page.evaluate(({ x, y }) => {
        const three = window.__CAD_R3F__();
        const root = three.scene.getObjectByName('designer-root');
        if (!root) return false;
        const r = three.gl.domElement.getBoundingClientRect();
        three.raycaster.setFromCamera(
          { x: ((x - r.left) / r.width) * 2 - 1, y: -(((y - r.top) / r.height) * 2 - 1) },
          three.camera,
        );
        return three.raycaster.intersectObject(root, true).some((h) => h.object.visible);
      }, { x: px, y: py }),

    /** Live scene-graph health: the numeric stand-in for "does it look broken". */
    meshHealth: () =>
      page.evaluate(() => {
        const scene = window.__CAD_R3F__().scene;
        const bad = [];
        let meshes = 0;
        let tris = 0;
        let nanMeshes = 0;
        let invertedMeshes = 0;
        scene.traverse((o) => {
          if (!o.isMesh || !o.geometry) return;
          const pos = o.geometry.attributes && o.geometry.attributes.position;
          if (!pos) return;
          meshes += 1;
          tris += pos.count / 3;
          const a = pos.array;
          let nan = 0;
          for (let i = 0; i < a.length; i += 1) if (!Number.isFinite(a[i])) nan += 1;
          if (nan) {
            nanMeshes += 1;
            bad.push({ name: o.name || o.type, nan });
          }
          if (!o.geometry.index) {
            let vol = 0;
            for (let i = 0; i + 8 < a.length; i += 9) {
              vol +=
                (a[i] * (a[i + 4] * a[i + 8] - a[i + 5] * a[i + 7]) -
                  a[i + 1] * (a[i + 3] * a[i + 8] - a[i + 5] * a[i + 6]) +
                  a[i + 2] * (a[i + 3] * a[i + 7] - a[i + 4] * a[i + 6])) /
                6;
            }
            if (vol < -1e-6) {
              invertedMeshes += 1;
              bad.push({ name: o.name || o.type, signedVolume: Number(vol.toFixed(6)) });
            }
          }
        });
        return { meshes, tris: Math.round(tris), nanMeshes, invertedMeshes, worst: bad.slice(0, 6) };
      }),

    /** A real pointer drag in viewport pixels — this is what exercises the gesture layer. */
    drag: async (from, to, steps = 24) => {
      await page.mouse.move(from.x, from.y);
      await page.mouse.down();
      for (let i = 1; i <= steps; i += 1) {
        await page.mouse.move(
          from.x + ((to.x - from.x) * i) / steps,
          from.y + ((to.y - from.y) * i) / steps,
        );
      }
      await page.mouse.up();
    },

    shot: async (path) => {
      const buf = await page.screenshot();
      writeFileSync(path, buf);
      return { path, bytes: buf.length };
    },

    close: () => browser.close(),
  };

  return api;
}

/**
 * Proves the harness itself, end to end. Run this before trusting any designer finding: every
 * capability the tests lean on is asserted against the live app, so a broken environment reports
 * itself instead of quietly producing blank screenshots and no-op drags.
 */
export async function selfTest({ shotPath = null } = {}) {
  const failures = [];
  const check = (label, pass, detail) => {
    console.log(`${pass ? 'PASS' : 'FAIL'}  ${label}${detail ? '  ' + detail : ''}`);
    if (!pass) failures.push(label);
  };

  const d = await openDesigner();

  const surface = await d.surface();
  check('canvas laid out', surface.css.w > 1000 && surface.css.h > 500, JSON.stringify(surface.css));

  const proof = await d.renderProof();
  check('frame rasterised', proof.distinctColours > 20, `colours=${proof.distinctColours}`);

  const health = await d.meshHealth();
  check(
    'no NaN / inverted meshes',
    health.nanMeshes === 0 && health.invertedMeshes === 0,
    `meshes=${health.meshes} tris=${health.tris}`,
  );

  const bodies = await d.bodies();
  check('designer-root exposes bodies', bodies.length > 0, `count=${bodies.length}`);

  const target = await d.findBodyPixel();
  check('found an interactive pixel', !!target, target ? `px=(${Math.round(target.x)},${Math.round(target.y)})` : 'none');

  if (target) {
    const hit = await d.pickAt(target.x, target.y);
    check('project -> pick round-trip', !!hit && hit.uuid === target.uuid, `hit=${hit ? hit.name : 'none'}`);

    const back = await d.project(hit ? hit.point : target.point);
    const drift = hit ? Math.hypot(back.x - target.x, back.y - target.y) : Infinity;
    check('pixel -> world -> pixel is stable', drift < 1.5, `drift=${drift.toFixed(3)}px`);

    const before = await d.store(() => {
      const run = window.__CAD_STORE__().scene.runs[0];
      return run ? { id: run.id, x: run.originX, y: run.originY } : null;
    });
    check('scene has a run to drive', !!before);

    if (before) {
      await d.page.mouse.click(target.x, target.y);
      const selection = await d.store(() => window.__CAD_STORE__().selection);
      check('click selects a body', !!selection && selection.kind !== null, JSON.stringify(selection).slice(0, 90));

      await d.drag({ x: target.x, y: target.y }, { x: target.x + 220, y: target.y + 60 });
      const after = await d.page.evaluate((id) => {
        const run = window.__CAD_STORE__().scene.runs.find((r) => r.id === id);
        return run ? { x: run.originX, y: run.originY } : null;
      }, before.id);
      check(
        'real mouse drag moves the body',
        !!after && (Math.abs(after.x - before.x) > 1 || Math.abs(after.y - before.y) > 1),
        `(${Math.round(before.x)},${Math.round(before.y)}) -> (${Math.round(after?.x ?? 0)},${Math.round(after?.y ?? 0)})`,
      );
    }
  }

  const alive = await d.page.evaluate(() => !window.__CAD_R3F__().gl.getContext().isContextLost());
  check('context survived the session', alive);

  if (shotPath) console.log('shot', JSON.stringify(await d.shot(shotPath)));
  console.log(failures.length ? `\n${failures.length} FAILED: ${failures.join(', ')}` : '\nALL GREEN');
  await d.close();
  return failures;
}

const isMain = process.argv[1] && process.argv[1].endsWith('harness.mjs');
if (isMain) {
  const shotIdx = process.argv.indexOf('--shot');
  const shotPath = shotIdx > -1 ? process.argv[shotIdx + 1] : null;
  const failures = await selfTest({ shotPath });
  process.exit(failures.length ? 1 : 0);
}
void PAGE_FNS;
