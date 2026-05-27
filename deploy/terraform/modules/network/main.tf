################################################################################
# VPC
################################################################################

resource "aws_vpc" "this" {
  cidr_block           = var.cidr_block
  enable_dns_support   = true
  enable_dns_hostnames = true

  tags = merge(var.tags, {
    Name = "${var.name}-vpc"
  })
}

################################################################################
# Internet Gateway
################################################################################

resource "aws_internet_gateway" "this" {
  vpc_id = aws_vpc.this.id

  tags = merge(var.tags, {
    Name = "${var.name}-igw"
  })
}

################################################################################
# Public Subnets
################################################################################

resource "aws_subnet" "public" {
  for_each = var.public_subnets

  vpc_id                  = aws_vpc.this.id
  cidr_block              = each.value.cidr_block
  availability_zone       = each.value.az
  map_public_ip_on_launch = true

  tags = merge(var.tags, {
    Name = "${var.name}-public-${each.key}"
    Type = "public"
  })
}

################################################################################
# Private Subnets
################################################################################

resource "aws_subnet" "private" {
  for_each = var.private_subnets

  vpc_id            = aws_vpc.this.id
  cidr_block        = each.value.cidr_block
  availability_zone = each.value.az

  tags = merge(var.tags, {
    Name = "${var.name}-private-${each.key}"
    Type = "private"
  })
}

################################################################################
# NAT Gateways
################################################################################

resource "aws_eip" "nat" {
  for_each = var.enable_nat_gateway ? (var.single_nat_gateway ? { "single" = aws_subnet.public[keys(aws_subnet.public)[0]] } : aws_subnet.public) : {}

  domain = "vpc"

  tags = merge(var.tags, {
    Name = "${var.name}-nat-${each.key}"
  })

  depends_on = [aws_internet_gateway.this]
}

resource "aws_nat_gateway" "this" {
  for_each = var.enable_nat_gateway ? (var.single_nat_gateway ? { "single" = aws_subnet.public[keys(aws_subnet.public)[0]] } : aws_subnet.public) : {}

  allocation_id = aws_eip.nat[each.key].id
  subnet_id     = each.value.id

  tags = merge(var.tags, {
    Name = "${var.name}-nat-${each.key}"
  })

  depends_on = [aws_internet_gateway.this]
}

################################################################################
# Route Tables
################################################################################

resource "aws_route_table" "public" {
  vpc_id = aws_vpc.this.id

  tags = merge(var.tags, {
    Name = "${var.name}-public"
  })
}

resource "aws_route" "public_internet_gateway" {
  route_table_id         = aws_route_table.public.id
  destination_cidr_block = "0.0.0.0/0"
  gateway_id             = aws_internet_gateway.this.id
}

resource "aws_route_table_association" "public" {
  for_each       = aws_subnet.public
  subnet_id      = each.value.id
  route_table_id = aws_route_table.public.id
}

resource "aws_route_table" "private" {
  for_each = var.enable_nat_gateway ? (var.single_nat_gateway ? { "single" = null } : aws_subnet.private) : aws_subnet.private

  vpc_id = aws_vpc.this.id

  tags = merge(var.tags, {
    Name = "${var.name}-private-${each.key}"
  })
}

resource "aws_route" "private_nat_gateway" {
  for_each = var.enable_nat_gateway ? aws_route_table.private : {}

  route_table_id         = each.value.id
  destination_cidr_block = "0.0.0.0/0"
  nat_gateway_id         = var.single_nat_gateway ? aws_nat_gateway.this["single"].id : aws_nat_gateway.this[each.key].id
}

resource "aws_route_table_association" "private" {
  for_each       = aws_subnet.private
  subnet_id      = each.value.id
  route_table_id = var.single_nat_gateway && var.enable_nat_gateway ? aws_route_table.private["single"].id : aws_route_table.private[each.key].id
}

################################################################################
# VPC Endpoints (Cost Optimization)
################################################################################

resource "aws_vpc_endpoint" "s3" {
  count = var.enable_s3_endpoint ? 1 : 0

  vpc_id            = aws_vpc.this.id
  service_name      = "com.amazonaws.${data.aws_region.current.id}.s3"
  vpc_endpoint_type = "Gateway"
  route_table_ids   = concat([aws_route_table.public.id], [for rt in aws_route_table.private : rt.id])

  tags = merge(var.tags, {
    Name = "${var.name}-s3-endpoint"
  })
}

resource "aws_vpc_endpoint" "ecr_api" {
  count = var.enable_ecr_endpoints ? 1 : 0

  vpc_id              = aws_vpc.this.id
  service_name        = "com.amazonaws.${data.aws_region.current.id}.ecr.api"
  vpc_endpoint_type   = "Interface"
  subnet_ids          = [for s in aws_subnet.private : s.id]
  security_group_ids  = [aws_security_group.vpc_endpoints[0].id]
  private_dns_enabled = true

  tags = merge(var.tags, {
    Name = "${var.name}-ecr-api-endpoint"
  })
}

