import { getHoldings, getTheses } from "../../lib/api";

export default async function HoldingsPage() {
  const [holdings, theses] = await Promise.all([getHoldings(), getTheses()]);

  const thesisByInstrumentId = theses.reduce<Record<string, string[]>>((accumulator, thesis) => {
    const rawInstrumentId = thesis.instrumentId;
    const instrumentId =
      rawInstrumentId == null
        ? null
        : typeof rawInstrumentId === "string"
          ? rawInstrumentId
          : rawInstrumentId.value;

    if (!instrumentId) {
      return accumulator;
    }

    accumulator[instrumentId] ??= [];
    accumulator[instrumentId].push(thesis.title);
    return accumulator;
  }, {});

  return (
    <section>
      <h1>Holdings</h1>
      <table style={{ borderCollapse: "collapse", width: "100%" }}>
        <thead>
          <tr>
            <th align="left">Symbol</th>
            <th align="right">Quantity</th>
            <th align="right">Avg Cost</th>
            <th align="right">Market Value</th>
            <th align="right">Unrealized PnL</th>
            <th align="right">Allocation</th>
            <th align="left">Pricing</th>
            <th align="left">Linked Theses</th>
          </tr>
        </thead>
        <tbody>
          {holdings.map((holding) => (
            <tr key={holding.instrumentId}>
              <td>{holding.symbol}</td>
              <td align="right">{holding.quantity.toFixed(4)}</td>
              <td align="right">{holding.averageCost.toFixed(2)}</td>
              <td align="right">{holding.marketValue.toFixed(2)}</td>
              <td align="right">{holding.unrealizedPnl.toFixed(2)}</td>
              <td align="right">{(holding.allocationPercent * 100).toFixed(2)}%</td>
              <td>{holding.missingPrice ? holding.missingPriceExplanation : "OK"}</td>
              <td>{(thesisByInstrumentId[holding.instrumentId] ?? []).join(", ") || "-"}</td>
            </tr>
          ))}
        </tbody>
      </table>
    </section>
  );
}

