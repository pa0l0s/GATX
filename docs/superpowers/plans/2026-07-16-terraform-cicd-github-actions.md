# Terraform CI/CD with GitHub Actions Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add GitHub Actions workflows that run Terraform to provision this project's AWS infrastructure — plan on PR, apply on merge — across `dev` and `production` environments, authenticated via GitHub OIDC with no stored AWS keys.

**Architecture:** The flat `infra/terraform` config is reorganized into `infra/{bootstrap,environments/{dev,production},modules}`. Shared modules are moved verbatim. Each environment is a thin root calling the modules, with S3 remote state (native lockfile locking) and its bucket supplied at `init` time via `-backend-config`. A one-time, locally-applied `infra/bootstrap` creates the OIDC provider, the `GitHubActionsTerraform` IAM role, and the state bucket. Two workflows (`terraform-plan.yml`, `terraform-apply.yml`) use OIDC to assume the role.

**Tech Stack:** Terraform 1.15.6, hashicorp/aws `~> 5.80`, GitHub Actions (`aws-actions/configure-aws-credentials@v6`, `hashicorp/setup-terraform@v4`, `actions/github-script@v7`), AWS (IAM OIDC, S3, EC2, RDS, ECR, CloudWatch, SNS).

## Global Constraints

- Terraform version pinned everywhere: **1.15.6**.
- AWS provider constraint: **`~> 5.80`**.
- Default AWS region: **`eu-central-1`**. Billing module provider alias region: **`us-east-1`** (AWS billing metric requirement).
- Environment app names: dev = **`gatx-dev`**, production = **`gatx-prod`**.
- GitHub repo slug (OIDC subject scope): **`pa0l0s/GATX`**.
- No long-lived AWS credentials in GitHub — OIDC federation only.
- `db_password` and `jwt_secret` are sensitive and are **never** committed; they arrive as `TF_VAR_db_password` / `TF_VAR_jwt_secret` env vars from GitHub secrets. `terraform validate` does not need them; `terraform plan`/`apply` do.
- All committed HCL must be `terraform fmt`-clean.

## Secrets & variables (GitHub) — reference

| Name | Kind | Level | Purpose |
|---|---|---|---|
| `TF_VAR_DB_PASSWORD` | secret | repo | RDS password (shared dev/prod for this demo) |
| `TF_VAR_JWT_SECRET` | secret | repo | JWT signing secret (shared dev/prod for this demo) |
| `AWS_ROLE_ARN` | variable | repo | Role assumed via OIDC (from bootstrap output) |
| `AWS_REGION` | variable | repo | `eu-central-1` |
| `TF_STATE_BUCKET` | variable | repo | State bucket name (from bootstrap output) |

Repo-level (not environment-level) secrets are used deliberately so the **plan** job can read them without declaring `environment:`, which would otherwise trip production's required-reviewer gate on every PR. The `production` environment is still declared on the **apply** job, so the approval gate applies to applies. Splitting per-environment secrets is a documented follow-up.

## File structure (end state)

```
infra/
  bootstrap/            main.tf  variables.tf  outputs.tf          # applied once, locally
  environments/
    dev/                backend.tf  main.tf  variables.tf  outputs.tf  terraform.tfvars
    production/         backend.tf  main.tf  variables.tf  outputs.tf  terraform.tfvars
  modules/
    app/  database/  billing/                                      # moved from infra/terraform/modules
  deploy.ps1            # rewritten: image build/push only
  CICD.md              # manual GitHub setup steps
.github/workflows/
  terraform-plan.yml
  terraform-apply.yml
  ci-cd.yml            # untouched
docs/superpowers/...   # spec + this plan
```
`infra/terraform/` is deleted.

---

### Task 1: Restructure repo — move modules, delete old root, fix .gitignore

**Files:**
- Move: `infra/terraform/modules/` → `infra/modules/`
- Delete: `infra/terraform/main.tf`, `infra/terraform/variables.tf`, `infra/terraform/outputs.tf`, `infra/terraform/terraform.tfvars.example` (content is re-created per-environment in later tasks)
- Modify: `.gitignore`

**Interfaces:**
- Produces: modules at `infra/modules/{app,database,billing}` consumed by Tasks 2–3 via `source = "../../modules/<name>"`. Module input contracts (unchanged): `app` requires `app_name, aws_region, db_host, db_username, db_password, jwt_secret`; `database` requires `app_name, db_username, db_password` and outputs `host`, `endpoint`; `billing` requires `app_name, alert_email, threshold_usd(=1 default)`.

