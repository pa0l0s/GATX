variable "app_name" {
  type = string
}

variable "alert_email" {
  description = "Email address to receive billing alerts"
  type        = string
}

variable "threshold_usd" {
  description = "Alert when estimated charges exceed this amount in USD"
  type        = number
  default     = 1
}
