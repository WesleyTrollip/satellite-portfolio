"use client";

import { FormEvent, useEffect, useState } from "react";

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

  return (
    <section>
      <h1>Trades</h1>
      <p>Manual trade entry and auditable correction workflow.</p>
      <p>{status}</p>

      <h2>Create Trade</h2>
      <form onSubmit={submitTrade}>
        <label>
          Instrument ID
          <input value={instrumentId} onChange={(e) => setInstrumentId(e.target.value)} required />
        </label>
        <label>
          Side
          <select name="side" defaultValue="1">
            <option value="1">Buy</option>
            <option value="2">Sell</option>
          </select>
        </label>
        <label>
          Quantity
          <input name="quantity" type="number" step="0.0001" required />
        </label>
        <label>
          Price
          <input name="priceAmount" type="number" step="0.01" required />
        </label>
        <label>
          Fees
          <input name="feesAmount" type="number" step="0.01" defaultValue="0" />
        </label>
        <label>
          Executed At (UTC)
          <input name="executedAt" type="datetime-local" required />
        </label>
        <label>
          Notes
          <input name="notes" />
        </label>
        <button type="submit">Create</button>
      </form>

      <h2>Correct Trade</h2>
      <form onSubmit={submitCorrection}>
        <label>
          Trade ID
          <input value={tradeIdToCorrect} onChange={(e) => setTradeIdToCorrect(e.target.value)} required />
        </label>
        <label>
          Quantity
          <input name="quantity" type="number" step="0.0001" required />
        </label>
        <label>
          Price
          <input name="priceAmount" type="number" step="0.01" required />
        </label>
        <label>
          Fees
          <input name="feesAmount" type="number" step="0.01" defaultValue="0" />
        </label>
        <label>
          Executed At (UTC)
          <input name="executedAt" type="datetime-local" required />
        </label>
        <label>
          Notes
          <input name="notes" />
        </label>
        <label>
          Reason
          <input name="reason" required />
        </label>
        <button type="submit">Correct</button>
      </form>

      <h2>Trade History</h2>
      {trades.length === 0 ? (
        <p>No trades found.</p>
      ) : (
        <table style={{ borderCollapse: "collapse", width: "100%" }}>
          <thead>
            <tr>
              <th align="left">Trade ID</th>
              <th align="left">Instrument ID</th>
              <th align="left">Side</th>
              <th align="right">Quantity</th>
              <th align="right">Price</th>
              <th align="right">Fees</th>
              <th align="left">Executed At</th>
              <th align="left">Notes</th>
            </tr>
          </thead>
          <tbody>
            {trades.map((trade) => (
              <tr key={getId(trade.id)}>
                <td>{getId(trade.id)}</td>
                <td>{getId(trade.instrumentId)}</td>
                <td>{getSideLabel(trade.side)}</td>
                <td align="right">{trade.quantity}</td>
                <td align="right">{trade.priceAmount}</td>
                <td align="right">{trade.feesAmount}</td>
                <td>{new Date(trade.executedAt).toLocaleString()}</td>
                <td>{trade.notes ?? "-"}</td>
              </tr>
            ))}
          </tbody>
        </table>
      )}

      <h2>Correction Audit Chains</h2>
      {buildCorrectionChains(trades).length === 0 ? (
        <p>No correction chains found.</p>
      ) : (
        <div>
          {buildCorrectionChains(trades).map((chain) => (
            <article key={chain.correctionGroupId} style={{ border: "1px solid #334155", padding: "0.75rem", marginBottom: "0.75rem" }}>
              <h3>Correction Group: {chain.correctionGroupId}</h3>
              <ol>
                {chain.items.map((item) => (
                  <li key={getId(item.id)}>
                    <strong>{item.isCorrectionReversal ? "Reversal" : "Replacement"}</strong>
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
    </section>
  );
}

