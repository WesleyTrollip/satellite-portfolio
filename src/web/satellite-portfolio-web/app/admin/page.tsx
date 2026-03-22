"use client";

import { FormEvent, useEffect, useMemo, useState } from "react";
import {
  createCorrectionReason,
  createInstrument,
  createPriceSource,
  createSector,
  deleteCorrectionReason,
  deleteInstrument,
  deletePriceSource,
  deleteSector,
  getCorrectionReasons,
  getInstruments,
  getPriceSources,
  getSectors,
  InstrumentView,
  LookupItem,
  normalizeInstrumentId,
  updateCorrectionReason,
  updateInstrument,
  updatePriceSource,
  updateSector
} from "../../lib/api";
import { AdminLookupSection } from "../components/admin-lookup-section";
import { CardSection, EmptyState, FieldLabel, PageHeader } from "../components/ui";

type InstrumentFormState = {
  id: string | null;
  symbol: string;
  name: string;
  sectorLookupId: string;
  currency: string;
};

const initialInstrumentForm: InstrumentFormState = {
  id: null,
  symbol: "",
  name: "",
  sectorLookupId: "",
  currency: "EUR"
};

export default function AdminPage() {
  const [sectors, setSectors] = useState<LookupItem[]>([]);
  const [priceSources, setPriceSources] = useState<LookupItem[]>([]);
  const [correctionReasons, setCorrectionReasons] = useState<LookupItem[]>([]);
  const [instruments, setInstruments] = useState<InstrumentView[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState("");
  const [instrumentForm, setInstrumentForm] = useState<InstrumentFormState>(initialInstrumentForm);
  const [instrumentStatus, setInstrumentStatus] = useState("");

  const refresh = async () => {
    setLoading(true);
    try {
      const [sectorData, sourceData, reasonData, instrumentData] = await Promise.all([
        getSectors(),
        getPriceSources(),
        getCorrectionReasons(),
        getInstruments()
      ]);
      setSectors(sectorData);
      setPriceSources(sourceData);
      setCorrectionReasons(reasonData);
      setInstruments(instrumentData);
      setError("");
    } catch {
      setError("Failed to load admin data.");
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    refresh().catch(() => setError("Failed to load admin data."));
  }, []);

  const sectorOptions = useMemo(
    () => sectors.map((sector) => ({ id: sector.id, label: `${sector.code} - ${sector.name}` })),
    [sectors]
  );

  const getSectorLookupId = (instrument: InstrumentView): string => {
    const raw = instrument.sectorLookupId;
    if (!raw) {
      return "";
    }

    return typeof raw === "string" ? raw : raw.value;
  };

  const saveInstrument = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    try {
      const payload = {
        symbol: instrumentForm.symbol,
        name: instrumentForm.name || null,
        sectorLookupId: instrumentForm.sectorLookupId || null,
        currency: instrumentForm.currency
      };
      if (instrumentForm.id) {
        await updateInstrument(instrumentForm.id, payload);
        setInstrumentStatus("Instrument updated.");
      } else {
        await createInstrument(payload);
        setInstrumentStatus("Instrument created.");
      }

      setInstrumentForm(initialInstrumentForm);
      await refresh();
    } catch (exception) {
      setInstrumentStatus(exception instanceof Error ? exception.message : "Instrument save failed.");
    }
  };

  const editInstrument = (instrument: InstrumentView) => {
    setInstrumentStatus("");
    setInstrumentForm({
      id: normalizeInstrumentId(instrument),
      symbol: instrument.symbol,
      name: instrument.name ?? "",
      sectorLookupId: getSectorLookupId(instrument),
      currency: instrument.currency
    });
  };

  const removeInstrument = async (id: string) => {
    try {
      await deleteInstrument(id);
      setInstrumentStatus("Instrument deleted.");
      if (instrumentForm.id === id) {
        setInstrumentForm(initialInstrumentForm);
      }
      await refresh();
    } catch (exception) {
      setInstrumentStatus(exception instanceof Error ? exception.message : "Instrument delete failed.");
    }
  };

  return (
    <section className="page-stack">
      <PageHeader title="Admin" description="Manage lookup datasets and instrument master data." />

      <AdminLookupSection
        title="Sectors"
        items={sectors}
        loading={loading}
        error={error}
        onSave={async (id, input) => {
          if (id) {
            await updateSector(id, input);
          } else {
            await createSector(input);
          }
          await refresh();
        }}
        onDelete={async (id) => {
          await deleteSector(id);
          await refresh();
        }}
      />

      <AdminLookupSection
        title="Price Sources"
        items={priceSources}
        loading={loading}
        error={error}
        onSave={async (id, input) => {
          if (id) {
            await updatePriceSource(id, input);
          } else {
            await createPriceSource(input);
          }
          await refresh();
        }}
        onDelete={async (id) => {
          await deletePriceSource(id);
          await refresh();
        }}
      />

      <AdminLookupSection
        title="Correction Reasons"
        items={correctionReasons}
        loading={loading}
        error={error}
        onSave={async (id, input) => {
          if (id) {
            await updateCorrectionReason(id, input);
          } else {
            await createCorrectionReason(input);
          }
          await refresh();
        }}
        onDelete={async (id) => {
          await deleteCorrectionReason(id);
          await refresh();
        }}
      />

      <CardSection title="Instruments">
        <form onSubmit={saveInstrument} className="grid gap-4 md:grid-cols-5 md:items-end">
          <div>
            <FieldLabel htmlFor="admin-instrument-symbol" label="Symbol" tooltip="Instrument ticker/symbol." />
            <input
              id="admin-instrument-symbol"
              className="input"
              value={instrumentForm.symbol}
              onChange={(event) => setInstrumentForm((current) => ({ ...current, symbol: event.target.value }))}
              required
            />
          </div>
          <div>
            <FieldLabel htmlFor="admin-instrument-name" label="Name" tooltip="Optional instrument full name." />
            <input
              id="admin-instrument-name"
              className="input"
              value={instrumentForm.name}
              onChange={(event) => setInstrumentForm((current) => ({ ...current, name: event.target.value }))}
            />
          </div>
          <div>
            <FieldLabel htmlFor="admin-instrument-sector" label="Sector" tooltip="Optional sector lookup link." />
            <select
              id="admin-instrument-sector"
              className="input"
              value={instrumentForm.sectorLookupId}
              onChange={(event) => setInstrumentForm((current) => ({ ...current, sectorLookupId: event.target.value }))}
            >
              <option value="">None</option>
              {sectorOptions.map((sector) => (
                <option key={sector.id} value={sector.id}>
                  {sector.label}
                </option>
              ))}
            </select>
          </div>
          <div>
            <FieldLabel htmlFor="admin-instrument-currency" label="Currency" tooltip="Three-letter ISO currency code." />
            <input
              id="admin-instrument-currency"
              className="input"
              value={instrumentForm.currency}
              onChange={(event) => setInstrumentForm((current) => ({ ...current, currency: event.target.value }))}
              required
            />
          </div>
          <div>
            <button type="submit" className="btn-primary">
              {instrumentForm.id ? "Save Changes" : "Create"}
            </button>
            <button type="button" className="btn-secondary ml-2" onClick={() => setInstrumentForm(initialInstrumentForm)}>
              Cancel
            </button>
          </div>
        </form>

        {instrumentStatus ? <p className="status">{instrumentStatus}</p> : null}
        {loading ? <p className="muted">Loading instruments...</p> : null}
        {error ? <p className="muted">{error}</p> : null}

        {instruments.length === 0 ? (
          <EmptyState message="No instruments found." />
        ) : (
          <div className="table-wrap">
            <table className="table-base">
              <thead>
                <tr>
                  <th scope="col">Symbol</th>
                  <th scope="col">Name</th>
                  <th scope="col">Sector</th>
                  <th scope="col">Currency</th>
                  <th scope="col">Actions</th>
                </tr>
              </thead>
              <tbody>
                {instruments.map((instrument) => {
                  const id = normalizeInstrumentId(instrument);
                  return (
                    <tr key={id}>
                      <td>{instrument.symbol}</td>
                      <td>{instrument.name ?? "-"}</td>
                      <td>{instrument.sector ?? "-"}</td>
                      <td>{instrument.currency}</td>
                      <td>
                        <button type="button" className="btn-secondary" onClick={() => editInstrument(instrument)}>
                          Edit
                        </button>
                        <button type="button" className="btn-secondary ml-2" onClick={() => removeInstrument(id)}>
                          Delete
                        </button>
                      </td>
                    </tr>
                  );
                })}
              </tbody>
            </table>
          </div>
        )}
      </CardSection>
    </section>
  );
}
