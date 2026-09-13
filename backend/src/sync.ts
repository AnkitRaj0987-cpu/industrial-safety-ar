/**
 * POST /v1/sync — business logic
 *
 * MVP scope (STEP 5.2):
 *  - Structural validation of the request body
 *  - Per-attempt: duplicate detection, worker/module/content_version validation,
 *    event sequence validation, transactional insert
 *  - server_score = client_score for MVP (rubric re-scoring is a later step)
 *  - No certificate issuance yet
 *
 * All DB work is done through the injected PoolClient so tests can mock it.
 * The pool itself is obtained from the caller (app.ts route handler).
 */

import type { Pool, PoolClient } from "pg";

// ---------------------------------------------------------------------------
// Contract types — inlined from docs/contracts/*.schema.json
// ---------------------------------------------------------------------------

export type EventPayload = Record<string, unknown>;

export type SyncAttemptEvent = {
  schema_version: string;
  client_event_id: string;
  sequence: number;
  event_type: string;
  occurred_at: string;
  step_id: string;
  payload: EventPayload;
};

export type SyncAttempt = {
  schema_version: string;
  client_attempt_id: string;
  worker_id: string;
  module_id: string;
  content_version: string;
  started_at: string;
  completed_at: string;
  client_score: number;
  passed: boolean;
  status: "completed";
  events: SyncAttemptEvent[];
  // optional server-enrichment fields (ignored if present on input)
  server_attempt_id?: string;
  server_score?: number;
  server_passed?: boolean;
  certificate_public_id?: string;
};

export type SyncRequest = {
  schema_version: string;
  client_request_id: string;
  worker: {
    worker_id: string;
    client_device_id?: string;
  };
  attempts: SyncAttempt[];
};

// Response item shapes
export type AcceptedItem = {
  client_attempt_id: string;
  server_attempt_id: string;
  server_score: number;
  server_passed: boolean;
};

export type DuplicateItem = {
  client_attempt_id: string;
  server_attempt_id: string;
};

export type RejectedItem = {
  client_attempt_id: string;
  error_code: string;
  message: string;
};

export type SyncResponse = {
  schema_version: "1.0.0";
  server_time: string;
  accepted: AcceptedItem[];
  duplicates: DuplicateItem[];
  rejected: RejectedItem[];
  certificates: unknown[]; // certificate issuance not implemented in MVP
};

// Internal DB row shapes
type AttemptRow = { id: string };
type WorkerRow = { id: string };
type ModuleRow = { id: string; content_version: string; pass_percent: string };

// ---------------------------------------------------------------------------
// Structural validation helpers
// ---------------------------------------------------------------------------

/** Returns a string describing the first structural problem, or null if ok. */
export function validateSyncRequest(body: unknown): string | null {
  if (body === null || typeof body !== "object" || Array.isArray(body)) {
    return "Request body must be a JSON object";
  }
  const b = body as Record<string, unknown>;

  if (b["schema_version"] !== "1.0.0") {
    return 'schema_version must be "1.0.0"';
  }
  if (typeof b["client_request_id"] !== "string" || b["client_request_id"].trim() === "") {
    return "client_request_id must be a non-empty string";
  }
  if (
    b["worker"] === null ||
    typeof b["worker"] !== "object" ||
    Array.isArray(b["worker"])
  ) {
    return "worker must be an object";
  }
  const w = b["worker"] as Record<string, unknown>;
  if (typeof w["worker_id"] !== "string" || w["worker_id"].trim() === "") {
    return "worker.worker_id must be a non-empty string";
  }
  if (!Array.isArray(b["attempts"])) {
    return "attempts must be an array";
  }
  const attempts = b["attempts"] as unknown[];
  if (attempts.length < 1 || attempts.length > 20) {
    return "attempts must contain between 1 and 20 items";
  }

  for (let i = 0; i < attempts.length; i++) {
    const a = attempts[i];
    if (a === null || typeof a !== "object" || Array.isArray(a)) {
      return `attempts[${i}] must be an object`;
    }
    const err = validateAttemptShape(a as Record<string, unknown>, i);
    if (err !== null) return err;
  }

  return null;
}

