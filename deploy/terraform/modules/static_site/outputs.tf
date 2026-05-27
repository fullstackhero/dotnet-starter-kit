output "bucket_name" {
  description = "Name of the S3 origin bucket."
  value       = aws_s3_bucket.this.id
}

output "bucket_arn" {
  description = "ARN of the S3 origin bucket."
  value       = aws_s3_bucket.this.arn
}

output "bucket_regional_domain_name" {
  description = "Region-specific domain name of the origin bucket."
  value       = aws_s3_bucket.this.bucket_regional_domain_name
}

output "cloudfront_distribution_id" {
  description = "CloudFront distribution ID (use for cache invalidation in CI)."
  value       = aws_cloudfront_distribution.this.id
}

output "cloudfront_distribution_arn" {
  description = "CloudFront distribution ARN."
  value       = aws_cloudfront_distribution.this.arn
}

output "cloudfront_domain_name" {
  description = "CloudFront distribution domain name (e.g. d111111abcdef8.cloudfront.net)."
  value       = aws_cloudfront_distribution.this.domain_name
}

output "cloudfront_hosted_zone_id" {
  description = "CloudFront hosted zone ID (for Route53 alias records)."
  value       = aws_cloudfront_distribution.this.hosted_zone_id
}

output "url" {
  description = "Primary HTTPS URL for the site (first custom alias if set, else the CloudFront domain)."
  value       = length(var.aliases) > 0 ? "https://${var.aliases[0]}" : "https://${aws_cloudfront_distribution.this.domain_name}"
}