- [ ] **Step 1: Move modules and remove the old root with git**

```bash
cd "D:/Source/claude/GATX"
git mv infra/terraform/modules infra/modules
git rm infra/terraform/main.tf infra/terraform/variables.tf infra/terraform/outputs.tf infra/terraform/terraform.tfvars.example
```

- [ ] **Step 2: Verify the old root is gone and modules landed**

Run: `ls infra && ls infra/modules`
Expected: `infra` lists `modules` and `deploy.ps1` (no `terraform` dir); `infra/modules` lists `app  billing  database`.

- [ ] **Step 3: Allow env tfvars to be committed in .gitignore**

The current `.gitignore` line `terraform.tfvars` ignores every tfvars file. Add a negation immediately after it so only the non-secret environment tfvars are tracked. Change the `terraform.tfvars` line region to:

```gitignore
terraform.tfstate
terraform.tfstate.*
.terraform/
.terraform.lock.hcl
terraform.tfvars
!infra/environments/**/terraform.tfvars
```

- [ ] **Step 4: Verify env tfvars are no longer ignored**

Run: `git check-ignore infra/environments/dev/terraform.tfvars; echo "exit=$?"`
Expected: no path printed and `exit=1` (meaning: not ignored). (Directory need not exist yet; the pattern check is what matters.)

- [ ] **Step 5: Format-check the moved modules**

Run: `"$LOCALAPPDATA/Microsoft/WinGet/Packages/Hashicorp.Terraform_Microsoft.Winget.Source_8wekyb3d8bbwe/terraform.exe" -chdir=infra fmt -check -recursive`
Expected: exits 0 with no output (modules were already formatted). If it lists files, run the same without `-check` to fix, then re-run.

> Note: `terraform` may not be on this shell's PATH yet (installed via winget this session). Use the full path above, or prepend it: `$env:PATH = "$env:LOCALAPPDATA\Microsoft\WinGet\Packages\Hashicorp.Terraform_Microsoft.Winget.Source_8wekyb3d8bbwe;$env:PATH"`. Later tasks assume `terraform` resolves; use this same fallback if it does not.

- [ ] **Step 6: Commit**

```bash
git add -A
git commit -m "refactor: move terraform modules to infra/modules; allow env tfvars"
```

---

### Task 2: `dev` environment configuration

**Files:**
- Create: `infra/environments/dev/backend.tf`
- Create: `infra/environments/dev/main.tf`
- Create: `infra/environments/dev/variables.tf`
- Create: `infra/environments/dev/outputs.tf`
- Create: `infra/environments/dev/terraform.tfvars`

**Interfaces:**
- Consumes: modules from Task 1 (`../../modules/{app,database,billing}`).
- Produces: outputs `database_endpoint`, `app_url`, `ecr_api`, `ecr_frontend`, `billing_sns_arn` (consumed by `deploy.ps1` in Task 7). Backend state key `dev/terraform.tfstate`; bucket supplied via `-backend-config`.

- [ ] **Step 1: Create `infra/environments/dev/backend.tf`**

```hcl
terraform {
  backend "s3" {
    key          = "dev/terraform.tfstate"
    region       = "eu-central-1"
    encrypt      = true
    use_lockfile = true
  }
}
```

- [ ] **Step 2: Create `infra/environments/dev/main.tf`**

```hcl
terraform {
  required_version = ">= 1.8.0"

  required_providers {
    aws = {
      source  = "hashicorp/aws"
      version = "~> 5.80"
    }
  }
}

provider "aws" {
  region = var.aws_region
}

provider "aws" {
  alias  = "us_east_1"
  region = "us-east-1"
}

module "app" {
  source = "../../modules/app"

  app_name    = var.app_name
  aws_region  = var.aws_region
  db_host     = module.database.host
  db_username = var.db_username
  db_password = var.db_password
  jwt_secret  = var.jwt_secret
}

module "database" {
  source = "../../modules/database"

  app_name    = var.app_name
  db_username = var.db_username
  db_password = var.db_password
}

module "billing" {
  source = "../../modules/billing"

  providers = {
    aws = aws.us_east_1
  }

  app_name      = var.app_name
  alert_email   = var.billing_alert_email
  threshold_usd = var.billing_threshold_usd
}
```

- [ ] **Step 3: Create `infra/environments/dev/variables.tf`**

