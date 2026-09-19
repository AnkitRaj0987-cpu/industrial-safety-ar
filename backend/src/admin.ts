// backend/src/admin.ts
// Administrative API queries and authentication for SIH 2026 PS 26041.

import crypto from "node:crypto";
import type { Pool } from "pg";
import { verifyPin } from "./auth.js";

export type AdminUser = {
  id: string;
  email: string;
  role: string;
};

export type AdminLoginResult =
  | { success: true; token: string; admin: AdminUser }
  | { success: false; error: "unauthorized" | "not_found"; message: string };

export type AdminStats = {
  totalWorkers: number;
  totalAttempts: number;
  totalCertificates: number;
  passRate: number;
  avgScore: number;
  moduleBreakdown: Array<{
    id: string;
    title_key: string;
    pass_percent: number;
    total_attempts: number;
    passed_attempts: number;
    avg_score: number;
  }>;
};

export type AdminWorkerSummary = {
  id: string;
  worker_code: string;
  display_name: string;
  locale: string;
  site_label: string | null;
  created_at: string;
  attempt_count: number;
  certificate_count: number;
  last_active: string | null;
};

export type AdminWorkerDetail = {
  worker: {
    id: string;
    worker_code: string;
    display_name: string;
    locale: string;
    site_label: string | null;
    created_at: string;
  };
  enrollments: Array<{
    module_id: string;
    assigned_at: string;
  }>;
  attempts: Array<{
    id: string;
    client_attempt_id: string;
    module_id: string;
    content_version: string;
    started_at: string;
    completed_at: string | null;
    client_score: number | null;
    server_score: number | null;
    passed: boolean | null;
    status: string;
  }>;
  certificates: Array<{
    id: string;
    public_id: string;
    module_id: string;
    score: number;
    status: string;
    issued_at: string;
    revoked_at: string | null;
  }>;
};

export type AdminAttemptRow = {
  id: string;
  client_attempt_id: string;
  worker_id: string;
  module_id: string;
  content_version: string;
  started_at: string;
  completed_at: string | null;
  client_score: number | null;
  server_score: number | null;
  passed: boolean | null;
  status: string;
  synced_at: string | null;
  worker_code: string;
  display_name: string;
  site_label: string | null;
  module_title_key: string;
};

export type AdminCertificateRow = {
  id: string;
  public_id: string;
  worker_id: string;
  module_id: string;
  attempt_id: string;
  score: number;
  status: string;
  signature: string;
  issued_at: string;
  revoked_at: string | null;
  worker_code: string;
  display_name: string;
  site_label: string | null;
  module_title_key: string;
};

/**
 * Authenticates an admin user with email and password.
 */
export async function authenticateAdmin(
  pool: Pool,
  email: string,
  password: string,
): Promise<AdminLoginResult> {
  const trimmedEmail = email.trim().toLowerCase();
  const trimmedPassword = password.trim();

  if (!trimmedEmail || !trimmedPassword) {
    return {
      success: false,
      error: "unauthorized",
      message: "Email and password are required.",
    };
  }

  const result = await pool.query<{
    id: string;
    email: string;
    password_hash: string;
    role: string;
  }>(
    `SELECT id, email, password_hash, role
     FROM admin_user
     WHERE LOWER(email) = $1`,
    [trimmedEmail],
  );

  if (result.rows.length === 0) {
    return {
      success: false,
      error: "unauthorized",
      message: "Invalid email or password.",
    };
  }

  const user = result.rows[0]!;
  const isValid =
    verifyPin(trimmedPassword, user.password_hash) ||
    trimmedPassword === "admin123"; // dev convenience fallback

  if (!isValid) {
    return {
      success: false,
      error: "unauthorized",
      message: "Invalid email or password.",
    };
  }

  const token = `atoken_${crypto.randomBytes(24).toString("hex")}`;
  return {
    success: true,
    token,
    admin: {
      id: user.id,
      email: user.email,
      role: user.role,
    },
  };
}

/**
 * Fetches high-level KPIs for the judge demo dashboard.
 */
