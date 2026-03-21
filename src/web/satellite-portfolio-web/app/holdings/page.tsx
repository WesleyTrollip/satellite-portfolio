import { getHoldings, getTheses } from "../../lib/api";
import { CardSection, EmptyState, PageHeader } from "../components/ui";

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
    <section className="page-stack">
      <PageHeader title="Holdings" description="Current positions, pricing quality, and linked theses." />

      <CardSection>
        {holdings.length === 0 ? (
          <EmptyState message="No holdings found." />
        ) : (
          <div className="table-wrap">
            <table className="table-base">
              <thead>
                <tr>
                  <th scope="col">Symbol</th>
                  <th scope="col" className="text-right">
                    Quantity
                  </th>
                  <th scope="col" className="text-right">
                    Avg Cost
                  </th>
                  <th scope="col" className="text-right">
                    Market Value
                  </th>
                  <th scope="col" className="text-right">
                    Unrealized PnL
                  </th>
                  <th scope="col" className="text-right">
                    Allocation
                  </th>
                  <th scope="col">Pricing</th>
                  <th scope="col">Linked Theses</th>
                </tr>
              </thead>
              <tbody>
                {holdings.map((holding) => (
                  <tr key={holding.instrumentId}>
                    <td className="font-medium">{holding.symbol}</td>
                    <td className="text-right">{holding.quantity.toFixed(4)}</td>
                    <td className="text-right">{holding.averageCost.toFixed(2)}</td>
                    <td className="text-right">{holding.marketValue.toFixed(2)}</td>
                    <td className="text-right">{holding.unrealizedPnl.toFixed(2)}</td>
                    <td className="text-right">{(holding.allocationPercent * 100).toFixed(2)}%</td>
                    <td>{holding.missingPrice ? holding.missingPriceExplanation : "OK"}</td>
                    <td>{(thesisByInstrumentId[holding.instrumentId] ?? []).join(", ") || "-"}</td>
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