```hcl
variable "aws_region" {
  description = "AWS region for this environment."
  type        = string
  default     = "eu-central-1"
}

variable "app_name" {
  description = "Name prefix for this environment's resources."
  type        = string
  default     = "gatx-dev"
}

variable "db_username" {
  description = "Database username."
  type        = string
  default     = "gatx"
}

variable "db_password" {
  description = "Database password. Supplied via TF_VAR_db_password; never committed."
  type        = string
  sensitive   = true
}

variable "jwt_secret" {
  description = "JWT signing secret. Supplied via TF_VAR_jwt_secret; never committed."
  type        = string
  sensitive   = true
}

variable "billing_alert_email" {
  description = "Email address to receive billing alerts."
  type        = string
}

variable "billing_threshold_usd" {
  description = "Send billing alert when estimated charges exceed this amount (USD)."
  type        = number
  default     = 1
}
```

- [ ] **Step 4: Create `infra/environments/dev/outputs.tf`**

```hcl
output "database_endpoint" {
  description = "RDS endpoint (host:port)."
  value       = module.database.endpoint
}

output "app_url" {
  description = "Open once the EC2 instance finishes startup (~3 min)."
  value       = "http://${module.app.ec2_public_ip}"
}

output "ecr_api" {
  description = "API image repository URL."
  value       = module.app.api_repository_url
}

output "ecr_frontend" {
  description = "Frontend image repository URL."
  value       = module.app.frontend_repository_url
}

output "billing_sns_arn" {
  description = "SNS topic ARN for billing alerts."
  value       = module.billing.sns_topic_arn
}
```

- [ ] **Step 5: Create `infra/environments/dev/terraform.tfvars`**

Replace `your@email.com` with the real billing-alert email before committing.

```hcl
aws_region            = "eu-central-1"
app_name              = "gatx-dev"
db_username           = "gatx"
billing_alert_email   = "your@email.com"
billing_threshold_usd = 1
```

- [ ] **Step 6: Validate without a backend or secrets**

Run:
```
terraform -chdir=infra/environments/dev init -backend=false -input=false
terraform -chdir=infra/environments/dev validate
```
Expected: init succeeds (downloads aws provider); `validate` prints `Success! The configuration is valid.` (`validate` does not require `db_password`/`jwt_secret` values.)

- [ ] **Step 7: Format-check**

Run: `terraform -chdir=infra/environments/dev fmt -check`
Expected: exits 0, no output.

- [ ] **Step 8: Commit**

```bash
git add infra/environments/dev
git commit -m "feat(infra): add dev environment terraform config"
```

---

### Task 3: `production` environment configuration

**Files:**
- Create: `infra/environments/production/backend.tf`
- Create: `infra/environments/production/main.tf`
- Create: `infra/environments/production/variables.tf`
- Create: `infra/environments/production/outputs.tf`
- Create: `infra/environments/production/terraform.tfvars`

**Interfaces:**
- Identical to Task 2 except state key `production/terraform.tfstate` and `app_name` default `gatx-prod`.

- [ ] **Step 1: Create `infra/environments/production/backend.tf`**

```hcl
terraform {
  backend "s3" {
    key          = "production/terraform.tfstate"
    region       = "eu-central-1"
    encrypt      = true
    use_lockfile = true
  }
}
```

- [ ] **Step 2: Create `infra/environments/production/main.tf`**

Identical to `dev/main.tf` (Task 2 Step 2). Reproduced in full:

```hcl
terraform {
  required_version = ">= 1.8.0"

  required_providers {
    aws = {
      source  = "hashicorp/aws"
      version = "~> 5.80"
    }
  }
}

provider "aws" {
  region = var.aws_region
}

provider "aws" {
  alias  = "us_east_1"
  region = "us-east-1"
}

module "app" {
  source = "../../modules/app"

  app_name    = var.app_name
  aws_region  = var.aws_region
  db_host     = module.database.host
  db_username = var.db_username
  db_password = var.db_password
  jwt_secret  = var.jwt_secret
}

module "database" {
  source = "../../modules/database"

  app_name    = var.app_name
  db_username = var.db_username
  db_password = var.db_password
}

module "billing" {
  source = "../../modules/billing"

  providers = {
    aws = aws.us_east_1
  }

  app_name      = var.app_name
  alert_email   = var.billing_alert_email
  threshold_usd = var.billing_threshold_usd
}
```

- [ ] **Step 3: Create `infra/environments/production/variables.tf`**

Identical to dev except the `app_name` default. Reproduced in full:

