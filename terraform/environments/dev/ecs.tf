module "ecs_fargate_webapi" {
  source = "../../modules/ecs"

  cluster_name    = "fullstackhero"
  service_name    = "webapi"
  container_name  = "fsh-webapi"
  container_image = "ghcr.io/fullstackhero/webapi:latest"
  container_port  = 8080
  subnets         = ["subnet-12345678", "subnet-87654321"]
  security_groups = ["sg-12345678"]

  environment = {
    ENV_VAR1 = "value1"
    ENV_VAR2 = "value2"
  }
}
