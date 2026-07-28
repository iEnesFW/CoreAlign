import type { ReactNode } from 'react';
import { paneSurfaceFrame } from '../../model/paneSurface';
import type { PaneSurface, SurfaceOffsetMm } from '../../model/paneSurface';

interface PaneMountProps {
  surface: PaneSurface;
  offset: SurfaceOffsetMm;
  children: ReactNode;
}

/**
 * Mounts anything ON a glass pane — hardware, a built-in handle, a lock — at the pane's own surface.
 *
 * WHY it is one component: the flat and curved panes used to mount their children through two
 * separate hand-written transforms, and they drifted. The built-in handle stepped to the pane edge
 * inside a FLAT chord frame and left a curved pane by up to 353 mm, while user-placed hardware on
 * the SAME pane sat correctly. `paneSurfaceFrame` treats a flat pane as the degenerate curved one,
 * so there is a single expression left to get right.
 */
export function PaneMount({ surface, offset, children }: PaneMountProps) {
  const frame = paneSurfaceFrame(surface, offset);
  return (
    <group position={frame.positionM} rotation={[0, frame.yawRad, 0]}>
      {children}
    </group>
  );
}
