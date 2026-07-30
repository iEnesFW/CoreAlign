/** Name carried by the group that holds the designer's own bodies (not ground/grid/shadows). */
export const DESIGNER_ROOT_NAME = 'designer-root';

/** One click of the zoom buttons: pull the camera 25% closer, or push it 25% further out. */
export const ZOOM_STEP = 1.25;

export interface ViewportCameraApi {
  fitTo: (objectName: string) => boolean;
  zoomBy: (factor: number) => boolean;
  /** Is any part of the object currently inside the viewport and in front of the camera? */
  framesObject: (objectName: string) => boolean;
}

let current: ViewportCameraApi | null = null;

export function registerViewportCamera(api: ViewportCameraApi): () => void {
  current = api;
  // WHY ownership-guarded, like the R3F handle: a viewport remount can run the OLD instance's
  // cleanup after the NEW one registered, and an unconditional clear would leave the toolbar
  // driving a disposed camera.
  return () => {
    if (current === api) current = null;
  };
}

export const viewportCamera: ViewportCameraApi = {
  fitTo: (objectName) => current?.fitTo(objectName) ?? false,
  zoomBy: (factor) => current?.zoomBy(factor) ?? false,
  framesObject: (objectName) => current?.framesObject(objectName) ?? false,
};

export function isViewportCameraReady(): boolean {
  return current !== null;
}
