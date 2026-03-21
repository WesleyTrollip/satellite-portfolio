"use client";

import { FormEvent, useEffect, useState } from "react";
import { AlertEventView, getCurrentAlerts, getRules, RuleView } from "../../lib/api";
import { CardSection, EmptyState, FieldLabel, PageHeader, StatusMessage } from "../components/ui";

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

  const getId = (value: { value: string } | string): string => (typeof value === "string" ? value : value.value);

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
    <section className="page-stack">
      <PageHeader title="Rules & Alerts" description="Configure portfolio limits and monitor active alert events." />
      <StatusMessage message={status} />

      <div className="grid gap-6 lg:grid-cols-2">
        <CardSection title="Create Rule">
          <form onSubmit={createRule} className="grid gap-4">
            <div>
              <FieldLabel
                htmlFor="rule-create-type"
                label="Type"
                tooltip="Rule category to enforce. Example: MaxPositionSize"
              />
              <select id="rule-create-type" name="type" defaultValue="1" className="input">
                <option value="1">MaxPositionSize</option>
                <option value="2">MaxSectorConcentration</option>
                <option value="3">MaxDrawdown</option>
              </select>
            </div>
            <div className="flex items-center gap-2">
              <input id="rule-create-enabled" name="enabled" type="checkbox" defaultChecked className="h-4 w-4 rounded border-border" />
              <FieldLabel
                htmlFor="rule-create-enabled"
                label="Enabled"
                tooltip="Turn this on to evaluate and trigger alerts. Example: Checked"
              />
            </div>
            <div>
              <FieldLabel
                htmlFor="rule-create-parameters"
                label="Parameters JSON"
                tooltip='JSON configuration for the selected rule type. Example: {"maxPct":0.10}'
              />
              <input id="rule-create-parameters" name="parametersJson" defaultValue='{"maxPct":0.10}' className="input" />
            </div>
            <div>
              <button type="submit" className="btn-primary">
                Create Rule
              </button>
            </div>
          </form>
        </CardSection>

        <CardSection title="Update Rule">
          <form onSubmit={updateRule} className="grid gap-4">
            <div>
              <FieldLabel
                htmlFor="rule-update-id"
                label="Rule ID"
                tooltip="GUID of an existing rule to update. Example: 9d8fd5ef-157c-44d4-88ac-4381e5d02fa4"
              />
              <input
                id="rule-update-id"
                className="input"
                value={selectedRuleId}
                onChange={(e) => setSelectedRuleId(e.target.value)}
              />
            </div>
            <div>
              <FieldLabel
                htmlFor="rule-update-type"
                label="Type"
                tooltip="Rule category for the updated rule definition. Example: MaxDrawdown"
              />
              <select id="rule-update-type" name="type" defaultValue="1" className="input">
                <option value="1">MaxPositionSize</option>
                <option value="2">MaxSectorConcentration</option>
                <option value="3">MaxDrawdown</option>
              </select>
            </div>
            <div className="flex items-center gap-2">
              <input id="rule-update-enabled" name="enabled" type="checkbox" defaultChecked className="h-4 w-4 rounded border-border" />
              <FieldLabel
                htmlFor="rule-update-enabled"
                label="Enabled"
                tooltip="Enable to keep this rule active for alert checks. Example: Checked"
              />
            </div>
            <div>
              <FieldLabel
                htmlFor="rule-update-parameters"
                label="Parameters JSON"
                tooltip='Updated JSON settings. Example: {"maxDrawdownPct":0.15}'
              />
              <input id="rule-update-parameters" name="parametersJson" defaultValue='{"maxPct":0.10}' className="input" />
            </div>
            <div>
              <button type="submit" className="btn-primary">
                Update Rule
              </button>
            </div>
          </form>
        </CardSection>
      </div>

      <CardSection title="Current Rules">
        {rules.length === 0 ? (
          <EmptyState message="No rules configured." />
        ) : (
          <div className="table-wrap">
            <table className="table-base">
              <thead>
                <tr>
                  <th scope="col">Rule ID</th>
                  <th scope="col">Type</th>
                  <th scope="col">Enabled</th>
                  <th scope="col">Parameters JSON</th>
                </tr>
              </thead>
              <tbody>
                {rules.map((rule) => (
                  <tr key={getId(rule.id)}>
                    <td>{getId(rule.id)}</td>
                    <td>{rule.type}</td>
                    <td>{rule.enabled ? "Yes" : "No"}</td>
                    <td className="font-mono text-xs">{rule.parametersJson}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}
      </CardSection>

      <CardSection title="Current Alerts">
        {alerts.length === 0 ? (
          <EmptyState message="No active alerts." />
        ) : (
          <div className="table-wrap">
            <table className="table-base">
              <thead>
                <tr>
                  <th scope="col">Alert ID</th>
                  <th scope="col">Rule ID</th>
                  <th scope="col">Severity</th>
                  <th scope="col">Title</th>
                  <th scope="col">Triggered At</th>
                </tr>
              </thead>
              <tbody>
                {alerts.map((alert) => (
                  <tr key={getId(alert.id)}>
                    <td>{getId(alert.id)}</td>
                    <td>{getId(alert.ruleId)}</td>
                    <td>{alert.severity}</td>
                    <td>{alert.title}</td>
                    <td>{new Date(alert.triggeredAt).toLocaleString()}</td>
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

