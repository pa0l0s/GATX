# Terraform CI/CD with GitHub Actions for AWS — Design

**Date:** 2026-07-16
**Repo:** `github.com:pa0l0s/GATX`
**Source reference:** https://oneuptime.com/blog/post/2026-02-12-terraform-cicd-github-actions-for-aws/view

## Goal

Add GitHub Actions CI/CD that manages the AWS **infrastructure** for this project
with Terraform: plan on pull request, apply on merge to `main`, across two
environments (`dev` and `production`) with a manual approval gate on production.
Authentication uses GitHub OIDC federation (no long-lived AWS keys stored in GitHub).

## Scope

**In scope**
- OIDC-based auth between GitHub Actions and AWS.
- Remote Terraform state in S3 with native lockfile locking.
- Two environments: `dev` (auto-apply on merge) and `production` (manual approval).
- Plan-on-PR workflow that comments the plan on the PR.
- Apply-on-merge workflow.
- Restructure existing `infra/terraform` into an `environments/` + `modules/` layout.

**Out of scope (unchanged / manual)**
- Building and pushing application Docker images (stays in `deploy.ps1`).
- Rolling the running EC2 app after infra changes.
- The existing `.github/workflows/ci-cd.yml` (dotnet/pnpm/docker build) — untouched.

## Decisions

| Decision | Choice | Rationale |
|---|---|---|
| Environments | `dev` + `production` | Realistic promotion flow with prod approval gate. |
| Pipeline scope | Terraform infra only | Clean separation from app image delivery. |
| Auth | GitHub OIDC federation | No stored AWS credentials; matches article. |
| State | S3 backend, `use_lockfile = true` | Terraform 1.15.6 supports native S3 locking; no DynamoDB. |
| CI role permissions | `AdministratorAccess` | Matches article; single-owner learning demo. Least-privilege is a documented follow-up. |
| Secret handling | `db_password` via GitHub Environment secret `TF_VAR_DB_PASSWORD` | Required + sensitive var cannot be interactive under auto-apply. |

## Target repository structure

```
infra/
  bootstrap/                 # applied ONCE, manually, with admin creds
    main.tf                  #   OIDC provider + GitHubActionsTerraform role + S3 state bucket
    variables.tf
    outputs.tf               #   role_arn, state_bucket (copied into GitHub config)
  environments/
    dev/
      backend.tf             # S3 backend, key = dev/terraform.tfstate, use_lockfile = true
      main.tf                # calls ../../modules/*, app_name = gatx-dev
      variables.tf
      terraform.tfvars       # non-secret: aws_region, app_name, billing_alert_email, threshold
    production/
      backend.tf             # S3 backend, key = production/terraform.tfstate, use_lockfile = true
      main.tf                # calls ../../modules/*, app_name = gatx-prod
      variables.tf
      terraform.tfvars
  modules/
    app/                     # moved verbatim from infra/terraform/modules/app
    database/                # moved verbatim from infra/terraform/modules/database
    billing/                 # moved verbatim from infra/terraform/modules/billing
  deploy.ps1                 # updated: image build/push only; reads ECR URL via `terraform output`
```

The old `infra/terraform/` directory is removed. Each environment `main.tf` is the
current root `main.tf` (app + database + billing modules) parameterized only by
`app_name` and its tfvars.

## Bootstrap (one-time, manual)

`infra/bootstrap` is applied once by the repo owner using local admin AWS credentials.
It is a chicken-and-egg prerequisite: the workflows cannot assume a role that does
not yet exist, and creating that role requires admin credentials that are not (yet)
configured locally. Bootstrap creates:

1. **IAM OIDC provider** for `https://token.actions.githubusercontent.com`
   (client id `sts.amazonaws.com`).
2. **IAM role `GitHubActionsTerraform`**, trust policy conditions:
   - `token.actions.githubusercontent.com:aud = sts.amazonaws.com`
   - `token.actions.githubusercontent.com:sub` like `repo:pa0l0s/GATX:*`
   - Attached policy: `arn:aws:iam::aws:policy/AdministratorAccess`.
3. **S3 bucket** for Terraform state — versioning enabled, SSE (AES256) enabled,
   public access blocked.

Bootstrap uses **local state** (it is the thing that creates the remote backend).
Its outputs (`role_arn`, `state_bucket`) are copied into GitHub configuration.

> The bootstrap S3 bucket name must be globally unique; it will be derived from a
> stable prefix plus the AWS account id (looked up via `aws_caller_identity`).

## Authentication flow

1. Workflow requests an OIDC token from GitHub (`permissions: id-token: write`).
2. `aws-actions/configure-aws-credentials@v6` exchanges it, assuming
   `GitHubActionsTerraform` via `sts:AssumeRoleWithWebIdentity`.
3. Terraform runs with temporary, auto-expiring credentials. No secrets stored.

## Secrets & variables

| Name | Type | Location | Used for |
|---|---|---|---|
| `TF_VAR_DB_PASSWORD` | secret | GitHub **Environment** (dev + production, separate values) | `db_password` Terraform var |
| `AWS_ROLE_ARN` | variable | GitHub repo (or environment) variable | `role-to-assume` in both workflows |
| `AWS_REGION` | variable | GitHub repo variable (default `eu-central-1`) | `aws-region` in both workflows |
| `aws_region`, `app_name`, `billing_alert_email`, `billing_threshold_usd` | committed | each env `terraform.tfvars` | non-secret Terraform vars |

**`.gitignore` fix:** the current `terraform.tfvars` rule ignores *all* tfvars.
Add `!infra/environments/**/terraform.tfvars` so the non-secret env tfvars are
committed while root/secret tfvars stay ignored. `db_password` is never committed.

## Workflows

### `.github/workflows/terraform-plan.yml`
- Trigger: `pull_request` on `main`, paths `infra/**`.
- Permissions: `id-token: write`, `contents: read`, `pull-requests: write`.
- Matrix over `[dev, production]`. Each leg: checkout → OIDC auth →
  `setup-terraform@v4` (1.15.6) → `init` → `validate` → `fmt -check` →
  `plan -out=tfplan` → comment plan on PR → fail job if plan failed.
- `db_password` supplied via `env: TF_VAR_db_password` from the environment secret.

### `.github/workflows/terraform-apply.yml`
- Trigger: `push` to `main`, paths `infra/**`.
- Permissions: `id-token: write`, `contents: read`.
- Job `apply-dev`: `environment: dev`, auto-applies `environments/dev`.
- Job `apply-production`: `needs: apply-dev`, `environment: production`
  (GitHub environment protection → **required reviewer** gates the run),
  applies `environments/production`.
- Concurrency group per workflow+ref with `cancel-in-progress: false` so applies
  are never cancelled mid-run.

## GitHub configuration (manual, documented steps)

1. Settings → Environments → create `dev` and `production`.
2. On `production`: enable **Required reviewers**, add reviewer.
3. Add `TF_VAR_DB_PASSWORD` secret to each environment.
4. Add repo variables `AWS_ROLE_ARN` (from bootstrap output) and `AWS_REGION`.

## Testing / validation

- `terraform validate` and `terraform fmt -check` run in the plan workflow.
- `terraform plan` on a PR proves auth, backend, and module wiring end-to-end
  without changing infrastructure.
- Local `terraform init` + `validate` in each environment dir before first PR.

## Follow-ups (noted, not in this change)

- Replace `AdministratorAccess` with a least-privilege policy scoped to the
  EC2 / RDS / ECR / IAM / S3 / CloudWatch / SNS actions the modules use.
- Optional: separate read-only plan role vs. apply role.
- Optional: Slack/email notification on apply (needs a webhook, not configured).
