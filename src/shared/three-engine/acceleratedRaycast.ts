import { Mesh } from 'three';
import { BufferGeometry } from 'three';
import { acceleratedRaycast, computeBoundsTree, disposeBoundsTree } from 'three-mesh-bvh';

/**
 * Give every mesh a BVH-accelerated raycast.
 *
 * WHY: three's default `Mesh.raycast` walks EVERY triangle. R3F raycasts the scene on each pointer
 * event, so a wall carved by CSG (a hole multiplies its triangle count) makes hover and drag cost
 * grow with how much detail the user has modelled — the designer gets heavier the more work is in
 * it, which is exactly the reported symptom. A bounds tree turns that walk into a log-depth
 * descent.
 *
 * The patch is global and idempotent; call it once at engine entry. Geometries opt IN by calling
 * `ensureBoundsTree` — an un-built geometry silently falls back to the brute-force path, so this is
 * safe for every mesh in the app, not just the ones we accelerate.
 */
let patched = false;

export const installAcceleratedRaycast = () => {
  if (patched) return;
  patched = true;
  const proto = BufferGeometry.prototype as unknown as Record<string, unknown>;
  proto.computeBoundsTree = computeBoundsTree;
  proto.disposeBoundsTree = disposeBoundsTree;
  Mesh.prototype.raycast = acceleratedRaycast;
};

/** Build the bounds tree for a geometry that is about to be raycast a lot. Cheap no-op if present. */
export const ensureBoundsTree = (geometry: BufferGeometry | null | undefined) => {
  if (!geometry) return;
  const g = geometry as BufferGeometry & {
    boundsTree?: unknown;
    computeBoundsTree?: () => void;
  };
  if (g.boundsTree || typeof g.computeBoundsTree !== 'function') return;
  if (!g.getAttribute('position')) return;
  g.computeBoundsTree();
};
