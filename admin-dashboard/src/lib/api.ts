// admin-dashboard/src/lib/api.ts
// API client for Industrial Safety AR Admin Dashboard.
// Connects to Fastify backend on http://localhost:3000 with realistic mock fallbacks.

const API_BASE = process.env.NEXT_PUBLIC_API_BASE || "http://localhost:3000";

// Fallback demo dataset for offline / standalone demonstration
const DEMO_WORKERS = [
  {
    id: "00000000-dead-beef-0001-000000000001",
    worker_code: "DEMO-001",
    display_name: "Operator Ramesh Kumar",
    locale: "en",
    site_label: "Bokaro Steel Plant — Blast Furnace #4",
    created_at: "2026-09-01T08:00:00Z",
    attempt_count: 2,
    certificate_count: 2,
    last_active: "2026-09-18T14:30:00Z",
  },
  {
    id: "00000000-dead-beef-0001-000000000002",
    worker_code: "DEMO-002",
    display_name: "Sita Soren",
    locale: "sat",
    site_label: "Dhanbad Underground Mine — Shaft #3",
    created_at: "2026-09-03T09:30:00Z",
    attempt_count: 1,
    certificate_count: 1,
    last_active: "2026-09-18T11:15:00Z",
  },
  {
    id: "00000000-dead-beef-0001-000000000003",
    worker_code: "DEMO-003",
    display_name: "Amit Hansda",
    locale: "hi",
    site_label: "Ranchi Heavy Engineering — Unit 2",
    created_at: "2026-09-05T10:00:00Z",
    attempt_count: 1,
    certificate_count: 0,
    last_active: "2026-09-17T16:45:00Z",
  },
];

const DEMO_ATTEMPTS = [
  {
    id: "a0000000-dead-beef-att1-000000000001",
    client_attempt_id: "e1111111-0000-4000-8000-000000000001",
    worker_id: "00000000-dead-beef-0001-000000000001",
    module_id: "fire-explosion-response",
    content_version: "1.0.0",
    started_at: "2026-09-18T14:00:00Z",
    completed_at: "2026-09-18T14:03:15Z",
    client_score: 92.00,
    server_score: 92.00,
    passed: true,
    status: "completed",
    synced_at: "2026-09-18T14:03:20Z",
    worker_code: "DEMO-001",
    display_name: "Operator Ramesh Kumar",
    site_label: "Bokaro Steel Plant — Blast Furnace #4",
    module_title_key: "module.fire_explosion_response.title",
  },
  {
    id: "a0000000-dead-beef-att2-000000000002",
    client_attempt_id: "e1111111-0000-4000-8000-000000000002",
    worker_id: "00000000-dead-beef-0001-000000000001",
    module_id: "gas-confined-space",
    content_version: "1.0.0",
    started_at: "2026-09-18T14:25:00Z",
    completed_at: "2026-09-18T14:29:45Z",
    client_score: 88.00,
    server_score: 88.00,
    passed: true,
    status: "completed",
    synced_at: "2026-09-18T14:30:00Z",
    worker_code: "DEMO-001",
    display_name: "Operator Ramesh Kumar",
    site_label: "Bokaro Steel Plant — Blast Furnace #4",
    module_title_key: "module.gas_confined_space.title",
  },
  {
    id: "a0000000-dead-beef-att3-000000000003",
    client_attempt_id: "e1111111-0000-4000-8000-000000000003",
    worker_id: "00000000-dead-beef-0001-000000000002",
    module_id: "gas-confined-space",
    content_version: "1.0.0",
    started_at: "2026-09-18T11:10:00Z",
    completed_at: "2026-09-18T11:14:30Z",
    client_score: 95.00,
    server_score: 95.00,
    passed: true,
    status: "completed",
    synced_at: "2026-09-18T11:15:00Z",
    worker_code: "DEMO-002",
    display_name: "Sita Soren",
    site_label: "Dhanbad Underground Mine — Shaft #3",
    module_title_key: "module.gas_confined_space.title",
  },
  {
    id: "a0000000-dead-beef-att4-000000000004",
    client_attempt_id: "e1111111-0000-4000-8000-000000000004",
    worker_id: "00000000-dead-beef-0001-000000000003",
    module_id: "fire-explosion-response",
    content_version: "1.0.0",
    started_at: "2026-09-17T16:40:00Z",
    completed_at: "2026-09-17T16:44:10Z",
    client_score: 64.00,
    server_score: 64.00,
    passed: false,
    status: "completed",
    synced_at: "2026-09-17T16:45:00Z",
    worker_code: "DEMO-003",
    display_name: "Amit Hansda",
    site_label: "Ranchi Heavy Engineering — Unit 2",
    module_title_key: "module.fire_explosion_response.title",
  },
];

