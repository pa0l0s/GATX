terraform {
  required_version = ">= 1.8.0"

  required_providers {
    aws = {
      source  = "hashicorp/aws"
      version = "~> 5.80"
    }
  }
}

# Primary provider — app region (eu-central-1 by default)
provider "aws" {
  region = var.aws_region
}

# Billing metrics are ONLY published in us-east-1 — required alias for the billing module
provider "aws" {
  alias  = "us_east_1"
  region = "us-east-1"
}

# ── App module: EC2 + ECR ─────────────────────────────────────────────────────
module "app" {
  source = "./modules/app"

  app_name    = var.app_name
  aws_region  = var.aws_region
  db_host     = module.database.host
  db_username = var.db_username
  db_password = var.db_password
}

# ── Database module: RDS db.t3.micro (free tier) ──────────────────────────────
module "database" {
  source = "./modules/database"

  app_name    = var.app_name
  db_username = var.db_username
  db_password = var.db_password
}

# ── Billing alarm (in us-east-1) ──────────────────────────────────────────────
module "billing" {
  source = "./modules/billing"

  providers = {
    aws = aws.us_east_1
  }

  app_name      = var.app_name
  alert_email   = var.billing_alert_email
  threshold_usd = var.billing_threshold_usd
}
