"use client";

import { FormEvent, useEffect, useMemo, useState } from "react";
import { getJournalEntries, getTheses, JournalEntryView, ThesisView } from "../../lib/api";

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
    <section>
      <h1>Journal & Theses</h1>
      <p>{status}</p>

      <h2>Create Thesis</h2>
      <form onSubmit={createThesis}>
        <label>
          Title
          <input name="title" required />
        </label>
        <label>
          Body
          <textarea name="body" required />
        </label>
        <label>
          Status
          <select name="status" defaultValue="1">
            <option value="1">Active</option>
            <option value="2">Retired</option>
          </select>
        </label>
        <label>
          Instrument ID (optional)
          <input name="instrumentId" />
        </label>
        <button type="submit">Create Thesis</button>
      </form>

      <h2>Create Journal Entry</h2>
      <form onSubmit={createJournal}>
        <label>
          Occurred At
          <input name="occurredAt" type="datetime-local" required />
        </label>
        <label>
          Title
          <input name="title" required />
        </label>
        <label>
          Body
          <textarea name="body" required />
        </label>
        <label>
          Tags
          <input name="tags" />
        </label>
        <label>
          Linked Thesis
          <select value={selectedThesisId} onChange={(e) => setSelectedThesisId(e.target.value)}>
            <option value="">None</option>
            {thesisOptions.map((option) => (
              <option key={option.id} value={option.id}>
                {option.label}
              </option>
            ))}
          </select>
        </label>
        <button type="submit">Create Journal Entry</button>
      </form>

      <h2>Update Journal Entry</h2>
      <form onSubmit={updateJournal}>
        <label>
          Journal Entry ID
          <input value={selectedJournalId} onChange={(e) => setSelectedJournalId(e.target.value)} />
        </label>
        <label>
          Occurred At
          <input name="occurredAt" type="datetime-local" required />
        </label>
        <label>
          Title
          <input name="title" required />
        </label>
        <label>
          Body
          <textarea name="body" required />
        </label>
        <label>
          Tags
          <input name="tags" />
        </label>
        <button type="submit">Update Journal Entry</button>
      </form>

      <h2>Journal Entries</h2>
      {entries.length === 0 ? <p>No entries found.</p> : <pre>{JSON.stringify(entries, null, 2)}</pre>}
    </section>
  );
}

