module "networking" {
  source = "../../modules/networking"

  project_id   = var.project_id
  project_name = var.project_name
  environment  = var.environment
  region       = var.region
}

module "security" {
  source = "../../modules/security"

  project_id   = var.project_id
  project_name = var.project_name
  environment  = var.environment
}

module "compute" {
  source = "../../modules/compute"

  project_id           = var.project_id
  project_name         = var.project_name
  environment          = var.environment
  region               = var.region
  vpc_connector_subnet = module.networking.app_subnet_name
  cpu                  = "1"
  memory               = "512Mi"
  min_instance_count   = 0
  max_instance_count   = 2

  depends_on = [module.networking, module.security]
}

module "database" {
  source = "../../modules/database"

  project_id        = var.project_id
  project_name      = var.project_name
  environment       = var.environment
  region            = var.region
  vpc_id            = module.networking.vpc_id
  database_tier     = "db-custom-1-3840"
  enable_ha         = false
  sql_root_password = var.sql_root_password

  depends_on = [module.networking]
}

module "storage" {
  source = "../../modules/storage"

  project_id   = var.project_id
  project_name = var.project_name
  environment  = var.environment
  region       = var.region

  depends_on = [module.security]
}

module "monitoring" {
  source = "../../modules/monitoring"

  project_id   = var.project_id
  project_name = var.project_name
  environment  = var.environment

  depends_on = [module.compute]
}
