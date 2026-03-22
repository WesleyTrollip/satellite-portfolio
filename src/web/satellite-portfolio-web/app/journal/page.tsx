"use client";

import { FormEvent, useEffect, useMemo, useState } from "react";
import { getInstruments, getJournalEntries, getTheses, InstrumentView, JournalEntryView, ThesisView } from "../../lib/api";
import { CardSection, EmptyState, FieldLabel, PageHeader, StatusMessage } from "../components/ui";

const API_BASE_URL = process.env.NEXT_PUBLIC_API_BASE_URL ?? "http://localhost:5014/api";

export default function JournalPage() {
  const [entries, setEntries] = useState<JournalEntryView[]>([]);
  const [theses, setTheses] = useState<ThesisView[]>([]);
  const [instruments, setInstruments] = useState<InstrumentView[]>([]);
  const [selectedThesisIdForCreate, setSelectedThesisIdForCreate] = useState<string>("");
  const [selectedInstrumentIdForCreate, setSelectedInstrumentIdForCreate] = useState<string>("");
  const [selectedThesisInstrumentId, setSelectedThesisInstrumentId] = useState<string>("");
  const [editingEntry, setEditingEntry] = useState<JournalEntryView | null>(null);
  const [editingOccurredAt, setEditingOccurredAt] = useState("");
  const [editingTitle, setEditingTitle] = useState("");
  const [editingBody, setEditingBody] = useState("");
  const [editingTags, setEditingTags] = useState("");
  const [editingThesisId, setEditingThesisId] = useState<string>("");
  const [editingInstrumentId, setEditingInstrumentId] = useState<string>("");
  const [lookupsLoading, setLookupsLoading] = useState(true);
  const [lookupError, setLookupError] = useState("");
  const [status, setStatus] = useState<string>("");

  const thesisOptions = useMemo(
    () =>
      theses.map((t) => {
        const id = typeof t.id === "string" ? t.id : t.id.value;
        return { id, label: t.title };
      }),
    [theses]
  );

  const thesisLabelMap = useMemo(
    () => Object.fromEntries(thesisOptions.map((option) => [option.id, option.label])),
    [thesisOptions]
  );

  const refresh = async () => {
    const [entryData, thesisData, instrumentData] = await Promise.all([getJournalEntries(), getTheses(), getInstruments()]);
    setEntries(entryData);
    setTheses(thesisData);
    setInstruments(instrumentData);
  };

  useEffect(() => {
    refresh()
      .then(() => setLookupError(""))
      .catch(() => {
        setStatus("Failed to load journal/thesis data.");
        setLookupError("Lookup data is unavailable.");
      })
      .finally(() => setLookupsLoading(false));
  }, []);

  const instrumentOptions = useMemo(
    () =>
      instruments.map((instrument) => {
        const id = typeof instrument.id === "string" ? instrument.id : instrument.id.value;
        return {
          id,
          label: instrument.name ? `${instrument.symbol} - ${instrument.name}` : instrument.symbol
        };
      }),
    [instruments]
  );

  const instrumentLabelMap = useMemo(
    () => Object.fromEntries(instrumentOptions.map((option) => [option.id, option.label])),
    [instrumentOptions]
  );

  const createJournal = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    const form = new FormData(event.currentTarget);
    const payload = {
      occurredAt: form.get("occurredAt"),
      title: form.get("title"),
      body: form.get("body"),
      tags: form.get("tags"),
      thesisIds: selectedThesisIdForCreate ? [selectedThesisIdForCreate] : [],
      instrumentIds: selectedInstrumentIdForCreate ? [selectedInstrumentIdForCreate] : []
    };

    const response = await fetch(`${API_BASE_URL}/journal`, {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify(payload)
    });

    setStatus(response.ok ? "Journal entry created." : "Journal entry creation failed.");
    if (response.ok) {
      await refresh();
    }
  };

  const updateJournal = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    if (!editingEntry) {
      setStatus("Select a journal entry row first.");
      return;
    }

    const payload = {
      occurredAt: editingOccurredAt,
      title: editingTitle,
      body: editingBody,
      tags: editingTags,
      thesisIds: editingThesisId ? [editingThesisId] : [],
      instrumentIds: editingInstrumentId ? [editingInstrumentId] : []
    };

    const editingId = typeof editingEntry.entry.id === "string" ? editingEntry.entry.id : editingEntry.entry.id.value;
    const response = await fetch(`${API_BASE_URL}/journal/${editingId}`, {
      method: "PUT",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify(payload)
    });

    setStatus(response.ok ? "Journal entry updated." : "Journal entry update failed.");
    if (response.ok) {
      cancelEdit();
      await refresh();
    }
  };

  const createThesis = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    const form = new FormData(event.currentTarget);
    const payload = {
      title: form.get("title"),
      body: form.get("body"),
      status: form.get("status"),
      instrumentId: selectedThesisInstrumentId || null
    };

    const response = await fetch(`${API_BASE_URL}/theses`, {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify(payload)
    });

    setStatus(response.ok ? "Thesis created." : "Thesis creation failed.");
    if (response.ok) {
      await refresh();
    }
  };

  const startEdit = (entry: JournalEntryView) => {
    setEditingEntry(entry);
    setEditingOccurredAt(entry.entry.occurredAt.slice(0, 16));
    setEditingTitle(entry.entry.title);
    setEditingBody(entry.entry.body);
    setEditingTags(entry.entry.tags ?? "");
    setEditingThesisId(entry.thesisIds[0] ?? "");
    setEditingInstrumentId(entry.instrumentIds[0] ?? "");
  };

  const cancelEdit = () => {
    setEditingEntry(null);
    setEditingOccurredAt("");
    setEditingTitle("");
    setEditingBody("");
    setEditingTags("");
    setEditingThesisId("");
    setEditingInstrumentId("");
  };

  return (
    <section className="page-stack">
      <PageHeader title="Journal & Theses" description="Capture thesis context and timestamped research decisions." />
      <StatusMessage message={status} />

      <div className="grid gap-6 lg:grid-cols-2">
        <CardSection title="Create Thesis">
          <form onSubmit={createThesis} className="grid gap-4">
            <div>
              <FieldLabel
                htmlFor="thesis-title"
                label="Title"
                tooltip="Short thesis headline that is easy to scan later. Example: Reduce concentration in mega-cap tech"
              />
              <input id="thesis-title" name="title" className="input" required />
            </div>
            <div>
              <FieldLabel
                htmlFor="thesis-body"
                label="Body"
                tooltip="Full thesis rationale and assumptions. Example: Earnings growth slowed while valuation remains elevated..."
              />
              <textarea id="thesis-body" name="body" className="input min-h-28" required />
            </div>
            <div>
              <FieldLabel
                htmlFor="thesis-status"
                label="Status"
                tooltip="Lifecycle of the thesis. Active means still in force; Retired means no longer active. Example: Active"
              />
              <select id="thesis-status" name="status" defaultValue="1" className="input">
                <option value="1">Active</option>
                <option value="2">Retired</option>
              </select>
            </div>
            <div>
              <FieldLabel
                htmlFor="thesis-instrument-id"
                label="Instrument (optional)"
                tooltip="Optionally link this thesis to one instrument."
              />
              <select
                id="thesis-instrument-id"
                name="instrumentId"
                className="input"
                value={selectedThesisInstrumentId}
                onChange={(e) => setSelectedThesisInstrumentId(e.target.value)}
              >
                <option value="">None</option>
                {instrumentOptions.map((instrument) => (
                  <option key={instrument.id} value={instrument.id}>
                    {instrument.label}
                  </option>
                ))}
              </select>
            </div>
            <div>
              <button type="submit" className="btn-primary">
                Create Thesis
              </button>
            </div>
          </form>
        </CardSection>

        <CardSection title="Create Journal Entry">
          <form onSubmit={createJournal} className="grid gap-4">
            <div>
              <FieldLabel
                htmlFor="journal-create-occurred-at"
                label="Occurred At"
                tooltip="When this observation or decision happened. Example: 2026-03-21T09:45"
              />
              <input id="journal-create-occurred-at" name="occurredAt" type="datetime-local" className="input" required />
            </div>
            <div>
              <FieldLabel
                htmlFor="journal-create-title"
                label="Title"
                tooltip="Short journal heading for quick review. Example: Rebalance after CPI release"
              />
              <input id="journal-create-title" name="title" className="input" required />
            </div>
            <div>
              <FieldLabel
                htmlFor="journal-create-body"
                label="Body"
                tooltip="Detailed note describing what happened and why it matters. Example: Added to position after pullback to support..."
              />
              <textarea id="journal-create-body" name="body" className="input min-h-28" required />
            </div>
            <div>
              <FieldLabel
                htmlFor="journal-create-tags"
                label="Tags"
                tooltip="Optional comma-separated labels for search and grouping. Example: macro,risk,rebalance"
              />
              <input id="journal-create-tags" name="tags" className="input" />
            </div>
            <div>
              <FieldLabel
                htmlFor="journal-create-thesis"
                label="Linked Thesis"
                tooltip="Optional thesis to associate with this journal entry. Example: Reduce concentration in mega-cap tech"
              />
              <select
                id="journal-create-thesis"
                className="input"
                value={selectedThesisIdForCreate}
                onChange={(e) => setSelectedThesisIdForCreate(e.target.value)}
              >
                <option value="">None</option>
                {thesisOptions.map((option) => (
                  <option key={option.id} value={option.id}>
                    {option.label}
                  </option>
                ))}
              </select>
            </div>
            <div>
              <FieldLabel
                htmlFor="journal-create-instrument"
                label="Linked Instrument"
                tooltip="Optional instrument link for this journal entry."
              />
              <select
                id="journal-create-instrument"
                className="input"
                value={selectedInstrumentIdForCreate}
                onChange={(e) => setSelectedInstrumentIdForCreate(e.target.value)}
              >
                <option value="">None</option>
                {instrumentOptions.map((instrument) => (
                  <option key={instrument.id} value={instrument.id}>
                    {instrument.label}
                  </option>
                ))}
              </select>
            </div>
            <div>
              <button type="submit" className="btn-primary">
                Create Journal Entry
              </button>
            </div>
          </form>
        </CardSection>
      </div>

      <CardSection title="Update Journal Entry">
        <form onSubmit={updateJournal} className="grid gap-4 md:grid-cols-2">
          <div>
            <FieldLabel
              htmlFor="journal-update-id"
              label="Selected Journal Entry"
              tooltip="Use Edit on a table row to preload this update form."
            />
            <input
              id="journal-update-id"
              className="input"
              value={
                editingEntry
                  ? typeof editingEntry.entry.id === "string"
                    ? editingEntry.entry.id
                    : editingEntry.entry.id.value
                  : ""
              }
              readOnly
            />
          </div>
          <div>
            <FieldLabel
              htmlFor="journal-update-occurred-at"
              label="Occurred At"
              tooltip="Updated timestamp for when the event occurred. Example: 2026-03-21T09:45"
            />
            <input
              id="journal-update-occurred-at"
              name="occurredAt"
              type="datetime-local"
              className="input"
              value={editingOccurredAt}
              onChange={(e) => setEditingOccurredAt(e.target.value)}
              required
            />
          </div>
          <div>
            <FieldLabel
              htmlFor="journal-update-title"
              label="Title"
              tooltip="Updated short heading for this entry. Example: Rebalance after CPI release"
            />
            <input
              id="journal-update-title"
              name="title"
              className="input"
              value={editingTitle}
              onChange={(e) => setEditingTitle(e.target.value)}
              required
            />
          </div>
          <div>
            <FieldLabel
              htmlFor="journal-update-tags"
              label="Tags"
              tooltip="Updated optional comma-separated labels. Example: macro,risk,rebalance"
            />
            <input
              id="journal-update-tags"
              name="tags"
              className="input"
              value={editingTags}
              onChange={(e) => setEditingTags(e.target.value)}
            />
          </div>
          <div className="md:col-span-2">
            <FieldLabel
              htmlFor="journal-update-body"
              label="Body"
              tooltip="Updated full journal note content. Example: Position sizing adjusted to stay within risk limits..."
            />
            <textarea
              id="journal-update-body"
              name="body"
              className="input min-h-28"
              value={editingBody}
              onChange={(e) => setEditingBody(e.target.value)}
              required
            />
          </div>
          <div>
            <FieldLabel htmlFor="journal-update-thesis" label="Linked Thesis" tooltip="Optional thesis link for this entry." />
            <select
              id="journal-update-thesis"
              className="input"
              value={editingThesisId}
              onChange={(e) => setEditingThesisId(e.target.value)}
            >
              <option value="">None</option>
              {thesisOptions.map((option) => (
                <option key={option.id} value={option.id}>
                  {option.label}
                </option>
              ))}
            </select>
          </div>
          <div>
            <FieldLabel
              htmlFor="journal-update-instrument"
              label="Linked Instrument"
              tooltip="Optional instrument link for this entry."
            />
            <select
              id="journal-update-instrument"
              className="input"
              value={editingInstrumentId}
              onChange={(e) => setEditingInstrumentId(e.target.value)}
            >
              <option value="">None</option>
              {instrumentOptions.map((instrument) => (
                <option key={instrument.id} value={instrument.id}>
                  {instrument.label}
                </option>
              ))}
            </select>
          </div>
          <div className="md:col-span-2">
            <button type="submit" className="btn-primary">
              Update Journal Entry
            </button>
            <button type="button" className="btn-secondary ml-2" onClick={cancelEdit}>
              Cancel
            </button>
          </div>
        </form>
      </CardSection>
      {lookupsLoading ? <p className="muted">Loading lookup data...</p> : null}
      {lookupError ? <p className="muted">{lookupError}</p> : null}

      <CardSection title="Journal Entries">
        {entries.length === 0 ? (
          <EmptyState message="No entries found." />
        ) : (
          <div className="table-wrap">
            <table className="table-base">
              <thead>
                <tr>
                  <th scope="col">Entry ID</th>
                  <th scope="col">Occurred At</th>
                  <th scope="col">Title</th>
                  <th scope="col">Tags</th>
                  <th scope="col">Linked Theses</th>
                  <th scope="col">Linked Instruments</th>
                  <th scope="col">Action</th>
                </tr>
              </thead>
              <tbody>
                {entries.map((entry) => {
                  const entryId = typeof entry.entry.id === "string" ? entry.entry.id : entry.entry.id.value;
                  return (
                    <tr key={entryId}>
                      <td>{entryId}</td>
                      <td>{new Date(entry.entry.occurredAt).toLocaleString()}</td>
                      <td>{entry.entry.title}</td>
                      <td>{entry.entry.tags || "-"}</td>
                      <td>{entry.thesisIds.map((id) => thesisLabelMap[id] ?? id).join(", ") || "-"}</td>
                      <td>{entry.instrumentIds.map((id) => instrumentLabelMap[id] ?? id).join(", ") || "-"}</td>
                      <td>
                        <button type="button" className="btn-secondary" onClick={() => startEdit(entry)}>
                          Edit
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

