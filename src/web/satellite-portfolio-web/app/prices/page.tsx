"use client";

import { FormEvent, useEffect, useMemo, useState } from "react";
import { getInstruments, getPriceSnapshots, getPriceSources, InstrumentView, LookupItem, PriceSnapshotView } from "../../lib/api";
import { CardSection, EmptyState, FieldLabel, PageHeader, StatusMessage } from "../components/ui";

const API_BASE_URL = process.env.NEXT_PUBLIC_API_BASE_URL ?? "http://localhost:5014/api";

export default function PricesPage() {
  const [snapshots, setSnapshots] = useState<PriceSnapshotView[]>([]);
  const [instruments, setInstruments] = useState<InstrumentView[]>([]);
  const [priceSources, setPriceSources] = useState<LookupItem[]>([]);
  const [instrumentId, setInstrumentId] = useState("");
  const [upsertInstrumentId, setUpsertInstrumentId] = useState("");
  const [upsertSourceId, setUpsertSourceId] = useState("");
  const [upsertDate, setUpsertDate] = useState("");
  const [upsertClosePrice, setUpsertClosePrice] = useState("");
  const [editingSnapshot, setEditingSnapshot] = useState<PriceSnapshotView | null>(null);
  const [lookupsLoading, setLookupsLoading] = useState(true);
  const [lookupError, setLookupError] = useState("");
  const [status, setStatus] = useState("");

  const refresh = async () => {
    const data = await getPriceSnapshots(instrumentId || undefined);
    setSnapshots(data);
  };

  useEffect(() => {
    Promise.all([getInstruments(), getPriceSources(true)])
      .then(([instrumentData, sourceData]) => {
        setInstruments(instrumentData);
        setPriceSources(sourceData);
        setLookupError("");
      })
      .catch(() => setLookupError("Failed to load instrument/price source lookup data."))
      .finally(() => setLookupsLoading(false));

    refresh().catch(() => setStatus("Failed to load price snapshots."));
  }, []);

  const instrumentOptions = useMemo(
    () =>
      instruments.map((instrument) => {
        const id = typeof instrument.id === "string" ? instrument.id : instrument.id.value;
        const label = instrument.name ? `${instrument.symbol} - ${instrument.name}` : instrument.symbol;
        return { id, label };
      }),
    [instruments]
  );

  const upsertPrice = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    if (!upsertInstrumentId || !upsertSourceId) {
      setStatus("Instrument and source are required.");
      return;
    }
    const payload = {
      instrumentId: upsertInstrumentId,
      date: upsertDate,
      closePriceAmount: Number(upsertClosePrice),
      priceSourceLookupId: upsertSourceId
    };

    const response = await fetch(`${API_BASE_URL}/prices/snapshots`, {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify(payload)
    });

    setStatus(response.ok ? "Price snapshot saved." : "Failed to save price snapshot.");
    if (response.ok) {
      setEditingSnapshot(null);
      await refresh();
    }
  };

  const filterSnapshots = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    await refresh();
  };

  const startEdit = (snapshot: PriceSnapshotView) => {
    setEditingSnapshot(snapshot);
    setUpsertInstrumentId(snapshot.instrumentId);
    setUpsertSourceId(snapshot.priceSourceLookupId);
    setUpsertDate(snapshot.date);
    setUpsertClosePrice(String(snapshot.closePriceAmount));
  };

  const cancelEdit = () => {
    setEditingSnapshot(null);
    setUpsertInstrumentId("");
    setUpsertSourceId("");
    setUpsertDate("");
    setUpsertClosePrice("");
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
                label="Instrument"
                tooltip="Select the instrument to attach this snapshot to."
              />
              <select
                id="snapshot-instrument-id"
                name="instrumentId"
                className="input"
                value={upsertInstrumentId}
                onChange={(e) => setUpsertInstrumentId(e.target.value)}
                required
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
                htmlFor="snapshot-date"
                label="Date"
                tooltip="Market close date for the snapshot. Example: 2026-03-20"
              />
              <input
                id="snapshot-date"
                name="date"
                type="date"
                className="input"
                value={upsertDate}
                onChange={(e) => setUpsertDate(e.target.value)}
                required
              />
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
                value={upsertClosePrice}
                onChange={(e) => setUpsertClosePrice(e.target.value)}
                required
              />
            </div>
            <div>
              <FieldLabel
                htmlFor="snapshot-source"
                label="Source"
                tooltip="Select the managed lookup value that describes the source."
              />
              <select
                id="snapshot-source"
                name="source"
                value={upsertSourceId}
                onChange={(e) => setUpsertSourceId(e.target.value)}
                className="input"
              >
                <option value="">Select source</option>
                {priceSources.map((source) => (
                  <option key={source.id} value={source.id}>
                    {source.name}
                  </option>
                ))}
              </select>
            </div>
            <div>
              <button type="submit" className="btn-primary">
                {editingSnapshot ? "Save Snapshot Update" : "Save Snapshot"}
              </button>
              <button type="button" className="btn-secondary ml-2" onClick={cancelEdit}>
                Cancel
              </button>
            </div>
          </form>
        </CardSection>

        <CardSection title="List Snapshots">
          <form onSubmit={filterSnapshots} className="grid gap-4 sm:grid-cols-[1fr_auto] sm:items-end">
            <div>
              <FieldLabel
                htmlFor="filter-instrument-id"
                label="Filter Instrument"
                tooltip="Optional instrument filter for snapshot results."
              />
              <select
                id="filter-instrument-id"
                className="input"
                value={instrumentId}
                onChange={(e) => setInstrumentId(e.target.value)}
              >
                <option value="">All instruments</option>
                {instrumentOptions.map((instrument) => (
                  <option key={instrument.id} value={instrument.id}>
                    {instrument.label}
                  </option>
                ))}
              </select>
            </div>
            <div>
              <button type="submit" className="btn-secondary">
                Apply Filter
              </button>
            </div>
          </form>
        </CardSection>
      </div>
      {lookupsLoading ? <p className="muted">Loading lookup data...</p> : null}
      {lookupError ? <p className="muted">{lookupError}</p> : null}

      <CardSection title="Snapshot Results">
        {snapshots.length === 0 ? (
          <EmptyState message="No snapshots found." />
        ) : (
          <div className="table-wrap">
            <table className="table-base">
              <thead>
                <tr>
                  <th scope="col">Snapshot ID</th>
                  <th scope="col">Instrument</th>
                  <th scope="col">Date</th>
                  <th scope="col" className="text-right">
                    Close Price
                  </th>
                  <th scope="col">Source</th>
                  <th scope="col">Action</th>
                </tr>
              </thead>
              <tbody>
                {snapshots.map((snapshot) => (
                  <tr key={snapshot.id}>
                    <td>{snapshot.id}</td>
                    <td>{snapshot.instrumentLabel}</td>
                    <td>{snapshot.date}</td>
                    <td className="text-right">{snapshot.closePriceAmount.toFixed(2)}</td>
                    <td>{snapshot.priceSourceName}</td>
                    <td>
                      <button type="button" className="btn-secondary" onClick={() => startEdit(snapshot)}>
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
    </section>
  );
}

