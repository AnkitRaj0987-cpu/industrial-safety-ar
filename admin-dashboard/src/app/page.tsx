// admin-dashboard/src/app/page.tsx
"use client";

import { useEffect, useState } from "react";
import Link from "next/link";
import { fetchStats, fetchAttempts } from "@/lib/api";

export default function DashboardPage() {
  const [stats, setStats] = useState<any>(null);
  const [recentAttempts, setRecentAttempts] = useState<any[]>([]);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    async function loadData() {
      try {
        const [s, a] = await Promise.all([fetchStats(), fetchAttempts()]);
        setStats(s);
        setRecentAttempts(a.slice(0, 5));
      } catch (err) {
        console.error(err);
      } finally {
        setLoading(false);
      }
    }
    loadData();
  }, []);

  return (
    <>
      <header className="topbar">
        <div>
          <h1 className="topbar-title">Compliance & Training Operations</h1>
        </div>
        <div style={{ display: "flex", gap: "12px", alignItems: "center" }}>
          <span className="topbar-badge">SIH 2026 PS 26041</span>
          <Link href="/login" className="btn btn-secondary btn-sm">
            Admin Account
          </Link>
        </div>
      </header>

      <main className="content-body">
        {/* KPI Grid */}
        <div className="kpi-grid">
          <div className="kpi-card">
            <span className="kpi-label">Registered Workforce</span>
            <div className="kpi-value">{loading ? "..." : stats?.totalWorkers ?? 0}</div>
            <span className="kpi-subtext">Bokaro, Dhanbad & Ranchi sites</span>
          </div>

          <div className="kpi-card">
            <span className="kpi-label">Training Sessions</span>
            <div className="kpi-value">{loading ? "..." : stats?.totalAttempts ?? 0}</div>
            <span className="kpi-subtext">Offline AR attempts synchronized</span>
          </div>

          <div className="kpi-card">
            <span className="kpi-label">Certified Operators</span>
            <div className="kpi-value" style={{ color: "var(--primary)" }}>
              {loading ? "..." : stats?.totalCertificates ?? 0}
            </div>
            <span className="kpi-subtext">QR verifiable credentials issued</span>
          </div>

          <div className="kpi-card">
            <span className="kpi-label">First-Time Pass Rate</span>
            <div className="kpi-value" style={{ color: "var(--success)" }}>
              {loading ? "..." : `${stats?.passRate ?? 0}%`}
            </div>
            <span className="kpi-subtext">Passing threshold: 70.0%</span>
          </div>
        </div>

        {/* Module Performance Breakdown */}
        <div style={{ display: "grid", gridTemplateColumns: "1fr 1fr", gap: "24px", marginBottom: "32px" }}>
          <div className="card" style={{ marginBottom: 0 }}>
            <div className="card-header">
              <div>
                <h3 className="card-title">Fire & Explosion Response AR</h3>
                <span style={{ fontSize: "12px", color: "var(--text-muted)" }}>Module ID: fire-explosion-response</span>
              </div>
              <span className="badge badge-info">Level 1 Core</span>
            </div>
            <div style={{ padding: "20px 24px", display: "flex", justifyContent: "space-between" }}>
              <div>
                <div style={{ fontSize: "24px", fontWeight: 800, color: "var(--text-primary)" }}>
                  {stats?.moduleBreakdown?.[0]?.avg_score ?? 78.0}
                  <span style={{ fontSize: "14px", color: "var(--text-secondary)", fontWeight: 500 }}> / 100</span>
                </div>
                <span style={{ fontSize: "12px", color: "var(--text-muted)" }}>Average Score</span>
              </div>
              <div>
                <div style={{ fontSize: "24px", fontWeight: 800, color: "var(--text-primary)" }}>
                  {stats?.moduleBreakdown?.[0]?.total_attempts ?? 2}
                </div>
                <span style={{ fontSize: "12px", color: "var(--text-muted)" }}>Total Attempts</span>
              </div>
              <div>
                <div style={{ fontSize: "24px", fontWeight: 800, color: "var(--success)" }}>
                  {stats?.moduleBreakdown?.[0]?.passed_attempts ?? 1}
                </div>
                <span style={{ fontSize: "12px", color: "var(--text-muted)" }}>Passed Credentials</span>
              </div>
            </div>
          </div>

          <div className="card" style={{ marginBottom: 0 }}>
            <div className="card-header">
              <div>
                <h3 className="card-title">Gas Leak & Confined Space AR</h3>
                <span style={{ fontSize: "12px", color: "var(--text-muted)" }}>Module ID: gas-confined-space</span>
              </div>
              <span className="badge badge-warning">Level 2 Critical</span>
            </div>
            <div style={{ padding: "20px 24px", display: "flex", justifyContent: "space-between" }}>
              <div>
                <div style={{ fontSize: "24px", fontWeight: 800, color: "var(--text-primary)" }}>
                  {stats?.moduleBreakdown?.[1]?.avg_score ?? 91.5}
                  <span style={{ fontSize: "14px", color: "var(--text-secondary)", fontWeight: 500 }}> / 100</span>
                </div>
                <span style={{ fontSize: "12px", color: "var(--text-muted)" }}>Average Score</span>
              </div>
              <div>
                <div style={{ fontSize: "24px", fontWeight: 800, color: "var(--text-primary)" }}>
                  {stats?.moduleBreakdown?.[1]?.total_attempts ?? 2}
                </div>
                <span style={{ fontSize: "12px", color: "var(--text-muted)" }}>Total Attempts</span>
              </div>
              <div>
                <div style={{ fontSize: "24px", fontWeight: 800, color: "var(--success)" }}>
                  {stats?.moduleBreakdown?.[1]?.passed_attempts ?? 2}
                </div>
                <span style={{ fontSize: "12px", color: "var(--text-muted)" }}>Passed Credentials</span>
              </div>
            </div>
          </div>
        </div>

        {/* Recent Attempts Table */}
        <div className="card">
          <div className="card-header">
            <div>
              <h2 className="card-title">Recent Practical Assessment Synchronizations</h2>
              <span style={{ fontSize: "12px", color: "var(--text-muted)" }}>
                Latest verified offline outbox payloads from Android field devices
              </span>
            </div>
            <Link href="/attempts" className="btn btn-secondary btn-sm">
              View All Attempts →
            </Link>
          </div>

          <div className="table-wrapper">
            <table>
              <thead>
                <tr>
                  <th>Worker</th>
                  <th>Module</th>
                  <th>Industrial Site</th>
                  <th>Score</th>
                  <th>Outcome</th>
                  <th>Completion Time</th>
                  <th>Actions</th>
                </tr>
              </thead>
              <tbody>
                {loading ? (
                  <tr>
                    <td colSpan={7} style={{ textAlign: "center", padding: "30px", color: "var(--text-muted)" }}>
                      Loading verified attempts...
                    </td>
                  </tr>
                ) : recentAttempts.length === 0 ? (
                  <tr>
                    <td colSpan={7} style={{ textAlign: "center", padding: "30px", color: "var(--text-muted)" }}>
                      No synchronized attempts recorded yet.
                    </td>
                  </tr>
                ) : (
                  recentAttempts.map((att) => (
                    <tr key={att.id}>
                      <td>
                        <div style={{ fontWeight: 600 }}>{att.display_name}</div>
                        <div style={{ fontSize: "12px", color: "var(--text-muted)" }}>{att.worker_code}</div>
                      </td>
                      <td>
                        <span style={{ fontWeight: 500 }}>
                          {att.module_id === "fire-explosion-response"
                            ? "Fire & Explosion Response"
                            : "Gas Leak & Confined Space"}
                        </span>
                      </td>
                      <td style={{ color: "var(--text-secondary)", fontSize: "13px" }}>
                        {att.site_label ?? "Jharkhand Industrial Zone"}
                      </td>
                      <td>
                        <strong style={{ fontSize: "15px" }}>
                          {att.server_score ?? att.client_score ?? 0}
                        </strong>
                        <span style={{ color: "var(--text-muted)", fontSize: "12px" }}> / 100</span>
                      </td>
                      <td>
                        {att.passed ? (
                          <span className="badge badge-pass">[PASS] PASSED</span>
                        ) : (
                          <span className="badge badge-fail">[FAIL] RETAKE</span>
                        )}
                      </td>
                      <td style={{ color: "var(--text-muted)", fontSize: "13px" }}>
                        {new Date(att.completed_at || att.started_at).toLocaleString("en-IN", {
                          dateStyle: "medium",
                          timeStyle: "short",
                        })}
                      </td>
                      <td>
                        <Link href={`/workers/${att.worker_id}`} className="btn btn-secondary btn-sm">
                          Profile
                        </Link>
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
