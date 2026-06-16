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

variable "replication_type" {
  type        = string
  description = "Storage replication type: LRS for non-prod, GRS for prod"
  default     = "LRS"

  validation {
    condition     = contains(["LRS", "GRS", "ZRS", "GZRS"], var.replication_type)
    error_message = "Replication type must be LRS, GRS, ZRS, or GZRS."
  }
}

variable "data_subnet_id" {
  type        = string
  description = "Subnet ID for storage private endpoint"
}

variable "vnet_id" {
  type        = string
  description = "VNet ID for private DNS zone link"
}

variable "tags" {
  type        = map(string)
  description = "Common tags applied to all resources"
  default     = {}
}