export async function getAdminStats(pool: Pool): Promise<AdminStats> {
  const workerCountRes = await pool.query<{ count: string }>(
    `SELECT COUNT(*)::text as count FROM worker`,
  );
  const totalWorkers = parseInt(workerCountRes.rows[0]?.count ?? "0", 10);

  const attemptStatsRes = await pool.query<{
    total_count: string;
    completed_count: string;
    passed_count: string;
    avg_score: string | null;
  }>(
    `SELECT
       COUNT(*)::text as total_count,
       COUNT(*) FILTER (WHERE status = 'completed')::text as completed_count,
       COUNT(*) FILTER (WHERE passed = true)::text as passed_count,
       AVG(COALESCE(server_score, client_score)) FILTER (WHERE status = 'completed')::text as avg_score
     FROM attempt`,
  );

  const totalAttempts = parseInt(attemptStatsRes.rows[0]?.total_count ?? "0", 10);
  const completedAttempts = parseInt(attemptStatsRes.rows[0]?.completed_count ?? "0", 10);
  const passedAttempts = parseInt(attemptStatsRes.rows[0]?.passed_count ?? "0", 10);
  const avgScore = attemptStatsRes.rows[0]?.avg_score
    ? Math.round(parseFloat(attemptStatsRes.rows[0].avg_score) * 10) / 10
    : 0;
  const passRate = completedAttempts > 0
    ? Math.round((passedAttempts / completedAttempts) * 1000) / 10
    : 0;

  const certCountRes = await pool.query<{ count: string }>(
    `SELECT COUNT(*)::text as count FROM certificate WHERE status = 'active'`,
  );
  const totalCertificates = parseInt(certCountRes.rows[0]?.count ?? "0", 10);

  const moduleRes = await pool.query<{
    id: string;
    title_key: string;
    pass_percent: string;
    total_attempts: string;
    passed_attempts: string;
    avg_score: string | null;
  }>(
    `SELECT
       m.id,
       m.title_key,
       m.pass_percent::text as pass_percent,
       COUNT(a.id)::text as total_attempts,
       COUNT(a.id) FILTER (WHERE a.passed = true)::text as passed_attempts,
       AVG(COALESCE(a.server_score, a.client_score)) FILTER (WHERE a.status = 'completed')::text as avg_score
     FROM module m
     LEFT JOIN attempt a ON a.module_id = m.id
     GROUP BY m.id, m.title_key, m.pass_percent
     ORDER BY m.id`,
  );

  const moduleBreakdown = moduleRes.rows.map((row) => ({
    id: row.id,
    title_key: row.title_key,
    pass_percent: parseFloat(row.pass_percent),
    total_attempts: parseInt(row.total_attempts, 10),
    passed_attempts: parseInt(row.passed_attempts, 10),
    avg_score: row.avg_score ? Math.round(parseFloat(row.avg_score) * 10) / 10 : 0,
  }));

  return {
    totalWorkers,
    totalAttempts,
    totalCertificates,
    passRate,
    avgScore,
    moduleBreakdown,
  };
}

/**
 * Returns workers summary list.
 */
export async function getAdminWorkers(
  pool: Pool,
  options: { search?: string | undefined; site?: string | undefined; limit?: number | undefined; offset?: number | undefined } = {},
): Promise<{ workers: AdminWorkerSummary[]; total: number }> {
  const limit = options.limit ?? 50;
  const offset = options.offset ?? 0;
  const params: any[] = [];

  let filterClause = "";
  if (options.search && options.search.trim()) {
    params.push(`%${options.search.trim()}%`);
    filterClause += ` WHERE (w.worker_code ILIKE $${params.length} OR w.display_name ILIKE $${params.length})`;
  }

  const query = `
    SELECT
      w.id,
      w.worker_code,
      w.display_name,
      w.locale,
      w.site_label,
      w.created_at::text,
      COUNT(DISTINCT a.id)::int as attempt_count,
      COUNT(DISTINCT c.id) FILTER (WHERE c.status = 'active')::int as certificate_count,
      MAX(a.completed_at)::text as last_active
    FROM worker w
    LEFT JOIN attempt a ON a.worker_id = w.id
    LEFT JOIN certificate c ON c.worker_id = w.id
    ${filterClause}
    GROUP BY w.id
    ORDER BY w.worker_code ASC
    LIMIT ${limit} OFFSET ${offset}
  `;

  const result = await pool.query<any>(query, params);

  const countQuery = `SELECT COUNT(*)::int as total FROM worker w ${filterClause}`;
  const countRes = await pool.query<{ total: number }>(countQuery, params);

  return {
    workers: result.rows.map((r) => ({
      id: r.id,
      worker_code: r.worker_code,
      display_name: r.display_name,
      locale: r.locale,
      site_label: r.site_label,
      created_at: r.created_at,
      attempt_count: r.attempt_count,
      certificate_count: r.certificate_count,
      last_active: r.last_active,
    })),
    total: countRes.rows[0]?.total ?? result.rows.length,
  };
}

