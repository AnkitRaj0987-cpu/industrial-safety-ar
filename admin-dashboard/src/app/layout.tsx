// admin-dashboard/src/app/layout.tsx
import type { Metadata } from "next";
import "./globals.css";
import Sidebar from "@/components/Sidebar";

export const metadata: Metadata = {
  title: "Industrial Safety AR — Government of Jharkhand Admin Portal",
  description: "Vocational safety training simulator & compliance monitoring system for mining & manufacturing sector.",
};

export default function RootLayout({
  children,
}: {
  children: React.ReactNode;
}) {
  return (
    <html lang="en">
      <body>
        <div className="app-container">
          <Sidebar />
          <div className="main-content">
            {children}
          </div>
        </div>
      </body>
    </html>
  );
}
