terraform {
  required_version = ">= 1.5.0"
  required_providers {
    google = {
      source  = "hashicorp/google"
      version = "~> 5.0"
    }
  }
}

locals {
  name_prefix = "${var.project_name}-${var.environment}"
}

# SEC-022: Secret Manager for secrets
resource "google_secret_manager_secret" "sql_password" {
  secret_id = "${local.name_prefix}-sql-password"
  project   = var.project_id

  replication {
    auto {}
  }
}

# SEC-020: Service account for the backend (Workload Identity)
resource "google_service_account" "backend" {
  account_id   = "${local.name_prefix}-backend-sa"
  display_name = "Backend Service Account - ${var.environment}"
  project      = var.project_id
}

# SEC-021: Minimal IAM bindings for backend service account
resource "google_project_iam_member" "backend_sql" {
  project = var.project_id
  role    = "roles/cloudsql.client"
  member  = "serviceAccount:${google_service_account.backend.email}"
}

resource "google_project_iam_member" "backend_storage" {
  project = var.project_id
  role    = "roles/storage.objectAdmin"
  member  = "serviceAccount:${google_service_account.backend.email}"
}

resource "google_project_iam_member" "backend_secrets" {
  project = var.project_id
  role    = "roles/secretmanager.secretAccessor"
  member  = "serviceAccount:${google_service_account.backend.email}"
}

variable "project_id" {
  type = string
}
variable "project_name" {
  type = string
}
variable "environment" {
  type = string
}

output "backend_service_account_email" {
  value = google_service_account.backend.email
}
