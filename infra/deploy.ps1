#!/usr/bin/env pwsh
# deploy.ps1 — End-to-end GATX deployment to AWS (free tier)
#
# Prerequisites:
#   - AWS CLI installed and configured (aws configure)
#   - Terraform >= 1.8 installed
#   - Docker Desktop running
#   - terraform.tfvars present in infra/terraform/ (copy from terraform.tfvars.example)
#
# Run from the repo root:  .\infra\deploy.ps1

param(
  [string]$Region = "eu-central-1",
  [string]$AppName = "gatx-demo",
  [switch]$DestroyAll   # run with -DestroyAll to tear everything down
)

$ErrorActionPreference = "Stop"
$RepoRoot = Split-Path $PSScriptRoot -Parent
$TerraformDir = Join-Path $PSScriptRoot "terraform"

Write-Host ""
Write-Host "=== GATX AWS Deployment ===" -ForegroundColor Cyan
Write-Host ""

# ── Step 1: Terraform init + apply ────────────────────────────────────────────
Write-Host "Step 1/4: Provisioning AWS infrastructure with Terraform..." -ForegroundColor Yellow
Push-Location $TerraformDir

terraform init -upgrade
if ($DestroyAll) {
  terraform destroy -auto-approve
  Write-Host "All resources destroyed." -ForegroundColor Green
  Pop-Location
  exit 0
}
terraform apply -auto-approve

# Capture outputs
$ECR_API      = (terraform output -raw ecr_api)
$ECR_FRONTEND = (terraform output -raw ecr_frontend)
$APP_URL      = (terraform output -raw app_url)
$AWS_ACCOUNT  = ($ECR_API -split "\.")[0]
$ECR_REGISTRY = "$AWS_ACCOUNT.dkr.ecr.$Region.amazonaws.com"

Pop-Location
Write-Host "  Infrastructure ready." -ForegroundColor Green

# ── Step 2: ECR login ─────────────────────────────────────────────────────────
Write-Host ""
Write-Host "Step 2/4: Authenticating Docker to ECR..." -ForegroundColor Yellow
aws ecr get-login-password --region $Region |
  docker login --username AWS --password-stdin $ECR_REGISTRY
Write-Host "  Docker logged in to ECR." -ForegroundColor Green

# ── Step 3: Build and push images ─────────────────────────────────────────────
Write-Host ""
Write-Host "Step 3/4: Building and pushing Docker images..." -ForegroundColor Yellow
Push-Location $RepoRoot

Write-Host "  Building API image..."
docker build -f backend/src/Gatx.WebApi/Dockerfile -t "${ECR_API}:latest" .
docker push "${ECR_API}:latest"

Write-Host "  Building frontend image..."
docker build -f frontend/apps/assembly-manager/Dockerfile -t "${ECR_FRONTEND}:latest" .
docker push "${ECR_FRONTEND}:latest"

Pop-Location
Write-Host "  Images pushed to ECR." -ForegroundColor Green

# ── Step 4: Done ──────────────────────────────────────────────────────────────
Write-Host ""
Write-Host "Step 4/4: EC2 instance is pulling images and starting the app..." -ForegroundColor Yellow
Write-Host "  (This takes ~3 minutes on first boot)" -ForegroundColor Gray
Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  App URL : $APP_URL" -ForegroundColor Green
Write-Host "  Health  : $APP_URL/health"   # API health, proxied by nginx
Write-Host "  Swagger : $APP_URL/swagger"
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "BILLING REMINDER: Run '.\infra\deploy.ps1 -DestroyAll' when done to avoid any charges." -ForegroundColor Magenta
