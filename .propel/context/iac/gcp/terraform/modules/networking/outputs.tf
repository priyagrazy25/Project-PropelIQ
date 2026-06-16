output "vpc_id" {
  value       = google_compute_network.main.id
  description = "ID of the VPC network"
}

output "vpc_name" {
  value       = google_compute_network.main.name
  description = "Name of the VPC network"
}

output "app_subnet_id" {
  value       = google_compute_subnetwork.app.id
  description = "ID of the application subnet"
}

output "app_subnet_name" {
  value       = google_compute_subnetwork.app.name
  description = "Name of the application subnet"
}

output "data_subnet_id" {
  value       = google_compute_subnetwork.data.id
  description = "ID of the data subnet"
}
