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

variable "app_subnet_id" {
  type        = string
  description = "Subnet ID for App Service VNet integration"
}

variable "app_service_sku" {
  type        = string
  description = "SKU for the App Service Plan"
  default     = "B1"

  validation {
    condition     = contains(["B1", "S1", "S2", "S3", "P1v3", "P2v3"], var.app_service_sku)
    error_message = "App Service SKU must be a valid tier."
  }
}

variable "min_instance_count" {
  type        = number
  description = "Minimum instance count for auto-scaling"
  default     = 1
}

variable "max_instance_count" {
  type        = number
  description = "Maximum instance count for auto-scaling"
  default     = 1
}

variable "enable_autoscale" {
  type        = bool
  description = "Enable auto-scaling for the App Service"
  default     = false
}

variable "tags" {
  type        = map(string)
  description = "Common tags applied to all resources"
  default     = {}
}
