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
  default     = "us-east1"
}

variable "vpc_cidr" {
  type        = string
  description = "Primary CIDR for the VPC subnet"
  default     = "10.0.0.0/16"
}
