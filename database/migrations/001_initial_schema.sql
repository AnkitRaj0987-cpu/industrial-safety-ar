-- =============================================================================
-- Migration: 001_initial_schema.sql
-- Project  : SIH 2026 PS 26041
--            AR-Based Vocational Training Simulator — Industrial Safety
-- Created  : STEP 6B
-- PostgreSQL: 16+
--
-- Applies the initial schema for all server-side entities.
-- gen_random_uuid() is available built-in from PostgreSQL 13+; no extension needed.
--
-- Run once against an empty (freshly created) database:
--   psql -U <user> -d industrial_safety_ar -f 001_initial_schema.sql
--
-- This migration is idempotent via IF NOT EXISTS guards.
-- It contains NO DROP statements and NO destructive changes.
-- =============================================================================

BEGIN;

-- ---------------------------------------------------------------------------
-- 1. AdminUser
--    Stores dashboard administrators and viewers.
--    Passwords are hashed by the application (bcrypt/argon2); never plaintext.
-- ---------------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS admin_user (
    id            UUID        NOT NULL DEFAULT gen_random_uuid() PRIMARY KEY,
    email         TEXT        NOT NULL,
    password_hash TEXT        NOT NULL,
    role          TEXT        NOT NULL
                              CHECK (role IN ('admin', 'viewer')),
    created_at    TIMESTAMPTZ NOT NULL DEFAULT now()
);

-- Unique email per admin account.
CREATE UNIQUE INDEX IF NOT EXISTS uidx_admin_user_email
    ON admin_user (email);

-- ---------------------------------------------------------------------------
-- 2. Worker
--    Registered industrial workers who take AR training modules.
--    PINs are hashed by the application; never stored plaintext.
--    locale follows BCP-47 (e.g. 'en', 'hi', 'sa').
-- ---------------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS worker (
    id           UUID        NOT NULL DEFAULT gen_random_uuid() PRIMARY KEY,
    worker_code  TEXT        NOT NULL,
    pin_hash     TEXT        NOT NULL,
    display_name TEXT        NOT NULL,
    locale       TEXT        NOT NULL DEFAULT 'en',
    site_label   TEXT        NULL,
    created_at   TIMESTAMPTZ NOT NULL DEFAULT now()
);

-- Unique worker code (the on-device login identifier).
CREATE UNIQUE INDEX IF NOT EXISTS uidx_worker_code
    ON worker (worker_code);

-- ---------------------------------------------------------------------------
-- 3. Module
--    Training module definitions. The two MVP modules are inserted via seed.
--    Additional modules can be added without schema changes.
-- ---------------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS module (
    id              TEXT        NOT NULL PRIMARY KEY,  -- e.g. 'fire-explosion-response'
    title_key       TEXT        NOT NULL,              -- i18n key, e.g. 'module.fire_explosion.title'
    pass_percent    NUMERIC(6,2) NOT NULL
                                CHECK (pass_percent >= 0 AND pass_percent <= 100),
    content_version TEXT        NOT NULL,              -- semver, e.g. '1.0.0'
    created_at      TIMESTAMPTZ NOT NULL DEFAULT now()
);

-- ---------------------------------------------------------------------------
-- 4. Enrollment
--    Many-to-many: a Worker is enrolled in one or more Modules.
--    The same worker cannot be enrolled in the same module twice.
-- ---------------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS enrollment (
    worker_id   UUID        NOT NULL
                            REFERENCES worker (id) ON DELETE CASCADE,
    module_id   TEXT        NOT NULL
                            REFERENCES module (id) ON DELETE CASCADE,
    assigned_at TIMESTAMPTZ NOT NULL DEFAULT now(),
    PRIMARY KEY (worker_id, module_id)
);

-- ---------------------------------------------------------------------------
-- 5. Attempt
--    One worker run of one module. Originated offline; synced to server.
--    client_attempt_id is a client-generated UUID — the sync idempotency key.
--    server_score / client_score are nullable (absent for in_progress attempts).
--    passed is nullable — set only on completion.
--
--    ON DELETE RESTRICT for worker/module: compliance history must not be
--    silently destroyed if a worker or module record is removed.
-- ---------------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS attempt (
    id                UUID        NOT NULL DEFAULT gen_random_uuid() PRIMARY KEY,
    client_attempt_id UUID        NOT NULL,
    worker_id         UUID        NOT NULL
                                  REFERENCES worker (id) ON DELETE RESTRICT,
    module_id         TEXT        NOT NULL
                                  REFERENCES module (id) ON DELETE RESTRICT,
    content_version   TEXT        NOT NULL,
    started_at        TIMESTAMPTZ NOT NULL,
    completed_at      TIMESTAMPTZ NULL,
    client_score      NUMERIC(8,2) NULL
                                   CHECK (client_score >= 0 AND client_score <= 10000),
    server_score      NUMERIC(8,2) NULL
                                   CHECK (server_score >= 0 AND server_score <= 10000),
    passed            BOOLEAN     NULL,
    status            TEXT        NOT NULL
                                  CHECK (status IN ('in_progress', 'completed', 'abandoned')),
    synced_at         TIMESTAMPTZ NULL  -- server timestamp when the sync was accepted
);

