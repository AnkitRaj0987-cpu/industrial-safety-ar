// admin-dashboard/src/components/Sidebar.tsx
"use client";

import Link from "next/link";
import { usePathname } from "next/navigation";

export default function Sidebar() {
  const pathname = usePathname();

  // Hide sidebar on standalone verification and login screens
  if (pathname.startsWith("/verify") || pathname === "/login") {
    return null;
  }

  const navItems = [
    { label: "Dashboard", href: "/", icon: "📊" },
    { label: "Workers Directory", href: "/workers", icon: "👷" },
    { label: "Training Attempts", href: "/attempts", icon: "📋" },
    { label: "Official Certificates", href: "/certificates", icon: "📜" },
  ];

  return (
    <aside className="sidebar">
      <div className="sidebar-header">
        <div className="sidebar-logo">JH</div>
        <div>
          <h2 className="sidebar-title">Industrial Safety AR</h2>
          <span className="sidebar-subtitle">GOVT. OF JHARKHAND</span>
        </div>
      </div>

      <nav className="sidebar-nav">
        {navItems.map((item) => {
          const isActive = pathname === item.href || (item.href !== "/" && pathname.startsWith(item.href));
          return (
            <Link
              key={item.href}
              href={item.href}
              className={`nav-item ${isActive ? "active" : ""}`}
            >
              <span style={{ fontSize: "16px" }}>{item.icon}</span>
              <span>{item.label}</span>
            </Link>
          );
        })}
      </nav>

      <div className="sidebar-footer">
        <div style={{ display: "flex", alignItems: "center", gap: "10px", marginBottom: "8px" }}>
          <div style={{ width: "8px", height: "8px", borderRadius: "50%", backgroundColor: "#22C55E" }} />
          <span style={{ color: "#F8FAFC", fontWeight: 600, fontSize: "12px" }}>Admin Portal Active</span>
        </div>
        <div style={{ color: "#64748B", fontSize: "11px" }}>SIH 2026 — PS 26041</div>
      </div>
    </aside>
  );
}
