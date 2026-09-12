import Fastify from "fastify";
import type { FastifyInstance } from "fastify";
import type { Pool } from "pg";
import { getPool as defaultGetPool } from "./db.js";

export type AppDeps = {
  /**
   * Override the database pool factory.
   * Used in tests to inject a mock pool without a real PostgreSQL server.
   * Defaults to the lazy singleton from db.ts.
   */
  getPool?: () => Pool;
};

export type ModuleRow = {
  id: string;
  title_key: string;
  pass_percent: string; // NUMERIC comes back as string from pg
  content_version: string;
};

export async function buildApp(deps: AppDeps = {}): Promise<FastifyInstance> {
  const getPool = deps.getPool ?? defaultGetPool;

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

  return app;
}
