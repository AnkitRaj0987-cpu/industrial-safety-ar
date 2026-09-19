/**
 * dev-seed.ts — LOCAL DEVELOPMENT & JUDGE DEMO SEED
 *
 * Inserts:
 * 1. Default modules (Fire & Explosion Response, Gas Leak & Confined Space Safety).
 * 2. Admin user (admin@safety.jharkhand.gov.in / admin123).
 * 3. Realistic Jharkhand industrial workers:
 *    - DEMO-001: Operator Ramesh Kumar (Bokaro Steel Plant)
 *    - DEMO-002: Sita Soren (Dhanbad Coalfield)
 *    - DEMO-003: Amit Hansda (Ranchi Heavy Engineering)
 * 4. Completed training attempts with realistic scores and event timelines.
 * 5. Official active certificates with public verification IDs.
 *
 * Idempotent: safe to run multiple times.
 */

import "dotenv/config";
import pg from "pg";
import { hashPin } from "./auth.js";

const ADMIN_EMAIL = "admin@safety.jharkhand.gov.in";
const ADMIN_PASSWORD_HASH = hashPin("admin123");

const MODULES = [
  { id: "fire-explosion-response", title_key: "module.fire_explosion_response.title", pass_percent: 70.00, content_version: "1.0.0" },
  { id: "gas-confined-space",      title_key: "module.gas_confined_space.title",      pass_percent: 70.00, content_version: "1.0.0" },
] as const;

const WORKERS = [
  {
    id: "00000000-dead-beef-0001-000000000001",
    worker_code: "DEMO-001",
    display_name: "Operator Ramesh Kumar",
    locale: "en",
    site_label: "Bokaro Steel Plant — Blast Furnace #4",
    pin_hash: hashPin("1234"),
  },
  {
    id: "00000000-dead-beef-0001-000000000002",
    worker_code: "DEMO-002",
    display_name: "Sita Soren",
    locale: "sat",
    site_label: "Dhanbad Underground Mine — Shaft #3",
    pin_hash: hashPin("1234"),
  },
  {
    id: "00000000-dead-beef-0001-000000000003",
    worker_code: "DEMO-003",
    display_name: "Amit Hansda",
    locale: "hi",
    site_label: "Ranchi Heavy Engineering — Unit 2",
    pin_hash: hashPin("1234"),
  },
] as const;

