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

variable "vpc_connector_subnet" {
  type        = string
  description = "Subnet name for VPC connector"
}

variable "container_image" {
  type        = string
  description = "Docker image URL for the backend service"
  default     = "gcr.io/placeholder/backend:latest"
}

variable "min_instance_count" {
  type        = number
  description = "Minimum instance count"
  default     = 0
}

variable "max_instance_count" {
  type        = number
  description = "Maximum instance count"
  default     = 2
}

variable "cpu" {
  type        = string
  description = "CPU allocation per instance"
  default     = "1"
}

variable "memory" {
  type        = string
  description = "Memory allocation per instance"
  default     = "512Mi"
}