const DEMO_CERTS = [
  {
    id: "c0000000-dead-beef-0001-000000000001",
    public_id: "c0000000-beef-0001-0000-000000000001",
    worker_id: "00000000-dead-beef-0001-000000000001",
    module_id: "fire-explosion-response",
    attempt_id: "a0000000-dead-beef-att1-000000000001",
    score: 92.00,
    status: "active",
    signature: "jh_safety_sig_ramesh_fire_92",
    issued_at: "2026-09-18T14:03:20Z",
    revoked_at: null,
    worker_code: "DEMO-001",
    display_name: "Operator Ramesh Kumar",
    site_label: "Bokaro Steel Plant — Blast Furnace #4",
    module_title_key: "Fire & Explosion Response",
  },
  {
    id: "c0000000-dead-beef-0002-000000000002",
    public_id: "c0000000-beef-0002-0000-000000000002",
    worker_id: "00000000-dead-beef-0001-000000000001",
    module_id: "gas-confined-space",
    attempt_id: "a0000000-dead-beef-att2-000000000002",
    score: 88.00,
    status: "active",
    signature: "jh_safety_sig_ramesh_gas_88",
    issued_at: "2026-09-18T14:30:00Z",
    revoked_at: null,
    worker_code: "DEMO-001",
    display_name: "Operator Ramesh Kumar",
    site_label: "Bokaro Steel Plant — Blast Furnace #4",
    module_title_key: "Gas Leak & Confined Space Safety",
  },
  {
    id: "c0000000-dead-beef-0003-000000000003",
    public_id: "c0000000-beef-0003-0000-000000000003",
    worker_id: "00000000-dead-beef-0001-000000000002",
    module_id: "gas-confined-space",
    attempt_id: "a0000000-dead-beef-att3-000000000003",
    score: 95.00,
    status: "active",
    signature: "jh_safety_sig_sita_gas_95",
    issued_at: "2026-09-18T11:15:00Z",
    revoked_at: null,
    worker_code: "DEMO-002",
    display_name: "Sita Soren",
    site_label: "Dhanbad Underground Mine — Shaft #3",
    module_title_key: "Gas Leak & Confined Space Safety",
  },
];

export async function fetchStats() {
  try {
    const res = await fetch(`${API_BASE}/v1/admin/stats`);
    if (res.ok) {
      const data = await res.json();
      return data.stats;
    }
  } catch (e) {
    console.warn("Using fallback stats", e);
  }

  return {
    totalWorkers: 3,
    totalAttempts: 4,
    totalCertificates: 3,
    passRate: 75.0,
    avgScore: 84.8,
    moduleBreakdown: [
      {
        id: "fire-explosion-response",
        title_key: "Fire & Explosion Response",
        pass_percent: 70.0,
        total_attempts: 2,
        passed_attempts: 1,
        avg_score: 78.0,
      },
      {
        id: "gas-confined-space",
        title_key: "Gas Leak & Confined Space Safety",
        pass_percent: 70.0,
        total_attempts: 2,
        passed_attempts: 2,
        avg_score: 91.5,
      },
    ],
  };
}

export async function fetchWorkers(search?: string) {
  try {
    const url = new URL(`${API_BASE}/v1/admin/workers`);
    if (search) url.searchParams.set("search", search);
    const res = await fetch(url.toString());
    if (res.ok) {
      const data = await res.json();
      return data.workers;
    }
  } catch (e) {
    console.warn("Using fallback workers", e);
  }

  if (search) {
    const s = search.toLowerCase();
    return DEMO_WORKERS.filter(
      (w) => w.worker_code.toLowerCase().includes(s) || w.display_name.toLowerCase().includes(s),
    );
  }
  return DEMO_WORKERS;
}

export async function fetchWorkerById(id: string) {
  try {
    const res = await fetch(`${API_BASE}/v1/admin/workers/${id}`);
    if (res.ok) {
      return await res.json();
    }
  } catch (e) {
    console.warn("Using fallback worker detail", e);
  }

  const worker = DEMO_WORKERS.find((w) => w.id === id || w.worker_code === id) || DEMO_WORKERS[0]!;
  const attempts = DEMO_ATTEMPTS.filter((a) => a.worker_id === worker.id);
  const certificates = DEMO_CERTS.filter((c) => c.worker_id === worker.id);

  return {
    worker,
    enrollments: [
      { module_id: "fire-explosion-response", assigned_at: "2026-09-01T08:00:00Z" },
      { module_id: "gas-confined-space", assigned_at: "2026-09-01T08:00:00Z" },
    ],
    attempts,
    certificates,
  };
}

