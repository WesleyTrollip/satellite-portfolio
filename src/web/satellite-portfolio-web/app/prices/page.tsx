"use client";

import { FormEvent, useEffect, useState } from "react";
import { getPriceSnapshots, PriceSnapshotView } from "../../lib/api";

const API_BASE_URL = process.env.NEXT_PUBLIC_API_BASE_URL ?? "http://localhost:5014/api";

export default function PricesPage() {
  const [snapshots, setSnapshots] = useState<PriceSnapshotView[]>([]);
  const [instrumentId, setInstrumentId] = useState("");
  const [status, setStatus] = useState("");

  const refresh = async () => {
    const data = await getPriceSnapshots(instrumentId || undefined);
    setSnapshots(data);
  };

  useEffect(() => {
    refresh().catch(() => setStatus("Failed to load price snapshots."));
  }, []);

  const upsertPrice = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    const form = new FormData(event.currentTarget);
    const payload = {
      instrumentId: form.get("instrumentId"),
      date: form.get("date"),
      closePriceAmount: Number(form.get("closePriceAmount")),
      source: Number(form.get("source"))
    };

    const response = await fetch(`${API_BASE_URL}/prices/snapshots`, {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify(payload)
    });

    setStatus(response.ok ? "Price snapshot saved." : "Failed to save price snapshot.");
    if (response.ok) {
      await refresh();
    }
  };

  const filterSnapshots = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    await refresh();
  };

  return (
    <section>
      <h1>EOD Price Snapshots</h1>
      <p>{status}</p>

      <h2>Add / Update Snapshot</h2>
      <form onSubmit={upsertPrice}>
        <label>
          Instrument ID
          <input name="instrumentId" required />
        </label>
        <label>
          Date
          <input name="date" type="date" required />
        </label>
        <label>
          Close Price
          <input name="closePriceAmount" type="number" step="0.01" required />
        </label>
        <label>
          Source
          <select name="source" defaultValue="1">
            <option value="1">Manual</option>
            <option value="2">Import</option>
            <option value="3">Other</option>
          </select>
        </label>
        <button type="submit">Save Snapshot</button>
      </form>

      <h2>List Snapshots</h2>
      <form onSubmit={filterSnapshots}>
        <label>
          Filter Instrument ID
          <input value={instrumentId} onChange={(e) => setInstrumentId(e.target.value)} />
        </label>
        <button type="submit">Apply Filter</button>
      </form>

      {snapshots.length === 0 ? <p>No snapshots found.</p> : <pre>{JSON.stringify(snapshots, null, 2)}</pre>}
    </section>
  );
}