```hcl
variable "aws_region" {
  description = "AWS region for this environment."
  type        = string
  default     = "eu-central-1"
}

variable "app_name" {
  description = "Name prefix for this environment's resources."
  type        = string
  default     = "gatx-prod"
}

variable "db_username" {
  description = "Database username."
  type        = string
  default     = "gatx"
}

variable "db_password" {
  description = "Database password. Supplied via TF_VAR_db_password; never committed."
  type        = string
  sensitive   = true
}

variable "jwt_secret" {
  description = "JWT signing secret. Supplied via TF_VAR_jwt_secret; never committed."
  type        = string
  sensitive   = true
}

variable "billing_alert_email" {
  description = "Email address to receive billing alerts."
  type        = string
}

variable "billing_threshold_usd" {
  description = "Send billing alert when estimated charges exceed this amount (USD)."
  type        = number
  default     = 1
}
```

- [ ] **Step 4: Create `infra/environments/production/outputs.tf`**

Identical to dev (Task 2 Step 4). Reproduced in full:

```hcl
output "database_endpoint" {
  description = "RDS endpoint (host:port)."
  value       = module.database.endpoint
}

output "app_url" {
  description = "Open once the EC2 instance finishes startup (~3 min)."
  value       = "http://${module.app.ec2_public_ip}"
}

output "ecr_api" {
  description = "API image repository URL."
  value       = module.app.api_repository_url
}

output "ecr_frontend" {
  description = "Frontend image repository URL."
  value       = module.app.frontend_repository_url
}

output "billing_sns_arn" {
  description = "SNS topic ARN for billing alerts."
  value       = module.billing.sns_topic_arn
}
```

- [ ] **Step 5: Create `infra/environments/production/terraform.tfvars`**

Replace `your@email.com` with the real billing-alert email before committing.

```hcl
aws_region            = "eu-central-1"
app_name              = "gatx-prod"
db_username           = "gatx"
billing_alert_email   = "your@email.com"
billing_threshold_usd = 1
```

- [ ] **Step 6: Validate**

Run:
```
terraform -chdir=infra/environments/production init -backend=false -input=false
terraform -chdir=infra/environments/production validate
```
Expected: `Success! The configuration is valid.`

- [ ] **Step 7: Format-check**

Run: `terraform -chdir=infra/environments/production fmt -check`
Expected: exits 0, no output.

- [ ] **Step 8: Commit**

```bash
git add infra/environments/production
git commit -m "feat(infra): add production environment terraform config"
```

---

### Task 4: Bootstrap configuration (OIDC provider, CI role, state bucket)

**Files:**
- Create: `infra/bootstrap/main.tf`
- Create: `infra/bootstrap/variables.tf`
- Create: `infra/bootstrap/outputs.tf`

**Interfaces:**
- Produces (as `terraform output`): `role_arn` (→ GitHub `AWS_ROLE_ARN` variable) and `state_bucket` (→ GitHub `TF_STATE_BUCKET` variable). Uses local state (it creates the remote backend). Bucket name = `gatx-tfstate-<account_id>`.

- [ ] **Step 1: Create `infra/bootstrap/main.tf`**

```hcl
terraform {
  required_version = ">= 1.8.0"

  required_providers {
    aws = {
      source  = "hashicorp/aws"
      version = "~> 5.80"
    }
  }
}

provider "aws" {
  region = var.aws_region
}

data "aws_caller_identity" "current" {}

locals {
  state_bucket = "gatx-tfstate-${data.aws_caller_identity.current.account_id}"
}

# ── GitHub OIDC provider ──────────────────────────────────────────────────────
resource "aws_iam_openid_connect_provider" "github" {
  url             = "https://token.actions.githubusercontent.com"
  client_id_list  = ["sts.amazonaws.com"]
  thumbprint_list = ["6938fd4d98bab03faadb97b34396831e3780aea1"]
}

# ── CI role assumed by GitHub Actions via OIDC ────────────────────────────────
resource "aws_iam_role" "github_actions_terraform" {
  name = "GitHubActionsTerraform"

  assume_role_policy = jsonencode({
    Version = "2012-10-17"
    Statement = [{
      Effect    = "Allow"
      Principal = { Federated = aws_iam_openid_connect_provider.github.arn }
      Action    = "sts:AssumeRoleWithWebIdentity"
      Condition = {
        StringEquals = {
          "token.actions.githubusercontent.com:aud" = "sts.amazonaws.com"
        }
        StringLike = {
          "token.actions.githubusercontent.com:sub" = "repo:pa0l0s/GATX:*"
        }
      }
    }]
  })
}

resource "aws_iam_role_policy_attachment" "admin" {
  role       = aws_iam_role.github_actions_terraform.name
  policy_arn = "arn:aws:iam::aws:policy/AdministratorAccess"
}

# ── Remote state bucket ───────────────────────────────────────────────────────
resource "aws_s3_bucket" "state" {
  bucket = local.state_bucket
}

resource "aws_s3_bucket_versioning" "state" {
  bucket = aws_s3_bucket.state.id
  versioning_configuration {
    status = "Enabled"
  }
}

resource "aws_s3_bucket_server_side_encryption_configuration" "state" {
  bucket = aws_s3_bucket.state.id
  rule {
    apply_server_side_encryption_by_default {
      sse_algorithm = "AES256"
    }
  }
}

resource "aws_s3_bucket_public_access_block" "state" {
  bucket                  = aws_s3_bucket.state.id
  block_public_acls       = true
  block_public_policy     = true
  ignore_public_acls      = true
  restrict_public_buckets = true
}
```

