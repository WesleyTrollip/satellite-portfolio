# API Contracts (REST)

This folder defines the REST API surface for `001-portfolio-tracker-mvp`.

- `endpoints.md`: concrete endpoint list, request/response shapes (conceptual).
- `openapi.md`: OpenAPI outline and conventions (to be implemented via Swagger in ASP.NET Core).

**Note**: Contracts must preserve MVP constraints:
- No broker integrations; no auto-trading endpoints.
- Rules/alerts are advisory only.
- Editing trades/cash entries is an auditable correction flow (no silent overwrite).

