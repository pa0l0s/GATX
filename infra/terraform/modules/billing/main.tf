# ── Billing alarm (MUST live in us-east-1 — that's where AWS publishes the metric) ──
#
# PREREQUISITE: In AWS Console → Account → Billing and Cost Management →
#   Billing Preferences → enable "Receive Billing Alerts" BEFORE running terraform apply.
#   (This one-time switch cannot be set via API/Terraform.)

resource "aws_sns_topic" "billing" {
  # SNS topic must also be in us-east-1 to receive billing alarm notifications
  name = "${var.app_name}-billing-alerts"
}

resource "aws_sns_topic_subscription" "email" {
  topic_arn = aws_sns_topic.billing.arn
  protocol  = "email"
  endpoint  = var.alert_email
  # After `terraform apply` you'll receive a confirmation email — click Confirm Subscription.
}

resource "aws_cloudwatch_metric_alarm" "billing" {
  alarm_name          = "${var.app_name}-billing-alert"
  alarm_description   = "AWS estimated charges exceeded $${var.threshold_usd}"
  namespace           = "AWS/Billing"
  metric_name         = "EstimatedCharges"
  statistic           = "Maximum"
  period              = 86400  # checked once per day
  threshold           = var.threshold_usd
  comparison_operator = "GreaterThanThreshold"
  evaluation_periods  = 1
  treat_missing_data  = "notBreaching"
  alarm_actions       = [aws_sns_topic.billing.arn]

  dimensions = {
    Currency = "USD"
  }

  tags = { Name = "${var.app_name}-billing-alarm" }
}

output "sns_topic_arn" {
  value = aws_sns_topic.billing.arn
}