resource "aws_vpc_endpoint" "ecr_dkr" {
  count = var.enable_ecr_endpoints ? 1 : 0

  vpc_id              = aws_vpc.this.id
  service_name        = "com.amazonaws.${data.aws_region.current.id}.ecr.dkr"
  vpc_endpoint_type   = "Interface"
  subnet_ids          = [for s in aws_subnet.private : s.id]
  security_group_ids  = [aws_security_group.vpc_endpoints[0].id]
  private_dns_enabled = true

  tags = merge(var.tags, {
    Name = "${var.name}-ecr-dkr-endpoint"
  })
}

resource "aws_vpc_endpoint" "logs" {
  count = var.enable_logs_endpoint ? 1 : 0

  vpc_id              = aws_vpc.this.id
  service_name        = "com.amazonaws.${data.aws_region.current.id}.logs"
  vpc_endpoint_type   = "Interface"
  subnet_ids          = [for s in aws_subnet.private : s.id]
  security_group_ids  = [aws_security_group.vpc_endpoints[0].id]
  private_dns_enabled = true

  tags = merge(var.tags, {
    Name = "${var.name}-logs-endpoint"
  })
}

resource "aws_vpc_endpoint" "secretsmanager" {
  count = var.enable_secretsmanager_endpoint ? 1 : 0

  vpc_id              = aws_vpc.this.id
  service_name        = "com.amazonaws.${data.aws_region.current.id}.secretsmanager"
  vpc_endpoint_type   = "Interface"
  subnet_ids          = [for s in aws_subnet.private : s.id]
  security_group_ids  = [aws_security_group.vpc_endpoints[0].id]
  private_dns_enabled = true

  tags = merge(var.tags, {
    Name = "${var.name}-secretsmanager-endpoint"
  })
}

resource "aws_security_group" "vpc_endpoints" {
  count = var.enable_ecr_endpoints || var.enable_logs_endpoint || var.enable_secretsmanager_endpoint ? 1 : 0

  name        = "${var.name}-vpc-endpoints"
  description = "Security group for VPC endpoints"
  vpc_id      = aws_vpc.this.id

  ingress {
    description = "HTTPS from VPC"
    from_port   = 443
    to_port     = 443
    protocol    = "tcp"
    cidr_blocks = [aws_vpc.this.cidr_block]
  }

  egress {
    description = "HTTPS to VPC"
    from_port   = 443
    to_port     = 443
    protocol    = "tcp"
    cidr_blocks = [aws_vpc.this.cidr_block]
  }

  tags = merge(var.tags, {
    Name = "${var.name}-vpc-endpoints-sg"
  })
}

################################################################################
# Default Security Group - Deny All (AWS Best Practice)
################################################################################

resource "aws_default_security_group" "this" {
  vpc_id = aws_vpc.this.id

  # No ingress or egress rules = deny all traffic on the default SG
  tags = merge(var.tags, {
    Name = "${var.name}-default-sg-DO-NOT-USE"
  })
}

################################################################################
# Flow Logs (Optional)
################################################################################

resource "aws_flow_log" "this" {
  count = var.enable_flow_logs ? 1 : 0

  vpc_id                   = aws_vpc.this.id
  traffic_type             = "ALL"
  iam_role_arn             = aws_iam_role.flow_logs[0].arn
  log_destination_type     = "cloud-watch-logs"
  log_destination          = aws_cloudwatch_log_group.flow_logs[0].arn
  max_aggregation_interval = 60

  tags = merge(var.tags, {
    Name = "${var.name}-flow-logs"
  })
}

resource "aws_cloudwatch_log_group" "flow_logs" {
  count = var.enable_flow_logs ? 1 : 0

  name              = "/aws/vpc/${var.name}/flow-logs"
  retention_in_days = var.flow_logs_retention_days
  kms_key_id        = var.kms_key_id

  tags = var.tags
}

resource "aws_iam_role" "flow_logs" {
  count = var.enable_flow_logs ? 1 : 0

  name = "${var.name}-flow-logs-role"

  assume_role_policy = jsonencode({
    Version = "2012-10-17"
    Statement = [{
      Action = "sts:AssumeRole"
      Effect = "Allow"
      Principal = {
        Service = "vpc-flow-logs.amazonaws.com"
      }
    }]
  })

  tags = var.tags
}

resource "aws_iam_role_policy" "flow_logs" {
  count = var.enable_flow_logs ? 1 : 0

  name = "${var.name}-flow-logs-policy"
  role = aws_iam_role.flow_logs[0].id

  policy = jsonencode({
    Version = "2012-10-17"
    Statement = [{
      Action = [
        "logs:CreateLogStream",
        "logs:PutLogEvents",
        "logs:DescribeLogStreams"
      ]
      Effect = "Allow"
      Resource = [
        aws_cloudwatch_log_group.flow_logs[0].arn,
        "${aws_cloudwatch_log_group.flow_logs[0].arn}:*"
      ]
    }]
  })
}

################################################################################
# Data Sources
################################################################################

data "aws_region" "current" {}
