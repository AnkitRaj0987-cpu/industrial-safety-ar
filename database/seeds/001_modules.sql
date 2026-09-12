-- =============================================================================
-- Seed: 001_modules.sql
-- Project: SIH 2026 PS 26041
--          AR-Based Vocational Training Simulator — Industrial Safety
-- Created: STEP 6B
--
-- Inserts the two MVP training modules.
-- Safe to re-run: ON CONFLICT DO NOTHING.
--
-- Run after 001_initial_schema.sql:
--   psql -U <user> -d industrial_safety_ar -f 001_modules.sql
--
-- No passwords, PINs, or secrets in this file.
-- =============================================================================

BEGIN;

INSERT INTO module (id, title_key, pass_percent, content_version)
VALUES
    (
        'fire-explosion-response',
        'module.fire_explosion_response.title',
        70.00,
        '1.0.0'
    ),
    (
        'gas-confined-space',
        'module.gas_confined_space.title',
        70.00,
        '1.0.0'
    )
ON CONFLICT (id) DO NOTHING;

COMMIT;
