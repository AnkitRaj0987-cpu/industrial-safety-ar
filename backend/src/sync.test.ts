/**
 * Tests for POST /v1/sync
 *
 * Uses buildApp(deps) with a mock pool — no real PostgreSQL server needed.
 * The mock pool uses a per-test query queue: each call to client.query()
 * pops the next QueryResult off the queue, making multi-step flows testable.
 */

import assert from "node:assert/strict";
import { after, test } from "node:test";
import { buildApp } from "./app.js";
import { validateSyncRequest } from "./sync.js";
import type { Pool, PoolClient, QueryResult } from "pg";

// ---------------------------------------------------------------------------
// UUID helpers — deterministic IDs for test data
// ---------------------------------------------------------------------------
const WORKER_ID      = "aaaaaaaa-0000-4000-8000-000000000001";
const MODULE_ID      = "fire-explosion-response";
const CONTENT_VER    = "1.0.0";
const ATTEMPT_ID_1   = "bbbbbbbb-0000-4000-8000-000000000001";
const EVENT_ID_1     = "cccccccc-0000-4000-8000-000000000001";
const SERVER_ATT_ID  = "dddddddd-0000-4000-8000-000000000001";

// ---------------------------------------------------------------------------
// Mock pool builder
// ---------------------------------------------------------------------------

/**
 * Builds a mock Pool that uses a queue of QueryResult values.
 * Each call to client.query() shifts one result from the queue.
 * If the queue is exhausted an error is thrown (catches test bugs early).
 *
 * BEGIN / COMMIT / ROLLBACK calls consume a slot; pass { rows: [], rowCount: 0 }
 * for those in the queue.
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
    // Direct pool.query is used by GET /v1/modules — not needed here but
    // include a fallback so the same app instance works for regression tests.
    query: async () => ({ rows: [], rowCount: 0 }) as unknown as QueryResult,
  };

  return pool as Pool;
}

/** Shorthand: an ok row result */
function ok(rows: Record<string, unknown>[] = [], rowCount?: number): QueryResult {
  return { rows, rowCount: rowCount ?? rows.length, command: "", oid: 0, fields: [] };
}

/** Shorthand: empty ok (BEGIN / COMMIT / ROLLBACK) */
const ACK = ok();

// ---------------------------------------------------------------------------
// Canonical valid sync request body
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

/**
 * Queue for a single successful attempt:
 * BEGIN, dup-check (empty), worker-check (found), module-check (found),
 * insert-attempt (returns server id), insert-event, COMMIT
 */
function successQueue(): Array<QueryResult | Error> {
  return [
    ACK,                                              // BEGIN
    ok([]),                                           // dup check → not found
    ok([{ id: WORKER_ID }]),                          // worker check → found
    ok([{ id: MODULE_ID, content_version: CONTENT_VER }]), // module check → found
    ok([{ id: SERVER_ATT_ID }]),                      // INSERT attempt RETURNING id
    ACK,                                              // INSERT event
    ACK,                                              // COMMIT
  ];
}

// ---------------------------------------------------------------------------
// App instance — shared across sync tests (pool is injected per-request via
// the route handler, so the same app works for all mock pools via AppDeps)
// ---------------------------------------------------------------------------

// We build separate app instances per scenario so each has its own pool wired in.
// All are built at top level so tsx handles top-level await.

const appNoDb = await buildApp({
  getPool: () => { throw new Error("DATABASE_URL is not set. Add it to your .env file (see .env.example)."); },
});

after(async () => {
  await appNoDb.close();
});

// ---------------------------------------------------------------------------
// Unit tests for validateSyncRequest (pure, no app/pool needed)
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
  assert.notEqual(
    validateSyncRequest(validSyncBody({ worker: {} })),
    null,
  );
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
    events: [{ ...(a["events"] as Array<Record<string, unknown>>)[0], sequence: 1.5 }],
  }));
  assert.notEqual(validateSyncRequest({ ...body, attempts }), null);
});

// ---------------------------------------------------------------------------
// Integration tests via app.inject (mock pool, no real DB)
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
  const body = res.json<{ error: string }>();
  assert.equal(body.error, "bad_request");
});

