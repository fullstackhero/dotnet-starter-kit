module "cluster" {
  source       = "../../modules/ecs/cluster"
  cluster_name = "fullstackhero"
}

module "webapi" {
  source          = "../../modules/ecs"
  vpc_id          = module.vpc.vpc_id
  environment     = var.environment
  cluster_id      = module.cluster.id
  service_name    = "webapi"
  container_name  = "fsh-webapi"
  container_image = "ghcr.io/fullstackhero/webapi:latest"
  subnet_ids      = [module.vpc.private_a_id, module.vpc.private_b_id]
  environment_variables = {
    DatabaseOptions__ConnectionString = module.rds.connection_string
    DatabaseOptions__Provider         = "postgresql"
    Serilog__MinimumLevel__Default    = "Error"
    OriginOptions__OriginUrl          = "http://${module.webapi.endpoint}:8080"
  }
}
