# Architecture pointer — SIH 2026 PS 26041

AR-Based Vocational Training Simulator for Industrial Safety in Jharkhand's Mining & Manufacturing Sector.

This document is a concise pointer for implementation agents. It is not the full architecture proposal. **Shared data shapes live only in** [`docs/contracts/`](contracts/).

## Major components

| Area | Intended path | Role |
| --- | --- | --- |
| Worker AR application | `worker-app/` | Unity Android client: bundled AR training, local assessment, SQLite outbox |
| Backend API | `backend/` | Auth, validation, deterministic re-score, persistence, certificate issue, sync |
| Database | `database/` | PostgreSQL schema and migrations aligned to contracts |
| Admin dashboard | `admin-dashboard/` | Supervisors/admins: progress, attempts, certificates (no AR runtime) |
| Contracts | `docs/contracts/` | Versioned JSON Schema (Draft 2020-12) — source of truth |

## Responsibilities

**Worker app**

- Run Fire and Gas training fully **offline** from bundled content.
- Execute a data-driven step graph from `module.schema.json`.
- Emit structured attempt events (`attempt-event.schema.json`); never score from camera frames or screenshots.
- Compute a **client** score using the matching rubric `content_version`.
- Persist attempts and a pending certificate **draft** locally; enqueue completed attempts in SQLite outbox.
- Sync via `POST /v1/sync` when online; acknowledge outbox only for `accepted` or `duplicates`.
- Replace a local pending draft with the **server-issued** certificate from the sync response.

**Backend**

- Accept retry-safe `POST /v1/sync` payloads (`sync-request.schema.json` / `sync-response.schema.json`).
- Deduplicate on `client_attempt_id` / `client_event_id`.
- Re-score against the rubric for the attempt's `content_version`. Server score is canonical.
- Persist attempts, events, and certificates. Issue certificates only for qualifying **server** passes.
- Serve public certificate verification. Never treat a client pending draft as a valid public certificate.

**Database**

- Store workers, modules/rubrics (or references), attempts, events, sync acknowledgements, certificates.
- Enforce uniqueness of `client_attempt_id` and `client_event_id` so retries cannot create duplicate history.
- Retakes are **new** attempts, never in-place overwrites.

**Admin dashboard**

- Read server-canonical progress, scores, and certificate status.
- Support verification UX using public certificate payloads (`certificate-verify.schema.json`).
- Do not implement Unity/AR or invent parallel data models.

## Contract-first development

1. Change a shared field in `docs/contracts/*.schema.json` first.
2. Bump **schema_version** (instance) and the schema `$id` path (`/v1/` → `/v2/` on breaking change).
3. Independently update Unity, backend, database, and dashboard to the same schemas.
4. Do **not** create a shared TypeScript package for Unity. Unity and backend consume JSON independently.

**Do not confuse:**

- **schema_version** — which JSON contract the document conforms to (for example `1.0.0`).
- **content_version** — which authored training/rubric pack the worker actually ran. Attempts **must** store this so the server can re-score against the same pack.

## Offline-first rule

No contract requires network connectivity for core training.

```
Offline:  train → assess from events → client score → local result → SQLite outbox
Online:   outbox → POST /v1/sync → validate → PostgreSQL → ack → certificate if pass
          → client stores canonical certificate
```

Core training, assessment, and local results must work with **zero** connectivity.

## Canonical sync

- **POST `/v1/sync`**
- MVP: one outbox item = one **completed** attempt **plus** its events (no per-event HTTP).
- Retries may resubmit the same payload. Server identity for dedup: `client_attempt_id`, `client_event_id`.
- Client deletes/acknowledges outbox rows only when the item is in `accepted[]` or `duplicates[]`. `rejected[]` stays queued or is surfaced for repair — it is not an acknowledgement.

## Certificates

- QR encodes a **verification URL** of the form `https://<public-host>/verify/{public_id}`. Host is environment-specific; **schemas do not hardcode a production domain**.
- Public verify payload: display name, module, score, pass/status, issued time, certificate status (`valid` | `revoked` | `not_found`). No secrets, government IDs, or other unnecessary PII.
- Server is authoritative. Local pending drafts are not `valid` public certificates.

## AR / content constraints

- **No Cloud Anchors.**
- **No runtime AR asset downloads.** Modules, locales, VFX cue **identifiers**, and AR assets ship with the client (or an explicit offline pack). `vfx_cue` in contracts is a string id, not a downloadable asset.
- Assessment is event-based, not visual.

## MVP boundary

Exactly two training modules in MVP (schemas remain domain-extensible):

1. Fire & Explosion Response
2. Gas Leak & Confined Space Safety

Future domains (for example machinery safety) add new `module_id` / `domain` values and content packs without encoding Fire/Gas logic into the schemas.

## AI agent ownership

| Agent | Owns | Must not |
| --- | --- | --- |
| Contract / architecture | `docs/ARCHITECTURE.md`, `docs/contracts/` | Unity, API routes, migrations, scoring code |
| Worker / Unity | `worker-app/` implementing contracts | Inventing competing JSON shapes |
| Backend | `backend/` sync, re-score, cert issue | Unity/AR, dashboard UI |
| Database | `database/` PostgreSQL aligned to contracts | App business logic beyond schema |
| Dashboard | `admin-dashboard/` | AR client, inventing sync contracts |

Independent agents must implement the **same** schemas; they must not fork undocumented fields (`additionalProperties` is false on closed objects).
