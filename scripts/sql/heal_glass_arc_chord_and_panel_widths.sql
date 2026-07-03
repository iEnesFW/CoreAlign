-- ============================================================================
-- HEAL: glass-enclosure arc runs — legacy lengthMm + panel widths (2026-07-03)
-- ============================================================================
-- Bağlam: Cam mekan (glass-enclosure) arc modeli CHORD-INVARIANT'tır:
--   length_mm  = KİRİŞ (iki sabit uç arasındaki düz açıklık) = 2·r·sin(sweep/2)
--   panel width_mm toplamı = AÇINIM uzunluğu (radius·sweep, fiziksel cam)
--
-- Bu script iki legacy bozulmayı onarır:
--   1) Eski arc-length modelinden kalan satırlar: length_mm hâlâ açınım
--      uzunluğunu tutuyor (kirişten %11-57 büyük). Kirişe indirilir.
--      ±5 mm tolerans korunur (integer radius yuvarlaması; tam girilmiş
--      kiriş değerleri ezilmez) — frontend migration'la birebir aynı kural.
--   2) Chord-share döneminde yazılmış panel genişlikleri: toplamları kirişe
--      eşit (açınıma değil). Oransal olarak açınım uzunluğuna ölçeklenir;
--      yuvarlama kalanı SON panele (en büyük panel_index) verilir — yeni
--      frontend/backend rebalance dağıtımıyla birebir aynı.
--
-- Idempotent: ikinci koşuda (Σwidth == açınım, |length_mm − kiriş| ≤ 5)
-- hiçbir satır değişmez. Tek transaction; önce SELECT'lerle etkiyi gör.
--
-- KULLANIM: psql -d corealign -f heal_glass_arc_chord_and_panel_widths.sql
-- (Önce yedek alın. Uygulama kapalıyken veya sakin saatte koşturun.)
-- ============================================================================

BEGIN;

-- ---------------------------------------------------------------------------
-- 0) ÖN-İZLEME: etkilenecek run'lar (bilgi amaçlı — çıktıyı kontrol edin)
-- ---------------------------------------------------------------------------
SELECT r.id,
       r.length_mm                                                    AS stored_length_mm,
       round(2 * r.geom_arc_radius_mm
               * sin(radians(abs(r.geom_arc_sweep_deg)) / 2))         AS derived_chord_mm,
       round(r.geom_arc_radius_mm * radians(abs(r.geom_arc_sweep_deg))) AS developed_mm,
       COALESCE((SELECT sum(p.width_mm) FROM glass_project_panels p
                  WHERE p.run_id = r.id), 0)                          AS panel_width_sum
  FROM glass_project_runs r
 WHERE r.geom_arc_radius_mm > 0
   AND abs(COALESCE(r.geom_arc_sweep_deg, 0)) >= 0.1;

-- ---------------------------------------------------------------------------
-- 1) length_mm → kiriş (yalnız ±5 mm dışına düşen legacy satırlar)
-- ---------------------------------------------------------------------------
UPDATE glass_project_runs r
   SET length_mm = round(2 * r.geom_arc_radius_mm
                           * sin(radians(abs(r.geom_arc_sweep_deg)) / 2))
 WHERE r.geom_arc_radius_mm > 0
   AND abs(COALESCE(r.geom_arc_sweep_deg, 0)) >= 0.1
   AND abs(r.length_mm
           - round(2 * r.geom_arc_radius_mm
                     * sin(radians(abs(r.geom_arc_sweep_deg)) / 2))) > 5;

-- ---------------------------------------------------------------------------
-- 2) Panel genişlikleri → açınım uzunluğuna oransal ölçekleme
--    (yalnız toplamı açınımdan 5 mm'den fazla sapan arc run'lar)
-- ---------------------------------------------------------------------------
WITH arc_runs AS (
    SELECT r.id AS run_id,
           round(r.geom_arc_radius_mm * radians(abs(r.geom_arc_sweep_deg)))::int AS developed_mm
      FROM glass_project_runs r
     WHERE r.geom_arc_radius_mm > 0
       AND abs(COALESCE(r.geom_arc_sweep_deg, 0)) >= 0.1
),
sums AS (
    SELECT p.run_id, sum(p.width_mm)::numeric AS total
      FROM glass_project_panels p
      JOIN arc_runs a ON a.run_id = p.run_id
     GROUP BY p.run_id
    HAVING sum(p.width_mm) > 0
),
targets AS (
    SELECT a.run_id, a.developed_mm, s.total
      FROM arc_runs a
      JOIN sums s ON s.run_id = a.run_id
     WHERE abs(s.total - a.developed_mm) > 5
),
scaled AS (
    SELECT p.id,
           p.run_id,
           p.panel_index,
           floor(p.width_mm * t.developed_mm / t.total)::int AS base_width,
           row_number() OVER (PARTITION BY p.run_id ORDER BY p.panel_index DESC) AS rn,
           t.developed_mm
      FROM glass_project_panels p
      JOIN targets t ON t.run_id = p.run_id
),
final_widths AS (
    SELECT s.id,
           CASE WHEN s.rn = 1
                THEN s.developed_mm
                     - COALESCE((SELECT sum(s2.base_width) FROM scaled s2
                                  WHERE s2.run_id = s.run_id AND s2.rn <> 1), 0)
                ELSE s.base_width
           END AS width_mm
      FROM scaled s
)
UPDATE glass_project_panels p
   SET width_mm = greatest(1, f.width_mm)
  FROM final_widths f
 WHERE p.id = f.id
   AND p.width_mm <> greatest(1, f.width_mm);

-- ---------------------------------------------------------------------------
-- 3) SON KONTROL: kalan sapma olmamalı (0 satır beklenir)
-- ---------------------------------------------------------------------------
SELECT r.id,
       r.length_mm,
       round(2 * r.geom_arc_radius_mm
               * sin(radians(abs(r.geom_arc_sweep_deg)) / 2))          AS chord_mm,
       (SELECT sum(p.width_mm) FROM glass_project_panels p
         WHERE p.run_id = r.id)                                        AS panel_sum,
       round(r.geom_arc_radius_mm * radians(abs(r.geom_arc_sweep_deg))) AS developed_mm
  FROM glass_project_runs r
 WHERE r.geom_arc_radius_mm > 0
   AND abs(COALESCE(r.geom_arc_sweep_deg, 0)) >= 0.1
   AND (abs(r.length_mm - round(2 * r.geom_arc_radius_mm
                                  * sin(radians(abs(r.geom_arc_sweep_deg)) / 2))) > 5
        OR abs(COALESCE((SELECT sum(p.width_mm) FROM glass_project_panels p
                          WHERE p.run_id = r.id), 0)
               - round(r.geom_arc_radius_mm * radians(abs(r.geom_arc_sweep_deg)))) > 5);

COMMIT;
