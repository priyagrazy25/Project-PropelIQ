variable "project_name" {
  type        = string
  description = "Project name used in resource naming"

  validation {
    condition     = length(var.project_name) >= 3 && length(var.project_name) <= 24
    error_message = "Project name must be between 3 and 24 characters."
  }
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
  default     = "eastus2"
}

variable "resource_group_name" {
  type        = string
  description = "Name of the resource group"
}

variable "vnet_cidr" {
  type        = string
  description = "CIDR block for the Virtual Network"
  default     = "10.0.0.0/16"
}

variable "public_subnet_cidr" {
  type        = string
  description = "CIDR block for the public subnet"
  default     = "10.0.1.0/24"
}

variable "app_subnet_cidr" {
  type        = string
  description = "CIDR block for the application subnet"
  default     = "10.0.2.0/24"
}

variable "data_subnet_cidr" {
  type        = string
  description = "CIDR block for the data subnet"
  default     = "10.0.3.0/24"
}

variable "tags" {
  type        = map(string)
  description = "Common tags applied to all resources"
  default     = {}
}
