/**
 * PostgreSQL connection pool — STEP 6A foundation.
 *
 * The pool is created lazily on first call to getPool().
 * This keeps GET /health functional even when DATABASE_URL is not set.
 *
 * Usage:
 *   import { getPool } from "./db.js";
 *   const result = await getPool().query("SELECT 1");
 *
 * Do NOT import this module at the top level of app.ts or server.ts.
 * Import it only inside route handlers or services that actually need DB access.
 */

import pg from "pg";
import type { Pool } from "pg";

const { Pool: PgPool } = pg;

let pool: Pool | undefined;

/**
 * Returns the shared PostgreSQL pool, creating it on first call.
 * Throws if DATABASE_URL is not set in the environment.
 */
export function getPool(): Pool {
  if (pool !== undefined) {
    return pool;
  }

  const connectionString = process.env["DATABASE_URL"];
  if (connectionString === undefined || connectionString.trim() === "") {
    throw new Error(
      "DATABASE_URL is not set. Add it to your .env file (see .env.example).",
    );
  }

  pool = new PgPool({
    connectionString,
    // Conservative defaults — tune in later phases.
    max: 10,
    idleTimeoutMillis: 30_000,
    connectionTimeoutMillis: 5_000,
  });

  // Surface unexpected pool errors to the process log rather than swallowing them.
  pool.on("error", (err: Error) => {
    console.error("[db] Unexpected pool error:", err.message);
  });

  return pool;
}

/**
 * Closes the pool if it was created. Call this during graceful shutdown.
 * Safe to call even if the pool was never initialised.
 */
export async function closePool(): Promise<void> {
  if (pool !== undefined) {
    await pool.end();
    pool = undefined;
  }
}
