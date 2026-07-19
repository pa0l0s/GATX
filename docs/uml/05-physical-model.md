# 5. Physical Model

The **Physical Model** (UML **deployment view**) shows *where* the software runs: the
execution nodes (devices and execution environments), the artifacts deployed onto them,
and the communication paths between them. It also covers how those artifacts get there —
the CI/CD pipeline that builds and provisions everything.

## 5.1 Runtime deployment diagram

The running system is intentionally minimal and free-tier friendly: a **single EC2
instance** runs both containers via Docker Compose, talking to a managed **RDS
PostgreSQL** instance inside the default VPC.

```mermaid
flowchart TB
    user(("👤 Planner<br/>web browser"))

    subgraph AWS["☁️ AWS · region eu-central-1 · Default VPC"]
        direction TB

        subgraph EC2["«device» EC2 t3.micro — Amazon Linux 2023 (public subnet)"]
            direction TB
            subgraph Docker["«executionEnvironment» Docker Compose"]
                fe["«artifact»<br/>frontend container<br/>nginx :80<br/>(SPA + /api proxy)"]
                api["«artifact»<br/>api container<br/>ASP.NET :8080"]
            end
        end

        subgraph RDSNode["«device» RDS db.t3.micro (private)"]
            pg[("«artifact»<br/>PostgreSQL 16<br/>database")]
        end

        ecr[["«artifact store»<br/>ECR<br/>gatx-*-api / gatx-*-frontend"]]
    end

    user -->|"HTTP :80"| fe
    fe -->|"proxy /api → :8080"| api
    api -->|"TCP :5432 (VPC-internal, SG-restricted)"| pg
    EC2 -.->|"docker pull :latest<br/>(instance profile, ECR read-only)"| ecr

    classDef node fill:#eef2ff,stroke:#4338ca,color:#111
    class EC2,RDSNode node
```

**Node & artifact notes**

| Node / artifact | Detail |
|-----------------|--------|
| **EC2 t3.micro** | Amazon Linux 2023, 30 GB gp2 root, public subnet in the default VPC. Bootstrapped by `userdata.sh.tpl`: installs Docker + Compose, logs in to ECR, writes `docker-compose.yml`, `docker-compose up -d`. An hourly cron re-authenticates to ECR (tokens expire after 12 h). |
| **frontend container** | nginx serving the built SPA on port 80 and reverse-proxying `/api` to the api container. The only publicly exposed port (SG allows 80 and 22). |
| **api container** | ASP.NET Core on :8080, `ASPNETCORE_ENVIRONMENT=Production`; reads DB connection string and JWT secret from environment. Not published to the internet directly — reached only via the nginx proxy. |
| **RDS PostgreSQL 16** | `db.t3.micro`, 20 GB, `publicly_accessible = false`, reachable only from inside the VPC via a security group that allows :5432 from the VPC CIDR. |
| **ECR** | Two repositories per environment (`-api`, `-frontend`). EC2 pulls with an instance-profile role holding `AmazonEC2ContainerRegistryReadOnly`. |
| **IAM / access** | EC2 role also carries `AmazonSSMManagedInstanceCore`, so the box is managed via SSM Session Manager (no SSH key pair required). |

## 5.2 Deployment / CI-CD pipeline

The artifacts above are produced and placed by GitHub Actions using **OIDC federation**
(no long-lived AWS keys stored in GitHub). Terraform provisions the infrastructure;
`deploy.ps1` builds and pushes the container images.

```mermaid
flowchart LR
    dev(("👩‍💻 Developer")) -->|git push / PR| gh["«node» GitHub<br/>repo pa0l0s/GATX"]

    subgraph GHA["«executionEnvironment» GitHub Actions"]
        direction TB
        ci["ci-cd.yml<br/>build + test<br/>(backend · frontend · docker)"]
        plan["terraform-plan.yml<br/>plan on PR"]
        apply["terraform-apply.yml<br/>apply on merge to main<br/>dev → (gated) production"]
    end

    gh --> ci
    gh --> plan
    gh --> apply

    apply -->|"AssumeRoleWithWebIdentity<br/>(OIDC, no stored keys)"| role["«node» IAM Role<br/>GitHubActionsTerraform"]

    subgraph AWSc["☁️ AWS account 484908302042"]
        s3[("«artifact»<br/>S3 remote state<br/>+ native lockfile")]
        ecr2[["ECR images"]]
        infra["EC2 · RDS · SG · IAM<br/>(Terraform-managed)"]
    end

    role --> s3
    role --> infra
    apply -. "deploy.ps1: docker build + push" .-> ecr2

    classDef env fill:#e6f4ea,stroke:#34a853,color:#111
    class GHA env
```

**Pipeline stages**

1. **`ci-cd.yml`** — on every push/PR: restore/build/test the .NET solution, build the
   frontend (Node 22 + pnpm), and build both Docker images (validation only).
2. **`terraform-plan.yml`** — on PR: `terraform plan` for `dev` and `production`
   (matrix), posting the plan as a PR comment. No approval gate, so plans stay fast.
3. **`terraform-apply.yml`** — on merge to `main`: `apply-dev` runs automatically, then
   `apply-production` runs **only after manual approval** (GitHub Environment required
   reviewer). State lives in **S3** with native lockfile locking; auth is via **OIDC**
   into `GitHubActionsTerraform`.

## 5.3 Environments and a free-tier constraint

Two environments are defined — **dev** (`gatx-dev`) and **production** (`gatx-prod`) —
each with its own Terraform root, state key, EC2, RDS and ECR repos.

> ⚠️ **Free-plan account limitation.** The AWS account backing this project is a
> *free-plan* account, which caps the number of **RDS instances** (and low-tier EC2) that
> can exist at once. `dev` already consumes that single RDS slot, so `apply-production`
> fails with `InstanceQuotaExceeded`. The infrastructure code is correct — the two
> environments simply cannot run **simultaneously** on this account tier. To run
> production, either destroy `dev` first, share a single database across environments, or
> upgrade the account. See the repository's CI/CD notes for the current operational state.

This completes the five UML views: from *what* the system does
([Use Cases](01-use-case-model.md)), through *how* it behaves
([Dynamic](02-dynamic-model.md)) and *what* it is made of
([Logical](03-logical-model.md), [Component](04-component-model.md)), to *where* it runs
(this Physical model).
