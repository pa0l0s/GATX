output "database_endpoint" {
  description = "RDS endpoint (host:port)."
  value       = module.database.endpoint
}

output "app_url" {
  description = "Open once the EC2 instance finishes startup (~3 min)."
  value       = "http://${module.app.ec2_public_ip}"
}

output "ecr_api" {
  description = "API image repository URL."
  value       = module.app.api_repository_url
}

output "ecr_frontend" {
  description = "Frontend image repository URL."
  value       = module.app.frontend_repository_url
}

output "billing_sns_arn" {
  description = "SNS topic ARN for billing alerts."
  value       = module.billing.sns_topic_arn
}
