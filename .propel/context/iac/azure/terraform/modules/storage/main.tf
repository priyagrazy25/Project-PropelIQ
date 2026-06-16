locals {
  name_prefix  = "${var.project_name}-${var.environment}"
  storage_name = replace("${var.project_name}${var.environment}st", "-", "")
}

# INFRA-020: Blob Storage for clinical documents with AES-256 encryption
resource "azurerm_storage_account" "clinical" {
  name                          = substr(local.storage_name, 0, 24)
  resource_group_name           = var.resource_group_name
  location                      = var.location
  account_tier                  = "Standard"
  account_replication_type      = var.replication_type # INFRA-021: GRS for prod
  min_tls_version               = "TLS1_2"            # SEC-011: TLS 1.2+
  public_network_access_enabled = false                # SEC-004: Private only
  tags                          = var.tags

  blob_properties {
    # SEC-032: Immutable storage for audit logs
    delete_retention_policy {
      days = 365
    }
    container_delete_retention_policy {
      days = 90
    }
  }
}

# INFRA-020: Container for clinical documents
resource "azurerm_storage_container" "clinical_docs" {
  name                  = "clinical-documents"
  storage_account_name  = azurerm_storage_account.clinical.name
  container_access_type = "private"
}

# INFRA-023: Container for database backups (write-only from app)
resource "azurerm_storage_container" "backups" {
  name                  = "database-backups"
  storage_account_name  = azurerm_storage_account.clinical.name
  container_access_type = "private"
}

# SEC-032: Container for immutable audit logs
resource "azurerm_storage_container" "audit_logs" {
  name                  = "audit-logs"
  storage_account_name  = azurerm_storage_account.clinical.name
  container_access_type = "private"
}

# INFRA-022: Lifecycle management — move old docs to cool tier
resource "azurerm_storage_management_policy" "lifecycle" {
  storage_account_id = azurerm_storage_account.clinical.id

  rule {
    name    = "move-to-cool"
    enabled = true

    filters {
      prefix_match = ["clinical-documents/"]
      blob_types   = ["blockBlob"]
    }

    actions {
      base_blob {
        tier_to_cool_after_days_since_modification_greater_than = 365
      }
    }
  }
}

# Private endpoint for Blob Storage
resource "azurerm_private_endpoint" "storage" {
  name                = "${local.name_prefix}-pe-storage"
  location            = var.location
  resource_group_name = var.resource_group_name
  subnet_id           = var.data_subnet_id
  tags                = var.tags

  private_service_connection {
    name                           = "${local.name_prefix}-psc-storage"
    private_connection_resource_id = azurerm_storage_account.clinical.id
    subresource_names              = ["blob"]
    is_manual_connection           = false
  }
}

resource "azurerm_private_dns_zone" "blob" {
  name                = "privatelink.blob.core.windows.net"
  resource_group_name = var.resource_group_name
  tags                = var.tags
}

resource "azurerm_private_dns_zone_virtual_network_link" "blob" {
  name                  = "${local.name_prefix}-dnslink-blob"
  resource_group_name   = var.resource_group_name
  private_dns_zone_name = azurerm_private_dns_zone.blob.name
  virtual_network_id    = var.vnet_id
}