test("POST /v1/sync returns 400 with message for malformed body", async () => {
  const app = await buildApp({ getPool: () => makeQueuedPool([]) });
  after(() => app.close());

  const res = await app.inject({
    method: "POST",
    url: "/v1/sync",
    headers: { "content-type": "application/json" },
    payload: JSON.stringify({ schema_version: "1.0.0" }), // missing required fields
  });
  assert.equal(res.statusCode, 400);
  const body = res.json<{ error: string; message: string }>();
  assert.equal(body.error, "bad_request");
  assert.ok(body.message.length > 0, "message must explain what is wrong");
});

test("POST /v1/sync returns 503 when DATABASE_URL is not configured", async () => {
  const res = await appNoDb.inject({
    method: "POST",
    url: "/v1/sync",
    headers: { "content-type": "application/json" },
    payload: JSON.stringify(validSyncBody()),
  });
  assert.equal(res.statusCode, 503);
  const body = res.json<{ error: string }>();
  assert.equal(body.error, "service_unavailable");
});

test("POST /v1/sync returns 200 for a valid first submission", async () => {
  const app = await buildApp({ getPool: () => makeQueuedPool(successQueue()) });
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
  const app = await buildApp({ getPool: () => makeQueuedPool(successQueue()) });
  after(() => app.close());

  const res = await app.inject({
    method: "POST",
    url: "/v1/sync",
    headers: { "content-type": "application/json" },
    payload: JSON.stringify(validSyncBody()),
  });
  const body = res.json<{ accepted: Array<{ client_attempt_id: string; server_attempt_id: string; server_score: number; server_passed: boolean }> }>();
  assert.equal(body.accepted.length, 1);
  assert.equal(body.accepted[0]!.client_attempt_id, ATTEMPT_ID_1);
  assert.equal(body.accepted[0]!.server_attempt_id, SERVER_ATT_ID);
  assert.equal(body.accepted[0]!.server_score, 80);
  assert.equal(body.accepted[0]!.server_passed, true);
});

test("POST /v1/sync response has correct schema shape", async () => {
  const app = await buildApp({ getPool: () => makeQueuedPool(successQueue()) });
  after(() => app.close());

  const res = await app.inject({
    method: "POST",
    url: "/v1/sync",
    headers: { "content-type": "application/json" },
    payload: JSON.stringify(validSyncBody()),
  });
  const body = res.json<Record<string, unknown>>();
  assert.equal(body["schema_version"], "1.0.0");
  assert.ok(typeof body["server_time"] === "string", "server_time must be a string");
  assert.ok(Array.isArray(body["accepted"]),     "accepted must be an array");
  assert.ok(Array.isArray(body["duplicates"]),   "duplicates must be an array");
  assert.ok(Array.isArray(body["rejected"]),     "rejected must be an array");
  assert.ok(Array.isArray(body["certificates"]), "certificates must be an array");
});

test("POST /v1/sync duplicate submission goes to duplicates array, not accepted", async () => {
  // Dup check returns an existing row → route to duplicates, no further DB work
  const dupQueue: Array<QueryResult | Error> = [
    ACK,                                              // BEGIN
    ok([{ id: SERVER_ATT_ID }]),                      // dup check → FOUND
    ACK,                                              // ROLLBACK
  ];
  const app = await buildApp({ getPool: () => makeQueuedPool(dupQueue) });
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
    worker_id: "ffffffff-0000-4000-8000-000000000099", // different from request worker
  }));
  const app = await buildApp({ getPool: () => makeQueuedPool([]) }); // no DB calls expected
  after(() => app.close());

  const res = await app.inject({
    method: "POST",
    url: "/v1/sync",
    headers: { "content-type": "application/json" },
    payload: JSON.stringify({ ...body, attempts }),
  });
  assert.equal(res.statusCode, 200);
  const resp = res.json<{
    accepted: unknown[];
    rejected: Array<{ client_attempt_id: string; error_code: string }>;
  }>();
  assert.equal(resp.accepted.length, 0);
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
        sequence: 3, // gap: missing 2
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
  const resp = res.json<{
    rejected: Array<{ error_code: string }>;
  }>();
  assert.equal(resp.rejected.length, 1);
  assert.equal(resp.rejected[0]!.error_code, "EVENT_SEQUENCE_INVALID");
});

