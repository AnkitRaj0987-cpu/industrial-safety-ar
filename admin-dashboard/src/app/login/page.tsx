// admin-dashboard/src/app/login/page.tsx
"use client";

import { useState } from "react";
import { useRouter } from "next/navigation";
import { loginAdmin } from "@/lib/api";

export default function LoginPage() {
  const router = useRouter();
  const [email, setEmail] = useState("admin@safety.jharkhand.gov.in");
  const [password, setPassword] = useState("admin123");
  const [error, setError] = useState<string | null>(null);
  const [loading, setLoading] = useState(false);

  async function handleSubmit(e: React.FormEvent) {
    e.preventDefault();
    setError(null);
    setLoading(true);

    try {
      const res = await loginAdmin(email, password);
      if (res.success) {
        if (typeof window !== "undefined") {
          localStorage.setItem("admin_token", res.token);
          localStorage.setItem("admin_user", JSON.stringify(res.admin));
        }
        router.push("/");
      } else {
        setError(res.message || "Invalid administrative credentials.");
      }
    } catch (err: any) {
      setError(err.message || "Network error communicating with authentication service.");
    } finally {
      setLoading(false);
    }
  }

  function handleQuickFill() {
    setEmail("admin@safety.jharkhand.gov.in");
    setPassword("admin123");
  }

  return (
    <div style={{
      minHeight: "100vh",
      width: "100%",
      display: "flex",
      alignItems: "center",
      justifyContent: "center",
      padding: "20px",
      background: "radial-gradient(circle at top, #1E293B 0%, #0B0F19 100%)",
    }}>
      <div style={{
        width: "100%",
        maxWidth: "440px",
        backgroundColor: "var(--bg-card)",
        border: "1px solid var(--border-subtle)",
        borderRadius: "16px",
        padding: "36px",
        boxShadow: "0 20px 40px rgba(0,0,0,0.6)",
      }}>
        {/* Emblem / Header */}
        <div style={{ textAlign: "center", marginBottom: "28px" }}>
          <div style={{
            width: "56px",
            height: "56px",
            borderRadius: "12px",
            background: "linear-gradient(135deg, #F97316 0%, #EA580C 100%)",
            display: "flex",
            alignItems: "center",
            justifyContent: "center",
            margin: "0 auto 16px",
            fontSize: "26px",
            color: "white",
            fontWeight: 800,
            boxShadow: "0 0 20px var(--primary-glow)",
          }}>
            JH
          </div>
          <h1 style={{ fontSize: "22px", fontWeight: 800, color: "var(--text-primary)" }}>
            Industrial Safety AR
          </h1>
          <p style={{ fontSize: "13px", color: "var(--primary)", fontWeight: 600, marginTop: "4px" }}>
            GOVERNMENT OF JHARKHAND — ADMIN PORTAL
          </p>
          <p style={{ fontSize: "12px", color: "var(--text-muted)", marginTop: "2px" }}>
            SIH 2026 Problem Statement ID 26041
          </p>
        </div>

        {error && (
          <div style={{
            backgroundColor: "var(--danger-bg)",
            border: "1px solid var(--danger-border)",
            color: "var(--danger)",
            padding: "10px 14px",
            borderRadius: "8px",
            fontSize: "13px",
            marginBottom: "20px",
          }}>
            {error}
          </div>
        )}

        <form onSubmit={handleSubmit} style={{ display: "flex", flexDirection: "column", gap: "16px" }}>
          <div>
            <label style={{ display: "block", fontSize: "12px", fontWeight: 600, color: "var(--text-secondary)", marginBottom: "6px" }}>
              GOVERNMENT EMAIL ADDRESS
            </label>
            <input
              type="email"
              required
              className="input"
              style={{ width: "100%" }}
              value={email}
              onChange={(e) => setEmail(e.target.value)}
              placeholder="admin@safety.jharkhand.gov.in"
            />
          </div>

          <div>
            <label style={{ display: "block", fontSize: "12px", fontWeight: 600, color: "var(--text-secondary)", marginBottom: "6px" }}>
              SECURITY PASSWORD
            </label>
            <input
              type="password"
              required
              className="input"
              style={{ width: "100%" }}
              value={password}
              onChange={(e) => setPassword(e.target.value)}
              placeholder="••••••••"
            />
          </div>

          <button
            type="submit"
            className="btn btn-primary"
            style={{ width: "100%", marginTop: "8px", padding: "12px" }}
            disabled={loading}
          >
            {loading ? "Authenticating..." : "Access Admin Portal"}
          </button>
        </form>

        {/* SIH Judge Demo Helper */}
        <div style={{
          marginTop: "24px",
          paddingTop: "20px",
          borderTop: "1px solid var(--border-subtle)",
          textAlign: "center",
        }}>
          <button
            type="button"
            onClick={handleQuickFill}
            style={{
              background: "none",
              border: "none",
              color: "var(--primary)",
              fontSize: "12px",
              cursor: "pointer",
              textDecoration: "underline",
              fontWeight: 600,
            }}
          >
            Fill SIH 2026 Judge Demo Credentials
          </button>
          <div style={{ fontSize: "11px", color: "var(--text-muted)", marginTop: "6px" }}>
            Preloaded: admin@safety.jharkhand.gov.in / admin123
          </div>
        </div>
      </div>
    </div>
  );
}
