output "database_endpoint" {
  description = "RDS endpoint (host:port)."
  value       = module.database.endpoint
}

output "app_url" {
  description = "Open this in your browser once the EC2 instance finishes its startup (~3 min)."
  value       = "http://${module.app.ec2_public_ip}"
}

output "ecr_api" {
  description = "Push your API image here."
  value       = module.app.api_repository_url
}

output "ecr_frontend" {
  description = "Push your frontend image here."
  value       = module.app.frontend_repository_url
}

output "billing_sns_arn" {
  description = "SNS topic ARN for billing alerts."
  value       = module.billing.sns_topic_arn
}
