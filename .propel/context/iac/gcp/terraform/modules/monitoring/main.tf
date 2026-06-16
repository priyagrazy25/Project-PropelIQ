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

# OPS-001/OPS-002: Monitoring notification channel
resource "google_monitoring_notification_channel" "email" {
  count        = var.alert_email != "" ? 1 : 0
  display_name = "${local.name_prefix} Ops Email"
  project      = var.project_id
  type         = "email"

  labels = {
    email_address = var.alert_email
  }
}

# OPS-010: Alert when Cloud Run error rate exceeds 5%
resource "google_monitoring_alert_policy" "error_rate" {
  display_name = "${local.name_prefix} - High Error Rate"
  project      = var.project_id
  combiner     = "OR"

  conditions {
    display_name = "Cloud Run 5xx error rate > 5%"

    condition_threshold {
      filter          = "resource.type=\"cloud_run_revision\" AND metric.type=\"run.googleapis.com/request_count\" AND metric.labels.response_code_class=\"5xx\""
      duration        = "300s"
      comparison      = "COMPARISON_GT"
      threshold_value = 5

      aggregations {
        alignment_period   = "300s"
        per_series_aligner = "ALIGN_RATE"
      }
    }
  }

  notification_channels = var.alert_email != "" ? [google_monitoring_notification_channel.email[0].name] : []
}

# OPS-003: Uptime check for health endpoint
resource "google_monitoring_uptime_check_config" "health" {
  count        = var.health_check_url != "" ? 1 : 0
  display_name = "${local.name_prefix} Health Check"
  project      = var.project_id
  timeout      = "10s"
  period       = "60s"

  http_check {
    path         = "/health/live"
    port         = 443
    use_ssl      = true
    validate_ssl = true
  }

  monitored_resource {
    type = "uptime_url"
    labels = {
      project_id = var.project_id
      host       = var.health_check_url
    }
  }
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
variable "alert_email" {
  type    = string
  default = ""
}
variable "health_check_url" {
  type    = string
  default = ""
}
