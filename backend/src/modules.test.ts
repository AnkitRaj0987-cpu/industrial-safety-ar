/**
 * Tests for GET /v1/modules
 *
 * Uses buildApp(deps) to inject a mock pool — no real PostgreSQL server needed.
 * Follows the same node:test + app.inject() pattern as health.test.ts.
 *
 * Each app instance is built at the true module top level (matching health.test.ts)
 * so that tsx/esbuild's top-level-await support handles the async initialisation.
 */

import assert from "node:assert/strict";
import { after, test } from "node:test";
import { buildApp } from "./app.js";
import type { Pool, QueryResult } from "pg";

// ---------------------------------------------------------------------------
// Helpers — minimal mock pool factory
// ---------------------------------------------------------------------------

const MVP_ROWS = [
  {
    id: "fire-explosion-response",
    title_key: "module.fire_explosion_response.title",
    pass_percent: "70.00", // pg returns NUMERIC as string
    content_version: "1.0.0",
  },
  {
    id: "gas-confined-space",
    title_key: "module.gas_confined_space.title",
    pass_percent: "70.00",
    content_version: "1.0.0",
  },
];

/**
 * Creates a mock Pool that returns the provided rows for any query.
 */
function mockPool(rows: Record<string, unknown>[]): Pool {
  return {
    query: async () =>
      ({ rows, rowCount: rows.length }) as unknown as QueryResult,
    on: () => { /* no-op */ },
    end: async () => undefined,
  } as unknown as Pool;
}

/**
 * A mock Pool whose query() always rejects — simulates a DB error.
 */
function failingPool(): Pool {
  return {
    query: async () => { throw new Error("simulated query failure"); },
    on: () => { /* no-op */ },
    end: async () => undefined,
  } as unknown as Pool;
}

/**
 * A getPool override that throws immediately — simulates missing DATABASE_URL.
 */
function noUrlGetPool(): Pool {
  throw new Error("DATABASE_URL is not set. Add it to your .env file (see .env.example).");
}

// ---------------------------------------------------------------------------
// App instances — built at module top level so tsx handles top-level await
// ---------------------------------------------------------------------------

const appSuccess  = await buildApp({ getPool: () => mockPool(MVP_ROWS) });
const appEmpty    = await buildApp({ getPool: () => mockPool([]) });
const appNoUrl    = await buildApp({ getPool: noUrlGetPool });
const appDbFail   = await buildApp({ getPool: () => failingPool() });

after(async () => {
  await Promise.all([
    appSuccess.close(),
    appEmpty.close(),
    appNoUrl.close(),
    appDbFail.close(),
  ]);
});

// ---------------------------------------------------------------------------
// Success path — two MVP modules returned
// ---------------------------------------------------------------------------

test("GET /v1/modules returns HTTP 200 when DB has rows", async () => {
  const res = await appSuccess.inject({ method: "GET", url: "/v1/modules" });
  assert.equal(res.statusCode, 200);
});

test("GET /v1/modules returns application/json content-type", async () => {
  const res = await appSuccess.inject({ method: "GET", url: "/v1/modules" });
  assert.match(res.headers["content-type"] as string, /application\/json/);
});

test("GET /v1/modules returns a modules array with both MVP modules", async () => {
  const res = await appSuccess.inject({ method: "GET", url: "/v1/modules" });
  const body = res.json<{ modules: unknown[] }>();
  assert.equal(Array.isArray(body.modules), true);
  assert.equal(body.modules.length, 2);
});

test("GET /v1/modules parses pass_percent as a number (not a string)", async () => {
  const res = await appSuccess.inject({ method: "GET", url: "/v1/modules" });
  const body = res.json<{ modules: Array<{ pass_percent: unknown }> }>();
  for (const mod of body.modules) {
    assert.equal(typeof mod.pass_percent, "number");
  }
});

test("GET /v1/modules includes all required fields on each module", async () => {
  const res = await appSuccess.inject({ method: "GET", url: "/v1/modules" });
  const body = res.json<{
    modules: Array<{ id: string; title_key: string; pass_percent: number; content_version: string }>;
  }>();
  for (const mod of body.modules) {
    assert.ok(mod.id,              "id must be present");
    assert.ok(mod.title_key,       "title_key must be present");
    assert.equal(typeof mod.pass_percent, "number");
    assert.ok(mod.content_version, "content_version must be present");
  }
});

test("GET /v1/modules returns fire-explosion-response with pass_percent 70", async () => {
  const res = await appSuccess.inject({ method: "GET", url: "/v1/modules" });
  const body = res.json<{ modules: Array<{ id: string; pass_percent: number }> }>();
  const fire = body.modules.find((m) => m.id === "fire-explosion-response");
  assert.ok(fire, "fire-explosion-response must be present");
  assert.equal(fire.pass_percent, 70);
});

test("GET /v1/modules returns gas-confined-space with pass_percent 70", async () => {
  const res = await appSuccess.inject({ method: "GET", url: "/v1/modules" });
  const body = res.json<{ modules: Array<{ id: string; pass_percent: number }> }>();
  const gas = body.modules.find((m) => m.id === "gas-confined-space");
  assert.ok(gas, "gas-confined-space must be present");
  assert.equal(gas.pass_percent, 70);
});

// ---------------------------------------------------------------------------
// Empty table — zero rows
// ---------------------------------------------------------------------------

test("GET /v1/modules returns HTTP 200 with empty array when table is empty", async () => {
  const res = await appEmpty.inject({ method: "GET", url: "/v1/modules" });
  assert.equal(res.statusCode, 200);
  const body = res.json<{ modules: unknown[] }>();
  assert.deepEqual(body.modules, []);
});

// ---------------------------------------------------------------------------
// Missing DATABASE_URL — 503
// ---------------------------------------------------------------------------

test("GET /v1/modules returns HTTP 503 when DATABASE_URL is not set", async () => {
  const res = await appNoUrl.inject({ method: "GET", url: "/v1/modules" });
  assert.equal(res.statusCode, 503);
});

test("GET /v1/modules returns error=service_unavailable when DATABASE_URL is not set", async () => {
  const res = await appNoUrl.inject({ method: "GET", url: "/v1/modules" });
  const body = res.json<{ error: string }>();
  assert.equal(body.error, "service_unavailable");
});

// ---------------------------------------------------------------------------
// Database query failure — 502
// ---------------------------------------------------------------------------

test("GET /v1/modules returns HTTP 502 when the DB query fails", async () => {
  const res = await appDbFail.inject({ method: "GET", url: "/v1/modules" });
  assert.equal(res.statusCode, 502);
});

test("GET /v1/modules returns error=bad_gateway when the DB query fails", async () => {
  const res = await appDbFail.inject({ method: "GET", url: "/v1/modules" });
  const body = res.json<{ error: string }>();
  assert.equal(body.error, "bad_gateway");
});

// ---------------------------------------------------------------------------
// Health check must still work — regression guard
// ---------------------------------------------------------------------------

test("GET /health still returns 200 { status: ok } after modules route added", async () => {
  const res = await appSuccess.inject({ method: "GET", url: "/health" });
  assert.equal(res.statusCode, 200);
  assert.deepEqual(res.json(), { status: "ok" });
});
