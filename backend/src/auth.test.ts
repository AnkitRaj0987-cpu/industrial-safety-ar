// backend/src/auth.test.ts

import test from "node:test";
import assert from "node:assert/strict";
import { hashPin, verifyPin, authenticateWorker } from "./auth.js";
import type { Pool } from "pg";

test("hashPin generates valid pbkdf2 format and verifyPin matches", () => {
  const pin = "1234";
  const hashed = hashPin(pin);
  assert.ok(hashed.startsWith("pbkdf2$10000$"));
  assert.equal(verifyPin(pin, hashed), true);
  assert.equal(verifyPin("0000", hashed), false);
  assert.equal(verifyPin("", hashed), false);
});

test("verifyPin handles fallback mock demo hashes", () => {
  assert.equal(verifyPin("1234", "$2b$10$demo_pin_hash"), true);
  assert.equal(verifyPin("9999", "$2b$10$demo_pin_hash"), false);
});

test("authenticateWorker validates against mock pool", async () => {
  const mockPool = {
    query: async (sql: string, params: any[]) => {
      if (params[0] === "DEMO-001" || params[0] === "00000000-dead-beef-0001-000000000001") {
        return {
          rows: [
            {
              id: "00000000-dead-beef-0001-000000000001",
              worker_code: "DEMO-001",
              pin_hash: hashPin("1234"),
              display_name: "Operator Ramesh Kumar",
              locale: "en",
              site_label: "Mining & Material Handling",
            },
          ],
        };
      }
      return { rows: [] };
    },
  } as unknown as Pool;

  // 1. Success with worker_code
  const res1 = await authenticateWorker(mockPool, "DEMO-001", "1234");
  assert.equal(res1.success, true);
  if (res1.success) {
    assert.equal(res1.worker.worker_code, "DEMO-001");
    assert.equal(res1.worker.display_name, "Operator Ramesh Kumar");
    assert.ok(res1.token.startsWith("wtoken_"));
  }

  // 2. Success with worker UUID
  const res2 = await authenticateWorker(mockPool, "00000000-dead-beef-0001-000000000001", "1234");
  assert.equal(res2.success, true);

  // 3. Failed PIN
  const res3 = await authenticateWorker(mockPool, "DEMO-001", "wrong_pin");
  assert.equal(res3.success, false);
  if (!res3.success) {
    assert.equal(res3.error, "unauthorized");
  }

  // 4. Missing worker
  const res4 = await authenticateWorker(mockPool, "NON-EXISTENT", "1234");
  assert.equal(res4.success, false);
  if (!res4.success) {
    assert.equal(res4.error, "not_found");
  }
});

import { buildApp } from "./app.js";

test("POST /v1/auth/worker/login returns 200 with token and profile on valid credentials", async () => {
  const mockPool = {
    query: async () => ({
      rows: [
        {
          id: "00000000-dead-beef-0001-000000000001",
          worker_code: "DEMO-001",
          pin_hash: hashPin("1234"),
          display_name: "Operator Ramesh Kumar",
          locale: "en",
          site_label: "Mining & Material Handling",
        },
      ],
    }),
  } as unknown as Pool;

  const app = await buildApp({ getPool: () => mockPool });
  try {
    const res = await app.inject({
      method: "POST",
      url: "/v1/auth/worker/login",
      payload: {
        worker_code: "DEMO-001",
        pin: "1234",
      },
    });

    assert.equal(res.statusCode, 200);
    const body = res.json();
    assert.ok(body.token);
    assert.equal(body.worker.worker_code, "DEMO-001");
    assert.equal(body.worker.display_name, "Operator Ramesh Kumar");
  } finally {
    await app.close();
  }
});

test("POST /v1/auth/worker/login returns 401 on invalid PIN", async () => {
  const mockPool = {
    query: async () => ({
      rows: [
        {
          id: "00000000-dead-beef-0001-000000000001",
          worker_code: "DEMO-001",
          pin_hash: hashPin("1234"),
          display_name: "Operator Ramesh Kumar",
          locale: "en",
          site_label: "Mining & Material Handling",
        },
      ],
    }),
  } as unknown as Pool;

  const app = await buildApp({ getPool: () => mockPool });
  try {
    const res = await app.inject({
      method: "POST",
      url: "/v1/auth/worker/login",
      payload: {
        worker_code: "DEMO-001",
        pin: "9999",
      },
    });

    assert.equal(res.statusCode, 401);
  } finally {
    await app.close();
  }
});

test("POST /v1/auth/worker/login returns 400 on missing parameters", async () => {
  const app = await buildApp({ getPool: () => ({} as Pool) });
  try {
    const res = await app.inject({
      method: "POST",
      url: "/v1/auth/worker/login",
      payload: {},
    });

    assert.equal(res.statusCode, 400);
  } finally {
    await app.close();
  }
});