async function seed(): Promise<void> {
  const connectionString = process.env["DATABASE_URL"];
  if (!connectionString || connectionString.trim() === "") {
    console.warn("WARNING: DATABASE_URL is not set. dev-seed skipped.");
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
  console.log("=== dev-seed: starting (SIH 2026 Demo Data) ===\n");

  // 1. Admin User
  await pool.query(
    `INSERT INTO admin_user (id, email, password_hash, role)
     VALUES ('00000000-dead-beef-ad01-000000000001', $1, $2, 'admin')
     ON CONFLICT (email) DO UPDATE SET password_hash = $2
     RETURNING id`,
    [ADMIN_EMAIL, ADMIN_PASSWORD_HASH],
  );
  console.log(`  [admin]   ready: ${ADMIN_EMAIL} (password: admin123)`);

  // 2. Modules
  for (const mod of MODULES) {
    await pool.query(
      `INSERT INTO module (id, title_key, pass_percent, content_version)
       VALUES ($1, $2, $3, $4)
       ON CONFLICT (id) DO UPDATE SET pass_percent = $3, content_version = $4`,
      [mod.id, mod.title_key, mod.pass_percent, mod.content_version],
    );
    console.log(`  [module]  ready: ${mod.id}`);
  }

  // 3. Workers
  for (const w of WORKERS) {
    await pool.query(
      `INSERT INTO worker (id, worker_code, pin_hash, display_name, locale, site_label)
       VALUES ($1, $2, $3, $4, $5, $6)
       ON CONFLICT (id) DO UPDATE SET
         worker_code = $2,
         pin_hash = $3,
         display_name = $4,
         locale = $5,
         site_label = $6`,
      [w.id, w.worker_code, w.pin_hash, w.display_name, w.locale, w.site_label],
    );
    console.log(`  [worker]  ready: ${w.worker_code} — ${w.display_name}`);

    // Enroll in both modules
    for (const mod of MODULES) {
      await pool.query(
        `INSERT INTO enrollment (worker_id, module_id)
         VALUES ($1, $2)
         ON CONFLICT (worker_id, module_id) DO NOTHING`,
        [w.id, mod.id],
      );
    }
  }

  // 4. Realistic Demo Attempts
  const ATTEMPTS = [
    {
      id: "a0000000-dead-beef-att1-000000000001",
      client_attempt_id: "e1111111-0000-4000-8000-000000000001",
      worker_id: WORKERS[0]!.id,
      module_id: "fire-explosion-response",
      content_version: "1.0.0",
      started_at: new Date(Date.now() - 3600 * 1000 * 4).toISOString(),
      completed_at: new Date(Date.now() - 3600 * 1000 * 4 + 180 * 1000).toISOString(),
      client_score: 92.00,
      server_score: 92.00,
      passed: true,
      status: "completed",
    },
    {
      id: "a0000000-dead-beef-att2-000000000002",
      client_attempt_id: "e1111111-0000-4000-8000-000000000002",
      worker_id: WORKERS[0]!.id,
      module_id: "gas-confined-space",
      content_version: "1.0.0",
      started_at: new Date(Date.now() - 3600 * 1000 * 2).toISOString(),
      completed_at: new Date(Date.now() - 3600 * 1000 * 2 + 240 * 1000).toISOString(),
      client_score: 88.00,
      server_score: 88.00,
      passed: true,
      status: "completed",
    },
    {
      id: "a0000000-dead-beef-att3-000000000003",
      client_attempt_id: "e1111111-0000-4000-8000-000000000003",
      worker_id: WORKERS[1]!.id,
      module_id: "gas-confined-space",
      content_version: "1.0.0",
      started_at: new Date(Date.now() - 3600 * 1000 * 6).toISOString(),
      completed_at: new Date(Date.now() - 3600 * 1000 * 6 + 210 * 1000).toISOString(),
      client_score: 95.00,
      server_score: 95.00,
      passed: true,
      status: "completed",
    },
    {
      id: "a0000000-dead-beef-att4-000000000004",
      client_attempt_id: "e1111111-0000-4000-8000-000000000004",
      worker_id: WORKERS[2]!.id,
      module_id: "fire-explosion-response",
      content_version: "1.0.0",
      started_at: new Date(Date.now() - 3600 * 1000 * 8).toISOString(),
      completed_at: new Date(Date.now() - 3600 * 1000 * 8 + 150 * 1000).toISOString(),
      client_score: 64.00,
      server_score: 64.00,
      passed: false,
      status: "completed",
    },
  ];

  for (const att of ATTEMPTS) {
    await pool.query(
      `INSERT INTO attempt (id, client_attempt_id, worker_id, module_id, content_version, started_at, completed_at, client_score, server_score, passed, status, synced_at)
       VALUES ($1, $2, $3, $4, $5, $6, $7, $8, $9, $10, $11, now())
       ON CONFLICT (id) DO UPDATE SET
         client_score = $8,
         server_score = $9,
         passed = $10,
         status = $11`,
      [
        att.id,
        att.client_attempt_id,
        att.worker_id,
        att.module_id,
        att.content_version,
        att.started_at,
        att.completed_at,
        att.client_score,
        att.server_score,
        att.passed,
        att.status,
      ],
    );

    // Seed events for the attempt
    await pool.query(
      `INSERT INTO attempt_event (attempt_id, client_event_id, seq, event_type, payload, occurred_at)
       VALUES
         ($1, gen_random_uuid(), 1, 'hazard_identified', '{"hazard":"gas_leak"}', $2),
         ($1, gen_random_uuid(), 2, 'danger_zone_marked', '{"perimeter_meters":5.0}', $2),
         ($1, gen_random_uuid(), 3, 'atmospheric_test_completed', '{"o2":18.2,"lel":14.0,"h2s":22.0}', $2)
       ON CONFLICT DO NOTHING`,
      [att.id, att.started_at],
    );
  }
  console.log(`  [attempts] seeded 4 realistic training attempts with events`);

  // 5. Active Certificates for Qualifying Passes
  const CERTS = [
    {
      id: "c0000000-dead-beef-0001-000000000001",
      public_id: "c0000000-beef-0001-0000-000000000001",
      worker_id: WORKERS[0]!.id,
      module_id: "fire-explosion-response",
      attempt_id: ATTEMPTS[0]!.id,
      score: 92.00,
    },
    {
      id: "c0000000-dead-beef-0002-000000000002",
      public_id: "c0000000-beef-0002-0000-000000000002",
      worker_id: WORKERS[0]!.id,
      module_id: "gas-confined-space",
      attempt_id: ATTEMPTS[1]!.id,
      score: 88.00,
    },
    {
      id: "c0000000-dead-beef-0003-000000000003",
      public_id: "c0000000-beef-0003-0000-000000000003",
      worker_id: WORKERS[1]!.id,
      module_id: "gas-confined-space",
      attempt_id: ATTEMPTS[2]!.id,
      score: 95.00,
    },
  ];

  for (const cert of CERTS) {
    await pool.query(
      `INSERT INTO certificate (id, public_id, worker_id, module_id, attempt_id, score, status, signature, issued_at)
       VALUES ($1, $2, $3, $4, $5, $6, 'active', 'hmac_sha256_mock_signature', now())
       ON CONFLICT (public_id) DO NOTHING`,
      [cert.id, cert.public_id, cert.worker_id, cert.module_id, cert.attempt_id, cert.score],
    );
  }
  console.log(`  [certs]    seeded 3 active certificates with QR verification public IDs`);

  console.log("\n=== dev-seed: finished successfully ===");
}

await seed();
