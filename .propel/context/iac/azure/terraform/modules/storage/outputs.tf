output "storage_account_id" {
  value       = azurerm_storage_account.clinical.id
  description = "ID of the storage account"
}

output "storage_account_name" {
  value       = azurerm_storage_account.clinical.name
  description = "Name of the storage account"
}

output "clinical_docs_container_name" {
  value       = azurerm_storage_container.clinical_docs.name
  description = "Name of the clinical documents container"
}

output "primary_blob_endpoint" {
  value       = azurerm_storage_account.clinical.primary_blob_endpoint
  description = "Primary blob endpoint URL"
}

output "primary_access_key" {
  value       = azurerm_storage_account.clinical.primary_access_key
  description = "Primary access key for the storage account"
  sensitive   = true
}
