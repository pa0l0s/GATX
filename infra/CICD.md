# GATX Infrastructure CI/CD

AWS infrastructure is provisioned by GitHub Actions with Terraform.

- **Plan on PR** (`.github/workflows/terraform-plan.yml`): any PR touching
  `infra/environments/**` or `infra/modules/**` runs `plan` for both `dev` and
  `production` and comments the output on the PR.
- **Apply on merge** (`.github/workflows/terraform-apply.yml`): merging to `main`
  (touching `infra/**`) applies `dev` automatically. **`production` is applied
  manually** via *Run workflow* (`workflow_dispatch`) with `environment: production`,
  behind a required-reviewer approval — it is never applied on push.

  > **Free-plan account limit:** this AWS account allows only one RDS instance at a
  > time, which `dev` already uses. Running `production` automatically fails with
  > `InstanceQuotaExceeded`, so it is manual-only. Free the dev slot first
  > (`terraform destroy` in `infra/environments/dev`) before dispatching a prod apply,
  > or use a paid account to run both at once.

Auth is GitHub OIDC — no AWS keys are stored in GitHub.

## One-time bootstrap (operator, local, admin AWS creds)

The CI role and state bucket must exist before any workflow can run.

```bash
cd infra/bootstrap
terraform init
terraform apply            # creates OIDC provider, GitHubActionsTerraform role, state bucket
terraform output           # note role_arn and state_bucket
```

> If the account already has a GitHub OIDC provider for `token.actions.githubusercontent.com`,
> `terraform apply` fails with EntityAlreadyExists. Import it first:
> `terraform import aws_iam_openid_connect_provider.github arn:aws:iam::<ACCOUNT_ID>:oidc-provider/token.actions.githubusercontent.com`.

## One-time GitHub configuration

**Settings → Secrets and variables → Actions → Variables (repo):**
- `AWS_ROLE_ARN` = bootstrap output `role_arn`
- `AWS_REGION` = `eu-central-1`
- `TF_STATE_BUCKET` = bootstrap output `state_bucket`
- `TF_VAR_BILLING_ALERT_EMAIL` = the email address for billing alerts

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