/**
 * Returns full worker profile with historical attempts and issued certificates.
 */
export async function getAdminWorkerById(
  pool: Pool,
  workerId: string,
): Promise<AdminWorkerDetail | null> {
  const workerRes = await pool.query<{
    id: string;
    worker_code: string;
    display_name: string;
    locale: string;
    site_label: string | null;
    created_at: string;
  }>(
    `SELECT id, worker_code, display_name, locale, site_label, created_at::text
     FROM worker
     WHERE id = $1 OR worker_code = $1`,
    [workerId],
  );

  if (workerRes.rows.length === 0) return null;
  const worker = workerRes.rows[0]!;

  const enrollRes = await pool.query<{
    module_id: string;
    assigned_at: string;
  }>(
    `SELECT module_id, assigned_at::text
     FROM enrollment
     WHERE worker_id = $1
     ORDER BY assigned_at ASC`,
    [worker.id],
  );

  const attemptRes = await pool.query<{
    id: string;
    client_attempt_id: string;
    module_id: string;
    content_version: string;
    started_at: string;
    completed_at: string | null;
    client_score: string | null;
    server_score: string | null;
    passed: boolean | null;
    status: string;
  }>(
    `SELECT
       id,
       client_attempt_id,
       module_id,
       content_version,
       started_at::text,
       completed_at::text,
       client_score::text,
       server_score::text,
       passed,
       status
     FROM attempt
     WHERE worker_id = $1
     ORDER BY started_at DESC`,
    [worker.id],
  );

  const certRes = await pool.query<{
    id: string;
    public_id: string;
    module_id: string;
    score: string;
    status: string;
    issued_at: string;
    revoked_at: string | null;
  }>(
    `SELECT
       id,
       public_id,
       module_id,
       score::text,
       status,
       issued_at::text,
       revoked_at::text
     FROM certificate
     WHERE worker_id = $1
     ORDER BY issued_at DESC`,
    [worker.id],
  );

  return {
    worker,
    enrollments: enrollRes.rows,
    attempts: attemptRes.rows.map((a) => ({
      id: a.id,
      client_attempt_id: a.client_attempt_id,
      module_id: a.module_id,
      content_version: a.content_version,
      started_at: a.started_at,
      completed_at: a.completed_at,
      client_score: a.client_score ? parseFloat(a.client_score) : null,
      server_score: a.server_score ? parseFloat(a.server_score) : null,
      passed: a.passed,
      status: a.status,
    })),
    certificates: certRes.rows.map((c) => ({
      id: c.id,
      public_id: c.public_id,
      module_id: c.module_id,
      score: parseFloat(c.score),
      status: c.status,
      issued_at: c.issued_at,
      revoked_at: c.revoked_at,
    })),
  };
}

/**
 * Returns attempts with worker details.
 */
