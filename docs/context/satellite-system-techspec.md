# Satellite Portfolio Technical Specification

## System Overview

AI assisted portfolio monitoring platform.

Purpose: Decision support only. The system does not automatically
execute trades.

## Architecture

Data Sources ↓ Market Data Layer ↓ Portfolio Engine ↓ Risk Engine ↓ AI
Analysis Layer ↓ User Dashboard

## Data Sources

  Data                  Source
  --------------------- -------------------
  Market prices         Yahoo Finance API
  Macro data            FRED API
  ETF data              Morningstar
  Economic indicators   ECB

## Portfolio Engine

Responsibilities:

-   Track holdings
-   Track cost basis
-   Calculate PnL
-   Calculate allocations

Key calculations: portfolio_value position_weight unrealized_pnl
realized_pnl

## Risk Engine

Example rules:

if position_weight \> 0.15: alert("Position exceeds allowed risk")

if portfolio_drawdown \> 0.30: alert("Portfolio drawdown critical")

## AI Analysis Layer

Supports:

-   Document analysis
-   Market research summarization
-   News analysis
-   Strategy evaluation

Example prompts:

"Summarize macroeconomic risks affecting semiconductor stocks."

"Evaluate the investment risks of stock ticker SGHC."

## Dashboard

Recommended stack:

Frontend: React / NextJS

Backend: Python FastAPI

Data processing: Python

Visualization: Plotly / Recharts

## Automation

Daily: - Update market data - Recalculate portfolio values

Weekly: - Risk review - AI research summaries

Monthly: - Strategy review
