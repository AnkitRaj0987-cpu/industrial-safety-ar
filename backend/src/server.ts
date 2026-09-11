import { buildApp } from "./app.js";
import { loadConfig } from "./config.js";

async function main(): Promise<void> {
  let config;
  try {
    config = loadConfig(process.env);
  } catch (err) {
    const message = err instanceof Error ? err.message : "Invalid configuration";
    console.error(message);
    process.exitCode = 1;
    return;
  }

  const app = await buildApp();

  try {
    await app.listen({ host: config.host, port: config.port });
  } catch (err) {
    app.log.error(err);
    process.exitCode = 1;
    await app.close();
  }
}

void main();
