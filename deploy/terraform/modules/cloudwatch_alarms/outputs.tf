output "sns_topic_arn" {
  description = "The ARN of the SNS topic for alarm notifications"
  value       = aws_sns_topic.alarms.arn
}

output "sns_topic_name" {
  description = "The name of the SNS topic for alarm notifications"
  value       = aws_sns_topic.alarms.name
}
