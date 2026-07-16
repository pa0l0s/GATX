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
