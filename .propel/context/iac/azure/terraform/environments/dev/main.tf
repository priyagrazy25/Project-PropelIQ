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

# ENV-001: Minimal compute sizing for dev
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

# ENV-001: B1 App Service for dev
module "compute" {
  source = "../../modules/compute"

  project_name        = var.project_name
  environment         = var.environment
  location            = var.location
  resource_group_name = azurerm_resource_group.main.name
  app_subnet_id       = module.networking.app_subnet_id
  app_service_sku     = "B1"
  enable_autoscale    = false
  tags                = local.common_tags

  depends_on = [module.networking, module.security]
}

# ENV-004: SQL Basic for dev
module "database" {
  source = "../../modules/database"

  project_name             = var.project_name
  environment              = var.environment
  location                 = var.location
  resource_group_name      = azurerm_resource_group.main.name
  data_subnet_id           = module.networking.data_subnet_id
  vnet_id                  = module.networking.vnet_id
  sql_sku                  = "Basic"
  sql_max_size_gb          = 2
  sql_admin_login          = var.sql_admin_login
  sql_admin_password       = var.sql_admin_password
  enable_zone_redundancy   = false
  backup_retention_days    = 7
  app_service_principal_id = module.compute.app_service_identity_principal_id
  tags                     = local.common_tags

  depends_on = [module.networking, module.security]
}

module "storage" {
  source = "../../modules/storage"

  project_name        = var.project_name
  environment         = var.environment
  location            = var.location
  resource_group_name = azurerm_resource_group.main.name
  replication_type    = "LRS"
  data_subnet_id      = module.networking.data_subnet_id
  vnet_id             = module.networking.vnet_id
  tags                = local.common_tags

  depends_on = [module.networking, module.security]
}

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
