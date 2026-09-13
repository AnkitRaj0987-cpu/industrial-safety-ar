/**
 * dev-seed.ts — LOCAL DEVELOPMENT ONLY
 *
 * Inserts the minimum records needed to exercise POST /v1/sync end-to-end
 * against a real local PostgreSQL database.
 *
 * DO NOT run this script in staging or production.
 * It creates a well-known demo worker identity that is not suitable for
 * any real deployment.
 *
 * Usage:
 *   npm run seed:dev
 *
 * The script is idempotent: running it multiple times will not create
 * duplicate records.
 *
 * After seeding, use the DEMO_WORKER_ID printed below as the worker_id
 * in POST /v1/sync request payloads for manual end-to-end testing.
 */

import "dotenv/config";
import pg from "pg";

// ---------------------------------------------------------------------------
// Demo identity constants
// These are fixed, well-known development values.
// DEMO_WORKER_ID is stable so you can hard-code it in manual test payloads.
// ---------------------------------------------------------------------------

const DEMO_WORKER_ID   = "00000000-dead-beef-0001-000000000001";
const DEMO_WORKER_CODE = "DEMO-001";
const DEMO_DISPLAY_NAME = "Demo Worker";
const DEMO_LOCALE      = "en";
// A bcrypt-shaped placeholder string — NOT a real hashed password/PIN.
// The sync flow only checks worker existence (SELECT id), never reads pin_hash.
// Replace with a real bcrypt hash if you add PIN-based auth in future steps.
const DEMO_PIN_HASH    = "$2b$10$demo_pin_hash_placeholder_not_a_real_hash_xxxxxxxxxxx";

// MVP modules inserted by 001_modules.sql — reused here, not invented.
const MODULES = [
  { id: "fire-explosion-response", title_key: "module.fire_explosion_response.title", pass_percent: 70.00, content_version: "1.0.0" },
  { id: "gas-confined-space",      title_key: "module.gas_confined_space.title",      pass_percent: 70.00, content_version: "1.0.0" },
] as const;

// ---------------------------------------------------------------------------
// Main
// ---------------------------------------------------------------------------

async function seed(): Promise<void> {
  const connectionString = process.env["DATABASE_URL"];
  if (!connectionString || connectionString.trim() === "") {
    console.error("ERROR: DATABASE_URL is not set. Copy .env.example to .env and fill in your local credentials.");
    process.exitCode = 1;
    return;
  }

  const pool = new pg.Pool({ connectionString, max: 1 });

  try {
    await run(pool);
  } finally {
    await pool.end();
  }
}

async function run(pool: pg.Pool): Promise<void> {
  console.log("=== dev-seed: starting (LOCAL DEVELOPMENT ONLY) ===\n");

  // ------------------------------------------------------------------
  // 1. Ensure the two MVP modules exist (same values as 001_modules.sql)
  // ------------------------------------------------------------------
  for (const mod of MODULES) {
    const result = await pool.query(
      `INSERT INTO module (id, title_key, pass_percent, content_version)
       VALUES ($1, $2, $3, $4)
       ON CONFLICT (id) DO NOTHING
       RETURNING id`,
      [mod.id, mod.title_key, mod.pass_percent, mod.content_version],
    );
    if (result.rowCount && result.rowCount > 0) {
      console.log(`  [module] inserted: ${mod.id}`);
    } else {
      console.log(`  [module] already exists: ${mod.id}`);
    }
  }

  // ------------------------------------------------------------------
  // 2. Ensure the demo worker exists
  // ------------------------------------------------------------------
  const workerInsert = await pool.query(
    `INSERT INTO worker (id, worker_code, pin_hash, display_name, locale)
     VALUES ($1, $2, $3, $4, $5)
     ON CONFLICT (id) DO NOTHING
     RETURNING id`,
    [DEMO_WORKER_ID, DEMO_WORKER_CODE, DEMO_PIN_HASH, DEMO_DISPLAY_NAME, DEMO_LOCALE],
  );

  // worker_code has a separate unique index; if the UUID already exists the
  // ON CONFLICT above handles it.  If only the worker_code conflicts (e.g. a
  // different UUID was used previously), report clearly rather than silently failing.
  if (workerInsert.rowCount && workerInsert.rowCount > 0) {
    console.log(`  [worker]  inserted: ${DEMO_WORKER_ID} (${DEMO_WORKER_CODE})`);
  } else {
    // Row exists — verify it really is our demo worker
    const existing = await pool.query<{ id: string; worker_code: string }>(
      `SELECT id, worker_code FROM worker WHERE id = $1`,
      [DEMO_WORKER_ID],
    );
    if (existing.rows.length > 0) {
      console.log(`  [worker]  already exists: ${DEMO_WORKER_ID} (${existing.rows[0]!.worker_code})`);
    } else {
      // UUID is absent but worker_code conflict prevented insert — mismatched state
      console.warn(
        `  [worker]  WARNING: worker_code "${DEMO_WORKER_CODE}" is already taken by a different worker. ` +
        `Run 'DELETE FROM worker WHERE worker_code = \'${DEMO_WORKER_CODE}\'' then re-run seed:dev.`,
      );
    }
  }

  // ------------------------------------------------------------------
  // 3. Ensure the demo worker is enrolled in both MVP modules
  // ------------------------------------------------------------------
  for (const mod of MODULES) {
    const enroll = await pool.query(
      `INSERT INTO enrollment (worker_id, module_id)
       VALUES ($1, $2)
       ON CONFLICT (worker_id, module_id) DO NOTHING
       RETURNING worker_id`,
      [DEMO_WORKER_ID, mod.id],
    );
    if (enroll.rowCount && enroll.rowCount > 0) {
      console.log(`  [enroll]  ${DEMO_WORKER_CODE} enrolled in ${mod.id}`);
    } else {
      console.log(`  [enroll]  already enrolled: ${DEMO_WORKER_CODE} → ${mod.id}`);
    }
  }

  // ------------------------------------------------------------------
  // 4. Summary — print the values needed for manual sync testing
  // ------------------------------------------------------------------
  console.log("\n=== dev-seed: done ===\n");
  console.log("Use these values in POST /v1/sync test payloads:");
  console.log(`  worker_id       : ${DEMO_WORKER_ID}`);
  console.log(`  module_id       : fire-explosion-response  (or gas-confined-space)`);
  console.log(`  content_version : 1.0.0`);
  console.log("\nExample minimal sync payload (generate fresh UUIDs for each attempt/event):\n");
  console.log(JSON.stringify({
    schema_version: "1.0.0",
    client_request_id: "<fresh-uuid>",
    worker: { worker_id: DEMO_WORKER_ID },
    attempts: [{
      schema_version: "1.0.0",
      client_attempt_id: "<fresh-uuid>",
      worker_id: DEMO_WORKER_ID,
      module_id: "fire-explosion-response",
      content_version: "1.0.0",
      started_at: new Date(Date.now() - 30 * 60 * 1000).toISOString(),
      completed_at: new Date().toISOString(),
      client_score: 80,
      passed: true,
      status: "completed",
      events: [{
        schema_version: "1.0.0",
        client_event_id: "<fresh-uuid>",
        sequence: 1,
        event_type: "step_completed",
        occurred_at: new Date(Date.now() - 25 * 60 * 1000).toISOString(),
        step_id: "step_01",
        payload: {},
      }],
    }],
  }, null, 2));
}

await seed();