function validateAttemptShape(a: Record<string, unknown>, idx: number): string | null {
  const prefix = `attempts[${idx}]`;
  for (const field of [
    "schema_version",
    "client_attempt_id",
    "worker_id",
    "module_id",
    "content_version",
    "started_at",
    "completed_at",
  ] as const) {
    if (typeof a[field] !== "string" || (a[field] as string).trim() === "") {
      return `${prefix}.${field} must be a non-empty string`;
    }
  }
  if (a["schema_version"] !== "1.0.0") {
    return `${prefix}.schema_version must be "1.0.0"`;
  }
  if (a["status"] !== "completed") {
    return `${prefix}.status must be "completed"`;
  }
  if (typeof a["client_score"] !== "number") {
    return `${prefix}.client_score must be a number`;
  }
  if (typeof a["passed"] !== "boolean") {
    return `${prefix}.passed must be a boolean`;
  }
  if (!Array.isArray(a["events"])) {
    return `${prefix}.events must be an array`;
  }

  const events = a["events"] as unknown[];
  for (let j = 0; j < events.length; j++) {
    const e = events[j];
    if (e === null || typeof e !== "object" || Array.isArray(e)) {
      return `${prefix}.events[${j}] must be an object`;
    }
    const err = validateEventShape(e as Record<string, unknown>, idx, j);
    if (err !== null) return err;
  }

  return null;
}

function validateEventShape(
  e: Record<string, unknown>,
  attemptIdx: number,
  eventIdx: number,
): string | null {
  const prefix = `attempts[${attemptIdx}].events[${eventIdx}]`;
  for (const field of ["schema_version", "client_event_id", "event_type", "occurred_at", "step_id"] as const) {
    if (typeof e[field] !== "string" || (e[field] as string).trim() === "") {
      return `${prefix}.${field} must be a non-empty string`;
    }
  }
  if (e["schema_version"] !== "1.0.0") {
    return `${prefix}.schema_version must be "1.0.0"`;
  }
  if (typeof e["sequence"] !== "number" || !Number.isInteger(e["sequence"]) || (e["sequence"] as number) < 1) {
    return `${prefix}.sequence must be a positive integer`;
  }
  if (e["payload"] === null || typeof e["payload"] !== "object" || Array.isArray(e["payload"])) {
    return `${prefix}.payload must be an object`;
  }
  return null;
}

/** Checks that event sequences for one attempt are 1..N with no gaps. */
function validateEventSequences(events: SyncAttemptEvent[]): string | null {
  if (events.length === 0) return null;
  const sorted = [...events].sort((a, b) => a.sequence - b.sequence);
  for (let i = 0; i < sorted.length; i++) {
    if (sorted[i]!.sequence !== i + 1) {
      return `Event sequences must be consecutive starting at 1; got ${sorted[i]!.sequence} at position ${i + 1}`;
    }
  }
  return null;
}

// ---------------------------------------------------------------------------
// Core sync processing
// ---------------------------------------------------------------------------

export type ProcessSyncDeps = {
  getPool: () => Pool;
  publicBaseUrl: string;
};

/**
 * Processes a validated sync request.
 * Each attempt is handled independently inside its own transaction so
 * one bad attempt does not roll back valid ones.
 */
export async function processSync(
  request: SyncRequest,
  deps: ProcessSyncDeps,
): Promise<SyncResponse> {
  const pool = deps.getPool();

  const accepted: AcceptedItem[] = [];
  const duplicates: DuplicateItem[] = [];
  const rejected: RejectedItem[] = [];

  for (const attempt of request.attempts) {
    const result = await processSingleAttempt(attempt, request.worker.worker_id, pool);
    if (result.kind === "accepted") {
      accepted.push(result.item);
    } else if (result.kind === "duplicate") {
      duplicates.push(result.item);
    } else {
      rejected.push(result.item);
    }
  }

  return {
    schema_version: "1.0.0",
    server_time: new Date().toISOString(),
    accepted,
    duplicates,
    rejected,
    certificates: [], // certificate issuance: later step
  };
}

type AttemptOutcome =
  | { kind: "accepted"; item: AcceptedItem }
  | { kind: "duplicate"; item: DuplicateItem }
  | { kind: "rejected"; item: RejectedItem };

