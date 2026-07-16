variable "app_name" {
  type = string
}

variable "aws_region" {
  type = string
}

variable "db_host" {
  description = "RDS endpoint (host only, without port)"
  type        = string
}

variable "db_username" {
  type = string
}

variable "db_password" {
  type      = string
  sensitive = true
}

variable "jwt_secret" {
  description = "Secret used to sign JWT access tokens."
  type        = string
  sensitive   = true
}
