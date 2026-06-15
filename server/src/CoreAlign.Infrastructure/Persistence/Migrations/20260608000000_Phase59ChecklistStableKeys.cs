using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CoreAlign.Infrastructure.Persistence.Migrations
{
    public partial class Phase59ChecklistStableKeys : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
DO $$
DECLARE
    rec RECORD;
    payload jsonb;
    cat jsonb;
    item jsonb;
    new_cats jsonb := '[]'::jsonb;
    new_items jsonb;
    cat_key text;
    item_key text;
    stable_key text;
BEGIN
    FOR rec IN SELECT id, checklist_json FROM installation_acceptances WHERE checklist_json IS NOT NULL AND checklist_json <> ''
    LOOP
        BEGIN
            payload := rec.checklist_json::jsonb;
        EXCEPTION WHEN others THEN
            CONTINUE;
        END;

        new_cats := '[]'::jsonb;

        FOR cat IN SELECT * FROM jsonb_array_elements(payload)
        LOOP
            cat_key := cat->>'category';
            new_items := '[]'::jsonb;

            FOR item IN SELECT * FROM jsonb_array_elements(cat->'items')
            LOOP
                item_key := item->>'key';
                IF item_key IS NULL OR item_key = '' THEN
                    CONTINUE;
                END IF;

                IF position('.' in item_key) > 0 THEN
                    stable_key := item_key;
                ELSE
                    stable_key := cat_key || '.' || item_key;
                END IF;

                new_items := new_items || jsonb_build_object(
                    'key', stable_key,
                    'result', COALESCE(item->>'result', 'NotEvaluated'),
                    'notes', item->'notes'
                );
            END LOOP;

            new_cats := new_cats || jsonb_build_object(
                'category', cat_key,
                'items', new_items
            );
        END LOOP;

        UPDATE installation_acceptances
            SET checklist_json = new_cats::text
            WHERE id = rec.id;
    END LOOP;
END $$;
");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
        }
    }
}
