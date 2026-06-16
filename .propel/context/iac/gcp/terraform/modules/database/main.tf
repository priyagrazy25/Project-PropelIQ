locals {
  name_prefix = "${var.project_name}-${var.environment}"
}

# Private service networking for Cloud SQL
resource "google_compute_global_address" "sql_private" {
  name          = "${local.name_prefix}-sql-private-ip"
  project       = var.project_id
  purpose       = "VPC_PEERING"
  address_type  = "INTERNAL"
  prefix_length = 24
  network       = var.vpc_id
}

resource "google_service_networking_connection" "sql" {
  network                 = var.vpc_id
  service                 = "servicenetworking.googleapis.com"
  reserved_peering_ranges = [google_compute_global_address.sql_private.name]
}

# INFRA-030: Cloud SQL for SQL Server
resource "google_sql_database_instance" "main" {
  name             = "${local.name_prefix}-sql"
  project          = var.project_id
  region           = var.region
  database_version = var.database_version
  root_password    = var.sql_root_password

  settings {
    tier              = var.database_tier
    availability_type = var.enable_ha ? "REGIONAL" : "ZONAL" # INFRA-031: HA for prod

    ip_configuration {
      ipv4_enabled    = false    # INFRA-033: No public IP — private only
      private_network = var.vpc_id
      require_ssl     = true     # SEC-011: TLS enforced
    }

    backup_configuration {
      enabled                        = var.backup_enabled # INFRA-032
      start_time                     = "02:00"
      point_in_time_recovery_enabled = var.enable_ha
      transaction_log_retention_days = var.enable_ha ? 7 : 1
      backup_retention_settings {
        retained_backups = var.enable_ha ? 35 : 7  # INFRA-032
      }
    }

    database_flags {
      name  = "contained database authentication"
      value = "on"
    }
  }

  deletion_protection = var.environment == "prod" ? true : false

  depends_on = [google_service_networking_connection.sql]
}

# Application database
resource "google_sql_database" "main" {
  name     = "${local.name_prefix}-db"
  project  = var.project_id
  instance = google_sql_database_instance.main.name
}
