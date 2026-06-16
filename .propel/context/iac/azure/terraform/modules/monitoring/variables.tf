variable "project_name" {
  type        = string
  description = "Project name used in resource naming"
}

variable "environment" {
  type        = string
  description = "Deployment environment"
}

variable "location" {
  type        = string
  description = "Azure region for resource deployment"
}

variable "resource_group_name" {
  type        = string
  description = "Name of the resource group"
}

variable "app_service_id" {
  type        = string
  description = "ID of the App Service to monitor"
  default     = ""
}

variable "alert_email" {
  type        = string
  description = "Email address for alert notifications"
  default     = ""
}

variable "tags" {
  type        = map(string)
  description = "Common tags applied to all resources"
  default     = {}
}
