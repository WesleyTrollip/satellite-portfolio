"use client";

import { FormEvent, useEffect, useMemo, useState } from "react";
import { getJournalEntries, getTheses, JournalEntryView, ThesisView } from "../../lib/api";
import { CardSection, EmptyState, FieldLabel, PageHeader, StatusMessage } from "../components/ui";

const API_BASE_URL = process.env.NEXT_PUBLIC_API_BASE_URL ?? "http://localhost:5014/api";

export default function JournalPage() {
  const [entries, setEntries] = useState<JournalEntryView[]>([]);
  const [theses, setTheses] = useState<ThesisView[]>([]);
  const [selectedJournalId, setSelectedJournalId] = useState<string>("");
  const [selectedThesisId, setSelectedThesisId] = useState<string>("");
  const [status, setStatus] = useState<string>("");

  const thesisOptions = useMemo(
    () =>
      theses.map((t) => {
        const id = typeof t.id === "string" ? t.id : t.id.value;
        return { id, label: t.title };
      }),
    [theses]
  );

  const refresh = async () => {
    const [entryData, thesisData] = await Promise.all([getJournalEntries(), getTheses()]);
    setEntries(entryData);
    setTheses(thesisData);
  };

  useEffect(() => {
    refresh().catch(() => setStatus("Failed to load journal/thesis data."));
  }, []);

  const createJournal = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    const form = new FormData(event.currentTarget);
    const payload = {
      occurredAt: form.get("occurredAt"),
      title: form.get("title"),
      body: form.get("body"),
      tags: form.get("tags"),
      thesisIds: selectedThesisId ? [selectedThesisId] : [],
      instrumentIds: []
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
    if (!selectedJournalId) {
      setStatus("Select a journal entry first.");
      return;
    }

    const form = new FormData(event.currentTarget);
    const payload = {
      occurredAt: form.get("occurredAt"),
      title: form.get("title"),
      body: form.get("body"),
      tags: form.get("tags"),
      thesisIds: selectedThesisId ? [selectedThesisId] : [],
      instrumentIds: []
    };

    const response = await fetch(`${API_BASE_URL}/journal/${selectedJournalId}`, {
      method: "PUT",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify(payload)
    });

    setStatus(response.ok ? "Journal entry updated." : "Journal entry update failed.");
    if (response.ok) {
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
      instrumentId: form.get("instrumentId") || null
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
                label="Instrument ID (optional)"
                tooltip="Optional GUID to tie this thesis to one instrument. Example: 3f8f0d76-1b4a-4cde-9a37-0b9e9d2f4c12"
              />
              <input id="thesis-instrument-id" name="instrumentId" className="input" />
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
                value={selectedThesisId}
                onChange={(e) => setSelectedThesisId(e.target.value)}
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
              label="Journal Entry ID"
              tooltip="GUID of the existing journal entry to update. Example: 21c6dc90-e0fd-4c3f-b6a7-e0af4b4f7a8a"
            />
            <input
              id="journal-update-id"
              className="input"
              value={selectedJournalId}
              onChange={(e) => setSelectedJournalId(e.target.value)}
            />
          </div>
          <div>
            <FieldLabel
              htmlFor="journal-update-occurred-at"
              label="Occurred At"
              tooltip="Updated timestamp for when the event occurred. Example: 2026-03-21T09:45"
            />
            <input id="journal-update-occurred-at" name="occurredAt" type="datetime-local" className="input" required />
          </div>
          <div>
            <FieldLabel
              htmlFor="journal-update-title"
              label="Title"
              tooltip="Updated short heading for this entry. Example: Rebalance after CPI release"
            />
            <input id="journal-update-title" name="title" className="input" required />
          </div>
          <div>
            <FieldLabel
              htmlFor="journal-update-tags"
              label="Tags"
              tooltip="Updated optional comma-separated labels. Example: macro,risk,rebalance"
            />
            <input id="journal-update-tags" name="tags" className="input" />
          </div>
          <div className="md:col-span-2">
            <FieldLabel
              htmlFor="journal-update-body"
              label="Body"
              tooltip="Updated full journal note content. Example: Position sizing adjusted to stay within risk limits..."
            />
            <textarea id="journal-update-body" name="body" className="input min-h-28" required />
          </div>
          <div className="md:col-span-2">
            <button type="submit" className="btn-primary">
              Update Journal Entry
            </button>
          </div>
        </form>
      </CardSection>

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
                      <td>{entry.thesisIds.join(", ") || "-"}</td>
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

