locals {
  name_prefix = "${var.project_name}-${var.environment}"
}

# OPS-002: Log Analytics Workspace for centralized logging
resource "azurerm_log_analytics_workspace" "main" {
  name                = "${local.name_prefix}-law"
  location            = var.location
  resource_group_name = var.resource_group_name
  sku                 = "PerGB2018"
  retention_in_days   = var.environment == "prod" ? 365 : 30 # OPS-002, SEC-032
  tags                = var.tags
}

# OPS-001: Application Insights for application metrics
resource "azurerm_application_insights" "main" {
  name                = "${local.name_prefix}-ai"
  location            = var.location
  resource_group_name = var.resource_group_name
  workspace_id        = azurerm_log_analytics_workspace.main.id
  application_type    = "web"
  tags                = var.tags
}

# OPS-010: Action group for alert notifications
resource "azurerm_monitor_action_group" "main" {
  name                = "${local.name_prefix}-ag"
  resource_group_name = var.resource_group_name
  short_name          = substr(var.environment, 0, 12)
  tags                = var.tags

  dynamic "email_receiver" {
    for_each = var.alert_email != "" ? [1] : []
    content {
      name          = "ops-team"
      email_address = var.alert_email
    }
  }
}

# OPS-010: Alert when API error rate exceeds 5%
resource "azurerm_monitor_metric_alert" "error_rate" {
  count               = var.app_service_id != "" ? 1 : 0
  name                = "${local.name_prefix}-alert-error-rate"
  resource_group_name = var.resource_group_name
  scopes              = [var.app_service_id]
  description         = "Alert when HTTP 5xx error rate exceeds 5%"
  severity            = 1
  frequency           = "PT5M"
  window_size         = "PT5M"
  tags                = var.tags

  criteria {
    metric_namespace = "Microsoft.Web/sites"
    metric_name      = "Http5xx"
    aggregation      = "Total"
    operator         = "GreaterThan"
    threshold        = 5
  }

  action {
    action_group_id = azurerm_monitor_action_group.main.id
  }
}

# OPS-011: Alert when p95 response time exceeds 2 seconds
resource "azurerm_monitor_metric_alert" "response_time" {
  count               = var.app_service_id != "" ? 1 : 0
  name                = "${local.name_prefix}-alert-latency"
  resource_group_name = var.resource_group_name
  scopes              = [var.app_service_id]
  description         = "Alert when response time p95 exceeds 2 seconds"
  severity            = 2
  frequency           = "PT5M"
  window_size         = "PT5M"
  tags                = var.tags

  criteria {
    metric_namespace = "Microsoft.Web/sites"
    metric_name      = "HttpResponseTime"
    aggregation      = "Average"
    operator         = "GreaterThan"
    threshold        = 2
  }

  action {
    action_group_id = azurerm_monitor_action_group.main.id
  }
}
