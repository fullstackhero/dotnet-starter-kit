module "rds" {
  environment   = var.environment
  source        = "../../modules/rds"
  vpc_id        = module.vpc.vpc_id
  subnet_ids    = [module.vpc.private_a_id, module.vpc.private_b_id]
  multi_az      = false
  database_name = "fsh"
  cidr_block    = module.vpc.cidr_block
}
