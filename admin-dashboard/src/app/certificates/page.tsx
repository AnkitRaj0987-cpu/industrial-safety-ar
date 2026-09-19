// admin-dashboard/src/app/certificates/page.tsx
"use client";

import { useEffect, useState } from "react";
import Link from "next/link";
import { fetchCertificates } from "@/lib/api";

export default function CertificatesPage() {
  const [certs, setCerts] = useState<any[]>([]);
  const [statusFilter, setStatusFilter] = useState<string>("");
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    async function load() {
      setLoading(true);
      try {
        const data = await fetchCertificates(statusFilter || undefined);
        setCerts(data);
      } catch (e) {
        console.error(e);
      } finally {
        setLoading(false);
      }
    }
    load();
  }, [statusFilter]);

  return (
    <>
      <header className="topbar">
        <div>
          <h1 className="topbar-title">Official Vocational Safety Certifications</h1>
        </div>
        <div style={{ display: "flex", gap: "12px", alignItems: "center" }}>
          <select
            className="input"
            value={statusFilter}
            onChange={(e) => setStatusFilter(e.target.value)}
            style={{ width: "180px", cursor: "pointer" }}
          >
            <option value="">All Statuses</option>
            <option value="active">Active Only</option>
            <option value="revoked">Revoked Only</option>
          </select>
        </div>
      </header>

      <main className="content-body">
        <div className="card">
          <div className="card-header">
            <div>
              <h2 className="card-title">Government-Authorized Worker Credentials</h2>
              <span style={{ fontSize: "12px", color: "var(--text-muted)" }}>
                Issued by Directorate of Factories & Industrial Safety, Jharkhand
              </span>
            </div>
            <span className="badge badge-pass">{certs.length} Issued Records</span>
          </div>

          <div className="table-wrapper">
            <table>
              <thead>
                <tr>
                  <th>Public Verification ID</th>
                  <th>Certified Operator</th>
                  <th>Certified Safety Competency</th>
                  <th>Qualifying Score</th>
                  <th>Authorization Status</th>
                  <th>Issue Date</th>
                  <th>Independent Verification</th>
                </tr>
              </thead>
              <tbody>
                {loading ? (
                  <tr>
                    <td colSpan={7} style={{ textAlign: "center", padding: "30px", color: "var(--text-muted)" }}>
                      Loading issued credentials...
                    </td>
                  </tr>
                ) : certs.length === 0 ? (
                  <tr>
                    <td colSpan={7} style={{ textAlign: "center", padding: "30px", color: "var(--text-muted)" }}>
                      No certificates match current filter.
                    </td>
                  </tr>
                ) : (
                  certs.map((c) => (
                    <tr key={c.id}>
                      <td>
                        <span style={{
                          fontFamily: "monospace",
                          fontSize: "12px",
                          backgroundColor: "rgba(255,255,255,0.05)",
                          padding: "3px 8px",
                          borderRadius: "4px",
                          color: "var(--primary)",
                        }}>
                          {c.public_id.substring(0, 18)}...
                        </span>
                      </td>
                      <td>
                        <div style={{ fontWeight: 600 }}>{c.display_name}</div>
                        <div style={{ fontSize: "12px", color: "var(--text-muted)" }}>{c.worker_code}</div>
                      </td>
                      <td style={{ fontWeight: 500 }}>
                        {c.module_id === "fire-explosion-response"
                          ? "Fire & Explosion Response"
                          : "Gas Leak & Confined Space Safety"}
                      </td>
                      <td>
                        <strong style={{ fontSize: "15px", color: "var(--success)" }}>{c.score}</strong>
                        <span style={{ fontSize: "12px", color: "var(--text-muted)" }}> / 100</span>
                      </td>
                      <td>
                        {c.status === "active" ? (
                          <span className="badge badge-pass">[OK] ACTIVE</span>
                        ) : (
                          <span className="badge badge-fail">[REVOKED]</span>
                        )}
                      </td>
                      <td style={{ color: "var(--text-muted)", fontSize: "13px" }}>
                        {new Date(c.issued_at).toLocaleDateString("en-IN", {
                          dateStyle: "medium",
                        })}
                      </td>
                      <td>
                        <Link
                          href={`/verify/${c.public_id}`}
                          target="_blank"
                          className="btn btn-primary btn-sm"
                        >
                          [ VERIFY ]
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
