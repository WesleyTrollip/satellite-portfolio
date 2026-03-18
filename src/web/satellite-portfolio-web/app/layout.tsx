import type { ReactNode } from "react";

export const metadata = {
  title: "Satellite Portfolio",
  description: "Portfolio tracker MVP"
};

export default function RootLayout({ children }: { children: ReactNode }) {
  return (
    <html lang="en">
      <body style={{ margin: 0, fontFamily: "Arial, sans-serif", backgroundColor: "#0f172a", color: "#e2e8f0" }}>
        <header style={{ padding: "1rem 1.5rem", borderBottom: "1px solid #1e293b" }}>
          <nav style={{ display: "flex", gap: "1rem" }}>
            <a href="/" style={{ color: "#93c5fd" }}>Overview</a>
            <a href="/holdings" style={{ color: "#93c5fd" }}>Holdings</a>
            <a href="/trades" style={{ color: "#93c5fd" }}>Trades</a>
            <a href="/journal" style={{ color: "#93c5fd" }}>Journal</a>
            <a href="/rules" style={{ color: "#93c5fd" }}>Rules</a>
          </nav>
        </header>
        <main style={{ padding: "1.5rem" }}>{children}</main>
      </body>
    </html>
  );
}

