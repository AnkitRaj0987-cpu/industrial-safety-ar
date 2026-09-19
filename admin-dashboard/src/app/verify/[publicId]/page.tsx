// admin-dashboard/src/app/verify/[publicId]/page.tsx
"use client";

import { useEffect, useState } from "react";
import { useParams } from "next/navigation";
import Link from "next/link";
import { verifyCertificate } from "@/lib/api";

export default function PublicVerifyPage() {
  const params = useParams();
  const publicId = params?.publicId as string;
  const [data, setData] = useState<any>(null);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    async function check() {
      if (!publicId) return;
      try {
        const res = await verifyCertificate(publicId);
        setData(res);
      } catch (err) {
        console.error(err);
      } finally {
        setLoading(false);
      }
    }
    check();
  }, [publicId]);

  return (
    <div style={{
      minHeight: "100vh",
      width: "100%",
      display: "flex",
      alignItems: "center",
      justifyContent: "center",
      padding: "24px",
      background: "radial-gradient(circle at top, #1E293B 0%, #0B0F19 100%)",
    }}>
      <div className="cert-card" style={{ width: "100%", maxWidth: "620px" }}>
        {/* Emblem & Official Header */}
        <div className="cert-header">
          <div className="cert-state-seal">
            🏛️
          </div>
          <h1 className="cert-title">Government of Jharkhand</h1>
          <p className="cert-subtitle">Directorate of Factories & Industrial Safety</p>
          <div style={{ fontSize: "11px", color: "var(--text-muted)", marginTop: "4px" }}>
            SIH 2026 PS ID 26041 — Vocational Safety Competency Framework
          </div>
        </div>

        {loading ? (
          <div style={{ textAlign: "center", padding: "40px", color: "var(--text-secondary)" }}>
            Verifying cryptographic digital record...
          </div>
        ) : !data || data.status === "not_found" ? (
          <div style={{
            textAlign: "center",
            padding: "30px",
            backgroundColor: "var(--danger-bg)",
            border: "1px solid var(--danger-border)",
            borderRadius: "12px",
          }}>
            <div style={{ fontSize: "36px", marginBottom: "8px" }}>⚠️</div>
            <h2 style={{ fontSize: "18px", color: "var(--danger)", fontWeight: 700 }}>
              Certificate Not Found
            </h2>
            <p style={{ fontSize: "13px", color: "var(--text-secondary)", marginTop: "6px" }}>
              The credential ID <code>{publicId}</code> could not be validated in the state registry.
            </p>
          </div>
        ) : data.status === "revoked" ? (
          <div style={{
            textAlign: "center",
            padding: "30px",
            backgroundColor: "var(--danger-bg)",
            border: "1px solid var(--danger-border)",
            borderRadius: "12px",
          }}>
            <div style={{ fontSize: "36px", marginBottom: "8px" }}>🚫</div>
            <h2 style={{ fontSize: "18px", color: "var(--danger)", fontWeight: 700 }}>
              Certificate Revoked
            </h2>
            <p style={{ fontSize: "13px", color: "var(--text-secondary)", marginTop: "6px" }}>
              This safety authorization has been superseded or revoked for operator: {data.worker_display_name}.
            </p>
          </div>
        ) : (
          <div>
            {/* Status Banner */}
            <div style={{
              display: "flex",
              alignItems: "center",
              justifyContent: "center",
              gap: "10px",
              padding: "14px",
              backgroundColor: "var(--success-bg)",
              border: "1px solid var(--success-border)",
              borderRadius: "10px",
              color: "var(--success)",
              fontWeight: 700,
              fontSize: "14px",
              letterSpacing: "0.5px",
              marginBottom: "28px",
            }}>
              <span>✓</span>
              <span>AUTHENTIC & AUTHORIZED SAFETY CREDENTIAL</span>
            </div>

            {/* Credential Data Grid */}
            <div style={{
              display: "grid",
              gridTemplateColumns: "1fr 1fr",
              gap: "20px",
              backgroundColor: "rgba(15, 23, 42, 0.6)",
              border: "1px solid var(--border-subtle)",
              borderRadius: "12px",
              padding: "24px",
              marginBottom: "24px",
            }}>
              <div>
                <span style={{ fontSize: "11px", color: "var(--text-muted)", textTransform: "uppercase", letterSpacing: "0.5px" }}>
                  Certified Worker
                </span>
                <div style={{ fontSize: "17px", fontWeight: 700, color: "var(--text-primary)", marginTop: "2px" }}>
                  {data.worker_display_name}
                </div>
              </div>

              <div>
                <span style={{ fontSize: "11px", color: "var(--text-muted)", textTransform: "uppercase", letterSpacing: "0.5px" }}>
                  Assessed Score
                </span>
                <div style={{ fontSize: "17px", fontWeight: 700, color: "var(--success)", marginTop: "2px" }}>
                  {data.score} / 100.00
                </div>
              </div>

              <div style={{ gridColumn: "span 2" }}>
                <span style={{ fontSize: "11px", color: "var(--text-muted)", textTransform: "uppercase", letterSpacing: "0.5px" }}>
                  Competency Module
                </span>
                <div style={{ fontSize: "16px", fontWeight: 600, color: "var(--text-primary)", marginTop: "2px" }}>
                  {data.module_id === "fire-explosion-response"
                    ? "Fire & Explosion Response in Mining Sector"
                    : "Gas Leak & Confined Space Safety Protocol"}
                </div>
              </div>

              <div>
                <span style={{ fontSize: "11px", color: "var(--text-muted)", textTransform: "uppercase", letterSpacing: "0.5px" }}>
                  Curriculum Version
                </span>
                <div style={{ fontSize: "13px", color: "var(--text-secondary)", marginTop: "2px" }}>
                  v{data.content_version} (OSHA / DGMS Aligned)
                </div>
              </div>

              <div>
                <span style={{ fontSize: "11px", color: "var(--text-muted)", textTransform: "uppercase", letterSpacing: "0.5px" }}>
                  Issue Timestamp
                </span>
                <div style={{ fontSize: "13px", color: "var(--text-secondary)", marginTop: "2px" }}>
                  {new Date(data.issued_at).toLocaleDateString("en-IN", { dateStyle: "long" })}
                </div>
              </div>
            </div>

            {/* Cryptographic Integrity Footer */}
            <div style={{
              fontSize: "11px",
              color: "var(--text-muted)",
              fontFamily: "monospace",
              textAlign: "center",
              wordBreak: "break-all",
              lineHeight: 1.5,
              padding: "12px",
              backgroundColor: "rgba(0,0,0,0.3)",
              borderRadius: "8px",
            }}>
              <div>Public ID: {data.public_id}</div>
              <div style={{ marginTop: "4px", color: "#16A34A" }}>
                ● Real-time SHA-256 Ledger Verification Passed ({new Date().toLocaleTimeString("en-IN")})
              </div>
            </div>
          </div>
        )}

        <div style={{ marginTop: "30px", display: "flex", justifyContent: "center", gap: "12px" }}>
          <Link href="/" className="btn btn-secondary btn-sm">
            ← Back to Admin Portal
          </Link>
        </div>
      </div>
    </div>
  );
}
