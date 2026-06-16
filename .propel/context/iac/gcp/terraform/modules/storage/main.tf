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

# INFRA-020: Cloud Storage for clinical documents
resource "google_storage_bucket" "clinical_docs" {
  name          = "${local.name_prefix}-clinical-docs"
  project       = var.project_id
  location      = var.region
  storage_class = "STANDARD"
  force_destroy = var.environment != "prod"

  uniform_bucket_level_access = true # SEC-021: Uniform access
  public_access_prevention    = "enforced"

  versioning {
    enabled = true
  }

  # SEC-010: Encryption at rest (Google-managed by default; CMK via var)
  encryption {
    default_kms_key_name = var.kms_key_id
  }

  # INFRA-022: Lifecycle — move to nearline after 1 year
  lifecycle_rule {
    condition {
      age = 365
    }
    action {
      type          = "SetStorageClass"
      storage_class = "NEARLINE"
    }
  }
}

# INFRA-023: Backup bucket
resource "google_storage_bucket" "backups" {
  name          = "${local.name_prefix}-db-backups"
  project       = var.project_id
  location      = var.region
  storage_class = "STANDARD"
  force_destroy = false

  uniform_bucket_level_access = true
  public_access_prevention    = "enforced"

  versioning {
    enabled = true
  }

  encryption {
    default_kms_key_name = var.kms_key_id
  }

  # SEC-032: 7-year retention for HIPAA
  retention_policy {
    retention_period = 220752000 # 7 years in seconds
    is_locked        = var.environment == "prod"
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
variable "region" {
  type = string
}
variable "kms_key_id" {
  type    = string
  default = ""
}

output "clinical_docs_bucket" {
  value = google_storage_bucket.clinical_docs.name
}

output "backups_bucket" {
  value = google_storage_bucket.backups.name
}
