"use client";

import { FormEvent, useEffect, useState } from "react";

const API_BASE_URL = process.env.NEXT_PUBLIC_API_BASE_URL ?? "http://localhost:5014/api";

type Trade = {
  id: { value: string } | string;
  instrumentId: { value: string } | string;
  side: string;
  quantity: number;
  priceAmount: number;
  feesAmount: number;
  executedAt: string;
  notes?: string;
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
      {trades.length === 0 ? <p>No trades found.</p> : <pre>{JSON.stringify(trades, null, 2)}</pre>}
    </section>
  );
}

