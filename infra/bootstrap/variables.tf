variable "aws_region" {
  description = "AWS region for bootstrap resources (state bucket, IAM is global)."
  type        = string
  default     = "eu-central-1"
}
