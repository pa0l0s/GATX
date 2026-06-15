# GATX Stack Demo Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Build a learning demo for the `project.md` stack using the assembly line manager CRUD domain.

**Architecture:** Use Clean Architecture on the .NET backend, with MediatR handlers and EF Core PostgreSQL in Infrastructure. Use an Nx-style pnpm/Vite/React workspace for a minimal frontend consuming the Product API.

**Tech Stack:** .NET 8, ASP.NET Core Web API, EF Core, PostgreSQL, MediatR, FluentValidation, Serilog, React 18, TypeScript, Nx, Vite, pnpm, Docker, Terraform, GitHub Actions.

---

### Task 1: Backend Foundation

**Files:**
- Create: `backend/GATX.sln`
- Create: `backend/src/Gatx.Domain/Gatx.Domain.csproj`
- Create: `backend/src/Gatx.Application/Gatx.Application.csproj`
- Create: `backend/src/Gatx.Infrastructure/Gatx.Infrastructure.csproj`
- Create: `backend/src/Gatx.WebApi/Gatx.WebApi.csproj`

- [x] Create solution and project files with explicit project references.

### Task 2: Domain and Persistence

**Files:**
- Create: `backend/src/Gatx.Domain/Entities/*.cs`
- Create: `backend/src/Gatx.Infrastructure/Persistence/AppDbContext.cs`
- Create: `backend/src/Gatx.Infrastructure/Persistence/Configurations/*.cs`

- [x] Model products, lines, workstations, and ordered allocations.
- [x] Configure EF Core keys, indexes, cascade behavior, and seed data.

### Task 3: Product CQRS Slice

**Files:**
- Create: `backend/src/Gatx.Application/Products/Commands/*.cs`
- Create: `backend/src/Gatx.Application/Products/Queries/*.cs`
- Create: `backend/src/Gatx.WebApi/Controllers/ProductsController.cs`

- [x] Implement create, update, delete, and list products with MediatR.
- [x] Add FluentValidation for product names.

### Task 4: Frontend Product Page

**Files:**
- Create: `frontend/package.json`
- Create: `frontend/apps/assembly-manager/src/features/products/ProductPage.tsx`
- Create: `frontend/apps/assembly-manager/src/shared/api/productsApi.ts`

- [x] Implement a typed React page that lists, creates, renames, and deletes products.

### Task 5: DevOps and Learning Docs

**Files:**
- Create: `docker-compose.yml`
- Create: `backend/src/Gatx.WebApi/Dockerfile`
- Create: `frontend/apps/assembly-manager/Dockerfile`
- Create: `infra/terraform/*.tf`
- Create: `.github/workflows/ci-cd.yml`
- Create: `README.md`

- [x] Add local Docker runtime, CI, and AWS starter infrastructure notes.

### Task 6: Verification

- [x] Run `dotnet restore backend/GATX.sln`.
- [x] Run `dotnet build backend/GATX.sln --no-restore`.
- [x] Run backend tests when restore succeeds.
- [x] Run frontend install/build when pnpm dependencies are available.

## Self-Review

The plan covers the approved narrow scope: one working Product vertical slice, full domain model, local Docker, AWS Terraform starter, and CI. No placeholders remain in the implementation tasks.
