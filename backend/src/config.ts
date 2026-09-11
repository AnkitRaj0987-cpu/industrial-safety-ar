export type AppConfig = {
  host: string;
  port: number;
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
  return { host, port };
}
