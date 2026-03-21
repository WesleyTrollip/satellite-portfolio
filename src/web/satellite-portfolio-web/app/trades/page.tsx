"use client";

import { FormEvent, useEffect, useState } from "react";
import { CardSection, EmptyState, FieldLabel, PageHeader, StatusMessage } from "../components/ui";

const API_BASE_URL = process.env.NEXT_PUBLIC_API_BASE_URL ?? "http://localhost:5014/api";

type Trade = {
  id: { value: string } | string;
  instrumentId: { value: string } | string;
  side: number | string;
  quantity: number;
  priceAmount: number;
  feesAmount: number;
  executedAt: string;
  notes?: string;
  correctionGroupId?: { value: string } | string | null;
  correctedByTradeId?: { value: string } | string | null;
  isCorrectionReversal?: boolean;
};

type TradeAuditChain = {
  correctionGroupId: string;
  items: Trade[];
};

export default function TradesPage() {
  const [trades, setTrades] = useState<Trade[]>([]);
  const [instrumentId, setInstrumentId] = useState("");
  const [tradeIdToCorrect, setTradeIdToCorrect] = useState("");
  const [status, setStatus] = useState("");

  const refresh = async () => {
    const response = await fetch(`${API_BASE_URL}/trades`);
    const data = await response.json();
    setTrades(data);
  };

  const getId = (value: { value: string } | string | null | undefined): string =>
    value == null ? "" : typeof value === "string" ? value : value.value;

  const getSideLabel = (side: number | string): string => {
    if (side === 1 || side === "1" || side === "Buy") {
      return "Buy";
    }

    if (side === 2 || side === "2" || side === "Sell") {
      return "Sell";
    }

    return String(side);
  };

  const getCorrectionReason = (notes: string | undefined): string | null => {
    if (!notes) {
      return null;
    }

    const marker = "Correction reason:";
    const index = notes.indexOf(marker);
    if (index < 0) {
      return null;
    }

    return notes.substring(index + marker.length).trim();
  };

  const buildCorrectionChains = (history: Trade[]): TradeAuditChain[] => {
    const grouped = new Map<string, Trade[]>();
    for (const trade of history) {
      const groupId = getId(trade.correctionGroupId ?? null);
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

  useEffect(() => {
    refresh().catch(() => setStatus("Failed to load trades."));
  }, []);

  const submitTrade = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    const form = new FormData(event.currentTarget);
    const payload = {
      instrumentId,
      side: form.get("side"),
      quantity: Number(form.get("quantity")),
      priceAmount: Number(form.get("priceAmount")),
      feesAmount: Number(form.get("feesAmount")),
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
    const form = new FormData(event.currentTarget);
    const payload = {
      quantity: Number(form.get("quantity")),
      priceAmount: Number(form.get("priceAmount")),
      feesAmount: Number(form.get("feesAmount")),
      executedAt: form.get("executedAt"),
      notes: form.get("notes"),
      reason: form.get("reason")
    };

    const response = await fetch(`${API_BASE_URL}/trades/${tradeIdToCorrect}/corrections`, {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify(payload)
    });

    setStatus(response.ok ? "Trade corrected." : "Trade correction failed.");
    if (response.ok) {
      await refresh();
    }
  };

  const correctionChains = buildCorrectionChains(trades);

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
                label="Instrument ID"
                tooltip="Unique GUID of the instrument being traded. Example: 3f8f0d76-1b4a-4cde-9a37-0b9e9d2f4c12"
              />
              <input
                id="trade-instrument-id"
                className="input"
                value={instrumentId}
                onChange={(e) => setInstrumentId(e.target.value)}
                required
              />
            </div>
            <div>
              <FieldLabel
                htmlFor="trade-side"
                label="Side"
                tooltip="Trade direction. Buy increases position and Sell reduces position. Example: Buy"
              />
              <select id="trade-side" name="side" defaultValue="1" className="input">
                <option value="1">Buy</option>
                <option value="2">Sell</option>
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
              <input id="trade-price" name="priceAmount" type="number" step="0.01" className="input" required />
            </div>
            <div>
              <FieldLabel
                htmlFor="trade-fees"
                label="Fees"
                tooltip="Total fees or commission for this trade. Use 0 when there are none. Example: 1.25"
              />
              <input id="trade-fees" name="feesAmount" type="number" step="0.01" defaultValue="0" className="input" />
            </div>
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
                label="Trade ID"
                tooltip="GUID of the existing trade to correct. Example: 7e6af4b0-3d32-4c9a-89a4-9d4b4f9270d8"
              />
              <input
                id="correction-trade-id"
                className="input"
                value={tradeIdToCorrect}
                onChange={(e) => setTradeIdToCorrect(e.target.value)}
                required
              />
            </div>
            <div>
              <FieldLabel
                htmlFor="correction-quantity"
                label="Quantity"
                tooltip="Replacement quantity for the corrected trade. Example: 9.75"
              />
              <input id="correction-quantity" name="quantity" type="number" step="0.0001" className="input" required />
            </div>
            <div>
              <FieldLabel
                htmlFor="correction-price"
                label="Price"
                tooltip="Replacement per-unit execution price. Example: 184.95"
              />
              <input id="correction-price" name="priceAmount" type="number" step="0.01" className="input" required />
            </div>
            <div>
              <FieldLabel
                htmlFor="correction-fees"
                label="Fees"
                tooltip="Replacement total fees for this corrected trade. Example: 1.00"
              />
              <input id="correction-fees" name="feesAmount" type="number" step="0.01" defaultValue="0" className="input" />
            </div>
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
                required
              />
            </div>
            <div>
              <FieldLabel
                htmlFor="correction-reason"
                label="Reason"
                tooltip="Required audit reason for this correction. Example: Broker confirmed wrong fill quantity"
              />
              <input id="correction-reason" name="reason" className="input" required />
            </div>
            <div className="sm:col-span-2">
              <FieldLabel
                htmlFor="correction-notes"
                label="Notes"
                tooltip="Optional additional context to store with the correction. Example: Ticket #BRK-18423"
              />
              <input id="correction-notes" name="notes" className="input" />
            </div>
            <div className="sm:col-span-2">
              <button type="submit" className="btn-primary">
                Correct
              </button>
            </div>
          </form>
        </CardSection>
      </div>

      <CardSection title="Trade History">
        {trades.length === 0 ? (
          <EmptyState message="No trades found." />
        ) : (
          <div className="table-wrap">
            <table className="table-base">
              <thead>
                <tr>
                  <th scope="col">Trade ID</th>
                  <th scope="col">Instrument ID</th>
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
                  <th scope="col">Executed At</th>
                  <th scope="col">Notes</th>
                </tr>
              </thead>
              <tbody>
                {trades.map((trade) => (
                  <tr key={getId(trade.id)}>
                    <td>{getId(trade.id)}</td>
                    <td>{getId(trade.instrumentId)}</td>
                    <td>{getSideLabel(trade.side)}</td>
                    <td className="text-right">{trade.quantity}</td>
                    <td className="text-right">{trade.priceAmount}</td>
                    <td className="text-right">{trade.feesAmount}</td>
                    <td>{new Date(trade.executedAt).toLocaleString()}</td>
                    <td>{trade.notes ?? "-"}</td>
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
                    <li key={getId(item.id)}>
                      <span className="font-semibold">{item.isCorrectionReversal ? "Reversal" : "Replacement"}</span>
                      {" - "}
                      {getSideLabel(item.side)} {item.quantity} @ {item.priceAmount}
                      {" | "}
                      {new Date(item.executedAt).toLocaleString()}
                      {getCorrectionReason(item.notes) ? ` | Reason: ${getCorrectionReason(item.notes)}` : ""}
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

