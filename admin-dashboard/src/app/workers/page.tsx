// admin-dashboard/src/app/workers/page.tsx
"use client";

import { useEffect, useState } from "react";
import Link from "next/link";
import { fetchWorkers } from "@/lib/api";

export default function WorkersPage() {
  const [workers, setWorkers] = useState<any[]>([]);
  const [search, setSearch] = useState("");
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    async function load() {
      setLoading(true);
      try {
        const data = await fetchWorkers(search);
        setWorkers(data);
      } catch (e) {
        console.error(e);
      } finally {
        setLoading(false);
      }
    }
    const timer = setTimeout(load, 200);
    return () => clearTimeout(timer);
  }, [search]);

  return (
    <>
      <header className="topbar">
        <div>
          <h1 className="topbar-title">Workforce Directory & Compliance Registry</h1>
        </div>
        <div style={{ display: "flex", gap: "12px", alignItems: "center" }}>
          <input
            type="text"
            className="input"
            placeholder="Search worker ID or name..."
            value={search}
            onChange={(e) => setSearch(e.target.value)}
            style={{ width: "260px" }}
          />
        </div>
      </header>

      <main className="content-body">
        <div className="card">
          <div className="card-header">
            <div>
              <h2 className="card-title">Enrolled Industrial Workers</h2>
              <span style={{ fontSize: "12px", color: "var(--text-muted)" }}>
                Total Workers: {workers.length} registered across Jharkhand industrial sectors
              </span>
            </div>
          </div>

          <div className="table-wrapper">
            <table>
              <thead>
                <tr>
                  <th>Worker Identifier</th>
                  <th>Display Name</th>
                  <th>Industrial Site / Plant</th>
                  <th>Interface Language</th>
                  <th>Completed Sessions</th>
                  <th>Active Credentials</th>
                  <th>Actions</th>
                </tr>
              </thead>
              <tbody>
                {loading ? (
                  <tr>
                    <td colSpan={7} style={{ textAlign: "center", padding: "30px", color: "var(--text-muted)" }}>
                      Searching workforce records...
                    </td>
                  </tr>
                ) : workers.length === 0 ? (
                  <tr>
                    <td colSpan={7} style={{ textAlign: "center", padding: "30px", color: "var(--text-muted)" }}>
                      No workers found matching "{search}".
                    </td>
                  </tr>
                ) : (
                  workers.map((w) => (
                    <tr key={w.id}>
                      <td>
                        <span style={{
                          fontFamily: "monospace",
                          fontWeight: 700,
                          backgroundColor: "rgba(255,255,255,0.05)",
                          padding: "3px 8px",
                          borderRadius: "4px",
                          color: "var(--primary)",
                        }}>
                          {w.worker_code}
                        </span>
                      </td>
                      <td style={{ fontWeight: 600 }}>{w.display_name}</td>
                      <td style={{ color: "var(--text-secondary)", fontSize: "13px" }}>
                        {w.site_label ?? "Unassigned Division"}
                      </td>
                      <td>
                        <span className="badge badge-info">
                          {w.locale === "hi" ? "Hindi (हिंदी)" : w.locale === "sat" ? "Santali (Ol Chiki)" : "English"}
                        </span>
                      </td>
                      <td>
                        <strong style={{ fontSize: "14px" }}>{w.attempt_count ?? 0}</strong>
                      </td>
                      <td>
                        {w.certificate_count > 0 ? (
                          <span className="badge badge-pass">{w.certificate_count} Active Cert</span>
                        ) : (
                          <span className="badge badge-warning">0 Pending</span>
                        )}
                      </td>
                      <td>
                        <Link href={`/workers/${w.id}`} className="btn btn-primary btn-sm">
                          View History
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
