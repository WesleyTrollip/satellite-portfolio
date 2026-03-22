"use client";

import Link from "next/link";
import { usePathname } from "next/navigation";

const navItems = [
  { href: "/", label: "Overview" },
  { href: "/holdings", label: "Holdings" },
  { href: "/trades", label: "Trades" },
  { href: "/prices", label: "Prices" },
  { href: "/journal", label: "Journal" },
  { href: "/rules", label: "Rules" },
  { href: "/admin", label: "Admin" }
];

export function SiteNav() {
  const pathname = usePathname();

  return (
    <nav aria-label="Primary navigation" className="overflow-x-auto">
      <ul className="flex min-w-max flex-wrap gap-2">
        {navItems.map((item) => {
          const isActive = item.href === "/" ? pathname === "/" : pathname.startsWith(item.href);
          return (
            <li key={item.href}>
              <Link
                href={item.href}
                aria-current={isActive ? "page" : undefined}
                className={
                  isActive
                    ? "inline-flex rounded-md border border-primary bg-blue-950/40 px-3 py-1.5 text-sm font-medium text-primary no-underline"
                    : "inline-flex rounded-md border border-border bg-surface-muted px-3 py-1.5 text-sm font-medium text-text hover:bg-slate-800 hover:no-underline"
                }
              >
                {item.label}
              </Link>
            </li>
          );
        })}
      </ul>
    </nav>
  );
}
