# Assembly Line Manager — Full App Design

Date: 2026-06-17

## Goal

Turn the GATX scaffold (Product-only vertical slice) into the full assembly line
manager described in `project_description.txt`, deployed on AWS. Same stack as the
scaffold (.NET 8 Clean Architecture + MediatR CQRS + EF Core/PostgreSQL; React 18 +
Vite + React Query). Behaviour mirrors the reference Node/Angular app.

## Decisions (confirmed)

- **Auth**: own JWT with a seeded user (BCrypt password hash). `[Authorize]` on all
  data endpoints. No external OAuth.
- **Allocation reorder**: up/down buttons, persisted via a reorder endpoint.
- **Styling**: lightweight custom CSS extending the existing `styles.css`.
- **Deploy**: build + verify locally, then rebuild images, push, replace the EC2
  instance, verify the live app.

## Data model

- Product (name)
- AssemblyLine (name, active, productId) — belongs to one Product
- Workstation (short_name, name, pc_name)
- AssemblyLineWorkstation (assemblyLineId, workstationId, position) — ordered M:N
- User (username, passwordHash) — new

Relationships: a Product has many AssemblyLines; an AssemblyLine has many ordered
Workstations; a Workstation can be on many AssemblyLines.

## Backend

### Application (CQRS slices mirroring `Products/`)

- **Workstations**: `GetWorkstationsQuery`; `Create/Update/DeleteWorkstationCommand`;
  `WorkstationDto(Id, ShortName, Name, PcName)`.
- **AssemblyLines**: `GetAssemblyLinesQuery(productId?)`, `GetAssemblyLineByIdQuery`;
  `Create/Update/DeleteAssemblyLineCommand`;
  `AssemblyLineDto(Id, Name, Active, ProductId, ProductName, WorkstationCount)`.
- **Allocations**: `GetAllocationsQuery(lineId)`;
  `AllocateWorkstationCommand(lineId, workstationId)` appends at `MAX(position)+1`;
  `RemoveAllocationCommand(lineId, workstationId)` deletes then recompacts positions;
  `ReorderAllocationsCommand(lineId, workstationIds[])` validates the payload is the
  exact current set, then rewrites positions to the given order.
  `AllocationDto(WorkstationId, ShortName, Name, PcName, Position)`.
- **Auth**: `LoginCommand(username, password) → AuthResultDto(Token, Username)`.
  New interfaces in `Common/Interfaces`: `IJwtTokenGenerator`, `IPasswordHasher`.

Validators (FluentValidation) for each command, matching the existing style.

### Infrastructure

- `UserConfiguration` (unique username).
- `JwtTokenGenerator` (System.IdentityModel.Tokens.Jwt) and `PasswordHasher`
  (BCrypt.Net-Next), registered in `DependencyInjection`.
- `Users` DbSet added to `IAppDbContext` and `AppDbContext`.
- New packages: `BCrypt.Net-Next`, `System.IdentityModel.Tokens.Jwt`.

### WebApi

- Controllers: `WorkstationsController`, `AssemblyLinesController` (CRUD +
  `GET/POST /{id}/workstations`, `PUT /{id}/workstations/order`,
  `DELETE /{id}/workstations/{workstationId}`), `AuthController` (`POST /api/auth/login`).
- `Program.cs`: JWT bearer authentication + authorization. `[Authorize]` on data
  controllers; `[AllowAnonymous]` on auth and `/health`.
- `DbSeeder` runs after `EnsureCreatedAsync`: seeds the default user and the sample
  data (products, workstations, lines, a couple of allocations) when the DB is empty.
- New package: `Microsoft.AspNetCore.Authentication.JwtBearer`.

### Config (env-overridable)

`Jwt:Secret`, `Jwt:Issuer`, `Jwt:Audience`, `Jwt:ExpiresMinutes`;
`Auth:DefaultUsername`, `Auth:DefaultPassword`. Terraform generates `jwt_secret` and
passes it to the API container as `Jwt__Secret`.

## Frontend

Add `react-router-dom`. Under `src/`:

- `shared/api/apiClient.ts` — fetch wrapper: attaches `Authorization: Bearer`, parses
  ProblemDetails, throws on non-OK, triggers logout/redirect on 401.
- `shared/auth/` — `AuthContext` (token + user in localStorage), `ProtectedRoute`.
- `app/Layout.tsx` — top nav (Products · Workstations · Assembly Lines) + logout.
- `features/login/LoginPage.tsx`.
- `features/products/ProductPage.tsx` (exists; switch to apiClient).
- `features/workstations/WorkstationPage.tsx` — CRUD table + form.
- `features/lines/LinePage.tsx` — list, filter by product, CRUD (name, active, product).
- `features/lines/LineDetailPage.tsx` — ordered allocations with up/down + remove, and
  an "add workstation" picker from the available (un-allocated) workstations.
- API modules: `workstationsApi`, `linesApi` (incl. allocations), `authApi`.

State via React Query; mutations invalidate queries. Errors surfaced from
ProblemDetails `title`/`detail`.

## Error handling

Backend `ExceptionHandlingMiddleware` + `AddProblemDetails` already map domain errors
to ProblemDetails. Client reads `title`/`detail`; 401 clears the token and routes to
`/login`.

## Testing

xUnit handler tests (mirror `CreateProductCommandHandlerTests`): assembly-line create +
product filter; allocation add → reorder → remove (position integrity); login
valid/invalid. Plus `dotnet build`/`test`, frontend `tsc` + `vite build`.

## Deploy

Update `userdata.sh.tpl` compose env with `Jwt__Secret` and seed credentials; add
`jwt_secret` Terraform variable. Rebuild + push both images, `terraform apply` to
replace the instance, verify login and all pages on the live URL.

## Out of scope (YAGNI)

- User registration / multiple roles (single seeded user is enough).
- Drag-and-drop reorder (up/down buttons chosen).
- EF migrations (schema created via `EnsureCreatedAsync`; noted as a future exercise).