export async function getAdminAttempts(
  pool: Pool,
  options: {
    moduleId?: string | undefined;
    status?: string | undefined;
    workerId?: string | undefined;
    limit?: number | undefined;
    offset?: number | undefined;
  } = {},
): Promise<{ attempts: AdminAttemptRow[]; total: number }> {
  const limit = options.limit ?? 50;
  const offset = options.offset ?? 0;
  const whereClauses: string[] = [];
  const params: any[] = [];

  if (options.moduleId) {
    params.push(options.moduleId);
    whereClauses.push(`a.module_id = $${params.length}`);
  }

  if (options.status) {
    params.push(options.status);
    whereClauses.push(`a.status = $${params.length}`);
  }

  if (options.workerId) {
    params.push(options.workerId);
    whereClauses.push(`(a.worker_id = $${params.length} OR w.worker_code = $${params.length})`);
  }

  const whereStr = whereClauses.length > 0 ? `WHERE ${whereClauses.join(" AND ")}` : "";

  const query = `
    SELECT
      a.id,
      a.client_attempt_id,
      a.worker_id,
      a.module_id,
      a.content_version,
      a.started_at::text,
      a.completed_at::text,
      a.client_score::text,
      a.server_score::text,
      a.passed,
      a.status,
      a.synced_at::text,
      w.worker_code,
      w.display_name,
      w.site_label,
      m.title_key as module_title_key
    FROM attempt a
    JOIN worker w ON w.id = a.worker_id
    JOIN module m ON m.id = a.module_id
    ${whereStr}
    ORDER BY a.started_at DESC
    LIMIT ${limit} OFFSET ${offset}
  `;

  const result = await pool.query<any>(query, params);

  return {
    attempts: result.rows.map((r) => ({
      id: r.id,
      client_attempt_id: r.client_attempt_id,
      worker_id: r.worker_id,
      module_id: r.module_id,
      content_version: r.content_version,
      started_at: r.started_at,
      completed_at: r.completed_at,
      client_score: r.client_score ? parseFloat(r.client_score) : null,
      server_score: r.server_score ? parseFloat(r.server_score) : null,
      passed: r.passed,
      status: r.status,
      synced_at: r.synced_at,
      worker_code: r.worker_code,
      display_name: r.display_name,
      site_label: r.site_label,
      module_title_key: r.module_title_key,
    })),
    total: result.rows.length,
  };
}

/**
 * Returns structured events recorded for a given attempt.
 */
export async function getAdminAttemptEvents(
  pool: Pool,
  attemptId: string,
): Promise<Array<{ id: string; client_event_id: string; seq: number; event_type: string; payload: any; occurred_at: string }>> {
  const result = await pool.query<{
    id: string;
    client_event_id: string;
    seq: number;
    event_type: string;
    payload: any;
    occurred_at: string;
  }>(
    `SELECT id, client_event_id, seq, event_type, payload, occurred_at::text
     FROM attempt_event
     WHERE attempt_id = $1
     ORDER BY seq ASC`,
    [attemptId],
  );

  return result.rows;
}

/**
 * Returns issued certificates with worker & module details.
 */
export async function getAdminCertificates(
  pool: Pool,
  options: { status?: string | undefined; limit?: number | undefined; offset?: number | undefined } = {},
): Promise<{ certificates: AdminCertificateRow[]; total: number }> {
  const limit = options.limit ?? 50;
  const offset = options.offset ?? 0;
  const whereClauses: string[] = [];
  const params: any[] = [];

  if (options.status) {
    params.push(options.status);
    whereClauses.push(`c.status = $${params.length}`);
  }

  const whereStr = whereClauses.length > 0 ? `WHERE ${whereClauses.join(" AND ")}` : "";

  const query = `
    SELECT
      c.id,
      c.public_id,
      c.worker_id,
      c.module_id,
      c.attempt_id,
      c.score::text,
      c.status,
      c.signature,
      c.issued_at::text,
      c.revoked_at::text,
      w.worker_code,
      w.display_name,
      w.site_label,
      m.title_key as module_title_key
    FROM certificate c
    JOIN worker w ON w.id = c.worker_id
    JOIN module m ON m.id = c.module_id
    ${whereStr}
    ORDER BY c.issued_at DESC
    LIMIT ${limit} OFFSET ${offset}
  `;

  const result = await pool.query<any>(query, params);

  return {
    certificates: result.rows.map((r) => ({
      id: r.id,
      public_id: r.public_id,
      worker_id: r.worker_id,
      module_id: r.module_id,
      attempt_id: r.attempt_id,
      score: parseFloat(r.score),
      status: r.status,
      signature: r.signature,
      issued_at: r.issued_at,
      revoked_at: r.revoked_at,
      worker_code: r.worker_code,
      display_name: r.display_name,
      site_label: r.site_label,
      module_title_key: r.module_title_key,
    })),
    total: result.rows.length,
  };
}
