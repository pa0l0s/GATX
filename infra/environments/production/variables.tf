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
