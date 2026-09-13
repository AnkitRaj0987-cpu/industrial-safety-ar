/**
 * Tests for GET /v1/certificates/verify/:publicId
 *
 * Uses buildApp(deps) with a mock pool — no real PostgreSQL server needed.
 * The cert-verify route uses pool.query() directly (not pool.connect()),
 * so the mock only needs to implement query().
 *
 * Follows the same node:test + app.inject() pattern as modules.test.ts.
 */

import assert from "node:assert/strict";
import { after, test } from "node:test";
import { buildApp } from "./app.js";
import type { Pool, QueryResult } from "pg";

// ---------------------------------------------------------------------------
// Deterministic test data
// ---------------------------------------------------------------------------
const PUBLIC_ID       = "ffffffff-1111-4000-8000-000000000001";
const UNKNOWN_ID      = "00000000-0000-4000-8000-000000000000";
const REVOKED_ID      = "ffffffff-2222-4000-8000-000000000002";

// A realistic DB row returned by the JOIN query for an active certificate.
// Note: score is a string (pg returns NUMERIC as string), issued_at is a Date.
const ACTIVE_ROW = {
  public_id:            PUBLIC_ID,
  score:                "85.00",
  status:               "active",
  issued_at:            new Date("2026-01-15T10:00:00.000Z"),
  revoked_at:           null,
  worker_display_name:  "Ravi Kumar",
  module_id:            "fire-explosion-response",
  module_title_key:     "module.fire_explosion_response.title",
  content_version:      "1.0.0",
};

const REVOKED_ROW = {
  ...ACTIVE_ROW,
  public_id:  REVOKED_ID,
  status:     "revoked",
  revoked_at: new Date("2026-03-01T09:00:00.000Z"),
};

// ---------------------------------------------------------------------------
// Mock pool helpers
// ---------------------------------------------------------------------------

/**
 * Creates a mock Pool whose query() always returns the given rows.
 * Matches the interface surface used by lookupCertificate (pool.query only).
 */
function mockPool(rows: Record<string, unknown>[]): Pool {
  return {
    query: async () =>
      ({ rows, rowCount: rows.length, command: "", oid: 0, fields: [] }) as unknown as QueryResult,
    on: () => { /* no-op */ },
    end: async () => undefined,
  } as unknown as Pool;
}

function failingPool(): Pool {
  return {
    query: async () => { throw new Error("simulated query failure"); },
    on: () => { /* no-op */ },
    end: async () => undefined,
  } as unknown as Pool;
}

function noUrlPool(): Pool {
  throw new Error("DATABASE_URL is not set. Add it to your .env file (see .env.example).");
}

// ---------------------------------------------------------------------------
// App instances (top-level await — tsx handles this)
// ---------------------------------------------------------------------------

const appActive  = await buildApp({ getPool: () => mockPool([ACTIVE_ROW]),  publicBaseUrl: "https://example.com" });
const appRevoked = await buildApp({ getPool: () => mockPool([REVOKED_ROW]), publicBaseUrl: "https://example.com" });
const appEmpty   = await buildApp({ getPool: () => mockPool([]) });
const appDbFail  = await buildApp({ getPool: () => failingPool() });
const appNoDb    = await buildApp({ getPool: noUrlPool });

after(async () => {
  await Promise.all([
    appActive.close(),
    appRevoked.close(),
    appEmpty.close(),
    appDbFail.close(),
    appNoDb.close(),
  ]);
});

// ---------------------------------------------------------------------------
// Active (valid) certificate
// ---------------------------------------------------------------------------

test("GET /v1/certificates/verify/:publicId returns 200 for an active certificate", async () => {
  const res = await appActive.inject({
    method: "GET",
    url: `/v1/certificates/verify/${PUBLIC_ID}`,
  });
  assert.equal(res.statusCode, 200);
});

test("active certificate response has status 'valid'", async () => {
  const res = await appActive.inject({ method: "GET", url: `/v1/certificates/verify/${PUBLIC_ID}` });
  const body = res.json<{ status: string }>();
  assert.equal(body.status, "valid");
});

