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

export type LookupItem = {
  id: string;
  code: string;
  name: string;
  isActive: boolean;
  createdAt: string;
  updatedAt: string;
};

export type LookupUpsertInput = {
  code: string;
  name: string;
  isActive: boolean;
};

export type InstrumentUpsertInput = {
  symbol: string;
  name: string | null;
  sectorLookupId: string | null;
  currency: string;
};

export type InstrumentView = {
  id: { value: string } | string;
  symbol: string;
  name: string | null;
  sector: string | null;
  sectorLookupId?: { value: string } | string | null;
  currency: string;
  createdAt: string;
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
  id: string;
  instrumentId: string;
  instrumentSymbol: string;
  instrumentLabel: string;
  date: string;
  closePriceAmount: number;
  priceSourceLookupId: string;
  priceSourceName: string;
};

export type TradeSide = 1 | 2 | 3 | "Buy" | "Sell" | "NonCashAcquisition";
export type CostBasisMode = "Zero" | "Custom";

export type TradeView = {
  id: string;
  instrumentId: string;
  instrumentSymbol: string;
  instrumentLabel: string;
  side: TradeSide;
  quantity: number;
  priceAmount: number;
  feesAmount: number;
  costBasisMode?: CostBasisMode | null;
  customTotalCost?: number | null;
  executedAt: string;
  notes?: string | null;
  correctionGroupId?: string | null;
  correctedByTradeId?: string | null;
  isCorrectionReversal?: boolean;
  correctionReasonLookupId?: string | null;
  correctionReasonName?: string | null;
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

export async function getTrades(): Promise<TradeView[]> {
  const response = await fetch(`${API_BASE_URL}/trades`, { cache: "no-store" });
  if (!response.ok) {
    throw new Error("Failed to load trades.");
  }

  return response.json();
}

export async function getInstruments(): Promise<InstrumentView[]> {
  const response = await fetch(`${API_BASE_URL}/instruments`, { cache: "no-store" });
  if (!response.ok) {
    throw new Error("Failed to load instruments.");
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

export async function getSectors(isActive?: boolean): Promise<LookupItem[]> {
  const query = typeof isActive === "boolean" ? `?isActive=${isActive}` : "";
  const response = await fetch(`${API_BASE_URL}/lookups/sectors${query}`, { cache: "no-store" });
  if (!response.ok) {
    throw new Error("Failed to load sectors.");
  }

  return response.json();
}

export async function getPriceSources(isActive?: boolean): Promise<LookupItem[]> {
  const query = typeof isActive === "boolean" ? `?isActive=${isActive}` : "";
  const response = await fetch(`${API_BASE_URL}/lookups/price-sources${query}`, { cache: "no-store" });
  if (!response.ok) {
    throw new Error("Failed to load price sources.");
  }

  return response.json();
}

export async function getCorrectionReasons(isActive?: boolean): Promise<LookupItem[]> {
  const query = typeof isActive === "boolean" ? `?isActive=${isActive}` : "";
  const response = await fetch(`${API_BASE_URL}/lookups/correction-reasons${query}`, { cache: "no-store" });
  if (!response.ok) {
    throw new Error("Failed to load correction reasons.");
  }

  return response.json();
}

const getInstrumentId = (value: { value: string } | string): string => (typeof value === "string" ? value : value.value);

async function extractErrorMessage(response: Response, fallback: string): Promise<string> {
  try {
    const payload = (await response.json()) as { message?: string; title?: string };
    return payload.message ?? payload.title ?? fallback;
  } catch {
    return fallback;
  }
}

export async function createSector(input: LookupUpsertInput): Promise<LookupItem> {
  const response = await fetch(`${API_BASE_URL}/lookups/sectors`, {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify(input)
  });
  if (!response.ok) {
    throw new Error(await extractErrorMessage(response, "Failed to create sector."));
  }

  return response.json();
}

export async function updateSector(id: string, input: LookupUpsertInput): Promise<LookupItem> {
  const response = await fetch(`${API_BASE_URL}/lookups/sectors/${id}`, {
    method: "PUT",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify(input)
  });
  if (!response.ok) {
    throw new Error(await extractErrorMessage(response, "Failed to update sector."));
  }

  return response.json();
}

export async function deleteSector(id: string): Promise<void> {
  const response = await fetch(`${API_BASE_URL}/lookups/sectors/${id}`, { method: "DELETE" });
  if (!response.ok) {
    throw new Error(await extractErrorMessage(response, "Failed to delete sector."));
  }
}

export async function createPriceSource(input: LookupUpsertInput): Promise<LookupItem> {
  const response = await fetch(`${API_BASE_URL}/lookups/price-sources`, {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify(input)
  });
  if (!response.ok) {
    throw new Error(await extractErrorMessage(response, "Failed to create price source."));
  }

  return response.json();
}

export async function updatePriceSource(id: string, input: LookupUpsertInput): Promise<LookupItem> {
  const response = await fetch(`${API_BASE_URL}/lookups/price-sources/${id}`, {
    method: "PUT",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify(input)
  });
  if (!response.ok) {
    throw new Error(await extractErrorMessage(response, "Failed to update price source."));
  }

  return response.json();
}

export async function deletePriceSource(id: string): Promise<void> {
  const response = await fetch(`${API_BASE_URL}/lookups/price-sources/${id}`, { method: "DELETE" });
  if (!response.ok) {
    throw new Error(await extractErrorMessage(response, "Failed to delete price source."));
  }
}

export async function createCorrectionReason(input: LookupUpsertInput): Promise<LookupItem> {
  const response = await fetch(`${API_BASE_URL}/lookups/correction-reasons`, {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify(input)
  });
  if (!response.ok) {
    throw new Error(await extractErrorMessage(response, "Failed to create correction reason."));
  }

  return response.json();
}

export async function updateCorrectionReason(id: string, input: LookupUpsertInput): Promise<LookupItem> {
  const response = await fetch(`${API_BASE_URL}/lookups/correction-reasons/${id}`, {
    method: "PUT",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify(input)
  });
  if (!response.ok) {
    throw new Error(await extractErrorMessage(response, "Failed to update correction reason."));
  }

  return response.json();
}

export async function deleteCorrectionReason(id: string): Promise<void> {
  const response = await fetch(`${API_BASE_URL}/lookups/correction-reasons/${id}`, { method: "DELETE" });
  if (!response.ok) {
    throw new Error(await extractErrorMessage(response, "Failed to delete correction reason."));
  }
}

export async function createInstrument(input: InstrumentUpsertInput): Promise<InstrumentView> {
  const response = await fetch(`${API_BASE_URL}/instruments`, {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify(input)
  });
  if (!response.ok) {
    throw new Error(await extractErrorMessage(response, "Failed to create instrument."));
  }

  return response.json();
}

export async function updateInstrument(id: string, input: InstrumentUpsertInput): Promise<InstrumentView> {
  const response = await fetch(`${API_BASE_URL}/instruments/${id}`, {
    method: "PUT",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify(input)
  });
  if (!response.ok) {
    throw new Error(await extractErrorMessage(response, "Failed to update instrument."));
  }

  return response.json();
}

export async function deleteInstrument(id: string): Promise<void> {
  const response = await fetch(`${API_BASE_URL}/instruments/${id}`, { method: "DELETE" });
  if (!response.ok) {
    throw new Error(await extractErrorMessage(response, "Failed to delete instrument."));
  }
}

export function normalizeInstrumentId(instrument: InstrumentView): string {
  return getInstrumentId(instrument.id);
}

