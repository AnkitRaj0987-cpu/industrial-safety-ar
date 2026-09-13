/**
 * dev-test-sync.ts — LOCAL DEVELOPMENT ONLY
 *
 * End-to-end certificate issuance smoke test.
 *
 * Calls processSync() and lookupCertificate() DIRECTLY — the same functions
 * used by POST /v1/sync and GET /v1/certificates/verify/:publicId — against
 * a real local PostgreSQL database.  No HTTP server is started.
 *
 * Prerequisites (run once first):
 *   npm run seed:dev           ← ensures DEMO-001 worker + modules exist
 *
 * Usage:
 *   npm run test:sync
 *
 * What it does:
 *   1. Builds a valid completed PASS attempt for DEMO-001 / fire-explosion-response
 *   2. Calls processSync() through the existing sync business logic
 *   3. Asserts the attempt was accepted and a certificate was issued
 *   4. Calls lookupCertificate() to confirm the cert is retrievable as 'valid'
 *   5. Prints the full certificate verification payload
 *
 * Running it a second time is safe: the same attempt UUID is detected as a
 * duplicate by the sync dedup logic and lands in duplicates[].
 * To re-test issuance, run with a fresh attempt UUID each time (the script
 * generates a new UUID automatically via crypto.randomUUID()).
 *
 * DO NOT run in staging or production.
 */

import "dotenv/config";
import { randomUUID } from "node:crypto";
import pg from "pg";
import { processSync } from "./sync.js";
import { lookupCertificate } from "./cert-verify.js";
import type { SyncRequest } from "./sync.js";

// ---------------------------------------------------------------------------
// Constants — must match dev-seed.ts
// ---------------------------------------------------------------------------
const DEMO_WORKER_ID    = "00000000-dead-beef-0001-000000000001";
const MODULE_ID         = "fire-explosion-response";
const CONTENT_VERSION   = "1.0.0";
const PUBLIC_BASE_URL   = process.env["PUBLIC_BASE_URL"] ?? "http://localhost:3000";

// ---------------------------------------------------------------------------
// Main
// ---------------------------------------------------------------------------

async function main(): Promise<void> {
  const connectionString = process.env["DATABASE_URL"];
  if (!connectionString || connectionString.trim() === "") {
    console.error(
      "ERROR: DATABASE_URL is not set.\n" +
      "Copy backend/.env.example to backend/.env and fill in your local credentials,\n" +
      "then run:  npm run seed:dev  before running  npm run test:sync",
    );
    process.exitCode = 1;
    return;
  }

  const pool = new pg.Pool({ connectionString, max: 2 });

  try {
    await run(pool);
  } finally {
    await pool.end();
  }
}

