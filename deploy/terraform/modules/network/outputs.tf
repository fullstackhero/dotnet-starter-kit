output "vpc_id" {
  description = "The ID of the VPC"
  value       = aws_vpc.this.id
}

output "vpc_arn" {
  description = "The ARN of the VPC"
  value       = aws_vpc.this.arn
}

output "vpc_cidr_block" {
  description = "The CIDR block of the VPC"
  value       = aws_vpc.this.cidr_block
}

output "public_subnet_ids" {
  description = "List of public subnet IDs"
  value       = [for s in aws_subnet.public : s.id]
}

output "private_subnet_ids" {
  description = "List of private subnet IDs"
  value       = [for s in aws_subnet.private : s.id]
}

output "public_subnets" {
  description = "Map of public subnet objects"
  value       = aws_subnet.public
}

output "private_subnets" {
  description = "Map of private subnet objects"
  value       = aws_subnet.private
}

output "nat_gateway_ids" {
  description = "List of NAT Gateway IDs"
  value       = [for nat in aws_nat_gateway.this : nat.id]
}

output "internet_gateway_id" {
  description = "The ID of the Internet Gateway"
  value       = aws_internet_gateway.this.id
}

output "public_route_table_id" {
  description = "The ID of the public route table"
  value       = aws_route_table.public.id
}

output "private_route_table_ids" {
  description = "Map of private route table IDs"
  value       = { for k, rt in aws_route_table.private : k => rt.id }
}
