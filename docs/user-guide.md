# Satellite Portfolio Website User Guide

This guide is for everyday users of the website. It explains what each section is for, what the key numbers mean, and how to use the app as a decision-support tracker.

## 1) What This Website Is For

Satellite Portfolio helps you:

- track your positions and cash
- monitor performance (realized and unrealized PnL)
- review concentration risk
- document your thinking with journal and thesis notes
- make better, more disciplined decisions over time

It does **not** place trades with brokers. It is a decision-support tool, not an execution platform.

## 2) Core Navigation

Top navigation sections:

- Overview
- Holdings
- Trades
- Prices
- Journal
- Rules
- Admin

## 3) How To Read The Main Numbers

These are the core formulas used by the application.

### Average Cost (per open position)

Average Cost per Unit = `Total Position Cost / Current Quantity`

Example:

- You buy 10 shares at 100 with 2 fees: total cost = 1,002
- You buy 5 shares at 110 with 1 fee: total cost = 551
- Combined quantity = 15
- Combined cost = 1,553
- Average cost = 1,553 / 15 = 103.53

### Realized PnL (when you sell)

Realized PnL = `Net Sale Proceeds - Cost Basis Of Sold Quantity`

Where:

- Net Sale Proceeds = `(Sell Quantity x Sell Price) - Sell Fees`
- Cost Basis Of Sold Quantity = `Sell Quantity x Current Average Cost`

Example:

- You hold 15 shares with average cost 103.53
- You sell 5 shares at 120 with 1 fee
- Net sale proceeds = (5 x 120) - 1 = 599
- Sold cost basis = 5 x 103.53 = 517.65
- Realized PnL = 599 - 517.65 = 81.35

### Unrealized PnL (open position only)

Unrealized PnL = `Market Value - Current Total Cost`

Where:

- Market Value = `Open Quantity x Latest End-Of-Day Price`

Example:

- Remaining quantity = 10
- Latest EOD price = 118
- Market value = 1,180
- Remaining cost = 1,035.35
- Unrealized PnL = 1,180 - 1,035.35 = 144.65

### Allocation %

Allocation % = `Position Market Value / (Total Market Value + Cash Balance)`

Important: cash is included in the denominator.

Example:

- Position A market value = 1,180
- All positions total market value = 5,000
- Cash balance = 2,000
- Denominator = 7,000
- Position A allocation = 1,180 / 7,000 = 16.86%

### Missing Price Behavior

If no end-of-day price is available on or before the valuation date:

- market value is treated as 0 for that line
- unrealized PnL is shown as 0 for that line
- allocation % for that line is 0
- the UI flags that the price is missing

## 4) Page-By-Page Guide

### Overview

What it is:

- Your portfolio dashboard.

What it is used for:

- Quick check of total value, cash, market value, realized and unrealized PnL.
- Recent alert visibility.
- Month-end snapshot summary.

How this helps:

- Gives a fast "am I on track?" view before deeper analysis.

How often to check:

- Daily for active monitoring.
- Weekly for trend awareness.
- Month-end for formal review.

### Holdings

What it is:

- Your current open positions table.

What it is used for:

- Reviewing quantity, average cost, market value, unrealized PnL, allocation, and pricing status by instrument.

How this helps:

- Shows concentration and where gains/losses are currently sitting.
- Helps you identify stale or missing pricing data.

How often to check:

- Daily or after price updates.

### Trades

What it is:

- Your manual transaction history and correction workflow.

What it is used for:

- Entering buys, sells, and non-cash acquisitions.
- Correcting past trade records with an auditable chain (rather than silent overwrite).

How this helps:

- Keeps performance and cost basis math trustworthy.
- Preserves a clear audit trail when corrections are needed.

### Non-Cash Acquisition (important)

Use this for events where quantity arrives without a normal cash buy (for example grants or transfers).

Cost basis modes:

- **Zero basis**: adds quantity with zero added cost basis.
- **Custom basis**: adds quantity and a custom total cost basis.

Why it matters:

- This choice changes future average cost and realized PnL when you later sell.

### Prices

What it is:

- End-of-day (EOD) price maintenance.

What it is used for:

- Adding/updating daily close prices by instrument and date.

How this helps:

- Portfolio valuation, unrealized PnL, and allocation depend on current EOD pricing.

How often to check:

- At least each market day you want valuations to reflect.

### Journal

What it is:

- Your investing decision journal and thesis tracker.

What it is used for:

- Capturing your reason for a decision before and after trades.
- Linking notes to instruments or specific theses.
- Recording what happened versus what you expected.

How this helps:

- Reduces emotional, memory-based decision making.
- Builds a personal evidence base of what works and what does not.
- Improves consistency and discipline over time.

Suggested cadence:

- **Before entry**: write thesis, risks, invalidation condition.
- **After entry**: note execution quality and conviction level.
- **Weekly**: short review of active theses and new information.
- **After major move**: explain whether thesis changed.
- **Month-end**: summarize lessons learned and behavior patterns.

Example journal pattern:

1. Thesis title: "Small-cap quality compounder"
2. Entry note:
   - why now
   - key assumptions
   - top 3 risks
   - what would prove me wrong
3. Follow-up notes:
   - earnings reaction
   - allocation change rationale
   - whether thesis is still intact
4. Exit note:
   - why position was closed
   - what was learned
   - would you repeat the process

### Rules

What it is:

- Risk thresholds and alert visibility.

What it is used for:

- Defining limits for:
  - max position size
  - max sector concentration
  - max drawdown

How this helps:

- Creates objective guardrails before risk gets too large.
- Helps avoid concentration creep.

How to think about each rule:

- **Max position size**: checks if your largest priced position is above limit.
- **Max sector concentration**: checks if your largest sector weight is above limit.
- **Max drawdown**: checks if drawdown from equity-curve peak exceeds limit.

### Admin

What it is:

- Master data management page.

What it is used for:

- Maintaining sectors, price sources, correction reasons, and instruments.

How this helps:

- Keeps dropdowns clean and consistent across Trades, Prices, and Journal.
- Prevents data quality issues caused by inconsistent labels.

Who should use it:

- Usually the portfolio owner or maintainer, not every day for all users.

## 5) Practical Operating Rhythm

Use this as a simple routine.

### Daily (or market days)

1. Update EOD prices.
2. Check Overview for major changes and alerts.
3. Review Holdings allocation and missing-price flags.
4. Add short Journal notes for important events.

### Weekly

1. Review top winners/losers.
2. Review concentration by position and sector.
3. Update or retire theses if assumptions changed.
4. Record one process improvement note in Journal.

### Month-End

1. Review month-end snapshot.
2. Summarize realized and unrealized PnL drivers.
3. Review alerts and decide whether thresholds need tuning.
4. Write a month-end Journal reflection.

## 6) Common Questions

### "Why is allocation lower than expected?"

Because cash is included in the denominator. Large idle cash lowers every position's allocation %.

### "Why do I see missing-price text?"

No valid EOD price exists on or before the selected valuation date.

### "Why did realized PnL change after a correction?"

Trade corrections replace prior economic details using an auditable chain, so historical cost basis/proceeds calculations are recomputed from corrected values.

### "Can I use this to auto-trade?"

No. The product is intentionally scoped for tracking, analysis, and decision support only.

## 7) Good Data Hygiene Tips

- Keep prices current so valuation metrics remain meaningful.
- Enter fees accurately; they directly affect realized PnL.
- Use Journal notes at decision points, not only after outcomes.
- Use clear rule thresholds and revisit them on a schedule.
- Keep instrument and lookup metadata tidy in Admin.
