# GATX Stack Demonstration

This repository is a learning-oriented rewrite of the assembly line manager challenge using the stack from `project.md`.

## What It Demonstrates

- .NET 8 ASP.NET Core Web API
- Clean Architecture split into Domain, Application, Infrastructure, and WebApi projects
- EF Core with PostgreSQL
- CQRS handlers with MediatR
- FluentValidation pipeline behavior
- ProblemDetails-style global exception handling
- Serilog request logging
- React 18 with TypeScript, Vite, pnpm, and an Nx-style workspace
- Docker Compose for local PostgreSQL, API, and frontend
- Terraform starter files for AWS learning
- GitHub Actions CI/CD skeleton

## Domain

The challenge domain is an assembly line manager:

- Product: `name`
- Assembly line: `name`, `active`, belongs to one product
- Workstation: `short_name`, `name`, `pc_name`
- Allocation: many-to-many relationship between assembly lines and workstations with an explicit order

The implemented vertical slice is Product CRUD. The rest of the model is present so you can extend the same pattern.

## Local Development

Requirements:

- .NET SDK 8
- Node.js 20
- pnpm 11
- Docker Desktop

Run everything with Docker:

```powershell
docker compose up --build
```

Open:

- Frontend: http://localhost:4200
- API Swagger: http://localhost:5080/swagger
- Health check: http://localhost:5080/health

Run backend locally:

```powershell
dotnet restore backend/GATX.sln
dotnet build backend/GATX.sln
dotnet test backend/GATX.sln
dotnet run --project backend/src/Gatx.WebApi/Gatx.WebApi.csproj
```

Run frontend locally:

```powershell
corepack enable
pnpm install
pnpm start
```

## How The Backend Fits Together

`Gatx.Domain` contains entities and business invariants. `Product.Rename` trims and validates names.

`Gatx.Application` contains MediatR requests:

- `GetProductsQuery`
- `CreateProductCommand`
- `UpdateProductCommand`
- `DeleteProductCommand`

`Gatx.Infrastructure` contains `AppDbContext` and EF Core configuration for PostgreSQL.

`Gatx.WebApi` is the composition root. Controllers are thin and delegate to MediatR.

## AWS Free-Tier-Oriented Setup

The Terraform files are intentionally a starter, not a production platform. They create:

- ECR repositories for backend and frontend images
- A small PostgreSQL RDS instance definition

Important: AWS Free Tier depends on your account, region, resource type, usage, storage, and traffic. RDS, NAT Gateways, load balancers, public IPs, logs, and data transfer can create charges. For learning, prefer:

- Destroy resources immediately after testing: `terraform destroy`
- Avoid NAT Gateways until you understand their hourly cost
- Use the AWS Billing alarm and Free Tier usage alerts
- Keep one region and tag all resources

Example Terraform flow:

```powershell
cd infra/terraform
$env:TF_VAR_db_password = "change-me-for-local-learning"
terraform init
terraform plan
terraform apply
terraform destroy
```

## Next Learning Exercises

1. Add `AssemblyLine` CRUD by copying the Product CQRS pattern.
2. Add `Workstation` CRUD.
3. Add allocation endpoints with ordered positions.
4. Add authentication with ASP.NET Core Identity or an external OAuth provider.
5. Replace the starter Terraform with ECS Fargate or App Runner after estimating monthly cost.
