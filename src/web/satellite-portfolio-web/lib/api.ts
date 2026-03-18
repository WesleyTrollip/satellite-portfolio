const API_BASE_URL = process.env.NEXT_PUBLIC_API_BASE_URL ?? "http://localhost:62356/api";

export type HoldingView = {
  instrumentId: string;
  symbol: string;
  sector: string | null;
  quantity: number;
  averageCost: number;
  marketValue: number;
  unrealizedPnl: number;
  allocationPercent: number;
  missingPrice: boolean;
  missingPriceExplanation: string | null;
};

export type JournalEntryView = {
  entry: {
    id: { value: string } | string;
    occurredAt: string;
    title: string;
    body: string;
    tags: string | null;
  };
  thesisIds: string[];
  instrumentIds: string[];
};

export type ThesisView = {
  id: { value: string } | string;
  title: string;
  body: string;
  status: string;
  instrumentId?: { value: string } | string | null;
};

export type RuleView = {
  id: { value: string } | string;
  type: string;
  enabled: boolean;
  parametersJson: string;
};

export type AlertEventView = {
  id: { value: string } | string;
  ruleId: { value: string } | string;
  severity: string;
  title: string;
  triggeredAt: string;
  detailsJson: string;
};

export type PriceSnapshotView = {
  id: { value: string } | string;
  instrumentId: { value: string } | string;
  date: string;
  closePriceAmount: number;
  source: string;
};

export type PortfolioOverviewView = {
  asOf: string;
  cashBalance: number;
  totalMarketValue: number;
  portfolioValue: number;
  totalCost: number;
  realizedPnl: number;
  unrealizedPnl: number;
  holdings: HoldingView[];
  sectorAllocations: { sector: string; allocationPercent: number }[];
  currentAlerts: { alertEventId: string; severity: string; title: string; triggeredAt: string }[];
};

export type MonthlyPortfolioStateView = {
  year: number;
  month: number;
  asOf: string;
  cashBalance: number;
  totalMarketValue: number;
  portfolioValue: number;
  totalCost: number;
  realizedPnl: number;
  unrealizedPnl: number;
  holdings: HoldingView[];
  sectorAllocations: { sector: string; allocationPercent: number }[];
};

export async function getOverview(): Promise<PortfolioOverviewView> {
  const response = await fetch(`${API_BASE_URL}/portfolio/overview`, { cache: "no-store" });
  if (!response.ok) {
    throw new Error("Failed to load overview.");
  }

  return response.json();
}

export async function getHoldings(): Promise<HoldingView[]> {
  const response = await fetch(`${API_BASE_URL}/holdings`, { cache: "no-store" });
  if (!response.ok) {
    throw new Error("Failed to load holdings.");
  }

  return response.json();
}

export async function getTrades(): Promise<Array<Record<string, unknown>>> {
  const response = await fetch(`${API_BASE_URL}/trades`, { cache: "no-store" });
  if (!response.ok) {
    throw new Error("Failed to load trades.");
  }

  return response.json();
}

export async function getMonthlyState(year: number, month: number): Promise<MonthlyPortfolioStateView> {
  const response = await fetch(`${API_BASE_URL}/portfolio/monthly?year=${year}&month=${month}`, { cache: "no-store" });
  if (!response.ok) {
    throw new Error("Failed to load monthly state.");
  }

  return response.json();
}

export async function getJournalEntries(): Promise<JournalEntryView[]> {
  const response = await fetch(`${API_BASE_URL}/journal`, { cache: "no-store" });
  if (!response.ok) {
    throw new Error("Failed to load journal entries.");
  }

  return response.json();
}

export async function getTheses(): Promise<ThesisView[]> {
  const response = await fetch(`${API_BASE_URL}/theses`, { cache: "no-store" });
  if (!response.ok) {
    throw new Error("Failed to load theses.");
  }

  return response.json();
}

export async function getRules(): Promise<RuleView[]> {
  const response = await fetch(`${API_BASE_URL}/rules`, { cache: "no-store" });
  if (!response.ok) {
    throw new Error("Failed to load rules.");
  }

  return response.json();
}

export async function getCurrentAlerts(): Promise<AlertEventView[]> {
  const response = await fetch(`${API_BASE_URL}/alerts/current`, { cache: "no-store" });
  if (!response.ok) {
    throw new Error("Failed to load current alerts.");
  }

  return response.json();
}

export async function getPriceSnapshots(instrumentId?: string): Promise<PriceSnapshotView[]> {
  const query = instrumentId ? `?instrumentId=${instrumentId}` : "";
  const response = await fetch(`${API_BASE_URL}/prices/snapshots${query}`, { cache: "no-store" });
  if (!response.ok) {
    throw new Error("Failed to load price snapshots.");
  }

  return response.json();
}

