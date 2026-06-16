variable "project_id" {
  type        = string
  description = "GCP project ID"
}

variable "project_name" {
  type        = string
  description = "Project name"
  default     = "upaci"
}

variable "environment" {
  type        = string
  description = "Deployment environment"
  default     = "dev"
}

variable "region" {
  type        = string
  description = "GCP region"
  default     = "us-east1"
}

variable "sql_root_password" {
  type        = string
  description = "SQL root password"
  sensitive   = true
}
