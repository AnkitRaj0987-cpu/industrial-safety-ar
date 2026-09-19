// backend/src/admin.test.ts
// Tests for administrative endpoints and queries.

import test from "node:test";
import assert from "node:assert/strict";
import type { Pool } from "pg";
import { buildApp } from "./app.js";
import { hashPin } from "./auth.js";

function createAdminMockPool(): Pool {
  return {
    query: async (sql: string, params: any[] = []) => {
      const normalizedSql = sql.replace(/\s+/g, " ").trim();

      // Admin user lookup
      if (normalizedSql.includes("FROM admin_user WHERE LOWER(email) = $1")) {
        if (params[0] === "admin@safety.jharkhand.gov.in") {
          return {
            rows: [
              {
                id: "00000000-dead-beef-ad01-000000000001",
                email: "admin@safety.jharkhand.gov.in",
                password_hash: hashPin("admin123"),
                role: "admin",
              },
            ],
          };
        }
        return { rows: [] };
      }

      // Stats: Worker count
      if (normalizedSql.includes("SELECT COUNT(*)::text as count FROM worker")) {
        return { rows: [{ count: "12" }] };
      }

      // Stats: Attempt aggregates
      if (normalizedSql.includes("COUNT(*) FILTER (WHERE status = 'completed')")) {
        return {
          rows: [
            {
              total_count: "20",
              completed_count: "18",
              passed_count: "15",
              avg_score: "86.5",
            },
          ],
        };
      }

      // Stats: Active certificate count
      if (normalizedSql.includes("SELECT COUNT(*)::text as count FROM certificate WHERE status = 'active'")) {
        return { rows: [{ count: "15" }] };
      }

      // Stats: Module breakdown
      if (normalizedSql.includes("FROM module m LEFT JOIN attempt a ON a.module_id = m.id")) {
        return {
          rows: [
            {
              id: "fire-explosion-response",
              title_key: "module.fire_explosion_response.title",
              pass_percent: "70.00",
              total_attempts: "11",
              passed_attempts: "9",
              avg_score: "85.2",
            },
            {
              id: "gas-confined-space",
              title_key: "module.gas_confined_space.title",
              pass_percent: "70.00",
              total_attempts: "9",
              passed_attempts: "6",
              avg_score: "88.1",
            },
          ],
        };
      }

      // Workers list
      if (normalizedSql.includes("FROM worker w") && normalizedSql.includes("COUNT(DISTINCT a.id)::int as attempt_count")) {
        return {
          rows: [
            {
              id: "00000000-dead-beef-0001-000000000001",
              worker_code: "DEMO-001",
              display_name: "Operator Ramesh Kumar",
              locale: "en",
              site_label: "Bokaro Steel Plant",
              created_at: "2026-09-01T00:00:00Z",
              attempt_count: 2,
              certificate_count: 2,
              last_active: "2026-09-18T10:00:00Z",
            },
          ],
        };
      }

      // Workers count
      if (normalizedSql.includes("SELECT COUNT(*)::int as total FROM worker w")) {
        return { rows: [{ total: 1 }] };
      }

      // Worker by ID
      if (normalizedSql.includes("FROM worker WHERE id = $1 OR worker_code = $1")) {
        if (params[0] === "DEMO-001" || params[0] === "00000000-dead-beef-0001-000000000001") {
          return {
            rows: [
              {
                id: "00000000-dead-beef-0001-000000000001",
                worker_code: "DEMO-001",
                display_name: "Operator Ramesh Kumar",
                locale: "en",
                site_label: "Bokaro Steel Plant",
                created_at: "2026-09-01T00:00:00Z",
              },
            ],
          };
        }
        return { rows: [] };
      }

      // Enrollments
      if (normalizedSql.includes("FROM enrollment WHERE worker_id = $1")) {
        return {
          rows: [
            { module_id: "fire-explosion-response", assigned_at: "2026-09-01T00:00:00Z" },
            { module_id: "gas-confined-space", assigned_at: "2026-09-01T00:00:00Z" },
          ],
        };
      }

      // Worker attempts
      if (normalizedSql.includes("FROM attempt WHERE worker_id = $1")) {
        return {
          rows: [
            {
              id: "att-001",
              client_attempt_id: "00000000-0000-0000-0001-000000000001",
              module_id: "fire-explosion-response",
              content_version: "1.0.0",
              started_at: "2026-09-18T09:40:00Z",
              completed_at: "2026-09-18T10:00:00Z",
              client_score: "92.00",
              server_score: "92.00",
              passed: true,
              status: "completed",
            },
          ],
        };
      }

      // Worker certificates
      if (normalizedSql.includes("FROM certificate WHERE worker_id = $1")) {
        return {
          rows: [
            {
              id: "cert-001",
              public_id: "c0000000-beef-0001-0000-000000000001",
              module_id: "fire-explosion-response",
              score: "92.00",
              status: "active",
              issued_at: "2026-09-18T10:00:00Z",
              revoked_at: null,
            },
          ],
        };
      }

      // Attempts list
      if (normalizedSql.includes("FROM attempt a JOIN worker w ON w.id = a.worker_id")) {
        return {
          rows: [
            {
              id: "att-001",
              client_attempt_id: "00000000-0000-0000-0001-000000000001",
              worker_id: "00000000-dead-beef-0001-000000000001",
              module_id: "fire-explosion-response",
              content_version: "1.0.0",
              started_at: "2026-09-18T09:40:00Z",
              completed_at: "2026-09-18T10:00:00Z",
              client_score: "92.00",
              server_score: "92.00",
              passed: true,
              status: "completed",
              synced_at: "2026-09-18T10:01:00Z",
              worker_code: "DEMO-001",
              display_name: "Operator Ramesh Kumar",
              site_label: "Bokaro Steel Plant",
              module_title_key: "module.fire_explosion_response.title",
            },
          ],
        };
      }

      // Certificates list
      if (normalizedSql.includes("FROM certificate c JOIN worker w ON w.id = c.worker_id")) {
        return {
          rows: [
            {
              id: "cert-001",
              public_id: "c0000000-beef-0001-0000-000000000001",
              worker_id: "00000000-dead-beef-0001-000000000001",
              module_id: "fire-explosion-response",
              attempt_id: "att-001",
              score: "92.00",
              status: "active",
              signature: "sig123",
              issued_at: "2026-09-18T10:00:00Z",
              revoked_at: null,
              worker_code: "DEMO-001",
              display_name: "Operator Ramesh Kumar",
              site_label: "Bokaro Steel Plant",
              module_title_key: "module.fire_explosion_response.title",
            },
          ],
        };
      }

      // Attempt events
      if (normalizedSql.includes("FROM attempt_event WHERE attempt_id = $1")) {
        return {
          rows: [
            {
              id: "ev-01",
              client_event_id: "00000000-0000-0000-0001-000000000002",
              seq: 1,
              event_type: "hazard_identified",
              payload: { hazard_type: "electrical_panel" },
              occurred_at: "2026-09-18T09:41:00Z",
            },
          ],
        };
      }

      return { rows: [] };
    },
  } as unknown as Pool;
}

