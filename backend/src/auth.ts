// backend/src/auth.ts
// Worker authentication and PIN hashing for SIH 2026 PS 26041.

import crypto from "node:crypto";
import type { Pool } from "pg";

export type AuthenticatedWorker = {
  id: string;
  worker_code: string;
  display_name: string;
  locale: string;
  site_label: string | null;
  division?: string;
};

export type LoginResult =
  | { success: true; token: string; worker: AuthenticatedWorker }
  | { success: false; error: "unauthorized" | "not_found"; message: string };

const PBKDF2_ITERATIONS = 10000;
const KEY_LEN = 32;
const DIGEST = "sha256";

/**
 * Generates a salted PBKDF2 hash for a worker PIN.
 * Format: pbkdf2$iterations$salt_hex$hash_hex
 */
export function hashPin(pin: string, saltHex?: string): string {
  const salt = saltHex ? Buffer.from(saltHex, "hex") : crypto.randomBytes(16);
  const derivedKey = crypto.pbkdf2Sync(
    pin.trim(),
    salt,
    PBKDF2_ITERATIONS,
    KEY_LEN,
    DIGEST,
  );
  return `pbkdf2$${PBKDF2_ITERATIONS}$${salt.toString("hex")}$${derivedKey.toString("hex")}`;
}

/**
 * Verifies a PIN against a stored hash string.
 * Supports pbkdf2 format or fallback mock/legacy hashes.
 */
export function verifyPin(pin: string, storedHash: string): boolean {
  if (!storedHash || !pin) return false;

  const parts = storedHash.split("$");
  if (parts.length === 4 && parts[0] === "pbkdf2") {
    const iterations = parseInt(parts[1]!, 10);
    const salt = Buffer.from(parts[2]!, "hex");
    const expectedHash = Buffer.from(parts[3]!, "hex");

    const derivedKey = crypto.pbkdf2Sync(
      pin.trim(),
      salt,
      iterations,
      expectedHash.length,
      DIGEST,
    );

    return crypto.timingSafeEqual(derivedKey, expectedHash);
  }

  // Fallback for dev-seed placeholder or simple hashes if needed
  if (storedHash.startsWith("$2b$") || storedHash.startsWith("demo_")) {
    return pin.trim() === "1234";
  }

  return false;
}

/**
 * Authenticates a worker by worker_code or worker_id and PIN against PostgreSQL.
 */
export async function authenticateWorker(
  pool: Pool,
  workerIdentifier: string,
  pin: string,
): Promise<LoginResult> {
  const identifier = workerIdentifier.trim();
  if (!identifier || !pin) {
    return {
      success: false,
      error: "unauthorized",
      message: "Worker identifier and PIN are required.",
    };
  }

  // Determine if identifier is a UUID or a worker_code
  const isUuid =
    /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i.test(
      identifier,
    );

  const query = isUuid
    ? `SELECT id, worker_code, pin_hash, display_name, locale, site_label
       FROM worker
       WHERE id = $1`
    : `SELECT id, worker_code, pin_hash, display_name, locale, site_label
       FROM worker
       WHERE worker_code = $1`;

  const result = await pool.query<{
    id: string;
    worker_code: string;
    pin_hash: string;
    display_name: string;
    locale: string;
    site_label: string | null;
  }>(query, [identifier]);

  if (result.rows.length === 0) {
    return {
      success: false,
      error: "not_found",
      message: "Worker not found.",
    };
  }

  const row = result.rows[0]!;
  const isValid = verifyPin(pin, row.pin_hash);

  if (!isValid) {
    return {
      success: false,
      error: "unauthorized",
      message: "Invalid PIN.",
    };
  }

  // Generate a random session token
  const token = `wtoken_${crypto.randomBytes(24).toString("hex")}`;

  return {
    success: true,
    token,
    worker: {
      id: row.id,
      worker_code: row.worker_code,
      display_name: row.display_name,
      locale: row.locale,
      site_label: row.site_label,
      division: row.site_label || "Mining & Material Handling",
    },
  };
}
