locals {
  name_prefix = "${var.project_name}-${var.environment}"
}

# VPC Connector for Cloud Run to access private resources
resource "google_vpc_access_connector" "main" {
  name          = "${local.name_prefix}-vpc-conn"
  project       = var.project_id
  region        = var.region
  subnet {
    name = var.vpc_connector_subnet
  }
  min_instances = 2
  max_instances = 3
}

# INFRA-001: Cloud Run service for ASP.NET Core 8.0 backend
resource "google_cloud_run_v2_service" "backend" {
  name     = "${local.name_prefix}-backend"
  project  = var.project_id
  location = var.region
  ingress  = "INGRESS_TRAFFIC_INTERNAL_LOAD_BALANCER" # SEC: No direct public access

  template {
    scaling {
      min_instance_count = var.min_instance_count # INFRA-002: Auto-scaling
      max_instance_count = var.max_instance_count
    }

    vpc_access {
      connector = google_vpc_access_connector.main.id
      egress    = "ALL_TRAFFIC"
    }

    containers {
      image = var.container_image

      ports {
        container_port = 8080
      }

      resources {
        limits = {
          cpu    = var.cpu
          memory = var.memory
        }
      }

      # OPS-003: Health check via startup probe
      startup_probe {
        http_get {
          path = "/health/live"
        }
        initial_delay_seconds = 10
        period_seconds        = 3
        failure_threshold     = 10
      }

      # OPS-003: Liveness check
      liveness_probe {
        http_get {
          path = "/health/live"
        }
        period_seconds = 30
      }

      env {
        name  = "ASPNETCORE_ENVIRONMENT"
        value = var.environment == "prod" ? "Production" : "Development"
      }
    }
  }
}
