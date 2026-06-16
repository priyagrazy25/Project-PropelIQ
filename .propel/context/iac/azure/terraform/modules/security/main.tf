locals {
  name_prefix = "${var.project_name}-${var.environment}"
}

# SEC-022: Azure Key Vault for secrets management
resource "azurerm_key_vault" "main" {
  name                        = "${local.name_prefix}-kv"
  location                    = var.location
  resource_group_name         = var.resource_group_name
  tenant_id                   = data.azurerm_client_config.current.tenant_id
  sku_name                    = "standard"
  purge_protection_enabled    = true
  soft_delete_retention_days  = 90
  enable_rbac_authorization   = true # SEC-021: RBAC for Key Vault
  tags                        = var.tags

  network_acls {
    default_action = "Deny"
    bypass         = "AzureServices"
  }
}

data "azurerm_client_config" "current" {}

# SEC-020: Grant App Service managed identity access to Key Vault secrets
resource "azurerm_role_assignment" "app_kv_reader" {
  count                = var.app_service_principal_id != "" ? 1 : 0
  scope                = azurerm_key_vault.main.id
  role_definition_name = "Key Vault Secrets User"
  principal_id         = var.app_service_principal_id
}
