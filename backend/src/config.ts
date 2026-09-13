export type AppConfig = {
  host: string;
  port: number;
  /** Present when DATABASE_URL is set in the environment. Undefined otherwise. */
  databaseUrl: string | undefined;
  /**
   * Returns DATABASE_URL or throws if it is absent.
   * Call this only from modules that actually need database access —
   * not during server startup, so GET /health works without a DB.
   */
  requireDatabaseUrl(): string;
  /**
   * Base URL used to construct certificate verification URLs.
   * e.g. "https://sih2026.example.com" → "https://sih2026.example.com/verify/<uuid>"
   * Defaults to "http://localhost:3000" when PUBLIC_BASE_URL is not set.
   */
  publicBaseUrl: string;
};

function parsePort(raw: string): number {
  if (!/^\d+$/.test(raw)) {
    throw new Error(`Invalid PORT "${raw}": expected an integer between 1 and 65535`);
  }
  const port = Number.parseInt(raw, 10);
  if (port < 1 || port > 65535) {
    throw new Error(`Invalid PORT "${raw}": expected an integer between 1 and 65535`);
  }
  return port;
}

export function loadConfig(env: NodeJS.ProcessEnv): AppConfig {
  const host = env.HOST ?? "0.0.0.0";
  if (host.trim() === "") {
    throw new Error("Invalid HOST: value must be a non-empty bind address");
  }

  const port = parsePort(env.PORT ?? "3000");
  const databaseUrl = env.DATABASE_URL ?? undefined;
  const publicBaseUrl = (env.PUBLIC_BASE_URL ?? "").trim() || "http://localhost:3000";

  return {
    host,
    port,
    databaseUrl,
    publicBaseUrl,
    requireDatabaseUrl(): string {
      if (databaseUrl === undefined || databaseUrl.trim() === "") {
        throw new Error(
          "DATABASE_URL is not set. Add it to your .env file (see .env.example).",
        );
      }
      return databaseUrl;
    },
  };
}