test("POST /v1/auth/admin/login succeeds with valid credentials", async () => {
  const app = await buildApp({ getPool: createAdminMockPool });
  const res = await app.inject({
    method: "POST",
    url: "/v1/auth/admin/login",
    payload: {
      email: "admin@safety.jharkhand.gov.in",
      password: "admin123",
    },
  });

  assert.equal(res.statusCode, 200);
  const data = JSON.parse(res.body);
  assert.equal(data.success, true);
  assert.ok(data.token.startsWith("atoken_"));
  assert.equal(data.admin.email, "admin@safety.jharkhand.gov.in");
  assert.equal(data.admin.role, "admin");
});

test("POST /v1/auth/admin/login fails with invalid password", async () => {
  const app = await buildApp({ getPool: createAdminMockPool });
  const res = await app.inject({
    method: "POST",
    url: "/v1/auth/admin/login",
    payload: {
      email: "admin@safety.jharkhand.gov.in",
      password: "wrong_password",
    },
  });

  assert.equal(res.statusCode, 401);
});

test("GET /v1/admin/stats returns aggregate metrics", async () => {
  const app = await buildApp({ getPool: createAdminMockPool });
  const res = await app.inject({
    method: "GET",
    url: "/v1/admin/stats",
  });

  assert.equal(res.statusCode, 200);
  const data = JSON.parse(res.body);
  assert.ok(data.stats);
  assert.equal(data.stats.totalWorkers, 12);
  assert.equal(data.stats.totalAttempts, 20);
  assert.equal(data.stats.totalCertificates, 15);
  assert.equal(data.stats.passRate, 83.3);
  assert.equal(data.stats.moduleBreakdown.length, 2);
});

test("GET /v1/admin/workers returns worker summary list", async () => {
  const app = await buildApp({ getPool: createAdminMockPool });
  const res = await app.inject({
    method: "GET",
    url: "/v1/admin/workers",
  });

  assert.equal(res.statusCode, 200);
  const data = JSON.parse(res.body);
  assert.equal(data.workers.length, 1);
  assert.equal(data.workers[0].worker_code, "DEMO-001");
  assert.equal(data.workers[0].attempt_count, 2);
  assert.equal(data.workers[0].certificate_count, 2);
});

test("GET /v1/admin/workers/:id returns full profile and history", async () => {
  const app = await buildApp({ getPool: createAdminMockPool });
  const res = await app.inject({
    method: "GET",
    url: "/v1/admin/workers/DEMO-001",
  });

  assert.equal(res.statusCode, 200);
  const data = JSON.parse(res.body);
  assert.equal(data.worker.worker_code, "DEMO-001");
  assert.equal(data.enrollments.length, 2);
  assert.equal(data.attempts.length, 1);
  assert.equal(data.attempts[0].client_score, 92);
  assert.equal(data.certificates.length, 1);
});

test("GET /v1/admin/attempts returns attempts table", async () => {
  const app = await buildApp({ getPool: createAdminMockPool });
  const res = await app.inject({
    method: "GET",
    url: "/v1/admin/attempts",
  });

  assert.equal(res.statusCode, 200);
  const data = JSON.parse(res.body);
  assert.equal(data.attempts.length, 1);
  assert.equal(data.attempts[0].worker_code, "DEMO-001");
  assert.equal(data.attempts[0].module_id, "fire-explosion-response");
});

test("GET /v1/admin/certificates returns certificate list", async () => {
  const app = await buildApp({ getPool: createAdminMockPool });
  const res = await app.inject({
    method: "GET",
    url: "/v1/admin/certificates",
  });

  assert.equal(res.statusCode, 200);
  const data = JSON.parse(res.body);
  assert.equal(data.certificates.length, 1);
  assert.equal(data.certificates[0].public_id, "c0000000-beef-0001-0000-000000000001");
});
