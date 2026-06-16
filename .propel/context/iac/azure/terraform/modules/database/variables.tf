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

variable "location" {
  type        = string
  description = "Azure region for resource deployment"
}

variable "resource_group_name" {
  type        = string
  description = "Name of the resource group"
}

variable "data_subnet_id" {
  type        = string
  description = "Subnet ID for SQL private endpoint"
}

variable "vnet_id" {
  type        = string
  description = "VNet ID for private DNS zone link"
}

variable "sql_sku" {
  type        = string
  description = "Azure SQL DTU-based SKU name"
  default     = "S0"
}

variable "sql_max_size_gb" {
  type        = number
  description = "Maximum database size in GB"
  default     = 10
}

variable "sql_admin_login" {
  type        = string
  description = "SQL Server administrator login name"
  sensitive   = true
}

variable "sql_admin_password" {
  type        = string
  description = "SQL Server administrator password"
  sensitive   = true
}

variable "enable_zone_redundancy" {
  type        = bool
  description = "Enable zone redundancy for HA"
  default     = false
}

variable "backup_retention_days" {
  type        = number
  description = "Number of days to retain backups"
  default     = 7

  validation {
    condition     = var.backup_retention_days >= 7 && var.backup_retention_days <= 35
    error_message = "Backup retention must be between 7 and 35 days."
  }
}

variable "app_service_principal_id" {
  type        = string
  description = "Principal ID of the App Service managed identity for RBAC"
  default     = ""
}

variable "tags" {
  type        = map(string)
  description = "Common tags applied to all resources"
  default     = {}
}
