"use client";

import { FormEvent, useEffect, useState } from "react";
import { AlertEventView, getCurrentAlerts, getRules, RuleView } from "../../lib/api";

const API_BASE_URL = process.env.NEXT_PUBLIC_API_BASE_URL ?? "http://localhost:5014/api";

export default function RulesPage() {
  const [rules, setRules] = useState<RuleView[]>([]);
  const [alerts, setAlerts] = useState<AlertEventView[]>([]);
  const [selectedRuleId, setSelectedRuleId] = useState("");
  const [status, setStatus] = useState("");

  const refresh = async () => {
    const [rulesData, alertsData] = await Promise.all([getRules(), getCurrentAlerts()]);
    setRules(rulesData);
    setAlerts(alertsData);
  };

  useEffect(() => {
    refresh().catch(() => setStatus("Failed to load rules/alerts."));
  }, []);

  const createRule = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    const form = new FormData(event.currentTarget);
    const payload = {
      type: form.get("type"),
      enabled: form.get("enabled") === "on",
      parametersJson: form.get("parametersJson")
    };

    const response = await fetch(`${API_BASE_URL}/rules`, {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify(payload)
    });

    setStatus(response.ok ? "Rule created." : "Rule creation failed.");
    if (response.ok) {
      await refresh();
    }
  };

  const updateRule = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    if (!selectedRuleId) {
      setStatus("Select a rule first.");
      return;
    }

    const form = new FormData(event.currentTarget);
    const payload = {
      type: form.get("type"),
      enabled: form.get("enabled") === "on",
      parametersJson: form.get("parametersJson")
    };

    const response = await fetch(`${API_BASE_URL}/rules/${selectedRuleId}`, {
      method: "PUT",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify(payload)
    });

    setStatus(response.ok ? "Rule updated." : "Rule update failed.");
    if (response.ok) {
      await refresh();
    }
  };

  return (
    <section>
      <h1>Rules & Alerts</h1>
      <p>{status}</p>

      <h2>Create Rule</h2>
      <form onSubmit={createRule}>
        <label>
          Type
          <select name="type" defaultValue="1">
            <option value="1">MaxPositionSize</option>
            <option value="2">MaxSectorConcentration</option>
            <option value="3">MaxDrawdown</option>
          </select>
        </label>
        <label>
          Enabled
          <input name="enabled" type="checkbox" defaultChecked />
        </label>
        <label>
          Parameters JSON
          <input name="parametersJson" defaultValue='{"maxPct":0.10}' />
        </label>
        <button type="submit">Create Rule</button>
      </form>

      <h2>Update Rule</h2>
      <form onSubmit={updateRule}>
        <label>
          Rule ID
          <input value={selectedRuleId} onChange={(e) => setSelectedRuleId(e.target.value)} />
        </label>
        <label>
          Type
          <select name="type" defaultValue="1">
            <option value="1">MaxPositionSize</option>
            <option value="2">MaxSectorConcentration</option>
            <option value="3">MaxDrawdown</option>
          </select>
        </label>
        <label>
          Enabled
          <input name="enabled" type="checkbox" defaultChecked />
        </label>
        <label>
          Parameters JSON
          <input name="parametersJson" defaultValue='{"maxPct":0.10}' />
        </label>
        <button type="submit">Update Rule</button>
      </form>

      <h2>Current Rules</h2>
      {rules.length === 0 ? <p>No rules configured.</p> : <pre>{JSON.stringify(rules, null, 2)}</pre>}

      <h2>Current Alerts</h2>
      {alerts.length === 0 ? <p>No active alerts.</p> : <pre>{JSON.stringify(alerts, null, 2)}</pre>}
    </section>
  );
}

