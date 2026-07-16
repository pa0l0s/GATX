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
