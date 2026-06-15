variable "aws_region" {
  description = "AWS region for the demo. Pick one region and destroy resources after learning."
  type        = string
  default     = "eu-central-1"
}

variable "app_name" {
  description = "Name prefix for demo resources."
  type        = string
  default     = "gatx-demo"
}

variable "db_username" {
  description = "Demo database username."
  type        = string
  default     = "gatx"
}

variable "db_password" {
  description = "Demo database password. Use TF_VAR_db_password locally; do not commit real secrets."
  type        = string
  sensitive   = true
}

variable "billing_alert_email" {
  description = "Email address to receive billing alerts when charges exceed the threshold."
  type        = string
}

variable "billing_threshold_usd" {
  description = "Send billing alert when estimated charges exceed this amount (USD). Default $1."
  type        = number
  default     = 1
}