test("active certificate response contains all required public fields", async () => {
  const res = await appActive.inject({ method: "GET", url: `/v1/certificates/verify/${PUBLIC_ID}` });
  const body = res.json<Record<string, unknown>>();
  assert.equal(body["schema_version"], "1.0.0");
  assert.equal(body["status"], "valid");
  assert.equal(body["public_id"], PUBLIC_ID);
  assert.ok(typeof body["worker_display_name"] === "string" && body["worker_display_name"].length > 0);
  assert.equal(body["module_id"], "fire-explosion-response");
  assert.ok(typeof body["module_title_key"] === "string");
  assert.ok(typeof body["content_version"] === "string");
  assert.equal(typeof body["score"], "number");
  assert.equal(body["passed"], true);
  assert.ok(typeof body["issued_at"] === "string");
  assert.ok(body["verification"] !== undefined);
});

test("active certificate score is a number (not a string)", async () => {
  const res = await appActive.inject({ method: "GET", url: `/v1/certificates/verify/${PUBLIC_ID}` });
  const body = res.json<{ score: unknown }>();
  assert.equal(typeof body.score, "number");
  assert.equal(body.score, 85);
});

test("active certificate verification object contains verification_url and verified_at", async () => {
  const res = await appActive.inject({ method: "GET", url: `/v1/certificates/verify/${PUBLIC_ID}` });
  const body = res.json<{ verification: { verification_url: string; verified_at: string } }>();
  assert.ok(body.verification.verification_url.includes(PUBLIC_ID));
  assert.ok(body.verification.verification_url.startsWith("https://example.com"));
  assert.ok(typeof body.verification.verified_at === "string" && body.verification.verified_at.length > 0);
});

test("active certificate does NOT contain revoked_at", async () => {
  const res = await appActive.inject({ method: "GET", url: `/v1/certificates/verify/${PUBLIC_ID}` });
  const body = res.json<Record<string, unknown>>();
  assert.equal(body["revoked_at"], undefined);
});

// ---------------------------------------------------------------------------
// Internal fields must never appear in the response
// ---------------------------------------------------------------------------

test("response does not contain internal database id", async () => {
  const res = await appActive.inject({ method: "GET", url: `/v1/certificates/verify/${PUBLIC_ID}` });
  const body = res.json<Record<string, unknown>>();
  // 'id' is the internal primary key — must be absent
  assert.equal(body["id"], undefined);
});

test("response does not contain worker_id", async () => {
  const res = await appActive.inject({ method: "GET", url: `/v1/certificates/verify/${PUBLIC_ID}` });
  const body = res.json<Record<string, unknown>>();
  assert.equal(body["worker_id"], undefined);
});

test("response does not contain attempt_id", async () => {
  const res = await appActive.inject({ method: "GET", url: `/v1/certificates/verify/${PUBLIC_ID}` });
  const body = res.json<Record<string, unknown>>();
  assert.equal(body["attempt_id"], undefined);
});

test("response does not contain signature", async () => {
  const res = await appActive.inject({ method: "GET", url: `/v1/certificates/verify/${PUBLIC_ID}` });
  const body = res.json<Record<string, unknown>>();
  assert.equal(body["signature"], undefined);
});

// ---------------------------------------------------------------------------
// Revoked certificate
// ---------------------------------------------------------------------------

test("GET /v1/certificates/verify/:publicId returns 200 for a revoked certificate", async () => {
  const res = await appRevoked.inject({ method: "GET", url: `/v1/certificates/verify/${REVOKED_ID}` });
  assert.equal(res.statusCode, 200);
});

test("revoked certificate response has status 'revoked', not 'valid'", async () => {
  const res = await appRevoked.inject({ method: "GET", url: `/v1/certificates/verify/${REVOKED_ID}` });
  const body = res.json<{ status: string }>();
  assert.equal(body.status, "revoked");
  assert.notEqual(body.status, "valid");
});

test("revoked certificate response contains revoked_at", async () => {
  const res = await appRevoked.inject({ method: "GET", url: `/v1/certificates/verify/${REVOKED_ID}` });
  const body = res.json<{ revoked_at: unknown }>();
  assert.ok(typeof body.revoked_at === "string" && body.revoked_at.length > 0);
});

