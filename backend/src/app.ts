import Fastify from "fastify";
import type { FastifyInstance } from "fastify";

export async function buildApp(): Promise<FastifyInstance> {
  const app = Fastify({
    logger: true,
  });

  app.get("/health", async () => {
    return { status: "ok" as const };
  });

  return app;
}
