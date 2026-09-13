/**
 * Tests for POST /v1/sync (including certificate issuance — STEP 5.4)
 *
 * Uses buildApp(deps) with a mock pool — no real PostgreSQL server needed.
 * The mock pool uses a per-test query queue: each call to client.query()
 * pops the next QueryResult off the queue, making multi-step flows testable.
 *
 * Query order inside a single PASS attempt transaction:
 *   1  BEGIN
 *   2  dup-check (SELECT id FROM attempt WHERE client_attempt_id = $1 FOR UPDATE)
 *   3  worker-check (SELECT id FROM worker WHERE id = $1)
 *   4  module-check (SELECT id, content_version FROM module WHERE id = $1)
 *   5  INSERT attempt RETURNING id
 *   6  INSERT event(s)   (one slot per event)
 *   7  SELECT cert by attempt_id  (idempotency guard — empty on first run)
 *   8  UPDATE revoke existing active cert (ACK — 0 rows affected first time)
 *   9  INSERT certificate RETURNING id, public_id, issued_at
 *  10  COMMIT
 *
 * For a FAIL attempt there is no cert issuance, so steps 7-9 are absent.
 */

import assert from "node:assert/strict";
import { after, test } from "node:test";
import { buildApp } from "./app.js";
import { validateSyncRequest } from "./sync.js";
import type { Pool, PoolClient, QueryResult } from "pg";

// ---------------------------------------------------------------------------
// Deterministic test IDs
// ---------------------------------------------------------------------------
const WORKER_ID       = "aaaaaaaa-0000-4000-8000-000000000001";
const MODULE_ID       = "fire-explosion-response";
const CONTENT_VER     = "1.0.0";
const ATTEMPT_ID_1    = "bbbbbbbb-0000-4000-8000-000000000001";
const ATTEMPT_ID_2    = "bbbbbbbb-0000-4000-8000-000000000002";
const EVENT_ID_1      = "cccccccc-0000-4000-8000-000000000001";
const SERVER_ATT_ID   = "dddddddd-0000-4000-8000-000000000001";
const SERVER_ATT_ID_2 = "dddddddd-0000-4000-8000-000000000002";
const CERT_PUBLIC_ID  = "ffffffff-0000-4000-8000-000000000001";
const CERT_PUBLIC_ID_2 = "ffffffff-0000-4000-8000-000000000002";
const OLD_CERT_ID     = "eeeeeeee-0000-4000-8000-000000000001";
const ISSUED_AT       = "2026-01-01T08:30:00.000Z";

// ---------------------------------------------------------------------------
// Mock pool builder
// ---------------------------------------------------------------------------

/**
 * Builds a mock Pool backed by a query-result queue.
 * Each call to client.query() shifts the next entry.
 * Throws if the queue is exhausted — catches missing queue entries early.
 */
function makeQueuedPool(queryQueue: Array<QueryResult | Error>): Pool {
  const client: Partial<PoolClient> = {
    query: async (_sql: unknown, _params?: unknown): Promise<QueryResult> => {
      const next = queryQueue.shift();
      if (next === undefined) {
        throw new Error("Mock query queue exhausted — add more entries to queryQueue");
      }
      if (next instanceof Error) throw next;
      return next;
    },
    release: () => { /* no-op */ },
  };

  const pool: Partial<Pool> = {
    connect: async () => client as PoolClient,
    on: () => pool as Pool,
    end: async () => undefined,
    // pool.query() is used by GET /v1/modules — safe fallback for regression tests
    query: async () => ({ rows: [], rowCount: 0 }) as unknown as QueryResult,
  };

  return pool as Pool;
}

/** Shorthand: a QueryResult carrying rows */
function ok(rows: Record<string, unknown>[] = [], rowCount?: number): QueryResult {
  return { rows, rowCount: rowCount ?? rows.length, command: "", oid: 0, fields: [] };
}

/** Shorthand: empty success — used for BEGIN / COMMIT / ROLLBACK / UPDATE */
const ACK = ok();

