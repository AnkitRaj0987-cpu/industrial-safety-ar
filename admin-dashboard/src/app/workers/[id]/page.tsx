// admin-dashboard/src/app/workers/[id]/page.tsx
"use client";

import { useEffect, useState } from "react";
import Link from "next/link";
import { useParams } from "next/navigation";
import { fetchWorkerById } from "@/lib/api";

export default function WorkerDetailPage() {
  const params = useParams();
  const id = params?.id as string;
  const [data, setData] = useState<any>(null);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    async function load() {
      if (!id) return;
      try {
        const res = await fetchWorkerById(id);
        setData(res);
      } catch (err) {
        console.error(err);
      } finally {
        setLoading(false);
      }
    }
    load();
  }, [id]);

  if (loading) {
    return (
      <div style={{ padding: "40px", color: "var(--text-muted)" }}>
        Loading worker profile...
      </div>
    );
  }

  if (!data || !data.worker) {
    return (
      <div style={{ padding: "40px" }}>
        <h2>Worker Not Found</h2>
        <Link href="/workers" className="btn btn-secondary btn-sm" style={{ marginTop: "16px" }}>
          ← Back to Workers
        </Link>
      </div>
    );
  }

  const { worker, attempts, certificates } = data;

  return (
    <>
      <header className="topbar">
        <div style={{ display: "flex", alignItems: "center", gap: "16px" }}>
          <Link href="/workers" className="btn btn-secondary btn-sm">
            ← Back
          </Link>
          <h1 className="topbar-title">{worker.display_name}</h1>
        </div>
        <div>
          <span className="badge badge-pass" style={{ fontSize: "13px" }}>
            {worker.worker_code}
          </span>
        </div>
      </header>

      <main className="content-body">
        {/* Profile Card */}
        <div className="card" style={{ marginBottom: "28px" }}>
          <div className="card-header">
            <h2 className="card-title">Operator Profile & Site Assignment</h2>
            <span className="badge badge-info">
              Locale: {worker.locale === "hi" ? "Hindi (हिंदी)" : worker.locale === "sat" ? "Santali (Ol Chiki)" : "English"}
            </span>
          </div>
          <div style={{ padding: "24px", display: "grid", gridTemplateColumns: "repeat(auto-fit, minmax(200px, 1fr))", gap: "20px" }}>
            <div>
              <span style={{ fontSize: "12px", color: "var(--text-muted)", textTransform: "uppercase" }}>Plant / Site</span>
              <div style={{ fontSize: "15px", fontWeight: 600, color: "var(--text-primary)", marginTop: "4px" }}>
                {worker.site_label ?? "Bokaro Industrial Complex"}
              </div>
            </div>
            <div>
              <span style={{ fontSize: "12px", color: "var(--text-muted)", textTransform: "uppercase" }}>Worker UUID</span>
              <div style={{ fontSize: "12px", fontFamily: "monospace", color: "var(--text-secondary)", marginTop: "4px" }}>
                {worker.id}
              </div>
            </div>
            <div>
              <span style={{ fontSize: "12px", color: "var(--text-muted)", textTransform: "uppercase" }}>Curriculum Enrolled</span>
              <div style={{ display: "flex", gap: "6px", marginTop: "6px", flexWrap: "wrap" }}>
                <span className="badge badge-info">Fire Response</span>
                <span className="badge badge-warning">Gas & Confined Space</span>
              </div>
            </div>
            <div>
              <span style={{ fontSize: "12px", color: "var(--text-muted)", textTransform: "uppercase" }}>Registered Since</span>
              <div style={{ fontSize: "14px", color: "var(--text-secondary)", marginTop: "4px" }}>
                {new Date(worker.created_at).toLocaleDateString("en-IN", { dateStyle: "long" })}
              </div>
            </div>
          </div>
        </div>

        {/* Issued Certificates */}
        <div className="card" style={{ marginBottom: "28px" }}>
          <div className="card-header">
            <div>
              <h2 className="card-title">Issued State Safety Certifications</h2>
              <span style={{ fontSize: "12px", color: "var(--text-muted)" }}>
                Cryptographically verifiable credentials for qualified operators
              </span>
            </div>
          </div>
          <div className="table-wrapper">
            <table>
              <thead>
                <tr>
                  <th>Certification Module</th>
                  <th>Assessment Score</th>
                  <th>Status</th>
                  <th>Issued Date</th>
                  <th>QR Public ID</th>
                  <th>Verification Action</th>
                </tr>
              </thead>
              <tbody>
                {certificates.length === 0 ? (
                  <tr>
                    <td colSpan={6} style={{ textAlign: "center", padding: "24px", color: "var(--text-muted)" }}>
                      No certificates issued yet for this worker.
                    </td>
                  </tr>
                ) : (
                  certificates.map((c: any) => (
                    <tr key={c.id}>
                      <td style={{ fontWeight: 600 }}>
                        {c.module_id === "fire-explosion-response"
                          ? "Fire & Explosion Response"
                          : "Gas Leak & Confined Space Safety"}
                      </td>
                      <td>
                        <strong style={{ color: "var(--success)", fontSize: "15px" }}>{c.score}</strong> / 100
                      </td>
                      <td>
                        <span className="badge badge-pass">VALID & ACTIVE</span>
                      </td>
                      <td style={{ color: "var(--text-muted)", fontSize: "13px" }}>
                        {new Date(c.issued_at).toLocaleDateString("en-IN", { dateStyle: "medium" })}
                      </td>
                      <td style={{ fontFamily: "monospace", fontSize: "12px", color: "var(--text-secondary)" }}>
                        {c.public_id}
                      </td>
                      <td>
                        <Link
                          href={`/verify/${c.public_id}`}
                          target="_blank"
                          className="btn btn-primary btn-sm"
                        >
                          [ VERIFY CREDENTIAL ]
                        </Link>
                      </td>
                    </tr>
                  ))
                )}
              </tbody>
            </table>
          </div>
        </div>

        {/* Training Assessment History */}
        <div className="card">
          <div className="card-header">
            <div>
              <h2 className="card-title">Practical Training Assessment History</h2>
              <span style={{ fontSize: "12px", color: "var(--text-muted)" }}>
                Audit trail of simulator execution runs and compliance scores
              </span>
            </div>
          </div>
          <div className="table-wrapper">
            <table>
              <thead>
                <tr>
                  <th>Session Timestamp</th>
                  <th>Module</th>
                  <th>Rubric Score</th>
                  <th>Pass / Retake</th>
                  <th>Attempt Status</th>
                </tr>
              </thead>
              <tbody>
                {attempts.length === 0 ? (
                  <tr>
                    <td colSpan={5} style={{ textAlign: "center", padding: "24px", color: "var(--text-muted)" }}>
                      No training attempts recorded.
                    </td>
                  </tr>
                ) : (
                  attempts.map((a: any) => (
                    <tr key={a.id}>
                      <td style={{ color: "var(--text-secondary)", fontSize: "13px" }}>
                        {new Date(a.started_at).toLocaleString("en-IN", {
                          dateStyle: "medium",
                          timeStyle: "short",
                        })}
                      </td>
                      <td style={{ fontWeight: 500 }}>
                        {a.module_id === "fire-explosion-response"
                          ? "Fire & Explosion Response"
                          : "Gas Leak & Confined Space Safety"}
                      </td>
                      <td>
                        <strong>{a.server_score ?? a.client_score ?? 0}</strong> / 100
                      </td>
                      <td>
                        {a.passed ? (
                          <span className="badge badge-pass">[PASS] QUALIFIED</span>
                        ) : (
                          <span className="badge badge-fail">[FAIL] RETAKE REQUIRED</span>
                        )}
                      </td>
                      <td>
                        <span style={{ textTransform: "capitalize", color: "var(--text-muted)", fontSize: "13px" }}>
                          {a.status}
                        </span>
                      </td>
                    </tr>
                  ))
                )}
              </tbody>
            </table>
          </div>
        </div>
      </main>
    </>
  );
}
