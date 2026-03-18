const API_BASE_URL = process.env.NEXT_PUBLIC_API_BASE_URL ?? "http://localhost:5014/api";

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

