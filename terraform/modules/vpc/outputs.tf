output "vpc_id" {
  value = aws_vpc.this.id
}

output "private_a_id" {
  value = aws_subnet.private_a.id
}

output "private_b_id" {
  value = aws_subnet.private_b.id
}

output "cidr_block" {
  value = var.cidr_block
}
