variable "project_id" {
  type        = string
  description = "GCP project ID"
}

variable "project_name" {
  type        = string
  description = "Project name used in resource naming"
}

variable "environment" {
  type        = string
  description = "Deployment environment"

  validation {
    condition     = contains(["dev", "qa", "staging", "prod"], var.environment)
    error_message = "Environment must be one of: dev, qa, staging, prod."
  }
}

variable "region" {
  type        = string
  description = "GCP region"
}

variable "vpc_id" {
  type        = string
  description = "VPC network ID for private IP"
}

variable "database_tier" {
  type        = string
  description = "Cloud SQL machine tier"
  default     = "db-custom-1-3840"
}

variable "database_version" {
  type        = string
  description = "SQL Server version"
  default     = "SQLSERVER_2022_EXPRESS"
}

variable "enable_ha" {
  type        = bool
  description = "Enable high availability"
  default     = false
}

variable "sql_root_password" {
  type        = string
  description = "Root password for SQL Server"
  sensitive   = true
}

variable "backup_enabled" {
  type        = bool
  description = "Enable automated backups"
  default     = true
}
