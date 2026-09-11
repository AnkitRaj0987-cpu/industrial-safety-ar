import assert from "node:assert/strict";
import { after, test } from "node:test";
import { buildApp } from "./app.js";

const app = await buildApp();

after(async () => {
  await app.close();
});

test("GET /health returns 200 and { status: ok }", async () => {
  const response = await app.inject({
    method: "GET",
    url: "/health",
  });

  assert.equal(response.statusCode, 200);
  assert.equal(response.headers["content-type"], "application/json; charset=utf-8");
  assert.deepEqual(response.json(), { status: "ok" });
});
