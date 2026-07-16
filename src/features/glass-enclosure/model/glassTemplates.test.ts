import { describe, expect, it } from 'vitest';
import {
  captureSceneAsTemplate,
  parseTemplatePayload,
  parseUserGlassTemplates,
  templatePayloadJson,
} from './glassTemplates';
import type { SceneRunState, SceneState, SceneWallState } from './project.types';

const wall = (over: Partial<SceneWallState> = {}): SceneWallState => ({
  id: 'w',
  originX: 0,
  originY: 0,
  lengthMm: 4000,
  rotationDeg: 0,
  heightMm: 2600,
  heightEndMm: null,
  thicknessMm: 200,
  colorHex: null,
  geomZ: 0,
  openings: [],
  features: [],
  ...over,
});

const run = (over: Partial<SceneRunState> = {}): SceneRunState => ({
  id: 'r',
  orderIndex: 0,
  label: 'r',
  lengthMm: 1000,
  heightMm: 2000,
  originX: 0,
  originY: 0,
  rotationDeg: 0,
  profileSystemId: 'ps',
  colorId: null,
  hasTopDrip: true,
  hasBottomThreshold: false,
  geomZ: 0,
  panels: [],
  ...over,
});

const scene = (parts: Partial<SceneState>): SceneState => ({
  runs: [],
  connections: [],
  walls: [],
  slabs: [],
  surfaces: [],
  camera: null,
  metadata: { schemaVersion: 1, savedAt: '' },
  ...parts,
});

describe('captureSceneAsTemplate', () => {
  it('anchors the snapshot at (0,0), clears the group bond, and captures runs as drafts', () => {
    const s = scene({
      walls: [wall({ id: 'a', originX: 1000, originY: 500, groupId: 'g1' })],
      runs: [run({ id: 'r1', originX: 1400, originY: 500, lengthMm: 800 })],
    });
    const tpl = captureSceneAsTemplate(s, 'tpl-1', 'My room');
    expect(tpl.id).toBe('tpl-1');
    expect(tpl.name).toBe('My room');
    // min origin (1000,500) becomes (0,0)
    expect(tpl.walls[0].originX).toBe(0);
    expect(tpl.walls[0].originY).toBe(0);
    expect(tpl.walls[0].groupId).toBeNull();
    expect(tpl.runs[0]).toMatchObject({ originX: 400, originY: 0, lengthMm: 800 });
  });
});

describe('parseUserGlassTemplates', () => {
  it('keeps well-formed templates and drops malformed ones', () => {
    const raw = [
      { id: 'ok', name: 'Good', walls: [], slabs: [], runs: [] },
      { id: 'bad', name: 'NoArrays' }, // missing arrays
      { name: 'NoId', walls: [], slabs: [], runs: [] }, // missing id
      'nonsense',
    ];
    const parsed = parseUserGlassTemplates(raw);
    expect(parsed).toHaveLength(1);
    expect(parsed![0].id).toBe('ok');
  });

  it('returns null for a non-array payload', () => {
    expect(parseUserGlassTemplates({ nope: true })).toBeNull();
  });
});

describe('parseTemplatePayload', () => {
  it('parses a well-formed payload into its three arrays', () => {
    const parsed = parseTemplatePayload('{"walls":[{}],"slabs":[],"runs":[{},{}]}');
    expect(parsed).not.toBeNull();
    expect(parsed!.walls).toHaveLength(1);
    expect(parsed!.slabs).toHaveLength(0);
    expect(parsed!.runs).toHaveLength(2);
  });

  it('returns null for malformed JSON', () => {
    expect(parseTemplatePayload('not-json')).toBeNull();
  });

  it('returns null when an array is missing', () => {
    expect(parseTemplatePayload('{"walls":[]}')).toBeNull();
  });
});

describe('templatePayloadJson', () => {
  it('round-trips a captured template back through parseTemplatePayload', () => {
    const s = scene({
      walls: [wall({ id: 'a' })],
      runs: [run({ id: 'r1' })],
    });
    const tpl = captureSceneAsTemplate(s, 'tpl', 'Round trip');
    const parsed = parseTemplatePayload(templatePayloadJson(tpl));
    expect(parsed).not.toBeNull();
    expect(parsed!.walls).toHaveLength(1);
    expect(parsed!.runs).toHaveLength(1);
  });
});