// ---------------------------------------------------------------------------
// Queue builders
// ---------------------------------------------------------------------------

/**
 * Happy-path PASS queue (one event, fresh cert, no prior active cert).
 * Steps: BEGIN, dup-, worker-, module-check, INSERT attempt, INSERT event,
 *        SELECT cert (empty), UPDATE revoke (0 rows), INSERT cert, COMMIT
 */
function passQueue(
  certPublicId = CERT_PUBLIC_ID,
  serverAttId  = SERVER_ATT_ID,
): Array<QueryResult | Error> {
  return [
    ACK,                                                          // 1  BEGIN
    ok([]),                                                       // 2  dup-check → not found
    ok([{ id: WORKER_ID }]),                                      // 3  worker-check → found
    ok([{ id: MODULE_ID, content_version: CONTENT_VER }]),        // 4  module-check → found
    ok([{ id: serverAttId }]),                                    // 5  INSERT attempt RETURNING id
    ACK,                                                          // 6  INSERT event
    ok([]),                                                       // 7  SELECT cert by attempt_id → empty
    ACK,                                                          // 8  UPDATE revoke active cert (0 rows)
    ok([{ id: "cert-internal-id", public_id: certPublicId, issued_at: ISSUED_AT }]), // 9  INSERT cert
    ACK,                                                          // 10 COMMIT
  ];
}

/**
 * FAIL attempt queue — no cert steps.
 * Steps: BEGIN, dup-, worker-, module-check, INSERT attempt, INSERT event, COMMIT
 */
function failQueue(): Array<QueryResult | Error> {
  return [
    ACK,
    ok([]),
    ok([{ id: WORKER_ID }]),
    ok([{ id: MODULE_ID, content_version: CONTENT_VER }]),
    ok([{ id: SERVER_ATT_ID }]),
    ACK,
    ACK, // COMMIT
  ];
}

/**
 * Duplicate attempt queue — dup-check finds a row, rolls back immediately.
 */
function dupQueue(): Array<QueryResult | Error> {
  return [
    ACK,
    ok([{ id: SERVER_ATT_ID }]), // dup-check → found
    ACK,                         // ROLLBACK
  ];
}

// ---------------------------------------------------------------------------
// Canonical request helpers
// ---------------------------------------------------------------------------

function validSyncBody(overrides: Record<string, unknown> = {}): unknown {
  return {
    schema_version: "1.0.0",
    client_request_id: "eeeeeeee-0000-4000-8000-000000000001",
    worker: { worker_id: WORKER_ID },
    attempts: [
      {
        schema_version: "1.0.0",
        client_attempt_id: ATTEMPT_ID_1,
        worker_id: WORKER_ID,
        module_id: MODULE_ID,
        content_version: CONTENT_VER,
        started_at: "2026-01-01T08:00:00Z",
        completed_at: "2026-01-01T08:30:00Z",
        client_score: 80,
        passed: true,
        status: "completed",
        events: [
          {
            schema_version: "1.0.0",
            client_event_id: EVENT_ID_1,
            sequence: 1,
            event_type: "step_completed",
            occurred_at: "2026-01-01T08:05:00Z",
            step_id: "step_01",
            payload: {},
          },
        ],
      },
    ],
    ...overrides,
  };
}

/** A FAIL attempt body (passed: false, client_score: 30) */
function failSyncBody(): unknown {
  const body = validSyncBody() as Record<string, unknown>;
  const attempts = (body["attempts"] as Array<Record<string, unknown>>).map((a) => ({
    ...a,
    client_attempt_id: ATTEMPT_ID_1,
    client_score: 30,
    passed: false,
  }));
  return { ...body, attempts };
}

// ---------------------------------------------------------------------------
// App instances (built at top level — tsx top-level await)
// ---------------------------------------------------------------------------

const appNoDb = await buildApp({
  getPool: () => {
    throw new Error("DATABASE_URL is not set. Add it to your .env file (see .env.example).");
  },
});

after(async () => {
  await appNoDb.close();
});

