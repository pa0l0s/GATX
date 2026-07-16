output "role_arn" {
  description = "Set this as the GitHub Actions variable AWS_ROLE_ARN."
  value       = aws_iam_role.github_actions_terraform.arn
}

output "state_bucket" {
  description = "Set this as the GitHub Actions variable TF_STATE_BUCKET."
  value       = aws_s3_bucket.state.bucket
}
