resource "aws_security_group" "rds_sg" {
  vpc_id = aws_vpc.project_ecs.id
  ingress {
    protocol    = "tcp"
    from_port   = 5432
    to_port     = 5432
    cidr_blocks = [var.cidr]
  }
}

resource "aws_db_instance" "postgres" {
  allocated_storage      = 10
  db_name                = var.db_name
  engine                 = "postgres"
  engine_version         = "14.6"
  instance_class         = "db.t3.micro"
  username               = var.pg_username
  password               = var.pg_password
  identifier             = var.db_name
  skip_final_snapshot    = true
  multi_az               = false
  vpc_security_group_ids = [aws_security_group.rds_sg.id]
  db_subnet_group_name   = aws_db_subnet_group.default.name
}
