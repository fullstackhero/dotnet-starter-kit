// AWS Configuration
aws_region   = "us-east-1"
aws_region_a = "us-east-1a"
aws_region_b = "us-east-1b"

// Default Project Tags
owner        = "Mukesh Murugan"
project_name = "fullstackhero-dotnet-starter-kit"

// RDS PostgreSQL Configuration
pg_password = "posgresqladmin"
pg_username = "posgresqladmin"
db_name     = "fullstackhero"

ecs_cluster_name = "fullstackhero"

api_container_cpu    = 512
api_container_memory = 1024
api_image_name       = "ghcr.io/fullstackhero/webapi:latest"
api_service_name     = "webapi"
