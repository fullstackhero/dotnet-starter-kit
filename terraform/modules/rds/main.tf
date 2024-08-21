resource "aws_db_subnet_group" "this" {
  name       = "${var.environment}-rds-dsg"
  subnet_ids = var.subnet_ids
}

resource "aws_security_group" "this" {
  vpc_id = var.vpc_id
  name   = "${var.environment}-rds-sg"
  ingress {
    from_port   = 5432
    to_port     = 5432
    protocol    = "tcp"
    cidr_blocks = [var.cidr_block]
  }
}

resource "aws_db_instance" "this" {
  identifier                 = "${var.database_name}-${var.environment}"
  allocated_storage          = var.allocated_storage
  engine                     = "postgres"
  engine_version             = "16.4"
  instance_class             = var.instance_class
  multi_az                   = var.multi_az
  db_name                    = var.database_name
  username                   = var.database_username
  password                   = var.database_password
  db_subnet_group_name       = aws_db_subnet_group.this.name
  vpc_security_group_ids     = [aws_security_group.this.id]
  skip_final_snapshot        = true
  storage_encrypted          = true
  backup_retention_period    = var.backup_retention_period
  auto_minor_version_upgrade = true
}
