output "cloud_run_service_url" {
  value       = google_cloud_run_v2_service.backend.uri
  description = "URL of the Cloud Run backend service"
}

output "cloud_run_service_name" {
  value       = google_cloud_run_v2_service.backend.name
  description = "Name of the Cloud Run backend service"
}