- [ ] **Step 2: Create `infra/bootstrap/variables.tf`**

```hcl
variable "aws_region" {
  description = "AWS region for bootstrap resources (state bucket, IAM is global)."
  type        = string
  default     = "eu-central-1"
}
```

- [ ] **Step 3: Create `infra/bootstrap/outputs.tf`**

```hcl
output "role_arn" {
  description = "Set this as the GitHub Actions variable AWS_ROLE_ARN."
  value       = aws_iam_role.github_actions_terraform.arn
}

output "state_bucket" {
  description = "Set this as the GitHub Actions variable TF_STATE_BUCKET."
  value       = aws_s3_bucket.state.bucket
}
```

- [ ] **Step 4: Validate (no AWS credentials required)**

Run:
```
terraform -chdir=infra/bootstrap init -input=false
terraform -chdir=infra/bootstrap validate
terraform -chdir=infra/bootstrap fmt -check
```
Expected: init downloads the aws provider; `validate` → `Success! The configuration is valid.`; `fmt -check` exits 0. (Do **not** run `apply` here — that requires admin AWS creds and is a manual operator step documented in Task 7's `CICD.md`.)

- [ ] **Step 5: Commit**

```bash
git add infra/bootstrap
git commit -m "feat(infra): add bootstrap for OIDC role and remote state bucket"
```

---

### Task 5: `terraform-plan.yml` workflow

**Files:**
- Create: `.github/workflows/terraform-plan.yml`

**Interfaces:**
- Consumes GitHub config: secrets `TF_VAR_DB_PASSWORD`, `TF_VAR_JWT_SECRET`; variables `AWS_ROLE_ARN`, `AWS_REGION`, `TF_STATE_BUCKET`. Operates on `infra/environments/{dev,production}` from Tasks 2–3.

- [ ] **Step 1: Create `.github/workflows/terraform-plan.yml`**

```yaml
name: Terraform Plan

on:
  pull_request:
    branches: [main]
    paths:
      - 'infra/environments/**'
      - 'infra/modules/**'

permissions:
  id-token: write
  contents: read
  pull-requests: write

jobs:
  plan:
    name: Plan (${{ matrix.environment }})
    runs-on: ubuntu-latest
    strategy:
      fail-fast: false
      matrix:
        environment: [dev, production]
    defaults:
      run:
        working-directory: infra/environments/${{ matrix.environment }}
    env:
      TF_VAR_db_password: ${{ secrets.TF_VAR_DB_PASSWORD }}
      TF_VAR_jwt_secret: ${{ secrets.TF_VAR_JWT_SECRET }}
    steps:
      - name: Checkout
        uses: actions/checkout@v4

      - name: Configure AWS credentials via OIDC
        uses: aws-actions/configure-aws-credentials@v6
        with:
          role-to-assume: ${{ vars.AWS_ROLE_ARN }}
          aws-region: ${{ vars.AWS_REGION }}

      - name: Setup Terraform
        uses: hashicorp/setup-terraform@v4
        with:
          terraform_version: 1.15.6

      - name: Terraform Init
        run: terraform init -no-color -input=false -backend-config="bucket=${{ vars.TF_STATE_BUCKET }}"

      - name: Terraform Validate
        run: terraform validate -no-color

      - name: Terraform Format Check
        id: fmt
        run: terraform fmt -check -recursive
        continue-on-error: true

      - name: Terraform Plan
        id: plan
        run: terraform plan -no-color -input=false -out=tfplan
        continue-on-error: true

      - name: Comment plan on PR
        uses: actions/github-script@v7
        env:
          PLAN: ${{ steps.plan.outputs.stdout }}
          ENVIRONMENT: ${{ matrix.environment }}
        with:
          script: |
            const plan = process.env.PLAN || '';
            const truncated = plan.length > 60000
              ? plan.substring(0, 60000) + '\n\n... (truncated)'
              : plan;
            const body = [
              `### Terraform Plan — ${process.env.ENVIRONMENT}`,
              '```',
              truncated,
              '```',
            ].join('\n');
            await github.rest.issues.createComment({
              issue_number: context.issue.number,
              owner: context.repo.owner,
              repo: context.repo.repo,
              body,
            });

      - name: Fail if plan failed
        if: steps.plan.outcome == 'failure'
        run: exit 1
