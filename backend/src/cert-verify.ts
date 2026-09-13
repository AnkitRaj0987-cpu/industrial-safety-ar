/**
 * Certificate public verification — GET /v1/certificates/verify/:publicId
 *
 * Looks up a certificate by its unguessable public_id and returns the
 * public verification shape defined in docs/contracts/certificate-verify.schema.json.
 *
 * Rules:
 *  - DB status 'active'   → contract status 'valid'
 *  - DB status 'revoked'  → contract status 'revoked'
 *  - Not found            → contract status 'not_found'
 *
 * Never exposes: internal id, worker_id, attempt_id, signature, pin_hash,
 * or any other non-public field.
 */

import type { Pool } from "pg";

// ---------------------------------------------------------------------------
// Contract response type — mirrors certificate-verify.schema.json
// ---------------------------------------------------------------------------

/** Returned when the public_id is not found. */
export type CertVerifyNotFound = {
  schema_version: "1.0.0";
  status: "not_found";
  public_id?: string; // optional echo of the queried id
};

/** Returned for an active (valid) certificate. */
export type CertVerifyValid = {
  schema_version: "1.0.0";
  status: "valid";
  public_id: string;
  worker_display_name: string;
  module_id: string;
  module_title_key: string;
  content_version: string;
  score: number;
  passed: true;
  issued_at: string;
  verification: {
    verification_url: string;
    verified_at: string;
  };
};

/** Returned for a revoked certificate. */
export type CertVerifyRevoked = {
  schema_version: "1.0.0";
  status: "revoked";
  public_id: string;
  worker_display_name: string;
  module_id: string;
  module_title_key: string;
  content_version: string;
  score: number;
  passed: boolean;
  issued_at: string;
  revoked_at: string;
  verification: {
    verification_url: string;
    verified_at: string;
  };
};

export type CertVerifyResponse = CertVerifyNotFound | CertVerifyValid | CertVerifyRevoked;

// ---------------------------------------------------------------------------
// Internal DB row shape — fields from the JOIN query, not exposed directly
// ---------------------------------------------------------------------------
type CertRow = {
  public_id: string;
  score: string;          // NUMERIC comes back as string from pg
  status: string;         // 'active' | 'revoked'
  issued_at: Date;
  revoked_at: Date | null;
  worker_display_name: string;
  module_id: string;
  module_title_key: string;
  content_version: string;
};

// ---------------------------------------------------------------------------
// Public lookup function
// ---------------------------------------------------------------------------

/**
 * Looks up a certificate by public_id using the provided pool.
 * Returns the contract-shaped verification response.
 * Never throws for not-found — that is a normal outcome.
 * Throws on connection / query errors so the caller can return 502/503.
 */
export async function lookupCertificate(
  publicId: string,
  pool: Pool,
  publicBaseUrl: string,
): Promise<CertVerifyResponse> {
  const result = await pool.query<CertRow>(
    `SELECT
       c.public_id,
       c.score,
       c.status,
       c.issued_at,
       c.revoked_at,
       w.display_name  AS worker_display_name,
       m.id            AS module_id,
       m.title_key     AS module_title_key,
       m.content_version
     FROM certificate c
     JOIN worker w ON w.id = c.worker_id
     JOIN module  m ON m.id = c.module_id
     WHERE c.public_id = $1`,
    [publicId],
  );

  if (result.rows.length === 0) {
    return { schema_version: "1.0.0", status: "not_found", public_id: publicId };
  }

  const row = result.rows[0]!;
  const verifiedAt = new Date().toISOString();
  const verificationUrl = buildVerificationUrl(publicBaseUrl, row.public_id);
  const issuedAt = row.issued_at instanceof Date
    ? row.issued_at.toISOString()
    : String(row.issued_at);
  const score = parseFloat(row.score);

  if (row.status === "active") {
    const resp: CertVerifyValid = {
      schema_version: "1.0.0",
      status: "valid",
      public_id: row.public_id,
      worker_display_name: row.worker_display_name,
      module_id: row.module_id,
      module_title_key: row.module_title_key,
      content_version: row.content_version,
      score,
      passed: true,
      issued_at: issuedAt,
      verification: { verification_url: verificationUrl, verified_at: verifiedAt },
    };
    return resp;
  }

  // status === 'revoked'
  const revokedAt = row.revoked_at instanceof Date
    ? row.revoked_at.toISOString()
    : String(row.revoked_at ?? "");

  const resp: CertVerifyRevoked = {
    schema_version: "1.0.0",
    status: "revoked",
    public_id: row.public_id,
    worker_display_name: row.worker_display_name,
    module_id: row.module_id,
    module_title_key: row.module_title_key,
    content_version: row.content_version,
    score,
    passed: false,
    issued_at: issuedAt,
    revoked_at: revokedAt,
    verification: { verification_url: verificationUrl, verified_at: verifiedAt },
  };
  return resp;
}

function buildVerificationUrl(publicBaseUrl: string, publicId: string): string {
  return `${publicBaseUrl.replace(/\/$/, "")}/verify/${publicId}`;
}