async function run(pool: pg.Pool): Promise<void> {
  console.log("=== dev-test-sync: certificate issuance smoke test (LOCAL ONLY) ===\n");

  // -----------------------------------------------------------------------
  // 1. Build a valid completed PASS attempt
  //    Fresh UUIDs every run so this is always treated as a new attempt.
  //    (Re-running is safe — second run lands in duplicates[], not accepted.)
  // -----------------------------------------------------------------------
  const now         = new Date();
  const startedAt   = new Date(now.getTime() - 35 * 60 * 1000).toISOString();
  const completedAt = new Date(now.getTime() - 5  * 60 * 1000).toISOString();
  const occurredAt  = new Date(now.getTime() - 30 * 60 * 1000).toISOString();

  const clientAttemptId = randomUUID();
  const clientRequestId = randomUUID();
  const clientEventId   = randomUUID();

  const syncRequest: SyncRequest = {
    schema_version: "1.0.0",
    client_request_id: clientRequestId,
    worker: { worker_id: DEMO_WORKER_ID },
    attempts: [
      {
        schema_version: "1.0.0",
        client_attempt_id: clientAttemptId,
        worker_id: DEMO_WORKER_ID,
        module_id: MODULE_ID,
        content_version: CONTENT_VERSION,
        started_at: startedAt,
        completed_at: completedAt,
        client_score: 82,
        passed: true,
        status: "completed",
        events: [
          {
            schema_version: "1.0.0",
            client_event_id: clientEventId,
            sequence: 1,
            event_type: "step_completed",
            occurred_at: occurredAt,
            step_id: "step_01",
            payload: { outcome: "success" },
          },
        ],
      },
    ],
  };

  console.log(`  attempt id : ${clientAttemptId}`);
  console.log(`  worker_id  : ${DEMO_WORKER_ID}`);
  console.log(`  module_id  : ${MODULE_ID}`);
  console.log(`  score      : 82  (passed: true)\n`);

  // -----------------------------------------------------------------------
  // 2. Call processSync() — the same function used by POST /v1/sync
  // -----------------------------------------------------------------------
  const syncResponse = await processSync(syncRequest, {
    getPool: () => pool,
    publicBaseUrl: PUBLIC_BASE_URL,
  });

  console.log("--- sync response ---");
  console.log(JSON.stringify(syncResponse, null, 2));
  console.log();

  // -----------------------------------------------------------------------
  // 3. Assert outcome
  // -----------------------------------------------------------------------
  const { accepted, duplicates, rejected, certificates } = syncResponse;

  if (rejected.length > 0) {
    console.error("FAIL: attempt was rejected.");
    console.error("  error_code:", rejected[0]!.error_code);
    console.error("  message   :", rejected[0]!.message);
    console.error("\nDid you run  npm run seed:dev  first?");
    process.exitCode = 1;
    return;
  }

  if (duplicates.length > 0) {
    // Duplicate means we already ran this exact attempt before — safe, not an error.
    console.log("NOTE: attempt was already synced (duplicate). No new certificate issued.");
    console.log("      This is expected if you re-run the script without restarting.");
    console.log("      The script always generates a fresh attempt UUID, so this should");
    console.log("      only happen if a UUID collision occurred (effectively impossible).");
    return;
  }

  if (accepted.length === 0) {
    console.error("FAIL: no accepted items in sync response.");
    process.exitCode = 1;
    return;
  }

  console.log(`OK: attempt accepted as server_attempt_id=${accepted[0]!.server_attempt_id}`);
  console.log(`    server_score=${accepted[0]!.server_score}  server_passed=${accepted[0]!.server_passed}`);

  if (certificates.length === 0) {
    console.error("FAIL: no certificate was issued. (passed=true but certificates[] is empty)");
    process.exitCode = 1;
    return;
  }

  const cert = certificates[0]!;
  console.log(`\nOK: certificate issued`);
  console.log(`    public_id        : ${cert.public_id}`);
  console.log(`    status           : ${cert.status}`);
  console.log(`    issued_at        : ${cert.issued_at}`);
  console.log(`    verification_url : ${cert.verification_url}`);

  // -----------------------------------------------------------------------
  // 4. Verify via lookupCertificate() — same path as GET /v1/certificates/verify/:publicId
  // -----------------------------------------------------------------------
  console.log("\n--- verifying via lookupCertificate() ---");
  const verifyResponse = await lookupCertificate(cert.public_id, pool, PUBLIC_BASE_URL);

  if (verifyResponse.status === "not_found") {
    console.error(`FAIL: certificate ${cert.public_id} not found via lookupCertificate().`);
    process.exitCode = 1;
    return;
  }

  if (verifyResponse.status !== "valid") {
    console.error(`FAIL: expected status 'valid', got '${verifyResponse.status}'.`);
    process.exitCode = 1;
    return;
  }

  console.log(JSON.stringify(verifyResponse, null, 2));
  console.log();
  console.log("=== PASS: certificate issued and verified successfully ===");
  console.log(`\nTo verify via HTTP once the server is running:`);
  console.log(`  curl http://localhost:3000/v1/certificates/verify/${cert.public_id}`);
}

await main();