```

- [ ] **Step 2: Whitespace check**

Run: `git add .github/workflows/terraform-plan.yml && git diff --cached --check`
Expected: no output (no trailing-whitespace / conflict markers).

- [ ] **Step 3: Structural read-through**

Confirm by eye: `permissions.id-token: write` present; matrix `[dev, production]`; `working-directory` uses the matrix; init passes `-backend-config` for the bucket; `TF_VAR_db_password`/`TF_VAR_jwt_secret` mapped from the uppercase secrets. (Authoritative validation is the first PR run — Task 7 Step 6.)

- [ ] **Step 4: Commit**

```bash
git commit -m "ci: add terraform plan-on-PR workflow"
```

---

### Task 6: `terraform-apply.yml` workflow

**Files:**
- Create: `.github/workflows/terraform-apply.yml`

**Interfaces:**
- Consumes the same GitHub config as Task 5. `apply-production` declares `environment: production` so its required-reviewer gate applies.

- [ ] **Step 1: Create `.github/workflows/terraform-apply.yml`**

```yaml
name: Terraform Apply

on:
  push:
    branches: [main]
    paths:
      - 'infra/environments/**'
      - 'infra/modules/**'

permissions:
  id-token: write
  contents: read

concurrency:
  group: terraform-apply-${{ github.ref }}
  cancel-in-progress: false

jobs:
  apply-dev:
    name: Apply Dev
    runs-on: ubuntu-latest
    environment: dev
    defaults:
      run:
        working-directory: infra/environments/dev
    env:
      TF_VAR_db_password: ${{ secrets.TF_VAR_DB_PASSWORD }}
      TF_VAR_jwt_secret: ${{ secrets.TF_VAR_JWT_SECRET }}
    steps:
      - name: Checkout
        uses: actions/checkout@v4

      - name: Configure AWS credentials via OIDC
        uses: aws-actions/configure-aws-credentials@v6
        with:
          role-to-assume: ${{ vars.AWS_ROLE_ARN }}
          aws-region: ${{ vars.AWS_REGION }}

      - name: Setup Terraform
        uses: hashicorp/setup-terraform@v4
        with:
          terraform_version: 1.15.6

      - name: Terraform Init
        run: terraform init -no-color -input=false -backend-config="bucket=${{ vars.TF_STATE_BUCKET }}"

      - name: Terraform Apply
        run: terraform apply -auto-approve -no-color -input=false

  apply-production:
    name: Apply Production
    needs: apply-dev
    runs-on: ubuntu-latest
    environment: production
    defaults:
      run:
        working-directory: infra/environments/production
    env:
      TF_VAR_db_password: ${{ secrets.TF_VAR_DB_PASSWORD }}
      TF_VAR_jwt_secret: ${{ secrets.TF_VAR_JWT_SECRET }}
    steps:
      - name: Checkout
        uses: actions/checkout@v4

      - name: Configure AWS credentials via OIDC
        uses: aws-actions/configure-aws-credentials@v6
        with:
          role-to-assume: ${{ vars.AWS_ROLE_ARN }}
          aws-region: ${{ vars.AWS_REGION }}

      - name: Setup Terraform
        uses: hashicorp/setup-terraform@v4
        with:
          terraform_version: 1.15.6

      - name: Terraform Init
        run: terraform init -no-color -input=false -backend-config="bucket=${{ vars.TF_STATE_BUCKET }}"

      - name: Terraform Apply
        run: terraform apply -auto-approve -no-color -input=false