test("POST /v1/sync rejects attempt when worker not found in DB", async () => {
  const workerNotFoundQueue: Array<QueryResult | Error> = [
    ACK,       // BEGIN
    ok([]),    // dup check → not found
    ok([]),    // worker check → NOT found
    ACK,       // ROLLBACK
  ];
  const app = await buildApp({ getPool: () => makeQueuedPool(workerNotFoundQueue) });
  after(() => app.close());

  const res = await app.inject({
    method: "POST",
    url: "/v1/sync",
    headers: { "content-type": "application/json" },
    payload: JSON.stringify(validSyncBody()),
  });
  assert.equal(res.statusCode, 200);
  const resp = res.json<{ rejected: Array<{ error_code: string }> }>();
  assert.equal(resp.rejected.length, 1);
  assert.equal(resp.rejected[0]!.error_code, "UNAUTHORIZED");
});

test("POST /v1/sync rejects attempt when module not found in DB", async () => {
  const moduleNotFoundQueue: Array<QueryResult | Error> = [
    ACK,                          // BEGIN
    ok([]),                       // dup check → not found
    ok([{ id: WORKER_ID }]),      // worker check → found
    ok([]),                       // module check → NOT found
    ACK,                          // ROLLBACK
  ];
  const app = await buildApp({ getPool: () => makeQueuedPool(moduleNotFoundQueue) });
  after(() => app.close());

  const res = await app.inject({
    method: "POST",
    url: "/v1/sync",
    headers: { "content-type": "application/json" },
    payload: JSON.stringify(validSyncBody()),
  });
  assert.equal(res.statusCode, 200);
  const resp = res.json<{ rejected: Array<{ error_code: string }> }>();
  assert.equal(resp.rejected.length, 1);
  assert.equal(resp.rejected[0]!.error_code, "UNKNOWN_MODULE");
});

test("POST /v1/sync rejects attempt when content_version does not match module", async () => {
  const wrongVersionQueue: Array<QueryResult | Error> = [
    ACK,
    ok([]),
    ok([{ id: WORKER_ID }]),
    ok([{ id: MODULE_ID, content_version: "2.0.0" }]), // module exists but different version
    ACK,
  ];
  const app = await buildApp({ getPool: () => makeQueuedPool(wrongVersionQueue) });
  after(() => app.close());

  const res = await app.inject({
    method: "POST",
    url: "/v1/sync",
    headers: { "content-type": "application/json" },
    payload: JSON.stringify(validSyncBody()),
  });
  assert.equal(res.statusCode, 200);
  const resp = res.json<{ rejected: Array<{ error_code: string }> }>();
  assert.equal(resp.rejected.length, 1);
  assert.equal(resp.rejected[0]!.error_code, "UNKNOWN_CONTENT_VERSION");
});

test("POST /v1/sync: certificates array is empty (issuance not implemented yet)", async () => {
  const app = await buildApp({ getPool: () => makeQueuedPool(successQueue()) });
  after(() => app.close());

  const res = await app.inject({
    method: "POST",
    url: "/v1/sync",
    headers: { "content-type": "application/json" },
    payload: JSON.stringify(validSyncBody()),
  });
  const body = res.json<{ certificates: unknown[] }>();
  assert.deepEqual(body.certificates, []);
});

// ---------------------------------------------------------------------------
// Regression: existing endpoints must still work
// ---------------------------------------------------------------------------

test("GET /health still returns 200 after sync route added", async () => {
  const app = await buildApp({ getPool: () => makeQueuedPool([]) });
  after(() => app.close());

  const res = await app.inject({ method: "GET", url: "/health" });
  assert.equal(res.statusCode, 200);
  assert.deepEqual(res.json(), { status: "ok" });
});
