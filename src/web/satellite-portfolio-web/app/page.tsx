import { getMonthlyState, getOverview } from "../lib/api";

export default async function OverviewPage() {
  const overview = await getOverview();
  const now = new Date();
  const monthlyState = await getMonthlyState(now.getUTCFullYear(), now.getUTCMonth() + 1);

  return (
    <section>
      <h1>Portfolio Overview</h1>
      <p>As of: {new Date(overview.asOf).toLocaleString()}</p>
      <ul>
        <li>Portfolio value: {overview.portfolioValue.toFixed(2)} EUR</li>
        <li>Cash balance: {overview.cashBalance.toFixed(2)} EUR</li>
        <li>Total market value: {overview.totalMarketValue.toFixed(2)} EUR</li>
        <li>Realized PnL: {overview.realizedPnl.toFixed(2)} EUR</li>
        <li>Unrealized PnL: {overview.unrealizedPnl.toFixed(2)} EUR</li>
      </ul>

      <h2>Current Alerts</h2>
      {overview.currentAlerts.length === 0 ? (
        <p>No active alerts.</p>
      ) : (
        <ul>
          {overview.currentAlerts.map((alert) => (
            <li key={alert.alertEventId}>
              {alert.severity}: {alert.title}
            </li>
          ))}
        </ul>
      )}

      <h2>Month-End Snapshot</h2>
      <ul>
        <li>As of: {new Date(monthlyState.asOf).toLocaleString()}</li>
        <li>Portfolio value: {monthlyState.portfolioValue.toFixed(2)} EUR</li>
        <li>Cash balance: {monthlyState.cashBalance.toFixed(2)} EUR</li>
      </ul>

    </section>
  );
}

