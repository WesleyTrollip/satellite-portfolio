"use client";

import { FormEvent, useEffect, useState } from "react";
import { getPriceSnapshots, PriceSnapshotView } from "../../lib/api";
import { CardSection, EmptyState, FieldLabel, PageHeader, StatusMessage } from "../components/ui";

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
    <section className="page-stack">
      <PageHeader title="EOD Price Snapshots" description="Capture daily close data and review source-tracked history." />
      <StatusMessage message={status} />

      <div className="grid gap-6 lg:grid-cols-2">
        <CardSection title="Add / Update Snapshot">
          <form onSubmit={upsertPrice} className="grid gap-4">
            <div>
              <FieldLabel
                htmlFor="snapshot-instrument-id"
                label="Instrument ID"
                tooltip="GUID of the instrument this EOD price belongs to. Example: 3f8f0d76-1b4a-4cde-9a37-0b9e9d2f4c12"
              />
              <input id="snapshot-instrument-id" name="instrumentId" className="input" required />
            </div>
            <div>
              <FieldLabel
                htmlFor="snapshot-date"
                label="Date"
                tooltip="Market close date for the snapshot. Example: 2026-03-20"
              />
              <input id="snapshot-date" name="date" type="date" className="input" required />
            </div>
            <div>
              <FieldLabel
                htmlFor="snapshot-close-price"
                label="Close Price"
                tooltip="End-of-day close price in instrument currency. Example: 412.87"
              />
              <input
                id="snapshot-close-price"
                name="closePriceAmount"
                type="number"
                step="0.01"
                className="input"
                required
              />
            </div>
            <div>
              <FieldLabel
                htmlFor="snapshot-source"
                label="Source"
                tooltip="Where the close price came from. Choose Manual, Import, or Other. Example: Import"
              />
              <select id="snapshot-source" name="source" defaultValue="1" className="input">
                <option value="1">Manual</option>
                <option value="2">Import</option>
                <option value="3">Other</option>
              </select>
            </div>
            <div>
              <button type="submit" className="btn-primary">
                Save Snapshot
              </button>
            </div>
          </form>
        </CardSection>

        <CardSection title="List Snapshots">
          <form onSubmit={filterSnapshots} className="grid gap-4 sm:grid-cols-[1fr_auto] sm:items-end">
            <div>
              <FieldLabel
                htmlFor="filter-instrument-id"
                label="Filter Instrument ID"
                tooltip="Optional GUID filter to show snapshots for one instrument only. Example: 3f8f0d76-1b4a-4cde-9a37-0b9e9d2f4c12"
              />
              <input
                id="filter-instrument-id"
                className="input"
                value={instrumentId}
                onChange={(e) => setInstrumentId(e.target.value)}
              />
            </div>
            <div>
              <button type="submit" className="btn-secondary">
                Apply Filter
              </button>
            </div>
          </form>
        </CardSection>
      </div>

      <CardSection title="Snapshot Results">
        {snapshots.length === 0 ? (
          <EmptyState message="No snapshots found." />
        ) : (
          <div className="table-wrap">
            <table className="table-base">
              <thead>
                <tr>
                  <th scope="col">Snapshot ID</th>
                  <th scope="col">Instrument ID</th>
                  <th scope="col">Date</th>
                  <th scope="col" className="text-right">
                    Close Price
                  </th>
                  <th scope="col">Source</th>
                </tr>
              </thead>
              <tbody>
                {snapshots.map((snapshot) => (
                  <tr key={typeof snapshot.id === "string" ? snapshot.id : snapshot.id.value}>
                    <td>{typeof snapshot.id === "string" ? snapshot.id : snapshot.id.value}</td>
                    <td>{typeof snapshot.instrumentId === "string" ? snapshot.instrumentId : snapshot.instrumentId.value}</td>
                    <td>{snapshot.date}</td>
                    <td className="text-right">{snapshot.closePriceAmount.toFixed(2)}</td>
                    <td>{snapshot.source}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}
      </CardSection>
    </section>
  );
}

