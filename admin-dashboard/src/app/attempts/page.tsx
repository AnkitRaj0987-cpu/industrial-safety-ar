// admin-dashboard/src/app/attempts/page.tsx
"use client";

import { useEffect, useState } from "react";
import Link from "next/link";
import { fetchAttempts, fetchAttemptEvents } from "@/lib/api";

export default function AttemptsPage() {
  const [attempts, setAttempts] = useState<any[]>([]);
  const [selectedModule, setSelectedModule] = useState<string>("");
  const [loading, setLoading] = useState(true);

  const [activeAttempt, setActiveAttempt] = useState<any | null>(null);
  const [events, setEvents] = useState<any[]>([]);
  const [eventsLoading, setEventsLoading] = useState(false);

  useEffect(() => {
    async function load() {
      setLoading(true);
      try {
        const data = await fetchAttempts(selectedModule || undefined);
        setAttempts(data);
      } catch (e) {
        console.error(e);
      } finally {
        setLoading(false);
      }
    }
    load();
  }, [selectedModule]);

  async function handleInspect(att: any) {
    setActiveAttempt(att);
    setEventsLoading(true);
    try {
      const evs = await fetchAttemptEvents(att.id);
      setEvents(evs);
    } catch (e) {
      console.error(e);
    } finally {
      setEventsLoading(false);
    }
  }

  return (
    <>
      <header className="topbar">
        <div>
          <h1 className="topbar-title">Practical Simulator Attempt Registry</h1>
        </div>
        <div style={{ display: "flex", gap: "12px", alignItems: "center" }}>
          <select
            className="input"
            value={selectedModule}
            onChange={(e) => setSelectedModule(e.target.value)}
            style={{ width: "240px", cursor: "pointer" }}
          >
            <option value="">All Curriculum Modules</option>
            <option value="fire-explosion-response">Fire & Explosion Response</option>
            <option value="gas-confined-space">Gas Leak & Confined Space</option>
          </select>
        </div>
      </header>

      <main className="content-body">
        <div className="card">
          <div className="card-header">
            <div>
              <h2 className="card-title">Recorded Assessment Sessions</h2>
              <span style={{ fontSize: "12px", color: "var(--text-muted)" }}>
                Verified domain event timelines synced from offline Android runtimes
              </span>
            </div>
            <span className="badge badge-info">{attempts.length} Total Sessions</span>
          </div>

          <div className="table-wrapper">
            <table>
              <thead>
                <tr>
                  <th>Worker Name & ID</th>
                  <th>Module</th>
                  <th>Site Location</th>
                  <th>Final Score</th>
                  <th>Result</th>
                  <th>Timestamp</th>
                  <th>Event Audit</th>
                </tr>
              </thead>
              <tbody>
                {loading ? (
                  <tr>
                    <td colSpan={7} style={{ textAlign: "center", padding: "30px", color: "var(--text-muted)" }}>
                      Loading assessment attempts...
                    </td>
                  </tr>
                ) : attempts.length === 0 ? (
                  <tr>
                    <td colSpan={7} style={{ textAlign: "center", padding: "30px", color: "var(--text-muted)" }}>
                      No attempts found for selected filter.
                    </td>
                  </tr>
                ) : (
                  attempts.map((att) => (
                    <tr key={att.id}>
                      <td>
                        <div style={{ fontWeight: 600 }}>{att.display_name}</div>
                        <div style={{ fontSize: "12px", color: "var(--text-muted)", fontFamily: "monospace" }}>
                          {att.worker_code}
                        </div>
                      </td>
                      <td style={{ fontWeight: 500 }}>
                        {att.module_id === "fire-explosion-response"
                          ? "Fire & Explosion Response"
                          : "Gas Leak & Confined Space"}
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
                        <button
                          onClick={() => handleInspect(att)}
                          className="btn btn-secondary btn-sm"
                        >
                          Inspect Events ({events.length > 0 && activeAttempt?.id === att.id ? events.length : "Audit"})
                        </button>
                      </td>
                    </tr>
                  ))
                )}
              </tbody>
            </table>
          </div>
        </div>

        {/* Modal / Drawer for Domain Events */}
        {activeAttempt && (
          <div style={{
            position: "fixed",
            top: 0,
            left: 0,
            right: 0,
            bottom: 0,
            backgroundColor: "rgba(11, 15, 25, 0.8)",
            backdropFilter: "blur(6px)",
            display: "flex",
            alignItems: "center",
            justifyContent: "center",
            zIndex: 100,
            padding: "20px",
          }}>
            <div style={{
              width: "100%",
              maxWidth: "680px",
              maxHeight: "85vh",
              backgroundColor: "var(--bg-card)",
              border: "1px solid var(--border-subtle)",
              borderRadius: "16px",
              boxShadow: "0 25px 50px rgba(0,0,0,0.7)",
              display: "flex",
              flexDirection: "column",
              overflow: "hidden",
            }}>
              <div style={{
                padding: "20px 24px",
                borderBottom: "1px solid var(--border-subtle)",
                display: "flex",
                alignItems: "center",
                justifyContent: "space-between",
              }}>
                <div>
                  <h3 style={{ fontSize: "17px", fontWeight: 700, color: "var(--text-primary)" }}>
                    Action-Based Rubric Evidence Log
                  </h3>
                  <span style={{ fontSize: "12px", color: "var(--text-muted)" }}>
                    {activeAttempt.display_name} ({activeAttempt.worker_code}) — Score: {activeAttempt.server_score ?? activeAttempt.client_score}/100
                  </span>
                </div>
                <button
                  onClick={() => setActiveAttempt(null)}
                  style={{
                    background: "none",
                    border: "none",
                    color: "var(--text-secondary)",
                    fontSize: "20px",
                    cursor: "pointer",
                  }}
                >
                  ✕
                </button>
              </div>

              <div style={{ padding: "24px", overflowY: "auto", flex: 1 }}>
                {eventsLoading ? (
                  <div style={{ textAlign: "center", padding: "30px", color: "var(--text-muted)" }}>
                    Loading recorded event sequence...
                  </div>
                ) : events.length === 0 ? (
                  <div style={{ textAlign: "center", padding: "30px", color: "var(--text-muted)" }}>
                    No fine-grained telemetry recorded for this attempt.
                  </div>
                ) : (
                  <div style={{ display: "flex", flexDirection: "column", gap: "12px" }}>
                    {events.map((ev) => (
                      <div
                        key={ev.id || ev.seq}
                        style={{
                          backgroundColor: "rgba(15, 23, 42, 0.6)",
                          border: "1px solid var(--border-subtle)",
                          borderRadius: "8px",
                          padding: "14px 16px",
                          display: "flex",
                          gap: "14px",
                          alignItems: "flex-start",
                        }}
                      >
                        <div style={{
                          backgroundColor: "var(--primary)",
                          color: "white",
                          fontSize: "11px",
                          fontWeight: 700,
                          borderRadius: "4px",
                          padding: "2px 6px",
                          marginTop: "2px",
                        }}>
                          #{ev.seq}
                        </div>
                        <div style={{ flex: 1 }}>
                          <div style={{ display: "flex", justifyContent: "space-between", alignItems: "center" }}>
                            <span style={{ fontWeight: 600, fontSize: "14px", color: "var(--text-primary)" }}>
                              {ev.event_type.replace(/_/g, " ").toUpperCase()}
                            </span>
                            <span style={{ fontSize: "11px", color: "var(--text-muted)" }}>
                              {new Date(ev.occurred_at).toLocaleTimeString("en-IN")}
                            </span>
                          </div>
                          <div style={{
                            marginTop: "6px",
                            fontSize: "12px",
                            fontFamily: "monospace",
                            color: "var(--text-secondary)",
                            backgroundColor: "rgba(0,0,0,0.25)",
                            padding: "6px 10px",
                            borderRadius: "4px",
                          }}>
                            {JSON.stringify(ev.payload)}
                          </div>
                        </div>
                      </div>
                    ))}
                  </div>
                )}
              </div>

              <div style={{
                padding: "16px 24px",
                borderTop: "1px solid var(--border-subtle)",
                display: "flex",
                justifyContent: "flex-end",
              }}>
                <button
                  onClick={() => setActiveAttempt(null)}
                  className="btn btn-secondary btn-sm"
                >
                  Close Inspection
                </button>
              </div>
            </div>
          </div>
        )}
      </main>
    </>
  );
}
