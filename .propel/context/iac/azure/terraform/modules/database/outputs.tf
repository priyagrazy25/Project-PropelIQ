output "sql_server_id" {
  value       = azurerm_mssql_server.main.id
  description = "ID of the SQL Server"
}

output "sql_server_fqdn" {
  value       = azurerm_mssql_server.main.fully_qualified_domain_name
  description = "FQDN of the SQL Server"
}

output "sql_database_id" {
  value       = azurerm_mssql_database.main.id
  description = "ID of the SQL Database"
}

output "sql_database_name" {
  value       = azurerm_mssql_database.main.name
  description = "Name of the SQL Database"
}

output "sql_connection_string" {
  value       = "Server=tcp:${azurerm_mssql_server.main.fully_qualified_domain_name},1433;Database=${azurerm_mssql_database.main.name};Encrypt=True;TrustServerCertificate=False;"
  description = "Base SQL connection string (add authentication separately)"
  sensitive   = true
}
