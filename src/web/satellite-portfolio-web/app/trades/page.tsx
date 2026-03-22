"use client";

import { FormEvent, useEffect, useMemo, useState } from "react";
import { CostBasisMode, getCorrectionReasons, getInstruments, getTrades, InstrumentView, LookupItem, TradeSide, TradeView } from "../../lib/api";
import { CardSection, EmptyState, FieldLabel, PageHeader, StatusMessage } from "../components/ui";

const API_BASE_URL = process.env.NEXT_PUBLIC_API_BASE_URL ?? "http://localhost:5014/api";

type CorrectionDraft = {
  quantity: string;
  priceAmount: string;
  feesAmount: string;
  costBasisMode: CostBasisMode;
  customTotalCost: string;
  executedAt: string;
  notes: string;
  correctionReasonLookupId: string;
};

type TradeAuditChain = {
  correctionGroupId: string;
  items: TradeView[];
};

export default function TradesPage() {
  const [trades, setTrades] = useState<TradeView[]>([]);
  const [instruments, setInstruments] = useState<InstrumentView[]>([]);
  const [correctionReasons, setCorrectionReasons] = useState<LookupItem[]>([]);
  const [instrumentId, setInstrumentId] = useState("");
  const [createSide, setCreateSide] = useState<1 | 2 | 3>(1);
  const [createCostBasisMode, setCreateCostBasisMode] = useState<CostBasisMode>("Zero");
  const [createCustomTotalCost, setCreateCustomTotalCost] = useState("");
  const [tradeToCorrect, setTradeToCorrect] = useState<TradeView | null>(null);
  const [lookupError, setLookupError] = useState("");
  const [lookupsLoading, setLookupsLoading] = useState(true);
  const [correctionDraft, setCorrectionDraft] = useState<CorrectionDraft>({
    quantity: "",
    priceAmount: "",
    feesAmount: "0",
    costBasisMode: "Zero",
    customTotalCost: "",
    executedAt: "",
    notes: "",
    correctionReasonLookupId: ""
  });
  const [status, setStatus] = useState("");

  const refresh = async () => {
    const data = await getTrades();
    setTrades(data);
  };

  const getSideLabel = (side: TradeSide): string => {
    if (side === 1 || side === "Buy") {
      return "Buy";
    }

    if (side === 2 || side === "Sell") {
      return "Sell";
    }

    if (side === 3 || side === "NonCashAcquisition") {
      return "Non-cash acquisition";
    }

    return String(side);
  };

  const normalizeTradeSide = (side: TradeSide): 1 | 2 | 3 => {
    if (side === 1 || side === "Buy") {
      return 1;
    }

    if (side === 2 || side === "Sell") {
      return 2;
    }

    return 3;
  };

  const buildCorrectionChains = (history: TradeView[]): TradeAuditChain[] => {
    const grouped = new Map<string, TradeView[]>();
    for (const trade of history) {
      const groupId = trade.correctionGroupId ?? "";
      if (!groupId) {
        continue;
      }

      if (!grouped.has(groupId)) {
        grouped.set(groupId, []);
      }

      grouped.get(groupId)!.push(trade);
    }

    return Array.from(grouped.entries()).map(([correctionGroupId, items]) => ({
      correctionGroupId,
      items: items.sort((left, right) => new Date(left.executedAt).getTime() - new Date(right.executedAt).getTime())
    }));
  };

  const instrumentOptions = useMemo(
    () =>
      instruments.map((instrument) => {
        const id = typeof instrument.id === "string" ? instrument.id : instrument.id.value;
        const label =
          instrument.name && instrument.name.trim().length > 0
            ? `${instrument.symbol} - ${instrument.name}`
            : instrument.symbol;
        return { id, label };
      }),
    [instruments]
  );

  useEffect(() => {
    Promise.all([getInstruments(), getCorrectionReasons(true)])
      .then(([instrumentData, reasonData]) => {
        setInstruments(instrumentData);
        setCorrectionReasons(reasonData);
        setLookupError("");
      })
      .catch(() => setLookupError("Failed to load instrument/correction reason lookups."))
      .finally(() => setLookupsLoading(false));

    refresh().catch(() => setStatus("Failed to load trades."));
  }, []);

  const submitTrade = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    const form = new FormData(event.currentTarget);
    if (!instrumentId) {
      setStatus("Select an instrument before creating a trade.");
      return;
    }

    const isCreateNonCash = createSide === 3;
    if (isCreateNonCash && createCostBasisMode === "Custom" && (!createCustomTotalCost || Number(createCustomTotalCost) < 0)) {
      setStatus("Provide a valid custom total cost for non-cash acquisition.");
      return;
    }

    const payload = {
      instrumentId,
      side: createSide,
      quantity: Number(form.get("quantity")),
      priceAmount: isCreateNonCash ? 0 : Number(form.get("priceAmount")),
      feesAmount: isCreateNonCash ? 0 : Number(form.get("feesAmount")),
      costBasisMode: isCreateNonCash ? createCostBasisMode : null,
      customTotalCost: isCreateNonCash && createCostBasisMode === "Custom" ? Number(createCustomTotalCost) : null,
      executedAt: form.get("executedAt"),
      notes: form.get("notes")
    };

    const response = await fetch(`${API_BASE_URL}/trades`, {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify(payload)
    });

    setStatus(response.ok ? "Trade created." : "Trade creation failed.");
    if (response.ok) {
      await refresh();
    }
  };

  const submitCorrection = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    if (!tradeToCorrect) {
      setStatus("Choose a trade row to edit before saving a correction.");
      return;
    }

    if (!correctionDraft.correctionReasonLookupId) {
      setStatus("Select a correction reason.");
      return;
    }

    const isCorrectionNonCash = normalizeTradeSide(tradeToCorrect.side) === 3;
    if (
      isCorrectionNonCash &&
      correctionDraft.costBasisMode === "Custom" &&
      (!correctionDraft.customTotalCost || Number(correctionDraft.customTotalCost) < 0)
    ) {
      setStatus("Provide a valid custom total cost for non-cash acquisition correction.");
      return;
    }

    const payload = {
      quantity: Number(correctionDraft.quantity),
      priceAmount: isCorrectionNonCash ? 0 : Number(correctionDraft.priceAmount),
      feesAmount: isCorrectionNonCash ? 0 : Number(correctionDraft.feesAmount),
      costBasisMode: isCorrectionNonCash ? correctionDraft.costBasisMode : null,
      customTotalCost:
        isCorrectionNonCash && correctionDraft.costBasisMode === "Custom"
          ? Number(correctionDraft.customTotalCost)
          : null,
      executedAt: correctionDraft.executedAt,
      notes: correctionDraft.notes || null,
      correctionReasonLookupId: correctionDraft.correctionReasonLookupId
    };

    const response = await fetch(`${API_BASE_URL}/trades/${tradeToCorrect.id}/corrections`, {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify(payload)
    });

    setStatus(response.ok ? "Trade corrected." : "Trade correction failed.");
    if (response.ok) {
      setTradeToCorrect(null);
      await refresh();
    }
  };

  const startCorrection = (trade: TradeView) => {
    setTradeToCorrect(trade);
    const tradeSide = normalizeTradeSide(trade.side);
    const tradeCostBasisMode = trade.costBasisMode ?? "Zero";
    setCorrectionDraft({
      quantity: String(trade.quantity),
      priceAmount: String(trade.priceAmount),
      feesAmount: String(trade.feesAmount),
      costBasisMode: tradeCostBasisMode,
      customTotalCost: trade.customTotalCost != null ? String(trade.customTotalCost) : "",
      executedAt: trade.executedAt.slice(0, 16),
      notes: trade.notes ?? "",
      correctionReasonLookupId: trade.correctionReasonLookupId ?? ""
    });
    if (tradeSide !== 3) {
      setCorrectionDraft((current) => ({
        ...current,
        costBasisMode: "Zero",
        customTotalCost: ""
      }));
    }
  };

  const cancelCorrection = () => {
    setTradeToCorrect(null);
    setCorrectionDraft({
      quantity: "",
      priceAmount: "",
      feesAmount: "0",
      costBasisMode: "Zero",
      customTotalCost: "",
      executedAt: "",
      notes: "",
      correctionReasonLookupId: ""
    });
  };

  const correctionChains = buildCorrectionChains(trades);
  const isCreateNonCash = createSide === 3;
  const isCorrectionNonCash = tradeToCorrect ? normalizeTradeSide(tradeToCorrect.side) === 3 : false;

  return (
    <section className="page-stack">
      <PageHeader title="Trades" description="Manual trade entry and auditable correction workflow." />
      <StatusMessage message={status} />

      <div className="grid gap-6 lg:grid-cols-2">
        <CardSection title="Create Trade">
          <form onSubmit={submitTrade} className="grid gap-4 sm:grid-cols-2">
            <div>
              <FieldLabel
                htmlFor="trade-instrument-id"
                label="Instrument"
                tooltip="Select the instrument to trade by symbol and name."
              />
              <select
                id="trade-instrument-id"
                className="input"
                value={instrumentId}
                onChange={(e) => setInstrumentId(e.target.value)}
                required
                disabled={lookupsLoading || !!lookupError}
              >
                <option value="">Select instrument</option>
                {instrumentOptions.map((instrument) => (
                  <option key={instrument.id} value={instrument.id}>
                    {instrument.label}
                  </option>
                ))}
              </select>
            </div>
            <div>
              <FieldLabel
                htmlFor="trade-side"
                label="Side"
                tooltip="Trade direction. Buy increases position and Sell reduces position. Example: Buy"
              />
              <select
                id="trade-side"
                name="side"
                value={createSide}
                className="input"
                onChange={(e) => setCreateSide(Number(e.target.value) as 1 | 2 | 3)}
              >
                <option value="1">Buy</option>
                <option value="2">Sell</option>
                <option value="3">Non-cash acquisition</option>
              </select>
            </div>
            <div>
              <FieldLabel
                htmlFor="trade-quantity"
                label="Quantity"
                tooltip="Number of units traded. Must be greater than zero. Example: 10.5"
              />
              <input id="trade-quantity" name="quantity" type="number" step="0.0001" className="input" required />
            </div>
            <div>
              <FieldLabel
                htmlFor="trade-price"
                label="Price"
                tooltip="Per-unit execution price in instrument currency. Example: 185.42"
              />
              <input
                id="trade-price"
                name="priceAmount"
                type="number"
                step="0.01"
                className="input"
                defaultValue={isCreateNonCash ? "0" : undefined}
                required={!isCreateNonCash}
                disabled={isCreateNonCash}
              />
            </div>
            <div>
              <FieldLabel
                htmlFor="trade-fees"
                label="Fees"
                tooltip="Total fees or commission for this trade. Use 0 when there are none. Example: 1.25"
              />
              <input
                id="trade-fees"
                name="feesAmount"
                type="number"
                step="0.01"
                defaultValue="0"
                className="input"
                disabled={isCreateNonCash}
              />
            </div>
            {isCreateNonCash ? (
              <>
                <div>
                  <FieldLabel
                    htmlFor="trade-cost-basis-mode"
                    label="Cost Basis Mode"
                    tooltip="Choose whether granted shares have zero basis or a custom total basis."
                  />
                  <select
                    id="trade-cost-basis-mode"
                    className="input"
                    value={createCostBasisMode}
                    onChange={(e) => setCreateCostBasisMode(e.target.value as CostBasisMode)}
                  >
                    <option value="Zero">Zero</option>
                    <option value="Custom">Custom</option>
                  </select>
                </div>
                {createCostBasisMode === "Custom" ? (
                  <div>
                    <FieldLabel
                      htmlFor="trade-custom-total-cost"
                      label="Custom Total Cost"
                      tooltip="Optional grant basis used for future realized/unrealized PnL calculations."
                    />
                    <input
                      id="trade-custom-total-cost"
                      type="number"
                      step="0.01"
                      min="0"
                      className="input"
                      value={createCustomTotalCost}
                      onChange={(e) => setCreateCustomTotalCost(e.target.value)}
                      required
                    />
                  </div>
                ) : null}
              </>
            ) : null}
            <div>
              <FieldLabel
                htmlFor="trade-executed-at"
                label="Executed At (UTC)"
                tooltip="Date and time when the trade was executed. Example: 2026-03-21T14:30"
              />
              <input id="trade-executed-at" name="executedAt" type="datetime-local" className="input" required />
            </div>
            <div className="sm:col-span-2">
              <FieldLabel
                htmlFor="trade-notes"
                label="Notes"
                tooltip="Optional context for the trade. Example: Partial fill completed in two lots"
              />
              <input id="trade-notes" name="notes" className="input" />
            </div>
            <div className="sm:col-span-2">
              <button type="submit" className="btn-primary">
                Create
              </button>
            </div>
          </form>
        </CardSection>

        <CardSection title="Correct Trade">
          <form onSubmit={submitCorrection} className="grid gap-4 sm:grid-cols-2">
            <div>
              <FieldLabel
                htmlFor="correction-trade-id"
                label="Selected Trade"
                tooltip="Pick a trade from the table and click Edit to preload this form."
              />
              <input
                id="correction-trade-id"
                className="input"
                value={tradeToCorrect ? `${tradeToCorrect.instrumentLabel} | ${new Date(tradeToCorrect.executedAt).toLocaleString()}` : ""}
                readOnly
              />
            </div>
            <div>
              <FieldLabel
                htmlFor="correction-quantity"
                label="Quantity"
                tooltip="Replacement quantity for the corrected trade. Example: 9.75"
              />
              <input
                id="correction-quantity"
                name="quantity"
                type="number"
                step="0.0001"
                className="input"
                value={correctionDraft.quantity}
                onChange={(e) => setCorrectionDraft((current) => ({ ...current, quantity: e.target.value }))}
                required
              />
            </div>
            <div>
              <FieldLabel
                htmlFor="correction-price"
                label="Price"
                tooltip="Replacement per-unit execution price. Example: 184.95"
              />
              <input
                id="correction-price"
                name="priceAmount"
                type="number"
                step="0.01"
                className="input"
                value={correctionDraft.priceAmount}
                onChange={(e) => setCorrectionDraft((current) => ({ ...current, priceAmount: e.target.value }))}
                required
                disabled={isCorrectionNonCash}
              />
            </div>
            <div>
              <FieldLabel
                htmlFor="correction-fees"
                label="Fees"
                tooltip="Replacement total fees for this corrected trade. Example: 1.00"
              />
              <input
                id="correction-fees"
                name="feesAmount"
                type="number"
                step="0.01"
                className="input"
                value={correctionDraft.feesAmount}
                onChange={(e) => setCorrectionDraft((current) => ({ ...current, feesAmount: e.target.value }))}
                disabled={isCorrectionNonCash}
              />
            </div>
            {isCorrectionNonCash ? (
              <>
                <div>
                  <FieldLabel
                    htmlFor="correction-cost-basis-mode"
                    label="Cost Basis Mode"
                    tooltip="Select zero or custom basis for this non-cash acquisition correction."
                  />
                  <select
                    id="correction-cost-basis-mode"
                    className="input"
                    value={correctionDraft.costBasisMode}
                    onChange={(e) =>
                      setCorrectionDraft((current) => ({ ...current, costBasisMode: e.target.value as CostBasisMode }))
                    }
                  >
                    <option value="Zero">Zero</option>
                    <option value="Custom">Custom</option>
                  </select>
                </div>
                {correctionDraft.costBasisMode === "Custom" ? (
                  <div>
                    <FieldLabel
                      htmlFor="correction-custom-total-cost"
                      label="Custom Total Cost"
                      tooltip="Custom basis amount used for realized and unrealized PnL."
                    />
                    <input
                      id="correction-custom-total-cost"
                      type="number"
                      step="0.01"
                      min="0"
                      className="input"
                      value={correctionDraft.customTotalCost}
                      onChange={(e) => setCorrectionDraft((current) => ({ ...current, customTotalCost: e.target.value }))}
                      required
                    />
                  </div>
                ) : null}
              </>
            ) : null}
            <div>
              <FieldLabel
                htmlFor="correction-executed-at"
                label="Executed At (UTC)"
                tooltip="Replacement execution timestamp for the corrected trade. Example: 2026-03-21T14:30"
              />
              <input
                id="correction-executed-at"
                name="executedAt"
                type="datetime-local"
                className="input"
                value={correctionDraft.executedAt}
                onChange={(e) => setCorrectionDraft((current) => ({ ...current, executedAt: e.target.value }))}
                required
              />
            </div>
            <div>
              <FieldLabel
                htmlFor="correction-reason"
                label="Reason"
                tooltip="Choose an approved correction reason from lookup data."
              />
              <select
                id="correction-reason"
                className="input"
                value={correctionDraft.correctionReasonLookupId}
                onChange={(e) =>
                  setCorrectionDraft((current) => ({ ...current, correctionReasonLookupId: e.target.value }))
                }
                required
              >
                <option value="">Select reason</option>
                {correctionReasons.map((reason) => (
                  <option key={reason.id} value={reason.id}>
                    {reason.name}
                  </option>
                ))}
              </select>
            </div>
            <div className="sm:col-span-2">
              <FieldLabel
                htmlFor="correction-notes"
                label="Notes"
                tooltip="Optional additional context to store with the correction. Example: Ticket #BRK-18423"
              />
              <input
                id="correction-notes"
                name="notes"
                className="input"
                value={correctionDraft.notes}
                onChange={(e) => setCorrectionDraft((current) => ({ ...current, notes: e.target.value }))}
              />
            </div>
            <div className="sm:col-span-2">
              <button type="submit" className="btn-primary">
                Correct
              </button>
              <button type="button" className="btn-secondary ml-2" onClick={cancelCorrection}>
                Cancel
              </button>
            </div>
          </form>
        </CardSection>
      </div>
      {lookupsLoading ? <p className="muted">Loading lookup data...</p> : null}
      {lookupError ? <p className="muted">{lookupError}</p> : null}

      <CardSection title="Trade History">
        {trades.length === 0 ? (
          <EmptyState message="No trades found." />
        ) : (
          <div className="table-wrap">
            <table className="table-base">
              <thead>
                <tr>
                  <th scope="col">Trade ID</th>
                  <th scope="col">Instrument</th>
                  <th scope="col">Side</th>
                  <th scope="col" className="text-right">
                    Quantity
                  </th>
                  <th scope="col" className="text-right">
                    Price
                  </th>
                  <th scope="col" className="text-right">
                    Fees
                  </th>
                  <th scope="col">Cost Basis</th>
                  <th scope="col">Executed At</th>
                  <th scope="col">Correction Reason</th>
                  <th scope="col">Notes</th>
                  <th scope="col">Action</th>
                </tr>
              </thead>
              <tbody>
                {trades.map((trade) => (
                  <tr key={trade.id}>
                    <td>{trade.id}</td>
                    <td>{trade.instrumentLabel}</td>
                    <td>{getSideLabel(trade.side)}</td>
                    <td className="text-right">{trade.quantity}</td>
                    <td className="text-right">{trade.priceAmount}</td>
                    <td className="text-right">{trade.feesAmount}</td>
                    <td>
                      {normalizeTradeSide(trade.side) === 3
                        ? trade.costBasisMode === "Custom"
                          ? `Custom (${trade.customTotalCost ?? 0})`
                          : "Zero"
                        : "-"}
                    </td>
                    <td>{new Date(trade.executedAt).toLocaleString()}</td>
                    <td>{trade.correctionReasonName ?? "-"}</td>
                    <td>{trade.notes ?? "-"}</td>
                    <td>
                      <button type="button" className="btn-secondary" onClick={() => startCorrection(trade)}>
                        Edit
                      </button>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}
      </CardSection>

      <CardSection title="Correction Audit Chains">
        {correctionChains.length === 0 ? (
          <EmptyState message="No correction chains found." />
        ) : (
          <div className="space-y-3">
            {correctionChains.map((chain) => (
              <article key={chain.correctionGroupId} className="rounded-md border border-border bg-surface-muted p-4">
                <h3 className="mb-2">Correction Group: {chain.correctionGroupId}</h3>
                <ol className="list-inside list-decimal space-y-1 text-sm">
                  {chain.items.map((item) => (
                    <li key={item.id}>
                      <span className="font-semibold">{item.isCorrectionReversal ? "Reversal" : "Replacement"}</span>
                      {" - "}
                      {getSideLabel(item.side)} {item.quantity} @ {item.priceAmount}
                      {" | "}
                      {new Date(item.executedAt).toLocaleString()}
                      {item.correctionReasonName ? ` | Reason: ${item.correctionReasonName}` : ""}
                    </li>
                  ))}
                </ol>
              </article>
            ))}
          </div>
        )}
      </CardSection>
    </section>
  );
}

