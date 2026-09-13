import Fastify from "fastify";
import type { FastifyInstance } from "fastify";
import type { Pool } from "pg";
import { getPool as defaultGetPool } from "./db.js";
import { validateSyncRequest, processSync } from "./sync.js";
import { lookupCertificate } from "./cert-verify.js";

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

  return app;
}
