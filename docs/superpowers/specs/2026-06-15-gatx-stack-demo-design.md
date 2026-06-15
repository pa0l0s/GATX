# GATX Stack Demo Design

## Goal

Create a learning-oriented demonstration project that ports the assembly line manager domain from the Node.js and Angular challenge into the stack required by `project.md`.

## Scope

The project demonstrates one complete vertical slice for `Product` CRUD and includes the domain model for products, assembly lines, workstations, and ordered allocations. The UI is intentionally simple because the primary goal is learning architecture, infrastructure, and the technology stack.

## Architecture

The backend uses Clean Architecture:

- `Gatx.Domain` owns entities and domain rules.
- `Gatx.Application` owns CQRS requests, validation, and interfaces.
- `Gatx.Infrastructure` owns EF Core PostgreSQL persistence.
- `Gatx.WebApi` owns HTTP endpoints, middleware, logging, and composition root.

The frontend is an Nx-style pnpm workspace with a Vite React TypeScript app. It has feature folders, typed API services, and a small Product CRUD page.

## Data Model

- `Product`: switchgear product name, with many assembly lines.
- `AssemblyLine`: line name, active flag, belongs to one product.
- `Workstation`: short name, full name, PC name.
- `AssemblyLineWorkstation`: many-to-many allocation with an explicit `Position` column.

## API Surface

The first vertical slice exposes:

- `GET /api/products`
- `POST /api/products`
- `PUT /api/products/{id}`
- `DELETE /api/products/{id}`

All handlers use MediatR. Reads use `AsNoTracking()`, commands use validation and EF Core async methods.

## Local Runtime

`docker-compose.yml` starts PostgreSQL, the backend, and the frontend. Configuration is provided through environment variables.

## AWS Learning Path

Terraform is a starter layout for AWS Free Tier-oriented experimentation. It favors small resources and includes comments warning that AWS can still charge for resources depending on account state, region, data transfer, and free-tier eligibility.

## Testing

The backend includes application-level tests for the create-product command validation and behavior. Verification focuses on restore/build/test for backend and install/build for frontend when dependencies are available.