// ---------------------------------------------------------------------------
// Unit tests — validateSyncRequest (pure, no app/pool needed)
// ---------------------------------------------------------------------------

test("validateSyncRequest: returns null for a valid body", () => {
  assert.equal(validateSyncRequest(validSyncBody()), null);
});

test("validateSyncRequest: rejects non-object body", () => {
  assert.notEqual(validateSyncRequest("string"), null);
  assert.notEqual(validateSyncRequest(null), null);
  assert.notEqual(validateSyncRequest([]), null);
});

test("validateSyncRequest: rejects wrong schema_version", () => {
  assert.notEqual(validateSyncRequest(validSyncBody({ schema_version: "2.0.0" })), null);
});

test("validateSyncRequest: rejects missing client_request_id", () => {
  const body = validSyncBody() as Record<string, unknown>;
  delete body["client_request_id"];
  assert.notEqual(validateSyncRequest(body), null);
});

test("validateSyncRequest: rejects missing worker.worker_id", () => {
  assert.notEqual(validateSyncRequest(validSyncBody({ worker: {} })), null);
});

test("validateSyncRequest: rejects empty attempts array", () => {
  assert.notEqual(validateSyncRequest(validSyncBody({ attempts: [] })), null);
});

test("validateSyncRequest: rejects attempts with status !== completed", () => {
  const body = validSyncBody() as Record<string, unknown>;
  const attempts = (body["attempts"] as Array<Record<string, unknown>>).map((a) => ({
    ...a,
    status: "in_progress",
  }));
  assert.notEqual(validateSyncRequest({ ...body, attempts }), null);
});

test("validateSyncRequest: rejects attempt with non-numeric client_score", () => {
  const body = validSyncBody() as Record<string, unknown>;
  const attempts = (body["attempts"] as Array<Record<string, unknown>>).map((a) => ({
    ...a,
    client_score: "eighty",
  }));
  assert.notEqual(validateSyncRequest({ ...body, attempts }), null);
});

test("validateSyncRequest: rejects event with non-integer sequence", () => {
  const body = validSyncBody() as Record<string, unknown>;
  const attempts = (body["attempts"] as Array<Record<string, unknown>>).map((a) => ({
    ...a,
    events: [
      { ...(a["events"] as Array<Record<string, unknown>>)[0], sequence: 1.5 },
    ],
  }));
  assert.notEqual(validateSyncRequest({ ...body, attempts }), null);
});

// ---------------------------------------------------------------------------
// Integration tests — HTTP layer (mock pool, no real DB)
// ---------------------------------------------------------------------------

test("POST /v1/sync returns 400 for missing body", async () => {
  const app = await buildApp({ getPool: () => makeQueuedPool([]) });
  after(() => app.close());

  const res = await app.inject({
    method: "POST",
    url: "/v1/sync",
    headers: { "content-type": "application/json" },
    payload: "{}",
  });
  assert.equal(res.statusCode, 400);
  assert.equal(res.json<{ error: string }>().error, "bad_request");
});

test("POST /v1/sync returns 400 with message for malformed body", async () => {
  const app = await buildApp({ getPool: () => makeQueuedPool([]) });
  after(() => app.close());

  const res = await app.inject({
    method: "POST",
    url: "/v1/sync",
    headers: { "content-type": "application/json" },
    payload: JSON.stringify({ schema_version: "1.0.0" }),
  });
  assert.equal(res.statusCode, 400);
  const body = res.json<{ error: string; message: string }>();
  assert.equal(body.error, "bad_request");
  assert.ok(body.message.length > 0);
});

test("POST /v1/sync returns 503 when DATABASE_URL is not configured", async () => {
  const res = await appNoDb.inject({
    method: "POST",
    url: "/v1/sync",
    headers: { "content-type": "application/json" },
    payload: JSON.stringify(validSyncBody()),
  });
  assert.equal(res.statusCode, 503);
  assert.equal(res.json<{ error: string }>().error, "service_unavailable");
});

