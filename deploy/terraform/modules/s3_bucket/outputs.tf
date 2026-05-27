output "bucket_name" {
  description = "The name of the S3 bucket"
  value       = aws_s3_bucket.this.id
}

output "bucket_arn" {
  description = "The ARN of the S3 bucket"
  value       = aws_s3_bucket.this.arn
}

output "bucket_domain_name" {
  description = "The bucket domain name"
  value       = aws_s3_bucket.this.bucket_domain_name
}

output "bucket_regional_domain_name" {
  description = "The bucket region-specific domain name"
  value       = aws_s3_bucket.this.bucket_regional_domain_name
}

output "cloudfront_domain_name" {
  description = "CloudFront domain for public access (when enabled)"
  value       = var.enable_cloudfront ? aws_cloudfront_distribution.this[0].domain_name : ""
}

output "cloudfront_distribution_id" {
  description = "CloudFront distribution ID (when enabled)"
  value       = var.enable_cloudfront ? aws_cloudfront_distribution.this[0].id : ""
}

output "cloudfront_distribution_arn" {
  description = "CloudFront distribution ARN (when enabled)"
  value       = var.enable_cloudfront ? aws_cloudfront_distribution.this[0].arn : ""
}