-- client_attempt_id is the sync deduplication key — must be globally unique.
CREATE UNIQUE INDEX IF NOT EXISTS uidx_attempt_client_attempt_id
    ON attempt (client_attempt_id);

-- Query pattern: worker's attempts for a module ordered by completion time.
CREATE INDEX IF NOT EXISTS idx_attempt_worker_module_completed
    ON attempt (worker_id, module_id, completed_at);

-- ---------------------------------------------------------------------------
-- 6. AttemptEvent
--    One structured worker action within an attempt.
--    client_event_id is the sync dedup key (per contract).
--    seq is monotonic per attempt, starting at 1.
--    payload is structured JSON evidence — no images or binary blobs.
--
--    ON DELETE CASCADE: events have no meaning without their parent attempt.
-- ---------------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS attempt_event (
    id              UUID        NOT NULL DEFAULT gen_random_uuid() PRIMARY KEY,
    attempt_id      UUID        NOT NULL
                                REFERENCES attempt (id) ON DELETE CASCADE,
    client_event_id UUID        NOT NULL,
    seq             INTEGER     NOT NULL
                                CHECK (seq >= 1 AND seq <= 10000),
    event_type      TEXT        NOT NULL,
    payload         JSONB       NOT NULL DEFAULT '{}',
    occurred_at     TIMESTAMPTZ NOT NULL
);

-- Sync dedup: client_event_id must be globally unique.
CREATE UNIQUE INDEX IF NOT EXISTS uidx_attempt_event_client_event_id
    ON attempt_event (client_event_id);

-- Ordering / scoring query: all events for an attempt in sequence order.
CREATE UNIQUE INDEX IF NOT EXISTS uidx_attempt_event_attempt_seq
    ON attempt_event (attempt_id, seq);

-- ---------------------------------------------------------------------------
-- 7. Certificate
--    Server-issued canonical certificates for qualifying server-scored passes.
--    Local pending drafts on the worker device are NOT represented here.
--
--    public_id is the UUID embedded in QR verification URLs.
--    signature is an HMAC or similar integrity token (populated by app layer).
--    status: 'active' | 'revoked' — no 'pending' or 'draft' at server level.
--
--    Rule: at most ONE active certificate per (worker_id, module_id).
--    Revoked certificates are retained for audit history.
--    ON DELETE RESTRICT: do not destroy certificate history on worker/module removal.
-- ---------------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS certificate (
    id         UUID        NOT NULL DEFAULT gen_random_uuid() PRIMARY KEY,
    public_id  UUID        NOT NULL DEFAULT gen_random_uuid(),
    worker_id  UUID        NOT NULL
                           REFERENCES worker (id) ON DELETE RESTRICT,
    module_id  TEXT        NOT NULL
                           REFERENCES module (id) ON DELETE RESTRICT,
    attempt_id UUID        NOT NULL
                           REFERENCES attempt (id) ON DELETE RESTRICT,
    score      NUMERIC(8,2) NOT NULL
                            CHECK (score >= 0 AND score <= 10000),
    status     TEXT        NOT NULL
                           CHECK (status IN ('active', 'revoked')),
    signature  TEXT        NOT NULL DEFAULT '',  -- populated when cert is issued
    issued_at  TIMESTAMPTZ NOT NULL DEFAULT now(),
    revoked_at TIMESTAMPTZ NULL
);

-- public_id appears in QR codes and public verify URLs — must be unique.
CREATE UNIQUE INDEX IF NOT EXISTS uidx_certificate_public_id
    ON certificate (public_id);

-- Enforce: only one ACTIVE certificate per worker+module combination.
-- Revoked certificates are exempt — they stay for audit history.
CREATE UNIQUE INDEX IF NOT EXISTS uidx_certificate_active_worker_module
    ON certificate (worker_id, module_id)
    WHERE status = 'active';

-- ---------------------------------------------------------------------------
-- 8. AuditLog
--    Append-only log of significant actions by admin users or the system.
--    actor is stored as text (not a FK) so log entries survive user deletion.
--    entity identifies the resource type and ID affected.
-- ---------------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS audit_log (
    id     BIGSERIAL   NOT NULL PRIMARY KEY,
    actor  TEXT        NOT NULL,  -- email or 'system'
    action TEXT        NOT NULL,  -- e.g. 'certificate.revoke', 'worker.create'
    entity TEXT        NOT NULL,  -- e.g. 'certificate:<uuid>'
    at     TIMESTAMPTZ NOT NULL DEFAULT now()
);

-- Fast lookup: all audit events for a given actor.
CREATE INDEX IF NOT EXISTS idx_audit_log_actor
    ON audit_log (actor);

-- Fast lookup: all audit events for a given entity.
CREATE INDEX IF NOT EXISTS idx_audit_log_entity
    ON audit_log (entity);

COMMIT;