test("POST /v1/sync returns 200 for a valid first submission", async () => {
  const app = await buildApp({ getPool: () => makeQueuedPool(passQueue()) });
  after(() => app.close());

  const res = await app.inject({
    method: "POST",
    url: "/v1/sync",
    headers: { "content-type": "application/json" },
    payload: JSON.stringify(validSyncBody()),
  });
  assert.equal(res.statusCode, 200);
});

test("POST /v1/sync accepted array contains the attempt on first submission", async () => {
  const app = await buildApp({ getPool: () => makeQueuedPool(passQueue()) });
  after(() => app.close());

  const res = await app.inject({
    method: "POST",
    url: "/v1/sync",
    headers: { "content-type": "application/json" },
    payload: JSON.stringify(validSyncBody()),
  });
  const body = res.json<{
    accepted: Array<{
      client_attempt_id: string;
      server_attempt_id: string;
      server_score: number;
      server_passed: boolean;
    }>;
  }>();
  assert.equal(body.accepted.length, 1);
  assert.equal(body.accepted[0]!.client_attempt_id, ATTEMPT_ID_1);
  assert.equal(body.accepted[0]!.server_attempt_id, SERVER_ATT_ID);
  assert.equal(body.accepted[0]!.server_score, 80);
  assert.equal(body.accepted[0]!.server_passed, true);
});

test("POST /v1/sync response has correct schema shape", async () => {
  const app = await buildApp({ getPool: () => makeQueuedPool(passQueue()) });
  after(() => app.close());

  const res = await app.inject({
    method: "POST",
    url: "/v1/sync",
    headers: { "content-type": "application/json" },
    payload: JSON.stringify(validSyncBody()),
  });
  const body = res.json<Record<string, unknown>>();
  assert.equal(body["schema_version"], "1.0.0");
  assert.ok(typeof body["server_time"] === "string");
  assert.ok(Array.isArray(body["accepted"]));
  assert.ok(Array.isArray(body["duplicates"]));
  assert.ok(Array.isArray(body["rejected"]));
  assert.ok(Array.isArray(body["certificates"]));
});

test("POST /v1/sync duplicate submission goes to duplicates array, not accepted", async () => {
  const app = await buildApp({ getPool: () => makeQueuedPool(dupQueue()) });
  after(() => app.close());

  const res = await app.inject({
    method: "POST",
    url: "/v1/sync",
    headers: { "content-type": "application/json" },
    payload: JSON.stringify(validSyncBody()),
  });
  assert.equal(res.statusCode, 200);
  const body = res.json<{
    accepted: unknown[];
    duplicates: Array<{ client_attempt_id: string; server_attempt_id: string }>;
    rejected: unknown[];
  }>();
  assert.equal(body.accepted.length, 0);
  assert.equal(body.duplicates.length, 1);
  assert.equal(body.duplicates[0]!.client_attempt_id, ATTEMPT_ID_1);
  assert.equal(body.duplicates[0]!.server_attempt_id, SERVER_ATT_ID);
  assert.equal(body.rejected.length, 0);
});

test("POST /v1/sync rejects attempt when worker_id mismatches request worker", async () => {
  const body = validSyncBody() as Record<string, unknown>;
  const attempts = (body["attempts"] as Array<Record<string, unknown>>).map((a) => ({
    ...a,
    worker_id: "ffffffff-0000-4000-8000-000000000099",
  }));
  const app = await buildApp({ getPool: () => makeQueuedPool([]) });
  after(() => app.close());

  const res = await app.inject({
    method: "POST",
    url: "/v1/sync",
    headers: { "content-type": "application/json" },
    payload: JSON.stringify({ ...body, attempts }),
  });
  assert.equal(res.statusCode, 200);
  const resp = res.json<{ rejected: Array<{ error_code: string }> }>();
  assert.equal(resp.rejected.length, 1);
  assert.equal(resp.rejected[0]!.error_code, "WORKER_MISMATCH");
});

