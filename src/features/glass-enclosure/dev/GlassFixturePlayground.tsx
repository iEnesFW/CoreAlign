import { useEffect, useMemo } from 'react';
import { useQueryClient } from '@tanstack/react-query';
import { useSearchParams } from 'react-router-dom';
import { CanvasPanel } from '../designer/panels';
import { useDesignerStore } from '../model/designerStore';
import {
  useColorOptionsQuery,
  useGlassTypesQuery,
  useProfileSystemsQuery,
} from '../hooks/useGlassEnclosureQueries';
import { authApi } from '@/features/auth/api/authApi';
import { useAuthStore } from '@/shared/lib/store/authStore';
import { SceneDataExporter } from './SceneDataExporter';
import { buildFixtureScene, DEFAULT_FIXTURE } from './fixtures';

// WHY: dev-only visual-verification playground. Renders the REAL 3D designer canvas against a
// deterministic fixture scene (injected into the store via applyScene) using the REAL catalogs — so
// the glass renders exactly like the normal designer. To stay login-free it auto-signs-in with the
// public demo credentials (DemoDataSeeder) when the session isn't authenticated; the refresh cookie
// then persists, so id/pw is never asked again. Registered only under import.meta.env.DEV (App.tsx).

const noop = () => undefined;
const DEMO_EMAIL = 'admin@demo.local';
const DEMO_PASSWORD = 'Demo!2345';

export function GlassFixturePlayground() {
  const [params] = useSearchParams();
  const sceneKey = params.get('scene') ?? DEFAULT_FIXTURE;
  const isAuthenticated = useAuthStore((s) => s.isAuthenticated);
  const authReady = useAuthStore((s) => s.authReady);
  const applyScene = useDesignerStore((s) => s.applyScene);
  const queryClient = useQueryClient();

  useEffect(() => {
    if (!import.meta.env.DEV) return;
    if (isAuthenticated || !authReady) return;
    let cancelled = false;
    authApi
      .login({ email: DEMO_EMAIL, password: DEMO_PASSWORD })
      .then((response) => {
        if (cancelled || !response.isSuccess || !response.data) return;
        useAuthStore.getState().setAuth(response.data.accessToken, response.data.user);
        // WHY: catalog queries fired (and 401'd) before the token existed; refetch them now.
        queryClient.invalidateQueries();
      })
      .catch(() => undefined);
    return () => {
      cancelled = true;
    };
  }, [isAuthenticated, authReady, queryClient]);

  const profileSystemsData = useProfileSystemsQuery().data;
  const glassTypesData = useGlassTypesQuery().data;
  const colorsData = useColorOptionsQuery().data;

  const profileSystems = useMemo(() => profileSystemsData?.data ?? [], [profileSystemsData]);
  const glassTypes = useMemo(() => glassTypesData?.data ?? [], [glassTypesData]);
  const colors = useMemo(() => colorsData?.data ?? [], [colorsData]);

  const ready = profileSystems.length > 0 && glassTypes.length > 0;

  const scene = useMemo(() => {
    if (!ready) return null;
    return buildFixtureScene(sceneKey, {
      profileSystemId: profileSystems[0].id,
      glassTypeId: glassTypes[0].id,
      colorId: colors[0]?.id ?? null,
    });
  }, [ready, sceneKey, profileSystems, glassTypes, colors]);

  useEffect(() => {
    if (scene) applyScene(scene);
  }, [applyScene, scene]);

  return (
    <div className="fixed inset-0 bg-white dark:bg-slate-950">
      <div
        data-testid="fixture-scene-key"
        data-scene={sceneKey}
        data-ready={ready ? 'true' : 'false'}
        className="pointer-events-none absolute left-2 top-2 z-10 rounded bg-black/70 px-2 py-1 text-xs text-white"
      >
        fixture: {sceneKey}
        {!ready && ' (loading…)'}
      </div>
      <SceneDataExporter />
      {ready ? (
        <div className="h-full w-full">
          <CanvasPanel
            view="3d"
            profileSystems={profileSystems}
            glassTypes={glassTypes}
            colors={colors}
            onAddRunFromPlan={noop}
            onUpdateRunGeometry={noop}
            onSelectConnectionCandidate={noop}
          />
        </div>
      ) : (
        <div className="flex h-full items-center justify-center text-sm text-slate-500">
          Loading catalogs…
        </div>
      )}
    </div>
  );
}