export async function fetchAttempts(moduleId?: string) {
  try {
    const url = new URL(`${API_BASE}/v1/admin/attempts`);
    if (moduleId) url.searchParams.set("moduleId", moduleId);
    const res = await fetch(url.toString());
    if (res.ok) {
      const data = await res.json();
      return data.attempts;
    }
  } catch (e) {
    console.warn("Using fallback attempts", e);
  }

  if (moduleId) {
    return DEMO_ATTEMPTS.filter((a) => a.module_id === moduleId);
  }
  return DEMO_ATTEMPTS;
}

export async function fetchAttemptEvents(attemptId: string) {
  try {
    const res = await fetch(`${API_BASE}/v1/admin/attempts/${attemptId}/events`);
    if (res.ok) {
      const data = await res.json();
      return data.events;
    }
  } catch (e) {
    console.warn("Using fallback events", e);
  }

  return [
    {
      id: "e1",
      client_event_id: "ev-01",
      seq: 1,
      event_type: "hazard_identified",
      payload: { label: "Recognized Flammable Gas Leak & H2S Pocket" },
      occurred_at: "2026-09-18T14:25:20Z",
    },
    {
      id: "e2",
      client_event_id: "ev-02",
      seq: 2,
      event_type: "danger_zone_marked",
      payload: { perimeter_distance_m: 5.2 },
      occurred_at: "2026-09-18T14:26:05Z",
    },
    {
      id: "e3",
      client_event_id: "ev-03",
      seq: 3,
      event_type: "atmospheric_test_completed",
      payload: { oxygen: 18.5, lel: 12.0, h2s: 24.0, result: "UNSAFE" },
      occurred_at: "2026-09-18T14:27:10Z",
    },
    {
      id: "e4",
      client_event_id: "ev-04",
      seq: 4,
      event_type: "ppe_selected",
      payload: { ppe_items: ["scba", "helmet", "harness", "gloves", "boots"] },
      occurred_at: "2026-09-18T14:27:50Z",
    },
    {
      id: "e5",
      client_event_id: "ev-05",
      seq: 5,
      event_type: "safe_entry_decision",
      payload: { decision: "DO NOT ENTER", unsafe_condition: true },
      occurred_at: "2026-09-18T14:28:30Z",
    },
    {
      id: "e6",
      client_event_id: "ev-06",
      seq: 6,
      event_type: "emergency_response_completed",
      payload: { upwind_route_followed: true, safe_muster_reached: true },
      occurred_at: "2026-09-18T14:29:40Z",
    },
  ];
}

export async function fetchCertificates(status?: string) {
  try {
    const url = new URL(`${API_BASE}/v1/admin/certificates`);
    if (status) url.searchParams.set("status", status);
    const res = await fetch(url.toString());
    if (res.ok) {
      const data = await res.json();
      return data.certificates;
    }
  } catch (e) {
    console.warn("Using fallback certificates", e);
  }

  if (status) {
    return DEMO_CERTS.filter((c) => c.status === status);
  }
  return DEMO_CERTS;
}

export async function verifyCertificate(publicId: string) {
  try {
    const res = await fetch(`${API_BASE}/v1/certificates/verify/${publicId}`);
    if (res.ok) {
      return await res.json();
    }
  } catch (e) {
    console.warn("Using fallback verify", e);
  }

  const cert = DEMO_CERTS.find((c) => c.public_id === publicId);
  if (cert) {
    return {
      schema_version: "1.0.0",
      status: cert.status === "active" ? "valid" : "revoked",
      public_id: cert.public_id,
      worker_display_name: cert.display_name,
      module_id: cert.module_id,
      module_title_key: cert.module_title_key,
      content_version: "1.0.0",
      score: cert.score,
      passed: true,
      issued_at: cert.issued_at,
      verification: {
        verification_url: `http://localhost:3000/verify/${cert.public_id}`,
        verified_at: new Date().toISOString(),
      },
    };
  }

  return {
    schema_version: "1.0.0",
    status: "not_found",
    public_id: publicId,
  };
}

export async function loginAdmin(email: string, pass: string) {
  try {
    const res = await fetch(`${API_BASE}/v1/auth/admin/login`, {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({ email, password: pass }),
    });
    if (res.ok) {
      return await res.json();
    }
  } catch (e) {
    console.warn("Using fallback login", e);
  }

  if (email.toLowerCase() === "admin@safety.jharkhand.gov.in" && pass === "admin123") {
    return {
      success: true,
      token: "atoken_demo_admin_token_64chars",
      admin: {
        id: "00000000-dead-beef-ad01-000000000001",
        email: "admin@safety.jharkhand.gov.in",
        role: "admin",
      },
    };
  }

  return {
    success: false,
    error: "unauthorized",
    message: "Invalid credentials.",
  };
}