test("POST /v1/sync rejects attempt with non-consecutive event sequences", async () => {
  const body = validSyncBody() as Record<string, unknown>;
  const attempts = (body["attempts"] as Array<Record<string, unknown>>).map((a) => ({
    ...a,
    events: [
      { ...(a["events"] as Array<Record<string, unknown>>)[0], sequence: 1 },
      {
        schema_version: "1.0.0",
        client_event_id: "cccccccc-0000-4000-8000-000000000002",
        sequence: 3,
        event_type: "step_completed",
        occurred_at: "2026-01-01T08:10:00Z",
        step_id: "step_03",
        payload: {},
      },
    ],
  }));
  const app = await buildApp({ getPool: () => makeQueuedPool([]) });
  after(() => app.close());

  const res = await app.inject({
    method: "POST",
    url: "/v1/sync",
    headers: { "content-type": "application/json" },
    payload: JSON.stringify({ ...body, attempts }),
  });
  assert.equal(res.statusCode, 200);
  assert.equal(
    res.json<{ rejected: Array<{ error_code: string }> }>().rejected[0]!.error_code,
    "EVENT_SEQUENCE_INVALID",
  );
});

test("POST /v1/sync rejects attempt when worker not found in DB", async () => {
  const q: Array<QueryResult | Error> = [
    ACK, ok([]), ok([]), ACK, // BEGIN, dup-check empty, worker not found, ROLLBACK
  ];
  const app = await buildApp({ getPool: () => makeQueuedPool(q) });
  after(() => app.close());

  const res = await app.inject({
    method: "POST",
    url: "/v1/sync",
    headers: { "content-type": "application/json" },
    payload: JSON.stringify(validSyncBody()),
  });
  assert.equal(res.statusCode, 200);
  assert.equal(
    res.json<{ rejected: Array<{ error_code: string }> }>().rejected[0]!.error_code,
    "UNAUTHORIZED",
  );
});

test("POST /v1/sync rejects attempt when module not found in DB", async () => {
  const q: Array<QueryResult | Error> = [
    ACK, ok([]), ok([{ id: WORKER_ID }]), ok([]), ACK,
  ];
  const app = await buildApp({ getPool: () => makeQueuedPool(q) });
  after(() => app.close());

  const res = await app.inject({
    method: "POST",
    url: "/v1/sync",
    headers: { "content-type": "application/json" },
    payload: JSON.stringify(validSyncBody()),
  });
  assert.equal(res.statusCode, 200);
  assert.equal(
    res.json<{ rejected: Array<{ error_code: string }> }>().rejected[0]!.error_code,
    "UNKNOWN_MODULE",
  );
});

test("POST /v1/sync rejects attempt when content_version does not match module", async () => {
  const q: Array<QueryResult | Error> = [
    ACK,
    ok([]),
    ok([{ id: WORKER_ID }]),
    ok([{ id: MODULE_ID, content_version: "2.0.0" }]),
    ACK,
  ];
  const app = await buildApp({ getPool: () => makeQueuedPool(q) });
  after(() => app.close());

  const res = await app.inject({
    method: "POST",
    url: "/v1/sync",
    headers: { "content-type": "application/json" },
    payload: JSON.stringify(validSyncBody()),
  });
  assert.equal(res.statusCode, 200);
  assert.equal(
    res.json<{ rejected: Array<{ error_code: string }> }>().rejected[0]!.error_code,
    "UNKNOWN_CONTENT_VERSION",
  );
});

// ---------------------------------------------------------------------------
// Certificate issuance tests (STEP 5.4)
// ---------------------------------------------------------------------------

test("PASS attempt: certificates array contains one entry", async () => {
  const app = await buildApp({ getPool: () => makeQueuedPool(passQueue()) });
  after(() => app.close());

  const res = await app.inject({
    method: "POST",
    url: "/v1/sync",
    headers: { "content-type": "application/json" },
    payload: JSON.stringify(validSyncBody()),
  });
  const body = res.json<{ certificates: unknown[] }>();
  assert.equal(body.certificates.length, 1);
});

