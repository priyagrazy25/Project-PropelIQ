locals {
  name_prefix = "${var.project_name}-${var.environment}"
}

# INFRA-001: App Service Plan for ASP.NET Core 8.0 backend
resource "azurerm_service_plan" "main" {
  name                = "${local.name_prefix}-asp"
  location            = var.location
  resource_group_name = var.resource_group_name
  os_type             = "Linux"
  sku_name            = var.app_service_sku
  tags                = var.tags
}

# INFRA-001: App Service running .NET 8 backend
resource "azurerm_linux_web_app" "backend" {
  name                = "${local.name_prefix}-app"
  location            = var.location
  resource_group_name = var.resource_group_name
  service_plan_id     = azurerm_service_plan.main.id
  https_only          = true # SEC-011: TLS enforced
  tags                = var.tags

  site_config {
    application_stack {
      dotnet_version = "8.0"
    }
    always_on        = var.app_service_sku != "B1" ? true : false
    ftps_state       = "Disabled" # Security: disable FTP
    minimum_tls_version = "1.2"  # SEC-011: TLS 1.2+
    http2_enabled    = true
    health_check_path = "/health/live" # OPS-003: Health check
  }

  # SEC-020: Managed Identity for service auth
  identity {
    type = "SystemAssigned"
  }

  app_settings = {
    "ASPNETCORE_ENVIRONMENT"            = var.environment == "prod" ? "Production" : "Development"
    "WEBSITE_RUN_FROM_PACKAGE"          = "1"
    "ApplicationInsights__Enabled"      = "true"
  }
}

# INFRA-001: VNet integration for App Service
resource "azurerm_app_service_virtual_network_swift_connection" "main" {
  app_service_id = azurerm_linux_web_app.backend.id
  subnet_id      = var.app_subnet_id
}

# INFRA-002: Auto-scaling for prod/staging
resource "azurerm_monitor_autoscale_setting" "main" {
  count               = var.enable_autoscale ? 1 : 0
  name                = "${local.name_prefix}-autoscale"
  resource_group_name = var.resource_group_name
  location            = var.location
  target_resource_id  = azurerm_service_plan.main.id
  tags                = var.tags

  profile {
    name = "default"

    capacity {
      default = var.min_instance_count
      minimum = var.min_instance_count
      maximum = var.max_instance_count
    }

    rule {
      metric_trigger {
        metric_name        = "CpuPercentage"
        metric_resource_id = azurerm_service_plan.main.id
        time_grain         = "PT1M"
        statistic          = "Average"
        time_window        = "PT5M"
        time_aggregation   = "Average"
        operator           = "GreaterThan"
        threshold          = 70
      }
      scale_action {
        direction = "Increase"
        type      = "ChangeCount"
        value     = "1"
        cooldown  = "PT5M"
      }
    }

    rule {
      metric_trigger {
        metric_name        = "CpuPercentage"
        metric_resource_id = azurerm_service_plan.main.id
        time_grain         = "PT1M"
        statistic          = "Average"
        time_window        = "PT10M"
        time_aggregation   = "Average"
        operator           = "LessThan"
        threshold          = 30
      }
      scale_action {
        direction = "Decrease"
        type      = "ChangeCount"
        value     = "1"
        cooldown  = "PT10M"
      }
    }
  }
}

# INFRA-006: Azure SignalR Service for real-time updates
resource "azurerm_signalr_service" "main" {
  name                = "${local.name_prefix}-signalr"
  location            = var.location
  resource_group_name = var.resource_group_name
  tags                = var.tags

  sku {
    name     = var.environment == "prod" ? "Standard_S1" : "Free_F1"
    capacity = 1
  }

  connectivity_logs_enabled = true
  messaging_logs_enabled    = true

  cors {
    allowed_origins = ["*"] # Restrict in production via app config
  }
}
