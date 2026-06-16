output "app_service_id" {
  value       = azurerm_linux_web_app.backend.id
  description = "ID of the App Service"
}

output "app_service_name" {
  value       = azurerm_linux_web_app.backend.name
  description = "Name of the App Service"
}

output "app_service_default_hostname" {
  value       = azurerm_linux_web_app.backend.default_hostname
  description = "Default hostname of the App Service"
}

output "app_service_identity_principal_id" {
  value       = azurerm_linux_web_app.backend.identity[0].principal_id
  description = "Principal ID of the App Service managed identity"
}

output "signalr_service_id" {
  value       = azurerm_signalr_service.main.id
  description = "ID of the SignalR Service"
}

output "signalr_primary_connection_string" {
  value       = azurerm_signalr_service.main.primary_connection_string
  description = "Primary connection string for SignalR"
  sensitive   = true
}