test("PASS attempt: issued certificate has correct required fields", async () => {
  const app = await buildApp({
    getPool: () => makeQueuedPool(passQueue(CERT_PUBLIC_ID)),
    publicBaseUrl: "http://localhost:3000",
  });
  after(() => app.close());

  const res = await app.inject({
    method: "POST",
    url: "/v1/sync",
    headers: { "content-type": "application/json" },
    payload: JSON.stringify(validSyncBody()),
  });
  const { certificates } = res.json<{
    certificates: Array<{
      client_attempt_id: string;
      public_id: string;
      module_id: string;
      content_version: string;
      score: number;
      passed: boolean;
      issued_at: string;
      status: string;
      verification_url: string;
    }>;
  }>();
  const cert = certificates[0]!;
  assert.equal(cert.client_attempt_id, ATTEMPT_ID_1);
  assert.equal(cert.public_id, CERT_PUBLIC_ID);
  assert.equal(cert.module_id, MODULE_ID);
  assert.equal(cert.content_version, CONTENT_VER);
  assert.equal(cert.score, 80);
  assert.equal(cert.passed, true);
  assert.equal(cert.status, "issued");
  assert.ok(cert.issued_at, "issued_at must be set");
  assert.ok(cert.verification_url.includes(CERT_PUBLIC_ID), "verification_url must contain public_id");
  assert.ok(cert.verification_url.startsWith("http://localhost:3000"), "verification_url must use publicBaseUrl");
});

test("PASS attempt: verification_url path is /verify/<public_id>", async () => {
  const app = await buildApp({
    getPool: () => makeQueuedPool(passQueue(CERT_PUBLIC_ID)),
    publicBaseUrl: "https://example.com",
  });
  after(() => app.close());

  const res = await app.inject({
    method: "POST",
    url: "/v1/sync",
    headers: { "content-type": "application/json" },
    payload: JSON.stringify(validSyncBody()),
  });
  const cert = res.json<{ certificates: Array<{ verification_url: string }> }>().certificates[0]!;
  assert.equal(cert.verification_url, `https://example.com/verify/${CERT_PUBLIC_ID}`);
});

test("FAIL attempt: certificates array is empty", async () => {
  const app = await buildApp({ getPool: () => makeQueuedPool(failQueue()) });
  after(() => app.close());

  const res = await app.inject({
    method: "POST",
    url: "/v1/sync",
    headers: { "content-type": "application/json" },
    payload: JSON.stringify(failSyncBody()),
  });
  assert.equal(res.statusCode, 200);
  const body = res.json<{ accepted: unknown[]; certificates: unknown[] }>();
  assert.equal(body.accepted.length, 1, "FAIL attempt must still be accepted");
  assert.equal(body.certificates.length, 0, "FAIL attempt must not produce a cert");
});

test("PASS attempt: duplicate resubmission does NOT produce a second certificate", async () => {
  // Dup check finds the existing attempt row → returns to duplicates[], no cert issuance
  const app = await buildApp({ getPool: () => makeQueuedPool(dupQueue()) });
  after(() => app.close());

  const res = await app.inject({
    method: "POST",
    url: "/v1/sync",
    headers: { "content-type": "application/json" },
    payload: JSON.stringify(validSyncBody()),
  });
  assert.equal(res.statusCode, 200);
  const body = res.json<{ duplicates: unknown[]; certificates: unknown[] }>();
  assert.equal(body.duplicates.length, 1);
  assert.equal(body.certificates.length, 0);
});

