locals {
  common_tags = {
    Environment = var.environment
    Project     = var.project_name
    ManagedBy   = "terraform"
  }
}

resource "azurerm_resource_group" "main" {
  name     = "${var.project_name}-${var.environment}-rg"
  location = var.location
  tags     = local.common_tags
}

module "networking" {
  source = "../../modules/networking"

  project_name        = var.project_name
  environment         = var.environment
  location            = var.location
  resource_group_name = azurerm_resource_group.main.name
  tags                = local.common_tags
}

module "security" {
  source = "../../modules/security"

  project_name        = var.project_name
  environment         = var.environment
  location            = var.location
  resource_group_name = azurerm_resource_group.main.name
  tags                = local.common_tags

  depends_on = [module.networking]
}

# ENV-030: Full-scale compute for production
module "compute" {
  source = "../../modules/compute"

  project_name        = var.project_name
  environment         = var.environment
  location            = var.location
  resource_group_name = azurerm_resource_group.main.name
  app_subnet_id       = module.networking.app_subnet_id
  app_service_sku     = "S2"           # INFRA-001: Prod sizing
  enable_autoscale    = true            # INFRA-002: Auto-scaling enabled
  min_instance_count  = 2
  max_instance_count  = 5
  tags                = local.common_tags

  depends_on = [module.networking, module.security]
}

# ENV-030: Production database with HA
module "database" {
  source = "../../modules/database"

  project_name             = var.project_name
  environment              = var.environment
  location                 = var.location
  resource_group_name      = azurerm_resource_group.main.name
  data_subnet_id           = module.networking.data_subnet_id
  vnet_id                  = module.networking.vnet_id
  sql_sku                  = "S3"         # INFRA-030: Prod SKU
  sql_max_size_gb          = 10           # C-4: SQL Express limit awareness
  sql_admin_login          = var.sql_admin_login
  sql_admin_password       = var.sql_admin_password
  enable_zone_redundancy   = true         # INFRA-031: Zone redundant HA
  backup_retention_days    = 35           # INFRA-032: Max retention
  app_service_principal_id = module.compute.app_service_identity_principal_id
  tags                     = local.common_tags

  depends_on = [module.networking, module.security]
}

# INFRA-021: GRS for prod storage
module "storage" {
  source = "../../modules/storage"

  project_name        = var.project_name
  environment         = var.environment
  location            = var.location
  resource_group_name = azurerm_resource_group.main.name
  replication_type    = "GRS"
  data_subnet_id      = module.networking.data_subnet_id
  vnet_id             = module.networking.vnet_id
  tags                = local.common_tags

  depends_on = [module.networking, module.security]
}

# ENV-033: Full monitoring for prod
module "monitoring" {
  source = "../../modules/monitoring"

  project_name        = var.project_name
  environment         = var.environment
  location            = var.location
  resource_group_name = azurerm_resource_group.main.name
  app_service_id      = module.compute.app_service_id
  alert_email         = var.alert_email
  tags                = local.common_tags

  depends_on = [module.compute]
}
