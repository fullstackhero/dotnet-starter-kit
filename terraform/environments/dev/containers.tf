resource "aws_ecs_cluster" "cluster" {
  name = var.ecs_cluster_name
}

resource "aws_ecs_cluster_capacity_providers" "cluster" {
  cluster_name       = aws_ecs_cluster.cluster.name
  capacity_providers = ["FARGATE"]
  default_capacity_provider_strategy {
    base              = 1
    weight            = 100
    capacity_provider = "FARGATE"
  }
}

resource "aws_ecs_service" "api_ecs_service" {
  name            = var.api_service_name
  cluster         = aws_ecs_cluster.cluster.id
  task_definition = aws_ecs_task_definition.api_ecs_task.arn
  launch_type     = "FARGATE"
  desired_count   = 1
  load_balancer {
    target_group_arn = aws_lb_target_group.fsh_api_tg.arn
    container_name   = var.api_service_name
    container_port   = 80
  }
  network_configuration {
    subnets          = [aws_subnet.private_east_a.id, aws_subnet.private_east_b.id]
    security_groups  = [aws_security_group.lb.id]
    assign_public_ip = true
  }
}

resource "aws_ecs_task_definition" "api_ecs_task" {
  family                   = var.api_service_name
  network_mode             = "awsvpc"
  requires_compatibilities = ["FARGATE"]
  cpu                      = var.api_container_cpu
  memory                   = var.api_container_memory
  task_role_arn            = aws_iam_role.ecs_task_role.arn
  execution_role_arn       = aws_iam_role.ecs_task_execution_role.arn
  container_definitions = jsonencode([
    {
      name : var.api_service_name
      image : var.api_image_name
      cpu : var.api_container_cpu
      memory : var.api_container_memory
      essential : true
      environment : [
        { "name" : "ASPNETCORE_ENVIRONMENT", "value" : "Development" },
        { "name" : "ASPNETCORE_HTTP_PORTS", "value" : "80" },
        { "name" : "DatabaseOptions__ConnectionString", "value" : "Host=${aws_db_instance.postgres.endpoint};Port=5432;Database=fshdb;Username=${var.pg_username};Password=${var.pg_password};Include Error Detail=true" },
        { "name" : "DatabaseOptions__Provider", "value" : "postgresql" },
        { "name" : "JwtOptions__Key", "value" : "QsJbczCNysv/5SGh+U7sxedX8C07TPQPBdsnSDKZ/aE=" },
        { "name" : "HangfireOptions__Username", "value" : "admin" },
        { "name" : "HangfireOptions__Password", "value" : "Secure1234!Me" },
        { "name" : "MailOptions__From", "value" : "mukesh@fullstackhero.net" },
        { "name" : "MailOptions__Host", "value" : "smtp.ethereal.email" },
        { "name" : "MailOptions__Port", "value" : "587" },
        { "name" : "MailOptions__UserName", "value" : "sherman.oconnell47@ethereal.email" },
        { "name" : "MailOptions__Password", "value" : "KbuTCFv4J6Fy7256vh" },
        { "name" : "MailOptions__DisplayName", "value" : "Mukesh Murugan" }
      ]
      logConfiguration : {
        "logDriver" : "awslogs",
        "options" : {
          "awslogs-region" : var.aws_region,
          "awslogs-group" : "fullstackhero/webapi",
          "awslogs-stream-prefix" : "webapi"
        }
      },
      portMappings : [
        {
          "containerPort" : 80,
          "hostPort" : 80
        }
      ]
    }
  ])
}

resource "aws_security_group" "lb" {
  name   = "security-group"
  vpc_id = aws_vpc.project_ecs.id
  ingress {
    protocol    = "tcp"
    from_port   = 80
    to_port     = 80
    cidr_blocks = ["0.0.0.0/0"]
  }

  egress {
    from_port   = 0
    to_port     = 0
    protocol    = "-1"
    cidr_blocks = ["0.0.0.0/0"]
  }
}

resource "aws_internet_gateway" "api" {
  vpc_id = aws_vpc.project_ecs.id
}

resource "aws_route" "internet_access" {
  route_table_id         = aws_vpc.project_ecs.main_route_table_id
  destination_cidr_block = "0.0.0.0/0"
  gateway_id             = aws_internet_gateway.api.id
}

resource "aws_cloudwatch_log_group" "api_log_group" {
  name              = "fullstackhero/webapi"
  retention_in_days = 5
}
