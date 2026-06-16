output "sql_instance_name" {
  value       = google_sql_database_instance.main.name
  description = "Name of the Cloud SQL instance"
}

output "sql_connection_name" {
  value       = google_sql_database_instance.main.connection_name
  description = "Connection name for Cloud SQL"
}

output "sql_private_ip" {
  value       = google_sql_database_instance.main.private_ip_address
  description = "Private IP address of the Cloud SQL instance"
}

output "database_name" {
  value       = google_sql_database.main.name
  description = "Name of the database"
}
