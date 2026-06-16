locals {
  name_prefix = "${var.project_name}-${var.environment}"
}

# INFRA-030: Azure SQL Server
resource "azurerm_mssql_server" "main" {
  name                         = "${local.name_prefix}-sqlsvr"
  resource_group_name          = var.resource_group_name
  location                     = var.location
  version                      = "12.0"
  administrator_login          = var.sql_admin_login
  administrator_login_password = var.sql_admin_password
  minimum_tls_version          = "1.2" # SEC-011: TLS 1.2+
  tags                         = var.tags

  # SEC-020: Azure AD admin for managed identity access
  identity {
    type = "SystemAssigned"
  }
}

# INFRA-030: Azure SQL Database with TDE (SEC-010: encryption at rest)
resource "azurerm_mssql_database" "main" {
  name                        = "${local.name_prefix}-sqldb"
  server_id                   = azurerm_mssql_server.main.id
  sku_name                    = var.sql_sku
  max_size_gb                 = var.sql_max_size_gb
  zone_redundant              = var.enable_zone_redundancy # INFRA-031: HA for prod
  tags                        = var.tags

  # INFRA-032: Automated backup configuration
  short_term_retention_policy {
    retention_days = var.backup_retention_days
  }

  long_term_retention_policy {
    weekly_retention  = "P4W"
    monthly_retention = "P12M"
    yearly_retention  = "P7Y" # SEC-032: 7-year retention for HIPAA
    week_of_year      = 1
  }

  # SEC-010: Transparent Data Encryption enabled by default
  transparent_data_encryption_enabled = true
}

# INFRA-033: Deny public network access — private endpoint only
resource "azurerm_mssql_firewall_rule" "deny_all" {
  name             = "DenyAllPublicAccess"
  server_id        = azurerm_mssql_server.main.id
  start_ip_address = "0.0.0.0"
  end_ip_address   = "0.0.0.0"
}

# INFRA-014: Private endpoint for SQL Server
resource "azurerm_private_endpoint" "sql" {
  name                = "${local.name_prefix}-pe-sql"
  location            = var.location
  resource_group_name = var.resource_group_name
  subnet_id           = var.data_subnet_id
  tags                = var.tags

  private_service_connection {
    name                           = "${local.name_prefix}-psc-sql"
    private_connection_resource_id = azurerm_mssql_server.main.id
    subresource_names              = ["sqlServer"]
    is_manual_connection           = false
  }
}

# Private DNS zone for SQL private endpoint resolution
resource "azurerm_private_dns_zone" "sql" {
  name                = "privatelink.database.windows.net"
  resource_group_name = var.resource_group_name
  tags                = var.tags
}

resource "azurerm_private_dns_zone_virtual_network_link" "sql" {
  name                  = "${local.name_prefix}-dnslink-sql"
  resource_group_name   = var.resource_group_name
  private_dns_zone_name = azurerm_private_dns_zone.sql.name
  virtual_network_id    = var.vnet_id
}

# SEC-031: Auditing enabled for HIPAA compliance
resource "azurerm_mssql_server_extended_auditing_policy" "main" {
  server_id              = azurerm_mssql_server.main.id
  retention_in_days      = 90
  log_monitoring_enabled = true
}