```

- [ ] **Step 2: Whitespace check**

Run: `git add .github/workflows/terraform-apply.yml && git diff --cached --check`
Expected: no output.

- [ ] **Step 3: Structural read-through**

Confirm: `apply-production` has `needs: apply-dev` and `environment: production`; `concurrency.cancel-in-progress: false`; each job's `working-directory` points at its env; init passes the bucket backend-config.

- [ ] **Step 4: Commit**

```bash
git commit -m "ci: add terraform apply-on-merge workflow with prod gate"
```

---

### Task 7: Rewrite `deploy.ps1`, add `CICD.md`, update README

**Files:**
- Overwrite: `infra/deploy.ps1`
- Create: `infra/CICD.md`
- Modify: `README.md` (replace the "AWS Free-Tier-Oriented Setup" Terraform-flow block)

**Interfaces:**
- Consumes env outputs `ecr_api`, `ecr_frontend`, `app_url` from Tasks 2–3.

- [ ] **Step 1: Overwrite `infra/deploy.ps1` (image build/push only)**

```powershell
#!/usr/bin/env pwsh
# deploy.ps1 — Build and push GATX app images to the ECR repos created by Terraform.
#
# Infrastructure is now managed by GitHub Actions (see infra/CICD.md), NOT this script.
# This script only builds and pushes the Docker images for an already-provisioned
# environment, then relies on the EC2 instance to pull them.
#
# Prerequisites:
#   - AWS CLI configured with credentials that can read the env's Terraform state + ECR
#   - Docker Desktop running
#   - The target environment already applied by the pipeline
#
# Run from the repo root:  .\infra\deploy.ps1 -Environment dev

param(
  [ValidateSet("dev", "production")]
  [string]$Environment = "dev",
  [string]$Region = "eu-central-1"
)

$ErrorActionPreference = "Stop"
$RepoRoot = Split-Path $PSScriptRoot -Parent
$EnvDir = Join-Path $PSScriptRoot "environments/$Environment"

Write-Host ""
Write-Host "=== GATX image push ($Environment) ===" -ForegroundColor Cyan

# ── Resolve state bucket from the caller's AWS account ────────────────────────
$AccountId = (aws sts get-caller-identity --query Account --output text).Trim()
$StateBucket = "gatx-tfstate-$AccountId"

# ── Read ECR repo URLs from Terraform outputs ────────────────────────────────
Push-Location $EnvDir
terraform init -input=false -backend-config="bucket=$StateBucket" | Out-Null
$ECR_API = (terraform output -raw ecr_api)
$ECR_FRONTEND = (terraform output -raw ecr_frontend)
$APP_URL = (terraform output -raw app_url)
Pop-Location

$Registry = ($ECR_API -split "/")[0]

# ── Authenticate Docker to ECR ───────────────────────────────────────────────
aws ecr get-login-password --region $Region |
  docker login --username AWS --password-stdin $Registry

# ── Build and push both images ───────────────────────────────────────────────
Push-Location $RepoRoot
docker build -f backend/src/Gatx.WebApi/Dockerfile -t "${ECR_API}:latest" .
docker push "${ECR_API}:latest"

docker build -f frontend/apps/assembly-manager/Dockerfile -t "${ECR_FRONTEND}:latest" .
docker push "${ECR_FRONTEND}:latest"
Pop-Location

Write-Host ""
Write-Host "Images pushed. App URL: $APP_URL" -ForegroundColor Green
Write-Host "(EC2 pulls new images on its hourly cycle or on next boot.)" -ForegroundColor Gray
```

- [ ] **Step 2: Create `infra/CICD.md`**

```markdown
# GATX Infrastructure CI/CD

AWS infrastructure is provisioned by GitHub Actions with Terraform.

- **Plan on PR** (`.github/workflows/terraform-plan.yml`): any PR touching
  `infra/environments/**` or `infra/modules/**` runs `plan` for both `dev` and
  `production` and comments the output on the PR.
- **Apply on merge** (`.github/workflows/terraform-apply.yml`): merging to `main`
  applies `dev` automatically, then `production` after a required-reviewer approval.

Auth is GitHub OIDC — no AWS keys are stored in GitHub.

## One-time bootstrap (operator, local, admin AWS creds)

The CI role and state bucket must exist before any workflow can run.

```bash
cd infra/bootstrap
terraform init
terraform apply            # creates OIDC provider, GitHubActionsTerraform role, state bucket
terraform output           # note role_arn and state_bucket
```

## One-time GitHub configuration

**Settings → Secrets and variables → Actions → Variables (repo):**
- `AWS_ROLE_ARN` = bootstrap output `role_arn`
- `AWS_REGION` = `eu-central-1`
- `TF_STATE_BUCKET` = bootstrap output `state_bucket`

**Settings → Secrets and variables → Actions → Secrets (repo):**
- `TF_VAR_DB_PASSWORD` = a strong database password
- `TF_VAR_JWT_SECRET` = a strong random JWT signing secret