test("revoked certificate response contains all required public fields", async () => {
  const res = await appRevoked.inject({ method: "GET", url: `/v1/certificates/verify/${REVOKED_ID}` });
  const body = res.json<Record<string, unknown>>();
  assert.equal(body["schema_version"], "1.0.0");
  assert.equal(body["public_id"], REVOKED_ID);
  assert.ok(typeof body["worker_display_name"] === "string");
  assert.ok(typeof body["module_id"] === "string");
  assert.ok(typeof body["issued_at"] === "string");
  assert.ok(body["verification"] !== undefined);
});

// ---------------------------------------------------------------------------
// Unknown / not-found certificate
// ---------------------------------------------------------------------------

test("GET /v1/certificates/verify/:publicId returns 404 for an unknown public_id", async () => {
  const res = await appEmpty.inject({ method: "GET", url: `/v1/certificates/verify/${UNKNOWN_ID}` });
  assert.equal(res.statusCode, 404);
});

test("not_found response has status 'not_found'", async () => {
  const res = await appEmpty.inject({ method: "GET", url: `/v1/certificates/verify/${UNKNOWN_ID}` });
  const body = res.json<{ status: string; schema_version: string }>();
  assert.equal(body.status, "not_found");
  assert.equal(body.schema_version, "1.0.0");
});

test("not_found response echoes the queried public_id", async () => {
  const res = await appEmpty.inject({ method: "GET", url: `/v1/certificates/verify/${UNKNOWN_ID}` });
  const body = res.json<{ public_id?: string }>();
  assert.equal(body.public_id, UNKNOWN_ID);
});

test("not_found response does not contain certificate data fields", async () => {
  const res = await appEmpty.inject({ method: "GET", url: `/v1/certificates/verify/${UNKNOWN_ID}` });
  const body = res.json<Record<string, unknown>>();
  assert.equal(body["score"],               undefined);
  assert.equal(body["passed"],              undefined);
  assert.equal(body["worker_display_name"], undefined);
  assert.equal(body["issued_at"],           undefined);
  assert.equal(body["verification"],        undefined);
});

// ---------------------------------------------------------------------------
// Error paths
// ---------------------------------------------------------------------------

test("returns 503 when DATABASE_URL is not configured", async () => {
  const res = await appNoDb.inject({ method: "GET", url: `/v1/certificates/verify/${PUBLIC_ID}` });
  assert.equal(res.statusCode, 503);
  assert.equal(res.json<{ error: string }>().error, "service_unavailable");
});

test("returns 502 when database query fails", async () => {
  const res = await appDbFail.inject({ method: "GET", url: `/v1/certificates/verify/${PUBLIC_ID}` });
  assert.equal(res.statusCode, 502);
  assert.equal(res.json<{ error: string }>().error, "bad_gateway");
});

// ---------------------------------------------------------------------------
// Regression: existing endpoints unaffected
// ---------------------------------------------------------------------------

test("GET /health still returns 200 after cert-verify route added", async () => {
  const res = await appActive.inject({ method: "GET", url: "/health" });
  assert.equal(res.statusCode, 200);
  assert.deepEqual(res.json(), { status: "ok" });
});

test("GET /v1/modules still returns 200 after cert-verify route added", async () => {
  // appActive uses mockPool which returns ACTIVE_ROW for any query —
  // modules just sees unexpected columns but the status code path is fine.
  // Use a dedicated empty-modules pool to test cleanly.
  const appModules = await buildApp({
    getPool: () => ({
      query: async () => ({ rows: [], rowCount: 0 }) as unknown as QueryResult,
      on: () => { /* no-op */ },
      end: async () => undefined,
    } as unknown as Pool),
  });
  after(() => appModules.close());

  const res = await appModules.inject({ method: "GET", url: "/v1/modules" });
  assert.equal(res.statusCode, 200);
  assert.deepEqual(res.json<{ modules: unknown[] }>().modules, []);
});
