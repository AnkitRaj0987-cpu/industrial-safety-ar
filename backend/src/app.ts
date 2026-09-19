import Fastify from "fastify";
import type { FastifyInstance } from "fastify";
import type { Pool } from "pg";
import { getPool as defaultGetPool } from "./db.js";
import { validateSyncRequest, processSync } from "./sync.js";
import { lookupCertificate } from "./cert-verify.js";
import { authenticateWorker } from "./auth.js";
import {
  authenticateAdmin,
  getAdminStats,
  getAdminWorkers,
  getAdminWorkerById,
  getAdminAttempts,
  getAdminAttemptEvents,
  getAdminCertificates,
} from "./admin.js";

export type AppDeps = {
  /**
   * Override the database pool factory.
   * Used in tests to inject a mock pool without a real PostgreSQL server.
   * Defaults to the lazy singleton from db.ts.
   */
  getPool?: () => Pool;
  /**
   * Base URL for certificate verification links, e.g. "http://localhost:3000".
   * Defaults to that value when not provided (safe for tests / local dev).
   */
  publicBaseUrl?: string;
};

export type ModuleRow = {
  id: string;
  title_key: string;
  pass_percent: string; // NUMERIC comes back as string from pg
  content_version: string;
};

export async function buildApp(deps: AppDeps = {}): Promise<FastifyInstance> {
  const getPool = deps.getPool ?? defaultGetPool;
  const publicBaseUrl = deps.publicBaseUrl ?? "http://localhost:3000";

  const app = Fastify({
    logger: true,
  });

  app.addHook("onRequest", async (request, reply) => {
    reply.header("Access-Control-Allow-Origin", "*");
    reply.header("Access-Control-Allow-Methods", "GET, POST, PUT, DELETE, OPTIONS");
    reply.header("Access-Control-Allow-Headers", "Content-Type, Authorization");
    if (request.method === "OPTIONS") {
      return reply.status(204).send();
    }
  });

  app.get("/health", async () => {
    return { status: "ok" as const };
  });

  /**
   * GET /v1/modules
   *
   * Returns the list of training modules from the database.
   * Read-only — no user input, but parameterized SQL is used as a baseline
   * for consistency with the project's query style.
   *
   * Errors:
   *   503 — DATABASE_URL is not configured.
   *   502 — Database query failed.
   */
  app.get("/v1/modules", async (_request, reply) => {
    let pool: Pool;
    try {
      pool = getPool();
    } catch (err) {
      const message = err instanceof Error ? err.message : "Database not configured";
      app.log.error({ err }, "getPool() failed — DATABASE_URL likely missing");
      return reply.status(503).send({ error: "service_unavailable", message });
    }

    let rows: ModuleRow[];
    try {
      const result = await pool.query<ModuleRow>(
        `SELECT id, title_key, pass_percent, content_version
         FROM module
         ORDER BY id`,
      );
      rows = result.rows;
    } catch (err) {
      app.log.error({ err }, "Failed to query module table");
      return reply.status(502).send({ error: "bad_gateway", message: "Database query failed" });
    }

    return reply.status(200).send({
      modules: rows.map((r) => ({
        id: r.id,
        title_key: r.title_key,
        pass_percent: parseFloat(r.pass_percent),
        content_version: r.content_version,
      })),
    });
  });

  /**
   * POST /v1/auth/worker/login
   *
   * Authenticates a worker by worker_code (or worker_id) and PIN.
   */
  app.post("/v1/auth/worker/login", async (request, reply) => {
    const body = request.body as any;
    if (!body || typeof body !== "object") {
      return reply.status(400).send({ error: "bad_request", message: "Invalid request body" });
    }

    const workerIdentifier = body.worker_code ?? body.worker_id;
    const pin = body.pin;

    if (!workerIdentifier || typeof workerIdentifier !== "string" || !pin || typeof pin !== "string") {
      return reply.status(400).send({
        error: "bad_request",
        message: "worker_code (or worker_id) and pin are required string fields",
      });
    }

    let pool: Pool;
    try {
      pool = getPool();
    } catch (err) {
      const message = err instanceof Error ? err.message : "Database not configured";
      return reply.status(503).send({ error: "service_unavailable", message });
    }

    try {
      const result = await authenticateWorker(pool, workerIdentifier, pin);
      if (!result.success) {
        if (result.error === "not_found") {
          return reply.status(404).send({ error: "not_found", message: result.message });
        }
        return reply.status(401).send({ error: "unauthorized", message: result.message });
      }

      return reply.status(200).send({
        token: result.token,
        worker: result.worker,
      });
    } catch (err) {
      app.log.error({ err }, "Error in POST /v1/auth/worker/login");
      return reply.status(500).send({ error: "internal_server_error", message: "Login failed" });
    }
  });

  /**
   * POST /v1/sync
   *
   * Accepts a worker's offline-accumulated attempt batch.
   * Idempotent: re-submitting the same client_attempt_id is safe.
   *
   * Always returns HTTP 200 with the structured sync response.
   * Per-attempt outcomes live in accepted / duplicates / rejected arrays.
   *
   * Errors:
   *   400 — Request body is structurally invalid.
   *   503 — DATABASE_URL is not configured.
   */
  app.post("/v1/sync", async (request, reply) => {
    // Structural validation — cheap, no DB required
    const validationError = validateSyncRequest(request.body);
    if (validationError !== null) {
      return reply.status(400).send({ error: "bad_request", message: validationError });
    }

    // Ensure pool is available before we start processing attempts
    let pool: Pool;
    try {
      pool = getPool();
    } catch (err) {
      const message = err instanceof Error ? err.message : "Database not configured";
      app.log.error({ err }, "getPool() failed in POST /v1/sync");
      return reply.status(503).send({ error: "service_unavailable", message });
    }

    try {
      // processSync takes ownership of pool usage; errors per-attempt are
      // captured inside the response arrays rather than thrown.
      const syncResponse = await processSync(
        // validateSyncRequest already confirmed the shape is correct
        request.body as Parameters<typeof processSync>[0],
        { getPool: () => pool, publicBaseUrl },
      );
      return reply.status(200).send(syncResponse);
    } catch (err) {
      app.log.error({ err }, "Unexpected error in POST /v1/sync");
      return reply.status(500).send({ error: "internal_server_error", message: "Unexpected error" });
    }
  });

  /**
   * GET /v1/certificates/verify/:publicId
   *
   * Public certificate verification — no authentication required.
   * Returns the contract-shaped verification payload from
   * docs/contracts/certificate-verify.schema.json.
   *
   * Responses:
   *   200 status:"valid"    — active certificate found
   *   200 status:"revoked"  — revoked certificate found
   *   404 status:"not_found"— unknown public_id
   *   503                   — DATABASE_URL not configured
   *   502                   — database query failed
   */
  app.get("/v1/certificates/verify/:publicId", async (request, reply) => {
    const { publicId } = request.params as { publicId: string };

    let pool: Pool;
    try {
      pool = getPool();
    } catch (err) {
      const message = err instanceof Error ? err.message : "Database not configured";
      app.log.error({ err }, "getPool() failed in GET /v1/certificates/verify");
      return reply.status(503).send({ error: "service_unavailable", message });
    }

    let certResponse;
    try {
      certResponse = await lookupCertificate(publicId, pool, publicBaseUrl);
    } catch (err) {
      app.log.error({ err }, "Failed to query certificate table");
      return reply.status(502).send({ error: "bad_gateway", message: "Database query failed" });
    }

    if (certResponse.status === "not_found") {
      return reply.status(404).send(certResponse);
    }

    return reply.status(200).send(certResponse);
  });

  /**
   * POST /v1/auth/admin/login
   *
   * Authenticates dashboard administrator with email and password.
   */
  app.post("/v1/auth/admin/login", async (request, reply) => {
    const body = request.body as any;
    if (!body || typeof body !== "object") {
      return reply.status(400).send({ error: "bad_request", message: "Invalid request body" });
    }

    const { email, password } = body;
    if (!email || typeof email !== "string" || !password || typeof password !== "string") {
      return reply.status(400).send({
        error: "bad_request",
        message: "email and password are required string fields",
      });
    }

    let pool: Pool;
    try {
      pool = getPool();
    } catch (err) {
      const message = err instanceof Error ? err.message : "Database not configured";
      return reply.status(503).send({ error: "service_unavailable", message });
    }

    try {
      const result = await authenticateAdmin(pool, email, password);
      if (!result.success) {
        return reply.status(401).send({ error: "unauthorized", message: result.message });
      }
      return reply.status(200).send(result);
    } catch (err) {
      app.log.error({ err }, "Error in POST /v1/auth/admin/login");
      return reply.status(500).send({ error: "internal_server_error", message: "Login failed" });
    }
  });

  /**
   * GET /v1/admin/stats
   *
   * Returns dashboard overview statistics (worker counts, attempt pass rate, certificates).
   */
  app.get("/v1/admin/stats", async (_request, reply) => {
    let pool: Pool;
    try {
      pool = getPool();
    } catch (err) {
      const message = err instanceof Error ? err.message : "Database not configured";
      return reply.status(503).send({ error: "service_unavailable", message });
    }

    try {
      const stats = await getAdminStats(pool);
      return reply.status(200).send({ stats });
    } catch (err) {
      app.log.error({ err }, "Failed to query admin stats");
      return reply.status(502).send({ error: "bad_gateway", message: "Database query failed" });
    }
  });

  /**
   * GET /v1/admin/workers
   *
   * Lists workers with summary metrics.
   */
  app.get("/v1/admin/workers", async (request, reply) => {
    const query = request.query as any;
    let pool: Pool;
    try {
      pool = getPool();
    } catch (err) {
      const message = err instanceof Error ? err.message : "Database not configured";
      return reply.status(503).send({ error: "service_unavailable", message });
    }

    try {
      const result = await getAdminWorkers(pool, {
        search: query?.search,
        site: query?.site,
        limit: query?.limit ? parseInt(query.limit, 10) : undefined,
        offset: query?.offset ? parseInt(query.offset, 10) : undefined,
      });
      return reply.status(200).send(result);
    } catch (err) {
      app.log.error({ err }, "Failed to query admin workers");
      return reply.status(502).send({ error: "bad_gateway", message: "Database query failed" });
    }
  });

  /**
   * GET /v1/admin/workers/:id
   *
   * Full worker details including training attempt history and issued certificates.
   */
  app.get("/v1/admin/workers/:id", async (request, reply) => {
    const { id } = request.params as { id: string };
    let pool: Pool;
    try {
      pool = getPool();
    } catch (err) {
      const message = err instanceof Error ? err.message : "Database not configured";
      return reply.status(503).send({ error: "service_unavailable", message });
    }

    try {
      const data = await getAdminWorkerById(pool, id);
      if (!data) {
        return reply.status(404).send({ error: "not_found", message: "Worker not found" });
      }
      return reply.status(200).send(data);
    } catch (err) {
      app.log.error({ err }, "Failed to query admin worker detail");
      return reply.status(502).send({ error: "bad_gateway", message: "Database query failed" });
    }
  });

  /**
   * GET /v1/admin/attempts
   *
   * Lists training attempts with worker and module joins.
   */
  app.get("/v1/admin/attempts", async (request, reply) => {
    const query = request.query as any;
    let pool: Pool;
    try {
      pool = getPool();
    } catch (err) {
      const message = err instanceof Error ? err.message : "Database not configured";
      return reply.status(503).send({ error: "service_unavailable", message });
    }

    try {
      const result = await getAdminAttempts(pool, {
        moduleId: query?.moduleId,
        status: query?.status,
        workerId: query?.workerId,
        limit: query?.limit ? parseInt(query.limit, 10) : undefined,
        offset: query?.offset ? parseInt(query.offset, 10) : undefined,
      });
      return reply.status(200).send(result);
    } catch (err) {
      app.log.error({ err }, "Failed to query admin attempts");
      return reply.status(502).send({ error: "bad_gateway", message: "Database query failed" });
    }
  });

  /**
   * GET /v1/admin/attempts/:id/events
   *
   * Returns structured events recorded for an attempt.
   */
  app.get("/v1/admin/attempts/:id/events", async (request, reply) => {
    const { id } = request.params as { id: string };
    let pool: Pool;
    try {
      pool = getPool();
    } catch (err) {
      const message = err instanceof Error ? err.message : "Database not configured";
      return reply.status(503).send({ error: "service_unavailable", message });
    }

    try {
      const events = await getAdminAttemptEvents(pool, id);
      return reply.status(200).send({ events });
    } catch (err) {
      app.log.error({ err }, "Failed to query admin attempt events");
      return reply.status(502).send({ error: "bad_gateway", message: "Database query failed" });
    }
  });

  /**
   * GET /v1/admin/certificates
   *
   * Lists issued certificates with status and worker details.
   */
  app.get("/v1/admin/certificates", async (request, reply) => {
    const query = request.query as any;
    let pool: Pool;
    try {
      pool = getPool();
    } catch (err) {
      const message = err instanceof Error ? err.message : "Database not configured";
      return reply.status(503).send({ error: "service_unavailable", message });
    }

    try {
      const result = await getAdminCertificates(pool, {
        status: query?.status,
        limit: query?.limit ? parseInt(query.limit, 10) : undefined,
        offset: query?.offset ? parseInt(query.offset, 10) : undefined,
      });
      return reply.status(200).send(result);
    } catch (err) {
      app.log.error({ err }, "Failed to query admin certificates");
      return reply.status(502).send({ error: "bad_gateway", message: "Database query failed" });
    }
  });

  return app;
}