**Settings → Environments:**
- Create `dev` (no protection).
- Create `production` and enable **Required reviewers** (add yourself). This gates
  the production apply.

## Deploying application images

Infrastructure only creates the ECR repos and EC2 host. Push app images with:

```powershell
.\infra\deploy.ps1 -Environment dev
```

## Follow-ups
- Replace `AdministratorAccess` on the CI role with a least-privilege policy.
- Split `TF_VAR_DB_PASSWORD` / `TF_VAR_JWT_SECRET` into per-environment secrets.
```

- [ ] **Step 3: Update `README.md`**

Replace the section from the `## AWS Free-Tier-Oriented Setup` heading through the end of its `terraform apply` / `terraform destroy` example block with a pointer to the new pipeline:

```markdown
## AWS Deployment (CI/CD)

AWS infrastructure is managed by GitHub Actions + Terraform, split into `dev` and
`production` environments with OIDC auth (no stored keys). See
[`infra/CICD.md`](infra/CICD.md) for the one-time bootstrap and GitHub setup.

- Open a PR touching `infra/**` → Terraform `plan` is posted as a PR comment.
- Merge to `main` → `dev` applies automatically; `production` applies after approval.

Push application images to an environment's ECR repos with:

```powershell
.\infra\deploy.ps1 -Environment dev
```

Cost note: `dev` and `production` each provision an EC2 instance and an RDS
database. Destroy an environment when you are done learning by running
`terraform destroy` in `infra/environments/<env>` (with the same
`-backend-config="bucket=..."` used by the pipeline).
```

- [ ] **Step 4: Format-check the whole infra tree**

Run: `terraform -chdir=infra fmt -check -recursive`
Expected: exits 0. (bootstrap + both envs + modules all clean.)

- [ ] **Step 5: Commit**

```bash
git add infra/deploy.ps1 infra/CICD.md README.md
git commit -m "docs: rewrite deploy.ps1 for image push; document CI/CD setup"
```

- [ ] **Step 6: End-to-end validation (operator, after bootstrap + GitHub config)**

This is the authoritative test of the workflows and cannot run locally.
1. Push the branch and open a PR against `main`.
2. Confirm the **Terraform Plan** workflow runs both matrix legs (`dev`, `production`)
   and posts a plan comment on the PR. A green plan proves OIDC auth, the S3
   backend + lockfile, and module wiring.
3. Merge the PR. Confirm **Apply Dev** runs to completion, then **Apply Production**
   waits on the required-reviewer approval before applying.

---

## Self-Review

**Spec coverage:**
- Restructure to `environments/` + `modules/` → Task 1.
- OIDC provider + role + trust scope → Task 4.
- S3 backend + `use_lockfile` → Tasks 2–4 (backend.tf) + Task 4 (bucket).
- `db_password` via secret (+ discovered `jwt_secret`) → Tasks 2/3 variables, Tasks 5/6 env mapping, Task 7 CICD.md.
- `.gitignore` fix for env tfvars → Task 1.
- plan-on-PR / apply-on-merge with prod gate → Tasks 5, 6.
- `deploy.ps1` = image push only → Task 7.
- GitHub config steps → Task 7 (`CICD.md`).
- `AdministratorAccess` + least-privilege follow-up → Task 4 + Task 7 follow-ups.

**Deviations from the design doc (intentional, noted for the reviewer):**
1. State bucket passed via `-backend-config="bucket=..."` (GitHub `TF_STATE_BUCKET` variable) instead of a hardcoded/derived name — keeps the account id out of git.
2. `TF_VAR_DB_PASSWORD`/`TF_VAR_JWT_SECRET` are **repo-level** secrets (shared across envs) rather than per-environment, so the plan job runs without tripping the production approval gate. Per-env split is a follow-up.
3. Added `jwt_secret` handling — the `app` module requires it; the design doc only listed `db_password`.

**Placeholder scan:** none — every file's full content is inline. `your@email.com` in tfvars and the bucket account-id are real operator inputs, flagged at their step.

**Type/name consistency:** module input names match the moved modules (`app`: `app_name, aws_region, db_host, db_username, db_password, jwt_secret`; `database`: `app_name, db_username, db_password`; `billing`: `app_name, alert_email, threshold_usd`). Output names (`ecr_api`, `ecr_frontend`, `app_url`, `database_endpoint`, `billing_sns_arn`) are consistent across env outputs.tf and `deploy.ps1`.
