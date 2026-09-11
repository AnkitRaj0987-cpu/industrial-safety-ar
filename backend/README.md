# Backend — SIH 2026 PS 26041

Minimal Node.js + TypeScript + Fastify HTTP foundation.

This package does **not** implement PostgreSQL, auth, sync, scoring, or certificates. Shared API payloads live in [`../docs/contracts/`](../docs/contracts/) and must not be duplicated here.

## Runtime

- Node.js 20+
- **ESM** (`"type": "module"` in `package.json`, TypeScript `module`/`moduleResolution`: `Node16`)
- Fastify logger only (no extra logging stack)

## Scripts

| Script | Command | Purpose |
| --- | --- | --- |
| Development | `npm run dev` | Run `src/server.ts` with reload (`tsx watch`) |
| Type check | `npm run typecheck` | `tsc --noEmit` |
| Build | `npm run build` | Compile to `dist/` |
| Start | `npm start` | Run compiled `dist/server.js` |
| Health test | `npm test` | In-process `GET /health` via Fastify inject |

## Configuration

Copy `.env.example` to `.env` locally if you want a file of values. The process currently reads **only** `HOST` and `PORT` from the environment (defaults: `0.0.0.0` and `3000`). Do not commit `.env`.

Placeholders in `.env.example` (`DATABASE_URL`, `JWT_SECRET`, `CERT_HMAC_SECRET`, `CORS_ORIGIN`) are for later phases and are not loaded.

## Health

`GET /health` → `200` `{ "status": "ok" }`

No database field: PostgreSQL is not part of this foundation.
