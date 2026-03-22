"use client";

import { FormEvent, useState } from "react";
import { LookupItem } from "../../lib/api";
import { CardSection, EmptyState, FieldLabel } from "./ui";

type LookupSectionProps = {
  title: string;
  items: LookupItem[];
  loading: boolean;
  error: string;
  onSave: (id: string | null, input: { code: string; name: string; isActive: boolean }) => Promise<void>;
  onDelete: (id: string) => Promise<void>;
};

type FormState = {
  id: string | null;
  code: string;
  name: string;
  isActive: boolean;
};

const initialState: FormState = {
  id: null,
  code: "",
  name: "",
  isActive: true
};

export function AdminLookupSection({ title, items, loading, error, onSave, onDelete }: LookupSectionProps) {
  const idPrefix = title.toLowerCase().replace(/\s+/g, "-");
  const [formState, setFormState] = useState<FormState>(initialState);
  const [status, setStatus] = useState("");
  const [saving, setSaving] = useState(false);

  const submit = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    setSaving(true);
    try {
      await onSave(formState.id, {
        code: formState.code,
        name: formState.name,
        isActive: formState.isActive
      });
      setStatus(formState.id ? "Updated successfully." : "Created successfully.");
      setFormState(initialState);
    } catch (exception) {
      setStatus(exception instanceof Error ? exception.message : "Save failed.");
    } finally {
      setSaving(false);
    }
  };

  const startEdit = (item: LookupItem) => {
    setStatus("");
    setFormState({
      id: item.id,
      code: item.code,
      name: item.name,
      isActive: item.isActive
    });
  };

  const clearForm = () => {
    setStatus("");
    setFormState(initialState);
  };

  const remove = async (id: string) => {
    setStatus("");
    try {
      await onDelete(id);
      if (formState.id === id) {
        setFormState(initialState);
      }
      setStatus("Deleted successfully.");
    } catch (exception) {
      setStatus(exception instanceof Error ? exception.message : "Delete failed.");
    }
  };

  return (
    <CardSection title={title}>
      <form onSubmit={submit} className="grid gap-4 md:grid-cols-4 md:items-end">
        <div>
          <FieldLabel htmlFor={`${idPrefix}-code`} label="Code" tooltip="Short canonical key (uppercase recommended)." />
          <input
            id={`${idPrefix}-code`}
            className="input"
            value={formState.code}
            onChange={(event) => setFormState((current) => ({ ...current, code: event.target.value }))}
            required
          />
        </div>
        <div>
          <FieldLabel htmlFor={`${idPrefix}-name`} label="Name" tooltip="User-facing display label." />
          <input
            id={`${idPrefix}-name`}
            className="input"
            value={formState.name}
            onChange={(event) => setFormState((current) => ({ ...current, name: event.target.value }))}
            required
          />
        </div>
        <div className="flex items-center gap-2">
          <input
            id={`${idPrefix}-active`}
            type="checkbox"
            checked={formState.isActive}
            onChange={(event) => setFormState((current) => ({ ...current, isActive: event.target.checked }))}
            className="h-4 w-4 rounded border-border"
          />
          <FieldLabel htmlFor={`${idPrefix}-active`} label="Active" tooltip="Inactive values are hidden from standard selectors." />
        </div>
        <div>
          <button type="submit" className="btn-primary" disabled={saving}>
            {formState.id ? "Save Changes" : "Create"}
          </button>
          <button type="button" className="btn-secondary ml-2" onClick={clearForm}>
            Cancel
          </button>
        </div>
      </form>

      {status ? <p className="status">{status}</p> : null}
      {loading ? <p className="muted">Loading data...</p> : null}
      {error ? <p className="muted">{error}</p> : null}

      {items.length === 0 ? (
        <EmptyState message="No records found." />
      ) : (
        <div className="table-wrap">
          <table className="table-base">
            <thead>
              <tr>
                <th scope="col">Code</th>
                <th scope="col">Name</th>
                <th scope="col">Active</th>
                <th scope="col">Actions</th>
              </tr>
            </thead>
            <tbody>
              {items.map((item) => (
                <tr key={item.id}>
                  <td>{item.code}</td>
                  <td>{item.name}</td>
                  <td>{item.isActive ? "Yes" : "No"}</td>
                  <td>
                    <button type="button" className="btn-secondary" onClick={() => startEdit(item)}>
                      Edit
                    </button>
                    <button
                      type="button"
                      className="btn-secondary ml-2"
                      onClick={() => remove(item.id)}
                      aria-label={`Delete ${item.name}`}
                    >
                      Delete
                    </button>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}
    </CardSection>
  );
}
