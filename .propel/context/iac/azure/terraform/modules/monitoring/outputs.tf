output "log_analytics_workspace_id" {
  value       = azurerm_log_analytics_workspace.main.id
  description = "ID of the Log Analytics Workspace"
}

output "application_insights_id" {
  value       = azurerm_application_insights.main.id
  description = "ID of Application Insights"
}

output "application_insights_connection_string" {
  value       = azurerm_application_insights.main.connection_string
  description = "Connection string for Application Insights"
  sensitive   = true
}

output "application_insights_instrumentation_key" {
  value       = azurerm_application_insights.main.instrumentation_key
  description = "Instrumentation key for Application Insights"
  sensitive   = true
}
