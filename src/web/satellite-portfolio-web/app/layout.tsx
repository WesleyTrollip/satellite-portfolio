import type { ReactNode } from "react";
import { SiteNav } from "./components/site-nav";
import "./globals.css";

export const metadata = {
  title: "Satellite Portfolio",
  description: "Portfolio tracker MVP"
};

export default function RootLayout({ children }: { children: ReactNode }) {
  return (
    <html lang="en">
      <body className="min-h-screen">
        <header className="border-b border-border bg-surface">
          <div className="app-shell py-4">
            <div className="flex flex-col gap-3">
              <div>
                <p className="text-lg font-semibold text-text">Satellite Portfolio</p>
                <p className="muted">Portfolio tracker MVP</p>
              </div>
              <SiteNav />
            </div>
          </div>
        </header>
        <main className="app-shell">{children}</main>
        <footer className="border-t border-border bg-surface">
          <div className="app-shell py-4">
            <p className="muted">Decision-support only. No broker execution or auto-trading.</p>
          </div>
        </footer>
      </body>
    </html>
  );
}

