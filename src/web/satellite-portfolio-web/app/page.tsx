import { getMonthlyState, getOverview } from "../lib/api";
import { CardSection, EmptyState, PageHeader } from "./components/ui";

export default async function OverviewPage() {
  const overview = await getOverview();
  const now = new Date();
  const monthlyState = await getMonthlyState(now.getUTCFullYear(), now.getUTCMonth() + 1);

  return (
    <section className="page-stack">
      <PageHeader title="Portfolio Overview" description={`As of ${new Date(overview.asOf).toLocaleString()}`} />

      <CardSection title="Headline Metrics">
        <dl className="grid gap-3 sm:grid-cols-2 lg:grid-cols-3">
          <div className="rounded-md border border-border bg-surface-muted p-3">
            <dt className="text-xs uppercase tracking-wide text-text-muted">Portfolio Value</dt>
            <dd className="mt-1 text-lg font-semibold">{overview.portfolioValue.toFixed(2)} EUR</dd>
          </div>
          <div className="rounded-md border border-border bg-surface-muted p-3">
            <dt className="text-xs uppercase tracking-wide text-text-muted">Cash Balance</dt>
            <dd className="mt-1 text-lg font-semibold">{overview.cashBalance.toFixed(2)} EUR</dd>
          </div>
          <div className="rounded-md border border-border bg-surface-muted p-3">
            <dt className="text-xs uppercase tracking-wide text-text-muted">Total Market Value</dt>
            <dd className="mt-1 text-lg font-semibold">{overview.totalMarketValue.toFixed(2)} EUR</dd>
          </div>
          <div className="rounded-md border border-border bg-surface-muted p-3">
            <dt className="text-xs uppercase tracking-wide text-text-muted">Realized PnL</dt>
            <dd className="mt-1 text-lg font-semibold">{overview.realizedPnl.toFixed(2)} EUR</dd>
          </div>
          <div className="rounded-md border border-border bg-surface-muted p-3">
            <dt className="text-xs uppercase tracking-wide text-text-muted">Unrealized PnL</dt>
            <dd className="mt-1 text-lg font-semibold">{overview.unrealizedPnl.toFixed(2)} EUR</dd>
          </div>
        </dl>
      </CardSection>

      <CardSection title="Current Alerts">
        {overview.currentAlerts.length === 0 ? (
          <EmptyState message="No active alerts." />
        ) : (
          <ul className="space-y-2">
            {overview.currentAlerts.map((alert) => (
              <li key={alert.alertEventId} className="rounded-md border border-border bg-surface-muted p-3">
                <p className="text-sm font-semibold">{alert.title}</p>
                <p className="muted">
                  Severity: {alert.severity} | Triggered: {new Date(alert.triggeredAt).toLocaleString()}
                </p>
              </li>
            ))}
          </ul>
        )}
      </CardSection>

      <CardSection title="Month-End Snapshot">
        <dl className="grid gap-3 sm:grid-cols-2 lg:grid-cols-3">
          <div className="rounded-md border border-border bg-surface-muted p-3">
            <dt className="text-xs uppercase tracking-wide text-text-muted">As Of</dt>
            <dd className="mt-1 text-sm">{new Date(monthlyState.asOf).toLocaleString()}</dd>
          </div>
          <div className="rounded-md border border-border bg-surface-muted p-3">
            <dt className="text-xs uppercase tracking-wide text-text-muted">Portfolio Value</dt>
            <dd className="mt-1 text-lg font-semibold">{monthlyState.portfolioValue.toFixed(2)} EUR</dd>
          </div>
          <div className="rounded-md border border-border bg-surface-muted p-3">
            <dt className="text-xs uppercase tracking-wide text-text-muted">Cash Balance</dt>
            <dd className="mt-1 text-lg font-semibold">{monthlyState.cashBalance.toFixed(2)} EUR</dd>
          </div>
        </dl>
      </CardSection>
    </section>
  );
}