test("Second PASS on same worker+module: revokes prior cert and issues new one", async () => {
  // Simulates a second attempt succeeding:
  // - dup-check finds nothing (new attempt_id)
  // - worker + module found
  // - INSERT attempt2
  // - INSERT event
  // - SELECT cert by attempt_id → empty (new attempt, no cert yet)
  // - UPDATE revoke existing active cert → 1 row affected (ACK is sufficient)
  // - INSERT new cert with CERT_PUBLIC_ID_2
  // - COMMIT
  const secondPassQueue: Array<QueryResult | Error> = [
    ACK,                                                               // BEGIN
    ok([]),                                                            // dup-check → not found
    ok([{ id: WORKER_ID }]),                                           // worker check → found
    ok([{ id: MODULE_ID, content_version: CONTENT_VER }]),             // module check → found
    ok([{ id: SERVER_ATT_ID_2 }]),                                     // INSERT attempt
    ACK,                                                               // INSERT event
    ok([]),                                                            // SELECT cert by attempt_id → empty
    ok([], 1),                                                         // UPDATE revoke existing (1 row affected)
    ok([{ id: OLD_CERT_ID, public_id: CERT_PUBLIC_ID_2, issued_at: ISSUED_AT }]), // INSERT new cert
    ACK,                                                               // COMMIT
  ];

  const syncBody = validSyncBody({
    attempts: [
      {
        schema_version: "1.0.0",
        client_attempt_id: ATTEMPT_ID_2,
        worker_id: WORKER_ID,
        module_id: MODULE_ID,
        content_version: CONTENT_VER,
        started_at: "2026-06-01T09:00:00Z",
        completed_at: "2026-06-01T09:30:00Z",
        client_score: 90,
        passed: true,
        status: "completed",
        events: [
          {
            schema_version: "1.0.0",
            client_event_id: "cccccccc-0000-4000-8000-000000000099",
            sequence: 1,
            event_type: "step_completed",
            occurred_at: "2026-06-01T09:05:00Z",
            step_id: "step_01",
            payload: {},
          },
        ],
      },
    ],
  });

  const app = await buildApp({ getPool: () => makeQueuedPool(secondPassQueue) });
  after(() => app.close());

  const res = await app.inject({
    method: "POST",
    url: "/v1/sync",
    headers: { "content-type": "application/json" },
    payload: JSON.stringify(syncBody),
  });
  assert.equal(res.statusCode, 200);
  const body = res.json<{
    accepted: Array<{ server_score: number }>;
    certificates: Array<{ public_id: string; score: number }>;
  }>();
  assert.equal(body.accepted.length, 1);
  assert.equal(body.accepted[0]!.server_score, 90);
  assert.equal(body.certificates.length, 1);
  assert.equal(body.certificates[0]!.public_id, CERT_PUBLIC_ID_2);
  assert.equal(body.certificates[0]!.score, 90);
});

test("Idempotency guard: if cert already exists for attempt_id, return it without re-inserting", async () => {
  // SELECT cert by attempt_id → returns existing cert (simulates unexpected re-entry)
  // No UPDATE or INSERT cert should follow — queue ends after the SELECT
  const idempotentQueue: Array<QueryResult | Error> = [
    ACK,
    ok([]),
    ok([{ id: WORKER_ID }]),
    ok([{ id: MODULE_ID, content_version: CONTENT_VER }]),
    ok([{ id: SERVER_ATT_ID }]),
    ACK,
    // SELECT cert by attempt_id → found (existing cert)
    ok([{ id: "cert-internal-id", public_id: CERT_PUBLIC_ID, issued_at: ISSUED_AT }]),
    // No UPDATE, no INSERT — queue ends here
    ACK, // COMMIT
  ];

  const app = await buildApp({ getPool: () => makeQueuedPool(idempotentQueue) });
  after(() => app.close());

  const res = await app.inject({
    method: "POST",
    url: "/v1/sync",
    headers: { "content-type": "application/json" },
    payload: JSON.stringify(validSyncBody()),
  });
  assert.equal(res.statusCode, 200);
  const body = res.json<{ certificates: Array<{ public_id: string }> }>();
  assert.equal(body.certificates.length, 1);
  assert.equal(body.certificates[0]!.public_id, CERT_PUBLIC_ID);
});

// ---------------------------------------------------------------------------
// Regression: existing endpoints unaffected
// ---------------------------------------------------------------------------

test("GET /health still returns 200 after cert issuance added", async () => {
  const app = await buildApp({ getPool: () => makeQueuedPool([]) });
  after(() => app.close());

  const res = await app.inject({ method: "GET", url: "/health" });
  assert.equal(res.statusCode, 200);
  assert.deepEqual(res.json(), { status: "ok" });
});