async function processSingleAttempt(
  attempt: SyncAttempt,
  requestWorkerId: string,
  pool: Pool,
): Promise<AttemptOutcome> {
  const caid = attempt.client_attempt_id;

  // 1. worker_id consistency check (pure, no DB needed)
  if (attempt.worker_id !== requestWorkerId) {
    return reject(caid, "WORKER_MISMATCH", "attempt.worker_id does not match request worker.worker_id");
  }

  // 2. event sequence validation (pure)
  const seqErr = validateEventSequences(attempt.events);
  if (seqErr !== null) {
    return reject(caid, "EVENT_SEQUENCE_INVALID", seqErr);
  }

  // Acquire a client for this attempt's transaction
  let client: PoolClient;
  try {
    client = await pool.connect();
  } catch (err) {
    const msg = err instanceof Error ? err.message : "Database connection failed";
    return reject(caid, "SCHEMA_INVALID", `Database unavailable: ${msg}`);
  }

  try {
    await client.query("BEGIN");

    // 3. Duplicate check — lock the row to prevent concurrent double-insert
    const dupCheck = await client.query<AttemptRow>(
      `SELECT id FROM attempt WHERE client_attempt_id = $1 FOR UPDATE`,
      [caid],
    );
    if (dupCheck.rows.length > 0) {
      await client.query("ROLLBACK");
      return {
        kind: "duplicate",
        item: { client_attempt_id: caid, server_attempt_id: dupCheck.rows[0]!.id },
      };
    }

    // 4. Worker existence check
    const workerCheck = await client.query<WorkerRow>(
      `SELECT id FROM worker WHERE id = $1`,
      [attempt.worker_id],
    );
    if (workerCheck.rows.length === 0) {
      await client.query("ROLLBACK");
      return reject(caid, "UNAUTHORIZED", "worker_id not found");
    }

    // 5. Module existence + content_version check
    const moduleCheck = await client.query<ModuleRow>(
      `SELECT id, content_version FROM module WHERE id = $1`,
      [attempt.module_id],
    );
    if (moduleCheck.rows.length === 0) {
      await client.query("ROLLBACK");
      return reject(caid, "UNKNOWN_MODULE", `module_id "${attempt.module_id}" not found`);
    }
    if (moduleCheck.rows[0]!.content_version !== attempt.content_version) {
      await client.query("ROLLBACK");
      return reject(
        caid,
        "UNKNOWN_CONTENT_VERSION",
        `content_version "${attempt.content_version}" does not match module's current version "${moduleCheck.rows[0]!.content_version}"`,
      );
    }

    // 6. MVP: server_score = client_score (rubric re-scoring is a later step)
    const serverScore = attempt.client_score;
    const serverPassed = attempt.passed;

    // 7. Insert attempt
    const insertAttempt = await client.query<AttemptRow>(
      `INSERT INTO attempt
         (client_attempt_id, worker_id, module_id, content_version,
          started_at, completed_at, client_score, server_score,
          passed, status, synced_at)
       VALUES ($1, $2, $3, $4, $5, $6, $7, $8, $9, $10, now())
       RETURNING id`,
      [
        attempt.client_attempt_id,
        attempt.worker_id,
        attempt.module_id,
        attempt.content_version,
        attempt.started_at,
        attempt.completed_at,
        attempt.client_score,
        serverScore,
        serverPassed,
        attempt.status,
      ],
    );
    const serverAttemptId = insertAttempt.rows[0]!.id;

    // 8. Insert events (sorted by sequence for consistency)
    const sortedEvents = [...attempt.events].sort((a, b) => a.sequence - b.sequence);
    for (const ev of sortedEvents) {
      await client.query(
        `INSERT INTO attempt_event
           (attempt_id, client_event_id, seq, event_type, payload, occurred_at)
         VALUES ($1, $2, $3, $4, $5, $6)`,
        [
          serverAttemptId,
          ev.client_event_id,
          ev.sequence,
          ev.event_type,
          JSON.stringify(ev.payload),
          ev.occurred_at,
        ],
      );
    }

    await client.query("COMMIT");

    return {
      kind: "accepted",
      item: {
        client_attempt_id: caid,
        server_attempt_id: serverAttemptId,
        server_score: serverScore,
        server_passed: serverPassed,
      },
    };
  } catch (err) {
    try { await client.query("ROLLBACK"); } catch { /* ignore rollback error */ }
    const msg = err instanceof Error ? err.message : "Unexpected database error";
    return reject(caid, "SCHEMA_INVALID", `Database error: ${msg}`);
  } finally {
    client.release();
  }
}

function reject(clientAttemptId: string, errorCode: string, message: string): AttemptOutcome {
  return {
    kind: "rejected",
    item: { client_attempt_id: clientAttemptId, error_code: errorCode, message },
  };
}
