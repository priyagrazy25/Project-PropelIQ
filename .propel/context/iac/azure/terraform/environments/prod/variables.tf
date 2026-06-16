variable "project_name" {
  type        = string
  description = "Project name"
}

variable "environment" {
  type        = string
  description = "Deployment environment"
}

variable "location" {
  type        = string
  description = "Azure region"
}

variable "sql_admin_login" {
  type        = string
  description = "SQL admin login"
  sensitive   = true
}

variable "sql_admin_password" {
  type        = string
  description = "SQL admin password"
  sensitive   = true
}

variable "alert_email" {
  type        = string
  description = "Alert notification email"
  default     = ""
}
